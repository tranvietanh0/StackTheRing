---
origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
<!-- t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true -->

# Skill Domain Routing (T1K Core)

Intent-based discovery for T1K core skills. This file augments keyword-based activation with natural-language intent matching.

**How to use:** When the user's request matches an intent row, prefer the listed skill(s). This is advisory — the agent still has final authority.

For kit-specific intents (Unity, Cocos, web, etc.), see the kit-level `skill-domain-routing-{kit}.md` fragment.

For workflow chains (multi-step intents like "plan then implement"), see `skill-workflow-routing.md`.

## Planning & Architecture

User wants to...
- Break a task into phases with tasks → `/t1k:plan`
- Explore options before committing to an approach → `/t1k:brainstorm`
- Apply structured sequential reasoning through a complex problem → `/t1k:think`
- Debate via 5 expert personas before coding → `/t1k:predict`
- Generate edge cases or risk scenarios for a feature → `/t1k:scenario`
- Ask a technical question or get authoritative guidance → `/t1k:ask`

## Implementation

User wants to...
- Build a feature end-to-end (plan → code → test → review) → `/t1k:cook`
- Execute an existing plan phase → `/t1k:cook <plan-path>`
- Implement with test-driven discipline → `/t1k:cook --tdd`
- Architecture-critical deep planning before touching code → `/t1k:plan --deep`

## Debugging & Fixing

User wants to...
- Investigate a runtime error or unexpected behavior (root cause only) → `/t1k:debug`
- Fix a bug, test failure, or CI error → `/t1k:fix`
- Fix type errors, lint issues, or trivial compile errors → `/t1k:fix --quick`
- Get unstuck from a recurring bug or complexity spiral → `/t1k:problem-solve`
- Run exhaustive edge case generation before fix → `/t1k:scenario`

## Testing & Review

User wants to...
- Run the test suite and analyze failures → `/t1k:test`
- Adversarial code review with rigor → `/t1k:review`
- Security audit (STRIDE, OWASP Top 10) → `/t1k:security`

## Codebase Exploration

User wants to...
- Find files, code, or usages across the codebase → `/t1k:scout`
- Discover which skill handles a topic → `/t1k:find-skill`
- Explain code visually with diagrams or slides → `/t1k:preview`

## Documentation

User wants to...
- Create, update, or init project docs → `/t1k:docs`
- Generate visual previews, slides, or architecture diagrams → `/t1k:preview`
- Save session context for a handoff → `/t1k:handoff`

## Git & Release

User wants to...
- Stage and commit changes with conventional commit format → `/t1k:git cm`
- Full shipping pipeline (test → review → merge → tag) → `/t1k:ship`
- Create a pull request → `/t1k:git pr`
- Monitor a PR until it goes green and merges → `/t1k:babysit-pr`
- Manage git worktrees for parallel development → `/t1k:worktree`

## Kit & Registry Management

User wants to...
- Validate kit integrity across all doctor checks → `/t1k:doctor`
- Manage optional skill modules (add, remove, list, update) → `/t1k:modules`
- Kit maintenance operations (release, scaffold, audit, migrate) → `/t1k:kit`
- Triage GitHub issues and PRs across kit repos → `/t1k:triage`
- File a skill or agent bug report to the owning kit repo → `/t1k:issue`
- Sync local skill edits back to the origin kit repo → `/t1k:sync-back`
- Create or update a T1K skill → `/t1k:skill-creator`
- Create or update a T1K agent → `/t1k:agent-creator`

## Session & Context

User wants to...
- Review what was done this session / wrap up → `/t1k:watzup`
- See the full usage guide with live registry state → `/t1k:help`
- Optimize context window and token usage → `/t1k:context`

## Multi-Agent Orchestration

User wants to...
- Orchestrate parallel multi-session teammates → `/t1k:team`

## Notes

- For any intent not listed here, fall back to keyword-based activation via `t1k-activation-*.json`
- Kit-specific intents (Unity, Cocos, web, RN, designer) live in their own `skill-domain-routing-{kit}.md`
- Combine with workflow chains: `/t1k:plan` → `/t1k:cook` → `/t1k:test` → `/t1k:review`
