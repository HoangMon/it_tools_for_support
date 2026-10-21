using System.Drawing;
using System.Windows.Forms;
using Ernastie.Services;

namespace Ernastie.Forms;

public class WindowsConfigForm : Form
{
    private readonly WindowsConfigService _service;

    // UI/UX Pro Max - Dynamic Design Tokens from ThemeHelper
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _cardBg => ThemeHelper.CardBg;
    private Color _borderColor => ThemeHelper.BorderColor;
    private Color _textHeading => ThemeHelper.TextHeading;
    private Color _textNormal => ThemeHelper.TextNormal;
    private Color _greenActive => ThemeHelper.SuccessGreen;
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnHover => ThemeHelper.BtnHover;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;

    // Security Controls
    private Label _lblToast = null!;
    private RadioButton _rbToastOn = null!;
    private RadioButton _rbToastOff = null!;

    private Label _lblUac = null!;
    private RadioButton _rbUacOn = null!;
    private RadioButton _rbUacOff = null!;

    private Label _lblSmartScreen = null!;
    private RadioButton _rbSmartScreenOn = null!;
    private RadioButton _rbSmartScreenOff = null!;

    private Label _lblFirewall = null!;
    private RadioButton _rbFirewallOn = null!;
    private RadioButton _rbFirewallOff = null!;

    private Label _lblSecAlerts = null!;
    private RadioButton _rbSecAlertsOn = null!;
    private RadioButton _rbSecAlertsOff = null!;

    // Quick Settings Controls
    private RadioButton _rbTime12 = null!;
    private RadioButton _rbTime24 = null!;
    private CheckBox _chkExplorerThisPc = null!;
    private CheckBox _chkDecimalComma = null!;
    private CheckBox _chkAdaptiveBrightness = null!;
    private CheckBox _chkUsKeyboard = null!;
    private Button _btnHibernate = null!;

    // User Management Controls
    private Label _lblCurrentLogin = null!;
    private DataGridView _gridUsers = null!;
    private List<UserModel> _userList = new();

    // Log & Status
    private RichTextBox _rtbLog = null!;
    private Label _lblStatus = null!;
    private CheckBox _chkDomain = null!;
    private Label _lblWorkgroupInfo = null!;

    public WindowsConfigForm()
    {
        _service = new WindowsConfigService();
        InitializeComponents();
        LoadCurrentConfigurations();
    }

    private void InitializeComponents()
    {
        Text = "⚙️ Trình cấu hình Windows";
        Size = new Size(1160, 920);
        MinimumSize = new Size(1040, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9f);
        Icon = SystemIcons.Shield;
        DoubleBuffered = true;
        MaximizeBox = true;
        MinimizeBox = true;

        var mainScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(16),
            BackColor = _bgForm
        };

