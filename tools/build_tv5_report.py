"""Build the editable TV5 report. Dependencies: python-docx and Pillow.

python tools/build_tv5_report.py --libs <optional python-docx dependency directory>
"""
from pathlib import Path
import argparse
import json
import re
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--libs')
args = parser.parse_args()
if args.libs:
    sys.path.insert(0, args.libs)

from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/report'
ASSETS = OUT / 'assets'
EVIDENCE = ROOT / 'docs/evidence/w3-05'
ASSETS.mkdir(parents=True, exist_ok=True)
DATA = json.loads((EVIDENCE / 'results.json').read_text(encoding='utf-8'))
CASES = {c['name']: c for c in DATA['cases']}
BLUE = '17365D'
TEAL = '007F86'
FIGURES = []
TABLES = []
doc = Document()
sec = doc.sections[0]
sec.page_width, sec.page_height = Cm(21), Cm(29.7)
sec.left_margin, sec.right_margin = Cm(3), Cm(2)
sec.top_margin, sec.bottom_margin = Cm(2), Cm(2)
sec.header_distance, sec.footer_distance = Cm(0.9), Cm(1)
sec.different_first_page_header_footer = True

normal = doc.styles['Normal']
normal.font.name = 'Times New Roman'
normal.font.size = Pt(13)
normal.paragraph_format.line_spacing = 1.3
normal.paragraph_format.space_after = Pt(6)
for name in ['Normal', 'Heading 1', 'Heading 2', 'Heading 3', 'Caption']:
    style = doc.styles[name]
    style.font.name = 'Times New Roman'
    style.element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'), 'Times New Roman')
for name, size in [('Heading 1', 17), ('Heading 2', 14), ('Heading 3', 13)]:
    s = doc.styles[name]
    s.font.size = Pt(size)
    s.font.color.rgb = RGBColor.from_string(BLUE)
    s.paragraph_format.keep_with_next = True
    s.paragraph_format.space_before = Pt(12)
    s.paragraph_format.space_after = Pt(7)
doc.styles['Heading 1'].paragraph_format.page_break_before = True
doc.styles['Caption'].font.size = Pt(11)
doc.styles['Caption'].font.color.rgb = RGBColor.from_string(BLUE)
doc.styles['Caption'].paragraph_format.space_after = Pt(7)

header = sec.header.paragraphs[0]
header.alignment = WD_ALIGN_PARAGRAPH.RIGHT
r = header.add_run('CLOUDPULSE  |  BÁO CÁO ĐỒ ÁN  |  TV5')
r.font.name = 'Times New Roman'
r.font.size = Pt(9)
r.font.color.rgb = RGBColor.from_string(TEAL)
footer = sec.footer.paragraphs[0]
footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
footer.add_run('CloudPulse · Bản ghép tuần 3 · Trang ').font.size = Pt(9)

def field(paragraph, instruction, cached='1'):
    run = paragraph.add_run()
    start = OxmlElement('w:fldChar'); start.set(qn('w:fldCharType'), 'begin')
    code = OxmlElement('w:instrText'); code.set(qn('xml:space'), 'preserve'); code.text = instruction
    separate = OxmlElement('w:fldChar'); separate.set(qn('w:fldCharType'), 'separate')
    val = OxmlElement('w:t'); val.text = cached
    end = OxmlElement('w:fldChar'); end.set(qn('w:fldCharType'), 'end')
    for el in [start, code, separate, val, end]: run._r.append(el)

field(footer, ' PAGE ')
settings = doc.settings.element
update = OxmlElement('w:updateFields'); update.set(qn('w:val'), 'true'); settings.append(update)

def clean(text):
    text = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'\1', text)
    return text.replace('`', '').replace('**', '')

def rich(paragraph, text):
    for chunk in re.split(r'(\*\*.*?\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))', text):
        if not chunk: continue
        if chunk.startswith('**'):
            paragraph.add_run(chunk[2:-2]).bold = True
        elif chunk.startswith('`'):
            r = paragraph.add_run(chunk[1:-1]); r.font.name = 'Consolas'; r.font.size = Pt(10)
        else:
            paragraph.add_run(clean(chunk))

def para(text, bold=False):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    rich(p, text)
    if bold:
        for r in p.runs: r.bold = True
    return p

def heading(text, level=2):
    return doc.add_heading(text, level)

def shade(cell, fill):
    shd = OxmlElement('w:shd'); shd.set(qn('w:fill'), fill); cell._tc.get_or_add_tcPr().append(shd)

def note(title, text):
    t = doc.add_table(rows=1, cols=1)
    t.autofit = False; t.columns[0].width = Cm(16)
    cell = t.cell(0, 0); shade(cell, 'FFF2CC')
    p = cell.paragraphs[0]
    r = p.add_run(title); r.bold = True; r.font.size = Pt(11); r.font.color.rgb = RGBColor.from_string('885400')
    p = cell.add_paragraph(text)
    for r in p.runs: r.font.size = Pt(11)
    for p in cell.paragraphs:
        p.paragraph_format.space_after = Pt(4); p.paragraph_format.line_spacing = 1.05
    doc.add_paragraph().paragraph_format.space_after = Pt(0)

