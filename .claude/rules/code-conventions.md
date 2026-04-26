---
origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Code Conventions (Universal)

Applies to ALL languages and frameworks. Kit-specific rules extend this in `code-conventions-{kit}.md`.

## SOLID Principles
- **Single Responsibility:** one class/function = one reason to change
- **Open/Closed:** extend via composition, not modification
- **Liskov Substitution:** subtypes must be substitutable for base types
- **Interface Segregation:** prefer small, focused interfaces
- **Dependency Inversion:** depend on abstractions, not concretions

## Naming
- Names must be self-documenting — if a name needs a comment, rename it
- Booleans: use `is`, `has`, `can`, `should` prefixes
- Functions: use verbs — `getUser`, `calculateTotal`, `validateInput`
- Avoid abbreviations except widely known ones (`id`, `url`, `api`)

## Structure
- One class/component per file (small related types may share)
- Max 200 lines per file — split if larger
- Guard clauses over nested if/else — return early
- Prefer composition over inheritance
- Prefer immutability (`const`, `readonly`, `final`) by default

## Code Quality
- No magic numbers — extract to named constants or config
- No empty catch blocks — handle or rethrow with context
- No `TODO` in merged code — track in issues instead
- Import order: stdlib → external packages → internal modules
- Comments only where logic isn't self-evident — code should be readable without them

## Data-Driven Over Hardcoded
- **NEVER hardcode mappings** (command→skill, command→role, command→agent, keyword→module) in hooks or scripts
- Always **read from registry files** at runtime: activation fragments, SKILL.md, routing JSON, config fragments
- When new skills/agents/modules are added, the system must auto-discover them — no code updates needed
- Use shared constants (e.g., `T1K` from `telemetry-utils.cjs`) for file names and prefixes
- **Test:** If deleting your static map breaks nothing because the data comes from files → correct. If it breaks → you're hardcoding.

## No Duplicated Logic
- Before writing a utility function, search for existing ones in shared modules (`telemetry-utils.cjs`, `lib/`)
- If the same pattern appears in 2+ files, extract to a shared module immediately — not "later"
- Each `.claude/` path resolution must use `resolveClaudeDir()` — no inline `path.join(cwd, '.claude')` checks
- `null` / `undefined` guards must be applied where data flows between systems (e.g., JSONL → Set → JSON → DB)

## No Derived Fields — SSOT for Data
- **NEVER store a value that can be computed from other columns.** Derived fields violate SSOT: they can drift from their source, waste storage, and double the maintenance cost on every update.
- If `C = f(A, B)` and `A` and `B` are already stored, do NOT also store `C`. Compute it in the query instead.
- **Database example:** store `total_tokens` and `window_size`; compute `percent = 100.0 * total_tokens / window_size` in the SELECT. Do not add a `percent` column.
- **Code example:** if an object has `width` and `height`, do not also store `area`. Expose it as a getter or compute at use site.
- **Exception — materialized for performance only:** if profiling proves the computation is a real bottleneck AND the source columns rarely change, a derived column is acceptable, but it MUST be kept in sync via trigger/constraint/CI, AND the derivation formula MUST be documented inline.
- **Test:** if you can delete the column and reconstruct every value from the remaining columns with a single deterministic expression, the column is derived and should not exist.

## Testing
- Test public behavior, not implementation details
- Each test should be independent — no shared mutable state
- Name tests descriptively: `should_returnError_when_inputIsNull`

## Living Document
If unsure about a convention not covered here, ask the user for their preference and update this file with the answer. Conventions grow from real decisions.
