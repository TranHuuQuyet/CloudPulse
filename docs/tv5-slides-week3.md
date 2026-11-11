# CloudPulse — Khung thuyết trình 15 slide

Ngày: 11/11/2026. Theo [dàn bài](tv5-slide-outline.md). Nội dung chữ và bố cục để đưa vào PowerPoint/Google Slides; placeholder trong ngoặc vuông phải thay bằng bằng chứng thật trước bảo vệ. Tổng 15 phút gồm demo. Không dùng ảnh mock làm bằng chứng AWS.

## Slide 1 — CloudPulse

**Chữ trên slide:** Giám sát Website & API trên AWS Serverless. Đồ án [TÊN MÔN] · [TRƯỜNG] · [GIẢNG VIÊN]. Nhóm [SỐ NHÓM] — [TÊN 5 THÀNH VIÊN].

**Bố cục:** Tên CloudPulse lớn ở giữa, mô tả một dòng, thông tin nhóm ở chân trang. **Lời dẫn:** Nhóm trình bày giải pháp kiểm tra, ghi lịch sử và cảnh báo sự cố. **Thời gian:** 15 giây.

## Slide 2 — Khi API ngừng phục vụ

**Chữ trên slide:** Website/API có thể trả 500 hoặc timeout. Kiểm tra thủ công phát hiện muộn. Thiếu lịch sử làm khó việc đối chiếu sự cố.

**Bố cục:** Trái là ba ý; phải là timeline “bắt đầu lỗi → phát hiện → xử lý”. Ghi rõ timeline minh họa. **Lời dẫn:** Khoảng chờ phát hiện là vấn đề cần giảm. **Thời gian:** 35 giây.

## Slide 3 — Người dùng và giá trị

**Chữ trên slide:** Lập trình viên độc lập, nhóm nhỏ và DevOps. Theo dõi tập trung nhiều endpoint. Nhận cảnh báo qua Email/Telegram. Xem latency và lịch sử để điều tra.

**Bố cục:** Ba thẻ “Theo dõi / Nhận cảnh báo / Đối chiếu lịch sử”; persona ở đầu trang. **Lời dẫn:** Người dùng không cần cài agent lên website đích. **Thời gian:** 30 giây.

## Slide 4 — Mục tiêu và phạm vi MVP

**Chữ trên slide:** Thêm/xem/xóa monitor; kiểm tra định kỳ; State Machine và Incident; cảnh báo; Check Now; MockTarget; Dashboard. Mục tiêu đo: MTTD ≤ 2 phút, alert ≤ 5 giây sau chuyển trạng thái — chờ kiểm chứng. Ngoài phạm vi: đa người dùng, đa vùng, tự sửa lỗi.

**Bố cục:** Hai cột chức năng/phạm vi; mục tiêu ở dải chân trang có nhãn “mục tiêu”. **Lời dẫn:** Liên kết FR-01–FR-08 với tiêu chí nghiệm thu. **Thời gian:** 40 giây.

## Slide 5 — Kiến trúc Serverless

**Chữ trên slide:** React → HTTP API → Lambda API → DynamoDB. EventBridge → HealthChecker → endpoint. Chuyển trạng thái → SNS/Telegram. CloudWatch ghi log.

**Bố cục:** Sơ đồ kiến trúc tại mục 3.1 [báo cáo](tv5-report-week3.md) chiếm 80% slide; tô riêng luồng tương tác và định kỳ. [CHÈN E01: tên tài nguyên/region thực tế do TV1 xác nhận]. **Lời dẫn:** Đóng Dashboard không dừng giám sát. **Thời gian:** 40 giây.

## Slide 6 — Dịch vụ AWS và trách nhiệm

**Chữ trên slide:** API Gateway tiếp nhận REST; Lambda xử lý nghiệp vụ; EventBridge chạy lịch; DynamoDB giữ dữ liệu; SNS gửi Email; CloudWatch hỗ trợ truy vết. SAM khai báo hạ tầng, IAM giới hạn quyền.

