using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.Win32;

namespace Ernastie.Services;

public class UserModel
{
    public string Username { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public bool IsDisabled { get; set; }
    public bool PasswordNeverExpires { get; set; }
    public bool DeletePassword { get; set; }
    public bool DeleteUser { get; set; }
    public bool DeleteProfile { get; set; }
}

public class WindowsConfigService
{
    public delegate void OutputHandler(string message);

    #region 1. Bảo Mật & Tường Lửa (Security & Firewall)

    public bool GetToastNotifications()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\PushNotifications");
            if (key != null)
            {
                var val = key.GetValue("ToastEnabled");
                if (val is int i) return i != 0;
            }
        }
        catch { }
        return true;
    }

    public void SetToastNotifications(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\PushNotifications");
            key?.SetValue("ToastEnabled", enabled ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }

    public bool GetUacStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            if (key != null)
            {
                var val = key.GetValue("EnableLUA");
                if (val is int i) return i != 0;
            }
        }
        catch { }
        return true;
    }

    public void SetUacStatus(bool enabled)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            key?.SetValue("EnableLUA", enabled ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }

    public bool GetSmartScreenStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
            if (key != null)
            {
                var val = key.GetValue("SmartScreenEnabled") as string;
                if (!string.IsNullOrEmpty(val) && val.Equals("Off", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }
        catch { }
        return true;
    }

    public void SetSmartScreenStatus(bool enabled)
    {
        try
        {
            using var keyExp = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
            keyExp?.SetValue("SmartScreenEnabled", enabled ? "Warn" : "Off", RegistryValueKind.String);

            using var keyApp = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AppHost");
            keyApp?.SetValue("EnableWebContentEvaluation", enabled ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }

    public bool GetFirewallStatus()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "advfirewall show allprofiles state",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(2000);
                return output.Contains("ON", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { }
        return true;
    }

    public void SetFirewallStatus(bool enabled)
    {
        try
        {
            var state = enabled ? "on" : "off";
            RunHiddenCmd($"netsh advfirewall set allprofiles state {state}");
        }
        catch { }
    }

    public bool GetSecurityAlertsStatus()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.SecurityAndMaintenance");
            if (key != null)
            {
                var val = key.GetValue("Enabled");
                if (val is int i) return i != 0;
            }
        }
        catch { }
        return true;
    }

    public void SetSecurityAlertsStatus(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.SecurityAndMaintenance");
            key?.SetValue("Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }

    #endregion

    #region 2. Thiết Lập Nhanh (Quick Settings)

    public void SetGmt7AndDateFormat(OutputHandler? log = null)
    {
        try
        {
            log?.Invoke("Đang đặt múi giờ GMT+7 (SE Asia Standard Time)...");
            RunHiddenCmd("tzutil /s \"SE Asia Standard Time\"");

            log?.Invoke("Đang đồng bộ lại đồng hồ hệ thống...");
            RunHiddenCmd("w32tm /resync /force");

            log?.Invoke("Đang thiết lập định dạng ngày: dd/MM/yyyy...");
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\International");
            if (key != null)
            {
                key.SetValue("sShortDate", "dd/MM/yyyy", RegistryValueKind.String);
                key.SetValue("sLongDate", "dddd, d MMMM, yyyy", RegistryValueKind.String);
                key.SetValue("sDate", "/", RegistryValueKind.String);
            }
            log?.Invoke("✅ Hoàn tất thiết lập giờ GMT+7 và định dạng dd/MM/yyyy!");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi thiết lập giờ: {ex.Message}");
        }
    }

    public bool GetTimeFormatIs12h()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\International");
            if (key != null)
            {
                var val = key.GetValue("sTimeFormat") as string;
                if (!string.IsNullOrEmpty(val) && val.Contains("tt", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }
        return true;
    }

    public void SetTimeFormat(bool is12h)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\International");
            if (key != null)
            {
                key.SetValue("sTimeFormat", is12h ? "hh:mm:ss tt" : "HH:mm:ss", RegistryValueKind.String);
                key.SetValue("sShortTime", is12h ? "hh:mm tt" : "HH:mm", RegistryValueKind.String);
            }
        }
        catch { }
    }

    public bool GetExplorerLaunchToThisPc()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            if (key != null)
            {
                var val = key.GetValue("LaunchTo");
                if (val is int i) return i == 1; // 1 = This PC, 2 = Quick Access
            }
        }
        catch { }
        return true;
    }

    public void SetExplorerLaunchToThisPc(bool thisPc)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            key?.SetValue("LaunchTo", thisPc ? 1 : 2, RegistryValueKind.DWord);
        }
        catch { }
    }

    public bool GetDecimalComma()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\International");
            if (key != null)
            {
                var val = key.GetValue("sDecimal") as string;
                return val == ",";
            }
        }
        catch { }
        return false;
    }

    public void SetDecimalComma(bool comma)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\International");
            if (key != null)
            {
                key.SetValue("sDecimal", comma ? "," : ".", RegistryValueKind.String);
                key.SetValue("sThousand", comma ? "." : ",", RegistryValueKind.String);
            }
        }
        catch { }
    }

    public void ToggleShowHiddenFiles(OutputHandler? log = null)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            if (key != null)
            {
                int current = key.GetValue("Hidden") is int h ? h : 2;
                bool willShow = current != 1;

                key.SetValue("Hidden", willShow ? 1 : 2, RegistryValueKind.DWord);
                key.SetValue("ShowSuperHidden", willShow ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("HideFileExt", willShow ? 0 : 1, RegistryValueKind.DWord);

                RestartExplorer();
                log?.Invoke(willShow ? "✅ Đã BẬT hiển thị file ẩn và đuôi tệp tin!" : "✅ Đã ẨN file ẩn theo mặc định.");
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi hiện file ẩn: {ex.Message}");
        }
    }

    public void SetAdaptiveBrightness(bool disable, OutputHandler? log = null)
    {
        try
        {
            int val = disable ? 0 : 1;
            RunHiddenCmd($"powercfg -setacvalueindex SCHEME_CURRENT SUB_VIDEO ADAPTBRIGHT {val}");
            RunHiddenCmd($"powercfg -setdcvalueindex SCHEME_CURRENT SUB_VIDEO ADAPTBRIGHT {val}");
            RunHiddenCmd("powercfg -setactive SCHEME_CURRENT");
            log?.Invoke(disable ? "✅ Đã tắt tự động điều chỉnh độ sáng màn hình." : "Đã kích hoạt cảm biến độ sáng.");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi thiết lập độ sáng: {ex.Message}");
        }
    }

    public void KeepOnlyUsKeyboard(OutputHandler? log = null)
    {
        try
        {
            log?.Invoke("Đang giữ lại bàn phím chuẩn US (en-US) và xóa các bố cục khác...");
            RunHiddenCmd("powershell -NoProfile -Command \"$l = New-WinUserLanguageList 'en-US'; Set-WinUserLanguageList $l -Force\"");
            log?.Invoke("✅ Đã thiết lập chỉ sử dụng bàn phím English (US)!");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi xóa bàn phím phụ: {ex.Message}");
        }
    }

    public (bool isEnabled, double sizeGb) GetHibernateInfo()
    {
        try
        {
            var hiberPath = @"C:\hiberfil.sys";
            if (File.Exists(hiberPath))
            {
                var fi = new FileInfo(hiberPath);
                double gb = (double)fi.Length / (1024 * 1024 * 1024);
                return (true, Math.Round(gb, 2));
            }
        }
        catch { }
        return (false, 0);
    }

    public void SetHibernate(bool enable, OutputHandler? log = null)
    {
        try
        {
            var cmd = enable ? "powercfg -h on" : "powercfg -h off";
            RunHiddenCmd(cmd);
            log?.Invoke(enable ? "✅ Đã BẬT tính năng ngủ đông (Hibernate)." : "✅ Đã TẮT tính năng ngủ đông và giải phóng dung lượng ổ C:!");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi ngủ đông: {ex.Message}");
        }
    }

    public void OpenPowerOptions()
    {
        try
        {
            Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.PowerOptions") { UseShellExecute = true });
        }
        catch { }
    }

    #endregion

    #region 3. Quản Lý User (Local User Management)

    public List<UserModel> GetLocalUsers()
    {
        var users = new List<UserModel>();
        var adminNames = GetAdminUsernames();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, Disabled, PasswordExpires, FullName FROM Win32_UserAccount WHERE LocalAccount=True");
            foreach (ManagementObject mo in searcher.Get())
            {
                var name = mo["Name"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(name)) continue;

                bool disabled = (bool)(mo["Disabled"] ?? false);
                bool pwExpires = (bool)(mo["PasswordExpires"] ?? true);
                bool isAdmin = adminNames.Contains(name, StringComparer.OrdinalIgnoreCase);

                var u = new UserModel
                {
                    Username = name,
                    DisplayText = isAdmin ? $"{name} [ADMIN]" : name,
                    IsAdmin = isAdmin,
                    IsActive = !disabled,
                    IsDisabled = disabled,
                    PasswordNeverExpires = !pwExpires,
                    DeletePassword = false,
                    DeleteUser = false,
                    DeleteProfile = false
                };
                users.Add(u);
            }
        }
        catch
        {
            // Fallback via net user
            try
            {
                var psi = new ProcessStartInfo("net", "user") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    var lines = p.StandardOutput.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    bool reading = false;
                    foreach (var l in lines)
                    {
                        if (l.StartsWith("----")) { reading = true; continue; }
                        if (l.Contains("The command completed")) break;
                        if (!reading) continue;

                        var parts = l.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            bool isAdmin = adminNames.Contains(part, StringComparer.OrdinalIgnoreCase);
                            users.Add(new UserModel
                            {
                                Username = part,
                                DisplayText = isAdmin ? $"{part} [ADMIN]" : part,
                                IsAdmin = isAdmin,
                                IsActive = true,
                                IsDisabled = false,
                                PasswordNeverExpires = true
                            });
                        }
                    }
                }
            }
            catch { }
        }

        return users.OrderByDescending(x => x.IsAdmin).ThenBy(x => x.Username).ToList();
    }

    public HashSet<string> GetAdminUsernames()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var psi = new ProcessStartInfo("net", "localgroup Administrators") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            if (p != null)
            {
                var lines = p.StandardOutput.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool reading = false;
                foreach (var line in lines)
                {
                    if (line.StartsWith("----")) { reading = true; continue; }
                    if (line.Contains("The command completed")) break;
                    if (!reading) continue;

                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed)) set.Add(trimmed);
                }
            }
        }
        catch { }
        return set;
    }

    public void ApplyUserChanges(List<UserModel> users, OutputHandler? log = null)
    {
        foreach (var u in users)
        {
            if (u.DeleteUser)
            {
                log?.Invoke($"Đang xóa tài khoản '{u.Username}'...");
                RunHiddenCmd($"net user \"{u.Username}\" /delete");
                log?.Invoke($"✅ Đã xóa tài khoản {u.Username}.");

                if (u.DeleteProfile)
                {
                    DeleteUserProfile(u.Username, log);
                }
                continue;
            }

            if (u.DeletePassword)
            {
                log?.Invoke($"Đang xóa mật khẩu tài khoản '{u.Username}'...");
                RunHiddenCmd($"net user \"{u.Username}\" \"\"");
                log?.Invoke($"✅ Đã xóa mật khẩu tài khoản {u.Username}.");
            }

            if (u.IsActive && u.IsDisabled)
            {
                // Conflict, default to active
                u.IsDisabled = false;
            }

            string activeFlag = u.IsActive ? "yes" : "no";
            RunHiddenCmd($"net user \"{u.Username}\" /active:{activeFlag}");

            if (u.IsAdmin)
            {
                RunHiddenCmd($"net localgroup Administrators \"{u.Username}\" /add");
            }
            else
            {
                RunHiddenCmd($"net localgroup Administrators \"{u.Username}\" /delete");
            }

            string expFlag = u.PasswordNeverExpires ? "false" : "true";
            RunHiddenCmd($"wmic useraccount where name='{u.Username}' set passwordexpires={expFlag}");

            if (u.DeleteProfile)
            {
                DeleteUserProfile(u.Username, log);
            }
        }

        log?.Invoke("✅ Đã thực thi áp dụng tất cả thay đổi tài khoản người dùng!");
    }

    public void CreateAdmin(string username, string password, OutputHandler? log = null)
    {
        try
        {
            log?.Invoke($"Đang tạo tài khoản Quản trị viên '{username}'...");
            RunHiddenCmd($"net user \"{username}\" \"{password}\" /add");
            RunHiddenCmd($"net localgroup Administrators \"{username}\" /add");
            RunHiddenCmd($"wmic useraccount where name='{username}' set passwordexpires=false");
            log?.Invoke($"✅ Đã tạo Administrator '{username}' thành công!");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi tạo Admin: {ex.Message}");
        }
    }

    public void CreateStandardUser(string username, string password, OutputHandler? log = null)
    {
        try
        {
            log?.Invoke($"Đang tạo tài khoản người dùng '{username}'...");
            RunHiddenCmd($"net user \"{username}\" \"{password}\" /add");
            RunHiddenCmd($"wmic useraccount where name='{username}' set passwordexpires=false");
            log?.Invoke($"✅ Đã tạo tài khoản '{username}' thành công!");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi tạo User: {ex.Message}");
        }
    }

    public void ChangeDisplayName(string username, string displayName, OutputHandler? log = null)
    {
        try
        {
            log?.Invoke($"Đang đổi tên hiển thị cho '{username}' thành '{displayName}'...");
            RunHiddenCmd($"net user \"{username}\" /fullname:\"{displayName}\"");
            log?.Invoke($"✅ Đã đổi tên hiển thị của {username} thành: {displayName}");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi đổi tên: {ex.Message}");
        }
    }

    public void SetPassword(string username, string newPassword, OutputHandler? log = null)
    {
        try
        {
            log?.Invoke($"Đang đổi mật khẩu cho tài khoản '{username}'...");
            RunHiddenCmd($"net user \"{username}\" \"{newPassword}\"");
            log?.Invoke($"✅ Đã cập nhật mật khẩu cho {username} thành công!");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi đặt mật khẩu: {ex.Message}");
        }
    }

    private void DeleteUserProfile(string username, OutputHandler? log = null)
    {
        try
        {
            log?.Invoke($"Đang xóa User Profile của '{username}'...");
            var profilePath = Path.Combine(@"C:\Users", username);
            if (Directory.Exists(profilePath))
            {
                RunHiddenCmd($"rd /s /q \"{profilePath}\"");
            }
            log?.Invoke($"✅ Đã dọn dẹp profile của {username}.");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Không thể xóa hoàn toàn profile thư mục: {ex.Message}");
        }
    }

    #endregion

    #region 4. Mạng & Workgroup (Network & Domain)

    public (string name, bool isDomain) GetWorkgroupInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Domain, Workgroup, PartOfDomain FROM Win32_ComputerSystem");
            foreach (ManagementObject mo in searcher.Get())
            {
                bool isDomain = (bool)(mo["PartOfDomain"] ?? false);
                string name = (isDomain ? mo["Domain"]?.ToString() : mo["Workgroup"]?.ToString()) ?? "WORKGROUP";
                return (name, isDomain);
            }
        }
        catch { }
        return ("WORKGROUP", false);
    }

    public void OpenNetworkSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ncpa.cpl") { UseShellExecute = true });
        }
        catch { }
    }

    public void OpenSystemProperties()
    {
        try
        {
            Process.Start(new ProcessStartInfo("sysdm.cpl") { UseShellExecute = true });
        }
        catch { }
    }

    #endregion

    #region Helpers

    private static void RunHiddenCmd(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(5000);
        }
        catch { }
    }

    private static void RestartExplorer()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("explorer"))
            {
                try { p.Kill(); } catch { }
            }
            Thread.Sleep(500);
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }
        catch { }
    }

    #endregion
}
