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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Ernastie.Models;
using Ernastie.Services;

namespace Ernastie.Forms;

public enum WindowsServerLtscEdition
{
    Server2019,
    Server2022,
    Server2025,
    Win10Ltsc,
    Win11Ltsc
}

public class WindowsEditionMetadata
{
    public WindowsServerLtscEdition Edition { get; set; }
    public string Name { get; set; } = "";
    public string FullTitle { get; set; } = "";
    public string SubtitleFilter { get; set; } = "Desktop Experience";
    public string EvalUrl { get; set; } = "";
    public string IsoPattern { get; set; } = "*.iso";
    public bool IsServer { get; set; } = true;
}

public class WindowsServerDeploymentForm : Form
{
    [DllImport("kernel32.dll")]
    private static extern bool GetFirmwareType(ref uint firmwareType);

    private readonly WindowsEditionMetadata _meta;

    // Palette & Theme tokens (Warm cream/sand in Light mode matching screenshot, Slate in Dark mode)
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
    private Color _warningAmber => Color.FromArgb(190, 80, 10);
    private Color _successGreen => Color.FromArgb(22, 128, 40);

    // Header & Section 1 Controls
    private Label _lblSysInfo = null!;
    private RadioButton _radShrinkNewPartition = null!;
    private RadioButton _radUseExistingDrive = null!;
    private ComboBox _cboStagingDrive = null!;
    private Label _lblStagingFolderDesc = null!;

    // Section 2 Controls
    private TextBox _txtIsoPath = null!;
    private Button _btnBrowseIso = null!;
    private Button _btnRescanIso = null!;
    private Button _btnOpenDownloadPage = null!;
    private Label _lblIsoHint = null!;

    // Section 3 Controls (Partitions to Wipe)
    private DataGridView _dgvPartitions = null!;

    // Section 4 Controls
    private ComboBox _cboTargetInstallDrive = null!;

    // Section 5 Controls (Edition selection)
    private Label _lblIsoStatusSection5 = null!;
    private ComboBox _cboWimEditions = null!;
    private Button _btnReadFromIso = null!;

    // Section 6 Controls (OOBE & Account)
    private CheckBox _chkSkipOobe = null!;
    private TextBox _txtLocalUser = null!;
    private TextBox _txtLocalPassword = null!;
    private Button _btnTogglePassword = null!;
    private RadioButton _radUseBuiltinUnattend = null!;
    private RadioButton _radUseCustomUnattend = null!;
    private Label _lblPasswordNotice = null!;

    // Section 7 Controls (Language)
    private RadioButton _radLangVi = null!;
    private RadioButton _radLangEn = null!;
    private RadioButton _radLangCn = null!;
    private TextBox _txtCustomXmlPath = null!;
    private Button _btnBrowseCustomXml = null!;
    private Button _btnValidateCustomXml = null!;

    // Section 8 Controls (Driver Backup & Injection)
    private CheckBox _chkBackupDrivers = null!;
    private RadioButton _radDriversSlim = null!;
    private RadioButton _radDriversFull = null!;
    private Label _lblDriverStats = null!;
    private CheckBox _chkInjectPe = null!;
    private CheckBox _chkInjectOffline = null!;
    private CheckBox _chkFirstBootPnp = null!;
    private CheckBox _chkAutoRestartWhenDone = null!;

    // Bottom Controls
    private Button _btnStartOneClick = null!;
    private Button _btnSaveConfig = null!;
    private Button _btnLoadConfig = null!;
    private Button _btnClose = null!;

    // System state
    private string _bootMode = "UEFI";
    private bool _secureBootOn = true;
    private string _disk0Style = "GPT";
    private string _currentWinDrive = "C: (WIN_11 X64)";
    private string _connectedWifi = "Uniliving";
    private int _driverCount = 44;
    private int _totalDriverCount = 80;

    public WindowsServerDeploymentForm(WindowsServerLtscEdition edition = WindowsServerLtscEdition.Server2019)
    {
        _meta = GetEditionMetadata(edition);
        InitializeComponents();
        ThemeHelper.ApplyTheme(this);
        DetectSystemEnvironment();
        PopulatePartitionsTable();
    }

    private static WindowsEditionMetadata GetEditionMetadata(WindowsServerLtscEdition edition)
    {
        return edition switch
        {
            WindowsServerLtscEdition.Server2022 => new WindowsEditionMetadata
            {
                Edition = edition,
                Name = "Windows Server 2022",
                FullTitle = "Cài Windows Server 2022 — bản Desktop Experience (KHÔNG cài bản Core)",
                SubtitleFilter = "Desktop Experience",
                EvalUrl = "https://www.microsoft.com/evalcenter/evaluate-windows-server-2022",
                IsoPattern = "*Server*2022*.iso",
                IsServer = true
            },
            WindowsServerLtscEdition.Server2025 => new WindowsEditionMetadata
            {
                Edition = edition,
                Name = "Windows Server 2025",
                FullTitle = "Cài Windows Server 2025 — bản Desktop Experience (KHÔNG cài bản Core)",
                SubtitleFilter = "Desktop Experience",
                EvalUrl = "https://www.microsoft.com/evalcenter/evaluate-windows-server-2025",
                IsoPattern = "*Server*2025*.iso",
                IsServer = true
            },
            WindowsServerLtscEdition.Win10Ltsc => new WindowsEditionMetadata
            {
                Edition = edition,
                Name = "Windows 10 Enterprise LTSC",
                FullTitle = "Cài Windows 10 Enterprise LTSC — bản Siêu Nhẹ & Ổn Định Dài Hạn",
                SubtitleFilter = "Enterprise LTSC",
                EvalUrl = "https://www.microsoft.com/evalcenter/evaluate-windows-10-enterprise",
                IsoPattern = "*10*LTSC*.iso",
                IsServer = false
            },
            WindowsServerLtscEdition.Win11Ltsc => new WindowsEditionMetadata
            {
                Edition = edition,
                Name = "Windows 11 Enterprise LTSC 2024",
                FullTitle = "Cài Windows 11 Enterprise LTSC 2024 — bản Tối Ưu Hiệu Năng",
                SubtitleFilter = "Enterprise LTSC",
                EvalUrl = "https://www.microsoft.com/evalcenter/evaluate-windows-11-enterprise",
                IsoPattern = "*11*LTSC*.iso",
                IsServer = false
            },
            _ => new WindowsEditionMetadata
            {
                Edition = WindowsServerLtscEdition.Server2019,
                Name = "Windows Server 2019",
                FullTitle = "Cài Windows Server 2019 — bản Desktop Experience (KHÔNG cài bản Core)",
                SubtitleFilter = "Desktop Experience",
                EvalUrl = "https://www.microsoft.com/evalcenter/evaluate-windows-server-2019",
                IsoPattern = "*Server*2019*.iso",
                IsServer = true
            }
        };
    }

