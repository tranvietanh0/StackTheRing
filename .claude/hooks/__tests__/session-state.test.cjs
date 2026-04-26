// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';
const path = require('path');
const fs = require('fs');
const manager = require(path.join(__dirname, '..', 'session-state-manager.cjs'));

describe('session-state-manager', () => {
  it('save creates a JSON file', () => {
    const filepath = manager.save({ test: true, gitBranch: 'main' });
    assertEqual(fs.existsSync(filepath), true);
    const data = JSON.parse(fs.readFileSync(filepath, 'utf8'));
    assertEqual(data.test, true);
    assertEqual(data.gitBranch, 'main');
    fs.unlinkSync(filepath); // cleanup
  });
  it('load returns latest saved state', () => {
    manager.save({ order: 1 });
    const loaded = manager.load();
    assertEqual(loaded.order, 1);
  });
  it('save includes timestamp', () => {
    const filepath = manager.save({ x: 1 });
    const data = JSON.parse(fs.readFileSync(filepath, 'utf8'));
    assertMatch(data.timestamp, /^\d{4}-\d{2}-\d{2}/);
  });
});