def table(headers, rows, title=None):
    if title:
        p=doc.add_paragraph(title, 'Caption'); p.paragraph_format.keep_with_next=True
        TABLES.append(title)
    t = doc.add_table(rows=1, cols=len(headers)); t.style = 'Table Grid'
    t.autofit = False
    # Prevent the 5-column acceptance matrix from making every cell excessively narrow.
    weights = [1] * len(headers)
    if len(headers)==5: weights=[0.8,1.1,1.5,3.1,1.1]
    if len(headers)==4: weights=[1.1,2,2,1.5]
    for c,w in zip(t.columns,weights): c.width=Cm(16*w/sum(weights))
    for i,h in enumerate(headers): t.rows[0].cells[i].text=clean(h)
    repeat=OxmlElement('w:tblHeader'); t.rows[0]._tr.get_or_add_trPr().append(repeat)
    for cell in t.rows[0].cells:
        shade(cell, BLUE)
        for r in cell.paragraphs[0].runs: r.bold=True; r.font.color.rgb=RGBColor(255,255,255)
    for idx,row in enumerate(rows):
        cells=t.add_row().cells
        for i,text in enumerate(row[:len(headers)]): cells[i].text=clean(str(text))
        if idx%2==0:
            for c in cells: shade(c,'F2F6FA')
    for row in t.rows:
        cant=OxmlElement('w:cantSplit'); row._tr.get_or_add_trPr().append(cant)
        for column_index, cell in enumerate(row.cells):
            cell.width = Cm(16 * weights[column_index] / sum(weights))
            for p in cell.paragraphs:
                p.paragraph_format.line_spacing=1.05
                p.paragraph_format.space_after=Pt(4)
                for r in p.runs: r.font.size=Pt(10)
    doc.add_paragraph().paragraph_format.space_after=Pt(0)
    return t

def image(path, caption, annotation, max_height=17.5, max_width=16):
    path=Path(path)
    with Image.open(path) as im: w,h=im.size
    width=min(max_width, max_height*w/h)
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.keep_with_next=True
    p.add_run().add_picture(str(path), width=Cm(width))
    p=doc.add_paragraph(caption,'Caption'); p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.keep_with_next=True
    FIGURES.append(caption)
    note('Ghi chú hình', annotation)

def screenshot(name, caption, annotation, crop=None, **kwargs):
    path=EVIDENCE/name
    if crop:
        # Layout crop only. Original evidence remains untouched.
        with Image.open(path) as im:
            path=ASSETS/(Path(name).stem+'-detail.png'); im.crop(crop).save(path)
        annotation += ' Ảnh cắt vùng để đọc rõ; bản gốc: docs/evidence/w3-05/'+name+'.'
    else: annotation += ' Nguồn: docs/evidence/w3-05/'+name+'.'
    image(path,caption,annotation,**kwargs)

def diagram():
    im=Image.new('RGB',(1800,1080),'white'); d=ImageDraw.Draw(im)
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',27)
    bold=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',29)
    boxes={
      'ui':(40,90,390,200,'React Dashboard'),
      'gateway':(510,90,875,200,'HTTP API Gateway'),
      'api':(1010,90,1380,200,'Lambda API (.NET 8)'),
      'schedule':(40,380,390,500,'EventBridge\nrate(1 minute)'),
      'checker':(510,380,875,500,'Lambda HealthChecker'),
      'target':(1010,380,1380,500,'Website / API\nMockTarget'),
      'monitors':(200,700,580,820,'DynamoDB\nMonitors'),
      'checks':(700,700,1080,820,'DynamoDB\nCheckResults · TTL 7 ngày'),
      'incidents':(1200,700,1580,820,'DynamoDB\nIncidents'),
      'sns':(1460,90,1760,200,'SNS Email'),
      'telegram':(1460,380,1760,500,'Telegram API'),
      'logs':(620,940,1190,1040,'CloudWatch Logs'),
    }
    def arrow(points,color='#597B99'):
        d.line(points,fill=color,width=5)
        x,y=points[-1]; ax,ay=points[-2]
        import math
        angle=math.atan2(y-ay,x-ax)
        wing=[(x,y),(x-18*math.cos(angle-.5),y-18*math.sin(angle-.5)),(x-18*math.cos(angle+.5),y-18*math.sin(angle+.5))]
        d.polygon(wing,fill=color)
    for points in [
        [(390,145),(510,145)],[(875,145),(1010,145)],[(390,440),(510,440)],[(875,440),(1010,440)],
        [(1195,200),(1195,280),(1390,280),(1390,700)],
        [(1195,280),(890,280),(890,700)],[(890,280),(390,280),(390,700)],
        [(690,500),(690,590),(390,590),(390,700)],[(690,590),(890,590),(890,700)],
        [(890,590),(1390,590),(1390,700)],[(875,415),(940,415),(940,240),(1610,240),(1610,200)],
        [(940,415),(940,550),(1610,550),(1610,500)],
        [(690,500),(620,500),(620,900),(905,900),(905,940)],
    ]: arrow(points)
    for key,(x1,y1,x2,y2,label) in boxes.items():
        fill='#E7F4F5' if key in ['monitors','checks','incidents'] else '#EDF3FA'
        d.rounded_rectangle((x1,y1,x2,y2),radius=20,fill=fill,outline='#17365D',width=3)
        d.multiline_text(((x1+x2)/2,(y1+y2)/2),label,font=bold if key=='checker' else font,fill='#17365D',anchor='mm',align='center',spacing=7)
    d.text((45,20),'CLOUDPULSE — KIẾN TRÚC LOGIC',font=bold,fill='#17365D')
    path=ASSETS/'architecture.png'; im.save(path); return path

