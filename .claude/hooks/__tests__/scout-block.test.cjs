// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';
const { execFileSync } = require('child_process');
const path = require('path');
const hookPath = path.join(__dirname, '..', 'scout-block.cjs');

function runHook(toolInput) {
  const input = JSON.stringify({ tool_input: toolInput });
  try {
    execFileSync(process.execPath, [hookPath], {
      input,
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'pipe'],
      windowsHide: true,
    });
    return { exitCode: 0 };
  } catch (e) {
    return { exitCode: e.status, output: e.stdout || '' };
  }
}

describe('scout-block', () => {
  it('blocks .git/ directory reads', () => {
    const r = runHook({ file_path: '/project/.git/objects/abc' });
    assertEqual(r.exitCode, 1);
  });
  it('allows .gitignore reads', () => {
    const r = runHook({ file_path: '/project/.gitignore' });
    assertEqual(r.exitCode, 0);
  });
  it('blocks node_modules/', () => {
    const r = runHook({ file_path: '/project/node_modules/express/index.js' });
    assertEqual(r.exitCode, 1);
  });
  it('blocks package-lock.json', () => {
    const r = runHook({ file_path: '/project/package-lock.json' });
    assertEqual(r.exitCode, 1);
  });
  it('allows normal source files', () => {
    const r = runHook({ file_path: '/project/src/index.ts' });
    assertEqual(r.exitCode, 0);
  });
  it('blocks dist/ directory', () => {
    const r = runHook({ file_path: '/project/dist/bundle.js' });
    assertEqual(r.exitCode, 1);
  });
});
