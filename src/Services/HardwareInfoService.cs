using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Ernastie.Services;

public class DetailedHardwareInfo
{
    // 1. General
    public string MachineName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ServiceTag { get; set; } = string.Empty;
    public string BiosInfo { get; set; } = string.Empty;
    public string CpuInfo { get; set; } = string.Empty;
    public string BatteryInfo { get; set; } = string.Empty;

    // 2. Windows & Licensing
    public string OsName { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ActivationStatus { get; set; } = string.Empty;
    public string InstalledKey { get; set; } = string.Empty;
    public string OemEdition { get; set; } = string.Empty;
    public string OemKey { get; set; } = string.Empty;

    // 3. GPU
    public List<string> Gpus { get; set; } = new();

    // 4. RAM
    public List<string> RamSlots { get; set; } = new();

    // 5. Disks
    public List<string> Disks { get; set; } = new();
    public string UpgradeInfo { get; set; } = string.Empty;
}

public class HardwareInfoService
{
    public DetailedHardwareInfo GetDetailedHardwareInfo()
    {
        var info = new DetailedHardwareInfo();

        // 1. General Info
        info.MachineName = Environment.MachineName;
        GetComputerSystemAndBios(info);
        GetCpuInfo(info);
        GetBatteryInfo(info);

        // 2. Windows & Licensing
        GetWindowsAndLicense(info);

        // 3. GPU
        GetGpuInfo(info);

        // 4. RAM
        GetRamInfo(info);

        // 5. Disks
        GetDiskInfo(info);

        return info;
    }