def code(text):
    t=doc.add_table(rows=1,cols=1); c=t.cell(0,0); shade(c,'F2F4F7')
    for i,line in enumerate(text.strip().splitlines()):
        p=c.paragraphs[0] if i==0 else c.add_paragraph()
        p.paragraph_format.line_spacing=1.05; p.paragraph_format.space_after=Pt(2)
        r=p.add_run(line);r.font.name='Consolas';r.font.size=Pt(9)
    doc.add_paragraph()

def chapter_extra(chapter):
    if chapter==3:
        note('Ghi chú hoàn thiện Chương 3', 'Sơ đồ và thiết kế đã có nội dung. TV1 bổ sung ảnh stack/IAM/TTL ở Chương 4; TV2 bổ sung log chuyển trạng thái ở Chương 5. Không thay sơ đồ logic bằng tuyên bố đã xác nhận toàn bộ tài nguyên triển khai.')
    if chapter==4:
        heading('4.4. Dashboard và dữ liệu đo trên AWS')
        para('Ảnh dưới đây được chụp từ Dashboard kết nối API AWS qua Vite proxy. Hai monitor minh họa là Mock 200 và Mock 500. Nhãn Dữ liệu API biểu thị dữ liệu truy xuất từ backend; tên Mock chỉ endpoint dùng thử nghiệm.')
        screenshot('live-dashboard.png','Hình 4.1. Dashboard kết nối API AWS và thống kê latency.', 'Dữ liệu AWS thật. Đối chiếu thời điểm chụp trong results.json; số liệu trên ảnh là snapshot. Thẻ uptime là tỷ lệ monitor UP hiện tại, chưa phải uptime theo thời gian; p95 chưa có nguồn tổng hợp nên hiện —.')
        screenshot('live-incident.png','Hình 4.2. Incident OPEN của monitor Mock 500.', 'Dữ liệu AWS thật. Quan sát tên monitor, nguyên nhân HTTP 500, nhãn OPEN, thời điểm bắt đầu và downtime. Hình này chưa chứng minh recovery hoặc chống spam.',crop=(120,980,1248,1265),max_height=8)
        heading('4.5. Cấu hình local và triển khai frontend')
        code('cd frontend\n# Sao chép .env.example thành .env (không commit)\nVITE_API_BASE_URL=https://ex9kcfdj5a.execute-api.ap-southeast-1.amazonaws.com\nVITE_MOCK_URL=https://ex9kcfdj5a.execute-api.ap-southeast-1.amazonaws.com/mock\n# Khởi động lại Vite sau khi đổi cấu hình\nnpm.cmd run dev\n# Kiểm tra build production\nnpm.cmd run build\nnpm.cmd run preview')
        note('Lưu ý môi trường', 'Dev dùng proxy để chuyển /api tới AWS. Preview/hosting gọi trực tiếp API Gateway và hiện còn lỗi CORS khi POST JSON. Không dùng việc dev chạy được để kết luận production đã đạt. File .env không nằm trong commit.')
        heading('4.6. Các ảnh hạ tầng cần ghép đúng vị trí')
        for title,text in [
          ('[E01 — CHỜ TV1] SAM outputs và tài nguyên AWS', 'Chèn ảnh ngay tại đây. Ảnh cần thấy region ap-southeast-1, trạng thái deploy và outputs API/MockTarget; ghi ngày giờ. Chú thích đề xuất: Hình 4.3. Tài nguyên CloudPulse sau triển khai AWS SAM.'),
          ('[E06-A — CHỜ TV1] IAM roles và policy', 'Chèn sau E01. Thể hiện role của từng Lambda, action và ARN bảng/topic được cấp; che thông tin tài khoản cần thiết. Chú thích đề xuất: Hình 4.4. Phạm vi quyền của Lambda.'),
          ('[E06-B — CHỜ TV1] TTL CheckResults', 'Chèn sau ảnh IAM. Thể hiện tên bảng và thuộc tính expireAt đã bật TTL. Chú thích đề xuất: Hình 4.5. Cấu hình TTL cho lịch sử kiểm tra.'),
        ]: note(title,text)
    if chapter==5:
        heading('5.4. Bằng chứng chi tiết kiểm thử frontend')
        check=CASES['live-sync-and-check-now-network']['detail']
        table(['Chỉ số','Giá trị đo','Cách hiểu'],[
            ['Check Now end-to-end',f"{check['responseMs']} ms",'Thời gian từ click đến response hoàn tất; một mẫu, không đại diện SLA.'],
            ['Latency HTTP đích',f"{check['check']['responseTimeMs']} ms",'Đo trong backend; không đồng nhất thời gian frontend chờ response.'],
            ['HTTP status',check['check']['statusCode'],'Mock 200 trả thành công.'],
            ['Thời điểm check (UTC)',check['check']['checkedAt'],'Đối chiếu điểm trong CheckResults cùng timestamp và latency.'],
            ['Hai request sau Sync',str(abs(check['sync'][0]['at']-check['sync'][1]['at']))+' ms chênh khởi phát','Hai request riêng, được khởi phát song song.'],
            ['Hai request sau Check Now',str(abs(check['afterCheck'][0]['at']-check['afterCheck'][1]['at']))+' ms chênh khởi phát','Bằng chứng Network Playwright; không phải thời gian hoàn tất hai request.'],
        ],'Bảng 5.3. Số đo kiểm thử Check Now và refresh từ results.json.')
        screenshot('race-B-after-delayed-A.png','Hình 5.1. Monitor B được giữ đúng sau khi response A trả về muộn.', 'Response lịch sử được điều khiển trong trình duyệt, dùng ID AWS thật: A chậm 1.200 ms, đổi lựa chọn cách 150 ms. Kết quả B giữ 222 ms và CAUSE_B. Không dùng hình làm bằng chứng latency AWS.',crop=(120,480,1248,1265))
        screenshot('live-empty-monitor.png','Hình 5.2. Empty state với monitor mới trên AWS.', 'Đã tạo monitor tạm trên AWS, query checks=[] và incidents=[] trước lượt kiểm tra định kỳ, sau thử đã xóa monitor tạm. Đây là empty state thật, không phải lỗi dữ liệu.',crop=(120,480,1248,1310))
        screenshot('api-error.png','Hình 5.3. Thông báo lỗi API và hướng dẫn Sync để thử lại.', 'Response HTTP 503 được điều khiển trong trình duyệt. Sau bỏ response lỗi, Sync tải lại dữ liệu AWS thành công; không fallback mock.',crop=(120,470,1248,780),max_height=8)
        screenshot('invalid-base-url.png','Hình 5.4. Cấu hình API URL sai không chuyển âm thầm sang mock.', 'Mode acceptance-invalid dùng target http://127.0.0.1:1; Vite proxy trả 502. Giao diện hiện lỗi, giữ nút Sync và không sinh monitor giả lập. Nhãn Dữ liệu API cho biết chế độ cấu hình, không có nghĩa request đã thành công.')
        screenshot('mock-mode.png','Hình 5.5. Chế độ dữ liệu giả lập khi API URL để trống.', 'Mode acceptance-mock đặt VITE_API_BASE_URL rỗng; chart và lịch sử dùng mock. Đây là ảnh kiểm thử fallback, không phải bằng chứng AWS.')
        screenshot('mobile-tooltip.png','Hình 5.6. Tooltip latency và mã HTTP trên màn hình 390 px.', 'Dữ liệu AWS thật. Tooltip hiển thị số ms và HTTP 500; giờ trục X đang UTC, đã ghi lỗi. Ảnh cắt vùng biểu đồ từ màn hình mobile.',crop=(0,1080,390,1610),max_height=16,max_width=10)
        screenshot('mobile-incidents.png','Hình 5.7. Bảng IncidentHistory hỗ trợ cuộn ngang trên mobile.', 'Khung bảng rộng 316 px, nội dung rộng 760 px; đã đặt scrollLeft=200 px để xác minh cuộn ngang. Sau sửa min-w-0, document rộng 390 px bằng viewport.',crop=(0,1590,390,1993),max_height=12,max_width=10)
        screenshot('preview-direct-api.png','Hình 5.8. Preview production tải dữ liệu bằng API Gateway trực tiếp.', 'GET/hiển thị trang thực hiện được. Ảnh trang này KHÔNG chứng minh POST đạt: console trình duyệt ghi lỗi CORS ở probe POST JSON, chi tiết bên dưới.')
        cors=CASES['preview-direct-api-CORS']['detail']
        code('\n'.join(cors.get('errors',[])[:2]))
        note('Kết luận nghiệm thu production', 'CORS POST: CHƯA ĐẠT. Browser chặn preflight vì response OPTIONS không có HTTP OK status. Người xử lý: TV1/TV3. Khi backend sửa, chạy lại preview, lưu console/Network mới rồi thay ghi nhận này.')
        heading('5.5. Vị trí bổ sung bằng chứng backend và chi phí')
        for title,text in [
          ('[E04 — CHỜ TV2] CloudWatch Logs', 'Chèn ngay tại đây. Cần chuỗi log cùng monitor ID, gồm FAIL 1, FAIL 2, chuyển DOWN, FAIL tiếp và recovery. Ghi thời điểm UTC/UTC+7 thống nhất; dùng log để tính MTTD, không suy ra từ chart.'),
          ('[E05-A — CHỜ TV2] Telegram DOWN và recovery', 'Chèn sau E04. Giữ cặp cảnh báo cùng monitor, thời gian gửi/nhận và nguyên nhân; che token/chat ID. Ghi rõ số thông báo để đối chiếu chống spam.'),
          ('[E05-B — CHỜ TV2] Email DOWN và recovery', 'Chèn sau Telegram. Giữ tiêu đề/nội dung/thời điểm, che email cá nhân nếu cần. Chưa nhận được ảnh thì giữ trạng thái CHỜ, không dùng hình minh họa.'),
          ('[E03-RESOLVED — CHỜ KIỂM THỬ] Sự cố đã khôi phục', 'Chèn sau cảnh báo recovery. Cần đúng monitor và incidentId của sự cố OPEN, resolvedAt và downtimeSeconds. Thêm monitor 200 khác không chứng minh phục hồi Incident cũ.'),
          ('[E07 — CHỜ TV1] Billing / Cost Explorer', 'Chèn cuối mục này và điền bảng 5.2. Cần kỳ đo, vùng, usage, chi phí trước/sau credits và thuế. Chưa có hóa đơn thì không kết luận hệ thống tốn $0.'),
        ]: note(title,text)

