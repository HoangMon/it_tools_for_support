using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Ernastie.Forms;

public class DataBackupForm : Form
{
    // Dynamic Design System Tokens from ThemeHelper
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _cardBg => ThemeHelper.CardBg;
    private Color _borderColor => ThemeHelper.BorderColor;
    private Color _textHeading => ThemeHelper.TextHeading;
    private Color _textMuted => ThemeHelper.TextMuted;
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnHover => ThemeHelper.BtnHover;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;
    private Color _primaryHover => ThemeHelper.PrimaryHover;

    private string _selectedDestFolder = string.Empty;
    private CancellationTokenSource? _cts;
    private bool _isWorking = false;

    // UI Controls
    private Label _lblDestPath = null!;
    private Button _btnBrowse = null!;
    private CheckBox _chkDesktop = null!;
    private CheckBox _chkDocuments = null!;
    private CheckBox _chkDownloads = null!;
    private CheckBox _chkPictures = null!;
    private CheckBox _chkFavorites = null!;
    private CheckBox _chkVideos = null!;

    private ProgressBar _progressBar = null!;
    private Label _lblStatus = null!;
    private Button _btnStart = null!;
    private Button _btnRestore = null!;
    private Button _btnStop = null!;
    private Button _btnFonts = null!;

    public DataBackupForm()
    {
        InitializeComponents();
        CheckDefaultDestination();
    }

