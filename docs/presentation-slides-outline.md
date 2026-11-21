# KHUNG SƯỜN SLIDE BẢO VỆ ĐỒ ÁN: CHUYÊN ĐỀ DỊCH VỤ ĐÁM MÂY (CLOUDPULSE)

> **Tài liệu kịch bản thuyết trình & thiết kế Slide**: Dành cho buổi báo cáo và bảo vệ đồ án môn Điện toán đám mây (Cloud Computing).  
> **Trọng tâm**: Kiến trúc dịch vụ Cloud, quyết định thiết kế (Trade-offs), bảo mật IAM, Infrastructure as Code và FinOps.  
> **Thời lượng đề xuất**: 12–15 phút (bao gồm Demo).

---

## BẢNG PHÂN BỔ THỜI LƯỢNG THUYẾT TRÌNH

| Phần | Nội dung chính | Các Slide | Thời lượng |
| :--- | :--- | :---: | :---: |
| **Phần I: Khởi Động** | Giới thiệu, Đặt vấn đề & Mục tiêu kiến trúc | Slide 1 – 2 | 2 phút |
| **Phần II: Kiến Trúc Cốt Lõi** | Sơ đồ 8 tầng, Compute (.NET 8), EventBridge, DynamoDB | Slide 3 – 6 | 4 phút |
| **Phần III: Mạng & Biên** | API Gateway v2, S3 Hosting & Cloudflare Edge CDN | Slide 7 – 8 | 2.5 phút |
| **Phần IV: Vận Hành & An Ninh**| Dual-Channel Alerting, IAM Least Privilege & Chống SSRF | Slide 9 – 10 | 2.5 phút |
| **Phần V: IaC & FinOps** | AWS SAM (Disaster Recovery RTO 3p), FinOps ($0.00) & Incident KYC | Slide 11 – 12 | 2 phút |
| **Phần VI: Tổng Kết & Demo** | Kết quả kiểm thử (96 tests), Kịch bản Demo & Hỏi đáp (Q&A) | Slide 13 – 14 | 2 phút |

---

## CHI TIẾT NỘI DUNG TỪNG SLIDE & LỜI THOẠI (SPEAKER NOTES)

---

### SLIDE 1: TRANG TIÊU ĐỀ (TITLE SLIDE)

* **Tiêu đề Slide**: **CloudPulse** — Hệ Thống Giám Sát Sức Khỏe & Hiệu Năng Dịch Vụ Web
* **Phụ đề**: Kiến trúc Hướng sự kiện (Event-Driven) & Không máy chủ (Serverless) trên nền tảng AWS
* **Thông tin trình bày**:
  * Học phần: Điện toán đám mây (Cloud Computing)
  * Giảng viên hướng dẫn: Thầy/Cô [Điền tên Giảng viên]
  * Nhóm thực hiện: Nhóm [Số nhóm] (Trưởng nhóm: Trần Hữu Quyết)
  * Môi trường vận hành thực tế: `https://clpulse.cloud` | Region: `ap-southeast-1` (Singapore)
* **Hình ảnh gợi ý**: Logo CloudPulse (đường sóng lifeline pulse màu xanh ngọc), các huy hiệu công nghệ: *AWS Serverless, .NET 8, React 18, Cloudflare*.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Kính thưa Thầy/Cô và các bạn, hôm nay nhóm em xin phép được báo cáo và bảo vệ đồ án môn Điện toán đám mây với đề tài: **CloudPulse — Nền tảng giám sát sức khỏe và hiệu năng dịch vụ web xây dựng trên kiến trúc Serverless & Event-Driven của AWS**. Dự án hiện đã được triển khai hoàn chỉnh trên môi trường thực tế tại tên miền chính thức `clpulse.cloud` thuộc vùng Singapore. Sau đây em xin đại diện nhóm trình bày chi tiết về kiến trúc các dịch vụ đám mây của dự án."*

---

### SLIDE 2: BỐI CẢNH & TẠI SAO CHỌN SERVERLESS? (PROBLEM & MOTIVATION)

