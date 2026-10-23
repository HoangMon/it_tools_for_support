# 🛠️ E-TOOLBOX - IT Tools for Support

<p align="center">
  <img src="src/Resources/app.png" alt="Ernastie Crafting Table Logo" width="128" height="128" />
</p>

<p align="center">
  <strong>Bộ công cụ toàn diện hỗ trợ kỹ thuật viên IT, cài đặt, bảo trì, chẩn đoán phần cứng và tối ưu hóa hệ điều hành Windows & WinPE.</strong>
</p>

<p align="center">
  <a href="https://github.com/HoangMon/it_tools_for_support/releases/latest"><img src="https://img.shields.io/github/v/release/HoangMon/it_tools_for_support?color=0078D7&label=Release&logo=github" alt="GitHub Release"></a>
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20%7C%20WinPE-blue?logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-8.0%20Windows%20Desktop-purple?logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/Architecture-x64%20Standalone-green" alt="Architecture">
  <img src="https://img.shields.io/badge/License-MIT-orange" alt="License">
</p>

---

## 📖 Giới thiệu (Overview)

**E-TOOLBOX** là giải pháp phần mềm chuyên dụng được thiết kế cho IT Helpdesk, Kỹ thuật viên bảo trì máy tính và người dùng yêu thích tối ưu hóa hệ thống.

Ứng dụng được đóng gói dưới dạng **Portable Single-File Executable (`.exe`)** tự chứa (.NET 8 Self-Contained), **hoạt động ngay lập tức** trên mọi máy tính mà không cần cài đặt và không yêu cầu cài trước bất kỳ thư viện hay .NET Runtime nào. Hoàn toàn tương thích và chạy mượt mà ngay cả trong môi trường cứu hộ **WinPE**.

---

## ✨ Tính năng nổi bật (Features)

### 1. 📊 Bảng điều khiển phần cứng (Hardware Dashboard)
- Giám sát thời gian thực: Tải CPU (%), xung nhịp, số nhân/luồng.
- Theo dõi RAM: Dung lượng sử dụng, dung lượng tổng, biểu đồ phần trăm.
- Nhận diện card màn hình (GPU): Hiển thị chính xác (hỗ trợ NVIDIA RTX, AMD Radeon, Intel Arc).
- Trạng thái phân vùng ổ đĩa: Dung lượng trống, tổng dung lượng và tỷ lệ chiếm dụng.

### 2. 🧪 Bộ công cụ Chẩn đoán & Kiểm tra (Check - Test)
- ⌨️ **Keyboard Tester**: Kiểm tra bàn phím trực quan thời gian thực, phát hiện phím liệt, kẹt phím, chập phím.
- 🖱️ **Mouse Tester**: Kiểm tra phím chuột Trái, Phải, Giữa, con lăn Scroll Wheel và độ nhạy Double Click.
- 🖥️ **Screen / Dead Pixel Tester**: Kiểm tra điểm chết và hở sáng màn hình qua chế độ toàn màn hình đa màu sắc.
- 🔊 **Audio & Speaker Tester**: Kiểm tra âm thanh Stereo tách biệt 2 kênh Trái (Left) và Phải (Right).
- 🎙️ **Microphone Tester**: Kiểm tra độ nhạy micro và hiển thị mức âm lượng thời gian thực (VU Meter).
- 🔋 **Battery Health Report**: Kiểm tra độ chai pin laptop, số chu kỳ sạc, công suất thiết kế vs hiện tại, xuất báo cáo Battery Report HTML.
- 🌐 **Network Diagnostic**: Kiểm tra card mạng, đo độ trễ Ping tới Google DNS (`8.8.8.8`) và Cloudflare (`1.1.1.1`).
- 💾 **Drive Health Summary**: Kiểm tra nhanh chuẩn kết nối (NVMe, SSD, HDD) và trạng thái phân vùng.

### 3. 🧹 Dọn dẹp rác hệ thống (System Cleaner)
- **Rác hệ thống**: Xóa sạch `Windows Temp`, `User Temp`, `Prefetch`, bộ nhớ đệm `Windows Update (SoftwareDistribution)`.
- **Trình duyệt Web**: Dọn dẹp Cache, Logs, Crash Reports cho Google Chrome, Microsoft Edge, Cốc Cốc, Brave, Mozilla Firefox.
- **Lịch sử & Bộ nhớ đệm**: Xóa lịch sử Run (MRU), Explorer Recent Files, DNS Cache (`Flush DNS`), Thumbnail Cache.
- **Tối ưu hóa ổ cứng**: Tự động dọn sạch Recycle Bin, kích hoạt lệnh TRIM tối ưu tốc độ SSD và kích hoạt Disk Cleanmgr.

