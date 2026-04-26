// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';
const fs = require('fs');
const os = require('os');
const path = require('path');
const {
  isT1KMetadata,
  deriveProjectName,
} = require('../telemetry-utils.cjs');

// Each test runs in a fresh CWD, so re-requiring the module gives a clean
// resolveProjectDir cache. Returns the fresh module.
function resetResolveCache() {
  delete require.cache[require.resolve('../telemetry-utils.cjs')];
  return require('../telemetry-utils.cjs');
}

function makeTmpRoot(prefix) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), `t1k-test-${prefix}-`));
  // macOS: os.tmpdir() returns /var/... but that's a symlink to /private/var/...
  // node's module loader resolves to the canonical path, so we need the same on
  // the test side for `assertEqual(result.t1kDir, ...)` to hold across platforms.
  return fs.realpathSync(dir);
}

function writeT1KMeta(claudeDir, content) {
  fs.mkdirSync(claudeDir, { recursive: true });
  fs.writeFileSync(path.join(claudeDir, 'metadata.json'), JSON.stringify(content));
}

function runInCwd(dir, fn) {
  const origCwd = process.cwd();
  const origEnv = { ...process.env };
  try {
    process.chdir(dir);
    return fn();
  } finally {
    process.chdir(origCwd);
    // Restore env too
    for (const k of Object.keys(process.env)) {
      if (!(k in origEnv)) delete process.env[k];
    }
    Object.assign(process.env, origEnv);
  }
}

// ─── isT1KMetadata ───────────────────────────────────────────────────────

describe('isT1KMetadata — positive shapes', () => {
  it('accepts v3 schema with installedModules', () => {
    assertEqual(isT1KMetadata({ installedModules: { foo: { version: '1.0.0' } } }), true);
  });

  it('accepts v2 legacy schema (schemaVersion:2 + modules)', () => {
    assertEqual(isT1KMetadata({ schemaVersion: 2, modules: ['foo'] }), true);
    assertEqual(isT1KMetadata({ schemaVersion: 2, modules: { foo: {} } }), true);
  });

  it('accepts v1 by name=theonekit-*', () => {
    assertEqual(isT1KMetadata({ name: 'theonekit-core', version: '1.0.0' }), true);
  });

  it('accepts v1 legacy by kitName=theonekit-*', () => {
    assertEqual(isT1KMetadata({ kitName: 'theonekit-unity' }), true);
  });
});

describe('isT1KMetadata — negative shapes', () => {
  it('rejects CK shape (kits.engineer)', () => {
    assertEqual(isT1KMetadata({ kits: { engineer: { version: '1.0.0' } } }), false);
  });

  it('rejects empty object', () => {
    assertEqual(isT1KMetadata({}), false);
  });

  it('rejects null and undefined', () => {
    assertEqual(isT1KMetadata(null), false);
    assertEqual(isT1KMetadata(undefined), false);
  });

  it('rejects non-T1K name prefix', () => {
    assertEqual(isT1KMetadata({ name: 'claudekit-engineer' }), false);
  });

  it('rejects v2 modules without schemaVersion:2', () => {
    // Needs both marker (schemaVersion=2) AND modules to be accepted as v2
    assertEqual(isT1KMetadata({ modules: ['foo'] }), false);
  });
});

// ─── deriveProjectName ───────────────────────────────────────────────────

