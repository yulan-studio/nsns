import fs from 'node:fs/promises';
import path from 'node:path';

const ids = process.argv.slice(2);
if (ids.length === 0) {
  throw new Error('Usage: node fetch_fulltext_xml.mjs PMCID [PMCID ...]');
}
const outDir = path.resolve('work', 'fulltext-xml');
await fs.mkdir(outDir, { recursive: true });
for (const id of ids) {
  const url = `https://www.ebi.ac.uk/europepmc/webservices/rest/${id}/fullTextXML`;
  const response = await fetch(url, { headers: { 'User-Agent': 'NeurodiversityResearchAssistant/0.1' } });
  if (!response.ok) throw new Error(`${id}: HTTP ${response.status}`);
  const text = await response.text();
  await fs.writeFile(path.join(outDir, `${id}.xml`), text, 'utf8');
  console.log(`${id}\t${text.length}`);
}
