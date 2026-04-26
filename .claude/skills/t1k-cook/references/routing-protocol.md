---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Routing Protocol

## Standard Routing (All Registry-Routed Commands)

1. **Read resolved config:** Check for `.t1k-resolved-config.json` first (CLI-generated, pre-merged)
   - If exists: read `routing.{role}` for pre-resolved agent name
   - If absent: fall back to manual resolution below

2. **Manual resolution (fallback):**
   - Read ALL `.claude/t1k-routing-*.json` files
   - Sort by `priority` field (descending — higher number wins)
   - For each role, use the highest-priority registration found
   - Fallback to `t1k-routing-core.json` (p10) if role not found elsewhere

3. **If no registry files exist:** Use `AskUserQuestion` to ask user which agent to use

## Module-First Routing

In the module-first architecture, **modules are independently installed units** with their own versions. Routing considers which modules are installed (from `metadata.json` → `installedModules`).

**Mode 1 — Single-Module Task** (keywords match 0-1 installed modules):
- Standard highest-priority routing. One agent per role.
- Inject that module's skills into the agent prompt.

**Mode 2 — Multi-Module Task** (keywords match 2+ installed modules):
- Context-based routing. Each module's agent handles its own domain.
- Triggers multi-agent pipeline (parallel domain agents).
- Example: "combat UI" → dots-combat-implementer for logic + ui-developer for UI.

## Module Routing Overlays

Module routing overlays (from `module.json` → `routingOverlay` or CI-generated fragments):
- Module agents: p91+ (deeper dependency = higher priority, computed: `91 + dependency_depth`)
- Kit-wide agents: p90
- Core fallback: p10

## Commands Using This Protocol

| Command | Role(s) |
|---------|---------|
| `/t1k:cook` | `implementer`, `planner`, `project-manager`, `docs-manager`, `git-manager` |
| `/t1k:fix` | `implementer`, `debugger` |
| `/t1k:debug` | `debugger` |
| `/t1k:test` | `tester` |
| `/t1k:review` | `reviewer` |
| `/t1k:triage` | `reviewer`, `skills-manager` |
| `/t1k:plan` | `planner` |
| `/t1k:brainstorm` | `brainstormer` |
| `/t1k:docs` | `docs-manager` |
| `/t1k:git` | `git-manager` |
| `/t1k:modules` | `skills-manager` |
