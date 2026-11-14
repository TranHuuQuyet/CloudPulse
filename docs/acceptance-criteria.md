# Tiêu Chí Nghiệm Thu Hệ Thống (Acceptance Criteria)

Tài liệu này định nghĩa rõ các điều kiện tiên quyết và tiêu chí kiểm thử nghiệm thu (Acceptance Criteria - AC) theo chuẩn **Given - When - Then** để xác nhận từng tính năng của **CloudPulse** đã hoàn thành đạt chuẩn chất lượng.

---

## 1. Bảng Tổng Hợp Tiêu Chí Nghiệm Thu (Acceptance Checklist)

| Mã AC | Tính năng cần nghiệm thu | Phương thức kiểm tra | Kết quả yêu cầu |
| :--- | :--- | :--- | :---: |
| **AC-01** | Thêm Endpoint hợp lệ | Giao diện Dashboard + API | URL được lưu vào DynamoDB, hiển thị trên UI |
| **AC-02** | Chống tấn công SSRF | Thử nhập IP nhạy cảm | Bị chặn cả ở Frontend lẫn Backend |
| **AC-03** | Lập lịch tự động định kỳ | CloudWatch Logs EventBridge | Tự động chạy mỗi 60 giây $\pm 5$ giây |
| **AC-04** | Ngưỡng xác nhận DOWN (Threshold) | Cho endpoint trả lỗi | Đúng 2 lần lỗi liên tiếp mới đổi sang `DOWN` |
| **AC-05** | Cảnh báo qua Email (SNS) | Endpoint chuyển DOWN | Nhận được 1 email trong vòng $\le 5$ giây |
| **AC-06** | Cảnh báo qua Telegram Bot | Endpoint chuyển DOWN | Nhận tin nhắn có chuông báo trên điện thoại |
| **AC-07** | Nhận diện Khôi phục (Recovery) | Cho endpoint trả về 200 | Chuyển `UP`, tính downtime, gửi tin báo Resolved |
| **AC-08** | Nút "Check Now" tức thì | Bấm nút trên Dashboard | Nhận kết quả và cập nhật UI trong $\le 2$ giây |
| **AC-09** | Dịch vụ MockTarget tạo lỗi | Truy cập `?status=500` | Trả về đúng HTTP 500 phục vụ kịch bản demo |
| **AC-10** | Tự dọn dẹp dữ liệu cũ (TTL) | Kiểm tra bảng DynamoDB | Thuộc tính `expireAt` được thiết lập đúng Epoch |
| **AC-11** | Quản lý hạ tầng bằng mã (IaC) | Chạy `sam deploy` & `sam delete` | Triển khai và xóa sạch 100% tài nguyên bằng 1 lệnh |

---

## 2. Chi tiết Tiêu Chí Nghiệm Thu Theo Chuẩn Given - When - Then

### AC-01: Thêm Endpoint Giám sát Hợp lệ
* **Given**: Người dùng đang ở màn hình Dashboard CloudPulse.
* **When**: Người dùng bấm "Thêm Monitor", nhập Tên: `Google Search Engine`, URL: `https://www.google.com` và bấm Lưu.
* **Then**:
  1. API `POST /api/monitors` trả về mã `HTTP 201 Created`.
  2. Một bản ghi mới xuất hiện trong bảng DynamoDB `CloudPulse_Monitors` với `status = UNKNOWN` và `consecutiveFailures = 0`.
  3. Giao diện Dashboard hiển thị ngay lập tức thẻ endpoint mới trong danh sách.

---

### AC-02: Ngăn chặn Tuyệt đối Tấn công SSRF (Server-Side Request Forgery)
* **Scenario 1: Nhập IP Loopback**:
  * **Given**: Người dùng nhập URL `http://127.0.0.1:8080` hoặc `http://localhost:3000`.
  * **When**: Bấm nút Lưu Endpoint.
  * **Then**: Giao diện và Backend trả về lỗi `HTTP 400 Bad Request` với thông điệp: *"Private or loopback IP addresses are not permitted."*
