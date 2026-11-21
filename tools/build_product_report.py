"""Build the product-focused CloudPulse report from repository evidence."""
from pathlib import Path
import sys
import io
import json

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'.report-libs'))
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from PIL import Image

OUT = ROOT/'docs/report'
ASSETS = OUT/'assets'
doc = Document()
sec = doc.sections[0]
sec.page_width, sec.page_height = Cm(21), Cm(29.7)
sec.left_margin, sec.right_margin = Cm(3), Cm(2)
sec.top_margin, sec.bottom_margin = Cm(2), Cm(2)
for name in ['Normal', 'Heading 1', 'Heading 2', 'Caption']:
    style = doc.styles[name]
    style.font.name = 'Times New Roman'
    style.element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'), 'Times New Roman')
doc.styles['Normal'].font.size = Pt(13)
doc.styles['Normal'].paragraph_format.line_spacing = 1.25
doc.styles['Normal'].paragraph_format.space_after = Pt(6)
doc.styles['Heading 1'].paragraph_format.page_break_before = True
for name, size in [('Heading 1',17),('Heading 2',14)]:
    doc.styles[name].font.size = Pt(size)
    doc.styles[name].font.color.rgb = RGBColor.from_string('17365D')
doc.styles['Caption'].font.size = Pt(11)
figures = []

def p(s):
    result = doc.add_paragraph(s)
    result.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    return result

def h(s, level=2):
    doc.add_heading(s,level)

def table(headers, rows):
    t = doc.add_table(rows=1, cols=len(headers))
    t.style='Table Grid'
    for c, v in zip(t.rows[0].cells,headers):
        c.text=v
        for r in c.paragraphs[0].runs: r.bold=True
    for row in rows:
        for c,v in zip(t.add_row().cells,row): c.text=v
    for row in t.rows:
        for cell in row.cells:
            for par in cell.paragraphs:
                for run in par.runs: run.font.size=Pt(11)
    doc.add_paragraph()

def placeholder(s):
    par=doc.add_paragraph('[BỔ SUNG ẢNH: '+s+']')
    for run in par.runs:
        run.italic=True
        run.font.color.rgb=RGBColor.from_string('885500')

def pic(filename, caption, description='', crop_bottom=0):
    path=ASSETS/filename
    if not path.exists():
        placeholder(caption)
        return
    data=io.BytesIO()
    with Image.open(path) as im:
        w, height=im.size
        im.convert('RGB').save(data,format='PNG')
    data.seek(0)
    width=min(16, 15*w/height)
    par=doc.add_paragraph()
    par.alignment=WD_ALIGN_PARAGRAPH.CENTER
    par.paragraph_format.keep_with_next=True
    shape=par.add_run().add_picture(data,width=Cm(width))
    if crop_bottom:
        rect=OxmlElement('a:srcRect'); rect.set('b',str(int(crop_bottom*100000)))
        shape._inline.graphic.graphicData.pic.blipFill.insert(1,rect)
        shape.height=int(shape.height*(1-crop_bottom))
    doc.add_paragraph(caption,'Caption').alignment=WD_ALIGN_PARAGRAPH.CENTER
    if description: p(description)
    figures.append(caption)

def field(par,instruction,cached):
    for tag,attrs,value in [('w:fldChar',{'w:fldCharType':'begin'},None),('w:instrText',{},instruction),('w:fldChar',{'w:fldCharType':'separate'},None),('w:t',{},cached),('w:fldChar',{'w:fldCharType':'end'},None)]:
        el=OxmlElement(tag)
        for k,v in attrs.items(): el.set(qn(k),v)
        el.text=value
        par.add_run()._r.append(el)

for text,size in [('[TÊN TRƯỜNG / KHOA]',16),('BÁO CÁO ĐỒ ÁN',22),('CLOUDPULSE',28),('HỆ THỐNG GIÁM SÁT WEBSITE VÀ API\nTRÊN NỀN TẢNG AWS SERVERLESS',17)]:
    par=doc.add_paragraph(); par.alignment=WD_ALIGN_PARAGRAPH.CENTER
    run=par.add_run(text); run.bold=True; run.font.size=Pt(size)
