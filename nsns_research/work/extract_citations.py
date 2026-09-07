import glob
import xml.etree.ElementTree as ET

wanted = {"35564866", "38392469", "35625385", "36620673", "42116390"}


def node_text(node):
    return " ".join("".join(node.itertext()).split()) if node is not None else ""


for filename in glob.glob("work/fulltext-xml/PMC*.xml"):
    root = ET.parse(filename).getroot()
    meta = root.find(".//article-meta")
    if meta is None:
        continue
    pmid = meta.find(".//article-id[@pub-id-type='pmid']")
    if pmid is None or (pmid.text or "") not in wanted:
        continue
    authors = []
    for contrib in meta.findall(".//contrib[@contrib-type='author']"):
        authors.append(
            " ".join(
                part for part in (
                    node_text(contrib.find(".//surname")),
                    node_text(contrib.find(".//given-names")),
                ) if part
            )
        )
    print(node_text(pmid))
    print(node_text(meta.find(".//article-title")))
    print("; ".join(authors))
    print(node_text(meta.find(".//article-id[@pub-id-type='doi']")))
    print()
