const exportButton = document.createElement('button');
exportButton.className = 'ghost';
exportButton.textContent = '下载完整项目记录';
const importButton = document.createElement('button');
importButton.className = 'ghost';
importButton.textContent = '导入项目记录';
const importInput = document.createElement('input');
importInput.type = 'file';
importInput.accept = '.json,application/json';
importInput.hidden = true;
const logoutButton = document.createElement('button');
logoutButton.className = 'ghost';
logoutButton.textContent = '退出登录';
document.querySelector('#resetProject').before(exportButton, importButton, importInput, logoutButton);
logoutButton.onclick = async () => { await fetch('/api/logout', { method: 'POST' }); location.replace('/login'); };

function exportProjectRecord() {
  const record = {
    ...state,
    project_record: {
      schema_version: '1.0',
      application: '神经多样性儿童研究论文助手',
      exported_at_utc: new Date().toISOString(),
      pdf_files_embedded: false,
      pdf_note: '项目记录保存PDF链接和解析结果，不包含PDF文件本体。换设备或存储环境后，无法访问的PDF需要重新上传。'
    }
  };
  const date = new Date().toISOString().slice(0, 10);
  download(`论文助手_完整项目记录_${date}.json`, JSON.stringify(record, null, 2), 'application/json;charset=utf-8');
}

exportButton.onclick = exportProjectRecord;
const draftExportButton = document.querySelector('#exportProject');
if (draftExportButton) draftExportButton.onclick = exportProjectRecord;

importButton.onclick = () => importInput.click();
importInput.onchange = async event => {
  const file = event.target.files[0];
  if (!file) return;
  try {
    const imported = JSON.parse(await file.text());
    if (!imported || typeof imported !== 'object' || !Array.isArray(imported.studies) || !Array.isArray(imported.files)) {
      throw new Error('文件不是有效的论文助手项目记录');
    }
    const restored = { ...initial, ...imported, dataMode: 'fresh-v1' };
    delete restored.project_record;
    let missing = 0;
    for (const study of restored.studies) {
      if (!String(study.fullTextUrl || '').startsWith('/api/uploads/')) continue;
      try {
        const response = await fetch(study.fullTextUrl, { method: 'HEAD' });
        if (!response.ok) throw new Error();
        study.pdfMissing = false;
      } catch {
        study.fullTextUrl = '';
        study.pdfMissing = true;
        study.outcome = `${study.outcome || ''}；PDF文件当前不可用，请重新上传后继续核对`;
        missing++;
      }
    }
    for (const item of restored.files) {
      if (String(item.url || '').startsWith('/api/uploads/') && !restored.studies.some(study => study.fullTextUrl === item.url)) item.url = '';
    }
    localStorage.setItem(KEY, JSON.stringify(restored));
    alert(`项目导入成功。恢复检索记录 ${restored.search?.pmids?.length || 0} 篇、筛选记录 ${restored.studies.length} 篇和论文草稿${restored.draft ? '1份' : '0份'}；${missing} 份PDF需要重新上传。`);
    location.reload();
  } catch (error) {
    alert(`导入失败：${error.message}`);
  } finally {
    event.target.value = '';
  }
};
