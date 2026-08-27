"""Extract visible text from a .pptx into stdout, one slide at a time."""
import re
import sys
import zipfile
from xml.etree import ElementTree as ET

A_NS = "{http://schemas.openxmlformats.org/drawingml/2006/main}"


def extract(path):
    with zipfile.ZipFile(path) as archive:
        slides = sorted(
            (name for name in archive.namelist()
             if re.fullmatch(r"ppt/slides/slide\d+\.xml", name)),
            key=lambda name: int(re.search(r"slide(\d+)", name).group(1)),
        )
        notes = {
            int(re.search(r"notesSlide(\d+)", name).group(1)): name
            for name in archive.namelist()
            if re.fullmatch(r"ppt/notesSlides/notesSlide\d+\.xml", name)
        }
        print(f"SLIDE_COUNT {len(slides)}")
        for index, name in enumerate(slides, 1):
            print(f"\n===== SLIDE {index} =====")
            print(paragraphs(archive.read(name)))
            note_name = notes.get(index)
            if note_name:
                note_text = paragraphs(archive.read(note_name))
                if note_text.strip():
                    print("--- NOTES ---")
                    print(note_text)


def paragraphs(xml_bytes):
    root = ET.fromstring(xml_bytes)
    lines = []
    for paragraph in root.iter(A_NS + "p"):
        bits = [node.text or "" for node in paragraph.iter(A_NS + "t")]
        text = "".join(bits).strip()
        if text:
            lines.append(text)
    return "\n".join(lines) if lines else "(no text)"


if __name__ == "__main__":
    source = sys.argv[1]
    dest = sys.argv[2] if len(sys.argv) > 2 else None
    if dest:
        with open(dest, "w", encoding="utf-8") as handle:
            sys.stdout = handle
            extract(source)
    else:
        extract(sys.argv[1])
