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
