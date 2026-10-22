using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace Ernastie.Services;

public record DirectAppInfo(string Url, string SilentArgs, string FileName, bool IsZip = false, string? ExeInsideZip = null);

public class SoftwareInstallService
{
    private static SoftwareInstallService? _instance;
    public static SoftwareInstallService Instance => _instance ??= new SoftwareInstallService();

    // Mapping Winget Package ID -> Direct Official Installer URL + Silent Switch
    public static readonly Dictionary<string, DirectAppInfo> FallbackApps = new(StringComparer.OrdinalIgnoreCase)
    {
        // 1. Trình duyệt & Internet
        ["Google.Chrome"] = new("https://dl.google.com/chrome/install/latest/chrome_installer.exe", "/silent /install", "chrome_installer.exe"),
        ["Brave.Brave"] = new("https://laptop-updates.brave.com/latest/winx64", "--silent --install", "brave_installer.exe"),
        ["Mozilla.Firefox"] = new("https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=vi", "-ms", "firefox_installer.exe"),
        ["CocCoc.CocCoc"] = new("https://files.coccoc.com/coccoc/coccoc_standalone_vi.exe", "/silent", "coccoc_installer.exe"),

        // 2. Tiện ích gõ tiếng Việt & Nén
        ["UniKey.UniKey"] = new("https://www.unikey.org/assets/release/unikey43RC5-200929-win64.zip", "", "unikey.zip", IsZip: true, ExeInsideZip: "UniKeyNT.exe"),
        ["EVKey.EVKey"] = new("https://github.com/lamquangminh/EVKey/releases/download/v5.4.7/EVKey.zip", "", "evkey.zip", IsZip: true, ExeInsideZip: "EVKey64.exe"),
        ["7zip.7zip"] = new("https://www.7-zip.org/a/7z2408-x64.exe", "/S", "7z_installer.exe"),
        ["RARLab.WinRAR"] = new("https://www.rarlab.com/rar/winrar-x64-701.exe", "/s", "winrar_installer.exe"),

        // 3. Hỗ trợ từ xa & Liên lạc
        ["UltraViewer.UltraViewer"] = new("https://www.ultraviewer.net/en/UltraViewer_setup_6.6_en.exe", "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART", "ultraviewer_setup.exe"),
        ["AnyDeskSoftwareGmbH.AnyDesk"] = new("https://download.anydesk.com/AnyDesk.exe", "--install \"C:\\Program Files (x86)\\AnyDesk\" --silent", "anydesk_setup.exe"),
        ["TeamViewer.TeamViewer"] = new("https://download.teamviewer.com/download/TeamViewer_Setup_x64.exe", "/S", "teamviewer_setup.exe"),
        ["VNG.Zalo"] = new("https://res-zalo.zadn.vn/download/ZaloSetup.exe", "/S", "zalo_setup.exe"),
        ["Telegram.TelegramDesktop"] = new("https://telegram.org/dl/desktop/win64", "/VERYSILENT /NORESTART", "telegram_setup.exe"),
        ["Zoom.Zoom"] = new("https://zoom.us/client/latest/ZoomInstallerFull.exe", "/silent", "zoom_setup.exe"),
        ["Discord.Discord"] = new("https://discord.com/api/download?platform=win", "--silent", "discord_setup.exe"),

        // 4. Văn phòng & PDF
        ["Foxit.FoxitReader"] = new("https://cdn01.foxitsoftware.com/product/reader/desktop/win/FoxitPDFReader_Setup_Prom_x64.exe", "/quiet", "foxit_setup.exe"),
        ["SumatraPDF.SumatraPDF"] = new("https://www.sumatrapdfreader.org/dl/rel/3.5.2/SumatraPDF-3.5.2-64-install.exe", "-s", "sumatra_setup.exe"),
        ["Kingsoft.WPSOffice"] = new("https://wdl1.pcfg.cache.wpscdn.com/wpsdl/wpsoffice/download/setup/WPSOffice_setup.exe", "/s", "wps_setup.exe"),
        ["TheDocumentFoundation.LibreOffice"] = new("https://download.documentfoundation.org/libreoffice/stable/24.8.2/win/x86_64/LibreOffice_24.8.2_Win_x86-64.msi", "/qn /norestart", "libreoffice_setup.msi"),

        // 5. Media & Lập trình
        ["VideoLAN.VLC"] = new("https://get.videolan.org/vlc/last/win64/vlc-3.0.21-win64.exe", "/L=1033 /S", "vlc_setup.exe"),
        ["CodecGuide.K-LiteCodecPack.Full"] = new("https://files3.codecguide.com/K-Lite_Codec_Pack_1855_Full.exe", "/verysilent /norestart", "klite_setup.exe"),
        ["Notepad++.Notepad++"] = new("https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7/npp.8.7.Installer.x64.exe", "/S", "npp_setup.exe"),
        ["Microsoft.VisualStudioCode"] = new("https://update.code.visualstudio.com/latest/win32-x64-user/stable", "/verysilent /mergetasks=!runcode", "vscode_setup.exe"),
        ["Git.Git"] = new("https://github.com/git-for-windows/git/releases/download/v2.46.0.windows.1/Git-2.46.0-64-bit.exe", "/VERYSILENT /NORESTART", "git_setup.exe"),
        ["OpenJS.NodeJS.LTS"] = new("https://nodejs.org/dist/v20.18.0/node-v20.18.0-x64.msi", "/qn /norestart", "nodejs_setup.msi"),
        ["Python.Python.3.12"] = new("https://www.python.org/ftp/python/3.12.7/python-3.12.7-amd64.exe", "/quiet InstallAllUsers=1 PrependPath=1", "python_setup.exe"),
        ["GIMP.GIMP"] = new("https://download.gimp.org/gimp/v2.10/windows/gimp-2.10.38-setup.exe", "/VERYSILENT /NORESTART", "gimp_setup.exe"),
        ["OBSProject.OBSStudio"] = new("https://github.com/obsproject/obs-studio/releases/download/30.2.3/OBS-Studio-30.2.3-Windows-Installer.exe", "/S", "obs_setup.exe")
    };

