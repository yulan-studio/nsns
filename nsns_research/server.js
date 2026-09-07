const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { Readable } = require('stream');
const { URL } = require('url');
const { execFile } = require('child_process');

const ROOT = __dirname;
const PUBLIC = path.join(ROOT, 'public');
const DATA_ROOT = process.env.DATA_DIR ? path.resolve(process.env.DATA_DIR) : path.join(ROOT, 'work');
const RECORDS = path.join(DATA_ROOT, 'search-records');
const UPLOADS = path.join(DATA_ROOT, 'uploads');
fs.mkdirSync(RECORDS, { recursive: true });
fs.mkdirSync(UPLOADS, { recursive: true });

const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8', '.json': 'application/json; charset=utf-8', '.csv': 'text/csv; charset=utf-8' };
function json(res, status, body) {
  res.writeHead(status, { 'Content-Type': types['.json'] });
  res.end(JSON.stringify(body, null, 2));
}
function safeFile(urlPath) {
  const rel = urlPath === '/' ? 'index.html' : decodeURIComponent(urlPath.slice(1));
  const full = path.resolve(PUBLIC, rel);
  return full.startsWith(PUBLIC + path.sep) || full === path.join(PUBLIC, 'index.html') ? full : null;
}
function stamp() { return new Date().toISOString().replace(/[:.]/g, '-'); }

function r2Config() {
  const accountId = process.env.R2_ACCOUNT_ID;
  const accessKeyId = process.env.R2_ACCESS_KEY_ID;
  const secretAccessKey = process.env.R2_SECRET_ACCESS_KEY;
  const bucket = process.env.R2_BUCKET_NAME;
  const supplied = [accountId, accessKeyId, secretAccessKey, bucket].filter(Boolean).length;
  return { enabled: supplied === 4, incomplete: supplied > 0 && supplied < 4, accountId, accessKeyId, secretAccessKey, bucket };
}

function sha256(value) { return crypto.createHash('sha256').update(value).digest('hex'); }
function hmac(key, value, encoding) { return crypto.createHmac('sha256', key).update(value).digest(encoding); }
function r2ObjectUrl(config, key) {
  const objectPath = key.split('/').map(encodeURIComponent).join('/');
  return new URL(`https://${config.accountId}.r2.cloudflarestorage.com/${encodeURIComponent(config.bucket)}/${objectPath}`);
}
function signedR2Request(method, key, body) {
  const config = r2Config();
  if (!config.enabled) throw new Error(config.incomplete ? 'R2环境变量配置不完整' : 'R2尚未配置');
  const url = r2ObjectUrl(config, key);
  const now = new Date();
  const amzDate = now.toISOString().replace(/[:-]|\.\d{3}/g, '');
  const date = amzDate.slice(0, 8);
  const payloadHash = sha256(body || Buffer.alloc(0));
  const canonicalHeaders = `host:${url.host}\nx-amz-content-sha256:${payloadHash}\nx-amz-date:${amzDate}\n`;
  const signedHeaders = 'host;x-amz-content-sha256;x-amz-date';
  const canonicalRequest = [method, url.pathname, '', canonicalHeaders, signedHeaders, payloadHash].join('\n');
  const scope = `${date}/auto/s3/aws4_request`;
  const stringToSign = ['AWS4-HMAC-SHA256', amzDate, scope, sha256(canonicalRequest)].join('\n');
  const dateKey = hmac(`AWS4${config.secretAccessKey}`, date);
  const regionKey = hmac(dateKey, 'auto');
  const serviceKey = hmac(regionKey, 's3');
  const signingKey = hmac(serviceKey, 'aws4_request');
  const signature = hmac(signingKey, stringToSign, 'hex');
  return {
    url,
    options: {
      method,
      headers: {
        Authorization: `AWS4-HMAC-SHA256 Credential=${config.accessKeyId}/${scope}, SignedHeaders=${signedHeaders}, Signature=${signature}`,
        'x-amz-content-sha256': payloadHash,
        'x-amz-date': amzDate,
        ...(body ? { 'Content-Type': 'application/pdf', 'Content-Length': String(body.length) } : {})
      },
      ...(body ? { body } : {})
    }
  };
}

async function putPdfInR2(filename, body) {
  const request = signedR2Request('PUT', `pdf/${filename}`, body);
  const response = await fetch(request.url, request.options);
  if (!response.ok) throw new Error(`R2上传失败（HTTP ${response.status}）`);
}

async function getPdfFromR2(filename, method) {
  const request = signedR2Request(method, `pdf/${filename}`);
  return fetch(request.url, request.options);
}

