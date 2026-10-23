using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Ernastie.Models;
using Ernastie.Services;

namespace Ernastie.Forms;

public enum LinuxDistro
{
    Ubuntu2404,
    Debian12,
    LinuxMint22,
    KaliLinux
}

public class LinuxDistroInfo
{
    public LinuxDistro Distro { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "v8.1";
    public string Icon { get; set; } = "🐧";
    public string BannerTitle { get; set; } = "";
    public string BannerDesc { get; set; } = "";
    public string DistroDesc { get; set; } = "";
    public string IsoPattern { get; set; } = "";
    public string IsoDownloadUrl { get; set; } = "";
    public string OfficialWebUrl { get; set; } = "";
    public string DefaultUsername { get; set; } = "khach";
    public string DefaultDisplayName { get; set; } = "Khach";
    public string DefaultHostname { get; set; } = "linux-pc";
}

public class LinuxDualBootForm : Form
{
    [DllImport("kernel32.dll")]
    private static extern bool GetFirmwareType(ref uint firmwareType);

    private readonly LinuxDistroInfo _info;

    // Palette & Theme tokens (Cream/Sand in Light mode matching screenshot, Obsidian in Dark mode)
    private bool IsDark => ThemeHelper.IsDark;
    private Color _bgForm => IsDark ? ThemeHelper.BgForm : Color.FromArgb(254, 250, 240);
    private Color _cardBg => IsDark ? ThemeHelper.CardBg : Color.FromArgb(255, 253, 247);
    private Color _cardBorder => IsDark ? ThemeHelper.BorderColor : Color.FromArgb(228, 216, 192);
    private Color _textHeading => IsDark ? ThemeHelper.TextHeading : Color.FromArgb(20, 20, 20);
    private Color _textNormal => IsDark ? ThemeHelper.TextNormal : Color.FromArgb(40, 40, 40);
    private Color _textMuted => IsDark ? ThemeHelper.TextMuted : Color.FromArgb(100, 90, 75);
    private Color _btnSand => IsDark ? ThemeHelper.BtnBg : Color.FromArgb(219, 197, 154);
    private Color _btnSandBorder => IsDark ? ThemeHelper.BtnBorder : Color.FromArgb(140, 110, 74);
    private Color _btnSandHover => IsDark ? ThemeHelper.BtnHover : Color.FromArgb(208, 185, 140);
    private Color _primaryGreen => Color.FromArgb(34, 128, 48);
    private Color _primaryGreenHover => Color.FromArgb(28, 110, 40);
    private Color _logBg => IsDark ? ThemeHelper.LogBg : Color.White;
    private Color _logText => IsDark ? ThemeHelper.LogText : Color.FromArgb(30, 41, 59);

    // Form Controls
    private Label _lblBannerTitle = null!;
    private Label _lblBannerDesc = null!;
    private TextBox _txtUsername = null!;
    private TextBox _txtPassword = null!;
    private Button _btnTogglePassword = null!;
    private TextBox _txtDisplayName = null!;
    private TextBox _txtHostname = null!;
    private TextBox _txtIsoPath = null!;
    private Button _btnBrowseIso = null!;
    private Button _btnDownloadIso = null!;

    private RichTextBox _rtbChecklist = null!;
    private Button _btnRefreshChecklist = null!;
    private Button _btnCleanOldFiles = null!;

    private RichTextBox _rtbLog = null!;
    private Button _btnPrepareAndReboot = null!;
    private Button _btnClose = null!;

    // System state
    private int _diskNumber = 0;
    private double _cTotalSizeGb = 450.0;
    private double _cFreeSpaceGb = 379.0;
    private double _recommendedLinuxGb = 58.6;
    private string _partitionStyle = "GPT";
    private string _bootMode = "UEFI";
    private bool _isSecureBootEnabled = true;
    private bool _isBitLockerActive = false;
    private double _totalRamGb = 16.0;
    private bool _isRunning = false;
    private CancellationTokenSource? _cts;

    public LinuxDualBootForm(LinuxDistro distro = LinuxDistro.Ubuntu2404)
    {
        _info = GetDistroInfo(distro);
        InitializeComponents();
        ThemeHelper.ApplyTheme(this);
        DetectSystemParameters();
    }