* **Scenario 2: Nhập IP AWS Metadata**:
  * **Given**: Kẻ tấn công cố tình nhập URL `http://169.254.169.254/latest/meta-data/` để đánh cắp IAM Role Token.
  * **When**: Gửi request tới hệ thống.
  * **Then**: Module `UrlValidator` lập tức chặn đứng request, ghi log cảnh báo và không cho phép lưu vào Database.

---

### AC-03: Lập lịch Kiểm tra Tự động Định kỳ
* **Given**: Có ít nhất 1 endpoint đang được cấu hình trong bảng `Monitors`.
* **When**: Chờ trong khoảng thời gian 2 phút mà không có bất kỳ thao tác thủ công nào của người dùng.
* **Then**:
  1. CloudWatch Logs của Lambda `HealthChecker` ghi nhận 2 lần thực thi cách nhau khoảng 1 phút (`rate(1 minute)`).
  2. Bảng DynamoDB `CheckResults` xuất hiện 2 bản ghi kết quả tương ứng.
  3. Thuộc tính `lastCheckedAt` của Monitor được cập nhật thời gian mới nhất.

---

### AC-04: Ngưỡng Chống Báo Động Giả (Consecutive Failures Threshold = 2)
* **Scenario 1: Lỗi lần 1 (Chưa chuyển DOWN)**:
  * **Given**: Endpoint đang ở trạng thái `UP`.
  * **When**: Endpoint trả về HTTP 500 ở lượt kiểm tra thứ nhất.
  * **Then**:
    * Bản ghi trong `CheckResults` ghi nhận `isSuccess = false`.
    * Thuộc tính `consecutiveFailures` tăng từ 0 lên 1.
    * Trạng thái tổng thể **VẪN GIỮ NGUYÊN LÀ `UP`**.
    * **KHÔNG CÓ** bất kỳ email SNS hay tin nhắn Telegram nào được phát đi.
* **Scenario 2: Lỗi lần 2 liên tiếp (Chuyển DOWN & Kích hoạt Cảnh báo)**:
  * **Given**: Endpoint có `consecutiveFailures = 1`.
  * **When**: Endpoint tiếp tục trả về HTTP 500 ở lượt kiểm tra thứ hai.
  * **Then**:
    * `consecutiveFailures` tăng lên 2 (đạt ngưỡng).
    * Trạng thái chuyển đổi từ `UP` $\rightarrow$ `DOWN` màu đỏ trên Dashboard.
    * Tạo một bản ghi mới trong bảng `Incidents` với trạng thái `OPEN`.
    * Cảnh báo lập tức được phát đi qua Email và Telegram.
* **Scenario 3: Duy trì DOWN (Không spam cảnh báo)**:
  * **Given**: Endpoint đã ở trạng thái `DOWN`.
  * **When**: Các lượt kiểm tra tiếp theo tiếp tục bị lỗi.
  * **Then**:
    * Hệ thống ghi log FAIL nhưng **TUYỆT ĐỐI KHÔNG GỬI THÊM EMAIL/TIN NHẮN TRÙNG LẶP**.

---

### AC-05 & AC-06: Nhận Cảnh Báo Đa Kênh (SNS Email & Telegram Bot)
* **Given**: Endpoint vừa chuyển sang trạng thái `DOWN`.
* **When**: Quá trình chuyển trạng thái hoàn tất trong Lambda.
* **Then**:
  1. Hộp thư Email của người quản trị nhận được 1 email từ AWS SNS với tiêu đề: `[ALERT DOWN] <Tên Website> is DOWN!` kèm thời gian và mã lỗi.
  2. Ứng dụng Telegram trên điện thoại/máy tính nảy chuông thông báo với nội dung: `🚨 [ALERT DOWN] <Tên Website> is DOWN! ...`.
  3. Thời gian từ lúc xảy ra sự cố đến khi người dùng nhận được cảnh báo $\le 5$ giây.

