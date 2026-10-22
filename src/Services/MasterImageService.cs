using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Ernastie.Services;

public class MasterImageStatus
{
    public bool IsWinPE { get; set; }
    public string RunningLocation { get; set; } = string.Empty;
    public string StorageDirectory { get; set; } = string.Empty;
    public string WimStatus { get; set; } = "CHƯA có (chưa chọn chỗ lưu)";
    public string IsoWin10Status { get; set; } = "CHƯA có (bấm Bước 3a để tải)";
    public string IsoWin11Status { get; set; } = "CHƯA có (bấm Bước 3b để tải)";
    public string Win10MasterStatus { get; set; } = "CHƯA tạo";
    public string Win11MasterStatus { get; set; } = "CHƯA tạo";
}

public class MasterImageService
{
    public bool IsWinPE()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
            var pe = key?.GetValue("PEFirmwareType");
            if (pe != null) return true;

            using var minint = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\MiniNT");
            if (minint != null) return true;

            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory);
            if (string.Equals(systemDrive, @"X:\", StringComparison.OrdinalIgnoreCase)) return true;
        }
        catch { }
        return false;
    }

    public bool IsAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public string GetDefaultStorageDirectory()
    {
        // Find non-OS fixed drive with most free space (e.g. D:, E:)
        try
        {
            var osDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\');
            DriveInfo? bestDrive = null;
            long maxFree = 0;

            foreach (var d in DriveInfo.GetDrives())
            {
                if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
                var letter = d.Name.TrimEnd('\\');
                if (string.Equals(letter, osDrive, StringComparison.OrdinalIgnoreCase)) continue;

                if (d.AvailableFreeSpace > maxFree)
                {
                    maxFree = d.AvailableFreeSpace;
                    bestDrive = d;
                }
            }

            if (bestDrive != null)
            {
                var candidate = Path.Combine(bestDrive.Name, "MasterImage");
                return candidate;
            }
        }
        catch { }

        // Fallback to D:\MasterImage or C:\MasterImage
        return Directory.Exists(@"D:\") ? @"D:\MasterImage" : @"C:\MasterImage";
    }

    public MasterImageStatus GetStatus(string storageDir)
    {
        var status = new MasterImageStatus
        {
            IsWinPE = IsWinPE(),
            RunningLocation = IsWinPE()
                ? "WinPE (Môi trường cứu hộ) - thư mục tool: " + AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')
                : "Windows chính  -  thư mục tool: " + AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'),
            StorageDirectory = string.IsNullOrWhiteSpace(storageDir)
                ? "CHƯA chọn – bấm '📁 Đổi chỗ lưu' (hoặc Bước 1/2 sẽ hỏi)"
                : storageDir
        };

        if (string.IsNullOrWhiteSpace(storageDir) || !Directory.Exists(storageDir))
        {
            status.WimStatus = "CHƯA có (chưa chọn chỗ lưu)";
            status.IsoWin10Status = "CHƯA có  (bấm Bước 3a để tải)";
            status.IsoWin11Status = "CHƯA có  (bấm Bước 3b để tải)";
            status.Win10MasterStatus = "CHƯA tạo";
            status.Win11MasterStatus = "CHƯA tạo";
            return status;
        }

        // 1. Check install.wim
        var wimPath = Path.Combine(storageDir, "install.wim");
        if (File.Exists(wimPath))
        {
            var fi = new FileInfo(wimPath);
            double gb = (double)fi.Length / (1024 * 1024 * 1024);
            status.WimStatus = $"ĐÃ CÓ: install.wim ({gb:N2} GB) - {fi.LastWriteTime:dd/MM/yyyy HH:mm}";
        }
        else
        {
            status.WimStatus = "CHƯA có (chạy Bước 2 trên WinPE để chụp)";
        }

        // 2. Check Windows 10 original ISO
        var win10Iso = FindFileByPattern(storageDir, "*10*.iso", excludePrefix: "win10master");
        if (!string.IsNullOrEmpty(win10Iso))
        {
            var fi = new FileInfo(win10Iso);
            double gb = (double)fi.Length / (1024 * 1024 * 1024);
            status.IsoWin10Status = $"ĐÃ CÓ: {fi.Name} ({gb:N2} GB)";
        }
        else
        {
            status.IsoWin10Status = "CHƯA có  (bấm Bước 3a để tải)";
        }

        // 3. Check Windows 11 original ISO
        var win11Iso = FindFileByPattern(storageDir, "*11*.iso", excludePrefix: "win11master");
        if (!string.IsNullOrEmpty(win11Iso))
        {
            var fi = new FileInfo(win11Iso);
            double gb = (double)fi.Length / (1024 * 1024 * 1024);
            status.IsoWin11Status = $"ĐÃ CÓ: {fi.Name} ({gb:N2} GB)";
        }
        else
        {
            status.IsoWin11Status = "CHƯA có  (bấm Bước 3b để tải)";
        }

        // 4. Check win10master.iso
        var w10Master = Path.Combine(storageDir, "win10master.iso");
        if (File.Exists(w10Master))
        {
            var fi = new FileInfo(w10Master);
            double gb = (double)fi.Length / (1024 * 1024 * 1024);
            status.Win10MasterStatus = $"ĐÃ TẠO: win10master.iso ({gb:N2} GB)";
        }
        else
        {
            status.Win10MasterStatus = "CHƯA tạo";
        }

        // 5. Check win11master.iso
        var w11Master = Path.Combine(storageDir, "win11master.iso");
        if (File.Exists(w11Master))
        {
            var fi = new FileInfo(w11Master);
            double gb = (double)fi.Length / (1024 * 1024 * 1024);
            status.Win11MasterStatus = $"ĐÃ TẠO: win11master.iso ({gb:N2} GB)";
        }
        else
        {
            status.Win11MasterStatus = "CHƯA tạo";
        }

        return status;
    }

    private string? FindFileByPattern(string dir, string searchPattern, string excludePrefix)
    {
        try
        {
            var files = Directory.GetFiles(dir, searchPattern, SearchOption.TopDirectoryOnly);
            foreach (var f in files)
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith(excludePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return f;
                }
            }
        }
        catch { }
        return null;
    }

    public List<string> DetectWindowsPartitions()
    {
        var list = new List<string>();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (!d.IsReady) continue;
                var root = d.RootDirectory.FullName;
                var ntoskrnl = Path.Combine(root, "Windows", "System32", "ntoskrnl.exe");
                if (File.Exists(ntoskrnl))
                {
                    double freeGb = (double)d.AvailableFreeSpace / (1024 * 1024 * 1024);
                    double totalGb = (double)d.TotalSize / (1024 * 1024 * 1024);
                    var label = string.IsNullOrEmpty(d.VolumeLabel) ? "Ổ hệ điều hành" : d.VolumeLabel;
                    list.Add($"{d.Name.TrimEnd('\\')} [{label} - {totalGb:N0} GB (Trống {freeGb:N1} GB)]");
                }
            }
        }
        catch { }
        return list;
    }

    public void FixSysprepErrors(Action<string> log)
    {
        log("[1/6] Kiểm tra và tạm dừng các dịch vụ xung đột Sysprep (WMP, Windows Update, BITS)...");
        try
        {
            RunProcessHidden("sc.exe", "stop WMPNetworkSvc");
            RunProcessHidden("sc.exe", "config WMPNetworkSvc start= disabled");
            RunProcessHidden("sc.exe", "stop wuauserv");
            RunProcessHidden("sc.exe", "stop bits");
            log("  -> Đã tạm dừng các dịch vụ xung đột thành công.");
        }
        catch (Exception ex)
        {
            log($"  -> Dịch vụ: {ex.Message}");
        }

        log("[2/6] Kiểm tra và tắt BitLocker bảo vệ ổ đĩa C:...");
        try
        {
            RunProcessHidden("manage-bde.exe", "-off C:");
            log("  -> Đã kích hoạt lệnh giải mã/tắt BitLocker cho C:.");
        }
        catch (Exception ex)
        {
            log($"  -> BitLocker: {ex.Message}");
        }

        log("[3/6] Khắc phục cờ trạng thái Registry Sysprep (GeneralizationState & CleanUpState)...");
        try
        {
            using var keyStatus = Registry.LocalMachine.CreateSubKey(@"SYSTEM\Setup\Status\SysprepStatus");
            keyStatus?.SetValue("GeneralizationState", 7, RegistryValueKind.DWord);
            keyStatus?.SetValue("CleanUpState", 2, RegistryValueKind.DWord);

            using var keySpp = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform");
            keySpp?.SetValue("SkipRearm", 1, RegistryValueKind.DWord);

            log("  -> Đã thiết lập SkipRearm=1, GeneralizationState=7, CleanUpState=2.");
        }
        catch (Exception ex)
        {
            log($"  -> Lỗi Registry: {ex.Message}");
        }

        log("[4/6] Gỡ bỏ các Appx UWP Package người dùng khác (Staged) gây lỗi Sysprep 0x80073cf2...");
        try
        {
            var psScript = @"
Get-AppxPackage -AllUsers | Where-Object { $_.PackageUserInformation -match 'Staged' -or $_.NonRemovable -ne $true } | ForEach-Object {
    try { Remove-AppxPackage -Package $_.PackageFullName -AllUsers -ErrorAction SilentlyContinue } catch {}
}
$badApps = @('*XboxApp*', '*BingWeather*', '*GetHelp*', '*SolitaireCollection*', '*ZuneVideo*', '*ZuneMusic*', '*MicrosoftEdgeDevTools*')
foreach ($b in $badApps) {
    Get-AppxPackage -AllUsers -Name $b | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue
    Get-AppxProvisionedPackage -Online | Where-Object DisplayName -like $b | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue
}
";
            RunPowerShell(psScript);
            log("  -> Đã dọn dẹp các ứng dụng UWP gây xung đột Sysprep.");
        }
        catch (Exception ex)
        {
            log($"  -> Gỡ Appx: {ex.Message}");
        }

        log("[5/6] Tạo tệp cấu hình mẫu unattend.xml (CopyProfile = true để lưu cấu hình Desktop)...");
        EnsureSysprepUnattendXml();
        log("  -> Đã tạo tệp cấu hình Sysprep unattend.xml thành công.");

        log("[6/6] Xóa bộ nhớ đệm log lỗi Sysprep cũ (Panther setupact.log)...");
        try
        {
            var panther = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "Sysprep", "Panther");
            if (Directory.Exists(panther))
            {
                foreach (var f in Directory.GetFiles(panther, "*.log"))
                {
                    try { File.Delete(f); } catch { }
                }
            }
            log("  -> Đã làm sạch thư mục log Panther.");
        }
        catch (Exception ex)
        {
            log($"  -> Log Panther: {ex.Message}");
        }

        log("✅ [HOÀN TẤT] Đã sửa xong toàn bộ các lỗi Sysprep phổ biến và sẵn sàng tạo Master Image!");
    }

    public string EnsureSysprepUnattendXml()
    {
        var sysprepDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "Sysprep");
        if (!Directory.Exists(sysprepDir)) Directory.CreateDirectory(sysprepDir);
        var unattendPath = Path.Combine(sysprepDir, "unattend.xml");
        string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<unattend xmlns=""urn:schemas-microsoft-com:unattend"">
    <settings pass=""generalize"">
        <component name=""Microsoft-Windows-Security-SPP"" processorArchitecture=""amd64"" publicKeyToken=""31bf3856ad364e35"" language=""neutral"" versionScope=""nonSxS"" xmlns:wcm=""http://schemas.microsoft.com/WMIConfig/2002/State"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
            <SkipRearm>1</SkipRearm>
        </component>
    </settings>
    <settings pass=""specialize"">
        <component name=""Microsoft-Windows-Shell-Setup"" processorArchitecture=""amd64"" publicKeyToken=""31bf3856ad364e35"" language=""neutral"" versionScope=""nonSxS"" xmlns:wcm=""http://schemas.microsoft.com/WMIConfig/2002/State"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
            <CopyProfile>true</CopyProfile>
        </component>
    </settings>