**Bố cục:** Lưới dịch vụ và mỗi dịch vụ một dòng trách nhiệm. Telegram đặt ngoài khung AWS. **Lời dẫn:** Template dùng Schedule EventBridge; không có Step Functions. **Thời gian:** 30 giây.

## Slide 7 — Dữ liệu từ check đến Incident

**Chữ trên slide:** Monitors: cấu hình và trạng thái hiện tại. CheckResults: monitorId + checkedAt, ms/HTTP, TTL 7 ngày. Incidents: monitorId + incidentId, OPEN/RESOLVED, timestamps và downtime. Lịch sử được truy vấn theo monitor.

**Bố cục:** Ba bảng nối bằng monitorId; dưới cùng là “check → lưu kết quả → cập nhật trạng thái → Incident khi chuyển trạng thái”. **Lời dẫn:** TTL dọn bất đồng bộ, không phải thời hạn xóa chính xác. **Thời gian:** 35 giây.

## Slide 8 — Hai lỗi mới xác nhận DOWN

**Chữ trên slide:** FAIL 1: tăng bộ đếm, chưa cảnh báo. FAIL 2: DOWN, mở Incident, gửi alert. FAIL tiếp: giữ DOWN. SUCCESS: UP, đóng Incident, gửi recovery. Downtime tính từ lúc xác nhận DOWN.

**Bố cục:** Timeline năm điểm với màu trạng thái, một dòng chú thích “logic ứng dụng”. [CHÈN E04: timestamps FAIL 1/2 và recovery thật]. **Lời dẫn:** Kiểm thử gửi trùng khi lượt chạy chồng nhau. **Thời gian:** 45 giây.

## Slide 9 — Email và Telegram

**Chữ trên slide:** Cảnh báo khi DOWN và khi khôi phục. Có tên endpoint, thời điểm và nguyên nhân quan sát. Kiểm tra không gửi lặp khi tiếp tục FAIL.

**Bố cục:** Hai khung ảnh bằng nhau: [CHÈN E05-A: Telegram DOWN/UP] và [CHÈN E05-B: Email DOWN/UP]. Chân ảnh ghi monitor ID, thời gian gửi/nhận, che thông tin nhạy cảm. **Lời dẫn:** HTTP 500 chưa chứng minh nguyên nhân gốc. **Thời gian:** 30 giây.

## Slide 10 — Dashboard dùng API thật

**Chữ trên slide:** Chọn monitor → tải song song checks và incidents. Sync/Check Now → tải lại lịch sử. Tooltip hiển thị ms và HTTP; MIN/AVG/MAX theo cửa sổ dữ liệu. Nhãn nguồn phân biệt API và giả lập.

**Bố cục:** [CHÈN E02: Dashboard API thật] chiếm 75%; phóng to tooltip và tên monitor ở cạnh. [CHÈN E03: hàng OPEN/RESOLVED]. **Lời dẫn:** Thống kê snapshot chưa tương đương uptime theo thời gian; p95 chưa có nguồn tổng hợp. **Thời gian:** 60 giây.

## Slide 11 — Bảo mật và hạ tầng

**Chữ trên slide:** Chỉ nhận HTTP(S), chặn loopback/private/metadata. Kiểm tra URL phía backend và DNS; nghiệm thu redirect/rebinding. IAM theo bảng/topic; tiếp tục rà action tối thiểu. SAM tái tạo tài nguyên; TTL giữ lịch sử checks có giới hạn.

**Bố cục:** Trái là bốn ý; phải [CHÈN E06: IAM/TTL] và [CHÈN ảnh response SSRF bị chặn]. **Lời dẫn:** Không dùng CORS thay cho xác thực; MVP cần giới hạn truy cập phù hợp. **Thời gian:** 60 giây.

## Slide 12 — Demo 7 bước

