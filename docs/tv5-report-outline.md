# Dàn bài báo cáo CloudPulse — TV5

Trạng thái: khung nội dung tuần 1, chưa phải báo cáo Word/PDF hoàn chỉnh.
Nguồn: requirements.md, acceptance-criteria.md, duties.md và CloudPulse_Overview1.md.
Khi chuyển sang Word, áp dụng mẫu bìa, font, căn lề và trích dẫn do trường cung cấp.

## Phần đầu
- Trang bìa: tên trường, môn học, đề tài, giảng viên, nhóm và thành viên.
- Bảng phân công đóng góp, mục lục, danh mục hình/bảng và từ viết tắt.

## Chương 1. Giới thiệu đề tài
- Bài toán website/API gián đoạn và phát hiện sự cố muộn.
- Đối tượng: nhóm phát triển nhỏ, DevOps và người vận hành website.
- Mục tiêu: kiểm tra tự động, lưu lịch sử và cảnh báo đa kênh.
- Phạm vi MVP và các chức năng ngoài phạm vi theo requirements.md.

## Chương 2. Phân tích yêu cầu
- Trình bày FR-01 đến FR-08; liên kết mỗi yêu cầu với chức năng và tiêu chí nghiệm thu.
- NFR: hiệu năng, độ tin cậy, SSRF, chi phí và IaC.
- Phân biệt mục tiêu thiết kế và kết quả đo thực tế; chưa có phép đo thì ghi “chưa kiểm chứng”.

## Chương 3. Thiết kế kiến trúc
- Dùng sơ đồ kiến trúc trong CloudPulse_Overview1.md.
- Giải thích React, API Gateway, Lambda, EventBridge, DynamoDB, SNS và Telegram.
- Mô tả 3 bảng Monitors, CheckResults, Incidents; TTL 7 ngày.
- Luồng kiểm tra, hai lần FAIL liên tiếp → DOWN, một SUCCESS → UP, chống gửi cảnh báo trùng.
- Bảo mật SSRF và IAM Least Privilege.

## Chương 4. Triển khai
- Cấu trúc mã nguồn, môi trường local và triển khai AWS SAM (phối hợp TV1–TV4).
- Dashboard: chỉ số tổng quan, biểu đồ độ trễ, lịch sử sự cố.
- TV5: AreaChart, tooltip thời gian/ms/HTTP, cách tính MIN/AVG/MAX.
- Mô tả rõ giai đoạn dùng Mock Data và giai đoạn tích hợp API thật.
- Chèn ảnh giao diện và đoạn code tiêu biểu khi hoàn thiện.

## Chương 5. Kiểm thử và đánh giá
- Lập bảng: mã yêu cầu, tình huống, kết quả mong đợi, kết quả thực tế, bằng chứng.
- Kiểm thử HTTP 200, HTTP 500, timeout, lỗi đơn lẻ, DOWN, recovery, chống spam và SSRF.
- Biểu đồ: dữ liệu thường, dữ liệu rỗng, thay dữ liệu, MIN/AVG/MAX, tooltip và màn hình nhỏ.
- Đính kèm CloudWatch Logs, email/Telegram, Incidents và ảnh Dashboard thật ở tuần 3–4.
- FinOps: bảng tài nguyên, mức dùng, chi phí ước tính và hóa đơn thực tế; TV1 xác nhận.
- Không ghi chi phí $0 hay mức giá dịch vụ so sánh là kết quả thực tế khi chưa có bằng chứng và nguồn giá tại thời điểm làm báo cáo.

## Chương 6. Kết luận
- Các yêu cầu đã hoàn thành, hạn chế và hướng phát triển.
- Bài học về Serverless, giám sát, bảo mật và phối hợp nhóm.

## Phần cuối
- Tài liệu tham khảo chính thức và ngày truy cập.
- Phụ lục: hướng dẫn chạy, kịch bản demo 7 bước, bảng phân công, link mã nguồn và video.

## Bằng chứng còn cần thu thập
- TV1: sơ đồ triển khai thực tế, kết quả SAM, cấu hình IAM/TTL và chi phí AWS.
- TV2: HealthChecker, State Machine, SNS/Telegram và log vận hành.
- TV3: cấu trúc API, dữ liệu trả về và MockTarget.
- TV4/TV5: giao diện tích hợp thật, lịch sử sự cố và kết quả kiểm thử.
