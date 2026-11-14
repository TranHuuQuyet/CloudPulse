# CloudPulse — Báo cáo đồ án tuần 2

Ngày cập nhật: 06/11/2026. Phạm vi TV5: W2-10, W2-11, issue #17.

Bản thảo Chương 1–2 dựa trên `requirements.md`, `acceptance-criteria.md`, `tv5-report-outline.md` và `CloudPulse_Overview1.md`. Đây là nội dung phục vụ ghép vào báo cáo nhóm; thông tin trường, giảng viên, thành viên và định dạng bìa cần điền theo mẫu chính thức. Các mục tiêu vận hành bên dưới chưa được xem là kết quả đo thực tế.

## Chương 1. Giới thiệu đề tài

### 1.1. Bối cảnh và bài toán

Website và API là điểm truy cập chính của nhiều dịch vụ số. Khi máy chủ trả HTTP 500/502, kết nối hết thời gian chờ hoặc gặp lỗi DNS/SSL, khách hàng có thể không hoàn thành giao dịch. Nếu nhóm vận hành chỉ kiểm tra thủ công hoặc chờ phản ánh, thời gian phát hiện bị kéo dài và bằng chứng về thời điểm xảy ra lỗi thường thiếu.

CloudPulse giải quyết nhu cầu kiểm tra liên tục, ghi lại lịch sử và thông báo khi dịch vụ thay đổi trạng thái. Hệ thống cần phân biệt một lần kiểm tra thất bại với một sự cố đã được xác nhận, qua đó giảm cảnh báo do lỗi mạng nhất thời. Sau khi dịch vụ khôi phục, lịch sử phải thể hiện thời gian bắt đầu, kết thúc, nguyên nhân quan sát được và tổng thời lượng gián đoạn.

### 1.2. Đối tượng sử dụng và Persona

Persona chính là lập trình viên độc lập hoặc nhân sự DevOps của nhóm nhỏ, quản lý khoảng 1–20 website/API. Người này cần biết endpoint nào gián đoạn, nhận cảnh báo qua điện thoại/email và xem lại diễn biến mà không phải cài agent lên máy chủ khách hàng. Hạn chế chính là ngân sách vận hành và thời gian quản trị hạ tầng.

Một tình huống điển hình: người vận hành thêm Payment API vào Dashboard. Khi hai lượt kiểm tra liên tiếp thất bại, hệ thống xác nhận DOWN, mở Incident và gửi cảnh báo. Khi kiểm tra thành công trở lại, hệ thống đóng Incident và thông báo thời lượng gián đoạn. Lịch sử hỗ trợ đối chiếu với log ứng dụng để điều tra; thông báo HTTP 500 là biểu hiện quan sát được, chưa đủ chứng minh nguyên nhân gốc.

Persona phụ là giảng viên hoặc hội đồng chấm đồ án. Nhóm này cần kiểm chứng kiến trúc Serverless, state machine, biện pháp SSRF, khả năng tái tạo lỗi và bằng chứng chi phí. Dịch vụ MockTarget giúp trình diễn HTTP 200, HTTP 500 và timeout theo kịch bản chủ động.

### 1.3. Mục tiêu

Mục tiêu chức năng là quản lý endpoint, kiểm tra theo lịch một phút, phân loại kết quả, cảnh báo khi chuyển trạng thái và cung cấp Dashboard trực quan. Mục tiêu kỹ thuật là triển khai bằng AWS SAM, sử dụng các dịch vụ được quản lý và hạn chế quyền truy cập tài nguyên.

Các chỉ tiêu dự kiến gồm MTTD không quá hai phút, độ trễ cảnh báo không quá năm giây sau chuyển trạng thái và chi phí thử nghiệm hướng tới $0 khi đủ điều kiện miễn phí hoặc credits. Đây là mục tiêu cần kiểm thử, phụ thuộc lịch chạy, thời gian request, tải hệ thống, nhà cung cấp thông báo và điều kiện tài khoản AWS.

### 1.4. Phạm vi MVP

MVP gồm thêm/xem/xóa endpoint; kiểm tra định kỳ; đo latency; lưu CheckResults; xác nhận DOWN sau hai FAIL liên tiếp; khôi phục sau một SUCCESS; thông báo SNS/Telegram; Check Now; MockTarget; thống kê, biểu đồ và lịch sử sự cố. Kiến trúc dự kiến sử dụng React, API Gateway, Lambda .NET 8, EventBridge Scheduler và DynamoDB.

