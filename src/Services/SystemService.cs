using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Ernastie.Models;

namespace Ernastie.Services;

public class SystemService
{
    [DllImport("kernel32.dll")]
    private static extern bool GetFirmwareType(ref uint firmwareType);

    public SystemInfoModel GetSystemInfo()
    {
        var model = new SystemInfoModel
        {
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            OsVersion = GetFriendlyOsVersion(),
            OsArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessorName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "x64 Processor",
            ProcessorCount = Environment.ProcessorCount,
            IsWinPE = CheckIfWinPE(),
            BootMode = GetBootMode(),
            RunningPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')
        };

        try
        {
            var gcMem = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            model.TotalMemoryMb = $"{gcMem / (1024 * 1024):N0} MB";
        }
        catch
        {
            model.TotalMemoryMb = "N/A";
        }

        // Hardware details via WMI (if available)
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (var mo in searcher.Get())
            {
                model.Manufacturer = mo["Manufacturer"]?.ToString() ?? "";
                model.Model = mo["Model"]?.ToString() ?? "";
            }

            using var bioSearcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (var mo in bioSearcher.Get())
            {
                model.SerialNumber = mo["SerialNumber"]?.ToString() ?? "";
            }
        }
        catch { }

        // Check BitLocker
        CheckBitLockerStatus(model);

        // Check PFX files
        CheckPfxStatus(model);

        // Check IRST / VMD
        CheckIrstStatus(model);

        // Extract Windows Key
        model.WindowsProductKey = ExtractWindowsKey();

