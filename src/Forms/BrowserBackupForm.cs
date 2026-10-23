using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace Ernastie.Forms;

public class BrowserBackupItem
{
    public string Name { get; set; } = string.Empty;
    public string BrowserType { get; set; } = string.Empty; // Chrome, Edge, CocCoc, Brave, Firefox
    public string ProfilePath { get; set; } = string.Empty;
    public bool IsDetected { get; set; }
    public CheckBox CheckBox { get; set; } = null!;
    public Label StatusLabel { get; set; } = null!;
}

public class BrowserBackupForm : Form
{
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _cardBg => ThemeHelper.CardBg;
    private Color _borderColor => ThemeHelper.BorderColor;
    private Color _textHeading => ThemeHelper.TextHeading;
    private Color _textMuted => ThemeHelper.TextMuted;
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnHover => ThemeHelper.BtnHover;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;
    private Color _successGreen => ThemeHelper.SuccessGreen;

    private readonly List<BrowserBackupItem> _browsers = new();
    private string _destPath = string.Empty;
    private bool _isBusy = false;

    // Controls
    private Label _lblDest = null!;
    private Button _btnBrowse = null!;
    private CheckBox _chkBookmarks = null!;
    private CheckBox _chkHistory = null!;
    private CheckBox _chkPreferences = null!;
    private Button _btnBackup = null!;
    private Button _btnRestore = null!;
    private Button _btnOpenFolder = null!;
    private ProgressBar _progressBar = null!;
    private Label _lblStatus = null!;
    private RichTextBox _rtbLog = null!;

    public BrowserBackupForm()
    {
        InitializeComponents();
        DetectBrowsers();
        InitDefaultDestination();
    }

    private void InitDefaultDestination()
    {
        string defaultRoot = Directory.Exists(@"D:\") ? @"D:\" : (Directory.Exists(@"E:\") ? @"E:\" : @"C:\");
        _destPath = Path.Combine(defaultRoot, "Backup_TrinhDuyet");
        _lblDest.Text = _destPath;
    }

    private void InitializeComponents()
    {
        Text = "Sao lưu & Khôi phục dữ liệu Trình duyệt web";
        Size = new Size(820, 680);
        MinimumSize = new Size(760, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9.25f);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = _bgForm
        };

        // 1. Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text = "SAO LƯU & KHÔI PHỤC BOOKMARK / PROFILE TRÌNH DUYỆT",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, 4),
            AutoSize = true
        };
        var lblSubtitle = new Label
        {
            Text = "Hỗ trợ Google Chrome, Microsoft Edge, Cốc Cốc, Brave Browser và Mozilla Firefox.",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = _textMuted,
            Location = new Point(2, 28),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubtitle);
        mainPanel.Controls.Add(pnlHeader);

        // 2. Destination Folder Card
        var pnlDestCard = CreateCardPanel(56);
        pnlDestCard.Dock = DockStyle.Top;
        pnlDestCard.Padding = new Padding(12, 8, 12, 8);

        var lblDestTitle = new Label
        {
            Text = "Thư mục lưu trữ bản sao lưu:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(12, 8),
            AutoSize = true
        };

        _lblDest = new Label
        {
            Text = @"D:\Backup_TrinhDuyet",
            Font = new Font("Consolas", 9f),
            ForeColor = _primaryBlue,
            Location = new Point(12, 30),
            Size = new Size(540, 20),
            AutoEllipsis = true
        };

