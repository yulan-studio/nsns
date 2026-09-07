import sys
import xml.etree.ElementTree as ET


def text(node):
    return " ".join("".join(node.itertext()).split())


root = ET.parse(sys.argv[1]).getroot()
keywords = (
    "method", "result", "participant", "intervention", "measure",
    "statistical", "limitation", "adverse", "attrition", "dropout",
)

for section in root.iter("sec"):
    title = section.find("title")
    if title is None:
        continue
    heading = text(title)
    if any(keyword in heading.lower() for keyword in keywords):
        print(f"\n### {heading}\n{text(section)}")

for table in root.iter("table-wrap"):
    label = table.find("label")
    print(f"\n### TABLE {text(label) if label is not None else ''}\n{text(table)}")
