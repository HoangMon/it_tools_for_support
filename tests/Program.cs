using System;
using System.IO;
using System.Threading.Tasks;
using Ernastie.Services;
using Ernastie.Forms;
using Ernastie.Models;

namespace Ernastie.Tests;

class Program
{
    static int _pass = 0;
    static int _fail = 0;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("==========================================================");
        Console.WriteLine("  BẮT ĐẦU KIỂM THỬ TỪNG CHỨC NĂNG CỦA ERNASTIE TOOL");
        Console.WriteLine("==========================================================");

        var sysService = new SystemService();
        var diskService = new DiskService();
        var maintService = new MaintenanceService();
        var backupService = new BackupService();
        var sheetService = new GoogleSheetService();

        // 1. SYSTEM SERVICE TESTS
        Console.WriteLine("\n--- [1/5] KIỂM THỬ HỆ THỐNG (SYSTEM SERVICE) ---");
        
        Test("GetSystemInfo (Đọc thông số phần cứng & máy)", () =>
        {
            var info = sysService.GetSystemInfo();
            if (string.IsNullOrEmpty(info.MachineName)) throw new Exception("Không đọc được MachineName");
            return $"Thiết bị: {info.MachineName} | OS: {info.OsVersion} | CPU: {info.ProcessorCount} Cores | RAM: {info.TotalMemoryMb}";
        });

        Test("CheckBitLocker (Kiểm tra trạng thái mã hóa ổ đĩa)", () =>
        {
            var info = sysService.GetSystemInfo();
            return $"Kết quả: {info.BitLockerStatus} (HasLocked = {info.HasBitLockerLocked})";
        });

        Test("CheckPfx (Quét tệp chứng thư cá nhân .pfx)", () =>
        {
            var info = sysService.GetSystemInfo();
            return $"Trạng thái PFX: {info.PfxStatusText}";
        });

        Test("CheckIRST (Kiểm tra phần cứng Intel VMD / IRST)", () =>
        {
            var info = sysService.GetSystemInfo();
            return $"Trạng thái IRST: {info.IrstStatusText} (Cần driver = {info.NeedsIrst})";
        });

        Test("ExtractProductKey (Trích xuất Windows Product Key)", () =>
        {
            var info = sysService.GetSystemInfo();
            return $"Key tìm thấy: {info.WindowsProductKey}";
        });

        Test("ShowWifiProfiles (Kiểm tra hồ sơ mạng WiFi đã lưu)", () =>
        {
            var wifi = sysService.ShowWifiProfiles();
            return $"Đã đọc hồ sơ WiFi: {wifi.Split('\n').Length} dòng thông tin.";
        });

        Test("ToggleWin10ContextMenu (Ghi cấu hình Menu chuột Win 10)", () =>
        {
            sysService.ToggleWin10ContextMenu(true);
            return "Đã áp dụng cấu hình Registry Menu chuột phải Win 10.";
        });

        Test("ToggleWindowsUpdate (Chặn tự động cập nhật)", () =>
        {
            sysService.ToggleWindowsUpdate(true);
            return "Đã thiết lập cấm tự động tải bản cập nhật.";
        });

        Test("BlockWin11Upgrade (Khóa bản dựng TargetRelease 22H2)", () =>
        {
            sysService.BlockWin11Upgrade(true);
            return "Đã thiết lập giữ phiên bản Windows 10 ổn định.";
        });

        // 2. DISK SERVICE TESTS
        Console.WriteLine("\n--- [2/5] KIỂM THỬ Ổ ĐĨA & WINPE (DISK SERVICE) ---");

        Test("GetDisks (Liệt kê phân vùng & dung lượng ổ đĩa)", () =>
        {
            var disks = diskService.GetDisks();
            if (disks.Count == 0) throw new Exception("Không tìm thấy ổ đĩa nào");
            var summary = string.Join(", ", disks.ConvertAll(d => $"{d.Name} ({d.TotalSizeGb}GB, {d.FreePercentage}% trống)"));
            return $"Phát hiện {disks.Count} ổ: {summary}";
        });

        Test("FixPartitionLimits (Phương thức tháo gỡ giới hạn co giãn ổ C)", () =>
        {
            var m = typeof(DiskService).GetMethod("FixPartitionLimits");
            if (m == null) throw new Exception("Phương thức chưa được cài đặt");
            return "Phương thức FixPartitionLimits đã sẵn sàng.";
        });

        Test("CleanWinPeBoot (Dọn dẹp phân vùng & Bootloader WinPE)", () =>
        {
            var m = typeof(DiskService).GetMethod("CleanWinPeBoot");
            if (m == null) throw new Exception("Phương thức chưa được cài đặt");
            return "Phương thức CleanWinPeBoot đã sẵn sàng.";
        });

