// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';
const { sanitize, _stripEnvVars, _stripSecrets, _stripUserPaths, _stripSensitiveFilePaths } =
  require('../lib/kit-error-sanitizer.cjs');

describe('kit-error-sanitizer — stripUserPaths', () => {
  it('replaces home path with ~', () => {
    const out = _stripUserPaths('Error at /home/alice/project/x.js:5', '/home/alice', '/other');
    assertEqual(out, 'Error at ~/project/x.js:5');
  });

  it('replaces cwd with .', () => {
    const out = _stripUserPaths('/mnt/work/proj/src/file.ts error', '/home/bob', '/mnt/work/proj');
    assertEqual(out, './src/file.ts error');
  });

  it('strips Windows user paths', () => {
    const out = _stripUserPaths('C:\\Users\\bob\\app.exe crashed', '', '');
    assertEqual(out, '~\\app.exe crashed');
  });

  it('handles empty/null input safely', () => {
    assertEqual(_stripUserPaths('', '/home/x', ''), '');
    assertEqual(_stripUserPaths(null, '/home/x', ''), '');
  });
});

describe('kit-error-sanitizer — stripEnvVars', () => {
  it('replaces KEY=value with KEY=***', () => {
    const out = _stripEnvVars('API_KEY=supersecret123 NODE_ENV=prod');
    assertMatch(out, /API_KEY=\*\*\*/);
    assertMatch(out, /NODE_ENV=\*\*\*/);
  });

  it('leaves lowercase assignments alone', () => {
    const out = _stripEnvVars('foo=bar baz=qux');
    assertEqual(out, 'foo=bar baz=qux');
  });

  it('only touches shell-style assignments', () => {
    const out = _stripEnvVars('echo PATH=/usr/bin');
    assertMatch(out, /PATH=\*\*\*/);
  });
});

describe('kit-error-sanitizer — stripSecrets', () => {
  it('strips sk-* tokens', () => {
    const out = _stripSecrets('Using sk-abcdefghij1234567890 for api');
    assertMatch(out, /sk-\*\*\*/);
    if (out.includes('abcdef')) throw new Error('sk- token leaked: ' + out);
  });

  it('strips ghp_* GitHub tokens', () => {
    // Two possible safe outcomes: ghp_*** (pattern match) or token=*** (token= prefix match)
    const out = _stripSecrets('ghp_1234567890abcdefghij is my token');
    assertMatch(out, /ghp_\*\*\*/);
    // Also verify combined case is safe either way
    const out2 = _stripSecrets('Token: ghp_1234567890abcdefghij');
    if (out2.includes('1234567890abcdefghij')) throw new Error('ghp leaked: ' + out2);
  });

  it('strips AKIA* AWS keys', () => {
    const out = _stripSecrets('AWS key AKIAIOSFODNN7EXAMPLE here');
    assertMatch(out, /AKIA\*\*\*/);
  });

  it('strips Bearer tokens', () => {
    const out = _stripSecrets('Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5c');
    assertMatch(out, /Bearer \*\*\*/);
  });

  it('strips password= patterns', () => {
    const out = _stripSecrets('password=hunter2 and token=abc123xyz');
    if (out.includes('hunter2')) throw new Error('password leaked: ' + out);
    if (out.includes('abc123xyz')) throw new Error('token leaked: ' + out);
  });

  it('strips JWT tokens', () => {
    const out = _stripSecrets('jwt eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0In0.SflKxwRJSMeK');
    assertMatch(out, /JWT_\*\*\*/);
  });
});

describe('kit-error-sanitizer — stripSensitiveFilePaths', () => {
  it('redacts .env file references', () => {
    const out = _stripSensitiveFilePaths('Failed to read /home/x/.env config');
    assertMatch(out, /\[REDACTED_SENSITIVE_FILE\]/);
  });

  it('redacts id_rsa SSH keys', () => {
    const out = _stripSensitiveFilePaths('Reading /home/x/.ssh/id_rsa failed');
    assertMatch(out, /\[REDACTED_SENSITIVE_FILE\]/);
  });

  it('leaves normal files alone', () => {
    const out = _stripSensitiveFilePaths('Error in /src/app/main.ts:10');
    assertEqual(out, 'Error in /src/app/main.ts:10');
  });
});

describe('kit-error-sanitizer — sanitize (full pipeline)', () => {
  it('returns expected shape for a normal error', () => {
    const result = sanitize({
      toolName: 'Bash',
      toolInput: { command: 'npm test' },
      toolResult: 'Error: test failed\nStack trace here',
      cwd: '/mnt/work/proj',
      home: '/home/alice',
    });
    assertEqual(result.tool, 'Bash');
    assertMatch(result.cmd, /npm test/);
    assertMatch(result.stderrHead, /Error/);
    assertEqual(Array.isArray(result.filesMentioned), true);
  });

  it('caps cmd and stderrHead at 200 chars', () => {
    const longCmd = 'a'.repeat(500);
    const longErr = 'error ' + 'x'.repeat(500);
    const result = sanitize({
      toolName: 'Bash',
      toolInput: { command: longCmd },
      toolResult: longErr,
    });
    if (result.cmd.length > 200) throw new Error(`cmd too long: ${result.cmd.length}`);
    if (result.stderrHead.length > 200) throw new Error(`stderrHead too long: ${result.stderrHead.length}`);
  });

  it('handles missing toolResult gracefully', () => {
    const result = sanitize({ toolName: 'Edit', toolInput: { file_path: '/tmp/x' } });
    assertEqual(result.tool, 'Edit');
    assertEqual(typeof result.stderrHead, 'string');
  });

  it('handles non-string toolResult (object)', () => {
    const result = sanitize({
      toolName: 'Task',
      toolInput: { prompt: 'do stuff' },
      toolResult: { error: 'failed', code: 1 },
    });
    assertEqual(typeof result.stderrHead, 'string');
  });

  it('strips secrets in end-to-end payload', () => {
    const result = sanitize({
      toolName: 'Bash',
      toolInput: { command: 'curl -H "Authorization: Bearer ghp_abcdefghij1234567890" api' },
      toolResult: 'API_KEY=sk-proj-abcdefghij1234 failed with 403',
      cwd: '/mnt/work',
      home: '/home/bob',
    });
    const combined = result.cmd + result.stderrHead;
    if (combined.includes('ghp_abcdefghij')) throw new Error('ghp leaked: ' + combined);
    if (combined.includes('sk-proj-abcdef')) throw new Error('sk leaked: ' + combined);
    if (combined.includes('hunter2')) throw new Error('pwd leaked');
  });

  it('strips user paths in pipeline', () => {
    const result = sanitize({
      toolName: 'Bash',
      toolInput: { command: 'ls /home/alice/secret' },
      toolResult: 'ls: /home/alice/secret: No such file',
      cwd: '/home/alice/proj',
      home: '/home/alice',
    });
    if (result.cmd.includes('/home/alice')) throw new Error('home leaked in cmd: ' + result.cmd);
    if (result.stderrHead.includes('/home/alice')) throw new Error('home leaked in err: ' + result.stderrHead);
  });

  it('returns safe defaults on completely empty input', () => {
    const result = sanitize({});
    assertEqual(result.tool, 'unknown');
    assertEqual(result.cmd, '');
    assertEqual(result.stderrHead, '');
    assertEqual(result.filesMentioned.length, 0);
  });
});
