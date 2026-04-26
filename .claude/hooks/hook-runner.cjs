#!/usr/bin/env node
// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';

/**
 * TheOneKit Hook Runner — Cross-platform hook bootstrapper with dedup
 *
 * Resolves the project root (git root or directory walk) and runs the
 * target hook script from the correct .claude/hooks/ location.
 *
 * Features:
 *   - Resolves project root even from subdirectories
 *   - Reads stdin (cross-platform, fd 0) and pipes to child hook
 *   - Auto-dedup: prevents double execution when installed at both global + project levels
 *   - Uses execFileSync to spawn hooks (stdin flows through correctly)
 *
 * Usage: node "$CLAUDE_PROJECT_DIR/.claude/hooks/hook-runner.cjs" <hook-name> [extra-args...]
 *
 * Source settings.json uses $CLAUDE_PROJECT_DIR (Claude Code env var, cross-platform).
 * The CLI's transformClaudePaths() converts to $HOME for global installs.
 */

const { execSync, execFileSync } = require('child_process');
const path = require('path');
const fs = require('fs');

/**
 * Find where hook .cjs files live (may differ from project root in global-only mode).
 * Strategy: __dirname walk first (most reliable), then CWD walk.
 */
function findHookDir() {
  // __dirname is always correct for .cjs files — hook-runner.cjs lives in .claude/hooks/
  const fromDirname = path.resolve(__dirname, '..', '..');
  if (fs.existsSync(path.join(fromDirname, '.claude', 'hooks'))) {
    return fromDirname;
  }
  return process.cwd();
}

/**
 * Find the user's actual project root.
 * Strategy: git rev-parse first (unconditional — no .claude/ check),
 * then CWD as fallback for non-git directories.
 */
function findProjectRoot() {
  // Strategy 1: git rev-parse (works in any subdirectory of a git repo)
  try {
    const gitRoot = execSync('git rev-parse --show-toplevel', {
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'ignore'],
      timeout: 3000,
      windowsHide: true
    }).trim();
    if (gitRoot) return gitRoot;
  } catch {}
  // Strategy 2: CWD (non-git directory)
  return process.cwd();
}

// Main
const hookName = process.argv[2];
if (!hookName) {
  process.stderr.write('hook-runner: missing hook name argument\n');
  process.exit(1);
}

const hookDir = findHookDir();
const projectRoot = findProjectRoot();
const hookPath = path.join(hookDir, '.claude', 'hooks', `${hookName}.cjs`);

if (!fs.existsSync(hookPath)) {
  // Silent exit — hook may not exist in all kits
  process.exit(0);
}

// Read stdin cross-platform (fd 0 works on Linux, macOS, AND Windows)
let stdin = '';
try {
  stdin = fs.readFileSync(0, 'utf8');
} catch {
  // No stdin available (e.g., some SessionStart/Stop hooks) — proceed with empty
}

// Dedup guard: prevent double execution when hook is registered in both global + project settings
// Claude Code fires hooks from both levels additively — first invocation creates lock, second exits
let _dedupHash = '';
let _isDuplicate = false;
try {
  const utilsPath = path.join(hookDir, '.claude', 'hooks', 'telemetry-utils.cjs');
  if (fs.existsSync(utilsPath)) {
    const { dedupGuard } = require(utilsPath);
    const result = dedupGuard(hookName, stdin);
    // Log AFTER the dedup decision — never before (logging itself must not affect dedup)
    try {
      const loggerPath = path.join(hookDir, '.claude', 'hooks', 'hook-logger.cjs');
      if (fs.existsSync(loggerPath)) {
        const { logHook } = require(loggerPath);
        // Compute a short hash for log correlation (same logic as dedupGuard internally)
        const crypto = require('crypto');
        _dedupHash = crypto.createHash('md5').update(hookName + (stdin || '')).digest('hex').slice(0, 8);
        _isDuplicate = !!result;
        logHook('hook-runner', { hookName: hookName, dedup: _isDuplicate ? 'duplicate' : 'first', dedupHash: _dedupHash });
      }
    } catch { /* fail-silent — log errors must never affect hook execution */ }
    if (result) {
      process.exit(0); // duplicate — already ran from the other settings level
    }
  }
} catch {
  // telemetry-utils.cjs not found or dedupGuard failed — proceed without dedup (fail-open)
}

try {
  // SessionStart hooks may do network I/O (kit updates, MCP health checks);
  // give them 30s. All other hooks keep the default 10s.
  const isSessionStart = hookName === 'SessionStart';
  execFileSync(process.execPath, [hookPath, ...process.argv.slice(3)], {
    input: stdin,
    stdio: ['pipe', 'inherit', 'inherit'],
    timeout: isSessionStart ? 30000 : 10000,
    cwd: projectRoot,
    windowsHide: true,
    env: {
      ...process.env,
      T1K_HOOK_DIR: hookDir,
      T1K_PROJECT_ROOT: projectRoot,
    },
  });
} catch (err) {
  // Exit code 2 = intentional block (PreToolUse security hooks). Propagate without logging as error.
  if (err.status === 2) {
    process.exit(2);
  }
  // All other errors: fail-open. Log for telemetry so we can track hook health.
  try {
    const telemetryDir = path.join(hookDir, '.claude', 'telemetry');
    if (!fs.existsSync(telemetryDir)) fs.mkdirSync(telemetryDir, { recursive: true });
    const date = require('./telemetry-utils.cjs').todayDateStr();
    const errFile = path.join(telemetryDir, `hook-errors-${date}.jsonl`);
    const stderr = (err.stderr || '').toString().substring(0, 300);
    const entry = {
      ts: new Date().toISOString(),
      hook: hookName,
      error: (err.message || 'unknown').substring(0, 200),
      stderr: stderr,
      exitCode: err.status || null,
      timeout: err.killed || false,
    };
    fs.appendFileSync(errFile, JSON.stringify(entry) + '\n');
    process.stderr.write(`[t1k:hook-error] ${hookName} exit=${err.status || '?'}: ${(err.message || 'unknown').substring(0, 100)}\n`);
  } catch { /* telemetry logging itself failed — truly give up */ }
}

process.exit(0);