    private void GetComputerSystemAndBios(DetailedHardwareInfo info)
    {
        try
        {
            using var searcherCs = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (ManagementObject mo in searcherCs.Get())
            {
                info.Manufacturer = mo["Manufacturer"]?.ToString()?.Trim() ?? "";
                info.Model = mo["Model"]?.ToString()?.Trim() ?? "";
            }
        }
        catch { }

        // Fallback to Registry if WMI was empty
        if (string.IsNullOrWhiteSpace(info.Manufacturer) || string.IsNullOrWhiteSpace(info.Model))
        {
            try
            {
                using var rk = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
                if (rk != null)
                {
                    if (string.IsNullOrWhiteSpace(info.Manufacturer))
                        info.Manufacturer = rk.GetValue("SystemManufacturer")?.ToString()?.Trim() ?? "Chưa rõ";
                    if (string.IsNullOrWhiteSpace(info.Model))
                        info.Model = rk.GetValue("SystemProductName")?.ToString()?.Trim() ?? "Chưa rõ";
                }
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(info.Manufacturer)) info.Manufacturer = "Chưa rõ";
        if (string.IsNullOrWhiteSpace(info.Model)) info.Model = "Chưa rõ";

        try
        {
            using var searcherBios = new ManagementObjectSearcher("SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate, SerialNumber FROM Win32_BIOS");
            foreach (ManagementObject mo in searcherBios.Get())
            {
                var mfg = mo["Manufacturer"]?.ToString()?.Trim() ?? "AMI";
                var ver = mo["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? "";
                var serial = mo["SerialNumber"]?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(serial))
                    info.ServiceTag = serial;

                string dateStr = "";
                if (mo["ReleaseDate"] != null)
                {
                    var rawDate = mo["ReleaseDate"].ToString()!;
                    if (rawDate.Length >= 8)
                    {
                        var y = rawDate.Substring(0, 4);
                        var m = rawDate.Substring(4, 2);
                        var d = rawDate.Substring(6, 2);
                        dateStr = $" ({y}-{m}-{d})";
                    }
                }

                info.BiosInfo = $"{mfg} {ver}{dateStr}".Trim();
            }
        }
        catch { }

        // Fallback for BIOS from Registry
        if (string.IsNullOrWhiteSpace(info.BiosInfo))
        {
            try
            {
                using var rk = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
                if (rk != null)
                {
                    var vendor = rk.GetValue("BIOSVendor")?.ToString()?.Trim() ?? "";
                    var ver = rk.GetValue("BIOSVersion")?.ToString()?.Trim() ?? "";
                    var date = rk.GetValue("BIOSReleaseDate")?.ToString()?.Trim() ?? "";
                    info.BiosInfo = $"{vendor} {ver} ({date})".Trim();
                }
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(info.ServiceTag))
        {
            try
            {
                using var searcherBb = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject mo in searcherBb.Get())
                {
                    var s = mo["SerialNumber"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s) && s != "None" && s != "Default string")
                    {
                        info.ServiceTag = s;
                        break;
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(info.ServiceTag))
                info.ServiceTag = "N/A (Chưa rõ)";
        }
    }

    private void GetCpuInfo(DetailedHardwareInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (ManagementObject mo in searcher.Get())
            {
                info.CpuInfo = mo["Name"]?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(info.CpuInfo)) break;
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(info.CpuInfo))
        {
            try
            {
                using var rk = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                info.CpuInfo = rk?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "";
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(info.CpuInfo))
        {
            info.CpuInfo = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "x64 Processor";
        }
    }

    private void GetBatteryInfo(DetailedHardwareInfo info)
    {
        try
        {
            double designedCapacity = 0;
            double fullCapacity = 0;
            int cycleCount = 0;
            int remainingPercent = -1;

            // Query Win32_Battery for remaining %
            try
            {
                using var searcherBat = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining FROM Win32_Battery");
                foreach (ManagementObject mo in searcherBat.Get())
                {
                    if (mo["EstimatedChargeRemaining"] != null)
                    {
                        remainingPercent = Convert.ToInt32(mo["EstimatedChargeRemaining"]);
                        break;
                    }
                }
            }
            catch { }

            // Fallback for remaining percent using SystemInformation
            if (remainingPercent < 0)
            {
                var pwr = SystemInformation.PowerStatus;
                if (pwr.BatteryLifePercent >= 0)
                {
                    remainingPercent = (int)Math.Round(pwr.BatteryLifePercent * 100);
                }
            }

            // Query BatteryFullChargedCapacity (root/wmi)
            try
            {
                using var searcherFull = new ManagementObjectSearcher(@"root\wmi", "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity");
                foreach (ManagementObject mo in searcherFull.Get())
                {
                    if (mo["FullChargedCapacity"] != null)
                    {
                        fullCapacity = Convert.ToDouble(mo["FullChargedCapacity"]);
                        break;
                    }
                }
            }
            catch { }

            // Query BatteryStaticData (root/wmi)
            try
            {
                using var searcherStatic = new ManagementObjectSearcher(@"root\wmi", "SELECT DesignedCapacity FROM BatteryStaticData");
                foreach (ManagementObject mo in searcherStatic.Get())
                {
                    if (mo["DesignedCapacity"] != null)
                    {
                        designedCapacity = Convert.ToDouble(mo["DesignedCapacity"]);
                        break;
                    }
                }
            }
            catch { }

            // Query BatteryCycleCount (root/wmi)
            try
            {
                using var searcherCycle = new ManagementObjectSearcher(@"root\wmi", "SELECT CycleCount FROM BatteryCycleCount");
                foreach (ManagementObject mo in searcherCycle.Get())
                {
                    if (mo["CycleCount"] != null)
                    {
                        cycleCount = Convert.ToInt32(mo["CycleCount"]);
                        break;
                    }
                }
            }
            catch { }

            // Detect power line status
            var powerLine = SystemInformation.PowerStatus.PowerLineStatus;
            string chargeStatus = powerLine == PowerLineStatus.Online ? "Đang cắm sạc (AC)" : "Đang dùng pin";

            // If no battery capacity and no percent, likely a desktop PC
            if (remainingPercent < 0 && fullCapacity == 0 && designedCapacity == 0)
            {
                info.BatteryInfo = "Không phát hiện pin laptop (PC để bàn hoặc WinPE)";
                return;
            }

            string cycleText = cycleCount > 0 ? $" | {cycleCount} chu kỳ sạc (Cycles)" : "";

            if (designedCapacity > 0 && fullCapacity > 0)
            {
                double health = (fullCapacity / designedCapacity) * 100.0;
                double fullWh = fullCapacity / 1000.0;
                double designedWh = designedCapacity / 1000.0;

                string condition = health >= 80 ? "tốt" : (health >= 50 ? "khá" : "chai nhiều");
                info.BatteryInfo = $"Sức khỏe {Math.Round(health)}% ({fullWh:N1}/{designedWh:N1} Wh) - {condition}{cycleText} - {chargeStatus} {Math.Max(0, remainingPercent)}%";
            }
            else if (fullCapacity > 0)
            {
                double fullWh = fullCapacity / 1000.0;
                info.BatteryInfo = $"Dung lượng: {fullWh:N1} Wh{cycleText} - {chargeStatus} {Math.Max(0, remainingPercent)}%";
            }
            else
            {
                info.BatteryInfo = $"{chargeStatus} - {Math.Max(0, remainingPercent)}%{cycleText}";
            }
        }
        catch
        {
            info.BatteryInfo = "Không phát hiện pin laptop (PC để bàn hoặc WinPE)";
        }
    }

    private void GetWindowsAndLicense(DetailedHardwareInfo info)
    {
        try
        {
            // OS info
            using var searcherOs = new ManagementObjectSearcher("SELECT Caption, Version, OSArchitecture, SerialNumber FROM Win32_OperatingSystem");
            foreach (ManagementObject mo in searcherOs.Get())
            {
                var caption = mo["Caption"]?.ToString()?.Replace("Microsoft ", "") ?? "Windows";
                var arch = mo["OSArchitecture"]?.ToString() ?? "64-bit";
                var ver = mo["Version"]?.ToString() ?? "";
                info.OsVersion = ver;
                info.ProductId = mo["SerialNumber"]?.ToString() ?? "";

                // Get DisplayVersion (e.g. 24H2, 23H2) from registry
                string displayVer = "";
                try
                {
                    using var rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    displayVer = rk?.GetValue("DisplayVersion")?.ToString() ?? "";
                }
                catch { }

                if (!string.IsNullOrEmpty(displayVer))
                {
                    info.OsName = $"{caption} {displayVer} - {arch}";
                }
                else
                {
                    info.OsName = $"{caption} - {arch}";
                }
            }
        }
        catch { }

        // Fallback for OS from Registry
        if (string.IsNullOrWhiteSpace(info.OsName))
        {
            try
            {
                using var rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (rk != null)
                {
                    var prod = rk.GetValue("ProductName")?.ToString() ?? "Windows";
                    var disp = rk.GetValue("DisplayVersion")?.ToString() ?? "";
                    info.OsName = string.IsNullOrEmpty(disp) ? prod : $"{prod} {disp}";
                    info.OsVersion = Environment.OSVersion.Version.ToString();
                    info.ProductId = rk.GetValue("ProductId")?.ToString() ?? "";
                }
            }
            catch { }
        }

        // License & Activation
        info.ActivationStatus = "Chưa kích hoạt hoặc Giấy phép tạm thời";
        try
        {
            using var searcherLic = new ManagementObjectSearcher("SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE PartialProductKey is not null");
            foreach (ManagementObject mo in searcherLic.Get())
            {
                int st = Convert.ToInt32(mo["LicenseStatus"] ?? 0);
                if (st == 1)
                {
                    info.ActivationStatus = "Đã kích hoạt (Licensed)";
                    break;
                }
                else if (st == 2)
                {
                    info.ActivationStatus = "Thời gian dùng thử (Grace Period)";
                }
                else if (st == 3)
                {
                    info.ActivationStatus = "Hết hạn dùng thử (OOB Grace)";
                }
                else if (st == 4)
                {
                    info.ActivationStatus = "Chưa kích hoạt (Non-Genuine)";
                }
                else if (st == 5)
                {
                    info.ActivationStatus = "Chưa kích hoạt (Notification)";
                }
            }
        }
        catch { }

        // Installed Key (extracted via SystemService directly)
        var sysService = new SystemService();
        info.InstalledKey = sysService.ExtractWindowsKey();

        // OEM Key & Edition from MSDM / SoftwareLicensingService
        try
        {
            using var searcherOa = new ManagementObjectSearcher("SELECT OA3xOriginalProductKey, OA3xOriginalProductKeyDescription FROM SoftwareLicensingService");
            foreach (ManagementObject mo in searcherOa.Get())
            {
                info.OemKey = mo["OA3xOriginalProductKey"]?.ToString()?.Trim() ?? "";
                info.OemEdition = mo["OA3xOriginalProductKeyDescription"]?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(info.OemKey)) break;
            }
        }
        catch { }

        if (string.IsNullOrEmpty(info.OemKey))
        {
            info.OemKey = "Không có OEM Key trong BIOS (Máy tự ráp hoặc No-OS)";
            info.OemEdition = "Không có";
        }
    }

    private void GetGpuInfo(DetailedHardwareInfo info)
    {
        try
        {
            using var searcherGpu = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
            int idx = 1;
            foreach (ManagementObject mo in searcherGpu.Get())
            {
                var name = mo["Name"]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(name)) continue;

                // Check 64-bit VRAM from registry if available
                long vramBytes = 0;
                try
                {
                    using var baseKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                    if (baseKey != null)
                    {
                        foreach (var sub in baseKey.GetSubKeyNames())
                        {
                            if (sub == "Properties") continue;
                            using var sk = baseKey.OpenSubKey(sub);
                            var desc = sk?.GetValue("DriverDesc")?.ToString();
                            if (!string.IsNullOrEmpty(desc) &&
                                (desc.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                 desc.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                                 name.Contains(desc, StringComparison.OrdinalIgnoreCase)))
                            {
                                var qw = sk?.GetValue("HardwareInformation.qwMemorySize");
                                long parsedQw = ParseMemoryBytes(qw);
                                if (parsedQw > 0)
                                {
                                    vramBytes = parsedQw;
                                    break;
                                }

                                var dw = sk?.GetValue("HardwareInformation.MemorySize");
                                long parsedDw = ParseMemoryBytes(dw);
                                if (parsedDw > 0 && vramBytes == 0)
                                {
                                    vramBytes = parsedDw;
                                }
                            }
                        }
                    }
                }
                catch { }

                if ((vramBytes <= 4L * 1024 * 1024 * 1024) && name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "nvidia-smi",
                            Arguments = "--query-gpu=memory.total --format=csv,noheader,nounits",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            string output = proc.StandardOutput.ReadToEnd().Trim();
                            if (proc.WaitForExit(1000))
                            {
                                var firstLine = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                                if (!string.IsNullOrEmpty(firstLine) && long.TryParse(firstLine.Trim(), out long mb) && mb > 0)
                                {
                                    vramBytes = mb * 1024L * 1024L;
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (vramBytes == 0 && mo["AdapterRAM"] != null)
                {
                    try { vramBytes = Convert.ToInt64(mo["AdapterRAM"]); } catch { }
                }

                double vramGb = Math.Round((double)vramBytes / (1024 * 1024 * 1024));
                string vramText;

                if (vramGb > 0)
                {
                    vramText = $"VRAM {vramGb} GB";
                }
                else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Radeon(TM) Graphics", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Vega", StringComparison.OrdinalIgnoreCase))
                {
                    vramText = "VRAM Onboard (Dùng chung RAM hệ thống)";
                }
                else
                {
                    vramText = "VRAM Tự động chia sẻ";
                }

                info.Gpus.Add($"GPU {idx}: {name} - {vramText}");
                idx++;
            }
        }
        catch { }

        // Fallback: Check Registry if WMI found 0 GPUs
        if (info.Gpus.Count == 0)
        {
            try
            {
                using var baseKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                if (baseKey != null)
                {
                    int idx = 1;
                    foreach (var sub in baseKey.GetSubKeyNames())
                    {
                        using var sk = baseKey.OpenSubKey(sub);
                        var desc = sk?.GetValue("DriverDesc")?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(desc))
                        {
                            long vramBytes = 0;
                            var qw = sk?.GetValue("HardwareInformation.qwMemorySize");
                            if (qw != null) vramBytes = Convert.ToInt64(qw);
                            double vramGb = Math.Round((double)vramBytes / (1024 * 1024 * 1024));
                            string vramText = vramGb > 0 ? $"VRAM {vramGb} GB" : "VRAM Onboard/Chia sẻ";
                            info.Gpus.Add($"GPU {idx}: {desc} - {vramText}");
                            idx++;
                        }
                    }
                }
            }
            catch { }
        }

        if (info.Gpus.Count == 0)
        {
            info.Gpus.Add("GPU: Microsoft Basic Display Adapter");
        }
    }

