process.env.AUTH_USERNAME = 'test-user';
process.env.AUTH_PASSWORD = 'test-only-password';
const test=require('node:test');const assert=require('node:assert/strict');const path=require('path');const {safeFile}=require('../server');
test('serves index safely',()=>assert.equal(path.basename(safeFile('/')),'index.html'));
test('serves the web user guide safely',()=>assert.equal(path.basename(safeFile('/user-guide.html')),'user-guide.html'));
test('blocks traversal',()=>assert.equal(safeFile('/../server.js'),null));

test('requires login and accepts configured local credentials', async () => {
  const { server } = require('../server');
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  try {
    const protectedResponse = await fetch(`${base}/`, { redirect: 'manual' });
    assert.equal(protectedResponse.status, 302);
    assert.equal(protectedResponse.headers.get('location'), '/login');

    const loginResponse = await fetch(`${base}/api/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: 'test-user', password: 'test-only-password' })
    });
    assert.equal(loginResponse.status, 200);
    assert.match(loginResponse.headers.get('set-cookie') || '', /research_session=/);
  } finally {
    await new Promise(resolve => server.close(resolve));
  }
});