p('Môn học: Điện toán đám mây')
p('Giảng viên: [ĐIỀN HỌ TÊN]')
p('Nhóm / Lớp: [ĐIỀN THÔNG TIN]')
p('Tháng 11 năm 2026')
h('Thông tin thành viên',1)
table(['Họ tên / MSSV','Nội dung thực hiện'],[
    ['[Thành viên 1]','Hạ tầng AWS, triển khai và quản lý chi phí'],
    ['[Thành viên 2]','HealthChecker, xử lý trạng thái và cảnh báo'],
    ['[Thành viên 3]','API và dịch vụ kiểm thử MockTarget'],
    ['[Thành viên 4]','Giao diện và quản lý monitor'],
    ['[Thành viên 5]','Biểu đồ, lịch sử sự cố và tổng hợp báo cáo'],
])
h('Mục lục',1)
field(doc.add_paragraph(),' TOC \\o "1-2" \\h \\z \\u ','Cập nhật mục lục trong Word bằng Update Field.')

h('Chương 1. Giới thiệu đề tài',1)
h('1.1. Bài toán thực tế')
p('Website và API có thể bị gián đoạn do lỗi máy chủ, lỗi kết nối hoặc phản hồi quá chậm. Việc kiểm tra thủ công khiến người vận hành phát hiện sự cố muộn, khó xác định thời điểm bắt đầu và khó tổng hợp thời gian gián đoạn. Với nhóm phát triển nhỏ, giải pháp giám sát cần dễ triển khai, có lịch sử rõ ràng và hạn chế công việc quản trị máy chủ.')
p('CloudPulse giải quyết bài toán bằng cách tự động gửi yêu cầu HTTP tới các endpoint, ghi nhận kết quả và thông báo khi trạng thái thay đổi. Dashboard tập trung danh sách dịch vụ, độ trễ và lịch sử sự cố, giúp người vận hành quan sát tình trạng hệ thống từ một giao diện.')
h('1.2. Mục tiêu và đối tượng sử dụng')
p('Mục tiêu chính là phát hiện endpoint không phản hồi đúng, giảm cảnh báo do lỗi nhất thời, thông báo kịp thời và lưu lịch sử để đối chiếu. Đối tượng sử dụng là lập trình viên, nhóm phát triển nhỏ và người phụ trách vận hành website/API. Giải pháp sử dụng AWS Serverless để tách giao diện, xử lý API và tiến trình giám sát định kỳ.')
h('1.3. Phạm vi sản phẩm')
p('Sản phẩm gồm quản lý monitor, kiểm tra theo lịch một phút, kiểm tra tức thời, biểu đồ độ trễ, quản lý sự cố và cảnh báo qua email/Telegram. MockTarget hỗ trợ tạo phản hồi thành công, lỗi HTTP và phản hồi chậm phục vụ kiểm thử. Phạm vi hiện tại tập trung giám sát từ bên ngoài qua HTTP(S); chưa bao gồm CPU/RAM, tự sửa lỗi hay giám sát đa vùng.')

h('Chương 2. Chức năng và yêu cầu hệ thống',1)
h('2.1. Các chức năng chính')
table(['Chức năng','Mục đích và hành vi'],[
    ['Quản lý monitor','Thêm tên và URL, xem trạng thái, tìm kiếm/lọc và xóa endpoint.'],
    ['Giám sát tự động','Kiểm tra định kỳ, ghi mã HTTP, độ trễ và kết quả thành công/thất bại.'],
    ['Xác nhận sự cố','Hai lần kiểm tra thất bại liên tiếp mới chuyển DOWN.'],
    ['Phục hồi','Khi kiểm tra thành công sau DOWN, chuyển UP và đóng Incident.'],
    ['Cảnh báo','Gửi thông báo DOWN và RECOVERED qua SNS Email và Telegram.'],
    ['Check Now','Thực hiện một lượt đo ngay theo yêu cầu người dùng.'],
    ['Biểu đồ và lịch sử','Xem latency, MIN/AVG/MAX và các sự cố OPEN/RESOLVED.'],
])
h('2.2. Luồng sử dụng')
p('Người dùng mở Dashboard, thêm endpoint hợp lệ và theo dõi trạng thái. Tiến trình định kỳ tiếp tục chạy dù người dùng đóng trang. Khi cần kiểm tra tức thời, người dùng bấm Check Now. Khi có sự cố, thông báo cung cấp tên monitor, URL, nguyên nhân quan sát được và thời gian; người dùng mở lịch sử để xem thời lượng gián đoạn và trạng thái phục hồi.')
h('2.3. Yêu cầu chất lượng')
p('Hệ thống cần phân biệt lỗi đơn lẻ với sự cố được xác nhận, hiển thị rõ nguồn dữ liệu và thống nhất đơn vị đo. URL đầu vào được kiểm tra để hạn chế SSRF. Hạ tầng được khai báo bằng AWS SAM, quyền truy cập phân theo vai trò và lịch sử kiểm tra có TTL để kiểm soát dung lượng. Các chỉ tiêu thời gian phản hồi và cảnh báo được đánh giá bằng phép đo thực tế, không suy ra chỉ từ cấu hình.')