    private void CheckDefaultDestination()
    {
        // Check if D: or external drive is available as a smart default suggestion
        if (Directory.Exists(@"D:\"))
        {
            SetDestinationFolder(@"D:\Backup_DuLieu");
        }
        else if (Directory.Exists(@"E:\"))
        {
            SetDestinationFolder(@"E:\Backup_DuLieu");
        }
    }

    private void SetDestinationFolder(string path)
    {
        _selectedDestFolder = path;
        _lblDestPath.Text = path;
        _lblDestPath.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _lblDestPath.ForeColor = Color.FromArgb(30, 64, 175); // Dark blue accent
    }

    private void InitializeComponents()
    {
        Text = "🛠️ Sao lưu Dữ liệu";
        Size = new Size(580, 480);
        MinimumSize = new Size(540, 460);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9.25f);
        Icon = SystemIcons.Application;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        DoubleBuffered = true;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 14),
            BackColor = _bgForm
        };

        // 1. Group 1: Chọn nơi lưu dữ liệu
        var gbDest = BuildDestinationGroup();
        gbDest.Dock = DockStyle.Top;
        gbDest.Height = 84;
        gbDest.Margin = new Padding(0, 0, 0, 12);
        mainPanel.Controls.Add(gbDest);

        // Spacer
        var spacer1 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
        mainPanel.Controls.Add(spacer1);

        // 2. Group 2: Chọn thư mục cần sao lưu
        var gbFolders = BuildFoldersGroup();
        gbFolders.Dock = DockStyle.Top;
        gbFolders.Height = 150;
        gbFolders.Margin = new Padding(0, 0, 0, 12);
        mainPanel.Controls.Add(gbFolders);

        // Spacer
        var spacer2 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
        mainPanel.Controls.Add(spacer2);

        // 3. Progress area
        var pnlProgress = BuildProgressArea();
        pnlProgress.Dock = DockStyle.Top;
        pnlProgress.Height = 44;
        mainPanel.Controls.Add(pnlProgress);

        // 4. Action Buttons Area
        var pnlActions = BuildActionsArea();
        pnlActions.Dock = DockStyle.Fill;
        mainPanel.Controls.Add(pnlActions);

        Controls.Add(mainPanel);

        // Proper visual z-order
        gbDest.BringToFront();
        spacer1.BringToFront();
        gbFolders.BringToFront();
        spacer2.BringToFront();
        pnlProgress.BringToFront();
        pnlActions.BringToFront();
        ThemeHelper.ApplyTheme(this);
    }

    private GroupBox BuildDestinationGroup()
    {
        var gb = new GroupBox
        {
            Text = "📁 Chọn nơi lưu dữ liệu",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            BackColor = Color.Transparent,
            Padding = new Padding(14, 8, 14, 10)
        };

        _btnBrowse = new Button
        {
            Text = "📁 Chọn...",
            Location = new Point(16, 28),
            Size = new Size(100, 36),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnBrowse.FlatAppearance.BorderColor = _btnBorder;
        _btnBrowse.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnBrowse.Click += (_, _) => ChooseDestinationFolder();

        _lblDestPath = new Label
        {
            Text = "Chưa chọn thư mục đích",
            Location = new Point(126, 28),
            Size = new Size(400, 36),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
            ForeColor = _textMuted
        };

        gb.Controls.Add(_btnBrowse);
        gb.Controls.Add(_lblDestPath);

        return gb;
    }

    private GroupBox BuildFoldersGroup()
    {
        var gb = new GroupBox
        {
            Text = "📦 Chọn thư mục cần sao lưu",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            BackColor = Color.Transparent,
            Padding = new Padding(18, 12, 18, 12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _cardBg,
            Padding = new Padding(10, 8, 10, 8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        var fontCheck = new Font("Segoe UI", 10f);

        // Column 1
        _chkDesktop = new CheckBox
        {
            Text = "🖥️  Desktop",
            Font = fontCheck,
            ForeColor = _textHeading,
            Checked = true,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        _chkDocuments = new CheckBox
        {
            Text = "📄  Documents",
            Font = fontCheck,
            ForeColor = _textHeading,
            Checked = true,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        _chkDownloads = new CheckBox
        {
            Text = "📥  Downloads",
            Font = fontCheck,
            ForeColor = _textHeading,
            Checked = true,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };

        // Column 2
        _chkPictures = new CheckBox
        {
            Text = "🖼️  Pictures",
            Font = fontCheck,
            ForeColor = _textHeading,
            Checked = true,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        _chkFavorites = new CheckBox
        {
            Text = "⭐  Favorites",
            Font = fontCheck,
            ForeColor = _textHeading,
            Checked = true,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        _chkVideos = new CheckBox
        {
            Text = "🎬  Videos",
            Font = fontCheck,
            ForeColor = _textHeading,
            Checked = false,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };

        layout.Controls.Add(_chkDesktop, 0, 0);
        layout.Controls.Add(_chkDocuments, 0, 1);
        layout.Controls.Add(_chkDownloads, 0, 2);

        layout.Controls.Add(_chkPictures, 1, 0);
        layout.Controls.Add(_chkFavorites, 1, 1);
        layout.Controls.Add(_chkVideos, 1, 2);

        gb.Controls.Add(layout);
        return gb;
    }

    private Panel BuildProgressArea()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 4, 0)
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 14,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Text = "Sẵn sàng sao lưu dữ liệu.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _textMuted,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pnl.Controls.Add(_progressBar);
        pnl.Controls.Add(_lblStatus);

        return pnl;
    }

    private Panel BuildActionsArea()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        // Row 1: Bắt đầu sao lưu | Khôi phục | Dừng
        var pnlRow1 = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        _btnStart = new Button
        {
            Text = "▶ Bắt đầu sao lưu",
            Size = new Size(155, 38),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(35, 0, 10, 0)
        };
        _btnStart.FlatAppearance.BorderColor = _btnBorder;
        _btnStart.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnStart.Click += async (_, _) => await StartBackupAsync();

        _btnRestore = new Button
        {
            Text = "↶ Khôi phục",
            Size = new Size(130, 38),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        _btnRestore.FlatAppearance.BorderColor = _btnBorder;
        _btnRestore.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnRestore.Click += async (_, _) => await RestoreBackupAsync();

        _btnStop = new Button
        {
            Text = "⏹ Dừng",
            Size = new Size(110, 38),
            BackColor = _btnBg,
            ForeColor = _textMuted,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Enabled = false,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 0)
        };
        _btnStop.FlatAppearance.BorderColor = _btnBorder;
        _btnStop.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnStop.Click += (_, _) => StopOperation();

        pnlRow1.Controls.Add(_btnStart);
        pnlRow1.Controls.Add(_btnRestore);
        pnlRow1.Controls.Add(_btnStop);

        // Row 2: Sao lưu / Khôi phục font chữ
        var pnlRow2 = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };

        _btnFonts = new Button
        {
            Text = "✍️ Sao lưu / Khôi phục font chữ",
            Size = new Size(270, 38),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(125, 6, 0, 0)
        };
        _btnFonts.FlatAppearance.BorderColor = _btnBorder;
        _btnFonts.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnFonts.Click += (_, _) => ShowFontsMenu();

        pnlRow2.Controls.Add(_btnFonts);

        pnl.Controls.Add(pnlRow2);
        pnl.Controls.Add(pnlRow1);

        return pnl;
    }

    private void ChooseDestinationFolder()
    {
        using var fbd = new FolderBrowserDialog
        {
            Description = "Chọn thư mục đích để lưu trữ bản sao lưu dữ liệu cá nhân",
            SelectedPath = Directory.Exists(_selectedDestFolder) ? _selectedDestFolder : @"C:\"
        };

        if (fbd.ShowDialog(this) == DialogResult.OK)
        {
            SetDestinationFolder(fbd.SelectedPath);
        }
    }

    private List<(string Name, string SourcePath)> GetSelectedFolders()
    {
        var list = new List<(string Name, string SourcePath)>();

        if (_chkDesktop.Checked)
            list.Add(("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));

        if (_chkDocuments.Checked)
            list.Add(("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));

        if (_chkDownloads.Checked)
            list.Add(("Downloads", GetDownloadsPath()));

        if (_chkPictures.Checked)
            list.Add(("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)));

        if (_chkFavorites.Checked)
            list.Add(("Favorites", Environment.GetFolderPath(Environment.SpecialFolder.Favorites)));

        if (_chkVideos.Checked)
            list.Add(("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)));

        return list;
    }

    private static string GetDownloadsPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
            if (key != null)
            {
                var val = key.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                if (!string.IsNullOrEmpty(val))
                {
                    return Environment.ExpandEnvironmentVariables(val);
                }
            }
        }
        catch { }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    private void SetBusy(bool busy)
    {
        _isWorking = busy;
        _btnStart.Enabled = !busy;
        _btnRestore.Enabled = !busy;
        _btnBrowse.Enabled = !busy;
        _btnFonts.Enabled = !busy;
        _btnStop.Enabled = busy;
        _btnStop.ForeColor = busy ? Color.FromArgb(185, 28, 28) : _textMuted; // Red text when active
    }

    private void SetStatus(string message, int progress = -1)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(message, progress));
            return;
        }

        _lblStatus.Text = message;
        if (progress >= 0 && progress <= 100)
        {
            _progressBar.Value = progress;
        }
    }

    private void StopOperation()
    {
        if (_isWorking && _cts != null)
        {
            _cts.Cancel();
            SetStatus("⚠️ Đang yêu cầu dừng tiến trình...");
        }
    }

    private async Task StartBackupAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedDestFolder))
        {
            MessageBox.Show("Vui lòng chọn thư mục đích để lưu trữ dữ liệu sao lưu!", "Chưa Chọn Thư Mục", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ChooseDestinationFolder();
            if (string.IsNullOrWhiteSpace(_selectedDestFolder)) return;
        }

        var foldersToBackup = GetSelectedFolders();
        if (foldersToBackup.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ít nhất một thư mục cần sao lưu (Desktop, Documents, Downloads...)!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Prepare destination folder: <SelectedFolder>\Backup_Data_<UserName>
        string finalTarget = _selectedDestFolder;
        if (Path.GetFileName(_selectedDestFolder).ToLowerInvariant() != "backup_dulieu" &&
            !_selectedDestFolder.Contains("Backup", StringComparison.OrdinalIgnoreCase))
        {
            finalTarget = Path.Combine(_selectedDestFolder, $"Backup_Data_{Environment.UserName}");
        }

        try
        {
            Directory.CreateDirectory(finalTarget);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tạo thư mục lưu trữ:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SetBusy(true);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        int totalCopiedFiles = 0;
        long totalBytes = 0;

        try
        {
            SetStatus("Đang phân tích danh sách tệp tin...", 5);

            await Task.Run(() =>
            {
                // Gather file list
                var allFiles = new List<(string SourceFile, string RelativePath, string DestFolder)>();
                foreach (var folder in foldersToBackup)
                {
                    if (ct.IsCancellationRequested) break;
                    if (!Directory.Exists(folder.SourcePath)) continue;

                    var destFolder = Path.Combine(finalTarget, folder.Name);
                    try
                    {
                        var files = Directory.GetFiles(folder.SourcePath, "*.*", SearchOption.AllDirectories);
                        foreach (var f in files)
                        {
                            var rel = f.Substring(folder.SourcePath.Length).TrimStart('\\', '/');
                            allFiles.Add((f, Path.Combine(folder.Name, rel), destFolder));
                        }
                    }
                    catch { }
                }

                if (ct.IsCancellationRequested) return;

                int count = allFiles.Count;
                int current = 0;

                foreach (var item in allFiles)
                {
                    if (ct.IsCancellationRequested) break;
                    current++;

                    var targetPath = Path.Combine(finalTarget, item.RelativePath);
                    try
                    {
                        var targetDir = Path.GetDirectoryName(targetPath);
                        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }

                        // Skip desktop.ini or transient thumbs
                        var fname = Path.GetFileName(item.SourceFile);
                        if (fname.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase) ||
                            fname.Equals("thumbs.db", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        File.Copy(item.SourceFile, targetPath, true);
                        totalCopiedFiles++;
                        try
                        {
                            totalBytes += new FileInfo(item.SourceFile).Length;
                        }
                        catch { }
                    }
                    catch { }

                    int pct = count > 0 ? (int)((current / (double)count) * 90) + 10 : 100;
                    if (current % 5 == 0 || current == count)
                    {
                        SetStatus($"Đang sao lưu: {item.RelativePath} ({current}/{count})", pct);
                    }
                }
            }, ct);

            if (ct.IsCancellationRequested)
            {
                SetStatus($"⚠️ Đã dừng sao lưu theo yêu cầu. Đã lưu {totalCopiedFiles} tệp.", 0);
                MessageBox.Show($"Quá trình sao lưu đã được dừng lại theo yêu cầu của bạn!\nĐã sao lưu được {totalCopiedFiles} tệp tin.", "Đã Dừng", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                double totalMb = totalBytes / (1024.0 * 1024.0);
                SetStatus($"✅ Đã sao lưu hoàn tất! Tổng cộng {totalCopiedFiles} tệp ({totalMb:F1} MB).", 100);

                var res = MessageBox.Show(
                    $"Đã sao lưu dữ liệu cá nhân thành công!\n\n" +
                    $"📁 Thư mục lưu trữ: {finalTarget}\n" +
                    $"📄 Tổng số tệp tin: {totalCopiedFiles} tệp ({totalMb:F1} MB)\n\n" +
                    $"Bạn có muốn mở thư mục sao lưu ngay bây giờ không?",
                    "Sao Lưu Hoàn Tất",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (res == DialogResult.Yes)
                {
                    try { Process.Start("explorer.exe", finalTarget); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Lỗi: {ex.Message}", 0);
            MessageBox.Show($"Xảy ra lỗi trong quá trình sao lưu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task RestoreBackupAsync()
    {
        // Ask for backup source folder if not set or let user select
        string backupSource = _selectedDestFolder;

        using var fbd = new FolderBrowserDialog
        {
            Description = "Chọn thư mục chứa bản sao lưu cần khôi phục (thư mục có chứa Desktop, Documents...)",
            SelectedPath = Directory.Exists(backupSource) ? backupSource : @"C:\"
        };

        if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            return;
        }

        backupSource = fbd.SelectedPath;

        // Verify if any backup folder exists
        var foldersToRestore = GetSelectedFolders();
        var foundFolders = new List<(string Name, string BackupDir, string SystemDest)>();

        foreach (var folder in foldersToRestore)
        {
            var bDir = Path.Combine(backupSource, folder.Name);
            if (Directory.Exists(bDir))
            {
                foundFolders.Add((folder.Name, bDir, folder.SourcePath));
            }
        }

        if (foundFolders.Count == 0)
        {
            // Maybe it's wrapped in a subfolder like Backup_Data_Username?
            var subDirs = Directory.GetDirectories(backupSource, "Backup_*", SearchOption.TopDirectoryOnly);
            if (subDirs.Length > 0)
            {
                foreach (var folder in foldersToRestore)
                {
                    var bDir = Path.Combine(subDirs[0], folder.Name);
                    if (Directory.Exists(bDir))
                    {
                        foundFolders.Add((folder.Name, bDir, folder.SourcePath));
                    }
                }
            }
        }

        if (foundFolders.Count == 0)
        {
            MessageBox.Show(
                $"Không tìm thấy dữ liệu sao lưu (Desktop, Documents, Downloads...) trong thư mục:\n{backupSource}\n\nVui lòng kiểm tra lại thư mục sao lưu bạn đã chọn!",
                "Không Tìm Thấy Dữ Liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn khôi phục {foundFolders.Count} thư mục ({string.Join(", ", foundFolders.Select(f => f.Name))}) vào máy tính hiện tại không?\n\n(Các tệp tin trùng tên sẽ được ghi đè bằng tệp sao lưu)",
            "Xác Nhận Khôi Phục Dữ Liệu",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        SetBusy(true);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        int totalRestored = 0;

        try
        {
            SetStatus("Đang phân tích tệp sao lưu...", 5);

            await Task.Run(() =>
            {
                var filesToRestore = new List<(string SourceFile, string DestFile)>();
                foreach (var item in foundFolders)
                {
                    if (ct.IsCancellationRequested) break;
                    try
                    {
                        var files = Directory.GetFiles(item.BackupDir, "*.*", SearchOption.AllDirectories);
                        foreach (var f in files)
                        {
                            var rel = f.Substring(item.BackupDir.Length).TrimStart('\\', '/');
                            filesToRestore.Add((f, Path.Combine(item.SystemDest, rel)));
                        }
                    }
                    catch { }
                }

                int count = filesToRestore.Count;
                int current = 0;

                foreach (var item in filesToRestore)
                {
                    if (ct.IsCancellationRequested) break;
                    current++;

                    try
                    {
                        var destDir = Path.GetDirectoryName(item.DestFile);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        File.Copy(item.SourceFile, item.DestFile, true);
                        totalRestored++;
                    }
                    catch { }

                    int pct = count > 0 ? (int)((current / (double)count) * 90) + 10 : 100;
                    if (current % 5 == 0 || current == count)
                    {
                        SetStatus($"Đang khôi phục: {Path.GetFileName(item.DestFile)} ({current}/{count})", pct);
                    }
                }
            }, ct);

            if (ct.IsCancellationRequested)
            {
                SetStatus($"⚠️ Đã dừng khôi phục theo yêu cầu. Đã chép {totalRestored} tệp.", 0);
            }
            else
            {
                SetStatus($"✅ Đã khôi phục thành công {totalRestored} tệp vào hệ thống!", 100);
                MessageBox.Show($"Đã khôi phục thành công {totalRestored} tệp tin vào máy tính!", "Khôi Phục Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Lỗi: {ex.Message}", 0);
            MessageBox.Show($"Lỗi trong quá trình khôi phục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void ShowFontsMenu()
    {
        var menu = new ContextMenuStrip();
        var itemBackupFonts = menu.Items.Add("📤 1. Sao lưu toàn bộ Font chữ ra thư mục đích");
        itemBackupFonts.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        itemBackupFonts.Click += async (_, _) => await BackupFontsAsync();

        var itemRestoreFonts = menu.Items.Add("📥 2. Khôi phục / Cài đặt Font chữ từ thư mục");
        itemRestoreFonts.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        itemRestoreFonts.Click += async (_, _) => await RestoreFontsAsync();

        menu.Show(_btnFonts, new Point(0, _btnFonts.Height));
    }

    private async Task BackupFontsAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedDestFolder))
        {
            MessageBox.Show("Vui lòng chọn thư mục đích để lưu trữ font chữ!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ChooseDestinationFolder();
            if (string.IsNullOrWhiteSpace(_selectedDestFolder)) return;
        }

        var fontsTarget = Path.Combine(_selectedDestFolder, "Backup_Fonts");
        try
        {
            Directory.CreateDirectory(fontsTarget);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tạo thư mục Fonts: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SetBusy(true);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        int copiedCount = 0;

        try
        {
            SetStatus("Đang quét danh sách Font chữ hệ thống...", 10);

            await Task.Run(() =>
            {
                var fontDirs = new List<string> { @"C:\Windows\Fonts" };
                var localFonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Fonts");
                if (Directory.Exists(localFonts)) fontDirs.Add(localFonts);

                var fontFiles = new List<string>();
                foreach (var dir in fontDirs)
                {
                    try
                    {
                        var files = Directory.GetFiles(dir, "*.*")
                            .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(".fon", StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase));
                        fontFiles.AddRange(files);
                    }
                    catch { }
                }

                int count = fontFiles.Count;
                int current = 0;

                foreach (var f in fontFiles)
                {
                    if (ct.IsCancellationRequested) break;
                    current++;

                    try
                    {
                        var fname = Path.GetFileName(f);
                        var target = Path.Combine(fontsTarget, fname);
                        File.Copy(f, target, true);
                        copiedCount++;
                    }
                    catch { }

                    if (current % 10 == 0 || current == count)
                    {
                        int pct = count > 0 ? (int)((current / (double)count) * 90) + 10 : 100;
                        SetStatus($"Đang sao lưu Font: {Path.GetFileName(f)} ({current}/{count})", pct);
                    }
                }
            }, ct);

            if (ct.IsCancellationRequested)
            {
                SetStatus($"⚠️ Đã dừng sao lưu Font. Đã lưu {copiedCount} font.", 0);
            }
            else
            {
                SetStatus($"✅ Đã sao lưu thành công {copiedCount} font chữ vào {fontsTarget}", 100);
                var res = MessageBox.Show(
                    $"Đã sao lưu thành công {copiedCount} font chữ vào thư mục:\n{fontsTarget}\n\nBạn có muốn mở thư mục ngay bây giờ không?",
                    "Sao Lưu Font Thành Công",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (res == DialogResult.Yes)
                {
                    try { Process.Start("explorer.exe", fontsTarget); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi sao lưu font: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task RestoreFontsAsync()
    {
        using var fbd = new FolderBrowserDialog
        {
            Description = "Chọn thư mục chứa các tệp Font chữ (.ttf, .otf) cần cài đặt vào Windows",
            SelectedPath = Directory.Exists(Path.Combine(_selectedDestFolder, "Backup_Fonts"))
                ? Path.Combine(_selectedDestFolder, "Backup_Fonts")
                : (Directory.Exists(_selectedDestFolder) ? _selectedDestFolder : @"C:\")
        };

        if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            return;
        }

        var sourceDir = fbd.SelectedPath;
        var fontFiles = Directory.GetFiles(sourceDir, "*.*")
            .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".fon", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (fontFiles.Count == 0)
        {
            MessageBox.Show($"Không tìm thấy tệp font chữ nào (.ttf, .otf, .ttc) trong thư mục:\n{sourceDir}", "Không Tìm Thấy Font", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Tìm thấy {fontFiles.Count} font chữ trong thư mục.\nBạn có muốn cài đặt toàn bộ font này vào hệ thống Windows không?",
            "Xác Nhận Cài Đặt Font",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        SetBusy(true);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        int installedCount = 0;

        try
        {
            SetStatus("Đang cài đặt font chữ vào Windows...", 10);

            await Task.Run(() =>
            {
                Type? shellType = Type.GetTypeFromProgID("Shell.Application");
                dynamic? shell = Activator.CreateInstance(shellType!);
                dynamic? fontsFolder = shell?.NameSpace(0x14); // CSIDL_FONTS = 0x14

                int count = fontFiles.Count;
                int current = 0;

                foreach (var fontFile in fontFiles)
                {
                    if (ct.IsCancellationRequested) break;
                    current++;

                    try
                    {
                        if (fontsFolder != null)
                        {
                            // 16 = FOF_NOCONFIRMATION (Respond "Yes to All" for any dialog)
                            fontsFolder.CopyHere(fontFile, 16);
                            installedCount++;
                        }
                    }
                    catch { }

                    if (current % 5 == 0 || current == count)
                    {
                        int pct = (int)((current / (double)count) * 90) + 10;
                        SetStatus($"Đang cài đặt: {Path.GetFileName(fontFile)} ({current}/{count})", pct);
                    }
                }
            }, ct);

            if (ct.IsCancellationRequested)
            {
                SetStatus($"⚠️ Đã dừng cài đặt font theo yêu cầu. Đã cài {installedCount} font.", 0);
            }
            else
            {
                SetStatus($"✅ Đã cài đặt thành công {installedCount} font chữ vào hệ thống!", 100);
                MessageBox.Show($"Đã cài đặt thành công {installedCount} font chữ vào hệ thống Windows!", "Cài Đặt Font Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cài đặt font: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }
}