async function pubmedSearch(reqUrl, res) {
  const term = (reqUrl.searchParams.get('term') || '').trim();
  const retmax = Math.min(Math.max(Number(reqUrl.searchParams.get('retmax')) || 20, 1), 500);
  const requestedSort = reqUrl.searchParams.get('sort') || 'relevance';
  const sort = ['relevance', 'pub_date', 'Author', 'JournalName'].includes(requestedSort) ? requestedSort : 'relevance';
  if (!term) return json(res, 400, { error: '检索式不能为空' });
  const api = new URL('https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi');
  api.searchParams.set('db', 'pubmed'); api.searchParams.set('retmode', 'json');
  api.searchParams.set('usehistory', 'y'); api.searchParams.set('retmax', String(retmax)); api.searchParams.set('term', term); api.searchParams.set('sort', sort);
  try {
    const response = await fetch(api, { headers: { 'User-Agent': 'NeurodiversityResearchAssistant/0.1 (local research tool)' } });
    if (!response.ok) throw new Error(`NCBI HTTP ${response.status}`);
    const raw = await response.json();
    const r = raw.esearchresult || {};
    const record = {
      schema_version: '0.1', source: 'NCBI PubMed E-utilities esearch',
      searched_at_utc: new Date().toISOString(), database: 'pubmed', query: term, sort, sort_label: sort === 'relevance' ? 'Best Match（相关性）' : sort,
      returned: (r.idlist || []).length, total_hits: Number(r.count || 0), pmids: r.idlist || [],
      webenv: r.webenv || null, query_key: r.querykey || null,
      screening: { imported: (r.idlist || []).length, title_abstract_screened: 0, full_text_assessed: 0, included: 0, exclusion_reasons: {} }
    };
    const filename = `pubmed-${stamp()}.json`;
    fs.writeFileSync(path.join(RECORDS, filename), JSON.stringify(record, null, 2));
    json(res, 200, { ...record, saved_as: `work/search-records/${filename}` });
  } catch (error) {
    const code = error.cause?.code || error.cause?.errors?.[0]?.code;
    const blocked = code === 'EACCES' || code === 'EPERM';
    json(res, 502, {
      error: blocked ? '当前环境未允许访问 NCBI。请为本地任务开启网络权限后重启服务。' : '无法连接 NCBI；请检查网络后重试。',
      detail: code ? `${error.message} (${code})` : error.message
    });
  }
}

async function pubmedSummaries(reqUrl, res) {
  const ids = (reqUrl.searchParams.get('ids') || '').split(',').map(x => x.trim()).filter(x => /^\d+$/.test(x)).slice(0, 100);
  if (!ids.length) return json(res, 400, { error: 'PMID不能为空' });
  const api = new URL('https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esummary.fcgi');
  api.searchParams.set('db', 'pubmed'); api.searchParams.set('retmode', 'json'); api.searchParams.set('id', ids.join(','));
  try {
    const response = await fetch(api, { headers: { 'User-Agent': 'NeurodiversityResearchAssistant/0.2 (local research tool)' } });
    if (!response.ok) throw new Error(`NCBI HTTP ${response.status}`);
    const raw = await response.json(); const result = raw.result || {};
    json(res, 200, { items: ids.map(id => ({ pmid: id, title: result[id]?.title || '', authors: (result[id]?.authors || []).map(a => a.name), journal: result[id]?.fulljournalname || result[id]?.source || '', pubdate: result[id]?.pubdate || '' })) });
  } catch (error) { json(res, 502, { error: '无法取得论文题名', detail: error.message }); }
}

function decodeXml(value = '') {
  return value.replace(/<[^>]+>/g, ' ').replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&').replace(/&quot;/g, '"').replace(/&#39;|&apos;/g, "'").replace(/\s+/g, ' ').trim();
}
async function pubmedAbstracts(reqUrl, res) {
  const ids = (reqUrl.searchParams.get('ids') || '').split(',').map(x => x.trim()).filter(x => /^\d+$/.test(x)).slice(0, 100);
  if (!ids.length) return json(res, 400, { error: 'PMID不能为空' });
  const api = new URL('https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi');
  api.searchParams.set('db', 'pubmed'); api.searchParams.set('retmode', 'xml'); api.searchParams.set('id', ids.join(','));
  try {
    const response = await fetch(api, { headers: { 'User-Agent': 'NeurodiversityResearchAssistant/0.3 (local research tool)' } });
    if (!response.ok) throw new Error(`NCBI HTTP ${response.status}`);
    const xml = await response.text();
    const items = [...xml.matchAll(/<PubmedArticle>[\s\S]*?<\/PubmedArticle>/g)].map(match => {
      const article = match[0];
      const pmid = (article.match(/<PMID[^>]*>(\d+)<\/PMID>/) || [])[1] || '';
      const title = decodeXml((article.match(/<ArticleTitle>([\s\S]*?)<\/ArticleTitle>/) || [])[1]);
      const parts = [...article.matchAll(/<AbstractText(?:\s[^>]*)?>([\s\S]*?)<\/AbstractText>/g)].map(x => decodeXml(x[1]));
      const publicationTypes = [...article.matchAll(/<PublicationType[^>]*>([\s\S]*?)<\/PublicationType>/g)].map(x => decodeXml(x[1]));
      return { pmid, title, abstract: parts.join(' '), publicationTypes };
    });
    json(res, 200, { items });
  } catch (error) { json(res, 502, { error: '无法取得PubMed摘要', detail: error.message }); }
}