* **Tiêu đề Slide**: Bối Cảnh Thực Tế & Lựa Chọn Kiến Trúc
* **Nội dung hiển thị trên Slide**:
  * **Đặc thù bài toán giám sát**:
    * Cần chạy định kỳ 24/7 để phát hiện sự cố website/API kịp thời.
    * Lưu lượng truy cập giao diện quản trị (Dashboard) chỉ diễn ra ngắt quãng theo đợt.
  * **Hạn chế của mô hình Monolith trên VPS / EC2**:
    * **Lãng phí chi phí rảnh rỗi (Idle Cost)**: Phải trả tiền thuê CPU/RAM 24/7 kể cả lúc không có tải.
    * **Single Point of Failure (SPOF)**: Nếu máy chủ EC2 crash hoặc bảo trì, toàn bộ hệ thống sập theo.
    * **Gánh nặng vận hành**: Tốn nhân sự vá lỗi hệ điều hành và cấu hình an ninh mạng.
  * **Giải pháp: Serverless-First trên AWS**:
    * **Zero Idle Cost**: Chỉ tính phí khi có request thực tế, tài nguyên tự động giải phóng khi rảnh rỗi.
    * **Khả năng chịu lỗi cao (High Availability)**: Tự động phân tán trên tối thiểu 3 Availability Zones (AZs).
    * **Tối ưu FinOps**: Toàn bộ chi phí vận hành tiệm cận **$0.00 USD/tháng**.
* **Hình ảnh gợi ý**: Bảng so sánh trực quan giữa *Mô hình truyền thống (EC2)* vs *Mô hình Không máy chủ (Serverless)*.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Thưa Thầy/Cô, khi bắt đầu thiết kế hệ thống giám sát, câu hỏi đầu tiên đặt ra là: Tại sao không thuê 1 máy ảo EC2 cho đơn giản? Nhóm nhận thấy rằng việc thuê EC2 sẽ tạo ra chi phí rảnh rỗi rất lớn vì CPU phần lớn thời gian không dùng tới, đồng thời tạo ra một điểm chết duy nhất (Single Point of Failure). Nhóm quyết định lựa chọn kiến trúc Serverless 100%, giúp hệ thống tự động co giãn theo tải thực tế, đảm bảo độ sẵn sàng cao trên đa vùng độc lập và giữ chi phí vận hành ở mức đúng 0 đô-la."*

---

### SLIDE 3: KIẾN TRÚC TỔNG THỂ HỆ THỐNG (SYSTEM ARCHITECTURE)

* **Tiêu đề Slide**: Kiến Trúc Tổng Thể Hệ Thống 8 Tầng (Loose Coupling)
* **Nội dung hiển thị trên Slide**:
  * **Tách rời hoàn toàn giữa 3 phân hệ chính**:
    1. **Luồng Thăm dò nền (Background Polling)**: EventBridge (1m) ➔ Lambda HealthChecker ➔ Target Endpoints.
    2. **Luồng Quản trị API (API Orchestration)**: Người dùng ➔ Cloudflare Edge ➔ API Gateway v2 ➔ Lambda Api.
    3. **Tầng Lưu trữ & Dữ liệu**: Amazon DynamoDB (On-Demand) & S3 Static Website Hosting.
  * **Tầng Thông báo Đa kênh (Fan-out)**: Phát tán đồng thời tới Amazon SNS (Email) và Telegram Bot (Push Alert).
* **Hình ảnh gợi ý**: Sơ đồ kiến trúc Mermaid 8 tầng trích từ file `README.md` (Tầng Biên ➔ Tầng Lưu Trữ Tĩnh ➔ Tầng API Gateway ➔ Tầng Lambda ➔ Tầng EventBridge ➔ Tầng DynamoDB ➔ Tầng Cảnh Báo).
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Trên màn hình là sơ đồ kiến trúc 8 tầng của CloudPulse. Điểm cốt lõi ở đây là nguyên lý Loose Coupling (Phân rã lỏng): Luồng kiểm tra định kỳ chạy nền độc lập hoàn toàn với luồng xử lý API của người dùng. Hai luồng này không gọi trực tiếp lẫn nhau mà đồng bộ trạng thái thông qua cơ sở dữ liệu DynamoDB. Nhờ đó, nếu giao diện có hàng nghìn người truy cập cùng lúc thì luồng ping kiểm tra sức khỏe của hệ thống vẫn hoạt động chính xác từng giây mà không hề bị ảnh hưởng."*

