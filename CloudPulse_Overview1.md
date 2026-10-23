# CloudPulse — Tổng quan đồ án

Hệ thống giám sát website và cảnh báo sự cố tự động trên AWS

- Môn học: Điện toán đám mây (Cloud Computing)
- Ngày cập nhật: 23/10/2026
- Mục đích: Tài liệu tổng quan để nhóm thống nhất ý tưởng và định hướng triển khai.
- Lưu ý: Những nội dung ghi là "đề xuất" hoặc "chưa chốt" cần được nhóm xác nhận trước khi triển khai chính thức.

---

## 1. Tóm tắt đề tài

CloudPulse là hệ thống cho phép người dùng thêm website hoặc API cần giám sát. Các dịch vụ AWS sẽ tự động kiểm tra trạng thái hoạt động và thời gian phản hồi theo lịch, phát hiện dấu hiệu sự cố, gửi email cảnh báo và lưu lịch sử kiểm tra/sự cố.

Người dùng có thể theo dõi website/API qua dashboard, xem trạng thái hiện tại, thống kê uptime, thời gian phản hồi, lịch sử sự cố, log và chi phí Cloud.

CloudPulse tập trung vào việc phát hiện, ghi nhận và cảnh báo sự cố; hệ thống không tự động sửa mã nguồn hoặc khôi phục website thay người quản trị.

## 2. Vấn đề thực tế và mục tiêu

### 2.1. Vấn đề

Người quản trị không thể tự mở website/API để kiểm tra liên tục. Website có thể ngừng phản hồi, trả lỗi máy chủ hoặc phản hồi quá chậm trong lúc không ai theo dõi; sự cố bị phát hiện muộn có thể ảnh hưởng người dùng.

### 2.2. Mục tiêu đã được nhóm nêu

1. Cho phép người dùng thêm website/API cần theo dõi.
2. Tự động kiểm tra định kỳ tình trạng và thời gian phản hồi qua Cloud.
3. Phát hiện lỗi, lưu lịch sử kiểm tra và lịch sử sự cố.
4. Gửi cảnh báo qua email khi website/API gặp sự cố.
5. Có dashboard cho biết trạng thái hiện tại, lịch sử và các thống kê.
6. Demo được vòng đời: bình thường → gây lỗi → phát hiện → cảnh báo → khôi phục → tổng kết.

### 2.3. Ngoài phạm vi được xác nhận

Chưa có quyết định rằng CloudPulse phải: tự chữa lỗi website, giám sát tài nguyên CPU/RAM, triển khai đa vùng, có tài khoản cho nhiều khách hàng, hỗ trợ SMS, AI hay IoT. Những nội dung này không được mặc định là chức năng đồ án.

## 3. Các chức năng đề xuất

### 3.1. Chức năng chính

- Thêm website/API: Cho phép người dùng nhập endpoint cần giám sát.
- Kiểm tra tự động: AWS thực hiện HTTP/HTTPS request định kỳ tới endpoint đã đăng ký.
- Hiển thị trạng thái: Cho biết website/API đang hoạt động (`UP`), gặp sự cố (`DOWN`) hoặc chưa rõ trạng thái (`UNKNOWN`).
- Đo thời gian phản hồi: Ghi nhận response time của mỗi lần kiểm tra.
- Phát hiện sự cố: Nhận biết lỗi HTTP, timeout hoặc trường hợp kết nối thất bại.
- Gửi email cảnh báo: Thông báo cho người nhận khi sự cố được xác nhận.
- Lưu lịch sử: Ghi lại kết quả kiểm tra và các lần xảy ra sự cố.
- Thống kê: Trình bày uptime, response time, log và chi phí Cloud trên dashboard.

### 3.2. Chức năng có thể bổ sung

- Kiểm tra thủ công (`Check now`): Cho phép kích hoạt ngay một lượt kiểm tra để phục vụ demo.
- Cảnh báo hồi phục: Gửi email khi website/API hoạt động trở lại.
- Hạn chế cảnh báo trùng: Chỉ gửi thông báo khi trạng thái thay đổi theo quy tắc đã định, thay vì gửi ở mọi lần lỗi.

