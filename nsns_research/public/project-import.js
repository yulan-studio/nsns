const importButton = document.createElement('button');
importButton.className = 'ghost';
importButton.textContent = '导入项目记录';
const importInput = document.createElement('input');
importInput.type = 'file';
importInput.accept = '.json,application/json';
importInput.hidden = true;
document.querySelector('#resetProject').before(importButton, importInput);

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
    alert(`项目导入成功。恢复 ${restored.studies.length} 篇研究；${missing} 份PDF需要重新上传。筛选决定和已提取依据已保留。`);
    location.reload();
  } catch (error) {
    alert(`导入失败：${error.message}`);
  } finally {
    event.target.value = '';
  }
};
