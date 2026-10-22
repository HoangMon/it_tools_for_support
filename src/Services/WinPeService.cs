using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Ernastie.Models;

namespace Ernastie.Services;

public class WinPeService
{
    public static readonly string DefaultAnhdvDir = @"C:\AnhdvBootTool";
    public static readonly string DefaultWinPeDir = @"C:\WinPE";

    /// <summary>
    /// Checks for existing AnhdvBootTool installations or WinPE files
    /// </summary>
    public (bool Found, string Path, string StatusMessage) CheckAnhdvBootStatus()
    {
        var possiblePaths = new[]
        {
            Path.Combine(DefaultAnhdvDir, "AnhdvBootTool.exe"),
            Path.Combine(DefaultAnhdvDir, "One_Click_Anhdv_Boot.bat"),
            Path.Combine(@"D:\AnhdvBootTool", "AnhdvBootTool.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnhdvBootTool", "AnhdvBootTool.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "AnhdvBootTool.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AnhdvBootTool", "AnhdvBootTool.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "AnhdvBootTool", "AnhdvBootTool.exe")
        };

        foreach (var p in possiblePaths)
        {
            if (File.Exists(p))
            {
                var dir = Path.GetDirectoryName(p) ?? DefaultAnhdvDir;
                return (true, dir, $"Đã tìm thấy AnhdvBootTool tại: {p}");
            }
        }

        // Check if directory exists even if exe isn't there yet
        if (Directory.Exists(DefaultAnhdvDir))
        {
            var files = Directory.GetFiles(DefaultAnhdvDir, "*.*", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return (true, DefaultAnhdvDir, $"Tìm thấy thư mục AnhdvBootTool ({files.Length} tệp tin).");
            }
        }

        return (false, DefaultAnhdvDir, "Chưa có AnhdvBootTool. Bấm 'Tải & giải nén tất cả' để chọn nơi lưu, hoặc tự tải về C:\\AnhdvBootTool.");
    }

