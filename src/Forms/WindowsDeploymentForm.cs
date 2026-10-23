using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ernastie.Models;
using Ernastie.Services;

namespace Ernastie.Forms;

public class WindowsDeploymentForm : Form
{
    public enum DeploymentMode
    {
        StandardReinstall, // Cài đặt lại Windows (tự động bung WIM/ESD)
        WinPeCleanInstall, // Cài Win sạch trong WinPE
        Windows7Install,   // Cài đặt Windows 7
        Windows7Downgrade  // Hạ cấp về Windows 7 (hướng dẫn & tích hợp NVMe/USB 3.0)
    }

    private readonly DeploymentMode _mode;
    private CancellationTokenSource? _cts;
    private string? _mountedIsoPath;

    // UI Controls
    private Label _lblHeaderTitle = null!;
    private Label _lblHeaderSub = null!;
    private Label _lblModeBadge = null!;

    private TextBox _txtImagePath = null!;
    private Button _btnBrowseImage = null!;
    private ComboBox _cboWimIndex = null!;
    private Label _lblWimStatus = null!;

    private ComboBox _cboTargetDrive = null!;
    private Label _lblDriveWarning = null!;
    private CheckBox _chkFormatTarget = null!;

    private CheckBox _chkBcdBoot = null!;
    private CheckBox _chkInjectDrivers = null!;
    private CheckBox _chkWin7Nvme = null!;
    private CheckBox _chkRestartAfter = null!;

    private ProgressBar _prgDeploy = null!;
    private Label _lblStatusText = null!;
    private RichTextBox _rtbLog = null!;

    private Button _btnStart = null!;
    private Button _btnClose = null!;

    public WindowsDeploymentForm(DeploymentMode mode = DeploymentMode.StandardReinstall)
    {
        _mode = mode;
        InitializeUi();
        ApplyModeSettings();
        ThemeHelper.ApplyTheme(this);
        LoadTargetDrives();
    }

    private static void SetDoubleBuffered(Control c)
    {
        typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(c, true, null);
    }