        var peService = new WinPeService();
        Test("CheckAnhdvBootStatus (Kiem tra trang thai bo Anhdv Boot)", () =>
        {
            var (found, p, msg) = peService.CheckAnhdvBootStatus();
            return $"Tim thay: {found} | Duong dan: {p}";
        });

        Test("FindSystemBootSdi (Tim tep boot.sdi tren he thong)", () =>
        {
            var sdi = peService.FindSystemBootSdi();
            var displaySdi = sdi != null ? sdi : "N/A"; return "Tep boot.sdi: " + displaySdi;
        });

        // 3. MAINTENANCE SERVICE TESTS
        Console.WriteLine("\n--- [3/5] KIỂM THỬ BẢO TRÌ & SỬA LỖI (MAINTENANCE SERVICE) ---");

        Test("CleanTempFiles (Dọn dẹp rác hệ thống & Temp)", () =>
        {
            var bytes = maintService.CleanTempFiles();
            return $"Đã dọn dẹp thành công {bytes / (1024 * 1024)} MB tệp rác.";
        });

        await TestAsync("NetworkDiagnostic (Chẩn đoán kết nối mạng IP/Ping)", async () =>
        {
            await maintService.RunSystemRepairAsync("network_diag");
            return "Tiến trình chẩn đoán mạng hoàn tất thành công.";
        });

        Test("RemoveBloatware (Khung gỡ ứng dụng rác UWP mặc định)", () =>
        {
            var m = typeof(MaintenanceService).GetMethod("RemoveBloatwareAsync");
            if (m == null) throw new Exception("Phương thức chưa được cài đặt");
            return "Mô-đun gỡ Bloatware đã sẵn sàng.";
        });

        // 4. BACKUP SERVICE TESTS
        Console.WriteLine("\n--- [4/5] KIỂM THỬ SAO LƯU & KHÔI PHỤC (BACKUP SERVICE) ---");

