document.querySelector('#files').onchange = async event => {
  const files = [...event.target.files];
  await parseUploadedPdfs(files);
  const lastName = files.at(-1)?.name;
  const row = [...document.querySelectorAll('#studyRows tr')].find(item => item.textContent.includes(lastName));
  if (row) {
    row.classList.add('just-uploaded');
    row.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }
};

const evidenceFields = [
  ['population', '研究对象', /participant|children|adolesc|autis|asd\b/i],
  ['intervention', '干预方案', /exercise|physical activity|training|week|session|aerobic|sport|yoga|swim/i],
  ['outcomes', '量表与结局', /anxi|depress|emotion|behavio|well-being|quality of life|scale|questionnaire/i],
  ['results', '研究结果', /result|significant|p\s*[<=>]|confidence interval|effect size|improv|decreas/i],
  ['missing', '缺失数据', /dropout|attrition|lost to follow|missing data|withdraw/i],
  ['adverse', '不良事件', /adverse|safety|injur|harm/i]
];

evidenceSnippets = function traceableEvidenceSnippets(text) {
  const marked = String(text || '');
  const pageMatches = [...marked.matchAll(/\[\[PDF_PAGE:(\d+)\]\]\s*([\s\S]*?)(?=\[\[PDF_PAGE:\d+\]\]|$)/g)];
  const sources = pageMatches.length
    ? pageMatches.map(match => ({ page: `PDF第${match[1]}页`, text: match[2] }))
    : [{ page: '网页全文（无PDF页码）', text: marked }];
  const result = { _evidence: {} };

  evidenceFields.forEach(([key, label, pattern]) => {
    const matches = [];
    sources.forEach(source => {
      source.text.split(/(?<=[.!?。！？])\s+/).forEach(sentence => {
        const clean = sentence.replace(/\s+/g, ' ').trim();
        if (clean.length > 30 && clean.length < 700 && pattern.test(clean) && matches.length < 2) matches.push({ page: source.page, quote: clean });
      });
    });
    const fallback = key === 'missing' ? '未自动定位；不能解释为没有缺失' : key === 'adverse' ? '未自动定位；不能解释为没有不良事件' : '原文中未自动定位';
    result[key] = matches.map(item => item.quote).join(' ') || fallback;
    result._evidence[key] = {
      label,
      page: matches.map(item => item.page).filter((value, index, all) => all.indexOf(value) === index).join('、') || (pageMatches.length ? 'PDF页码未定位' : '网页全文（无PDF页码）'),
      quote: matches.map(item => item.quote).join(' ') || '没有自动定位到可引用片段，请人工查看全文。',
      status: '待人工确认',
      confirmedAt: null
    };
  });
  return result;
};

function ensureEvidenceTrace(study) {
  if (!study.fullText) return null;
  if (!study.fullText._evidence) {
    study.fullText._evidence = {};
    evidenceFields.forEach(([key, label]) => {
      study.fullText._evidence[key] = {
        label,
        page: study.uploadedName ? '旧解析记录无页码，请重新上传PDF或人工定位' : '网页全文（无PDF页码）',
        quote: study.fullText[key] || '没有自动定位到可引用片段，请人工查看全文。',
        status: '待人工确认',
        confirmedAt: null
      };
    });
  }
  return study.fullText._evidence;
}

function evidenceResolved(item) {
  return ['已人工确认', '人工修正', '原文未报告'].includes(item?.status);
}

function pdfPageNumber(item) {
  return Number((item?.page?.match(/PDF第(\d+)页/) || [])[1]) || 1;
}