def convert_markdown(text):
    lines=text.splitlines(); i=0; chapter=None; table_index={}
    while i<len(lines):
        line=lines[i].strip()
        if not line: i+=1; continue
        if line.startswith('```'):
            language=line[3:]; block=[]; i+=1
            while i<len(lines) and not lines[i].startswith('```'): block.append(lines[i]);i+=1
            if language=='mermaid':
                image(diagram(),'Hình 3.1. Kiến trúc logic CloudPulse dựa trên SAM và mã nguồn.', 'Sơ đồ thiết kế, không phải ảnh console AWS. Telegram nằm ngoài AWS; EventBridge kích hoạt kiểm tra theo lịch. Luồng Check Now thực thi trong Lambda API; không dùng Step Functions.',max_height=11)
            else: code('\n'.join(block))
            i+=1;continue
        if line.startswith('|'):
            block=[]
            while i<len(lines) and lines[i].strip().startswith('|'):
                block.append([c.strip() for c in lines[i].strip().strip('|').split('|')]);i+=1
            rows=[row for row in block[1:] if not all(re.fullmatch(r'[:\- ]+',c) for c in row)]
            table_index[chapter]=table_index.get(chapter,0)+1
            title=f'Bảng {chapter}.{table_index[chapter]}. '+{1:'Nội dung tổng hợp',2:'Yêu cầu và tiêu chí',3:'Thành phần và thiết kế',4:'Danh mục bằng chứng',5:'Kiểm thử và đánh giá'}.get(chapter,'Nội dung')
            table(block[0],rows,title);continue
        m=re.match(r'^(#{2,4})\s+(.+)',line)
        if m:
            title=m.group(2)
            if title.startswith('Chương '):
                if chapter: chapter_extra(chapter)
                chapter=int(re.search(r'Chương (\d+)',title).group(1))
                title=title.replace(' — khung ghép bằng chứng','').replace(' — khung điền kết quả','')
                heading(title,1)
            else: heading(title,min(len(m.group(1))-1,3))
            i+=1;continue
        if line.startswith('# '): i+=1;continue
        if re.match(r'^[-*]\s',line) or re.match(r'^\d+\.\s',line):
            p=doc.add_paragraph(style='List Bullet' if line[0] in '-*' else 'List Number')
            rich(p,re.sub(r'^([-*]|\d+\.)\s+','',line));i+=1;continue
        block=[line];i+=1
        while i<len(lines) and lines[i].strip() and not re.match(r'^(#|\||```|[-*]\s|\d+\.\s)',lines[i].strip()): block.append(lines[i].strip());i+=1
        para(' '.join(block))
    if chapter: chapter_extra(chapter)

