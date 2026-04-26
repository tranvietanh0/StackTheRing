// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';
const { isKitError, _loadAgentNames, _extractFilePaths } =
  require('../lib/kit-error-classifier.cjs');

describe('kit-error-classifier — rule 1: T1K command (Bash)', () => {
  it('detects bare `t1k` command', () => {
    const r = isKitError({
      toolName: 'Bash',
      toolInput: { command: 't1k update --yes' },
      toolResult: 'error: update failed',
    });
    assertEqual(r.isKit, true);
    assertEqual(r.reason, 't1k-command');
  });

  it('detects /t1k: slash command reference', () => {
    const r = isKitError({
      toolName: 'Bash',
      toolInput: { command: 'echo calling /t1k:cook' },
      toolResult: 'error',
    });
    assertEqual(r.isKit, true);
    assertEqual(r.reason, 't1k-command');
  });

  it('does NOT match plain npm test', () => {
    const r = isKitError({
      toolName: 'Bash',
      toolInput: { command: 'npm test' },
      toolResult: '1 test failed',
    });
    assertEqual(r.isKit, false);
  });
});

describe('kit-error-classifier — rule 2: T1K agent (Task)', () => {
  it('detects registered agent subagent_type', () => {
    // Real agent from .claude/agents/
    const r = isKitError({
      toolName: 'Task',
      toolInput: { subagent_type: 'tester', prompt: 'run tests' },
      toolResult: 'failed',
    });
    assertEqual(r.isKit, true);
    assertEqual(r.reason, 't1k-agent');
  });

  it('ignores unknown subagent_type', () => {
    const r = isKitError({
      toolName: 'Task',
      toolInput: { subagent_type: 'made-up-agent-xyz-9999', prompt: 'do stuff' },
      toolResult: 'failed',
    });
    assertEqual(r.isKit, false);
  });
});

describe('kit-error-classifier — rule 3: T1K skill', () => {
  it('detects t1k- skill prefix', () => {
    const r = isKitError({
      toolName: 'Skill',
      toolInput: { skill: 't1k-cook' },
      toolResult: 'error',
    });
    assertEqual(r.isKit, true);
    assertEqual(r.reason, 'skill-invocation');
  });

  it('detects t1k: skill prefix', () => {
    const r = isKitError({
      toolName: 'Skill',
      toolInput: { skill: 't1k:debug' },
      toolResult: 'error',
    });
    assertEqual(r.isKit, true);
  });

  it('ignores non-t1k skills', () => {
    const r = isKitError({
      toolName: 'Skill',
      toolInput: { skill: 'nextjs' },
      toolResult: 'error',
    });
    assertEqual(r.isKit, false);
  });
});

describe('kit-error-classifier — rule 4: stack trace path', () => {
  it('detects .claude/hooks/ in error', () => {
    const r = isKitError({
      toolName: 'Bash',
      toolInput: { command: 'node script.js' },
      toolResult: 'Error at /mnt/x/.claude/hooks/something.cjs:42',
    });
    assertEqual(r.isKit, true);
    // May come back as stack-trace-path OR origin-metadata if the file exists
    if (r.reason !== 'stack-trace-path' && r.reason !== 'origin-metadata') {
      throw new Error(`unexpected reason: ${r.reason}`);
    }
  });

  it('detects .claude/skills/ in error', () => {
    const r = isKitError({
      toolName: 'Read',
      toolInput: { file_path: '/x' },
      toolResult: 'Failed reading .claude/skills/t1k-cook/SKILL.md',
    });
    assertEqual(r.isKit, true);
  });

  it('detects .claude/agents/ in error', () => {
    const r = isKitError({
      toolName: 'Bash',
      toolInput: { command: 'cat file' },
      toolResult: 'ENOENT: .claude/agents/planner.md missing',
    });
    assertEqual(r.isKit, true);
  });
});

describe('kit-error-classifier — rule 6: required MCP', () => {
  it('detects mcp__github__* failure', () => {
    const r = isKitError({
      toolName: 'mcp__github__create_issue',
      toolInput: { owner: 'x', repo: 'y' },
      toolResult: 'API rate limit exceeded',
    });
    assertEqual(r.isKit, true);
    assertEqual(r.reason, 'required-mcp');
  });

  it('detects mcp__context7__* failure', () => {
    const r = isKitError({
      toolName: 'mcp__context7__query-docs',
      toolInput: { library: 'react' },
      toolResult: 'timeout',
    });
    assertEqual(r.isKit, true);
    assertEqual(r.reason, 'required-mcp');
  });

  it('ignores non-required MCP', () => {
    const r = isKitError({
      toolName: 'mcp__chrome-devtools__click',
      toolInput: { selector: '#x' },
      toolResult: 'element not found',
    });
    assertEqual(r.isKit, false);
  });
});

describe('kit-error-classifier — negative cases', () => {
  it('returns isKit:false for vanilla npm error', () => {
    const r = isKitError({
      toolName: 'Bash',
      toolInput: { command: 'npm install express' },
      toolResult: 'npm ERR! code ENOTFOUND',
    });
    assertEqual(r.isKit, false);
  });

  it('returns isKit:false for TypeScript compile error', () => {
    const r = isKitError({
      toolName: 'Bash',
      toolInput: { command: 'tsc --noEmit' },
      toolResult: 'src/app.ts(5,10): error TS2304',
    });
    assertEqual(r.isKit, false);
  });

  it('returns isKit:false for Read tool on user file', () => {
    const r = isKitError({
      toolName: 'Read',
      toolInput: { file_path: '/tmp/x' },
      toolResult: 'File does not exist',
    });
    assertEqual(r.isKit, false);
  });

  it('fail-open on malformed input', () => {
    const r = isKitError({ toolName: null, toolInput: null, toolResult: null });
    assertEqual(r.isKit, false);
  });

  it('fail-open on thrown errors', () => {
    // Pass something that would normally throw during string operations
    const r = isKitError({
      toolName: 'Bash',
      toolInput: { command: { nested: 'object-not-string' } },
      toolResult: undefined,
    });
    assertEqual(typeof r.isKit, 'boolean');
  });
});

describe('kit-error-classifier — helpers', () => {
  it('loadAgentNames returns a Set with T1K agents', () => {
    const names = _loadAgentNames();
    if (!(names instanceof Set)) throw new Error('expected Set');
    // planner should be a registered agent in core
    if (!names.has('planner') && !names.has('debugger')) {
      throw new Error('no T1K agents found in registry: ' + Array.from(names).join(','));
    }
  });

  it('extractFilePaths finds .claude/ paths', () => {
    const paths = _extractFilePaths('Error at /x/.claude/hooks/foo.cjs:5 and ./src/app.js');
    if (!paths.some(p => p.includes('.claude/hooks/foo.cjs'))) {
      throw new Error('missed .claude path: ' + paths.join(','));
    }
  });
});