    public string? FindWingetPath()
    {
        // 1. Check in PATH
        try
        {
            var psi = new ProcessStartInfo("where", "winget")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                var line = p.StandardOutput.ReadLine();
                p.WaitForExit(1500);
                if (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(line) && File.Exists(line.Trim()))
                {
                    return line.Trim();
                }
            }
        }
        catch { }

        // 2. Check LocalAppData WindowsApps
        var localApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "winget.exe");
        if (File.Exists(localApp)) return localApp;

        // 3. Check Program Files WindowsApps
        try
        {
            var winApps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            if (Directory.Exists(winApps))
            {
                var matches = Directory.GetFiles(winApps, "winget.exe", SearchOption.AllDirectories);
                if (matches.Length > 0 && File.Exists(matches[0])) return matches[0];
            }
        }
        catch { }

        return null;
    }

    public bool IsWingetAvailable()
    {
        var path = FindWingetPath();
        if (string.IsNullOrEmpty(path)) return false;
        try
        {
            var psi = new ProcessStartInfo(path, "--version")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                p.WaitForExit(2000);
                return p.ExitCode == 0;
            }
        }
        catch { }
        return false;
    }

    public async Task<bool> InstallAppDirectFallbackAsync(string appName, string wingetId, Action<string> log, Action<int> progress, CancellationToken ct)
    {
        if (!FallbackApps.TryGetValue(wingetId, out var info))
        {
            log($"⚠️ [{appName}]: Chưa có gói Direct Fallback tự động. Cần có Winget để tải phần mềm này.");
            return false;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "Ernastie_Installers");
        Directory.CreateDirectory(tempDir);
        var destFile = Path.Combine(tempDir, info.FileName);

        try
        {
            log($"🌐 Đang tải tệp cài đặt chính thức: {appName}...");
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            using var response = await client.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[16384];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                totalRead += bytesRead;
                if (totalBytes > 0)
                {
                    int pct = (int)((totalRead * 100) / totalBytes);
                    progress(pct);
                }
            }

            fileStream.Close();
            log($"⬇️ Đã tải xong tệp cài đặt: {info.FileName} ({(totalRead / (1024.0 * 1024)):F1} MB).");

            if (info.IsZip)
            {
                log($"📦 Đang giải nén tệp {appName}...");
                var extractTarget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), appName);
                if (!Directory.Exists(extractTarget)) Directory.CreateDirectory(extractTarget);

                ZipFile.ExtractToDirectory(destFile, extractTarget, overwriteFiles: true);

                if (!string.IsNullOrEmpty(info.ExeInsideZip))
                {
                    var exePath = Path.Combine(extractTarget, info.ExeInsideZip);
                    CreateDesktopShortcut(appName, exePath);
                    log($"✅ Đã tạo lối tắt Desktop cho {appName}.");
                }

                return true;
            }

            log($"⚙️ Đang thực thi cài đặt âm thầm (Silent Install): {appName}...");
            var psi = new ProcessStartInfo(destFile, info.SilentArgs)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            using var p = Process.Start(psi);
            if (p != null)
            {
                await p.WaitForExitAsync(ct);
                if (p.ExitCode == 0 || p.ExitCode == 3010)
                {
                    log($"✅ Cài đặt thành công qua Direct Fallback: {appName}");
                    return true;
                }
                else
                {
                    log($"⚠️ Cài đặt {appName} hoàn tất với mã: {p.ExitCode}");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            log($"❌ Lỗi cài đặt trực tiếp {appName}: {ex.Message}");
        }
        finally
        {
            try { if (File.Exists(destFile) && !info.IsZip) File.Delete(destFile); } catch { }
        }

        return false;
    }

    public async Task<bool> InstallWingetEnvironmentAsync(Action<string> log, Action<int> progress)
    {
        log("🚀 Bắt đầu cài đặt môi trường Winget CLI (dành cho Windows Lite / Windows 10 LTSC)...");
        var temp = Path.Combine(Path.GetTempPath(), "WingetSetup");
        Directory.CreateDirectory(temp);

        var vclibsUrl = "https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx";
        var xamlUrl = "https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx";
        var wingetUrl = "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle";

        var vclibsPath = Path.Combine(temp, "vclibs.appx");
        var xamlPath = Path.Combine(temp, "xaml.appx");
        var wingetPath = Path.Combine(temp, "winget.msixbundle");

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        try
        {
            log("[1/4] Đang tải Microsoft VCLibs Framework...");
            var vcData = await client.GetByteArrayAsync(vclibsUrl);
            await File.WriteAllBytesAsync(vclibsPath, vcData);
            progress(25);

            log("[2/4] Đang tải Microsoft UI XAML 2.8...");
            var xamlData = await client.GetByteArrayAsync(xamlUrl);
            await File.WriteAllBytesAsync(xamlPath, xamlData);
            progress(50);

            log("[3/4] Đang tải gói Winget DesktopAppInstaller Bundle...");
            var wingetData = await client.GetByteArrayAsync(wingetUrl);
            await File.WriteAllBytesAsync(wingetPath, wingetData);
            progress(75);

            log("[4/4] Đang cài đặt các gói Package AppX vào hệ thống Windows...");
            var psScript = $@"
Add-AppxPackage -Path '{vclibsPath}' -ErrorAction SilentlyContinue
Add-AppxPackage -Path '{xamlPath}' -ErrorAction SilentlyContinue
Add-AppxPackage -Path '{wingetPath}' -DependencyPath '{vclibsPath}','{xamlPath}' -ErrorAction Stop
";
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript.Replace("\"", "\\\"")}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var p = Process.Start(psi);
            if (p != null)
            {
                await p.WaitForExitAsync();
            }

            progress(100);

            if (IsWingetAvailable())
            {
                log("🎉 [THÀNH CÔNG] Đã kích hoạt môi trường Winget CLI thành công trên máy tính!");
                return true;
            }
            else
            {
                log("⚠️ Windows Lite không hỗ trợ AppXSvc. Ứng dụng sẽ tự động chuyển sang chế độ cài trực tiếp (Direct Fallback).");
                return false;
            }
        }
        catch (Exception ex)
        {
            log($"❌ Không thể cài Winget Package: {ex.Message}. Sẽ dùng cơ chế Direct Fallback.");
            return false;
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    private static void CreateDesktopShortcut(string shortcutName, string targetPath)
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktop, $"{shortcutName}.lnk");
            var vbsScript = $@"
Set oWS = WScript.CreateObject(""WScript.Shell"")
sLinkFile = ""{shortcutPath}""
Set oLink = oWS.CreateShortcut(sLinkFile)
oLink.TargetPath = ""{targetPath}""
oLink.WorkingDirectory = ""{Path.GetDirectoryName(targetPath)}""
oLink.Save
";
            var tempVbs = Path.Combine(Path.GetTempPath(), "shortcut.vbs");
            File.WriteAllText(tempVbs, vbsScript);
            var psi = new ProcessStartInfo("cscript.exe", $"//Nologo \"{tempVbs}\"") { CreateNoWindow = true, UseShellExecute = false };
            using var p = Process.Start(psi);
            p?.WaitForExit(2000);
            try { File.Delete(tempVbs); } catch { }
        }
        catch { }
    }
}