        return model;
    }

    private string GetFriendlyOsVersion()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                var prod = key.GetValue("ProductName")?.ToString();
                var disp = key.GetValue("DisplayVersion")?.ToString();
                if (!string.IsNullOrEmpty(prod))
                {
                    return string.IsNullOrEmpty(disp) ? prod : $"{prod} ({disp})";
                }
            }
        }
        catch { }
        return Environment.OSVersion.VersionString;
    }

    private void CheckBitLockerStatus(SystemInfoModel model)
    {
        try
        {
            var lockedDrives = new List<string>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed) continue;
                var letter = drive.Name.TrimEnd('\\');
                try
                {
                    // Check protection status via manage-bde
                    using var proc = new Process();
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "manage-bde.exe",
                        Arguments = $"-status {letter}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    proc.Start();
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    if (output.Contains("Protection On") || output.Contains("Locked") || output.Contains("Bảo vệ: Bật"))
                    {
                        lockedDrives.Add(letter);
                    }
                }
                catch { }
            }

            if (lockedDrives.Count > 0)
            {
                model.HasBitLockerLocked = true;
                model.BitLockerStatus = $"ổ đang KHÓA/mã hóa: {string.Join(" ", lockedDrives)}";
            }
            else
            {
                model.HasBitLockerLocked = false;
                model.BitLockerStatus = "Không có ổ bị khóa BitLocker";
            }
        }
        catch
        {
            model.BitLockerStatus = "Chưa phát hiện BitLocker";
        }
    }

    private void CheckPfxStatus(SystemInfoModel model)
    {
        try
        {
            var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var locations = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(userDir, "Downloads")
            };

            int pfxCount = 0;
            foreach (var loc in locations)
            {
                if (Directory.Exists(loc))
                {
                    pfxCount += Directory.GetFiles(loc, "*.pfx", SearchOption.TopDirectoryOnly).Length;
                }
            }

            if (pfxCount > 0)
            {
                model.HasPfxFiles = true;
                model.PfxStatusText = $"Phát hiện {pfxCount} file chứng thư .pfx";
            }
            else
            {
                model.HasPfxFiles = false;
                model.PfxStatusText = "Không phát hiện file .pfx";
            }
        }
        catch
        {
            model.PfxStatusText = "Không phát hiện file .pfx";
        }
    }

    private void CheckIrstStatus(SystemInfoModel model)
    {
        try
        {
            bool hasVmd = false;
            using var searcher = new ManagementObjectSearcher("SELECT DeviceID, Name FROM Win32_PnPEntity WHERE Name LIKE '%VMD%' OR Name LIKE '%RST%' OR Name LIKE '%Rapid Storage%'");
            foreach (var _ in searcher.Get())
            {
                hasVmd = true;
                break;
            }

            if (hasVmd)
            {
                model.NeedsIrst = true;
                model.IrstStatusText = "CẦN driver IRST (VMD)";
            }
            else
            {
                model.NeedsIrst = false;
                model.IrstStatusText = "Không cần driver IRST";
            }
        }
        catch
        {
            model.IrstStatusText = "Chuẩn lưu trữ bình thường";
        }
    }

    public string ExtractWindowsKey()
    {
        try
        {
            // First check firmware OA3 Key (UEFI embedded OEM key)
            using var searcher = new ManagementObjectSearcher("SELECT OA3xOriginalProductKey FROM SoftwareLicensingService");
            foreach (var mo in searcher.Get())
            {
                var k = mo["OA3xOriginalProductKey"]?.ToString();
                if (!string.IsNullOrEmpty(k)) return k;
            }

            // Fallback: Registry DigitalProductId decoding
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("DigitalProductId") is byte[] digitalProductId)
            {
                return DecodeProductKey(digitalProductId);
            }
        }
        catch { }

        return "N/A hoặc Bản quyền số kỹ thuật số (Digital License)";
    }

    private string DecodeProductKey(byte[] digitalProductId)
    {
        try
        {
            const int keyOffset = 52;
            const string digits = "BCDFGHJKMPQRTVWXY2346789";
            var keyChars = new char[29];
            var raw = new List<byte>();

            for (int i = keyOffset; i <= keyOffset + 15; i++)
            {
                raw.Add(digitalProductId[i]);
            }

            for (int i = 28; i >= 0; i--)
            {
                if ((i + 1) % 6 == 0)
                {
                    keyChars[i] = '-';
                }
                else
                {
                    int digitMap = 0;
                    for (int j = 14; j >= 0; j--)
                    {
                        int byteVal = (digitMap << 8) | raw[j];
                        raw[j] = (byte)(byteVal / 24);
                        digitMap = byteVal % 24;
                    }
                    keyChars[i] = digits[digitMap];
                }
            }
            return new string(keyChars);
        }
        catch
        {
            return "Digital License";
        }
    }

    public string ShowWifiProfiles()
    {
        try
        {
            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c netsh wlan show profiles",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output;
        }
        catch (Exception ex)
        {
            return $"Lỗi khi kiểm tra WiFi: {ex.Message}";
        }
    }

    public void TurnOffBitLocker(string drive = "C:")
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c manage-bde -off {drive} && pause",
                UseShellExecute = true
            });
        }
        catch { }
    }

    public bool IsWindows11OrGreater()
    {
        return Environment.OSVersion.Version.Build >= 22000;
    }

    public bool IsWin10ContextMenuActive()
    {
        // Trên Windows 10 hoặc cũ hơn, hệ thống luôn mặc định là menu chuột phải cổ điển
        if (!IsWindows11OrGreater())
        {
            return true;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}");
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    public void ToggleWin10ContextMenu(bool useWin10Menu, bool restartExplorer = true)
    {
        try
        {
            const string subKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
            if (useWin10Menu)
            {
                using var key = Registry.CurrentUser.CreateSubKey(subKey);
                key?.SetValue("", "");
            }
            else
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false);
            }

            if (restartExplorer)
            {
                foreach (var p in Process.GetProcessesByName("explorer"))
                {
                    try { p.Kill(); } catch { }
                }
                Thread.Sleep(500);
                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
            }
        }
        catch { }
    }

    public void ToggleWindowsUpdate(bool block)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU");
            if (block)
            {
                key?.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
                Process.Start(new ProcessStartInfo("cmd.exe", "/c net stop wuauserv & sc config wuauserv start= disabled") { CreateNoWindow = true, UseShellExecute = false });
            }
            else
            {
                key?.DeleteValue("NoAutoUpdate", false);
                Process.Start(new ProcessStartInfo("cmd.exe", "/c sc config wuauserv start= auto & net start wuauserv") { CreateNoWindow = true, UseShellExecute = false });
            }
        }
        catch { }
    }

    public void BlockWin11Upgrade(bool block)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
            if (block)
            {
                key?.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
                key?.SetValue("TargetReleaseVersionInfo", "22H2", RegistryValueKind.String);
                key?.SetValue("ProductVersion", "Windows 10", RegistryValueKind.String);
            }
            else
            {
                key?.DeleteValue("TargetReleaseVersion", false);
                key?.DeleteValue("TargetReleaseVersionInfo", false);
                key?.DeleteValue("ProductVersion", false);
            }
        }
        catch { }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public bool ResetPrintSpooler(Action<string>? log = null)
    {
        try
        {
            log?.Invoke("Đang dừng dịch vụ Print Spooler...");
            var p1 = Process.Start(new ProcessStartInfo("cmd.exe", "/c net stop spooler") { CreateNoWindow = true, UseShellExecute = false });
            p1?.WaitForExit(10000);

            log?.Invoke("Đang dọn dẹp hàng đợi in trong %systemroot%\\System32\\Spool\\Printers...");
            string spoolDir = Path.Combine(Environment.SystemDirectory, @"spool\PRINTERS");
            int deleted = 0;
            if (Directory.Exists(spoolDir))
            {
                foreach (var f in Directory.GetFiles(spoolDir, "*.*"))
                {
                    try { File.Delete(f); deleted++; } catch { }
                }
            }
            log?.Invoke($"Đã xóa {deleted} tệp kẹt lệnh in.");

            log?.Invoke("Đang khởi động lại dịch vụ Print Spooler...");
            var p2 = Process.Start(new ProcessStartInfo("cmd.exe", "/c sc config spooler start= auto && net start spooler") { CreateNoWindow = true, UseShellExecute = false });
            p2?.WaitForExit(10000);

            log?.Invoke("✅ Đã khởi động lại dịch vụ máy in (Print Spooler) thành công!");
            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"[Lỗi xử lý máy in]: {ex.Message}");
            return false;
        }
    }

    public bool FixCompressedFileAssociations(Action<string>? log = null)
    {
        try
        {
            log?.Invoke("Đang khôi phục liên kết mặc định cho file nén (.zip)...");
            using (var key = Registry.ClassesRoot.CreateSubKey(".zip"))
            {
                key?.SetValue("", "CompressedFolder");
                key?.SetValue("Content Type", "application/x-zip-compressed");
            }
            using (var key = Registry.ClassesRoot.CreateSubKey(@"CompressedFolder\Shell\Open\Command"))
            {
                key?.SetValue("", @"%SystemRoot%\Explorer.exe /idlist,%I,%L");
            }

            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.zip\UserChoice", false);
            }
            catch { }

            string p7z = @"C:\Program Files\7-Zip\7zFM.exe";
            string pRar = @"C:\Program Files\WinRAR\WinRAR.exe";
            if (File.Exists(p7z))
            {
                log?.Invoke($"Phát hiện 7-Zip tại {p7z}. Đăng ký xử lý .7z...");
                using var k7 = Registry.ClassesRoot.CreateSubKey(".7z");
                k7?.SetValue("", "7-Zip.7z");
            }
            if (File.Exists(pRar))
            {
                log?.Invoke($"Phát hiện WinRAR tại {pRar}. Đăng ký xử lý .rar...");
                using var kR = Registry.ClassesRoot.CreateSubKey(".rar");
                kR?.SetValue("", "WinRAR");
            }

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            log?.Invoke("✅ Đã khôi phục liên kết tệp nén thành công!");
            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"[Lỗi khôi phục liên kết]: {ex.Message}");
            return false;
        }
    }

    public string GetWindowsEditionsInfo()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = "/online /get-currentedition",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string outCurrent = p.StandardOutput.ReadToEnd();
                p.WaitForExit(10000);

                var psiTarget = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = "/online /get-targeteditions",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p2 = Process.Start(psiTarget);
                string outTarget = p2 != null ? p2.StandardOutput.ReadToEnd() : "";
                p2?.WaitForExit(10000);

                return $"=== THÔNG TIN PHIÊN BẢN WINDOWS HIỆN TẠI ===\n{outCurrent}\n=== CÁC PHIÊN BẢN CÓ THỂ NÂNG CẤP TRỰC TIẾP ===\n{outTarget}";
            }
        }
        catch (Exception ex)
        {
            return $"Lỗi truy vấn DISM: {ex.Message}";
        }
        return "Không thể truy vấn thông tin phiên bản Windows qua DISM.";
    }

    private bool CheckIfWinPE()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\MiniNT");
            if (key != null) return true;

            var peShl = Path.Combine(Environment.SystemDirectory, "winpeshl.ini");
            return File.Exists(peShl);
        }
        catch
        {
            return false;
        }
    }

    private string GetBootMode()
    {
        try
        {
            uint type = 0;
            if (GetFirmwareType(ref type))
            {
                return type switch
                {
                    1 => "MBR - Legacy BIOS",
                    2 => "GPT - UEFI",
                    _ => "GPT - UEFI"
                };
            }
        }
        catch { }
        return "GPT - UEFI";
    }

    public void RestartSystem(int delaySeconds = 0)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = $"/r /t {delaySeconds}",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    public void ShutdownSystem(int delaySeconds = 0)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = $"/s /t {delaySeconds}",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    public string GetActiveMacAddress()
    {
        try
        {
            var nic = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                                     n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);
            if (nic != null)
            {
                var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length > 0)
                {
                    return string.Join(":", bytes.Select(b => b.ToString("X2")));
                }
            }
        }
        catch { }
        return "N/A";
    }
}