async function pubmedFreeFullText(reqUrl, res) {
  const ids = (reqUrl.searchParams.get('ids') || '').split(',').map(x => x.trim()).filter(x => /^\d+$/.test(x)).slice(0, 50);
  if (!ids.length) return json(res, 400, { error: 'PMID不能为空' });
  try {
    const linkUrl = new URL('https://eutils.ncbi.nlm.nih.gov/entrez/eutils/elink.fcgi');
    linkUrl.searchParams.set('dbfrom', 'pubmed'); linkUrl.searchParams.set('db', 'pmc'); linkUrl.searchParams.set('retmode', 'json'); linkUrl.searchParams.set('linkname', 'pubmed_pmc'); ids.forEach(id => linkUrl.searchParams.append('id', id));
    const linkResponse = await fetch(linkUrl, { headers: { 'User-Agent': 'NeurodiversityResearchAssistant/0.4 (local research tool)' } });
    if (!linkResponse.ok) throw new Error(`NCBI link HTTP ${linkResponse.status}`);
    const linked = await linkResponse.json(); const pairs = [];
    (linked.linksets || []).forEach((set, index) => { const pmid = String(set.ids?.[0] || ids[index] || ''); const links = set.linksetdbs?.flatMap(x => x.links || []) || []; if (links[0]) pairs.push({ pmid, pmcid: String(links[0]) }); });
    const items = [];
    for (const pair of pairs) {
      const fetchUrl = new URL('https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi'); fetchUrl.searchParams.set('db', 'pmc'); fetchUrl.searchParams.set('retmode', 'xml'); fetchUrl.searchParams.set('id', pair.pmcid);
      const response = await fetch(fetchUrl, { headers: { 'User-Agent': 'NeurodiversityResearchAssistant/0.4 (local research tool)' } });
      if (!response.ok) continue; const xml = await response.text();
      const title = decodeXml((xml.match(/<article-title>([\s\S]*?)<\/article-title>/) || [])[1]);
      const body = decodeXml((xml.match(/<body[^>]*>([\s\S]*?)<\/body>/) || [])[1]).slice(0, 180000);
      items.push({ pmid: pair.pmid, pmcid: `PMC${pair.pmcid}`, title, text: body, url: `https://pmc.ncbi.nlm.nih.gov/articles/PMC${pair.pmcid}/` });
    }
    json(res, 200, { items, requested: ids.length, free_full_text_found: items.length });
  } catch (error) { json(res, 502, { error: '无法查找免费全文', detail: error.message }); }
}

function extractUploadedPdf(req, res) {
  const declared = Number(req.headers['content-length'] || 0); const max = 30 * 1024 * 1024;
  if (declared > max) return json(res, 413, { error: 'PDF不能超过30MB' });
  const chunks = []; let size = 0;
  req.on('data', chunk => { size += chunk.length; if (size <= max) chunks.push(chunk); else req.destroy(); });
  req.on('end', () => {
    if (!size || size > max) return json(res, size > max ? 413 : 400, { error: size > max ? 'PDF不能超过30MB' : '没有收到PDF文件' });
    const original = decodeURIComponent(String(req.headers['x-filename'] || 'uploaded.pdf')); if (!/\.pdf$/i.test(original)) return json(res, 415, { error: '只支持PDF文件' });
    const filename = `${Date.now()}-${path.basename(original).replace(/[^\w.()-]+/g, '_')}`; const file = path.join(UPLOADS, filename); fs.writeFileSync(file, Buffer.concat(chunks));
    const bundledPython = 'C:\\Users\\Yulan\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\python\\python.exe';
    const python = process.env.PYTHON_BIN || process.env.CODEX_PYTHON || (process.platform === 'win32' && fs.existsSync(bundledPython) ? bundledPython : process.platform === 'win32' ? 'python' : 'python3');
    execFile(python, [path.join(ROOT, 'scripts', 'extract_pdf.py'), file], { maxBuffer: 5 * 1024 * 1024 }, async (error, stdout, stderr) => {
      try {
        const result = JSON.parse(stdout || '{}');
        if (error || result.error) { try { fs.unlinkSync(file); } catch {} return json(res, 422, { error: result.error || 'PDF解析失败', detail: stderr.trim() }); }
        const config = r2Config();
        if (config.incomplete) { try { fs.unlinkSync(file); } catch {} return json(res, 500, { error: 'R2环境变量配置不完整' }); }
        if (config.enabled) {
          await putPdfInR2(filename, fs.readFileSync(file));
          try { fs.unlinkSync(file); } catch {}
        }
        json(res, 200, { ...result, filename: original, stored_as: filename, storage: config.enabled ? 'cloudflare-r2' : 'local', url: `/api/uploads/${encodeURIComponent(filename)}` });
      }
      catch (uploadError) { try { fs.unlinkSync(file); } catch {} json(res, 500, { error: uploadError.message || 'PDF保存失败', detail: stderr.trim() }); }
    });
  });
}

