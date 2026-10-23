# Ernastie Support Tool (C# .NET 8 WinForms)

<p align="center">
  <img src="Resources/app.png" alt="Ernastie Crafting Table Logo" width="112" height="112" />
</p>

Công cụ hỗ trợ IT, cài đặt và bảo trì Windows / WinPE chuyên nghiệp, hiện đại, nhẹ và đầy đủ 100% tính năng của tool gốc, khắc phục hoàn toàn lỗi cảnh báo virus nhầm (`Wacatac.B!ml`).

---

## 🖥️ Giao Diện Hiện Đại & Bố Cục Chức Năng

Ứng dụng được thiết kế theo giao diện thẻ Card phẳng (Modern Slate Theme), tối ưu hóa diện tích hiển thị, font chữ `Segoe UI` rõ nét, tốc độ khởi chạy tức thì.

### 1. Thanh Trạng Thái Hệ Thống (Top Header)
* **BitLocker:** Nhận diện thời gian thực các ổ đĩa bị khóa/mã hóa (ví dụ: `C: D:`).
* **Chứng thư .pfx:** Tự động rà soát file khóa bảo mật trong thư mục người dùng.
* **Chuẩn Boot:** Tự động phát hiện `GPT - UEFI` hoặc `MBR - Legacy BIOS`.
* **Lưu trữ IRST/VMD:** Kiểm tra phần cứng Intel Rapid Storage / VMD để cảnh báo trước khi cài Win.
* **Đường dẫn & User:** Hiển thị vị trí thực thi và tài khoản người dùng đang đăng nhập.

---

### 2. Tab 1: 💼 Chức Năng
* **Hệ thống:** Thiết lập Windows, Check thông tin máy chi tiết, Tắt BitLocker, Master Image (Sysprep/WIM), Driver IRST/VMD, Show WiFi & VPN.
* **Sao lưu - Khôi phục:** Sao lưu WiFi ra XML, Sao lưu Driver bằng DISM, Sao lưu dữ liệu cá nhân, Sao lưu trình duyệt, Mật khẩu & Chữ ký số, Cập nhật Driver hãng (Dell, HP, Lenovo, Asus...).
* **Cài đặt:** Cài đặt phần mềm nhanh, Gỡ nhiều phần mềm, Xóa Bloatware UWP Windows.
* **WinPE - Cài Win:** Tạo WinPE Boot, Fix limit chia drive, Cài lại Win 10/11 (áp ảnh WIM/ESD), Kho ISO Windows, Dọn boot + ổ WinPE, Lấy driver từ ổ cứng cũ.
* **Tiện ích khác:** Xóa file rác Temp/Prefetch, Check - Test phần cứng, Chuyển menu chuột phải Win 10, Tác vụ tự động, Hỗ trợ từ xa, Thiết lập Office.

---

### 3. Tab 2: ⚙️ Nâng Cao
* **Update Windows:** Reset PC, Chặn Windows Update vĩnh viễn, Chặn nâng cấp Win 10 lên 11, Nâng / Hạ Windows Edition, Đưa Win về đúng bản, Quản lý Startup.
* **Khi máy có sự cố:** Chẩn đoán kết nối mạng, Sửa lỗi file nén (.zip/.rar/.7z), Sửa lỗi kẹt máy in (Reset Print Spooler).
* **Cài đặt Windows:** Cài Win trong WinPE, Chuyển đổi qua lại giữa Windows 7 và Windows 10/11.
* **Linux thay Windows (xoá ổ C:):** Cài đặt tự động Ubuntu, Debian, Linux Mint, Kali Linux thay thế Windows (giữ ổ D:/E:).
* **Linux song song Windows (Dual Boot):** Cài đặt song song Ubuntu (v8.1), Debian (v9.1), Mint (v10.1), Kali (v11.1), Gỡ bỏ Linux & phục hồi Bootloader.
* **Windows LTSC / Server:** Tải và cài đặt tự động Windows Server 2019, 2022, 2025, Windows 10 LTSC, Windows 11 LTSC (1-Click Deployment Wizard).

---

### 4. Tab 3: 📱 Thiết Bị (Google Sheet Management)
* **Liên kết Google Sheet:** Kết nối Web App Google Apps Script, mở trang tính trực tiếp, tích hợp cửa sổ xuất mã Deploy Apps Script mẫu.
* **Khởi tạo PC - chạy thẳng:** Đổi tên máy tính tức thì, Quản lý danh sách máy và công ty, Đẩy cấu hình lên 1 hoặc 2 Google Sheets, Export .gs, Trích xuất Windows Product Key & so sánh key.
* **Đồng bộ với Google Sheet:** Phục hồi cấu hình PC từ đám mây, Kiểm tra trùng lặp thiết bị, Tải lại cấu hình.
* **Nhật ký thời gian thực:** Thanh tiến trình (ProgressBar 0-100%), nút Xóa log, khung Console RichTextBox màu tối trực quan.

---

## 🚀 Bản Đóng Gói Thực Thi

1. **Bản chuẩn nhẹ (Dành cho máy có .NET 8):**
   * [D:\Projects\it_tools_for_support\bin\Ernastie.exe](file:///d:/Projects/it_tools_for_support/bin/Ernastie.exe) (~715 KB)
2. **Bản độc lập Standalone (Dành cho USB WinPE cứu hộ):**
   * [D:\Projects\it_tools_for_support\bin\standalone\Ernastie.exe](file:///d:/Projects/it_tools_for_support/bin/standalone/Ernastie.exe)