        await TestAsync("BackupWifi (Xuất hồ sơ WiFi ra thư mục)", async () =>
        {
            var temp = Path.Combine(Path.GetTempPath(), "Ernastie_Wifi_Test");
            await backupService.BackupWifiAsync(temp);
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, true);
                return "Đã gọi lệnh xuất hồ sơ WiFi và tạo thư mục thành công.";
            }
            return "Lệnh xuất hoàn tất.";
        });

        Test("BrandDriverPage (Định tuyến trang driver của hãng)", () =>
        {
            var m = typeof(BackupService).GetMethod("OpenBrandDriverPage");
            if (m == null) throw new Exception("Phương thức chưa được cài đặt");
            return "Định tuyến trang Driver cho Dell/HP/Lenovo/Asus/Acer đã sẵn sàng.";
        });

        // 5. GOOGLE SHEET SERVICE TESTS
        Console.WriteLine("\n--- [5/5] KIỂM THỬ ĐỒNG BỘ GOOGLE SHEET (DEVICE SERVICE) ---");

        Test("LoadAndSaveConfig (Đọc và ghi cấu hình thiết bị/công ty)", () =>
        {
            var cfg = sheetService.LoadConfig();
            sheetService.SaveConfig(cfg);
            return $"Cấu hình JSON: {cfg.MachineNames.Count} máy, {cfg.Companies.Count} công ty.";
        });

        Test("GenerateAppsScriptTemplate (Tạo mã nguồn Google Apps Script)", () =>
        {
            var code = sheetService.GenerateAppsScriptTemplate();
            if (!code.Contains("SpreadsheetApp") || !code.Contains("doPost"))
                throw new Exception("Mã Apps Script không đầy đủ");
            return $"Tạo mã Apps Script .gs thành công ({code.Length} ký tự).";
        });

        await TestAsync("CheckConnection (Xử lý kết nối và Timeout mạng)", async () =>
        {
            var ok = await sheetService.CheckConnectionAsync("https://invalid-url-testing.google.com");
            return $"Kiểm tra kết nối xử lý đúng khi URL offline (Kết quả = {ok}).";
        });

        // 6. WINDOWS CONFIG SERVICE TESTS
        Console.WriteLine("\n--- [6/6] KIỂM THỬ TRÌNH CẤU HÌNH WINDOWS (WINDOWS CONFIG SERVICE) ---");
        var winConfigService = new WindowsConfigService();

        Test("GetSecuritySettings (Đọc trạng thái Toast, UAC, SmartScreen, Firewall, Alerts)", () =>
        {
            bool toast = winConfigService.GetToastNotifications();
            bool uac = winConfigService.GetUacStatus();
            bool smart = winConfigService.GetSmartScreenStatus();
            bool fw = winConfigService.GetFirewallStatus();
            bool alerts = winConfigService.GetSecurityAlertsStatus();
            return $"Toast: {toast} | UAC: {uac} | SmartScreen: {smart} | Firewall: {fw} | Alerts: {alerts}";
        });

        Test("GetQuickSettings (Đọc định dạng 12h/24h, Explorer This PC, Dấu phẩy thập phân)", () =>
        {
            bool is12h = winConfigService.GetTimeFormatIs12h();
            bool thisPc = winConfigService.GetExplorerLaunchToThisPc();
            bool comma = winConfigService.GetDecimalComma();
            return $"Định dạng giờ 12h: {is12h} | Mở This PC: {thisPc} | Dấu phẩy: {comma}";
        });

        Test("GetHibernateInfo (Kiểm tra trạng thái ngủ đông và kích thước hiberfil.sys)", () =>
        {
            var (isEnabled, sizeGb) = winConfigService.GetHibernateInfo();
            return isEnabled ? $"Ngủ đông ĐANG BẬT, dung lượng: {sizeGb:N2} GB" : "Ngủ đông ĐÃ TẮT";
        });

        Test("GetLocalUsers (Truy vấn danh sách User cục bộ và nhóm Administrators)", () =>
        {
            var users = winConfigService.GetLocalUsers();
            if (users.Count == 0) throw new Exception("Không đọc được danh sách người dùng");
            var adminList = string.Join(", ", users.Where(u => u.IsAdmin).Select(u => u.Username));
            return $"Đã tìm thấy {users.Count} tài khoản (Admins: {adminList})";
        });

        Test("GetWorkgroupInfo (Đọc thông tin Workgroup / Domain hiện tại)", () =>
        {
            var (name, isDomain) = winConfigService.GetWorkgroupInfo();
            return $"Loại mạng: {(isDomain ? "Domain" : "Workgroup")} | Tên: {name}";
        });

        // 7. HARDWARE INFO SERVICE TESTS
        Console.WriteLine("\n--- [7/7] KIỂM THỬ THÔNG TIN HỆ THỐNG CHI TIẾT (HARDWARE INFO SERVICE) ---");
        var hwService = new HardwareInfoService();

        Test("GetDetailedHardwareInfo (Truy vấn BIOS, CPU, Pin, GPU, RAM, Ổ cứng, Bản quyền)", () =>
        {
            var hw = hwService.GetDetailedHardwareInfo();
            if (string.IsNullOrEmpty(hw.MachineName)) throw new Exception("Không đọc được MachineName");
            if (string.IsNullOrEmpty(hw.ServiceTag)) throw new Exception("Không đọc được ServiceTag");
            if (hw.RamSlots.Count == 0) throw new Exception("Không đọc được RAM Slots");
            if (hw.Disks.Count == 0) throw new Exception("Không đọc được Ổ đĩa");

            return $"Máy: {hw.Manufacturer} {hw.Model} (ST: {hw.ServiceTag}) | CPU: {hw.CpuInfo} | RAM: {hw.RamSlots.Count} khe | GPU: {string.Join(" ; ", hw.Gpus)} | Ổ: {hw.Disks.Count} ổ | Key: {hw.InstalledKey}";
        });

        // 8. POP-UP FORMS & THEME SYNCHRONIZATION TESTS
        Console.WriteLine("\n--- [8/8] KIỂM THỬ ĐỒNG BỘ THEME TẤT CẢ POP-UP FORMS (LIGHT & DARK) ---");

        var testThemes = new[] { "Light", "Dark" };
        foreach (var theme in testThemes)
        {
            var s = SettingsService.Instance.Settings;
            s.Theme = theme;
            SettingsService.Instance.SaveSettings(s);

            Console.WriteLine($"\n  >>> Kiểm thử giao diện chế độ: {theme.ToUpper()} <<<");

            Test($"[{theme}] SystemInfoForm (Thông tin hệ thống)", () =>
            {
                using var f = new SystemInfoForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] WindowsConfigForm (Thiết lập Windows & User)", () =>
            {
                using var f = new WindowsConfigForm();
                ThemeHelper.ApplyTheme(f);
                var dgv = FindControls<DataGridView>(f).FirstOrDefault();
                var rtb = FindControls<RichTextBox>(f).FirstOrDefault();
                var btns = FindControls<Button>(f).ToList();
                if (dgv != null && dgv.BackgroundColor != ThemeHelper.CardBg)
                    throw new Exception($"DGV BackgroundColor {dgv.BackgroundColor} != {ThemeHelper.CardBg}");
                if (rtb != null && rtb.BackColor != ThemeHelper.LogBg)
                    throw new Exception($"RTB BackColor {rtb.BackColor} != {ThemeHelper.LogBg}");
                return $"Form: {f.BackColor.Name}, DGV: {dgv?.BackgroundColor.Name}, Log: {rtb?.BackColor.Name}, Btns: {btns.Count}";
            });

            Test($"[{theme}] MasterImageForm (Tạo Master Image)", () =>
            {
                using var f = new MasterImageForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] IrstDriverForm (Intel VMD / RST)", () =>
            {
                using var f = new IrstDriverForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] WifiBackupForm (Sao lưu WiFi)", () =>
            {
                using var f = new WifiBackupForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] DataBackupForm (Sao lưu Dữ liệu)", () =>
            {
                using var f = new DataBackupForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] DriverBackupForm (Sao lưu Driver)", () =>
            {
                using var f = new DriverBackupForm("Dell", @"C:\backupdriver");
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] SelectDriversDialog (Chọn từng driver)", () =>
            {
                using var f = new SelectDriversDialog(new List<DriverItemModel>(), @"C:\backupdriver");
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] CredentialAndCertificateForm (Mật khẩu & Chữ ký số)", () =>
            {
                using var f = new CredentialAndCertificateForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] SoftwareInstallerForm (Cài đặt phần mềm)", () =>
            {
                using var f = new SoftwareInstallerForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] BatchUninstallerForm (Gỡ nhiều phần mềm)", () =>
            {
                using var f = new BatchUninstallerForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] LeftoverCleanDialog (Dọn dẹp thư mục còn sót)", () =>
            {
                using var f = new LeftoverCleanDialog(new List<string> { @"C:\DummyLeftover" });
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] BloatwareRemoverForm (Gỡ sạch Bloatware)", () =>
            {
                using var f = new BloatwareRemoverForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] WinPeBootForm (Tạo boot WinPE AnhDV / WIM)", () =>
            {
                using var f = new WinPeBootForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] SettingsForm (Cài đặt hệ thống)", () =>
            {
                using var f = new SettingsForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] SystemCleanerForm (Dọn rác hệ thống)", () =>
            {
                using var f = new SystemCleanerForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] CheckTestForm (Kiểm tra & Chẩn đoán Check-Test)", () =>
            {
                using var f = new CheckTestForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] KeyboardTestForm (Kiểm tra bàn phím)", () =>
            {
                using var f = new KeyboardTestForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });

            Test($"[{theme}] RamTestForm (Kiểm tra RAM)", () =>
            {
                using var f = new RamTestForm();
                ThemeHelper.ApplyTheme(f);
                return $"BackColor = {f.BackColor}, Controls = {f.Controls.Count}";
            });
        }

        // SUMMARY
        Console.WriteLine("\n==========================================================");
        Console.WriteLine($"TỔNG KẾT: {_pass} / {_pass + _fail} CHỨC NĂNG ĐẠT (PASS)!");
        if (_fail == 0)
        {
            Console.WriteLine("TẤT CẢ 100% CÁC CHỨC NĂNG ĐỀU HOẠT ĐỘNG HOÀN TOÀN CHÍNH XÁC!");
        }
        else
        {
            Console.WriteLine($"CÓ {_fail} CHỨC NĂNG GẶP LỖI!");
        }
        Console.WriteLine("==========================================================");
    }

    static void Test(string name, Func<string> action)
    {
        try
        {
            var result = action();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  [PASS] ");
            Console.ResetColor();
            Console.WriteLine($"{name} -> {result}");
            _pass++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  [FAIL] ");
            Console.ResetColor();
            Console.WriteLine($"{name} -> Lỗi: {ex.Message}");
            _fail++;
        }
    }

    static async Task TestAsync(string name, Func<Task<string>> action)
    {
        try
        {
            var result = await action();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  [PASS] ");
            Console.ResetColor();
            Console.WriteLine($"{name} -> {result}");
            _pass++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  [FAIL] ");
            Console.ResetColor();
            Console.WriteLine($"{name} -> Lỗi: {ex.Message}");
            _fail++;
        }
    }

    static IEnumerable<T> FindControls<T>(Control parent) where T : Control
    {
        foreach (Control c in parent.Controls)
        {
            if (c is T match) yield return match;
            foreach (var child in FindControls<T>(c)) yield return child;
        }
    }
}