---

### AC-07: Tự Động Nhận Diện Khôi Phục (Recovery)
* **Given**: Endpoint đang ở trạng thái `DOWN` và có 1 Incident đang ở trạng thái `OPEN`.
* **When**: Endpoint được sửa chữa và trả về mã `HTTP 200 OK` ở lượt kiểm tra kế tiếp.
* **Then**:
  1. Trạng thái endpoint trên Dashboard tự động chuyển từ `DOWN` $\rightarrow$ `UP` màu xanh.
  2. Thuộc tính `consecutiveFailures` được reset về 0.
  3. Bản ghi Incident được cập nhật: `status = RESOLVED`, `resolvedAt` được gán mốc thời gian hiện tại, và `downtimeDurationMs` tính đúng tổng số mili-giây gián đoạn.
  4. Hệ thống gửi thông báo khôi phục: `[RESOLVED] <Tên Website> is back UP` kèm thời gian downtime qua cả Email và Telegram.

---

### AC-08: Hiệu Năng Nút "Check Now"
* **Given**: Người dùng đang quan sát một endpoint trên Dashboard.
* **When**: Người dùng bấm nút "Check Now".
* **Then**:
  1. Nút bấm hiển thị spinner loading xoay tròn.
  2. Kết quả kiểm tra được trả về và hiển thị trên màn hình trong vòng **$\le 2.0$ giây**.
  3. Biểu đồ Latency Chart bổ sung thêm 1 điểm dữ liệu mới.
  4. Xuất hiện thông báo Toast màu xanh thông báo kết quả đo đạc.

---

### AC-09: Khả Năng Điều Khiển Dịch Vụ MockTarget
* **Given**: Hàm Lambda `CloudPulse.MockTarget` đã được deploy lên API Gateway.
* **When 1**: Gửi `GET /mock` $\rightarrow$ Trả về mã `HTTP 200` và body JSON có thuộc tính `"simulatedStatus": 200`.
* **When 2**: Gửi `GET /mock?status=500` $\rightarrow$ Trả về đúng mã `HTTP 500`.
* **When 3**: Gửi `GET /mock?delay=7000` $\rightarrow$ Response bị treo đúng 7 giây trước khi phản hồi (khiến Lambda Checker bị dính Timeout 5 giây).

---

### AC-10: Tự Động Dọn Dẹp Dữ Liệu Cũ (DynamoDB TTL)
* **Given**: Một lượt kiểm tra vừa được thực hiện vào thời điểm $T$.
* **When**: Kiểm tra thuộc tính `expireAt` của bản ghi trong bảng `CheckResults`.
* **Then**:
  1. Thuộc tính `expireAt` lưu trữ số nguyên dạng Unix Epoch timestamp bằng chính xác $T + 7\text{ ngày}$.
  2. Tính năng Time-To-Live (TTL) trên bảng `CheckResults` được kích hoạt (`Enabled: true`) trên thuộc tính `expireAt`.

---

### AC-11: Quản Trị Hạ Tầng Bằng Mã Nguồn (IaC)
* **Given**: Mã nguồn hạ tầng được định nghĩa trong file `template.yaml`.
* **When 1**: Chạy lệnh `sam build && sam deploy`.
  * **Then 1**: Toàn bộ 3 bảng DynamoDB, 1 SNS Topic, 1 API Gateway, 1 EventBridge Scheduler và 3 Lambda Functions được khởi tạo hoàn chỉnh không gặp lỗi.
* **When 2**: Chạy lệnh `sam delete --stack-name cloudpulse-app`.
  * **Then 2**: Toàn bộ tài nguyên trên CloudFormation Stack bị xóa sạch hoàn toàn, không để sót tài nguyên phát sinh chi phí ngầm.

