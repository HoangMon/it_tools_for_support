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

    public bool IsWin10ContextMenuActive()
    {
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

    public void ResetPrintSpooler()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c net stop spooler && del /Q /F /S \"%systemroot%\\System32\\Spool\\Printers\\*.*\" && net start spooler",
                UseShellExecute = true
            });
        }
        catch { }
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
}
