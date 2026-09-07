import csv, json, re
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parent
XML_DIR = ROOT / 'fulltext-xml'

def text(el):
    return ' '.join(''.join(el.itertext()).split())

def references(root):
    out=[]
    for i, ref in enumerate(root.findall('.//ref-list/ref'), 1):
        raw=text(ref)
        title=''
        node=ref.find('.//article-title')
        if node is not None: title=text(node)
        year=''
        node=ref.find('.//year')
        if node is not None: year=text(node)
        doi=''; pmid=''
        for p in ref.findall('.//pub-id'):
            kind=(p.attrib.get('pub-id-type') or '').lower()
            if kind=='doi': doi=text(p)
            if kind=='pmid': pmid=text(p)
        if not doi:
            m=re.search(r'10\.\d{4,9}/[-._;()/:A-Za-z0-9]+',raw,re.I)
            doi=m.group(0).rstrip('.,;)') if m else ''
        if not year:
            m=re.search(r'\b(19|20)\d{2}\b',raw); year=m.group(0) if m else ''
        out.append({'ref_number':i,'ref_id':ref.attrib.get('id',''),'title':title,'year':year,'doi':doi,'pmid':pmid,'citation':raw})
    return out

def table_rows(root):
    tables=[]
    for tw in root.findall('.//table-wrap'):
        caption=text(tw.find('caption')) if tw.find('caption') is not None else ''
        rows=[]
        for tr in tw.findall('.//tr'):
            cells=[text(x) for x in list(tr) if x.tag.rsplit('}',1)[-1] in ('td','th')]
            if cells: rows.append(cells)
        tables.append({'caption':caption,'rows':rows})
    return tables

all_data={}
for file in XML_DIR.glob('*.xml'):
    root=ET.parse(file).getroot()
    all_data[file.stem]={'references':references(root),'tables':table_rows(root)}

(ROOT/'review_structure.json').write_text(json.dumps(all_data,ensure_ascii=False,indent=2),encoding='utf-8')

rows=[]
# 2026 meta-analysis states its 12 included studies are references 34–45.
for ref in all_data['PMC12963353']['references']:
    if 34 <= ref['ref_number'] <= 45:
        rows.append({'source_review':'PMC12963353','source_location':f"reference {ref['ref_number']}",**ref})

# Match author/year pairs in the explicit included-study tables to bibliography entries.
for pmc, caption_hint in [('PMC12296984','Evidence table'),('PMC11491325','detailed information')]:
    refs=all_data[pmc]['references']
    used={}
    for table in all_data[pmc]['tables']:
        if caption_hint.lower() not in table['caption'].lower(): continue
        for cells in table['rows'][1:]:
            lead=cells[0] if cells else ''
            if lead.strip().lower() in ('age','author','author/year'): continue
            yr=re.search(r'(19|20)\d{2}', lead)
            author_part=lead.split(';')[0]
            author_part=re.sub(r'\bet\s+al\.?','',author_part,flags=re.I)
            author_part=re.sub(r'\([^)]*\)','',author_part)
            words=re.findall(r'[A-Za-zÀ-ÿ-]+',author_part)
            surname=words[-1] if words else ''
            first_names=words[:-1]
            initials=''.join(w[0] for w in first_names if w).lower()
            surname_rx=re.compile(r'\b'+re.escape(surname)+r'\b',re.I) if surname else None
            candidates=[r for r in refs if surname_rx and surname_rx.search(r['citation']) and (not yr or yr.group() in r['citation'])]
            if initials:
                strong=[r for r in candidates if re.search(r'\b'+re.escape(surname)+r'\s+'+re.escape(initials[0]),r['citation'],re.I)]
                if strong: candidates=strong
            candidates=sorted(candidates,key=lambda r:int(r['ref_number']) if str(r['ref_number']).isdigit() else 9999)
            key=(surname.lower(),yr.group() if yr else '')
            pos=used.get(key,0); used[key]=pos+1
            ref=candidates[min(pos,len(candidates)-1)] if candidates else {'ref_number':'','ref_id':'','title':'','year':yr.group() if yr else '','doi':'','pmid':'','citation':lead}
            rows.append({'source_review':pmc,'source_location':table['caption'],'table_lead':lead,'table_outcome':cells[-1] if cells else '','table_row':' | '.join(cells),**ref})

with (ROOT/'candidate_references.csv').open('w',newline='',encoding='utf-8-sig') as f:
    keys=sorted({k for r in rows for k in r})
    w=csv.DictWriter(f,fieldnames=keys);w.writeheader();w.writerows(rows)
(ROOT/'candidate_references.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'candidate_rows':len(rows),'output':str(ROOT/'candidate_references.csv')},ensure_ascii=False))