    private void GetRamInfo(DetailedHardwareInfo info)
    {
        try
        {
            using var searcherRam = new ManagementObjectSearcher("SELECT DeviceLocator, BankLabel, Capacity, Speed, SMBIOSMemoryType, ConfiguredClockSpeed FROM Win32_PhysicalMemory");
            int slotIdx = 1;
            foreach (ManagementObject mo in searcherRam.Get())
            {
                var locator = mo["DeviceLocator"]?.ToString()?.Trim() ?? $"Slot {slotIdx}";
                long capBytes = mo["Capacity"] != null ? Convert.ToInt64(mo["Capacity"]) : 0;
                int gb = (int)(capBytes / (1024 * 1024 * 1024));

                int speed = mo["Speed"] != null ? Convert.ToInt32(mo["Speed"]) : 0;
                if (speed == 0 && mo["ConfiguredClockSpeed"] != null)
                {
                    speed = Convert.ToInt32(mo["ConfiguredClockSpeed"]);
                }

                int typeNum = mo["SMBIOSMemoryType"] != null ? Convert.ToInt32(mo["SMBIOSMemoryType"]) : 0;
                string ddrType = typeNum switch
                {
                    20 => "DDR",
                    21 => "DDR2",
                    24 => "DDR3",
                    26 => "DDR4",
                    30 => "LPDDR4",
                    34 => "DDR5",
                    35 => "LPDDR5",
                    _ => speed > 4000 ? "DDR5" : "DDR4"
                };

                string speedStr = speed > 0 ? $" - {speed} MHz" : "";
                info.RamSlots.Add($"Khe {slotIdx}: {locator} - {gb} GB - {ddrType}{speedStr}");
                slotIdx++;
            }
        }
        catch { }

        // Fallback for RAM if WMI failed or 0 slots
        if (info.RamSlots.Count == 0)
        {
            try
            {
                var totalBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                int totalGb = (int)Math.Round((double)totalBytes / (1024 * 1024 * 1024));
                info.RamSlots.Add($"RAM hệ thống: {totalGb} GB khả dụng");
            }
            catch
            {
                info.RamSlots.Add("RAM hệ thống: Đã nhận diện");
            }
        }
    }