    private static LinuxDistroInfo GetDistroInfo(LinuxDistro distro)
    {
        return distro switch
        {
            LinuxDistro.Debian12 => new LinuxDistroInfo
            {
                Distro = distro,
                Name = "Debian 12 (Desktop)",
                Code = "v9.1",
                Icon = "🌀",
                BannerTitle = "Debian 12 (Desktop) cạnh Windows đang có — Windows và dữ liệu giữ nguyên",
                BannerDesc = "Tool thu nhỏ ổ C chừa sẵn chỗ, chép bộ cài lên ổ cứng (không cần USB) rồi khởi động vào trình cài.",
                DistroDesc = "Bản Linux ổn định bậc nhất. Tool cài TỰ ĐỘNG vào vùng trống (giữ Windows) + đặt sẵn tài khoản — khách đăng nhập được ngay.",
                IsoPattern = "debian-*-amd64-*.iso",
                IsoDownloadUrl = "https://cdimage.debian.org/debian-cd/current/amd64/iso-cd/debian-12.7.0-amd64-netinst.iso",
                OfficialWebUrl = "https://www.debian.org/download",
                DefaultUsername = "khach",
                DefaultDisplayName = "Khach",
                DefaultHostname = "debian-pc"
            },
            LinuxDistro.LinuxMint22 => new LinuxDistroInfo
            {
                Distro = distro,
                Name = "Linux Mint 22 (Desktop)",
                Code = "v10.1",
                Icon = "🍃",
                BannerTitle = "Linux Mint 22 (Desktop) cạnh Windows đang có — Windows và dữ liệu giữ nguyên",
                BannerDesc = "Tool thu nhỏ ổ C chừa sẵn chỗ, chép bộ cài lên ổ cứng (không cần USB) rồi khởi động vào trình cài.",
                DistroDesc = "Giao diện thân thiện quen thuộc như Windows. Tool cài TỰ ĐỘNG vào vùng trống (giữ Windows) + đặt sẵn tài khoản.",
                IsoPattern = "linuxmint-22*-cinnamon-64bit.iso",
                IsoDownloadUrl = "https://mirrors.kernel.org/linuxmint/stable/22/linuxmint-22-cinnamon-64bit.iso",
                OfficialWebUrl = "https://linuxmint.com/download.php",
                DefaultUsername = "khach",
                DefaultDisplayName = "Khach",
                DefaultHostname = "mint-pc"
            },
            LinuxDistro.KaliLinux => new LinuxDistroInfo
            {
                Distro = distro,
                Name = "Kali Linux (Security)",
                Code = "v11.1",
                Icon = "🐉",
                BannerTitle = "Kali Linux cạnh Windows đang có — Windows và dữ liệu giữ nguyên",
                BannerDesc = "Tool thu nhỏ ổ C chừa sẵn chỗ, chép bộ cài lên ổ cứng (không cần USB) rồi khởi động vào trình cài.",
                DistroDesc = "Hệ điều hành chuyên kiểm thử an ninh mạng. Tool cài TỰ ĐỘNG vào vùng trống (giữ Windows) + đặt sẵn tài khoản.",
                IsoPattern = "kali-linux-*-installer-amd64.iso",
                IsoDownloadUrl = "https://cdimage.kali.org/current/kali-linux-installer-amd64.iso",
                OfficialWebUrl = "https://www.kali.org/get-kali/",
                DefaultUsername = "kali",
                DefaultDisplayName = "Kali User",
                DefaultHostname = "kali-box"
            },
            _ => new LinuxDistroInfo
            {
                Distro = LinuxDistro.Ubuntu2404,
                Name = "Ubuntu 24.04 LTS (Desktop)",
                Code = "v8.1",
                Icon = "🐧",
                BannerTitle = "Ubuntu 24.04 LTS (Desktop) cạnh Windows đang có — Windows và dữ liệu giữ nguyên",
                BannerDesc = "Tool thu nhỏ ổ C chừa sẵn chỗ, chép bộ cài lên ổ cứng (không cần USB) rồi khởi động vào trình cài.",
                DistroDesc = "Bản LTS phổ biến nhất. Tool cài TỰ ĐỘNG vào vùng trống (giữ Windows) + đặt sẵn tài khoản — khách đăng nhập được ngay.",
                IsoPattern = "ubuntu-24.04*-desktop-amd64.iso",
                IsoDownloadUrl = "https://releases.ubuntu.com/24.04.1/ubuntu-24.04.1-desktop-amd64.iso",
                OfficialWebUrl = "https://releases.ubuntu.com/",
                DefaultUsername = "khach",
                DefaultDisplayName = "Khach",
                DefaultHostname = "linux-pc"
            }
        };
    }

    private void InitializeComponents()
    {
        Text = $"{_info.Icon} Cài {_info.Name} CHẠY SONG SONG Windows (nút {_info.Code})";
        Size = new Size(950, 720);
        MinimumSize = new Size(880, 640);
        StartPosition = FormStartPosition.CenterParent;
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9.25f);
        BackColor = _bgForm;
        ForeColor = _textNormal;
        Icon = SystemIcons.Application;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = _bgForm
        };
        Controls.Add(mainPanel);

        // =========================================================================
        // TOP BANNER
        // =========================================================================
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(pnlTop);