---

### SLIDE 4: TẦNG TÍNH TOÁN SERVERLESS (AWS LAMBDA WITH .NET 8)

* **Tiêu đề Slide**: Tầng Tính Toán Serverless — AWS Lambda (.NET 8)
* **Nội dung hiển thị trên Slide**:
  * **3 Hàm Lambda chuyên trách**:
    * `CloudPulse-Api`: ASP.NET Core Minimal API xử lý các request RESTful CRUD và API đo tức thời ("Check Now").
    * `CloudPulse-HealthChecker`: Worker động cơ kiểm tra chạy nền định kỳ mỗi 60 giây.
    * `CloudPulse-MockTarget`: Dịch vụ độc lập mô phỏng mã lỗi HTTP 200, 500 và độ trễ 7000ms.
  * **Các quyết định thiết kế tối ưu**:
    * **Runtime**: .NET 8 Minimal API giúp gói đóng gói siêu nhẹ, giảm thời gian khởi động lạnh (Cold Start).
    * **Cấu hình tối ưu**: `Memory: 256MB` | `Timeout: 30s` (điểm cân bằng giữa xung nhịp CPU và định mức Free Tier).
    * **Quyết định Non-VPC**: Chạy ngoài VPC để kết nối Internet tự nhiên, **tiết kiệm $32.40/tháng** phí thuê AWS NAT Gateway mà vẫn an toàn qua SigV4.
* **Hình ảnh gợi ý**: Biểu đồ phân bổ 3 hàm Lambda kèm thông số RAM, Timeout, IAM Policies và Triggers.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Tại tầng tính toán, nhóm lựa chọn AWS Lambda với runtime .NET 8. Một quyết định kiến trúc rất quan trọng mà nhóm đã cân nhắc là **không đặt Lambda vào trong VPC**. Lý do là vì Lambda cần ping ra ngoài Internet tới các website mục tiêu. Nếu đặt trong VPC, bắt buộc phải sử dụng AWS NAT Gateway với chi phí cố định tối thiểu 32.40 USD mỗi tháng. Bằng cách để Lambda chạy Non-VPC, hệ thống vừa gọi ra ngoài Internet miễn phí, vừa giao tiếp an toàn với DynamoDB qua mạng trục nội bộ AWS bằng cơ chế ký danh tính SigV4."*

---

### SLIDE 5: ĐIỀU PHỐI LẬP LỊCH & BỘ MÁY TRẠNG THÁI (STATE MACHINE)

* **Tiêu đề Slide**: Lập Lịch Event-Driven & Khử Nhiễu Cảnh Báo
* **Nội dung hiển thị trên Slide**:
  * **Amazon EventBridge Scheduler**:
    * Quy tắc `rate(1 minute)` phát nhịp (Heartbeat) kích hoạt Lambda bất đồng bộ (Asynchronous Invocation).
    * Giải phóng tài nguyên: Không cần chạy vòng lặp vô tận `while(true)` gây chiếm dụng RAM.
  * **Bộ máy trạng thái (State Machine) Chống Bão Cảnh Báo (Anti-Alert Fatigue)**:
    * **Ngưỡng 2 chu kỳ ($N \ge 2$)**: Khi gặp lỗi lần đầu, hệ thống chỉ ghi nhận `consecutiveFailures = 1` (giữ nguyên trạng thái). Đúng 2 lần lỗi liên tiếp mới xác nhận `DOWN` và mở sự cố `OPEN`.
    * **Không spam lặp lại**: Trong suốt thời gian website duy trì `DOWN`, hệ thống duy trì theo dõi nhưng tuyệt đối không gửi thêm cảnh báo ở các phút kế tiếp.
    * **Tự động đóng sự cố (Self-Healing Closure)**: Khi website hồi phục HTTP 200, tự động chuyển `UP`, đóng sự cố (`RESOLVED`), tính toán chính xác tổng thời gian chết (`downtimeSeconds`) và gửi duy nhất 1 bản tin phục hồi.