# Cover
for text,size,bold in [('[ĐIỀN TÊN TRƯỜNG]',15,True),('[ĐIỀN KHOA / BỘ MÔN]',14,False),('',12,False),('BÁO CÁO ĐỒ ÁN',22,True),('CLOUDPULSE',30,True),('GIÁM SÁT WEBSITE VÀ API\nTRÊN AWS SERVERLESS',18,True)]:
    p=doc.add_paragraph();p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after=Pt(15)
    r=p.add_run(text);r.font.size=Pt(size);r.bold=bold;r.font.color.rgb=RGBColor.from_string(BLUE)
para('Môn học: [ĐIỀN TÊN MÔN]')
para('Giảng viên hướng dẫn: [ĐIỀN HỌ VÀ TÊN]')
para('Nhóm / Lớp: [ĐIỀN NHÓM, LỚP]')
para('Thành viên: [ĐIỀN HỌ TÊN VÀ MÃ SINH VIÊN CỦA 5 THÀNH VIÊN]')
p=doc.add_paragraph('Bản ghép TV5 · cập nhật 11/11/2026');p.alignment=WD_ALIGN_PARAGRAPH.CENTER
p=doc.add_paragraph('[ĐỊA ĐIỂM], tháng 11 năm 2026');p.alignment=WD_ALIGN_PARAGRAPH.CENTER