    private void GetDiskInfo(DetailedHardwareInfo info)
    {
        int nvmeCount = 0;
        int sataCount = 0;
        long totalCapacityBytes = 0;

        // Try MSFT_PhysicalDisk first for accurate health and bus type
        try
        {
            using var searcherMsft = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT FriendlyName, MediaType, BusType, HealthStatus, Size FROM MSFT_PhysicalDisk");
            int diskIdx = 0;
            foreach (ManagementObject mo in searcherMsft.Get())
            {
                var name = mo["FriendlyName"]?.ToString()?.Trim() ?? $"Ổ đĩa {diskIdx}";
                long sizeBytes = mo["Size"] != null ? Convert.ToInt64(mo["Size"]) : 0;
                int sizeGb = (int)(sizeBytes / (1024 * 1024 * 1024));
                totalCapacityBytes += sizeBytes;

                int busType = mo["BusType"] != null ? Convert.ToInt32(mo["BusType"]) : 0;
                int mediaType = mo["MediaType"] != null ? Convert.ToInt32(mo["MediaType"]) : 0;
                int health = mo["HealthStatus"] != null ? Convert.ToInt32(mo["HealthStatus"]) : 0;

                string busText = busType switch
                {
                    17 => "NVMe (PCIe M.2)",
                    11 => "SATA",
                    7 => "USB",
                    8 => "RAID",
                    _ => name.Contains("NVMe", StringComparison.OrdinalIgnoreCase) ? "NVMe (PCIe M.2)" : "SATA"
                };

                if (busType == 17 || name.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                    nvmeCount++;
                else if (busType == 11)
                    sataCount++;

                string mediaText = mediaType switch
                {
                    4 => "SSD",
                    3 => "HDD",
                    5 => "SCM",
                    _ => name.Contains("SSD", StringComparison.OrdinalIgnoreCase) ? "SSD" : "Ổ đĩa"
                };

                string healthText = health switch
                {
                    0 => "Tốt (Healthy)",
                    1 => "Cảnh báo (Warning)",
                    2 => "Nguy hiểm (Unhealthy)",
                    _ => "Bình thường (OK)"
                };

                info.Disks.Add($"Disk {diskIdx}: {name} - {sizeGb} GB - {mediaText} {busText} - Sức khỏe: {healthText}");
                diskIdx++;
            }
        }
        catch { }

        // Fallback to Win32_DiskDrive if MSFT_PhysicalDisk returned 0
        if (info.Disks.Count == 0)
        {
            try
            {
                using var searcherDisk = new ManagementObjectSearcher("SELECT Model, Size, InterfaceType, MediaType FROM Win32_DiskDrive");
                int diskIdx = 0;
                foreach (ManagementObject mo in searcherDisk.Get())
                {
                    var model = mo["Model"]?.ToString()?.Trim() ?? $"Ổ đĩa {diskIdx}";
                    long sizeBytes = mo["Size"] != null ? Convert.ToInt64(mo["Size"]) : 0;
                    int sizeGb = (int)(sizeBytes / (1024 * 1024 * 1024));
                    totalCapacityBytes += sizeBytes;

                    bool isNvme = model.Contains("NVMe", StringComparison.OrdinalIgnoreCase);
                    string type = isNvme ? "SSD NVMe (PCIe M.2)" : "SATA SSD/HDD";
                    if (isNvme) nvmeCount++; else sataCount++;

                    info.Disks.Add($"Disk {diskIdx}: {model} - {sizeGb} GB - {type} - Sức khỏe: Tốt (Healthy)");
                    diskIdx++;
                }
            }
            catch { }
        }

        // Fallback to DriveInfo.GetDrives if still 0 (e.g. WinPE / VM without WMI)
        if (info.Disks.Count == 0)
        {
            try
            {
                int diskIdx = 0;
                foreach (var d in DriveInfo.GetDrives())
                {
                    if (d.DriveType != DriveType.Fixed) continue;
                    long sizeGb = d.TotalSize / (1024 * 1024 * 1024);
                    totalCapacityBytes += d.TotalSize;
                    info.Disks.Add($"Ổ {d.Name.TrimEnd('\\')}: {sizeGb} GB (Trống {d.AvailableFreeSpace / (1024 * 1024 * 1024)} GB) - {d.DriveFormat}");
                    diskIdx++;
                }
            }
            catch { }
        }

        int totalGbTotal = (int)(totalCapacityBytes / (1024 * 1024 * 1024));
        if (nvmeCount > 0 || sataCount > 0)
        {
            var parts = new List<string>();
            if (nvmeCount > 0) parts.Add($"{nvmeCount} ổ NVMe (M.2)");
            if (sataCount > 0) parts.Add($"{sataCount} ổ SATA");
            info.UpgradeInfo = $"Đang gắn: {string.Join(" + ", parts)} | Tổng dung lượng: ~{totalGbTotal} GB";
        }
        else
        {
            info.UpgradeInfo = $"Tổng dung lượng ổ đĩa khả dụng: ~{totalGbTotal} GB";
        }
    }

    private static long ParseMemoryBytes(object? val)
    {
        if (val == null) return 0;
        if (val is long l) return l;
        if (val is ulong ul) return (long)ul;
        if (val is int i) return (uint)i;
        if (val is uint u) return (long)u;
        if (val is byte[] bytes)
        {
            if (bytes.Length >= 8) return BitConverter.ToInt64(bytes, 0);
            if (bytes.Length >= 4) return BitConverter.ToUInt32(bytes, 0);
        }
        try { return Convert.ToInt64(val); } catch { return 0; }
    }
}