    /// <summary>
    /// Finds any available boot.sdi on the system
    /// </summary>
    public string? FindSystemBootSdi()
    {
        var candidates = new[]
        {
            @"C:\Windows\Boot\DVD\EFI\boot.sdi",
            @"C:\Windows\Boot\DVD\PCAT\boot.sdi",
            @"C:\Windows\System32\boot.sdi",
            @"C:\Windows\SysWOW64\boot.sdi"
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        return null;
    }

    /// <summary>
    /// Searches local drives for a bootable WinPE WIM file (boot.wim, winpe.wim, W11PE.wim, W10PE.wim)
    /// </summary>
    public string? FindAvailableWinPeWim(string? customDir = null)
    {
        var locations = new List<string>();

        if (!string.IsNullOrWhiteSpace(customDir) && Directory.Exists(customDir))
        {
            locations.Add(Path.Combine(customDir, "boot.wim"));
            locations.Add(Path.Combine(customDir, "winpe.wim"));
            locations.Add(Path.Combine(customDir, "W11PE.wim"));
            locations.Add(Path.Combine(customDir, "W10PE.wim"));
            locations.Add(Path.Combine(customDir, "sources", "boot.wim"));
            locations.Add(Path.Combine(customDir, "ISO", "sources", "boot.wim"));
            locations.Add(Path.Combine(customDir, "apps", "boot.wim"));
        }

        locations.AddRange(new[]
        {
            Path.Combine(DefaultAnhdvDir, "boot.wim"),
            Path.Combine(DefaultAnhdvDir, "winpe.wim"),
            Path.Combine(DefaultAnhdvDir, "W11PE.wim"),
            Path.Combine(DefaultAnhdvDir, "W10PE.wim"),
            Path.Combine(DefaultAnhdvDir, "sources", "boot.wim"),
            Path.Combine(@"D:\AnhdvBootTool", "boot.wim"),
            Path.Combine(@"D:\AnhdvBootTool", "winpe.wim"),
            Path.Combine(DefaultWinPeDir, "boot.wim"),
            Path.Combine(DefaultWinPeDir, "winpe.wim"),
            Path.Combine(@"D:\WinPE", "boot.wim"),
            Path.Combine(@"D:\WinPE", "winpe.wim"),
            @"C:\Recovery\WindowsRE\Winre.wim",
            @"C:\Windows\System32\Recovery\winre.wim"
        });

        foreach (var loc in locations)
        {
            if (File.Exists(loc)) return loc;
        }

        // Also check if any drive labeled WINPE or any drive has sources\boot.wim or boot.wim
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.IsReady)
                {
                    var p1 = Path.Combine(d.RootDirectory.FullName, "sources", "boot.wim");
                    if (File.Exists(p1)) return p1;
                    var p2 = Path.Combine(d.RootDirectory.FullName, "boot.wim");
                    if (File.Exists(p2)) return p2;
                    var p3 = Path.Combine(d.RootDirectory.FullName, "winpe.wim");
                    if (File.Exists(p3)) return p3;
                    var p4 = Path.Combine(d.RootDirectory.FullName, "W11PE.wim");
                    if (File.Exists(p4)) return p4;
                    var p5 = Path.Combine(d.RootDirectory.FullName, "W10PE.wim");
                    if (File.Exists(p5)) return p5;
                }
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Configures Windows BCD using bcdedit to boot into WinPE.
    /// Supports both bootOnce (bcdedit /bootsequence) and permanent boot (bcdedit /displayorder).
    /// </summary>
    public (bool Success, string Message, string? EntryGuid) ConfigureWinPeBcd(
        string wimPath,
        bool bootOnce,
        string entryTitle = "Anhdv Boot WinPE Rescue",
        Action<string>? log = null)
    {
        if (!File.Exists(wimPath))
        {
            return (false, $"Tệp WinPE WIM không tồn tại tại: {wimPath}", null);
        }

        var fullWimPath = Path.GetFullPath(wimPath);
        var driveLetter = Path.GetPathRoot(fullWimPath)?.TrimEnd('\\');
        if (string.IsNullOrEmpty(driveLetter)) driveLetter = "C:";

        var relativeWimPath = fullWimPath.Substring(driveLetter.Length);
        if (!relativeWimPath.StartsWith('\\')) relativeWimPath = "\\" + relativeWimPath;

        log?.Invoke($"Đang chuẩn bị BCD Boot: Ổ đĩa = {driveLetter}, Đường dẫn WIM = {relativeWimPath}");

        // 1. Ensure boot.sdi exists in the target directory or drive root
        var sdiTargetDir = Path.GetDirectoryName(fullWimPath) ?? driveLetter + "\\";
        var sdiTargetPath = Path.Combine(sdiTargetDir, "boot.sdi");
        if (!File.Exists(sdiTargetPath))
        {
            var sysSdi = FindSystemBootSdi();
            if (sysSdi != null && File.Exists(sysSdi))
            {
                try
                {
                    File.Copy(sysSdi, sdiTargetPath, true);
                    log?.Invoke($"Đã sao chép boot.sdi vào: {sdiTargetPath}");
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Cảnh báo khi sao chép boot.sdi: {ex.Message}");
                }
            }
        }

        var relativeSdiPath = sdiTargetPath.Substring(driveLetter.Length);
        if (!relativeSdiPath.StartsWith('\\')) relativeSdiPath = "\\" + relativeSdiPath;

        // 2. Setup Ramdisk Options in BCD
        log?.Invoke("Đang cấu hình Ramdisk Device Options trong BCD...");
        RunBcdCommand("create {ramdiskoptions} /d \"Ramdisk Options\"");
        RunBcdCommand($"set {{ramdiskoptions}} ramdisksdidevice partition={driveLetter}");
        RunBcdCommand($"set {{ramdiskoptions}} ramdisksdipath {relativeSdiPath}");

        // 3. Create Boot Entry
        log?.Invoke($"Tạo mục nạp hệ điều hành mới: '{entryTitle}'...");
        var createResult = RunBcdCommand($"create /d \"{entryTitle}\" /application osloader");
        var match = Regex.Match(createResult, @"\{[a-fA-F0-9\-]+\}");
        if (!match.Success)
        {
            return (false, $"Không tạo được mục Bootloader BCD: {createResult}", null);
        }

        var guid = match.Value;
        log?.Invoke($"Mã GUID tạo thành công: {guid}");

        // 4. Determine UEFI or Legacy BIOS
        var isUefi = CheckIfUefi();
        var winload = isUefi ? @"\windows\system32\winload.efi" : @"\windows\system32\winload.exe";
        log?.Invoke($"Hệ thống phát hiện: {(isUefi ? "GPT - UEFI" : "MBR - Legacy BIOS")} (Loader: {winload})");

        // 5. Configure loader parameters
        RunBcdCommand($"set {guid} device ramdisk=[{driveLetter}]{relativeWimPath},{{ramdiskoptions}}");
        RunBcdCommand($"set {guid} osdevice ramdisk=[{driveLetter}]{relativeWimPath},{{ramdiskoptions}}");
        RunBcdCommand($"set {guid} path {winload}");
        RunBcdCommand($"set {guid} systemroot \\windows");
        RunBcdCommand($"set {guid} winpe yes");
        RunBcdCommand($"set {guid} detecthal yes");

        if (bootOnce)
        {
            // One-time boot sequence
            log?.Invoke("Thiết lập Boot 1 lần: bcdedit /bootsequence...");
            RunBcdCommand($"set {{bootmgr}} bootsequence {guid}");
            RunBcdCommand("set {bootmgr} timeout 5");
            log?.Invoke("✅ Đã thiết lập Boot 1 lần vào WinPE thành công! Lần khởi động kế tiếp máy tính sẽ vào WinPE.");
            return (true, "Đã cấu hình Boot 1 lần vào WinPE thành công! Lần khởi động kế tiếp máy tính sẽ vào môi trường cứu hộ.", guid);
        }
        else
        {
            // Permanent boot entry
            log?.Invoke("Thiết lập Boot vĩnh viễn: bcdedit /displayorder...");
            RunBcdCommand($"displayorder {guid} /addlast");
            RunBcdCommand("set {bootmgr} timeout 10");
            log?.Invoke("✅ Đã thêm mục WinPE vào Menu Boot vĩnh viễn thành công! Thời gian chờ menu: 10 giây.");
            return (true, "Đã thêm mục WinPE vào Menu Boot vĩnh viễn thành công! Menu khởi động sẽ hiển thị mỗi khi bật máy.", guid);
        }
    }

    /// <summary>
    /// Creates a 25GB NTFS WinPE partition by shrinking Drive C:
    /// </summary>
    public (bool Success, string Message, string? DriveLetter) Create25GbWinPePartition(Action<string>? log = null)
    {
        try
        {
            log?.Invoke("Đang kiểm tra dung lượng ổ đĩa hiện tại...");
            var driveC = new DriveInfo("C");
            if (!driveC.IsReady)
            {
                return (false, "Không thể đọc thông tin ổ C:", null);
            }

            var freeGb = driveC.TotalFreeSpace / (1024.0 * 1024 * 1024);
            log?.Invoke($"Dung lượng trống trên ổ C: {freeGb:F1} GB.");

            if (freeGb < 27)
            {
                return (false, $"Dung lượng trống ổ C: còn {freeGb:F1} GB, không đủ để co 25GB (cần tối thiểu 27GB trống).", null);
            }

            // Run fix partition limits first to ensure unmovable files don't block shrink
            log?.Invoke("Tối ưu hóa hệ thống để mở khóa giới hạn co ổ đĩa (Fix Partition Limits)...");
            RunCommandLine("powercfg -h off");
            RunCommandLine("fsutil usn deletejournal /d C:");
            RunCommandLine("vssadmin delete shadows /all /quiet");

            int diskNum = GetDiskNumberForDrive("C");
            log?.Invoke($"Đã phát hiện phân vùng C: nằm trên Ổ đĩa vật lý (Disk) #{diskNum}.");

            // Diskpart script creation with explicit disk and volume selection
            var scriptPath = Path.Combine(Path.GetTempPath(), "diskpart_winpe_25gb.txt");
            var scriptContent = new StringBuilder();
            scriptContent.AppendLine($"select disk {diskNum}");
            scriptContent.AppendLine("select volume C");
            scriptContent.AppendLine("shrink desired=25600 minimum=5120");
            scriptContent.AppendLine($"select disk {diskNum}");
            scriptContent.AppendLine("create partition primary");
            scriptContent.AppendLine("format fs=ntfs quick label=\"WINPE\"");
            scriptContent.AppendLine("assign");
            scriptContent.AppendLine("exit");

            File.WriteAllText(scriptPath, scriptContent.ToString());
            log?.Invoke("Đang thực thi Diskpart để tạo phân vùng WinPE 25GB (NTFS)...");

            var output = RunCommandLine($"diskpart /s \"{scriptPath}\"");
            log?.Invoke(output);

            try { File.Delete(scriptPath); } catch { }

            // Check if WINPE was created
            var winPeDrive = FindWinPeDrive();

            // If Diskpart failed, try PowerShell Storage API fallback
            if (winPeDrive == null)
            {
                log?.Invoke("⚠️ Diskpart chưa tạo được phân vùng. Đang thử cơ chế dự phòng bằng PowerShell Storage API...");
                var psScript = @"
try {
    $p = Get-Partition -DriveLetter C -ErrorAction Stop
    $targetSize = $p.Size - 25GB
    if ($targetSize -gt 15GB) {
        Resize-Partition -DriveLetter C -Size $targetSize -ErrorAction SilentlyContinue
    }
    $newP = New-Partition -DiskNumber $p.DiskNumber -UseMaximumSize -AssignDriveLetter -ErrorAction Stop
    Format-Volume -Partition $newP -FileSystem NTFS -NewFileSystemLabel 'WINPE' -Confirm:$false
} catch {
    Write-Output ('Lỗi PowerShell Storage: ' + $_.Exception.Message)
}
";
                var tempPs1 = Path.Combine(Path.GetTempPath(), "resize_winpe.ps1");
                File.WriteAllText(tempPs1, psScript);
                var psOut = RunCommandLine($"powershell -NoProfile -ExecutionPolicy Bypass -File \"{tempPs1}\"");
                log?.Invoke(psOut);
                try { File.Delete(tempPs1); } catch { }

                winPeDrive = FindWinPeDrive();
            }

            if (winPeDrive != null)
            {
                var letter = winPeDrive.Name.TrimEnd('\\');
                log?.Invoke($"✅ Tạo phân vùng WINPE thành công tại ổ [{letter}]!");

                // Create standard folder structure
                var dirs = new[] { "Boot", "sources", "Drivers", "Apps", "ISO" };
                foreach (var sub in dirs)
                {
                    var dirPath = Path.Combine(winPeDrive.RootDirectory.FullName, sub);
                    Directory.CreateDirectory(dirPath);
                }

                // Copy boot.sdi into the new partition
                var sysSdi = FindSystemBootSdi();
                if (sysSdi != null && File.Exists(sysSdi))
                {
                    try
                    {
                        File.Copy(sysSdi, Path.Combine(winPeDrive.RootDirectory.FullName, "boot.sdi"), true);
                        File.Copy(sysSdi, Path.Combine(winPeDrive.RootDirectory.FullName, "Boot", "boot.sdi"), true);
                        log?.Invoke("Đã khởi tạo sẵn cấu trúc thư mục cứu hộ và boot.sdi trên phân vùng WinPE.");
                    }
                    catch { }
                }

                return (true, $"Đã tạo thành công phân vùng WinPE (ổ {letter})!", letter);
            }

            return (false, "Không thể co ổ đĩa và tạo phân vùng WinPE. Vui lòng kiểm tra xem ổ C có đang bị BitLocker khóa hoặc giới hạn co ổ không.", null);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi tạo phân vùng WinPE: {ex.Message}", null);
        }
    }

    public int GetDiskNumberForDrive(string driveLetter)
    {
        char letter = driveLetter.Trim().ToUpperInvariant()[0];
        try
        {
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"(Get-Partition -DriveLetter {letter} -ErrorAction SilentlyContinue).DiskNumber\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                var outText = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(3000);
                if (int.TryParse(outText, out int diskNum)) return diskNum;
            }
        }
        catch { }

        return 0;
    }

