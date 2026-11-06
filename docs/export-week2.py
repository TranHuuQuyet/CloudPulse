"""Export editable Week 2 documents. Requires python-docx and python-pptx.

Run: python docs/export-week2.py
"""
from pathlib import Path
import re

from docx import Document
from docx.shared import Cm, Pt
from pptx import Presentation
from pptx.util import Inches, Pt as SlidePt

ROOT = Path(__file__).resolve().parent


def plain(text):
    return text.replace('`', '').replace('**', '')


def export_report():
    doc = Document()
    section = doc.sections[0]
    section.page_width, section.page_height = Cm(21), Cm(29.7)
    section.top_margin = section.bottom_margin = Cm(2)
    section.left_margin, section.right_margin = Cm(3), Cm(2)
    normal = doc.styles['Normal']
    normal.font.name, normal.font.size = 'Times New Roman', Pt(13)
    normal.paragraph_format.line_spacing = 1.3
    lines = (ROOT / 'tv5-report-week2.md').read_text(encoding='utf-8').splitlines()
    table = None
    for line in lines:
        if line.startswith('|'):
            cells = [plain(cell.strip()) for cell in line.strip('|').split('|')]
            if all(re.fullmatch(r'[:\- ]+', cell) for cell in cells):
                continue
            if table is None:
                table = doc.add_table(rows=0, cols=len(cells))
                table.style = 'Table Grid'
            row = table.add_row()
            for cell, text in zip(row.cells, cells):
                cell.text = text
            continue
        table = None
        if not line.strip():
            continue
        if line.startswith('#'):
            level = len(line) - len(line.lstrip('#'))
            if level == 2:
                doc.add_page_break()
            doc.add_heading(plain(line.lstrip('# ')), level=min(level - 1, 3))
        elif re.match(r'^\d+\. ', line):
            doc.add_paragraph(plain(re.sub(r'^\d+\. ', '', line)), style='List Number')
        else:
            doc.add_paragraph(plain(line))
    path = ROOT / 'CloudPulse_BaoCao_Tuan2.docx'
    doc.save(path)
    check = Document(path)
    assert len(check.tables) == 3
    assert any('Chương 2' in p.text for p in check.paragraphs)
    print(f'Word: {path.name}, {len(check.paragraphs)} paragraphs, {len(check.tables)} tables')


def export_slides():
    deck = Presentation()
    deck.slide_width, deck.slide_height = Inches(13.333), Inches(7.5)
    content = (ROOT / 'tv5-slides-week2.md').read_text(encoding='utf-8')
    for block in content.split('\n## ')[1:]:
        title, *lines = block.splitlines()
        slide = deck.slides.add_slide(deck.slide_layouts[1])
        slide.shapes.title.text = title
        slide.shapes.title.text_frame.paragraphs[0].font.size = SlidePt(32)
        body = slide.placeholders[1].text_frame
        body.clear()
        bullets = [line[2:] for line in lines if line.startswith('- ')]
        for index, text in enumerate(bullets):
            p = body.paragraphs[0] if index == 0 else body.add_paragraph()
            p.text = plain(text)
            p.font.size = SlidePt(24)
            p.space_after = SlidePt(20)
        footer = slide.shapes.add_textbox(Inches(.5), Inches(7), Inches(12), Inches(.3))
        footer.text_frame.paragraphs[0].text = f'CloudPulse | Khung trình bày tuần 2 | {len(deck.slides)}/15'
        footer.text_frame.paragraphs[0].font.size = SlidePt(11)
    path = ROOT / 'CloudPulse_KhungThuyetTrinh_15slides.pptx'
    deck.save(path)
    assert len(Presentation(path).slides) == 15
    print(f'PowerPoint: {path.name}, 15 slides')


if __name__ == '__main__':
    export_report()
    export_slides()