* **Hình ảnh gợi ý**: Sơ đồ máy trạng thái hữu hạn (FSM Diagram) minh họa luồng chuyển dịch: `UP ➔ Lỗi 1 (Chờ) ➔ Lỗi 2 (Báo động DOWN) ➔ Duy trì DOWN ➔ Khôi phục (UP & RECOVERED)`.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Để kích hoạt kiểm tra, nhóm dùng Amazon EventBridge với chu kỳ `rate(1 minute)`. Điểm đặc biệt của CloudPulse nằm ở thuật toán State Machine khử nhiễu: Trên môi trường Internet, mạng chập chờn nhất thời là điều rất hay xảy ra. Nếu lỗi 1 lần mà báo động ngay sẽ gây ra 'bão cảnh báo' làm phiền quản trị viên. Vì vậy, hệ thống đặt ngưỡng đúng 2 lần lỗi liên tiếp mới xác nhận DOWN và bắn duy nhất 1 cảnh báo. Khi website hoạt động trở lại, hệ thống tự động đóng sự cố, đo lường tổng thời gian sập mạng và gửi tin phục hồi."*

---

### SLIDE 6: CƠ SỞ DỮ LIỆU NOSQL & VÒNG ĐỜI DỮ LIỆU (AMAZON DYNAMODB)

* **Tiêu đề Slide**: Cơ Sở Dữ Liệu DynamoDB & Cơ Chế TTL Tự Động Dọn Rác
* **Nội dung hiển thị trên Slide**:
  * **Chế độ On-Demand (`PAY_PER_REQUEST`)**:
    * Tự động co giãn theo lưu lượng thực tế, không bị nghẽn thông lượng và không mất phí dự phòng RCU/WCU.
  * **Thiết kế Khóa chính (Data Modeling)**:
    * `CloudPulse_Monitors`: Khóa chính `monitorId` (hashing đồng đều, loại bỏ hoàn toàn Hot Partition).
    * `CloudPulse_CheckResults`: Khóa kết hợp `HASH(monitorId) + RANGE(checkedAt)`.
    * *Hiệu năng*: Truy vấn lịch sử 30 lần đo gần nhất chỉ mất **5ms** bằng cách quét ngược Sort Key (`ScanIndexForward = false`).
  * **Tự động thu hồi bộ nhớ (Time-To-Live - TTL)**:
    * Kích hoạt TTL trên trường `expireAt = checkedAt + 7 days`.
    * AWS tự động xóa sạch các bản ghi quá hạn 7 ngày ngầm dưới nền hoàn toàn miễn phí ➔ **Zero Data Bloat**.
* **Hình ảnh gợi ý**: Bảng cấu trúc dữ liệu của 3 bảng DynamoDB + sơ đồ minh họa cơ chế tự hủy bản ghi của tính năng TTL.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Về tầng lưu trữ, nhóm sử dụng DynamoDB ở chế độ On-Demand. Với dữ liệu chuỗi thời gian như lịch sử ping, cứ mỗi 1 phút lại sinh ra hàng trăm bản ghi mới. Nếu không xử lý, cơ sở dữ liệu sẽ nhanh chóng phình to và phát sinh chi phí lưu trữ. Nhóm đã kích hoạt tính năng DynamoDB Time-To-Live (TTL) trên trường `expireAt`. Sau đúng 7 ngày, AWS sẽ tự động quét và xóa sạch các bản ghi cũ ngầm dưới nền hoàn toàn miễn phí, giúp dữ liệu luôn giữ ở mức tinh gọn nhất."*

---

### SLIDE 7: CỔNG ĐIỀU PHỐI API & TỐI ƯU MẠNG (AMAZON API GATEWAY V2)