function renderEvidenceTrace(row, study, studyIndex) {
  const details = row.querySelector('details');
  const trace = details && ensureEvidenceTrace(study);
  if (!trace || row.nextElementSibling?.classList.contains('evidence-trace-row')) return;
  const panel = document.createElement('section');
  panel.className = 'evidence-trace';
  const confirmed = Object.values(trace).filter(evidenceResolved).length;
  const pdfAvailable = String(study.fullTextUrl || '').startsWith('/api/uploads/');
  const initialPage = pdfPageNumber(trace.population);
  const fieldHtml = evidenceFields.map(([key, label]) => {
    const item = trace[key];
    const resolved = evidenceResolved(item);
    const page = pdfPageNumber(item);
    const pageLabel = pdfAvailable && /PDF第\d+页/.test(item.page)
      ? `<button class="page-link" type="button" data-pdf-page="${page}">${esc(item.page)}</button>`
      : `<span>${esc(item.page)}</span>`;
    return `<details class="trace-item" ${resolved ? '' : 'open'}><summary><strong>${label}</strong><span class="badge ${resolved ? '' : 'warn'}">${esc(item.status)}</span></summary><div class="trace-body"><div class="trace-location">${pageLabel}</div><blockquote>${esc(item.quote)}</blockquote>${item.originalQuote ? `<details class="original-extract"><summary>查看原自动提取内容</summary><p>${esc(item.originalQuote)}</p></details>` : ''}<div class="trace-actions">${resolved ? `<small>处理时间：${esc(item.confirmedAt || '已记录')}</small>` : `<button type="button" data-confirm-extract="${key}" data-study="${studyIndex}">与原文一致</button><button type="button" class="ghost" data-correct-extract="${key}" data-study="${studyIndex}">内容不正确</button><button type="button" class="ghost" data-not-reported="${key}" data-study="${studyIndex}">原文未报告</button>`}</div></div></details>`;
  }).join('');
  const viewer = pdfAvailable
    ? `<iframe class="pdf-viewer" title="${esc(study.name)} PDF全文" src="${esc(study.fullTextUrl)}#page=${initialPage}"></iframe>`
    : `<div class="pdf-unavailable"><strong>当前来源不是已上传PDF</strong><p>PMC网页全文没有固定PDF页码。请在新标签打开原文核对。</p>${study.fullTextUrl ? `<a href="${esc(study.fullTextUrl)}" target="_blank" rel="noopener">打开全文</a>` : ''}</div>`;
  panel.innerHTML = `<div class="trace-heading"><div><h4>全文提取核对（${confirmed}/${evidenceFields.length}项已处理）</h4><p>左侧查看全文，右侧逐项核对。已处理项目自动折叠。</p></div><button type="button" data-confirm-descriptive="${studyIndex}">本页全部确认（仅描述性3项）</button></div><div class="trace-workspace"><div class="trace-document">${viewer}</div><div class="trace-fields">${fieldHtml}</div></div>`;
  const traceRow = document.createElement('tr');
  traceRow.className = 'evidence-trace-row';
  traceRow.hidden = !details.open;
  const traceCell = document.createElement('td');
  traceCell.colSpan = 6;
  traceCell.append(panel);
  traceRow.append(traceCell);
  row.after(traceRow);
  details.addEventListener('toggle', () => { traceRow.hidden = !details.open; });

  panel.querySelectorAll('[data-pdf-page]').forEach(button => button.onclick = () => {
    const viewerFrame = panel.querySelector('.pdf-viewer');
    if (viewerFrame) viewerFrame.src = `${study.fullTextUrl}#page=${button.dataset.pdfPage}`;
  });
  panel.querySelector('[data-confirm-descriptive]').onclick = () => {
    ['population', 'intervention', 'outcomes'].forEach(key => {
      const item = trace[key];
      if (!evidenceResolved(item)) { item.status = '已人工确认'; item.confirmedAt = new Date().toISOString(); }
    });
    save(); rows();
  };
  panel.querySelectorAll('[data-confirm-extract]').forEach(button => button.onclick = () => {
    const target = trace[button.dataset.confirmExtract];
    target.status = '已人工确认'; target.confirmedAt = new Date().toISOString();
    save(); rows();
  });
  panel.querySelectorAll('[data-correct-extract]').forEach(button => button.onclick = () => {
    const key = button.dataset.correctExtract; const target = trace[key];
    const correction = prompt('请输入根据原文核对后的正确内容：', target.quote);
    if (!correction?.trim()) return;
    target.originalQuote ||= target.quote; target.quote = correction.trim(); target.status = '人工修正'; target.confirmedAt = new Date().toISOString();
    study.fullText[key] = correction.trim();
    save(); rows();
  });
  panel.querySelectorAll('[data-not-reported]').forEach(button => button.onclick = () => {
    const key = button.dataset.notReported; const target = trace[key];
    if (!confirm('确认您已查看全文，并确认该项在原文中未报告？')) return;
    target.originalQuote ||= target.quote; target.quote = '原文未报告（研究者确认）'; target.status = '原文未报告'; target.confirmedAt = new Date().toISOString();
    study.fullText[key] = '原文未报告';
    save(); rows();
  });
}

