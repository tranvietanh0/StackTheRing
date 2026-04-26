---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Team Operations

## Tool Reference

### Agent Tool (spawn teammates)

```
Agent(
  subagent_type: "<registry-resolved-type>",
  description: "short task summary",
  prompt: "full instructions + T1K Context Block",
  model: "opus",                    # Required for Agent Teams teammates
  run_in_background: true,          # Non-blocking spawn
  isolation: "worktree"             # Git worktree isolation (cook devs)
)
```

**Note:** `Task` was renamed to `Agent` in v2.1.63. Both names work; prefer `Agent` for new code.

### Team Management Tools

| Tool | Purpose | Params |
|------|---------|--------|
| `TeamCreate` | Create team + shared task list | `team_name`, `description` |
| `TeamDelete` | Remove team resources | *none* |
| `TaskCreate` | Create work item | `subject`, `description`, `priority`, `addBlockedBy`, `addBlocks` |
| `TaskUpdate` | Claim/complete task | `taskId`, `status`, `owner`, `metadata` |
| `TaskGet` | Full task details | `taskId` |
| `TaskList` | All tasks (minimal fields) | *none* |
| `SendMessage` | Inter-agent messaging | `type`, `to`/`recipient`, `message` |

### SendMessage Types

| Type | Purpose |
|------|---------|
| `message` | DM to one teammate (requires `recipient`) |
| `broadcast` | Send to ALL teammates (use sparingly) |
| `shutdown_request` | Ask teammate to gracefully exit |
| `shutdown_response` | Teammate approves/rejects shutdown (requires `request_id`) |
| `plan_approval_response` | Lead approves/rejects teammate plan (requires `request_id`) |

## --delegate Mode

When `--delegate` flag is passed:
- Lead ONLY: spawns teammates, manages tasks, sends messages, synthesizes reports
- Lead NEVER: edits files, runs tests, executes git commands directly
- For cook Step 6 MERGE: spawn a dedicated merge teammate instead of lead doing it

## T1K Differentiators (vs CK `/team`)

| Aspect | CK `/team` | T1K `/t1k:team` |
|--------|-----------|------------------|
| Role resolution | Hardcoded `subagent_type` | Registry-routed via `t1k-routing-*.json` |
| Skill injection | None | Module-scoped per `subagent-injection-protocol.md` |
| File ownership | Manual glob patterns | Auto-derived from `.t1k-manifest.json` |
| Worktree | Optional | Mandatory for cook/debug |
| Module boundaries | Not checked | Reviewed for violations |
| Triage | Not available | Parallel cross-repo processing |

## Display Modes

| Mode | How | When |
|------|-----|------|
| `auto` (default) | Split panes if in tmux, otherwise in-process | Default |
| `in-process` | All in one terminal. `Shift+Up/Down` navigate. `Ctrl+T` task list. | No tmux |
| `tmux/split` | Each teammate gets own pane. Requires tmux or iTerm2. | Recommended for cook/debug |

Override with `--teammate-mode in-process` or `--teammate-mode split`.
**Incompatible:** Windows Terminal, basic SSH, serial consoles.

## Monitoring & Event Lifecycle

**Event order per teammate:**
```
SubagentStart -> [work...] -> TaskCompleted -> SubagentStop -> TeammateIdle
```

**Primary:** Event-driven hooks — TaskCompleted and TeammateIdle events auto-notify the lead.
**Fallback:** TaskList poll every 60s if no events received.
**Stuck:** If teammate unresponsive >5 min, SendMessage directly. If still stuck, shutdown and replace.

## Cross-Session Memory

Teammates retain learnings in `~/.claude/agent-memory/<name>/` (persists after TeamDelete).

Add `memory: project` to teammate's agent definition frontmatter. First 200 lines of `MEMORY.md` auto-injected at start.

## Worktree Isolation (Cook Template)

`isolation: "worktree"` gives each dev:
- **Own git worktree** — isolated working directory, staging area, HEAD
- **Own branch** — auto-created, returned in agent result
- **No file conflicts** — devs can edit same files independently

After all devs complete, lead merges branches sequentially.

## Token Budget Estimates

| Template | Teammates | Estimated Tokens |
|----------|-----------|-----------------|
| Research (3) | 3 | ~150K-300K |
| Review (3) | 3 | ~100K-200K |
| Cook (auto) | 2-5 | ~400K-800K |
| Debug (3) | 3 | ~200K-400K |
| Triage | 2-4 | ~200K-400K |

## Error Recovery

1. **Check status:** `Shift+Up/Down` (in-process) or click pane (split). Or TaskList.
2. **Redirect:** SendMessage with corrective instructions to specific teammate
3. **Replace:** Shutdown failed teammate, spawn replacement for same task
4. **Reassign:** TaskUpdate stuck task to unblock dependents
5. **Abort:** SendMessage(type: "shutdown_request") to all, then TeamDelete

## Abort & Cleanup

```
1. SendMessage(type: "shutdown_request") to each teammate
2. Wait for shutdown_response (or timeout 30s)
3. TeamDelete (no parameters)
```

**If unresponsive:** Close terminal or kill session. Then manually clean up:
- `rm -rf ~/.claude/teams/<team-name>/` — orphaned team state
- `git worktree list` -> `git worktree remove <path>` — orphaned worktrees

## Limitations

- **One team per session** — cannot manage multiple teams simultaneously
- **No nested teams** — teammates cannot spawn their own teams
- **Fixed lead** — no lead promotion/transfer during session
- **Opus 4.6 only** — all teammates must run same model
- **TTY required** — Agent Teams disabled in VSCode extension
- **Session resume broken** — `/resume` does not restore in-progress teammates
- **Instruction-based ownership** — file ownership enforced by prompt, not filesystem locks
- **No CI/CD mode** — Agent Teams requires interactive terminal
