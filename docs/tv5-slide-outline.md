# Dàn bài slide CloudPulse — TV5

Khung 15 slide, thời lượng trình bày mục tiêu 15 phút gồm demo.
Các số đo, ảnh AWS và chi phí thực tế sẽ bổ sung sau khi triển khai; hiện chưa có bằng chứng vận hành.

| Slide | Nội dung | Hình ảnh / bằng chứng cần chuẩn bị |
| --- | --- | --- |
| 1 | Tên đề tài, môn học, nhóm và thành viên | Logo/tên CloudPulse |
| 2 | Bài toán: website/API gián đoạn, phát hiện muộn | Tình huống sử dụng |
| 3 | Người dùng và giá trị đề xuất | Nhóm phát triển nhỏ, DevOps |
| 4 | Mục tiêu và phạm vi MVP | Tóm tắt FR-01–FR-08 |
| 5 | Kiến trúc tổng thể Serverless | Sơ đồ từ tài liệu overview |
| 6 | Vai trò các dịch vụ AWS | API Gateway, Lambda, EventBridge, DynamoDB, SNS |
| 7 | Luồng kiểm tra và lưu dữ liệu | Monitors → CheckResults → Incidents |
| 8 | State Machine và chống báo động giả | Hai FAIL → DOWN; SUCCESS → UP |
| 9 | Cảnh báo Email và Telegram | Ảnh cảnh báo mở/khôi phục thực tế |
| 10 | Dashboard và biểu đồ độ trễ | Ảnh UI, MIN/AVG/MAX, tooltip HTTP |
| 11 | Bảo mật và quản lý hạ tầng | SSRF, IAM, SAM, TTL |
| 12 | Demo 7 bước | Bình thường → gây lỗi → FAIL 1 → DOWN/cảnh báo → Incident → khôi phục → Logs/chi phí |
| 13 | Kiểm thử và kết quả đo | Bảng nghiệm thu, CloudWatch Logs; điền số đo thực tế |
| 14 | Chi phí và giới hạn | Bảng chi phí có nguồn/ngày kiểm tra, hóa đơn do TV1 cung cấp |
| 15 | Kết luận, hướng phát triển, hỏi đáp | Kết quả đã xác minh, link repo/video |

Gợi ý thời lượng: slide 1–4: 2 phút; 5–9: 3 phút; 10–11: 2 phút; demo: 5 phút; 13–15: 3 phút.
TV5 phối hợp cả nhóm phân vai trình bày khi tổng duyệt.
