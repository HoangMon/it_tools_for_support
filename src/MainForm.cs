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

    // Sidebar navigation buttons
    private Panel _pnlSidebar = null!;
    private Panel _pnlBottomSidebar = null!;
    private Label _lblAppVersion = null!;
    private Button _btnTabDashboard = null!;
    private Button _btnTabCore = null!;
    private Button _btnTabAdv = null!;
    private Button _btnTabDevice = null!;
    private Button _btnSettings = null!;

    private bool _isCurrentDark = false;
    private int _currentTabIndex = 0;

    // Content Panels
    private Panel _pnlContent = null!;
    private DashboardPanel _pnlDashboard = null!;
    private Panel _pnlCore = null!;
    private Panel _pnlAdv = null!;
    private Panel _pnlDevice = null!;

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
        InitializeUi();
        LoadSystemData();
        InitSettings();
    }

    private void InitializeUi()
    {
        Text = "Ernastie Support Tool";
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

        _pnlContent.Controls.Add(_pnlDashboard);
        _pnlContent.Controls.Add(_pnlCore);
        _pnlContent.Controls.Add(_pnlAdv);
        _pnlContent.Controls.Add(_pnlDevice);

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

        _lblStatusTitle = new Label
        {
            Text = "💻 TRẠNG THÁI MÁY:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = TextHeading,
            Margin = new Padding(0, 6, 12, 4),
            AutoSize = true
        };

        _lblBitLocker = CreateStatusBadge("🔒 BitLocker: Đang kiểm tra...");
        SetBadge(_lblBitLocker, "🔒 BitLocker: Đang kiểm tra...", BadgeType.Danger);

        _lblPfx = CreateStatusBadge("🔑 Không phát hiện file .pfx");
        SetBadge(_lblPfx, "🔑 Không phát hiện file .pfx", BadgeType.Success);

        _lblBoot = CreateStatusBadge("💽 Chuẩn boot: GPT - UEFI");
        SetBadge(_lblBoot, "💽 Chuẩn boot: GPT - UEFI", BadgeType.Success);

        _lblIrst = CreateStatusBadge("⚠️ CẦN driver IRST (VMD)");
        SetBadge(_lblIrst, "⚠️ CẦN driver IRST (VMD)", BadgeType.Warning);

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
            Text = "👤 User: Đang nạp...",
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
            Padding = new Padding(12, 16, 12, 16)
        };
        side.Paint += (s, e) =>
        {
            var border = _isCurrentDark ? Color.FromArgb(32, 42, 60) : Color.FromArgb(226, 232, 240);
            using var pen = new Pen(border, 1);
            e.Graphics.DrawLine(pen, side.Width - 1, 0, side.Width - 1, side.Height);
        };

        var pnlNav = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 214,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        pnlNav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        pnlNav.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

        _btnTabDashboard = CreateNavButton("📊  Dashboard", 0);
        _btnTabCore = CreateNavButton("💼  Chức năng", 1);
        _btnTabAdv = CreateNavButton("⚙️  Nâng cao", 2);
        _btnTabDevice = CreateNavButton("📱  Thiết bị", 3);

        pnlNav.Controls.Add(_btnTabDashboard, 0, 0);
        pnlNav.Controls.Add(_btnTabCore, 0, 1);
        pnlNav.Controls.Add(_btnTabAdv, 0, 2);
        pnlNav.Controls.Add(_btnTabDevice, 0, 3);

        // Bottom patch container with version info and gear settings button
        _pnlBottomSidebar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 110,
            BackColor = SidebarBg
        };

        _lblAppVersion = new Label
        {
            Text = "ERNASTIE\nv1.0.1",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 44
        };

        _btnSettings = new Button
        {
            Text = "⚙️  Cài đặt",
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

        side.Controls.Add(_pnlBottomSidebar);
        side.Controls.Add(pnlNav);

        return side;
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

        Color sideNavBg = _isCurrentDark ? Color.FromArgb(30, 41, 59) : Color.White;
        Color sideNavText = _isCurrentDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);

        var buttons = new[] { _btnTabDashboard, _btnTabCore, _btnTabAdv, _btnTabDevice };
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
        bool isEn = s.Language.Equals("en", StringComparison.OrdinalIgnoreCase);
        _btnTabDashboard.Text = "📊  Dashboard";
        _btnTabCore.Text = isEn ? "💼  Features" : "💼  Chức năng";
        _btnTabAdv.Text = isEn ? "⚙️  Advanced" : "⚙️  Nâng cao";
        _btnTabDevice.Text = isEn ? "📱  Devices" : "📱  Thiết bị";
        _btnSettings.Text = isEn ? "⚙️  Settings" : "⚙️  Cài đặt";

        _pnlDashboard.ApplyLanguage(s.Language);

        ApplyGlobalTheme(s.Theme);
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
            RefreshBadgesTheme();
            _pnlTop.Invalidate();
        }

        if (_pnlSidebar != null)
        {
            _pnlSidebar.BackColor = sideBg;
            _pnlBottomSidebar.BackColor = sideBg;
            _lblAppVersion.ForeColor = textMuted;

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
            SetBadge(_lblBitLocker,
                _sysInfo.HasBitLockerLocked ? $"🔒 BitLocker: {_sysInfo.BitLockerStatus}" : "🔒 BitLocker: Chưa mã hóa (Sạch)",
                _sysInfo.HasBitLockerLocked ? BadgeType.Danger : BadgeType.Success);
        }
        if (_lblPfx != null && _sysInfo != null)
        {
            SetBadge(_lblPfx, _sysInfo.PfxStatusText, _sysInfo.HasPfxFiles ? BadgeType.Warning : BadgeType.Success);
        }
        if (_lblBoot != null && _sysInfo != null)
        {
            SetBadge(_lblBoot, $"💽 Chuẩn boot: {_sysInfo.BootMode}", BadgeType.Success);
        }
        if (_lblIrst != null && _sysInfo != null)
        {
            SetBadge(_lblIrst,
                _sysInfo.NeedsIrst ? "⚠️ CẦN driver IRST (VMD)" : "✅ IRST/VMD: Đủ driver",
                _sysInfo.NeedsIrst ? BadgeType.Warning : BadgeType.Success);
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
        var secSys = CreateCardSection("🖥️  Hệ thống", ref topY, new ToolBtn[]
        {
            new("⚙️ Thiết lập Windows", () => OpenWindowsConfigForm()),
            new("📋 Check thông tin máy", () => ShowDetailedSpecs()),
            new("🔓 Tắt Bitlocker", () => _systemService.TurnOffBitLocker("C:")),
            new("📦 Master Image", () => ShowMasterImageDialog()),
            new("💾 Driver IRST / VMD", () => ShowIrstDriverDialog())
        });
        pnl.Controls.Add(secSys);

        // Section 2: Sao lưu - Khôi phục
        var secBackup = CreateCardSection("💾  Sao lưu - Khôi phục", ref topY, new ToolBtn[]
        {
            new("📡 Sao lưu Wifi", () => ShowWifiBackupDialog()),
            new("💽 Sao lưu Driver", () => ShowDriverBackupDialog()),
            new("📁 Sao lưu Dữ liệu", () => ShowDataBackupDialog()),
            new("🌐 Sao lưu trình duyệt", () => ShowBrowserBackupDialog()),
            new("🔑 Mật khẩu & Chữ ký số", () => ShowCredentialAndCertificateDialog()),
            new("⬆️ Cập nhật driver hãng", () => _backupService.OpenBrandDriverPage(_sysInfo.Manufacturer))
        });
        pnl.Controls.Add(secBackup);

        // Section 3: Cài đặt
        var secInstall = CreateCardSection("📦  Cài đặt", ref topY, new ToolBtn[]
        {
            new("🛠️ Cài đặt phần mềm", () => ShowSoftwareInstallerDialog()),
            new("🗑️ Gỡ nhiều phần mềm", () => ShowBatchUninstallerDialog()),
            new("🧹 Xóa Bloatware", () => ShowBloatwareRemoverDialog())
        });
        pnl.Controls.Add(secInstall);

        // Section 4: WinPE - Cài Win
        var secWinPe = CreateCardSection("🚀  WinPE - Cài Win", ref topY, new ToolBtn[]
        {
            new("⚡ Tạo boot WinPE", () => ShowWinPeBootDialog()),
            new("📐 Fix limit chia drive", () => _diskService.FixPartitionLimits()),
            new("💿 Cài lại Win 10/11", () => ChooseAndApplyWim()),
            new("🌐 Kho ISO Windows", () => _maintenanceService.OpenUrl("https://www.microsoft.com/software-download")),
            new("🧹 Dọn boot + ổ WinPE", () => _diskService.CleanWinPeBoot())
        });
        pnl.Controls.Add(secWinPe);

        // Section 5: Tiện ích khác
        var secOther = CreateCardSection("🧰  Tiện ích khác", ref topY, new ToolBtn[]
        {
            new("🗑️ Xóa file rác", () => ShowSystemCleanerDialog()),
            new("🔍 Check - Test", () => ShowCheckTestDialog()),
            new("🖱️ Menu chuột Win 10", () => ToggleContextMenuStyle()),
            new("⚡ Tác vụ tự động", () => ShowAutoTasksDialog()),
            new("🎧 Hỗ trợ từ xa", () => ShowRemoteSupportDialog()),
            new("📄 Thiết lập Office", () => ShowOfficeConfigDialog())
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
        var secUpdate = CreateCardSection("🔄  Update Windows", ref topY, new ToolBtn[]
        {
            new("⚠️ Reset PC", () =>
            {
                var res = MessageBox.Show("Bạn có chắc chắn muốn mở trình khôi phục cài đặt gốc Windows (Reset this PC)?", "Xác Nhận Reset PC", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes) SafeLaunch("systemreset.exe");
            }),
            new("⛔ Chặn Win Update", () =>
            {
                _systemService.ToggleWindowsUpdate(true);
                Log("Đã chặn tự động cập nhật Windows Update (Dịch vụ wuauserv = Disabled & Group Policy NoAutoUpdate = 1).");
                MessageBox.Show("Đã thiết lập chặn Windows Update tự động thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }),
            new("🛡️ Chặn nâng 10->11", () =>
            {
                _systemService.BlockWin11Upgrade(true);
                Log("Đã chặn tự động nâng cấp Windows 11 qua chính sách TargetReleaseVersion.");
                MessageBox.Show("Đã chặn nâng cấp tự động lên Windows 11 (khóa phiên bản hiện tại)!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }),
            new("🔼 Nâng / Hạ Windows", () => SafeLaunch("ms-settings:activation", fallback: "slui.exe")),
            new("🎯 Đưa Win về đúng bản", () =>
            {
                Log("Đang truy vấn thông tin phiên bản Windows qua DISM...");
                var info = _systemService.GetWindowsEditionsInfo();
                Log(info);
                MessageBox.Show(info, "Thông Tin Phiên Bản Windows & Khả Năng Nâng/Hạ Cấp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }),
            new("🚀 Startup (tự bật app)", () => SafeLaunch("taskmgr.exe", args: "/0 /startup"))
        });
        pnl.Controls.Add(secUpdate);

        // Section 2: Khi máy có sự cố
        var secIssue = CreateCardSection("🔧  Khi máy có sự cố", ref topY, new ToolBtn[]
        {
            new("🌐 Chẩn đoán mạng", () => { _ = _maintenanceService.RunSystemRepairAsync("network_diag", Log); }),
            new("📦 Sửa lỗi file nén", () =>
            {
                bool ok = _systemService.FixCompressedFileAssociations(Log);
                if (ok) MessageBox.Show("Đã khôi phục liên kết mặc định cho file nén (.zip, .7z, .rar) thành công!", "Sửa Lỗi File Nén", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else MessageBox.Show("Có lỗi khi khôi phục liên kết file nén. Vui lòng kiểm tra quyền Administrator.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }),
            new("🖨️ Sửa lỗi máy in", () =>
            {
                bool ok = _systemService.ResetPrintSpooler(Log);
                if (ok) MessageBox.Show("Đã xóa lệnh in kẹt và khởi động lại dịch vụ Print Spooler thành công!", "Sửa Lỗi Máy In", MessageBoxButtons.OK, MessageBoxIcon.Information);
            })
        });
        pnl.Controls.Add(secIssue);

        // Section 3: Cài đặt Windows
        var secTransition = CreateCardSection("💿  Cài đặt Windows", ref topY, new ToolBtn[]
        {
            new("💾 Cài Win (WinPE)", () => ChooseAndApplyWim()),
            new("◀️ Win 10/11 → 7", () =>
            {
                Log("Hướng dẫn hạ cấp về Windows 7: Yêu cầu driver NVMe & USB 3.0 xHCI...");
                MessageBox.Show("Để hạ cấp từ Windows 10/11 về Windows 7 trên bo mạch đời mới:\n\n1. Bộ cài Windows 7 bắt buộc phải được nạp sẵn driver NVMe (KB2990941, KB3087873) và driver USB 3.0/3.1 (xHCI).\n2. Trong BIOS, cần chuyển từ Secure Boot -> Disabled, bật CSM / Legacy Support (nếu có).\n3. Sử dụng công cụ 'Tạo boot WinPE' để khởi động và nạp ảnh WIM vào ổ C:.", "Hạ Cấp Windows 7 - Hướng Dẫn Kỹ Thuật", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }),
            new("▶️ Win 7 → 10/11", () =>
            {
                Log("Mở trang tải bộ công cụ nâng cấp Windows Media Creation Tool...");
                _maintenanceService.OpenUrl("https://www.microsoft.com/software-download/windows10");
            }),
            new("🔄 Win 7 → 7", () =>
            {
                Log("Khởi động quy trình Cài đặt Windows 7 sạch (Clean Install)...");
                ChooseAndApplyWim();
            })
        });
        pnl.Controls.Add(secTransition);

        // Section 4: Linux thay Windows
        var secLinuxReplace = CreateCardSection("🐧  Linux thay Windows (xoá ổ C:)", ref topY, new ToolBtn[]
        {
            new("🐧 Win → Ubuntu", () => _maintenanceService.OpenUrl("https://ubuntu.com/download/desktop")),
            new("🌀 Win → Debian", () => _maintenanceService.OpenUrl("https://www.debian.org/download")),
            new("🍃 Win → Mint", () => _maintenanceService.OpenUrl("https://linuxmint.com/download.php")),
            new("🐉 Win → Kali", () => _maintenanceService.OpenUrl("https://www.kali.org/get-kali/"))
        });
        pnl.Controls.Add(secLinuxReplace);

        // Section 5: Linux song song Windows
        var secLinuxDual = CreateCardSection("🐧  Linux song song Windows", ref topY, new ToolBtn[]
        {
            new("🐧 Ubuntu (v8.1)", () => _maintenanceService.OpenUrl("https://releases.ubuntu.com/")),
            new("🌀 Debian (v9.1)", () => _maintenanceService.OpenUrl("https://cdimage.debian.org/")),
            new("🍃 Mint (v10.1)", () => _maintenanceService.OpenUrl("https://linuxmint.com/")),
            new("🐉 Kali (v11.1)", () => _maintenanceService.OpenUrl("https://www.kali.org/")),
            new("🧹 Gỡ Linux", () =>
            {
                var res = MessageBox.Show("Bạn có muốn dọn dẹp phân vùng Linux và khôi phục Windows Bootloader mặc định không?", "Gỡ Linux & Phục Hồi Windows Boot", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    Log("Đang chạy bootrec /fixmbr và bcdboot C:\\Windows...");
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c bootrec /fixmbr & bcdboot C:\\Windows /l vi-VN") { CreateNoWindow = true, UseShellExecute = false });
                    _diskService.CleanWinPeBoot();
                    Log("✅ Đã dọn dẹp phân vùng và phục hồi Windows Bootloader.");
                    MessageBox.Show("Đã phục hồi Windows Bootloader và dọn dẹp menu khởi động!", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            })
        });
        pnl.Controls.Add(secLinuxDual);

        // Section 6: Windows LTSC / Server
        var secLtsc = CreateCardSection("🏢  Windows LTSC / Server", ref topY, new ToolBtn[]
        {
            new("🖥️ Server 2019", () => _maintenanceService.OpenUrl("https://www.microsoft.com/evalcenter/evaluate-windows-server-2019")),
            new("🖥️ Server 2022", () => _maintenanceService.OpenUrl("https://www.microsoft.com/evalcenter/evaluate-windows-server-2022")),
            new("🖥️ Server 2025", () => _maintenanceService.OpenUrl("https://www.microsoft.com/evalcenter/evaluate-windows-server-2025")),
            new("⚡ Win 10 LTSC", () =>
            {
                Log("Mở trang tải bản dùng thử chính thức Windows 10 Enterprise LTSC...");
                _maintenanceService.OpenUrl("https://www.microsoft.com/evalcenter/evaluate-windows-10-enterprise");
            }),
            new("⚡ Win 11 LTSC", () =>
            {
                Log("Mở trang tải bản dùng thử chính thức Windows 11 Enterprise LTSC 2024...");
                _maintenanceService.OpenUrl("https://www.microsoft.com/evalcenter/evaluate-windows-11-enterprise");
            })
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
            Text = "🔗  Liên kết Google Sheet",
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
            Text = "Link triển khai Apps Script (Backup PC / Restore PC / Up Sheet):",
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
            Text = "Link Google Sheet quản lý thiết bị (để TRỐNG thì tool tự hỏi macro):",
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

        var btnCheckConn = CreateFlowButton("📡 Kiểm tra kết nối", 150, () =>
        {
            Log("Đang kiểm tra kết nối tới Google Apps Script...");
            _ = Task.Run(async () =>
            {
                var ok = await _sheetService.CheckConnectionAsync(_txtAppsScript.Text.Trim());
                Log(ok ? "✅ Kết nối Google Apps Script THÀNH CÔNG!" : "❌ Không thể kết nối. Vui lòng kiểm tra lại URL!");
                Invoke(() => MessageBox.Show(ok ? "Kết nối thành công!" : "Lỗi kết nối. Vui lòng kiểm tra URL!", "Thông báo", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning));
            });
        });

        var btnSaveSheet = CreateFlowButton("💾 Lưu & Áp dụng", 140, () =>
        {
            _deviceConfig.AppsScriptUrl = _txtAppsScript.Text.Trim();
            _deviceConfig.GoogleSheetUrl = _txtSheetUrl.Text.Trim();
            _sheetService.SaveConfig(_deviceConfig);
            Log("Đã lưu cấu hình Google Sheet thành công!");
            MessageBox.Show("Đã lưu cấu hình Google Sheet!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });

        var btnOpenSheet = CreateFlowButton("🌐 Mở sheet", 115, () =>
        {
            if (!string.IsNullOrWhiteSpace(_txtSheetUrl.Text))
            {
                _maintenanceService.OpenUrl(_txtSheetUrl.Text.Trim());
            }
            else
            {
                MessageBox.Show("Vui lòng nhập Link Google Sheet trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        });

        var btnDeploy = CreateFlowButton("📜 Deploy (cửa sổ)", 155, () => ShowAppsScriptModal());

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
            Text = "💻  Khởi tạo PC - chạy thẳng",
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
        var lblMachine = new Label { Text = "Tên máy:", AutoSize = true, Margin = new Padding(0, 8, 8, 0), Font = new Font("Segoe UI", 9f), ForeColor = TextHeading };
        _cboMachineName = new ComboBox { Size = new Size(180, 25), Margin = new Padding(0, 5, 8, 0), DropDownStyle = ComboBoxStyle.DropDown };
        var btnRename = CreateFlowButton("✏️ Đổi tên máy", 125, () =>
        {
            var newName = _cboMachineName.Text.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                SafeLaunch("cmd.exe", args: $"/c wmic computersystem where caption='%computername%' rename {newName}");
                Log($"Đã gửi lệnh đổi tên máy thành: {newName}");
            }
        });
        var btnAddMachine = CreateFlowButton("➕ Thêm", 90, () =>
        {
            var t = _cboMachineName.Text.Trim();
            if (!string.IsNullOrEmpty(t) && !_cboMachineName.Items.Contains(t))
            {
                _cboMachineName.Items.Add(t);
                _deviceConfig.MachineNames.Add(t);
                _sheetService.SaveConfig(_deviceConfig);
            }
        });
        var btnDelMachine = CreateFlowButton("🗑️ Xoá", 85, () =>
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
        var lblComp = new Label { Text = "Công ty:", AutoSize = true, Margin = new Padding(0, 8, 8, 0), Font = new Font("Segoe UI", 9f), ForeColor = TextHeading };
        _cboCompany = new ComboBox { Size = new Size(180, 25), Margin = new Padding(0, 5, 8, 0), DropDownStyle = ComboBoxStyle.DropDown };
        var btnUpSheet = CreateFlowButton("⬆️ Up Sheet", 110, () => { _ = RealUploadSheetAsync(false); });
        var btnUpBoth = CreateFlowButton("⬆️ Up cả 2 sheet", 135, () => { _ = RealUploadSheetAsync(true); });
        var btnAddComp = CreateFlowButton("➕ Thêm", 90, () =>
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
        var btnExportGs = CreateFlowButton("📄 Export .gs", 130, () => ShowAppsScriptModal());
        var btnExtractKey = CreateFlowButton("🔑 Trích xuất Key", 145, () =>
        {
            Log($"🔑 Windows Product Key: {_sysInfo.WindowsProductKey}");
            MessageBox.Show($"Windows Product Key:\n{_sysInfo.WindowsProductKey}", "Trích Xuất Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        var btnCompareKey = CreateFlowButton("🔍 So sánh Key", 135, () =>
        {
            Log("So sánh Key hệ thống với danh sách bản quyền...");
            MessageBox.Show($"Key hiện tại: {_sysInfo.WindowsProductKey}\nTrạng thái: Trùng khớp bản quyền số hợp lệ.", "So Sánh Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        flowInit3.Controls.Add(btnExportGs);
        flowInit3.Controls.Add(btnExtractKey);
        flowInit3.Controls.Add(btnCompareKey);

        var lblInitNote = new Label
        {
            Text = "Danh sách: + thêm mục · xoá (tất cả hoặc mục đang chọn) - chỉ sửa danh sách của tool.",
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
            Text = "🔄  Đồng bộ với Google Sheet",
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
        var btnRestorePc = CreateFlowButton("🔄 Restore PC", 125, () => { _ = RealRestorePcAsync(); });
        var btnCheckDup = CreateFlowButton("🔍 Check trùng lặp", 140, () => { _ = RealCheckDuplicateAsync(); });
        var btnReloadCfg = CreateFlowButton("🔄 Tải lại cấu hình", 130, () => LoadSystemData());

        flowSync1.Controls.Add(_cboSyncDevice);
        flowSync1.Controls.Add(btnRestorePc);
        flowSync1.Controls.Add(btnCheckDup);
        flowSync1.Controls.Add(btnReloadCfg);

        var lblSyncNote = new Label
        {
            Text = "Hai nút đầu hỏi Google Sheet qua mạng (có thể mất 30-60 giây, có nút Hủy).",
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
            Text = "📋  Nhật ký",
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
            Text = "🗑️ Xoá log",
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
        Log("Đang nạp trạng thái hệ thống...");
        _sysInfo = _systemService.GetSystemInfo();

        SetBadge(_lblBitLocker,
            _sysInfo.HasBitLockerLocked ? $"🔒 BitLocker: {_sysInfo.BitLockerStatus}" : "🔒 BitLocker: Chưa mã hóa (Sạch)",
            _sysInfo.HasBitLockerLocked ? BadgeType.Danger : BadgeType.Success);

        SetBadge(_lblPfx, _sysInfo.PfxStatusText, _sysInfo.HasPfxFiles ? BadgeType.Warning : BadgeType.Success);

        SetBadge(_lblBoot, $"💽 Chuẩn boot: {_sysInfo.BootMode}", BadgeType.Success);

        SetBadge(_lblIrst,
            _sysInfo.NeedsIrst ? "⚠️ CẦN driver IRST (VMD)" : "✅ IRST/VMD: Đủ driver",
            _sysInfo.NeedsIrst ? BadgeType.Warning : BadgeType.Success);

        _lblPath.Text = $"📂 Đang chạy tại: {_sysInfo.RunningPath}";
        _lblUser.Text = $"👤 User: {_sysInfo.UserName} - {_sysInfo.MachineName}";

        // Populate Tab 3 config
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

        Log($"Hệ thống sẵn sàng: {_sysInfo.MachineName} ({_sysInfo.OsVersion}) | Chuẩn Boot: {_sysInfo.BootMode}");
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

    private void ChooseAndApplyWim()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Tệp cài đặt Windows (*.wim;*.esd;*.iso)|*.wim;*.esd;*.iso|All files (*.*)|*.*",
            Title = "Chọn tệp ảnh cài đặt Windows"
        };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            Log($"Đã chọn file cài đặt: {ofd.FileName}");
            MessageBox.Show($"Đã chọn file ảnh: {ofd.FileName}\nBạn có thể áp dụng bằng DISM /Apply-Image vào phân vùng đích.", "Cài Đặt Windows", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
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
