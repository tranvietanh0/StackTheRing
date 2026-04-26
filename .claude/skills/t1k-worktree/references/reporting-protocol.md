---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Reporting Protocol

Every command MUST report before/after state to the user.

**Before execution:** Show current state relevant to the operation.
**After execution:** Show what changed.

## Per-Command Report Format

| Command | Before | After |
|---------|--------|-------|
| `create` | Repo type, base branch, worktree root | Created path, branch name, env files copied, next steps |
| `session` | Worktree path, branch, terminal detected | Launched confirmation, session command, layout (split panes) |
| `sync` | Per-worktree: branch, ahead/behind, dirty state | Per-worktree: rebase result (success/conflict/skipped), new ahead/behind |
| `envsync` | Source dir, env files found, target worktree count | Per-worktree per-file: copied/skipped/differs, total summary |
| `diff` | Total worktrees being compared | Per-worktree: ahead/behind, changed files list, dirty state, commit log |
| `status` | Total worktrees, base branch | Per-worktree: branch, dirty state, ahead/behind, env sync status |
| `remove` | Worktree path, branch name | Removed confirmation, branch deleted/kept |
| `merge` | Branch, dirty state, ahead/behind, existing PRs | PR created/found, merge result, post-merge actions |

## Summary Line (MANDATORY)

End every operation with:
```
Summary: X worktrees synced, Y skipped, Z conflicts
```

## Individual Workflow Commands

### Session
```bash
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs session "<NAME>" --json
```
Reports: worktree path, branch, session command (`cd <path> && claude`).
Then execute the session command for the user.

### Sync (Rebase)
```bash
# Sync all worktrees
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs sync --json
# Sync specific worktree
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs sync --worktree "<NAME>" --json
```
Reports per worktree: status (success/conflict/skipped), ahead/behind, conflicts.
Skips dirty worktrees. Auto-aborts failed rebases.

### Env Sync
```bash
# Sync from main worktree to all others
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs envsync --json
# Preview only
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs envsync --dry-run --json
# Custom source
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs envsync --source /path/to/source --json
```

### Diff
```bash
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs diff --json
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs diff --worktree "<NAME>" --json
```
Reports: commits ahead/behind base, changed files list, dirty state, commit log.

### Status
```bash
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs status --json
```
Combined view: branch, dirty state, ahead/behind, env sync status per worktree.
