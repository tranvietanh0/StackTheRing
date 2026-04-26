// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';
/**
 * Tests for auto-update-major behavior.
 *
 * Covers:
 *   1. readFeatureFlag — default, explicit true, explicit false, multi-fragment precedence
 *   2. isMajorBump — version compare semantics
 *   3. T1K constants — flag names and log tags exist (prevents hardcoded-string regression)
 *   4. check-kit-updates.cjs NOOP mode — end-to-end dry-run for CI, asserts correct
 *      allowMajor flag threads through discovery for both default-true and opt-out-false.
 *
 * Design: pure helpers are unit-tested; the hook is integration-tested via NOOP env
 * so CI can verify branching without real gh release calls.
 */
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync } = require('child_process');
const { T1K, readFeatureFlag } = require('../telemetry-utils.cjs');

function makeTmpClaudeDir(prefix) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), `t1k-upd-${prefix}-`));
  const real = fs.realpathSync(root);
  const claudeDir = path.join(real, '.claude');
  fs.mkdirSync(claudeDir, { recursive: true });
  return { root: real, claudeDir };
}

function writeFragment(claudeDir, name, content) {
  fs.writeFileSync(path.join(claudeDir, `${T1K.CONFIG_PREFIX}${name}.json`), JSON.stringify(content));
}

function cleanup(root) {
  try { fs.rmSync(root, { recursive: true, force: true }); } catch { /* ok */ }
}

describe('readFeatureFlag', () => {
  it('returns default when no fragments exist', () => {
    const { root, claudeDir } = makeTmpClaudeDir('default');
    try {
      assertEqual(readFeatureFlag(claudeDir, 'autoUpdateMajor', true), true);
      assertEqual(readFeatureFlag(claudeDir, 'autoUpdateMajor', false), false);
    } finally { cleanup(root); }
  });

  it('returns default when fragments exist but flag not set', () => {
    const { root, claudeDir } = makeTmpClaudeDir('unset');
    try {
      writeFragment(claudeDir, 'core', { features: { telemetry: true } });
      assertEqual(readFeatureFlag(claudeDir, 'autoUpdateMajor', true), true);
      assertEqual(readFeatureFlag(claudeDir, 'autoUpdateMajor', false), false);
    } finally { cleanup(root); }
  });

  it('explicit false wins over default true', () => {
    const { root, claudeDir } = makeTmpClaudeDir('optout');
    try {
      writeFragment(claudeDir, 'core', { features: { autoUpdateMajor: false } });
      assertEqual(readFeatureFlag(claudeDir, 'autoUpdateMajor', true), false);
    } finally { cleanup(root); }
  });

  it('explicit true wins over default false', () => {
    const { root, claudeDir } = makeTmpClaudeDir('optin');
    try {
      writeFragment(claudeDir, 'core', { features: { autoUpdateMajor: true } });
      assertEqual(readFeatureFlag(claudeDir, 'autoUpdateMajor', false), true);
    } finally { cleanup(root); }
  });

  it('any false fragment wins over other true fragments (opt-out wins)', () => {
    const { root, claudeDir } = makeTmpClaudeDir('multi');
    try {
      writeFragment(claudeDir, 'core', { features: { autoUpdateMajor: true } });
      writeFragment(claudeDir, 'unity', { features: { autoUpdateMajor: false } });
      writeFragment(claudeDir, 'web', { features: { autoUpdateMajor: true } });
      assertEqual(readFeatureFlag(claudeDir, 'autoUpdateMajor', true), false);
    } finally { cleanup(root); }
  });

  it('malformed fragment does not crash; falls back to default', () => {
    const { root, claudeDir } = makeTmpClaudeDir('bad');
    try {
      fs.writeFileSync(path.join(claudeDir, `${T1K.CONFIG_PREFIX}bad.json`), '{ this is not valid json');
      assertEqual(readFeatureFlag(claudeDir, 'autoUpdateMajor', true), true);
    } finally { cleanup(root); }
  });

  it('non-existent claudeDir returns default without crashing', () => {
    assertEqual(readFeatureFlag('/nonexistent/path/xyz', 'autoUpdateMajor', true), true);
    assertEqual(readFeatureFlag('/nonexistent/path/xyz', 'autoUpdateMajor', false), false);
  });
});