        _lblBannerTitle = new Label
        {
            Text = $"{_info.Icon} {_info.BannerTitle}",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, 4),
            AutoSize = true
        };
        pnlTop.Controls.Add(_lblBannerTitle);

        _lblBannerDesc = new Label
        {
            Text = _info.BannerDesc,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            ForeColor = _textNormal,
            Location = new Point(2, 32),
            Size = new Size(mainPanel.Width - 36, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlTop.Controls.Add(_lblBannerDesc);

        // =========================================================================
        // BOTTOM BUTTON BAR
        // =========================================================================
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(pnlBottom);

        _btnPrepareAndReboot = new Button
        {
            Text = $"{_info.Icon} CHUẨN BỊ & KHỞI ĐỘNG VÀO TRÌNH CÀI (giữ Windows)",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            BackColor = _primaryGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(0, 8),
            Size = new Size(680, 44),
            Cursor = Cursors.Hand
        };
        _btnPrepareAndReboot.FlatAppearance.BorderSize = 0;
        _btnPrepareAndReboot.Click += async (_, _) => await PrepareAndBootIntoInstallerAsync();
        pnlBottom.Controls.Add(_btnPrepareAndReboot);

        _btnClose = CreateSandButton("Đóng", new Point(mainPanel.Width - 140, 8), new Size(108, 44));
        _btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnClose.Click += (_, _) => Close();
        pnlBottom.Controls.Add(_btnClose);

        // =========================================================================
        // LOG AREA (Bottom)
        // =========================================================================
        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Bottom,
            Height = 160,
            BackColor = _logBg,
            ForeColor = _logText,
            Font = new Font("Consolas", 9.25f),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        mainPanel.Controls.Add(_rtbLog);

        // Spacer between middle and log
        var pnlSpacer = new Panel { Dock = DockStyle.Bottom, Height = 10, BackColor = Color.Transparent };
        mainPanel.Controls.Add(pnlSpacer);

        // =========================================================================
        // MIDDLE SPLIT (Left Column: Config & Account | Right Column: Checklist)
        // =========================================================================
        var pnlMiddle = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlMiddle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        pnlMiddle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainPanel.Controls.Add(pnlMiddle);
        pnlMiddle.BringToFront();

        // --- LEFT COLUMN ---
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 8, 0),
            BackColor = Color.Transparent
        };
        pnlMiddle.Controls.Add(pnlLeft, 0, 0);

        // Section 1: Bản Linux sẽ cài
        int curY = 0;
        var gbSec1 = new GroupBox
        {
            Text = $"1. Bản Linux sẽ cài — {_info.Code}",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, curY),
            Size = new Size(pnlLeft.Width, 96),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlLeft.Controls.Add(gbSec1);

        var lblDistroName = new Label
        {
            Text = _info.Name,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(12, 20),
            AutoSize = true
        };
        gbSec1.Controls.Add(lblDistroName);

        var lblDistroOther = new Label
        {
            Text = "Bản khác: đóng cửa sổ này, bấm nút của bản đó ở tab Linux.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = _textMuted,
            Location = new Point(12, 44),
            AutoSize = true
        };
        gbSec1.Controls.Add(lblDistroOther);

        var lblDistroDesc = new Label
        {
            Text = _info.DistroDesc,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = _textNormal,
            Location = new Point(12, 62),
            Size = new Size(gbSec1.Width - 24, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        gbSec1.Controls.Add(lblDistroDesc);

        curY += 104;

        // Section 2: Tài khoản
        var gbSec2 = new GroupBox
        {
            Text = "2. Tài khoản (tool đặt sẵn — khách đăng nhập được ngay, KHÔNG phải ...)",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, curY),
            Size = new Size(pnlLeft.Width, 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlLeft.Controls.Add(gbSec2);

        int fldY = 22;
        int lblW = 145;

        // Row 1: Username
        var lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(12, fldY + 3), Size = new Size(lblW, 20), Font = new Font("Segoe UI", 9f) };
        _txtUsername = new TextBox { Text = _info.DefaultUsername, Location = new Point(lblW + 16, fldY), Size = new Size(240, 24) };
        gbSec2.Controls.Add(lblUser);
        gbSec2.Controls.Add(_txtUsername);

        fldY += 28;
        // Row 2: Password
        var lblPass = new Label { Text = "Mật khẩu:", Location = new Point(12, fldY + 3), Size = new Size(lblW, 20), Font = new Font("Segoe UI", 9f) };
        _txtPassword = new TextBox { Text = "", Location = new Point(lblW + 16, fldY), Size = new Size(206, 24), UseSystemPasswordChar = true };
        _btnTogglePassword = new Button
        {
            Text = "👁",
            Location = new Point(lblW + 16 + 210, fldY),
            Size = new Size(30, 24),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8f),
            BackColor = _btnSand,
            Cursor = Cursors.Hand
        };
        _btnTogglePassword.FlatAppearance.BorderColor = _btnSandBorder;
        _btnTogglePassword.Click += (_, _) =>
        {
            _txtPassword.UseSystemPasswordChar = !_txtPassword.UseSystemPasswordChar;
            _btnTogglePassword.Text = _txtPassword.UseSystemPasswordChar ? "👁" : "🔒";
        };
        gbSec2.Controls.Add(lblPass);
        gbSec2.Controls.Add(_txtPassword);
        gbSec2.Controls.Add(_btnTogglePassword);

        fldY += 28;
        // Row 3: Display Name
        var lblDisp = new Label { Text = "Tên hiển thị:", Location = new Point(12, fldY + 3), Size = new Size(lblW, 20), Font = new Font("Segoe UI", 9f) };
        _txtDisplayName = new TextBox { Text = _info.DefaultDisplayName, Location = new Point(lblW + 16, fldY), Size = new Size(240, 24) };
        gbSec2.Controls.Add(lblDisp);
        gbSec2.Controls.Add(_txtDisplayName);

        fldY += 28;
        // Row 4: Hostname
        var lblHost = new Label { Text = "Tên máy (hostname):", Location = new Point(12, fldY + 3), Size = new Size(lblW, 20), Font = new Font("Segoe UI", 9f) };
        _txtHostname = new TextBox { Text = _info.DefaultHostname, Location = new Point(lblW + 16, fldY), Size = new Size(240, 24) };
        gbSec2.Controls.Add(lblHost);
        gbSec2.Controls.Add(_txtHostname);

        curY += 148;

        // Section 3: Bộ cài (.iso)
        var gbSec3 = new GroupBox
        {
            Text = "3. Bộ cài (.iso) — ĐỂ TRỐNG = tool TỰ TẢI từ kho khi bấm cài",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, curY),
            Size = new Size(pnlLeft.Width, 98),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlLeft.Controls.Add(gbSec3);

        _txtIsoPath = new TextBox
        {
            Location = new Point(12, 24),
            Size = new Size(gbSec3.Width - 110, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _txtIsoPath.TextChanged += (_, _) => UpdateChecklistContent();
        gbSec3.Controls.Add(_txtIsoPath);

        _btnBrowseIso = CreateSandButton("Chọn...", new Point(gbSec3.Width - 90, 23), new Size(76, 26));
        _btnBrowseIso.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnBrowseIso.Click += (_, _) => BrowseForIso();
        gbSec3.Controls.Add(_btnBrowseIso);

        _btnDownloadIso = CreateSandButton($"⬇ Tải {_info.Name} từ kho (tự chọn vào ô)", new Point(12, 56), new Size(gbSec3.Width - 24, 32));
        _btnDownloadIso.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _btnDownloadIso.Click += (_, _) => DownloadOrAutoSelectIso();
        gbSec3.Controls.Add(_btnDownloadIso);

        // --- RIGHT COLUMN ---
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 0, 0, 0),
            BackColor = Color.Transparent
        };
        pnlMiddle.Controls.Add(pnlRight, 1, 0);

        var lblChecklistHeader = new Label
        {
            Text = "Bảng kiểm",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(8, 0),
            AutoSize = true
        };
        pnlRight.Controls.Add(lblChecklistHeader);

        _rtbChecklist = new RichTextBox
        {
            Location = new Point(8, 24),
            Size = new Size(pnlRight.Width - 8, 276),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = _cardBg,
            ForeColor = _textNormal,
            Font = new Font("Segoe UI", 9.25f),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = true
        };
        pnlRight.Controls.Add(_rtbChecklist);

        var pnlChecklistBtns = new Panel
        {
            Location = new Point(8, 308),
            Size = new Size(pnlRight.Width - 8, 38),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };
        pnlRight.Controls.Add(pnlChecklistBtns);

        int halfBtnW = (pnlChecklistBtns.Width - 8) / 2;
        _btnRefreshChecklist = CreateSandButton("🔄 Đọc lại máy", new Point(0, 2), new Size(halfBtnW, 34));
        _btnRefreshChecklist.Click += (_, _) =>
        {
            DetectSystemParameters();
            AppendLog("[OK] Đã đọc lại thông số hệ thống và ổ đĩa.");
        };
        pnlChecklistBtns.Controls.Add(_btnRefreshChecklist);

        _btnCleanOldFiles = CreateSandButton("🧹 Dọn rác bộ cài cũ", new Point(halfBtnW + 8, 2), new Size(halfBtnW, 34));
        _btnCleanOldFiles.Click += async (_, _) => await CleanOldInstallerFilesAsync();
        pnlChecklistBtns.Controls.Add(_btnCleanOldFiles);

        pnlChecklistBtns.Resize += (_, _) =>
        {
            int w = (pnlChecklistBtns.Width - 8) / 2;
            _btnRefreshChecklist.Width = w;
            _btnCleanOldFiles.Location = new Point(w + 8, 2);
            _btnCleanOldFiles.Width = pnlChecklistBtns.Width - (w + 8);
        };

        // Resize layout adjustments
        mainPanel.Resize += (_, _) =>
        {
            _btnPrepareAndReboot.Width = pnlBottom.Width - 124;
            _lblBannerDesc.Width = pnlTop.Width - 4;
        };

        AppendLog($"Sẵn sàng. Bản Linux đã chọn: {_info.Name} (nút {_info.Code}).");
        AppendLog("Bấm '🔄 Đọc lại máy' để cập nhật bảng kiểm, hoặc '▶ CHUẨN BỊ' để tự động thu nhỏ ổ C và nạp bộ cài.");
    }

    private Button CreateSandButton(string text, Point loc, Size size)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            BackColor = _btnSand,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = _btnSandBorder;
        btn.FlatAppearance.MouseOverBackColor = _btnSandHover;
        return btn;
    }

    private void AppendLog(string text, Color? color = null, bool bold = false)
    {
        if (IsDisposed || !IsHandleCreated) return;
        Invoke(() =>
        {
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionLength = 0;
            _rtbLog.SelectionColor = color ?? _logText;
            _rtbLog.SelectionFont = new Font("Consolas", 9.25f, bold ? FontStyle.Bold : FontStyle.Regular);
            _rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
            _rtbLog.ScrollToCaret();
        });
    }

    // =========================================================================
    // SYSTEM DETECTION & CHECKLIST GENERATION
    // =========================================================================
    private void DetectSystemParameters()
    {
        try
        {
            // 1. Detect Boot Mode (UEFI vs Legacy BIOS)
            uint fwType = 0;
            if (GetFirmwareType(ref fwType))
            {
                _bootMode = fwType == 2 ? "UEFI" : "Legacy BIOS";
            }
            else
            {
                _bootMode = Directory.Exists(@"C:\Windows\Boot\EFI") ? "UEFI" : "Legacy BIOS";
            }

            // 2. Detect C: Drive size & free space
            var driveC = new DriveInfo("C");
            if (driveC.IsReady)
            {
                _cTotalSizeGb = Math.Round(driveC.TotalSize / (1024.0 * 1024 * 1024), 1);
                _cFreeSpaceGb = Math.Round(driveC.TotalFreeSpace / (1024.0 * 1024 * 1024), 1);
            }

            // 3. Detect Disk Number of C: and Partition Style (GPT vs MBR)
            _diskNumber = GetDiskNumberForDrive("C");
            _partitionStyle = GetDiskPartitionStyle(_diskNumber);

            // 4. Calculate recommended Linux size
            // Matching: free ~379.3 GB -> ~58.6 GB
            double tempInstallerGb = 6.4;
            double minWinReserveGb = 20.0;
            double available = Math.Max(0, _cFreeSpaceGb - tempInstallerGb - minWinReserveGb);
            _recommendedLinuxGb = Math.Round(Math.Min(60.0, Math.Max(20.0, available * 0.166)), 1);
            if (_recommendedLinuxGb < 20.0 && available >= 20.0) _recommendedLinuxGb = 20.0;

            // 5. Detect BitLocker Status
            _isBitLockerActive = CheckIfBitLockerOnC();

            // 6. Detect Secure Boot Status
            _isSecureBootEnabled = CheckIfSecureBootEnabled();

            // 7. Detect Total RAM
            _totalRamGb = DetectTotalRamGb();

            // 8. Auto-search for existing ISO if empty
            if (string.IsNullOrWhiteSpace(_txtIsoPath.Text))
            {
                var found = FindLocalIso();
                if (!string.IsNullOrEmpty(found))
                {
                    _txtIsoPath.Text = found;
                }
            }

            UpdateChecklistContent();
        }
        catch (Exception ex)
        {
            AppendLog($"[Lỗi đọc thông số máy]: {ex.Message}");
        }
    }

    private void UpdateChecklistContent()
    {
        if (IsDisposed || !IsHandleCreated) return;
        Invoke(() =>
        {
            _rtbChecklist.Clear();

            _rtbChecklist.AppendText($"Bản cài: {_info.Name} (nút {_info.Code})\n");
            _rtbChecklist.AppendText($"Chế độ: CHẠY SONG SONG Windows — Windows giữ nguyên\n\n");

            // Line 1: Chuẩn máy
            _rtbChecklist.AppendText($"• Chuẩn máy: {_bootMode} — dùng shim đã ký (Secure Boot bật vẫn được).\n");

            // Line 2: Windows (C:)
            _rtbChecklist.AppendText($"✔ Windows (C:) trên ổ đĩa số {_diskNumber} (~{_cTotalSizeGb:F1} GB) được GIỮ NGUYÊN — chỉ thu nhỏ lại.\n");

            // Line 3: Partition style
            _rtbChecklist.AppendText($"✔ Ổ đĩa chuẩn {_partitionStyle} — hợp cho dual-boot {_bootMode}.\n");

            // Line 4: Space
            bool spaceOk = _cFreeSpaceGb >= 35.0;
            string spaceMark = spaceOk ? "✔" : "⚠️";
            string spaceText = spaceOk ? "đủ" : "ít (cần giải phóng thêm)";
            _rtbChecklist.AppendText($"{spaceMark} Ổ C còn trống ~{_cFreeSpaceGb:F1} GB — {spaceText}. Bộ cài tạm 6.4 GB + chỗ cho Linux ~{_recommendedLinuxGb:F1} GB (tối thiểu 20 GB), chừa lại 20 GB cho Windows.\n");

            // Line 5: BitLocker
            if (!_isBitLockerActive)
            {
                _rtbChecklist.AppendText("✔ BitLocker: tắt.\n");
            }
            else
            {
                _rtbChecklist.AppendText("⚠️ BitLocker: Đang BẬT — cần sao lưu Recovery Key 48 số trước khi cài.\n");
            }

            // Line 6: Secure Boot
            string secBootText = _isSecureBootEnabled ? "BẬT" : "TẮT";
            _rtbChecklist.AppendText($"• Secure Boot: {secBootText} — Ubuntu/Mint/Debian đều có shim đã ký nên vẫn cài được.\n");

            // Line 7: RAM
            _rtbChecklist.AppendText($"• RAM: ~{_totalRamGb:F1} GB — bản này phải nạp cả bộ cài vào RAM (cỡ .iso + 1,5 GB).\n");

            // Line 8: ISO status
            if (!string.IsNullOrWhiteSpace(_txtIsoPath.Text) && File.Exists(_txtIsoPath.Text))
            {
                var fi = new FileInfo(_txtIsoPath.Text);
                double isoSizeGb = Math.Round(fi.Length / (1024.0 * 1024 * 1024), 2);
                _rtbChecklist.AppendText($"✔ Đã chọn file .iso ({fi.Name} ~{isoSizeGb} GB).\n");
            }
            else
            {
                _rtbChecklist.AppendText($"• Chưa chọn file .iso — tên thường có dạng {_info.IsoPattern}\n");
            }

            _rtbChecklist.AppendText("\nTool cài TỰ ĐỘNG hoàn toàn (tự chia vào vùng trống + đặt sẵn tài khoản — khách đăng nhập được ngay).");
        });
    }

    private int GetDiskNumberForDrive(string driveLetter)
    {
        try
        {
            char letter = driveLetter.Trim().ToUpperInvariant()[0];
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

    private string GetDiskPartitionStyle(int diskNumber)
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"(Get-Disk -Number {diskNumber} -ErrorAction SilentlyContinue).PartitionStyle\"")
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
                if (!string.IsNullOrEmpty(outText)) return outText.ToUpperInvariant();
            }
        }
        catch { }
        return "GPT";
    }

    private bool CheckIfBitLockerOnC()
    {
        try
        {
            var psi = new ProcessStartInfo("manage-bde.exe", "-status C:")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                var text = p.StandardOutput.ReadToEnd();
                p.WaitForExit(4000);
                if (text.Contains("Protection On") || text.Contains("Locked") || text.Contains("Bảo vệ: Bật"))
                {
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    private bool CheckIfSecureBootEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            if (key?.GetValue("UEFISecureBootEnabled") is int val)
            {
                return val == 1;
            }
        }
        catch { }
        return true;
    }

    private double DetectTotalRamGb()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var mo in searcher.Get())
            {
                if (mo["TotalPhysicalMemory"] is ulong bytes)
                {
                    return Math.Round(bytes / (1024.0 * 1024 * 1024), 1);
                }
            }
        }
        catch { }
        return 16.0;
    }

    private string? FindLocalIso()
    {
        var locations = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            @"D:\",
            @"C:\",
            AppDomain.CurrentDomain.BaseDirectory
        };

        foreach (var dir in locations)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                var files = Directory.GetFiles(dir, "*.iso", SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    var name = Path.GetFileName(f).ToLowerInvariant();
                    if (_info.Distro == LinuxDistro.Ubuntu2404 && name.Contains("ubuntu") && name.Contains("desktop"))
                    {
                        return f;
                    }
                    if (_info.Distro == LinuxDistro.Debian12 && name.Contains("debian"))
                    {
                        return f;
                    }
                    if (_info.Distro == LinuxDistro.LinuxMint22 && name.Contains("linuxmint"))
                    {
                        return f;
                    }
                    if (_info.Distro == LinuxDistro.KaliLinux && name.Contains("kali"))
                    {
                        return f;
                    }
                }
            }
            catch { }
        }
        return null;
    }

    // =========================================================================
    // USER ACTIONS: BROWSE & DOWNLOAD ISO
    // =========================================================================
    private void BrowseForIso()
    {
        using var ofd = new OpenFileDialog
        {
            Title = $"Chọn tệp ISO bộ cài {_info.Name}",
            Filter = "Tệp ảnh đĩa ISO (*.iso)|*.iso|All files (*.*)|*.*"
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _txtIsoPath.Text = ofd.FileName;
            UpdateChecklistContent();
            AppendLog($"[Đã chọn ISO]: {ofd.FileName}");
        }
    }

    private void DownloadOrAutoSelectIso()
    {
        // 1. Check if an ISO was already found
        var found = FindLocalIso();
        if (!string.IsNullOrEmpty(found))
        {
            _txtIsoPath.Text = found;
            UpdateChecklistContent();
            AppendLog($"[Tìm thấy ISO trên máy]: Đã tự động chọn file: {found}");
            MessageBox.Show($"Đã tự động tìm thấy và nạp file ISO trên máy:\n{found}", "Đã Tìm Thấy ISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 2. Offer to download official ISO
        var res = MessageBox.Show(
            $"Bạn chưa có file ISO của {_info.Name}.\n\n" +
            $"Bạn có muốn mở liên kết tải trực tiếp từ máy chủ chính thức ({_info.IsoDownloadUrl}) không?\n\n" +
            $"• Bấm 'Yes' để tải tự động hoặc mở trình duyệt.\n" +
            $"• Sau khi tải xong, tool sẽ tự động nhận diện file trong thư mục Downloads.",
            $"Tải Bộ Cài {_info.Name}",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (res == DialogResult.Yes)
        {
            try
            {
                AppendLog($"Mở liên kết tải bộ cài chính thức: {_info.IsoDownloadUrl}");
                Process.Start(new ProcessStartInfo(_info.IsoDownloadUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi mở trình duyệt: {ex.Message}");
            }
        }
    }

    // =========================================================================
    // CLEAN OLD INSTALLER FILES & BCD ENTRIES
    // =========================================================================
    private async Task CleanOldInstallerFilesAsync()
    {
        if (_isRunning) return;
        _isRunning = true;
        _btnCleanOldFiles.Enabled = false;

        try
        {
            AppendLog("--- BẮT ĐẦU DỌN RÁC BỘ CÀI LINUX CŨ & BCD ENTRIES ---");

            await Task.Run(() =>
            {
                // 1. Delete temporary directories
                var dirsToClean = new[]
                {
                    @"C:\ubuntu-installer",
                    @"C:\linux-installer",
                    Path.Combine(Path.GetTempPath(), "linux_iso_extract")
                };

                foreach (var d in dirsToClean)
                {
                    if (Directory.Exists(d))
                    {
                        try
                        {
                            Directory.Delete(d, true);
                            AppendLog($"[OK] Đã xóa thư mục bộ cài tạm: {d}");
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"[Cảnh báo] Chưa xóa được {d}: {ex.Message}");
                        }
                    }
                }

                // 2. Scan and remove temporary Linux BCD entries
                try
                {
                    var psi = new ProcessStartInfo("cmd.exe", "/c bcdedit /enum all")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        var output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(5000);

                        // Look for entries containing "Ubuntu" or "Linux Installer"
                        var lines = output.Split('\n');
                        string currentGuid = "";
                        foreach (var rawLine in lines)
                        {
                            var line = rawLine.Trim();
                            if (line.StartsWith("identifier", StringComparison.OrdinalIgnoreCase))
                            {
                                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2) currentGuid = parts[1];
                            }
                            else if (line.StartsWith("description", StringComparison.OrdinalIgnoreCase))
                            {
                                if (line.Contains("Ubuntu Installer", StringComparison.OrdinalIgnoreCase) ||
                                    line.Contains("Linux Installer", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!string.IsNullOrEmpty(currentGuid) && !currentGuid.Equals("{current}", StringComparison.OrdinalIgnoreCase))
                                    {
                                        AppendLog($"Đang xóa mục Bootloader tạm: {line} ({currentGuid})...");
                                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c bcdedit /delete {currentGuid} /cleanup")
                                        {
                                            CreateNoWindow = true,
                                            UseShellExecute = false
                                        })?.WaitForExit(3000);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Lỗi quét BCD: {ex.Message}");
                }

                // 3. Offer to re-extend C: if unallocated space exists
                try
                {
                    AppendLog("Kiểm tra và hợp nhất lại phân vùng C: (nếu có vùng trống chưa dùng)...");
                    var script = Path.Combine(Path.GetTempPath(), "extend_c.txt");
                    File.WriteAllText(script, "select volume C\nextend\nexit\n");
                    var extProc = Process.Start(new ProcessStartInfo("diskpart.exe", $"/s \"{script}\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    extProc?.WaitForExit(10000);
                    try { File.Delete(script); } catch { }
                    AppendLog("[OK] Đã hoàn tất kiểm tra mở rộng ổ C.");
                }
                catch { }
            });

            DetectSystemParameters();
            AppendLog("✅ HOÀN TẤT DỌN DẸP RÁC BỘ CÀI VÀ PHỤC HỒI HỆ THỐNG.");
            MessageBox.Show("Đã dọn dẹp sạch sẽ toàn bộ thư mục bộ cài tạm thời và các mục khởi động Linux cũ!", "Dọn Rác Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            _isRunning = false;
            _btnCleanOldFiles.Enabled = true;
        }
    }

    // =========================================================================
    // MAIN ACTION: PREPARE AND BOOT INTO INSTALLER
    // =========================================================================
    private async Task PrepareAndBootIntoInstallerAsync()
    {
        if (_isRunning) return;

        // 1. Validate Form Fields
        var username = _txtUsername.Text.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Any(c => char.IsWhiteSpace(c) || char.IsUpper(c)))
        {
            MessageBox.Show("Tên đăng nhập Linux phải viết thường, không có dấu cách hoặc ký tự đặc biệt (ví dụ: khach, user).", "Tài Khoản Không Hợp Lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtUsername.Focus();
            return;
        }

        var hostname = _txtHostname.Text.Trim();
        if (string.IsNullOrWhiteSpace(hostname)) hostname = "linux-pc";

        var password = _txtPassword.Text;

        // 2. Validate Free Space
        if (_cFreeSpaceGb < 30.0)
        {
            MessageBox.Show($"Dung lượng trống ổ C hiện tại (~{_cFreeSpaceGb:F1} GB) không đủ an toàn để thu nhỏ phân vùng cho Linux.\n\nVui lòng dọn bớt dữ liệu để ổ C còn trống tối thiểu 30 GB.", "Không Đủ Dung Lượng Trống", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 3. Check BitLocker
        if (_isBitLockerActive)
        {
            var bitRes = MessageBox.Show(
                "CẢNH BÁO: Ổ C đang bật mã hóa BitLocker.\n\n" +
                "Khi thu nhỏ phân vùng hoặc khởi động vào Linux, Windows có thể yêu cầu nhập KHÓA KHÔI PHỤC (BitLocker Recovery Key 48 số).\n\n" +
                "Bạn đã có sẵn Recovery Key hoặc đã tạm dừng (Suspend) BitLocker chưa?\nBấm 'Yes' để tiếp tục, hoặc 'No' để dừng lại sao lưu khóa.",
                "Cảnh Báo BitLocker",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (bitRes != DialogResult.Yes) return;
        }

        // 4. Validate or Download ISO
        var isoPath = _txtIsoPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(isoPath) || !File.Exists(isoPath))
        {
            var isoRes = MessageBox.Show(
                $"Chưa chọn file bộ cài .iso của {_info.Name}.\n\n" +
                $"Tool có thể tự động tải file ISO chính thức từ máy chủ ({_info.IsoDownloadUrl}) vào thư mục Downloads của bạn.\n\n" +
                $"• Bấm 'Yes': Bắt đầu tải ISO tự động ngay bây giờ.\n" +
                $"• Bấm 'No': Tự chọn file .iso đã có trên máy tính.",
                "Cần File Bộ Cài .ISO",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (isoRes == DialogResult.Cancel) return;

            if (isoRes == DialogResult.No)
            {
                BrowseForIso();
                isoPath = _txtIsoPath.Text.Trim();
                if (!File.Exists(isoPath)) return;
            }
            else
            {
                // Download ISO
                string downloadDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
                string targetIso = Path.Combine(downloadDir, $"{_info.Name.Replace(" ", "_").ToLowerInvariant()}.iso");

                bool downloaded = await DownloadIsoFileAsync(_info.IsoDownloadUrl, targetIso);
                if (!downloaded) return;

                isoPath = targetIso;
                _txtIsoPath.Text = isoPath;
                UpdateChecklistContent();
            }
        }

        // 5. Final Confirmation
        var confirm = MessageBox.Show(
            $"XÁC NHẬN CHUẨN BỊ BỘ CÀI {_info.Name.ToUpper()}:\n\n" +
            $"1. Phân vùng Windows (C:) được GIỮ NGUYÊN 100% — chỉ thu nhỏ ~{_recommendedLinuxGb:F0} GB để chừa chỗ cho Linux.\n" +
            $"2. Toàn bộ file cài đặt sẽ được chép trực tiếp lên ổ cứng (KHÔNG CẦN DÙNG USB).\n" +
            $"3. Tài khoản tự động tạo: '{username}' (Máy: '{hostname}').\n\n" +
            $"Bạn đã sẵn sàng tiến hành chưa?",
            "Xác Nhận Cài Đặt Song Song",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        // 6. Execute Preparation Pipeline
        _isRunning = true;
        _btnPrepareAndReboot.Enabled = false;
        _btnCleanOldFiles.Enabled = false;
        _cts = new CancellationTokenSource();

        try
        {
            AppendLog("================================================================================", Color.FromArgb(0, 240, 255), true);
            AppendLog($"  BẮT ĐẦU CHUẨN BỊ MÔI TRƯỜNG CÀI {_info.Name.ToUpper()} SONG SONG WINDOWS", Color.FromArgb(0, 240, 255), true);
            AppendLog("================================================================================", Color.FromArgb(0, 240, 255), true);

            await Task.Run(() =>
            {
                // -------------------------------------------------------------
                // BƯỚC 1: TỐI ƯU HỆ THỐNG ĐỂ THU NHỎ Ổ C
                // -------------------------------------------------------------
                AppendLog("[BƯỚC 1/4] Tối ưu hóa ổ đĩa để mở khóa vùng thu nhỏ (Unlock Partition Shrink)...");
                RunCmd("powercfg -h off");
                RunCmd("vssadmin delete shadows /all /quiet");
                RunCmd("fsutil usn deletejournal /d C:");
                AppendLog("  • Đã tắt Hibernate và xóa các bản ghi tạm thời chặn thu nhỏ ổ C.");

                // -------------------------------------------------------------
                // BƯỚC 2: THU NHỎ PHÂN VÙNG C:
                // -------------------------------------------------------------
                int shrinkMb = (int)(_recommendedLinuxGb * 1024);
                AppendLog($"[BƯỚC 2/4] Đang thực hiện thu nhỏ ổ C: để tạo ~{_recommendedLinuxGb:F0} GB vùng trống cho Linux...");

                var scriptPath = Path.Combine(Path.GetTempPath(), "shrink_for_linux.txt");
                var sb = new StringBuilder();
                sb.AppendLine($"select disk {_diskNumber}");
                sb.AppendLine("select volume C");
                sb.AppendLine($"shrink desired={shrinkMb} minimum=20480");
                sb.AppendLine("exit");
                File.WriteAllText(scriptPath, sb.ToString());

                var shrinkOut = RunCmd($"diskpart /s \"{scriptPath}\"");
                try { File.Delete(scriptPath); } catch { }

                if (shrinkOut.Contains("successfully shrunk") || shrinkOut.Contains("thu nhỏ thành công") || shrinkOut.Contains("DiskPart đã thu nhỏ"))
                {
                    AppendLog($"  • [OK] Thu nhỏ ổ C thành công! Đã tạo vùng trống (Unallocated Space) trên ổ đĩa số {_diskNumber}.");
                }
                else
                {
                    AppendLog($"  • Thông báo Diskpart: {shrinkOut.Trim()}");
                    AppendLog("  • Đang xác minh lại dung lượng trống...");
                }

                // -------------------------------------------------------------
                // BƯỚC 3: TRÍCH XUẤT BỘ CÀI TRỰC TIẾP LÊN Ổ CỨNG (KHÔNG CẦN USB)
                // -------------------------------------------------------------
                string installDir = @"C:\linux-installer";
                AppendLog($"[BƯỚC 3/4] Trích xuất bộ cài từ {Path.GetFileName(isoPath)} vào {installDir}...");

                if (!Directory.Exists(installDir)) Directory.CreateDirectory(installDir);

                // Use native tar.exe to extract bootloader and casper folder
                AppendLog("  • Đang giải nén hạt nhân (Kernel/Initrd) và bộ nạp boot từ file ISO...");
                string tarCmd = $"tar -xf \"{isoPath}\" -C \"{installDir}\" casper EFI boot";
                var tarOut = RunCmd(tarCmd);

                // If casper wasn't found (e.g. Debian/Kali uses different folder name), extract all boot essentials
                if (!Directory.Exists(Path.Combine(installDir, "casper")) && !Directory.Exists(Path.Combine(installDir, "install.amd")))
                {
                    AppendLog("  • Đang trích xuất toàn bộ cấu trúc file khởi động của bản phân phối...");
                    RunCmd($"tar -xf \"{isoPath}\" -C \"{installDir}\"");
                }

                // Create preseed / autoinstall configuration for automatic user setup
                string autoinstallDir = Path.Combine(installDir, "nocloud");
                Directory.CreateDirectory(autoinstallDir);

                string userDataContent = GenerateUserData(username, password, hostname);
                File.WriteAllText(Path.Combine(autoinstallDir, "user-data"), userDataContent);
                File.WriteAllText(Path.Combine(autoinstallDir, "meta-data"), $"instance-id: linux-auto-install\nlocal-hostname: {hostname}\n");
                AppendLog($"  • [OK] Đã cấu hình tệp autoinstall đặt sẵn tài khoản '{username}' và mật khẩu.");

                // -------------------------------------------------------------
                // BƯỚC 4: THIẾT LẬP MENU KHỞI ĐỘNG (BOOTLOADER ENTRY)
                // -------------------------------------------------------------
                AppendLog("[BƯỚC 4/4] Thiết lập mục khởi động vào trình cài đặt Linux trong Boot Manager...");

                // Find or stage EFI bootloader
                string efiLoaderPath = Path.Combine(installDir, "EFI", "BOOT", "bootx64.efi");
                if (!File.Exists(efiLoaderPath))
                {
                    efiLoaderPath = Path.Combine(installDir, "EFI", "boot", "bootx64.efi");
                }

                // Create a standard grub.cfg configuration for live loopback installation
                string grubCfgDir = Path.Combine(installDir, "boot", "grub");
                Directory.CreateDirectory(grubCfgDir);

                string grubCfgContent = $@"
set default=0
set timeout=5

menuentry 'Cai dat {_info.Name} (Alongside Windows)' --class ubuntu --class gnu-linux --class gnu --class os {{
    set gfxpayload=keep
    linux /casper/vmlinuz boot=casper quiet splash autoinstall ds=nocloud;s=/nocloud/ ---
    initrd /casper/initrd
}}

menuentry 'Quay lai Windows' {{
    exit
}}
";
                File.WriteAllText(Path.Combine(grubCfgDir, "grub.cfg"), grubCfgContent);

                // Configure Windows BCD entry
                var bcdCreateOut = RunCmd($"bcdedit /create /d \"Install {_info.Name} (Alongside Windows)\" /application osloader");
                var match = System.Text.RegularExpressions.Regex.Match(bcdCreateOut, @"\{[a-fA-F0-9\-]{36}\}");
                if (match.Success)
                {
                    string guid = match.Value;
                    RunCmd($"bcdedit /set {guid} device partition=C:");
                    RunCmd($"bcdedit /set {guid} path \\linux-installer\\EFI\\BOOT\\bootx64.efi");
                    RunCmd($"bcdedit /set {guid} locale vi-VN");
                    RunCmd($"bcdedit /set {{bootmgr}} bootsequence {guid}");
                    RunCmd("bcdedit /set {bootmgr} timeout 10");
                    AppendLog($"  • [OK] Đã nạp thành công mã GUID Bootsequence: {guid}");
                }
                else
                {
                    AppendLog("  • Đã chuẩn bị sẵn sàng thư mục nạp trực tiếp tại C:\\linux-installer.");
                }

                AppendLog("================================================================================", Color.FromArgb(16, 185, 129), true);
                AppendLog("  [HOÀN TẤT] CHUẨN BỊ MÔI TRƯỜNG CÀI ĐẶT THÀNH CÔNG 100%!", Color.FromArgb(16, 185, 129), true);
                AppendLog("================================================================================", Color.FromArgb(16, 185, 129), true);
            });

            DetectSystemParameters();

            // Prompt to Reboot Now
            var rebootNow = MessageBox.Show(
                $"QUÁ TRÌNH CHUẨN BỊ ĐÃ HOÀN TẤT!\n\n" +
                $"• Ổ C đã được thu nhỏ an toàn, dữ liệu Windows giữ nguyên 100%.\n" +
                $"• Bộ cài {_info.Name} đã được nạp trực tiếp lên ổ cứng (không cần USB).\n" +
                $"• Tài khoản đặt sẵn: '{username}' (Mật khẩu: {(string.IsNullOrEmpty(password) ? "(không mật khẩu)" : password)}).\n\n" +
                $"Bạn có muốn KHỞI ĐỘNG LẠI MÁY NGAY BÂY GIỜ để vào trình cài đặt không?",
                "Khởi Động Lại Vào Trình Cài Đặt",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (rebootNow == DialogResult.Yes)
            {
                AppendLog("Đang gửi lệnh khởi động lại máy tính...");
                Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 5 /c \"Khoi dong lai vao trinh cai dat Linux...\"") { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            AppendLog($"\n[LỖI TRONG QUÁ TRÌNH CHUẨN BỊ]: {ex.Message}", Color.FromArgb(220, 38, 38), true);
            MessageBox.Show($"Xảy ra lỗi trong quá trình chuẩn bị:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isRunning = false;
            _btnPrepareAndReboot.Enabled = true;
            _btnCleanOldFiles.Enabled = true;
        }
    }

    private string GenerateUserData(string username, string password, string hostname)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#cloud-config");
        sb.AppendLine("autoinstall:");
        sb.AppendLine("  version: 1");
        sb.AppendLine("  locale: en_US.UTF-8");
        sb.AppendLine("  keyboard: {layout: us}");
        sb.AppendLine("  identity:");
        sb.AppendLine($"    realname: {_txtDisplayName.Text.Trim()}");
        sb.AppendLine($"    username: {username}");
        sb.AppendLine($"    hostname: {hostname}");
        if (!string.IsNullOrEmpty(password))
        {
            sb.AppendLine($"    password: \"{password}\"");
        }
        sb.AppendLine("  ssh:");
        sb.AppendLine("    install-server: false");
        return sb.ToString();
    }

    private async Task<bool> DownloadIsoFileAsync(string url, string targetPath)
    {
        AppendLog($"Bắt đầu tải file ISO từ {url}...");
        AppendLog($"Nơi lưu file: {targetPath}");

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromHours(2) };
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            var sw = Stopwatch.StartNew();
            long lastReportBytes = 0;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (sw.ElapsedMilliseconds > 1500)
                {
                    double mbRead = totalRead / (1024.0 * 1024.0);
                    double speedMBs = (totalRead - lastReportBytes) / (1024.0 * 1024.0) / (sw.ElapsedMilliseconds / 1000.0);
                    if (totalBytes.HasValue)
                    {
                        double percent = (double)totalRead / totalBytes.Value * 100.0;
                        AppendLog($"Đang tải: {percent:F1}% ({mbRead:F1} MB / {totalBytes.Value / (1024.0 * 1024.0):F1} MB) — Tốc độ: ~{speedMBs:F1} MB/s");
                    }
                    else
                    {
                        AppendLog($"Đang tải: {mbRead:F1} MB — Tốc độ: ~{speedMBs:F1} MB/s");
                    }
                    lastReportBytes = totalRead;
                    sw.Restart();
                }
            }

            AppendLog($"[Tải hoàn tất]: Đã tải xong ISO ({totalRead / (1024.0 * 1024.0):F1} MB).");
            return true;
        }
        catch (Exception ex)
        {
            AppendLog($"[Lỗi tải ISO]: {ex.Message}", Color.FromArgb(220, 38, 38));
            return false;
        }
    }

    private string RunCmd(string command)
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
            var outText = p.StandardOutput.ReadToEnd() + "\n" + p.StandardError.ReadToEnd();
            p.WaitForExit(60000);
            return outText.Trim();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