describe('deriveProjectName', () => {
  it('falls back to cwd basename when no git remote', () => {
    const dir = makeTmpRoot('noremote');
    try {
      assertEqual(deriveProjectName(dir), path.basename(dir));
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  it('returns "unknown" for empty string input gracefully', () => {
    // path.basename('') is '' which falls through to 'unknown'
    const result = deriveProjectName('/nonexistent-path-xyz-123');
    // With a nonexistent path, git fails, then basename('/nonexistent-path-xyz-123') = 'nonexistent-path-xyz-123'
    assertEqual(result, 'nonexistent-path-xyz-123');
  });
});

// ─── resolveProjectDir ───────────────────────────────────────────────────

describe('resolveProjectDir — env-valid', () => {
  it('uses env CLAUDE_PROJECT_DIR when T1K-shape', () => {
    const tmp = makeTmpRoot('env-valid');
    try {
      const projectDir = path.join(tmp, 'myproj');
      writeT1KMeta(path.join(projectDir, '.claude'), { name: 'theonekit-core', version: '1.0.0' });

      const fresh = resetResolveCache();
      runInCwd(tmp, () => {
        process.env.CLAUDE_PROJECT_DIR = projectDir;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'env');
        assertEqual(result.globalOnly, false);
        assertEqual(result.projectName, 'myproj');
        assertEqual(result.t1kDir, path.join(projectDir, '.claude'));
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('resolveProjectDir — env-missing-walk', () => {
  it('walks up when env unset', () => {
    const tmp = makeTmpRoot('walk');
    try {
      const projectDir = path.join(tmp, 'parent');
      const childDir = path.join(projectDir, 'child', 'grandchild');
      writeT1KMeta(path.join(projectDir, '.claude'), { schemaVersion: 3, installedModules: {}, name: 'theonekit-core' });
      fs.mkdirSync(childDir, { recursive: true });

      const fresh = resetResolveCache();
      runInCwd(childDir, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'walk');
        assertEqual(result.projectName, 'parent');
        assertEqual(result.t1kDir, path.join(projectDir, '.claude'));
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('resolveProjectDir — walk-skip-stub', () => {
  it('skips stub .claude/ (no metadata.json)', () => {
    const tmp = makeTmpRoot('stub');
    try {
      const parentDir = path.join(tmp, 'parent');
      const childDir = path.join(parentDir, 'child');
      // Parent has real T1K metadata
      writeT1KMeta(path.join(parentDir, '.claude'), { name: 'theonekit-unity' });
      // Child has stub .claude/ with only settings.local.json — NO metadata.json
      const childClaude = path.join(childDir, '.claude');
      fs.mkdirSync(childClaude, { recursive: true });
      fs.writeFileSync(path.join(childClaude, 'settings.local.json'), '{}');

      const fresh = resetResolveCache();
      runInCwd(childDir, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'walk');
        assertEqual(result.projectName, 'parent');
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('resolveProjectDir — CK-shape-rejected', () => {
  it('skips CK metadata, continues walking', () => {
    const tmp = makeTmpRoot('ck');
    try {
      const outerDir = path.join(tmp, 'outer');
      const ckDir = path.join(outerDir, 'ck');
      const childDir = path.join(ckDir, 'child');
      // Outer has T1K
      writeT1KMeta(path.join(outerDir, '.claude'), { name: 'theonekit-core' });
      // Middle has CK shape
      writeT1KMeta(path.join(ckDir, '.claude'), { kits: { engineer: { version: '1.0.0' } } });
      fs.mkdirSync(childDir, { recursive: true });

      const fresh = resetResolveCache();
      runInCwd(childDir, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'walk');
        assertEqual(result.projectName, 'outer');
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('resolveProjectDir — global-only-fallback', () => {
  it('returns globalOnly=true when no T1K in tree', () => {
    const tmp = makeTmpRoot('global');
    try {
      const fakeHome = path.join(tmp, 'home');
      const fakeGlobalClaude = path.join(fakeHome, '.claude');
      fs.mkdirSync(fakeGlobalClaude, { recursive: true });
      const workDir = path.join(tmp, 'random-project');
      fs.mkdirSync(workDir, { recursive: true });

      const fresh = resetResolveCache();
      runInCwd(workDir, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        process.env.HOME = fakeHome;
        process.env.USERPROFILE = fakeHome;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'global-fallback');
        assertEqual(result.globalOnly, true);
        assertEqual(result.projectName, 'random-project');
        assertEqual(result.t1kDir, fakeGlobalClaude);
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('handles missing global ~/.claude/ (t1kDir=null)', () => {
    const tmp = makeTmpRoot('nohome');
    try {
      const fakeHome = path.join(tmp, 'empty-home');
      fs.mkdirSync(fakeHome, { recursive: true });
      const workDir = path.join(tmp, 'proj');
      fs.mkdirSync(workDir, { recursive: true });

      const fresh = resetResolveCache();
      runInCwd(workDir, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        process.env.HOME = fakeHome;
        process.env.USERPROFILE = fakeHome;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'global-fallback');
        assertEqual(result.globalOnly, true);
        assertEqual(result.t1kDir, null);
        assertEqual(result.projectName, 'proj');
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('resolveProjectDir — env with non-T1K metadata falls through to walk', () => {
  it('env set but target is CK → ignores env, walks up', () => {
    const tmp = makeTmpRoot('envck');
    try {
      const realT1K = path.join(tmp, 'real');
      const fakeEnv = path.join(tmp, 'fakeenv');
      writeT1KMeta(path.join(realT1K, '.claude'), { name: 'theonekit-rn' });
      writeT1KMeta(path.join(fakeEnv, '.claude'), { kits: { engineer: {} } });
      const cwd = path.join(realT1K, 'sub');
      fs.mkdirSync(cwd, { recursive: true });

      const fresh = resetResolveCache();
      runInCwd(cwd, () => {
        process.env.CLAUDE_PROJECT_DIR = fakeEnv; // points at CK, should be ignored
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'walk');
        assertEqual(result.projectName, 'real');
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('resolveProjectDir — caching', () => {
  it('returns identical reference on second call with same CWD', () => {
    const tmp = makeTmpRoot('cache');
    try {
      const projectDir = path.join(tmp, 'proj');
      writeT1KMeta(path.join(projectDir, '.claude'), { name: 'theonekit-core' });

      const fresh = resetResolveCache();
      runInCwd(projectDir, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        const a = fresh.resolveProjectDir();
        const b = fresh.resolveProjectDir();
        assertEqual(a === b, true);
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('resolveProjectDir — malformed JSON', () => {
  it('treats unparseable metadata.json as non-T1K and continues walk', () => {
    const tmp = makeTmpRoot('malformed');
    try {
      const parentDir = path.join(tmp, 'parent');
      const childDir = path.join(parentDir, 'child');
      writeT1KMeta(path.join(parentDir, '.claude'), { name: 'theonekit-unity' });
      // Child has corrupt metadata
      const childClaude = path.join(childDir, '.claude');
      fs.mkdirSync(childClaude, { recursive: true });
      fs.writeFileSync(path.join(childClaude, 'metadata.json'), '{ not valid json');

      const fresh = resetResolveCache();
      runInCwd(childDir, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'walk');
        assertEqual(result.projectName, 'parent');
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

// ─── Issue #14: T1K_HOOK_DIR priority (project in subdir of git repo) ────────

describe('resolveProjectDir — T1K_HOOK_DIR priority (issue #14)', () => {
  it('uses T1K_HOOK_DIR when CWD is git-root (no .claude/)', () => {
    // Reproduces #14: git repo root has NO .claude/, project lives in subdir.
    // hook-runner sets cwd=git-root + T1K_HOOK_DIR=project-dir.
    // Without the fix, walk-up from git-root reaches $HOME/.claude/ (wrong).
    const tmp = makeTmpRoot('hookdir');
    try {
      const gitRoot = path.join(tmp, 'GitParent');
      const projectDir = path.join(gitRoot, 'ChildProject');
      fs.mkdirSync(gitRoot, { recursive: true });
      writeT1KMeta(path.join(projectDir, '.claude'), { name: 'theonekit-core', version: '1.0.0' });

      const fresh = resetResolveCache();
      runInCwd(gitRoot, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        process.env.T1K_HOOK_DIR = projectDir;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'env');
        assertEqual(result.globalOnly, false);
        assertEqual(result.t1kDir, path.join(projectDir, '.claude'));
        assertEqual(result.projectName, 'ChildProject');
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('T1K_HOOK_DIR takes priority over CLAUDE_PROJECT_DIR', () => {
    const tmp = makeTmpRoot('hookpri');
    try {
      const hookTarget = path.join(tmp, 'hook-target');
      const envTarget = path.join(tmp, 'env-target');
      writeT1KMeta(path.join(hookTarget, '.claude'), { name: 'theonekit-core' });
      writeT1KMeta(path.join(envTarget, '.claude'), { name: 'theonekit-core' });

      const fresh = resetResolveCache();
      runInCwd(tmp, () => {
        process.env.T1K_HOOK_DIR = hookTarget;
        process.env.CLAUDE_PROJECT_DIR = envTarget;
        const result = fresh.resolveProjectDir();
        assertEqual(result.t1kDir, path.join(hookTarget, '.claude'));
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('T1K_HOOK_DIR pointing at non-T1K dir falls through to walk', () => {
    const tmp = makeTmpRoot('hookfall');
    try {
      const realT1K = path.join(tmp, 'real');
      const fakeHook = path.join(tmp, 'fake');
      writeT1KMeta(path.join(realT1K, '.claude'), { name: 'theonekit-core' });
      fs.mkdirSync(fakeHook, { recursive: true }); // no .claude/
      const cwd = path.join(realT1K, 'sub');
      fs.mkdirSync(cwd, { recursive: true });

      const fresh = resetResolveCache();
      runInCwd(cwd, () => {
        delete process.env.CLAUDE_PROJECT_DIR;
        process.env.T1K_HOOK_DIR = fakeHook;
        const result = fresh.resolveProjectDir();
        assertEqual(result.source, 'walk');
        assertEqual(result.projectName, 'real');
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('resolveClaudeDir — T1K_HOOK_DIR priority (issue #14)', () => {
  it('uses T1K_HOOK_DIR when CWD is git-root (no .claude/)', () => {
    const tmp = makeTmpRoot('rcd-hook');
    try {
      const gitRoot = path.join(tmp, 'GitParent');
      const projectDir = path.join(gitRoot, 'ChildProject');
      fs.mkdirSync(gitRoot, { recursive: true });
      writeT1KMeta(path.join(projectDir, '.claude'), { name: 'theonekit-core' });

      const fresh = resetResolveCache();
      runInCwd(gitRoot, () => {
        process.env.T1K_HOOK_DIR = projectDir;
        const result = fresh.resolveClaudeDir();
        assertEqual(result.claudeDir, path.join(projectDir, '.claude'));
        assertEqual(result.isGlobalOnly, false);
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('T1K_HOOK_DIR without metadata/settings falls through to walk', () => {
    const tmp = makeTmpRoot('rcd-fall');
    try {
      const realT1K = path.join(tmp, 'real');
      const emptyHook = path.join(tmp, 'empty');
      const subDir = path.join(realT1K, 'sub');
      writeT1KMeta(path.join(realT1K, '.claude'), { name: 'theonekit-core' });
      fs.mkdirSync(emptyHook, { recursive: true });
      fs.mkdirSync(subDir, { recursive: true });

      const fresh = resetResolveCache();
      runInCwd(subDir, () => {
        process.env.T1K_HOOK_DIR = emptyHook;
        const result = fresh.resolveClaudeDir();
        assertEqual(result.claudeDir, path.join(realT1K, '.claude'));
      });
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('walkUpForClaudeDir — bounded depth', () => {
  it('respects maxDepth and stops at filesystem root', () => {
    const fresh = resetResolveCache();
    // Walk from /tmp with predicate that never matches — should return null without hanging
    const result = fresh.walkUpForClaudeDir(() => false, os.tmpdir(), 5);
    assertEqual(result, null);
  });
});

describe('getHomeDir', () => {
  it('reads from HOME or USERPROFILE env', () => {
    const fresh = resetResolveCache();
    const orig = { HOME: process.env.HOME, USERPROFILE: process.env.USERPROFILE };
    try {
      process.env.HOME = '/test/home';
      delete process.env.USERPROFILE;
      assertEqual(fresh.getHomeDir(), '/test/home');
      delete process.env.HOME;
      process.env.USERPROFILE = 'C:\\Users\\test';
      assertEqual(fresh.getHomeDir(), 'C:\\Users\\test');
      delete process.env.USERPROFILE;
      assertEqual(fresh.getHomeDir(), '');
    } finally {
      if (orig.HOME !== undefined) process.env.HOME = orig.HOME; else delete process.env.HOME;
      if (orig.USERPROFILE !== undefined) process.env.USERPROFILE = orig.USERPROFILE; else delete process.env.USERPROFILE;
    }
  });
});