    private Panel CreateCard(string title, int height)
    {
        var card = new Panel
        {
            Height = height,
            BackColor = ThemeHelper.CardBg,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 12)
        };
        card.Paint += (s, e) =>
        {
            using var p = new Pen(ThemeHelper.BorderColor, 1);
            e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
        };
        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ThemeHelper.TextHeading,
            AutoSize = true,
            Location = new Point(14, 10)
        };
        card.Controls.Add(lblTitle);
        return card;
    }

    private void InitializeUi()
    {
        Text = "Triển Khai & Cài Đặt Windows (DISM Engine)";
        Size = new Size(880, 720);
        MinimumSize = new Size(820, 650);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ThemeHelper.BgForm;
        Font = new Font("Segoe UI", 9.25f);
        SetDoubleBuffered(this);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = ThemeHelper.CardBg,
            Padding = new Padding(20, 14, 20, 10)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var p = new Pen(ThemeHelper.BorderColor, 1);
            e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        _lblHeaderTitle = new Label
        {
            Text = "TRIỂN KHAI & CÀI ĐẶT WINDOWS (DISM APPLY-IMAGE ENGINE)",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ThemeHelper.TextHeading,
            AutoSize = true,
            Location = new Point(18, 14)
        };

        _lblHeaderSub = new Label
        {
            Text = "Bung file ảnh WIM / ESD / ISO trực tiếp vào phân vùng đĩa, nạp bootloader và inject driver tự động.",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = ThemeHelper.TextMuted,
            AutoSize = true,
            Location = new Point(20, 42)
        };

        _lblModeBadge = new Label
        {
            Location = new Point(620, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeHelper.StyleBadge(_lblModeBadge, "Chế độ: Chuẩn", BadgeType.Info);

        pnlHeader.Controls.Add(_lblHeaderTitle);
        pnlHeader.Controls.Add(_lblHeaderSub);
        pnlHeader.Controls.Add(_lblModeBadge);
        Controls.Add(pnlHeader);

        // Bottom Bar
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = ThemeHelper.CardBg,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var p = new Pen(ThemeHelper.BorderColor, 1);
            e.Graphics.DrawLine(p, 0, 0, pnlBottom.Width, 0);
        };

        _btnStart = new Button
        {
            Text = "🚀 Bắt đầu Bung ảnh & Cài đặt",
            Size = new Size(220, 38),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 360, 11),
            Tag = "primary"
        };
        _btnStart.Click += async (s, e) => await StartDeploymentAsync();

        _btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(110, 38),
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 130, 11)
        };
        _btnClose.Click += (s, e) =>
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                var res = MessageBox.Show("Quá trình bung ảnh đang chạy! Bạn có chắc chắn muốn hủy không?", "Cảnh Báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    _cts.Cancel();
                    Log("[HỦY BỎ]: Người dùng đã hủy quá trình bung ảnh.");
                }
                return;
            }
            CleanupMountedIso();
            Close();
        };

        pnlBottom.Controls.Add(_btnStart);
        pnlBottom.Controls.Add(_btnClose);
        Controls.Add(pnlBottom);

        // Main Content Panel (Scrollable)
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20, 14, 20, 14)
        };
        Controls.Add(pnlContent);

        int currentY = 8;

        // Card 1: Chọn Tệp Cài Đặt (WIM / ESD / ISO)
        var cardImage = CreateCard("1. CHỌN TỆP ẢNH HỆ ĐIỀU HÀNH (SOURCE IMAGE)", 135);
        cardImage.Location = new Point(0, currentY);
        cardImage.Width = pnlContent.Width - 45;
        cardImage.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var lblImg = new Label { Text = "Tệp ảnh nguồn (*.wim, *.esd, *.iso):", Location = new Point(16, 34), AutoSize = true, ForeColor = ThemeHelper.TextHeading };
        _txtImagePath = new TextBox { Location = new Point(16, 56), Size = new Size(cardImage.Width - 165, 26), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _btnBrowseImage = new Button { Text = "📂 Chọn file...", Location = new Point(cardImage.Width - 140, 54), Size = new Size(122, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _btnBrowseImage.Click += (s, e) => BrowseImageFile();

        var lblIdx = new Label { Text = "Phiên bản (Index):", Location = new Point(16, 94), AutoSize = true, ForeColor = ThemeHelper.TextHeading };
        _cboWimIndex = new ComboBox { Location = new Point(140, 91), Size = new Size(cardImage.Width - 400, 26), DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _lblWimStatus = new Label { Text = "Chưa chọn tệp ảnh", Location = new Point(cardImage.Width - 250, 94), AutoSize = true, ForeColor = ThemeHelper.TextMuted, Anchor = AnchorStyles.Top | AnchorStyles.Right };

        cardImage.Controls.Add(lblImg);
        cardImage.Controls.Add(_txtImagePath);
        cardImage.Controls.Add(_btnBrowseImage);
        cardImage.Controls.Add(lblIdx);
        cardImage.Controls.Add(_cboWimIndex);
        cardImage.Controls.Add(_lblWimStatus);
        pnlContent.Controls.Add(cardImage);

        currentY += 145;

        // Card 2: Chọn Phân Vùng Đích
        var cardDrive = CreateCard("2. PHÂN VÙNG CÀI ĐẶT ĐÍCH (TARGET PARTITION)", 115);
        cardDrive.Location = new Point(0, currentY);
        cardDrive.Width = pnlContent.Width - 45;
        cardDrive.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var lblTarget = new Label { Text = "Chọn ổ đĩa đích cần bung ảnh:", Location = new Point(16, 34), AutoSize = true, ForeColor = ThemeHelper.TextHeading };
        _cboTargetDrive = new ComboBox { Location = new Point(16, 56), Size = new Size(cardDrive.Width - 260, 26), DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _cboTargetDrive.SelectedIndexChanged += (s, e) => OnTargetDriveChanged();

        _chkFormatTarget = new CheckBox
        {
            Text = "Format nhanh phân vùng đích trước khi bung (NTFS)",
            Location = new Point(cardDrive.Width - 235, 57),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            ForeColor = ThemeHelper.TextHeading
        };

        _lblDriveWarning = new Label
        {
            Text = "💡 Lưu ý chọn đúng phân vùng để tránh mất dữ liệu quan trọng.",
            Location = new Point(16, 88),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ThemeHelper.TextMuted
        };

        cardDrive.Controls.Add(lblTarget);
        cardDrive.Controls.Add(_cboTargetDrive);
        cardDrive.Controls.Add(_chkFormatTarget);
        cardDrive.Controls.Add(_lblDriveWarning);
        pnlContent.Controls.Add(cardDrive);

        currentY += 125;

        // Card 3: Tùy Chọn Nâng Cao & Tích Hợp Driver
        var cardOptions = CreateCard("3. TÙY CHỌN TÍCH HỢP TỰ ĐỘNG & BOOTLOADER", 110);
        cardOptions.Location = new Point(0, currentY);
        cardOptions.Width = pnlContent.Width - 45;
        cardOptions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _chkBcdBoot = new CheckBox
        {
            Text = "Tự động tạo/sửa BCD Bootloader (bcdboot X:\\Windows /l vi-VN)",
            Location = new Point(16, 35),
            AutoSize = true,
            Checked = true,
            ForeColor = ThemeHelper.TextHeading
        };

        _chkInjectDrivers = new CheckBox
        {
            Text = "Trích xuất & nạp Driver máy hiện tại vào Windows mới (dism /Add-Driver)",
            Location = new Point(16, 68),
            AutoSize = true,
            Checked = true,
            ForeColor = ThemeHelper.TextHeading
        };

        _chkWin7Nvme = new CheckBox
        {
            Text = "Tự động tích hợp Driver NVMe & USB 3.0 xHCI (Dành cho Windows 7)",
            Location = new Point(cardOptions.Width / 2 + 10, 35),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            ForeColor = ThemeHelper.TextHeading
        };

        _chkRestartAfter = new CheckBox
        {
            Text = "Tự động khởi động lại máy sau khi hoàn tất cài đặt",
            Location = new Point(cardOptions.Width / 2 + 10, 68),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            ForeColor = ThemeHelper.TextHeading
        };

        cardOptions.Controls.Add(_chkBcdBoot);
        cardOptions.Controls.Add(_chkInjectDrivers);
        cardOptions.Controls.Add(_chkWin7Nvme);
        cardOptions.Controls.Add(_chkRestartAfter);
        pnlContent.Controls.Add(cardOptions);

        currentY += 120;

        // Card 4: Tiến Trình & Nhật Ký Thực Thi
        var cardProgress = CreateCard("4. TIẾN TRÌNH THỰC THI & NHẬT KÝ CHI TIẾT", 185);
        cardProgress.Location = new Point(0, currentY);
        cardProgress.Width = pnlContent.Width - 45;
        cardProgress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

        _lblStatusText = new Label
        {
            Text = "Sẵn sàng thực thi. Vui lòng cấu hình các bước trên và bấm 'Bắt đầu Bung ảnh'.",
            Location = new Point(16, 34),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ThemeHelper.PrimaryBlue
        };

        _prgDeploy = new ProgressBar
        {
            Location = new Point(16, 58),
            Size = new Size(cardProgress.Width - 32, 18),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Style = ProgressBarStyle.Blocks,
            Value = 0
        };

        _rtbLog = new RichTextBox
        {
            Location = new Point(16, 84),
            Size = new Size(cardProgress.Width - 32, 90),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 8.75f),
            BackColor = ThemeHelper.IsDark ? Color.FromArgb(7, 10, 15) : Color.FromArgb(241, 245, 249),
            ForeColor = ThemeHelper.IsDark ? Color.FromArgb(16, 185, 129) : Color.FromArgb(15, 23, 42)
        };

        cardProgress.Controls.Add(_lblStatusText);
        cardProgress.Controls.Add(_prgDeploy);
        cardProgress.Controls.Add(_rtbLog);
        pnlContent.Controls.Add(cardProgress);
    }

    private void ApplyModeSettings()
    {
        switch (_mode)
        {
            case DeploymentMode.WinPeCleanInstall:
                _lblHeaderTitle.Text = "CÀI WIN SẠCH TRONG MÔI TRƯỜNG WINPE (CLEAN INSTALL)";
                _lblHeaderSub.Text = "Môi trường cứu hộ WinPE cho phép định dạng sạch ổ đĩa và áp dụng ảnh Windows chuẩn không bị khóa file.";
                ThemeHelper.StyleBadge(_lblModeBadge, "WinPE Clean Install", BadgeType.Success);
                _chkFormatTarget.Checked = true;
                break;

            case DeploymentMode.Windows7Install:
                _lblHeaderTitle.Text = "CÀI ĐẶT WINDOWS 7 (CLEAN INSTALL & NVME SUPPORT)";
                _lblHeaderSub.Text = "Cài đặt Windows 7 có tích hợp tự động driver ổ cứng NVMe và cổng USB 3.0 cho bo mạch đời mới.";
                ThemeHelper.StyleBadge(_lblModeBadge, "Windows 7 Install", BadgeType.Warning);
                _chkWin7Nvme.Checked = true;
                break;

            case DeploymentMode.Windows7Downgrade:
                _lblHeaderTitle.Text = "HẠ CẤP VỀ WINDOWS 7 (DOWNGRADE ASSISTANT)";
                _lblHeaderSub.Text = "Hướng dẫn & tự động hóa hạ cấp từ Win 10/11 về Win 7: Cần kiểm tra CSM BIOS và nạp driver NVMe/USB3.";
                ThemeHelper.StyleBadge(_lblModeBadge, "Hạ cấp Win 7", BadgeType.Danger);
                _chkWin7Nvme.Checked = true;
                _chkWin7Nvme.Enabled = false; // Bắt buộc bật cho hạ cấp
                Log("[HẠ CẤP WINDOWS 7]: Bo mạch đời mới yêu cầu driver NVMe (KB2990941/KB3087873) và driver USB 3.0 xHCI. Công cụ sẽ tự động nạp vào ảnh sau khi bung.");
                break;

            default:
                _lblHeaderTitle.Text = "CÀI ĐẶT LẠI WINDOWS (DISM APPLY-IMAGE ENGINE)";
                _lblHeaderSub.Text = "Bung file ảnh WIM / ESD / ISO trực tiếp vào phân vùng đĩa, nạp bootloader và inject driver tự động.";
                ThemeHelper.StyleBadge(_lblModeBadge, "Cài đặt lại Windows", BadgeType.Info);
                break;
        }
    }

    private void LoadTargetDrives()
    {
        _cboTargetDrive.Items.Clear();
        var drives = DriveInfo.GetDrives();
        int selectedIdx = -1;

        for (int i = 0; i < drives.Length; i++)
        {
            var d = drives[i];
            if (d.DriveType == DriveType.Fixed && d.IsReady)
            {
                double freeGb = d.TotalFreeSpace / (1024.0 * 1024 * 1024);
                double totalGb = d.TotalSize / (1024.0 * 1024 * 1024);
                string label = string.IsNullOrWhiteSpace(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel;
                string item = $"{d.Name.TrimEnd('\\')} [{label}] - Trống {freeGb:N1} GB / {totalGb:N1} GB ({d.DriveFormat})";
                _cboTargetDrive.Items.Add(item);

                // Auto select C: if available or first drive
                if (d.Name.StartsWith("C:", StringComparison.OrdinalIgnoreCase) && selectedIdx == -1)
                {
                    selectedIdx = _cboTargetDrive.Items.Count - 1;
                }
            }
        }

        if (_cboTargetDrive.Items.Count > 0)
        {
            _cboTargetDrive.SelectedIndex = selectedIdx >= 0 ? selectedIdx : 0;
        }
    }

    private void OnTargetDriveChanged()
    {
        string sel = _cboTargetDrive.SelectedItem?.ToString() ?? "";
        bool isC = sel.StartsWith("C:", StringComparison.OrdinalIgnoreCase);
        bool isWinPe = Environment.GetEnvironmentVariable("WINPE") == "1" || 
                       Directory.Exists(@"X:\Windows") || 
                       Environment.SystemDirectory.StartsWith(@"X:\", StringComparison.OrdinalIgnoreCase);

        if (isC && !isWinPe)
        {
            _lblDriveWarning.Text = "⚠️ CẢNH BÁO: Ổ C: là hệ điều hành đang chạy! Không thể format trực tiếp. Khuyến nghị chạy trong WinPE hoặc chọn phân vùng khác.";
            _lblDriveWarning.ForeColor = Color.FromArgb(225, 29, 72);
            _chkFormatTarget.Checked = false;
            _chkFormatTarget.Enabled = false;
        }
        else
        {
            _lblDriveWarning.Text = "✅ Phân vùng đích hợp lệ và sẵn sàng để bung ảnh hệ điều hành.";
            _lblDriveWarning.ForeColor = ThemeHelper.IsDark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61);
            _chkFormatTarget.Enabled = true;
        }
    }

    private void BrowseImageFile()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Tệp cài đặt Windows (*.wim;*.esd;*.iso)|*.wim;*.esd;*.iso|Tệp WIM (*.wim)|*.wim|Tệp ESD (*.esd)|*.esd|Tệp ISO (*.iso)|*.iso|All files (*.*)|*.*",
            Title = "Chọn tệp ảnh cài đặt Windows (WIM, ESD hoặc ISO)"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _txtImagePath.Text = ofd.FileName;
            _ = InspectImageFileAsync(ofd.FileName);
        }
    }

    private async Task InspectImageFileAsync(string filePath)
    {
        _lblWimStatus.Text = "Đang quét thông tin ảnh...";
        _lblWimStatus.ForeColor = ThemeHelper.PrimaryBlue;
        _cboWimIndex.Items.Clear();

        string actualWimPath = filePath;

        // If ISO, mount it first
        if (filePath.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
        {
            Log($"[ISO]: Phát hiện tệp ISO. Đang mount đĩa ảo để tìm install.wim / install.esd...");
            actualWimPath = await Task.Run(() => MountIsoAndFindWim(filePath)) ?? "";
            if (string.IsNullOrEmpty(actualWimPath))
            {
                _lblWimStatus.Text = "Lỗi: Không tìm thấy file WIM/ESD trong ISO!";
                _lblWimStatus.ForeColor = Color.FromArgb(225, 29, 72);
                Log("[LỖI ISO]: Không tìm thấy thư mục sources\\install.wim hoặc install.esd bên trong tệp ISO.");
                return;
            }
            Log($"[ISO]: Đã tìm thấy tệp cài đặt: {actualWimPath}");
        }

        var (success, editions) = await Task.Run(() => QueryWimEditions(actualWimPath));

        if (success && editions.Count > 0)
        {
            foreach (var ed in editions)
            {
                _cboWimIndex.Items.Add(ed);
            }
            _cboWimIndex.SelectedIndex = 0;
            _lblWimStatus.Text = $"Đã nhận diện {editions.Count} phiên bản";
            _lblWimStatus.ForeColor = ThemeHelper.IsDark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61);
            Log($"✅ Nhận diện thành công {editions.Count} phiên bản trong tệp ảnh.");
        }
        else
        {
            _cboWimIndex.Items.Add("Index 1 (Mặc định)");
            _cboWimIndex.SelectedIndex = 0;
            _lblWimStatus.Text = "Mặc định Index 1";
            _lblWimStatus.ForeColor = ThemeHelper.TextMuted;
        }
    }

    private string? MountIsoAndFindWim(string isoPath)
    {
        try
        {
            CleanupMountedIso();
            _mountedIsoPath = isoPath;

            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"Mount-DiskImage -ImagePath '{isoPath}' -PassThru | Get-Volume | Select-Object -ExpandProperty DriveLetter\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string driveLetter = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(10000);

                if (!string.IsNullOrEmpty(driveLetter))
                {
                    string root = $"{driveLetter}:\\sources";
                    string wim = Path.Combine(root, "install.wim");
                    string esd = Path.Combine(root, "install.esd");
                    if (File.Exists(wim)) return wim;
                    if (File.Exists(esd)) return esd;
                }
            }
        }
        catch { }

        return null;
    }

    private void CleanupMountedIso()
    {
        if (!string.IsNullOrEmpty(_mountedIsoPath))
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"Dismount-DiskImage -ImagePath '{_mountedIsoPath}' -ErrorAction SilentlyContinue\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(5000);
            }
            catch { }
            _mountedIsoPath = null;
        }
    }

    private (bool, List<string>) QueryWimEditions(string wimPath)
    {
        var list = new List<string>();
        try
        {
            var psi = new ProcessStartInfo("dism.exe", $"/Get-WimInfo /WimFile:\"{wimPath}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(10000);

                var matches = Regex.Matches(output, @"Index\s*:\s*(\d+)[\r\n]+Name\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    string idx = m.Groups[1].Value.Trim();
                    string name = m.Groups[2].Value.Trim();
                    list.Add($"Index {idx}: {name}");
                }

                if (list.Count > 0) return (true, list);
            }
        }
        catch { }

        return (false, list);
    }

    private async Task StartDeploymentAsync()
    {
        string rawPath = _txtImagePath.Text.Trim();
        if (string.IsNullOrWhiteSpace(rawPath) || (!File.Exists(rawPath) && !File.Exists(Path.GetFullPath(rawPath))))
        {
            MessageBox.Show("Vui lòng chọn tệp ảnh hệ điều hành (.wim, .esd, hoặc .iso) hợp lệ trước!", "Thiếu Tệp Cài Đặt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cboTargetDrive.SelectedItem == null)
        {
            MessageBox.Show("Vui lòng chọn phân vùng ổ đĩa đích cần cài đặt!", "Thiếu Phân Vùng Đích", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string driveSel = _cboTargetDrive.SelectedItem.ToString() ?? "";
        string targetDrive = driveSel.Substring(0, 2).ToUpper(); // e.g. "C:"

        // Safety check
        bool isOnlineWin = !Environment.SystemDirectory.StartsWith(@"X:\", StringComparison.OrdinalIgnoreCase) && 
                           !Directory.Exists(@"X:\Windows");

        if (targetDrive == "C:" && isOnlineWin && _chkFormatTarget.Checked)
        {
            MessageBox.Show("Không thể Format phân vùng C: vì đây là hệ điều hành Windows bạn đang sử dụng trực tiếp!\n\nVui lòng khởi động vào WinPE để Format sạch ổ C:, hoặc bỏ chọn tùy chọn Format.", "Cảnh Báo An Toàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Parse Index
        int index = 1;
        if (_cboWimIndex.SelectedItem is string selIdx)
        {
            var match = Regex.Match(selIdx, @"Index\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int parsed))
            {
                index = parsed;
            }
        }

        // Confirmation Dialog
        string confirmMsg = $"XÁC NHẬN TRIỂN KHAI CÀI ĐẶT WINDOWS:\n\n" +
                           $"• Tệp nguồn: {Path.GetFileName(rawPath)}\n" +
                           $"• Phiên bản Index: {index}\n" +
                           $"• Phân vùng đích: {targetDrive}\\\n" +
                           $"• Format nhanh phân vùng: {(_chkFormatTarget.Checked ? "CÓ (DỮ LIỆU CŨ SẼ BỊ XÓA)" : "KHÔNG")}\n" +
                           $"• Cấu hình BCD Bootloader: {(_chkBcdBoot.Checked ? "CÓ" : "KHÔNG")}\n" +
                           $"• Nạp Driver máy hiện tại: {(_chkInjectDrivers.Checked ? "CÓ" : "KHÔNG")}\n\n" +
                           $"Bạn có chắc chắn muốn tiến hành cài đặt không?";

        var confirm = MessageBox.Show(confirmMsg, "Xác Nhận Bung Ảnh", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        // Toggle UI
        _btnStart.Enabled = false;
        _btnBrowseImage.Enabled = false;
        _cboWimIndex.Enabled = false;
        _cboTargetDrive.Enabled = false;
        _chkFormatTarget.Enabled = false;
        _chkBcdBoot.Enabled = false;
        _chkInjectDrivers.Enabled = false;
        _chkWin7Nvme.Enabled = false;

        _cts = new CancellationTokenSource();
        _prgDeploy.Value = 0;
        _prgDeploy.Style = ProgressBarStyle.Blocks;
        _lblStatusText.Text = "Đang khởi tạo tiến trình triển khai...";

        try
        {
            string applyWimPath = rawPath;
            if (rawPath.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
            {
                _lblStatusText.Text = "Đang gắn kết tệp ISO...";
                applyWimPath = await Task.Run(() => MountIsoAndFindWim(rawPath)) ?? "";
                if (string.IsNullOrEmpty(applyWimPath))
                {
                    throw new Exception("Không thể tìm thấy tệp install.wim hoặc install.esd trong đĩa ISO.");
                }
            }

            // Step 1: Format target if requested
            if (_chkFormatTarget.Checked)
            {
                _lblStatusText.Text = $"Đang format nhanh phân vùng {targetDrive}...";
                Log($"[BƯỚC 1/4]: Đang format nhanh phân vùng {targetDrive} (NTFS)...");
                await Task.Run(() => FormatTargetPartition(targetDrive));
                Log($"✅ Đã format phân vùng {targetDrive} thành công.");
            }

            // Step 2: DISM Apply Image
            _lblStatusText.Text = $"Đang bung ảnh vào {targetDrive}\\ (DISM Apply-Image)...";
            Log($"[BƯỚC 2/4]: Khởi chạy DISM bung ảnh Index {index} vào {targetDrive}\\...");

            bool applyOk = await Task.Run(() => ExecuteDismApply(applyWimPath, index, targetDrive, _cts.Token));
            if (!applyOk)
            {
                throw new Exception("Quá trình DISM Apply-Image gặp lỗi hoặc bị hủy bỏ.");
            }
            Log($"✅ Đã bung hoàn tất ảnh hệ điều hành vào {targetDrive}\\.");

            // Step 3: Inject Drivers if requested
            if (_chkInjectDrivers.Checked)
            {
                _lblStatusText.Text = "Đang trích xuất & nạp Driver máy vào Windows mới...";
                Log("[BƯỚC 3/4]: Trích xuất Driver máy hiện tại và nạp vào ảnh mới...");
                await Task.Run(() => InjectSystemDrivers(targetDrive));
                Log("✅ Đã hoàn tất nạp Driver vào phân vùng đích.");
            }

            // Special Windows 7 NVMe / USB3 driver injection
            if (_chkWin7Nvme.Checked)
            {
                _lblStatusText.Text = "Đang kiểm tra & tích hợp Driver NVMe / USB 3.0 cho Windows 7...";
                Log("[WIN7 DRIVER]: Đang nạp gói hỗ trợ NVMe & USB 3.0 xHCI vào Windows 7...");
                await Task.Run(() => InjectWin7Drivers(targetDrive));
                Log("✅ Đã nạp driver lưu trữ NVMe và USB 3.0 cho Windows 7.");
            }

            // Step 4: Configure BCD Bootloader
            if (_chkBcdBoot.Checked)
            {
                _lblStatusText.Text = "Đang cấu hình menu khởi động BCD Bootloader...";
                Log("[BƯỚC 4/4]: Thực thi bcdboot cấu hình phân vùng khởi động...");
                await Task.Run(() => ConfigureBcdBoot(targetDrive));
                Log("✅ Đã cấu hình BCD Bootloader thành công.");
            }

            _prgDeploy.Value = 100;
            _lblStatusText.Text = "🎉 CHÚC MỪNG: Triển khai cài đặt Windows thành công!";
            _lblStatusText.ForeColor = ThemeHelper.IsDark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61);
            Log("=================================================");
            Log("🎉 QUY TRÌNH TRIỂN KHAI HOÀN TẤT XUẤT SẮC!");
            Log($"Hệ điều hành mới đã sẵn sàng tại phân vùng {targetDrive}\\.");
            Log("=================================================");

            MessageBox.Show($"Chúc mừng! Đã hoàn tất cài đặt Windows vào phân vùng {targetDrive}\\\n\nBạn có thể khởi động lại máy để hoàn tất các bước thiết lập tài khoản ban đầu.", "Cài Đặt Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (_chkRestartAfter.Checked)
            {
                Process.Start("shutdown.exe", "/r /t 10 /c \"He thong se khoi dong lai sau 10 giay de hoan tat cai dat Windows.\"");
            }
        }
        catch (Exception ex)
        {
            _lblStatusText.Text = "❌ Quá trình cài đặt gặp lỗi!";
            _lblStatusText.ForeColor = Color.FromArgb(225, 29, 72);
            Log($"[LỖI]: {ex.Message}");
            MessageBox.Show($"Có lỗi trong quá trình cài đặt:\n{ex.Message}", "Lỗi Cài Đặt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            CleanupMountedIso();
            _btnStart.Enabled = true;
            _btnBrowseImage.Enabled = true;
            _cboWimIndex.Enabled = true;
            _cboTargetDrive.Enabled = true;
            _chkFormatTarget.Enabled = true;
            _chkBcdBoot.Enabled = true;
            _chkInjectDrivers.Enabled = true;
            _chkWin7Nvme.Enabled = true;
            _cts = null;
        }
    }

    private void FormatTargetPartition(string drive)
    {
        var psi = new ProcessStartInfo("cmd.exe", $"/c format {drive} /FS:NTFS /V:Windows /Q /Y")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        using var p = Process.Start(psi);
        p?.WaitForExit(30000);
    }

    private bool ExecuteDismApply(string wimPath, int index, string targetDrive, CancellationToken ct)
    {
        string args = $"/Apply-Image /ImageFile:\"{wimPath}\" /Index:{index} /ApplyDir:{targetDrive}\\";
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
            string line = e.Data.Trim();

            var match = Regex.Match(line, @"\[\s*(\d+(\.\d+)?)%\s*\]");
            if (match.Success && double.TryParse(match.Groups[1].Value, out double pct))
            {
                int val = Math.Clamp((int)pct, 0, 100);
                Invoke(() =>
                {
                    _prgDeploy.Value = val;
                    _lblStatusText.Text = $"Đang bung ảnh vào {targetDrive}\\ ({val}%)...";
                });
                Log($"[DISM Tiến độ]: {val}%");
            }
            else if (line.Contains("Đang", StringComparison.OrdinalIgnoreCase) || 
                     line.Contains("Tiến độ", StringComparison.OrdinalIgnoreCase) || 
                     line.Contains("Applying", StringComparison.OrdinalIgnoreCase))
            {
                Log($"  {line}");
            }
        };

        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) Log($"[DISM Cảnh báo]: {e.Data.Trim()}");
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        while (!proc.HasExited)
        {
            if (ct.IsCancellationRequested)
            {
                try { proc.Kill(); } catch { }
                return false;
            }
            Thread.Sleep(200);
        }

        return proc.ExitCode == 0;
    }

    private void InjectSystemDrivers(string targetDrive)
    {
        string tempDriverDir = Path.Combine(targetDrive + "\\", "TempExportDrivers");
        try
        {
            Directory.CreateDirectory(tempDriverDir);

            // Export from online
            var exportPsi = new ProcessStartInfo("dism.exe", $"/Online /Export-Driver /Destination:\"{tempDriverDir}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var pExp = Process.Start(exportPsi);
            pExp?.WaitForExit(60000);

            // Inject into target
            var injectPsi = new ProcessStartInfo("dism.exe", $"/Image:{targetDrive}\\ /Add-Driver /Driver:\"{tempDriverDir}\" /Recurse")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var pInj = Process.Start(injectPsi);
            pInj?.WaitForExit(90000);
        }
        catch { }
        finally
        {
            try { if (Directory.Exists(tempDriverDir)) Directory.Delete(tempDriverDir, true); } catch { }
        }
    }

    private void InjectWin7Drivers(string targetDrive)
    {
        try
        {
            // Search standard driver paths
            var searchDirs = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Drivers"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Drivers"),
                @"C:\AnhdvBootTool\Drivers",
                @"D:\AnhdvBootTool\Drivers"
            };

            foreach (var dir in searchDirs)
            {
                if (Directory.Exists(dir))
                {
                    var psi = new ProcessStartInfo("dism.exe", $"/Image:{targetDrive}\\ /Add-Driver /Driver:\"{dir}\" /Recurse")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(60000);
                    Log($"[WIN7 DRIVER]: Đã quét và nạp driver từ: {dir}");
                }
            }
        }
        catch { }
    }

    private void ConfigureBcdBoot(string targetDrive)
    {
        try
        {
            var psi = new ProcessStartInfo("bcdboot.exe", $"{targetDrive}\\Windows /l vi-VN")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(15000);
        }
        catch { }
    }

    private void Log(string msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(msg));
            return;
        }

        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
        _rtbLog.AppendText(line);
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.ScrollToCaret();
    }
}