heading('Hướng dẫn hoàn thiện và phạm vi tài liệu',1)
para('Báo cáo tổng hợp bản thảo Chương 1–2, thiết kế Chương 3 và kết quả tích hợp/kiểm thử tuần 3. Ảnh có sẵn đã được chèn trực tiếp vào đúng mục. Các ghi chú nền vàng là hướng dẫn biên tập hoặc vị trí đang chờ bằng chứng, không phải kết quả kiểm thử.')
note('Thông tin cần điền trước khi nộp', 'Thay các ô [ĐIỀN...] ở bìa, bảng thành viên và phụ lục bằng thông tin chính thức. Chưa có mẫu của trường: khổ A4, Times New Roman 13, lề trái 3 cm/phải 2 cm/trên dưới 2 cm; nếu trường quy định khác, chỉnh style toàn văn.')
note('Phân biệt bằng chứng', 'AWS THẬT: Dashboard, Incident OPEN, Check Now, monitor rỗng mới tạo và mobile. RESPONSE ĐIỀU KHIỂN: race, HTTP 503 và empty bổ sung. MOCK: env không có API URL. THIẾT KẾ: sơ đồ kiến trúc. Các ảnh đều có chú thích nguồn và giới hạn.')
note('Hai vấn đề chưa đóng', 'CORS preflight của POST ở preview/hosting còn lỗi; trục X chart đang UTC và chậm 7 giờ so với Incident UTC+7. Hai vấn đề đã ghi ở Chương 5; không đánh dấu toàn hệ thống nghiệm thu hoàn tất.')
table(['Vị trí','Cần bổ sung','Phụ trách'],[
 ['Bìa / phân công','Trường, môn, lớp, tên/MSSV và xác nhận đóng góp','Cả nhóm'],
 ['4.6 / E01, E06','SAM outputs, IAM và TTL','TV1'],
 ['5.5 / E04, E05','Logs, Telegram và Email DOWN/UP','TV2'],
 ['5.5 / E03-RESOLVED','Khôi phục cùng Incident','TV2/TV3/TV5'],
 ['5.2 và 5.5 / E07','Bảng giá đúng region, hóa đơn, credits','TV1'],
 ['Phụ lục B','Video dự phòng và phân vai demo','Cả nhóm'],
],'Bảng hướng dẫn. Các vị trí còn chờ bổ sung trước khi nộp.')

heading('Mục lục',1)
para('Các tiêu đề trong tài liệu dùng Heading 1–3 để điều hướng trong Word. Mục lục bên dưới có thể cập nhật bằng bấm phải → Update Field → Update entire table; số trang phụ thuộc phiên bản Word và máy in.')
field(doc.add_paragraph(),' TOC \\o "1-3" \\h \\z \\u ','Bấm phải tại đây và chọn Update Field để tạo mục lục có số trang.')
for title in ['Chương 1. Giới thiệu đề tài','Chương 2. Phân tích yêu cầu','Chương 3. Thiết kế kiến trúc','Chương 4. Triển khai','Chương 5. Kiểm thử và đánh giá','Chương 6. Kết luận','Tài liệu tham khảo','Phụ lục A. Hướng dẫn chạy và tái tạo kiểm thử','Phụ lục B. Demo 7 bước','Phụ lục C. Danh mục ảnh và bảng']:
    doc.add_paragraph(title)

heading('Phân công và từ viết tắt',1)
table(['Vai trò','Họ tên / MSSV','Nhiệm vụ','Đóng góp xác nhận'],[
 ['TV1','[ĐIỀN]','Hạ tầng SAM, deploy, IAM, chi phí','[CHỜ NHÓM XÁC NHẬN]'],
 ['TV2','[ĐIỀN]','HealthChecker, State Machine, cảnh báo','[CHỜ NHÓM XÁC NHẬN]'],
 ['TV3','[ĐIỀN]','REST API, Check Now, MockTarget','[CHỜ NHÓM XÁC NHẬN]'],
 ['TV4','[ĐIỀN]','Dashboard, danh sách và thao tác monitor','[CHỜ NHÓM XÁC NHẬN]'],
 ['TV5','[ĐIỀN]','Chart, IncidentHistory, kiểm thử, báo cáo/slide','[CHỜ NHÓM XÁC NHẬN]'],
],'Bảng phân công. Vai trò nhóm theo docs/duties.md.')
table(['Từ viết tắt','Giải thích'],[['API','Application Programming Interface'],['AWS','Amazon Web Services'],['IAM','Identity and Access Management'],['IaC','Infrastructure as Code'],['MTTD','Mean Time To Detect'],['MVP','Minimum Viable Product'],['SAM','Serverless Application Model'],['SNS','Simple Notification Service'],['SSRF','Server-Side Request Forgery'],['TTL','Time To Live'],['UTC','Coordinated Universal Time']],'Bảng thuật ngữ. Từ viết tắt sử dụng trong báo cáo.')

