using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Ernastie.Models;
using Ernastie.Services;

namespace Ernastie.Forms;

public class WifiBackupForm : Form
{
    // Dynamic Design System Tokens from ThemeHelper
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _cardBg => ThemeHelper.CardBg;
    private Color _borderColor => ThemeHelper.BorderColor;
    private Color _textHeading => ThemeHelper.TextHeading;
    private Color _textLabel => ThemeHelper.TextMuted;
    private Color _textValue => ThemeHelper.TextHeading;
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;
    private Color _primaryHover => ThemeHelper.PrimaryHover;
    private Color _successGreen => ThemeHelper.SuccessGreen;
    private Color _successHover => Color.FromArgb(21, 128, 61);
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnHover => ThemeHelper.BtnHover;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _rowAltBg => ThemeHelper.IsDark ? Color.FromArgb(20, 28, 42) : Color.FromArgb(248, 250, 252);

    private readonly BackupService _backupService = new();
    private List<WifiProfileModel> _allProfiles = new();

    private TextBox _txtBackupPath = null!;
    private TextBox _txtSearch = null!;
    private CheckBox _chkShowPasswords = null!;
    private Label _lblProfileCount = null!;
    private Label _lblStatus = null!;
    private DataGridView _dgvProfiles = null!;
    private ProgressBar _progressBar = null!;

    public WifiBackupForm()
    {
        InitializeComponents();
        LoadWifiProfiles();
    }