Các quy tắc chi tiết, thứ tự ưu tiên và phạm vi phiên bản đầu tiên sẽ được thống nhất ở giai đoạn thiết kế.

## 4. Kịch bản sử dụng minh họa

Giả sử người dùng đăng ký URL `https://example.com/health` (URL chỉ mang tính minh họa):

1. Dashboard cho thấy endpoint UP khi kiểm tra đáp ứng tiêu chí hợp lệ.
2. Người demo chủ động khiến endpoint trả HTTP 500 hoặc hết thời gian chờ (*timeout*).
3. Đến lượt kiểm tra theo lịch (hoặc kiểm tra thủ công nếu có chức năng này), Lambda gọi endpoint và ghi nhận một lượt FAIL.
4. Khi đạt quy tắc xác nhận sự cố (chưa chốt: ví dụ 2 lần lỗi liên tiếp), hệ thống chuyển endpoint thành DOWN, lưu dấu mốc bắt đầu sự cố và kích hoạt email qua SNS.
5. Dashboard thể hiện trạng thái mới, kết quả/lịch sử liên quan.
6. Người demo khôi phục endpoint; lượt kiểm tra tiếp theo hợp lệ và hệ thống ghi nhận đã phục hồi thành UP.
7. Nhóm trình bày thời gian phản hồi, tỷ lệ kiểm tra thành công, khoảng thời gian xảy ra sự cố, CloudWatch logs và dự toán/chi phí Cloud.

Lưu ý: HTTP `500` là một dấu hiệu lỗi rõ ràng; còn việc coi HTTP `3xx`/`4xx` là UP hay DOWN, kiểm tra nội dung response, timeout bao lâu và bao nhiêu lần lỗi liên tiếp mới cảnh báo đều cần được thống nhất khi thiết kế.

## 5. Các dịch vụ AWS dự kiến sử dụng

Nhóm dự kiến dùng bảy dịch vụ AWS sau. Vai trò dưới đây là cách tổ chức kỹ thuật tham khảo, chưa phải thiết kế đã triển khai.

- Amazon EventBridge Scheduler
  - Kích hoạt tác vụ kiểm tra website/API theo lịch.
- AWS Lambda
  - Gửi HTTP/HTTPS request, đo response time, đánh giá kết quả và xử lý trạng thái sự cố.
- Amazon API Gateway
  - Cung cấp API để dashboard thêm website/API, đọc trạng thái và xem lịch sử.
- Amazon DynamoDB
  - Lưu website/API đã đăng ký, kết quả kiểm tra, trạng thái và lịch sử sự cố.
- Amazon Simple Notification Service (SNS)
  - Gửi email cảnh báo đến người nhận đã đăng ký và xác nhận.
- AWS Identity and Access Management (IAM)
  - Cấp quyền tối thiểu cho các thành phần AWS truy cập đúng tài nguyên cần thiết.
- Amazon CloudWatch
  - Thu thập log và metric để theo dõi hệ thống, điều tra lỗi và hỗ trợ trình bày demo.

Chưa chốt: công nghệ frontend, nơi triển khai dashboard, xác thực người dùng, số lượng Lambda, thiết kế DynamoDB, phương án lập lịch và cách quản lý email.



### 6. Hai phương án lập lịch cần cân nhắc

- Phương án A — Một lịch chung: Scheduler gọi Lambda định kỳ; Lambda đọc danh sách endpoint từ DynamoDB và kiểm tra những mục đến hạn. Dễ demo ở quy mô nhỏ nhưng phải tính thời gian xử lý và giới hạn thực thi.
- Phương án B — Lịch riêng theo website: Mỗi endpoint có lịch kích hoạt phù hợp. Dễ hiểu từng mục nhưng cần quản lý vòng đời lịch (tạo, sửa, xóa), quyền và chi phí.

Chưa lựa chọn phương án chính thức. Với demo ít website, nhóm có thể ưu tiên giải pháp đơn giản nhất đáp ứng đề bài.

## 7. Vòng đời trạng thái và quy tắc cảnh báo đề xuất

