#!/usr/bin/env node
// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
/**
 * check-kit-updates.cjs — Auto-update installed kits/modules at session start.
 *
 * PRIMARY PATH: Detect updates → spawn `t1k update --yes` (CLI handles everything:
 *   extraction, deletions, ownership, metadata, both global + project scopes).
 * FALLBACK PATH: If CLI binary not found, do manual gh download + extractZip.
 *
 * No time-based cache — checks every session. Opt-out: features.autoUpdate: false.
 * Self-update guard: skips repos matching CWD's git remote.
 * Coordination: writes marker so check-cli-updates.cjs can skip redundant work.
 */

const fs = require('fs');
const path = require('path');
const os = require('os');
const { execSync, execFileSync, spawn } = require('child_process');
const { extractZip } = require('./module-manifest-helpers.cjs');
const { T1K } = require('./telemetry-utils.cjs');

function isMajorBump(local, remote) {
  return Number(remote.split('.')[0]) > Number((local || '0').split('.')[0]);
}

/**
 * Fix relative .claude/ paths in global ~/.claude/settings.json.
 * Transforms "node .claude/..." → "node \"$HOME/.claude/...\"" (or %USERPROFILE% on Windows).
 * Idempotent, fail-open. Only touches global settings — never project-level.
 */
function fixGlobalSettingsPaths(home) {
  const settingsPath = path.join(home, '.claude', 'settings.json');
  if (!fs.existsSync(settingsPath)) return;

  let raw;
  try { raw = fs.readFileSync(settingsPath, 'utf8'); } catch { return; }

  if (!/(node\s+)(?:\.\/)?(\.claude\/)/.test(raw)) return;
  if (/\$HOME\/\.claude\//.test(raw) || /%USERPROFILE%/.test(raw)) return;

  let settings;
  try { settings = JSON.parse(raw); } catch { return; }

  const prefix = process.platform === 'win32' ? '$USERPROFILE' : '$HOME';
  let fixCount = 0;

  function fixCommand(cmd) {
    if (!cmd || !cmd.includes('.claude/')) return cmd;
    if (/\$HOME|%USERPROFILE%|\$CLAUDE_PROJECT_DIR|%CLAUDE_PROJECT_DIR%/.test(cmd)) return cmd;
    const fixed = cmd.replace(
      /(node\s+)(?:\.\/)?(\.claude\/\S+)/,
      `$1"${prefix}/$2"`
    );
    if (fixed !== cmd) fixCount++;
    return fixed;
  }

  if (settings.hooks) {
    for (const entries of Object.values(settings.hooks)) {
      if (!Array.isArray(entries)) continue;
      for (const entry of entries) {
        if (entry.command) entry.command = fixCommand(entry.command);
        if (Array.isArray(entry.hooks)) {
          for (const hook of entry.hooks) {
            if (hook.command) hook.command = fixCommand(hook.command);
          }
        }
      }
    }
  }

  if (settings.statusLine?.command) {
    settings.statusLine.command = fixCommand(settings.statusLine.command);
  }

  if (fixCount === 0) return;

  try {
    fs.writeFileSync(settingsPath, JSON.stringify(settings, null, 2) + '\n');
    console.log(`[t1k:settings-repair] Fixed ${fixCount} relative path(s) in global settings.json`);
  } catch { /* fail-open */ }
}

/**
 * Find the t1k CLI binary on PATH.
 * Returns the path to the binary, or null if not found.
 */
function findCliBinary() {
  const cmd = process.platform === 'win32' ? 'where' : 'which';
  try {
    return execFileSync(cmd, ['t1k'], { encoding: 'utf8', timeout: 3000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true }).trim().split('\n')[0];
  } catch { return null; }
}

/**
 * Write coordination marker so check-cli-updates.cjs knows we already
 * spawned `t1k update --yes` (which handles CLI + content).
 */
function writeUpdateMarker() {
  try {
    const markerDir = path.join(os.tmpdir(), 't1k-update');
    fs.mkdirSync(markerDir, { recursive: true });
    fs.writeFileSync(path.join(markerDir, 'spawned'), new Date().toISOString());
  } catch { /* ok */ }
}

(async () => {
  try {
    const cwd = process.cwd();
    const { resolveClaudeDir, isT1KMetadata, readFeatureFlag } = require('./telemetry-utils.cjs');
    const { logHook, createHookTimer, logHookCrash } = require('./hook-logger.cjs');
    const resolved = resolveClaudeDir();
    if (!resolved) process.exit(0);
    const { claudeDir, isGlobalOnly, home } = resolved;
    const timer = createHookTimer('check-kit-updates');

    // Always fix global settings paths (fast, idempotent, no network)
    fixGlobalSettingsPaths(home);

    // Check opt-out flag
    if (readFeatureFlag(claudeDir, T1K.FEATURES.AUTO_UPDATE, true) === false) process.exit(0);

    // Dry-run: skip all network calls — exposes branching for CI tests
    const noop = process.env[T1K.ENV.KIT_UPDATE_NOOP] === '1';

    const metadataPath = path.join(claudeDir, T1K.METADATA_FILE);
    if (!fs.existsSync(metadataPath)) process.exit(0);
    let metadata;
    try { metadata = JSON.parse(fs.readFileSync(metadataPath, 'utf8')); } catch { process.exit(0); }
    if (!isT1KMetadata(metadata)) process.exit(0);

    // Self-update guard: skip repos matching CWD's git remote
    let cwdRemotes = '';
    try { cwdRemotes = execSync('git remote -v', { encoding: 'utf8', timeout: 5000, stdio: ['pipe', 'pipe', 'ignore'] }); } catch { /* ok */ }

    // ── Discover all repos to check ──────────────────────────────────────────
    const repoMap = new Map();

    for (const [name, entry] of Object.entries(metadata.installedModules || {})) {
      if (!entry.repository || cwdRemotes.includes(entry.repository)) continue;
      if (!repoMap.has(entry.repository)) repoMap.set(entry.repository, { modules: [], isModular: true });
      repoMap.get(entry.repository).modules.push({ name, version: (entry.version || '0.0.0').replace(/^v/, '') });
    }

    for (const cf of fs.readdirSync(claudeDir).filter(f => f.startsWith(T1K.CONFIG_PREFIX) && f.endsWith('.json'))) {
      try {
        const config = JSON.parse(fs.readFileSync(path.join(claudeDir, cf), 'utf8'));
        const repo = config.repos?.primary;
        if (!repo || cwdRemotes.includes(repo) || repoMap.has(repo)) continue;
        const localVersion = (metadata.version || '0.0.0').replace(/^v/, '');
        repoMap.set(repo, { modules: [], isModular: false, localKitVersion: localVersion });
      } catch { /* skip */ }
    }

    if (repoMap.size === 0) { process.exit(0); }

    const allowMajor = readFeatureFlag(claudeDir, T1K.FEATURES.AUTO_UPDATE_MAJOR, true) !== false;

    if (noop) {
      for (const [repo, info] of repoMap) {
        const kind = info.isModular ? `modular (${info.modules.length} modules)` : `flat (v${info.localKitVersion})`;
        console.log(`[t1k:noop] would check ${repo} — ${kind} — allowMajor=${allowMajor}`);
      }
      timer.end({ outcome: 'noop', repos: repoMap.size, allowMajor });
      process.exit(0);
    }

    // ── Check each repo for updates ──────────────────────────────────────────
    let hasUpdates = false;
    let hasMajorBlocked = false;

    for (const [repo, info] of repoMap) {
      try {
        const result = info.isModular && info.modules.length > 0
          ? checkModularRepoVersions(repo, info.modules, allowMajor)
          : checkKitRepoVersions(repo, info.localKitVersion, allowMajor);
        if (result === 'update') hasUpdates = true;
        if (result === 'major-blocked') hasMajorBlocked = true;
      } catch { /* skip repo, retry next session */ }
    }

    if (!hasUpdates) {
      logHook('check-kit-updates', { repos: repoMap.size, outcome: 'up-to-date' });
      timer.end({ outcome: 'up-to-date', repos: repoMap.size });
      process.exit(0);
    }

    // ── PRIMARY PATH: delegate to CLI ────────────────────────────────────────
    const cliBinary = findCliBinary();

    if (cliBinary) {
      // Spawn `t1k update --yes` detached — handles CLI + content + deletions + both scopes
      const logDir = home ? path.join(home, '.claude') : claudeDir;
      const logPath = path.join(logDir, '.kit-update.log');

      const marker = `\n===== [${new Date().toISOString()}] Auto-update via CLI =====\n`;
      try { fs.appendFileSync(logPath, marker); } catch { /* ok */ }

      const spawnEnv = { ...process.env, CI: '1', NO_COLOR: '1', FORCE_COLOR: '0', TERM: 'dumb' };

      try {
        const logFd = fs.openSync(logPath, 'a');
        const child = spawn(cliBinary, ['update', '--yes'], {
          detached: true,
          stdio: ['ignore', logFd, logFd],
          windowsHide: true,
          env: spawnEnv,
        });
        child.unref();
        try { fs.closeSync(logFd); } catch { /* ok */ }
        writeUpdateMarker();
        console.log(`[t1k:update] Spawned 't1k update --yes' in background (log: ${logPath})`);
      } catch (err) {
        try { fs.appendFileSync(logPath, `Spawn failed: ${err && err.message ? err.message : String(err)}\n`); } catch { /* ok */ }
        // Fall through to manual fallback
        manualFallback(repoMap, metadata, metadataPath, claudeDir, isGlobalOnly ? home : cwd, isGlobalOnly, allowMajor);
      }
    } else {
      // ── FALLBACK: manual extraction (no CLI binary on PATH) ────────────────
      manualFallback(repoMap, metadata, metadataPath, claudeDir, isGlobalOnly ? home : cwd, isGlobalOnly, allowMajor);
    }

    logHook('check-kit-updates', { repos: repoMap.size, cli: !!cliBinary });
    timer.end({ outcome: cliBinary ? 'cli-delegated' : 'manual-fallback', repos: repoMap.size });
    process.exit(0);
  } catch (err) {
    try { require('./hook-logger.cjs').logHookCrash('check-kit-updates', err); } catch { /* ok */ }
    process.exit(0); // fail-open
  }
})();

// ── Version check helpers (no extraction — just detect if updates available) ─

/**
 * Check modular repo versions. Returns 'update', 'major-blocked', or 'up-to-date'.
 */
function checkModularRepoVersions(repo, modules, allowMajor) {
  let manifest;
  try {
    const raw = execFileSync('gh', ['release', 'download', '--repo', repo, '--pattern', 'manifest.json', '--output', '-'], { encoding: 'utf8', timeout: 10000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true });
    const parsed = JSON.parse(raw);
    manifest = parsed.modules || parsed;
  } catch {
    try {
      const tag = JSON.parse(execFileSync('gh', ['release', 'view', '--repo', repo, '--json', 'tagName'], { encoding: 'utf8', timeout: 10000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true })).tagName.replace(/^v/, '');
      manifest = Object.fromEntries(modules.map(m => [m.name, { version: tag }]));
    } catch { return 'up-to-date'; }
  }

  let result = 'up-to-date';
  for (const { name, version: local } of modules) {
    const remote = (manifest[name]?.version || '').replace(/^v/, '');
    if (!remote || remote === local) continue;

    if (isMajorBump(local, remote) && !allowMajor) {
      console.log(`${T1K.TAGS.KIT_MAJOR} ${name} ${local} → ${remote} (major). Run 't1k update' manually.`);
      result = 'major-blocked';
      continue;
    }
    console.log(`[t1k:available] ${name} ${local} → ${remote}`);
    result = 'update';
  }
  return result;
}

/**
 * Check kit-level repo version. Returns 'update', 'major-blocked', or 'up-to-date'.
 */
function checkKitRepoVersions(repo, localVersion, allowMajor) {
  if (!localVersion || localVersion === '0.0.0-source' || localVersion === '0.0.0') return 'up-to-date';

  const rel = JSON.parse(execFileSync('gh', ['release', 'view', '--repo', repo, '--json', 'tagName'], { encoding: 'utf8', timeout: 10000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true }));
  const remote = rel.tagName.replace(/^v/, '');
  if (remote === localVersion) return 'up-to-date';

  const kitName = repo.split('/').pop();

  if (isMajorBump(localVersion, remote) && !allowMajor) {
    console.log(`${T1K.TAGS.KIT_MAJOR} ${kitName} ${localVersion} → ${remote} (major). Run 't1k update' manually.`);
    return 'major-blocked';
  }

  console.log(`[t1k:available] ${kitName} ${localVersion} → ${remote}`);
  return 'update';
}

// ── Manual fallback (when CLI binary not on PATH) ────────────────────────────

function readMetadata(metadataPath) {
  try { return JSON.parse(fs.readFileSync(metadataPath, 'utf8')); } catch { return null; }
}

function writeMetadata(metadataPath, data) {
  try { fs.writeFileSync(metadataPath, JSON.stringify(data, null, 2) + '\n'); } catch { /* ok */ }
}

function processDeletions(deletions, claudeDir) {
  if (!Array.isArray(deletions) || deletions.length === 0) return;
  let count = 0;
  for (const pattern of deletions) {
    if (!pattern || typeof pattern !== 'string') continue;
    const fullPath = path.join(claudeDir, pattern);
    if (!fullPath.startsWith(claudeDir + path.sep) && fullPath !== claudeDir) continue;
    if (pattern.includes('*')) {
      try {
        const dir = path.join(claudeDir, path.dirname(pattern));
        if (!fs.existsSync(dir)) continue;
        if (path.basename(pattern) === '**') { fs.rmSync(dir, { recursive: true, force: true }); count++; }
      } catch { /* skip */ }
    } else {
      try {
        if (fs.existsSync(fullPath)) {
          const stat = fs.lstatSync(fullPath);
          if (stat.isDirectory()) { fs.rmSync(fullPath, { recursive: true, force: true }); }
          else { fs.unlinkSync(fullPath); }
          count++;
          try {
            const parent = path.dirname(fullPath);
            if (parent !== claudeDir && fs.existsSync(parent) && fs.readdirSync(parent).length === 0) fs.rmdirSync(parent);
          } catch { /* ok */ }
        }
      } catch { /* skip */ }
    }
  }
  if (count > 0) console.log(`[t1k:cleanup] Removed ${count} deprecated file(s)`);
}

/**
 * Manual fallback: extract ZIPs + process deletions + auto-commit.
 * Used when `t1k` CLI binary is not on PATH.
 */
function manualFallback(repoMap, metadata, metadataPath, claudeDir, extractionRoot, isGlobalOnly, allowMajor) {
  for (const [repo, info] of repoMap) {
    try {
      if (info.isModular && info.modules.length > 0) {
        manualModularUpdate(repo, info.modules, metadata, metadataPath, claudeDir, extractionRoot, allowMajor);
      } else {
        manualKitUpdate(repo, info.localKitVersion, metadata, metadataPath, claudeDir, extractionRoot, allowMajor);
      }
    } catch { /* skip repo */ }
  }

  if (!isGlobalOnly) {
    autoCommitUpdates(process.cwd());
  }
}

function manualModularUpdate(repo, modules, metadata, metadataPath, claudeDir, cwd, allowMajor) {
  let manifest;
  try {
    const raw = execFileSync('gh', ['release', 'download', '--repo', repo, '--pattern', 'manifest.json', '--output', '-'], { encoding: 'utf8', timeout: 10000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true });
    const parsed = JSON.parse(raw);
    manifest = parsed.modules || parsed;
  } catch {
    try {
      const tag = JSON.parse(execFileSync('gh', ['release', 'view', '--repo', repo, '--json', 'tagName'], { encoding: 'utf8', timeout: 10000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true })).tagName.replace(/^v/, '');
      manifest = Object.fromEntries(modules.map(m => [m.name, { version: tag }]));
    } catch { return; }
  }

  for (const { name, version: local } of modules) {
    const remote = (manifest[name]?.version || '').replace(/^v/, '');
    if (!remote || remote === local) continue;
    if (isMajorBump(local, remote) && !allowMajor) continue;

    try {
      const oldManifestPath = path.join(claudeDir, 'modules', name, '.t1k-manifest.json');
      let oldFiles = [];
      try { oldFiles = JSON.parse(fs.readFileSync(oldManifestPath, 'utf8')).files || []; } catch { /* ok */ }

      const tmpZip = path.join(claudeDir, `.${name}-update.zip`);
      execFileSync('gh', ['release', 'download', '--repo', repo, '--pattern', `${name}-*.zip`, '--output', tmpZip, '--clobber'], { timeout: 30000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true });
      extractZip(tmpZip, cwd);
      try { fs.unlinkSync(tmpZip); } catch { /* ok */ }

      let newFiles = [];
      try { newFiles = JSON.parse(fs.readFileSync(oldManifestPath, 'utf8')).files || []; } catch { /* ok */ }
      const newSet = new Set(newFiles);
      for (const f of oldFiles) {
        if (!newSet.has(f)) { try { fs.rmSync(path.join(claudeDir, f), { recursive: true, force: true }); } catch { /* ok */ } }
      }

      const m = readMetadata(metadataPath);
      if (m?.installedModules?.[name]) { m.installedModules[name].version = remote; writeMetadata(metadataPath, m); }
      console.log(`${T1K.TAGS.KIT_UPDATED} ${name} ${local} → ${remote}`);
    } catch { /* retry next session */ }
  }
}

function manualKitUpdate(repo, localVersion, metadata, metadataPath, claudeDir, cwd, allowMajor) {
  if (!localVersion || localVersion === '0.0.0-source' || localVersion === '0.0.0') return;

  const rel = JSON.parse(execFileSync('gh', ['release', 'view', '--repo', repo, '--json', 'tagName,assets'], { encoding: 'utf8', timeout: 10000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true }));
  const remote = rel.tagName.replace(/^v/, '');
  if (remote === localVersion) return;
  if (isMajorBump(localVersion, remote) && !allowMajor) return;

  if (rel.assets?.find(a => a.name.endsWith('.zip'))) {
    const kitName = repo.split('/').pop();
    const tmpZip = path.join(claudeDir, `.${kitName}-update.zip`);
    execFileSync('gh', ['release', 'download', '--repo', repo, '--pattern', '*.zip', '--output', tmpZip, '--clobber'], { timeout: 30000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true });
    extractZip(tmpZip, cwd);
    try { fs.unlinkSync(tmpZip); } catch { /* ok */ }
    const m = readMetadata(metadataPath);
    if (m) {
      m.version = remote;
      writeMetadata(metadataPath, m);
      processDeletions(m.deletions, claudeDir);
    }
    console.log(`${T1K.TAGS.KIT_UPDATED} ${kitName} ${localVersion} → ${remote}`);
  }
}

/**
 * Auto-commit .claude/ changes (manual fallback only).
 * CLI path doesn't need this — `t1k init` handles its own commits.
 */
function autoCommitUpdates(cwd) {
  try {
    const gitStatus = execSync('git status --porcelain', { encoding: 'utf8', cwd, timeout: 5000, stdio: ['pipe', 'pipe', 'ignore'] });
    if (!gitStatus.trim()) return;

    const gitDir = path.join(cwd, '.git');
    if (fs.existsSync(path.join(gitDir, 'MERGE_HEAD')) || fs.existsSync(path.join(gitDir, 'rebase-merge')) || fs.existsSync(path.join(gitDir, 'rebase-apply'))) return;

    const claudeChanges = gitStatus.split('\n')
      .filter(l => l.length >= 4)
      .filter(l => {
        const filePath = l.substring(3).trimEnd().replace(/^"(.*)"$/, '$1');
        return filePath.startsWith('.claude/');
      })
      .map(l => l.substring(3).trimEnd().replace(/^"(.*)"$/, '$1'));

    if (claudeChanges.length === 0) return;
    execSync('git add .claude/', { cwd, timeout: 5000 });

    let diffSummary = '';
    try { diffSummary = execSync('git diff --cached --name-only', { encoding: 'utf8', cwd, timeout: 5000, stdio: ['pipe', 'pipe', 'ignore'] }).trim(); } catch { /* ok */ }
    if (!diffSummary) return;

    const changedFiles = diffSummary.split('\n').filter(Boolean);
    const msg = `chore(deps): auto-update kit content\n\nAuto-committed by check-kit-updates hook (manual fallback).\nFiles: ${changedFiles.length} changed in .claude/`;
    execFileSync('git', ['commit', '-m', msg, '--no-verify'], { cwd, timeout: 10000, stdio: ['pipe', 'pipe', 'ignore'], windowsHide: true });
    console.log(`[t1k:auto-commit] Committed ${changedFiles.length} .claude/ file(s)`);
  } catch { /* fail-open */ }
}
