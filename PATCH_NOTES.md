# 📦 Ernastie IT Tools - Patch Notes v1.0.1

<p align="center">
  <img src="src/Resources/app.png" alt="Ernastie Crafting Table Logo" width="128" height="128" />
</p>

**Phiên bản:** `v1.0.1`  
**Ngày phát hành:** 22/10/2026  
**Nền tảng mục tiêu:** Windows 10, Windows 11, Windows Server & Môi trường cứu hộ WinPE (x64)  
**Tương thích:** Standalone Single-File Executable (Độc lập, không cần cài .NET Runtime)

---

## 🌟 TỔNG QUAN PHIÊN BẢN v1.0.1

Bản phát hành **v1.0.1** là một bản cập nhật lớn mang tính bước ngoặt, giải quyết triệt để **16/16 lỗi và tồn đọng hệ thống (`repo1` - `repo16`)**, thay thế toàn bộ các chức năng giữ chỗ (mock/placeholder) bằng các mô-đun thực thi thực tế, đồng thời đồng bộ biểu tượng ứng dụng mới: **Minecraft Crafting Table**.

---

## 🚀 DANH SÁCH CHI TIẾT CÁC THAY ĐỔI & TÍNH NĂNG MỚI

### 1. 🛠️ Cài Đặt Phần Mềm (repo1)
- **Hỗ trợ toàn diện Windows Lite / Win 10 LTSC:** Tự động phát hiện nếu hệ thống thiếu môi trường `winget.exe`.
- **Cài đặt Offline Bundle:** Tự động tải và cấu hình bộ offline bundle `Microsoft.DesktopAppInstaller` kèm các phụ thuộc `Microsoft.VCLibs` và `Microsoft.UI.Xaml`.
- **Fallback tải trực tiếp:** Tích hợp bộ tải trực tiếp và cài đặt im lặng (silent installation) từ URL gốc khi máy hoàn toàn không có Microsoft Store.

### 2. 📦 Master Image & Sysprep (repo2)
- **Tự động dọn dẹp AppX xung đột:** Chạy script PowerShell chuyên dụng loại bỏ các gói ứng dụng UWP gây chặn Sysprep (khắc phục mã lỗi `0x80073cf2`).
- **Tạm dừng dịch vụ nền:** Tự động stop `wuauserv`, `bits`, `WMPNetworkSvc` trước khi chạy Sysprep.
- **Tự động bắt log Panther:** Phân tích trực tiếp tệp log `setuperr.log` và hiển thị chi tiết nguyên nhân nếu Sysprep thất bại.

### 3. 🌐 Giao Diện Mạng & WiFi (repo3)
- **Xóa nút trùng lặp:** Loại bỏ nút `Show WiFi & VPN` dư thừa tại Section 1 Hệ thống, chuẩn hóa duy nhất nút `Sao lưu Wifi` tại Section 2.

### 4. 📁 Sao Lưu & Khôi Phục Trình Duyệt (repo4)
- **Ra mắt `BrowserBackupForm`:** Hỗ trợ sao lưu Profile, Bookmarks, Lịch sử duyệt web của 5 trình duyệt phổ biến: **Google Chrome, Microsoft Edge, Cốc Cốc, Brave, Mozilla Firefox**.
- **Đóng gói ZIP thông minh:** Nén toàn bộ dữ liệu vào tệp ZIP gọn nhẹ để chuyển đổi máy tính hoặc lưu trữ an toàn.

### 5. ⚡ Tạo Boot WinPE & Bộ Anhdv Boot (repo5)
- **Sửa lỗi hiển thị UI:** Chuyển đổi panel điều khiển sang `TableLayoutPanel` có thứ tự rõ ràng, khắc phục triệt để lỗi đảo ngược vị trí nút do WinForms Top-Docking.
- **Mở rộng hộp thoại Boot:** Tăng kích thước hộp thoại chọn bộ boot lên 680x420, áp dụng ThemeHelper đồng bộ, không còn hiện tượng tràn chữ.

