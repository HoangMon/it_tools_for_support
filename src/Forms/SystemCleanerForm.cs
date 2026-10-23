using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ernastie.Models;
using Ernastie.Services;

namespace Ernastie.Forms;

public class SystemCleanerForm : Form
{
    private readonly MaintenanceService _maintenanceService;
    private string _targetUser;

    // Theme Tokens
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _cardBg => ThemeHelper.CardBg;
    private Color _borderColor => ThemeHelper.BorderColor;
    private Color _textHeading => ThemeHelper.TextHeading;
    private Color _textNormal => ThemeHelper.TextNormal;
    private Color _textMuted => ThemeHelper.TextMuted;
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnHover => ThemeHelper.BtnHover;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;
    private Color _dangerRed => ThemeHelper.DangerRed;

    // Top Controls
    private ComboBox _cboUser = null!;
    private Label _lblAutoDetect = null!;

    // Checkboxes - Column 1 (Left)
    private CheckBox _chkUserTemp = null!;
    private CheckBox _chkWinTemp = null!;
    private CheckBox _chkWinUpdate = null!;
    private CheckBox _chkPrefetch = null!;
    private CheckBox _chkExplorerCache = null!;
    private CheckBox _chkEdgeCache = null!;
    private CheckBox _chkChromeCache = null!;
    private CheckBox _chkFirefoxCache = null!;
    private CheckBox _chkEdgeHistory = null!;
    private CheckBox _chkFirefoxHistory = null!;
    private CheckBox _chkChromeLogout = null!;
    private CheckBox _chkEdgeLogout = null!;
    private CheckBox _chkFirefoxLogout = null!;
    private CheckBox _chkZaloReset = null!;
    private CheckBox _chkCredentialManager = null!;

    // Checkboxes - Column 2 (Right)
    private CheckBox _chkINetCache = null!;
    private CheckBox _chkWebCache = null!;
    private CheckBox _chkCrashDumps = null!;
    private CheckBox _chkWer = null!;
    private CheckBox _chkDirectXCache = null!;
    private CheckBox _chkRecycleBin = null!;
    private CheckBox _chkQuickAccess = null!;
    private CheckBox _chkChromeHistory = null!;
    private CheckBox _chkTeamsReset = null!;

    // Custom Folders
    private ListBox _lstCustomFolders = null!;
    private Button _btnAddFolder = null!;
    private Button _btnRemoveFolder = null!;

    // Action Buttons
    private Button _btnSelectAll = null!;
    private Button _btnDeselectAll = null!;
    private Button _btnStartClean = null!;
    private Button _btnEmailSettings = null!;
    private Button _btnClose = null!;

    // Status & Progress
    private ProgressBar _prgBar = null!;
    private Label _lblStatus = null!;

    public SystemCleanerForm()
    {
        _maintenanceService = new MaintenanceService();
        _targetUser = Environment.UserName;

        InitializeComponents();
        ThemeHelper.ApplyTheme(this);
    }

    private void InitializeComponents()
    {
        Text = "Dọn rác hệ thống";
        Size = new Size(890, 770);
        MinimumSize = new Size(820, 690);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgForm;
        ForeColor = _textNormal;
        Font = new Font("Segoe UI", 9.25f);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;

        var mainScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = _bgForm
        };
        Controls.Add(mainScroll);

        // 1. Top Bar: Dọn rác cho user & Tự động dò
        var pnlUserRow = BuildUserSelectionBar();
        pnlUserRow.Dock = DockStyle.Top;
        mainScroll.Controls.Add(pnlUserRow);

        // 2. Main Checkboxes Card: Chọn nơi cần dọn
        var pnlCheckboxes = BuildCheckboxesCard();
        pnlCheckboxes.Dock = DockStyle.Top;
        mainScroll.Controls.Add(pnlCheckboxes);

        // 3. Custom Folders Card: Thư mục khác cần dọn
        var pnlCustomFolders = BuildCustomFoldersCard();
        pnlCustomFolders.Dock = DockStyle.Top;
        mainScroll.Controls.Add(pnlCustomFolders);

        // 4. Action Buttons Bar
        var pnlActions = BuildActionsBar();
        pnlActions.Dock = DockStyle.Top;
        mainScroll.Controls.Add(pnlActions);