* **Tiêu đề Slide**: Cổng API Gateway v2 & Kỹ Thuật Xử Lý Native CORS
* **Nội dung hiển thị trên Slide**:
  * **Lựa chọn HTTP API (v2) thay vì REST API (v1)**:
    * Giảm **70% độ trễ xử lý (overhead)** và tiết kiệm 30% chi phí.
    * Hạn mức mặc định cao: **10,000 request/giây** (RPS) và burst 5,000 RPS.
  * **Kỹ thuật Explicit Routing giải quyết triệt để lỗi CORS**:
    * Thay vì dùng method `ANY`, file SAM khai báo rõ ràng các method `GET`, `POST`, `DELETE` trên path `/{proxy+}`.
    * **Cơ chế phản hồi tức thời**: Trình duyệt khi gửi dữ liệu luôn gửi trước request preflight `OPTIONS`. API Gateway nhận diện và **tự động phản hồi HTTP 204 No Content trong vòng 1ms ngay tại tầng Gateway**.
    * **Hiệu quả**: Không cần đánh thức Lambda xử lý preflight ➔ Tiết kiệm 50% số lần gọi hàm và triệt tiêu 100% lỗi CORS trên Dashboard.
* **Hình ảnh gợi ý**: Sơ đồ luồng xử lý Preflight Request giữa Trình duyệt ➔ API Gateway (HTTP 204) và Request thực tế đi vào Lambda.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Đối với cổng API, nhóm dùng Amazon API Gateway thế hệ 2 (HTTP API). Một kỹ thuật xử lý mạng rất hay mà nhóm áp dụng là khai báo phương thức tường minh thay vì dùng ANY. Nhờ đó, các request preflight OPTIONS do trình duyệt gửi đến sẽ được chính API Gateway trả lời ngay lập tức bằng mã HTTP 204 trong 1 mili-giây mà không cần chuyển tiếp vào Lambda. Điều này vừa giúp giao diện web không bao giờ bị lỗi CORS, vừa tiết kiệm một nửa số lần gọi Lambda không cần thiết."*

---

### SLIDE 8: LƯU TRỮ TĨNH & PHÂN PHỐI TẦNG BIÊN (S3 + CLOUDFLARE CDN)

* **Tiêu đề Slide**: Lưu Trữ Tĩnh S3 & Mạng Lưới Biên Cloudflare CDN
* **Nội dung hiển thị trên Slide**:
  * **Amazon S3 Static Website Hosting**:
    * Bucket `cloudpulse-dashboard-tv1` lưu trữ gói build React 18 SPA.
    * Cấu hình `--error-document index.html` hỗ trợ chuyển hướng Client-side Routing.
  * **Cloudflare CDN & Tên miền chính thức `clpulse.cloud`**:
    * **Cấp chứng chỉ HTTPS miễn phí** qua chế độ **SSL Flexible** (Client gọi HTTPS cổng 443 ➔ Cloudflare gọi S3 qua HTTP cổng 80, giải quyết triệt để lỗi **Error 522**).
    * **Tính năng Cloud Connector**: Tự động ánh xạ và ghi đè Host Header, khắc phục lỗi **404 NoSuchBucket** do tên miền ngoài khác tên bucket S3.
    * **Edge Caching**: Lưu cache toàn bộ file tĩnh tại hơn 300 trạm biên toàn cầu, giảm 99% tải cho S3 và triệt tiêu chi phí Data Transfer Out (Egress).
* **Hình ảnh gợi ý**: Sơ đồ luồng phân phối nội dung tĩnh: `User (HTTPS:443)` ➔ `Cloudflare Edge CDN (SSL Termination)` ➔ `AWS S3 Bucket (HTTP:80)`.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Giao diện người dùng được lưu trữ trên AWS S3 dưới dạng Static Website Hosting. Để cung cấp tên miền chuyên nghiệp `clpulse.cloud` và chứng chỉ bảo mật HTTPS, nhóm đặt Cloudflare CDN làm Reverse Proxy phía trước S3. Tại đây, nhóm đã xử lý 2 sự cố kinh điển: Cấu hình SSL Flexible để tránh lỗi 522 do S3 chỉ mở cổng 80, và cấu hình Cloud Connector để sửa lỗi lệch Host Header 404 NoSuchBucket. Toàn bộ file tĩnh được lưu cache tại trạm biên Cloudflare giúp tốc độ mở web cực nhanh và chi phí truyền dữ liệu của S3 bằng 0."*

---