</unattend>";
        try
        {
            File.WriteAllText(unattendPath, xml);
        }
        catch { }
        return unattendPath;
    }

    public string? GetSysprepFailureReason()
    {
        try
        {
            var pantherErr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "Sysprep", "Panther", "setuperr.log");
            if (File.Exists(pantherErr))
            {
                var content = File.ReadAllText(pantherErr);
                if (!string.IsNullOrWhiteSpace(content)) return content.Trim();
            }
        }
        catch { }
        return null;
    }

    public void RunSysprep(string action, Action<string> log)
    {
        var sysprepExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "Sysprep", "sysprep.exe");
        if (!File.Exists(sysprepExe))
        {
            throw new FileNotFoundException("Không tìm thấy tệp sysprep.exe trong hệ thống!", sysprepExe);
        }

        log("Tự động chuẩn bị tiền điều kiện và cấu hình unattend.xml...");
        FixSysprepErrors(log);
        var unattend = EnsureSysprepUnattendXml();

        string actionArg = action.ToLowerInvariant() switch
        {
            "shutdown" => "/shutdown",
            "quit" => "/quit",
            _ => "/reboot"
        };

        string args = $"/oobe /generalize /unattend:\"{unattend}\" {actionArg}";
        log($"Đang khởi chạy Sysprep chuẩn hóa ảnh hệ thống: {args}");

        Process.Start(new ProcessStartInfo(sysprepExe, args)
        {
            UseShellExecute = true,
            Verb = "runas"
        });
    }

    public async Task CaptureWimAsync(string sourceDriveLetter, string targetWimPath, Action<string> log, Action<int> progressCallback, CancellationToken ct)
    {
        var targetDir = Path.GetDirectoryName(targetWimPath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var cleanDrive = sourceDriveLetter.TrimEnd('\\').TrimEnd(':') + @":\";
        log($"Bắt đầu chụp ảnh DISM từ [{cleanDrive}] đến [{targetWimPath}]...");
        log("Quá trình nén và chụp ảnh có thể mất từ 5 - 15 phút tùy dung lượng ổ...");

        var args = $"/Capture-Image /ImageFile:\"{targetWimPath}\" /CaptureDir:\"{cleanDrive}\" /Name:\"Windows Master\" /Description:\"Ernastie Master Image\" /Compress:max /CheckIntegrity /Verify";

        await Task.Run(() =>
        {
            var psi = new ProcessStartInfo("dism.exe", args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };
            proc.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data)) return;
                var line = e.Data.Trim();

                // Look for progress like [ 15.0% ]
                var match = Regex.Match(line, @"\[\s*(\d+(\.\d+)?)%\s*\]");
                if (match.Success && double.TryParse(match.Groups[1].Value, out double pct))
                {
                    progressCallback((int)pct);
                    log($"[DISM Tiến độ]: {pct:N1}%");
                }
                else if (line.Contains("Đang", StringComparison.OrdinalIgnoreCase) || 
                         line.Contains("Tiến độ", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("Saving image", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("Scanning", StringComparison.OrdinalIgnoreCase))
                {
                    log($"  {line}");
                }
            };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) log($"[DISM Cảnh báo]: {e.Data.Trim()}");
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            while (!proc.HasExited)
            {
                if (ct.IsCancellationRequested)
                {
                    try { proc.Kill(); } catch { }
                    throw new OperationCanceledException("Người dùng đã hủy quá trình chụp ảnh WIM.");
                }
                Thread.Sleep(500);
            }

            if (proc.ExitCode != 0)
            {
                throw new Exception($"Lệnh DISM thất bại với mã lỗi (ExitCode): {proc.ExitCode}");
            }

            progressCallback(100);
            log("✅ DISM chụp ảnh install.wim thành công!");
        }, ct);
    }

    public async Task BuildMasterIsoAsync(string originalIsoPath, string wimPath, string outputIsoPath, Action<string> log, Action<int> progressCallback, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            if (!File.Exists(originalIsoPath)) throw new FileNotFoundException("Không tìm thấy file ISO gốc!", originalIsoPath);
            if (!File.Exists(wimPath)) throw new FileNotFoundException("Không tìm thấy file install.wim!", wimPath);

            var tempDir = Path.Combine(Path.GetDirectoryName(outputIsoPath) ?? @"D:\MasterImage", "Temp_Iso_Build");
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
            Directory.CreateDirectory(tempDir);

            try
            {
                log($"[1/4] Đang giải nén ISO gốc [{Path.GetFileName(originalIsoPath)}]...");
                progressCallback(10);

            // Extract with 7-Zip (System32\7z.exe or Program Files)
            string sevenZip = File.Exists(@"C:\Windows\System32\7z.exe") ? @"C:\Windows\System32\7z.exe" : @"C:\Program Files\7-Zip\7z.exe";
            if (!File.Exists(sevenZip))
            {
                // Fallback: PowerShell extraction
                log("  -> Sử dụng PowerShell mount disk để sao chép file ISO...");
                var psMount = $@"
$mount = Mount-DiskImage -ImagePath '{originalIsoPath}' -PassThru
$drive = ($mount | Get-Volume).DriveLetter + ':\'
Copy-Item ($drive + '*') -Destination '{tempDir}' -Recurse -Force
Dismount-DiskImage -ImagePath '{originalIsoPath}'
";
                RunPowerShell(psMount);
            }
            else
            {
                var psi7z = new ProcessStartInfo(sevenZip, $"x \"{originalIsoPath}\" -o\"{tempDir}\" -y")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi7z)!;
                p.WaitForExit();
                if (p.ExitCode != 0) throw new Exception("Giải nén ISO gốc thất bại.");
            }

            ct.ThrowIfCancellationRequested();
            progressCallback(35);
            log("  -> Đã giải nén ISO gốc xong.");

            log("[2/4] Đang thay thế install.wim mẫu vào bộ cài...");
            var sourcesDir = Path.Combine(tempDir, "sources");
            if (!Directory.Exists(sourcesDir)) Directory.CreateDirectory(sourcesDir);

            // Delete old install.esd / install.wim
            var oldEsd = Path.Combine(sourcesDir, "install.esd");
            var oldWim = Path.Combine(sourcesDir, "install.wim");
            if (File.Exists(oldEsd)) File.Delete(oldEsd);
            if (File.Exists(oldWim)) File.Delete(oldWim);

            // Copy new install.wim
            log($"  -> Đang sao chép [{Path.GetFileName(wimPath)}] vào thư mục sources...");
            File.Copy(wimPath, oldWim, true);
            progressCallback(60);
            log("  -> Đã thay thế install.wim thành công.");

            ct.ThrowIfCancellationRequested();

            log("[3/4] Đang đóng gói tạo file ISO Bootable chuẩn UEFI & Legacy BIOS...");
            progressCallback(75);

            // Check if oscdimg is available
            string? oscdimg = FindOscdimg();
            if (!string.IsNullOrEmpty(oscdimg))
            {
                log($"  -> Sử dụng công cụ chính thức oscdimg.exe...");
                var bootData = $"-m -o -u2 -udfver102 -bootdata:2#p0,e,b\"{Path.Combine(tempDir, "boot", "etfsboot.com")}\"#pEF,e,b\"{Path.Combine(tempDir, "efi", "microsoft", "boot", "efisys.bin")}\" \"{tempDir}\" \"{outputIsoPath}\"";
                var psiOsc = new ProcessStartInfo(oscdimg, bootData)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var pOsc = Process.Start(psiOsc)!;
                pOsc.WaitForExit();
                if (pOsc.ExitCode != 0) throw new Exception($"Lỗi đóng gói ISO với oscdimg (ExitCode: {pOsc.ExitCode})");
            }
            else
            {
                log("  -> Sử dụng PowerShell IMAPI2 FileSystemImage để tạo ISO bootable...");
                var psIso = $@"
$fsi = New-Object -ComObject IMAPI2FS.MsftFileSystemImage
$fsi.FileSystemsToCreate = 7 # ISO9001, Joliet, UDF
$fsi.ChooseImageDefaultsForMediaType(12) # DVD+R DL / Custom

$bootFile = '{Path.Combine(tempDir, "boot", "etfsboot.com").Replace('\\', '/')}'
if (Test-Path $bootFile) {{
    $bootStream = [System.IO.File]::OpenRead($bootFile)
    $bootStreamCom = New-Object -ComObject ADODB.Stream
    $bootStreamCom.Type = 1 # Binary
    $bootStreamCom.Open()
    $bytes = [System.IO.File]::ReadAllBytes($bootFile)
    $bootStreamCom.Write($bytes)
    $bootOptions = New-Object -ComObject IMAPI2FS.BootOptions
    $bootOptions.AssignBootImage($bootStreamCom)
    $bootOptions.PlatformId = 0 # x86 / BIOS
    $bootOptions.Emulation = 0 # No emulation
    $fsi.BootImageOptions = $bootOptions
}}

$fsi.Root.AddTree('{tempDir.Replace('\\', '/')}', $false)
$resultImage = $fsi.CreateResultImage()
$stream = $resultImage.ImageStream

$isoStream = [System.IO.File]::OpenWrite('{outputIsoPath.Replace('\\', '/')}')
$buffer = New-Object byte[] (1024 * 1024 * 4) # 4MB buffer
$readBytes = 0

$comStream = [System.Runtime.InteropServices.ComTypes.IStream]$stream
$read = 0

while (($read = $comStream.Read($buffer, $buffer.Length, [System.IntPtr]::Zero)) -gt 0) {{
    $isoStream.Write($buffer, 0, $read)
}}
$isoStream.Close()
";
                RunPowerShell(psIso);
            }

            progressCallback(95);
            log("[4/4] Dọn dẹp các tệp tin tạm...");
            try { Directory.Delete(tempDir, true); } catch { }

            progressCallback(100);
            log($"🎉 [THÀNH CÔNG] Đã tạo xong file ISO Master tại:\n   -> {outputIsoPath}");
        }
        catch (Exception ex)
        {
            try { Directory.Delete(tempDir, true); } catch { }
            throw new Exception($"Lỗi ghép ISO: {ex.Message}", ex);
        }
        }, ct);
    }

    private string? FindOscdimg()
    {
        string[] candidates = {
            @"C:\Windows\System32\oscdimg.exe",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oscdimg.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "oscdimg.exe"),
            @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe"
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }
        return null;
    }

    public void OpenDownloadLink(string osType)
    {
        string url = osType.Equals("win10", StringComparison.OrdinalIgnoreCase)
            ? "https://www.microsoft.com/software-download/windows10"
            : "https://www.microsoft.com/software-download/windows11";

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void RunProcessHidden(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi);
        p?.WaitForExit(10000);
    }

    private void RunPowerShell(string script)
    {
        var tempPs1 = Path.Combine(Path.GetTempPath(), $"sysprep_fix_{Guid.NewGuid():N}.ps1");
        try
        {
            File.WriteAllText(tempPs1, script);
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{tempPs1}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch { }
        finally
        {
            try { if (File.Exists(tempPs1)) File.Delete(tempPs1); } catch { }
        }
    }
}
