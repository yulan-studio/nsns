import json
import sys
from pathlib import Path

from pypdf import PdfReader

sys.stdout.reconfigure(encoding="utf-8")


def main():
    path = Path(sys.argv[1])
    reader = PdfReader(str(path))
    if reader.is_encrypted:
        try:
            reader.decrypt("")
        except Exception as exc:
            raise RuntimeError("PDF已加密，无法读取") from exc
    pages = []
    for page in reader.pages:
        pages.append(page.extract_text() or "")
    text = "\n".join(pages).strip()
    meta = reader.metadata or {}
    print(json.dumps({
        "filename": path.name,
        "pages": len(reader.pages),
        "title": str(meta.get("/Title") or "").strip(),
        "text": text[:300000],
        "text_length": len(text),
    }, ensure_ascii=False))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(json.dumps({"error": str(exc)}, ensure_ascii=False))
        sys.exit(1)