### SLIDE 9: CẢNH BÁO SỰ CỐ ĐA KÊNH (DUAL-CHANNEL ALERTING)

* **Tiêu đề Slide**: Cơ Chế Cảnh Báo Sự Cố Kênh Đôi Tức Thời
* **Nội dung hiển thị trên Slide**:
  * **Kênh 1: Amazon SNS (Email Pub/Sub)**:
    * Khai báo tự động bằng Infrastructure as Code (`AWS::SNS::Subscription`).
    * Mô hình Xuất bản - Đăng ký (Fan-out): Lambda chỉ bắn 1 message vào Topic, SNS tự động phát tán tới toàn bộ danh sách quản trị viên.
    * Đảm bảo tính lưu trữ chính thức, có nhật ký phục vụ báo cáo kiểm toán kỹ thuật.
  * **Kênh 2: Telegram Bot API (Webhook Push Notification)**:
    * Bắn thông báo trực tiếp vào nhóm Telegram của ban quản trị trong vòng **dưới 1 giây**.
    * Kích hoạt chuông và rung điện thoại tức thời cho kỹ sư trực vận hành.
    * Bảo mật mã Token bằng thuộc tính `NoEcho: true` trên CloudFormation.
* **Hình ảnh gợi ý**: Ảnh chụp thực tế Email cảnh báo từ Amazon SNS đặt cạnh Ảnh chụp tin nhắn Telegram Bot nảy chuông trên điện thoại.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Khi phát hiện sự cố, hệ thống kích hoạt cơ chế cảnh báo kênh đôi: Amazon SNS gửi email chính thức phục vụ việc lưu trữ kiểm toán, đồng thời Telegram Bot gửi thông báo đẩy nảy chuông điện thoại của kỹ sư trực vận hành trong vòng chưa đầy 1 giây. Việc kết hợp 2 kênh này đảm bảo đội ngũ kỹ thuật phản ứng nhanh nhất mà không lo bị sót thông báo."*

---

### SLIDE 10: AN NINH ĐÁM MÂY & PHÒNG THỦ ĐA LỚP (CLOUD SECURITY)

* **Tiêu đề Slide**: An Ninh Đám Mây & Phòng Thủ Chiều Sâu (Defense in Depth)
* **Nội dung hiển thị trên Slide**:
  * **Nguyên tắc Đặc quyền Tối thiểu (IAM Least Privilege)**:
    * Sử dụng SAM Policy Templates chuẩn (`DynamoDBCrudPolicy`, `SNSPublishMessagePolicy`).
    * Cô lập hoàn toàn hàm `MockTarget`: Không có quyền truy cập cơ sở dữ liệu hay gửi tin cảnh báo, giảm thiểu tối đa bán kính thiệt hại (Blast Radius).
  * **Phòng vệ Chống Tấn công SSRF (Server-Side Request Forgery)**:
    * Module `UrlValidator.cs` phân giải DNS và kiểm tra IP trước khi gửi request.
    * **Chặn triệt để**: Dải IP riêng RFC 1918 (`10.x`, `172.16.x`, `192.168.x`), Loopback (`127.0.0.1`), và đặc biệt là dải Link-Local **`169.254.0.0/16`** nhằm ngăn chặn nguy cơ kẻ xấu trích xuất IAM Credentials thông qua AWS Instance Metadata Service (IMDS).
    * Bộ test suite gồm **51 unit tests** kiểm chứng 100% Passed.
* **Hình ảnh gợi ý**: Sơ đồ minh họa kiến trúc bộ lọc bảo mật SSRF chặn đứng các request hướng vào địa chỉ Metadata `169.254.169.254`.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Về mặt bảo mật, CloudPulse áp dụng nguyên tắc Defense in Depth: Ở tầng hạ tầng, nhóm tuân thủ IAM Least Privilege, chỉ cấp đúng quyền đọc/ghi trên 3 bảng cụ thể. Ở tầng ứng dụng, vì hệ thống cho phép người dùng nhập URL để ping kiểm tra, nhóm đã xây dựng module UrlValidator ngăn chặn triệt để lỗ hổng SSRF. Nếu kẻ xấu cố tình nhập địa chỉ IP Link-Local `169.254.169.254` nhằm đánh cắp IAM Role của máy chủ AWS, hệ thống sẽ lập tức từ chối và chặn đứng cuộc tấn công."*