function placeRowUploadLinks() {
  document.querySelectorAll('#studyRows tr').forEach(row => {
    const studyIndex = Number(row.dataset.studyI);
    if (Number.isInteger(studyIndex)) renderEvidenceTrace(row, state.studies[studyIndex], studyIndex);
    const upload = row.querySelector('.row-upload');
    if (!upload) return;

    const decision = row.querySelector('select[data-i]')?.value;
    if (row.querySelector('details') || decision === '排除') {
      upload.remove();
      return;
    }

    const input = upload.querySelector('input');
    if (upload.firstChild?.nodeValue !== '上传PDF') {
      upload.replaceChildren(document.createTextNode('上传PDF'));
      if (input) upload.append(input);
    }

    const statusCell = row.lastElementChild;
    if (statusCell && upload.parentElement !== statusCell) statusCell.append(upload);
  });
  updateScreeningButtons();
}

function updateScreeningButtons() {
  const autoScreen = document.querySelector('#autoScreen');
  const findFullText = document.querySelector('#findFullText');
  if (!autoScreen || !findFullText) return;

  const total = state.search?.pmids?.length || 0;
  const complete = total > 0 && state.studies.length >= total;
  autoScreen.classList.toggle('ghost', complete);
  findFullText.classList.toggle('ghost', !complete);
}

const uploadRows = document.querySelector('#studyRows');
if (uploadRows) {
  new MutationObserver(placeRowUploadLinks).observe(uploadRows, { childList: true, subtree: true });
  placeRowUploadLinks();
}

function suggestedEvidenceLevel(study) {
  const outcomeText = `${study.name || ''} ${study.outcome || ''} ${study.fullText?.outcomes || ''}`.toLowerCase();
  if (/anxi|焦虑/.test(outcomeText)) return { level: '核心焦虑', reason: '原文提取内容提到焦虑结局或焦虑量表' };
  if (/emotion|behavio|internaliz|externaliz|well-?being|quality of life|情绪|行为|生活质量/.test(outcomeText)) {
    return { level: '相关情绪', reason: '原文提取内容提到相关情绪、行为或生活质量结局' };
  }
  return { level: '待研究者分层', reason: '自动提取内容不足以确定结局类别，请查看全文后选择' };
}

function evidenceLevelLabel(level) {
  if (level === '核心焦虑') return '核心焦虑';
  if (['相关行为', '相关情绪', '支持性', '相关情绪/行为'].includes(level)) return '相关情绪/行为';
  if (level === '探索性') return '探索性证据';
  return '待分类';
}