        _btnBrowse = new Button
        {
            Text = "Đổi thư mục...",
            Size = new Size(115, 30),
            Location = new Point(pnlDestCard.Width - 245, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnBrowse.FlatAppearance.BorderColor = _btnBorder;
        _btnBrowse.Click += (_, _) => ChooseDestinationFolder();

        _btnOpenFolder = new Button
        {
            Text = "Mở thư mục",
            Size = new Size(115, 30),
            Location = new Point(pnlDestCard.Width - 125, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnOpenFolder.FlatAppearance.BorderColor = _btnBorder;
        _btnOpenFolder.Click += (_, _) =>
        {
            if (!Directory.Exists(_destPath)) Directory.CreateDirectory(_destPath);
            Process.Start(new ProcessStartInfo("explorer.exe", _destPath) { UseShellExecute = true });
        };

        pnlDestCard.Controls.Add(lblDestTitle);
        pnlDestCard.Controls.Add(_lblDest);
        pnlDestCard.Controls.Add(_btnBrowse);
        pnlDestCard.Controls.Add(_btnOpenFolder);
        mainPanel.Controls.Add(pnlDestCard);

        // 3. Browsers selection card
        var pnlBrowsersCard = CreateCardPanel(165);
        pnlBrowsersCard.Dock = DockStyle.Top;
        pnlBrowsersCard.Padding = new Padding(14, 10, 14, 10);

        var lblBrowserHeader = new Label
        {
            Text = "Danh sách trình duyệt phát hiện trên hệ thống:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Top,
            Height = 24
        };
        pnlBrowsersCard.Controls.Add(lblBrowserHeader);

        var flowBrowsers = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        AddBrowserRow(flowBrowsers, "Google Chrome", "Chrome",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data"));
        AddBrowserRow(flowBrowsers, "Microsoft Edge", "Edge",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data"));
        AddBrowserRow(flowBrowsers, "Cốc Cốc", "CocCoc",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CocCoc", "Browser", "User Data"));
        AddBrowserRow(flowBrowsers, "Brave Browser", "Brave",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data"));
        AddBrowserRow(flowBrowsers, "Mozilla Firefox", "Firefox",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles"));

        pnlBrowsersCard.Controls.Add(flowBrowsers);
        mainPanel.Controls.Add(pnlBrowsersCard);

        // 4. Options card (What to backup)
        var pnlOptions = CreateCardPanel(52);
        pnlOptions.Dock = DockStyle.Top;
        pnlOptions.Padding = new Padding(14, 10, 14, 8);

        var flowOpts = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent };

        var lblOptTitle = new Label { Text = "Mục sao lưu:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = _textHeading, AutoSize = true, Margin = new Padding(0, 4, 16, 0) };
        _chkBookmarks = new CheckBox { Text = "Dấu trang (Bookmarks / Favorites)", Checked = true, AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = _textHeading, Margin = new Padding(0, 3, 20, 0) };
        _chkHistory = new CheckBox { Text = "Lịch sử duyệt web (History)", Checked = true, AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = _textHeading, Margin = new Padding(0, 3, 20, 0) };
        _chkPreferences = new CheckBox { Text = "Cấu hình & Tiện ích (Settings)", Checked = false, AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = _textHeading, Margin = new Padding(0, 3, 10, 0) };

        flowOpts.Controls.Add(lblOptTitle);
        flowOpts.Controls.Add(_chkBookmarks);
        flowOpts.Controls.Add(_chkHistory);
        flowOpts.Controls.Add(_chkPreferences);
        pnlOptions.Controls.Add(flowOpts);
        mainPanel.Controls.Add(pnlOptions);

        // 5. Actions Row (Buttons)
        var pnlActions = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 6) };
        _btnBackup = new Button
        {
            Text = "Bắt đầu Sao lưu (Backup to ZIP)",
            Size = new Size(240, 36),
            Location = new Point(0, 6),
            BackColor = _primaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnBackup.FlatAppearance.BorderSize = 0;
        _btnBackup.Click += async (_, _) => await StartBackupAsync();

        _btnRestore = new Button
        {
            Text = "Khôi phục Trình duyệt (Restore from ZIP)",
            Size = new Size(270, 36),
            Location = new Point(250, 6),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRestore.FlatAppearance.BorderColor = _btnBorder;
        _btnRestore.Click += async (_, _) => await StartRestoreAsync();

        pnlActions.Controls.Add(_btnBackup);
        pnlActions.Controls.Add(_btnRestore);
        mainPanel.Controls.Add(pnlActions);

        // 6. Progress and Status
        var pnlProgress = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent, Padding = new Padding(0, 2, 0, 4) };
        _progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 14, Minimum = 0, Maximum = 100, Value = 0 };
        _lblStatus = new Label { Text = "Sẵn sàng.", Dock = DockStyle.Bottom, Height = 18, Font = new Font("Segoe UI", 8.5f), ForeColor = _textMuted };
        pnlProgress.Controls.Add(_lblStatus);
        pnlProgress.Controls.Add(_progressBar);
        mainPanel.Controls.Add(pnlProgress);

        // 7. Log output
        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.LogBg,
            ForeColor = ThemeHelper.LogText,
            Font = new Font("Consolas", 9f),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        mainPanel.Controls.Add(_rtbLog);

        Controls.Add(mainPanel);

        // Z-order docking
        pnlHeader.BringToFront();
        pnlDestCard.BringToFront();
        pnlBrowsersCard.BringToFront();
        pnlOptions.BringToFront();
        pnlActions.BringToFront();
        pnlProgress.BringToFront();
        _rtbLog.BringToFront();

        ThemeHelper.ApplyTheme(this);
    }

    private Panel CreateCardPanel(int height)
    {
        var pnl = new Panel
        {
            Height = height,
            BackColor = _cardBg,
            Margin = new Padding(0, 0, 0, 8)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };
        return pnl;
    }

    private void AddBrowserRow(FlowLayoutPanel parent, string name, string type, string profilePath)
    {
        var row = new Panel { Size = new Size(740, 24), Margin = new Padding(0, 2, 0, 2), BackColor = Color.Transparent };
        var chk = new CheckBox
        {
            Text = name,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            Size = new Size(180, 22),
            Location = new Point(0, 1),
            AutoSize = false
        };

        var lblStatus = new Label
        {
            Font = new Font("Consolas", 8.5f),
            Size = new Size(540, 20),
            Location = new Point(190, 3),
            AutoEllipsis = true
        };

        row.Controls.Add(chk);
        row.Controls.Add(lblStatus);
        parent.Controls.Add(row);

        _browsers.Add(new BrowserBackupItem
        {
            Name = name,
            BrowserType = type,
            ProfilePath = profilePath,
            CheckBox = chk,
            StatusLabel = lblStatus
        });
    }

    private void DetectBrowsers()
    {
        foreach (var b in _browsers)
        {
            if (Directory.Exists(b.ProfilePath))
            {
                b.IsDetected = true;
                b.CheckBox.Checked = true;
                b.StatusLabel.Text = $"[OK] Đã phát hiện profile ({b.ProfilePath})";
                b.StatusLabel.ForeColor = _successGreen;
            }
            else
            {
                b.IsDetected = false;
                b.CheckBox.Checked = false;
                b.CheckBox.Enabled = false;
                b.StatusLabel.Text = "[Chưa có] Chưa cài đặt hoặc không tìm thấy profile.";
                b.StatusLabel.ForeColor = _textMuted;
            }
        }
    }

    private void ChooseDestinationFolder()
    {
        using var fbd = new FolderBrowserDialog
        {
            Description = "Chọn thư mục lưu trữ file sao lưu trình duyệt",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_destPath) ? _destPath : @"C:\"
        };
        if (fbd.ShowDialog() == DialogResult.OK)
        {
            _destPath = fbd.SelectedPath;
            _lblDest.Text = _destPath;
            Log($"Đã đổi thư mục lưu: {_destPath}");
        }
    }

    private async Task StartBackupAsync()
    {
        var selected = _browsers.Where(b => b.CheckBox.Checked && b.IsDetected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Vui lòng tích chọn ít nhất 1 trình duyệt để sao lưu!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Directory.CreateDirectory(_destPath);
        string timeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string zipFileName = $"Browser_Backup_{timeStr}.zip";
        string zipPath = Path.Combine(_destPath, zipFileName);

        _isBusy = true;
        _btnBackup.Enabled = false;
        _btnRestore.Enabled = false;
        _progressBar.Value = 10;
        _lblStatus.Text = "Đang sao lưu...";

        Log($"════════════════════════════════════════════════════════════");
        Log($"[BẮT ĐẦU] Sao lưu TRÌNH DUYỆT VÀO: {zipFileName}");

        await Task.Run(() =>
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), $"BrowserBackup_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempFolder);

            try
            {
                int current = 0;
                foreach (var b in selected)
                {
                    current++;
                    Log($"⏳ [{current}/{selected.Count}] Đang sao lưu {b.Name}...");
                    var bTargetDir = Path.Combine(tempFolder, b.BrowserType);
                    Directory.CreateDirectory(bTargetDir);

                    if (b.BrowserType == "Firefox")
                    {
                        BackupFirefoxProfile(b.ProfilePath, bTargetDir);
                    }
                    else
                    {
                        BackupChromiumProfile(b.ProfilePath, bTargetDir);
                    }
                }

                Log($"Đang nén toàn bộ dữ liệu vào tệp ZIP: {zipPath}...");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(tempFolder, zipPath, CompressionLevel.Optimal, false);

                var fi = new FileInfo(zipPath);
                Log($"[HOÀN TẤT] Sao lưu thành công THÀNH CÔNG!");
                Log($"  -> Đường dẫn: {zipPath}");
                Log($"  -> Kích thước tệp: {(fi.Length / 1024.0 / 1024.0):F2} MB");

                Invoke(() =>
                {
                    _progressBar.Value = 100;
                    _lblStatus.Text = "Đã sao lưu thành công!";
                    MessageBox.Show($"Sao lưu trình duyệt thành công!\n\nĐường dẫn:\n{zipPath}\n\nDung lượng: {(fi.Length / 1024.0 / 1024.0):F2} MB", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                Log($"[Lỗi] Sao lưu thất bại: {ex.Message}");
                Invoke(() => MessageBox.Show($"Lỗi sao lưu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                try { Directory.Delete(tempFolder, true); } catch { }
            }
        });

        _isBusy = false;
        _btnBackup.Enabled = true;
        _btnRestore.Enabled = true;
    }

    private void BackupChromiumProfile(string userDataPath, string targetDir)
    {
        // Default and Profile 1..9
        var subDirs = Directory.GetDirectories(userDataPath);
        var profileDirs = new List<string>();

        var defPath = Path.Combine(userDataPath, "Default");
        if (Directory.Exists(defPath)) profileDirs.Add(defPath);

        foreach (var d in subDirs)
        {
            var dirName = Path.GetFileName(d);
            if (dirName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
            {
                profileDirs.Add(d);
            }
        }

        foreach (var pDir in profileDirs)
        {
            var pName = Path.GetFileName(pDir);
            var destSub = Path.Combine(targetDir, pName);
            Directory.CreateDirectory(destSub);

            if (_chkBookmarks.Checked)
            {
                SafeCopyFile(Path.Combine(pDir, "Bookmarks"), Path.Combine(destSub, "Bookmarks"));
                SafeCopyFile(Path.Combine(pDir, "Favicons"), Path.Combine(destSub, "Favicons"));
            }

            if (_chkHistory.Checked)
            {
                SafeCopyFile(Path.Combine(pDir, "History"), Path.Combine(destSub, "History"));
                SafeCopyFile(Path.Combine(pDir, "Top Sites"), Path.Combine(destSub, "Top Sites"));
            }

            if (_chkPreferences.Checked)
            {
                SafeCopyFile(Path.Combine(pDir, "Preferences"), Path.Combine(destSub, "Preferences"));
            }
        }
    }

    private void BackupFirefoxProfile(string profilesRoot, string targetDir)
    {
        if (!Directory.Exists(profilesRoot)) return;
        var pDirs = Directory.GetDirectories(profilesRoot);

        foreach (var p in pDirs)
        {
            var pName = Path.GetFileName(p);
            var destSub = Path.Combine(targetDir, pName);
            Directory.CreateDirectory(destSub);

            if (_chkBookmarks.Checked || _chkHistory.Checked)
            {
                SafeCopyFile(Path.Combine(p, "places.sqlite"), Path.Combine(destSub, "places.sqlite"));
                SafeCopyFile(Path.Combine(p, "favicons.sqlite"), Path.Combine(destSub, "favicons.sqlite"));
            }

            if (_chkPreferences.Checked)
            {
                SafeCopyFile(Path.Combine(p, "prefs.js"), Path.Combine(destSub, "prefs.js"));
            }
        }
    }

    private async Task StartRestoreAsync()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Chọn tệp sao lưu trình duyệt (.zip) để phục hồi",
            Filter = "Tệp sao lưu trình duyệt (*.zip)|*.zip|Tất cả tệp (*.*)|*.*",
            InitialDirectory = Directory.Exists(_destPath) ? _destPath : @"C:\"
        };

        if (ofd.ShowDialog() != DialogResult.OK) return;

        var confirm = MessageBox.Show(
            $"Bạn có muốn khôi phục dữ liệu Bookmark/Profile từ tệp sau không?\n\n{ofd.FileName}\n\nLưu ý: Hãy đóng các trình duyệt trước khi tiếp tục.",
            "Xác Nhận Khôi Phục",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        _isBusy = true;
        _btnBackup.Enabled = false;
        _btnRestore.Enabled = false;
        _progressBar.Value = 20;

        Log($"════════════════════════════════════════════════════════════");
        Log($"[BẮT ĐẦU] Phục hồi dữ liệu DỮ LIỆU TỪ: {Path.GetFileName(ofd.FileName)}");

        await Task.Run(() =>
        {
            var tempExtract = Path.Combine(Path.GetTempPath(), $"Restore_{Guid.NewGuid():N}");
            try
            {
                ZipFile.ExtractToDirectory(ofd.FileName, tempExtract);
                int restoredCount = 0;

                foreach (var b in _browsers)
                {
                    var sourceBrowserDir = Path.Combine(tempExtract, b.BrowserType);
                    if (Directory.Exists(sourceBrowserDir))
                    {
                        Log($"Đang khôi phục cho {b.Name}...");
                        if (b.BrowserType == "Firefox")
                        {
                            RestoreFirefox(sourceBrowserDir, b.ProfilePath);
                        }
                        else
                        {
                            RestoreChromium(sourceBrowserDir, b.ProfilePath);
                        }
                        restoredCount++;
                    }
                }

                Log($"[HOÀN TẤT] Khôi phục thành công THÀNH CÔNG CHO {restoredCount} TRÌNH DUYỆT!");
                Invoke(() =>
                {
                    _progressBar.Value = 100;
                    _lblStatus.Text = "Khôi phục hoàn tất!";
                    MessageBox.Show($"Đã khôi phục thành công dữ liệu cho {restoredCount} trình duyệt!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                Log($"[Lỗi] Khôi phục thất bại: {ex.Message}");
                Invoke(() => MessageBox.Show($"Lỗi khôi phục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                try { Directory.Delete(tempExtract, true); } catch { }
            }
        });

        _isBusy = false;
        _btnBackup.Enabled = true;
        _btnRestore.Enabled = true;
    }

    private void RestoreChromium(string sourceDir, string targetUserDataPath)
    {
        if (!Directory.Exists(targetUserDataPath)) Directory.CreateDirectory(targetUserDataPath);

        foreach (var pDir in Directory.GetDirectories(sourceDir))
        {
            var pName = Path.GetFileName(pDir);
            var targetProfile = Path.Combine(targetUserDataPath, pName);
            if (!Directory.Exists(targetProfile)) Directory.CreateDirectory(targetProfile);

            foreach (var file in Directory.GetFiles(pDir))
            {
                var fName = Path.GetFileName(file);
                SafeCopyFile(file, Path.Combine(targetProfile, fName));
                Log($"  -> Đã khôi phục {fName} vào {pName}");
            }
        }
    }

    private void RestoreFirefox(string sourceDir, string targetProfilesRoot)
    {
        if (!Directory.Exists(targetProfilesRoot)) return;
        var existingProfiles = Directory.GetDirectories(targetProfilesRoot);
        if (existingProfiles.Length == 0) return;

        // Restore to the default active firefox profile
        var targetProfile = existingProfiles.FirstOrDefault(p => p.Contains("default-release")) ?? existingProfiles[0];

        foreach (var pDir in Directory.GetDirectories(sourceDir))
        {
            foreach (var file in Directory.GetFiles(pDir))
            {
                var fName = Path.GetFileName(file);
                SafeCopyFile(file, Path.Combine(targetProfile, fName));
                Log($"  -> Đã khôi phục {fName} vào Firefox ({Path.GetFileName(targetProfile)})");
            }
        }
    }

    private static void SafeCopyFile(string src, string dest)
    {
        if (!File.Exists(src)) return;
        try
        {
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
            File.Copy(src, dest, true);
        }
        catch { }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_isBusy)
        {
            var res = MessageBox.Show("Quá trình sao lưu/khôi phục đang diễn ra, bạn có chắc chắn muốn đóng không?", "Cảnh Báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) e.Cancel = true;
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message));
            return;
        }

        _rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.ScrollToCaret();
    }
}
