const test=require('node:test');const assert=require('node:assert/strict');const path=require('path');const {safeFile}=require('../server');
test('serves index safely',()=>assert.equal(path.basename(safeFile('/')),'index.html'));
test('blocks traversal',()=>assert.equal(safeFile('/../server.js'),null));