**Chữ trên slide:** ① Monitor đang UP → ② Gây HTTP 500/timeout → ③ Quan sát FAIL 1 → ④ FAIL 2, DOWN và alert → ⑤ Xem Incident OPEN → ⑥ Khôi phục, UP/RESOLVED → ⑦ Đối chiếu Logs và chi phí.

**Bố cục:** Thanh tiến trình bảy bước ở trên; chuyển sang demo toàn màn hình. [CHÈN LINK VIDEO DEMO DỰ PHÒNG] và [ẢNH DỰ PHÒNG E02–E05]. **Lời dẫn:** Dùng monitor/MockTarget thử nghiệm do nhóm chuẩn bị; ghi rõ cách gây lỗi và hồi phục. Nếu API không hỗ trợ đổi URL, chuẩn bị cơ chế đổi trạng thái target trước buổi demo; thêm monitor khác không chứng minh recovery của Incident cũ. **Thời gian:** 5 phút.

## Slide 13 — Kiểm thử và kết quả

**Chữ trên slide:** HTTP 200/500/timeout; hai FAIL và recovery; chống spam; SSRF; chọn monitor/Sync/Check Now; dữ liệu rỗng và lỗi API. MTTD: [CHỜ ĐO] · Alert latency: [CHỜ ĐO] · Check Now: [CHỜ ĐO].

**Bố cục:** Trái bảng ca/kỳ vọng/thực tế; phải [CHÈN E04: CloudWatch Logs có timestamp/monitor ID]. Chân slide ghi số mẫu và thời gian thử. **Lời dẫn:** Chỉ đánh dấu đạt khi có bằng chứng tương ứng. **Thời gian:** 60 giây.

## Slide 14 — FinOps và giới hạn

**Chữ trên slide:** So sánh cùng số endpoint, chu kỳ và số ngày. CloudPulse: Lambda + API + DynamoDB + Logs/SNS. Synthetics: canary run và tài nguyên phụ trợ. Free Tier/credits tùy tài khoản; hóa đơn: [CHỜ TV1]. Giới hạn: một vùng, chưa đa người dùng, còn chi phí bảo trì.

**Bố cục:** Bảng ba cột “CloudPulse ước tính / Synthetics ước tính / Hóa đơn thực tế” theo mục 5.2 báo cáo; [CHÈN E07: Billing/Cost Explorer đã che dữ liệu nhạy cảm]. Chân slide: AWS Free Tier và CloudWatch Pricing, kiểm tra 11/11/2026. **Lời dẫn:** Không kết luận $0 vs $50 khi chưa có phép tính và hóa đơn. **Thời gian:** 60 giây.

## Slide 15 — Kết luận và hỏi đáp

**Chữ trên slide:** Giải pháp liên kết kiểm tra, lịch sử và cảnh báo. Kết quả đã xác minh: [ĐIỀN THEO MA TRẬN NGHIỆM THU]. Hướng phát triển: xác thực, kiểm tra đa vùng, chỉ số lịch sử và củng cố tính nhất quán. Mã nguồn: [LINK REPO] · Demo: [LINK VIDEO]. Cảm ơn và hỏi đáp.

**Bố cục:** Ba ý chính, QR/link ở góc phải; nền giản dị để hỗ trợ trao đổi. **Lời dẫn:** Phân biệt phần hoàn thành và phần còn giới hạn. **Thời gian:** 60 giây, chuyển hỏi đáp sau 15 phút.

## Checklist trước khi xuất slide

- Thay E01–E07 bằng ảnh thật có ngày giờ/region/monitor tương ứng; giữ bản gốc để truy vết.
- TV1 xác nhận FinOps; TV2 xác nhận Logs/alert; TV3 xác nhận API; TV4/TV5 xác nhận Dashboard.
- Điền trường/lớp/thành viên/link và phân vai; kiểm tra font tiếng Việt, độ rõ ảnh và chế độ trình chiếu.
- Tổng duyệt demo 5 phút; kiểm tra video dự phòng mở được offline; xuất PDF sau khi cập nhật số slide.
