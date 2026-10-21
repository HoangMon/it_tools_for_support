using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Ernastie.Models;

namespace Ernastie.Services;

public class MaintenanceService
{
    public delegate void OutputHandler(string message);

    public long CleanTempFiles(OutputHandler? log = null)
    {
        long bytesDeleted = 0;
        var targets = new List<string>
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download")
        };

        foreach (var path in targets)
        {
            if (!Directory.Exists(path)) continue;

            log?.Invoke($"Đang dọn dẹp thư mục: {path}...");
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.GetFiles())
                {
                    try
                    {
                        var len = file.Length;
                        file.Delete();
                        bytesDeleted += len;
                    }
                    catch { }
                }

                foreach (var subDir in dir.GetDirectories())
                {
                    try
                    {
                        subDir.Delete(true);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"Lỗi truy cập {path}: {ex.Message}");
            }
        }

        var mb = bytesDeleted / (1024 * 1024);
        log?.Invoke($"Hoàn tất dọn rác! Đã giải phóng: {mb} MB.");
        return bytesDeleted;
    }

    public async Task RemoveBloatwareAsync(OutputHandler? log = null)
    {
        log?.Invoke("Đang tiến hành gỡ bỏ Bloatware UWP mặc định của Windows...");
        const string psCmd = "Get-AppxPackage -AllUsers *3dbuilder* | Remove-AppxPackage; " +
                             "Get-AppxPackage -AllUsers *bing* | Remove-AppxPackage; " +
                             "Get-AppxPackage -AllUsers *zune* | Remove-AppxPackage; " +
                             "Get-AppxPackage -AllUsers *solitaire* | Remove-AppxPackage; " +
                             "Get-AppxPackage -AllUsers *xbox* | Remove-AppxPackage; " +
                             "Get-AppxPackage -AllUsers *feedback* | Remove-AppxPackage;";

        await Task.Run(() =>
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCmd}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                proc.Start();
                proc.WaitForExit();
                log?.Invoke("Đã hoàn tất dọn dẹp các ứng dụng Bloatware UWP!");
            }
            catch (Exception ex)
            {
                log?.Invoke($"Lỗi gỡ bloatware: {ex.Message}");
            }
        });
    }

    public void RestartExplorer(OutputHandler? log = null)
    {
        log?.Invoke("Đang khởi động lại Windows Explorer...");
        try
        {
            foreach (var proc in Process.GetProcessesByName("explorer"))
            {
                proc.Kill();
            }
            Thread.Sleep(500);
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
            log?.Invoke("Windows Explorer đã được khởi động lại thành công.");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Không thể khởi động lại Explorer: {ex.Message}");
        }
    }

    public async Task RunSystemRepairAsync(string tool, OutputHandler? log = null)
    {
        string exe = "cmd.exe";
        string args = tool switch
        {
            "sfc" => "/c sfc /scannow",
            "dism" => "/c DISM.exe /Online /Cleanup-Image /RestoreHealth",
            "network" => "/c netsh winsock reset && ipconfig /flushdns",
            "network_diag" => "/c ping 8.8.8.8 -n 4 && ipconfig /all",
            "spooler" => "/c net stop spooler && del /Q /F /S \"%systemroot%\\System32\\Spool\\Printers\\*.*\" && net start spooler",
            "bcdboot" => "/c bcdboot C:\\Windows /l vi-VN || bcdboot C:\\Windows",
            _ => "/c echo Sẵn sàng"
        };

        log?.Invoke($"Bắt đầu tiến trình: {args}...");

        await Task.Run(() =>
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) log?.Invoke(e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) log?.Invoke("[Lỗi] " + e.Data); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                log?.Invoke($"Tiến trình hoàn tất với mã thoát: {proc.ExitCode}");
            }
            catch (Exception ex)
            {
                log?.Invoke($"Lỗi thực thi: {ex.Message}");
            }
        });
    }

    public void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    public List<string> GetLocalUserProfiles()
    {
        var users = new List<string>();
        try
        {
            if (Directory.Exists(@"C:\Users"))
            {
                foreach (var dir in Directory.GetDirectories(@"C:\Users"))
                {
                    var name = Path.GetFileName(dir);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (name.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("Default User", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("All Users", StringComparison.OrdinalIgnoreCase))
                        continue;

                    users.Add(name);
                }
            }
        }
        catch { }

        if (!users.Contains(Environment.UserName, StringComparer.OrdinalIgnoreCase))
        {
            users.Insert(0, Environment.UserName);
        }
        return users;
    }

    public async Task<CleanResultModel> ExecuteDeepCleanAsync(
        CleanerOptionsModel options,
        Action<string, int>? onProgress = null,
        OutputHandler? log = null)
    {
        return await Task.Run(() =>
        {
            var result = new CleanResultModel();
            long totalBytes = 0;
            int fileCount = 0;

            string targetUser = string.IsNullOrWhiteSpace(options.TargetUser) ? Environment.UserName : options.TargetUser;
            string userProfile = Path.Combine(@"C:\Users", targetUser);
            if (!Directory.Exists(userProfile))
            {
                userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            log?.Invoke($"Bắt đầu dọn dẹp hệ thống cho user [{targetUser}]...");
            onProgress?.Invoke("Khởi tạo tiến trình dọn rác...", 2);

            // 1. Temp người dùng
            if (options.UserTemp)
            {
                string p = Path.Combine(userProfile, @"AppData\Local\Temp");
                onProgress?.Invoke($"Đang dọn Temp người dùng ({p})...", 5);
                log?.Invoke($"Đang dọn Temp người dùng: {p}");
                SafeDeleteDirectoryContents(p, ref totalBytes, ref fileCount);
            }

            // 2. Windows Temp
            if (options.WindowsTemp)
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                onProgress?.Invoke("Đang dọn: Windows Temp...", 10);
                log?.Invoke($"Đang dọn Windows Temp: {p}");
                SafeDeleteDirectoryContents(p, ref totalBytes, ref fileCount);
            }

            // 3. Windows Update cache
            if (options.WindowsUpdateCache)
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"SoftwareDistribution\Download");
                onProgress?.Invoke("Đang dọn: Windows Update cache...", 15);
                log?.Invoke($"Đang dọn Windows Update cache: {p}");
                SafeDeleteDirectoryContents(p, ref totalBytes, ref fileCount);
            }

            // 4. Prefetch
            if (options.Prefetch)
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                onProgress?.Invoke("Đang dọn: Windows Prefetch...", 20);
                log?.Invoke($"Đang dọn Windows Prefetch: {p}");
                SafeDeleteDirectoryContents(p, ref totalBytes, ref fileCount);
            }

            // 5. Explorer cache
            if (options.ExplorerCache)
            {
                onProgress?.Invoke("Đang dọn: Explorer cache (thumb/icon cache)...", 25);
                log?.Invoke("Đang dọn Explorer cache...");
                string explorerDir = Path.Combine(userProfile, @"AppData\Local\Microsoft\Windows\Explorer");
                if (Directory.Exists(explorerDir))
                {
                    SafeDeletePattern(explorerDir, "thumbcache_*.db", ref totalBytes, ref fileCount);
                    SafeDeletePattern(explorerDir, "iconcache_*.db", ref totalBytes, ref fileCount);
                }
                SafeDeleteFile(Path.Combine(userProfile, @"AppData\Local\IconCache.db"), ref totalBytes, ref fileCount);
            }

            // 6. INetCache
            if (options.INetCache)
            {
                string p = Path.Combine(userProfile, @"AppData\Local\Microsoft\Windows\INetCache");
                onProgress?.Invoke("Đang dọn: INetCache (Internet cache cũ)...", 30);
                log?.Invoke($"Đang dọn INetCache: {p}");
                SafeDeleteDirectoryContents(p, ref totalBytes, ref fileCount);
            }

            // 7. WebCache
            if (options.WebCache)
            {
                string p = Path.Combine(userProfile, @"AppData\Local\Microsoft\Windows\WebCache");
                onProgress?.Invoke("Đang dọn: WebCache...", 35);
                log?.Invoke($"Đang dọn WebCache: {p}");
                SafeDeleteDirectoryContents(p, ref totalBytes, ref fileCount);
            }

            // 8. CrashDumps
            if (options.CrashDumps)
            {
                onProgress?.Invoke("Đang dọn: CrashDumps & Minidump...", 40);
                log?.Invoke("Đang dọn CrashDumps...");
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Local\CrashDumps"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump"), ref totalBytes, ref fileCount);
                SafeDeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP"), ref totalBytes, ref fileCount);
            }

            // 9. Windows Error Reporting (WER)
            if (options.Wer)
            {
                onProgress?.Invoke("Đang dọn: Windows Error Reporting (WER)...", 45);
                log?.Invoke("Đang dọn WER...");
                SafeDeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Local\Microsoft\Windows\WER"), ref totalBytes, ref fileCount);
            }

            // 10. DirectX Shader Cache
            if (options.DirectXCache)
            {
                onProgress?.Invoke("Đang dọn: DirectX Shader Cache (D3DSCache)...", 50);
                log?.Invoke("Đang dọn DirectX Shader Cache...");
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Local\D3DSCache"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Local\NVIDIA\DXCache"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Local\AMD\DxCache"), ref totalBytes, ref fileCount);
            }

            // 11. Recycle Bin
            if (options.RecycleBin)
            {
                onProgress?.Invoke("Đang dọn: Thùng rác (Recycle Bin)...", 55);
                log?.Invoke("Đang dọn sạch Thùng rác...");
                SafeEmptyRecycleBin();
            }

            // 12. Quick Access / Recent files
            if (options.QuickAccess)
            {
                onProgress?.Invoke("Đang dọn: Lịch sử Quick Access / Recent files...", 60);
                log?.Invoke("Đang dọn Quick Access & Recent files...");
                string recent = Path.Combine(userProfile, @"AppData\Roaming\Microsoft\Windows\Recent");
                SafeDeleteDirectoryContents(recent, ref totalBytes, ref fileCount);
            }

            // 13. Browser Caches
            if (options.EdgeCache)
            {
                onProgress?.Invoke("Đang dọn: Microsoft Edge cache...", 64);
                log?.Invoke("Đang dọn Edge cache...");
                CleanChromiumCache(Path.Combine(userProfile, @"AppData\Local\Microsoft\Edge\User Data"), ref totalBytes, ref fileCount);
            }

            if (options.ChromeCache)
            {
                onProgress?.Invoke("Đang dọn: Google Chrome cache...", 68);
                log?.Invoke("Đang dọn Chrome cache...");
                CleanChromiumCache(Path.Combine(userProfile, @"AppData\Local\Google\Chrome\User Data"), ref totalBytes, ref fileCount);
            }

            if (options.FirefoxCache)
            {
                onProgress?.Invoke("Đang dọn: Mozilla Firefox cache...", 72);
                log?.Invoke("Đang dọn Firefox cache...");
                CleanFirefoxCache(Path.Combine(userProfile, @"AppData\Local\Mozilla\Firefox\Profiles"), ref totalBytes, ref fileCount);
            }

            // 14. Browser History
            if (options.EdgeHistory)
            {
                onProgress?.Invoke("Đang dọn: Lịch sử duyệt web Edge...", 75);
                log?.Invoke("Đang dọn lịch sử Edge...");
                CleanChromiumHistory(Path.Combine(userProfile, @"AppData\Local\Microsoft\Edge\User Data"), ref totalBytes, ref fileCount);
            }

            if (options.ChromeHistory)
            {
                onProgress?.Invoke("Đang dọn: Lịch sử duyệt web Chrome...", 78);
                log?.Invoke("Đang dọn lịch sử Chrome...");
                CleanChromiumHistory(Path.Combine(userProfile, @"AppData\Local\Google\Chrome\User Data"), ref totalBytes, ref fileCount);
            }

            if (options.FirefoxHistory)
            {
                onProgress?.Invoke("Đang dọn: Lịch sử duyệt web Firefox...", 81);
                log?.Invoke("Đang dọn lịch sử Firefox...");
                CleanFirefoxHistory(Path.Combine(userProfile, @"AppData\Roaming\Mozilla\Firefox\Profiles"), ref totalBytes, ref fileCount);
            }

            // 15. Logout Accounts (Cookies & Passwords)
            if (options.ChromeLogout)
            {
                onProgress?.Invoke("Đang đăng xuất tài khoản Chrome (đóng tiến trình Chrome)...", 84);
                log?.Invoke("Đang đăng xuất tài khoản trong Chrome...");
                KillProcessByName("chrome");
                CleanChromiumLogins(Path.Combine(userProfile, @"AppData\Local\Google\Chrome\User Data"), ref totalBytes, ref fileCount);
            }

            if (options.EdgeLogout)
            {
                onProgress?.Invoke("Đang đăng xuất tài khoản Edge (đóng tiến trình Edge)...", 87);
                log?.Invoke("Đang đăng xuất tài khoản trong Edge...");
                KillProcessByName("msedge");
                CleanChromiumLogins(Path.Combine(userProfile, @"AppData\Local\Microsoft\Edge\User Data"), ref totalBytes, ref fileCount);
            }

            if (options.FirefoxLogout)
            {
                onProgress?.Invoke("Đang đăng xuất tài khoản Firefox (đóng tiến trình Firefox)...", 90);
                log?.Invoke("Đang đăng xuất tài khoản trong Firefox...");
                KillProcessByName("firefox");
                CleanFirefoxLogins(Path.Combine(userProfile, @"AppData\Roaming\Mozilla\Firefox\Profiles"), ref totalBytes, ref fileCount);
            }

            // 16. Reset Zalo PC
            if (options.ResetZalo)
            {
                onProgress?.Invoke("Đang Reset Zalo PC (đóng Zalo & xóa ZaloData)...", 93);
                log?.Invoke("Đang Reset Zalo PC...");
                KillProcessByName("Zalo");
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Roaming\ZaloData"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Roaming\Zalo"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Local\ZaloPC"), ref totalBytes, ref fileCount);
            }

            // 17. Reset Teams
            if (options.ResetTeams)
            {
                onProgress?.Invoke("Đang Reset Microsoft Teams...", 95);
                log?.Invoke("Đang Reset Microsoft Teams...");
                KillProcessByName("Teams");
                KillProcessByName("ms-teams");
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Roaming\Microsoft\Teams"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Local\Packages\MSTeams_8wekyb3d8bbwe\LocalCache"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(userProfile, @"AppData\Local\Microsoft\Teams"), ref totalBytes, ref fileCount);
            }

            // 18. Clear Windows Credential Manager
            if (options.ClearCredentials)
            {
                onProgress?.Invoke("Đang xóa Windows Credential Manager...", 97);
                log?.Invoke("Đang xóa Windows Credential Manager...");
                int credsDeleted = 0;
                ClearWindowsCredentials(ref credsDeleted);
                fileCount += credsDeleted;
            }

            // 19. Custom folders
            if (options.CustomFolders != null && options.CustomFolders.Count > 0)
            {
                onProgress?.Invoke("Đang dọn dẹp các thư mục tùy chọn...", 98);
                foreach (var folder in options.CustomFolders)
                {
                    if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) continue;
                    log?.Invoke($"Đang dọn thư mục tùy chọn: {folder}");
                    SafeDeleteDirectoryContents(folder, ref totalBytes, ref fileCount);
                }
            }

            onProgress?.Invoke("Hoàn tất dọn dẹp hệ thống!", 100);
            result.BytesFreed = totalBytes;
            result.FilesDeleted = fileCount;
            log?.Invoke($"Đã hoàn tất dọn dẹp! Giải phóng {result.MegabytesFreed} MB ({result.FilesDeleted} tệp tin).");

            return result;
        });
    }

    private static void SafeDeleteDirectoryContents(string dirPath, ref long totalBytes, ref int fileCount, bool deleteSubDirs = true)
    {
        if (!Directory.Exists(dirPath)) return;

        try
        {
            var di = new DirectoryInfo(dirPath);
            foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    long len = fi.Length;
                    fi.Attributes = FileAttributes.Normal;
                    fi.Delete();
                    Interlocked.Add(ref totalBytes, len);
                    Interlocked.Increment(ref fileCount);
                }
                catch { }
            }

            if (deleteSubDirs)
            {
                foreach (var sub in di.EnumerateDirectories())
                {
                    try { sub.Delete(true); } catch { }
                }
            }
        }
        catch { }
    }

    private static void SafeDeleteFile(string filePath, ref long totalBytes, ref int fileCount)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var fi = new FileInfo(filePath);
                long len = fi.Length;
                fi.Attributes = FileAttributes.Normal;
                fi.Delete();
                Interlocked.Add(ref totalBytes, len);
                Interlocked.Increment(ref fileCount);
            }
        }
        catch { }
    }

    private static void SafeDeletePattern(string dirPath, string pattern, ref long totalBytes, ref int fileCount)
    {
        if (!Directory.Exists(dirPath)) return;
        try
        {
            foreach (var file in Directory.EnumerateFiles(dirPath, pattern))
            {
                SafeDeleteFile(file, ref totalBytes, ref fileCount);
            }
        }
        catch { }
    }

    private static void KillProcessByName(string processName)
    {
        try
        {
            foreach (var p in Process.GetProcessesByName(processName))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(1000);
                }
                catch { }
            }
        }
        catch { }
    }

    private static void SafeEmptyRecycleBin()
    {
        try
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, 7);
        }
        catch
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(2000);
            }
            catch { }
        }
    }

    private static void ClearWindowsCredentials(ref int count)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmdkey.exe",
                Arguments = "/list",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3000);

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                {
                    var target = trimmed.Substring(7).Trim();
                    if (!string.IsNullOrEmpty(target))
                    {
                        try
                        {
                            var delPsi = new ProcessStartInfo
                            {
                                FileName = "cmdkey.exe",
                                Arguments = $"/delete:\"{target}\"",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            using var dp = Process.Start(delPsi);
                            dp?.WaitForExit(1500);
                            Interlocked.Increment(ref count);
                        }
                        catch { }
                    }
                }
            }
        }
        catch { }
    }

    private static void CleanChromiumCache(string userDataDir, ref long totalBytes, ref int fileCount)
    {
        if (!Directory.Exists(userDataDir)) return;
        try
        {
            var profileDirs = new List<string> { Path.Combine(userDataDir, "Default") };
            foreach (var sub in Directory.GetDirectories(userDataDir, "Profile *"))
            {
                profileDirs.Add(sub);
            }

            foreach (var prof in profileDirs)
            {
                if (!Directory.Exists(prof)) continue;
                SafeDeleteDirectoryContents(Path.Combine(prof, "Cache"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(prof, "Code Cache"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(prof, "GPUCache"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(prof, @"Service Worker\CacheStorage"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(prof, "GrShaderCache"), ref totalBytes, ref fileCount);
            }
        }
        catch { }
    }

    private static void CleanChromiumHistory(string userDataDir, ref long totalBytes, ref int fileCount)
    {
        if (!Directory.Exists(userDataDir)) return;
        try
        {
            var profileDirs = new List<string> { Path.Combine(userDataDir, "Default") };
            foreach (var sub in Directory.GetDirectories(userDataDir, "Profile *"))
            {
                profileDirs.Add(sub);
            }

            string[] historyFiles = { "History", "History-journal", "Visited Links", "Top Sites", "Top Sites-journal", "Shortcuts", "Favicons", "Favicons-journal" };
            foreach (var prof in profileDirs)
            {
                if (!Directory.Exists(prof)) continue;
                foreach (var hf in historyFiles)
                {
                    SafeDeleteFile(Path.Combine(prof, hf), ref totalBytes, ref fileCount);
                }
            }
        }
        catch { }
    }

    private static void CleanChromiumLogins(string userDataDir, ref long totalBytes, ref int fileCount)
    {
        if (!Directory.Exists(userDataDir)) return;
        try
        {
            var profileDirs = new List<string> { Path.Combine(userDataDir, "Default") };
            foreach (var sub in Directory.GetDirectories(userDataDir, "Profile *"))
            {
                profileDirs.Add(sub);
            }

            string[] loginFiles = {
                "Cookies", "Cookies-journal", "Login Data", "Login Data-journal",
                "Login Data For Account", "Web Data", "Web Data-journal",
                "Current Session", "Current Tabs", "Last Session", "Last Tabs"
            };

            foreach (var prof in profileDirs)
            {
                if (!Directory.Exists(prof)) continue;
                foreach (var lf in loginFiles)
                {
                    SafeDeleteFile(Path.Combine(prof, lf), ref totalBytes, ref fileCount);
                }
                SafeDeleteFile(Path.Combine(prof, @"Network\Cookies"), ref totalBytes, ref fileCount);
                SafeDeleteFile(Path.Combine(prof, @"Network\Cookies-journal"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(prof, "Sessions"), ref totalBytes, ref fileCount);
            }
        }
        catch { }
    }

    private static void CleanFirefoxCache(string profilesDir, ref long totalBytes, ref int fileCount)
    {
        if (!Directory.Exists(profilesDir)) return;
        try
        {
            foreach (var prof in Directory.GetDirectories(profilesDir))
            {
                SafeDeleteDirectoryContents(Path.Combine(prof, "cache2"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(prof, "jumpListCache"), ref totalBytes, ref fileCount);
                SafeDeleteDirectoryContents(Path.Combine(prof, "startupCache"), ref totalBytes, ref fileCount);
            }
        }
        catch { }
    }

    private static void CleanFirefoxHistory(string profilesDir, ref long totalBytes, ref int fileCount)
    {
        if (!Directory.Exists(profilesDir)) return;
        try
        {
            string[] histFiles = { "places.sqlite", "places.sqlite-wal", "favicons.sqlite", "favicons.sqlite-wal" };
            foreach (var prof in Directory.GetDirectories(profilesDir))
            {
                foreach (var hf in histFiles)
                {
                    SafeDeleteFile(Path.Combine(prof, hf), ref totalBytes, ref fileCount);
                }
            }
        }
        catch { }
    }

    private static void CleanFirefoxLogins(string profilesDir, ref long totalBytes, ref int fileCount)
    {
        if (!Directory.Exists(profilesDir)) return;
        try
        {
            string[] loginFiles = {
                "cookies.sqlite", "cookies.sqlite-wal", "logins.json",
                "key4.db", "cert9.db", "sessionstore.jsonlz4"
            };
            foreach (var prof in Directory.GetDirectories(profilesDir))
            {
                foreach (var lf in loginFiles)
                {
                    SafeDeleteFile(Path.Combine(prof, lf), ref totalBytes, ref fileCount);
                }
                SafeDeleteDirectoryContents(Path.Combine(prof, "sessionstore-backups"), ref totalBytes, ref fileCount);
            }
        }
        catch { }
    }
}