week2=(ROOT/'docs/tv5-report-week2.md').read_text(encoding='utf-8')
week2=week2[week2.index('## Chương 1.'):]
week2=re.sub(r'### 2\.4\..*?(?=### 2\.5\.)','### 2.4. Nguyên tắc đánh giá chi phí\n\nSo sánh trên cùng số endpoint, chu kỳ và thời gian vận hành; tính đủ các dịch vụ và ưu đãi thực sự áp dụng. Bảng FinOps tại mục 5.2 tách ước tính với hóa đơn thực tế. Không mặc định CloudPulse miễn phí hoặc tái sử dụng giá vùng khác cho ap-southeast-1.\n\n',week2,flags=re.S)
week2=week2.replace('EventBridge Scheduler và DynamoDB','EventBridge theo lịch và DynamoDB')
week2=week2.replace('### 2.5. Tiêu chí nghiệm thu IncidentHistory tuần 2','### 2.5. Tiêu chí nghiệm thu IncidentHistory giai đoạn mock (tuần 2)')
convert_markdown(week2)
week3=(ROOT/'docs/tv5-report-week3.md').read_text(encoding='utf-8')
week3=week3[week3.index('## Chương 3.'):]
convert_markdown(week3)

heading('Chương 6. Kết luận',1)
heading('6.1. Kết quả đã kiểm chứng')
para('TV5 đã tích hợp LatencyChart và IncidentHistory với API thật theo monitor được chọn; Sync và Check Now tải lại hai lịch sử song song. Kiểm thử trình duyệt xác nhận dữ liệu AWS được hiển thị, ánh xạ Incident OPEN đúng monitor, các chỉ số latency tính đúng trên cửa sổ dữ liệu, response cũ không ghi đè lựa chọn mới, trạng thái rỗng/lỗi và chế độ mock hoạt động theo thiết kế. Giao diện mobile đã được sửa để trang không tràn ngang và bảng vẫn cuộn được.')
detail=CASES['live-sync-and-check-now-network']['detail']
para(f"Lần đo được lưu trong results.json ghi nhận Check Now {detail['responseMs']} ms, latency HTTP đích {detail['check']['responseTimeMs']} ms, HTTP {detail['check']['statusCode']}. Đây là kết quả của một mẫu với monitor thử nghiệm, không đủ suy ra hiệu năng ở tải lớn hoặc cam kết SLA. Lint và build đã chạy thành công; bundle vẫn có cảnh báo kích thước trên 500 kB.")
heading('6.2. Hạn chế và công việc tiếp theo')
para('Production POST còn bị CORS và giờ chart chưa thống nhất UTC+7. Ca khôi phục RESOLVED, chống spam, timeout, SSRF, MTTD, độ trễ cảnh báo và hóa đơn cần bổ sung chứng cứ trước khi kết luận nghiệm thu toàn hệ thống. Các số liệu uptime/p95 tổng hợp chưa có API lịch sử đầy đủ. MVP chưa bao gồm xác thực đa người dùng hoặc kiểm tra đa vùng.')
heading('6.3. Hướng phát triển và bài học')
para('Hướng phát triển gồm thống nhất timestamp ISO UTC trên API, xác thực và phân quyền, củng cố tính nhất quán khi checker chạy trùng, tổng hợp thống kê lịch sử, kiểm tra nhiều vùng và cảnh báo theo mục tiêu vận hành. Quá trình tích hợp cho thấy cần kiểm thử cả dev lẫn production, phân biệt latency đích với thời gian API end-to-end, đồng thời tách dữ liệu minh họa khỏi bằng chứng vận hành để kết luận có thể đối chiếu.')

heading('Tài liệu tham khảo',1)
for text in [
 '[1] Nhóm CloudPulse. docs/requirements.md, docs/acceptance-criteria.md, docs/use-cases.md và docs/duties.md. Tài liệu nội bộ dự án.',
 '[2] Nhóm CloudPulse. template.yaml và mã nguồn backend/frontend trong repository. Dùng đối chiếu thiết kế ngày 11/11/2026; không thay thế cấu hình deployment thực tế.',
 '[3] TV5. docs/evidence/w3-05/results.json và ảnh kiểm thử. Lần chạy lưu: '+DATA['executedAt']+'.',
 '[4] Amazon Web Services. AWS Free Tier. https://aws.amazon.com/free/ — nguồn trong bản thảo đã kiểm tra 11/11/2026; cần TV1 xác nhận điều kiện tài khoản.',
 '[5] Amazon Web Services. Amazon CloudWatch Pricing. https://aws.amazon.com/cloudwatch/pricing/ — nguồn trong bản thảo đã kiểm tra 11/11/2026; chưa chốt giá vùng Singapore.',
 '[6] Amazon Web Services. AWS Pricing Calculator. https://calculator.aws/ — công cụ để TV1 hoàn thiện dự toán theo region và usage.',
]: para(text)

