// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
/**
 * kit-error-collector-e2e.test.cjs — End-to-end dry-run tests for the
 * auto-issue pipeline. Covers classifier rules, sanitizer, dedup, rate limit,
 * and secret leakage. LIVE1 (real scratch repo) is skipped — documented in
 * plans/reports/e2e-dry-run-report.md as a manual follow-up.
 */
'use strict';
const fs = require('fs');
const path = require('path');
const os = require('os');
const { execFileSync } = require('child_process');

const HOOK = path.join(__dirname, '..', 'telemetry-kit-error-collector.cjs');
const CORE_ROOT = path.join(__dirname, '..', '..', '..');
const TELEMETRY_DIR = path.join(CORE_ROOT, '.claude', 'telemetry');
const CONFIG_PATH = path.join(CORE_ROOT, '.claude', 't1k-config-core.json');

// Unique isolated paths for tests
const TEST_CACHE = path.join(os.tmpdir(), `t1k-e2e-cache-${process.pid}.json`);
const TEST_RATE_DIR = path.join(os.tmpdir(), 't1k-auto-issue');

function resetTelemetry() {
  // Clean up kit-errors-*.jsonl and pending submissions
  try {
    if (fs.existsSync(TELEMETRY_DIR)) {
      for (const f of fs.readdirSync(TELEMETRY_DIR)) {
        if (f.startsWith('kit-errors-') || f === 'pending-issue-submissions.jsonl') {
          try { fs.unlinkSync(path.join(TELEMETRY_DIR, f)); } catch {}
        }
      }
    }
  } catch {}
  // Clear fingerprint cache
  try { if (fs.existsSync(TEST_CACHE)) fs.unlinkSync(TEST_CACHE); } catch {}
  // Clear rate-limit counters
  try {
    if (fs.existsSync(TEST_RATE_DIR)) {
      for (const f of fs.readdirSync(TEST_RATE_DIR)) {
        try { fs.unlinkSync(path.join(TEST_RATE_DIR, f)); } catch {}
      }
    }
  } catch {}
}

function enableFlag() {
  const cfg = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf8'));
  cfg.features.autoIssueSubmission = true;
  fs.writeFileSync(CONFIG_PATH, JSON.stringify(cfg, null, 2));
}

function disableFlag() {
  const cfg = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf8'));
  cfg.features.autoIssueSubmission = false;
  fs.writeFileSync(CONFIG_PATH, JSON.stringify(cfg, null, 2));
}

/**
 * Run the hook with a payload and capture outputs.
 * @returns {{ exitCode, stdout, stderr, jsonlEntries, pendingEntries }}
 */
function runHook(payload, { dryRun = false, sessionId = null } = {}) {
  const env = {
    ...process.env,
    T1K_KIT_ERROR_CACHE_PATH: TEST_CACHE,
  };
  if (dryRun) env.T1K_AUTO_ISSUE_DRY_RUN = '1';
  if (sessionId) env.CLAUDE_SESSION_ID = sessionId;

  let exitCode = 0;
  let stdout = '';
  let stderr = '';
  try {
    stdout = execFileSync(process.execPath, [HOOK], {
      input: JSON.stringify(payload),
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'pipe'],
      env,
      windowsHide: true,
    });
  } catch (e) {
    exitCode = e.status || 1;
    stdout = e.stdout || '';
    stderr = e.stderr || '';
  }

  // Read telemetry outputs
  let jsonlEntries = [];
  let pendingEntries = [];
  try {
    for (const f of fs.readdirSync(TELEMETRY_DIR)) {
      if (f.startsWith('kit-errors-')) {
        const content = fs.readFileSync(path.join(TELEMETRY_DIR, f), 'utf8').trim();
        if (content) jsonlEntries.push(...content.split('\n').map(l => JSON.parse(l)));
      }
    }
    const pendingFile = path.join(TELEMETRY_DIR, 'pending-issue-submissions.jsonl');
    if (fs.existsSync(pendingFile)) {
      const content = fs.readFileSync(pendingFile, 'utf8').trim();
      if (content) pendingEntries = content.split('\n').map(l => JSON.parse(l));
    }
  } catch {}

  return { exitCode, stdout, stderr, jsonlEntries, pendingEntries };
}

// ═══════════════════════════════════════════════════════════════════
// TESTS
// ═══════════════════════════════════════════════════════════════════

describe('kit-error-collector-e2e — setup', () => {
  it('enables autoIssueSubmission for test run', () => {
    enableFlag();
    const cfg = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf8'));
    assertEqual(cfg.features.autoIssueSubmission, true);
  });
});