Đồ án chưa bao gồm xác thực đa người dùng, tự sửa lỗi ứng dụng, giám sát CPU/RAM, SMS/cuộc gọi và kiểm tra đa vùng. Dashboard nội bộ cần được đặt trong môi trường truy cập phù hợp trước khi triển khai thực tế.

### 1.5. Phương pháp và kết quả tuần 2

Nhóm phân tích yêu cầu, ánh xạ sang tiêu chí nghiệm thu, phát triển từng thành phần bằng dữ liệu giả lập rồi tích hợp API. TV5 bổ sung IncidentHistory sau biểu đồ latency. Mock data tách riêng khỏi component, gồm sự cố OPEN và RESOLVED, HTTP 500/502 và timeout. OPEN tính thời lượng đến thời điểm hiện tại; RESOLVED tính đến resolvedAt. Các dữ liệu này minh họa giao diện, không phải số liệu vận hành AWS.

## Chương 2. Phân tích yêu cầu

### 2.1. Tác nhân và luồng nghiệp vụ

Người vận hành cấu hình endpoint và xem Dashboard. Scheduler kích hoạt HealthChecker; HealthChecker gửi request, lưu kết quả và cập nhật trạng thái. SNS/Telegram chuyển cảnh báo tới người nhận. API phục vụ thao tác quản lý và truy vấn dữ liệu. MockTarget đóng vai trò endpoint có thể điều khiển trong kiểm thử.

Luồng sự cố: UP → FAIL lần 1 (vẫn UP) → FAIL lần 2 (DOWN, mở OPEN và gửi cảnh báo) → FAIL tiếp theo (không gửi lặp) → SUCCESS (UP, đóng RESOLVED, gửi khôi phục). Downtime của Incident được tính từ thời điểm xác nhận DOWN đến lúc khôi phục; không đồng nhất với thời gian mất dịch vụ thực tế trước khi được phát hiện.

### 2.2. Yêu cầu chức năng và truy vết

| Mã | Nội dung và hành vi mong đợi | Bằng chứng nghiệm thu |
| --- | --- | --- |
| FR-01 | Thêm tên/URL HTTP(S), xem UP/DOWN/UNKNOWN, xóa để ngừng giám sát | AC-01: API 201, bản ghi Monitor, giao diện; kiểm tra xóa |
| FR-02 | Scheduler chạy mỗi phút; kiểm tra endpoint song song; timeout 5.000 ms | AC-03: log nhiều lượt theo lịch; thử endpoint chậm |
| FR-03 | Đo thời gian đến response header; 200–399 là SUCCESS trong giới hạn timeout; phân loại 4xx/5xx, DNS/SSL/kết nối | CheckResults và log HTTP 200/500, timeout |
| FR-04 | Hai FAIL liên tiếp mới DOWN; một SUCCESS hồi phục; tạo/đóng Incident | AC-04, AC-07: trạng thái, timestamps, downtime |
| FR-05 | Chỉ thông báo khi chuyển trạng thái; hỗ trợ SNS Email và Telegram | AC-05, AC-06, AC-07; đếm thông báo khi FAIL lặp |
| FR-06 | Check Now gọi kiểm tra ngay, hiển thị kết quả trên Dashboard | AC-08: spinner, kết quả, thời gian phản hồi |
| FR-07 | MockTarget trả 200, 500 hoặc delay 7.000 ms theo tham số | AC-09: response và log checker |
| FR-08 | Thống kê, biểu đồ latency, lịch sử có bắt đầu/kết thúc/downtime/nguyên nhân | Kiểm tra UI, dữ liệu rỗng, OPEN/RESOLVED, màn hình nhỏ |

Các trường dữ liệu hiển thị của IncidentHistory tuần 2 gồm id, endpointName, status, startedAt, resolvedAt và cause. Timestamp dùng ISO 8601; giao diện hiển thị giờ Việt Nam. Khi tích hợp, cần ánh xạ DTO backend vào cấu trúc này và thống nhất đơn vị ms/giây. Mock incident không tự thay đổi theo Check Now hay thao tác xóa monitor.

### 2.3. Yêu cầu phi chức năng