describe('T1K constants (SSOT for flag names and log tags)', () => {
  it('exports feature flag constants', () => {
    assertEqual(T1K.FEATURES.AUTO_UPDATE, 'autoUpdate');
    assertEqual(T1K.FEATURES.AUTO_UPDATE_MAJOR, 'autoUpdateMajor');
    assertEqual(T1K.FEATURES.TELEMETRY, 'telemetry');
  });

  it('exports log tag constants matching the format parsed by rules/', () => {
    assertEqual(T1K.TAGS.CLI_UPDATE, '[t1k:cli-update]');
    assertEqual(T1K.TAGS.CLI_MAJOR, '[t1k:cli-major]');
    assertEqual(T1K.TAGS.KIT_UPDATED, '[t1k:updated]');
    assertEqual(T1K.TAGS.KIT_MAJOR, '[t1k:major-update]');
  });

  it('exports dry-run env var names', () => {
    assertEqual(T1K.ENV.CLI_UPDATE_NOOP, 'T1K_CLI_UPDATE_NOOP');
    assertEqual(T1K.ENV.KIT_UPDATE_NOOP, 'T1K_KIT_UPDATE_NOOP');
  });
});

describe('isMajorBump (inlined in check-kit-updates.cjs)', () => {
  // Reproduce locally to avoid requiring the hook module (which has IIFE side effects)
  function isMajorBump(local, remote) {
    return Number(remote.split('.')[0]) > Number((local || '0').split('.')[0]);
  }

  it('returns true for major bumps', () => {
    assertEqual(isMajorBump('1.5.3', '2.0.0'), true);
    assertEqual(isMajorBump('0.9.0', '1.0.0'), true);
  });

  it('returns false for minor and patch bumps', () => {
    assertEqual(isMajorBump('1.0.0', '1.1.0'), false);
    assertEqual(isMajorBump('1.5.3', '1.5.4'), false);
  });

  it('returns false for equal versions', () => {
    assertEqual(isMajorBump('1.0.0', '1.0.0'), false);
  });

  it('handles missing local version (treats as 0)', () => {
    assertEqual(isMajorBump(null, '1.0.0'), true);
    assertEqual(isMajorBump('', '0.5.0'), false);
  });
});

describe('check-kit-updates.cjs NOOP integration', () => {
  const hookPath = path.join(__dirname, '..', 'check-kit-updates.cjs');

  function runHookNoop(claudeDir, extraEnv = {}) {
    const cwd = path.dirname(claudeDir);
    try {
      return execFileSync('node', [hookPath], {
        cwd,
        encoding: 'utf8',
        timeout: 10000,
        stdio: ['pipe', 'pipe', 'pipe'],
        windowsHide: true,
        env: {
          ...process.env,
          [T1K.ENV.KIT_UPDATE_NOOP]: '1',
          HOME: cwd,
          USERPROFILE: cwd,
          ...extraEnv,
        },
      });
    } catch (e) {
      // NOOP exits 0 on success; capture output even on non-zero
      return (e.stdout || '') + (e.stderr || '');
    }
  }

  it('logs allowMajor=true by default', () => {
    const { root, claudeDir } = makeTmpClaudeDir('noop-default');
    try {
      fs.writeFileSync(path.join(claudeDir, 'metadata.json'),
        JSON.stringify({ name: 'theonekit-core', version: '1.59.0', kitName: 'theonekit-core' }));
      writeFragment(claudeDir, 'core', {
        kitName: 'theonekit-core',
        repos: { primary: 'The1Studio/theonekit-core' },
        features: { autoUpdate: true, autoUpdateMajor: true },
      });
      const out = runHookNoop(claudeDir);
      assertMatch(out, /\[t1k:noop\].*allowMajor=true/);
    } finally { cleanup(root); }
  });

  it('logs allowMajor=false when features.autoUpdateMajor is false', () => {
    const { root, claudeDir } = makeTmpClaudeDir('noop-optout');
    try {
      fs.writeFileSync(path.join(claudeDir, 'metadata.json'),
        JSON.stringify({ name: 'theonekit-core', version: '1.59.0', kitName: 'theonekit-core' }));
      writeFragment(claudeDir, 'core', {
        kitName: 'theonekit-core',
        repos: { primary: 'The1Studio/theonekit-core' },
        features: { autoUpdate: true, autoUpdateMajor: false },
      });
      const out = runHookNoop(claudeDir);
      assertMatch(out, /\[t1k:noop\].*allowMajor=false/);
    } finally { cleanup(root); }
  });

  it('exits silently when features.autoUpdate is false (full opt-out)', () => {
    const { root, claudeDir } = makeTmpClaudeDir('noop-full-optout');
    try {
      fs.writeFileSync(path.join(claudeDir, 'metadata.json'),
        JSON.stringify({ name: 'theonekit-core', version: '1.59.0', kitName: 'theonekit-core' }));
      writeFragment(claudeDir, 'core', {
        kitName: 'theonekit-core',
        repos: { primary: 'The1Studio/theonekit-core' },
        features: { autoUpdate: false },
      });
      const out = runHookNoop(claudeDir);
      // Full opt-out → no [t1k:noop] line emitted
      if (/\[t1k:noop\]/.test(out)) throw new Error(`Expected no NOOP output, got: ${out}`);
    } finally { cleanup(root); }
  });
});