describe('kit-error-collector-e2e — positive scenarios (classifier rules)', () => {
  it('S1: T1K command error → reason=t1k-command', () => {
    resetTelemetry();
    const r = runHook({
      tool_name: 'Bash',
      tool_input: { command: 't1k cook implement feature' },
      tool_result: 'exit code 1\nTypeError: Cannot read properties of undefined',
    }, { dryRun: true, sessionId: 's1' });
    assertEqual(r.exitCode, 0);
    if (r.jsonlEntries.length !== 1) throw new Error(`expected 1 entry, got ${r.jsonlEntries.length}`);
    assertEqual(r.jsonlEntries[0].reason, 't1k-command');
    assertEqual(r.jsonlEntries[0].skipReason, 'dry-run');
  });

  it('S4: stack trace mentioning .claude/hooks → reason detected', () => {
    resetTelemetry();
    const r = runHook({
      tool_name: 'Bash',
      tool_input: { command: 'node script.js' },
      tool_result: 'Error: MODULE_NOT_FOUND\n    at /nonexistent/.claude/hooks/fake-hook.cjs:42',
    }, { dryRun: true, sessionId: 's4' });
    if (r.jsonlEntries.length !== 1) throw new Error(`expected 1 entry, got ${r.jsonlEntries.length}`);
    // Reason can be stack-trace-path OR origin-metadata (if file exists)
    const reason = r.jsonlEntries[0].reason;
    if (reason !== 'stack-trace-path' && reason !== 'origin-metadata') {
      throw new Error(`unexpected reason: ${reason}`);
    }
  });

  it('S6: required MCP failure → reason=required-mcp', () => {
    resetTelemetry();
    const r = runHook({
      tool_name: 'mcp__github__create_issue',
      tool_input: { owner: 'x', repo: 'y' },
      tool_result: 'exit code 1\nAPI rate limit exceeded',
    }, { dryRun: true, sessionId: 's6' });
    if (r.jsonlEntries.length !== 1) throw new Error(`expected 1 entry, got ${r.jsonlEntries.length}`);
    assertEqual(r.jsonlEntries[0].reason, 'required-mcp');
  });

  it('S2: T1K agent blocked → reason=t1k-agent', () => {
    resetTelemetry();
    const r = runHook({
      tool_name: 'Task',
      tool_input: { subagent_type: 'planner', prompt: 'plan X' },
      tool_result: 'Status: BLOCKED\nCannot proceed without more context',
    }, { dryRun: true, sessionId: 's2' });
    if (r.jsonlEntries.length !== 1) throw new Error(`expected 1 entry, got ${r.jsonlEntries.length}`);
    assertEqual(r.jsonlEntries[0].reason, 't1k-agent');
  });

  it('S3: T1K skill invocation error → reason=skill-invocation', () => {
    resetTelemetry();
    const r = runHook({
      tool_name: 'Skill',
      tool_input: { skill: 't1k-cook' },
      tool_result: '{"isError":true,"message":"exit code 1"}',
    }, { dryRun: true, sessionId: 's3' });
    if (r.jsonlEntries.length !== 1) throw new Error(`expected 1 entry, got ${r.jsonlEntries.length}`);
    assertEqual(r.jsonlEntries[0].reason, 'skill-invocation');
  });
});

describe('kit-error-collector-e2e — negative scenarios', () => {
  it('N1: vanilla npm test fail → no classification', () => {
    resetTelemetry();
    const r = runHook({
      tool_name: 'Bash',
      tool_input: { command: 'npm test' },
      tool_result: 'exit code 1\nTypeError: app is undefined at src/main.js:5',
    }, { dryRun: true, sessionId: 'n1' });
    assertEqual(r.exitCode, 0);
    assertEqual(r.jsonlEntries.length, 0);
    assertEqual(r.pendingEntries.length, 0);
  });

  it('N2: shell command not found → no classification', () => {
    resetTelemetry();
    const r = runHook({
      tool_name: 'Bash',
      tool_input: { command: 'lss /tmp' },
      tool_result: 'bash: lss: command not found',
    }, { dryRun: true, sessionId: 'n2' });
    assertEqual(r.jsonlEntries.length, 0);
  });

  it('N3: git merge conflict → no classification', () => {
    resetTelemetry();
    const r = runHook({
      tool_name: 'Bash',
      tool_input: { command: 'git merge feature' },
      tool_result: 'exit code 1\nCONFLICT (content): Merge conflict in src/app.js',
    }, { dryRun: true, sessionId: 'n3' });
    assertEqual(r.jsonlEntries.length, 0);
  });
});