| Mã | Mục tiêu thiết kế | Cách kiểm chứng và giới hạn |
| --- | --- | --- |
| NFR-01 | MTTD ≤ 2 phút; đọc/ghi API ≤ 500 ms; Check Now ≤ 2 giây | Đo nhiều lượt, ghi region và cold/warm start; chưa kiểm chứng. Request timeout 5 giây có thể vượt mục tiêu Check Now 2 giây; nhóm cần thống nhất SLA cho đường lỗi/timeout |
| NFR-02 | Giảm báo giả; một thông báo mở/đóng trên mỗi kênh mỗi đợt | Thử FAIL→SUCCESS, FAIL→FAIL→FAIL→SUCCESS; kiểm tra đồng thời/retry. Hai FAIL liên tiếp chưa chứng minh báo giả bằng 0 trong mọi điều kiện |
| NFR-03 | Chặn SSRF và dùng IAM Least Privilege | AC-02: loopback, private IP, metadata; kiểm tra backend, DNS/redirect và policy thực tế |
| NFR-04 | Hướng tới $0 ở quy mô thử nghiệm; TTL CheckResults 7 ngày | AC-10 và hóa đơn/Budgets, điều kiện Free Tier/credits; chưa có xác nhận TV1 |
| NFR-05 | Tài nguyên định nghĩa trong AWS SAM; có thể deploy/delete | AC-11: log triển khai và kiểm tra tài nguyên còn lại sau xóa |

### 2.4. FinOps: so sánh theo giả định

Đơn vị USD, chưa gồm thuế. Ví dụ tham chiếu us-east-1, một endpoint, chạy mỗi phút trong 30 ngày: 43.200 lượt. Region của Dashboard hiện là ap-southeast-1; TV1 cần tính lại giá đúng region triển khai trước khi chốt báo cáo.

| Phương án | Cách tính minh họa | Kết luận được phép sử dụng |
| --- | --- | --- |
| CloudPulse Serverless | Số lượt Scheduler/Lambda, request và dung lượng DynamoDB, API Gateway, SNS, log, lưu trữ và truyền dữ liệu; trừ ưu đãi hợp lệ/credits nếu có | $0 là mục tiêu hoặc số tiền phải trả có điều kiện; chưa phải chi phí vận hành đã đo |
| CloudWatch Synthetics | 43.200 × $0,0012 = $51,84 tiền canary trước ưu đãi; nếu tài khoản đủ 100 lượt miễn phí thì (43.200 − 100) × $0,0012 = $51,72 | Khoảng $50/tháng cho phí chạy canary ở giả định này; chưa gồm Lambda, S3, logs và metrics liên quan |

Nguồn giá tham chiếu: https://aws.amazon.com/cloudwatch/pricing/ (truy cập 06/11/2026). CloudWatch Synthetics hỗ trợ kiểm tra hành trình trình duyệt phong phú hơn HTTP ping của MVP; bảng này không khẳng định hai phương án tương đương chức năng.

Điều kiện miễn phí/credits thay đổi theo tài khoản và chương trình; không mặc định DynamoDB On-Demand hoặc toàn bộ hệ thống đều miễn phí. Tham chiếu: https://aws.amazon.com/free/free-tier-faqs/ (truy cập 06/11/2026). TV1 cần cung cấp region, tuổi tài khoản/chương trình, cấu hình bộ nhớ và thời lượng Lambda, số endpoint, chế độ tính phí DynamoDB, kích thước item, retention logs, cách tính SNS và ảnh Billing/Cost Explorer. Bảng này là bản chuẩn bị phối hợp, chưa có xác nhận của TV1.

### 2.5. Tiêu chí nghiệm thu IncidentHistory tuần 2

1. Dữ liệu giả lập được import vào App và truyền qua props, không gọi API sự cố thật.
2. Bảng sắp xếp thời gian bắt đầu mới nhất trước; OPEN màu đỏ, RESOLVED màu xanh và có nhãn chữ.
3. Hiển thị endpoint, nguyên nhân, bắt đầu, khôi phục và số giây downtime; mẫu timeout có đúng 124s downtime.
4. OPEN cập nhật mỗi giây và hiển thị “Đang diễn ra”; RESOLVED không tiếp tục tăng.
5. Mảng rỗng hiển thị thông báo; timestamp thiếu/sai hiển thị dấu gạch; bảng cuộn ngang trên màn hình nhỏ.
6. Build và lint frontend thành công. Kiểm thử AWS, thông báo và chi phí cần bằng chứng tích hợp ở giai đoạn tiếp theo.

### 2.6. Vấn đề cần nhóm thống nhất

Cần làm rõ mục tiêu Check Now hai giây đối với timeout năm giây; định nghĩa MTTD có bao gồm thời gian request; ánh xạ DTO Incident; chính sách lưu lịch sử sau xóa monitor; và quy mô/ưu đãi để nghiệm thu chi phí. Các điểm này không ngăn hoàn thành component mock tuần 2 nhưng phải được xử lý trước nghiệm thu hệ thống thật.