Để tránh nhầm lẫn giữa một lượt kiểm tra lỗi và một sự cố đã xác nhận, nên tách hai khái niệm:

- Check result: `SUCCESS` hoặc `FAIL` cho từng lần gửi request.
- Monitor status: `UP`, `DOWN`, hoặc `UNKNOWN` (chưa có đủ kết quả / không xác định).

Gợi ý thực hiện:

1. Khi kiểm tra thành công, ghi response time, HTTP status, timestamp.
2. Khi kiểm tra thất bại, ghi loại lỗi (HTTP error, timeout, DNS/TLS/connection error nếu phân loại được).
3. Chỉ đổi từ UP → DOWN khi đạt ngưỡng lỗi đã thống nhất, ví dụ 2 lần liên tiếp (con số minh họa, chưa chốt).
4. Chỉ phát một cảnh báo sự cố khi thực sự chuyển sang DOWN; tránh gửi lặp mỗi lần kiểm tra.
5. Khi đủ điều kiện hồi phục, cập nhật DOWN → UP, khép lại bản ghi sự cố; email hồi phục là tùy chọn.
6. Phải dự liệu tác vụ được thử lại/chạy trùng hoặc chồng lấn; tránh tạo nhiều sự cố và email cho cùng một biến cố.

Để demo ngắn và dễ quan sát, có thể chọn ngưỡng/lịch thử nghiệm riêng; không mặc định áp dụng nguyên xi cho hệ thống thật.

## 8. Mô hình dữ liệu khái niệm (đề xuất)

Các nhóm dữ liệu sau giúp hình dung thông tin cần lưu. Đây chưa phải schema DynamoDB chính thức.

- `Monitor`
  - Trường tham khảo: `monitorId`, `name`, `url`, `interval`, `status`, `createdAt`.
  - Mục đích: Quản lý các website/API được giám sát.
- `CheckResult`
  - Trường tham khảo: `checkId`, `monitorId`, `checkedAt`, `httpStatus`, `responseTimeMs`, `success`, `errorType`.
  - Mục đích: Lưu từng lần kiểm tra và kết quả tương ứng.
- `Incident`
  - Trường tham khảo: `incidentId`, `monitorId`, `openedAt`, `resolvedAt`, `reason`, `notificationState`.
  - Mục đích: Theo dõi thời điểm phát sinh sự cố, cảnh báo và hồi phục.
- `NotificationConfig`
  - Trường tham khảo: `monitorId`, `recipient`, `confirmed`.
  - Mục đích: Quản lý địa chỉ email nhận cảnh báo nếu cần lưu thông tin này trong ứng dụng.

Cấu trúc bảng, partition key/sort key, index, TTL và thời gian lưu dữ liệu cần quyết định dựa vào nhu cầu truy vấn thực tế.

## 9. API cho dashboard (đề xuất)

Các endpoint dưới đây chỉ mang tính minh họa:

- `POST /monitors`: Thêm website/API cần theo dõi.
- `GET /monitors`: Xem danh sách và trạng thái các website/API.
- `GET /monitors/{id}`: Xem chi tiết một website/API.
- `GET /monitors/{id}/checks`: Xem lịch sử các lượt kiểm tra.
- `GET /monitors/{id}/incidents`: Xem lịch sử sự cố.
- `POST /monitors/{id}/check`: Kích hoạt kiểm tra thủ công (tùy chọn).

Chưa chốt: API sửa/xóa, xác thực người dùng, phân quyền, phân trang và giới hạn tần suất truy cập. Không công khai API quản trị khi chưa có cơ chế bảo vệ phù hợp.

## 10. Dashboard và các chỉ số cần trình bày

### 10.1. Thành phần dashboard theo ý tưởng đã chốt

- Danh sách website/API được giám sát và trạng thái hiện tại.
- Thời gian phản hồi, có thể biểu diễn bằng biểu đồ theo thời gian.
- Lịch sử các lượt kiểm tra và các sự cố.
- Thống kê uptime, response time, log và chi phí Cloud.

### 10.2. Cách diễn giải chỉ số (đề xuất, cần thống nhất công thức)