### 6. 📐 Phân Vùng Ổ Đĩa WinPE 25GB (repo6)
- **Sửa lỗi Diskpart:** Tự động truy vấn WMI để lấy chính xác số Disk Index chứa ổ C: và chèn lệnh `select disk <index>` trước khi tạo phân vùng `primary`.
- **Dọn dẹp Shadow Copies:** Chạy lệnh `vssadmin delete shadows /all /quiet` giải phóng không gian thu nhỏ ổ đĩa.
- **Cơ chế dự phòng:** Tích hợp PowerShell Storage Module (`Resize-Partition` & `New-Partition`) nếu Diskpart bị khóa.

### 7. 💽 Tối Ưu Danh Mục WinPE (repo7)
- **Loại bỏ tính năng dư thừa:** Xóa nút `Lấy driver từ ổ cứng` tại Section WinPE do đã có công cụ `Sao lưu Driver` chuyên nghiệp bằng DISM.

### 8. 🖱️ Menu Chuột Phải Windows 10/11 (repo8)
- **Tự động nhận diện hệ điều hành:** Bổ sung hàm kiểm tra Build OS (`Environment.OSVersion.Version.Build >= 22000`).
- **Thông báo chuẩn xác:** Trên Windows 10, thông báo rõ ràng cho người dùng hệ thống vốn đã sử dụng menu cổ điển, tránh gây hiểu lầm.

### 9. ⚡ Tác Vụ Tự Động Hóa 1-Click (repo9)
- **Ra mắt `AutoTasksForm`:** Hỗ trợ thực thi chuỗi kịch bản tối ưu hóa máy tính tức thì:
  - Dọn dẹp tệp tạm `Temp`, `Prefetch`, bộ nhớ đệm `SoftwareDistribution`.
  - Làm mới cấu hình mạng: Flush DNS, Winsock Reset, tái thiết lập TCP/IP.
  - Đồng bộ lại thời gian hệ thống chuẩn quốc tế qua giao thức NTP (`w32tm`).
  - Chuyển đổi chế độ nguồn điện High Performance Power Plan.
  - Tối ưu hóa ổ đĩa SSD bằng lệnh TRIM (`defrag C: /O`).
  - Kiểm tra và sửa chữa kho linh kiện Windows (`dism /online /cleanup-image /restorehealth`).

### 10. 🎧 Trung Tâm Hỗ Trợ Từ Xa (repo10)
- **Ra mắt `RemoteSupportForm`:** Tự động kiểm tra cài đặt và cho phép mở nhanh 5 phần mềm điều khiển từ xa hàng đầu: **UltraViewer, AnyDesk, TeamViewer, RustDesk, Quick Assist**.
- **1-Click Tải Portable:** Hỗ trợ tải ngay bản Portable nếu máy tính khách hàng chưa cài đặt công cụ nào.

### 11. 📄 Quản Lý & Sửa Lỗi Microsoft Office (repo11)
- **Ra mắt `OfficeConfigForm`:** Cung cấp bộ công cụ IT Support chuyên nghiệp cho Office:
  - Kiểm tra trạng thái bản quyền chi tiết qua OSPP (`ospp.vbs /dstatus`).
  - Tự động nhận diện và gỡ bỏ Product Key lỗi (`/unpkey`).
  - Cấu hình máy chủ KMS nội bộ và kích hoạt tức thì (`/act`).
  - Dọn sạch Token bản quyền lỗi và reset thông tin đăng nhập bị kẹt (xóa cache Windows Credential Manager).
  - Khởi chạy nhanh công cụ Quick Repair và Online Repair.
  - Chuẩn hóa font chữ Microsoft Word (Times New Roman 13pt) và bật/tắt Hardware Graphics Acceleration.

### 12. 🛡️ Tinh Gọn Dashboard (repo12)
- **Xóa bỏ Control Power Plan:** Loại bỏ cụm nút Silent/Balanced/Turbo/Eco chiếm nhiều diện tích trên Dashboard để tối ưu không gian cho biểu đồ hiệu năng.

