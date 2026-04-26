// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';
const fs = require('fs');
const path = require('path');
const os = require('os');

// Use an isolated cache path for tests
const TEST_CACHE = path.join(os.tmpdir(), `t1k-kit-error-dedup-test-${process.pid}.json`);
process.env.T1K_KIT_ERROR_CACHE_PATH = TEST_CACHE;

const { fingerprint, checkAndRecord, markSubmitted, pruneCache, clearCache, _loadCache, _saveCache } =
  require('../lib/kit-error-dedup.cjs');

function resetCache() {
  clearCache();
}

describe('kit-error-dedup — fingerprint', () => {
  it('produces a 16-char md5 hex', () => {
    const fp = fingerprint(
      { tool: 'Bash', cmd: 'npm test', stderrHead: 'error' },
      { reason: 't1k-command', originKit: 'theonekit-core' }
    );
    assertEqual(typeof fp, 'string');
    assertEqual(fp.length, 16);
    assertMatch(fp, /^[a-f0-9]+$/);
  });

  it('is deterministic for same inputs', () => {
    const a = fingerprint({ tool: 'X', cmd: 'y', stderrHead: 'z' }, { reason: 'r' });
    const b = fingerprint({ tool: 'X', cmd: 'y', stderrHead: 'z' }, { reason: 'r' });
    assertEqual(a, b);
  });

  it('differs when inputs differ', () => {
    const a = fingerprint({ tool: 'X', cmd: 'y', stderrHead: 'z' }, { reason: 'r' });
    const b = fingerprint({ tool: 'X', cmd: 'y', stderrHead: 'DIFFERENT' }, { reason: 'r' });
    if (a === b) throw new Error('expected different fingerprints');
  });

  it('handles empty/null payload without throwing', () => {
    const fp = fingerprint({}, {});
    assertEqual(typeof fp, 'string');
    assertEqual(fp.length, 16);
  });
});

describe('kit-error-dedup — checkAndRecord', () => {
  it('first-time fingerprint is not a duplicate', () => {
    resetCache();
    const fp = fingerprint({ tool: 'Bash', cmd: 'a', stderrHead: 'b' }, { reason: 't1k-command' });
    const r = checkAndRecord(fp, { reason: 't1k-command' });
    assertEqual(r.isDuplicate, false);
    assertEqual(r.count, 1);
  });

  it('second time within TTL returns duplicate with count 2', () => {
    resetCache();
    const fp = fingerprint({ tool: 'Bash', cmd: 'x', stderrHead: 'y' }, { reason: 't1k-command' });
    checkAndRecord(fp, { reason: 't1k-command' });
    const r = checkAndRecord(fp, { reason: 't1k-command' });
    assertEqual(r.isDuplicate, true);
    assertEqual(r.count, 2);
  });

  it('persists across load/save', () => {
    resetCache();
    const fp = fingerprint({ tool: 'T', cmd: 'c', stderrHead: 'h' }, {});
    checkAndRecord(fp);
    const cache = _loadCache();
    if (!cache.entries[fp]) throw new Error('entry not persisted');
  });

  it('fail-open on corrupted cache file', () => {
    resetCache();
    // Write garbage to the cache file
    fs.writeFileSync(TEST_CACHE, '{not json]', { mode: 0o600 });
    const fp = fingerprint({ tool: 'A', cmd: 'B', stderrHead: 'C' }, {});
    const r = checkAndRecord(fp);
    assertEqual(r.isDuplicate, false);
  });
});

describe('kit-error-dedup — pruneCache', () => {
  it('removes entries older than maxAgeDays', () => {
    const oldDate = new Date(Date.now() - 10 * 24 * 60 * 60 * 1000).toISOString();
    const cache = {
      version: 1,
      entries: {
        old: { firstSeen: oldDate, lastSeen: oldDate, count: 1 },
        fresh: { firstSeen: new Date().toISOString(), lastSeen: new Date().toISOString(), count: 1 },
      },
    };
    pruneCache(cache, 7);
    if (cache.entries.old) throw new Error('old entry not pruned');
    if (!cache.entries.fresh) throw new Error('fresh entry wrongly pruned');
  });

  it('handles empty cache without errors', () => {
    const cache = pruneCache({ version: 1, entries: {} }, 7);
    assertEqual(Object.keys(cache.entries).length, 0);
  });
});

describe('kit-error-dedup — markSubmitted', () => {
  it('sets submitted flag and issueUrl on existing entry', () => {
    resetCache();
    const fp = fingerprint({ tool: 'Bash', cmd: 'm', stderrHead: 'n' }, {});
    checkAndRecord(fp);
    markSubmitted(fp, 'https://github.com/x/y/issues/42');
    const cache = _loadCache();
    assertEqual(cache.entries[fp].submitted, true);
    assertEqual(cache.entries[fp].issueUrl, 'https://github.com/x/y/issues/42');
  });

  it('creates entry if missing then marks submitted', () => {
    resetCache();
    markSubmitted('newfp12345abcdef', 'https://example.com/1');
    const cache = _loadCache();
    if (!cache.entries['newfp12345abcdef']) throw new Error('entry not created');
    assertEqual(cache.entries['newfp12345abcdef'].submitted, true);
  });
});

// Cleanup after all tests
process.on('exit', () => {
  try { if (fs.existsSync(TEST_CACHE)) fs.unlinkSync(TEST_CACHE); } catch {}
});
