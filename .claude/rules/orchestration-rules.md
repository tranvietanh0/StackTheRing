---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# TheOneKit Orchestration Rules

Classify every user request and route to the matching T1K command. Read `t1k-routing-*.json` fragments to resolve role→agent.

## Decision Tree

```
User Request → Classify:

 1. FEATURE / IMPLEMENT       → /t1k:cook
 2. PLANNING / ARCHITECTURE   → /t1k:plan
 3. BUG / ERROR / COMPILE     → /t1k:fix
 4. RUN TESTS                 → /t1k:test
 5. INVESTIGATE / DEBUG       → /t1k:debug
 6. CODE REVIEW               → /t1k:review
 7. DOCUMENTATION             → /t1k:docs
 8. GIT OPERATIONS            → /t1k:git (cm|cp|pr|merge)
 9. SKILL / AGENT MANAGEMENT  → /t1k:issue, /t1k:sync-back
10. TRIAGE ISSUES / PRs       → /t1k:triage
11. BRAINSTORM / IDEATION     → /t1k:brainstorm
12. TECHNICAL QUESTION        → /t1k:ask
13. EXPLORE CODEBASE          → /t1k:scout
14. SESSION REVIEW            → /t1k:watzup
15. REGISTRY VALIDATION       → /t1k:doctor
16. USAGE GUIDE               → /t1k:help
17. MODULE MANAGEMENT         → /t1k:modules
18. PARALLEL MULTI-AGENT      → /t1k:team
19. STUCK / BLOCKED           → /t1k:problem-solve
20. STRUCTURED REASONING      → /t1k:think
```

## Priority Order

1. **T1K Commands** (registry-routed workflows)
2. **Skills** (auto-activated by keyword context)
3. **Standard Tools** (Read, Write, Edit, Bash — trivial tasks only)

## Mandatory Skill Usage (NEVER bypass)

- `/t1k:sync-back` — sync .claude/ changes to kit repos. Background sub-agent only.
- `/t1k:issue` — report skill/agent bugs. Background sub-agent only.
- `/t1k:triage` — process issues/PRs. Never manually browse issues.
- `/t1k:git` — git operations. Never raw git commit/push without security checks.