---

### SLIDE 11: HẠ TẦNG DƯỚI DẠNG MÃ & PHỤC HỒI THẢM HỌA (IAC & DR)

* **Tiêu đề Slide**: Hạ Tầng Dưới Dạng Mã (IaC) & Khả Năng Khôi Phục Thảm Họa
* **Nội dung hiển thị trên Slide**:
  * **AWS SAM (Serverless Application Model)**:
    * 100% tài nguyên đám mây được định nghĩa trong tệp `template.yaml`.
    * Tự động hóa toàn bộ quy trình biên dịch, đóng gói và triển khai qua các lệnh: `sam validate ➔ sam build ➔ sam deploy`.
  * **Khả năng Phục hồi Thảm họa (Disaster Recovery - DR)**:
    * **RTO (Recovery Time Objective)**: Chỉ mất **3–5 phút** để tái sinh toàn bộ hệ thống sang một Region hoàn toàn mới (ví dụ Tokyo `ap-northeast-1`) khi xảy ra thiên tai sập vùng Singapore.
    * **RPO (Recovery Point Objective)**: Xấp xỉ **bằng 0 (dưới 1 giây)** nhờ tính năng sao chép dữ liệu đa vùng chủ động (DynamoDB Global Tables).
* **Hình ảnh gợi ý**: Quy trình triển khai IaC với AWS SAM và bảng so sánh chỉ số RTO/RPO chuẩn quốc tế.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Toàn bộ hệ thống CloudPulse được quản lý 100% bằng Infrastructure as Code thông qua AWS SAM. Điều này đem lại năng lực phục hồi thảm họa cực kỳ ấn tượng: Nếu toàn bộ trung tâm dữ liệu tại Singapore gặp sự cố bất khả kháng, nhóm chỉ cần chạy duy nhất 1 câu lệnh `sam deploy` sang Region Tokyo hoặc US. Toàn bộ cơ sở dữ liệu, API Gateway và các hàm tính toán sẽ được tái tạo hoàn chỉnh trong vòng đúng 3 đến 5 phút."*

---

### SLIDE 12: QUẢN TRỊ TÀI CHÍNH FINOPS & XỬ LÝ SỰ CỐ THỰC CHIẾN

* **Tiêu đề Slide**: Tối Ưu Hóa Chi Phí FinOps & Bài Học Vận Hành Thực Tế
* **Nội dung hiển thị trên Slide**:
  * **Chứng minh Toán học FinOps — Hóa đơn $0.00 USD/tháng**:
    * **Lambda**: Cron 1 phút tiêu thụ: $1 \times 60 \times 24 \times 30 = 43,200\text{ req/tháng}$ (chỉ chiếm **4.32%** định mức 1 triệu request miễn phí của AWS).
    * **DynamoDB**: 25 GB Storage và 25 RCU/WCU Always Free (dữ liệu nhóm dùng < 10 MB).
    * **S3 & Cloudflare**: Bản build chỉ 500 KB, băng thông do Cloudflare miễn phí gánh tải.
    * **Kết quả nghiệm thu**: Hóa đơn tháng 11/2026 đạt đúng **$0.00 USD**, bảo toàn nguyên vẹn gói tài trợ tín dụng AWS.
  * **Kinh nghiệm Xử lý Sự cố Thực tế (Incident Management)**:
    * Cơ chế Fraud Detection của AWS tạm đặt tài khoản vào diện xác minh (Verification Hold) khi phát hiện cron ping liên tục.
    * Điều tra nguyên nhân gốc rễ (RCA) qua **CloudWatch Logs** (`AccessDeniedException: None`).
    * Quy trình KYC: Hoàn thiện hồ sơ CCCD và Sao kê Techcombank nộp lên cổng bảo mật `upload.aws.amazon.com`.
