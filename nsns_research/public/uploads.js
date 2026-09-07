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

function placeRowUploadLinks() {
  document.querySelectorAll('#studyRows tr').forEach(row => {
    const upload = row.querySelector('.row-upload');
    if (!upload) return;

    if (row.querySelector('details')) {
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
