namespace Ernastie.Models;

public class CleanerOptionsModel
{
    public string TargetUser { get; set; } = Environment.UserName;

    // Cột 1 (Bên trái)
    public bool UserTemp { get; set; } = true;
    public bool WindowsTemp { get; set; } = true;
    public bool WindowsUpdateCache { get; set; } = true;
    public bool Prefetch { get; set; } = false;
    public bool ExplorerCache { get; set; } = true;
    public bool EdgeCache { get; set; } = false;
    public bool ChromeCache { get; set; } = false;
    public bool FirefoxCache { get; set; } = false;
    public bool EdgeHistory { get; set; } = false;
    public bool FirefoxHistory { get; set; } = false;
    public bool ChromeLogout { get; set; } = false;
    public bool EdgeLogout { get; set; } = false;
    public bool FirefoxLogout { get; set; } = false;
    public bool ResetZalo { get; set; } = false;
    public bool ClearCredentials { get; set; } = false;

    // Cột 2 (Bên phải)
    public bool INetCache { get; set; } = true;
    public bool WebCache { get; set; } = false;
    public bool CrashDumps { get; set; } = true;
    public bool Wer { get; set; } = true;
    public bool DirectXCache { get; set; } = true;
    public bool RecycleBin { get; set; } = true;
    public bool QuickAccess { get; set; } = false;
    public bool ChromeHistory { get; set; } = false;
    public bool ResetTeams { get; set; } = false;

    // Thư mục tùy chọn khác
    public List<string> CustomFolders { get; set; } = new();
}

public class CleanResultModel
{
    public long BytesFreed { get; set; }
    public int FilesDeleted { get; set; }
    public int ItemsProcessed { get; set; }
    public double MegabytesFreed => Math.Round((double)BytesFreed / (1024 * 1024), 2);
}