### 4. ⚙️ Tinh chỉnh hệ điều hành (Windows Tweaks)
- **Menu chuột phải Windows 11 ⇄ Windows 10**: Chuyển đổi linh hoạt giữa giao diện Context Menu cổ điển và giao diện Win 11 mới chỉ với 1 click, tự động khởi động lại Explorer ngay lập tức.
- Bật/tắt các thiết lập hệ thống, gỡ bỏ ứng dụng rác (**Bloatware Remover**), gỡ phần mềm hàng loạt (**Batch Uninstaller**).

### 5. 💾 Sao lưu & Phục hồi (Backup & Recovery)
- **Data Backup**: Sao lưu nhanh dữ liệu người dùng (Desktop, Documents, Downloads, Favorites,...).
- **Driver Backup**: Sao lưu toàn bộ Driver hệ thống và phục hồi dễ dàng khi cài lại Windows.
- **Wi-Fi Backup**: Xuất danh sách tên và mật khẩu Wi-Fi đã từng kết nối trên máy tính.

### 6. 💿 Cứu hộ & Cài đặt hệ thống (WinPE & Deploy)
- **IRST Driver Loader**: Nạp nhanh Driver Intel Rapid Storage Technology cho laptop/PC Intel Gen 11, 12, 13, 14 bị ẩn ổ cứng khi cài đặt Windows.
- **Master Image & Ghost**: Hỗ trợ triển khai ảnh Windows (WIM / ESD / ISO).
- **WinPE Boot Config**: Hỗ trợ tạo và cấu hình USB boot cứu hộ.
- **Software Installer**: Trình cài đặt nhanh các phần mềm văn phòng và tiện ích thiết yếu.

---

## 📥 Tải về & Sử dụng (Download)

Bạn có thể tải ngay bản phát hành mới nhất tại mục Releases:

👉 **[Tải về Ernastie IT Tools (Bản mới nhất)](https://github.com/HoangMon/it_tools_for_support/releases/latest)**

| Tên file | Mô tả |
| :--- | :--- |
| **`Ernastie.exe`** | File chạy trực tiếp không cần giải nén (Portable Standalone Single-File) |
| **`Ernastie_vRelease_x64.zip`** | Gói nén ZIP bao gồm file thực thi |

> **Lưu ý**: Hãy click chuột phải vào file và chọn **Run as Administrator** để cấp đủ quyền hạn cho các tác vụ dọn dẹp hệ thống, TRIM SSD và cấu hình registry.

---

## 💻 Cấu hình yêu cầu (System Requirements)

- **Hệ điều hành**: Windows 10, Windows 11 hoặc các môi trường cứu hộ Windows PE (WinPE 64-bit).
- **Kiến trúc**: x64 (64-bit).
- **Yêu cầu cài đặt**: **Không** (Đã tích hợp sẵn .NET 8 Runtime bên trong file thực thi).

---

## 🔨 Hướng dẫn Build từ Source Code

Nếu bạn muốn tự biên dịch ứng dụng từ mã nguồn:

### Yêu cầu:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) trở lên.
- Visual Studio 2022 hoặc VS Code.

### Biên dịch nhanh bằng script:
Nhấp đúp chuột vào file:
```cmd
package_release.bat
```
Script sẽ tự động build bản **Single-File Release Self-Contained**, kích hoạt nén binary tối ưu và đóng gói sẵn file `.zip` vào thư mục `release/`.

### Biên dịch thủ công qua lệnh CLI:
```bash
# Clone repository
git clone https://github.com/HoangMon/it_tools_for_support.git
cd it_tools_for_support

# Build & Publish Single-File Executable
dotnet publish src/Ernastie.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -o release/single_file
```

---

## 📂 Cấu trúc thư mục (Project Structure)

```plaintext
it_tools_for_support/
├── src/
│   ├── Forms/             # Các giao diện chức năng (Dashboard, Cleaner, CheckTest,...)
│   ├── Services/          # Xử lý logic phần cứng, dọn dẹp rác, WMI, Registry,...
│   ├── Resources/         # Icon và tài nguyên ứng dụng
│   └── Ernastie.csproj    # Cấu hình project .NET 8 WinForms
├── tests/                 # Unit tests và Test Runner tự động
├── release/               # Thư mục chứa file đóng gói phát hành (.exe, .zip)
├── package_release.bat    # Script 1-click tự động đóng gói bản release
├── push_to_github.bat     # Script đẩy code lên GitHub
└── README.md              # Tài liệu dự án
```

---

## 📄 Bản quyền (License)

Dự án được phân phối dưới giấy phép **MIT License**. Bạn được tự do sử dụng, chỉnh sửa và phân phối cho mục đích cá nhân cũng như thương mại.
