const sortSelect = document.createElement('label');
sortSelect.innerHTML = '排序方式<select id="sort"><option value="relevance">Best Match（相关性）</option><option value="pub_date">发表日期（最新优先）</option><option value="Author">第一作者</option><option value="JournalName">期刊名称</option></select>';
document.querySelector('#retmax').closest('label').before(sortSelect);
document.querySelector('#sort').value = state.sort || 'relevance';
document.querySelector('#sort').onchange = event => {
  state.sort = event.target.value;
  save();
};