async function serveUploadedPdf(reqUrl, res, headOnly = false) {
  const name = decodeURIComponent(reqUrl.pathname.slice('/api/uploads/'.length));
  if (!name || name !== path.basename(name) || !/\.pdf$/i.test(name)) return json(res, 400, { error: '无效PDF地址' });
  const config = r2Config();
  if (config.incomplete) return json(res, 500, { error: 'R2环境变量配置不完整' });
  if (config.enabled) {
    try {
      const remote = await getPdfFromR2(name, headOnly ? 'HEAD' : 'GET');
      if (remote.status === 404) return json(res, 404, { error: 'PDF不存在或已移除' });
      if (!remote.ok) return json(res, 502, { error: `无法从R2读取PDF（HTTP ${remote.status}）` });
      const headers = { 'Content-Type': 'application/pdf', 'Content-Disposition': `inline; filename="${name.replace(/["\r\n]/g, '_')}"`, 'Cache-Control': 'private, max-age=3600' };
      const contentLength = remote.headers.get('content-length');
      if (contentLength) headers['Content-Length'] = contentLength;
      res.writeHead(200, headers);
      if (headOnly) return res.end();
      return Readable.fromWeb(remote.body).pipe(res);
    } catch (error) { return json(res, 502, { error: '无法从R2读取PDF', detail: error.message }); }
  }
  const file = path.join(UPLOADS, name);
  if (!fs.existsSync(file) || !fs.statSync(file).isFile()) return json(res, 404, { error: 'PDF不存在或已移除' });
  res.writeHead(200, { 'Content-Type': 'application/pdf', 'Content-Disposition': `inline; filename="${name.replace(/["\r\n]/g, '_')}"`, 'Cache-Control': 'private, max-age=3600' });
  if (headOnly) return res.end();
  fs.createReadStream(file).pipe(res);
}

const server = http.createServer(async (req, res) => {
  const reqUrl = new URL(req.url, 'http://localhost');
  if (req.method === 'GET' && reqUrl.pathname === '/health') return json(res, 200, { ok: true, pdf_storage: r2Config().enabled ? 'cloudflare-r2' : r2Config().incomplete ? 'r2-incomplete' : 'local' });
  if (req.method === 'GET' && reqUrl.pathname === '/api/pubmed/search') return pubmedSearch(reqUrl, res);
  if (req.method === 'GET' && reqUrl.pathname === '/api/pubmed/summaries') return pubmedSummaries(reqUrl, res);
  if (req.method === 'GET' && reqUrl.pathname === '/api/pubmed/abstracts') return pubmedAbstracts(reqUrl, res);
  if (req.method === 'GET' && reqUrl.pathname === '/api/pubmed/free-fulltext') return pubmedFreeFullText(reqUrl, res);
  if (req.method === 'POST' && reqUrl.pathname === '/api/pdf/extract') return extractUploadedPdf(req, res);
  if ((req.method === 'GET' || req.method === 'HEAD') && reqUrl.pathname.startsWith('/api/uploads/')) return serveUploadedPdf(reqUrl, res, req.method === 'HEAD');
  if (req.method !== 'GET') return json(res, 405, { error: 'Method not allowed' });
  const file = safeFile(reqUrl.pathname);
  if (!file || !fs.existsSync(file) || fs.statSync(file).isDirectory()) return json(res, 404, { error: 'Not found' });
  res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream' });
  fs.createReadStream(file).pipe(res);
});

if (require.main === module) {
  const port = Number(process.env.PORT) || 8787;
  const host = process.env.HOST || '0.0.0.0';
  server.listen(port, host, () => console.log(`研究助手已启动：http://${host}:${port}`));
}
module.exports = { server, safeFile };