heading('Phụ lục A. Hướng dẫn chạy và tái tạo kiểm thử',1)
para('Sao chép frontend/.env.example sang frontend/.env, điền hai URL đã bàn giao và khởi động lại Vite. Giữ .env trong gitignore. Các mode acceptance-mock và acceptance-invalid phục vụ thử nghiệm độc lập, không thay .env AWS gốc.')
code('cd frontend\nnpm.cmd run lint\nnpm.cmd run build\nnpm.cmd run dev\n# Bộ nghiệm thu tự động (cần Node, Chrome, Playwright)\nnode tests/w3-acceptance.mjs "<playwright/index.mjs>"')
para('Cách khởi chạy bốn server/port và điều kiện dữ liệu được ghi trong docs/evidence/w3-05/README.md. Script tạo rồi xóa một monitor tạm cho ca dữ liệu rỗng; gọi Check Now trên Mock 200. Cần hai monitor Mock 200 và Mock 500 đã có lịch sử, cùng Incident OPEN để tái tạo các ca hiện tại.')
note('Vị trí kiểm tra bằng chứng', 'results.json chứa URL request, timestamp, response và trạng thái từng ca. KNOWN_FAILURE của preview là lỗi CORS đã tái hiện. Không được đổi thành PASS khi backend chưa sửa và chưa chạy lại. Sau mỗi lần chạy, số liệu/ảnh là snapshot mới; cần đồng bộ lại báo cáo.')

heading('Phụ lục B. Demo 7 bước',1)
table(['Bước','Thao tác','Điểm cần quan sát'],[
 ['1','Mở monitor khỏe và chọn đúng monitor','UP; chart nhãn API; kiểm tra tooltip.'],
 ['2','Gây lỗi HTTP 500 hoặc timeout ở target thử nghiệm','Ghi thời điểm gây lỗi; giữ nguyên monitor ID.'],
 ['3','Quan sát FAIL lần 1','Chưa xác nhận DOWN chỉ từ một FAIL.'],
 ['4','Đợi FAIL lần 2','DOWN, cảnh báo mở và timestamp trong Logs.'],
 ['5','Mở IncidentHistory','OPEN, reason và downtime của cùng monitor.'],
 ['6','Khôi phục chính target đó về 200','UP, RESOLVED, downtime cố định, cảnh báo recovery.'],
 ['7','Đối chiếu Logs / thông báo / chi phí','Khớp thời gian, tránh gửi lặp; nêu số liệu hóa đơn xác nhận.'],
],'Bảng phụ lục B. Kịch bản trình diễn.')
note('[CHỜ NHÓM] Cơ chế đổi trạng thái và video dự phòng', 'API hiện chưa được báo cáo là hỗ trợ sửa URL monitor. Chuẩn bị cách chuyển target từ lỗi sang 200 mà giữ nguyên monitor để demo recovery. Điền link video dự phòng: [ĐIỀN LINK]. Link mã nguồn: https://github.com/TranHuuQuyet/CloudPulse. Phân vai demo: [ĐIỀN].')

heading('Phụ lục C. Danh mục ảnh và bảng',1)
para('Danh mục dưới đây là danh sách caption hiện có. Những vị trí chờ ảnh AWS chưa được tính là hình hoàn chỉnh; chỉ cấp số/chèn hình chính thức khi nhận đủ chứng cứ.')
for caption in FIGURES: doc.add_paragraph(caption)
for caption in TABLES: doc.add_paragraph(caption)
note('Kiểm tra trước khi nộp', 'Điền bìa/thành viên, bổ sung các khung E01/E03-RESOLVED/E04/E05/E06/E07, xác nhận số liệu FinOps, cập nhật mục lục trong Word và kiểm tra ngắt trang. Khi không còn dùng hướng dẫn biên tập, xóa các khung vàng hướng dẫn; giữ nguyên ghi nhận lỗi/giới hạn chưa được giải quyết.')

doc.core_properties.title='CloudPulse — Báo cáo đồ án TV5 tuần 3'
doc.core_properties.subject='Thiết kế kiến trúc, tích hợp API, kiểm thử và bằng chứng'
doc.core_properties.author='Nhóm CloudPulse — TV5'
doc.core_properties.keywords='CloudPulse, TV5, AWS, W3-05'
target=OUT/'CloudPulse_BaoCao_TV5_Tuan3.docx'
doc.save(target)
check=Document(target)
assert len(check.inline_shapes)==len(FIGURES)
assert len(check.tables)>20
assert all(any(p.text.startswith(f'Chương {n}.') for p in check.paragraphs) for n in range(1,7))
validation={
 'output':str(target.relative_to(ROOT)), 'paragraphs':len(check.paragraphs),
 'tables':len(check.tables),'embedded_images':len(check.inline_shapes),
 'chapters':6,'source_test_run':DATA['executedAt'],
 'validation':'DOCX reopened successfully; chapter headings, tables and embedded images checked.',
 'rendering':'Microsoft Word / LibreOffice not installed; pagination has not been visually rendered. Update TOC and review page breaks in Word.',
}
(OUT/'validation.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(validation,ensure_ascii=False))