evidence = function renderEvidenceWorkspace() {
  const included = state.studies.filter(study => study.decision.startsWith('纳入'));
  included.forEach(study => {
    if ((!study.level || study.level === '待研究者分层') && study.levelSource !== '研究者手工确认') {
      const suggestion = suggestedEvidenceLevel(study);
      study.level = suggestion.level;
      study.levelBasis = suggestion.reason;
      study.levelSource = 'AI建议，待研究者确认';
    }
  });

  const categories = [
    ['核心焦虑', included.filter(study => evidenceLevelLabel(study.level) === '核心焦虑'), '直接测量焦虑或使用焦虑量表的研究。'],
    ['相关情绪/行为', included.filter(study => evidenceLevelLabel(study.level) === '相关情绪/行为'), '测量情绪调节、问题行为、幸福感或生活质量等相关结局的研究。'],
    ['探索性证据', included.filter(study => evidenceLevelLabel(study.level) === '探索性证据'), '与主题有关，但不能直接回答“运动是否改善心理健康”。例如只报告体适能、以心理健康为次要结局、没有合适对照组或样本很小。它不等于研究质量差，也不等于应当排除；论文引用时需要降低结论强度并说明局限。'],
    ['待分类', included.filter(study => evidenceLevelLabel(study.level) === '待分类'), '自动提取不足，必须查看全文后由研究者决定。']
  ];

  $('#evidenceCards').innerHTML = categories.map(([name, studies, explanation]) => `<article class="card evidence-summary"><h3>${name}</h3><strong>${studies.length}<small>篇研究</small></strong><p>${explanation}</p><ul>${studies.map(study => `<li>${esc(study.name)}</li>`).join('') || '<li>暂无</li>'}</ul></article>`).join('');

  let panel = $('#evidenceClassification');
  if (!panel) {
    panel = document.createElement('section');
    panel.id = 'evidenceClassification';
    panel.className = 'evidence-classification';
    $('#evidenceCards').insertAdjacentElement('afterend', panel);
  }
  panel.innerHTML = `<h3>逐篇核对证据分类</h3><p>分类用于决定论文如何组织证据，不代表研究质量高低或干预有效。请核对原文后修改不准确的AI建议。</p>${included.length ? `<div class="tablewrap"><table><thead><tr><th>研究</th><th>当前分类</th><th>分类依据</th><th>确认状态</th></tr></thead><tbody>${included.map(study => { const index = state.studies.indexOf(study); return `<tr><td>${esc(study.name)}</td><td><select data-level-i="${index}"><option ${evidenceLevelLabel(study.level)==='核心焦虑'?'selected':''}>核心焦虑</option><option ${evidenceLevelLabel(study.level)==='相关情绪/行为'?'selected':''}>相关情绪/行为</option><option ${evidenceLevelLabel(study.level)==='探索性证据'?'selected':''}>探索性证据</option><option ${evidenceLevelLabel(study.level)==='待分类'?'selected':''}>待分类</option></select></td><td>${esc(study.levelBasis || '尚未记录分类依据')}</td><td><span class="badge ${study.levelSource==='研究者手工确认'?'':'warn'}">${esc(study.levelSource || 'AI建议，待研究者确认')}</span></td></tr>`}).join('')}</tbody></table></div>` : '<div class="notice warning">尚无经全文核验后纳入的研究。请返回第3步完成全文决定。</div>'}`;

  document.querySelectorAll('[data-level-i]').forEach(select => select.onchange = () => {
    const study = state.studies[+select.dataset.levelI];
    const mapping = { '核心焦虑': '核心焦虑', '相关情绪/行为': '相关情绪', '探索性证据': '探索性', '待分类': '待研究者分层' };
    study.level = mapping[select.value];
    study.levelBasis = `研究者查看全文后选择“${select.value}”`;
    study.levelSource = '研究者手工确认';
    save();
    evidence();
  });

  const classified = included.filter(study => evidenceLevelLabel(study.level) !== '待分类').length;
  const anxiety = included.filter(study => evidenceLevelLabel(study.level) === '核心焦虑').length;
  $('#metaDecision').innerHTML = `<b>这是AI方法学建议，不是疗效结论。</b> 当前纳入 ${included.length} 篇，已分类 ${classified} 篇，其中直接关注焦虑 ${anxiety} 篇。不同结局、量表、运动方案和对照条件不应直接合并成一个“精神健康总效应”。应先分别综合焦虑、相关情绪/行为和探索性证据；只有在研究设计、结局定义和可用数值足够一致时，才考虑Meta分析。`;
  save();
};

if (state.step === 4) evidence();
