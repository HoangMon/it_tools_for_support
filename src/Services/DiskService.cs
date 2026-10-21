using System.Diagnostics;
using Ernastie.Models;

namespace Ernastie.Services;

public class DiskService
{
    public List<DiskPartitionModel> GetDisks()
    {
        var list = new List<DiskPartitionModel>();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                var item = new DiskPartitionModel
                {
                    Name = d.Name,
                    DriveType = d.DriveType.ToString(),
                    IsReady = d.IsReady
                };

                if (d.IsReady)
                {
                    item.VolumeLabel = string.IsNullOrEmpty(d.VolumeLabel) ? "(Local Disk)" : d.VolumeLabel;
                    item.DriveFormat = d.DriveFormat;
                    item.TotalSizeGb = d.TotalSize / (1024 * 1024 * 1024);
                    item.FreeSpaceGb = d.TotalFreeSpace / (1024 * 1024 * 1024);
                    if (d.TotalSize > 0)
                    {
                        item.FreePercentage = Math.Round((double)d.TotalFreeSpace / d.TotalSize * 100, 1);
                    }
                }
                list.Add(item);
            }
        }
        catch { }

        return list;
    }

    public void OpenDiskManagement()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "diskmgmt.msc",
                UseShellExecute = true
            });
        }
        catch { }
    }

    public void OpenDiskpart()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c start diskpart",
                UseShellExecute = true
            });
        }
        catch { }
    }

    public void FixPartitionLimits()
    {
        try
        {
            // Disable hibernation and paging file temporarily to unblock unshrinkable partitions
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c powercfg -h off && fsutil usn deletejournal /d C: && echo Da tat Hibernate va Usn Journal de co gian o dia! && pause",
                UseShellExecute = true
            });
        }
        catch { }
    }

    public void CleanWinPeBoot()
    {
        try
        {
            var res = System.Windows.Forms.MessageBox.Show(
                "Bạn có muốn tự động quét và dọn sạch các mục Bootloader WinPE cứu hộ khỏi hệ thống không?",
                "Dọn Boot WinPE",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Question);

            if (res == System.Windows.Forms.DialogResult.Yes)
            {
                var peService = new WinPeService();
                var result = peService.CleanWinPeBcdEntries();
                System.Windows.Forms.MessageBox.Show(
                    result.Message,
                    "Dọn Boot WinPE",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    result.Success ? System.Windows.Forms.MessageBoxIcon.Information : System.Windows.Forms.MessageBoxIcon.Warning);
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c bcdedit /enum all && echo Kiem tra cac muc Bootloader && pause",
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }
}
