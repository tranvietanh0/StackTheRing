---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# CLI Auto-Update

TheOneKit CLI (`t1k`) auto-updates itself in the background at session start.

## How It Works

`check-cli-updates.cjs` fires on `SessionStart` (after `check-kit-updates.cjs`):

1. Reads `cli.repo` and `cli.npmPackage` from any `t1k-config-*.json` fragment
2. Locates the `t1k` binary on PATH (`which` / `where`)
3. Parses `t1k --version` → current semver
4. Queries `gh release view --repo <cli.repo>` → latest release tag
5. Compares versions:
   - **Equal or ahead** → silent exit, cache refreshed
   - **Major bump** (default behavior, `autoUpdateMajor: true`) → same auto-update path as minor/patch
   - **Major bump** (when `autoUpdateMajor: false`) → prints `[t1k:cli-major]` notice; user must run `t1k update` manually
   - **Minor / patch bump** → spawns a **detached** `t1k update --yes --cli-only` (or `t1k update --yes` on pre-2.5.0 CLIs) whose stdout/stderr stream to the rolling log with `NO_COLOR=1` / `FORCE_COLOR=0` / `TERM=dumb` for readable non-ANSI output; the current session keeps using the old binary, the new one activates on next session start

### --cli-only flag and version-gate

The `--cli-only` flag ships in theonekit-cli ≥ 2.5.0. It suppresses the post-update kit content cascade (`promptKitUpdate`), which would otherwise re-init the global `~/.claude/` kit under `--yes`. The hook version-gates the flag:

- **CLI ≥ 2.5.0**: spawns `t1k update --yes --cli-only` — CLI binary is upgraded, zero kit content side effects.
- **CLI < 2.5.0**: spawns `t1k update --yes` (legacy) — the upgrade happens but the cascade may still fire once. The next session, now on 2.5.0+, will use `--cli-only` going forward.

This graceful degradation keeps users on old CLIs unblocked while delivering the fix automatically once they upgrade.

## Config

Declared in `t1k-config-core.json`:

```json
{
  "cli": {
    "repo": "The1Studio/theonekit-cli",
    "npmPackage": "@the1studio/theonekit-cli"
  }
}
```

Kits do NOT need to declare this — core owns the CLI repo reference.

## Opt-Out

Shared with kit auto-update. Any `t1k-config-*.json` can disable both:

```json
{ "features": { "autoUpdate": false } }
```

### Major-Only Opt-Out

To keep minor/patch auto-updates but require manual action for major bumps (e.g., to review breaking changes), set:

```json
{ "features": { "autoUpdateMajor": false } }
```

Default: `true` (majors are auto-applied just like minor/patch). When `false`, majors fall back to the legacy notify-only behavior with the `[t1k:cli-major]` / `[t1k:major-update]` tags. Applies to both CLI binary and kit content (flat and modular).

## Cache

- File: `~/.claude/.cli-update-check-cache`
- TTL: 24 hours
- Global scope — one check per user, not per project

## Log

- File: `~/.claude/.cli-update.log`
- Rolling, capped at ~100KB (keeps the last half when it overflows)
- Each run appends a timestamped header + full `t1k update` output
- Inspect manually after a background update: `cat ~/.claude/.cli-update.log`

## Safeguards

| Guard | Behavior |
|---|---|
| **No `t1k` on PATH** | Silent exit — user is likely running from source |
| **CWD git remote matches `cli.repo`** | Silent exit — never self-update the CLI from its own source tree |
| **Cache hit (< 24h)** | Silent exit |
| **`gh` not authenticated / network error** | Fail-open, cache refreshed, retry next day |
| **Spawn fails (EACCES, PATH error, etc.)** | Logged to `.cli-update.log`, session continues |
| **Any uncaught error** | Fail-open, exit 0 |

## Dry-Run (for debugging)

```bash
rm -f ~/.claude/.cli-update-check-cache
T1K_CLI_UPDATE_NOOP=1 node .claude/hooks/check-cli-updates.cjs
```

Emits the `[t1k:cli-update]` tag and writes to the log, but does NOT spawn the real update. Useful for verifying version detection, gh lookup, and comparison logic without mutating the CLI install.

## What The AI Should Do When It Sees `[t1k:cli-update]`

- Note the version bump for the user
- Do NOT run `t1k update` — it is already running in the background
- Remind the user to restart their shell / session after the update log shows completion
- Suggest inspecting `~/.claude/.cli-update.log` if anything seems wrong on the next session

## What The AI Should Do When It Sees `[t1k:cli-major]`

This tag only appears when `features.autoUpdateMajor: false` is set.

- Surface the notice to the user
- Offer to run `t1k update` interactively so they can review release notes and any breaking changes
- Do NOT spawn a background update for major bumps when this tag is emitted — the opt-out is explicit

## What The AI Should Do When It Sees `[t1k:major-update]` (kit content)

Emitted by `check-kit-updates.cjs` when a kit or module has a major bump AND `features.autoUpdateMajor: false`.

- Surface the notice to the user with kit/module name and version range
- Offer to run the suggested `gh release download` command
- If migrating from an old schema (e.g., registry v1→v2), also recommend running `/t1k:doctor fix` after the update