        // Master vertical container
        var masterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _bgForm
        };
        masterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        masterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        masterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        masterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // ROW 1: TableLayout with 2 Columns (Bảo mật & Thiết lập nhanh)
        var tblTop = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 390,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _bgForm,
            Margin = new Padding(0, 0, 0, 14)
        };
        tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var cardSecurity = BuildSecurityCard();
        cardSecurity.Dock = DockStyle.Fill;
        cardSecurity.Margin = new Padding(0, 0, 8, 0);

        var cardQuick = BuildQuickCard();
        cardQuick.Dock = DockStyle.Fill;
        cardQuick.Margin = new Padding(8, 0, 0, 0);

        tblTop.Controls.Add(cardSecurity, 0, 0);
        tblTop.Controls.Add(cardQuick, 1, 0);
        masterLayout.Controls.Add(tblTop, 0, 0);

        // ROW 2: User Management Card (Resizable, spacious)
        var cardUsers = BuildUserCard();
        cardUsers.Dock = DockStyle.Top;
        cardUsers.Height = 390;
        cardUsers.Margin = new Padding(0, 0, 0, 14);
        masterLayout.Controls.Add(cardUsers, 0, 1);

        // ROW 3: Log Card & Status Bar
        var cardBottom = BuildBottomCard();
        cardBottom.Dock = DockStyle.Top;
        cardBottom.Height = 240;
        cardBottom.Margin = new Padding(0, 0, 0, 16);
        masterLayout.Controls.Add(cardBottom, 0, 2);

        mainScroll.Controls.Add(masterLayout);
        Controls.Add(mainScroll);
        ThemeHelper.ApplyTheme(this);
    }

    #region 1. Bảo Mật & Tường Lửa Card

    private Panel BuildSecurityCard()
    {
        var card = CreateBaseCard();

        var lblTitle = new Label
        {
            Text = "🔒  Bảo mật & Tường lửa",
            Font = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };

        // Responsive rows container
        var pnlRows = new Panel
        {
            Dock = DockStyle.Top,
            Height = 185,
            BackColor = Color.Transparent
        };

        // 5 Rows built in reverse order for Dock = Top
        _lblSecAlerts = CreateSettingRow(pnlRows, "Security Alerts:", out _rbSecAlertsOn, out _rbSecAlertsOff);
        _lblFirewall = CreateSettingRow(pnlRows, "Tường lửa (mọi profile):", out _rbFirewallOn, out _rbFirewallOff);
        _lblSmartScreen = CreateSettingRow(pnlRows, "SmartScreen (Ứng dụng & Explorer):", out _rbSmartScreenOn, out _rbSmartScreenOff);
        _lblUac = CreateSettingRow(pnlRows, "UAC (Yêu cầu khởi động lại):", out _rbUacOn, out _rbUacOff);
        _lblToast = CreateSettingRow(pnlRows, "Thông báo Windows (Toast):", out _rbToastOn, out _rbToastOff);

        // Action Buttons Row (Responsive TableLayoutPanel)
        var tblButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 6)
        };
        tblButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31f));
        tblButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31f));
        tblButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));

        var btnDefault = CreateStyledButton("↩ Mặc định", () =>
        {
            _rbToastOn.Checked = true;
            _rbUacOn.Checked = true;
            _rbSmartScreenOn.Checked = true;
            _rbFirewallOn.Checked = true;
            _rbSecAlertsOn.Checked = true;
            Log("Đã chọn cấu hình Bảo mật MẶC ĐỊNH.");
        });
        btnDefault.Dock = DockStyle.Fill;
        btnDefault.Margin = new Padding(0, 0, 4, 0);

        var btnReload = CreateStyledButton("🔄 Đọc lại", () =>
        {
            LoadSecuritySettings();
            Log("Đã nạp lại trạng thái Bảo mật & Tường lửa từ hệ thống.");
        });
        btnReload.Dock = DockStyle.Fill;
        btnReload.Margin = new Padding(4, 0, 4, 0);

        var btnApply = CreatePrimaryButton("Áp dụng", () =>
        {
            ApplySecuritySettings();
        });
        btnApply.Dock = DockStyle.Fill;
        btnApply.Margin = new Padding(4, 0, 0, 0);

        tblButtons.Controls.Add(btnDefault, 0, 0);
        tblButtons.Controls.Add(btnReload, 1, 0);
        tblButtons.Controls.Add(btnApply, 2, 0);

        // Note Label
        var lblNote = new Label
        {
            Text = "Ghi chú: Lựa chọn màu XANH LÁ biểu thị chức năng đang bật.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(71, 85, 105),
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Added in reverse order for Dock = Top
        card.Controls.Add(lblNote);
        card.Controls.Add(tblButtons);
        card.Controls.Add(pnlRows);
        card.Controls.Add(lblTitle);

        return card;
    }

    private Label CreateSettingRow(Panel parent, string labelText, out RadioButton rbOn, out RadioButton rbOff)
    {
        var row = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4)
        };

        // Right side: Radios panel pinned to right edge with ample width for high DPI & bold text
        var pnlRadios = new Panel
        {
            Dock = DockStyle.Right,
            Width = 190,
            BackColor = Color.Transparent
        };

        rbOn = new RadioButton
        {
            Text = "Bật",
            Location = new Point(0, 4),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        rbOff = new RadioButton
        {
            Text = "Tắt",
            Location = new Point(95, 4),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };

        var onRef = rbOn;
        var offRef = rbOff;
        onRef.CheckedChanged += (_, _) =>
        {
            onRef.ForeColor = onRef.Checked ? Color.FromArgb(21, 128, 61) : _textNormal;
            onRef.Font = new Font("Segoe UI", 9f, onRef.Checked ? FontStyle.Bold : FontStyle.Regular);
        };
        offRef.CheckedChanged += (_, _) =>
        {
            offRef.ForeColor = offRef.Checked ? Color.FromArgb(185, 28, 28) : _textNormal;
            offRef.Font = new Font("Segoe UI", 9f, offRef.Checked ? FontStyle.Bold : FontStyle.Regular);
        };

        pnlRadios.Controls.Add(rbOn);
        pnlRadios.Controls.Add(rbOff);

        // Left side: Label fills ALL remaining space, guaranteed zero truncation
        var lbl = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textHeading,
            UseMnemonic = false,
            AutoEllipsis = false
        };

        row.Controls.Add(lbl);
        row.Controls.Add(pnlRadios);
        parent.Controls.Add(row);

        return lbl;
    }

    #endregion

    #region 2. Thiết Lập Nhanh Card

    private Panel BuildQuickCard()
    {
        var card = CreateBaseCard();

        var lblTitle = new Label
        {
            Text = "⚡  Thiết lập nhanh",
            Font = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Item 1: Button GMT+7
        var btnGmt7 = CreateStyledButton("⏰ Đặt giờ GMT+7 + Định dạng dd/MM/yyyy", () => _service.SetGmt7AndDateFormat(Log));
        btnGmt7.Dock = DockStyle.Top;
        btnGmt7.Height = 34;

        // Item 2: Clock 12h / 24h row
        var pnlClockRow = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent, Padding = new Padding(0, 2, 0, 2) };
        var pnlClockRadios = new Panel { Dock = DockStyle.Right, Width = 210, BackColor = Color.Transparent };
        _rbTime12 = new RadioButton { Text = "12h (AM/PM)", Location = new Point(0, 5), AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = _textHeading, Cursor = Cursors.Hand };
        _rbTime24 = new RadioButton { Text = "24h", Location = new Point(120, 5), AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = _textHeading, Cursor = Cursors.Hand };
        _rbTime12.CheckedChanged += (_, _) => { if (_rbTime12.Focused) _service.SetTimeFormat(true); };
        _rbTime24.CheckedChanged += (_, _) => { if (_rbTime24.Focused) _service.SetTimeFormat(false); };
        pnlClockRadios.Controls.Add(_rbTime12);
        pnlClockRadios.Controls.Add(_rbTime24);

        var lblClock = new Label { Text = "🕐 Giờ hệ thống:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = _textHeading };
        pnlClockRow.Controls.Add(lblClock);
        pnlClockRow.Controls.Add(pnlClockRadios);

        // Item 3: Explorer This PC
        _chkExplorerThisPc = new CheckBox
        {
            Text = "Mở File Explorer mặc định là This PC",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _chkExplorerThisPc.CheckedChanged += (_, _) =>
        {
            if (_chkExplorerThisPc.Focused)
            {
                _service.SetExplorerLaunchToThisPc(_chkExplorerThisPc.Checked);
                Log(_chkExplorerThisPc.Checked ? "Đã đặt Explorer mở This PC." : "Đã đặt Explorer mở Quick Access.");
            }
        };

        // Item 4: Comma decimal & Show Hidden
        var pnlCommaRow = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent, Padding = new Padding(0, 2, 0, 2) };
        var btnShowHidden = CreateStyledButton("👁️ Hiện file ẩn", () => _service.ToggleShowHiddenFiles(Log));
        btnShowHidden.Dock = DockStyle.Right;
        btnShowHidden.Width = 150;

        _chkDecimalComma = new CheckBox
        {
            Text = "Đổi dấu . thành ,",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _chkDecimalComma.CheckedChanged += (_, _) =>
        {
            if (_chkDecimalComma.Focused)
            {
                _service.SetDecimalComma(_chkDecimalComma.Checked);
                Log(_chkDecimalComma.Checked ? "Đã chuyển dấu thập phân thành dấu phẩy (,)." : "Đã chuyển dấu thập phân thành dấu chấm (.).");
            }
        };
        pnlCommaRow.Controls.Add(_chkDecimalComma);
        pnlCommaRow.Controls.Add(btnShowHidden);

        // Item 5: Adaptive brightness
        _chkAdaptiveBrightness = new CheckBox
        {
            Text = "Tắt tự động tăng giảm độ sáng màn hình",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _chkAdaptiveBrightness.CheckedChanged += (_, _) =>
        {
            if (_chkAdaptiveBrightness.Focused)
            {
                _service.SetAdaptiveBrightness(_chkAdaptiveBrightness.Checked, Log);
            }
        };

        // Item 6: Remove other keyboards
        _chkUsKeyboard = new CheckBox
        {
            Text = "Xóa bàn phím ngôn ngữ khác (giữ lại US)",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _chkUsKeyboard.CheckedChanged += (_, _) =>
        {
            if (_chkUsKeyboard.Focused && _chkUsKeyboard.Checked)
            {
                _service.KeepOnlyUsKeyboard(Log);
            }
        };

        // Item 7: Hibernate Button
        _btnHibernate = CreateStyledButton("🚫 Tắt ngủ đông", () =>
        {
            var (isEnabled, _) = _service.GetHibernateInfo();
            _service.SetHibernate(!isEnabled, Log);
            UpdateHibernateButton();
        });
        _btnHibernate.Dock = DockStyle.Top;
        _btnHibernate.Height = 34;

        // Item 8: Power mode & Default
        var tblPower = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        tblPower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
        tblPower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));

        var btnPower = CreateStyledButton("💤 Ngủ / Màn hình / Power mode", () => _service.OpenPowerOptions());
        btnPower.Dock = DockStyle.Fill;
        btnPower.Margin = new Padding(0, 0, 4, 0);

        var btnQuickDef = CreateStyledButton("↩ Mặc định", () =>
        {
            _chkExplorerThisPc.Checked = true;
            _chkAdaptiveBrightness.Checked = true;
            _chkUsKeyboard.Checked = true;
            _rbTime12.Checked = true;
            Log("Đã khôi phục thiết lập nhanh mặc định.");
        });
        btnQuickDef.Dock = DockStyle.Fill;
        btnQuickDef.Margin = new Padding(4, 0, 0, 0);

        tblPower.Controls.Add(btnPower, 0, 0);
        tblPower.Controls.Add(btnQuickDef, 1, 0);

        // Add all controls in REVERSE order so Dock = Top stacks them from top to bottom!
        card.Controls.Add(tblPower);
        card.Controls.Add(_btnHibernate);
        card.Controls.Add(_chkUsKeyboard);
        card.Controls.Add(_chkAdaptiveBrightness);
        card.Controls.Add(pnlCommaRow);
        card.Controls.Add(_chkExplorerThisPc);
        card.Controls.Add(pnlClockRow);
        card.Controls.Add(btnGmt7);
        card.Controls.Add(lblTitle);

        return card;
    }

    #endregion

    #region 3. Quản Lý User (Local) Card

    private Panel BuildUserCard()
    {
        var card = CreateBaseCard();

        // Top Header Row with title on left and login info on right
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "👤  Quản lý User (local)",
            Font = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblCurrentLogin = new Label
        {
            Text = $"User đang login: {Environment.UserName} - {Environment.MachineName}",
            ForeColor = Color.FromArgb(2, 132, 199),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(_lblCurrentLogin);
        card.Controls.Add(pnlHeader);

        // 3 Action Buttons Top (Responsive TableLayoutPanel)
        var tblTopBtns = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 6)
        };
        tblTopBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        tblTopBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        tblTopBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        var btnCreateAdmin = CreateStyledButton("Tạo admin", () => ShowCreateUserModal(true));
        btnCreateAdmin.Dock = DockStyle.Fill;
        btnCreateAdmin.Margin = new Padding(0, 0, 4, 0);

        var btnRename = CreateStyledButton("Đổi tên hiển thị", () => ShowRenameModal());
        btnRename.Dock = DockStyle.Fill;
        btnRename.Margin = new Padding(4, 0, 4, 0);

        var btnCreateUser = CreateStyledButton("Tạo User mới", () => ShowCreateUserModal(false));
        btnCreateUser.Dock = DockStyle.Fill;
        btnCreateUser.Margin = new Padding(4, 0, 0, 0);

        tblTopBtns.Controls.Add(btnCreateAdmin, 0, 0);
        tblTopBtns.Controls.Add(btnRename, 1, 0);
        tblTopBtns.Controls.Add(btnCreateUser, 2, 0);
        card.Controls.Add(tblTopBtns);

        // 3 Action Buttons Bottom (Responsive TableLayoutPanel)
        var tblBottomBtns = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0)
        };
        tblBottomBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
        tblBottomBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
        tblBottomBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));

        var btnExecute = CreatePrimaryButton("THỰC THI", () => ExecuteUserChanges());
        btnExecute.Dock = DockStyle.Fill;
        btnExecute.Margin = new Padding(0, 0, 4, 0);

        var btnRefresh = CreateStyledButton("Làm Mới Danh Sách", () => LoadUserList());
        btnRefresh.Dock = DockStyle.Fill;
        btnRefresh.Margin = new Padding(4, 0, 4, 0);

        var btnSetPass = CreateStyledButton("Đặt Mật Khẩu", () => ShowSetPasswordModal());
        btnSetPass.Dock = DockStyle.Fill;
        btnSetPass.Margin = new Padding(4, 0, 0, 0);

        tblBottomBtns.Controls.Add(btnExecute, 0, 0);
        tblBottomBtns.Controls.Add(btnRefresh, 1, 0);
        tblBottomBtns.Controls.Add(btnSetPass, 2, 0);
        card.Controls.Add(tblBottomBtns);

        // DataGridView fills the rest of the space, adapts to any size
        _gridUsers = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = _cardBg,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Font = new Font("Segoe UI", 9f),
            EnableHeadersVisualStyles = false,
            GridColor = _borderColor,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        _gridUsers.DefaultCellStyle.BackColor = _cardBg;
        _gridUsers.DefaultCellStyle.ForeColor = _textNormal;
        _gridUsers.ColumnHeadersDefaultCellStyle.BackColor = _btnBg;
        _gridUsers.ColumnHeadersDefaultCellStyle.ForeColor = _textHeading;
        _gridUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _gridUsers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _gridUsers.ColumnHeadersHeight = 34;
        _gridUsers.RowTemplate.Height = 28;

        // Fully responsive columns with fill weights and minimum widths
        _gridUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "User", Name = "ColUser", FillWeight = 26, MinimumWidth = 160, ReadOnly = true });
        _gridUsers.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Active", Name = "ColActive", FillWeight = 10, MinimumWidth = 65 });
        _gridUsers.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Disable", Name = "ColDisable", FillWeight = 10, MinimumWidth = 65 });
        _gridUsers.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Admins", Name = "ColAdmins", FillWeight = 10, MinimumWidth = 65 });
        _gridUsers.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Never Exp", Name = "ColNeverExp", FillWeight = 11, MinimumWidth = 80 });
        _gridUsers.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Xóa Pass", Name = "ColDelPass", FillWeight = 11, MinimumWidth = 80 });
        _gridUsers.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "XÓA USER", Name = "ColDelUser", FillWeight = 11, MinimumWidth = 85 });
        _gridUsers.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "XÓA PROFILE", Name = "ColDelProfile", FillWeight = 12, MinimumWidth = 95 });

        _gridUsers.CellFormatting += (s, e) =>
        {
            if (e.ColumnIndex == 0 && e.Value != null)
            {
                var val = e.Value.ToString() ?? "";
                if (val.Contains("[ADMIN]"))
                {
                    e.CellStyle!.ForeColor = _greenActive;
                    e.CellStyle!.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
            }
        };

        card.Controls.Add(_gridUsers);
        return card;
    }

    #endregion

    #region 4. Nhật Ký & Thanh Trạng Thái Card

    private Panel BuildBottomCard()
    {
        var card = CreateBaseCard();

        // Header with title and clear button
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.Transparent
        };

        var lblLog = new Label
        {
            Text = "📋  Nhật ký",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnClearLog = CreateStyledButton("🗑️ Xoá log", () => _rtbLog.Clear());
        btnClearLog.Dock = DockStyle.Right;
        btnClearLog.Width = 95;

        pnlHeader.Controls.Add(lblLog);
        pnlHeader.Controls.Add(btnClearLog);
        card.Controls.Add(pnlHeader);

        // Status bar docked to bottom (Responsive FlowLayoutPanel)
        var pnlStatus = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 6, 0, 0)
        };

        _lblStatus = new Label
        {
            Text = "Trạng thái: Sẵn sàng.",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Margin = new Padding(0, 6, 16, 0),
            AutoSize = true
        };

        var btnNetwork = CreateStyledButton("🌐 Thiết lập mạng", () => _service.OpenNetworkSettings());
        btnNetwork.Size = new Size(160, 32);
        btnNetwork.Margin = new Padding(0, 0, 16, 0);

        _chkDomain = new CheckBox
        {
            Text = "🏢 Domain (bỏ tick = Workgroup)",
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textNormal,
            Margin = new Padding(0, 6, 16, 0),
            Cursor = Cursors.Hand
        };
        _chkDomain.CheckedChanged += (_, _) =>
        {
            if (_chkDomain.Focused)
            {
                _service.OpenSystemProperties();
            }
        };

        _lblWorkgroupInfo = new Label
        {
            Text = "Hiện tại: Workgroup WORKGROUP",
            AutoSize = true,
            ForeColor = Color.FromArgb(2, 132, 199),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Margin = new Padding(0, 6, 0, 0)
        };

        pnlStatus.Controls.Add(_lblStatus);
        pnlStatus.Controls.Add(btnNetwork);
        pnlStatus.Controls.Add(_chkDomain);
        pnlStatus.Controls.Add(_lblWorkgroupInfo);
        card.Controls.Add(pnlStatus);

        // Log fills the center space
        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.LogBg,
            ForeColor = ThemeHelper.LogText,
            Font = new Font("Consolas", 9.25f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };
        card.Controls.Add(_rtbLog);

        return card;
    }

    #endregion

    #region Helpers & Data Loading

    private Panel CreateBaseCard()
    {
        var pnl = new Panel
        {
            BackColor = _cardBg,
            Padding = new Padding(14),
            Tag = "card"
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };
        return pnl;
    }

    private Button CreateStyledButton(string text, Action action)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = _btnBorder;
        btn.FlatAppearance.MouseOverBackColor = _btnHover;
        btn.Click += (_, _) =>
        {
            try { action(); }
            catch (Exception ex) { Log($"[Lỗi]: {ex.Message}"); }
        };
        return btn;
    }

    private Button CreatePrimaryButton(string text, Action action)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = _primaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(29, 78, 216);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 130, 246);
        btn.Click += (_, _) =>
        {
            try { action(); }
            catch (Exception ex) { Log($"[Lỗi]: {ex.Message}"); }
        };
        return btn;
    }

    private void LoadCurrentConfigurations()
    {
        Log("Đang nạp cấu hình Windows...");

        // 1. Security
        LoadSecuritySettings();

        // 2. Quick Settings
        bool is12h = _service.GetTimeFormatIs12h();
        if (is12h) _rbTime12.Checked = true; else _rbTime24.Checked = true;

        _chkExplorerThisPc.Checked = _service.GetExplorerLaunchToThisPc();
        _chkDecimalComma.Checked = _service.GetDecimalComma();
        _chkAdaptiveBrightness.Checked = true;
        _chkUsKeyboard.Checked = true;

        UpdateHibernateButton();

        // 3. Users
        LoadUserList();

        // 4. Domain & Workgroup
        var (wg, isDomain) = _service.GetWorkgroupInfo();
        _chkDomain.Checked = isDomain;
        _lblWorkgroupInfo.Text = isDomain ? $"Hiện tại: Domain {wg}" : $"Hiện tại: Workgroup {wg}";

        _lblStatus.Text = "Trạng thái: Sẵn sàng.";
        Log("✅ Nạp hoàn tất trạng thái trình cấu hình Windows!");
    }

    private void LoadSecuritySettings()
    {
        bool toast = _service.GetToastNotifications();
        _rbToastOn.Checked = toast; _rbToastOff.Checked = !toast;
        UpdateLabelColor(_lblToast, toast);

        bool uac = _service.GetUacStatus();
        _rbUacOn.Checked = uac; _rbUacOff.Checked = !uac;
        UpdateLabelColor(_lblUac, uac);

        bool smart = _service.GetSmartScreenStatus();
        _rbSmartScreenOn.Checked = smart; _rbSmartScreenOff.Checked = !smart;
        UpdateLabelColor(_lblSmartScreen, smart);

        bool fw = _service.GetFirewallStatus();
        _rbFirewallOn.Checked = fw; _rbFirewallOff.Checked = !fw;
        UpdateLabelColor(_lblFirewall, fw);

        bool alerts = _service.GetSecurityAlertsStatus();
        _rbSecAlertsOn.Checked = alerts; _rbSecAlertsOff.Checked = !alerts;
        UpdateLabelColor(_lblSecAlerts, alerts);
    }

    private void ApplySecuritySettings()
    {
        _service.SetToastNotifications(_rbToastOn.Checked);
        _service.SetUacStatus(_rbUacOn.Checked);
        _service.SetSmartScreenStatus(_rbSmartScreenOn.Checked);
        _service.SetFirewallStatus(_rbFirewallOn.Checked);
        _service.SetSecurityAlertsStatus(_rbSecAlertsOn.Checked);

        UpdateLabelColor(_lblToast, _rbToastOn.Checked);
        UpdateLabelColor(_lblUac, _rbUacOn.Checked);
        UpdateLabelColor(_lblSmartScreen, _rbSmartScreenOn.Checked);
        UpdateLabelColor(_lblFirewall, _rbFirewallOn.Checked);
        UpdateLabelColor(_lblSecAlerts, _rbSecAlertsOn.Checked);

        Log("✅ Đã áp dụng các thiết lập Bảo mật & Tường lửa thành công!");
        MessageBox.Show("Đã lưu các thiết lập Bảo mật & Tường lửa!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateLabelColor(Label lbl, bool active)
    {
        // Keep label heading high-contrast (Slate 900) so it remains razor-sharp (WCAG AAA)
        lbl.ForeColor = _textHeading;
        lbl.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold);
    }

    private void UpdateHibernateButton()
    {
        var (isEnabled, sizeGb) = _service.GetHibernateInfo();
        if (isEnabled)
        {
            _btnHibernate.Text = $"🚫 Tắt ngủ đông - đang chiếm {sizeGb:N2} GB";
            _btnHibernate.ForeColor = Color.FromArgb(185, 28, 28);
        }
        else
        {
            _btnHibernate.Text = "⚡ Bật ngủ đông (Hibernate)";
            _btnHibernate.ForeColor = _textHeading;
        }
    }

    private void LoadUserList()
    {
        _gridUsers.Rows.Clear();
        _userList = _service.GetLocalUsers();

        foreach (var u in _userList)
        {
            _gridUsers.Rows.Add(
                u.DisplayText,
                u.IsActive,
                u.IsDisabled,
                u.IsAdmin,
                u.PasswordNeverExpires,
                false,
                false,
                false
            );
        }
        Log($"Đã nạp danh sách {_userList.Count} tài khoản người dùng cục bộ.");
    }

    private void ExecuteUserChanges()
    {
        if (_gridUsers.Rows.Count == 0) return;

        for (int i = 0; i < _gridUsers.Rows.Count; i++)
        {
            var row = _gridUsers.Rows[i];
            var u = _userList[i];

            u.IsActive = (bool)(row.Cells["ColActive"].Value ?? false);
            u.IsDisabled = (bool)(row.Cells["ColDisable"].Value ?? false);
            u.IsAdmin = (bool)(row.Cells["ColAdmins"].Value ?? false);
            u.PasswordNeverExpires = (bool)(row.Cells["ColNeverExp"].Value ?? false);
            u.DeletePassword = (bool)(row.Cells["ColDelPass"].Value ?? false);
            u.DeleteUser = (bool)(row.Cells["ColDelUser"].Value ?? false);
            u.DeleteProfile = (bool)(row.Cells["ColDelProfile"].Value ?? false);
        }

        _service.ApplyUserChanges(_userList, Log);
        LoadUserList();
        MessageBox.Show("Đã hoàn tất thực thi các thay đổi User!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowCreateUserModal(bool isAdmin)
    {
        using var form = new Form
        {
            Text = isAdmin ? "Tạo Tài Khoản Quản Trị Viên (Admin)" : "Tạo Tài Khoản Người Dùng Mới",
            Size = new Size(440, 250),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = _bgForm,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9f)
        };

        var lblU = new Label { Text = "Tên tài khoản (Username):", Location = new Point(24, 18), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = _textHeading };
        var txtU = new TextBox { Location = new Point(24, 44), Size = new Size(395, 26), Font = new Font("Segoe UI", 9.5f) };

        var lblP = new Label { Text = "Mật khẩu (để trống nếu không đặt):", Location = new Point(24, 82), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = _textHeading };
        var txtP = new TextBox { Location = new Point(24, 108), Size = new Size(395, 26), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 9.5f) };

        var btnOk = CreatePrimaryButton(isAdmin ? "Tạo Admin" : "Tạo User", () =>
        {
            var u = txtU.Text.Trim();
            if (string.IsNullOrEmpty(u))
            {
                MessageBox.Show("Vui lòng nhập tên tài khoản!", "Cảnh Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (isAdmin) _service.CreateAdmin(u, txtP.Text, Log);
            else _service.CreateStandardUser(u, txtP.Text, Log);
            LoadUserList();
            form.Close();
        });
        btnOk.Location = new Point(24, 154);
        btnOk.Size = new Size(160, 36);

        var btnCancel = CreateStyledButton("Hủy", () => form.Close());
        btnCancel.Location = new Point(196, 154);
        btnCancel.Size = new Size(120, 36);

        form.Controls.Add(lblU);
        form.Controls.Add(txtU);
        form.Controls.Add(lblP);
        form.Controls.Add(txtP);
        form.Controls.Add(btnOk);
        form.Controls.Add(btnCancel);
        form.ShowDialog(this);
    }

    private void ShowRenameModal()
    {
        if (_gridUsers.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn một dòng User trong bảng trước!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int idx = _gridUsers.SelectedRows[0].Index;
        var user = _userList[idx];

        using var form = new Form
        {
            Text = $"Đổi Tên Hiển Thị Cho '{user.Username}'",
            Size = new Size(460, 220),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = _bgForm,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9f)
        };

        var lbl = new Label { Text = "Tên hiển thị mới (Full Name):", Location = new Point(24, 20), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = _textHeading };
        var txt = new TextBox { Location = new Point(24, 48), Size = new Size(395, 26), Font = new Font("Segoe UI", 9.5f) };

        var btnOk = CreatePrimaryButton("Lưu Tên Mới", () =>
        {
            var newName = txt.Text.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                _service.ChangeDisplayName(user.Username, newName, Log);
                LoadUserList();
                form.Close();
            }
        });
        btnOk.Location = new Point(24, 110);
        btnOk.Size = new Size(160, 36);

        var btnCancel = CreateStyledButton("Hủy", () => form.Close());
        btnCancel.Location = new Point(196, 110);
        btnCancel.Size = new Size(120, 36);

        form.Controls.Add(lbl);
        form.Controls.Add(txt);
        form.Controls.Add(btnOk);
        form.Controls.Add(btnCancel);
        form.ShowDialog(this);
    }

    private void ShowSetPasswordModal()
    {
        if (_gridUsers.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn tài khoản cần đặt mật khẩu trong bảng!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int idx = _gridUsers.SelectedRows[0].Index;
        var user = _userList[idx];

        using var form = new Form
        {
            Text = $"Đặt Mật Khẩu Cho '{user.Username}'",
            Size = new Size(460, 230),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = _bgForm,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9f)
        };

        var lbl = new Label { Text = $"Nhập mật khẩu mới cho '{user.Username}':", Location = new Point(24, 20), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = _textHeading };
        var txt = new TextBox { Location = new Point(24, 48), Size = new Size(395, 26), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 9.5f) };

        var btnOk = CreatePrimaryButton("Đặt Mật Khẩu", () =>
        {
            _service.SetPassword(user.Username, txt.Text, Log);
            MessageBox.Show($"Đã cập nhật mật khẩu cho '{user.Username}'!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            form.Close();
        });
        btnOk.Location = new Point(24, 115);
        btnOk.Size = new Size(160, 36);

        var btnCancel = CreateStyledButton("Hủy", () => form.Close());
        btnCancel.Location = new Point(196, 115);
        btnCancel.Size = new Size(120, 36);

        form.Controls.Add(lbl);
        form.Controls.Add(txt);
        form.Controls.Add(btnOk);
        form.Controls.Add(btnCancel);
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
