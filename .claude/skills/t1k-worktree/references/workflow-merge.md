---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Workflow: Merge (PR-based)

Merges a worktree's branch back to base via GitHub PR. Handles the worktree constraint
(target branch checked out elsewhere) by using `gh pr` instead of local merge.

**Why PR-based:** In worktrees, the target branch (e.g., master) is checked out in the main worktree,
so you can't `git checkout master` here. Using `gh pr merge` avoids this entirely.

## Options
- `--target <branch>` — Target branch (default: base branch from `info`)
- `--delete` — Delete worktree after merge (default: keep)
- `--reset` — Reset worktree branch to target after merge (default: no reset)
- `--squash` — Squash merge (default, tries squash first, falls back to rebase)

## Step 1: Pre-merge checks
```bash
# Get repo info for base branch
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs info --json
# Check dirty state
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs diff --worktree "<NAME>" --json
```
- If dirty: commit or stash uncommitted changes first
- If behind base: warn user, recommend `sync` first

## Step 2: Push branch
```bash
git push origin <branch>
```

## Step 3: Create PR (if none exists)
```bash
gh pr list --head <branch> --state open --json number
# If no open PR:
gh pr create --base <target> --head <branch> --title "<title>" --body "<body>"
```
- Auto-generate PR title from branch name or commit summary
- Body: list commits, changed files count

## Step 4: Merge PR
```bash
# Try squash first (most repos prefer this)
gh pr merge <number> --squash
# If squash disallowed, try rebase
gh pr merge <number> --rebase
# If rebase disallowed, try merge
gh pr merge <number> --merge
```

## Step 5: Post-merge (optional flags)
```bash
# --reset: Reset worktree branch to match target
git fetch origin
git reset --hard origin/<target>

# --delete: Remove worktree entirely
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs remove "<NAME>"
```

## Step 6: Update main worktree
```bash
# Pull latest in main worktree so it has the merged changes
cd <main-worktree-path> && git pull origin <target>
```

## Error Handling

| Error | Action |
|-------|--------|
| Merge commits not allowed | Try `--squash`, then `--rebase` |
| PR has conflicts | Run `sync` to rebase first, re-push |
| Branch not pushed | Push before creating PR |
| Dirty worktree | Commit or stash first |
