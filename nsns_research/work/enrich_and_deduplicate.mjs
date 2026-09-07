import fs from 'node:fs/promises';

const rows=JSON.parse(await fs.readFile('work/candidate_references.json','utf8'));
const norm=s=>(s||'').toLowerCase().replace(/^https?:\/\/(dx\.)?doi\.org\//,'').replace(/[\s.,;()]/g,'');
const groups=new Map();
for(const row of rows){
  const key=row.doi?`doi:${norm(row.doi)}`:`cite:${norm(row.citation).slice(0,180)}`;
  if(!groups.has(key))groups.set(key,{key,doi:row.doi||'',pmid:row.pmid||'',title:row.title||'',year:row.year||'',citation:row.citation||'',source_reviews:[],source_rows:[],outcomes:[]});
  const g=groups.get(key);
  if(!g.pmid&&row.pmid)g.pmid=row.pmid;if(!g.title&&row.title)g.title=row.title;if(!g.year&&row.year)g.year=row.year;
  if(!g.source_reviews.includes(row.source_review))g.source_reviews.push(row.source_review);
  if(row.table_lead&&!g.source_rows.includes(row.table_lead))g.source_rows.push(row.table_lead);
  if(row.table_outcome&&!g.outcomes.includes(row.table_outcome))g.outcomes.push(row.table_outcome);
}
const items=[...groups.values()];
async function crossref(item){
  if(!item.doi)return;
  try{const r=await fetch(`https://api.crossref.org/works/${encodeURIComponent(item.doi)}`,{headers:{'User-Agent':'NeurodiversityResearchAssistant/0.1'}});if(!r.ok)return;const m=(await r.json()).message;item.title=item.title||(m.title||[])[0]||'';item.year=item.year||String(m.published?.['date-parts']?.[0]?.[0]||'');}catch{}
}
async function pubmed(item){
  if(item.pmid||!item.doi)return;
  try{const u=new URL('https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi');u.searchParams.set('db','pubmed');u.searchParams.set('retmode','json');u.searchParams.set('retmax','1');u.searchParams.set('term',`${item.doi}[AID]`);const r=await fetch(u);const j=await r.json();item.pmid=j.esearchresult?.idlist?.[0]||'';}catch{}
}
for(let i=0;i<items.length;i+=5){await Promise.all(items.slice(i,i+5).map(crossref));await Promise.all(items.slice(i,i+5).map(pubmed));await new Promise(r=>setTimeout(r,450));}
await fs.writeFile('work/deduplicated_candidates.json',JSON.stringify(items,null,2),'utf8');
console.log(JSON.stringify({raw_rows:rows.length,unique_records:items.length,with_doi:items.filter(x=>x.doi).length,with_pmid:items.filter(x=>x.pmid).length}));