h('Chương 3. Kiến trúc và thiết kế',1)
h('3.1. Kiến trúc tổng thể')
p('CloudPulse gồm ba luồng chính: trình duyệt truy cập frontend qua tên miền và Cloudflare; frontend gọi API Gateway/Lambda để quản lý và đọc dữ liệu; EventBridge kích hoạt HealthChecker để kiểm tra các endpoint. DynamoDB lưu trạng thái và lịch sử. SNS và Telegram chuyển thông báo tới người vận hành, trong khi CloudWatch lưu log thực thi.')
pic('architecture.png','Hình 3.1. Kiến trúc xử lý cốt lõi của CloudPulse.','Sơ đồ thể hiện backend và luồng giám sát; frontend được phân phối qua S3 và Cloudflare như mô tả tại mục 4.2.')
table(['Thành phần','Vai trò'],[
    ['React / Vite / Recharts','Dashboard, thao tác monitor, biểu đồ và lịch sử.'],
    ['S3 / Cloudflare','Lưu frontend tĩnh và phân phối qua tên miền clpulse.cloud.'],
    ['API Gateway / Lambda API','Cung cấp giao diện HTTP cho frontend.'],
    ['EventBridge / HealthChecker','Kích hoạt định kỳ và thực thi kiểm tra.'],
    ['DynamoDB','Lưu Monitors, CheckResults và Incidents.'],
    ['SNS / Telegram','Chuyển thông báo thay đổi trạng thái.'],
    ['CloudWatch / IAM / SAM','Quan sát thực thi, quản lý quyền và triển khai hạ tầng.'],
])
h('3.2. Thiết kế dữ liệu')
table(['Bảng','Khóa','Dữ liệu chính'],[
    ['Monitors','monitorId','Tên, URL, trạng thái, bộ đếm lỗi và lượt đo gần nhất.'],
    ['CheckResults','monitorId + checkedAt','Mã HTTP, latency, isSuccess, lỗi và expireAt.'],
    ['Incidents','monitorId + incidentId','OPEN/RESOLVED, thời điểm mở/đóng, nguyên nhân và downtimeSeconds.'],
])
p('Monitors phục vụ hiển thị trạng thái hiện tại; CheckResults phục vụ biểu đồ từng lượt đo; Incidents mô tả khoảng gián đoạn. Timestamp nghiệp vụ được lưu theo UTC; latency dùng mili giây và downtime dùng giây. CheckResults cấu hình TTL để tự dọn dữ liệu hết hạn; thiết kế sử dụng expireAt dạng epoch giây và thời hạn bảy ngày.')
h('3.3. Cơ chế xác nhận DOWN và phục hồi')
table(['Trạng thái / kết quả','Xử lý'],[
    ['UP hoặc UNKNOWN + FAIL lần đầu','Tăng bộ đếm lỗi, chưa mở sự cố.'],
    ['FAIL lần hai liên tiếp','Chuyển DOWN, mở Incident và gửi cảnh báo.'],
    ['DOWN + FAIL tiếp','Tiếp tục lưu kết quả; không yêu cầu gửi lại cảnh báo DOWN theo cùng trạng thái.'],
    ['DOWN + SUCCESS','Chuyển UP, reset bộ đếm, đóng Incident và gửi RECOVERED.'],
])
p('Downtime được tính từ thời điểm xác nhận DOWN tới thời điểm phục hồi. Cách tính này mô tả thời lượng sự cố được hệ thống ghi nhận; thời điểm endpoint bắt đầu lỗi có thể sớm hơn do khoảng cách giữa các lượt kiểm tra. Cảnh báo theo chuyển trạng thái giúp giảm thông báo lặp khi endpoint tiếp tục DOWN.')
h('3.4. Check Now và an toàn URL')
p('Kiểm tra định kỳ đảm nhiệm xử lý trạng thái, Incident và cảnh báo. Check Now phục vụ một lượt đo nhanh qua API, lưu kết quả và cập nhật thông tin kiểm tra gần nhất; theo mã nguồn hiện tại, đường này không trực tiếp thay đổi Status và ConsecutiveFailures. Hai luồng chia sẻ dữ liệu lịch sử nhưng phục vụ mục đích khác nhau.')
p('Backend xác thực URL HTTP(S), chặn loopback, IP nội bộ và địa chỉ metadata trước khi thực hiện yêu cầu. Đường kiểm tra thủ công cấu hình không tự đi theo redirect. IAM cấp quyền cho Lambda truy cập các bảng/topic cần thiết; việc giới hạn action và tài nguyên được đối chiếu với policy khi triển khai.')

