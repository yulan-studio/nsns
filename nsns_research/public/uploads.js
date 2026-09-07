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