    private void InitializeComponents()
    {
        Text = "📡 Quản Lý, Sao Lưu & Khôi Phục Hồ Sơ WiFi";
        Size = new Size(880, 640);
        MinimumSize = new Size(800, 560);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9f);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = _bgForm
        };

        // 1. Header Card
        var pnlHeader = BuildHeaderCard();
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Margin = new Padding(0, 0, 0, 10);
        mainPanel.Controls.Add(pnlHeader);

        // 2. Path & Control Bar Card
        var pnlControls = BuildControlsCard();
        pnlControls.Dock = DockStyle.Top;
        pnlControls.Margin = new Padding(0, 0, 0, 10);
        mainPanel.Controls.Add(pnlControls);

        // 3. Grid Card (Fill)
        var pnlGridCard = BuildGridCard();
        pnlGridCard.Dock = DockStyle.Fill;
        mainPanel.Controls.Add(pnlGridCard);

        // 4. Action Buttons Bar
        var pnlActions = BuildActionsBar();
        pnlActions.Dock = DockStyle.Bottom;
        pnlActions.Height = 52;
        mainPanel.Controls.Add(pnlActions);

        // 5. Status Strip
        var statusStrip = BuildStatusStrip();
        statusStrip.Dock = DockStyle.Bottom;
        mainPanel.Controls.Add(statusStrip);

        Controls.Add(mainPanel);

        // Ordering for dock logic
        pnlHeader.BringToFront();
        pnlControls.BringToFront();
        pnlGridCard.BringToFront();
        ThemeHelper.ApplyTheme(this);
    }

    private Panel BuildHeaderCard()
    {
        var card = new Panel
        {
            Height = 68,
            BackColor = _cardBg,
            Padding = new Padding(14, 10, 14, 10)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var lblIcon = new Label
        {
            Text = "📡",
            Font = new Font("Segoe UI", 22f),
            Size = new Size(46, 46),
            Location = new Point(14, 10),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblTitle = new Label
        {
            Text = "Trình Quản Lý & Sao Lưu / Khôi Phục Hồ Sơ WiFi",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(66, 10),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Trích xuất mật khẩu rõ (clear-text), xuất hồ sơ XML để khôi phục nhanh sau khi cài lại Windows hoặc chuyển máy.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _textLabel,
            Location = new Point(68, 36),
            AutoSize = true
        };

        _lblProfileCount = new Label
        {
            Text = "Đang quét...",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _primaryBlue,
            BackColor = Color.FromArgb(239, 246, 255),
            Padding = new Padding(8, 4, 8, 4),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _lblProfileCount.Location = new Point(card.Width - 170, 18);
        card.Resize += (_, _) =>
        {
            _lblProfileCount.Location = new Point(card.Width - _lblProfileCount.Width - 16, 18);
        };

        card.Controls.Add(lblIcon);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblSub);
        card.Controls.Add(_lblProfileCount);

        return card;
    }

    private Panel BuildControlsCard()
    {
        var card = new Panel
        {
            Height = 84,
            BackColor = _cardBg,
            Padding = new Padding(12, 10, 12, 10)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // Row 1: Thư mục sao lưu
        var lblPath = new Label
        {
            Text = "📁 Thư mục lưu trữ:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(12, 12),
            Size = new Size(125, 24),
            TextAlign = ContentAlignment.MiddleLeft
        };

        string defaultDrive = Directory.Exists("D:\\") ? @"D:\Backup_Wifi" : @"C:\Backup_Wifi";
        _txtBackupPath = new TextBox
        {
            Text = defaultDrive,
            Font = new Font("Consolas", 9f),
            Location = new Point(140, 13),
            Width = 470,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var btnBrowse = new Button
        {
            Text = "Chọn thư mục...",
            Location = new Point(618, 11),
            Size = new Size(110, 27),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnBrowse.FlatAppearance.BorderColor = _btnBorder;
        btnBrowse.FlatAppearance.MouseOverBackColor = _btnHover;
        btnBrowse.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục lưu trữ hồ sơ sao lưu WiFi",
                SelectedPath = _txtBackupPath.Text
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _txtBackupPath.Text = fbd.SelectedPath;
            }
        };

        var btnOpenDir = new Button
        {
            Text = "📂 Mở",
            Location = new Point(734, 11),
            Size = new Size(60, 27),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnOpenDir.FlatAppearance.BorderColor = _btnBorder;
        btnOpenDir.FlatAppearance.MouseOverBackColor = _btnHover;
        btnOpenDir.Click += (_, _) =>
        {
            try
            {
                if (!Directory.Exists(_txtBackupPath.Text))
                {
                    Directory.CreateDirectory(_txtBackupPath.Text);
                }
                Process.Start("explorer.exe", _txtBackupPath.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        // Row 2: Search & Filter
        var lblSearch = new Label
        {
            Text = "🔍 Tìm kiếm SSID:",
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textLabel,
            Location = new Point(12, 48),
            Size = new Size(125, 24),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _txtSearch = new TextBox
        {
            PlaceholderText = "Nhập tên WiFi để lọc...",
            Font = new Font("Segoe UI", 9f),
            Location = new Point(140, 48),
            Width = 320,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _txtSearch.TextChanged += (_, _) => FilterProfiles();

        _chkShowPasswords = new CheckBox
        {
            Text = "👁️ Hiện mật khẩu",
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading,
            Checked = true,
            Location = new Point(475, 48),
            Size = new Size(130, 24),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _chkShowPasswords.CheckedChanged += (_, _) => FilterProfiles();

        var btnRefresh = new Button
        {
            Text = "🔄 Quét lại",
            Location = new Point(618, 46),
            Size = new Size(110, 27),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnRefresh.FlatAppearance.BorderColor = _btnBorder;
        btnRefresh.FlatAppearance.MouseOverBackColor = _btnHover;
        btnRefresh.Click += (_, _) => LoadWifiProfiles();

        card.Controls.Add(lblPath);
        card.Controls.Add(_txtBackupPath);
        card.Controls.Add(btnBrowse);
        card.Controls.Add(btnOpenDir);
        card.Controls.Add(lblSearch);
        card.Controls.Add(_txtSearch);
        card.Controls.Add(_chkShowPasswords);
        card.Controls.Add(btnRefresh);

        return card;
    }

    private Panel BuildGridCard()
    {
        var card = new Panel
        {
            BackColor = _cardBg,
            Padding = new Padding(1)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        _dgvProfiles = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = _cardBg,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            EnableHeadersVisualStyles = false,
            GridColor = _borderColor,
            Font = new Font("Segoe UI", 9.25f),
            RowTemplate = { Height = 34 }
        };

        // Header Style
        _dgvProfiles.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        _dgvProfiles.ColumnHeadersDefaultCellStyle.ForeColor = _textHeading;
        _dgvProfiles.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _dgvProfiles.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _dgvProfiles.ColumnHeadersHeight = 36;
        _dgvProfiles.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        // Default Cell Style
        _dgvProfiles.DefaultCellStyle.BackColor = Color.White;
        _dgvProfiles.DefaultCellStyle.ForeColor = _textValue;
        _dgvProfiles.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
        _dgvProfiles.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
        _dgvProfiles.AlternatingRowsDefaultCellStyle.BackColor = _rowAltBg;

        // Columns definition
        var colIdx = new DataGridViewTextBoxColumn
        {
            Name = "ColIdx",
            HeaderText = "#",
            Width = 45,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };

        var colName = new DataGridViewTextBoxColumn
        {
            Name = "ColName",
            HeaderText = "Tên Mạng WiFi (SSID)",
            Width = 240,
            DefaultCellStyle = { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
        };

        var colAuth = new DataGridViewTextBoxColumn
        {
            Name = "ColAuth",
            HeaderText = "Kiểu Xác Thực",
            Width = 160
        };

        var colPass = new DataGridViewTextBoxColumn
        {
            Name = "ColPass",
            HeaderText = "Mật Khẩu (Password)",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            DefaultCellStyle = { Font = new Font("Consolas", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 64, 175) }
        };

        var colCopyBtn = new DataGridViewButtonColumn
        {
            Name = "ColCopy",
            HeaderText = "Thao tác",
            Text = "📋 Copy MK",
            UseColumnTextForButtonValue = true,
            Width = 100,
            FlatStyle = FlatStyle.Flat
        };

        _dgvProfiles.Columns.AddRange(colIdx, colName, colAuth, colPass, colCopyBtn);

        // Actions on cell click / double click
        _dgvProfiles.CellContentClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == _dgvProfiles.Columns["ColCopy"].Index)
            {
                CopyRowPassword(e.RowIndex);
            }
        };

        _dgvProfiles.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0)
            {
                CopyRowPassword(e.RowIndex);
            }
        };

        // Context Menu
        var ctx = new ContextMenuStrip();
        var menuCopyPass = ctx.Items.Add("📋 Sao chép mật khẩu");
        menuCopyPass.Click += (_, _) =>
        {
            if (_dgvProfiles.CurrentRow != null)
                CopyRowPassword(_dgvProfiles.CurrentRow.Index);
        };

        var menuCopySsid = ctx.Items.Add("📝 Sao chép Tên WiFi (SSID)");
        menuCopySsid.Click += (_, _) =>
        {
            if (_dgvProfiles.CurrentRow != null)
            {
                var ssid = _dgvProfiles.CurrentRow.Cells["ColName"].Value?.ToString() ?? "";
                Clipboard.SetText(ssid);
                SetStatus($"Đã sao chép SSID: \"{ssid}\"");
            }
        };

        var menuCopyBoth = ctx.Items.Add("📄 Sao chép cả Tên & Mật khẩu");
        menuCopyBoth.Click += (_, _) =>
        {
            if (_dgvProfiles.CurrentRow != null)
            {
                var ssid = _dgvProfiles.CurrentRow.Cells["ColName"].Value?.ToString() ?? "";
                var pass = _dgvProfiles.CurrentRow.Cells["ColPass"].Tag?.ToString() ?? "";
                Clipboard.SetText($"SSID: {ssid}\nPassword: {pass}");
                SetStatus($"Đã sao chép SSID & Mật khẩu của \"{ssid}\"");
            }
        };

        ctx.Items.Add(new ToolStripSeparator());

        var menuExportOne = ctx.Items.Add("💾 Xuất file XML riêng cho mạng này");
        menuExportOne.Click += (_, _) =>
        {
            if (_dgvProfiles.CurrentRow != null)
            {
                var ssid = _dgvProfiles.CurrentRow.Cells["ColName"].Value?.ToString() ?? "";
                ExportSingleProfile(ssid);
            }
        };

        _dgvProfiles.ContextMenuStrip = ctx;
        card.Controls.Add(_dgvProfiles);

        return card;
    }

    private Panel BuildActionsBar()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        var btnBackupAll = new Button
        {
            Text = "💾 Sao Lưu Toàn Bộ WiFi (XML + Text)",
            Location = new Point(0, 10),
            Size = new Size(250, 38),
            BackColor = _primaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnBackupAll.FlatAppearance.BorderColor = Color.FromArgb(29, 78, 216);
        btnBackupAll.FlatAppearance.MouseOverBackColor = _primaryHover;
        btnBackupAll.Click += async (_, _) => await BackupAllProfilesAsync();

        var btnRestore = new Button
        {
            Text = "📥 Khôi Phục WiFi Từ File XML",
            Location = new Point(258, 10),
            Size = new Size(220, 38),
            BackColor = _successGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRestore.FlatAppearance.BorderColor = Color.FromArgb(21, 128, 61);
        btnRestore.FlatAppearance.MouseOverBackColor = _successHover;
        btnRestore.Click += async (_, _) => await RestoreProfilesDialogAsync();

        var btnCopyAll = new Button
        {
            Text = "📋 Sao Chép Toàn Bộ Mật Khẩu",
            Location = new Point(486, 10),
            Size = new Size(210, 38),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopyAll.FlatAppearance.BorderColor = _btnBorder;
        btnCopyAll.FlatAppearance.MouseOverBackColor = _btnHover;
        btnCopyAll.Click += (_, _) => CopyAllPasswordsToClipboard();

        var btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(90, 38),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnClose.Location = new Point(pnl.Width - 90, 10);
        pnl.Resize += (_, _) =>
        {
            btnClose.Location = new Point(pnl.Width - 90, 10);
        };
        btnClose.FlatAppearance.BorderColor = _btnBorder;
        btnClose.FlatAppearance.MouseOverBackColor = _btnHover;
        btnClose.Click += (_, _) => Close();

        pnl.Controls.Add(btnBackupAll);
        pnl.Controls.Add(btnRestore);
        pnl.Controls.Add(btnCopyAll);
        pnl.Controls.Add(btnClose);

        return pnl;
    }

    private Panel BuildStatusStrip()
    {
        var pnl = new Panel
        {
            Height = 28,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(8, 4, 8, 4)
        };

        _lblStatus = new Label
        {
            Text = "💡 Mẹo: Nhấn đúp chuột vào bất kỳ hàng nào để sao chép nhanh mật khẩu.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _textLabel,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Right,
            Width = 140,
            Visible = false,
            Style = ProgressBarStyle.Marquee
        };

        pnl.Controls.Add(_lblStatus);
        pnl.Controls.Add(_progressBar);

        return pnl;
    }

    private void SetStatus(string message, bool isBusy = false)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(message, isBusy));
            return;
        }

        _lblStatus.Text = message;
        _progressBar.Visible = isBusy;
    }

    private async void LoadWifiProfiles()
    {
        SetStatus("Đang quét danh sách hồ sơ WiFi trong hệ thống...", true);
        _lblProfileCount.Text = "Đang quét...";

        await Task.Run(() =>
        {
            try
            {
                _allProfiles = _backupService.GetWifiProfiles();
            }
            catch
            {
                _allProfiles = new List<WifiProfileModel>();
            }
        });

        FilterProfiles();
        SetStatus($"Đã tải xong {_allProfiles.Count} hồ sơ WiFi. Nhấn đúp để copy mật khẩu.", false);
        _lblProfileCount.Text = $"{_allProfiles.Count} mạng đã lưu";
    }

    private void FilterProfiles()
    {
        _dgvProfiles.Rows.Clear();
        var search = _txtSearch.Text.Trim();
        bool showPass = _chkShowPasswords.Checked;

        var filtered = _allProfiles
            .Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        int idx = 1;
        foreach (var p in filtered)
        {
            string displayPass;
            if (p.Password.StartsWith("(") && p.Password.EndsWith(")"))
            {
                displayPass = p.Password;
            }
            else
            {
                displayPass = showPass ? p.Password : new string('•', Math.Min(p.Password.Length, 12));
            }

            int rowIdx = _dgvProfiles.Rows.Add(idx++, p.Name, p.Authentication, displayPass);
            // Store raw clear password in Tag for copying even if masked
            _dgvProfiles.Rows[rowIdx].Cells["ColPass"].Tag = p.Password;
        }
    }

    private void CopyRowPassword(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _dgvProfiles.Rows.Count) return;

        var row = _dgvProfiles.Rows[rowIndex];
        var ssid = row.Cells["ColName"].Value?.ToString() ?? "";
        var pass = row.Cells["ColPass"].Tag?.ToString() ?? row.Cells["ColPass"].Value?.ToString() ?? "";

        if (!string.IsNullOrEmpty(pass))
        {
            Clipboard.SetText(pass);
            SetStatus($"✅ Đã sao chép mật khẩu của WiFi \"{ssid}\" vào Clipboard!");
        }
    }

    private void CopyAllPasswordsToClipboard()
    {
        if (_allProfiles.Count == 0)
        {
            MessageBox.Show("Không có hồ sơ WiFi nào để sao chép!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== DANH SÁCH MẬT KHẨU WIFI ===");
        sb.AppendLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Thiết bị : {Environment.MachineName}");
        sb.AppendLine("--------------------------------");
        int i = 1;
        foreach (var p in _allProfiles)
        {
            sb.AppendLine($"{i++}. {p.Name} | {p.Authentication} | Mật khẩu: {p.Password}");
        }

        Clipboard.SetText(sb.ToString());
        SetStatus($"✅ Đã sao chép {_allProfiles.Count} mật khẩu WiFi vào Clipboard!");
        MessageBox.Show($"Đã sao chép danh sách {_allProfiles.Count} hồ sơ WiFi kèm mật khẩu vào Clipboard thành công!", "Đã Sao Chép", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task BackupAllProfilesAsync()
    {
        var targetFolder = _txtBackupPath.Text.Trim();
        if (string.IsNullOrEmpty(targetFolder))
        {
            MessageBox.Show("Vui lòng chọn hoặc nhập đường dẫn thư mục sao lưu!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetStatus($"Đang xuất cấu hình WiFi sang: {targetFolder}...", true);

        int exportedCount = 0;
        await Task.Run(() =>
        {
            try
            {
                exportedCount = _backupService.ExportWifiProfiles(targetFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sao lưu WiFi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        SetStatus($"✅ Hoàn tất sao lưu! Đã xuất {exportedCount} file cấu hình WiFi vào {targetFolder}", false);

        var res = MessageBox.Show(
            $"Đã sao lưu thành công {exportedCount} hồ sơ WiFi vào thư mục:\n{targetFolder}\n\n(Bao gồm các file .xml cấu hình và file DanhSach_MatKhau_WiFi.txt).\n\nBạn có muốn mở thư mục sao lưu ngay bây giờ không?",
            "Sao Lưu Thành Công",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (res == DialogResult.Yes)
        {
            try
            {
                Process.Start("explorer.exe", targetFolder);
            }
            catch { }
        }
    }

    private void ExportSingleProfile(string profileName)
    {
        var targetFolder = _txtBackupPath.Text.Trim();
        if (string.IsNullOrEmpty(targetFolder))
        {
            MessageBox.Show("Vui lòng nhập đường dẫn thư mục sao lưu!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Directory.CreateDirectory(targetFolder);
            var psi = new ProcessStartInfo("netsh.exe", $"wlan export profile name=\"{profileName}\" folder=\"{targetFolder.TrimEnd('\\', '/')}\" key=clear")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit();

            SetStatus($"✅ Đã xuất riêng cấu hình WiFi \"{profileName}\" vào {targetFolder}");
            MessageBox.Show($"Đã xuất cấu hình mạng \"{profileName}\" thành công vào thư mục:\n{targetFolder}", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xuất hồ sơ \"{profileName}\": {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RestoreProfilesDialogAsync()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Chọn tệp XML hồ sơ WiFi cần khôi phục (hoặc chọn nhiều tệp)",
            Filter = "Hồ sơ WiFi XML (*.xml)|*.xml|Tất cả tệp (*.*)|*.*",
            Multiselect = true,
            InitialDirectory = Directory.Exists(_txtBackupPath.Text) ? _txtBackupPath.Text : "C:\\"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK || ofd.FileNames.Length == 0)
        {
            return;
        }

        SetStatus($"Đang nạp {ofd.FileNames.Length} hồ sơ WiFi vào Windows...", true);

        int count = 0;
        await Task.Run(() =>
        {
            foreach (var file in ofd.FileNames)
            {
                count += _backupService.RestoreWifiProfiles(file);
            }
        });

        LoadWifiProfiles();
        SetStatus($"✅ Đã khôi phục thành công {count} hồ sơ WiFi!", false);

        MessageBox.Show($"Đã khôi phục thành công {count} hồ sơ WiFi vào Windows!", "Khôi Phục Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
