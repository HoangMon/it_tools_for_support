using System.Diagnostics;
using Ernastie.Models;

namespace Ernastie.Services;

public class BackupService
{
    public delegate void OutputHandler(string message);

    public List<WifiProfileModel> GetWifiProfiles()
    {
        var list = new List<WifiProfileModel>();
        try
        {
            var psi = new ProcessStartInfo("netsh.exe", "wlan show profiles")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            var names = new List<string>();
            using (var reader = new StringReader(output))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    int colonIdx = line.IndexOf(':');
                    if (colonIdx > 0 && (line.Contains("Profile") || line.Contains("hồ sơ")))
                    {
                        var name = line.Substring(colonIdx + 1).Trim();
                        if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                        {
                            names.Add(name);
                        }
                    }
                }
            }

            foreach (var name in names)
            {
                var item = new WifiProfileModel { Name = name };
                try
                {
                    var psiDetail = new ProcessStartInfo("netsh.exe", $"wlan show profile name=\"{name}\" key=clear")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using var procDetail = Process.Start(psiDetail)!;
                    var detailOutput = procDetail.StandardOutput.ReadToEnd();
                    procDetail.WaitForExit();

                    using var detailReader = new StringReader(detailOutput);
                    string? dLine;
                    while ((dLine = detailReader.ReadLine()) != null)
                    {
                        int cIdx = dLine.IndexOf(':');
                        if (cIdx <= 0) continue;
                        var left = dLine.Substring(0, cIdx).Trim();
                        var right = dLine.Substring(cIdx + 1).Trim();

                        if (left.Contains("Authentication") || left.Contains("Xác thực"))
                        {
                            item.Authentication = right;
                        }
                        else if (left.Contains("Key Content") || left.Contains("Nội dung khoá") || left.Contains("khoá"))
                        {
                            item.Password = right;
                        }
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(item.Password))
                {
                    item.Password = "(Không có mật khẩu / Open)";
                }

                list.Add(item);
            }
        }
        catch { }

        return list;
    }

    public int ExportWifiProfiles(string targetFolder)
    {
        targetFolder = targetFolder.TrimEnd('\\', '/');
        Directory.CreateDirectory(targetFolder);

        var psi = new ProcessStartInfo("netsh.exe", $"wlan export profile folder=\"{targetFolder}\" key=clear")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var proc = Process.Start(psi)!;
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        // Also export a human-readable text file with all passwords
        try
        {
            var profiles = GetWifiProfiles();
            if (profiles.Count > 0)
            {
                var txtPath = Path.Combine(targetFolder, "DanhSach_MatKhau_WiFi.txt");
                using var sw = new StreamWriter(txtPath, false, System.Text.Encoding.UTF8);
                sw.WriteLine("============================================================");
                sw.WriteLine("        DANH SÁCH HỒ SƠ & MẬT KHẨU WIFI ĐÃ SAO LƯU          ");
                sw.WriteLine($"  Thời gian sao lưu : {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                sw.WriteLine($"  Máy tính          : {Environment.MachineName}");
                sw.WriteLine("============================================================");
                sw.WriteLine();
                int idx = 1;
                foreach (var p in profiles)
                {
                    sw.WriteLine($"[{idx++}] SSID / Tên WiFi : {p.Name}");
                    sw.WriteLine($"    Kiểu xác thực  : {p.Authentication}");
                    sw.WriteLine($"    Mật khẩu       : {p.Password}");
                    sw.WriteLine("------------------------------------------------------------");
                }
            }
        }
        catch { }

        var xmlFiles = Directory.GetFiles(targetFolder, "*.xml", SearchOption.TopDirectoryOnly);
        return xmlFiles.Length;
    }

    public int RestoreWifiProfiles(string folderOrFile, Action<string>? log = null)
    {
        var files = new List<string>();
        if (File.Exists(folderOrFile) && folderOrFile.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            files.Add(folderOrFile);
        }
        else if (Directory.Exists(folderOrFile))
        {
            files.AddRange(Directory.GetFiles(folderOrFile, "*.xml", SearchOption.TopDirectoryOnly));
        }

        int successCount = 0;
        foreach (var file in files)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh.exe", $"wlan add profile filename=\"{file}\" user=all")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi)!;
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    successCount++;
                    log?.Invoke($"Đã khôi phục: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"Lỗi nạp file {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return successCount;
    }

    public async Task BackupWifiAsync(string targetFolder, OutputHandler? log = null)
    {
        Directory.CreateDirectory(targetFolder);
        log?.Invoke($"Đang xuất cấu hình WiFi vào: {targetFolder}...");

        await Task.Run(() =>
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = $"wlan export profile folder=\"{targetFolder}\" key=clear",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                proc.Start();
                var res = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                log?.Invoke(res);
                log?.Invoke("Đã hoàn tất sao lưu hồ sơ WiFi!");
            }
            catch (Exception ex)
            {
                log?.Invoke($"Lỗi sao lưu WiFi: {ex.Message}");
            }
        });
    }

    public async Task BackupDriversAsync(string targetFolder, OutputHandler? log = null)
    {
        Directory.CreateDirectory(targetFolder);
        log?.Invoke($"Đang xuất toàn bộ Driver hệ thống bằng DISM vào: {targetFolder}...");

        await Task.Run(() =>
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/online /export-driver /destination:\"{targetFolder}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) log?.Invoke(e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) log?.Invoke("[Lỗi] " + e.Data); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                log?.Invoke("Đã hoàn tất sao lưu Drivers!");
            }
            catch (Exception ex)
            {
                log?.Invoke($"Lỗi xuất driver: {ex.Message}");
            }
        });
    }

    public void OpenBrandDriverPage(string manufacturer)
    {
        var m = manufacturer.ToLowerInvariant();
        string url;
        if (m.Contains("dell")) url = "https://www.dell.com/support/home/vi-vn/products/drivers";
        else if (m.Contains("hp") || m.Contains("hewlett")) url = "https://support.hp.com/vn-en/drivers";
        else if (m.Contains("lenovo")) url = "https://pcsupport.lenovo.com/vn/vi";
        else if (m.Contains("asus")) url = "https://www.asus.com/support/Download-Center/";
        else if (m.Contains("acer")) url = "https://www.acer.com/vn-vi/support";
        else url = "https://www.google.com/search?q=" + Uri.EscapeDataString(manufacturer + " drivers support");

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    public async Task BackupUserDataAsync(string destDrive, OutputHandler? log = null)
    {
        var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var destFolder = Path.Combine(destDrive, $"Backup_{Environment.UserName}_{DateTime.Now:yyyyMMdd}");
        Directory.CreateDirectory(destFolder);
        log?.Invoke($"Đang sao lưu Desktop & Documents sang: {destFolder}...");

        await Task.Run(() =>
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            CopyFolder(desktop, Path.Combine(destFolder, "Desktop"), log);
            CopyFolder(docs, Path.Combine(destFolder, "Documents"), log);
            log?.Invoke("Đã hoàn tất sao lưu dữ liệu cá nhân!");
        });
    }

    private void CopyFolder(string source, string dest, OutputHandler? log)
    {
        if (!Directory.Exists(source)) return;
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
        {
            try
            {
                var target = Path.Combine(dest, Path.GetFileName(file));
                File.Copy(file, target, true);
            }
            catch { }
        }
        foreach (var dir in Directory.GetDirectories(source))
        {
            try
            {
                CopyFolder(dir, Path.Combine(dest, Path.GetFileName(dir)), log);
            }
            catch { }
        }
    }
}
