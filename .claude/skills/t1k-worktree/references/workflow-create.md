---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Workflow: Create Worktree

## Step 1: Get Repo Info
```bash
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs info --json
```
Parse: `repoType`, `baseBranch`, `projects`, `worktreeRoot`.

## Step 2: Detect Branch Prefix
- "fix", "bug", "error" → `fix`
- "refactor", "rewrite" → `refactor`
- "docs", "readme" → `docs`
- "test", "coverage" → `test`
- "chore", "deps" → `chore`
- "perf", "optimize" → `perf`
- Default → `feat`

## Step 3: Slug
"add authentication" → `add-auth`. Max 50 chars, kebab-case.

## Step 4: Monorepo
If monorepo and project not specified, use `AskUserQuestion` with project options.

## Step 5: Execute
```bash
# Standalone
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs create "<SLUG>" --prefix <TYPE>
# Monorepo
node $HOME/.claude/skills/t1k-worktree/scripts/worktree.cjs create "<PROJECT>" "<SLUG>" --prefix <TYPE>
```

## Step 6: Install Dependencies
Detect lockfile → run install in background.

## Notes
- Auto-detects superproject, monorepo, standalone repos
- Smart worktree location: superproject > monorepo > sibling
- Env templates (`.env*.example`) auto-copied on create