    private DriveInfo? FindWinPeDrive()
    {
        System.Threading.Thread.Sleep(2000);
        foreach (var d in DriveInfo.GetDrives())
        {
            if (d.IsReady && string.Equals(d.VolumeLabel, "WINPE", StringComparison.OrdinalIgnoreCase))
            {
                return d;
            }
        }
        return null;
    }

    /// <summary>
    /// Scans BCD for WinPE entries and deletes them cleanly
    /// </summary>
    public (bool Success, string Message, int RemovedCount) CleanWinPeBcdEntries(Action<string>? log = null)
    {
        try
        {
            log?.Invoke("Đang quét toàn bộ danh sách Bootloader BCD...");
            var bcdEnum = RunBcdCommand("enum all");
            var lines = bcdEnum.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var currentGuid = string.Empty;
            var currentDesc = string.Empty;
            var isWinPeEntry = false;
            var toDelete = new List<(string Guid, string Desc)>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("identifier", StringComparison.OrdinalIgnoreCase))
                {
                    currentGuid = trimmed.Substring(10).Trim();
                    currentDesc = string.Empty;
                    isWinPeEntry = false;
                }
                else if (trimmed.StartsWith("description", StringComparison.OrdinalIgnoreCase))
                {
                    currentDesc = trimmed.Substring(11).Trim();
                    if (currentDesc.Contains("WinPE", StringComparison.OrdinalIgnoreCase) ||
                        currentDesc.Contains("Anhdv", StringComparison.OrdinalIgnoreCase) ||
                        currentDesc.Contains("Rescue", StringComparison.OrdinalIgnoreCase))
                    {
                        isWinPeEntry = true;
                    }
                }
                else if (trimmed.StartsWith("winpe", StringComparison.OrdinalIgnoreCase))
                {
                    if (trimmed.Contains("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        isWinPeEntry = true;
                    }
                }

                if (isWinPeEntry && !string.IsNullOrEmpty(currentGuid) && !currentGuid.Equals("{current}", StringComparison.OrdinalIgnoreCase))
                {
                    if (!toDelete.Exists(x => x.Guid.Equals(currentGuid, StringComparison.OrdinalIgnoreCase)))
                    {
                        toDelete.Add((currentGuid, currentDesc));
                    }
                }
            }

            int count = 0;
            foreach (var item in toDelete)
            {
                log?.Invoke($"Đang gỡ bỏ mục BCD: {item.Desc} ({item.Guid})...");
                RunBcdCommand($"delete {item.Guid} /cleanup");
                count++;
            }

            log?.Invoke($"✅ Đã dọn dẹp sạch sẽ {count} mục boot WinPE trong BCD.");
            return (true, $"Đã gỡ bỏ {count} mục boot WinPE khỏi hệ thống.", count);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi dọn dẹp Boot WinPE: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Injects Intel RST / VMD and/or Host WiFi drivers into a WIM file using DISM
    /// </summary>
    public (bool Success, string Message) InjectDriversIntoWim(
        string wimPath,
        bool includeVmd,
        bool includeWifi,
        Action<string>? log = null)
    {
        if (!File.Exists(wimPath))
        {
            return (false, $"Không tìm thấy file WIM: {wimPath}");
        }

        var mountDir = Path.Combine(Path.GetTempPath(), "WinPe_Mount_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(mountDir);

        try
        {
            log?.Invoke($"Đang gắn kết ảnh WIM vào thư mục tạm: {mountDir}...");
            var mountOut = RunCommandLine($"dism /Mount-Wim /WimFile:\"{wimPath}\" /Index:1 /MountDir:\"{mountDir}\"");
            log?.Invoke(mountOut);

            if (includeWifi)
            {
                var wifiDriverDir = Path.Combine(Path.GetTempPath(), "Host_Wifi_Drivers");
                Directory.CreateDirectory(wifiDriverDir);
                log?.Invoke("Đang trích xuất driver WiFi từ máy chủ hiện tại...");
                RunCommandLine($"dism /online /export-driver /destination:\"{wifiDriverDir}\"");
                log?.Invoke($"Đang nạp driver trích xuất vào WinPE WIM...");
                var addWifiOut = RunCommandLine($"dism /Image:\"{mountDir}\" /Add-Driver /Driver:\"{wifiDriverDir}\" /Recurse /ForceUnsigned");
                log?.Invoke(addWifiOut);
                try { Directory.Delete(wifiDriverDir, true); } catch { }
            }

            if (includeVmd)
            {
                // Scan for local IRST / VMD drivers
                var sys32Drivers = @"C:\Windows\System32\DriverStore\FileRepository";
                log?.Invoke("Đang tìm kiếm driver Intel RST / VMD Controller trong DriverStore...");
                if (Directory.Exists(sys32Drivers))
                {
                    var vmdFolders = Directory.GetDirectories(sys32Drivers, "*iaStorVD*");
                    if (vmdFolders.Length == 0) vmdFolders = Directory.GetDirectories(sys32Drivers, "*rst*");

                    foreach (var f in vmdFolders)
                    {
                        log?.Invoke($"Nạp driver IRST từ: {Path.GetFileName(f)}...");
                        RunCommandLine($"dism /Image:\"{mountDir}\" /Add-Driver /Driver:\"{f}\" /Recurse /ForceUnsigned");
                    }
                }
            }

            log?.Invoke("Đang lưu thay đổi và đóng gói lại tệp WIM (Commit)...");
            var unmountOut = RunCommandLine($"dism /Unmount-Wim /MountDir:\"{mountDir}\" /Commit");
            log?.Invoke(unmountOut);

            return (true, "Đã tích hợp driver vào file WinPE WIM thành công!");
        }
        catch (Exception ex)
        {
            log?.Invoke($"Lỗi: {ex.Message}. Đang hủy gắn kết...");
            RunCommandLine($"dism /Unmount-Wim /MountDir:\"{mountDir}\" /Discard");
            return (false, $"Lỗi trong quá trình tích hợp driver: {ex.Message}");
        }
        finally
        {
            try { if (Directory.Exists(mountDir)) Directory.Delete(mountDir, true); } catch { }
        }
    }

    /// <summary>
    /// Creates a bootable USB rescue drive from a WIM file
    /// </summary>
    public (bool Success, string Message) CreateUsbBoot(string usbDriveLetter, string wimPath, Action<string>? log = null)
    {
        try
        {
            var letter = usbDriveLetter.TrimEnd('\\');
            log?.Invoke($"Bắt đầu khởi tạo USB Boot cứu hộ trên ổ [{letter}]...");

            var root = letter + "\\";
            var bootDir = Path.Combine(root, "Boot");
            var efiBootDir = Path.Combine(root, "EFI", "Boot");
            var sourcesDir = Path.Combine(root, "sources");

            Directory.CreateDirectory(bootDir);
            Directory.CreateDirectory(efiBootDir);
            Directory.CreateDirectory(sourcesDir);

            // Copy boot.sdi
            var sdi = FindSystemBootSdi();
            if (sdi != null && File.Exists(sdi))
            {
                File.Copy(sdi, Path.Combine(bootDir, "boot.sdi"), true);
                File.Copy(sdi, Path.Combine(root, "boot.sdi"), true);
            }

            // Copy WIM file as sources\boot.wim
            var targetWim = Path.Combine(sourcesDir, "boot.wim");
            log?.Invoke($"Đang sao chép file WIM vào {targetWim}...");
            File.Copy(wimPath, targetWim, true);

            // Write MBR bootcode to USB
            log?.Invoke("Nạp mã khởi động bootsect /nt60 vào USB...");
            var bootsectOut = RunCommandLine($"bootsect /nt60 {letter} /mbr /force");
            log?.Invoke(bootsectOut);

            log?.Invoke($"✅ Tạo USB Boot WinPE thành công trên ổ [{letter}]!");
            return (true, $"Tạo USB Boot WinPE thành công trên ổ {letter}!");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi tạo USB Boot: {ex.Message}");
        }
    }

    private string RunBcdCommand(string args)
    {
        return RunCommandLine($"bcdedit {args}");
    }

    private string RunCommandLine(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var p = Process.Start(psi);
            if (p == null) return string.Empty;

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(30000);

            return (stdout + "\n" + stderr).Trim();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private bool CheckIfUefi()
    {
        try
        {
            var bcd = RunBcdCommand("enum {current}");
            return bcd.Contains("winload.efi", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }
}