### 13. 📊 Đo Đạc Telemetry Chuẩn Xác (repo13)
- **Đo tải GPU thực tế:** Sử dụng trực tiếp Windows Performance Counter `GPU Engine` (3D Engine) để lấy thông số phần cứng từ DWM / DirectX Graphics Kernel thay cho công thức giả lập cũ.
- **Hiển thị quạt tản nhiệt thông minh:** Truy vấn cảm biến thực từ `Win32_Fan`; trường hợp bo mạch điều khiển tự động qua phần cứng, hiển thị trạng thái `Tự động (Smart PWM)` chính xác.

### 14. ⚙️ Hiện Thực Hóa Toàn Bộ Tab Nâng Cao (repo14)
- **Sửa lỗi máy in:** Khởi chạy chuỗi lệnh dừng Spooler, dọn sạch hàng đợi in kẹt trong `%systemroot%\System32\spool\PRINTERS` và bật lại dịch vụ.
- **Khôi phục liên kết tệp nén:** Khôi phục liên kết mặc định cho `.zip`, `.7z`, `.rar` và làm mới shell Explorer (`SHChangeNotify`).
- **Kiểm tra phiên bản Windows:** Chạy DISM để hiển thị edition hiện tại và danh sách các edition đích có thể nâng cấp trực tiếp.
- **Gỡ Linux & khôi phục Bootloader:** Tự động sửa lại MBR và phục hồi Windows Bootloader mặc định qua `bcdboot`.

### 15. 📱 Đồng Bộ Dữ Liệu Thiết Bị Lên Google Sheet (repo15)
- **Nâng cấp `GoogleSheetService`:** Tích hợp HTTP POST gửi toàn bộ thông số phần cứng thực tế (CPU, RAM, Serial, Windows Key, MAC Address, Model) lên Webhook Google Apps Script.
- **Kích hoạt Restore PC:** Tự động truy vấn dữ liệu từ Google Sheet và áp dụng lại tên máy/công ty cho thiết bị tương ứng.
- **Kiểm tra trùng lặp thiết bị:** Quét toàn bộ bảng tính để cảnh báo nếu máy đã tồn tại dựa trên số Serial hoặc tên máy.

### 16. 🖥️ Tối Ưu Bố Cục Dashboard & Hỗ Trợ High-DPI (repo16)
- **Chống đè chữ CPU/GPU:** Đặt thông số CPU và GPU vào `TableLayoutPanel` 2 cột co giãn linh hoạt, loại bỏ tọa độ cố định.
- **Chống cắt xén giao diện:** Bật `AutoScrollMinSize` giúp Dashboard tự động xuất hiện thanh cuộn mượt mà trên các màn hình tỉ lệ co giãn cao (125%, 150%) hoặc độ phân giải 1366x768.

### 17. 🎨 Nhận Diện Thương Hiệu Mới
- **Crafting Table Icon:** Toàn bộ file thực thi `.exe`, biểu tượng cửa sổ Form và logo trên tài liệu README đã được đồng bộ sang biểu tượng **Bàn chế tạo Minecraft (Crafting Table)** với 6 kích thước điểm ảnh chuẩn (`16x16` đến `256x256`).

---

## 💾 HƯỚNG DẪN SỬ DỤNG & CÀI ĐẶT

1. **Tải về:** Tải tệp `Ernastie_v1.0.1_x64.zip` từ mục Release.
2. **Giải nén & Chạy:** Giải nén và chạy trực tiếp file `Ernastie.exe` với quyền Administrator (`Run as Administrator`).
3. **Không cần cài đặt:** Ứng dụng tự chứa đầy đủ các thư viện cần thiết, hoạt động ngay lập tức trên Windows 10, Windows 11 và USB cứu hộ WinPE.

---
*Phát triển bởi Ernas / HoangMon - IT Tools for Support Project.*