- Response time: khoảng thời gian từ lúc bắt đầu gửi request đến khi hoàn thành/nhận phản hồi theo quy tắc đo; chỉ rõ đơn vị milliseconds (ms).
- Tỷ lệ lượt kiểm tra thành công: `số lượt SUCCESS / tổng số lượt hợp lệ × 100%` trong khoảng thời gian được chọn.
- Uptime: cần định nghĩa rõ là tỷ lệ thời gian ước lượng UP hay tỷ lệ lượt kiểm tra thành công. Hai chỉ số không tự động giống nhau; không nên gọi tỷ lệ check success là uptime thực tế tuyệt đối khi chưa có phương pháp suy ra khoảng downtime.
- Thời gian sự cố: ước tính từ thời điểm phát hiện và xác nhận DOWN đến thời điểm ghi nhận phục hồi; độ chính xác phụ thuộc chu kỳ kiểm tra.
- Cloud cost: dùng chi phí thực tế từ số liệu thanh toán khi có quyền và có đủ dữ liệu, hoặc dự toán ghi rõ giả định. Không mô tả ước lượng là hóa đơn thực tế.

Chỉ số p95 latency, error rate, tỷ lệ cảnh báo sai là các hướng đánh giá bổ sung theo kiến thức môn học, chưa phải yêu cầu nhóm xác nhận.

## 11. Bảo mật, độ tin cậy và chi phí cần lưu ý

### 11.1. Bảo mật tối thiểu

- IAM least privilege: Lambda chỉ được phép truy cập đúng bảng DynamoDB, topic SNS và các tài nguyên cần thiết.
- Bảo vệ API Dashboard: cần cơ chế xác thực/phân quyền phù hợp, nhất là nếu quản lý website của nhiều người dùng.
- Kiểm soát URL đầu vào: không để hệ thống kiểm tra tùy ý các địa chỉ nội bộ, metadata service hay các mục tiêu không được phép; cần chống lạm dụng chức năng gọi URL (SSRF), kiểm soát redirect và giới hạn đích.
- Không ghi lộ bí mật như API token, header nhạy cảm hay thông tin truy cập trong log hoặc dashboard.
- Email SNS: người nhận có thể cần xác nhận đăng ký trước khi nhận thông báo; demo phải kiểm tra trước việc này.

### 11.2. Độ tin cậy

- Đặt timeout hợp lý; xem xét retry có giới hạn và ngưỡng lỗi liên tiếp.
- Ghi thời gian, trạng thái HTTP và nguyên nhân lỗi để giải thích được kết luận.
- Chống tác vụ trùng/kiểm tra chồng lấn gây thông báo lặp.
- Khi endpoint khôi phục, lịch sử phải thể hiện được sự kiện DOWN và hồi phục.
- Giới hạn số website, chu kỳ kiểm tra và lưu trữ để phù hợp quy mô bài tập.

### 11.3. Chi phí

Các thành phần cần ước lượng gồm: Scheduler, số lần gọi và thời gian chạy Lambda, API Gateway requests, DynamoDB reads/writes/storage, SNS notifications, CloudWatch logs/metrics và dịch vụ hỗ trợ phát sinh nếu có. Không đưa ra con số USD cố định nếu chưa có region, traffic, cấu hình và bảng giá tại thời điểm tính.

Cần ghi rõ: AWS account/sandbox được phép dùng, region, giới hạn ngân sách, người phụ trách, thời hạn demo, danh sách tài nguyên và bước cleanup. Budget alert là cảnh báo chứ không nhất thiết khóa chi phí. Không tự tạo tài nguyên AWS có phí khi chưa được cho phép.

## 12. Kịch bản demo dự kiến

1. Hiển thị hoạt động bình thường
   - Mở dashboard, hiển thị các website/API đang ở trạng thái `UP`.
   - Bằng chứng: trạng thái hiện tại và kết quả kiểm tra thành công.

2. Chủ động tạo sự cố
   - Cho một website/API thử nghiệm trả lỗi HTTP hoặc không phản hồi.
   - Bằng chứng: endpoint gặp lỗi/timeout theo đúng kịch bản.