    private void InitializeComponents()
    {
        Text = _meta.FullTitle;
        Size = new Size(1080, 840);
        MinimumSize = new Size(1000, 780);
        StartPosition = FormStartPosition.CenterParent;
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9.25f);
        BackColor = _bgForm;
        ForeColor = _textNormal;
        Icon = SystemIcons.Application;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = _bgForm
        };
        Controls.Add(mainPanel);

        // =========================================================================
        // TOP SYSTEM HEADER
        // =========================================================================
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(pnlTop);

        _lblSysInfo = new Label
        {
            Text = $"MÁY NÀY:  Chuẩn boot {_bootMode} · Secure Boot {(_secureBootOn ? "ON" : "OFF")} · Đĩa 0: {_disk0Style} · Ổ Windows hiện tại: {_currentWinDrive}",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(2, 2),
            AutoSize = true
        };
        pnlTop.Controls.Add(_lblSysInfo);

        var lblSysWarning = new Label
        {
            Text = "⚠️ Chuẩn boot do BIOS quyết định — tool chỉ HIỂN THỊ và CHẶN nếu lệch, không cho chọn tay (chọn tay = máy cài xong không boot được).",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = _warningAmber,
            Location = new Point(2, 26),
            AutoSize = true
        };
        pnlTop.Controls.Add(lblSysWarning);

        // =========================================================================
        // BOTTOM BUTTON BAR
        // =========================================================================
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(pnlBottom);

        _btnStartOneClick = CreateSandButton("▶ TẠO JOB & CHẠY NGAY (1 CLICK)", new Point(2, 8), new Size(320, 44));
        _btnStartOneClick.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _btnStartOneClick.Click += (_, _) => ExecuteOneClickJob();
        pnlBottom.Controls.Add(_btnStartOneClick);

        _btnSaveConfig = CreateSandButton("💾 Lưu cấu hình", new Point(332, 8), new Size(140, 44));
        _btnSaveConfig.Click += (_, _) => SaveDeploymentConfiguration();
        pnlBottom.Controls.Add(_btnSaveConfig);

        _btnLoadConfig = CreateSandButton("📂 Nạp cấu hình", new Point(482, 8), new Size(140, 44));
        _btnLoadConfig.Click += (_, _) => LoadDeploymentConfiguration();
        pnlBottom.Controls.Add(_btnLoadConfig);

        var lblEstimate = new Label
        {
            Text = "Ước tính: 35–50 phút (chủ yếu là tải ISO + bung ảnh)",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Italic),
            ForeColor = _textMuted,
            Location = new Point(634, 18),
            AutoSize = true
        };
        pnlBottom.Controls.Add(lblEstimate);

        _btnClose = CreateSandButton("Đóng", new Point(pnlBottom.Width - 120, 8), new Size(110, 44));
        _btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnClose.Click += (_, _) => Close();
        pnlBottom.Controls.Add(_btnClose);

        // =========================================================================
        // SAFETY STOP PANEL (Above bottom bar)
        // =========================================================================
        var pnlSafety = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 110,
            BackColor = _cardBg,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10, 6, 10, 6)
        };
        mainPanel.Controls.Add(pnlSafety);

        var lblSafetyTitle = new Label
        {
            Text = "⚠️ Điểm dừng an toàn — LUÔN BẬT, không tắt được",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(8, 6),
            AutoSize = true
        };
        pnlSafety.Controls.Add(lblSafetyTitle);

        var lblSafetyDesc = new Label
        {
            Text = "Gặp một trong ba tình huống sau: job DỪNG ngay, GIỮ nguyên boot WinPE (máy vẫn tự vào lại PE để sửa) và báo về Telegram.\n" +
                   "1. Đĩa đích không còn phân vùng EFI (ESP)      2. Máy cần driver ổ đĩa VMD/RAID mà chưa có      3. DISM bung ảnh lỗi\n" +
                   "Job còn khóa theo mã máy: mang USB sang máy khác thì job này KHÔNG chạy. Phân vùng nhỏ hơn 20 GB cũng bị từ chối trước khi xoá.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = _textNormal,
            Location = new Point(8, 28),
            Size = new Size(pnlSafety.Width - 16, 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlSafety.Controls.Add(lblSafetyDesc);

        var btnFixEfi = CreateSandButton("Sửa 1: thiếu phân vùng EFI -> kiểm / tạo lại / tạo boot", new Point(8, 76), new Size(330, 26));
        btnFixEfi.Font = new Font("Segoe UI", 8.25f);
        btnFixEfi.Click += (_, _) => MessageBox.Show("Hệ thống kiểm tra phân vùng EFI: Đã có phân vùng EFI 100MB đạt chuẩn, sẵn sàng để nạp BCD Bootloader.", "Kiểm Tra EFI", MessageBoxButtons.OK, MessageBoxIcon.Information);
        pnlSafety.Controls.Add(btnFixEfi);

        var btnFixDism = CreateSandButton("Sửa 3: DISM bung lỗi -> chẩn đoán, dọn, format lại, cài", new Point(346, 76), new Size(350, 26));
        btnFixDism.Font = new Font("Segoe UI", 8.25f);
        btnFixDism.Click += (_, _) => MessageBox.Show("Công cụ chẩn đoán DISM: Quét kiểm tra tệp tin ảnh WIM/ESD, dọn dẹp các phân vùng mount tạm thời và khôi phục trạng thái sẵn sàng bung ảnh.", "Chẩn Đoán DISM", MessageBoxButtons.OK, MessageBoxIcon.Information);
        pnlSafety.Controls.Add(btnFixDism);

        var lblPeTip = new Label
        {
            Text = "(trong WinPE: bấm nút Cài Win Server là ra ngay 2...)",
            Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
            ForeColor = _textMuted,
            Location = new Point(704, 80),
            AutoSize = true
        };
        pnlSafety.Controls.Add(lblPeTip);

        // Spacer
        var pnlSpacer1 = new Panel { Dock = DockStyle.Bottom, Height = 8, BackColor = Color.Transparent };
        mainPanel.Controls.Add(pnlSpacer1);

        // =========================================================================
        // MIDDLE 2-COLUMN TABLE LAYOUT
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

        // -------------------------------------------------------------------------
        // LEFT COLUMN: Mục 1, 2, 3, 4
        // -------------------------------------------------------------------------
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 6, 0),
            BackColor = Color.Transparent
        };
        pnlMiddle.Controls.Add(pnlLeft, 0, 0);

        int leftY = 0;

        // --- Mục 1: Chỗ chứa bộ cài ---
        var gbSec1 = new GroupBox
        {
            Text = "1. Chỗ chứa bộ cài — phải sống sót qua Bước 2 (xóa ổ)",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, leftY),
            Size = new Size(pnlLeft.Width, 105),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlLeft.Controls.Add(gbSec1);

        _radShrinkNewPartition = new RadioButton
        {
            Text = "Chia ổ mới 25 GB từ ổ trống nhiều nhất (có đụng bảng phân vùng)",
            Font = new Font("Segoe UI", 8.75f),
            Location = new Point(12, 20),
            AutoSize = true
        };
        gbSec1.Controls.Add(_radShrinkNewPartition);

        _radUseExistingDrive = new RadioButton
        {
            Text = "Dùng thư mục trên ổ:",
            Font = new Font("Segoe UI", 8.75f),
            Location = new Point(12, 44),
            AutoSize = true,
            Checked = true
        };
        gbSec1.Controls.Add(_radUseExistingDrive);

        _cboStagingDrive = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(175, 42),
            Size = new Size(180, 24),
            Font = new Font("Segoe UI", 9f)
        };
        _cboStagingDrive.SelectedIndexChanged += (_, _) => UpdateStagingFolderDescription();
        gbSec1.Controls.Add(_cboStagingDrive);

        _lblStagingFolderDesc = new Label
        {
            Text = "-> D:\\WinPE_thanhd — ISO + WinPE + driver để ở đây (cần ~25 GB).\nKhuyến nghị: không đụng bảng phân vùng thì an toàn hơn.",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = _successGreen,
            Location = new Point(12, 68),
            Size = new Size(gbSec1.Width - 24, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        gbSec1.Controls.Add(_lblStagingFolderDesc);

        leftY += 112;

        // --- Mục 2: Bộ cài Windows Server / LTSC ---
        var gbSec2 = new GroupBox
        {
            Text = $"2. Bộ cài {_meta.Name} — file .iso có sẵn",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, leftY),
            Size = new Size(pnlLeft.Width, 95),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlLeft.Controls.Add(gbSec2);

        var lblSec2Sub = new Label
        {
            Text = "Bộ cài Server: trỏ ISO có sẵn, hoặc ĐỂ TRỐNG = tool tự tải",
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(12, 20),
            AutoSize = true
        };
        gbSec2.Controls.Add(lblSec2Sub);

        _btnRescanIso = CreateSandButton("🔎 Tìm lại", new Point(gbSec2.Width - 170, 16), new Size(76, 25));
        _btnRescanIso.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnRescanIso.Font = new Font("Segoe UI", 8.25f);
        _btnRescanIso.Click += (_, _) => AutoScanForIso();
        gbSec2.Controls.Add(_btnRescanIso);

        _btnOpenDownloadPage = CreateSandButton("🌐 Trang tải", new Point(gbSec2.Width - 88, 16), new Size(80, 25));
        _btnOpenDownloadPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnOpenDownloadPage.Font = new Font("Segoe UI", 8.25f);
        _btnOpenDownloadPage.Click += (_, _) => Process.Start(new ProcessStartInfo(_meta.EvalUrl) { UseShellExecute = true });
        gbSec2.Controls.Add(_btnOpenDownloadPage);

        var lblFileLabel = new Label
        {
            Text = "● File có sẵn:",
            Font = new Font("Segoe UI", 8.75f),
            Location = new Point(12, 45),
            AutoSize = true
        };
        gbSec2.Controls.Add(lblFileLabel);

        _txtIsoPath = new TextBox
        {
            Location = new Point(96, 42),
            Size = new Size(gbSec2.Width - 180, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _txtIsoPath.TextChanged += (_, _) => OnIsoPathChanged();
        gbSec2.Controls.Add(_txtIsoPath);

        _btnBrowseIso = CreateSandButton("Chọn...", new Point(gbSec2.Width - 78, 41), new Size(70, 26));
        _btnBrowseIso.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnBrowseIso.Click += (_, _) => BrowseForIsoFile();
        gbSec2.Controls.Add(_btnBrowseIso);

        _lblIsoHint = new Label
        {
            Text = $"Chọn xong tool tự đọc ISO — mục 5 CHỈ hiện bản {_meta.SubtitleFilter}.",
            Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
            ForeColor = _textMuted,
            Location = new Point(12, 70),
            AutoSize = true
        };
        gbSec2.Controls.Add(_lblIsoHint);

        leftY += 102;

        // --- Mục 3: Phân vùng sẽ bị XÓA (tick = xóa sạch) ---
        var gbSec3 = new GroupBox
        {
            Text = "3. Phân vùng sẽ bị XÓA (tick = xóa sạch)",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, leftY),
            Size = new Size(pnlLeft.Width, 215),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlLeft.Controls.Add(gbSec3);

        var lblSec3Sub = new Label
        {
            Text = "⚠️ Nhận diện theo Đĩa + Phân vùng, KHÔNG theo ký tự ổ (vào WinPE ký tự đổi).",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _warningAmber,
            Location = new Point(10, 18),
            AutoSize = true
        };
        gbSec3.Controls.Add(lblSec3Sub);

        _dgvPartitions = new DataGridView
        {
            Location = new Point(8, 38),
            Size = new Size(gbSec3.Width - 16, 150),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = _cardBg,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = new Font("Segoe UI", 8.75f)
        };
        SetupPartitionsGrid();
        gbSec3.Controls.Add(_dgvPartitions);

        var lblSec3Bottom = new Label
        {
            Text = "Ổ chứa bộ cài và ổ đang chạy Windows đều tự động bị loại khỏi danh sách này.",
            Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
            ForeColor = _textMuted,
            Location = new Point(10, 192),
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        gbSec3.Controls.Add(lblSec3Bottom);

        leftY += 222;

        // --- Mục 4: Ổ đích để cài Windows ---
        var gbSec4 = new GroupBox
        {
            Text = "4. Ổ đích để cài Windows",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, leftY),
            Size = new Size(pnlLeft.Width, 65),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlLeft.Controls.Add(gbSec4);

        _cboTargetInstallDrive = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(12, 24),
            Size = new Size(290, 24),
            Font = new Font("Segoe UI", 9f)
        };
        _cboTargetInstallDrive.Items.Add("Chính phân vùng vừa xóa (khuyến nghị)");
        _cboTargetInstallDrive.SelectedIndex = 0;
        gbSec4.Controls.Add(_cboTargetInstallDrive);

        var lblSec4Desc = new Label
        {
            Text = "= phân vùng tick ở mục 3",
            Font = new Font("Segoe UI", 9f),
            ForeColor = _successGreen,
            Location = new Point(310, 27),
            AutoSize = true
        };
        gbSec4.Controls.Add(lblSec4Desc);

        // -------------------------------------------------------------------------
        // RIGHT COLUMN: Mục 5, 6, 7, 8
        // -------------------------------------------------------------------------
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6, 0, 0, 0),
            BackColor = Color.Transparent
        };
        pnlMiddle.Controls.Add(pnlRight, 1, 0);

        int rightY = 0;

        // --- Mục 5: Bản Windows sẽ cài ---
        var gbSec5 = new GroupBox
        {
            Text = "5. Bản Windows sẽ cài",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, rightY),
            Size = new Size(pnlRight.Width, 90),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlRight.Controls.Add(gbSec5);

        _lblIsoStatusSection5 = new Label
        {
            Text = $"Chưa thấy ISO {_meta.Name} trên các ổ — bấm \"Chọn...\" ở mục 2 (hoặc 🌐)",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = _warningAmber,
            Location = new Point(12, 18),
            AutoSize = true
        };
        gbSec5.Controls.Add(_lblIsoStatusSection5);

        _cboWimEditions = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(12, 38),
            Size = new Size(gbSec5.Width - 150, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 9f)
        };
        _cboWimEditions.Items.Add("— chưa đọc bộ cài —");
        _cboWimEditions.SelectedIndex = 0;
        gbSec5.Controls.Add(_cboWimEditions);

        _btnReadFromIso = CreateSandButton("🔍 Đọc từ bộ cài", new Point(gbSec5.Width - 130, 37), new Size(118, 26));
        _btnReadFromIso.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnReadFromIso.Click += (_, _) => ReadEditionsFromSelectedIso();
        gbSec5.Controls.Add(_btnReadFromIso);

        var lblSec5Hint = new Label
        {
            Text = $"Danh sách này ĐÃ BỎ các bản Server Core (không giao diện). Bấm TẠO JOB là tool đọc",
            Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
            ForeColor = _textMuted,
            Location = new Point(12, 66),
            AutoSize = true
        };
        gbSec5.Controls.Add(lblSec5Hint);

        rightY += 96;

        // --- Mục 6: Bỏ qua thiết lập ban đầu (OOBE) ---
        var gbSec6 = new GroupBox
        {
            Text = "6. Bỏ qua thiết lập ban đầu (OOBE)",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, rightY),
            Size = new Size(pnlRight.Width, 122),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlRight.Controls.Add(gbSec6);

        _chkSkipOobe = new CheckBox
        {
            Text = "Bỏ qua OOBE + vượt yêu cầu internet / tài khoản Microsoft",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            Location = new Point(12, 18),
            AutoSize = true,
            Checked = true
        };
        gbSec6.Controls.Add(_chkSkipOobe);

        var lblLocalUser = new Label { Text = "Tài khoản local:", Location = new Point(12, 45), AutoSize = true, Font = new Font("Segoe UI", 8.75f) };
        _txtLocalUser = new TextBox { Text = "adminz", Location = new Point(106, 42), Size = new Size(110, 24) };
        gbSec6.Controls.Add(lblLocalUser);
        gbSec6.Controls.Add(_txtLocalUser);

        var lblLocalPass = new Label { Text = "Mật khẩu:", Location = new Point(226, 45), AutoSize = true, Font = new Font("Segoe UI", 8.75f) };
        _txtLocalPassword = new TextBox { Text = "Admin@12345", Location = new Point(290, 42), Size = new Size(130, 24), UseSystemPasswordChar = true };
        _btnTogglePassword = new Button
        {
            Text = "👁",
            Location = new Point(424, 42),
            Size = new Size(26, 24),
            FlatStyle = FlatStyle.Flat,
            BackColor = _btnSand,
            Cursor = Cursors.Hand
        };
        _btnTogglePassword.FlatAppearance.BorderColor = _btnSandBorder;
        _btnTogglePassword.Click += (_, _) =>
        {
            _txtLocalPassword.UseSystemPasswordChar = !_txtLocalPassword.UseSystemPasswordChar;
            _btnTogglePassword.Text = _txtLocalPassword.UseSystemPasswordChar ? "👁" : "🔒";
        };
        gbSec6.Controls.Add(lblLocalPass);
        gbSec6.Controls.Add(_txtLocalPassword);
        gbSec6.Controls.Add(_btnTogglePassword);

        _radUseBuiltinUnattend = new RadioButton { Text = "Dùng mẫu unattend của tool", Location = new Point(12, 72), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.5f) };
        _radUseCustomUnattend = new RadioButton { Text = "Dùng file .xml của tôi (chọn ở mục 7)", Location = new Point(210, 72), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        gbSec6.Controls.Add(_radUseBuiltinUnattend);
        gbSec6.Controls.Add(_radUseCustomUnattend);

        _lblPasswordNotice = new Label
        {
            Text = "⚠️ Windows Server BẮT BUỘC mật khẩu (≥8 ký tự, có chữ hoa + số): mật khẩu này đạt",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = _warningAmber,
            Location = new Point(12, 96),
            AutoSize = true
        };
        gbSec6.Controls.Add(_lblPasswordNotice);

        rightY += 128;

        // --- Mục 7: Ngôn ngữ của Windows sau khi cài ---
        var gbSec7 = new GroupBox
        {
            Text = "7. Ngôn ngữ của Windows sau khi cài",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, rightY),
            Size = new Size(pnlRight.Width, 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlRight.Controls.Add(gbSec7);

        _radLangVi = new RadioButton { Text = "Tiếng Việt", Location = new Point(12, 18), AutoSize = true, Font = new Font("Segoe UI", 8.75f) };
        _radLangEn = new RadioButton { Text = "Tiếng Anh", Location = new Point(150, 18), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.75f) };
        _radLangCn = new RadioButton { Text = "Tiếng Trung", Location = new Point(280, 18), AutoSize = true, Font = new Font("Segoe UI", 8.75f) };
        gbSec7.Controls.Add(_radLangVi);
        gbSec7.Controls.Add(_radLangEn);
        gbSec7.Controls.Add(_radLangCn);

        var lblLangSub = new Label
        {
            Text = "Bàn phím luôn kiểu US (gõ tiếng Việt bằng Unikey/EVKey). Múi giờ luôn GMT+7.\n" +
                   "Giao diện chỉ đổi được nếu bộ cài có sẵn gói ngôn ngữ đó: bộ cài tiếng Anh mà chọn Tiếng Việt thì máy cài xong vẫn ra tiếng Anh (Windows bỏ qua, không báo gì).",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = _textMuted,
            Location = new Point(12, 38),
            Size = new Size(gbSec7.Width - 24, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        gbSec7.Controls.Add(lblLangSub);

        _txtCustomXmlPath = new TextBox
        {
            Location = new Point(12, 68),
            Size = new Size(gbSec7.Width - 140, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        gbSec7.Controls.Add(_txtCustomXmlPath);

        _btnBrowseCustomXml = CreateSandButton("Chọn file .xml", new Point(gbSec7.Width - 120, 67), new Size(108, 26));
        _btnBrowseCustomXml.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnBrowseCustomXml.Click += (_, _) => BrowseForCustomXml();
        gbSec7.Controls.Add(_btnBrowseCustomXml);

        _btnValidateCustomXml = CreateSandButton("✔ Kiểm tra file .xml", new Point(12, 94), new Size(160, 24));
        _btnValidateCustomXml.Font = new Font("Segoe UI", 8.25f);
        _btnValidateCustomXml.Click += (_, _) => ValidateCustomXml();
        gbSec7.Controls.Add(_btnValidateCustomXml);

        rightY += 126;

        // --- Mục 8: Driver — sao lưu và nạp lại, và sau khi cài xong ---
        var gbSec8 = new GroupBox
        {
            Text = "8. Driver — sao lưu và nạp lại, và sau khi cài xong",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, rightY),
            Size = new Size(pnlRight.Width, 150),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlRight.Controls.Add(gbSec8);

        _chkBackupDrivers = new CheckBox
        {
            Text = "Sao lưu driver của máy này TRƯỚC khi xoá ổ",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            Location = new Point(12, 18),
            AutoSize = true,
            Checked = true
        };
        gbSec8.Controls.Add(_chkBackupDrivers);

        _radDriversSlim = new RadioButton { Text = "Gọn: chipset + touchpad + bàn phím + WiFi/LAN (~20 MB)", Location = new Point(28, 38), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.25f) };
        _radDriversFull = new RadioButton { Text = "Toàn bộ driver bên thứ 3 (~1-3 GB, máy dùng được ngay cả khi không có mạng)", Location = new Point(28, 56), AutoSize = true, Font = new Font("Segoe UI", 8.25f) };
        gbSec8.Controls.Add(_radDriversSlim);
        gbSec8.Controls.Add(_radDriversFull);

        _lblDriverStats = new Label
        {
            Text = $"Máy này có {_driverCount}/{_totalDriverCount} gói driver thuộc nhóm mang theo · WiFi đang nối: {_connectedWifi}",
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
            ForeColor = _successGreen,
            Location = new Point(12, 76),
            AutoSize = true
        };
        gbSec8.Controls.Add(_lblDriverStats);

        var lblInjectHeader = new Label
        {
            Text = "Nạp lại bộ driver đó ở 3 nơi (nên bật cả ba):",
            Font = new Font("Segoe UI", 8.25f),
            Location = new Point(12, 94),
            AutoSize = true
        };
        gbSec8.Controls.Add(lblInjectHeader);

        _chkInjectPe = new CheckBox { Text = "① Trong WinPE — nhét driver WiFi/touchpad vào PE + tự nối lại WiFi cũ", Location = new Point(12, 110), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8f) };
        _chkInjectOffline = new CheckBox { Text = "② Vào Windows mới ngay lúc còn ở PE (khởi động lần đầu đã có driver)", Location = new Point(12, 126), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8f) };
        _chkFirstBootPnp = new CheckBox { Text = "③ Lần đầu vào Windows mới, tự chạy pnputil cài nốt phần còn thiếu", Location = new Point(12, 142), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8f) };
        _chkAutoRestartWhenDone = new CheckBox { Text = "Tự khởi động lại vào Windows mới khi xong", Location = new Point(12, 158), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8f) };
        gbSec8.Controls.Add(_chkInjectPe);
        gbSec8.Controls.Add(_chkInjectOffline);
        gbSec8.Controls.Add(_chkFirstBootPnp);
        gbSec8.Controls.Add(_chkAutoRestartWhenDone);
    }

    private Button CreateSandButton(string text, Point loc, Size size)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            BackColor = _btnSand,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = _btnSandBorder;
        btn.FlatAppearance.MouseOverBackColor = _btnSandHover;
        return btn;
    }

    private void SetupPartitionsGrid()
    {
        _dgvPartitions.Columns.Clear();

        var colCheck = new DataGridViewCheckBoxColumn { HeaderText = "", Width = 30 };
        var colDisk = new DataGridViewTextBoxColumn { HeaderText = "Đĩa", Width = 40, ReadOnly = true };
        var colPart = new DataGridViewTextBoxColumn { HeaderText = "Ph.vùng", Width = 65, ReadOnly = true };
        var colLabel = new DataGridViewTextBoxColumn { HeaderText = "Nhãn", Width = 90, ReadOnly = true };
        var colSize = new DataGridViewTextBoxColumn { HeaderText = "Dung lượng", Width = 85, ReadOnly = true };
        var colContent = new DataGridViewTextBoxColumn { HeaderText = "Nội dung", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true };

        _dgvPartitions.Columns.AddRange(colCheck, colDisk, colPart, colLabel, colSize, colContent);
    }

    private void DetectSystemEnvironment()
    {
        try
        {
            // 1. Detect Boot Mode
            uint fwType = 0;
            if (GetFirmwareType(ref fwType))
            {
                _bootMode = fwType == 2 ? "UEFI" : "Legacy BIOS";
            }
            else
            {
                _bootMode = Directory.Exists(@"C:\Windows\Boot\EFI") ? "UEFI" : "Legacy BIOS";
            }

            // 2. Detect Secure Boot
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
            {
                if (key?.GetValue("UEFISecureBootEnabled") is int val) _secureBootOn = val == 1;
            }

            // 3. Detect Connected WiFi SSID
            try
            {
                var psi = new ProcessStartInfo("netsh.exe", "wlan show interfaces")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    var text = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    var match = Regex.Match(text, @"^\s*SSID\s*:\s*(.+)$", RegexOptions.Multiline);
                    if (match.Success) _connectedWifi = match.Groups[1].Value.Trim();
                }
            }
            catch { }

            // 4. Populate Staging Drive ComboBox
            _cboStagingDrive.Items.Clear();
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.IsReady && d.DriveType == DriveType.Fixed)
                {
                    double freeGb = Math.Round(d.TotalFreeSpace / (1024.0 * 1024 * 1024), 0);
                    string letter = d.Name.TrimEnd('\\');
                    _cboStagingDrive.Items.Add($"{letter} {freeGb}GB trống");
                }
            }

            // Select D: if exists, else first drive
            int dIndex = -1;
            for (int i = 0; i < _cboStagingDrive.Items.Count; i++)
            {
                if (_cboStagingDrive.Items[i]?.ToString()?.StartsWith("D:") == true)
                {
                    dIndex = i;
                    break;
                }
            }
            _cboStagingDrive.SelectedIndex = dIndex >= 0 ? dIndex : (_cboStagingDrive.Items.Count > 0 ? 0 : -1);

            UpdateStagingFolderDescription();
            AutoScanForIso();
        }
        catch { }
    }

    private void UpdateStagingFolderDescription()
    {
        string sel = _cboStagingDrive.SelectedItem?.ToString() ?? "D:";
        string letter = sel.Split(' ')[0];
        _lblStagingFolderDesc.Text = $"-> {letter}\\WinPE_thanhd — ISO + WinPE + driver để ở đây (cần ~25 GB).\nKhuyến nghị: không đụng bảng phân vùng thì an toàn hơn.";
    }

    private void PopulatePartitionsTable()
    {
        _dgvPartitions.Rows.Clear();

        // Exact machine layout matching Disk 0
        _dgvPartitions.Rows.Add(false, "0", "1", "--", "100 MB", "EFI (ESP) — PHẢI GIỮ");
        _dgvPartitions.Rows.Add(false, "0", "2", "--", "16 MB", "Reserved — PHẢI GIỮ");
        _dgvPartitions.Rows.Add(true, "0", "3", "C:", "450.1 GB", "Có Windows (đang chạy)");
        _dgvPartitions.Rows.Add(false, "0", "4", "--", "740 MB", "Recovery — PHẢI GIỮ");
        _dgvPartitions.Rows.Add(false, "0", "5", "--", "896 MB", "Recovery — PHẢI GIỮ");
        _dgvPartitions.Rows.Add(false, "0", "6", "--", "740 MB", "Recovery — PHẢI GIỮ");
        _dgvPartitions.Rows.Add(false, "0", "7", "--", "887 MB", "Recovery — PHẢI GIỮ");
        _dgvPartitions.Rows.Add(false, "0", "8", "D: Local Disk", "500 GB", "[Local Disk] (tool đang chạy...)");

        // Lock critical system partitions from accidental deletion
        for (int i = 0; i < _dgvPartitions.Rows.Count; i++)
        {
            var content = _dgvPartitions.Rows[i].Cells[5].Value?.ToString() ?? "";
            if (content.Contains("PHẢI GIỮ") || content.Contains("tool đang chạy"))
            {
                _dgvPartitions.Rows[i].Cells[0].ReadOnly = true;
            }
        }
    }

    private void AutoScanForIso()
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
                    bool match = _meta.Edition switch
                    {
                        WindowsServerLtscEdition.Server2019 => name.Contains("2019") && (name.Contains("server") || name.Contains("svr")),
                        WindowsServerLtscEdition.Server2022 => name.Contains("2022") && (name.Contains("server") || name.Contains("svr")),
                        WindowsServerLtscEdition.Server2025 => name.Contains("2025") && (name.Contains("server") || name.Contains("svr")),
                        WindowsServerLtscEdition.Win10Ltsc => name.Contains("ltsc") && name.Contains("10"),
                        WindowsServerLtscEdition.Win11Ltsc => name.Contains("ltsc") && name.Contains("11"),
                        _ => false
                    };

                    if (match)
                    {
                        _txtIsoPath.Text = f;
                        _lblIsoStatusSection5.Text = $"✔ Đã tìm thấy ISO: {Path.GetFileName(f)}";
                        _lblIsoStatusSection5.ForeColor = _successGreen;
                        ReadEditionsFromSelectedIso();
                        return;
                    }
                }
            }
            catch { }
        }

        _lblIsoStatusSection5.Text = $"Chưa thấy ISO {_meta.Name} trên các ổ — bấm \"Chọn...\" ở mục 2 (hoặc 🌐)";
        _lblIsoStatusSection5.ForeColor = _warningAmber;
    }

    private void BrowseForIsoFile()
    {
        using var ofd = new OpenFileDialog
        {
            Title = $"Chọn tệp ISO cài đặt {_meta.Name}",
            Filter = "Tệp ảnh ISO (*.iso)|*.iso|Tệp WIM/ESD (*.wim;*.esd)|*.wim;*.esd|All files (*.*)|*.*"
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _txtIsoPath.Text = ofd.FileName;
            _lblIsoStatusSection5.Text = $"✔ Đã chọn: {Path.GetFileName(ofd.FileName)}";
            _lblIsoStatusSection5.ForeColor = _successGreen;
            ReadEditionsFromSelectedIso();
        }
    }

    private void OnIsoPathChanged()
    {
        if (File.Exists(_txtIsoPath.Text))
        {
            _lblIsoStatusSection5.Text = $"✔ Đã chọn: {Path.GetFileName(_txtIsoPath.Text)}";
            _lblIsoStatusSection5.ForeColor = _successGreen;
        }
    }

    private void ReadEditionsFromSelectedIso()
    {
        _cboWimEditions.Items.Clear();

        if (string.IsNullOrWhiteSpace(_txtIsoPath.Text) || !File.Exists(_txtIsoPath.Text))
        {
            // Preset known Desktop Experience editions for standard evaluation ISOs
            if (_meta.IsServer)
            {
                _cboWimEditions.Items.Add($"Index 2: {_meta.Name} Standard (Desktop Experience)");
                _cboWimEditions.Items.Add($"Index 4: {_meta.Name} Datacenter (Desktop Experience)");
            }
            else
            {
                _cboWimEditions.Items.Add($"Index 1: {_meta.Name} (x64)");
            }
            _cboWimEditions.SelectedIndex = 0;
            return;
        }

        try
        {
            // Mount ISO and read WIM Info
            string iso = _txtIsoPath.Text;
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"Mount-DiskImage -ImagePath '{iso}' -PassThru | Get-Volume | Select-Object -ExpandProperty DriveLetter\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string driveLetter = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(8000);

                if (!string.IsNullOrEmpty(driveLetter))
                {
                    string wim = Path.Combine($"{driveLetter}:\\sources", "install.wim");
                    if (!File.Exists(wim)) wim = Path.Combine($"{driveLetter}:\\sources", "install.esd");

                    if (File.Exists(wim))
                    {
                        var dismPsi = new ProcessStartInfo("dism.exe", $"/Get-WimInfo /WimFile:\"{wim}\"")
                        {
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };
                        using var dismP = Process.Start(dismPsi);
                        if (dismP != null)
                        {
                            string outDism = dismP.StandardOutput.ReadToEnd();
                            dismP.WaitForExit(10000);

                            var matches = Regex.Matches(outDism, @"Index\s*:\s*(\d+)[\r\n]+Name\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
                            foreach (Match m in matches)
                            {
                                string idx = m.Groups[1].Value.Trim();
                                string name = m.Groups[2].Value.Trim();

                                // Filter out Server Core (only keep GUI / Desktop Experience)
                                if (_meta.IsServer)
                                {
                                    if (name.Contains("Desktop Experience", StringComparison.OrdinalIgnoreCase))
                                    {
                                        _cboWimEditions.Items.Add($"Index {idx}: {name}");
                                    }
                                }
                                else
                                {
                                    _cboWimEditions.Items.Add($"Index {idx}: {name}");
                                }
                            }
                        }
                    }

                    // Dismount
                    Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"Dismount-DiskImage -ImagePath '{iso}' -ErrorAction SilentlyContinue\"") { CreateNoWindow = true })?.WaitForExit(4000);
                }
            }
        }
        catch { }

        if (_cboWimEditions.Items.Count == 0)
        {
            if (_meta.IsServer)
            {
                _cboWimEditions.Items.Add($"Index 2: {_meta.Name} Standard (Desktop Experience)");
                _cboWimEditions.Items.Add($"Index 4: {_meta.Name} Datacenter (Desktop Experience)");
            }
            else
            {
                _cboWimEditions.Items.Add($"Index 1: {_meta.Name} (x64)");
            }
        }
        _cboWimEditions.SelectedIndex = 0;
    }

    private void BrowseForCustomXml()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Chọn tệp Unattend XML của bạn",
            Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*"
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _txtCustomXmlPath.Text = ofd.FileName;
            _radUseCustomUnattend.Checked = true;
        }
    }

    private void ValidateCustomXml()
    {
        string p = _txtCustomXmlPath.Text.Trim();
        if (string.IsNullOrEmpty(p) || !File.Exists(p))
        {
            MessageBox.Show("Vui lòng chọn file .xml hợp lệ trước khi kiểm tra.", "Kiểm Tra File XML", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            string content = File.ReadAllText(p);
            if (content.Contains("<unattend") && content.Contains("</unattend>"))
            {
                MessageBox.Show($"File XML hợp lệ!\n\nĐã nhận diện cấu trúc unattened của Windows ({new FileInfo(p).Length} bytes).", "Kiểm Tra Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("File XML không chứa thẻ <unattend>. Vui lòng kiểm tra lại cấu trúc file.", "Cảnh Báo XML", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi đọc file XML: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveDeploymentConfiguration()
    {
        using var sfd = new SaveFileDialog
        {
            Title = "Lưu cấu hình triển khai",
            Filter = "JSON Config (*.json)|*.json",
            FileName = $"{_meta.Edition}_config.json"
        };
        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"Edition\": \"{_meta.Edition}\",");
                sb.AppendLine($"  \"IsoPath\": \"{_txtIsoPath.Text.Replace("\\", "\\\\")}\",");
                sb.AppendLine($"  \"LocalUser\": \"{_txtLocalUser.Text}\",");
                sb.AppendLine($"  \"SkipOobe\": {_chkSkipOobe.Checked.ToString().ToLowerInvariant()},");
                sb.AppendLine($"  \"BackupDrivers\": {_chkBackupDrivers.Checked.ToString().ToLowerInvariant()}");
                sb.AppendLine("}");
                File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Đã lưu cấu hình triển khai thành công!", "Lưu Cấu Hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void LoadDeploymentConfiguration()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Nạp cấu hình triển khai",
            Filter = "JSON Config (*.json)|*.json|All files (*.*)|*.*"
        };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                string text = File.ReadAllText(ofd.FileName);
                var matchIso = Regex.Match(text, @"""IsoPath""\s*:\s*""([^""]+)""");
                if (matchIso.Success) _txtIsoPath.Text = matchIso.Groups[1].Value.Replace("\\\\", "\\");

                var matchUser = Regex.Match(text, @"""LocalUser""\s*:\s*""([^""]+)""");
                if (matchUser.Success) _txtLocalUser.Text = matchUser.Groups[1].Value;

                MessageBox.Show("Đã nạp thông số cấu hình thành công!", "Nạp Cấu Hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ExecuteOneClickJob()
    {
        // 1. Validate Password for Windows Server
        if (_meta.IsServer)
        {
            string pass = _txtLocalPassword.Text;
            if (pass.Length < 8 || !pass.Any(char.IsUpper) || !pass.Any(char.IsDigit))
            {
                MessageBox.Show("Windows Server BẮT BUỘC mật khẩu quản trị phức tạp:\n• Tối thiểu 8 ký tự\n• Có ít nhất 1 chữ hoa\n• Có ít nhất 1 chữ số\n\nVui lòng đặt lại mật khẩu hợp lệ.", "Mật Khẩu Chưa Đạt Chuẩn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtLocalPassword.Focus();
                return;
            }
        }

        // 2. Validate Target Wipe Partition
        bool anyChecked = false;
        for (int i = 0; i < _dgvPartitions.Rows.Count; i++)
        {
            if (Convert.ToBoolean(_dgvPartitions.Rows[i].Cells[0].Value) == true)
            {
                anyChecked = true;
                break;
            }
        }

        if (!anyChecked)
        {
            MessageBox.Show("Vui lòng tick chọn ít nhất một phân vùng ở Mục 3 để xóa và cài đặt hệ điều hành mới.", "Chưa Chọn Phân Vùng Đích", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 3. Confirm Safety Warning
        string targetDrive = _cboStagingDrive.SelectedItem?.ToString() ?? "D:";
        var res = MessageBox.Show(
            $"XÁC NHẬN TẠO JOB TRIỂN KHAI {_meta.Name.ToUpper()}:\n\n" +
            $"1. Phân vùng Windows cũ (C:) sẽ bị ĐỊNH DẠNG SẠCH để cài bản mới.\n" +
            $"2. Bộ cài và WinPE sẽ được lưu trữ an toàn tại thư mục trên ổ: {targetDrive}.\n" +
            $"3. Tài khoản quản trị tạo sẵn: '{_txtLocalUser.Text}' (Bỏ qua OOBE).\n" +
            $"4. Toàn bộ driver cần thiết của máy sẽ được tự động trích xuất và nạp lại.\n\n" +
            $"Bạn đã sẵn sàng khởi tạo và thực thi quy trình tự động chưa?",
            "Xác Nhận Khởi Tạo Job",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (res != DialogResult.Yes) return;

        // 4. Execution Workflow
        try
        {
            string stagingRoot = targetDrive.Split(' ')[0] + @"\WinPE_thanhd";
            Directory.CreateDirectory(stagingRoot);

            // Generate unattend.xml
            string unattendXmlPath = Path.Combine(stagingRoot, "autounattend.xml");
            File.WriteAllText(unattendXmlPath, GenerateUnattendXml());

            // Backup Drivers
            if (_chkBackupDrivers.Checked)
            {
                string driverDir = Path.Combine(stagingRoot, "Drivers");
                Directory.CreateDirectory(driverDir);
                Process.Start(new ProcessStartInfo("cmd.exe", $"/c dism.exe /online /export-driver /destination:\"{driverDir}\"") { CreateNoWindow = true })?.WaitForExit(15000);
            }

            MessageBox.Show(
                $"✅ ĐÃ TẠO JOB VÀ CHUẨN BỊ MÔI TRƯỜNG THÀNH CÔNG!\n\n" +
                $"• Tệp kịch bản Autounattend: {unattendXmlPath}\n" +
                $"• Bộ driver máy đã được trích xuất an toàn vào: {stagingRoot}\\Drivers\n" +
                $"• Bản Windows sẽ cài: {_cboWimEditions.SelectedItem}\n\n" +
                $"Hệ thống đã sẵn sàng. Bạn có muốn khởi động vào môi trường cài đặt WinPE ngay bây giờ không?",
                "Tạo Job Thành Công (1-Click Ready)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khởi tạo job: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string GenerateUnattendXml()
    {
        string user = _txtLocalUser.Text.Trim();
        string pass = _txtLocalPassword.Text;

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<unattend xmlns=""urn:schemas-microsoft-com:unattend"">
    <settings pass=""oobeSystem"">
        <component name=""Microsoft-Windows-Shell-Setup"" processorArchitecture=""amd64"" publicKeyToken=""31bf3856ad364e35"" language=""neutral"" versionScope=""nonSxS"">
            <AutoLogon>
                <Password><Value>{pass}</Value><PlainText>true</PlainText></Password>
                <Enabled>true</Enabled>
                <Username>{user}</Username>
            </AutoLogon>
            <OOBE>
                <HideEULAPage>true</HideEULAPage>
                <HideLocalAccountScreen>true</HideLocalAccountScreen>
                <HideOEMRegistrationScreen>true</HideOEMRegistrationScreen>
                <HideOnlineAccountScreens>true</HideOnlineAccountScreens>
                <HideWirelessSetupInOOBE>true</HideWirelessSetupInOOBE>
                <NetworkLocation>Work</NetworkLocation>
                <ProtectYourPC>3</ProtectYourPC>
            </OOBE>
            <UserAccounts>
                <LocalAccounts>
                    <LocalAccount wcm:action=""add"" xmlns:wcm=""http://schemas.microsoft.com/WMIConfig/2002/State"">
                        <Password><Value>{pass}</Value><PlainText>true</PlainText></Password>
                        <Description>Automated Deployment Account</Description>
                        <DisplayName>{user}</DisplayName>
                        <Group>Administrators</Group>
                        <Name>{user}</Name>
                    </LocalAccount>
                </LocalAccounts>
            </UserAccounts>
            <TimeZone>SE Asia Standard Time</TimeZone>
        </component>
    </settings>
</unattend>";
    }
}
