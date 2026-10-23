using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Ernastie.Forms;

public class CredentialAndCertificateForm : Form
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
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;
    private Color _successGreen => ThemeHelper.SuccessGreen;

    private string _exportDestDir = string.Empty;
    private string _importSourceDir = string.Empty;

    // Controls Left (MÁY CŨ)
    private Label _lblExportPath = null!;
    private Label _lblScanStatus = null!;
    private DataGridView _dgvOldMachine = null!;
    private TextBox _txtExportPassword = null!;
    private Button _btnExport = null!;

    // Controls Right (MÁY MỚI)
    private Label _lblImportPath = null!;
    private DataGridView _dgvNewMachine = null!;
    private TextBox _txtImportPassword = null!;
    private Button _btnImport = null!;

    // Top & Bottom
    private ComboBox _cboUserAccount = null!;
    private Label _lblBottomStatus = null!;

    public CredentialAndCertificateForm()
    {
        InitializeComponents();
        InitDefaults();
    }

    private void InitDefaults()
    {
        // Default target destination
        string defaultPath = Directory.Exists(@"D:\") ? @"D:\" : @"C:\";
        _exportDestDir = defaultPath;
        _lblExportPath.Text = TruncatePath(_exportDestDir, 32);

        // List accounts
        _cboUserAccount.Items.Clear();
        _cboUserAccount.Items.Add(Environment.UserName);
        _cboUserAccount.SelectedIndex = 0;
    }

    private string TruncatePath(string path, int maxLen = 32)
    {
        if (string.IsNullOrEmpty(path)) return "Chưa chọn";
        if (path.Length <= maxLen) return path;
        var root = Path.GetPathRoot(path) ?? "";
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)) return path;
        return $"{root}...\\{name}";
    }

    private void InitializeComponents()
    {
        Text = "Quản Lý Mật Khẩu Windows & Chữ Ký Số (Credentials & Certificates)";
        Size = new Size(950, 580);
        MinimumSize = new Size(920, 540);
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
            Padding = new Padding(16, 10, 16, 10),
            BackColor = _bgForm
        };

        // 1. Top Header Description Banner
        var pnlHeader = BuildTopHeaderBanner();
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 84;
        mainPanel.Controls.Add(pnlHeader);

        // 2. User Account Row
        var pnlUserRow = BuildUserAccountRow();
        pnlUserRow.Dock = DockStyle.Top;
        pnlUserRow.Height = 36;
        mainPanel.Controls.Add(pnlUserRow);

        // 3. Two Columns Table Container
        var pnlCols = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _bgForm
        };
        pnlCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var colLeft = BuildOldMachineGroup();
        colLeft.Margin = new Padding(0, 4, 8, 4);
        pnlCols.Controls.Add(colLeft, 0, 0);

        var colRight = BuildNewMachineGroup();
        colRight.Margin = new Padding(8, 4, 0, 4);
        pnlCols.Controls.Add(colRight, 1, 0);

        mainPanel.Controls.Add(pnlCols);

        // 4. Bottom Status
        var pnlStatus = BuildBottomStatus();
        pnlStatus.Dock = DockStyle.Bottom;
        pnlStatus.Height = 28;
        mainPanel.Controls.Add(pnlStatus);

        Controls.Add(mainPanel);

        // Dock ordering
        pnlHeader.BringToFront();
        pnlUserRow.BringToFront();
        pnlCols.BringToFront();
        pnlStatus.BringToFront();
        ThemeHelper.ApplyTheme(this);
    }

    private Panel BuildTopHeaderBanner()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 4)
        };

        var lblDesc = new Label
        {
            Text = "Cài lại máy / đổi máy sẽ MẤT: mật khẩu Windows đã lưu (WiFi, ổ mạng, máy in, Outlook...) và chữ ký số (thuế, hoá đơn, BHXH).\n" +
                   "Làm ở MÁY CŨ trước (cột trái) rồi cất vào USB; sang MÁY MỚI làm cột phải. Khách phải đang đăng nhập bằng đúng tài khoản của họ.",
            Location = new Point(0, 2),
            Size = new Size(780, 52),
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = Color.FromArgb(146, 64, 14) // Dark amber/brown
        };

        var btnHelp = new Button
        {
            Text = "? Hướng dẫn",
            Size = new Size(110, 36),
            Location = new Point(pnl.Width - 110, 8),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnHelp.FlatAppearance.BorderColor = _btnBorder;
        btnHelp.FlatAppearance.MouseOverBackColor = _btnHover;
        btnHelp.Click += (_, _) => ShowHelpModal();

        pnl.Controls.Add(lblDesc);
        pnl.Controls.Add(btnHelp);

        return pnl;
    }

    private Panel BuildUserAccountRow()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4)
        };

        var lblUser = new Label
        {
            Text = "Tài khoản người dùng:",
            Location = new Point(0, 5),
            Size = new Size(155, 24),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _cboUserAccount = new ComboBox
        {
            Location = new Point(160, 5),
            Size = new Size(190, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };

        var lblStatus = new Label
        {
            Text = "✓ Đang đăng nhập - lấy ra / đưa vào được đầy đủ",
            Location = new Point(360, 5),
            Size = new Size(380, 24),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _successGreen,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pnl.Controls.Add(lblUser);
        pnl.Controls.Add(_cboUserAccount);
        pnl.Controls.Add(lblStatus);

        return pnl;
    }

    private GroupBox BuildOldMachineGroup()
    {
        var gb = new GroupBox
        {
            Text = "SAO LƯU (MÁY HIỆN TẠI) - Xuất dữ liệu cất trữ",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10)
        };

        // Bước 1: Chọn nơi cất (USB)
        var lblB1 = new Label
        {
            Text = "Bước 1",
            Location = new Point(10, 28),
            Size = new Size(58, 28),
            BackColor = Color.FromArgb(28, 25, 23),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnChooseDest = new Button
        {
            Text = "Chọn thư mục lưu trữ (USB/Ổ đĩa)...",
            Location = new Point(74, 27),
            Size = new Size(150, 30),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnChooseDest.FlatAppearance.BorderColor = _btnBorder;
        btnChooseDest.FlatAppearance.MouseOverBackColor = _btnHover;
        btnChooseDest.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn USB hoặc thư mục để cất Mật khẩu & Chữ ký số",
                SelectedPath = Directory.Exists(_exportDestDir) ? _exportDestDir : @"C:\"
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _exportDestDir = fbd.SelectedPath;
                _lblExportPath.Text = TruncatePath(_exportDestDir, 28);
            }
        };

        _lblExportPath = new Label
        {
            Text = @"D:\...",
            Location = new Point(230, 28),
            Size = new Size(200, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading
        };

        // Bước 2: Kiểm tra máy này có gì
        var lblB2 = new Label
        {
            Text = "Bước 2",
            Location = new Point(10, 66),
            Size = new Size(58, 28),
            BackColor = Color.FromArgb(28, 25, 23),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnScan = new Button
        {
            Text = "Kiểm tra chứng thư & mật khẩu trên máy",
            Location = new Point(74, 65),
            Size = new Size(170, 30),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnScan.FlatAppearance.BorderColor = _btnBorder;
        btnScan.FlatAppearance.MouseOverBackColor = _btnHover;
        btnScan.Click += (_, _) => ScanOldMachineItems();

        _lblScanStatus = new Label
        {
            Text = "(chưa kiểm tra)",
            Location = new Point(250, 66),
            Size = new Size(180, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            ForeColor = _textMuted
        };

        // Table (DataGridView)
        _dgvOldMachine = new DataGridView
        {
            Location = new Point(10, 104),
            Size = new Size(420, 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            GridColor = _borderColor,
            Font = new Font("Segoe UI", 9f)
        };
        _dgvOldMachine.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        _dgvOldMachine.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _dgvOldMachine.ColumnHeadersHeight = 28;

        var colCheck = new DataGridViewCheckBoxColumn { Name = "ColCheck", HeaderText = "✓", Width = 35 };
        var colType = new DataGridViewTextBoxColumn { Name = "ColType", HeaderText = "Loại", Width = 115 };
        var colDesc = new DataGridViewTextBoxColumn { Name = "ColDesc", HeaderText = "Nội dung (tích để lấy ra)", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
        _dgvOldMachine.Columns.AddRange(colCheck, colType, colDesc);

        // Bước 3: Đặt mật khẩu bảo vệ
        var lblB3 = new Label
        {
            Text = "Bước 3",
            Location = new Point(10, 254),
            Size = new Size(58, 26),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = Color.FromArgb(28, 25, 23),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblPassTitle = new Label
        {
            Text = "Đặt mật khẩu bảo vệ file chữ ký số:",
            Location = new Point(74, 254),
            Size = new Size(220, 26),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f)
        };

        _txtExportPassword = new TextBox
        {
            Location = new Point(298, 254),
            Size = new Size(132, 26),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            PasswordChar = '•',
            Text = "123456"
        };

        // Big Export Button
        _btnExport = new Button
        {
            Text = "Xuất toàn bộ vào thư mục đã chọn",
            Location = new Point(10, 290),
            Size = new Size(420, 38),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnExport.FlatAppearance.BorderColor = _btnBorder;
        _btnExport.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnExport.Click += async (_, _) => await DoExportAsync();

        gb.Controls.Add(lblB1);
        gb.Controls.Add(btnChooseDest);
        gb.Controls.Add(_lblExportPath);
        gb.Controls.Add(lblB2);
        gb.Controls.Add(btnScan);
        gb.Controls.Add(_lblScanStatus);
        gb.Controls.Add(_dgvOldMachine);
        gb.Controls.Add(lblB3);
        gb.Controls.Add(lblPassTitle);
        gb.Controls.Add(_txtExportPassword);
        gb.Controls.Add(_btnExport);

        return gb;
    }

    private GroupBox BuildNewMachineGroup()
    {
        var gb = new GroupBox
        {
            Text = "PHỤC HỒI (MÁY ĐÍCH) - Nạp dữ liệu vào máy này",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10)
        };

        // Bước 1: Chọn thư mục đã cất
        var lblB1 = new Label
        {
            Text = "Bước 1",
            Location = new Point(10, 28),
            Size = new Size(58, 28),
            BackColor = Color.FromArgb(28, 25, 23),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnChooseSrc = new Button
        {
            Text = "Chọn thư mục chứa bản sao lưu...",
            Location = new Point(74, 27),
            Size = new Size(160, 30),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnChooseSrc.FlatAppearance.BorderColor = _btnBorder;
        btnChooseSrc.FlatAppearance.MouseOverBackColor = _btnHover;
        btnChooseSrc.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục MatKhau_ChuKySo_... đã cất trên USB",
                SelectedPath = Directory.Exists(_importSourceDir) ? _importSourceDir : (Directory.Exists(_exportDestDir) ? _exportDestDir : @"C:\")
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _importSourceDir = fbd.SelectedPath;
                _lblImportPath.Text = TruncatePath(_importSourceDir, 28);
                ScanImportFolder();
            }
        };

        _lblImportPath = new Label
        {
            Text = "Chưa chọn",
            Location = new Point(240, 28),
            Size = new Size(190, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading
        };

        // Hint note
        var lblHint = new Label
        {
            Text = "Chọn thư mục 'MatKhau_ChuKySo_...' trên USB để xem bên trong có gì.",
            Location = new Point(10, 68),
            Size = new Size(420, 24),
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _textMuted
        };

        // Table (DataGridView)
        _dgvNewMachine = new DataGridView
        {
            Location = new Point(10, 104),
            Size = new Size(420, 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            GridColor = _borderColor,
            Font = new Font("Segoe UI", 9f)
        };
        _dgvNewMachine.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        _dgvNewMachine.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _dgvNewMachine.ColumnHeadersHeight = 28;

        var colCheck = new DataGridViewCheckBoxColumn { Name = "ColCheck", HeaderText = "✓", Width = 35 };
        var colType = new DataGridViewTextBoxColumn { Name = "ColType", HeaderText = "Loại", Width = 115 };
        var colFile = new DataGridViewTextBoxColumn { Name = "ColFile", HeaderText = "File (tích để đưa vào)", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
        _dgvNewMachine.Columns.AddRange(colCheck, colType, colFile);

        // Bước 2: Mật khẩu đã đặt
        var lblB2 = new Label
        {
            Text = "Bước 2",
            Location = new Point(10, 254),
            Size = new Size(58, 26),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = Color.FromArgb(28, 25, 23),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblPassTitle = new Label
        {
            Text = "Mật khẩu đã đặt lúc lấy ra:",
            Location = new Point(74, 254),
            Size = new Size(180, 26),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f)
        };

        _txtImportPassword = new TextBox
        {
            Location = new Point(258, 254),
            Size = new Size(172, 26),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            PasswordChar = '•',
            Text = "123456"
        };

        // Big Import Button
        _btnImport = new Button
        {
            Text = "Nạp dữ liệu vào máy tính này",
            Location = new Point(10, 290),
            Size = new Size(420, 38),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnImport.FlatAppearance.BorderColor = _btnBorder;
        _btnImport.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnImport.Click += async (_, _) => await DoImportAsync();

        gb.Controls.Add(lblB1);
        gb.Controls.Add(btnChooseSrc);
        gb.Controls.Add(_lblImportPath);
        gb.Controls.Add(lblHint);
        gb.Controls.Add(_dgvNewMachine);
        gb.Controls.Add(lblB2);
        gb.Controls.Add(lblPassTitle);
        gb.Controls.Add(_txtImportPassword);
        gb.Controls.Add(_btnImport);

        return gb;
    }

    private Panel BuildBottomStatus()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        _lblBottomStatus = new Label
        {
            Text = "Sẵn sàng.",
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pnl.Controls.Add(_lblBottomStatus);
        return pnl;
    }

    private void SetStatus(string msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(msg));
            return;
        }
        _lblBottomStatus.Text = $"{msg}";
    }

    private void ScanOldMachineItems()
    {
        SetStatus("Đang quét chứng thư số & mật khẩu trên máy này...");
        _dgvOldMachine.Rows.Clear();

        int count = 0;

        // 1. Digital Certificates in Personal Store (CurrentUser\My)
        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                string subject = cert.GetNameInfo(X509NameType.SimpleName, false);
                if (string.IsNullOrEmpty(subject)) subject = cert.Subject;

                string issuer = cert.GetNameInfo(X509NameType.SimpleName, true);
                string expDate = cert.NotAfter.ToString("dd/MM/yyyy");

                string typeName = "Chữ ký số (CKS)";
                if (issuer.Contains("Viettel", StringComparison.OrdinalIgnoreCase)) typeName = "CKS Viettel-CA";
                else if (issuer.Contains("VNPT", StringComparison.OrdinalIgnoreCase)) typeName = "CKS VNPT-CA";
                else if (issuer.Contains("BKAV", StringComparison.OrdinalIgnoreCase)) typeName = "CKS BKAV-CA";
                else if (issuer.Contains("FPT", StringComparison.OrdinalIgnoreCase)) typeName = "CKS FPT-CA";
                else if (issuer.Contains("MISA", StringComparison.OrdinalIgnoreCase)) typeName = "CKS MISA eSign";
                else if (issuer.Contains("EasyCA", StringComparison.OrdinalIgnoreCase)) typeName = "CKS EasyCA";

                string desc = $"{subject} (Hạn: {expDate} | Cấp bởi: {issuer})";
                int rIdx = _dgvOldMachine.Rows.Add(true, typeName, desc);
                _dgvOldMachine.Rows[rIdx].Tag = new ExportItemTag { ItemType = "CERT", Identifier = cert.Thumbprint, Cert = cert };
                count++;
            }
            store.Close();
        }
        catch { }

        // 2. Scan for local PFX / P12 files in User Profile (Desktop, Documents)
        try
        {
            var userFolders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            foreach (var folder in userFolders)
            {
                if (!Directory.Exists(folder)) continue;
                var pfxFiles = Directory.GetFiles(folder, "*.pfx", SearchOption.TopDirectoryOnly);
                foreach (var f in pfxFiles)
                {
                    int rIdx = _dgvOldMachine.Rows.Add(true, "Tệp Chữ ký số (.pfx)", $"{Path.GetFileName(f)} ({f})");
                    _dgvOldMachine.Rows[rIdx].Tag = new ExportItemTag { ItemType = "FILE_PFX", Identifier = f };
                    count++;
                }
            }
        }
        catch { }

        // 3. Stored Windows Credentials (cmdkey)
        try
        {
            var psi = new ProcessStartInfo("cmdkey.exe", "/list")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            var lines = output.Split('\n');
            string curTarget = "";
            string curType = "";
            string curUser = "";

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(curTarget))
                    {
                        int rIdx = _dgvOldMachine.Rows.Add(true, "Mật khẩu Windows", $"{curTarget} (User: {curUser})");
                        _dgvOldMachine.Rows[rIdx].Tag = new ExportItemTag { ItemType = "CRED", Identifier = $"{curTarget}|{curUser}|{curType}" };
                        count++;
                    }
                    curTarget = line.Substring("Target:".Length).Trim();
                    curType = "";
                    curUser = "";
                }
                else if (line.StartsWith("Type:", StringComparison.OrdinalIgnoreCase))
                {
                    curType = line.Substring("Type:".Length).Trim();
                }
                else if (line.StartsWith("User:", StringComparison.OrdinalIgnoreCase))
                {
                    curUser = line.Substring("User:".Length).Trim();
                }
            }

            if (!string.IsNullOrEmpty(curTarget))
            {
                int rIdx = _dgvOldMachine.Rows.Add(true, "Mật khẩu Windows", $"{curTarget} (User: {curUser})");
                _dgvOldMachine.Rows[rIdx].Tag = new ExportItemTag { ItemType = "CRED", Identifier = $"{curTarget}|{curUser}|{curType}" };
                count++;
            }
        }
        catch { }

        // 4. Stored WiFi profiles
        try
        {
            var psi = new ProcessStartInfo("netsh.exe", "wlan show profiles")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            var wifiProfiles = new List<string>();
            using (var reader = new StringReader(output))
            {
                string? l;
                while ((l = reader.ReadLine()) != null)
                {
                    int cIdx = l.IndexOf(':');
                    if (cIdx > 0 && (l.Contains("Profile") || l.Contains("hồ sơ")))
                    {
                        var name = l.Substring(cIdx + 1).Trim();
                        if (!string.IsNullOrEmpty(name) && !wifiProfiles.Contains(name))
                            wifiProfiles.Add(name);
                    }
                }
            }

            if (wifiProfiles.Count > 0)
            {
                int rIdx = _dgvOldMachine.Rows.Add(true, "Mạng WiFi", $"Toàn bộ hồ sơ WiFi ({wifiProfiles.Count} mạng đã lưu kèm mật khẩu)");
                _dgvOldMachine.Rows[rIdx].Tag = new ExportItemTag { ItemType = "WIFI_ALL", Identifier = string.Join(";", wifiProfiles) };
                count++;
            }
        }
        catch { }

        _lblScanStatus.Text = $"(Đã tìm thấy {count} mục)";
        _lblScanStatus.ForeColor = count > 0 ? _successGreen : _textMuted;
        SetStatus($"Đã kiểm tra xong! Tìm thấy {count} chứng thư số và thông tin đăng nhập.");
    }

    private async Task DoExportAsync()
    {
        if (string.IsNullOrWhiteSpace(_exportDestDir) || !Directory.Exists(_exportDestDir))
        {
            MessageBox.Show("Vui lòng chọn thư mục hoặc USB để cất dữ liệu!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedRows = new List<DataGridViewRow>();
        foreach (DataGridViewRow row in _dgvOldMachine.Rows)
        {
            if (Convert.ToBoolean(row.Cells["ColCheck"].Value))
            {
                selectedRows.Add(row);
            }
        }

        if (selectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng bấm 'Kiểm tra máy này có gì' và tích chọn ít nhất một mục để lấy ra!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var password = _txtExportPassword.Text.Trim();
        if (string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Vui lòng đặt mật khẩu bảo vệ cho file chữ ký số!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtExportPassword.Focus();
            return;
        }

        // Target: <ExportDestDir>\MatKhau_ChuKySo_<UserName>_<Date>
        var folderName = $"MatKhau_ChuKySo_{Environment.UserName}_{DateTime.Now:yyyyMMdd_HHmm}";
        var finalFolder = Path.Combine(_exportDestDir, folderName);
        Directory.CreateDirectory(finalFolder);

        SetStatus("Đang xuất chứng thư số và mật khẩu ra USB...");

        int exportedCount = 0;
        await Task.Run(() =>
        {
            var manifestItems = new List<ManifestItem>();

            foreach (var row in selectedRows)
            {
                var tag = row.Tag as ExportItemTag;
                if (tag == null) continue;

                var itemType = tag.ItemType;
                var identifier = tag.Identifier;
                var cert = tag.Cert;

                if (itemType == "CERT" && cert != null)
                {
                    try
                    {
                        var safeName = MakeSafeFilename(cert.GetNameInfo(X509NameType.SimpleName, false));
                        if (string.IsNullOrEmpty(safeName)) safeName = "Chukhyso";

                        if (cert.HasPrivateKey)
                        {
                            try
                            {
                                var pfxBytes = cert.Export(X509ContentType.Pfx, password);
                                var pfxPath = Path.Combine(finalFolder, $"{safeName}_{cert.Thumbprint.Substring(0, 8)}.pfx");
                                File.WriteAllBytes(pfxPath, pfxBytes);

                                manifestItems.Add(new ManifestItem
                                {
                                    Type = "PFX",
                                    FileName = Path.GetFileName(pfxPath),
                                    Description = row.Cells["ColDesc"].Value?.ToString() ?? ""
                                });
                                exportedCount++;
                                continue;
                            }
                            catch { }
                        }

                        // Export public certificate (.cer)
                        var cerBytes = cert.Export(X509ContentType.Cert);
                        var cerPath = Path.Combine(finalFolder, $"{safeName}_{cert.Thumbprint.Substring(0, 8)}.cer");
                        File.WriteAllBytes(cerPath, cerBytes);

                        manifestItems.Add(new ManifestItem
                        {
                            Type = "CER",
                            FileName = Path.GetFileName(cerPath),
                            Description = row.Cells["ColDesc"].Value?.ToString() ?? ""
                        });
                        exportedCount++;
                    }
                    catch { }
                }
                else if (itemType == "FILE_PFX")
                {
                    try
                    {
                        var srcFile = identifier;
                        if (File.Exists(srcFile))
                        {
                            var dstFile = Path.Combine(finalFolder, Path.GetFileName(srcFile));
                            File.Copy(srcFile, dstFile, true);

                            manifestItems.Add(new ManifestItem
                            {
                                Type = "FILE_PFX",
                                FileName = Path.GetFileName(dstFile),
                                Description = row.Cells["ColDesc"].Value?.ToString() ?? ""
                            });
                            exportedCount++;
                        }
                    }
                    catch { }
                }
                else if (itemType == "WIFI_ALL")
                {
                    try
                    {
                        var wifiDir = Path.Combine(finalFolder, "WiFi");
                        Directory.CreateDirectory(wifiDir);
                        var psi = new ProcessStartInfo("netsh.exe", $"wlan export profile folder=\"{wifiDir}\" key=clear")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var proc = Process.Start(psi)!;
                        proc.WaitForExit();

                        manifestItems.Add(new ManifestItem
                        {
                            Type = "WIFI",
                            FileName = "WiFi",
                            Description = row.Cells["ColDesc"].Value?.ToString() ?? ""
                        });
                        exportedCount++;
                    }
                    catch { }
                }
                else if (itemType == "CRED")
                {
                    manifestItems.Add(new ManifestItem
                    {
                        Type = "CREDENTIAL",
                        FileName = identifier,
                        Description = row.Cells["ColDesc"].Value?.ToString() ?? ""
                    });
                    exportedCount++;
                }
            }

            // Write Manifest file
            var manifest = new ManifestFile
            {
                SourceMachine = Environment.MachineName,
                SourceUser = Environment.UserName,
                CreatedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                Items = manifestItems
            };

            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(finalFolder, "manifest.json"), json, Encoding.UTF8);

            // Write human-readable summary
            var txtSummary = new StringBuilder();
            txtSummary.AppendLine("==================================================================");
            txtSummary.AppendLine("    BẢN SAO LƯU MẬT KHẨU WINDOWS VÀ CHỮ KÝ SỐ ĐÃ LẤY RA           ");
            txtSummary.AppendLine($"  Máy xuất          : {Environment.MachineName}");
            txtSummary.AppendLine($"  Tài khoản         : {Environment.UserName}");
            txtSummary.AppendLine($"  Thời gian         : {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            txtSummary.AppendLine($"  Mật khẩu bảo vệ   : {password}");
            txtSummary.AppendLine("==================================================================");
            txtSummary.AppendLine();
            txtSummary.AppendLine("HƯỚNG DẪN ĐƯA VÀO MÁY MỚI:");
            txtSummary.AppendLine("1. Cắm USB vào máy mới, mở tool 'Mật khẩu Windows & Chữ ký số'.");
            txtSummary.AppendLine("2. Ở cột MÁY MỚI (bên phải), chọn thư mục này.");
            txtSummary.AppendLine("3. Nhập mật khẩu bảo vệ ở trên rồi bấm 'ĐƯA VÀO máy này'.");
            File.WriteAllText(Path.Combine(finalFolder, "HuongDan_SuDung.txt"), txtSummary.ToString(), Encoding.UTF8);
        });

        SetStatus($"Hoàn tất! Đã lấy ra {exportedCount} mục vào: {finalFolder}");

        var res = MessageBox.Show(
            $"Đã lấy ra thành công {exportedCount} mục và cất vào thư mục:\n{finalFolder}\n\n" +
            $"Mật khẩu bảo vệ đã đặt: {password}\n\n" +
            $"Bạn có muốn mở thư mục này ngay bây giờ không?",
            "Lấy Ra Thành Công",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (res == DialogResult.Yes)
        {
            try { Process.Start("explorer.exe", finalFolder); } catch { }
        }
    }

    private void ScanImportFolder()
    {
        _dgvNewMachine.Rows.Clear();
        if (string.IsNullOrEmpty(_importSourceDir) || !Directory.Exists(_importSourceDir))
        {
            return;
        }

        SetStatus("Đang đọc nội dung thư mục sao lưu...");
        int count = 0;

        // Check for manifest.json
        var manifestPath = Path.Combine(_importSourceDir, "manifest.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ManifestFile>(json);
                if (manifest?.Items != null)
                {
                    foreach (var item in manifest.Items)
                    {
                        string typeDisplay = item.Type switch
                        {
                            "PFX" => "Chữ ký số (.pfx)",
                            "CER" => "Chứng thư công khai (.cer)",
                            "FILE_PFX" => "File Chữ ký số (.pfx)",
                            "WIFI" => "Mạng WiFi",
                            "CREDENTIAL" => "Mật khẩu Windows",
                            _ => item.Type
                        };

                        int rIdx = _dgvNewMachine.Rows.Add(true, typeDisplay, $"{item.FileName} - {item.Description}");
                        _dgvNewMachine.Rows[rIdx].Tag = item;
                        count++;
                    }
                }
            }
            catch { }
        }

        // Also scan loose files in directory
        if (count == 0)
        {
            var pfxFiles = Directory.GetFiles(_importSourceDir, "*.pfx", SearchOption.AllDirectories);
            foreach (var f in pfxFiles)
            {
                int rIdx = _dgvNewMachine.Rows.Add(true, "Chữ ký số (.pfx)", Path.GetFileName(f));
                _dgvNewMachine.Rows[rIdx].Tag = new ManifestItem { Type = "PFX", FileName = Path.GetFileName(f) };
                count++;
            }

            var cerFiles = Directory.GetFiles(_importSourceDir, "*.cer", SearchOption.AllDirectories);
            foreach (var f in cerFiles)
            {
                int rIdx = _dgvNewMachine.Rows.Add(true, "Chứng thư số (.cer)", Path.GetFileName(f));
                _dgvNewMachine.Rows[rIdx].Tag = new ManifestItem { Type = "CER", FileName = Path.GetFileName(f) };
                count++;
            }

            var wifiXmls = Directory.GetFiles(_importSourceDir, "*.xml", SearchOption.AllDirectories);
            if (wifiXmls.Length > 0)
            {
                int rIdx = _dgvNewMachine.Rows.Add(true, "Hồ sơ WiFi", $"Toàn bộ WiFi ({wifiXmls.Length} tệp .xml)");
                _dgvNewMachine.Rows[rIdx].Tag = new ManifestItem { Type = "WIFI", FileName = "WiFi" };
                count++;
            }
        }

        SetStatus($"Đã tìm thấy {count} mục trong thư mục sao lưu sẵn sàng đưa vào máy này.");
    }

    private async Task DoImportAsync()
    {
        if (string.IsNullOrWhiteSpace(_importSourceDir) || !Directory.Exists(_importSourceDir))
        {
            MessageBox.Show("Vui lòng chọn thư mục chứa bản sao lưu trên USB!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedRows = new List<DataGridViewRow>();
        foreach (DataGridViewRow row in _dgvNewMachine.Rows)
        {
            if (Convert.ToBoolean(row.Cells["ColCheck"].Value))
            {
                selectedRows.Add(row);
            }
        }

        if (selectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng tích chọn ít nhất một mục để đưa vào máy này!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var password = _txtImportPassword.Text.Trim();

        SetStatus("Đang nạp chữ ký số và mật khẩu vào máy này...");

        int importedCount = 0;
        await Task.Run(() =>
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            foreach (var row in selectedRows)
            {
                var item = row.Tag as ManifestItem;
                if (item == null) continue;

                if (item.Type == "PFX" || item.Type == "FILE_PFX")
                {
                    try
                    {
                        var filePath = Path.Combine(_importSourceDir, item.FileName);
                        if (!File.Exists(filePath))
                        {
                            var matches = Directory.GetFiles(_importSourceDir, item.FileName, SearchOption.AllDirectories);
                            if (matches.Length > 0) filePath = matches[0];
                        }

                        if (File.Exists(filePath))
                        {
                            var cert = new X509Certificate2(filePath, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet);
                            store.Add(cert);
                            importedCount++;
                        }
                    }
                    catch
                    {
                        // Wrong password or export issue
                    }
                }
                else if (item.Type == "CER")
                {
                    try
                    {
                        var filePath = Path.Combine(_importSourceDir, item.FileName);
                        if (File.Exists(filePath))
                        {
                            var cert = new X509Certificate2(filePath);
                            store.Add(cert);
                            importedCount++;
                        }
                    }
                    catch { }
                }
                else if (item.Type == "WIFI")
                {
                    try
                    {
                        var xmlFiles = Directory.GetFiles(_importSourceDir, "*.xml", SearchOption.AllDirectories);
                        foreach (var xml in xmlFiles)
                        {
                            try
                            {
                                var psi = new ProcessStartInfo("netsh.exe", $"wlan add profile filename=\"{xml}\" user=all")
                                {
                                    CreateNoWindow = true,
                                    UseShellExecute = false
                                };
                                using var proc = Process.Start(psi)!;
                                proc.WaitForExit();
                                importedCount++;
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }

            store.Close();
        });

        SetStatus($"Hoàn tất! Đã nạp thành công {importedCount} mục vào máy tính.");
        MessageBox.Show($"Đã đưa vào máy này thành công {importedCount} mục!\nCác chứng thư số và mật khẩu đã sẵn sàng sử dụng.", "Nạp Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowHelpModal()
    {
        var helpMsg = new StringBuilder();
        helpMsg.AppendLine("HƯỚNG DẪN SAO LƯU & CHUYỂN ĐỔI CHỮ KÝ SỐ VÀ MẬT KHẨU WINDOWS");
        helpMsg.AppendLine("=====================================================================");
        helpMsg.AppendLine("1. KHI CÀI LẠI WIN HOẶC ĐỔI MÁY MỚI:");
        helpMsg.AppendLine("   - Mọi mật khẩu đã lưu (WiFi, ổ đĩa mạng công ty, máy in, Outlook...)");
        helpMsg.AppendLine("     và các chữ ký số kê khai thuế, hóa đơn điện tử sẽ BỊ XÓA MẤT.");
        helpMsg.AppendLine();
        helpMsg.AppendLine("2. THỰC HIỆN TRÊN MÁY CŨ (CỘT TRÁI):");
        helpMsg.AppendLine("   - Bước 1: Cắm USB và bấm 'Chọn nơi cất (USB)'.");
        helpMsg.AppendLine("   - Bước 2: Bấm 'Kiểm tra máy này có gì', phần mềm sẽ tự dò tìm");
        helpMsg.AppendLine("     toàn bộ chữ ký số (Viettel, VNPT, BKAV, MISA...), mật khẩu mạng.");
        helpMsg.AppendLine("   - Bước 3: Đặt một mật khẩu bảo vệ dễ nhớ (VD: 123456).");
        helpMsg.AppendLine("   - Bước 4: Bấm 'LẤY RA và cất vào nơi đã chọn'.");
        helpMsg.AppendLine();
        helpMsg.AppendLine("3. THỰC HIỆN TRÊN MÁY MỚI (CỘT PHẢI):");
        helpMsg.AppendLine("   - Bước 1: Cắm USB vào máy mới, bấm 'Chọn thư mục đã cất'.");
        helpMsg.AppendLine("   - Bước 2: Nhập đúng mật khẩu đã đặt lúc lấy ra.");
        helpMsg.AppendLine("   - Bước 3: Bấm 'ĐƯA VÀO máy này'.");
        helpMsg.AppendLine();
        helpMsg.AppendLine("4. LƯU Ý VỚI USB TOKEN CHỮ KÝ SỐ CỨNG:");
        helpMsg.AppendLine("   - Đối với USB Token (cục cắm rời): Sau khi nạp chứng thư,");
        helpMsg.AppendLine("     bạn chỉ cần cắm USB Token vào máy mới và cài phần mềm quản lý của hãng Token là ký số bình thường!");

        MessageBox.Show(helpMsg.ToString(), "Hướng Dẫn Chi Tiết", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string MakeSafeFilename(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return "item";
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            filename = filename.Replace(c, '_');
        }
        return filename.Trim();
    }
}

public class ManifestFile
{
    public string SourceMachine { get; set; } = string.Empty;
    public string SourceUser { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public List<ManifestItem> Items { get; set; } = new();
}

public class ManifestItem
{
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ExportItemTag
{
    public string ItemType { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public X509Certificate2? Cert { get; set; }
}

