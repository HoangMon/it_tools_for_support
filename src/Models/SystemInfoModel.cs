namespace Ernastie.Models;

public class SystemInfoModel
{
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string OsArchitecture { get; set; } = string.Empty;
    public string ProcessorName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public string TotalMemoryMb { get; set; } = string.Empty;
    public bool IsWinPE { get; set; }
    public string BootMode { get; set; } = "GPT - UEFI";
    public string BitLockerStatus { get; set; } = "Bình thường (Chưa mã hóa)";
    public bool HasBitLockerLocked { get; set; }
    public bool HasPfxFiles { get; set; }
    public string PfxStatusText { get; set; } = "Không phát hiện file .pfx";
    public bool NeedsIrst { get; set; }
    public string IrstStatusText { get; set; } = "Đủ driver lưu trữ";
    public string RunningPath { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string WindowsProductKey { get; set; } = string.Empty;
}

public class DiskPartitionModel
{
    public string Name { get; set; } = string.Empty;
    public string VolumeLabel { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public string DriveFormat { get; set; } = string.Empty;
    public long TotalSizeGb { get; set; }
    public long FreeSpaceGb { get; set; }
    public double FreePercentage { get; set; }
    public bool IsReady { get; set; }
    public bool IsBitLockerEncrypted { get; set; }
}

public class DeviceConfigModel
{
    public string AppsScriptUrl { get; set; } = string.Empty;
    public string GoogleSheetUrl { get; set; } = string.Empty;
    public List<string> MachineNames { get; set; } = new();
    public List<string> Companies { get; set; } = new();
}
