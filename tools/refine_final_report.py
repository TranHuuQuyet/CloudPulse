"""Editorial pass: focus the final report on the implemented system."""
import json
import re
import sys
from pathlib import Path
if '--libs' in sys.argv:
    sys.path.insert(0, sys.argv[sys.argv.index('--libs') + 1])
from docx import Document
from docx.oxml.ns import qn


def refine(root):
    path = root/'docs/report/CloudPulse_BaoCao_CuoiCung.docx'
    doc = Document(path)
    body = doc.element.body

    def text(el):
        return ''.join(el.itertext()) if False else ''.join(t.text or '' for t in el.iter(qn('w:t')))

    def remove_section(start, end):
        active = False
        for el in list(body):
            value = text(el)
            if el.tag == qn('w:p') and value.startswith(start):
                active = True
            if active and el.tag == qn('w:p') and value.startswith(end):
                break
            if active:
                body.remove(el)

    # Remove internal coordination material and repeated task lists.
    remove_section('Hướng dẫn hoàn thiện và phạm vi tài liệu', 'Mục lục')
    remove_section('2.6.', 'Chương 3.')
    remove_section('5.3.', '5.4.')
    remove_section('4.2.', '4.3.')
    for el in list(body):
        value = text(el)
        if el.tag == qn('w:tbl') and any(k in value for k in ['Ghi chú hoàn thiện Chương 3', 'Giai đoạnCông việcBằng chứng / trạng thái']):
            body.remove(el)
        if el.tag == qn('w:p') and value.startswith('Bảng 4.2. Quá trình thực hiện'):
            body.remove(el)

    for table in doc.tables:
        if table.cell(0, 0).text == 'Vai trò':
            for row in table.rows:
                for idx in (3, 1):
                    row._tr.remove(row.cells[idx]._tc)
            for idx in (3, 1):
                table._tbl.tblGrid.remove(table._tbl.tblGrid[idx])
            table.cell(0, 0).text = 'Thành viên'
            table.cell(0, 1).text = 'Phạm vi thực hiện'
            for i, row in enumerate(table.rows[1:], 1):
                row.cells[0].text = f'[Họ tên / MSSV {i}]'

    # Rewrite the mock-stage section as a description of the overall method.
    remove_section('1.5.', 'Chương 2.')

    def insert_before(anchor, entries):
        for p in doc.paragraphs:
            if p.style.name.startswith('Heading') and p.text.startswith(anchor):
                for style, content in entries:
                    new = p.insert_paragraph_before(content, style)
                    if style == 'Normal':
                        new.paragraph_format.keep_with_next = False
                return
        raise ValueError('Missing section: '+anchor)

    insert_before('Chương 2.', [
        ('Heading 2', '1.5. Phương pháp thực hiện'),
        ('Normal', 'Dự án được thực hiện theo chuỗi: xác định nhu cầu giám sát → đặc tả yêu cầu và tiêu chí nghiệm thu → thiết kế kiến trúc và dữ liệu → phát triển backend/frontend → tích hợp AWS → kiểm thử và đánh giá chi phí. Dữ liệu giả lập hỗ trợ phát triển giao diện trước khi API sẵn sàng; kết quả nghiệm thu được tách riêng theo dữ liệu AWS thật, response điều khiển và dữ liệu mock.'),
        ('Normal', 'Báo cáo trình bày quan hệ giữa yêu cầu, quyết định thiết kế, cách triển khai và bằng chứng kiểm thử. Các chỉ tiêu chưa có phép đo hoặc ảnh vận hành được giữ ở trạng thái chờ xác nhận; thông tin cấu hình trong mã nguồn được phân biệt với cấu hình thực tế của deployment.'),
    ])
    insert_before('Chương 4.', [
        ('Heading 2', '3.7. Hai luồng kiểm tra và trách nhiệm cập nhật trạng thái'),
        ('Normal', 'Luồng định kỳ bắt đầu từ EventBridge, gọi HealthChecker để kiểm tra endpoint và cập nhật state machine, Incident cùng cảnh báo. Luồng Check Now bắt đầu từ thao tác người dùng, đi qua API Gateway tới Lambda API để đo và ghi kết quả ngay. Trong mã nguồn hiện tại, Check Now cập nhật thông tin lượt kiểm tra nhưng không thay đổi Status và ConsecutiveFailures; việc mở/đóng Incident và cảnh báo thuộc luồng HealthChecker. Vì vậy, bấm Check Now thành công chưa đủ chứng minh sự cố đã được đóng.'),
        ('Normal', 'API giới hạn lượt kiểm tra thủ công ở 1.500 ms, dành thời gian còn lại cho lưu dữ liệu và phản hồi; HealthChecker nhận TIMEOUT_MS=5000 từ SAM. Hai giới hạn phục vụ hai luồng khác nhau. Thời gian tới response header được đo bằng Stopwatch và có đơn vị ms, còn thời gian người dùng chờ gồm thêm DNS/validation, API Gateway, truy cập DynamoDB và truyền mạng.'),
        ('Heading 2', '3.8. Tính nhất quán và giới hạn mở rộng'),
        ('Normal', 'CheckResults lưu từng lần kiểm tra, Monitors giữ snapshot mới nhất và Incidents lưu các khoảng sự cố. Ba loại dữ liệu phục vụ ba mục đích riêng: dựng biểu đồ, hiển thị trạng thái hiện tại và truy vết downtime. Khi hai lượt kiểm tra hoặc retry chạy gần nhau, cần kiểm chứng cập nhật có điều kiện và cơ chế tránh gửi trùng; việc tách bảng chưa tự bảo đảm tính nguyên tử giữa lưu trạng thái và gửi thông báo.'),
        ('Normal', 'API danh sách monitor hiện dùng Scan, phù hợp phạm vi thử nghiệm nhỏ. API lịch sử truy vấn theo monitorId và giới hạn 30 kết quả checks; biểu đồ vì vậy mô tả cửa sổ gần nhất, không đại diện toàn bộ lịch sử. Khi số monitor và dữ liệu tăng, cần phân trang, truy vấn theo khoảng thời gian và tổng hợp thống kê riêng để hạn chế đọc dữ liệu và chi phí.'),
    ])
    insert_before('4.3.', [
        ('Heading 2', '4.2. Giao tiếp API và tổ chức xử lý dữ liệu'),
        ('Normal', 'Lambda API triển khai ASP.NET Core Minimal API và nhận sự kiện HTTP API v2. GET /api/status cung cấp trạng thái tiến trình; GET /api/monitors trả danh sách; POST /api/monitors kiểm tra tên/URL và tạo monitor UNKNOWN với bộ đếm lỗi bằng 0, trả 201. GET /api/monitors/{id} đọc chi tiết; DELETE cùng đường dẫn xóa monitor và trả 204. ID không tồn tại trả 404 ở các thao tác có kiểm tra monitor.'),
        ('Normal', 'GET /api/monitors/{id}/checks trả tối đa 30 kết quả với time, responseTimeMs và statusCode. GET /api/monitors/{id}/incidents trả lịch sử sự cố của monitor. POST /api/monitors/{id}/check thực hiện lượt đo thủ công, lưu CheckResult rồi cập nhật thông tin đo gần nhất. Các nhóm endpoint tách thao tác quản lý, đọc lịch sử và thực thi kiểm tra để frontend có thể làm mới đúng dữ liệu cần thiết.'),
        ('Normal', 'Frontend lưu monitor đang chọn, tải checks và incidents song song và loại bỏ response thuộc lựa chọn đã hết hiệu lực. Sau Sync hoặc Check Now, giao diện nạp lại lịch sử để chart và bảng sự cố cùng tham chiếu một monitor. API lỗi hiển thị thông báo và cho phép thử lại; chế độ mock chỉ dùng khi không cấu hình API URL, giúp tránh nhầm dữ liệu minh họa với dữ liệu vận hành.'),
    ])

    # Keep editorial image boxes but remove assignment language throughout.
    substitutions = {
        'Phân công và từ viết tắt': 'Thành viên thực hiện và thuật ngữ',
        'Bảng phân công. Vai trò nhóm theo docs/duties.md.': 'Bảng thông tin thành viên và phạm vi thực hiện.',
        'Theo tài liệu bàn giao TV1': 'Theo tài liệu triển khai hạ tầng của dự án',
        'được TV1 ghi nhận': 'được ghi nhận trong nhật ký triển khai',
        'nhật ký TV1': 'nhật ký triển khai',
        'Nhật ký TV1': 'Nhật ký triển khai',
        'TV1 cần': 'Cần',
        'TV2 cần': 'Cần',
        'TV1/TV3': 'nhóm phát triển backend và hạ tầng',
        'TV4/TV5': 'nhóm frontend',
        'TV2/TV5': 'nhóm kiểm thử',
        'TV2/TV3/TV5': 'nhóm kiểm thử',
        'TV1 ghi nhận': 'Tài liệu triển khai ghi nhận',
        'TV1 xác nhận': 'xác nhận từ cấu hình triển khai',
        'TV5 bổ sung': 'Giao diện bổ sung',
        'theo mẫu chính thức': 'theo mẫu chính thức',
    }
    paragraphs = list(doc.paragraphs)
    for table in doc.tables:
        for row in table.rows:
            for cell in row.cells:
                paragraphs.extend(cell.paragraphs)
    for p in paragraphs:
        for run in p.runs:
            value = run.text
            for old, new in substitutions.items():
                value = value.replace(old, new)
            value = re.sub(r'\[E(\d+[A-Z-]*) — CHỜ TV\d\]', r'[CHÈN ẢNH E\1]', value)
            value = re.sub(r'\[CHỜ TV\d([^\]]*)\]', r'[BỔ SUNG\1]', value)
            value = re.sub(r'\bTV[1-5]\b', 'nhóm dự án', value)
            if value != run.text:
                run.text = value

    # Rebuild the static caption list after removing coordination tables.
    in_list = False
    for p in list(doc.paragraphs):
        if p.text == 'Phụ lục C. Danh mục ảnh và bảng':
            in_list = True
            continue
        if in_list and p.text.startswith(('Bảng hướng dẫn.', 'Bảng 4.2. Quá trình', 'Bảng phân công.')):
            p._element.getparent().remove(p._element)
    path = path.with_name('CloudPulse_BaoCao_CuoiCung_ChinhLy.docx')
    doc.save(path)
    check = Document(path)
    assert len(check.inline_shapes) == 17
    assert all(any(p.text.startswith(f'Chương {n}.') for p in check.paragraphs) for n in range(1, 7))
    assert sum(t.cell(0, 0).text == 'Thành viên' for t in check.tables) == 1
    report = root/'docs/report/validation-final.json'
    validation = json.loads((report if report.exists() else report.with_name('validation.json')).read_text(encoding='utf-8'))
    validation['output'] = str(path.relative_to(root))
    validation['rendering'] = 'DOCX structure checked; visual pagination has not been reviewed. Update TOC and review page breaks in Word.'
    validation.update(paragraphs=len(check.paragraphs), tables=len(check.tables), editorial_focus='Project architecture, implementation, data flow and evidence; one compact member table.', embedded_images=len(check.inline_shapes))
    report.write_text(json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(validation, ensure_ascii=False))


if __name__ == '__main__':
    refine(Path(__file__).resolve().parents[1])