h('Chương 4. Triển khai sản phẩm',1)
h('4.1. Triển khai backend trên AWS')
p('Backend sử dụng .NET 8 và AWS SAM tại vùng ap-southeast-1. Stack cloudpulse-app tạo API Gateway, ba hàm Lambda API/HealthChecker/MockTarget, các bảng DynamoDB và SNS cùng tài nguyên hỗ trợ. Outputs công bố API endpoint và MockTarget endpoint để frontend tích hợp. Ảnh stack cho thấy trạng thái UPDATE_COMPLETE.')
pic('e01-cloudformation-outputs.jpg','Hình 4.1. Outputs của stack CloudPulse.')
pic('e01-cloudformation-resources.jpg','Hình 4.2. Các tài nguyên được triển khai bằng CloudFormation.')
h('4.2. Hosting frontend và tên miền')
p('Frontend được build thành tài nguyên tĩnh và lưu tại bucket cloudpulse-dashboard-tv1. Người dùng truy cập https://clpulse.cloud qua Cloudflare; Cloud Connector định tuyến tới origin S3. Trình duyệt gọi API Gateway riêng để đọc và ghi dữ liệu. Cấu trúc này tách phân phối giao diện khỏi tiến trình giám sát, cho phép cập nhật frontend độc lập với HealthChecker.')
placeholder('S3 Static Website Hosting và Cloudflare DNS / SSL / Cloud Connector')
h('4.3. API và tích hợp giao diện')
table(['API','Chức năng'],[
    ['GET /api/monitors','Đọc danh sách monitor.'],
    ['POST /api/monitors','Kiểm tra đầu vào và tạo monitor.'],
    ['GET, DELETE /api/monitors/{id}','Xem chi tiết hoặc xóa monitor.'],
    ['GET /api/monitors/{id}/checks','Đọc dữ liệu độ trễ gần nhất.'],
    ['GET /api/monitors/{id}/incidents','Đọc lịch sử sự cố.'],
    ['POST /api/monitors/{id}/check','Thực hiện Check Now.'],
])
p('Khi chọn monitor, frontend tải checks và incidents song song. Khi đổi lựa chọn, response của lượt tải cũ được bỏ qua để tránh hiển thị nhầm dữ liệu. Sau Sync hoặc Check Now, giao diện làm mới dữ liệu của monitor đang chọn. Biểu đồ tính MIN/AVG/MAX trên cửa sổ kết quả đang hiển thị.')
h('4.4. Lịch chạy, lưu trữ và quan sát')
p('EventBridge kích hoạt HealthChecker theo lịch rate(1 minute). Mỗi chu kỳ kiểm tra các endpoint và ghi kết quả vào DynamoDB. CloudWatch lưu thời điểm bắt đầu/kết thúc và thông tin thực thi, giúp đối chiếu hoạt động định kỳ. Ảnh log thể hiện các chu kỳ liên tiếp; ảnh DynamoDB thể hiện CheckResults bật TTL và các Incident OPEN/RESOLVED.')
pic('e04-cloudwatch-logs-execution.jpg','Hình 4.3. Các chu kỳ thực thi HealthChecker trong CloudWatch.')
pic('e06-dynamodb-checkresults-ttl-on.jpg','Hình 4.4. Bảng CheckResults bật TTL.')

