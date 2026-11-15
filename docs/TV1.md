# Nhật Ký Hoạt Động & Hồ Sơ Báo Cáo Cá Nhân — Thành Viên 1 (TV1)

**Dự án**: CloudPulse — Website & API Health Monitoring on AWS  
**Môn học**: Điện toán đám mây (Cloud Computing)  
**Họ và tên**: Trần Hữu Quyết  
**Vai trò**: Trưởng nhóm / Kỹ sư Hạ tầng & Vận hành (Leader / Cloud Architect & DevOps)  
**AWS Account ID**: `180459766853`  
**AWS Default Region**: `ap-southeast-1` (Singapore)  

---

## 📌 MỤC LỤC TỔNG THỂ
1. [TUẦN 1: Khởi Tạo Hạ Tầng (IaC), Quản Trị Git & Kết Nối Cloud AWS](#-tuần-1-khởi-tạo-hạ-tầng-iac-quản-trị-git--kết-nối-cloud-aws)
2. [TUẦN 2: Cấu Hình IAM Least Privilege, SNS Subscription & Kênh Telegram](#-tuần-2-cấu-hình-iam-least-privilege-sns-subscription--kênh-telegram)
3. [TUẦN 3: Triển Khai AWS Thật (sam deploy) & Ghép Nối Toàn Hệ Thống](#-tuần-3-triển-khai-aws-thật-sam-deploy--ghép-nối-toàn-hệ-thống)
4. [TUẦN 4: Diễn Tập Demo 7 Bước, Nghiệm Thu Báo Cáo & Bảo Vệ Đồ Án](#-tuần-4-diễn-tập-demo-7-bước-nghiệm-thu-báo-cáo--bảo-vệ-đồ-án-kế-hoạch)
5. [Cẩm Nang Vấn Đáp Với Giảng Viên (Q&A Chấm Điểm A+)](#-cẩm-nang-vấn-đáp-với-giảng-viên-qa-chấm-điểm-a)

---

## 🚀 TUẦN 1: KHỞI TẠO HẠ TẦNG (IaC), QUẢN TRỊ GIT & KẾT NỐI CLOUD AWS
> **Mục tiêu tuần**: Khởi tạo mã nguồn hạ tầng Serverless bằng AWS SAM, thiết lập môi trường Git phân nhánh cho 5 thành viên và cấu hình xác thực an toàn với Cloud AWS.

### 1. Bảng phân rã công việc & Kết quả nghiệm thu (DoD)

| Mã Task | Tên Công Việc | Trạng Thái | Chi Tiết Nghiệm Thu Thực Tế (DoD) |
| :---: | :--- | :---: | :--- |
| **W1-01** | Setup Git Repository & Phân quyền nhánh | ✅ **100% Xong** | Tạo nhánh `develop`, bảo vệ nhánh `main`, cấp quyền Collaborator cho 4 thành viên. Cả 5 người clone và merge code mượt mà. |
| **W1-02** | Cấu hình tài khoản AWS & Access Key | ✅ **100% Xong** | Hoàn tất xác minh tài khoản qua AWS Support (Case `179344469000044`), cấu hình `aws configure` và lệnh `aws sts get-caller-identity` trả về đúng Account ID `180459766853`. |
| **W1-03** | Soạn thảo file hạ tầng `template.yaml` (AWS SAM) | ✅ **100% Xong** | Khai báo 3 bảng DynamoDB On-Demand, SNS Topic, HTTP API Gateway v2, EventBridge 1m và 3 Lambda. Vượt qua kiểm tra `sam validate`. |

---

## ⚙️ TUẦN 2: CẤU HÌNH IAM LEAST PRIVILEGE, SNS SUBSCRIPTION & KÊNH TELEGRAM
> **Mục tiêu tuần**: Hoàn thiện chính sách bảo mật IAM Least-Privilege, tự động hóa gửi email cảnh báo qua SNS Subscription và khởi tạo kênh Telegram Bot nảy chuông sự cố thoại cho Backend.

### 1. Bảng phân rã công việc & Kết quả nghiệm thu (DoD)

| Mã Task | Tên Công Việc | Trạng Thái | Chi Tiết Nghiệm Thu Thực Tế (DoD) |
| :---: | :--- | :---: | :--- |
| **W2-01** | Cấu hình IAM Least Privilege & EventBridge Cron 1m | ✅ **100% Xong** | Cấu hình quyền `DynamoDBCrudPolicy` và `SNSPublishMessagePolicy` chỉ định danh cụ thể từng bảng/topic. Thêm biến `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID` vào `Globals`. |
| **W2-02** | Đăng ký Topic SNS Email & Hòm thư quản trị viên | ✅ **100% Xong** | Bổ sung `AlertEmailSubscription` (Protocol `email`, Endpoint `AdminEmail`) vào `template.yaml`. Khi deploy, AWS sẽ tự động gửi link Confirm Subscription. |
| **Hỗ trợ TV2** | Tạo Telegram Bot & Group Chat ID | ✅ **100% Xong** | Tạo bot `@cloudpulse_downtime_bot` (*CloudPulse Alerts*) qua `@BotFather`, tạo group *CloudPulse Demo Alerts* với Chat ID `-5320123575` bàn giao cho hệ thống. |
| **Git PR & Review** | Merge PR hạ tầng & Code Review Tuần 2 cả nhóm | ✅ **100% Xong** | Đã merge PR #19 (`968a683` → `6281307`), cùng các PR của TV3 (`7767dc3`), TV4 (`82b186a`), TV5 (`31fc1a5`); review & kiểm thử 75/75 unit test cho nhánh TV2 (`3a85e48`) và merge vào `develop` (`6a4bf43`). |

### 2. Chi tiết kỹ thuật nâng cấp trong `template.yaml` (Tuần 2)
1. **Khối Parameters linh hoạt**:
   * `AdminEmail`: Hòm thư nhận cảnh báo (mặc định: `huuquyet120406@gmail.com`).
   * `TelegramBotToken` & `TelegramChatId`: Cho phép truyền token an toàn mà không cần hard-code vào mã nguồn.
2. **Resource `AlertEmailSubscription`**:
   ```yaml
   AlertEmailSubscription:
     Type: AWS::SNS::Subscription
     Properties:
       TopicArn: !Ref CloudPulseAlertsTopic
       Protocol: email
       Endpoint: !Ref AdminEmail
   ```
3. **Bảo mật IAM Least Privilege**:
   * `ApiFunction`: Chỉ có quyền `DynamoDBCrudPolicy` trên 3 bảng.
   * `HealthCheckerFunction`: Quyền `DynamoDBCrudPolicy` trên 3 bảng và quyền `SNSPublishMessagePolicy` trên duy nhất topic `CloudPulse_Alerts`.
   * `MockTargetFunction`: Không gắn bất kỳ quyền truy cập dữ liệu nào (nguyên tắc tối thiểu quyền hạn).

---

## ☁️ TUẦN 3: TRIỂN KHAI AWS THẬT (sam deploy) & GHÉP NỐI TOÀN HỆ THỐNG
> **Mục tiêu tuần**: Đưa toàn bộ hạ tầng Serverless lên Cloud AWS thật, xuất API Gateway URL thật bàn giao cho Frontend, deploy Website lên S3 Hosting và kích hoạt Hard Code-Freeze vào cuối tuần.  
> **Cập nhật đến**: 14/11/2026 — **Đạt 100% (Hoàn thành toàn diện Hạ tầng, Backend AWS, Alerting, Ghép Frontend & S3 Hosting)**.

### 1. Bảng phân rã công việc & Kết quả nghiệm thu (DoD)

| Mã Task | Tên Công Việc | Trạng Thái | Chi Tiết Nghiệm Thu Thực Tế (DoD) |
| :---: | :--- | :---: | :--- |
| **W3-01** | Build & Deploy toàn bộ hạ tầng AWS SAM | ✅ **100% Xong** | Chạy `sam validate`, `sam build` và `sam deploy --guided` lên region `ap-southeast-1` (Stack `cloudpulse-app`). Khởi tạo đủ 3 bảng DynamoDB, SNS Topic + Email Subscription, HTTP API Gateway v2, EventBridge `rate(1 minute)` và 3 Lambda (.NET 8). |
| **W3-02** | Bàn giao Output URL & Sửa dứt điểm CORS OPTIONS | ✅ **100% Xong** | Xuất và bàn giao `ApiEndpoint` & `MockTargetEndpoint` cho nhóm. Đã cấu hình tường minh methods `GET`, `POST`, `DELETE` trong `template.yaml` để API Gateway v2 tự xử lý preflight `OPTIONS` trả về `204`, không bị CORS chặn khi gọi từ trình duyệt. |
| **W3-03** | Xác nhận SNS Email Subscription | ✅ **100% Xong** | Đã xác nhận link *Confirm subscription* cho `huuquyet120406@gmail.com` (`SubscriptionArn: arn:aws:sns:ap-southeast-1:180459766853:CloudPulse_Alerts:c0fa6370-a3e0-4fa2-8e9b-1e2d6a3ad062`) và nhận email `[CloudPulse DOWN]` thực tế. |
| **W3-06** | Kiểm tra Cron 1 phút & State Machine trên AWS | ✅ **100% Xong** | Kiểm chứng EventBridge gọi `CloudPulse-HealthChecker` đều đặn mỗi 60s (`17:22:37Z` → `17:26:37Z`). Thử nghiệm endpoint lỗi `HTTP 500`: đạt ngưỡng 2 lần liên tiếp → chuyển `DOWN`, tự mở Incident `OPEN` (`INC-1794331417140`), bắn **đúng 1 Email SNS + 1 tin nhắn Telegram 🚨** lúc `17:23:37 UTC` (`00:23` VN) và không spam ở lượt 3+. |
| **Điều phối** | Giao Issue Tuần 3 cho TV1–TV5 & API Contract | ✅ **100% Xong** | Đã chốt API Contract giữa `CloudPulse.Api` và Frontend, nghiệm thu và merge PR #29 (TV3), PR #30 (TV4), PR #31 (TV2), PR #28 (TV5) sạch sẽ vào nhánh `develop`. |
| **W3-07** | Build Frontend production & Deploy S3 Hosting | ✅ **100% Xong** | Đã tạo S3 Bucket `cloudpulse-dashboard-tv1` tại `ap-southeast-1`, bật tính năng Static Website Hosting, mở Bucket Policy Public Read và đồng bộ `frontend/dist/`. Trang web chính thức online tại: `http://cloudpulse-dashboard-tv1.s3-website-ap-southeast-1.amazonaws.com`, đồng bộ giờ Việt Nam (UTC+7). |
| **W3-08** | Gắn Tên Miền Tùy Chỉnh `clpulse.cloud` qua Cloudflare CDN | ✅ **100% Xong** | Mua domain `clpulse.cloud` tại P.A Vietnam, chuyển Nameservers về Cloudflare (`dion.ns.cloudflare.com`, `elle.ns.cloudflare.com`). Cấu hình SSL Flexible xử lý dứt điểm lỗi 522, dùng **Cloudflare Cloud Connector** giải quyết lỗi Host Header `NoSuchBucket` của S3. Website hoạt động mượt mà với chứng chỉ HTTPS bảo mật tại: `https://clpulse.cloud`. |
| **W3-09** | Xử lý Sự cố Vận hành Thực chiến & Xác minh KYC với AWS | ✅ **100% Xong** | Phát hiện và truy vết lỗi `Internal Server Error` bằng CloudWatch Metrics/CloudTrail (xác định thời điểm Lambda bị AWS Trust & Safety tạm giữ lúc 20:46 ngày 14/11 do cron 1m chạy liên tục). Nhanh chóng chuẩn bị hồ sơ KYC gồm Sao kê Techcombank (PDF) + CCCD 2 mặt tải lên cổng bảo mật `upload.aws.amazon.com`, nhận email xác nhận thụ lý từ AWS. |

### 2. Thông số Stack đã triển khai thực tế (`cloudpulse-app`)
* **Region**: `ap-southeast-1` (Singapore)
* **Custom Domain (Production)**: `https://clpulse.cloud` (Bảo mật SSL/TLS qua Cloudflare Edge)
* **Frontend S3 Website (Origin)**: `http://cloudpulse-dashboard-tv1.s3-website-ap-southeast-1.amazonaws.com`
* **ApiEndpoint**: `https://ex9kcfdj5a.execute-api.ap-southeast-1.amazonaws.com`
* **MockTargetEndpoint**: `https://ex9kcfdj5a.execute-api.ap-southeast-1.amazonaws.com/mock`
* **AlertsTopicArn**: `arn:aws:sns:ap-southeast-1:180459766853:CloudPulse_Alerts`
* **Cloudflare Nameservers**: `dion.ns.cloudflare.com`, `elle.ns.cloudflare.com`

### 3. Checklist tiến độ Tuần 3
- [x] **W3-01**: Chạy `sam build` và `sam deploy --guided` lên AWS cá nhân (`cloudpulse-app`).
- [x] **W3-02**: Bàn giao API Gateway URL thật (`ApiEndpoint`, `MockTargetEndpoint`) cho nhóm và xử lý dứt điểm CORS `OPTIONS`.
- [x] **W3-03**: Xác nhận link "Confirm Subscription" từ email do Amazon SNS gửi về.
- [x] **W3-06**: Kiểm tra EventBridge kích hoạt `HealthChecker` mỗi 1 phút và nghiệm thu luồng `UP/UNKNOWN -> DOWN`, mở Incident & bắn SNS/Telegram không spam.
- [x] **W3-07**: Build frontend production và deploy lên S3 Hosting (`http://cloudpulse-dashboard-tv1.s3-website-ap-southeast-1.amazonaws.com`).
- [x] **W3-08**: Cấu hình tên miền tùy chỉnh `https://clpulse.cloud`, thiết lập Cloudflare CDN, SSL Flexible và Cloud Connector định tuyến S3.
- [x] **W3-09**: Xử lý sự cố vận hành thực tế (Incident Management), truy vết lỗi CloudWatch/CloudTrail và hoàn tất nộp hồ sơ KYC mở khóa AWS Lambda.

---

## 🎓 TUẦN 4: DIỄN TẬP DEMO 7 BƯỚC, NGHIỆM THU BÁO CÁO & BẢO VỆ ĐỒ ÁN (KẾ HOẠCH)
> **Mục tiêu tuần**: Cùng cả nhóm diễn tập demo 7 bước trơn tru, thu thập hóa đơn AWS $0.00 và bảo vệ đồ án trước hội đồng.

- [ ] **W4-01 & W4-02**: Diễn tập kịch bản demo 7 bước thực chiến ít nhất 3 lần thành công.
- [x] **W4-03**: Thu thập bằng chứng FinOps hóa đơn AWS $0.00 USD và chi tiết Credit tài trợ $120.00 còn nguyên $119.97 (lưu tại docs/evidence/ và docs/report/assets/e07-*).
- [ ] **W4-07**: Tổng duyệt thuyết trình (Dry Run 15 phút).
- [ ] **W4-08**: BẢO VỆ ĐỒ ÁN & Chạy lệnh `sam delete` dọn sạch toàn bộ tài nguyên trên AWS.

---

## 🗣️ CẨM NANG VẤN ĐÁP VỚI GIẢNG VIÊN (Q&A CHẤM ĐIỂM A+)

### Câu 1: Em hãy trình bày vai trò và những việc đã làm được ở Tuần 1 và Tuần 2?
> **Trả lời mẫu**:  
> *"Thưa Thầy/Cô, trong dự án CloudPulse, em đảm nhận vai trò **Leader / Cloud Architect & DevOps**.  
> - **Ở Tuần 1**: Em đã thiết lập phân nhánh Git cho 5 thành viên, thiết kế file `template.yaml` bằng **AWS SAM** khai báo 3 bảng DynamoDB On-Demand (bật TTL 7 ngày), API Gateway v2, SNS Topic và cấu hình kết nối AWS CLI cục bộ bằng lệnh `aws configure`.  
> - **Ở Tuần 2**: Em hoàn thiện phân quyền **IAM Least-Privilege** cho các hàm Lambda, tự động hóa đăng ký email nhận tin sự cố qua **AWS::SNS::Subscription**, và khởi tạo bot Telegram cùng group chat để cấp mã Token/Chat ID cho TV2 tích hợp tính năng nảy chuông điện thoại tức thời."*

### Câu 2: Tại sao em lại cấu hình SNS Subscription trực tiếp trong file SAM thay vì lên Web Console bấm Subscribe?
> **Trả lời mẫu**:  
> *"Dạ thưa Thầy/Cô, việc khai báo `AWS::SNS::Subscription` trực tiếp trong `template.yaml` đảm bảo tính toàn vẹn của mô hình **Infrastructure as Code (IaC)**. Khi em chạy `sam deploy`, CloudFormation sẽ tự động đăng ký endpoint email vào Topic mà không cần người quản trị phải nhớ lên console bấm tay. Điều này giảm thiểu 100% rủi ro quên cấu hình khi triển khai ở các môi trường khác nhau."*

### Câu 3: Nguyên tắc IAM Least Privilege được áp dụng như thế nào trong dự án này?
> **Trả lời mẫu**:  
> *"Dạ, em không dùng các quyền rộng như `AdministratorAccess` hay `PowerUserAccess`. Thay vào đó, em dùng các Policy Template chuẩn của SAM: hàm `ApiFunction` chỉ được cấp quyền CRUD trên đúng 3 bảng DynamoDB của dự án; hàm `HealthCheckerFunction` chỉ có thêm quyền `SNSPublishMessagePolicy` trên duy nhất Topic `CloudPulseAlertsTopic`; còn hàm `MockTargetFunction` hoàn toàn không có quyền truy cập vào bất kỳ cơ sở dữ liệu hay tài nguyên nội bộ nào."*

### Câu 4: Nhóm đã triển khai và kiểm chứng hệ thống trên AWS thực tế như thế nào ở Tuần 3?
> **Trả lời mẫu**:  
> *"Dạ thưa Thầy/Cô, ở Tuần 3 em đã hợp nhất mã nguồn của cả 5 thành viên lên nhánh `develop` và dùng `sam build && sam deploy --guided` triển khai stack `cloudpulse-app` lên region `ap-southeast-1`. Các thông số nhạy cảm như `TelegramBotToken` được truyền qua CloudFormation Parameter (`NoEcho: true`) thay vì hard-code vào Git hay `samconfig.toml`. Sau khi xác nhận SNS Email Subscription, em đã dùng chính `MockTarget` giả lập lỗi `HTTP 500` để kiểm chứng trực tiếp trên AWS thật: EventBridge tự kích hoạt `HealthChecker` đúng mỗi 60 giây, sau đúng 2 lần lỗi liên tiếp hệ thống chuyển `DOWN`, tự mở Incident `OPEN` trong DynamoDB và gửi đồng thời 1 Email SNS kèm 1 tin nhắn Telegram mà không bị gửi trùng lặp ở các phút kế tiếp."*

### Câu 5: Tại sao nhóm lại sử dụng Cloudflare đứng trước AWS S3 và giải quyết các bài toán hạ tầng như thế nào?
> **Trả lời mẫu**:  
> *"Dạ thưa Thầy/Cô, việc đặt **Cloudflare CDN** làm Reverse Proxy đứng trước **AWS S3 Static Website** mang lại 3 giá trị kiến trúc cốt lõi:
> 1. **Bảo mật và Tối ưu Chi phí**: S3 Static Website mặc định chỉ hỗ trợ HTTP (cổng 80). Nếu dùng AWS CloudFront để cấp SSL thì sẽ phát sinh cấu hình chứng chỉ ACM phức tạp. Đặt Cloudflare đứng trước giúp cấp chứng chỉ **HTTPS miễn phí**, đồng thời CDN toàn cầu giúp giảm thiểu chi phí Data Transfer Out (Egress) của S3 về \$0.
> 2. **Xử lý sự cố SSL (Lỗi 522)**: Vì S3 Website không hỗ trợ HTTPS cổng 443 ở phía origin, em đã cấu hình Cloudflare SSL ở chế độ **Flexible** (người dùng vào `https://clpulse.cloud` bằng cổng 443 an toàn, còn Cloudflare kết nối về S3 qua cổng 80).
> 3. **Xử lý sự cố Host Header (Lỗi 404 NoSuchBucket)**: S3 sử dụng HTTP Host Header để phân giải tên bucket. Vì tên miền `clpulse.cloud` khác với tên bucket `cloudpulse-dashboard-tv1`, em đã cấu hình **Cloudflare Cloud Connector** để tự động map domain vào đúng endpoint S3 của dự án."*

### Câu 6: Trong quá trình vận hành hệ thống Serverless, em đã phát hiện và xử lý sự cố thực tế (Incident Management) như thế nào?
> **Trả lời mẫu**:  
> *"Dạ thưa Thầy/Cô, một tình huống thực chiến rất thú vị mà nhóm em đã trải qua là cơ chế rà soát bảo mật tự động của AWS (Trust & Safety / Fraud Detection). Khi hàm `HealthChecker` chạy cron liên tục mỗi 1 phút để ping kiểm tra các website, AWS đã tạm đặt tài khoản vào trạng thái `On Hold` để xác minh danh tính.  
> - Em đã dùng công cụ **CloudWatch Metrics** và **CloudTrail** để điều tra nguyên nhân gốc rễ (Root Cause Analysis), xác định chính xác thời điểm các hàm Lambda bị chặn gọi API (`AccessDeniedException: None` lúc 20:46 tối 14/11), trong khi các tài nguyên lưu trữ như DynamoDB và S3 vẫn nguyên vẹn.  
> - Ngay sau đó, em đã phối hợp cùng AWS Support, chuẩn bị đầy đủ hồ sơ KYC gồm Sao kê ngân hàng Techcombank (Bank statement PDF) và Căn cước công dân nộp lên cổng bảo mật `upload.aws.amazon.com`. Đây là một bài học thực chiến vô cùng quý báu về quy trình vận hành và ứng cứu sự cố trên môi trường Cloud thực tế của doanh nghiệp."*

### Câu 7: Chiến lược FinOps của nhóm đã giúp tiết kiệm chi phí như thế nào?
> **Trả lời mẫu**:  
> *"Dạ thưa Thầy/Cô, nhóm em áp dụng triệt để văn hóa **FinOps (Cloud Financial Operations)**:
> 1. **Serverless 100%**: Sử dụng DynamoDB chế độ **On-Demand (Pay-per-request)**, Lambda chỉ tính phí trên từng millisecond thực thi, S3 Static Website chỉ tốn vài kilobyte lưu trữ.
> 2. **Dọn dẹp tự động (TTL)**: Cấu hình DynamoDB TTL 7 ngày trên bảng `CheckResults` để dữ liệu cũ tự hủy, không tích lũy dung lượng rác.
> 3. **Kết quả nghiệm thu**: Hóa đơn tháng 11/2026 của AWS đạt đúng **$0.00 USD**, toàn bộ gói Credit tài trợ $120.00 vẫn được bảo toàn nguyên vẹn $119.97 mà hệ thống vẫn duy trì khả năng sẵn sàng cao."*

