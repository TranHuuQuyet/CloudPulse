# Đặc Tả Yêu Cầu Hệ Thống (Requirements Specification)

Tài liệu này xác định đầy đủ các yêu cầu chức năng (Functional Requirements - FR), yêu cầu phi chức năng (Non-Functional Requirements - NFR) và phạm vi ngoài dự án (Out of Scope) cho hệ thống **CloudPulse**.

---

## 1. Đối tượng Sử dụng (Target Users)

* **Primary Persona — SRE / DevOps / Indie Developer**:
  * Người chịu trách nhiệm vận hành từ 1 đến 20 website/API dịch vụ.
  * Mong muốn: Được cảnh báo ngay lập tức vào điện thoại/email khi dịch vụ gặp sự cố, chi phí vận hành tiệm cận \$0, không cần cài đặt thêm phần mềm giám sát lên máy chủ đích.
* **Secondary Persona — Giảng viên / Hội đồng Chấm thi**:
  * Đánh giá kiến trúc Serverless, khả năng vận hành tự động, tính đúng đắn của State Machine, cơ chế bảo mật (chống SSRF) và khả năng demo kịch bản thực tế.

---

## 2. Yêu cầu Chức năng (Functional Requirements - MVP)

Hệ thống CloudPulse phiên bản tối thiểu (MVP) bắt buộc phải đáp ứng 8 yêu cầu chức năng cốt lõi sau:

### FR-01: Quản lý Danh sách Endpoint (Endpoint Management)
* **FR-01.1**: Người dùng có thể thêm một website hoặc API mới cần giám sát qua Dashboard bằng cách nhập Tên định danh (Friendly Name) và Địa chỉ URL (`http://` hoặc `https://`).
* **FR-01.2**: Người dùng có thể xem danh sách toàn bộ các endpoint đang được theo dõi kèm theo trạng thái hiện tại (`UP`, `DOWN`, hoặc `UNKNOWN`).
* **FR-01.3**: Người dùng có thể xóa một endpoint khỏi danh sách giám sát. Khi xóa, hệ thống ngừng kiểm tra endpoint đó.

### FR-02: Kiểm tra Tự động Định kỳ (Automated Health Check)
* **FR-02.1**: Hệ thống phải tự động kích hoạt tác vụ kiểm tra định kỳ mỗi **1 phút** một lần thông qua Amazon EventBridge Scheduler.
* **FR-02.2**: Hàm kiểm tra (Lambda HealthChecker) phải đọc toàn bộ danh sách endpoint trong bảng `Monitors` và thực hiện gửi request kiểm tra song song (Parallel Requests) để tránh nghẽn luồng.
* **FR-02.3**: Thời gian chờ phản hồi tối đa (Timeout) cho mỗi lượt ping là **5.000 ms (5 giây)**. Nếu quá thời gian này, lượt kiểm tra được coi là thất bại (`TIMEOUT`).

### FR-03: Đo lường Hiệu năng & Phân loại Kết quả (Metrics & Error Classification)
* **FR-03.1**: Hệ thống phải đo chính xác thời gian phản hồi (Response Time tính bằng mili-giây) từ thời điểm bắt đầu gửi request đến khi nhận được response header.
* **FR-03.2**: Phân loại kết quả của từng lượt kiểm tra (`CheckResult`):
  * **SUCCESS**: Endpoint trả về HTTP Status Code thuộc dải `200 - 399` và thời gian phản hồi $\le 5.000$ ms.
  * **FAIL**: Endpoint trả về HTTP `4xx`, `5xx`, hoặc request bị `Timeout`, hoặc lỗi DNS/SSL/kết nối.
* **FR-03.3**: Lưu kết quả từng lần kiểm tra vào bảng `CheckResults` kèm thời gian thực hiện (ISO 8601) và mã lỗi chi tiết.

