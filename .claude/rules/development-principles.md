---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Development Principles

Universal principles for all TheOneKit projects. These apply to every kit, module, and project.

## SSOT — No Duplicates

Follow Single Source of Truth strictly. NEVER create duplicate methods, functions, classes, packages, modules, or files that serve the same purpose.

- Search existing codebase for similar functionality before creating new
- Reuse existing implementations — extend, don't duplicate
- One responsibility = one location in the codebase
- If duplicates found, consolidate immediately
- **No derived fields** — never store a value that can be computed from other columns/fields. If `C = f(A, B)` and `A` and `B` are stored, compute `C` at query/use time instead. Storing derived data doubles the update surface and guarantees eventual drift. See `rules/code-conventions.md` → "No Derived Fields" for the full rule and the one narrow exception (materialization for proven hot paths).

## Errors Over Silent Fallbacks

NEVER use silent fallbacks that hide errors. ALWAYS throw exceptions with clear error messages.

- Prefer explicit error states over invisible fallback behavior
- Silent fallbacks hide bugs and make debugging impossible
- The only acceptable fallback: explicitly documented, logged, and user informed

## Automate Over Manual — Git Is Truth

For any repetitive or pattern-based task, implement in CI/CD scripts that commit results back to git.

- No hidden state — everything visible in the repo
- CI transforms files (metadata, prefixes, versions) and commits back after each release
- Release artifacts are built from the already-transformed git state
- If the same manual change is needed in multiple places → automate it

## No-Override Rule

Files under `$HOME/.claude/` MUST have globally unique names across all kits and modules.

- No file from any kit/module may overwrite a file from another
- CI/CD auto-prefixes agent filenames: core=no prefix, kit-wide=`{kit-short}-`, module=`{kit-short}-{module}-`
- Already-prefixed files are skipped
- CI validates no collisions before release

## Test Pass Gate — Zero Failures Before Done

ALL unit tests MUST pass before reporting any task as "done." This applies to every engine, framework, and language.

- After ANY implementation: run the full test suite, not just compilation
- Zero test failures required — skipped/ignored tests are acceptable ONLY with documented justification (e.g., known engine bug)
- If tests fail: fix them as part of the current task, not as a follow-up
- NEVER report "done" with test failures pending, even if the failures seem unrelated
- When deleting or renaming any type: grep ALL source files (including tests) for references BEFORE deleting — broken references = broken tests
- When merging systems or consolidating code: verify test setup registrations still reference valid types

## Pre-Delete Reference Check

Before deleting or renaming ANY file, function, class, or type:

1. `grep -r "TypeName"` across ALL source files (runtime + tests + editor)
2. Update every reference BEFORE or alongside the deletion
3. Run tests after deletion to confirm zero breakage
4. If a test references a deleted type and the hook/agent missed it — that is a bug in the workflow, not an acceptable outcome

## Update Skills After Every Error

After encountering ANY error (compile, runtime, gotcha), ALWAYS update the relevant `$HOME/.claude/skills/` with a gotcha/warning entry BEFORE continuing implementation.

- Never face the same error twice
- Treat skill updates as part of the fix, not a separate task
- After updating: invoke `/t1k:sync-back` to propagate to kit repo
