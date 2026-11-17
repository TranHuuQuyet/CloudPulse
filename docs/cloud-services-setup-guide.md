# CẨM NANG CHI TIẾT: CÁCH THIẾT LẬP VÀ GIẢI NGHĨA TỪNG DỊCH VỤ ĐÁM MÂY (CLOUDPULSE)

> **Tài liệu hướng dẫn kỹ thuật chuyên sâu**: Dành cho Trưởng nhóm, thành viên thực hiện và bảo vệ đồ án môn Điện toán đám mây (Cloud Computing).  
> **Phiên bản**: 2.0 (Cập nhật chuẩn thực chiến & phản biện học thuật).

---

## MỤC LỤC
1. [Tổng quan Triết lý Kiến trúc & Phương pháp Triển khai](#1-tổng-quan-triết-lý-kiến-trúc--phương-pháp-triển-khai)
2. [Dịch vụ 1: Amazon DynamoDB (Cơ sở dữ liệu NoSQL)](#2-dịch-vụ-1-amazon-dynamodb-cơ-sở-dữ-liệu-nosql)
3. [Dịch vụ 2: AWS Lambda (Tính toán Serverless với .NET 8)](#3-dịch-vụ-2-aws-lambda-tính-toán-serverless-với-net-8)
4. [Dịch vụ 3: Amazon API Gateway v2 (HTTP API Gateway)](#4-dịch-vụ-3-amazon-api-gateway-v2-http-api-gateway)
5. [Dịch vụ 4: Amazon EventBridge (Bộ điều phối Lập lịch Event-Driven)](#5-dịch-vụ-4-amazon-eventbridge-bộ-điều-phối-lập-lịch-event-driven)
6. [Dịch vụ 5: Amazon SNS (Simple Notification Service - Email Fan-out)](#6-dịch-vụ-5-amazon-sns-simple-notification-service---email-fan-out)
7. [Dịch vụ 6: Amazon S3 (Lưu trữ & Phân phối Website Tĩnh SPA)](#7-dịch-vụ-6-amazon-s3-lưu-trữ--phân-phối-website-tĩnh-spa)
8. [Dịch vụ 7: Cloudflare (DNS, Edge CDN, SSL Flexible & Cloud Connector)](#8-dịch-vụ-7-cloudflare-dns-edge-cdn-ssl-flexible--cloud-connector)
9. [Dịch vụ 8: Telegram Bot API (Kênh Cảnh báo Tức thời Đa kênh)](#9-dịch-vụ-8-telegram-bot-api-kênh-cảnh-báo-tức-thời-đa-kênh)
10. [Dịch vụ 9: AWS IAM (Quản lý Quyền theo Nguyên tắc Least Privilege)](#10-dịch-vụ-9-aws-iam-quản-lý-quyền-theo-nguyên-tắc-least-privilege)
11. [Dịch vụ 10: Amazon CloudWatch (Ghi vết Log & Giám sát Vận hành)](#11-dịch-vụ-10-amazon-cloudwatch-ghi-vết-log--giám-sát-vận-hành)
12. [Dịch vụ 11: AWS SAM & CloudFormation (Infrastructure as Code - IaC)](#12-dịch-vụ-11-aws-sam--cloudformation-infrastructure-as-code---iac)
13. [Bảng Tổng kết Khắc phục Sự cố Thực tế (Troubleshooting Matrix)](#13-bảng-tổng-kết-khắc-phục-sự-cố-thực-tế-troubleshooting-matrix)

---

## 1. TỔNG QUAN TRIẾT LÝ KIẾN TRÚC & PHƯƠNG PHÁP TRIỂN KHAI

Hệ thống **CloudPulse** không sử dụng cách làm truyền thống (lên AWS Web Console bấm chuột tạo từng tài nguyên). Thay vào đó, toàn bộ hệ thống tuân thủ **2 nguyên lý cốt lõi của kỹ thuật Cloud hiện đại**:

1. **Infrastructure as Code (IaC)**: Mọi tài nguyên Backend (DynamoDB, Lambda, API Gateway, EventBridge, SNS, IAM) đều được định nghĩa 100% bằng mã trong tệp `template.yaml` (AWS SAM).
   * *Ý nghĩa*: Đảm bảo tính tái hiện (Reproducibility), loại bỏ 100% sai sót do thao tác tay, và có thể triển khai lại toàn bộ hệ thống sang một Region khác chỉ trong vòng 3 phút.
2. **FinOps-First (Tối ưu hóa Chi phí Tiệm cận $0.00)**:
   * Tất cả dịch vụ đều sử dụng mô hình Serverless Pay-as-you-go và nằm trọn trong hạn mức **AWS Always Free Tier** hoặc **12-Month Free Tier**.
   * Không phát sinh chi phí duy trì cố định (Idle Cost = $0).

---

## 2. DỊCH VỤ 1: AMAZON DYNAMODB (CƠ SỞ DỮ LIỆU NOSQL)

### 2.1. Vai trò trong dự án
DynamoDB đóng vai trò là cơ sở dữ liệu chính lưu trữ toàn bộ trạng thái của hệ thống giám sát. Dự án sử dụng **3 bảng riêng biệt**:
* `CloudPulse_Monitors`: Lưu thông tin cấu hình và trạng thái sống/chết hiện tại của các website cần theo dõi.
* `CloudPulse_CheckResults`: Lưu dữ liệu lịch sử chuỗi thời gian (Time-series) của từng lần đo đạc độ trễ (latency ms).
* `CloudPulse_Incidents`: Lưu lịch sử các phiên gián đoạn dịch vụ (sự cố mở `OPEN`, đóng `RESOLVED`, thời gian chết).

### 2.2. Các bước thiết lập trong `template.yaml`
```yaml
MonitorsTable:
  Type: AWS::DynamoDB::Table
  Properties:
    TableName: CloudPulse_Monitors
    BillingMode: PAY_PER_REQUEST
    AttributeDefinitions:
      - AttributeName: monitorId
        AttributeType: S
    KeySchema:
      - AttributeName: monitorId
        KeyType: HASH

CheckResultsTable:
  Type: AWS::DynamoDB::Table
  Properties:
    TableName: CloudPulse_CheckResults
    BillingMode: PAY_PER_REQUEST
    AttributeDefinitions:
      - AttributeName: monitorId
        AttributeType: S
      - AttributeName: checkedAt
        AttributeType: S
    KeySchema:
      - AttributeName: monitorId
        KeyType: HASH
      - AttributeName: checkedAt
        KeyType: RANGE
    TimeToLiveSpecification:
      AttributeName: expireAt
      Enabled: true

IncidentsTable:
  Type: AWS::DynamoDB::Table
  Properties:
    TableName: CloudPulse_Incidents
    BillingMode: PAY_PER_REQUEST
    AttributeDefinitions:
      - AttributeName: monitorId
        AttributeType: S
      - AttributeName: incidentId
        AttributeType: S
    KeySchema:
      - AttributeName: monitorId
        KeyType: HASH
      - AttributeName: incidentId
        KeyType: RANGE
```

### 2.3. Giải thích chi tiết từng bước & Ý nghĩa kiến trúc

#### Bước 1: Chọn chế độ thanh toán `BillingMode: PAY_PER_REQUEST`
* **Ý nghĩa**: Đây là chế độ On-Demand Capacity của DynamoDB.
* **Tại sao làm vậy**:
  * Chế độ truyền thống (`PROVISIONED`) bắt buộc bạn phải ấn định trước số Read/Write Capacity Units (RCU/WCU). Nếu lưu lượng thấp, bạn vẫn phải trả tiền cho số dung lượng rảnh rỗi đó; nếu lưu lượng đột biến, hệ thống sẽ bị lỗi `ProvisionedThroughputExceededException` (nghẽn cổ chai).
  * Với `PAY_PER_REQUEST`, AWS chỉ tính tiền trên từng request đọc/ghi thực tế phát sinh. Khi không có ai truy cập, chi phí = **$0.00**. Ngoài ra, AWS tặng miễn phí 25 WCU và 25 RCU trọn đời cho mỗi tài khoản, giúp dự án hoàn toàn miễn phí.

#### Bước 2: Thiết kế Khóa chính (Key Schema: HASH vs RANGE)
* **Bảng `MonitorsTable` chỉ cần HASH (`monitorId`)**:
  * `monitorId` là UUID duy nhất định danh một website. DynamoDB sử dụng thuật toán băm (hashing) trên `monitorId` để phân bổ dữ liệu đồng đều trên các phân vùng vật lý (Physical Partitions), loại bỏ hiện tượng điểm nóng (Hot Partition).
* **Bảng `CheckResultsTable` dùng Khóa kết hợp: HASH (`monitorId`) + RANGE (`checkedAt`)**:
  * HASH (`monitorId`): Gom tất cả lịch sử đo của cùng một website vào chung một phân vùng logic.
  * RANGE (`checkedAt`): Sắp xếp các lần đo theo thời gian (chuỗi ISO 8601 UTC).
  * *Ý nghĩa*: Khi người dùng mở biểu đồ độ trễ, câu lệnh `Query` chỉ mất **5ms** để quét ngược (ScanIndexForward = false) lấy ra đúng 30 lần đo gần nhất mà không cần quét toàn bộ bảng (`Scan`).

#### Bước 3: Cấu hình Time-To-Live (`TimeToLiveSpecification`)
* **Ý nghĩa**: Bật tính năng tự hủy bản ghi dựa trên trường số `expireAt` (định dạng Unix Epoch Timestamp tính bằng giây).
* **Tại sao làm vậy**: Cứ mỗi 1 phút cron ping lại sinh ra 1 bản ghi mới cho mỗi website. Sau vài tháng, số lượng bản ghi sẽ lên hàng triệu dòng. Khi lưu dữ liệu, code backend tính: `expireAt = checkedAt + 7 days`.
* **Nếu không làm thì sao?**: Bảng dữ liệu sẽ ngày càng phình to, vượt quá 25GB miễn phí của AWS, khiến nhóm phải trả tiền lưu trữ hàng tháng hoặc tốn công viết một cron job khác để quét và xóa rác thủ công. TTL giúp AWS tự động xóa rác ngầm dưới nền hoàn toàn miễn phí (Zero Data Bloat).

---

## 3. DỊCH VỤ 2: AWS LAMBDA (TÍNH TOÁN SERVERLESS VỚI .NET 8)

### 3.1. Vai trò trong dự án
Lambda là trung tâm tính toán thực thi mọi logic của CloudPulse mà không cần quản lý bất kỳ hệ điều hành (OS), máy chủ hay container nào. Dự án có 3 hàm:
1. `CloudPulse-Api`: Xử lý các API CRUD từ Dashboard.
2. `CloudPulse-HealthChecker`: Chạy nền theo chu kỳ 60 giây, thực hiện ping HTTP/HTTPS và tính toán State Machine.
3. `CloudPulse-MockTarget`: Mô phỏng website mục tiêu để diễn tập lỗi.

### 3.2. Các bước thiết lập trong `template.yaml`
```yaml
Globals:
  Function:
    Timeout: 30
    MemorySize: 256
    Runtime: dotnet8
    Architectures:
      - x86_64
    Environment:
      Variables:
        MONITORS_TABLE: !Ref MonitorsTable
        CHECK_RESULTS_TABLE: !Ref CheckResultsTable
        INCIDENTS_TABLE: !Ref IncidentsTable

Resources:
  ApiFunction:
    Type: AWS::Serverless::Function
    Properties:
      FunctionName: CloudPulse-Api
      Handler: CloudPulse.Api
      CodeUri: ./backend/src/CloudPulse.Api/
      Policies:
        - DynamoDBCrudPolicy:
            TableName: !Ref MonitorsTable
        - DynamoDBCrudPolicy:
            TableName: !Ref CheckResultsTable
        - DynamoDBCrudPolicy:
            TableName: !Ref IncidentsTable

  HealthCheckerFunction:
    Type: AWS::Serverless::Function
    Properties:
      FunctionName: CloudPulse-HealthChecker
      Handler: CloudPulse.HealthChecker::CloudPulse.HealthChecker.Function::FunctionHandler
      CodeUri: ./backend/src/CloudPulse.HealthChecker/
      Environment:
        Variables:
          SNS_TOPIC_ARN: !Ref CloudPulseAlertsTopic
          CONSECUTIVE_FAILURES_THRESHOLD: "2"
          TIMEOUT_MS: "5000"
          TELEGRAM_BOT_TOKEN: !Ref TelegramBotToken
          TELEGRAM_CHAT_ID: !Ref TelegramChatId
      Policies:
        - DynamoDBCrudPolicy:
            TableName: !Ref MonitorsTable
        - DynamoDBCrudPolicy:
            TableName: !Ref CheckResultsTable
        - DynamoDBCrudPolicy:
            TableName: !Ref IncidentsTable
        - SNSPublishMessagePolicy:
            TopicName: !GetAtt CloudPulseAlertsTopic.TopicName
```

### 3.3. Giải thích chi tiết từng bước & Ý nghĩa kiến trúc

#### Bước 1: Sử dụng khối `Globals`
* **Ý nghĩa**: Định nghĩa các thuộc tính chuẩn áp dụng chung cho tất cả các hàm Lambda trong dự án.
* **Các thông số then chốt**:
  * `Runtime: dotnet8`: Sử dụng phiên bản Long-Term Support (LTS) mới nhất của .NET với công nghệ JIT nâng cao, giúp giảm Cold Start đáng kể so với .NET 6/7.
  * `MemorySize: 256 MB`: Mức RAM 256MB là điểm cân bằng vàng (sweet spot) trên AWS Lambda. Trên AWS, dung lượng CPU được cấp phát tỷ lệ thuận với dung lượng RAM. Ở mức 128MB, .NET runtime khởi động khá chậm; nâng lên 256MB giúp tăng gấp đôi sức mạnh CPU, rút ngắn thời gian Cold Start xuống một nửa trong khi chi phí vẫn nằm trọn trong 3.2 triệu giây tính toán miễn phí mỗi tháng.
  * `Timeout: 30 seconds`: Đảm bảo hàm có đủ thời gian hoàn tất các request song song (`Task.WhenAll`) mà không bị AWS cưỡng chế ngắt (kill) đột ngột giữa chừng.

#### Bước 2: Thiết lập Biến Môi Trường (`Environment Variables`)
* **Ý nghĩa**: Tách rời cấu hình ra khỏi mã nguồn (tuân thủ nguyên tắc Twelve-Factor App).
* **Tại sao làm vậy**:
  * Các tên bảng như `!Ref MonitorsTable` hoặc ARN như `!Ref CloudPulseAlertsTopic` được CloudFormation tự động sinh ra và truyền vào Lambda lúc deploy.
  * Code C# chỉ cần đọc `Environment.GetEnvironmentVariable("MONITORS_TABLE")`. Nhờ đó, nếu bạn triển khai sang môi trường Staging hay Production, code không cần phải sửa hay build lại.

#### Bước 3: Đặt Lambda ngoài VPC (Non-VPC Lambda)
* **Ý nghĩa**: Không cấu hình `VpcConfig` cho các hàm Lambda.
* **Lý do chuyên sâu**:
  * Hàm `HealthChecker` bắt buộc phải gửi request ra ngoài Internet để kiểm tra trạng thái các website của khách hàng.
  * Nếu đặt Lambda vào một VPC riêng, Lambda sẽ mất toàn bộ kết nối Internet và bắt buộc phải đi qua một **AWS NAT Gateway**. Một NAT Gateway có chi phí tối thiểu **~$32.40 USD/tháng** (chưa tính phí băng thông), điều này sẽ phá vỡ mục tiêu FinOps $0.
  * Lambda Non-VPC giao tiếp với DynamoDB và SNS thông qua hạ tầng mạng nội bộ bảo mật của AWS (**AWS Internal Backbone**) bằng mã hóa SigV4, đảm bảo an toàn tuyệt đối mà không tốn một đồng chi phí mạng nào.

---

## 4. DỊCH VỤ 3: AMAZON API GATEWAY V2 (HTTP API GATEWAY)

### 4.1. Vai trò trong dự án
Đóng vai trò là "Cửa khẩu ngõ vào" duy nhất cho toàn bộ hệ thống API. Nó tiếp nhận các lệnh từ giao diện Dashboard trên trình duyệt (`GET /api/monitors`, `POST /api/monitors`, `POST /api/monitors/{id}/check`), kiểm tra bảo mật, xử lý CORS và ủy quyền cho hàm Lambda `CloudPulse-Api` thực thi.

### 4.2. Các bước thiết lập trong `template.yaml`
```yaml
HttpApi:
  Type: AWS::Serverless::HttpApi
  Properties:
    Name: CloudPulse-HttpApi
    CorsConfiguration:
      AllowMethods:
        - GET
        - POST
        - DELETE
        - OPTIONS
      AllowHeaders:
        - "*"
      AllowOrigins:
        - "*"

ApiFunction:
  Type: AWS::Serverless::Function
  Properties:
    # ...
    Events:
      HttpApiGet:
        Type: HttpApi
        Properties:
          ApiId: !Ref HttpApi
          Path: /{proxy+}
          Method: GET
      HttpApiPost:
        Type: HttpApi
        Properties:
          ApiId: !Ref HttpApi
          Path: /{proxy+}
          Method: POST
      HttpApiDelete:
        Type: HttpApi
        Properties:
          ApiId: !Ref HttpApi
          Path: /{proxy+}
          Method: DELETE
```

### 4.3. Giải thích chi tiết từng bước & Ý nghĩa kiến trúc

#### Bước 1: Chọn HTTP API (v2) thay vì REST API (v1)
* **Ý nghĩa**: AWS cung cấp 2 loại API Gateway: REST API cổ điển và HTTP API thế hệ 2.
* **Tại sao chọn HTTP API**:
  * Độ trễ cực thấp: HTTP API được thiết kế tối giản, loại bỏ các bước xử lý mapping template cồng kềnh, giảm **~70% độ trễ xử lý (overhead)** so với REST API.
  * Tiết kiệm chi phí: Rẻ hơn 30% so với REST API (với CloudPulse, HTTP API miễn phí 1 triệu request đầu tiên mỗi tháng).

#### Bước 2: Cấu hình Native CORS trực tiếp trên API Gateway
* **Ý nghĩa**: Cross-Origin Resource Sharing (CORS) là cơ chế bảo mật của trình duyệt chặn các request gọi từ domain này (`clpulse.cloud`) sang domain khác (`*.execute-api.amazonaws.com`).
* **Tại sao làm vậy**: Cấu hình `CorsConfiguration` giúp API Gateway tự động gắn các header:
  `Access-Control-Allow-Origin: *`  
  `Access-Control-Allow-Methods: GET, POST, DELETE, OPTIONS`  
  `Access-Control-Allow-Headers: *`

#### Bước 3: Khai báo Explicit Method (GET, POST, DELETE) thay vì dùng `ANY`
* **Đây là một kỹ thuật xử lý lỗi CORS kinh điển**:
  * Khi trình duyệt gửi request có thay đổi dữ liệu (POST, DELETE), trình duyệt luôn tự động gửi trước 1 request thăm dò gọi là **Preflight Request (`OPTIONS`)**.
  * Nếu bạn cấu hình `Method: ANY`, API Gateway sẽ chuyển tiếp cả request `OPTIONS` này vào Lambda. Lúc này nếu ASP.NET Core không xử lý kịp hoặc bị Cold Start, request sẽ bị timeout và trình duyệt sẽ báo lỗi **CORS Policy Error**.
  * Bằng cách chỉ định rõ `Method: GET`, `Method: POST`, `Method: DELETE`, API Gateway nhận biết rằng method `OPTIONS` không cần vào Lambda. Do đó, API Gateway sẽ **tự động phản hồi HTTP 204 No Content ngay lập tức tại tầng Gateway** trong 1ms. Điều này giúp loại bỏ 100% lỗi CORS trên Dashboard và tiết kiệm 50% số lần gọi Lambda không cần thiết!

---

## 5. DỊCH VỤ 4: AMAZON EVENTBRIDGE (BỘ ĐIỀU PHỐI LẬP LỊCH EVENT-DRIVEN)

### 5.1. Vai trò trong dự án
EventBridge đóng vai trò là chiếc đồng hồ báo thức phát xung nhịp (Heartbeat) của toàn bộ hệ thống giám sát. Cứ mỗi 60 giây, EventBridge tự động gửi một tín hiệu kích hoạt hàm `HealthCheckerFunction`.

### 5.2. Các bước thiết lập trong `template.yaml`
```yaml
HealthCheckerFunction:
  Type: AWS::Serverless::Function
  Properties:
    # ...
    Events:
      EveryMinuteSchedule:
        Type: Schedule
        Properties:
          Schedule: rate(1 minute)
          Enabled: true
```

### 5.3. Giải thích chi tiết từng bước & Ý nghĩa kiến trúc

#### Bước 1: Khai báo `Type: Schedule` với biểu thức `rate(1 minute)`
* **Ý nghĩa**: Khởi tạo một EventBridge Rule chạy đều đặn mỗi 1 phút một lần.
* **Tại sao làm vậy**:
  * So với việc viết cron Linux truyền thống (`* * * * *`), cú pháp `rate(1 minute)` của AWS ngắn gọn, trực quan và không bị nhầm lẫn giữa múi giờ UTC và giờ địa phương.
  * Tách rời hoàn toàn (Decoupling): Động cơ kiểm tra không cần phải chạy vòng lặp vô tận `while(true) { Thread.Sleep(60000); }` trên một máy chủ EC2. Khi hết 1 phút, EventBridge đánh thức Lambda; Lambda chạy xong trong 2 giây rồi tự hủy giải phóng tài nguyên.

#### Bước 2: Cơ chế Gọi Bất Đồng Bộ (Asynchronous Invocation) & Xử lý Idempotency
* **Nguyên lý hoạt động**: EventBridge kích hoạt Lambda theo mô hình Asynchronous. Khi gửi event đi, EventBridge chỉ cần nhận mã xác nhận `HTTP 202 Accepted` từ hạ tầng Lambda là coi như thành công.
* **Vấn đề At-least-once Delivery**:
  * Trong điện toán phân tán, EventBridge cam kết phân phối sự kiện theo nguyên tắc **Ít nhất một lần (At-least-once)**. Trong trường hợp có sự cố mạng vi mô giữa các phân vùng của AWS, EventBridge có thể kích hoạt Lambda 2 lần trong cùng một chu kỳ.
  * **Giải pháp của CloudPulse**: Hàm `HealthChecker` lưu bản ghi kết quả đo vào DynamoDB với khóa chính kết hợp `(monitorId, checkedAt)`. Nhờ tính chất **Idempotent (tính lũy thừa)** của thao tác `PutItem`, việc ghi đè bản ghi trùng lặp tại cùng một thời điểm hoàn toàn không làm sai lệch số liệu và State Machine của hệ thống chỉ gửi cảnh báo đúng 1 lần khi trạng thái thực sự chuyển sang `DOWN`.

---

## 6. DỊCH VỤ 5: AMAZON SNS (SIMPLE NOTIFICATION SERVICE - EMAIL FAN-OUT)

### 6.1. Vai trò trong dự án
Amazon SNS cung cấp kênh phát tán cảnh báo sự cố chuẩn doanh nghiệp theo mô hình **Xuất bản - Đăng ký (Publish/Subscribe - Pub/Sub)**. Khi một website được xác nhận là bị sập (`DOWN`), Lambda chỉ cần gửi duy nhất 1 thông điệp vào SNS Topic; SNS sẽ tự động nhân bản và gửi email đồng loạt đến toàn bộ danh sách quản trị viên.

### 6.2. Các bước thiết lập trong `template.yaml`
```yaml
Parameters:
  AdminEmail:
    Type: String
    Description: Admin email address for CloudPulse downtime alert notifications
    Default: "huuquyet120406@gmail.com"

Resources:
  CloudPulseAlertsTopic:
    Type: AWS::SNS::Topic
    Properties:
      TopicName: CloudPulse_Alerts

  AlertEmailSubscription:
    Type: AWS::SNS::Subscription
    Properties:
      TopicArn: !Ref CloudPulseAlertsTopic
      Protocol: email
      Endpoint: !Ref AdminEmail
```

### 6.3. Giải thích chi tiết từng bước & Ý nghĩa kiến trúc

#### Bước 1: Khai báo `AWS::SNS::Subscription` trực tiếp trong file IaC
* **Ý nghĩa**: Tự động hóa đăng ký email nhận tin vào Topic.
* **Tại sao làm vậy**:
  * Nhiều bạn sinh viên thường lên Web Console để bấm nút "Create Subscription" bằng tay. Cách làm đó vi phạm tiêu chuẩn IaC vì nếu sang môi trường mới hoặc tài khoản khác, bạn sẽ quên cấu hình bước này.
  * Khai báo trong `template.yaml` giúp CloudFormation tự động liên kết hòm thư `AdminEmail` vào Topic ngay khi chạy lệnh `sam deploy`.

#### Bước 2: Quy trình Xác thực Subscription (Subscription Handshake)
* **Ý nghĩa an toàn**: AWS SNS không bao giờ tự ý gửi email rác đến người dùng mà chưa có sự đồng thuận (Opt-in).
* **Quy trình thực tế**:
  1. Khi stack CloudFormation tạo tài nguyên `AlertEmailSubscription`, trạng thái của subscription trên AWS sẽ là `PendingConfirmation`.
  2. AWS gửi 1 email tự động từ địa chỉ `no-reply@sns.amazonaws.com` với tiêu đề: *"AWS Notification - Subscription Confirmation"*.
  3. Quản trị viên mở email và bấm vào liên kết **Confirm subscription**.
  4. Trạng thái chuyển sang `Confirmed`. Kể từ thời điểm này, bất kỳ sự cố nào được Lambda phát hiện đều sẽ bắn email về hòm thư này trong vòng 2 giây.

---

## 7. DỊCH VỤ 6: AMAZON S3 (LƯU TRỮ & PHÂN PHỐI WEBSITE TĨNH SPA)

### 7.1. Vai trò trong dự án
Lưu trữ toàn bộ sản phẩm build của ứng dụng Single Page Application (React 18 + Vite) gồm các file: `index.html`, các file nén Javascript/CSS trong thư mục `assets/` và biểu tượng `favicon.svg`. S3 đóng vai trò là Origin Server cung cấp giao diện trực quan cho người quản trị.

### 7.2. Các bước thiết lập qua AWS CLI
*(Thực hiện tại thư mục gốc của dự án)*:

```bash
# Bước 1: Tạo S3 Bucket tại region ap-southeast-1
aws s3 mb s3://cloudpulse-dashboard-tv1 --region ap-southeast-1

# Bước 2: Tắt toàn bộ 4 tầng khóa Public Access của AWS S3
aws s3api put-public-access-block --bucket cloudpulse-dashboard-tv1 \
  --public-access-block-configuration "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false"

# Bước 3: Cấu hình Bucket Policy cho phép người dùng Internet đọc file
cat <<EOF > /tmp/s3-policy.json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::cloudpulse-dashboard-tv1/*"
    }
  ]
}
EOF
aws s3api put-bucket-policy --bucket cloudpulse-dashboard-tv1 --policy file:///tmp/s3-policy.json

# Bước 4: Bật tính năng Static Website Hosting cho Bucket
aws s3 website s3://cloudpulse-dashboard-tv1 \
  --index-document index.html \
  --error-document index.html

# Bước 5: Build mã nguồn Frontend và đồng bộ lên S3
cd frontend
npm run build
aws s3 sync dist s3://cloudpulse-dashboard-tv1 --delete
```

### 7.3. Giải thích chi tiết từng bước & Ý nghĩa kiến trúc

#### Bước 1 & 2: Tại sao phải tắt `BlockPublicAccess`?
* Mặc định từ năm 2023, AWS tự động bật tính năng "Block Public Access" trên tất cả S3 bucket mới tạo để chống lộ dữ liệu riêng tư.
* Tuy nhiên, vì đây là một Website công khai (Public Dashboard), chúng ta bắt buộc phải cấu hình `BlockPublicPolicy=false` thì mới có thể gắn quyền `s3:GetObject` cho người dùng toàn cầu truy cập tải giao diện web.

#### Bước 3: Tại sao cấu hình `Error Document` trỏ về `index.html`?
* **Đây là điểm cốt lõi của ứng dụng Single Page Application (SPA - React Router)**:
  * Khi người dùng truy cập trực tiếp vào một đường dẫn con như `https://clpulse.cloud/monitors` hoặc bấm F5 tải lại trang, S3 về bản chất không có thư mục vật lý nào tên là `/monitors`. Nếu để mặc định, S3 sẽ trả về lỗi **404 Not Found**.
  * Bằng cách đặt `--error-document index.html`, khi S3 không tìm thấy file con, nó sẽ tự động trả về tệp `index.html`. Sau đó, React Router bên trong trình duyệt của người dùng sẽ nhận diện URL và tự động hiển thị đúng trang tương ứng một cách mượt mà.

#### Bước 4: Tùy chọn `--delete` trong lệnh `aws s3 sync`
* Tùy chọn `--delete` đảm bảo tính đồng bộ tuyệt đối: nếu trong lần build mới bạn xóa bỏ hoặc đổi tên một file CSS/JS cũ, AWS CLI sẽ tự động xóa các file rác cũ đó trên S3, tránh tình trạng xung đột phiên bản mã nguồn trong cache trình duyệt.

---

## 8. DỊCH VỤ 7: CLOUDFLARE (DNS, EDGE CDN, SSL FLEXIBLE & CLOUD CONNECTOR)

### 8.1. Vai trò trong dự án
Cloudflare đóng vai trò là **Reverse Proxy và Tường lửa biên (Edge Shield)** đứng chắn phía trước AWS S3. Nó mang lại tên miền thương hiệu chính thức `https://clpulse.cloud`, chứng chỉ bảo mật SSL miễn phí và mạng lưới CDN phủ sóng toàn cầu.

### 8.2. Các bước thiết lập thực tế trên Cloudflare Dashboard

#### Bước 1: Quản lý DNS & Cấu hình Nameserver
1. Đăng ký tên miền `clpulse.cloud` trên nhà cung cấp tên miền.
2. Trỏ Nameservers của domain về 2 máy chủ DNS được Cloudflare chỉ định.
3. Tạo bản ghi DNS (DNS Record):
   * Type: `CNAME`
   * Name: `@` (hoặc `clpulse.cloud`)
   * Target: `cloudpulse-dashboard-tv1.s3-website-ap-southeast-1.amazonaws.com`
   * Proxy status: **Proxied (Đám mây màu cam)**.

#### Bước 2: Cấu hình SSL/TLS Chế độ "Flexible"
* Đường dẫn: **SSL/TLS** -> **Overview** -> Chọn **Flexible**.
* **Giải thích nguyên nhân cốt lõi**:
  * Điểm cuối của AWS S3 Static Website Hosting (`s3-website-ap-southeast-1.amazonaws.com`) **chỉ hỗ trợ giao thức HTTP qua cổng 80**, không hỗ trợ HTTPS cổng 443 trực tiếp.
  * Nếu chọn chế độ `Full` hoặc `Full (Strict)`, Cloudflare sẽ cố gắng kết nối tới S3 bằng HTTPS cổng 443 ➔ S3 từ chối kết nối ➔ Cloudflare trả về lỗi kinh điển **Error 522 Connection Timed Out**.
  * Chọn **Flexible** giải quyết hoàn hảo: Người dùng từ Internet kết nối vào Cloudflare bằng **HTTPS cổng 443** (có ổ khóa xanh bảo mật), còn Cloudflare kết nối về S3 Origin qua **HTTP cổng 80**.

#### Bước 3: Cấu hình Cloudflare Cloud Connector
* Đường dẫn: **Rules** -> **Cloud Connector** -> **Configure Cloud Connector**:
  * Tên: `cloudpulse-s3-connector`
  * Rule: Bấm chọn radio **`All incoming requests`**
  * Target Bucket: Trỏ vào endpoint S3 website của dự án.
* **Giải thích nguyên nhân cốt lõi**:
  * Khi người dùng truy cập `https://clpulse.cloud`, Cloudflare mặc định sẽ chuyển tiếp HTTP Header `Host: clpulse.cloud` về cho S3.
  * S3 sử dụng Host Header để xác định xem người dùng muốn truy cập vào bucket nào. Vì S3 thấy Host Header là `clpulse.cloud` chứ không phải `cloudpulse-dashboard-tv1`, S3 sẽ lập tức trả về lỗi: **404 NoSuchBucket: The specified bucket does not exist**.
  * Tính năng **Cloud Connector** tự động ghi đè và điều hướng toàn bộ traffic của tên miền vào đúng bucket `cloudpulse-dashboard-tv1`, giúp website hoạt động trơn tru.

---

## 9. DỊCH VỤ 8: TELEGRAM BOT API (KÊNH CẢNH BÁO TỨC THỜI ĐA KÊNH)

### 9.1. Vai trò trong dự án
Bên cạnh Amazon SNS (Email) mang tính chất lưu trữ hồ sơ, Telegram Bot mang lại khả năng **phản ứng nhanh trong tình huống khẩn cấp**: đẩy thông báo tức thời nảy chuông và rung điện thoại của đội ngũ kỹ sư trực vận hành trong vòng **dưới 1 giây**.

### 9.2. Các bước thiết lập thực tế

#### Bước 1: Tạo Bot trên Telegram
1. Mở ứng dụng Telegram, tìm kiếm tài khoản chính thức **`@BotFather`**.
2. Gõ lệnh `/newbot`, đặt tên cho bot (ví dụ: `CloudPulse Alerts`) và username (ví dụ: `cloudpulse_alerts_bot`).
3. Nhận mã xác thực bí mật: **`TELEGRAM_BOT_TOKEN`** (chuỗi ký tự dạng `123456789:ABCdefGhIJK...`).

#### Bước 2: Tạo Group Chat & Lấy Chat ID
1. Tạo một nhóm chat trên Telegram đặt tên là **CloudPulse Alerts Group**.
2. Thêm bot vừa tạo vào nhóm chat và cấp quyền gửi tin nhắn.
3. Gửi một tin nhắn bất kỳ vào nhóm (ví dụ: `/start` hoặc `hello`).
4. Mở trình duyệt truy cập đường dẫn:
   `https://api.telegram.org/bot<TELEGRAM_BOT_TOKEN>/getUpdates`
5. Tìm kiếm trường `"chat":{"id": -100xxxxxxxxxx,...}`. Chuỗi số âm bắt đầu bằng dấu trừ này chính là **`TELEGRAM_CHAT_ID`**.

#### Bước 3: Bảo mật Token trong `template.yaml`
```yaml
Parameters:
  TelegramBotToken:
    Type: String
    Description: Telegram Bot Token from @BotFather
    NoEcho: true
    Default: ""
  TelegramChatId:
    Type: String
    Description: Telegram Group Chat ID to receive instant alerts
    Default: ""
```
* **Ý nghĩa thuộc tính `NoEcho: true`**:
  * Đây là tiêu chuẩn bảo mật điện toán đám mây bắt buộc.
  * Khi cấu hình `NoEcho: true`, CloudFormation sẽ ẩn mã Token bí mật (hiển thị dưới dạng dấu `*****`) trên AWS Management Console, trong nhật ký CloudTrail và trong các bản xuất file cấu hình, ngăn chặn việc lộ lọt mã điều khiển Bot ra bên ngoài.

---

## 10. DỊCH VỤ 9: AWS IAM (QUẢN LÝ QUYỀN THEO NGUYÊN TẮC LEAST PRIVILEGE)

### 10.1. Vai trò trong dự án
Đảm bảo ranh giới an ninh tuyệt đối cho toàn bộ hệ thống. Mọi hàm Lambda chỉ được cấp đúng những quyền tối thiểu cần thiết để thực thi nhiệm vụ được giao, không thừa không thiếu.

### 10.2. Cấu hình thực tế trong dự án
Thay vì tạo các IAM Role với quyền quản trị rộng (`AdministratorAccess` hay `PowerUserAccess`), dự án sử dụng các **SAM Policy Templates** tiêu chuẩn:

```yaml
# Hàm API: Chỉ được đọc ghi dữ liệu trên 3 bảng của dự án
Policies:
  - DynamoDBCrudPolicy:
      TableName: !Ref MonitorsTable
  - DynamoDBCrudPolicy:
      TableName: !Ref CheckResultsTable
  - DynamoDBCrudPolicy:
      TableName: !Ref IncidentsTable

# Hàm HealthChecker: Ngoài 3 bảng DynamoDB, chỉ có thêm quyền bắn tin vào SNS Topic
Policies:
  # ... 3 DynamoDBCrudPolicy ...
  - SNSPublishMessagePolicy:
      TopicName: !GetAtt CloudPulseAlertsTopic.TopicName
```

### 10.3. Ý nghĩa kiến trúc & Điểm cộng bảo vệ đồ án
* **Hàm `MockTargetFunction`**: Hoàn toàn **không được khai báo khối `Policies`**. Nó chỉ nhận quyền cơ bản `AWSLambdaBasicExecutionRole` (để ghi log ra CloudWatch). Hàm này không có bất kỳ quyền nào để đọc trộm bảng DynamoDB hay gửi tin vào SNS.
* **Nguyên tắc cô lập rủi ro (Blast Radius Reduction)**: Nếu một kẻ tấn công tìm được lỗ hổng trong hàm Mock, chúng hoàn toàn không thể sử dụng quyền hạn của hàm đó để truy cập cơ sở dữ liệu hay gửi thư rác giả mạo từ hệ thống của bạn.

---

## 11. DỊCH VỤ 10: AMAZON CLOUDWATCH (GHI VẾT LOG & GIÁM SÁT VẬN HÀNH)

### 11.1. Vai trò trong dự án
Cung cấp khả năng quan sát toàn diện (**Observability**). Khi Lambda thực thi, mọi câu lệnh ghi log (`Console.WriteLine` hoặc `ILogger`) cùng các thông số phần cứng đều được tự động đẩy về CloudWatch Logs.

### 11.2. Cách thức hoạt động & Khai thác thực tế
1. **Tự động tạo Log Groups**: Khi triển khai bằng SAM, AWS tự động tạo 3 nhóm log:
   * `/aws/lambda/CloudPulse-Api`
   * `/aws/lambda/CloudPulse-HealthChecker`
   * `/aws/lambda/CloudPulse-MockTarget`
2. **Dòng Log Báo Cáo Chuẩn (REPORT Line)**:
   Cuối mỗi lần thực thi của Lambda, CloudWatch tự động xuất 1 dòng thống kê dạng:
   ```text
   REPORT RequestId: 8a7b3c2d... Duration: 412.50 ms Billed Duration: 413 ms Memory Size: 256 MB Max Memory Used: 78 MB
   ```
   * *Ý nghĩa*: Dựa vào chỉ số `Max Memory Used: 78 MB`, nhóm biết rằng cấu hình RAM 256MB là hoàn toàn dư dả và không bao giờ bị tràn bộ nhớ (Out of Memory).
3. **Ứng cứu Sự cố Thực tế (Incident RCA)**:
   * Khi tài khoản gặp tình trạng KYC Verification Hold từ AWS Trust & Safety, việc kiểm tra log CloudWatch đã giúp nhóm tìm ra chính xác ngoại lệ `Amazon.Lambda.Model.ServiceException: AccessDeniedException: None` lúc 20:46, chứng minh lỗi không bắt nguồn từ mã nguồn mà do cơ chế kiểm soát gian lận tự động của AWS.

---

## 12. DỊCH VỤ 11: AWS SAM & CLOUDFORMATION (INFRASTRUCTURE AS CODE - IAC)

### 12.1. Vai trò trong dự án
Bộ công cụ quản lý vòng đời hạ tầng (IaC Lifecycle). Biến toàn bộ sơ đồ kiến trúc phức tạp thành một tệp mã nguồn duy nhất `template.yaml`.

### 12.2. Quy trình làm việc chuẩn 3 bước

```bash
# Bước 1: Kiểm tra tính hợp lệ về mặt cú pháp
sam validate

# Bước 2: Biên dịch các dự án C# .NET 8 và đóng gói artifact
sam build

# Bước 3: Triển khai hạ tầng lên AWS Cloud
sam deploy --guided
```

### 12.3. Giải thích cơ chế bên dưới (Under the hood)
1. Lệnh `sam validate`: Kiểm tra cấu trúc ngữ pháp của file `template.yaml` so với JSON Schema của AWS Serverless Application Model, cảnh báo ngay lập tức nếu thụt đầu dòng (indentation) sai hoặc thiếu thuộc tính bắt buộc.
2. Lệnh `sam build`: Chạy trình biên dịch `dotnet publish -c Release` cho từng project trong thư mục `backend/src/`, sinh ra các file `.dll` tối ưu hóa và gom vào thư mục ẩn `.aws-sam/build/`.
3. Lệnh `sam deploy`:
   * Chuyển đổi mã SAM thành tệp mẫu chuẩn **AWS CloudFormation**.
   * Nén mã nguồn tải lên một S3 Bucket trung gian.
   * Tạo một **CloudFormation ChangeSet** (bản tóm tắt các tài nguyên sẽ được tạo mới hoặc sửa đổi).
   * Thực thi tạo tài nguyên tuần tự và tự động liên kết các biến phụ thuộc (`!Ref`, `!GetAtt`).

---

## 13. BẢNG TỔNG KẾT KHẮC PHỤC SỰ CỐ THỰC TẾ (TROUBLESHOOTING MATRIX)

Bảng tổng hợp các lỗi kinh điển mà nhóm đã gặp phải trong quá trình thiết lập và giải pháp xử lý triệt để:

| Dịch vụ | Mã Lỗi / Triệu chứng | Nguyên nhân Gốc rễ | Giải pháp Kỹ thuật Đã Thực Hiện |
| :--- | :--- | :--- | :--- |
| **Cloudflare & S3** | **Error 522 Connection Timed Out** | Cloudflare để SSL chế độ `Full`, cố kết nối tới S3 qua HTTPS cổng 443 trong khi S3 Website chỉ hỗ trợ HTTP cổng 80. | Chuyển chế độ mã hóa SSL/TLS trên Cloudflare sang **Flexible**. |
| **Cloudflare & S3** | **404 NoSuchBucket** | S3 kiểm tra Host Header thấy `clpulse.cloud` không trùng với tên bucket `cloudpulse-dashboard-tv1`. | Thiết lập tính năng **Cloud Connector** trên Cloudflare trỏ tất cả incoming requests về đúng S3 endpoint. |
| **API Gateway & SPA** | **CORS Policy Error** khi gọi API | Trình duyệt gửi preflight request `OPTIONS` nhưng hàm Lambda xử lý chậm hoặc cấu hình method `ANY` bị nghẽn. | Khai báo rõ ràng từng method `GET, POST, DELETE` trên SAM; API Gateway tự động trả về `204 No Content` cho `OPTIONS`. |
| **DynamoDB** | Nguy cơ phình to dung lượng sau nhiều tháng | Cron ping định kỳ mỗi 1 phút ghi hàng trăm nghìn bản ghi kiểm tra mới. | Cấu hình thuộc tính **Time-To-Live (TTL)** trên trường `expireAt`, tự hủy dữ liệu cũ sau 7 ngày hoàn toàn miễn phí. |
| **AWS Lambda** | **AccessDeniedException: None** | Hệ thống chống gian lận tự động của AWS (Fraud Detection) tạm giữ tài khoản (On Hold) khi thấy cron ping ra ngoài liên tục. | Chuẩn bị hồ sơ KYC (CCCD 2 mặt + Sao kê Techcombank PDF) nộp qua cổng bảo mật `upload.aws.amazon.com`. |
| **Cảnh báo đa kênh** | Bão cảnh báo (Alert Fatigue) | Website chập chờn hoặc sập lâu gửi hàng trăm email và tin nhắn mỗi phút. | Xây dựng **State Machine** với ngưỡng 2 lần lỗi liên tiếp; chỉ gửi đúng 1 thông báo khi chuyển `DOWN` và 1 thông báo khi `RECOVERED`. |

---
*Tài liệu này được biên soạn độc quyền cho đồ án CloudPulse — Bản quyền học thuật 2026.*