        // 5. Progress Bar
        _prgBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 14,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous,
            Margin = new Padding(0, 8, 0, 4)
        };
        var pnlProgress = new Panel { Dock = DockStyle.Top, Height = 22, Padding = new Padding(0, 4, 0, 4), BackColor = _bgForm };
        pnlProgress.Controls.Add(_prgBar);
        mainScroll.Controls.Add(pnlProgress);

        // 6. Status Label
        _lblStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "Sẵn sàng.",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textNormal,
            TextAlign = ContentAlignment.MiddleLeft
        };
        mainScroll.Controls.Add(_lblStatus);

        // Reverse docking order for WinForms top docking
        mainScroll.Controls.SetChildIndex(_lblStatus, 0);
        mainScroll.Controls.SetChildIndex(pnlProgress, 1);
        mainScroll.Controls.SetChildIndex(pnlActions, 2);
        mainScroll.Controls.SetChildIndex(pnlCustomFolders, 3);
        mainScroll.Controls.SetChildIndex(pnlCheckboxes, 4);
        mainScroll.Controls.SetChildIndex(pnlUserRow, 5);
    }

    private Panel BuildUserSelectionBar()
    {
        var pnl = new Panel
        {
            Height = 40,
            Padding = new Padding(2, 4, 2, 8),
            BackColor = _bgForm
        };

        var lblPrompt = new Label
        {
            Text = "Dọn rác cho user:",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = _textNormal,
            Location = new Point(4, 8)
        };
        pnl.Controls.Add(lblPrompt);

        _cboUser = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(135, 5),
            Width = 190,
            BackColor = ThemeHelper.InputBg,
            ForeColor = _textHeading
        };

        var users = _maintenanceService.GetLocalUserProfiles();
        foreach (var u in users) _cboUser.Items.Add(u);
        if (_cboUser.Items.Contains(_targetUser))
            _cboUser.SelectedItem = _targetUser;
        else if (_cboUser.Items.Count > 0)
            _cboUser.SelectedIndex = 0;

        _cboUser.SelectedIndexChanged += (_, _) =>
        {
            _targetUser = _cboUser.SelectedItem?.ToString() ?? Environment.UserName;
            UpdateUserTempLabel();
        };
        pnl.Controls.Add(_cboUser);

        _lblAutoDetect = new Label
        {
            Text = $"Tự động dò: {Environment.UserName}",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textMuted,
            Location = new Point(340, 8)
        };
        pnl.Controls.Add(_lblAutoDetect);

        return pnl;
    }

    private Panel BuildCheckboxesCard()
    {
        var card = new Panel
        {
            Height = 445,
            Padding = new Padding(8, 6, 8, 8),
            BackColor = _cardBg,
            Tag = "card",
            Margin = new Padding(0, 0, 0, 10)
        };

        var lblTitle = new Label
        {
            Text = "Chọn nơi cần dọn",
            Font = new Font("Segoe UI", 11.25f, FontStyle.Bold | FontStyle.Italic),
            ForeColor = _textHeading,
            AutoSize = true,
            Location = new Point(8, 4)
        };
        card.Controls.Add(lblTitle);

        // 2-column TableLayoutPanel
        var tlp = new TableLayoutPanel
        {
            Location = new Point(6, 30),
            Size = new Size(card.Width - 16, 405),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 2,
            RowCount = 15,
            BackColor = _cardBg
        };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        for (int i = 0; i < 15; i++)
        {
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        }

        // Left Column Checkboxes
        _chkUserTemp = CreateCheck($"Temp người dùng (C:\\Users\\{_targetUser}\\AppData\\Local\\Temp)", true);
        _chkWinTemp = CreateCheck("Windows Temp", true);
        _chkWinUpdate = CreateCheck("Windows Update cache", true);
        _chkPrefetch = CreateCheck("Prefetch", false);
        _chkExplorerCache = CreateCheck("Explorer cache (thumb/icon cache)", true);
        _chkEdgeCache = CreateCheck("Microsoft Edge cache", false);
        _chkChromeCache = CreateCheck("Google Chrome cache", false);
        _chkFirefoxCache = CreateCheck("Mozilla Firefox cache", false);
        _chkEdgeHistory = CreateCheck("Lịch sử duyệt web Edge", false);
        _chkFirefoxHistory = CreateCheck("Lịch sử duyệt web Firefox (kèm cả Bookmark, không tách được)", false);
        _chkChromeLogout = CreateCheck("Đăng xuất tài khoản đã đăng nhập trong Chrome (Cookies + mật khẩu đã lưu - vd: Facebook, Gmail...)", false);
        _chkEdgeLogout = CreateCheck("Đăng xuất tài khoản đã đăng nhập trong Edge (Cookies + mật khẩu đã lưu - tool sẽ tự đóng Edge trước khi dọn)", false);
        _chkFirefoxLogout = CreateCheck("Đăng xuất tài khoản đã đăng nhập trong Firefox (Cookies + mật khẩu đã lưu - tool sẽ tự đóng Firefox trước khi dọn)", false);
        _chkZaloReset = CreateCheck("Reset Zalo PC (Xóa toàn bộ ZaloData - Dữ liệu / Lịch sử tin nhắn)", false);
        _chkCredentialManager = CreateCheck("Xóa Windows Credential Manager (mật khẩu mạng/RDP/tài khoản Microsoft đã lưu trong Windows)", false);

        // Right Column Checkboxes
        _chkINetCache = CreateCheck("INetCache (Internet cache cũ)", true);
        _chkWebCache = CreateCheck("WebCache", false);
        _chkCrashDumps = CreateCheck("CrashDumps", true);
        _chkWer = CreateCheck("Windows Error Reporting (WER)", true);
        _chkDirectXCache = CreateCheck("DirectX Shader Cache (D3DSCache)", true);
        _chkRecycleBin = CreateCheck("Dọn Thùng rác (Recycle Bin)", true);
        _chkQuickAccess = CreateCheck("Lịch sử Quick Access / Recent files", false);
        _chkChromeHistory = CreateCheck("Lịch sử duyệt web Chrome", false);
        _chkTeamsReset = CreateCheck("Reset Microsoft Teams (xóa dữ liệu đăng nhập & cache Teams)", false);

        // Add to TableLayoutPanel Grid
        // Row 0
        tlp.Controls.Add(_chkUserTemp, 0, 0);
        // Row 1
        tlp.Controls.Add(_chkWinTemp, 0, 1);
        tlp.Controls.Add(_chkINetCache, 1, 1);
        // Row 2
        tlp.Controls.Add(_chkWinUpdate, 0, 2);
        tlp.Controls.Add(_chkWebCache, 1, 2);
        // Row 3
        tlp.Controls.Add(_chkPrefetch, 0, 3);
        tlp.Controls.Add(_chkCrashDumps, 1, 3);
        // Row 4
        tlp.Controls.Add(_chkExplorerCache, 0, 4);
        tlp.Controls.Add(_chkWer, 1, 4);
        // Row 5
        tlp.Controls.Add(_chkEdgeCache, 0, 5);
        tlp.Controls.Add(_chkDirectXCache, 1, 5);
        // Row 6
        tlp.Controls.Add(_chkChromeCache, 0, 6);
        tlp.Controls.Add(_chkRecycleBin, 1, 6);
        // Row 7
        tlp.Controls.Add(_chkFirefoxCache, 0, 7);
        tlp.Controls.Add(_chkQuickAccess, 1, 7);
        // Row 8
        tlp.Controls.Add(_chkEdgeHistory, 0, 8);
        tlp.Controls.Add(_chkChromeHistory, 1, 8);
        // Row 9
        tlp.Controls.Add(_chkFirefoxHistory, 0, 9);
        // Row 10
        tlp.Controls.Add(_chkChromeLogout, 0, 10);
        // Row 11
        tlp.Controls.Add(_chkEdgeLogout, 0, 11);
        // Row 12
        tlp.Controls.Add(_chkFirefoxLogout, 0, 12);
        // Row 13
        tlp.Controls.Add(_chkZaloReset, 0, 13);
        tlp.Controls.Add(_chkTeamsReset, 1, 13);
        // Row 14
        tlp.Controls.Add(_chkCredentialManager, 0, 14);

        card.Controls.Add(tlp);
        return card;
    }

    private Panel BuildCustomFoldersCard()
    {
        var card = new Panel
        {
            Height = 115,
            Padding = new Padding(8, 4, 8, 6),
            BackColor = _cardBg,
            Tag = "card",
            Margin = new Padding(0, 8, 0, 8)
        };

        var lblTitle = new Label
        {
            Text = "Thư mục bổ sung cần dọn dẹp (Ứng dụng nội bộ, CAD, phần mềm khác...)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            AutoSize = true,
            Location = new Point(6, 4)
        };
        card.Controls.Add(lblTitle);

        _lstCustomFolders = new ListBox
        {
            Location = new Point(6, 26),
            Size = new Size(card.Width - 180, 78),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = ThemeHelper.InputBg,
            ForeColor = _textNormal,
            Font = new Font("Segoe UI", 9.25f),
            IntegralHeight = false
        };
        card.Controls.Add(_lstCustomFolders);

        _btnAddFolder = new Button
        {
            Text = "Thêm thư mục...",
            Size = new Size(155, 36),
            Location = new Point(card.Width - 165, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _btnAddFolder.FlatAppearance.BorderColor = _btnBorder;
        _btnAddFolder.Click += BtnAddFolder_Click;
        card.Controls.Add(_btnAddFolder);

        _btnRemoveFolder = new Button
        {
            Text = "━ Bỏ mục đã chọn",
            Size = new Size(155, 36),
            Location = new Point(card.Width - 165, 68),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _btnRemoveFolder.FlatAppearance.BorderColor = _btnBorder;
        _btnRemoveFolder.Click += (_, _) =>
        {
            if (_lstCustomFolders.SelectedIndex >= 0)
                _lstCustomFolders.Items.RemoveAt(_lstCustomFolders.SelectedIndex);
        };
        card.Controls.Add(_btnRemoveFolder);

        return card;
    }

    private Panel BuildActionsBar()
    {
        var pnl = new Panel
        {
            Height = 52,
            Padding = new Padding(2, 6, 2, 6),
            BackColor = _bgForm
        };

        _btnSelectAll = CreateButton("Chọn tất cả", new Point(0, 8), new Size(115, 36), false);
        _btnSelectAll.Click += (_, _) => SetAllCheckboxes(true);
        pnl.Controls.Add(_btnSelectAll);

        _btnDeselectAll = CreateButton("Bỏ chọn tất cả", new Point(122, 8), new Size(125, 36), false);
        _btnDeselectAll.Click += (_, _) => SetAllCheckboxes(false);
        pnl.Controls.Add(_btnDeselectAll);

        _btnStartClean = CreateButton("Bắt đầu dọn dẹp", new Point(255, 8), new Size(140, 36), true);
        _btnStartClean.Click += async (_, _) => await StartCleanAsync();
        pnl.Controls.Add(_btnStartClean);

        _btnEmailSettings = CreateButton("Cài đặt Email & Tài khoản", new Point(403, 8), new Size(245, 36), false);
        _btnEmailSettings.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:emailandaccounts") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở Cài đặt Windows: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        pnl.Controls.Add(_btnEmailSettings);

        _btnClose = CreateButton("Đóng", new Point(pnl.Width - 95, 8), new Size(90, 36), false);
        _btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnClose.Click += (_, _) => Close();
        pnl.Controls.Add(_btnClose);

        return pnl;
    }

    private CheckBox CreateCheck(string text, bool isChecked)
    {
        var chk = new CheckBox
        {
            Text = text,
            Checked = isChecked,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textNormal,
            AutoEllipsis = true,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 2, 4, 2)
        };
        return chk;
    }

    private Button CreateButton(string text, Point loc, Size size, bool isPrimary)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, isPrimary ? FontStyle.Bold : FontStyle.Regular),
            BackColor = isPrimary ? _primaryBlue : _btnBg,
            ForeColor = isPrimary ? Color.White : _textHeading,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = isPrimary ? _primaryBlue : _btnBorder;
        return btn;
    }

    private void UpdateUserTempLabel()
    {
        if (_chkUserTemp != null)
        {
            _chkUserTemp.Text = $"Temp người dùng (C:\\Users\\{_targetUser}\\AppData\\Local\\Temp)";
        }
    }

    private void SetAllCheckboxes(bool state)
    {
        var checks = new[]
        {
            _chkUserTemp, _chkWinTemp, _chkWinUpdate, _chkPrefetch, _chkExplorerCache,
            _chkEdgeCache, _chkChromeCache, _chkFirefoxCache, _chkEdgeHistory, _chkFirefoxHistory,
            _chkChromeLogout, _chkEdgeLogout, _chkFirefoxLogout, _chkZaloReset, _chkCredentialManager,
            _chkINetCache, _chkWebCache, _chkCrashDumps, _chkWer, _chkDirectXCache,
            _chkRecycleBin, _chkQuickAccess, _chkChromeHistory, _chkTeamsReset
        };

        foreach (var c in checks)
        {
            if (c != null) c.Checked = state;
        }
    }

    private void BtnAddFolder_Click(object? sender, EventArgs e)
    {
        using var fbd = new FolderBrowserDialog
        {
            Description = "Chọn thư mục bất kỳ cần dọn rác (ví dụ cache của CAD, Adobe, phần mềm nội bộ...):"
        };
        if (fbd.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            if (!_lstCustomFolders.Items.Contains(fbd.SelectedPath))
            {
                _lstCustomFolders.Items.Add(fbd.SelectedPath);
            }
        }
    }

    private async Task StartCleanAsync()
    {
        if (_chkZaloReset.Checked)
        {
            var res = MessageBox.Show(
                "Bạn đã chọn Reset Zalo PC. Thao tác này sẽ xóa toàn bộ ZaloData (hình ảnh, file gửi và lịch sử tin nhắn lưu cục bộ trên máy)!\n\nBạn có chắc chắn muốn xóa ZaloData không?",
                "Cảnh báo xóa dữ liệu Zalo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res != DialogResult.Yes)
            {
                _chkZaloReset.Checked = false;
            }
        }

        var options = new CleanerOptionsModel
        {
            TargetUser = _targetUser,
            UserTemp = _chkUserTemp.Checked,
            WindowsTemp = _chkWinTemp.Checked,
            WindowsUpdateCache = _chkWinUpdate.Checked,
            Prefetch = _chkPrefetch.Checked,
            ExplorerCache = _chkExplorerCache.Checked,
            EdgeCache = _chkEdgeCache.Checked,
            ChromeCache = _chkChromeCache.Checked,
            FirefoxCache = _chkFirefoxCache.Checked,
            EdgeHistory = _chkEdgeHistory.Checked,
            FirefoxHistory = _chkFirefoxHistory.Checked,
            ChromeLogout = _chkChromeLogout.Checked,
            EdgeLogout = _chkEdgeLogout.Checked,
            FirefoxLogout = _chkFirefoxLogout.Checked,
            ResetZalo = _chkZaloReset.Checked,
            ClearCredentials = _chkCredentialManager.Checked,

            INetCache = _chkINetCache.Checked,
            WebCache = _chkWebCache.Checked,
            CrashDumps = _chkCrashDumps.Checked,
            Wer = _chkWer.Checked,
            DirectXCache = _chkDirectXCache.Checked,
            RecycleBin = _chkRecycleBin.Checked,
            QuickAccess = _chkQuickAccess.Checked,
            ChromeHistory = _chkChromeHistory.Checked,
            ResetTeams = _chkTeamsReset.Checked
        };

        foreach (var item in _lstCustomFolders.Items)
        {
            if (item != null) options.CustomFolders.Add(item.ToString()!);
        }

        SetControlsEnabled(false);
        _prgBar.Value = 0;
        _lblStatus.Text = "Đang bắt đầu dọn dẹp...";

        try
        {
            var result = await _maintenanceService.ExecuteDeepCleanAsync(
                options,
                (statusText, progressPercent) =>
                {
                    if (IsHandleCreated && !IsDisposed)
                    {
                        Invoke(() =>
                        {
                            _lblStatus.Text = statusText;
                            _prgBar.Value = Math.Clamp(progressPercent, 0, 100);
                        });
                    }
                });

            _lblStatus.Text = $"[Hoàn tất] Đã giải phóng: {result.MegabytesFreed:N1} MB ({result.FilesDeleted:N0} tệp tin đã xóa).";
            _prgBar.Value = 100;

            MessageBox.Show(
                $"Đã hoàn tất tiến trình dọn dẹp hệ thống!\n\n• Dung lượng giải phóng: {result.MegabytesFreed:N1} MB\n• Số tệp tin đã xử lý: {result.FilesDeleted:N0}\n• Người dùng: {_targetUser}",
                "Dọn rác hoàn tất",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"[Lỗi] Dọn rác thất bại: {ex.Message}";
            MessageBox.Show($"Xảy ra lỗi trong quá trình dọn dẹp: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        _btnStartClean.Enabled = enabled;
        _btnSelectAll.Enabled = enabled;
        _btnDeselectAll.Enabled = enabled;
        _btnAddFolder.Enabled = enabled;
        _btnRemoveFolder.Enabled = enabled;
        _cboUser.Enabled = enabled;
    }
}