h('Chương 5. Kết quả sản phẩm và đánh giá',1)
h('5.1. Dashboard quản lý endpoint')
p('Dashboard cung cấp danh sách endpoint, trạng thái UP/DOWN, độ trễ gần nhất, bộ lọc và Check Now. Ảnh sản phẩm ghi nhận sáu monitor, trong đó năm UP và một DOWN. Nhờ hiển thị tập trung, người dùng có thể xác định endpoint cần chú ý mà không truy cập từng dịch vụ. Các chỉ số trên ảnh là snapshot tại thời điểm chụp.')
pic('e02-live-dashboard-full.png','Hình 5.1. Dashboard quản lý các endpoint CloudPulse.','Ảnh minh họa danh sách và trạng thái monitor; biểu đồ chi tiết được trình bày riêng bên dưới.')
h('5.2. Biểu đồ độ trễ và lịch sử sự cố')
p('Biểu đồ độ trễ giúp quan sát thay đổi thời gian phản hồi giữa các lượt kiểm tra; tooltip cung cấp thời gian, số ms và mã HTTP. MIN/AVG/MAX tóm tắt tập dữ liệu đang hiển thị. Lịch sử sự cố thể hiện trạng thái OPEN hoặc RESOLVED, thời điểm bắt đầu/kết thúc và downtime để người vận hành xem lại diễn biến.')
placeholder('Biểu đồ bản sản phẩm có dữ liệu, tooltip và MIN/AVG/MAX')
pic('e03-dynamodb-incidents-open-resolved.jpg','Hình 5.2. Dữ liệu Incident OPEN và RESOLVED trong DynamoDB.','Các bản ghi cho thấy hệ thống lưu trạng thái sự cố và downtimeSeconds. Đây là các sự cố khác nhau; hình không mô tả chuỗi trước/sau của một incident duy nhất.')
placeholder('Lịch sử sự cố trên Dashboard, hiển thị RESOLVED và downtime')
h('5.3. Cảnh báo đa kênh')
p('Email và Telegram đưa thông tin sự cố tới người vận hành ngoài Dashboard. Cảnh báo DOWN nêu endpoint và nguyên nhân kiểm tra thất bại; cảnh báo RECOVERED thông báo dịch vụ trở lại UP cùng tổng downtime. Các ảnh dưới minh họa thông báo thực tế đã thu thập của sản phẩm.')
pic('e05-a-telegram-alert.jpg','Hình 5.3. Telegram cảnh báo monitor DOWN.')
pic('e05-c-telegram-recovery-alert.jpg','Hình 5.4. Telegram thông báo dịch vụ đã phục hồi.','Ảnh RECOVERED thuộc các monitor UTH-course và Dashboard-Course; ảnh DOWN phía trên thuộc Mock 500. Hai hình minh họa hai loại thông báo, không ghép thành một sự cố duy nhất.')
pic('e05-b-email-sns-alert.jpg','Hình 5.5. Email SNS thông báo endpoint DOWN.',crop_bottom=.55)
placeholder('Email thông báo RECOVERED')
h('5.4. Chi phí vận hành thử nghiệm')
p('CloudPulse sử dụng dịch vụ tính phí theo mức sử dụng, do đó cần theo dõi cả chi phí dịch vụ và ưu đãi tài khoản. Ảnh phân bổ tháng 11/2026 hiển thị DynamoDB khoảng 0,06 USD và API Gateway khoảng 0,04 USD, tổng các khoản hiển thị khoảng 0,10 USD. Đây là số liệu tài khoản tại thời điểm chụp; chưa tách chi phí ngoài dự án nếu có.')
pic('e07-cost-breakdown-10cents.png','Hình 5.6. Phân bổ chi phí theo dịch vụ trong tháng 11/2026.')
p('Ảnh Bills thu trước đó hiển thị estimated grand total 0,00 USD trong kỳ đang chạy; ảnh credits hiển thị số sử dụng ước tính 0,03 USD. Các ảnh khác thời điểm và khác loại tổng hợp nên không dùng để cộng trừ trực tiếp. Khi trình bày chi phí cần phân biệt usage trước ưu đãi, credits áp dụng và số tiền phải thanh toán.')
h('5.5. Tổng hợp kết quả theo chức năng')
table(['Nội dung','Kết quả được thể hiện'],[
    ['Triển khai serverless','Stack và các tài nguyên CloudFormation đã có ảnh xác nhận.'],
    ['Quản lý và theo dõi monitor','Dashboard hiển thị endpoint UP/DOWN và lượt đo gần nhất.'],
    ['Thực thi định kỳ','CloudWatch ghi nhận các chu kỳ HealthChecker liên tiếp.'],
    ['Lịch sử sự cố','DynamoDB lưu các Incident OPEN/RESOLVED và downtime.'],
    ['Cảnh báo','Đã có ảnh Telegram DOWN/RECOVERED và email DOWN.'],
    ['Kiểm soát dữ liệu và chi phí','CheckResults bật TTL; có ảnh chi phí theo dịch vụ.'],
])
p('Các kết quả trên tập trung vào chức năng và bằng chứng sản phẩm. Chúng không thay thế phép đo nhiều mẫu về thời gian phát hiện, độ trễ cảnh báo hoặc khả năng chịu tải. Những chỉ tiêu này là cơ sở đánh giá sâu hơn khi mở rộng quy mô sử dụng.')

