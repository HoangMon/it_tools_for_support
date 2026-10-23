using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Ernastie.Forms;
using Ernastie.Models;
using Ernastie.Services;

namespace Ernastie;

public class MainForm : Form
{
    private record ToolBtn(string Label, Action Action);

    private readonly SystemService _systemService = new();
    private readonly DiskService _diskService = new();
    private readonly MaintenanceService _maintenanceService = new();
    private readonly BackupService _backupService = new();
    private readonly GoogleSheetService _sheetService = new();

    private SystemInfoModel _sysInfo = new();
    private DeviceConfigModel _deviceConfig = new();

    // UI/UX Pro Max - Minimalism & Swiss Style Design Tokens (WCAG AAA)
    private static readonly Color BgApp = Color.FromArgb(248, 250, 252);        // Slate 50 (#F8FAFC)
    private static readonly Color CardBg = Color.White;                          // Pure White (#FFFFFF)
    private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);    // Slate 200 (#E2E8F0)
    private static readonly Color TextHeading = Color.FromArgb(15, 23, 42);      // Slate 900 (#0F172A)
    private static readonly Color TextMuted = Color.FromArgb(71, 85, 105);       // Slate 600 (#475569)
    private static readonly Color TextBody = Color.FromArgb(30, 41, 59);         // Slate 800 (#1E293B)
    private static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);     // Blue 600 (#2563EB)
    private static readonly Color BtnBg = Color.FromArgb(241, 245, 249);         // Slate 100 (#F1F5F9)
    private static readonly Color BtnHover = Color.FromArgb(224, 231, 255);      // Indigo 100 (#E0E7FF)
    private static readonly Color BtnBorder = Color.FromArgb(203, 213, 225);     // Slate 300 (#CBD5E1)
    private static readonly Color SidebarBg = Color.FromArgb(15, 23, 42);        // Slate 900 (#0F172A)
    private static readonly Color SidebarNavBg = Color.FromArgb(30, 41, 59);     // Slate 800 (#1E293B)
    private static readonly Color SidebarActive = Color.FromArgb(37, 99, 235);    // Blue 600 (#2563EB)
    private static readonly Color SuccessGreen = Color.FromArgb(22, 163, 74);    // Green 600 (#16A34A)
    private static readonly Color WarningAmber = Color.FromArgb(217, 119, 6);    // Amber 600 (#D97706)
    private static readonly Color DangerRed = Color.FromArgb(220, 38, 38);       // Red 600 (#DC2626)

    // Top Status Header Controls
    private Panel _pnlTop = null!;
    private FlowLayoutPanel _flowRow1 = null!;
    private Panel _pnlRow2 = null!;
    private Label _lblStatusTitle = null!;
    private Label _lblBitLocker = null!;
    private Label _lblPfx = null!;
    private Label _lblBoot = null!;
    private Label _lblIrst = null!;
    private Label _lblPath = null!;
    private Label _lblUser = null!;

    // Sidebar navigation buttons and toggle state
    private Panel _pnlSidebar = null!;
    private Panel _pnlBottomSidebar = null!;
    private Label _lblAppVersion = null!;
    private Button _btnTabDashboard = null!;
    private Button _btnTabCore = null!;
    private Button _btnTabAdv = null!;
    private Button _btnTabDevice = null!;
    private Button _btnTabSearch = null!;
    private Button _btnSettings = null!;

    private bool _isSidebarCollapsed = false;
    private bool _isSidebarHidden = false;
    private bool _isEn = false;
    private string _currentLanguage = "";
    private readonly ToolTip _toolTip = new()
    {
        InitialDelay = 150,
        ReshowDelay = 100,
        AutoPopDelay = 4000,
        ShowAlways = true
    };
    private Button _btnToggleSidebar = null!;
    private Panel _pnlSidebarHeader = null!;
    private Label _lblSidebarBrand = null!;
    private Button _btnCollapseSidebar = null!;
    private Button _btnHideSidebar = null!;

    private bool _isCurrentDark = false;
    private int _currentTabIndex = 0;

    // Content Panels
    private Panel _pnlContent = null!;
    private DashboardPanel _pnlDashboard = null!;
    private Panel _pnlCore = null!;
    private Panel _pnlAdv = null!;
    private Panel _pnlDevice = null!;
    private Panel _pnlSearch = null!;
    private Panel _pnlSearchBox = null!;
    private Label _lblSearchIcon = null!;
    private Button _btnClearSearch = null!;
    private TextBox _txtSearch = null!;
    private Label _lblSearchTitle = null!;
    private Label _lblSearchSub = null!;
    private Label _lblSearchResultCount = null!;
    private FlowLayoutPanel _flowSearchResults = null!;
    private string _selectedSearchCategory = "all";
    private readonly List<Button> _categoryChipButtons = new();
    private List<SearchableFeature> _allSearchFeatures = new();

    // Tab 3 Device controls
    private TextBox _txtAppsScript = null!;
    private TextBox _txtSheetUrl = null!;
    private ComboBox _cboMachineName = null!;
    private ComboBox _cboCompany = null!;
    private ComboBox _cboSyncDevice = null!;
    private ProgressBar _progressBar = null!;
    private RichTextBox _rtbLog = null!;

    public MainForm()
    {
        var s = SettingsService.Instance.Settings;
        _isCurrentDark = !s.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        _isEn = s.Language.Equals("en", StringComparison.OrdinalIgnoreCase);
        _currentLanguage = s.Language;

        InitializeUi();
        LoadSystemData();
        InitSettings();
    }

    private void InitializeUi()
    {
        Text = "E-TOOLBOX - IT Tools for Support";
        DoubleBuffered = true;
        KeyPreview = true;
        // Generous, wide window to eliminate text clipping and wrapping
        Size = new Size(1300, 840);
        MinimumSize = new Size(1060, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        BackColor = BgApp;

        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);
        }
        catch { }

        // 1. TOP HEADER (STATUS BAR)
        _pnlTop = CreateTopHeader();
        Controls.Add(_pnlTop);

        // 2. LEFT SIDEBAR NAVIGATION
        _pnlSidebar = CreateSidebar();
        Controls.Add(_pnlSidebar);

        // 3. MAIN CONTENT CONTAINER
        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgApp,
            Padding = new Padding(14, 10, 14, 10)
        };
        Controls.Add(_pnlContent);
        _pnlContent.BringToFront();

        // Build Pages
        _pnlDashboard = new DashboardPanel();
        _pnlCore = BuildCoreFeaturesPanel();
        _pnlAdv = BuildAdvancedPanel();
        _pnlDevice = BuildDevicePanel();
        _pnlSearch = BuildSearchPanel();

        _pnlContent.Controls.Add(_pnlDashboard);
        _pnlContent.Controls.Add(_pnlCore);
        _pnlContent.Controls.Add(_pnlAdv);
        _pnlContent.Controls.Add(_pnlDevice);
        _pnlContent.Controls.Add(_pnlSearch);

        // Default to Tab 0: Dashboard (Hardware Telemetry HUD)
        SwitchTab(0);
    }

    #region Header and Navigation

    private enum BadgeType { Success, Warning, Danger, Info }

    private Panel CreateTopHeader()
    {
        var top = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = CardBg,
            Padding = new Padding(16, 8, 16, 8)
        };

        top.Paint += (s, e) =>
        {
            var border = _isCurrentDark ? Color.FromArgb(32, 42, 60) : Color.FromArgb(226, 232, 240);
            using var pen = new Pen(border, 1);
            e.Graphics.DrawLine(pen, 0, top.Height - 1, top.Width, top.Height - 1);
        };

        // Row 1: FlowLayoutPanel with status badge pills (Responsive wrapping, no clipping)
        _flowRow1 = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = CardBg,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _btnToggleSidebar = new Button
        {
            Text = "☰",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Size = new Size(34, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = BtnBg,
            ForeColor = TextHeading,
            Margin = new Padding(0, 3, 10, 4),
            Cursor = Cursors.Hand,
            UseMnemonic = false,
            Visible = false
        };
        _btnToggleSidebar.FlatAppearance.BorderSize = 1;
        _btnToggleSidebar.FlatAppearance.BorderColor = BtnBorder;
        _btnToggleSidebar.Click += (_, _) => ToggleSidebar();

        _lblStatusTitle = new Label
        {
            Text = "● TRẠNG THÁI HỆ THỐNG / SYSTEM STATUS:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = TextHeading,
            Margin = new Padding(0, 6, 12, 4),
            AutoSize = true
        };

        _lblBitLocker = CreateStatusBadge("BitLocker: Đang kiểm tra...");
        SetBadge(_lblBitLocker, "BitLocker: Đang kiểm tra...", BadgeType.Danger);

        _lblPfx = CreateStatusBadge("Chứng thư PFX: Không có");
        SetBadge(_lblPfx, "Chứng thư PFX: Không có", BadgeType.Success);

        _lblBoot = CreateStatusBadge("Boot: GPT - UEFI");
        SetBadge(_lblBoot, "Boot: GPT - UEFI", BadgeType.Success);

        _lblIrst = CreateStatusBadge("Driver IRST: Cần nạp");
        SetBadge(_lblIrst, "Driver IRST: Cần nạp", BadgeType.Warning);

        _flowRow1.Controls.Add(_btnToggleSidebar);
        _flowRow1.Controls.Add(_lblStatusTitle);
        _flowRow1.Controls.Add(_lblBitLocker);
        _flowRow1.Controls.Add(_lblPfx);
        _flowRow1.Controls.Add(_lblBoot);
        _flowRow1.Controls.Add(_lblIrst);

        // Row 2: Responsive Panel with Path on left and User on right
        _pnlRow2 = new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = CardBg,
            Padding = new Padding(2, 4, 2, 0),
            Margin = new Padding(0, 4, 0, 0)
        };

        _lblUser = new Label
        {
            Text = "Tài khoản: Đang nạp...",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = PrimaryBlue,
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true
        };

        _lblPath = new Label
        {
            Text = "📂 Đường dẫn: Đang nạp...",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = TextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        _pnlRow2.Controls.Add(_lblPath);
        _pnlRow2.Controls.Add(_lblUser);

        // Added in reverse order for Dock = Top
        top.Controls.Add(_pnlRow2);
        top.Controls.Add(_flowRow1);

        return top;
    }

    private static Label CreateStatusBadge(string text)
    {
        var lbl = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 3, 10, 4),
            AutoSize = true
        };
        lbl.Paint += (s, e) =>
        {
            if (lbl.Tag is Color borderColor)
            {
                using var pen = new Pen(borderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, lbl.Width - 1, lbl.Height - 1);
            }
        };
        return lbl;
    }

    private void SetBadge(Label badge, string text, BadgeType type)
    {
        badge.Text = text;
        if (_isCurrentDark)
        {
            switch (type)
            {
                case BadgeType.Success:
                    badge.ForeColor = Color.FromArgb(74, 222, 128);
                    badge.BackColor = Color.FromArgb(5, 46, 22);
                    badge.Tag = Color.FromArgb(22, 101, 52);
                    break;
                case BadgeType.Warning:
                    badge.ForeColor = Color.FromArgb(251, 191, 36);
                    badge.BackColor = Color.FromArgb(69, 26, 3);
                    badge.Tag = Color.FromArgb(146, 64, 14);
                    break;
                case BadgeType.Danger:
                    badge.ForeColor = Color.FromArgb(248, 113, 113);
                    badge.BackColor = Color.FromArgb(69, 10, 10);
                    badge.Tag = Color.FromArgb(153, 27, 27);
                    break;
                default:
                    badge.ForeColor = Color.FromArgb(96, 165, 250);
                    badge.BackColor = Color.FromArgb(23, 37, 84);
                    badge.Tag = Color.FromArgb(30, 64, 175);
                    break;
            }
        }
        else
        {
            switch (type)
            {
                case BadgeType.Success:
                    badge.ForeColor = Color.FromArgb(21, 128, 61);
                    badge.BackColor = Color.FromArgb(240, 253, 244);
                    badge.Tag = Color.FromArgb(187, 247, 208);
                    break;
                case BadgeType.Warning:
                    badge.ForeColor = Color.FromArgb(180, 83, 9);
                    badge.BackColor = Color.FromArgb(254, 252, 232);
                    badge.Tag = Color.FromArgb(253, 230, 138);
                    break;
                case BadgeType.Danger:
                    badge.ForeColor = Color.FromArgb(185, 28, 28);
                    badge.BackColor = Color.FromArgb(254, 242, 242);
                    badge.Tag = Color.FromArgb(254, 202, 202);
                    break;
                default:
                    badge.ForeColor = Color.FromArgb(30, 64, 175);
                    badge.BackColor = Color.FromArgb(239, 246, 255);
                    badge.Tag = Color.FromArgb(191, 219, 254);
                    break;
            }
        }
        badge.Invalidate();
    }

    private Panel CreateSidebar()
    {
        var side = new Panel
        {
            Dock = DockStyle.Left,
            Width = 215,
            BackColor = SidebarBg,
            Padding = new Padding(12, 12, 12, 16)
        };
        _pnlSidebar = side;
        side.Paint += (s, e) =>
        {
            var border = _isCurrentDark ? Color.FromArgb(32, 42, 60) : Color.FromArgb(226, 232, 240);
            using var pen = new Pen(border, 1);
            e.Graphics.DrawLine(pen, side.Width - 1, 0, side.Width - 1, side.Height);
        };

        // Top Header of Sidebar (Brand + Collapse Button + Hide Button)
        _pnlSidebarHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(2, 0, 0, 6)
        };

        _lblSidebarBrand = new Label
        {
            Text = "E-TOOLBOX",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = PrimaryBlue,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            Width = 110
        };

        _btnHideSidebar = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Size = new Size(26, 28),
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 0, 0, 0)
        };
        _btnHideSidebar.FlatAppearance.BorderSize = 0;
        _btnHideSidebar.Click += (_, _) => SetSidebarHidden(true);

        _btnCollapseSidebar = new Button
        {
            Text = "«",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Size = new Size(30, 28),
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(226, 232, 240),
            BackColor = SidebarNavBg,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 2, 0)
        };
        _btnCollapseSidebar.FlatAppearance.BorderSize = 0;
        _btnCollapseSidebar.Click += (_, _) => ToggleCollapseSidebar();

        _pnlSidebarHeader.Controls.Add(_lblSidebarBrand);
        _pnlSidebarHeader.Controls.Add(_btnCollapseSidebar);
        _pnlSidebarHeader.Controls.Add(_btnHideSidebar);

        var pnlNav = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 268,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        pnlNav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

        _btnTabDashboard = CreateNavButton("⊞  Dashboard", 0);
        _btnTabCore = CreateNavButton("⚙  Chức năng", 1);
        _btnTabAdv = CreateNavButton("⚡  Nâng cao", 2);
        _btnTabDevice = CreateNavButton("☵  Thiết bị", 3);
        _btnTabSearch = CreateNavButton("🔍  Tìm kiếm", 4);

        pnlNav.Controls.Add(_btnTabDashboard, 0, 0);
        pnlNav.Controls.Add(_btnTabCore, 0, 1);
        pnlNav.Controls.Add(_btnTabAdv, 0, 2);
        pnlNav.Controls.Add(_btnTabDevice, 0, 3);
        pnlNav.Controls.Add(_btnTabSearch, 0, 4);

        // Bottom patch container with version info and gear settings button
        _pnlBottomSidebar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 110,
            BackColor = SidebarBg
        };

        _lblAppVersion = new Label
        {
            Text = "E-TOOLBOX\nv1.0.2",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 44
        };

        _btnSettings = new Button
        {
            Text = "⚙  Cài đặt",
            Dock = DockStyle.Bottom,
            Height = 44,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(226, 232, 240),
            BackColor = SidebarNavBg,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        _btnSettings.FlatAppearance.BorderSize = 1;
        _btnSettings.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
        _btnSettings.Click += (_, _) => OpenSettingsForm();

        _pnlBottomSidebar.Controls.Add(_btnSettings);
        _pnlBottomSidebar.Controls.Add(_lblAppVersion);

        // Setup Right-click Context Menu
        SetupSidebarContextMenu();

        side.Controls.Add(_pnlBottomSidebar);
        side.Controls.Add(pnlNav);
        side.Controls.Add(_pnlSidebarHeader);
        _pnlSidebarHeader.SendToBack();

        return side;
    }

    private void ToggleSidebar()
    {
        if (_isSidebarHidden)
        {
            SetSidebarHidden(false);
        }
        else
        {
            ToggleCollapseSidebar();
        }
    }

    private void ToggleCollapseSidebar()
    {
        if (_isSidebarHidden)
        {
            SetSidebarHidden(false);
            return;
        }

        _isSidebarCollapsed = !_isSidebarCollapsed;
        UpdateSidebarVisuals();
    }

    private void SetSidebarHidden(bool hidden)
    {
        _isSidebarHidden = hidden;
        _pnlSidebar.Visible = !hidden;
        _btnToggleSidebar.Visible = hidden;
        _btnToggleSidebar.Text = "☰";
        _btnToggleSidebar.BackColor = _isCurrentDark ? Color.FromArgb(37, 99, 235) : Color.FromArgb(219, 234, 254);

        _toolTip.SetToolTip(_btnToggleSidebar, _isEn ? "Show Main Sidebar (Ctrl+B)" : "Hiện thanh tác vụ chính (Ctrl+B)");
    }

    private void UpdateSidebarVisuals()
    {
        if (_isSidebarHidden)
        {
            _pnlSidebar.Visible = false;
            _btnToggleSidebar.Visible = true;
            return;
        }

        _pnlSidebar.Visible = true;
        _btnToggleSidebar.Visible = false;

        if (_isSidebarCollapsed)
        {
            _pnlSidebar.Width = 64;
            _pnlSidebar.Padding = new Padding(6, 10, 6, 12);

            _lblSidebarBrand.Visible = false;
            _btnHideSidebar.Visible = false;
            _btnCollapseSidebar.Dock = DockStyle.Fill;
            _btnCollapseSidebar.Text = "»";

            _btnTabDashboard.Text = "⊞";
            _btnTabDashboard.TextAlign = ContentAlignment.MiddleCenter;
            _btnTabDashboard.Padding = Padding.Empty;
            _btnTabDashboard.Font = new Font("Segoe UI", 12f, FontStyle.Bold);

            _btnTabCore.Text = "⚙";
            _btnTabCore.TextAlign = ContentAlignment.MiddleCenter;
            _btnTabCore.Padding = Padding.Empty;
            _btnTabCore.Font = new Font("Segoe UI", 12f, FontStyle.Bold);

            _btnTabAdv.Text = "⚡";
            _btnTabAdv.TextAlign = ContentAlignment.MiddleCenter;
            _btnTabAdv.Padding = Padding.Empty;
            _btnTabAdv.Font = new Font("Segoe UI", 12f, FontStyle.Bold);

            _btnTabDevice.Text = "☵";
            _btnTabDevice.TextAlign = ContentAlignment.MiddleCenter;
            _btnTabDevice.Padding = Padding.Empty;
            _btnTabDevice.Font = new Font("Segoe UI", 12f, FontStyle.Bold);

            _btnTabSearch.Text = "🔍";
            _btnTabSearch.TextAlign = ContentAlignment.MiddleCenter;
            _btnTabSearch.Padding = Padding.Empty;
            _btnTabSearch.Font = new Font("Segoe UI", 12f, FontStyle.Bold);

            _btnSettings.Text = "⚙";
            _btnSettings.TextAlign = ContentAlignment.MiddleCenter;
            _btnSettings.Padding = Padding.Empty;
            _btnSettings.Font = new Font("Segoe UI", 12f, FontStyle.Bold);

            _lblAppVersion.Text = "v1.0.2";
            _lblAppVersion.Height = 28;
            _lblAppVersion.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            _pnlBottomSidebar.Height = 85;

            _toolTip.SetToolTip(_btnTabDashboard, _isEn ? "Dashboard (F1)" : "Dashboard - Bảng điều khiển (F1)");
            _toolTip.SetToolTip(_btnTabCore, _isEn ? "Core Features (F2)" : "Chức năng hệ thống (F2)");
            _toolTip.SetToolTip(_btnTabAdv, _isEn ? "Advanced Tools (F3)" : "Công cụ nâng cao (F3)");
            _toolTip.SetToolTip(_btnTabDevice, _isEn ? "Devices & Sync (F4)" : "Quản lý thiết bị & Đồng bộ (F4)");
            _toolTip.SetToolTip(_btnTabSearch, _isEn ? "Search Features (F5 / Ctrl+F)" : "Tìm kiếm chức năng (F5 / Ctrl+F)");
            _toolTip.SetToolTip(_btnSettings, _isEn ? "Settings" : "Cài đặt ứng dụng");
            _toolTip.SetToolTip(_btnCollapseSidebar, _isEn ? "Expand Sidebar (Ctrl+B)" : "Mở rộng thanh tác vụ (Ctrl+B)");
        }
        else
        {
            _pnlSidebar.Width = 215;
            _pnlSidebar.Padding = new Padding(12, 12, 12, 16);

            _lblSidebarBrand.Visible = true;
            _btnHideSidebar.Visible = true;
            _btnCollapseSidebar.Dock = DockStyle.Right;
            _btnCollapseSidebar.Text = "«";

            _btnTabDashboard.Text = "⊞  Dashboard";
            _btnTabDashboard.TextAlign = ContentAlignment.MiddleLeft;
            _btnTabDashboard.Padding = new Padding(12, 0, 0, 0);
            _btnTabDashboard.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            _btnTabCore.Text = _isEn ? "⚙  Features" : "⚙  Chức năng";
            _btnTabCore.TextAlign = ContentAlignment.MiddleLeft;
            _btnTabCore.Padding = new Padding(12, 0, 0, 0);
            _btnTabCore.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            _btnTabAdv.Text = _isEn ? "⚡  Advanced" : "⚡  Nâng cao";
            _btnTabAdv.TextAlign = ContentAlignment.MiddleLeft;
            _btnTabAdv.Padding = new Padding(12, 0, 0, 0);
            _btnTabAdv.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            _btnTabDevice.Text = _isEn ? "☵  Devices" : "☵  Thiết bị";
            _btnTabDevice.TextAlign = ContentAlignment.MiddleLeft;
            _btnTabDevice.Padding = new Padding(12, 0, 0, 0);
            _btnTabDevice.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            _btnTabSearch.Text = _isEn ? "🔍  Search" : "🔍  Tìm kiếm";
            _btnTabSearch.TextAlign = ContentAlignment.MiddleLeft;
            _btnTabSearch.Padding = new Padding(12, 0, 0, 0);
            _btnTabSearch.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            _btnSettings.Text = _isEn ? "⚙  Settings" : "⚙  Cài đặt";
            _btnSettings.TextAlign = ContentAlignment.MiddleLeft;
            _btnSettings.Padding = new Padding(12, 0, 0, 0);
            _btnSettings.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            _lblAppVersion.Text = "E-TOOLBOX\nv1.0.2";
            _lblAppVersion.Height = 44;
            _lblAppVersion.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _pnlBottomSidebar.Height = 110;

            _toolTip.SetToolTip(_btnTabDashboard, "");
            _toolTip.SetToolTip(_btnTabCore, "");
            _toolTip.SetToolTip(_btnTabAdv, "");
            _toolTip.SetToolTip(_btnTabDevice, "");
            _toolTip.SetToolTip(_btnTabSearch, "");
            _toolTip.SetToolTip(_btnSettings, "");
            _toolTip.SetToolTip(_btnCollapseSidebar, _isEn ? "Collapse Sidebar (Ctrl+B)" : "Thu gọn thanh tác vụ (Ctrl+B)");
        }

        _toolTip.SetToolTip(_btnHideSidebar, _isEn ? "Hide Sidebar (Ctrl+B)" : "Ẩn thanh tác vụ (Ctrl+B)");
        _pnlSidebar.Invalidate();
    }

    private void SetupSidebarContextMenu()
    {
        var menu = new ContextMenuStrip();
        var itemExpand = new ToolStripMenuItem(_isEn ? "⊞ Expand Sidebar (215px)" : "⊞ Mở rộng thanh tác vụ (215px)");
        itemExpand.Click += (_, _) => { SetSidebarHidden(false); _isSidebarCollapsed = false; UpdateSidebarVisuals(); };

        var itemCollapse = new ToolStripMenuItem(_isEn ? "⇤ Collapse to Mini (64px)" : "⇤ Thu gọn thanh tác vụ (64px)");
        itemCollapse.Click += (_, _) => { SetSidebarHidden(false); _isSidebarCollapsed = true; UpdateSidebarVisuals(); };

        var itemHide = new ToolStripMenuItem(_isEn ? "✕ Hide Sidebar Completely (Ctrl+B)" : "✕ Ẩn hoàn toàn thanh tác vụ (Ctrl+B)");
        itemHide.Click += (_, _) => SetSidebarHidden(true);

        menu.Items.Add(itemExpand);
        menu.Items.Add(itemCollapse);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(itemHide);

        if (_btnToggleSidebar != null) _btnToggleSidebar.ContextMenuStrip = menu;
        if (_pnlSidebar != null) _pnlSidebar.ContextMenuStrip = menu;
        if (_lblSidebarBrand != null) _lblSidebarBrand.ContextMenuStrip = menu;
    }


    private Button CreateNavButton(string text, int tabIndex)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(226, 232, 240),
            BackColor = SidebarNavBg,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Margin = new Padding(0, 0, 0, 6),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => SwitchTab(tabIndex);
        return btn;
    }

    private void SwitchTab(int index)
    {
        _currentTabIndex = index;
        _pnlDashboard.Visible = (index == 0);
        _pnlCore.Visible = (index == 1);
        _pnlAdv.Visible = (index == 2);
        _pnlDevice.Visible = (index == 3);
        _pnlSearch.Visible = (index == 4);

        Color sideNavBg = _isCurrentDark ? Color.FromArgb(30, 41, 59) : Color.White;
        Color sideNavText = _isCurrentDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);

        var buttons = new[] { _btnTabDashboard, _btnTabCore, _btnTabAdv, _btnTabDevice, _btnTabSearch };
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].BackColor = (index == i) ? SidebarActive : sideNavBg;
            buttons[i].ForeColor = (index == i) ? Color.White : sideNavText;
            buttons[i].FlatAppearance.BorderColor = _isCurrentDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);
            buttons[i].FlatAppearance.BorderSize = _isCurrentDark ? 0 : 1;
        }

        if (index == 0)
        {
            _pnlDashboard.ResumeMonitoring();
        }
        else
        {
            _pnlDashboard.PauseMonitoring();
        }

        if (index == 4)
        {
            _txtSearch?.Focus();
        }
    }

    private void InitSettings()
    {
        ApplySettings(SettingsService.Instance.Settings);
        SettingsService.Instance.SettingsChanged += s =>
        {
            if (!IsDisposed && IsHandleCreated)
            {
                Invoke(() => ApplySettings(s));
            }
        };
    }

    private void ApplySettings(AppSettingsModel s)
    {
        bool langChanged = !string.Equals(_currentLanguage, s.Language, StringComparison.OrdinalIgnoreCase);
        _currentLanguage = s.Language;
        _isEn = s.Language.Equals("en", StringComparison.OrdinalIgnoreCase);

        Text = _isEn ? "E-TOOLBOX - IT Tools for Support" : "E-TOOLBOX - Công cụ Hỗ trợ Kỹ thuật IT";
        UpdateSidebarVisuals();
        SetupSidebarContextMenu();
        UpdateHeaderLanguage();

        _pnlDashboard.ApplyLanguage(s.Language);

        if (langChanged)
        {
            RebuildFeaturePanels();
        }

        ApplyGlobalTheme(s.Theme);
    }

    private void UpdateHeaderLanguage()
    {
        if (_lblStatusTitle != null)
        {
            _lblStatusTitle.Text = _isEn ? "● SYSTEM STATUS:" : "● TRẠNG THÁI HỆ THỐNG / SYSTEM STATUS:";
        }
        RefreshBadgesTheme();
        if (_lblPath != null)
        {
            if (_sysInfo != null && !string.IsNullOrEmpty(_sysInfo.RunningPath))
                _lblPath.Text = _isEn ? $"Working Directory: {_sysInfo.RunningPath}" : $"📂 Thư mục làm việc: {_sysInfo.RunningPath}";
            else
                _lblPath.Text = _isEn ? "📂 Working Directory: Loading..." : "📂 Thư mục làm việc: Đang nạp...";
        }
        if (_lblUser != null)
        {
            if (_sysInfo != null && !string.IsNullOrEmpty(_sysInfo.UserName))
                _lblUser.Text = _isEn ? $"Account: {_sysInfo.UserName} ({_sysInfo.MachineName})" : $"Tài khoản: {_sysInfo.UserName} ({_sysInfo.MachineName})";
            else
                _lblUser.Text = _isEn ? "Account: Loading..." : "Tài khoản: Đang nạp...";
        }
    }

    private void PopulateDevicePanelData()
    {
        if (_txtAppsScript == null || _txtSheetUrl == null || _cboMachineName == null) return;

        _deviceConfig = _sheetService.LoadConfig();
        _txtAppsScript.Text = _deviceConfig.AppsScriptUrl;
        _txtSheetUrl.Text = _deviceConfig.GoogleSheetUrl;

        _cboMachineName.Items.Clear();
        foreach (var m in _deviceConfig.MachineNames) _cboMachineName.Items.Add(m);
        if (_cboMachineName.Items.Count > 0) _cboMachineName.SelectedIndex = 0;

        _cboCompany.Items.Clear();
        foreach (var c in _deviceConfig.Companies) _cboCompany.Items.Add(c);
        if (_cboCompany.Items.Count > 0) _cboCompany.SelectedIndex = 0;

        _cboSyncDevice.Items.Clear();
        foreach (var m in _deviceConfig.MachineNames) _cboSyncDevice.Items.Add(m);
        if (_cboSyncDevice.Items.Count > 0) _cboSyncDevice.SelectedIndex = 0;
    }

    private void RebuildFeaturePanels()
    {
        if (_pnlContent == null) return;

        int activeIndex = _currentTabIndex;

        _pnlContent.SuspendLayout();

        if (_pnlCore != null)
        {
            _pnlContent.Controls.Remove(_pnlCore);
            _pnlCore.Dispose();
        }
        if (_pnlAdv != null)
        {
            _pnlContent.Controls.Remove(_pnlAdv);
            _pnlAdv.Dispose();
        }
        if (_pnlDevice != null)
        {
            _pnlContent.Controls.Remove(_pnlDevice);
            _pnlDevice.Dispose();
        }
        if (_pnlSearch != null)
        {
            _pnlContent.Controls.Remove(_pnlSearch);
            _pnlSearch.Dispose();
        }

        _pnlCore = BuildCoreFeaturesPanel();
        _pnlAdv = BuildAdvancedPanel();
        _pnlDevice = BuildDevicePanel();
        _pnlSearch = BuildSearchPanel();

        _pnlContent.Controls.Add(_pnlCore);
        _pnlContent.Controls.Add(_pnlAdv);
        _pnlContent.Controls.Add(_pnlDevice);
        _pnlContent.Controls.Add(_pnlSearch);

        _pnlContent.ResumeLayout(true);

        PopulateDevicePanelData();
        SwitchTab(activeIndex);
    }

    private void ApplyGlobalTheme(string theme)
    {
        bool isLight = theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        _isCurrentDark = !isLight;

        Color appBg = isLight ? Color.FromArgb(248, 250, 252) : Color.FromArgb(11, 14, 20);
        Color cardBg = isLight ? Color.White : Color.FromArgb(17, 23, 34);
        Color borderCol = isLight ? Color.FromArgb(226, 232, 240) : Color.FromArgb(32, 42, 60);
        Color textHead = isLight ? Color.FromArgb(15, 23, 42) : Color.FromArgb(241, 245, 249);
        Color textMuted = isLight ? Color.FromArgb(71, 85, 105) : Color.FromArgb(148, 163, 184);

        Color sideBg = isLight ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42);
        Color sideNavBg = isLight ? Color.White : Color.FromArgb(30, 41, 59);
        Color sideNavText = isLight ? Color.FromArgb(30, 41, 59) : Color.FromArgb(226, 232, 240);

        Color btnBg = isLight ? Color.FromArgb(241, 245, 249) : Color.FromArgb(24, 34, 52);
        Color btnBorder = isLight ? Color.FromArgb(203, 213, 225) : Color.FromArgb(45, 60, 88);
        Color btnText = isLight ? Color.FromArgb(30, 41, 59) : Color.FromArgb(226, 232, 240);

        BackColor = appBg;

        if (_pnlTop != null)
        {
            _pnlTop.BackColor = cardBg;
            if (_flowRow1 != null) _flowRow1.BackColor = cardBg;
            if (_pnlRow2 != null) _pnlRow2.BackColor = cardBg;
            _lblStatusTitle.ForeColor = textHead;
            _lblPath.ForeColor = textMuted;
            _lblUser.ForeColor = PrimaryBlue;
            if (_btnToggleSidebar != null)
            {
                _btnToggleSidebar.BackColor = isLight ? Color.FromArgb(241, 245, 249) : Color.FromArgb(30, 41, 59);
                _btnToggleSidebar.ForeColor = textHead;
                _btnToggleSidebar.FlatAppearance.BorderColor = borderCol;
            }
            RefreshBadgesTheme();
            _pnlTop.Invalidate();
        }

        if (_pnlSidebar != null)
        {
            _pnlSidebar.BackColor = sideBg;
            _pnlBottomSidebar.BackColor = sideBg;
            _lblAppVersion.ForeColor = textMuted;
            if (_lblSidebarBrand != null) _lblSidebarBrand.ForeColor = PrimaryBlue;
            if (_btnCollapseSidebar != null)
            {
                _btnCollapseSidebar.BackColor = sideNavBg;
                _btnCollapseSidebar.ForeColor = sideNavText;
                _btnCollapseSidebar.FlatAppearance.BorderColor = borderCol;
            }
            if (_btnHideSidebar != null)
            {
                _btnHideSidebar.ForeColor = textMuted;
            }

            _btnSettings.BackColor = sideNavBg;
            _btnSettings.ForeColor = sideNavText;
            _btnSettings.FlatAppearance.BorderColor = borderCol;
            _btnSettings.FlatAppearance.BorderSize = isLight ? 1 : 0;
            _pnlSidebar.Invalidate();
        }

        if (_pnlContent != null)
        {
            _pnlContent.BackColor = appBg;
            _pnlCore.BackColor = appBg;
            _pnlAdv.BackColor = appBg;
            _pnlDevice.BackColor = appBg;
            if (_pnlSearch != null)
            {
                _pnlSearch.BackColor = appBg;
                _flowSearchResults.BackColor = appBg;
                _lblSearchTitle.ForeColor = textHead;
                if (_lblSearchSub != null) _lblSearchSub.ForeColor = textMuted;
                if (_pnlSearchBox != null)
                {
                    _pnlSearchBox.BackColor = cardBg;
                    _pnlSearchBox.Invalidate();
                }
                if (_lblSearchIcon != null)
                {
                    _lblSearchIcon.ForeColor = isLight ? PrimaryBlue : Color.FromArgb(0, 240, 255);
                }
                if (_btnClearSearch != null)
                {
                    _btnClearSearch.BackColor = Color.Transparent;
                    _btnClearSearch.ForeColor = textMuted;
                }
                if (_txtSearch != null)
                {
                    _txtSearch.BackColor = cardBg;
                    _txtSearch.ForeColor = textHead;
                }
                if (_lblSearchResultCount != null)
                {
                    _lblSearchResultCount.ForeColor = isLight ? PrimaryBlue : Color.FromArgb(56, 189, 248);
                }
                foreach (var chip in _categoryChipButtons)
                {
                    UpdateChipAppearance(chip, (string)chip.Tag! == _selectedSearchCategory);
                }
                FilterSearchResults();
            }

            UpdateCardsAndButtonsInPanel(_pnlCore, cardBg, borderCol, textHead, btnBg, btnBorder, btnText, isLight);
            UpdateCardsAndButtonsInPanel(_pnlAdv, cardBg, borderCol, textHead, btnBg, btnBorder, btnText, isLight);
            UpdateDevicePanelTheme(cardBg, borderCol, textHead, textMuted, btnBg, btnBorder, btnText, isLight);
        }

        if (_pnlDashboard != null)
        {
            _pnlDashboard.ApplyTheme(theme);
        }

        SwitchTab(_currentTabIndex);
        Invalidate(true);
    }

    private static void UpdateCardsAndButtonsInPanel(Panel parent, Color cardBg, Color borderCol, Color textHead, Color btnBg, Color btnBorder, Color btnText, bool isLight)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is Panel card)
            {
                card.BackColor = cardBg;
                foreach (Control sub in card.Controls)
                {
                    if (sub is Label lbl)
                    {
                        lbl.ForeColor = textHead;
                    }
                    else if (sub is FlowLayoutPanel flow)
                    {
                        foreach (Control b in flow.Controls)
                        {
                            if (b is Button btn)
                            {
                                btn.BackColor = btnBg;
                                btn.ForeColor = btnText;
                                btn.FlatAppearance.BorderColor = btnBorder;
                                btn.FlatAppearance.MouseOverBackColor = isLight ? Color.FromArgb(224, 231, 255) : Color.FromArgb(37, 99, 235);
                            }
                        }
                    }
                }
                card.Invalidate();
            }
        }
    }

    private void UpdateDevicePanelTheme(Color cardBg, Color borderCol, Color textHead, Color textMuted, Color btnBg, Color btnBorder, Color btnText, bool isLight)
    {
        Color inputBg = isLight ? Color.White : Color.FromArgb(20, 28, 42);

        void ThemeControlTree(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is GroupBox gb)
                {
                    gb.BackColor = cardBg;
                    gb.ForeColor = textHead;
                    ThemeControlTree(gb);
                }
                else if (c is Panel p && p != _pnlDevice)
                {
                    p.BackColor = (p.Parent is GroupBox) ? cardBg : _pnlDevice.BackColor;
                    ThemeControlTree(p);
                }
                else if (c is Button btn)
                {
                    btn.BackColor = btnBg;
                    btn.ForeColor = btnText;
                    btn.FlatAppearance.BorderColor = btnBorder;
                    btn.FlatAppearance.MouseOverBackColor = isLight ? Color.FromArgb(224, 231, 255) : Color.FromArgb(37, 99, 235);
                }
                else if (c is TextBox tb)
                {
                    tb.BackColor = inputBg;
                    tb.ForeColor = textHead;
                }
                else if (c is ComboBox cb)
                {
                    cb.BackColor = inputBg;
                    cb.ForeColor = textHead;
                }
                else if (c is Label lbl)
                {
                    if (lbl.Font.Italic || lbl.Text.StartsWith("Link") || lbl.Text.StartsWith("Danh sách") || lbl.Text.StartsWith("Hai nút"))
                    {
                        lbl.ForeColor = textMuted;
                    }
                    else
                    {
                        lbl.ForeColor = textHead;
                    }
                }
                else if (c.HasChildren)
                {
                    ThemeControlTree(c);
                }
            }
        }

        ThemeControlTree(_pnlDevice);

        if (_rtbLog != null)
        {
            _rtbLog.BackColor = isLight ? Color.FromArgb(241, 245, 249) : Color.FromArgb(7, 10, 15);
            _rtbLog.ForeColor = isLight ? Color.FromArgb(15, 23, 42) : Color.FromArgb(16, 185, 129);
        }
    }

    private void RefreshBadgesTheme()
    {
        if (_lblBitLocker != null && _sysInfo != null)
        {
            string blText = _isEn
                ? (_sysInfo.HasBitLockerLocked ? $"BitLocker: {_sysInfo.BitLockerStatus}" : "BitLocker: Clean (Unlocked)")
                : (_sysInfo.HasBitLockerLocked ? $"BitLocker: {_sysInfo.BitLockerStatus}" : "BitLocker: Sạch (Chưa mã hóa)");
            SetBadge(_lblBitLocker, blText, _sysInfo.HasBitLockerLocked ? BadgeType.Danger : BadgeType.Success);
        }
        if (_lblPfx != null && _sysInfo != null)
        {
            string pfxText = _isEn
                ? (_sysInfo.HasPfxFiles ? "PFX Cert: Detected" : "PFX Cert: None")
                : _sysInfo.PfxStatusText;
            SetBadge(_lblPfx, pfxText, _sysInfo.HasPfxFiles ? BadgeType.Warning : BadgeType.Success);
        }
        if (_lblBoot != null && _sysInfo != null)
        {
            SetBadge(_lblBoot, $"Boot: {_sysInfo.BootMode}", BadgeType.Success);
        }
        if (_lblIrst != null && _sysInfo != null)
        {
            string irstText = _isEn
                ? (_sysInfo.NeedsIrst ? "Driver IRST: Needed" : "Driver IRST: Ready")
                : (_sysInfo.NeedsIrst ? "Driver IRST: Cần nạp" : "Driver IRST: Sẵn sàng");
            SetBadge(_lblIrst, irstText, _sysInfo.NeedsIrst ? BadgeType.Warning : BadgeType.Success);
        }
    }

    private void OpenSettingsForm()
    {
        using var dlg = new SettingsForm();
        dlg.ShowDialog(this);
    }

    #endregion

    #region Tab 1: Chức Năng (Core Features)

    private Panel BuildCoreFeaturesPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = BgApp
        };

        int topY = 6;

        // Section 1: Hệ thống
        var secSys = CreateCardSection(_isEn ? "SYSTEM & DIAGNOSTICS" : "HỆ THỐNG & CHẨN ĐOÁN (SYSTEM)", ref topY, new ToolBtn[]
        {
            new(_isEn ? "Windows Configuration" : "Thiết lập Windows", () => OpenWindowsConfigForm()),
            new(_isEn ? "Hardware Specs" : "Thông tin phần cứng", () => ShowDetailedSpecs()),
            new(_isEn ? "Turn Off BitLocker" : "Tắt BitLocker", () => _systemService.TurnOffBitLocker("C:")),
            new(_isEn ? "Create Master Image" : "Tạo Master Image", () => ShowMasterImageDialog()),
            new(_isEn ? "IRST / VMD Driver" : "Driver IRST / VMD", () => ShowIrstDriverDialog())
        });
        pnl.Controls.Add(secSys);

        // Section 2: Sao lưu - Khôi phục
        var secBackup = CreateCardSection(_isEn ? "BACKUP & RESTORE" : "SAO LƯU & PHỤC HỒI (BACKUP & RESTORE)", ref topY, new ToolBtn[]
        {
            new(_isEn ? "Backup Wi-Fi" : "Sao lưu Wi-Fi", () => ShowWifiBackupDialog()),
            new(_isEn ? "Backup Drivers" : "Sao lưu Driver", () => ShowDriverBackupDialog()),
            new(_isEn ? "Backup Data" : "Sao lưu Dữ liệu", () => ShowDataBackupDialog()),
            new(_isEn ? "Backup Browser" : "Sao lưu Trình duyệt", () => ShowBrowserBackupDialog()),
            new(_isEn ? "Credentials & Certificates" : "Mật khẩu & Chữ ký số", () => ShowCredentialAndCertificateDialog()),
            new(_isEn ? "OEM Driver Updates" : "Cập nhật Driver hãng", () => _backupService.OpenBrandDriverPage(_sysInfo.Manufacturer))
        });
        pnl.Controls.Add(secBackup);

        // Section 3: Cài đặt
        var secInstall = CreateCardSection(_isEn ? "SOFTWARE MANAGEMENT" : "QUẢN LÝ PHẦN MỀM (SOFTWARE MANAGEMENT)", ref topY, new ToolBtn[]
        {
            new(_isEn ? "Install Software" : "Cài đặt phần mềm", () => ShowSoftwareInstallerDialog()),
            new(_isEn ? "Batch Uninstaller" : "Gỡ phần mềm hàng loạt", () => ShowBatchUninstallerDialog()),
            new(_isEn ? "Remove Windows Bloatware" : "Gỡ Bloatware Windows", () => ShowBloatwareRemoverDialog())
        });
        pnl.Controls.Add(secInstall);

        // Section 4: WinPE - Cài Win
        var secWinPe = CreateCardSection(_isEn ? "WINPE & WINDOWS DEPLOYMENT" : "WINPE & TRIỂN KHAI WINDOWS", ref topY, new ToolBtn[]
        {
            new(_isEn ? "Create WinPE Boot" : "Tạo Boot WinPE", () => ShowWinPeBootDialog()),
            new(_isEn ? "Fix Partition Limits" : "Sửa giới hạn phân vùng", () => _diskService.FixPartitionLimits()),
            new(_isEn ? "Reinstall Windows" : "Cài đặt lại Windows", () => ShowWindowsDeploymentDialog(WindowsDeploymentForm.DeploymentMode.StandardReinstall)),
            new(_isEn ? "Official ISO Portal" : "Kho ISO chính thức", () => _maintenanceService.OpenUrl("https://www.microsoft.com/software-download")),
            new(_isEn ? "Clean WinPE Boot" : "Dọn dẹp Boot WinPE", () => _diskService.CleanWinPeBoot())
        });
        pnl.Controls.Add(secWinPe);

        // Section 5: Tiện ích khác
        var secOther = CreateCardSection(_isEn ? "UTILITIES & TOOLS" : "TIỆN ÍCH BỔ SUNG", ref topY, new ToolBtn[]
        {
            new(_isEn ? "System Cleaner" : "Dọn rác hệ thống", () => ShowSystemCleanerDialog()),
            new(_isEn ? "Hardware Diagnostics" : "Chẩn đoán phần cứng", () => ShowCheckTestDialog()),
            new(_isEn ? "Network Diagnostics" : "Chẩn đoán mạng", () => ShowNetworkDiagnosticDialog()),
            new(_isEn ? "Win 10/11 Context Menu" : "Menu chuột phải Win 10/11", () => ToggleContextMenuStyle()),
            new(_isEn ? "Automated Tasks" : "Tác vụ tự động hóa", () => ShowAutoTasksDialog()),
            new(_isEn ? "Remote Support" : "Hỗ trợ từ xa", () => ShowRemoteSupportDialog()),
            new(_isEn ? "Office Configuration" : "Thiết lập Office", () => ShowOfficeConfigDialog())
        });
        pnl.Controls.Add(secOther);

        pnl.Resize += (_, _) => RelayoutCards(pnl);
        pnl.HandleCreated += (_, _) => RelayoutCards(pnl);

        return pnl;
    }

    #endregion

    #region Tab 2: Nâng Cao (Advanced)

    private Panel BuildAdvancedPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = BgApp
        };

        int topY = 6;

        // Section 1: Update Windows
        var secUpdate = CreateCardSection(_isEn ? "WINDOWS UPDATE & RECOVERY" : "WINDOWS UPDATE & KHÔI PHỤC", ref topY, new ToolBtn[]
        {
            new(_isEn ? "Reset This PC" : "Khôi phục PC (Reset)", () =>
            {
                var msg = _isEn ? "Are you sure you want to open Windows Reset this PC?" : "Bạn có chắc chắn muốn mở trình khôi phục cài đặt gốc Windows (Reset this PC)?";
                var title = _isEn ? "Confirm Reset PC" : "Xác Nhận Reset PC";
                var res = MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes) SafeLaunch("systemreset.exe");
            }),
            new(_isEn ? "Block Windows Update" : "Chặn Windows Update", () =>
            {
                _systemService.ToggleWindowsUpdate(true);
                Log(_isEn ? "Blocked Windows Update automatic updates." : "Đã chặn tự động cập nhật Windows Update (Dịch vụ wuauserv = Disabled & Group Policy NoAutoUpdate = 1).");
                MessageBox.Show(_isEn ? "Successfully blocked automatic Windows Update!" : "Đã thiết lập chặn Windows Update tự động thành công!", _isEn ? "Notification" : "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }),
            new(_isEn ? "🛡️ Block Win 10->11" : "🛡️ Chặn nâng 10->11", () =>
            {
                _systemService.BlockWin11Upgrade(true);
                Log(_isEn ? "Blocked automatic Windows 11 upgrade via TargetReleaseVersion policy." : "Đã chặn tự động nâng cấp Windows 11 qua chính sách TargetReleaseVersion.");
                MessageBox.Show(_isEn ? "Blocked automatic upgrade to Windows 11 (pinned current version)!" : "Đã chặn nâng cấp tự động lên Windows 11 (khóa phiên bản hiện tại)!", _isEn ? "Notification" : "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }),
            new(_isEn ? "🔼 Upgrade / Downgrade Win" : "🔼 Nâng / Hạ Windows", () => SafeLaunch("ms-settings:activation", fallback: "slui.exe")),
            new(_isEn ? "🎯 Align Target Edition" : "🎯 Đưa Win về đúng bản", () =>
            {
                Log(_isEn ? "Querying Windows editions via DISM..." : "Đang truy vấn thông tin phiên bản Windows qua DISM...");
                var info = _systemService.GetWindowsEditionsInfo();
                Log(info);
                MessageBox.Show(info, _isEn ? "Windows Edition Info" : "Thông Tin Phiên Bản Windows & Khả Năng Nâng/Hạ Cấp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }),
            new(_isEn ? "🚀 Startup Apps" : "🚀 Startup (tự bật app)", () => SafeLaunch("taskmgr.exe", args: "/0 /startup"))
        });
        pnl.Controls.Add(secUpdate);

        // Section 2: Khi máy có sự cố
        var secIssue = CreateCardSection(_isEn ? "SYSTEM TROUBLESHOOTING" : "XỬ LÝ SỰ CỐ HỆ THỐNG", ref topY, new ToolBtn[]
        {
            new(_isEn ? "Network Diagnostics" : "Chẩn đoán mạng", () => ShowNetworkDiagnosticDialog()),
            new(_isEn ? "Fix Archive Associations" : "Sửa liên kết File nén", () =>
            {
                bool ok = _systemService.FixCompressedFileAssociations(Log);
                if (ok) MessageBox.Show(_isEn ? "Restored default archive associations (.zip, .7z, .rar) successfully!" : "Đã khôi phục liên kết mặc định cho file nén (.zip, .7z, .rar) thành công!", _isEn ? "Success" : "Sửa Lỗi File Nén", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else MessageBox.Show(_isEn ? "Error fixing archive associations. Please verify Administrator privileges." : "Có lỗi khi khôi phục liên kết file nén. Vui lòng kiểm tra quyền Administrator.", _isEn ? "Error" : "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }),
            new(_isEn ? "Reset Print Spooler" : "Sửa lỗi Print Spooler", () =>
            {
                bool ok = _systemService.ResetPrintSpooler(Log);
                if (ok) MessageBox.Show(_isEn ? "Cleared stuck print jobs and restarted Print Spooler successfully!" : "Đã xóa lệnh in kẹt và khởi động lại dịch vụ Print Spooler thành công!", _isEn ? "Printer Fix" : "Sửa Lỗi Máy In", MessageBoxButtons.OK, MessageBoxIcon.Information);
            })
        });
        pnl.Controls.Add(secIssue);

        // Section 3: Cài đặt Windows
        var secTransition = CreateCardSection(_isEn ? "WINDOWS EDITION MIGRATION" : "CHUYỂN ĐỔI PHIÊN BẢN WINDOWS", ref topY, new ToolBtn[]
        {
            new(_isEn ? "Clean Install Win (WinPE)" : "Cài Win sạch (WinPE)", () => ShowWindowsDeploymentDialog(WindowsDeploymentForm.DeploymentMode.WinPeCleanInstall)),
            new(_isEn ? "Downgrade to Windows 7" : "Hạ cấp về Windows 7", () => ShowWindowsDeploymentDialog(WindowsDeploymentForm.DeploymentMode.Windows7Downgrade)),
            new(_isEn ? "Upgrade to Win 10 / 11" : "Nâng cấp Win 10 / 11", () =>
            {
                Log(_isEn ? "Opening Windows Media Creation Tool..." : "Mở trang tải bộ công cụ nâng cấp Windows Media Creation Tool...");
                _maintenanceService.OpenUrl("https://www.microsoft.com/software-download/windows10");
            }),
            new(_isEn ? "Install Windows 7" : "Cài đặt Windows 7", () => ShowWindowsDeploymentDialog(WindowsDeploymentForm.DeploymentMode.Windows7Install))
        });
        pnl.Controls.Add(secTransition);

        // Section 4: Linux thay Windows
        var secLinuxReplace = CreateCardSection(_isEn ? "INSTALL LINUX TO REPLACE WINDOWS" : "CÀI ĐẶT LINUX THAY THẾ WINDOWS", ref topY, new ToolBtn[]
        {
            new(_isEn ? "Install Ubuntu Desktop" : "Cài Ubuntu Desktop", () => ShowLinuxReplaceWindowsDialog(LinuxDistro.Ubuntu2404)),
            new(_isEn ? "Install Debian" : "Cài Debian", () => ShowLinuxReplaceWindowsDialog(LinuxDistro.Debian12)),
            new(_isEn ? "Install Linux Mint" : "Cài Linux Mint", () => ShowLinuxReplaceWindowsDialog(LinuxDistro.LinuxMint22)),
            new(_isEn ? "Install Kali Linux" : "Cài Kali Linux", () => ShowLinuxReplaceWindowsDialog(LinuxDistro.KaliLinux))
        });
        pnl.Controls.Add(secLinuxReplace);

        // Section 5: Linux song song Windows
        var secLinuxDual = CreateCardSection(_isEn ? "INSTALL DUAL BOOT LINUX" : "CÀI ĐẶT LINUX SONG SONG (DUAL BOOT)", ref topY, new ToolBtn[]
        {
            new("Ubuntu LTS (Dual Boot)", () => ShowLinuxDualBootDialog(LinuxDistro.Ubuntu2404)),
            new("Debian (Dual Boot)", () => ShowLinuxDualBootDialog(LinuxDistro.Debian12)),
            new("Linux Mint (Dual Boot)", () => ShowLinuxDualBootDialog(LinuxDistro.LinuxMint22)),
            new("Kali Linux (Dual Boot)", () => ShowLinuxDualBootDialog(LinuxDistro.KaliLinux)),
            new(_isEn ? "Remove Linux & Restore Boot" : "Gỡ Linux & Khôi phục Boot", () =>
            {
                var msg = _isEn ? "Do you want to clean up Linux partitions and restore default Windows Bootloader?" : "Bạn có muốn dọn dẹp phân vùng Linux và khôi phục Windows Bootloader mặc định không?";
                var title = _isEn ? "Remove Linux & Restore Boot" : "Gỡ Linux & Phục Hồi Windows Boot";
                var res = MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    Log(_isEn ? "Running bootrec /fixmbr and bcdboot C:\\Windows..." : "Đang chạy bootrec /fixmbr và bcdboot C:\\Windows...");
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c bootrec /fixmbr & bcdboot C:\\Windows /l vi-VN") { CreateNoWindow = true, UseShellExecute = false });
                    _diskService.CleanWinPeBoot();
                    Log(_isEn ? "✅ Cleaned up partitions and restored Windows Bootloader." : "✅ Đã dọn dẹp phân vùng và phục hồi Windows Bootloader.");
                    MessageBox.Show(_isEn ? "Restored Windows Bootloader and cleaned boot menu!" : "Đã phục hồi Windows Bootloader và dọn dẹp menu khởi động!", _isEn ? "Completed" : "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            })
        });
        pnl.Controls.Add(secLinuxDual);

        // Section 6: Windows LTSC / Server
        var secLtsc = CreateCardSection("WINDOWS LTSC & SERVER", ref topY, new ToolBtn[]
        {
            new("Windows Server 2019", () => ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition.Server2019)),
            new("Windows Server 2022", () => ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition.Server2022)),
            new("Windows Server 2025", () => ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition.Server2025)),
            new("Windows 10 LTSC", () => ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition.Win10Ltsc)),
            new("Windows 11 LTSC", () => ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition.Win11Ltsc))
        });
        pnl.Controls.Add(secLtsc);

        pnl.Resize += (_, _) => RelayoutCards(pnl);
        pnl.HandleCreated += (_, _) => RelayoutCards(pnl);

        return pnl;
    }

    #endregion

    #region Tab 3: Thiết Bị (Google Sheet & Sync)

    private Panel BuildDevicePanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgApp,
            Padding = new Padding(12)
        };

        var pnlLeft = new Panel
        {
            Dock = DockStyle.Left,
            Width = 710,
            AutoScroll = true,
            Padding = new Padding(0, 0, 10, 0)
        };

        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 0, 0, 0)
        };

        // Section 1: Liên kết Google Sheet
        var gbSheet = new GroupBox
        {
            Text = _isEn ? "Google Sheet Connection" : "Liên kết Google Sheet",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = TextHeading,
            BackColor = CardBg,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 12, 16, 14),
            Margin = new Padding(0, 0, 0, 14)
        };

        var lblAppsScript = new Label
        {
            Text = _isEn ? "Apps Script Deployment URL (Backup PC / Restore PC / Up Sheet):" : "Link triển khai Apps Script (Backup PC / Restore PC / Up Sheet):",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtAppsScript = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Consolas", 9.25f),
            Margin = new Padding(0, 0, 0, 8)
        };

        var lblSheetUrl = new Label
        {
            Text = _isEn ? "Management Google Sheet URL (leave EMPTY to prompt via script):" : "Link Google Sheet quản lý thiết bị (để TRỐNG thì tool tự hỏi macro):",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtSheetUrl = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Consolas", 9.25f),
            Margin = new Padding(0, 0, 0, 10)
        };

        var flowSheetBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0)
        };

        var btnCheckConn = CreateFlowButton(_isEn ? "Test Connection" : "Kiểm tra kết nối", 150, () =>
        {
            Log(_isEn ? "Testing connection to Google Apps Script..." : "Đang kiểm tra kết nối tới Google Apps Script...");
            _ = Task.Run(async () =>
            {
                var ok = await _sheetService.CheckConnectionAsync(_txtAppsScript.Text.Trim());
                Log(ok ? (_isEn ? "✅ Google Apps Script connection SUCCESSFUL!" : "✅ Kết nối Google Apps Script THÀNH CÔNG!") : (_isEn ? "❌ Could not connect. Please check URL!" : "❌ Không thể kết nối. Vui lòng kiểm tra lại URL!"));
                Invoke(() => MessageBox.Show(ok ? (_isEn ? "Connection successful!" : "Kết nối thành công!") : (_isEn ? "Connection error. Please check URL!" : "Lỗi kết nối. Vui lòng kiểm tra URL!"), _isEn ? "Notification" : "Thông báo", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning));
            });
        });

        var btnSaveSheet = CreateFlowButton(_isEn ? "Save & Apply" : "Lưu & Áp dụng", 140, () =>
        {
            _deviceConfig.AppsScriptUrl = _txtAppsScript.Text.Trim();
            _deviceConfig.GoogleSheetUrl = _txtSheetUrl.Text.Trim();
            _sheetService.SaveConfig(_deviceConfig);
            Log(_isEn ? "Saved Google Sheet configuration successfully!" : "Đã lưu cấu hình Google Sheet thành công!");
            MessageBox.Show(_isEn ? "Saved Google Sheet configuration!" : "Đã lưu cấu hình Google Sheet!", _isEn ? "Notification" : "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });

        var btnOpenSheet = CreateFlowButton(_isEn ? "Open Sheet" : "Mở Google Sheet", 115, () =>
        {
            if (!string.IsNullOrWhiteSpace(_txtSheetUrl.Text))
            {
                _maintenanceService.OpenUrl(_txtSheetUrl.Text.Trim());
            }
            else
            {
                MessageBox.Show(_isEn ? "Please enter Google Sheet URL first!" : "Vui lòng nhập Link Google Sheet trước!", _isEn ? "Notification" : "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        });

        var btnDeploy = CreateFlowButton(_isEn ? "Deploy Script" : "Triển khai Apps Script", 155, () => ShowAppsScriptModal());

        flowSheetBtns.Controls.Add(btnCheckConn);
        flowSheetBtns.Controls.Add(btnSaveSheet);
        flowSheetBtns.Controls.Add(btnOpenSheet);
        flowSheetBtns.Controls.Add(btnDeploy);

        // Add to gbSheet in reverse order for Dock = Top
        gbSheet.Controls.Add(flowSheetBtns);
        gbSheet.Controls.Add(_txtSheetUrl);
        gbSheet.Controls.Add(lblSheetUrl);
        gbSheet.Controls.Add(_txtAppsScript);
        gbSheet.Controls.Add(lblAppsScript);

        // Section 2: Khởi tạo PC - chạy thẳng
        var gbInit = new GroupBox
        {
            Text = _isEn ? "Device Initialization & Licensing" : "Khởi tạo thiết bị & Bản quyền",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = TextHeading,
            BackColor = CardBg,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 12, 16, 14),
            Margin = new Padding(0, 0, 0, 14)
        };

        // Row 1: Tên máy
        var flowInit1 = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };
        var lblMachine = new Label { Text = _isEn ? "PC Name:" : "Tên máy:", AutoSize = true, Margin = new Padding(0, 8, 8, 0), Font = new Font("Segoe UI", 9f), ForeColor = TextHeading };
        _cboMachineName = new ComboBox { Size = new Size(180, 25), Margin = new Padding(0, 5, 8, 0), DropDownStyle = ComboBoxStyle.DropDown };
        var btnRename = CreateFlowButton(_isEn ? "Rename PC" : "Đổi tên máy", 125, () =>
        {
            var newName = _cboMachineName.Text.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                SafeLaunch("cmd.exe", args: $"/c wmic computersystem where caption='%computername%' rename {newName}");
                Log($"Đã gửi lệnh đổi tên máy thành: {newName}");
            }
        });
        var btnAddMachine = CreateFlowButton(_isEn ? "Add" : "Thêm", 90, () =>
        {
            var t = _cboMachineName.Text.Trim();
            if (!string.IsNullOrEmpty(t) && !_cboMachineName.Items.Contains(t))
            {
                _cboMachineName.Items.Add(t);
                _deviceConfig.MachineNames.Add(t);
                _sheetService.SaveConfig(_deviceConfig);
            }
        });
        var btnDelMachine = CreateFlowButton(_isEn ? "Delete" : "Xóa", 85, () =>
        {
            if (_cboMachineName.SelectedItem is string sel)
            {
                _cboMachineName.Items.Remove(sel);
                _deviceConfig.MachineNames.Remove(sel);
                _sheetService.SaveConfig(_deviceConfig);
            }
        });
        flowInit1.Controls.Add(lblMachine);
        flowInit1.Controls.Add(_cboMachineName);
        flowInit1.Controls.Add(btnRename);
        flowInit1.Controls.Add(btnAddMachine);
        flowInit1.Controls.Add(btnDelMachine);

        // Row 2: Công ty
        var flowInit2 = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };
        var lblComp = new Label { Text = _isEn ? "Company:" : "Công ty:", AutoSize = true, Margin = new Padding(0, 8, 8, 0), Font = new Font("Segoe UI", 9f), ForeColor = TextHeading };
        _cboCompany = new ComboBox { Size = new Size(180, 25), Margin = new Padding(0, 5, 8, 0), DropDownStyle = ComboBoxStyle.DropDown };
        var btnUpSheet = CreateFlowButton(_isEn ? "Upload to Sheet" : "Tải lên Sheet", 110, () => { _ = RealUploadSheetAsync(false); });
        var btnUpBoth = CreateFlowButton(_isEn ? "Upload Both Sheets" : "Tải lên cả 2 Sheet", 135, () => { _ = RealUploadSheetAsync(true); });
        var btnAddComp = CreateFlowButton(_isEn ? "Add" : "Thêm", 90, () =>
        {
            var t = _cboCompany.Text.Trim();
            if (!string.IsNullOrEmpty(t) && !_cboCompany.Items.Contains(t))
            {
                _cboCompany.Items.Add(t);
                _deviceConfig.Companies.Add(t);
                _sheetService.SaveConfig(_deviceConfig);
            }
        });
        flowInit2.Controls.Add(lblComp);
        flowInit2.Controls.Add(_cboCompany);
        flowInit2.Controls.Add(btnUpSheet);
        flowInit2.Controls.Add(btnUpBoth);
        flowInit2.Controls.Add(btnAddComp);

        // Row 3: Key & Export
        var flowInit3 = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };
        var btnExportGs = CreateFlowButton(_isEn ? "Export .gs Script" : "Xuất mã .gs", 130, () => ShowAppsScriptModal());
        var btnExtractKey = CreateFlowButton(_isEn ? "Extract Key" : "Trích xuất Key", 145, () =>
        {
            Log($"🔑 Windows Product Key: {_sysInfo.WindowsProductKey}");
            MessageBox.Show($"Windows Product Key:\n{_sysInfo.WindowsProductKey}", _isEn ? "Extract Key" : "Trích Xuất Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        var btnCompareKey = CreateFlowButton(_isEn ? "Compare Key" : "So sánh Key", 135, () => ShowKeyComparisonDialog());
        flowInit3.Controls.Add(btnExportGs);
        flowInit3.Controls.Add(btnExtractKey);
        flowInit3.Controls.Add(btnCompareKey);

        var lblInitNote = new Label
        {
            Text = _isEn
                ? "List: + add item · delete (selected item) - modifies local tool list only."
                : "Danh sách: + thêm mục · xoá (tất cả hoặc mục đang chọn) - chỉ sửa danh sách của tool.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = TextMuted,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Add in reverse order for Dock = Top
        gbInit.Controls.Add(lblInitNote);
        gbInit.Controls.Add(flowInit3);
        gbInit.Controls.Add(flowInit2);
        gbInit.Controls.Add(flowInit1);

        // Section 3: Đồng bộ với Google Sheet
        var gbSync = new GroupBox
        {
            Text = _isEn ? "Device Synchronization with Google Sheet" : "Đồng bộ thiết bị với Google Sheet",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = TextHeading,
            BackColor = CardBg,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 12, 16, 14),
            Margin = new Padding(0, 0, 0, 14)
        };

        var flowSync1 = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };

        _cboSyncDevice = new ComboBox { Size = new Size(200, 25), Margin = new Padding(0, 5, 8, 0), DropDownStyle = ComboBoxStyle.DropDownList };
        var btnRestorePc = CreateFlowButton(_isEn ? "Restore PC" : "Khôi phục PC", 125, () => { _ = RealRestorePcAsync(); });
        var btnCheckDup = CreateFlowButton(_isEn ? "Check Duplicate" : "Kiểm tra trùng lặp", 140, () => { _ = RealCheckDuplicateAsync(); });
        var btnReloadCfg = CreateFlowButton(_isEn ? "Reload Config" : "Tải lại cấu hình", 130, () => LoadSystemData());

        flowSync1.Controls.Add(_cboSyncDevice);
        flowSync1.Controls.Add(btnRestorePc);
        flowSync1.Controls.Add(btnCheckDup);
        flowSync1.Controls.Add(btnReloadCfg);

        var lblSyncNote = new Label
        {
            Text = _isEn
                ? "First two buttons query Google Sheet online (may take 30-60s, cancelable)."
                : "Hai nút đầu hỏi Google Sheet qua mạng (có thể mất 30-60 giây, có nút Hủy).",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = TextMuted,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        };

        gbSync.Controls.Add(lblSyncNote);
        gbSync.Controls.Add(flowSync1);

        // Add to pnlLeft in reverse order for Dock = Top
        pnlLeft.Controls.Add(gbSync);
        pnlLeft.Controls.Add(gbInit);
        pnlLeft.Controls.Add(gbSheet);

        // Right side: Nhật ký
        var gbLog = new GroupBox
        {
            Text = _isEn ? "Execution Log" : "Nhật ký thực thi (Execution Log)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = TextHeading,
            BackColor = CardBg,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 12, 10, 10)
        };

        var pnlLogTop = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 0, 0, 6), BackColor = Color.Transparent };
        _progressBar = new ProgressBar { Dock = DockStyle.Fill, Height = 26, Minimum = 0, Maximum = 100, Value = 0 };
        var btnClearLog = new Button
        {
            Text = _isEn ? "Clear Log" : "Xóa log",
            Dock = DockStyle.Right,
            Width = 95,
            Font = new Font("Segoe UI", 9f),
            BackColor = BtnBg,
            ForeColor = TextHeading,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnClearLog.FlatAppearance.BorderColor = BtnBorder;
        btnClearLog.FlatAppearance.MouseOverBackColor = BtnHover;
        btnClearLog.Click += (_, _) => _rtbLog.Clear();

        pnlLogTop.Controls.Add(_progressBar);
        pnlLogTop.Controls.Add(btnClearLog);

        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(134, 239, 172),
            Font = new Font("Consolas", 9.25f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };

        gbLog.Controls.Add(_rtbLog);
        gbLog.Controls.Add(pnlLogTop);
        pnlRight.Controls.Add(gbLog);

        pnl.Controls.Add(pnlRight);
        pnl.Controls.Add(pnlLeft);

        return pnl;
    }

    private Button CreateFlowButton(string text, int width, Action action)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, 34),
            Margin = new Padding(0, 0, 8, 6),
            Font = new Font("Segoe UI", 8.75f),
            BackColor = BtnBg,
            ForeColor = TextHeading,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = BtnBorder;
        btn.FlatAppearance.MouseOverBackColor = BtnHover;
        btn.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log($"[Lỗi thực thi {text}]: {ex.Message}");
            }
        };
        return btn;
    }

    #endregion

    #region Tab 4: Tìm Kiếm (Feature Search & Quick Launch)

    private class SearchableFeature
    {
        public string TitleVi { get; set; } = "";
        public string TitleEn { get; set; } = "";
        public string DescVi { get; set; } = "";
        public string DescEn { get; set; } = "";
        public string CategoryVi { get; set; } = "";
        public string CategoryEn { get; set; } = "";
        public string CategoryKey { get; set; } = "all";
        public string Icon { get; set; } = "⚡";
        public string Keywords { get; set; } = "";
        public Action Action { get; set; } = null!;
        public int TargetTab { get; set; } = -1;
    }

    private Panel BuildSearchPanel()
    {
        InitSearchFeatures();

        Color appBg = _isCurrentDark ? Color.FromArgb(11, 14, 20) : BgApp;
        Color cardBg = _isCurrentDark ? Color.FromArgb(17, 23, 34) : CardBg;
        Color borderCol = _isCurrentDark ? Color.FromArgb(45, 60, 88) : Color.FromArgb(203, 213, 225);
        Color textHead = _isCurrentDark ? Color.FromArgb(241, 245, 249) : TextHeading;
        Color textMuted = _isCurrentDark ? Color.FromArgb(148, 163, 184) : TextMuted;
        Color accentCol = _isCurrentDark ? Color.FromArgb(0, 240, 255) : PrimaryBlue;

        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = appBg,
            Padding = new Padding(16, 12, 16, 12)
        };

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 162,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 8)
        };

        _lblSearchTitle = new Label
        {
            Text = _isEn ? "🔍 FEATURE SEARCH & QUICK LAUNCH" : "🔍 TÌM KIẾM CHỨC NĂNG & KHỞI CHẠY NHANH",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = textHead,
            Dock = DockStyle.Top,
            Height = 28,
            UseMnemonic = false
        };

        _lblSearchSub = new Label
        {
            Text = _isEn
                ? "Type keywords to instantly find and launch any tool in E-TOOLBOX. Click any card to run."
                : "Nhập từ khóa để tìm kiếm tức thì mọi công cụ trong E-TOOLBOX. Nhấn thẻ hoặc nút để khởi chạy ngay.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = textMuted,
            Dock = DockStyle.Top,
            Height = 22,
            UseMnemonic = false
        };

        // Search Input Box
        _pnlSearchBox = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = cardBg,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(10, 6, 10, 6)
        };
        _pnlSearchBox.Paint += (s, e) =>
        {
            var bCol = _isCurrentDark ? Color.FromArgb(45, 60, 88) : Color.FromArgb(203, 213, 225);
            using var pen = new Pen(bCol, 1.5f);
            e.Graphics.DrawRectangle(pen, 0, 0, _pnlSearchBox.Width - 1, _pnlSearchBox.Height - 1);
        };

        _lblSearchIcon = new Label
        {
            Text = "🔍",
            Font = new Font("Segoe UI", 11f),
            ForeColor = accentCol,
            BackColor = Color.Transparent,
            Dock = DockStyle.Left,
            Width = 30,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _btnClearSearch = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = textMuted,
            BackColor = Color.Transparent,
            Dock = DockStyle.Right,
            Width = 28,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnClearSearch.FlatAppearance.BorderSize = 0;
        _btnClearSearch.Click += (_, _) =>
        {
            _txtSearch.Text = "";
            _txtSearch.Focus();
        };

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 11f),
            BackColor = cardBg,
            ForeColor = textHead,
            PlaceholderText = _isEn
                ? "Type keywords to search (e.g. driver, office, keyboard, reinstall, wifi, bitlocker, vmd)..."
                : "Nhập từ khóa tìm kiếm (ví dụ: driver, office, phím, cài win, wifi, bitlocker, vmd, test)..."
        };
        _txtSearch.TextChanged += (_, _) => FilterSearchResults();

        _pnlSearchBox.Controls.Add(_txtSearch);
        _pnlSearchBox.Controls.Add(_lblSearchIcon);
        _pnlSearchBox.Controls.Add(_btnClearSearch);

        // Category Chips Row
        var pnlChips = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0),
            WrapContents = false,
            AutoScroll = false
        };

        var categories = new (string key, string vi, string en)[]
        {
            ("all", "Tất cả", "All"),
            ("win", "💻 Cài Win & Boot", "💻 Windows & Boot"),
            ("driver", "💾 Driver & Sao lưu", "💾 Drivers & Backup"),
            ("office", "📝 Văn phòng & Office", "📝 Office & Apps"),
            ("test", "🛠️ Chẩn đoán & Test", "🛠️ Diagnostics & Test"),
            ("trouble", "⚡ Tối ưu & Sửa lỗi", "⚡ Tweaks & Fixes"),
            ("device", "🌐 Thiết bị & Sheet", "🌐 Devices & Sync")
        };

        _categoryChipButtons.Clear();
        foreach (var (k, vi, en) in categories)
        {
            var chip = new Button
            {
                Text = _isEn ? en : vi,
                Tag = k,
                Height = 28,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 0),
                Padding = new Padding(8, 0, 8, 0)
            };
            chip.FlatAppearance.BorderSize = 1;
            UpdateChipAppearance(chip, k == _selectedSearchCategory);

            chip.Click += (s, e) =>
            {
                _selectedSearchCategory = (string)((Button)s!).Tag!;
                foreach (var b in _categoryChipButtons)
                {
                    UpdateChipAppearance(b, (string)b.Tag! == _selectedSearchCategory);
                }
                FilterSearchResults();
            };

            _categoryChipButtons.Add(chip);
            pnlChips.Controls.Add(chip);
        }

        // Result Count Label
        _lblSearchResultCount = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
            ForeColor = _isCurrentDark ? Color.FromArgb(56, 189, 248) : Color.FromArgb(14, 165, 233),
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };

        pnlHeader.Controls.Add(_lblSearchResultCount);
        pnlHeader.Controls.Add(pnlChips);
        pnlHeader.Controls.Add(_pnlSearchBox);
        pnlHeader.Controls.Add(_lblSearchSub);
        pnlHeader.Controls.Add(_lblSearchTitle);

        pnl.Controls.Add(pnlHeader);

        // Results Flow Panel
        _flowSearchResults = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = appBg,
            Padding = new Padding(0, 4, 0, 8),
            WrapContents = true
        };
        pnl.Controls.Add(_flowSearchResults);
        _flowSearchResults.BringToFront();

        FilterSearchResults();

        pnl.Resize += (_, _) => AdjustSearchCardSizes();

        return pnl;
    }

    private Control CreateFeatureCard(SearchableFeature f, int cardWidth)
    {
        Color currentCardBg = _isCurrentDark ? Color.FromArgb(17, 23, 34) : Color.White;
        Color currentBorderCol = _isCurrentDark ? Color.FromArgb(45, 60, 88) : Color.FromArgb(226, 232, 240);
        Color currentHoverBg = _isCurrentDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249);
        Color currentTextHead = _isCurrentDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42);
        Color currentTextMuted = _isCurrentDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
        Color currentCatCol = _isCurrentDark ? Color.FromArgb(56, 189, 248) : Color.FromArgb(2, 132, 199);
        Color currentIconCol = _isCurrentDark ? Color.FromArgb(0, 240, 255) : PrimaryBlue;

        var card = new Panel
        {
            Width = cardWidth,
            Height = 96,
            BackColor = currentCardBg,
            Margin = new Padding(0, 0, 10, 10),
            Padding = new Padding(10, 8, 10, 8),
            Cursor = Cursors.Hand
        };

        Color activeBorderCol = currentBorderCol;
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(activeBorderCol, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // Icon Box (Left)
        var lblIcon = new Label
        {
            Text = f.Icon,
            Font = new Font("Segoe UI", 15f),
            ForeColor = currentIconCol,
            BackColor = Color.Transparent,
            Location = new Point(8, 14),
            Size = new Size(38, 38),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };

        // Action Button (Right)
        var btnAction = new Button
        {
            Text = _isEn ? "🚀 Open" : "🚀 Mở",
            Location = new Point(cardWidth - 84, 28),
            Size = new Size(72, 34),
            BackColor = SidebarActive,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnAction.FlatAppearance.BorderSize = 0;
        btnAction.Click += (_, _) => ExecuteFeature(f);

        // Category Tag
        var lblCat = new Label
        {
            Text = _isEn ? f.CategoryEn.ToUpperInvariant() : f.CategoryVi.ToUpperInvariant(),
            Font = new Font("Segoe UI", 7.25f, FontStyle.Bold),
            ForeColor = currentCatCol,
            BackColor = Color.Transparent,
            Location = new Point(50, 8),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        // Title
        var lblTitle = new Label
        {
            Text = _isEn ? f.TitleEn : f.TitleVi,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = currentTextHead,
            BackColor = Color.Transparent,
            Location = new Point(50, 24),
            Size = new Size(cardWidth - 140, 22),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };

        // Description
        var lblDesc = new Label
        {
            Text = _isEn ? f.DescEn : f.DescVi,
            Font = new Font("Segoe UI", 8f),
            ForeColor = currentTextMuted,
            BackColor = Color.Transparent,
            Location = new Point(50, 48),
            Size = new Size(cardWidth - 140, 38),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };

        void SetHover(bool h)
        {
            card.BackColor = h ? currentHoverBg : currentCardBg;
            activeBorderCol = h ? SidebarActive : currentBorderCol;
            card.Invalidate();
        }

        foreach (Control c in new Control[] { card, lblIcon, lblCat, lblTitle, lblDesc })
        {
            c.MouseEnter += (_, _) => SetHover(true);
            c.MouseLeave += (_, _) => SetHover(false);
            c.Click += (_, _) => ExecuteFeature(f);
        }

        card.Controls.Add(lblIcon);
        card.Controls.Add(lblCat);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblDesc);
        card.Controls.Add(btnAction);

        return card;
    }

    private void ExecuteFeature(SearchableFeature f)
    {
        try
        {
            if (f.TargetTab >= 0)
            {
                SwitchTab(f.TargetTab);
            }
            f.Action?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi thực thi '{(_isEn ? f.TitleEn : f.TitleVi)}':\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FilterSearchResults()
    {
        if (_flowSearchResults == null) return;

        string query = _txtSearch?.Text.Trim().ToLowerInvariant() ?? "";
        string cat = _selectedSearchCategory;

        var matches = _allSearchFeatures.Where(f =>
        {
            if (cat != "all" && f.CategoryKey != cat) return false;
            if (string.IsNullOrEmpty(query)) return true;

            return f.TitleVi.ToLowerInvariant().Contains(query) ||
                   f.TitleEn.ToLowerInvariant().Contains(query) ||
                   f.DescVi.ToLowerInvariant().Contains(query) ||
                   f.DescEn.ToLowerInvariant().Contains(query) ||
                   f.Keywords.ToLowerInvariant().Contains(query);
        }).ToList();

        _flowSearchResults.SuspendLayout();
        _flowSearchResults.Controls.Clear();

        int availableWidth = Math.Max(320, _flowSearchResults.ClientSize.Width - 24);
        int cols = Math.Max(1, availableWidth / 360);
        int cardWidth = (availableWidth / cols) - 12;
        if (cardWidth < 320) cardWidth = availableWidth;

        if (matches.Count == 0)
        {
            var pnlEmpty = new Panel
            {
                Width = availableWidth,
                Height = 180,
                BackColor = Color.Transparent
            };
            var lblEmpty = new Label
            {
                Text = _isEn
                    ? $"🔍 No features found matching \"{query}\"\nTry searching with different keywords like 'driver', 'win', 'office', 'test'..."
                    : $"🔍 Không tìm thấy chức năng nào phù hợp với \"{query}\"\nHãy thử tìm với các từ khóa khác như 'driver', 'win', 'office', 'phím', 'wifi'...",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = _isCurrentDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmpty.Controls.Add(lblEmpty);
            _flowSearchResults.Controls.Add(pnlEmpty);

            if (_lblSearchResultCount != null)
                _lblSearchResultCount.Text = _isEn ? "No matching features found" : "Không tìm thấy chức năng phù hợp";
        }
        else
        {
            foreach (var f in matches)
            {
                _flowSearchResults.Controls.Add(CreateFeatureCard(f, cardWidth));
            }

            if (_lblSearchResultCount != null)
                _lblSearchResultCount.Text = _isEn
                    ? $"Showing {matches.Count} matching feature{(matches.Count > 1 ? "s" : "")}"
                    : $"Tìm thấy {matches.Count} chức năng phù hợp";
        }

        _flowSearchResults.ResumeLayout();
    }

    private void AdjustSearchCardSizes()
    {
        if (_flowSearchResults == null || _flowSearchResults.Controls.Count == 0) return;
        int availableWidth = Math.Max(320, _flowSearchResults.ClientSize.Width - 24);
        int cols = Math.Max(1, availableWidth / 360);
        int cardWidth = (availableWidth / cols) - 12;
        if (cardWidth < 320) cardWidth = availableWidth;

        _flowSearchResults.SuspendLayout();
        foreach (Control c in _flowSearchResults.Controls)
        {
            if (c is Panel p && p.Tag == null)
            {
                p.Width = cardWidth;
            }
        }
        _flowSearchResults.ResumeLayout();
    }

    private void UpdateChipAppearance(Button chip, bool isSelected)
    {
        if (isSelected)
        {
            chip.BackColor = SidebarActive;
            chip.ForeColor = Color.White;
            chip.FlatAppearance.BorderColor = SidebarActive;
            chip.FlatAppearance.MouseOverBackColor = SidebarActive;
        }
        else
        {
            chip.BackColor = _isCurrentDark ? Color.FromArgb(24, 34, 52) : Color.FromArgb(241, 245, 249);
            chip.ForeColor = _isCurrentDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(51, 65, 85);
            chip.FlatAppearance.BorderColor = _isCurrentDark ? Color.FromArgb(45, 60, 88) : Color.FromArgb(203, 213, 225);
            chip.FlatAppearance.MouseOverBackColor = _isCurrentDark ? Color.FromArgb(32, 45, 68) : Color.FromArgb(224, 231, 255);
        }
    }

    private void InitSearchFeatures()
    {
        if (_allSearchFeatures.Count > 0) return;

        _allSearchFeatures = new List<SearchableFeature>
        {
            // === DRIVER & BACKUP ===
            new()
            {
                Icon = "💾",
                TitleVi = "Sao lưu Driver & Wi-Fi",
                TitleEn = "Backup Drivers & Wi-Fi",
                DescVi = "Trích xuất driver toàn bộ hệ thống hoặc chỉ driver thiết yếu (Wi-Fi, Mạng, IRST) ra thư mục lưu trữ an toàn.",
                DescEn = "Extract system drivers or essential Wi-Fi & network drivers to a backup folder for instant restore.",
                CategoryVi = "Driver & Sao lưu",
                CategoryEn = "Drivers & Backup",
                CategoryKey = "driver",
                Keywords = "driver wifi sao luu trich xuat restore mang oem backup",
                Action = () => ShowDriverBackupDialog()
            },
            new()
            {
                Icon = "⚡",
                TitleVi = "Tải & Nạp Driver IRST / Intel VMD",
                TitleEn = "Intel IRST / VMD Driver",
                DescVi = "Tự động tải và giải nén driver Intel Rapid Storage Technology khi cài Win không nhìn thấy ổ cứng.",
                DescEn = "Auto download and extract Intel RST / VMD drivers to fix invisible hard drive during Windows install.",
                CategoryVi = "Driver & Sao lưu",
                CategoryEn = "Drivers & Backup",
                CategoryKey = "driver",
                Keywords = "irst vmd rapid storage intel khong nhan o dia cai win ssd nvme",
                Action = () => ShowIrstDriverDialog()
            },
            new()
            {
                Icon = "📶",
                TitleVi = "Sao lưu Cấu hình Wi-Fi",
                TitleEn = "Backup Wi-Fi Profiles",
                DescVi = "Sao lưu toàn bộ danh sách mạng Wi-Fi và mật khẩu đã kết nối trên máy tính.",
                DescEn = "Backup all connected Wi-Fi networks and saved security passwords.",
                CategoryVi = "Driver & Sao lưu",
                CategoryEn = "Drivers & Backup",
                CategoryKey = "driver",
                Keywords = "wifi mang mat khau password sao luu profile",
                Action = () => ShowWifiBackupDialog()
            },
            new()
            {
                Icon = "📁",
                TitleVi = "Sao lưu Dữ liệu Người Dùng",
                TitleEn = "Backup User Data",
                DescVi = "Sao lưu thư mục Desktop, Documents, Downloads, Pictures của tài khoản người dùng.",
                DescEn = "Backup user profile folders including Desktop, Documents, Downloads, and Pictures.",
                CategoryVi = "Driver & Sao lưu",
                CategoryEn = "Drivers & Backup",
                CategoryKey = "driver",
                Keywords = "data du lieu desktop document download picture backup",
                Action = () => ShowDataBackupDialog()
            },
            new()
            {
                Icon = "🌐",
                TitleVi = "Sao lưu Trình duyệt (Bookmarks)",
                TitleEn = "Backup Browser Data",
                DescVi = "Sao lưu lịch sử, dấu trang (bookmarks) của Google Chrome, Microsoft Edge, Cốc Cốc.",
                DescEn = "Backup browsing history, bookmarks, and sessions from Chrome, Edge, and CocCoc.",
                CategoryVi = "Driver & Sao lưu",
                CategoryEn = "Drivers & Backup",
                CategoryKey = "driver",
                Keywords = "browser chrome edge coccoc bookmark dau trang du lieu web",
                Action = () => ShowBrowserBackupDialog()
            },
            new()
            {
                Icon = "🔑",
                TitleVi = "Mật khẩu & Chữ ký số",
                TitleEn = "Credentials & Certificates",
                DescVi = "Sao lưu và quản lý chứng thư số (Certificates) và thông tin đăng nhập Windows Credentials.",
                DescEn = "Backup and manage digital certificates and Windows Credential Manager passwords.",
                CategoryVi = "Driver & Sao lưu",
                CategoryEn = "Drivers & Backup",
                CategoryKey = "driver",
                Keywords = "credentials certificate chu ky so mat khau token",
                Action = () => ShowCredentialAndCertificateDialog()
            },
            new()
            {
                Icon = "🏢",
                TitleVi = "Cập nhật Driver Hãng (OEM)",
                TitleEn = "OEM Brand Driver Portal",
                DescVi = "Mở nhanh trang hỗ trợ tải driver chính hãng tự động nhận diện theo máy (Dell, HP, Asus, Lenovo...).",
                DescEn = "Quickly open official manufacturer support page for Dell, HP, Asus, Lenovo, Acer, etc.",
                CategoryVi = "Driver & Sao lưu",
                CategoryEn = "Drivers & Backup",
                CategoryKey = "driver",
                Keywords = "oem driver hang dell hp asus lenovo acer msi trang chu update",
                Action = () => _backupService.OpenBrandDriverPage(_sysInfo.Manufacturer)
            },

            // === VĂN PHÒNG & SOFTWARE ===
            new()
            {
                Icon = "📝",
                TitleVi = "Cấu hình & Tải Office Tùy chỉnh",
                TitleEn = "Custom Office Configuration & Download",
                DescVi = "Tùy biến bộ cài Office C2R sạch: Chọn Word, Excel, PowerPoint, Outlook... phiên bản 2021, 2024, 365.",
                DescEn = "Custom clean Office C2R deployment: Select Word, Excel, PowerPoint, Outlook... for 2021, 2024, 365.",
                CategoryVi = "Văn phòng & Office",
                CategoryEn = "Office & Software",
                CategoryKey = "office",
                Keywords = "office word excel powerpoint outlook access c2r cai dat tai office 2021 2024 365",
                Action = () => ShowOfficeConfigDialog()
            },
            new()
            {
                Icon = "📦",
                TitleVi = "Cài đặt Phần mềm Thiết yếu",
                TitleEn = "Install Essential Software",
                DescVi = "Cài đặt hàng loạt phần mềm phổ biến: Unikey, WinRAR, Chrome, UltraViewer, AnyDesk, PDF, v.v.",
                DescEn = "Batch install popular software: Unikey, WinRAR, Chrome, UltraViewer, AnyDesk, PDF, etc.",
                CategoryVi = "Văn phòng & Office",
                CategoryEn = "Office & Software",
                CategoryKey = "office",
                Keywords = "software cai phan mem unikey winrar chrome anydesk ultraviewer installer",
                Action = () => ShowSoftwareInstallerDialog()
            },
            new()
            {
                Icon = "🗑️",
                TitleVi = "Gỡ phần mềm hàng loạt",
                TitleEn = "Batch Software Uninstaller",
                DescVi = "Quét danh sách ứng dụng đã cài đặt và gỡ bỏ hàng loạt nhanh chóng, sạch sẽ.",
                DescEn = "Scan installed applications and uninstall multiple programs cleanly.",
                CategoryVi = "Văn phòng & Office",
                CategoryEn = "Office & Software",
                CategoryKey = "office",
                Keywords = "uninstaller go phan mem batch xoa ung dung",
                Action = () => ShowBatchUninstallerDialog()
            },

            // === CHẨN ĐOÁN & TEST ===
            new()
            {
                Icon = "⌨️",
                TitleVi = "Kiểm tra Bàn Phím (Keyboard Test)",
                TitleEn = "Interactive Keyboard Test",
                DescVi = "Giao diện bàn phím trực quan, test phím liệt, phím kẹt, kiểm tra đèn LED, âm thanh gõ và n-key rollover.",
                DescEn = "Interactive virtual keyboard to test stuck keys, key rollover, typing sound and LEDs.",
                CategoryVi = "Chẩn đoán & Test",
                CategoryEn = "Diagnostics & Test",
                CategoryKey = "test",
                Keywords = "keyboard ban phim liet phim ket phim test phim key nkey testbanphim",
                Action = () => ShowKeyboardTestDialog()
            },
            new()
            {
                Icon = "🩺",
                TitleVi = "Chẩn đoán Phần cứng Toàn diện",
                TitleEn = "Hardware Health Diagnostics",
                DescVi = "Kiểm tra sức khỏe Pin (Battery Health), quét Bad Sector ổ cứng SSD/HDD, đo tốc độ Read/Write và nhiệt độ.",
                DescEn = "Check battery health and cycle count, SSD/HDD bad sectors, benchmark read/write speeds and temps.",
                CategoryVi = "Chẩn đoán & Test",
                CategoryEn = "Diagnostics & Test",
                CategoryKey = "test",
                Keywords = "check test phan cung pin battery health disk o cung ssd nvme speed toc do benchmark",
                Action = () => ShowCheckTestDialog()
            },
            new()
            {
                Icon = "ℹ️",
                TitleVi = "Thông tin Phần cứng Chi tiết",
                TitleEn = "Detailed Hardware Specifications",
                DescVi = "Xem thông số chi tiết CPU, RAM, GPU, Bo mạch chủ, Ổ cứng, Serial máy, Ngày sản xuất, BIOS.",
                DescEn = "View comprehensive hardware specs: CPU, RAM, GPU, Motherboard, Storage, Serial, BIOS.",
                CategoryVi = "Chẩn đoán & Test",
                CategoryEn = "Diagnostics & Test",
                CategoryKey = "test",
                Keywords = "hardware info specs phan cung cpu ram gpu serial bios motherboard thong tin",
                Action = () => ShowDetailedSpecs()
            },
            new()
            {
                Icon = "⊞",
                TitleVi = "Bảng điều khiển HUD & Giám sát Phần cứng",
                TitleEn = "System HUD & Hardware Dashboard",
                DescVi = "Theo dõi thời gian thực xung nhịp, nhiệt độ, quạt tản nhiệt, tải CPU/GPU (dGPU vs iGPU), RAM và công suất điện.",
                DescEn = "Real-time monitoring of CPU/GPU clocks, temperatures, dual fans, RAM, storage and power wattage.",
                CategoryVi = "Chẩn đoán & Test",
                CategoryEn = "Diagnostics & Test",
                CategoryKey = "test",
                Keywords = "dashboard hud cpu gpu quat fan rpm nhiet do xung nhip power watt telemetry",
                Action = () => SwitchTab(0)
            },

            // === CÀI WIN & BOOT ===
            new()
            {
                Icon = "💻",
                TitleVi = "Cài đặt lại Windows (Chuẩn)",
                TitleEn = "Reinstall Windows (Standard)",
                DescVi = "Triển khai cài đặt lại Windows chuẩn trực tiếp từ bộ cài ISO, hỗ trợ bypass TPM và CPU cũ.",
                DescEn = "Standard Windows deployment from ISO image with TPM & CPU bypass support.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "reinstall windows cai lai win cai win 10 11 deployment",
                Action = () => ShowWindowsDeploymentDialog(WindowsDeploymentForm.DeploymentMode.StandardReinstall)
            },
            new()
            {
                Icon = "⚡",
                TitleVi = "Cài Win Sạch trong WinPE",
                TitleEn = "WinPE Clean Install Windows",
                DescVi = "Triển khai cài đặt Windows sạch hoàn toàn từ môi trường cứu hộ WinPE.",
                DescEn = "Deploy fresh Windows installation directly inside the WinPE rescue environment.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "clean install winpe cai win sach deployment",
                Action = () => ShowWindowsDeploymentDialog(WindowsDeploymentForm.DeploymentMode.WinPeCleanInstall)
            },
            new()
            {
                Icon = "🪟",
                TitleVi = "Tạo Boot WinPE Cứu Hộ",
                TitleEn = "Create WinPE Rescue Boot",
                DescVi = "Tạo USB hoặc phân vùng khởi động WinPE đầy đủ công cụ cứu hộ máy tính.",
                DescEn = "Create a bootable rescue WinPE USB or disk partition with essential IT diagnostic tools.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "winpe boot usb cuu ho rufus mini windows",
                Action = () => ShowWinPeBootDialog()
            },
            new()
            {
                Icon = "🏢",
                TitleVi = "Cài Windows 10 LTSC Doanh Nghiệp",
                TitleEn = "Install Windows 10 LTSC",
                DescVi = "Triển khai Windows 10 LTSC bản sạch siêu nhẹ, mượt mà, không bloatware, hỗ trợ cập nhật lâu dài.",
                DescEn = "Deploy clean lightweight Windows 10 LTSC Enterprise edition without bloatware.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "windows 10 ltsc doanh nghiep nhe muot cai win 10",
                Action = () => ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition.Win10Ltsc)
            },
            new()
            {
                Icon = "🚀",
                TitleVi = "Cài Windows 11 LTSC Doanh Nghiệp",
                TitleEn = "Install Windows 11 LTSC",
                DescVi = "Triển khai Windows 11 LTSC phiên bản mới nhất, tối ưu hiệu năng tối đa cho gaming và văn phòng.",
                DescEn = "Deploy the latest Windows 11 LTSC edition optimized for high performance and stability.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "windows 11 ltsc doanh nghiep nhe toi uu cai win 11",
                Action = () => ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition.Win11Ltsc)
            },
            new()
            {
                Icon = "🖥️",
                TitleVi = "Cài đặt Windows Server 2025 / 2022 / 2019",
                TitleEn = "Windows Server Deployment",
                DescVi = "Hỗ trợ tải và cài đặt tự động các phiên bản Windows Server 2025, 2022, 2019.",
                DescEn = "Automated download and deployment for Windows Server 2025, 2022, and 2019.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "windows server 2019 2022 2025 cai may chu",
                Action = () => ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition.Server2022)
            },
            new()
            {
                Icon = "🐧",
                TitleVi = "Cài Linux thay thế Windows (Ubuntu, Mint, Debian...)",
                TitleEn = "Install Linux to Replace Windows",
                DescVi = "Cài đặt hệ điều hành Linux (Ubuntu Desktop, Linux Mint, Debian, Kali) thay thế Windows hoàn toàn.",
                DescEn = "Install Linux (Ubuntu, Linux Mint, Debian, Kali Linux) as primary OS replacing Windows.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "linux ubuntu mint debian kali cai linux thay windows",
                Action = () => ShowLinuxReplaceWindowsDialog(LinuxDistro.Ubuntu2404)
            },
            new()
            {
                Icon = "🔄",
                TitleVi = "Cài Linux Song Song (Dual Boot)",
                TitleEn = "Install Dual Boot Linux",
                DescVi = "Cài đặt Linux song song với Windows, tự động cấu hình menu khởi động kép GRUB an toàn.",
                DescEn = "Install Linux alongside Windows with safe dual-boot GRUB configuration.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "dual boot song song linux windows ubuntu mint debian",
                Action = () => ShowLinuxDualBootDialog(LinuxDistro.Ubuntu2404)
            },
            new()
            {
                Icon = "🌐",
                TitleVi = "Kho Tải ISO Windows Chính Thức",
                TitleEn = "Official Windows ISO Portal",
                DescVi = "Mở cổng tải bộ cài ISO nguyên gốc từ máy chủ Microsoft.",
                DescEn = "Open official Microsoft ISO portal to download verified Windows setup media.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "iso windows portal download tai win microsoft",
                Action = () => _maintenanceService.OpenUrl("https://www.microsoft.com/software-download")
            },
            new()
            {
                Icon = "💾",
                TitleVi = "Tạo Master Image Hệ Thống",
                TitleEn = "Create Master System Image",
                DescVi = "Sao lưu toàn bộ ổ cài Windows thành file WIM chuẩn để triển khai hàng loạt máy.",
                DescEn = "Capture current Windows installation into a standard WIM image for mass deployment.",
                CategoryVi = "Cài Win & Boot",
                CategoryEn = "Windows & Boot",
                CategoryKey = "win",
                Keywords = "master image wim dism capture backup he thong",
                Action = () => ShowMasterImageDialog()
            },

            // === TỐI ƯU & SỬA LỖI ===
            new()
            {
                Icon = "🧹",
                TitleVi = "Dọn Rác Hệ Thống & Bộ Nhớ Đệm",
                TitleEn = "Clean Junk Files & System Cache",
                DescVi = "Xóa file tạm Temp, Prefetch, Log lỗi, Windows Update cache, GPU cache và lịch sử rác.",
                DescEn = "Clean Windows temporary files, Prefetch, error logs, Windows Update cache, and GPU cache.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "don rac cleaner clean temp prefetch update cache giai phong dung luong",
                Action = () => ShowSystemCleanerDialog()
            },
            new()
            {
                Icon = "🌐",
                TitleVi = "Chẩn đoán & Sửa Lỗi Mạng (Network Fix)",
                TitleEn = "Network Diagnostics & Repair",
                DescVi = "Kiểm tra kết nối Internet, Ping gateway, Flush DNS, Reset Winsock & TCP/IP khi mất mạng.",
                DescEn = "Test internet connection, gateway ping, flush DNS, reset Winsock and TCP/IP stack.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "mang network ping dns winsock internet sua loi mat mang",
                Action = () => ShowNetworkDiagnosticDialog()
            },
            new()
            {
                Icon = "🔒",
                TitleVi = "Tắt Mã Hóa Ổ Đĩa BitLocker",
                TitleEn = "Turn Off BitLocker Encryption",
                DescVi = "Tắt mã hóa BitLocker trên ổ đĩa C: để tránh bị khóa máy bất ngờ khi cập nhật BIOS hoặc cài Win.",
                DescEn = "Disable BitLocker drive encryption on C: to prevent unexpected lockout on BIOS/OS updates.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "bitlocker tat ma hoa khoa o c turn off bitlocker",
                Action = () => _systemService.TurnOffBitLocker("C:")
            },
            new()
            {
                Icon = "🚫",
                TitleVi = "Chặn Tự Động Cập Nhật Windows Update",
                TitleEn = "Block Automatic Windows Update",
                DescVi = "Vô hiệu hóa dịch vụ wuauserv và Group Policy NoAutoUpdate để ngăn Win tự cập nhật làm chậm máy.",
                DescEn = "Disable wuauserv service and configure Group Policy to block automatic Windows Updates.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "chan update block windows update wuauserv tat update",
                Action = () =>
                {
                    _systemService.ToggleWindowsUpdate(true);
                    MessageBox.Show(_isEn ? "Successfully blocked automatic Windows Update!" : "Đã thiết lập chặn Windows Update tự động thành công!", _isEn ? "Notification" : "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            },
            new()
            {
                Icon = "🛡️",
                TitleVi = "Chặn Tự Động Nâng Cấp Lên Windows 11",
                TitleEn = "Block Automatic Windows 11 Upgrade",
                DescVi = "Khóa phiên bản Windows 10 hiện tại, ngăn Microsoft tự động ép nâng cấp lên Windows 11.",
                DescEn = "Pin current Windows 10 release to prevent Microsoft from forcing Windows 11 upgrade.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "chan win 11 block upgrade 10 to 11 khoa phien ban",
                Action = () =>
                {
                    _systemService.BlockWin11Upgrade(true);
                    MessageBox.Show(_isEn ? "Blocked automatic upgrade to Windows 11!" : "Đã chặn nâng cấp tự động lên Windows 11!", _isEn ? "Notification" : "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            },
            new()
            {
                Icon = "🗜️",
                TitleVi = "Sửa Lỗi Mở File Nén (.zip, .7z, .rar)",
                TitleEn = "Fix Archive Associations",
                DescVi = "Khôi phục lại liên kết mặc định trong Windows Registry cho các định dạng file nén phổ biến.",
                DescEn = "Restore default Windows registry associations for archive files (.zip, .7z, .rar).",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "file nen zip rar 7z association sua loi mo file",
                Action = () =>
                {
                    bool ok = _systemService.FixCompressedFileAssociations(Log);
                    if (ok) MessageBox.Show(_isEn ? "Restored archive associations successfully!" : "Đã khôi phục liên kết mặc định cho file nén thành công!", _isEn ? "Success" : "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            },
            new()
            {
                Icon = "🖨️",
                TitleVi = "Sửa Lỗi Máy In Kẹt Lệnh (Print Spooler)",
                TitleEn = "Reset Print Spooler Service",
                DescVi = "Xóa toàn bộ các lệnh in bị kẹt trong hàng đợi và khởi động lại dịch vụ Print Spooler.",
                DescEn = "Clear stuck print jobs from queue and restart the Windows Print Spooler service.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "print spooler may in ket lenh in printer fix in an",
                Action = () =>
                {
                    bool ok = _systemService.ResetPrintSpooler(Log);
                    if (ok) MessageBox.Show(_isEn ? "Cleared stuck print jobs successfully!" : "Đã xóa lệnh in kẹt và khởi động lại Print Spooler thành công!", _isEn ? "Printer Fix" : "Sửa Lỗi Máy In", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            },
            new()
            {
                Icon = "🔄",
                TitleVi = "Khôi phục Cài Đặt Gốc (Reset this PC)",
                TitleEn = "Reset This PC to Factory State",
                DescVi = "Mở trình khôi phục cài đặt gốc mặc định của Windows để đưa máy về trạng thái ban đầu.",
                DescEn = "Open built-in Windows Reset This PC wizard to restore computer to clean state.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "reset this pc khoi phuc cai dat goc factory restore",
                Action = () => SafeLaunch("systemreset.exe")
            },
            new()
            {
                Icon = "🖱️",
                TitleVi = "Đổi Kiểu Menu Chuột Phải Win 10/11",
                TitleEn = "Toggle Win 10/11 Context Menu",
                DescVi = "Bật menu chuột phải đầy đủ kiểu cổ điển của Windows 10 trên Windows 11 mà không cần bấm 'Show more'.",
                DescEn = "Enable classic full right-click context menu on Windows 11 without clicking 'Show more'.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "context menu chuot phai win 11 classic show more options",
                Action = () => ToggleContextMenuStyle()
            },
            new()
            {
                Icon = "🚀",
                TitleVi = "Quản lý Ứng dụng Khởi động (Startup)",
                TitleEn = "Startup Applications Manager",
                DescVi = "Mở trình quản lý các ứng dụng tự khởi động cùng Windows để tối ưu tốc độ mở máy.",
                DescEn = "Open Windows Startup tab to enable or disable apps launching on boot.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "startup ung dung khoi dong taskmgr boot speed khoi dong nhanh",
                Action = () => SafeLaunch("taskmgr.exe", args: "/0 /startup")
            },
            new()
            {
                Icon = "🛠️",
                TitleVi = "Sửa Giới Hạn Phân Vùng Ổ Đĩa",
                TitleEn = "Fix Partition Limits",
                DescVi = "Sửa lỗi không tạo được thêm phân vùng mới hoặc không mở rộng được ổ đĩa trong Disk Management.",
                DescEn = "Resolve disk partition boundaries and extend/shrink limitations in Disk Management.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "partition sua gioi han phan vung o dia extend shrink",
                Action = () => _diskService.FixPartitionLimits()
            },
            new()
            {
                Icon = "🗑️",
                TitleVi = "Gỡ Bloatware Windows (Ứng dụng rác)",
                TitleEn = "Remove Windows Bloatware",
                DescVi = "Gỡ các ứng dụng cài sẵn không cần thiết của Windows (Xbox, Cortana, Maps, News...).",
                DescEn = "Uninstall pre-installed Windows bloatware apps (Xbox, Cortana, News, Weather).",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "bloatware go app rac xbox cortana xoa bot app win",
                Action = () => ShowBloatwareRemoverDialog()
            },
            new()
            {
                Icon = "🎧",
                TitleVi = "Hỗ Trợ Từ Xa (UltraViewer / AnyDesk)",
                TitleEn = "Remote Support Assistant",
                DescVi = "Mở nhanh phần mềm hỗ trợ kỹ thuật từ xa qua UltraViewer hoặc AnyDesk.",
                DescEn = "Quickly launch remote desktop support tool via UltraViewer or AnyDesk.",
                CategoryVi = "Tối ưu & Sửa lỗi",
                CategoryEn = "Tweaks & Fixes",
                CategoryKey = "trouble",
                Keywords = "remote support ho tro tu xa ultraviewer anydesk teamviewer",
                Action = () => ShowRemoteSupportDialog()
            },

            // === THIẾT BỊ & GOOGLE SHEET ===
            new()
            {
                Icon = "🌐",
                TitleVi = "Liên Kết & Quản Lý Google Sheet Thiết Bị",
                TitleEn = "Google Sheet Devices Management",
                DescVi = "Cấu hình liên kết Google Sheet để đồng bộ thông tin máy tính, cấu hình phần cứng và người sử dụng.",
                DescEn = "Configure Google Sheet connection to synchronize PC hardware specs and user info.",
                CategoryVi = "Thiết bị & Sheet",
                CategoryEn = "Devices & Sync",
                CategoryKey = "device",
                Keywords = "google sheet lien ket dong bo thiet bi quan ly serial script",
                Action = () => SwitchTab(3),
                TargetTab = 3
            },
            new()
            {
                Icon = "📝",
                TitleVi = "Khởi Tạo Tên Máy & Bản Quyền Thiết Bị",
                TitleEn = "Device Initialization & Licensing",
                DescVi = "Đặt tên máy tính (PC Name), Công ty/Phòng ban và kiểm tra bản quyền Windows / Office tự động.",
                DescEn = "Configure Computer Name, Department/Company and inspect Windows/Office license status.",
                CategoryVi = "Thiết bị & Sheet",
                CategoryEn = "Devices & Sync",
                CategoryKey = "device",
                Keywords = "khoi tao thiet bi ten may cong ty pc name license ban quyen",
                Action = () => SwitchTab(3),
                TargetTab = 3
            },
            new()
            {
                Icon = "🔍",
                TitleVi = "Kiểm Tra Trùng Lặp Thiết Bị Trên Sheet",
                TitleEn = "Check Device Duplication on Sheet",
                DescVi = "Kiểm tra xem máy tính hiện tại (Serial number) đã tồn tại trên file Google Sheet hay chưa.",
                DescEn = "Check if the current machine serial number already exists in the connected Google Sheet.",
                CategoryVi = "Thiết bị & Sheet",
                CategoryEn = "Devices & Sync",
                CategoryKey = "device",
                Keywords = "kiem tra trung lap duplicate serial sheet thiet bi",
                Action = () => SwitchTab(3),
                TargetTab = 3
            },

            // === CÀI ĐẶT ỨNG DỤNG ===
            new()
            {
                Icon = "⚙️",
                TitleVi = "Cài Đặt Ứng Dụng (Giao diện & Ngôn ngữ)",
                TitleEn = "Application Settings",
                DescVi = "Tùy chỉnh giao diện Sáng / Tối (Light / Dark) và ngôn ngữ hiển thị (Tiếng Việt / English).",
                DescEn = "Customize theme (Dark / Light) and display language (Vietnamese / English).",
                CategoryVi = "Cài đặt",
                CategoryEn = "Settings",
                CategoryKey = "all",
                Keywords = "cai dat settings theme sang toi giao dien ngon ngu tieng viet english",
                Action = () => OpenSettingsForm()
            }
        };
    }

    #endregion

    #region Helper UI Builders & Safe Launch

    private void SafeLaunch(string command, string? args = null, string? fallback = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args ?? "",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(fallback))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(fallback) { UseShellExecute = true });
                    return;
                }
                catch { }
            }
            Log($"[Thông báo hệ thống] Không thể mở '{command}': {ex.Message}");
        }
    }

    private Panel CreateCardSection(string title, ref int currentY, ToolBtn[] buttons)
    {
        var card = new Panel
        {
            Location = new Point(14, currentY),
            Size = new Size(1160, 96),
            BackColor = CardBg,
            Padding = new Padding(16, 10, 16, 10)
        };

        card.Paint += (s, e) =>
        {
            var border = _isCurrentDark ? Color.FromArgb(32, 42, 60) : Color.FromArgb(226, 232, 240);
            using var pen = new Pen(border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = TextHeading,
            Location = new Point(14, 10),
            AutoSize = true
        };
        card.Controls.Add(lblTitle);

        var flow = new FlowLayoutPanel
        {
            Location = new Point(12, 40),
            Size = new Size(card.Width - 24, 50),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            BackColor = Color.Transparent
        };

        int btnWidth = 176;
        int btnHeight = 42;

        foreach (var item in buttons)
        {
            var btn = new Button
            {
                Text = item.Label,
                Size = new Size(btnWidth, btnHeight),
                Margin = new Padding(0, 0, 8, 6),
                Font = new Font("Segoe UI", 8.75f),
                BackColor = BtnBg,
                ForeColor = TextHeading,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = BtnBorder;
            btn.FlatAppearance.MouseOverBackColor = BtnHover;
            btn.Click += (_, _) =>
            {
                try
                {
                    item.Action();
                }
                catch (Exception ex)
                {
                    Log($"[Lỗi thực thi {item.Label}]: {ex.Message}");
                }
            };
            flow.Controls.Add(btn);
        }

        card.Controls.Add(flow);

        flow.SizeChanged += (s, e) =>
        {
            card.Height = Math.Max(96, flow.Bottom + 12);
        };

        currentY += card.Height + 10;
        return card;
    }

    private void RelayoutCards(Panel pnl)
    {
        int y = 6;
        int targetWidth = Math.Max(750, pnl.ClientSize.Width - 28);
        foreach (Control c in pnl.Controls)
        {
            c.Location = new Point(14, y);
            c.Width = targetWidth;
            var flow = c.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
            if (flow != null)
            {
                flow.Width = targetWidth - 24;
                flow.PerformLayout();
                var pref = flow.GetPreferredSize(new Size(targetWidth - 24, 0));
                flow.Height = pref.Height;
                c.Height = Math.Max(96, flow.Top + flow.Height + 14);
            }
            else
            {
                c.Height = 96;
            }
            c.Invalidate();
            y += c.Height + 12;
        }
    }

    private Button CreateMiniButton(string text, int x, int y, int width, Action action)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 36),
            Font = new Font("Segoe UI", 9f),
            BackColor = BtnBg,
            ForeColor = TextHeading,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = BtnBorder;
        btn.FlatAppearance.MouseOverBackColor = BtnHover;
        btn.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log($"[Lỗi thực thi {text}]: {ex.Message}");
            }
        };
        return btn;
    }

    #endregion

    #region Data Loading & Actions

    private void LoadSystemData()
    {
        Log(_isEn ? "Loading system telemetry..." : "Đang nạp trạng thái hệ thống...");
        _sysInfo = _systemService.GetSystemInfo();

        UpdateHeaderLanguage();
        PopulateDevicePanelData();

        Log(_isEn
            ? $"System ready: {_sysInfo.MachineName} ({_sysInfo.OsVersion}) | Boot Mode: {_sysInfo.BootMode}"
            : $"Hệ thống sẵn sàng: {_sysInfo.MachineName} ({_sysInfo.OsVersion}) | Chuẩn Boot: {_sysInfo.BootMode}");
    }

    private void OpenWindowsConfigForm()
    {
        try
        {
            Log("Mở Trình cấu hình Windows (Bảo mật, Thiết lập nhanh, Quản lý User)...");
            using var form = new WindowsConfigForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Trình cấu hình Windows]: {ex.Message}");
        }
    }

    private void ShowDetailedSpecs()
    {
        try
        {
            Log("Mở cửa sổ Thông tin hệ thống chi tiết (CPU, RAM, GPU, Ổ cứng, Bản quyền, Pin)...");
            using var form = new SystemInfoForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Thông tin hệ thống]: {ex.Message}");
        }
    }

    private void ShowMasterImageDialog()
    {
        try
        {
            Log("Mở Trình tạo Master Image từ máy mẫu (Sysprep / WIM / ISO)...");
            using var form = new MasterImageForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Master Image]: {ex.Message}");
        }
    }

    private void ShowIrstDriverDialog()
    {
        try
        {
            Log("Mở Trình quản lý & Cài đặt Driver Intel RST (VMD)...");
            using var form = new IrstDriverForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Driver IRST]: {ex.Message}");
        }
    }

    private void ShowWifiBackupDialog()
    {
        try
        {
            Log("Mở Trình quản lý, Sao lưu & Khôi phục WiFi...");
            using var form = new WifiBackupForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Sao lưu WiFi]: {ex.Message}");
        }
    }

    private void ShowDataBackupDialog()
    {
        try
        {
            Log("Mở Trình sao lưu & khôi phục dữ liệu cá nhân...");
            using var form = new DataBackupForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Sao lưu Dữ liệu]: {ex.Message}");
        }
    }

    private void ShowDriverBackupDialog()
    {
        try
        {
            Log("Mở Trình sao lưu & Nạp lại Driver / WiFi...");
            using var form = new DriverBackupForm(_sysInfo.Manufacturer, SettingsService.Instance.Settings.DriverFolder);
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Sao lưu Driver]: {ex.Message}");
        }
    }

    private void ShowBrowserBackupDialog()
    {
        try
        {
            Log("Mở Trình sao lưu & khôi phục dữ liệu Trình duyệt web (Bookmarks, History, Profile)...");
            using var form = new BrowserBackupForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Sao lưu trình duyệt]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Sao lưu trình duyệt:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowCredentialAndCertificateDialog()
    {
        try
        {
            Log("Mở Trình quản lý Mật khẩu Windows & Chữ ký số...");
            using var form = new CredentialAndCertificateForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Mật khẩu & Chữ ký số]: {ex.Message}");
        }
    }

    private void ShowSoftwareInstallerDialog()
    {
        try
        {
            Log("Mở Trình cài đặt phần mềm tự động...");
            using var form = new SoftwareInstallerForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Cài đặt phần mềm]: {ex.Message}");
        }
    }

    private void ShowBatchUninstallerDialog()
    {
        try
        {
            Log("Mở Trình gỡ nhiều phần mềm cùng lúc...");
            using var form = new BatchUninstallerForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Gỡ nhiều phần mềm]: {ex.Message}");
        }
    }

    private void ShowBloatwareRemoverDialog()
    {
        try
        {
            Log("Mở Giao diện Gỡ sạch Bloatware...");
            using var form = new BloatwareRemoverForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Gỡ Bloatware]: {ex.Message}");
        }
    }

    private void ShowSystemCleanerDialog()
    {
        try
        {
            Log("Mở Giao diện Dọn rác hệ thống (Temp, Cache, Lịch sử, Reset app)...");
            using var form = new SystemCleanerForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Dọn rác hệ thống]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Dọn rác hệ thống:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowNetworkDiagnosticDialog()
    {
        try
        {
            Log("Mở Giao diện Chẩn đoán mạng (Network Diagnostics)...");
            using var form = new NetworkDiagnosticForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Chẩn đoán mạng]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Chẩn đoán mạng:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowLinuxDualBootDialog(LinuxDistro distro = LinuxDistro.Ubuntu2404)
    {
        try
        {
            Log($"Mở Giao diện Cài đặt Linux song song ({distro})...");
            using var form = new LinuxDualBootForm(distro);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Cài Linux song song]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Cài đặt Linux song song:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowLinuxReplaceWindowsDialog(LinuxDistro distro = LinuxDistro.Ubuntu2404)
    {
        try
        {
            Log($"Mở Giao diện Cài đặt Linux thay thế Windows ({distro})...");
            using var form = new LinuxReplaceWindowsForm(distro);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Cài Linux thay thế Windows]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Cài đặt Linux thay thế Windows:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowWindowsServerDeploymentDialog(WindowsServerLtscEdition edition = WindowsServerLtscEdition.Server2019)
    {
        try
        {
            Log($"Mở Giao diện Cài đặt tự động {edition}...");
            using var form = new WindowsServerDeploymentForm(edition);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Cài Windows Server / LTSC]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Cài đặt Windows Server / LTSC:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowKeyboardTestDialog()
    {
        try
        {
            Log("Mở Giao diện Kiểm tra Bàn Phím (Keyboard Test)...");
            using var form = new KeyboardTestForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Keyboard Test]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Keyboard Test:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowCheckTestDialog()
    {
        try
        {
            Log("Mở Giao diện Kiểm tra & Chẩn đoán phần cứng (Check - Test)...");
            using var form = new CheckTestForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Check - Test]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Check - Test:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowAutoTasksDialog()
    {
        try
        {
            Log("Mở Giao diện Tác vụ tự động hóa bảo trì & tối ưu hệ thống...");
            using var form = new AutoTasksForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Tác vụ tự động]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Tác vụ tự động:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowRemoteSupportDialog()
    {
        try
        {
            Log("Mở Trung tâm Hỗ trợ từ xa (UltraViewer, AnyDesk, TeamViewer, RustDesk, Quick Assist)...");
            using var form = new RemoteSupportForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Hỗ trợ từ xa]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Hỗ trợ từ xa:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowOfficeConfigDialog()
    {
        try
        {
            Log("Mở Giao diện Thiết lập & Sửa lỗi Microsoft Office (OSPP Licensing & Repair)...");
            using var form = new OfficeConfigForm();
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Thiết lập Office]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Thiết lập Office:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleContextMenuStyle()
    {
        try
        {
            if (!_systemService.IsWindows11OrGreater())
            {
                string win10Msg = $"💻 Hệ điều hành phát hiện: Windows 10 (Build {Environment.OSVersion.Version.Build}).\n\n" +
                                  "Hệ điều hành Windows 10 đã mặc định sử dụng menu chuột phải cổ điển đầy đủ tính năng ngay từ đầu.\n" +
                                  "Tính năng chuyển đổi menu chuột chỉ áp dụng cho Windows 11.";
                Log($"[Menu chuột phải] Hệ thống là Windows 10 (Build {Environment.OSVersion.Version.Build}) - Đã mặc định dùng menu cổ điển.");
                MessageBox.Show(this, win10Msg, "Nhận Diện Hệ Điều Hành: Windows 10", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool isWin10Style = _systemService.IsWin10ContextMenuActive();
            string user = Environment.UserName;

            if (isWin10Style)
            {
                string msg = $"💻 Hệ điều hành: Windows 11 (user: {user}).\n" +
                             "Hiện tại: Chuột phải đang là menu kiểu WINDOWS 10 CỔ ĐIỂN.\n\n" +
                             "Bạn có muốn trả về menu hiện đại mặc định của Windows 11 (menu gọn, có dòng \"Show more options\") không?\n\n" +
                             "Thanh taskbar và Windows Explorer sẽ tự khởi động lại trong giây lát.";

                var res = MessageBox.Show(this, msg, "Chuyển Đổi Menu Chuột Phải", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    Log("Đang chuyển đổi menu chuột phải về mặc định của Windows 11...");
                    _systemService.ToggleWin10ContextMenu(false, true);
                    Log("✅ Đã chuyển đổi chuột phải về menu kiểu Windows 11 thành công!");
                }
            }
            else
            {
                string msg = $"💻 Hệ điều hành: Windows 11 (user: {user}).\n" +
                             "Hiện tại: Chuột phải đang là menu mặc định của WINDOWS 11.\n\n" +
                             "Bạn có muốn đổi sang menu kiểu Windows 10 (cổ điển, hiển thị đầy đủ tính năng ngay từ đầu) không?\n\n" +
                             "Thanh taskbar và Windows Explorer sẽ tự khởi động lại trong giây lát.";

                var res = MessageBox.Show(this, msg, "Chuyển Đổi Menu Chuột Phải", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    Log("Đang chuyển đổi menu chuột phải sang kiểu Windows 10 cổ điển...");
                    _systemService.ToggleWin10ContextMenu(true, true);
                    Log("✅ Đã chuyển đổi chuột phải sang menu kiểu Windows 10 thành công!");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Lỗi chuyển menu chuột phải]: {ex.Message}");
            MessageBox.Show($"Xảy ra lỗi khi thao tác: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void ShowWinPeBootDialog()
    {
        try
        {
            Log("Mở công cụ Tạo boot WinPE (bộ AnhDV / WIM tùy chỉnh)...");
            using var form = new WinPeBootForm();
            form.StartPosition = FormStartPosition.CenterScreen;
            ThemeHelper.ApplyTheme(form);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Tạo boot WinPE]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Tạo boot WinPE:\n\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.B))
        {
            ToggleSidebar();
            return true;
        }
        if (keyData == Keys.F1 && ActiveControl is not TextBox)
        {
            SwitchTab(0);
            return true;
        }
        if (keyData == Keys.F2 && ActiveControl is not TextBox)
        {
            SwitchTab(1);
            return true;
        }
        if (keyData == Keys.F3 && ActiveControl is not TextBox)
        {
            SwitchTab(2);
            return true;
        }
        if (keyData == Keys.F4 && ActiveControl is not TextBox)
        {
            SwitchTab(3);
            return true;
        }
        if (keyData == (Keys.Control | Keys.F))
        {
            SwitchTab(4);
            _txtSearch?.Focus();
            _txtSearch?.SelectAll();
            return true;
        }
        if (keyData == Keys.F5 && ActiveControl is not TextBox)
        {
            SwitchTab(4);
            _txtSearch?.Focus();
            return true;
        }

        // Phím 0: Mở Driver & WiFi - Sao lưu / Nạp lại
        if ((keyData == Keys.D0 || keyData == Keys.NumPad0) && ActiveControl is not TextBox)
        {
            ShowDriverBackupDialog();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ShowWifiAndVpn()
    {
        var output = _systemService.ShowWifiProfiles();
        Log("=== DANH SÁCH HỒ SƠ WIFI ĐÃ LƯU ===");
        Log(output);

        using var form = new Form
        {
            Text = "📶 Danh Sách Hồ Sơ WiFi Đã Lưu",
            Size = new Size(640, 500),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = BgApp,
            Font = new Font("Segoe UI", 9f),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var pnlCard = new Panel
        {
            Location = new Point(16, 16),
            Size = new Size(592, 380),
            BackColor = CardBg,
            Padding = new Padding(12)
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        var rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Text = output,
            Font = new Font("Consolas", 9.5f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = CardBg,
            ForeColor = TextHeading
        };
        pnlCard.Controls.Add(rtb);
        form.Controls.Add(pnlCard);

        var btnCopy = new Button
        {
            Text = "📋 Sao chép danh sách",
            Location = new Point(16, 410),
            Size = new Size(200, 38),
            BackColor = PrimaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopy.FlatAppearance.BorderColor = Color.FromArgb(29, 78, 216);
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(output);
            MessageBox.Show("Đã sao chép danh sách WiFi vào Clipboard!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var btnClose = new Button
        {
            Text = "Đóng",
            Location = new Point(226, 410),
            Size = new Size(110, 38),
            BackColor = BtnBg,
            ForeColor = TextHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderColor = BtnBorder;
        btnClose.FlatAppearance.MouseOverBackColor = BtnHover;
        btnClose.Click += (_, _) => form.Close();

        form.Controls.Add(btnCopy);
        form.Controls.Add(btnClose);
        form.ShowDialog(this);
    }

    private void ShowWindowsDeploymentDialog(WindowsDeploymentForm.DeploymentMode mode)
    {
        using var dlg = new WindowsDeploymentForm(mode);
        dlg.ShowDialog(this);
    }

    private void ShowKeyComparisonDialog()
    {
        using var dlg = new KeyComparisonForm(_sysInfo, _sheetService, _deviceConfig);
        dlg.ShowDialog(this);
    }

    private async Task RealUploadSheetAsync(bool bothSheets)
    {
        string scriptUrl = _txtAppsScript.Text.Trim();
        if (string.IsNullOrWhiteSpace(scriptUrl))
        {
            MessageBox.Show("Vui lòng nhập Link triển khai Google Apps Script trước khi tải lên!", "Thiếu Cấu Hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string machineName = string.IsNullOrWhiteSpace(_cboMachineName.Text) ? _sysInfo.MachineName : _cboMachineName.Text.Trim();
        string company = string.IsNullOrWhiteSpace(_cboCompany.Text) ? "Công ty TNHH Mẫu" : _cboCompany.Text.Trim();

        var payload = new DeviceSyncPayload
        {
            Action = "upload",
            MachineName = machineName,
            Company = company,
            OsVersion = $"{_sysInfo.OsVersion} ({_sysInfo.OsArchitecture})",
            Cpu = _sysInfo.ProcessorName,
            Ram = _sysInfo.TotalMemoryMb,
            ProductKey = _sysInfo.WindowsProductKey,
            SerialNumber = _sysInfo.SerialNumber,
            Manufacturer = _sysInfo.Manufacturer,
            Model = _sysInfo.Model,
            MacAddress = _systemService.GetActiveMacAddress(),
            Status = "Active",
            BothSheets = bothSheets
        };

        Log($"[Google Sheet] Bắt đầu đồng bộ thông tin máy '{machineName}' ({company}) lên Sheet...");
        var (ok, msg) = await _sheetService.UploadDeviceInfoAsync(scriptUrl, payload, p =>
        {
            if (InvokeRequired) Invoke(() => _progressBar.Value = p);
            else _progressBar.Value = p;
        });

        _progressBar.Value = 0;
        Log(ok ? $"[Google Sheet] ✅ {msg}" : $"[Google Sheet] ❌ {msg}");
        MessageBox.Show(msg, ok ? "Đồng Bộ Thành Công" : "Lỗi Đồng Bộ", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private async Task RealRestorePcAsync()
    {
        string scriptUrl = _txtAppsScript.Text.Trim();
        if (string.IsNullOrWhiteSpace(scriptUrl))
        {
            MessageBox.Show("Vui lòng nhập Link triển khai Google Apps Script trước!", "Thiếu Cấu Hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Log("[Google Sheet] Đang truy vấn danh sách thiết bị từ Google Sheet...");
        _progressBar.Value = 30;
        var (ok, list, msg) = await _sheetService.FetchDevicesAsync(scriptUrl);
        _progressBar.Value = 0;

        if (!ok)
        {
            Log($"[Google Sheet] ❌ {msg}");
            MessageBox.Show(msg, "Lỗi Truy Vấn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Log($"[Google Sheet] ✅ Đã nhận diện {list.Count} máy từ Sheet.");

        string curSerial = _sysInfo.SerialNumber;
        string curMachine = _sysInfo.MachineName;
        var matched = list.FirstOrDefault(d =>
            (!string.IsNullOrEmpty(curSerial) && !string.IsNullOrEmpty(d.SerialNumber) && d.SerialNumber.Equals(curSerial, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(curMachine) && d.MachineName.Equals(curMachine, StringComparison.OrdinalIgnoreCase)));

        if (matched != null)
        {
            string detail = $"Tìm thấy bản ghi khớp:\n- Tên máy: {matched.MachineName}\n- Công ty: {matched.Company}\n- Serial: {matched.SerialNumber}\n- Key: {matched.ProductKey}\n\nBạn có muốn áp dụng Tên máy và Công ty này vào cấu hình không?";
            var res = MessageBox.Show(detail, "Khôi Phục Cấu Hình PC (Restore PC)", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                if (!string.IsNullOrEmpty(matched.MachineName)) _cboMachineName.Text = matched.MachineName;
                if (!string.IsNullOrEmpty(matched.Company)) _cboCompany.Text = matched.Company;
                Log($"[Restore PC] Đã áp dụng cấu hình: Tên '{matched.MachineName}', Công ty '{matched.Company}'.");
            }
        }
        else
        {
            MessageBox.Show($"Không tìm thấy bản ghi nào trên Google Sheet khớp với Serial '{curSerial}' hoặc Tên '{curMachine}'.\nTổng số máy trong Sheet: {list.Count}.", "Không Tìm Thấy", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async Task RealCheckDuplicateAsync()
    {
        string scriptUrl = _txtAppsScript.Text.Trim();
        if (string.IsNullOrWhiteSpace(scriptUrl))
        {
            MessageBox.Show("Vui lòng nhập Link triển khai Google Apps Script trước!", "Thiếu Cấu Hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Log("[Google Sheet] Đang kiểm tra trùng lặp thiết bị với dữ liệu Google Sheet...");
        _progressBar.Value = 30;
        var (ok, list, msg) = await _sheetService.FetchDevicesAsync(scriptUrl);
        _progressBar.Value = 0;

        if (!ok)
        {
            Log($"[Google Sheet] ❌ {msg}");
            MessageBox.Show(msg, "Lỗi Kiểm Tra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string curSerial = _sysInfo.SerialNumber;
        string curMachine = string.IsNullOrWhiteSpace(_cboMachineName.Text) ? _sysInfo.MachineName : _cboMachineName.Text.Trim();

        var duplicates = list.Where(d =>
            (!string.IsNullOrEmpty(curSerial) && !string.IsNullOrEmpty(d.SerialNumber) && d.SerialNumber.Equals(curSerial, StringComparison.OrdinalIgnoreCase)) ||
            d.MachineName.Equals(curMachine, StringComparison.OrdinalIgnoreCase)).ToList();

        if (duplicates.Count > 0)
        {
            string dupInfo = $"⚠️ Phát hiện {duplicates.Count} thiết bị trùng lặp trên Google Sheet:\n\n" +
                string.Join("\n", duplicates.Select(d => $"• Máy: {d.MachineName} | Cty: {d.Company} | Serial: {d.SerialNumber} | Key: {d.ProductKey}"));
            Log($"[Check trùng lặp] {dupInfo}");
            MessageBox.Show(dupInfo, "Cảnh Báo Trùng Lặp Thiết Bị", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            string okMsg = $"✅ Tuyệt vời! Thiết bị '{curMachine}' (Serial: {curSerial}) hoàn toàn MỚI, KHÔNG bị trùng lặp với {list.Count} thiết bị trong Google Sheet.";
            Log($"[Check trùng lặp] {okMsg}");
            MessageBox.Show(okMsg, "Kiểm Tra Trùng Lặp", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ShowAppsScriptModal()
    {
        var template = _sheetService.GenerateAppsScriptTemplate();
        using var form = new Form
        {
            Text = "📜 Mã Nguồn Google Apps Script (Sao chép vào Google Sheet)",
            Size = new Size(720, 540),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ThemeHelper.BgForm,
            Font = new Font("Segoe UI", 9f),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var pnlCard = new Panel
        {
            Location = new Point(16, 16),
            Size = new Size(672, 420),
            BackColor = ThemeHelper.CardBg,
            Padding = new Padding(12)
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemeHelper.BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        var rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Text = template,
            Font = new Font("Consolas", 9.5f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = ThemeHelper.LogBg,
            ForeColor = ThemeHelper.LogText
        };
        pnlCard.Controls.Add(rtb);
        form.Controls.Add(pnlCard);

        var btnCopy = new Button
        {
            Text = "📋 Sao chép mã vào Clipboard",
            Location = new Point(16, 448),
            Size = new Size(240, 38),
            BackColor = ThemeHelper.PrimaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopy.FlatAppearance.BorderColor = ThemeHelper.PrimaryHover;
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(template);
            MessageBox.Show("Đã sao chép mã nguồn Apps Script vào Clipboard!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var btnClose = new Button
        {
            Text = "Đóng",
            Location = new Point(266, 448),
            Size = new Size(110, 38),
            BackColor = ThemeHelper.BtnBg,
            ForeColor = ThemeHelper.TextHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderColor = ThemeHelper.BtnBorder;
        btnClose.FlatAppearance.MouseOverBackColor = ThemeHelper.BtnHover;
        btnClose.Click += (_, _) => form.Close();

        form.Controls.Add(btnCopy);
        form.Controls.Add(btnClose);
        ThemeHelper.ApplyTheme(form);
        form.ShowDialog(this);
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _rtbLog.AppendText($"[{timestamp}] {message}\n");
        _rtbLog.ScrollToCaret();
    }

    #endregion
}
