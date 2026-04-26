---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Doctor Checks Reference

## Core Checks (#1–6)

1. **Role coverage** — every role in `t1k-routing-*.json` has a matching agent `.md` file
2. **Skill existence** — every skill in `t1k-activation-*.json` has a matching skill folder in `.claude/skills/`
3. **No cross-layer hardcoding** — scan `t1k-routing-*.json` values for engine-specific strings (dots-, unity-, cocos-)
4. **Manifest integrity** — `.t1k-manifest.json` matches actually installed files
5. **Registry version compat** — all `t1k-routing-*.json` and `t1k-activation-*.json` use `registryVersion: 1`
6. **Config completeness** — every command in `t1k-config-*.json` has a matching skill folder

## Module Checks (#7–17)

Follow protocol: `skills/t1k-modules/references/module-detection-protocol.md` — skip if no `installedModules` key or no metadata.

| # | Check | Validates |
|---|---|---|
| 7 | Module file ownership | Every skill file belongs to exactly one module via `.t1k-manifest.json` (no overlap) |
| 8 | Module dependency integrity | All declared dependencies (from module.json) are installed with compatible versions |
| 9 | Activation fragment match | Each installed module has activation source (module.json or t1k-activation-*.json) |
| 10 | Module agent presence | Each module declaring agents has matching `.md` files |
| 11 | Routing overlay validity | Module overlays reference only that module's agents |
| 12 | No stale module files | No files from uninstalled modules remain (cross-check manifests) |
| 13 | SessionBaseline in required module | `sessionBaseline` skills are in required modules only |
| 14 | Keyword uniqueness | No keyword maps to skills in two different modules |
| 15 | Routing priority uniqueness | No two module overlays override same role at same priority |
| 16 | Origin frontmatter match | In-file `origin` frontmatter matches metadata entry |
| 17 | Module frontmatter presence | Files in `modules/*/` have `module:` field in frontmatter matching parent dir |

## Manifest Checks (#21)

| # | Check | Validates |
|---|---|---|
| 21 | Module manifest integrity | Each installed module has `modules/{name}/manifest.json`; listed files exist at flat locations; no orphaned flat files |

