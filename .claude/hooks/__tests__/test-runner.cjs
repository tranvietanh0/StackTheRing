#!/usr/bin/env node
// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';
const path = require('path');
let total = 0, passed = 0, failed = 0;
const failures = [];

function describe(name, fn) { console.log(`\n  ${name}`); fn(); }
function it(name, fn) {
  total++;
  try { fn(); passed++; console.log(`    ✓ ${name}`); }
  catch (e) { failed++; failures.push({ name, error: e.message }); console.log(`    ✗ ${name}: ${e.message}`); }
}
function assertEqual(a, b) { if (a !== b) throw new Error(`Expected ${JSON.stringify(b)}, got ${JSON.stringify(a)}`); }
function assertMatch(str, re) { if (!re.test(str)) throw new Error(`"${str}" does not match ${re}`); }
function assertIncludes(arr, item) { if (!arr.includes(item)) throw new Error(`Array does not include ${JSON.stringify(item)}`); }

global.describe = describe; global.it = it;
global.assertEqual = assertEqual; global.assertMatch = assertMatch; global.assertIncludes = assertIncludes;

// Run all test files
const testDir = __dirname;
const fs = require('fs');
const testFiles = fs.readdirSync(testDir).filter(f => f.endsWith('.test.cjs'));
for (const f of testFiles) { require(path.join(testDir, f)); }

console.log(`\n  Results: ${passed}/${total} passed, ${failed} failed`);
if (failed > 0) { console.log('\n  Failures:'); failures.forEach(f => console.log(`    - ${f.name}: ${f.error}`)); process.exit(1); }
process.exit(0);