3. Kích hoạt lượt kiểm tra
   - Đợi lịch Scheduler hoặc chạy kiểm tra thủ công nếu đã bổ sung chức năng này.
   - Bằng chứng: thời gian chạy và log của Lambda.

4. AWS phát hiện và ghi nhận sự cố
   - Lambda đánh giá kết quả, hệ thống cập nhật trạng thái `DOWN` sau khi đủ điều kiện xác nhận lỗi.
   - Bằng chứng: dashboard đổi trạng thái, DynamoDB có lịch sử lượt kiểm tra/sự cố.

5. Gửi cảnh báo
   - SNS gửi email đến người nhận đã xác nhận đăng ký.
   - Bằng chứng: email thông báo được nhận và thông tin sự cố khớp với log.

6. Khôi phục website/API
   - Khôi phục endpoint, thực hiện lượt kiểm tra tiếp theo và cho thấy trạng thái quay về `UP`.
   - Bằng chứng: trạng thái hồi phục cùng mốc thời gian trong lịch sử.

7. Trình bày thống kê và tổng kết
   - Hiển thị uptime, response time, log CloudWatch và dự toán/chi phí Cloud.
   - Bằng chứng: dữ liệu và công thức/giả định phù hợp với kết quả đo.

Chuẩn bị trước demo: website/API do nhóm kiểm soát, cách bật/tắt lỗi có thể lặp lại, email SNS đã xác nhận, dữ liệu mẫu và phương án dự phòng nếu Internet hoặc AWS gặp sự cố.

## 13. Điểm nổi bật và giới hạn so với công cụ sẵn có

Lợi thế nhóm đã nêu:

- Bám sát môn Cloud Computing.
- Thể hiện kiến thức AWS, IAM, monitoring, serverless, event-driven và hệ thống phân tán.
- Demo trực quan bằng sự cố website/API thật trong môi trường thử nghiệm.
- Không cần phần cứng IoT hay hệ thống AI phức tạp.

Thách thức nhóm đã nhận diện:

- Phân biệt CloudPulse với công cụ giám sát đã có trên AWS.
- Triển khai kiểm tra, phát hiện sự cố và cảnh báo đáng tin cậy.

Định vị phù hợp (đề xuất): CloudPulse là ứng dụng tích hợp và cung cấp trải nghiệm giám sát website/API qua một dashboard riêng, được xây dựng bằng dịch vụ AWS. Không khẳng định nhóm phát minh cơ chế health check hay hệ thống gửi thông báo hoàn toàn mới. Khi báo cáo, so sánh dựa trên luồng sử dụng, phạm vi chức năng, giới hạn và bằng chứng demo.

## 14. Phạm vi phiên bản tối thiểu (MVP) đề xuất

Để tránh đồ án quá rộng, có thể ưu tiên:

1. Một dashboard quản trị đơn giản.
2. Thêm và xem một vài website/API mẫu (ví dụ 1–3 endpoint, con số chưa chốt).
3. Một cơ chế kiểm tra định kỳ bằng EventBridge Scheduler + Lambda.
4. Ghi từng lượt kiểm tra và trạng thái cơ bản trong DynamoDB.
5. SNS gửi một email cảnh báo khi xác nhận DOWN.
6. Lưu và hiển thị một sự cố cùng quá trình phục hồi.
7. Một trang tổng hợp response time, kết quả kiểm tra, log và ước tính chi phí.

Ưu tiên sau MVP (nếu còn thời gian và đáp ứng yêu cầu): kiểm tra thủ công, cảnh báo hồi phục, bộ lọc thời gian, p95 latency, lịch kiểm tra riêng theo endpoint, quản lý người dùng, cải thiện độ tin cậy và đánh giá nhiều trường hợp lỗi.

## 15. Những quyết định còn cần nhóm/giảng viên thống nhất