describe('kit-error-collector-e2e — rate limit', () => {
  it('R1: 6 distinct errors → 5 submissions, 6th rate-limited', () => {
    resetTelemetry();
    const results = [];
    for (let i = 1; i <= 6; i++) {
      results.push(runHook({
        tool_name: 'Bash',
        tool_input: { command: `t1k cook scenario-r1-${i}` },
        tool_result: `exit code 1\nError at .claude/hooks/fake-${i}.cjs:${i}`,
      }, { dryRun: false, sessionId: 'r1-session' }));
    }
    // Count pending submissions (only first 5 should be there)
    const lastRun = results[results.length - 1];
    const submitted = lastRun.jsonlEntries.filter(e => !e.skipReason);
    const rateLimited = lastRun.jsonlEntries.filter(e => e.skipReason === 'rate-limited');
    if (submitted.length !== 5) {
      throw new Error(`expected 5 submitted, got ${submitted.length} (rateLimited=${rateLimited.length})`);
    }
    if (rateLimited.length !== 1) {
      throw new Error(`expected 1 rate-limited, got ${rateLimited.length}`);
    }
    // Pending submissions should also be 5
    if (lastRun.pendingEntries.length !== 5) {
      throw new Error(`expected 5 pending entries, got ${lastRun.pendingEntries.length}`);
    }
  });
});

describe('kit-error-collector-e2e — dedup', () => {
  it('D1: same error twice within session → second is local-duplicate', () => {
    resetTelemetry();
    const payload = {
      tool_name: 'Bash',
      tool_input: { command: 't1k cook dedup-test' },
      tool_result: 'exit code 1\nError at .claude/hooks/dedup-test.cjs',
    };
    runHook(payload, { dryRun: false, sessionId: 'd1' });
    const r2 = runHook(payload, { dryRun: false, sessionId: 'd1' });
    const dup = r2.jsonlEntries.filter(e => e.skipReason === 'local-duplicate');
    if (dup.length !== 1) {
      throw new Error(`expected 1 duplicate entry, got ${dup.length}`);
    }
    // Only 1 pending submission should exist (first run)
    if (r2.pendingEntries.length !== 1) {
      throw new Error(`expected 1 pending entry, got ${r2.pendingEntries.length}`);
    }
  });
});

describe('kit-error-collector-e2e — SEC1 (CRITICAL: secret leakage)', () => {
  it('SEC1: payload with secrets → NONE appear in output', () => {
    resetTelemetry();
    const secrets = [
      'sk-abcdefghij1234567890',
      'ghp_xyzXYZ12345678901234567',
      'AKIAIOSFODNN7EXAMPLE',
      'Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature',
      'hunter2',
      '/home/testuser/.ssh/id_rsa',
      '/home/testuser/.env',
    ];
    const r = runHook({
      tool_name: 'Bash',
      tool_input: {
        command: `t1k cook API_KEY=hunter2 TOKEN=ghp_xyzXYZ12345678901234567`,
      },
      tool_result: `exit code 1
Error: Authentication failed with sk-abcdefghij1234567890
AWS: AKIAIOSFODNN7EXAMPLE
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature
Could not read /home/testuser/.ssh/id_rsa
Could not read /home/testuser/.env`,
    }, { dryRun: false, sessionId: 'sec1' });

    if (r.jsonlEntries.length === 0) throw new Error('expected at least 1 entry');
    const serialized = JSON.stringify(r.jsonlEntries) + JSON.stringify(r.pendingEntries);

    for (const secret of secrets) {
      if (serialized.includes(secret)) {
        throw new Error(`SECRET LEAKED: "${secret}" found in output`);
      }
    }
  });
});

describe('kit-error-collector-e2e — feature flag kill switch', () => {
  it('disabled flag → hook exits 0 with no output', () => {
    resetTelemetry();
    disableFlag();
    const r = runHook({
      tool_name: 'Bash',
      tool_input: { command: 't1k cook test' },
      tool_result: 'exit code 1\nError at .claude/hooks/x.cjs',
    }, { dryRun: false, sessionId: 'killswitch' });
    assertEqual(r.exitCode, 0);
    assertEqual(r.jsonlEntries.length, 0);
    assertEqual(r.pendingEntries.length, 0);
    assertEqual(r.stdout, '');
  });
});

describe('kit-error-collector-e2e — teardown', () => {
  it('restores autoIssueSubmission flag to false', () => {
    disableFlag();
    resetTelemetry();
    // Cleanup fingerprint cache
    try { if (fs.existsSync(TEST_CACHE)) fs.unlinkSync(TEST_CACHE); } catch {}
    const cfg = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf8'));
    assertEqual(cfg.features.autoIssueSubmission, false);
  });
});