* **Hình ảnh gợi ý**: Ảnh chụp hóa đơn AWS Billing $0.00 USD đặt cạnh ảnh chụp nhật ký lỗi CloudWatch lúc điều tra sự cố.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Về khía cạnh quản trị chi phí FinOps, nhờ tính toán định mức Free Tier kỹ lưỡng, hệ thống chạy liên tục 24/7 nhưng hóa đơn thực tế của nhóm đạt đúng 0.00 USD. Trong quá trình vận hành, nhóm cũng có được một bài học thực chiến rất quý báu về quy trình ứng cứu sự cố: Khi cron ping liên tục, hệ thống phòng vệ gian lận của AWS đã tạm khóa quyền Lambda. Nhóm đã sử dụng CloudWatch Logs để điều tra nguyên nhân gốc rễ và phối hợp nộp hồ sơ KYC chính chủ để mở khóa tài khoản."*

---

### SLIDE 13: KẾT QUẢ NGHIỆM THU & QUY TRÌNH DEMO (LIVE DEMO)

* **Tiêu đề Slide**: Kết Quả Nghiệm Thu Kỹ Thuật & Kịch Bản Demo
* **Nội dung hiển thị trên Slide**:
  * **Kết quả kiểm thử tự động toàn diện**:
    * **96/96 Unit Tests Backend Passed 100%** (.NET 8).
    * Mã nguồn Frontend đạt chuẩn tĩnh ESLint (0 errors, 0 warnings).
    * Giao diện Responsive, chuẩn Light Theme, đồng bộ múi giờ Việt Nam (UTC+7).
  * **Kịch bản Demo trực tiếp 4 bước**:
    1. Giới thiệu Dashboard trực quan tại `https://clpulse.cloud`.
    2. Thêm endpoint bình thường và kiểm thử tính năng đo đạc tức thời **"Check Now"** (1-2s).
    3. Giả lập lỗi máy chủ bằng `MockTarget` (`?status=500`): Sau đúng 2 chu kỳ ping, hệ thống chuyển `DOWN`, mở sự cố `OPEN` và phát tán cảnh báo đồng thời qua Email SNS và Telegram Bot.
    4. Trả về trạng thái bình thường: Hệ thống tự động chuyển `UP`, đánh dấu `RESOLVED` và gửi thông báo phục hồi `[RECOVERED]`.
* **Hình ảnh gợi ý**: Ảnh chụp màn hình Dashboard CloudPulse trực quan với biểu đồ LatencyChart và danh sách Monitors.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Sau đây, nhóm em xin phép được trình chiếu và demo trực tiếp các tính năng của hệ thống trên môi trường Cloud thực tế tại tên miền clpulse.cloud..."*

---

### SLIDE 14: LỜI CẢM ƠN & PHẢN BIỆN (Q&A)

* **Tiêu đề Slide**: Lời Cảm Ơn & Sẵn Sàng Trả Lời Phản Biện
* **Nội dung hiển thị trên Slide**:
  * **Dự án**: CloudPulse — Serverless Health & Latency Monitoring Platform
  * **Kênh truy cập trực tuyến**:
    * Production Web: [https://clpulse.cloud](https://clpulse.cloud)
    * Mã nguồn GitHub: `github.com/TranHuuQuyet/CloudPulse`
    * Tài liệu kỹ thuật: `docs/cloud-services-setup-guide.md` & `docs/TV1.md`
  * **Nhóm xin trân trọng cảm ơn Quý Thầy/Cô và các bạn đã chú ý lắng nghe!**
  * *Nhóm rất mong nhận được những câu hỏi và ý kiến đóng góp từ Hội đồng đánh giá.*
* **Hình ảnh gợi ý**: Mã QR quét link truy cập trang web `https://clpulse.cloud` và liên kết GitHub repo.
* **Lời thoại thuyết trình (Speaker Notes)**:
  > *"Phần trình bày của nhóm em về kiến trúc dịch vụ đám mây của dự án CloudPulse đến đây là kết thúc. Nhóm em xin chân thành cảm ơn Quý Thầy/Cô đã dành thời gian theo dõi và rất mong nhận được những câu hỏi phản biện từ Thầy/Cô ạ!"*

---
*Tài liệu này được biên soạn độc quyền phục vụ báo cáo và bảo vệ đồ án CloudPulse — Bản quyền học thuật 2026.*