### FR-04: Quản lý Vòng đời Trạng thái & Chống Báo động Giả (Smart State Machine)
* **FR-04.1**: Tách biệt rõ ràng giữa kết quả từng lần kiểm tra (`SUCCESS` / `FAIL`) và trạng thái sức khỏe tổng thể (`UP` / `DOWN`).
* **FR-04.2 (Ngưỡng cảnh báo)**: Chỉ chuyển trạng thái từ `UP` sang `DOWN` khi endpoint gặp **ít nhất 2 lần thất bại liên tiếp** (`consecutiveFailures >= 2`).
* **FR-04.3 (Hồi phục)**: Khi endpoint đang ở trạng thái `DOWN`, nếu có 1 lượt kiểm tra thành công (`SUCCESS`), hệ thống lập tức chuyển trạng thái về `UP` và đặt lại `consecutiveFailures = 0`.
* **FR-04.4 (Quản lý Sự cố)**: Khi endpoint chuyển sang `DOWN`, hệ thống tự động mở một bản ghi `Incident` mới (trạng thái `OPEN`). Khi hồi phục về `UP`, hệ thống cập nhật thời gian kết thúc (`resolvedAt`), tính toán tổng thời gian gián đoạn (`downtimeDurationMs`) và đánh dấu sự cố là `RESOLVED`.

### FR-05: Cảnh báo Sự cố Đa kênh (Multi-Channel Alerting)
* **FR-05.1 (Chống spam cảnh báo)**: Hệ thống chỉ gửi thông báo khi có sự **chuyển dịch trạng thái** (`UP -> DOWN` hoặc `DOWN -> UP`). Khi endpoint đã ở trạng thái DOWN, các lượt FAIL tiếp theo không được gửi thêm email/tin nhắn trùng lặp.
* **FR-05.2 (Email qua Amazon SNS)**: Gửi 1 email thông báo sự cố đến địa chỉ quản trị viên đã đăng ký khi endpoint chuyển sang `DOWN`, và gửi 1 email thông báo khôi phục khi endpoint quay về `UP`.
* **FR-05.3 (Tin nhắn Telegram Bot)**: Gửi tin nhắn tức thời kèm biểu tượng cảnh báo 🚨 và nguyên nhân lỗi đến Telegram Chat ID được cấu hình.

### FR-06: Kích hoạt Kiểm tra Tức thời ("Check Now" Button)
* **FR-06.1**: Cung cấp nút bấm **"Check Now"** trên từng thẻ endpoint tại Dashboard.
* **FR-06.2**: Khi người dùng bấm nút, hệ thống gửi lệnh qua API Gateway tới Lambda thực hiện kiểm tra ngay lập tức và trả kết quả về giao diện trong vòng $\le 2$ giây mà không cần chờ đợi cron 1 phút.

### FR-07: Dịch vụ Giả lập Tạo lỗi Nội bộ (Mock Target Service)
* **FR-07.1**: Xây dựng 1 endpoint Lambda nội bộ `CloudPulse.MockTarget` đóng vai trò làm website mẫu để phục vụ demo.
* **FR-07.2**: Cho phép điều khiển trạng thái phản hồi qua Query Parameters:
  * `GET /mock` $\rightarrow$ Trả về `200 OK`.
  * `GET /mock?status=500` $\rightarrow$ Trả về `500 Internal Server Error`.
  * `GET /mock?delay=7000` $\rightarrow$ Trì hoãn 7 giây để tạo lỗi Timeout.

### FR-08: Trực quan hóa Dữ liệu (Dashboard & Observability)
* **FR-08.1**: Hiển thị tổng quan các chỉ số quan trọng: Tỷ lệ khả dụng Uptime (%), Số endpoint UP, Số endpoint DOWN, Độ trễ trung bình.
* **FR-08.2**: Biểu đồ đường/diện tích (AreaChart) thể hiện biến thiên thời gian phản hồi (ms) của endpoint theo thời gian.
* **FR-08.3**: Danh sách lịch sử các sự cố (Incidents) hiển thị thời gian bắt đầu, thời gian kết thúc, tổng thời lượng downtime và nguyên nhân lỗi.

---

