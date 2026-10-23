using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Ernastie.Services;

namespace Ernastie.Forms;

public class DriverBackupForm : Form
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
    private Color _warningAmber => ThemeHelper.WarningAmber;
    private Color _successGreen => ThemeHelper.SuccessGreen;

    private readonly BackupService _backupService = new();
    private readonly string _manufacturer;

    private string _backupFolder = string.Empty;
    private string _restoreFolder = string.Empty;
    private bool _isBusy = false;

    // UI Elements
    private Label _lblBackupPath = null!;
    private Label _lblRestorePath = null!;
    private Label _lblWifiStatus = null!;
    private Label _lblPackageStatus = null!;
    private Label _lblProgressStatus = null!;
    private ProgressBar _progressBar = null!;

    private Button _btnBackupCompact = null!;
    private Button _btnBackupFull = null!;
    private Button _btnBackupCustom = null!;
    private Button _btnRestoreWinPe = null!;
    private Button _btnRestoreRunningWin = null!;
    private Button _btnUpdateFromBrand = null!;

    public DriverBackupForm(string manufacturer = "", string initialFolder = "")
    {
        _manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? "Dell" : manufacturer;
        InitializeComponents();
        InitDefaultPaths(initialFolder);
        RefreshFolderScan();
    }

    private void InitDefaultPaths(string initialFolder = "")
    {
        string defaultPath = !string.IsNullOrWhiteSpace(initialFolder)
            ? initialFolder
            : (Directory.Exists(@"D:\") ? @"D:\backupdriver" : @"C:\backupdriver");
        _backupFolder = defaultPath;
        _restoreFolder = defaultPath;
        _lblBackupPath.Text = TruncatePath(_backupFolder, 28);
        _lblRestorePath.Text = TruncatePath(_restoreFolder, 28);
    }

    private string TruncatePath(string path, int maxLen = 30)
    {
        if (string.IsNullOrEmpty(path)) return "Chưa chọn";
        if (path.Length <= maxLen) return path;
        var root = Path.GetPathRoot(path) ?? "";
        var name = Path.GetFileName(path);
        return $"{root}...\\{name}";
    }

    private void InitializeComponents()
    {
        Text = "Driver & WiFi - Sao lưu / Nạp lại (phím 0)";
        Size = new Size(740, 560);
        MinimumSize = new Size(720, 530);
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
            Padding = new Padding(16, 12, 16, 14),
            BackColor = _bgForm
        };

        // 1. Two-column container (SAO LƯU & NẠP LẠI)
        var pnlCols = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 280,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _bgForm
        };
        pnlCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var colLeft = BuildBackupCard();
        colLeft.Margin = new Padding(0, 0, 8, 0);
        pnlCols.Controls.Add(colLeft, 0, 0);

        var colRight = BuildRestoreCard();
        colRight.Margin = new Padding(8, 0, 0, 0);
        pnlCols.Controls.Add(colRight, 1, 0);

        mainPanel.Controls.Add(pnlCols);

        // 2. Status Banner (Driver WiFi đã lưu... / Bộ driver máy...)
        var pnlStatus = BuildStatusBanner();
        pnlStatus.Dock = DockStyle.Top;
        pnlStatus.Height = 70;
        mainPanel.Controls.Add(pnlStatus);

        // 3. Progress area
        var pnlProgress = BuildProgressArea();
        pnlProgress.Dock = DockStyle.Top;
        pnlProgress.Height = 55;
        mainPanel.Controls.Add(pnlProgress);

        // 4. Bottom Button (Cập nhật driver từ hãng...)
        var pnlBottom = BuildBottomArea();
        pnlBottom.Dock = DockStyle.Bottom;
        pnlBottom.Height = 50;
        mainPanel.Controls.Add(pnlBottom);

        Controls.Add(mainPanel);

        // Dock layout ordering
        pnlCols.BringToFront();
        pnlStatus.BringToFront();
        pnlProgress.BringToFront();
        pnlBottom.BringToFront();
        ThemeHelper.ApplyTheme(this);
    }

    private GroupBox BuildBackupCard()
    {
        var gb = new GroupBox
        {
            Text = "SAO LƯU DRIVER (HỆ THỐNG HIỆN TẠI)",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = _textHeading,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 10)
        };

        // Row: Nơi lưu
        var lblLoc = new Label
        {
            Text = "Thư mục lưu:",
            Location = new Point(10, 28),
            Size = new Size(68, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f)
        };

        var btnChange = new Button
        {
            Text = "Thay đổi...",
            Location = new Point(80, 27),
            Size = new Size(66, 28),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnChange.FlatAppearance.BorderColor = _btnBorder;
        btnChange.FlatAppearance.MouseOverBackColor = _btnHover;
        btnChange.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục lưu trữ Driver & WiFi sao lưu",
                SelectedPath = Directory.Exists(_backupFolder) ? _backupFolder : @"C:\"
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _backupFolder = fbd.SelectedPath;
                _lblBackupPath.Text = TruncatePath(_backupFolder, 28);
                RefreshFolderScan();
            }
        };

        _lblBackupPath = new Label
        {
            Text = @"D:\...\backupdriver",
            Location = new Point(152, 28),
            Size = new Size(185, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading
        };

        // Button 1: Bộ gọn + WiFi (~20 MB · 3 giây)
        _btnBackupCompact = new Button
        {
            Text = "Bộ thiết yếu & Wi-Fi (~20 MB)",
            Location = new Point(10, 64),
            Size = new Size(325, 42),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnBackupCompact.FlatAppearance.BorderColor = _btnBorder;
        _btnBackupCompact.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnBackupCompact.Click += async (_, _) => await BackupCompactAsync();

        // Button 2: Toàn bộ kho driver (~3 GB)
        _btnBackupFull = new Button
        {
            Text = "Toàn bộ Driver hệ thống (~3 GB)",
            Location = new Point(10, 114),
            Size = new Size(325, 38),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnBackupFull.FlatAppearance.BorderColor = _btnBorder;
        _btnBackupFull.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnBackupFull.Click += async (_, _) => await BackupFullAsync();

        // Button 3: Chọn từng gói...
        _btnBackupCustom = new Button
        {
            Text = "Chọn từng gói Driver...",
            Location = new Point(10, 160),
            Size = new Size(325, 36),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnBackupCustom.FlatAppearance.BorderColor = _btnBorder;
        _btnBackupCustom.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnBackupCustom.Click += (_, _) => OpenCustomSelectionDialog();

        // Hint note
        var lblNote = new Label
        {
            Text = "Máy không vào được Windows? Xem nút \"Lấy driver từ ổ cứng\" ở nhóm WinPE, màn hình chính.",
            Location = new Point(10, 206),
            Size = new Size(325, 48),
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = _textMuted
        };

        gb.Controls.Add(lblLoc);
        gb.Controls.Add(btnChange);
        gb.Controls.Add(_lblBackupPath);
        gb.Controls.Add(_btnBackupCompact);
        gb.Controls.Add(_btnBackupFull);
        gb.Controls.Add(_btnBackupCustom);
        gb.Controls.Add(lblNote);

        return gb;
    }

    private GroupBox BuildRestoreCard()
    {
        var gb = new GroupBox
        {
            Text = "NẠP PHỤC HỒI DRIVER (RESTORE)",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = _textHeading,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 10)
        };

        // Row: Lấy từ
        var lblSrc = new Label
        {
            Text = "Thư mục nguồn:",
            Location = new Point(10, 28),
            Size = new Size(68, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f)
        };

        var btnChange = new Button
        {
            Text = "Thay đổi...",
            Location = new Point(80, 27),
            Size = new Size(66, 28),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnChange.FlatAppearance.BorderColor = _btnBorder;
        btnChange.FlatAppearance.MouseOverBackColor = _btnHover;
        btnChange.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục chứa bản sao lưu Driver & WiFi cần nạp",
                SelectedPath = Directory.Exists(_restoreFolder) ? _restoreFolder : @"C:\"
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _restoreFolder = fbd.SelectedPath;
                _lblRestorePath.Text = TruncatePath(_restoreFolder, 28);
                RefreshFolderScan();
            }
        };

        _lblRestorePath = new Label
        {
            Text = @"D:\...\backupdriver",
            Location = new Point(152, 28),
            Size = new Size(185, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading
        };

        // Button 1: Nạp vào WinPE + kết nối WiFi
        _btnRestoreWinPe = new Button
        {
            Text = "Nạp vào WinPE & Kết nối Wi-Fi",
            Location = new Point(10, 64),
            Size = new Size(325, 42),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRestoreWinPe.FlatAppearance.BorderColor = _btnBorder;
        _btnRestoreWinPe.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnRestoreWinPe.Click += async (_, _) => await RestoreWinPeAsync();

        // Button 2: Nạp vào Windows đang chạy
        _btnRestoreRunningWin = new Button
        {
            Text = "Nạp trực tiếp vào Windows",
            Location = new Point(10, 114),
            Size = new Size(325, 42),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRestoreRunningWin.FlatAppearance.BorderColor = _btnBorder;
        _btnRestoreRunningWin.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnRestoreRunningWin.Click += async (_, _) => await RestoreRunningWinAsync();

        // Hint note
        var lblNote = new Label
        {
            Text = "Windows vừa bung xong thì Bước 3 của nút \"Cài đặt Windows\" đã tự nhét — không cần bấm gì ở đây.",
            Location = new Point(10, 170),
            Size = new Size(325, 48),
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = _textMuted
        };

        gb.Controls.Add(lblSrc);
        gb.Controls.Add(btnChange);
        gb.Controls.Add(_lblRestorePath);
        gb.Controls.Add(_btnRestoreWinPe);
        gb.Controls.Add(_btnRestoreRunningWin);
        gb.Controls.Add(lblNote);

        return gb;
    }

    private Panel BuildStatusBanner()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(4, 6, 4, 6)
        };

        _lblWifiStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Text = "Driver WiFi đã lưu: chưa   |   Thông tin WiFi đã lưu: chưa"
        };

        _lblPackageStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Text = "Bộ driver máy: Chưa có bản sao lưu nào trong thư mục"
        };

        pnl.Controls.Add(_lblPackageStatus);
        pnl.Controls.Add(_lblWifiStatus);

        return pnl;
    }

    private Panel BuildProgressArea()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(12, 4, 12, 4)
        };

        _lblProgressStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Text = "Tiến trình: Sẵn sàng"
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Bottom,
            Height = 16,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous
        };

        pnl.Controls.Add(_lblProgressStatus);
        pnl.Controls.Add(_progressBar);

        return pnl;
    }

    private Panel BuildBottomArea()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(12, 0, 12, 0)
        };

        _btnUpdateFromBrand = new Button
        {
            Text = "⬆ Cập nhật driver từ hãng (Dell / HP / Lenovo)...",
            Dock = DockStyle.Fill,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnUpdateFromBrand.FlatAppearance.BorderColor = _btnBorder;
        _btnUpdateFromBrand.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnUpdateFromBrand.Click += (_, _) => _backupService.OpenBrandDriverPage(_manufacturer);

        pnl.Controls.Add(_btnUpdateFromBrand);
        return pnl;
    }

    private void SetBusy(bool busy, string statusText = "")
    {
        _isBusy = busy;
        _btnBackupCompact.Enabled = !busy;
        _btnBackupFull.Enabled = !busy;
        _btnBackupCustom.Enabled = !busy;
        _btnRestoreWinPe.Enabled = !busy;
        _btnRestoreRunningWin.Enabled = !busy;
        _btnUpdateFromBrand.Enabled = !busy;

        if (!string.IsNullOrEmpty(statusText))
        {
            _lblProgressStatus.Text = $"Tiến trình: {statusText}";
        }
    }

    private void RefreshFolderScan()
    {
        var target = _restoreFolder;
        if (!Directory.Exists(target))
        {
            _lblWifiStatus.Text = "Driver WiFi đã lưu: chưa   |   Thông tin WiFi đã lưu: chưa";
            _lblPackageStatus.Text = "Bộ driver máy: Chưa có bản sao lưu nào trong thư mục";
            _lblPackageStatus.ForeColor = _textMuted;
            return;
        }

        bool hasWifiDriver = false;
        bool hasWifiProfiles = false;
        int packageCount = 0;
        long totalBytes = 0;
        string originMachine = string.Empty;

        try
        {
            // Check WiFi profiles (xml files)
            var xmlFiles = Directory.GetFiles(target, "*.xml", SearchOption.AllDirectories);
            hasWifiProfiles = xmlFiles.Length > 0;

            // Check INF files
            var infFiles = Directory.GetFiles(target, "*.inf", SearchOption.AllDirectories);
            packageCount = infFiles.Length;

            foreach (var inf in infFiles)
            {
                var name = Path.GetFileName(inf).ToLowerInvariant();
                if (name.Contains("net") || name.Contains("wifi") || name.Contains("wlan") || name.Contains("wireless"))
                {
                    hasWifiDriver = true;
                }
            }

            // Calculate total size
            var allFiles = Directory.GetFiles(target, "*.*", SearchOption.AllDirectories);
            foreach (var f in allFiles)
            {
                try { totalBytes += new FileInfo(f).Length; } catch { }
            }

            // Check metadata file
            var metaFile = Path.Combine(target, "machine_info.txt");
            if (File.Exists(metaFile))
            {
                var lines = File.ReadAllLines(metaFile);
                foreach (var l in lines)
                {
                    if (l.StartsWith("MachineName=", StringComparison.OrdinalIgnoreCase))
                    {
                        originMachine = l.Substring("MachineName=".Length).Trim();
                    }
                }
            }
        }
        catch { }

        // Update labels
        string wifiDriverText = hasWifiDriver ? "Có (WiFi/WLAN)" : "chưa";
        string wifiProfileText = hasWifiProfiles ? "Có (Đã lưu XML)" : "chưa";
        _lblWifiStatus.Text = $"Driver WiFi đã lưu: {wifiDriverText}   |   Thông tin WiFi đã lưu: {wifiProfileText}";

        if (packageCount == 0)
        {
            _lblPackageStatus.Text = "Bộ driver máy: Chưa có gói driver nào trong thư mục này";
            _lblPackageStatus.ForeColor = _textMuted;
        }
        else
        {
            double mb = totalBytes / (1024.0 * 1024.0);
            if (!string.IsNullOrEmpty(originMachine) && !originMachine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                _lblPackageStatus.Text = $"Bộ driver máy: {packageCount} gói ({mb:F1} MB) — ⚠ của MÁY KHÁC ({originMachine})";
                _lblPackageStatus.ForeColor = _warningAmber;
            }
            else
            {
                _lblPackageStatus.Text = $"Bộ driver máy: {packageCount} gói ({mb:F1} MB) — ✓ KHỚP MÁY NÀY ({Environment.MachineName})";
                _lblPackageStatus.ForeColor = _successGreen;
            }
        }
    }

    private async Task BackupCompactAsync()
    {
        var target = _backupFolder.TrimEnd('\\', '/');
        if (string.IsNullOrEmpty(target))
        {
            MessageBox.Show("Vui lòng chọn thư mục lưu trữ!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Directory.CreateDirectory(target);
        SetBusy(true, "Đang quét driver Mạng & WiFi...");
        _progressBar.Style = ProgressBarStyle.Marquee;

        int exportedDrivers = 0;
        int exportedWifi = 0;

        await Task.Run(() =>
        {
            try
            {
                // 1. Enumerate all third-party drivers
                var allDrivers = GetInstalledDrivers();
                // Filter for essential: Network, Storage, Touchpad
                var compactList = allDrivers.Where(d => d.IsNetworkOrWifi || d.IsStorage || d.IsInput).ToList();

                int idx = 0;
                foreach (var d in compactList)
                {
                    idx++;
                    ExportSingleDriverPackage(d.PublishedName, target);
                    exportedDrivers++;
                }

                // 2. Export WiFi profiles
                var wifiDir = Path.Combine(target, "WiFi");
                Directory.CreateDirectory(wifiDir);
                var psiWifi = new ProcessStartInfo("netsh.exe", $"wlan export profile folder=\"{wifiDir}\" key=clear")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var pWifi = Process.Start(psiWifi)!;
                pWifi.WaitForExit();
                exportedWifi = Directory.GetFiles(wifiDir, "*.xml").Length;

                // Also copy to root for easy detection
                foreach (var xml in Directory.GetFiles(wifiDir, "*.xml"))
                {
                    try { File.Copy(xml, Path.Combine(target, Path.GetFileName(xml)), true); } catch { }
                }

                // 3. Write metadata file
                WriteMachineMetadata(target, "Compact_WiFi", compactList.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sao lưu bộ gọn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 100;
        SetBusy(false, $"Hoàn tất! Đã xuất {exportedDrivers} gói driver quan trọng & {exportedWifi} hồ sơ WiFi.");
        RefreshFolderScan();

        var res = MessageBox.Show(
            $"Đã sao lưu thành công Bộ gọn Driver & WiFi!\n\n" +
            $"📁 Thư mục: {target}\n" +
            $"📦 Driver thiết yếu: {exportedDrivers} gói (WiFi, Mạng, Storage, Touchpad)\n" +
            $"📶 Hồ sơ WiFi: {exportedWifi} mạng\n\n" +
            $"Bạn có muốn mở thư mục ngay không?",
            "Sao Lưu Thành Công",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (res == DialogResult.Yes)
        {
            try { Process.Start("explorer.exe", target); } catch { }
        }
    }

    private async Task BackupFullAsync()
    {
        var target = _backupFolder.TrimEnd('\\', '/');
        if (string.IsNullOrEmpty(target))
        {
            MessageBox.Show("Vui lòng chọn thư mục lưu trữ!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            "Sao lưu toàn bộ kho driver có thể mất từ 1 - 3 phút và chiếm khoảng 1 - 3 GB dung lượng ổ đĩa.\n\nBạn có muốn tiếp tục không?",
            "Xác Nhận Sao Lưu Toàn Bộ",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        Directory.CreateDirectory(target);
        SetBusy(true, "Đang xuất toàn bộ kho driver hệ thống bằng pnputil / dism...");
        _progressBar.Style = ProgressBarStyle.Marquee;

        int count = 0;
        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("pnputil.exe", $"/export-driver * \"{target}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi)!;
                proc.WaitForExit();

                // Export WiFi profiles
                var wifiDir = Path.Combine(target, "WiFi");
                Directory.CreateDirectory(wifiDir);
                var psiWifi = new ProcessStartInfo("netsh.exe", $"wlan export profile folder=\"{wifiDir}\" key=clear")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var pWifi = Process.Start(psiWifi)!;
                pWifi.WaitForExit();

                count = Directory.GetFiles(target, "*.inf", SearchOption.AllDirectories).Length;
                WriteMachineMetadata(target, "Full_Driver_Store", count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất toàn bộ driver: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 100;
        SetBusy(false, $"Hoàn tất! Đã xuất {count} gói driver toàn bộ hệ thống.");
        RefreshFolderScan();

        var res = MessageBox.Show(
            $"Đã xuất toàn bộ kho driver thành công!\nTổng cộng {count} gói driver trong thư mục:\n{target}\n\nBạn có muốn mở thư mục ngay bây giờ không?",
            "Hoàn Tất",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (res == DialogResult.Yes)
        {
            try { Process.Start("explorer.exe", target); } catch { }
        }
    }

    private void OpenCustomSelectionDialog()
    {
        SetBusy(true, "Đang đọc danh sách driver hệ thống...");
        var drivers = GetInstalledDrivers();
        SetBusy(false, "Sẵn sàng");

        if (drivers.Count == 0)
        {
            MessageBox.Show("Không tìm thấy gói driver bên thứ ba (OEM) nào trong hệ thống!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SelectDriversDialog(drivers, _backupFolder);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            RefreshFolderScan();
        }
    }

    private async Task RestoreWinPeAsync()
    {
        var source = _restoreFolder.TrimEnd('\\', '/');
        if (!Directory.Exists(source))
        {
            MessageBox.Show($"Thư mục nguồn không tồn tại:\n{source}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true, "Đang nạp driver vào WinPE và cấu hình WiFi...");
        _progressBar.Style = ProgressBarStyle.Marquee;

        int driverCount = 0;
        int wifiCount = 0;

        await Task.Run(() =>
        {
            try
            {
                // In WinPE, try drvload first for all INF files
                var infFiles = Directory.GetFiles(source, "*.inf", SearchOption.AllDirectories);
                foreach (var inf in infFiles)
                {
                    try
                    {
                        var psi = new ProcessStartInfo("drvload.exe", $"\"{inf}\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p = Process.Start(psi)!;
                        p.WaitForExit();
                        driverCount++;
                    }
                    catch
                    {
                        // Fallback to pnputil
                        var psi2 = new ProcessStartInfo("pnputil.exe", $"/add-driver \"{inf}\" /install")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p2 = Process.Start(psi2)!;
                        p2.WaitForExit();
                        driverCount++;
                    }
                }

                // Start WLAN service if not started
                try
                {
                    var psiSvc = new ProcessStartInfo("net.exe", "start wlansvc")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var pSvc = Process.Start(psiSvc)!;
                    pSvc.WaitForExit();
                }
                catch { }

                // Add WiFi XML profiles
                var xmlFiles = Directory.GetFiles(source, "*.xml", SearchOption.AllDirectories);
                foreach (var xml in xmlFiles)
                {
                    try
                    {
                        var psiWifi = new ProcessStartInfo("netsh.exe", $"wlan add profile filename=\"{xml}\" user=all")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var pWifi = Process.Start(psiWifi)!;
                        pWifi.WaitForExit();
                        wifiCount++;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp driver WinPE: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 100;
        SetBusy(false, $"Đã nạp {driverCount} driver và khôi phục {wifiCount} hồ sơ WiFi vào WinPE.");
        MessageBox.Show($"Đã nạp xong {driverCount} driver vào WinPE!\nĐã khôi phục {wifiCount} hồ sơ WiFi.", "WinPE Driver Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task RestoreRunningWinAsync()
    {
        var source = _restoreFolder.TrimEnd('\\', '/');
        if (!Directory.Exists(source))
        {
            MessageBox.Show($"Thư mục nguồn không tồn tại:\n{source}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có muốn cài đặt toàn bộ driver từ thư mục:\n{source}\n\nvào hệ thống Windows đang chạy không?",
            "Xác Nhận Nạp Driver",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        SetBusy(true, "Đang cài đặt driver bằng PnPUtil vào Windows...");
        _progressBar.Style = ProgressBarStyle.Marquee;

        int wifiCount = 0;
        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("pnputil.exe", $"/add-driver \"{source}\\*.inf\" /subdirs /install")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi)!;
                proc.WaitForExit();

                // Add WiFi XML profiles
                var xmlFiles = Directory.GetFiles(source, "*.xml", SearchOption.AllDirectories);
                foreach (var xml in xmlFiles)
                {
                    try
                    {
                        var psiWifi = new ProcessStartInfo("netsh.exe", $"wlan add profile filename=\"{xml}\" user=all")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var pWifi = Process.Start(psiWifi)!;
                        pWifi.WaitForExit();
                        wifiCount++;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp driver vào Windows: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 100;
        SetBusy(false, "Hoàn tất cài đặt driver vào Windows.");
        MessageBox.Show($"Đã hoàn tất cài đặt toàn bộ driver vào Windows đang chạy!\nĐã nạp {wifiCount} hồ sơ WiFi.", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ExportSingleDriverPackage(string oemName, string targetDir)
    {
        try
        {
            var psi = new ProcessStartInfo("pnputil.exe", $"/export-driver {oemName} \"{targetDir}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
        }
        catch { }
    }

    private static void WriteMachineMetadata(string targetDir, string backupType, int count)
    {
        try
        {
            var metaFile = Path.Combine(targetDir, "machine_info.txt");
            var sb = new StringBuilder();
            sb.AppendLine($"MachineName={Environment.MachineName}");
            sb.AppendLine($"UserName={Environment.UserName}");
            sb.AppendLine($"OSVersion={Environment.OSVersion}");
            sb.AppendLine($"BackupDate={DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"BackupType={backupType}");
            sb.AppendLine($"PackageCount={count}");
            File.WriteAllText(metaFile, sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }

    private static List<DriverItemModel> GetInstalledDrivers()
    {
        var list = new List<DriverItemModel>();
        try
        {
            var psi = new ProcessStartInfo("pnputil.exe", "/enum-drivers")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            var lines = output.Split('\n');
            DriverItemModel? current = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                int colonIdx = line.IndexOf(':');
                if (colonIdx <= 0) continue;

                var key = line.Substring(0, colonIdx).Trim();
                var val = line.Substring(colonIdx + 1).Trim();

                if (key.Contains("Published Name") || key.Contains("Tên đã phát hành") || key.Contains("Tên phát hành") || key.StartsWith("Published"))
                {
                    if (current != null) list.Add(current);
                    current = new DriverItemModel { PublishedName = val };
                }
                else if (current != null)
                {
                    if (key.Contains("Original Name") || key.Contains("Tên gốc"))
                        current.OriginalName = val;
                    else if (key.Contains("Provider Name") || key.Contains("Nhà cung cấp"))
                        current.ProviderName = val;
                    else if (key.Contains("Class Name") || key.Contains("Tên lớp") || key.Contains("Lớp"))
                        current.ClassName = val;
                    else if (key.Contains("Driver Version") || key.Contains("Phiên bản"))
                        current.DriverVersion = val;
                }
            }
            if (current != null) list.Add(current);
        }
        catch { }

        return list;
    }
}

public class DriverItemModel
{
    public string PublishedName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string DriverVersion { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = true;

    public bool IsNetworkOrWifi => ClassName.Equals("Net", StringComparison.OrdinalIgnoreCase) ||
                                  OriginalName.Contains("wifi", StringComparison.OrdinalIgnoreCase) ||
                                  OriginalName.Contains("wlan", StringComparison.OrdinalIgnoreCase) ||
                                  OriginalName.Contains("netw", StringComparison.OrdinalIgnoreCase);

    public bool IsStorage => ClassName.Equals("SCSIAdapter", StringComparison.OrdinalIgnoreCase) ||
                             ClassName.Equals("HDC", StringComparison.OrdinalIgnoreCase) ||
                             OriginalName.Contains("iaStor", StringComparison.OrdinalIgnoreCase) ||
                             OriginalName.Contains("vmd", StringComparison.OrdinalIgnoreCase) ||
                             OriginalName.Contains("rst", StringComparison.OrdinalIgnoreCase);

    public bool IsInput => ClassName.Equals("HIDClass", StringComparison.OrdinalIgnoreCase) ||
                           ClassName.Equals("Mouse", StringComparison.OrdinalIgnoreCase) ||
                           ClassName.Equals("Keyboard", StringComparison.OrdinalIgnoreCase);
}

public class SelectDriversDialog : Form
{
    private readonly List<DriverItemModel> _drivers;
    private readonly string _targetDir;
    private DataGridView _dgv = null!;

    public SelectDriversDialog(List<DriverItemModel> drivers, string targetDir)
    {
        _drivers = drivers;
        _targetDir = targetDir;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Chọn danh sách Driver cần sao lưu";
        Size = new Size(820, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = ThemeHelper.BgForm;
        Font = new Font("Segoe UI", 9f);
        Icon = SystemIcons.Application;

        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(10, 8, 10, 0),
            BackColor = Color.Transparent
        };

        var btnSelectAll = new Button { Text = "✓ Chọn tất cả", Size = new Size(110, 30), BackColor = Color.FromArgb(235, 225, 205), FlatStyle = FlatStyle.Flat };
        btnSelectAll.Click += (_, _) => SetAllChecked(true);

        var btnDeselectAll = new Button { Text = "✕ Bỏ chọn hết", Size = new Size(110, 30), BackColor = Color.FromArgb(235, 225, 205), FlatStyle = FlatStyle.Flat };
        btnDeselectAll.Click += (_, _) => SetAllChecked(false);

        var btnNetOnly = new Button { Text = "Chỉ Mạng & Wi-Fi", Size = new Size(140, 30), BackColor = Color.FromArgb(235, 225, 205), FlatStyle = FlatStyle.Flat };
        btnNetOnly.Click += (_, _) => SelectByFilter(d => d.IsNetworkOrWifi);

        var btnStorageOnly = new Button { Text = "Chỉ Storage / VMD", Size = new Size(140, 30), BackColor = Color.FromArgb(235, 225, 205), FlatStyle = FlatStyle.Flat };
        btnStorageOnly.Click += (_, _) => SelectByFilter(d => d.IsStorage);

        topBar.Controls.AddRange(new Control[] { btnSelectAll, btnDeselectAll, btnNetOnly, btnStorageOnly });

        _dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(226, 218, 196)
        };
        _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 225, 205);
        _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _dgv.ColumnHeadersHeight = 32;

        var colCheck = new DataGridViewCheckBoxColumn { Name = "ColCheck", HeaderText = "Chọn", Width = 50 };
        var colOem = new DataGridViewTextBoxColumn { Name = "ColOem", HeaderText = "Gói OEM", Width = 90 };
        var colProvider = new DataGridViewTextBoxColumn { Name = "ColProvider", HeaderText = "Nhà cung cấp", Width = 160 };
        var colClass = new DataGridViewTextBoxColumn { Name = "ColClass", HeaderText = "Loại thiết bị", Width = 110 };
        var colVersion = new DataGridViewTextBoxColumn { Name = "ColVersion", HeaderText = "Phiên bản", Width = 130 };
        var colOrig = new DataGridViewTextBoxColumn { Name = "ColOrig", HeaderText = "Tên tệp gốc (.inf)", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };

        _dgv.Columns.AddRange(colCheck, colOem, colProvider, colClass, colVersion, colOrig);

        foreach (var d in _drivers)
        {
            _dgv.Rows.Add(d.IsSelected, d.PublishedName, d.ProviderName, d.ClassName, d.DriverVersion, d.OriginalName);
        }

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(10, 6, 10, 6), BackColor = Color.Transparent };
        var btnExport = new Button
        {
            Text = "Xuất các gói đã chọn",
            Dock = DockStyle.Right,
            Width = 190,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnExport.Click += (_, _) => DoExportSelected();

        var btnClose = new Button
        {
            Text = "Hủy",
            Dock = DockStyle.Right,
            Width = 90,
            BackColor = Color.FromArgb(235, 225, 205),
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        btnClose.Click += (_, _) => Close();

        bottomBar.Controls.Add(btnExport);
        bottomBar.Controls.Add(btnClose);

        Controls.Add(_dgv);
        Controls.Add(topBar);
        Controls.Add(bottomBar);
        ThemeHelper.ApplyTheme(this);
    }

    private void SetAllChecked(bool check)
    {
        foreach (DataGridViewRow row in _dgv.Rows)
        {
            row.Cells["ColCheck"].Value = check;
        }
    }

    private void SelectByFilter(Func<DriverItemModel, bool> predicate)
    {
        for (int i = 0; i < _drivers.Count && i < _dgv.Rows.Count; i++)
        {
            _dgv.Rows[i].Cells["ColCheck"].Value = predicate(_drivers[i]);
        }
    }

    private void DoExportSelected()
    {
        var selectedOems = new List<string>();
        for (int i = 0; i < _dgv.Rows.Count; i++)
        {
            if (Convert.ToBoolean(_dgv.Rows[i].Cells["ColCheck"].Value))
            {
                selectedOems.Add(_dgv.Rows[i].Cells["ColOem"].Value?.ToString() ?? "");
            }
        }

        if (selectedOems.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ít nhất một gói driver để xuất!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Directory.CreateDirectory(_targetDir);
        int success = 0;
        foreach (var oem in selectedOems)
        {
            try
            {
                var psi = new ProcessStartInfo("pnputil.exe", $"/export-driver {oem} \"{_targetDir}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                success++;
            }
            catch { }
        }

        MessageBox.Show($"Đã xuất thành công {success}/{selectedOems.Count} gói driver vào:\n{_targetDir}", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }
}
