"""Add new evidence to the user's edited Word document without rebuilding it."""
from pathlib import Path
import sys, io, re
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'.report-libs'))
from docx import Document
from docx.shared import Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from PIL import Image

root=Path(__file__).resolve().parents[1]
source=Path('C:/Users/Cam Tu/Downloads/CloudPulse_BaoCao_SanPham.docx')
doc=Document(source)
original_images=len(doc.inline_shapes)

def anchor(prefix):
    return next(p for p in doc.paragraphs if p.text.startswith(prefix))

def add_before(prefix, images):
    target=anchor(prefix)
    for filename,caption in images:
        stream=io.BytesIO()
        with Image.open(root/'docs/report/assets'/filename) as im:
            width=min(16,14*im.width/im.height)
            im.convert('RGB').save(stream,format='PNG')
        stream.seek(0)
        p=target.insert_paragraph_before()
        p.alignment=WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.keep_with_next=True
        shape=p.add_run().add_picture(stream,width=Cm(width))
        if 'email-sns-recovery' in filename:
            from docx.oxml import OxmlElement
            rect=OxmlElement('a:srcRect');rect.set('b','55000')
            shape._inline.graphic.graphicData.pic.blipFill.insert(1,rect)
            shape.height=int(shape.height*.45)
        c=target.insert_paragraph_before(caption,'Caption')
        c.alignment=WD_ALIGN_PARAGRAPH.CENTER

add_before('4.2.', [('e01-lambda-runtime-dotnet8.png','Hình 4.0. Runtime .NET 8 của Lambda API.')])
add_before('4.3.',[
 ('e02-s3-static-website-hosting-enabled.png','Hình 4.0. Cấu hình Static Website Hosting trên S3.'),
 ('e02-cloudflare-dns-cname.png','Hình 4.0. Cấu hình DNS tên miền clpulse.cloud.'),
 ('e02-cloudflare-ssl-flexible.png','Hình 4.0. Chế độ SSL/TLS Flexible tại Cloudflare.'),
 ('e02-cloudflare-cloud-connector.png','Hình 4.0. Cloud Connector định tuyến tới S3.'),
])
add_before('4.4.',[
 ('e01-api-gateway-routes.png','Hình 4.0. Các route của API Gateway.'),
 ('e01-api-gateway-cors.png','Hình 4.0. Cấu hình CORS của HTTP API.'),
])
add_before('Chương 5.',[
 ('e04-eventbridge-scheduled-rule-enabled.png','Hình 4.0. Rule EventBridge của HealthChecker đang bật.'),
 ('e06-dynamodb-ttl-details-expireAt.png','Hình 4.0. TTL bật trên thuộc tính expireAt.'),
 ('e06-dynamodb-item-ttl-expireAt.png','Hình 4.0. Bản ghi kiểm tra với expireAt và dữ liệu phản hồi.'),
 ('e06-iam-policy-json-action-resource.jpg','Hình 4.0. Action và Resource của policy HealthChecker trong trình soạn thảo IAM.'),
 ('e05-sns-subscription-confirmed.png','Hình 4.0. SNS email subscription có trạng thái Confirmed.'),
])
add_before('5.4.', [('e05-d-email-sns-recovery-alert.jpg','Hình 5.0. Email SNS thông báo endpoint phục hồi; downtime 115 giây.')])
for p in list(doc.paragraphs):
    if p.text.startswith('[BỔ SUNG ẢNH:') and any(s in p.text for s in ['S3 Static','Email thông báo RECOVERED']):
        p._element.getparent().remove(p._element)
counter={}
for p in doc.paragraphs:
    m=re.match(r'Hình (\d+)\.\d+\.',p.text)
    if m:
        chapter=m.group(1);counter[chapter]=counter.get(chapter,0)+1
        p.text=re.sub(r'^Hình \d+\.\d+\.',f'Hình {chapter}.{counter[chapter]}.',p.text)
output=root/'docs/report/CloudPulse_BaoCao_SanPham_TuFileCuaBan.docx'
doc.save(output)
check=Document(output)
assert len(check.inline_shapes)==original_images+13
print(f'Saved {output}; {len(check.inline_shapes)} images; original text and tables preserved.')
