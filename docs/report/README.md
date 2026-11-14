# Báo cáo Word CloudPulse — TV5

File chính: [CloudPulse_BaoCao_TV5_Tuan3.docx](CloudPulse_BaoCao_TV5_Tuan3.docx).

Đã ghép Chương 1–6, bìa, phân công, từ viết tắt, mục lục cập nhật trong Word, tài liệu tham khảo và ba phụ lục. Có 11 hình nhúng trực tiếp, gồm sơ đồ kiến trúc và ảnh kiểm thử, kèm caption/nguồn/giới hạn. Ảnh trong assets là bản cắt phục vụ trình bày; ảnh gốc giữ tại ../evidence/w3-05.

Các khung vàng chỉ rõ cần làm gì, ở đâu và ai cung cấp:

| Vị trí | Nội dung còn thiếu |
| --- | --- |
| Bìa / bảng phân công | Trường, khoa, môn, lớp, giảng viên, tên và MSSV, đóng góp xác nhận |
| 4.6 / E01, E06 | TV1: ảnh SAM outputs, IAM và TTL |
| 5.5 / E04, E05 | TV2: CloudWatch Logs, Telegram/Email DOWN và recovery |
| 5.5 / E03-RESOLVED | Kiểm thử recovery cùng monitor/Incident |
| 5.2 / 5.5 / E07 | TV1: dự toán đúng region và hóa đơn trước/sau credits |
| Phụ lục B | Cơ chế đổi target để demo recovery, phân vai và video dự phòng |

CORS POST production và timezone chart còn được ghi nhận là lỗi; không coi báo cáo hiện tại là bằng chứng nghiệm thu toàn bộ hệ thống.

Mở Word, điền thông tin còn thiếu và cập nhật mục lục bằng bấm phải vào trường mục lục → Update Field → Update entire table. Môi trường tạo file chưa cài Microsoft Word/LibreOffice nên chưa render để kiểm tra số trang/ngắt trang; đã kiểm tra mở lại DOCX, XML, ảnh nhúng, bảng và sáu tiêu đề chương. Thông số A4/TNR/lề chỉ là lựa chọn tạm vì chưa có mẫu chính thức của trường.

Tái tạo file: cài `python-docx`, `Pillow`, rồi chạy `python tools/build_tv5_report.py`. Có thể dùng `--libs <thư mục thư viện>` nếu cài thư viện tách riêng. Script đọc các bản thảo Markdown và results.json. Sau khi chỉnh trực tiếp file Word, không chạy lại script nếu chưa lưu bản sao: script tạo lại file Word cùng tên.
