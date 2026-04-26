#!/usr/bin/env node
// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
/**
 * skill-commit-gate.cjs - Block git commit when skill files are staged but not synced
 *
 * PreToolUse hook for Bash tool.
 * When `git commit` is detected and .claude/skills/ files are staged,
 * blocks with a reminder to run /t1k:sync-back --dry-run first.
 *
 * Standalone — no shared lib dependencies. Ships with theonekit-core.
 */
'use strict';
try {
  const { execSync } = require('child_process');
  const { parseHookStdin } = require('./telemetry-utils.cjs');

  const hookData = parseHookStdin();
  if (!hookData) process.exit(0);

  const { tool_name: toolName, tool_input: toolInput } = hookData;

  // Only check Bash commands
  if (toolName !== 'Bash' || !toolInput?.command) process.exit(0);

  const cmd = toolInput.command.trim();

  // Only check git commit commands
  if (!/git\s+commit/.test(cmd)) process.exit(0);

  // Get staged files
  let stagedFiles;
  try {
    const output = execSync('git diff --cached --name-only --diff-filter=M', {
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'ignore'],
      timeout: 5000,
    });
    stagedFiles = output.trim().split('\n').filter(Boolean);
  } catch {
    process.exit(0);
  }

  // Check if any staged files are under .claude/skills/
  const stagedSkillFiles = stagedFiles.filter(f => f.startsWith('.claude/skills/'));
  if (stagedSkillFiles.length === 0) process.exit(0);

  // Skip gate if we're in the origin kit repo (sync-back doesn't apply)
  try {
    const fs = require('fs');
    const metadata = JSON.parse(fs.readFileSync('.claude/metadata.json', 'utf8'));
    const remoteUrl = execSync('git remote get-url origin', { encoding: 'utf8', stdio: ['pipe', 'pipe', 'ignore'] }).trim();
    if (metadata.repository && remoteUrl.includes(metadata.repository)) {
      process.exit(0); // Origin repo — sync-back not needed
    }
  } catch {
    // Can't determine origin — fall through to gate
  }

  // Block — staged skill files need sync-back check
  console.error(`
\x1b[33mSYNC-BACK GATE\x1b[0m: Skill files staged for commit:

${stagedSkillFiles.map(f => `  \x1b[33m!\x1b[0m ${f}`).join('\n')}

  \x1b[34mBefore committing:\x1b[0m Run /t1k:sync-back --dry-run
  If changes are generic (not project-specific), run /t1k:sync-back to create PR.
  If already synced or project-specific, proceed with the commit.
`);
  process.exit(2); // Block — requires user approval to proceed
} catch {
  process.exit(0); // Fail-open
}
