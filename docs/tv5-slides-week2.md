# Khung thuyết trình CloudPulse — 15 slide

Ngày: 06/11/2026. Khung nội dung, bổ sung ảnh và kết quả thật trước buổi bảo vệ.

## 1. Trang bìa
- CloudPulse: giám sát Website/API trên AWS Serverless.
- Điền trường, môn, giảng viên và danh sách thành viên theo mẫu nhóm.

## 2. Bài toán
- Website gián đoạn, phát hiện muộn, thiếu lịch sử điều tra.
- Tác động: giao dịch thất bại và tăng thời gian xử lý.

## 3. Persona
- DevOps/indie developer vận hành 1–20 endpoint.
- Cần thông báo điện thoại/email, ít công quản trị, ngân sách thấp.

## 4. Mục tiêu và phạm vi
- Kiểm tra mỗi phút, quản lý trạng thái, thông báo, Dashboard.
- MVP chưa có multi-tenancy, self-healing và giám sát CPU/RAM.

## 5. Các yêu cầu chức năng
- FR-01–FR-03: endpoint, lịch chạy, phân loại kết quả.
- FR-04–FR-08: state machine, cảnh báo, Check Now, MockTarget, Dashboard.

## 6. Yêu cầu phi chức năng
- MTTD, latency, độ tin cậy, SSRF, IAM, FinOps, IaC.
- Trình bày mục tiêu; bổ sung kết quả đo sau tích hợp.

## 7. Kiến trúc tổng thể
- React → API Gateway → Lambda → DynamoDB.
- Scheduler → HealthChecker → SNS/Telegram.
- Chèn sơ đồ trong CloudPulse_Overview1.md; TV1 xác nhận hạ tầng thật.

## 8. Mô hình dữ liệu
- Monitors: endpoint và trạng thái.
- CheckResults: kết quả từng lượt, TTL 7 ngày.
- Incidents: bắt đầu, kết thúc, nguyên nhân và downtime.

## 9. State machine
- FAIL lần 1 vẫn UP; FAIL lần 2 chuyển DOWN và mở OPEN.
- FAIL tiếp không gửi lặp; SUCCESS chuyển UP và RESOLVED.

## 10. Dashboard và latency
- Chỉ số tổng quan, monitor list và AreaChart.
- MIN/AVG/MAX, tooltip thời gian/ms/HTTP.
- Chèn ảnh giao diện và ghi rõ phần nào dùng mock.

## 11. IncidentHistory
- OPEN đỏ, RESOLVED xanh; sắp xếp mới nhất trước.
- HTTP 500, timeout, thời gian bắt đầu/kết thúc.
- OPEN cập nhật theo giây; RESOLVED mẫu có 124s downtime.

## 12. Bảo mật và vận hành
- Chặn loopback/private/metadata, kiểm tra backend.
- IAM tối thiểu, quản lý secrets, log và cleanup bằng SAM.
- Chèn policy và kết quả kiểm thử thật khi có.

## 13. FinOps
- Một endpoint/mỗi phút/30 ngày = 43.200 lượt.
- Canary us-east-1: $51,84 trước ưu đãi, chưa gồm dịch vụ liên quan.
- CloudPulse hướng tới $0 có điều kiện; TV1 xác nhận Billing và region.
- Nguồn: aws.amazon.com/cloudwatch/pricing/ và aws.amazon.com/free/free-tier-faqs/.

## 14. Demo và kiểm thử
- Bình thường → tạo HTTP 500 → hai FAIL → DOWN/cảnh báo → xem OPEN → hồi phục → RESOLVED.
- Thử thêm timeout, dữ liệu rỗng, SSRF, không spam.
- Chèn log/ảnh Email/Telegram thật; không dùng mock làm bằng chứng AWS.

## 15. Kết quả và hướng phát triển
- Tuần 2: IncidentHistory mock, bản thảo Chương 1–2, khung thuyết trình.
- Tiếp theo: API thật, kiểm thử đầu cuối, xác nhận chi phí và hoàn thiện báo cáo.
- Hỏi đáp.
