# Bằng chứng W3-05 — 11/11/2026

Môi trường: Windows, Chrome headless, timezone Asia/Ho_Chi_Minh; API AWS ap-southeast-1. [results.json](results.json) là kết quả lần chạy gần nhất, timestamp UTC có hậu tố Z (giờ Việt Nam cộng 7). Các request dev đi qua Vite proxy; preview gọi trực tiếp API Gateway.

| Ca | Nguồn dữ liệu | Bằng chứng |
| --- | --- | --- |
| Dashboard và MIN/AVG/MAX | AWS thật, tối đa 30 điểm | [Desktop](live-dashboard.png), results.json |
| Sync / Check Now | AWS thật; POST check trên Mock 200 có sẵn | results.json: URL, thời điểm khởi phát, HTTP result, tổng thời gian phản hồi và latency đích |
| Incident OPEN | AWS thật, Mock 500 có sẵn | [Incident](live-incident.png) |
| Đổi monitor nhanh | ID monitor AWS; route lịch sử điều khiển: A 111 ms chậm 1.200 ms, B 222 ms | [Kết quả vẫn thuộc B](race-B-after-delayed-A.png) |
| Không có lịch sử | Tạo monitor AWS tạm rồi query hai mảng rỗng; xóa monitor đó trong finally. Đồng thời có ca response [] điều khiển | [Empty AWS thật](live-empty-monitor.png), [Empty điều khiển](empty-history.png) |
| API lỗi và retry | Response 503 điều khiển; bỏ route rồi Sync trở lại AWS | [Lỗi API](api-error.png), results.json |
| Env URL sai | Vite mode acceptance-invalid; target cổng localhost 1 không phục vụ | [URL sai, không fallback](invalid-base-url.png) |
| Env URL rỗng | Vite mode acceptance-mock | [Dữ liệu giả lập](mock-mode.png) |
| Mobile 390×844 | AWS thật; kiểm tra kích thước và scrollLeft DOM | [Tooltip ms/HTTP](mobile-tooltip.png), [Bảng cuộn ngang](mobile-incidents.png) |
| Preview production | Build với .env AWS; POST body {} để kích hoạt preflight mà không tạo monitor | [Trang preview](preview-direct-api.png); lỗi CORS nằm trong results.json. POST **chưa đạt** |

Script: [w3-acceptance.mjs](../../../frontend/tests/w3-acceptance.mjs). Cần Node, Chrome và Playwright. Công cụ Playwright được tải qua npm exec, không thay dependencies của frontend. Chạy bằng `node tests/w3-acceptance.mjs "<đường dẫn tuyệt đối tới playwright/index.mjs>"` từ frontend. Script gọi Check Now một lần và tạo/xóa một monitor tạm cho ca empty; cần sẵn Mock 200 và Mock 500 có lịch sử, gồm Incident OPEN của Mock 500. Kết quả KNOWN_FAILURE ghi nhận lỗi production CORS đã tái hiện; PASS chỉ áp dụng cho từng ca tương ứng.

Chuẩn bị bốn server trên 127.0.0.1:

- Port 3015: `npm.cmd run dev -- --port 3015 --strictPort --host 127.0.0.1` với .env AWS.
- Port 4185: `npm.cmd run build`, sau đó `npm.cmd run preview -- --port 4185 --strictPort --host 127.0.0.1`.
- Port 3016: tạo `.env.acceptance-mock` chứa `VITE_API_BASE_URL=`, chạy dev với `--mode acceptance-mock --port 3016 --strictPort --host 127.0.0.1`.
- Port 3017: tạo `.env.acceptance-invalid` chứa `VITE_API_BASE_URL=http://127.0.0.1:1`, chạy dev với `--mode acceptance-invalid --port 3017 --strictPort --host 127.0.0.1`.

Hai file env mode chỉ phục vụ kiểm thử và được Git bỏ qua. Không commit .env hoặc các file env mode. Ảnh race/empty/error thể hiện response được điều khiển dù nhãn UI là Dữ liệu API; không sử dụng làm bằng chứng backend thực sự tạo ra các dữ liệu đó.

Giới hạn: một máy, một trình duyệt, ít mẫu; không chứng minh SLA, MTTD, chống spam alert hoặc recovery. Chart còn hiển thị giờ UTC do backend trả chuỗi mất timezone; Incident dùng UTC+7. CORS production POST còn lỗi cần TV1/TV3 xử lý.