## 3. Yêu cầu Phi Chức năng (Non-Functional Requirements - NFR)

### NFR-01: Hiệu năng & Khả năng Đáp ứng (Performance)
* Thời gian phát hiện sự cố (Mean Time to Detect - MTTD) không vượt quá **2 phút**.
* Thời gian xử lý của API Gateway + Lambda cho các thao tác đọc/ghi Dashboard $\le 500$ ms.
* Nút "Check Now" trả về kết quả hiển thị trên UI trong vòng $\le 2$ giây.

### NFR-02: Độ tin cậy & Tính Toàn vẹn (Reliability)
* Tỷ lệ báo động giả (False Positive Rate) do mạng chập chờn dưới 1 phút phải bằng **0%** nhờ cơ chế ngưỡng 2 lần lỗi liên tiếp.
* Mỗi đợt sự cố chỉ phát sinh đúng **1 email cảnh báo mở** và **1 email cảnh báo đóng**.

### NFR-03: Bảo mật Mạng (Security & SSRF Mitigation)
* **Ngăn chặn SSRF**: Hệ thống bắt buộc phải kiểm tra và từ chối các URL trỏ tới:
  * Địa chỉ loopback (`127.0.0.1`, `localhost`).
  * Các dải Private IP nội bộ (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`).
  * Địa chỉ AWS Instance Metadata Service (`169.254.169.254`) nhằm chống rò rỉ IAM Role Token.
* **IAM Least Privilege**: Các thành phần chỉ được cấp quyền tối thiểu trên đúng bảng DynamoDB và Topic SNS của dự án.

### NFR-04: Tối ưu Chi phí Vận hành (Cost Efficiency / FinOps)
* Toàn bộ hệ thống phải vận hành ở mức **\$0.00/tháng** trong phạm vi thử nghiệm nhờ tối ưu hóa trong giới hạn AWS Free Tier.
* Áp dụng tính năng **DynamoDB Time-To-Live (TTL = 7 ngày)** trên bảng `CheckResults` để tự động dọn dẹp dữ liệu cũ, không làm phát sinh chi phí lưu trữ tích lũy.

### NFR-05: Khả năng Quản lý & Tự động hóa Hạ tầng (Maintainability & IaC)
* 100% tài nguyên hạ tầng phải được định nghĩa bằng mã nguồn **AWS SAM (`template.yaml`)**.
* Cho phép khởi tạo toàn bộ hạ tầng bằng lệnh `sam deploy` và xóa sạch dấu vết tài nguyên bằng lệnh `sam delete`.

---

## 4. Phạm vi Ngoài Dự án (Out of Scope / Non-Goals)

Để đảm bảo dự án hoàn thành đúng tiến độ 4 tuần và tập trung vào mục tiêu Cloud Computing, các tính năng sau được **chủ động loại bỏ khỏi phạm vi MVP**:

* ❌ **Xác thực người dùng phức tạp (AWS Cognito / OAuth2 / Multi-tenancy)**: Dashboard được thiết kế mở nội bộ, chưa phân quyền vai trò (Role-based access).
* ❌ **Tự động phục hồi mã nguồn (Self-Healing)**: Hệ thống chỉ dừng lại ở việc phát hiện và cảnh báo, không tự động can thiệp sửa mã hay restart server của website đích.
* ❌ **Giám sát tài nguyên máy chủ chuyên sâu (APM)**: Không theo dõi CPU, RAM, Disk I/O của máy chủ đích (vì không cài đặt agent).
* ❌ **Cảnh báo qua tin nhắn viễn thông SMS / Cuộc gọi thoại**: Chỉ hỗ trợ Email (SNS) và Telegram Bot (miễn phí), không dùng dịch vụ SMS trả phí của AWS SNS/Twilio.
* ❌ **Kiểm tra đa vùng địa lý (Multi-Region Probing)**: Toàn bộ Lambda Worker chạy tập trung tại 1 AWS Region được chỉ định (mặc định: `ap-southeast-1` hoặc `us-east-1`).