1. Quy định chính thức của đồ án: bắt buộc dịch vụ nào, bài báo cáo, video, slide, thời lượng demo, ngân sách?
2. Một website/API được kiểm tra bao lâu một lần? Có cho người dùng đổi lịch không?
3. Quy tắc UP/DOWN: HTTP status chấp nhận, timeout, retry, số lần lỗi để xác nhận sự cố, điều kiện hồi phục?
4. Lịch kiểm tra chung hay lịch riêng từng endpoint?
5. Frontend framework, hosting và cơ chế đăng nhập/quản lý người dùng?
6. Cấu trúc bảng DynamoDB, thời hạn giữ log/lịch sử và các truy vấn cần phục vụ?
7. Email sẽ quản lý qua SNS thế nào, gửi cho một người hay nhiều người, có gửi thông báo hồi phục không?
8. Nguồn website/API thử nghiệm và cách giả lập lỗi an toàn, lặp lại được?
9. Region, tài khoản AWS được cho phép, ngân sách, giới hạn tài nguyên và cách cleanup?
10. Cách định nghĩa và tính uptime, chi phí, cách chứng minh khác biệt với dịch vụ AWS sẵn có?

## 16. Tiêu chí thành công cho bản demo (đề xuất)

- Thêm được một URL/API hợp lệ và quan sát nó trên dashboard.
- Có bằng chứng một lượt HTTP check thành công và ít nhất một lượt check thất bại.
- Dashboard đổi trạng thái theo đúng quy tắc được trình bày rõ.
- DynamoDB có bản ghi kết quả và lịch sử một sự cố.
- Email SNS được nhận khi sự cố được xác nhận; không gửi trùng vô hạn.
- Endpoint phục hồi được ghi nhận và hiển thị trở lại UP.
- Nhóm trình bày được luồng Scheduler → Lambda → DynamoDB/SNS, cùng quyền IAM, CloudWatch logs và chi phí có giả định.
- Có bằng chứng tài nguyên demo đã được dọn dẹp hoặc kế hoạch cleanup được phê duyệt.

## 17. Tổng hợp ý tưởng đề xuất và những nội dung chưa chốt

### 17.1. Định hướng và ý tưởng đề xuất

- Xây dựng một dashboard quản lý nhiều website/API cần giám sát.
- Tự động kiểm tra endpoint theo lịch bằng EventBridge Scheduler và Lambda.
- Lưu kết quả kiểm tra và lịch sử sự cố bằng DynamoDB; gửi email cảnh báo bằng SNS.
- Sử dụng API Gateway để kết nối dashboard với backend, IAM để giới hạn quyền và CloudWatch để xem log.
- Tách kết quả của một lần kiểm tra (`SUCCESS`/`FAIL`) khỏi trạng thái tổng thể (`UP`/`DOWN`/`UNKNOWN`).
- Cân nhắc quy tắc xác nhận nhiều lần lỗi, tránh email trùng và ghi nhận lần phục hồi.
- Demo trọn quy trình: bình thường → gây lỗi → phát hiện → cảnh báo → phục hồi → thống kê.
- Bắt đầu bằng phạm vi MVP nhỏ, có thể chạy thử và chứng minh rõ ràng trước khi mở rộng.

### 17.2. Những nội dung chưa chốt

- Các yêu cầu bắt buộc của giảng viên, phạm vi cuối cùng và tiêu chí đánh giá.
- Công nghệ frontend, cách triển khai dashboard và cơ chế đăng nhập.
- Chu kỳ kiểm tra, timeout, HTTP status được coi là hợp lệ và số lần lỗi để xác nhận sự cố.
- Một lịch Scheduler dùng chung hay lịch riêng cho từng website/API.
- Thiết kế DynamoDB, thời gian lưu lịch sử, cách tính uptime và response time.
- Cách quản lý email SNS, số người nhận và việc gửi cảnh báo khi hồi phục.
- Quy mô MVP, các chức năng bổ sung (như `Check now`) và nguồn website/API thử nghiệm.
- AWS region, môi trường được cấp phép, ngân sách, kế hoạch cleanup và cách chứng minh khác biệt so với công cụ giám sát có sẵn.

Khi nhóm thống nhất từng quyết định, cập nhật trực tiếp tài liệu này để nó tiếp tục phản ánh đúng phạm vi đồ án CloudPulse.