**Check #21 details:**
1. For each installed module in metadata: verify `.claude/modules/{name}/manifest.json` exists
2. For each file in manifest: verify it exists at the flattened location
3. Scan `.claude/skills/` for dirs matching `{module}-*` pattern not in any manifest → orphaned
4. Severity: WARN (pre-flattening installs won't have manifests)

## SSOT & Structure Checks (#22–27)

| # | Check | Validates |
|---|---|---|
| 22 | schemaVersion present | `metadata.json` has `schemaVersion: 3` |
| 23 | Version presence | `metadata.json` has real `version` (not `"0.0.0-source"`) and `buildDate` (not `null`) |
| 24 | No stale root modules/ | No `modules/` at repo root alongside `.claude/modules/` (canonical) |
| 25 | Context requiredPaths set | Engine kits (unity/cocos/rn) have `context.requiredPaths` in config |
| 26 | Activation format modern | All `t1k-activation-*.json` use `mappings` array, not deprecated `keywords` object |
| 27 | v3 installedModules | CLI writes `installedModules` with `kit`, `repository`, `version` per module |

## No-Override Checks (#28–29)

| # | Check | Validates |
|---|---|---|
| 28 | Filename collision detection | No two installed kits/modules have same-named agents, skills, or rules. Group files by basename + read `origin` metadata. Exception: merge targets (metadata.json, t1k-modules.json, settings.json, CLAUDE.md). |
| 29 | Agent prefix correctness | Non-core agents have proper prefix: `{kit-short}-` (kit-wide) or `{kit-short}-{module}-` (module). Core agents have no prefix. |

**Check #28 details:**
1. Walk `.claude/agents/`, `.claude/skills/`, `.claude/rules/`
2. Read each file's `origin` metadata (frontmatter/`_origin`)
3. Group files by basename; if same basename with different `origin` values → ERROR: collision
4. Fix mode: suggest running CI auto-prefix or manual rename

**Check #29 details:**
1. For each agent in `.claude/agents/`, read `origin` field — derive expected kit-short
2. If origin != core: verify filename starts with `{kit-short}-`
3. If module agents: verify filename starts with `{kit-short}-{module}-`

## Frontmatter Quality Checks (#18–20)

| # | Check | Validates |
|---|---|---|
| 18 | Agent maxTurns presence | Every agent `.md` has `maxTurns:` in frontmatter |
| 19 | Skill effort presence | Every skill `SKILL.md` has `effort:` in frontmatter (low/medium/high) |
| 20 | Agent model appropriateness | Implementer/debugger agents should use `inherit` or `opus`; utility agents (git, docs) should use `sonnet` |

## Cross-Platform Checks (#30)

| # | Check | Validates |
|---|---|---|
| 30 | Hook cross-platform compliance | All `.cjs` files in `.claude/hooks/` are free of shell-only patterns |

**Check #30 details:**

Scan all `.cjs` files in `.claude/hooks/` for these violations:

| Pattern | Why It Fails | Fix |
|---------|-------------|-----|
| `2>/dev/null` in command strings | Shell redirect, not cross-platform | Use `stdio: ['pipe', 'pipe', 'ignore']` |
| `2>&1` in command strings | Shell redirect, not cross-platform | Capture both stdout/stderr via `stdio: ['pipe', 'pipe', 'pipe']` |
| `/dev/stdin` | Linux-only, breaks Windows | Use `fs.readFileSync(0, 'utf8')` |
| `/dev/null` (outside comments) | Unix-only | Use `stdio` option or `os.devNull` |
| `execSync('cmd arg')` (shell string) | Spawns shell, injection risk | Use `execFileSync('cmd', ['arg'])` |
| Hardcoded `/tmp/` | Unix-only temp path | Use `os.tmpdir()` |
| Hardcoded `/home/` or `/Users/` (in logic, not regex) | Platform-specific | Use `os.homedir()` or `process.env.HOME \|\| process.env.USERPROFILE` |

**Implementation:**
1. Read each `.cjs` file, strip comment lines (`//` and `/* */`)
2. Regex-match against violation patterns
3. Report file:line for each violation
4. Severity: WARN (hooks still work on Linux/macOS, just break on Windows)

**Fix mode:** Cannot auto-fix — requires manual code changes. Report violations with suggested replacement.

## Sync-back Health Checks (#32)

| # | Check | Validates |
|---|---|---|
| 32 | Sync-back PR health | Recent `/t1k:sync-back` PRs are healthy — no CONFLICTING state and no phantom-file (all-additions) diffs |

**Check #32 details:**

Validates that the `/t1k:sync-back` skill is producing healthy PRs. Added after the 2026-04-09 incident where two sync-back PRs were unusable: core#7 was stale (no upstream fetch → CONFLICTING), unity#7 targeted a non-existent path (missing `.claude/` prefix → phantom file at wrong location).

1. Collect all kit repos from `.claude/t1k-config-*.json` → `repos.primary` and from in-file `repository` frontmatter across changed files
2. For each repo (up to 10 distinct repos to bound runtime), query the last 5 PRs with sync-back branch prefix:
   ```
   gh pr list --repo {owner}/{repo} --search "head:t1k-sync/" --state all --limit 5 --json number,title,state,mergeStateStatus,headRefName,additions,deletions,files
   ```
3. For each returned PR, check two signatures:
   - **Staleness signature** — `mergeStateStatus == "CONFLICTING"` while the PR is still `OPEN` → WARN: stale sync-back PR (fix: the skill pushed without fetching upstream)
   - **Phantom-file signature** — any file in the PR has `additions > 0` AND `deletions == 0` AND the filename matches a skill/agent/rule basename that exists elsewhere in the repo → WARN: likely path-resolution bug (fix: verify `.claude/` prefix for modular kits)
4. Report counts: `Sync-back health: {healthy}/{checked} PRs healthy across {N} repos`
5. List problem PRs with URL and signature

**Severity:** WARN (advisory — doesn't fail doctor, just flags drift)

**Skip conditions (fail-open, never block):**
- `gh` CLI not available → skip with note
- `gh auth status` not authenticated → skip with note
- No kit repos resolvable from configs → skip
- Network error during PR query → skip with note

**Fix mode:** Cannot auto-fix — each problem PR needs manual review. For each flagged PR:
- Stale → close and re-run `/t1k:sync-back` (v1.2.0+ has staleness check)
- Phantom-file → close and re-run `/t1k:sync-back` (v1.2.0+ has `.claude/` prefix + path verification)
- Suggest: `gh pr close {number} --comment "Superseded by healthy resync"`

**Why this check exists:** The acceptance criteria for The1Studio/theonekit-core#8 require a doctor check or test that detects these two failure modes in historical PRs. Running this check after releasing a sync-back fix is a cheap smoke-test to confirm no broken PRs slipped through.

## MCP Health Checks (#31)

| # | Check | Validates |
|---|---|---|
| 31 | MCP server connectivity | All required MCPs are connected; recommended MCPs present |

**Check #31 details:**

1. Read ALL `t1k-config-*.json` → collect `mcp.required` and `mcp.recommended` entries (additive across kits)
2. Deduplicate by `name` (higher-priority config wins on conflict)
3. Run `claude mcp list` to get connected servers
4. Also check `~/.claude/mcp.json` and `.mcp.json` for registered servers
5. For each **required** MCP not connected:
   - Output: `ERROR: Required MCP "{name}" not connected — {purpose}`
   - Suggest: `Fix: {installCmd}`
6. For each **recommended** MCP not connected:
   - Output: `WARN: Recommended MCP "{name}" not connected — {purpose}`
7. If entry has `verifyTool` field:
   - Check if deferred tools with that prefix exist via `ToolSearch`
   - If MCP is registered but no tools found: `WARN: MCP "{name}" registered but not functional (may need auth)`
8. Summary line: `MCP health: {N}/{total} required connected, {M} recommended missing`

**Severity:**
- Missing required: ERROR (fails doctor check)
- Missing recommended: WARN (advisory)
- Registered but not functional: WARN (advisory)

**Fix mode:**
- For each missing MCP with `installCmd`: run the install command via `claude mcp add ... -s user`
- After install: verify with `claude mcp get {name}`
- If `verifyTool` exists: verify deferred tools are available
- Suggest `! claude mcp auth {name}` if MCP needs authentication

### Frontmatter Check Output
```
### Frontmatter Quality
- Agent maxTurns: [PASS | WARN — N agents missing maxTurns: {list}]
- Skill effort: [PASS | WARN — N skills missing effort: {list}]
- Agent model: [PASS | WARN — {agent} uses {model} but role suggests {recommended}]
```