h('Chương 6. Kết luận và hướng phát triển',1)
h('6.1. Giá trị đạt được')
p('CloudPulse xây dựng một quy trình giám sát website/API gồm kiểm tra định kỳ, xác nhận sự cố, lưu lịch sử và thông báo phục hồi. Dashboard tập trung dữ liệu để người dùng quan sát trạng thái; kiến trúc serverless giảm nhu cầu quản trị máy chủ, còn AWS SAM hỗ trợ triển khai hạ tầng có thể tái tạo. Sản phẩm kết nối được lớp giao diện, API, lưu trữ và cảnh báo trong cùng một luồng vận hành.')
h('6.2. Hướng phát triển')
p('Các hướng mở rộng gồm xác thực và phân quyền người dùng, thống kê uptime theo thời gian, kiểm tra từ nhiều vùng và cấu hình chu kỳ theo monitor. Về vận hành, có thể bổ sung tổng hợp số liệu, phân trang lịch sử và bảo đảm xử lý idempotent khi có retry hoặc kiểm tra đồng thời. Việc mở rộng cần đi cùng đo hiệu năng và theo dõi chi phí thực tế.')

h('Tài liệu tham khảo',1)
for s in [
    '[1] Nhóm CloudPulse. Mã nguồn dự án, template.yaml và tài liệu yêu cầu, use case, tiêu chí nghiệm thu trong repository.',
    '[2] Nhóm CloudPulse. Tài liệu triển khai AWS, S3, Cloudflare và các ảnh sản phẩm tại docs/report/assets.',
    '[3] Nhóm CloudPulse. Kết quả tích hợp và kiểm thử lưu tại docs/evidence/w3-05.',
    '[4] Amazon Web Services. Tài liệu dịch vụ AWS SAM, Lambda, DynamoDB, EventBridge, SNS và CloudWatch: docs.aws.amazon.com.',
]: p(s)
h('Phụ lục. Kịch bản trình diễn sản phẩm',1)
table(['Bước','Nội dung'],[
    ['1','Mở Dashboard và giới thiệu danh sách monitor.'],
    ['2','Chọn endpoint, xem biểu đồ và thực hiện Check Now.'],
    ['3','Dùng target thử nghiệm để tạo phản hồi lỗi, quan sát DOWN.'],
    ['4','Xem Incident OPEN và cảnh báo Email/Telegram.'],
    ['5','Phục hồi cùng target, quan sát UP/RESOLVED và thông báo RECOVERED.'],
    ['6','Đối chiếu downtime, dữ liệu lưu và chi phí vận hành.'],
])
settings=OxmlElement('w:updateFields');settings.set(qn('w:val'),'true');doc.settings.element.append(settings)
footer=sec.footer.paragraphs[0];footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
footer.add_run('CloudPulse · ');field(footer,' PAGE ','1')
doc.core_properties.title='CloudPulse — Báo cáo sản phẩm'
doc.core_properties.author='Nhóm CloudPulse'
target=OUT/'CloudPulse_BaoCao_SanPham.docx'
doc.save(target)
check=Document(target)
assert len(check.inline_shapes)==len(figures)
assert all(any(par.text.startswith(f'Chương {i}.') for par in check.paragraphs) for i in range(1,7))
assert not any(s in '\n'.join(par.text for par in check.paragraphs) for s in ['Internal Server Error','CORS POST:','race-B','KNOWN_FAILURE'])
validation={'output':str(target.relative_to(ROOT)),'chapters':6,'embedded_images':len(check.inline_shapes),'tables':len(check.tables),'paragraphs':len(check.paragraphs),'focus':'Product purpose, functions, architecture, implementation and observed results.','validation':'DOCX reopened and six chapters/images checked.','rendering':'Update TOC and review pagination in Microsoft Word.'}
(OUT/'validation-product.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(validation,ensure_ascii=False))
