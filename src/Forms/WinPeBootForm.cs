using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ernastie.Services;

namespace Ernastie.Forms;

public class WinPeBootForm : Form
{
    private readonly WinPeService _peService = new();

    // Dynamic Design Tokens matching ThemeHelper
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _btnGold => ThemeHelper.IsDark ? ThemeHelper.BtnBg : Color.FromArgb(219, 197, 154);
    private Color _btnGoldBorder => ThemeHelper.IsDark ? ThemeHelper.BtnBorder : Color.FromArgb(140, 110, 74);
    private Color _btnGoldHover => ThemeHelper.IsDark ? ThemeHelper.BtnHover : Color.FromArgb(208, 185, 140);
    private Color _btnSand => ThemeHelper.IsDark ? ThemeHelper.CardBg : Color.FromArgb(230, 216, 195);
    private Color _btnSandBorder => ThemeHelper.IsDark ? ThemeHelper.BorderColor : Color.FromArgb(150, 123, 84);
    private Color _btnSandHover => ThemeHelper.IsDark ? ThemeHelper.BtnHover : Color.FromArgb(218, 202, 179);
    private Color _textDark => ThemeHelper.TextHeading;
    private Color _textBrown => ThemeHelper.TextMuted;
    private Color _tabActiveBorder => ThemeHelper.PrimaryBlue;
    private Color _tabInactiveBorder => ThemeHelper.BorderColor;

    // Controls
    private Button _btnTabAnhdv = null!;
    private Button _btnTabCustomWim = null!;
    private Panel _pnlAnhdv = null!;
    private Panel _pnlCustomWim = null!;

    // Tab 1 controls
    private Button _btnDownloadAll = null!;
    private Button _btnBootOnce = null!;
    private Button _btnBootPermanent = null!;
    private Button _btn25GbWinPe = null!;

    // Tab 2 controls
    private TextBox _txtWimPath = null!;
    private Button _btnBrowseWim = null!;
    private Button _btnAutoDetectWim = null!;
    private CheckBox _chkInjectVmd = null!;
    private CheckBox _chkInjectWifi = null!;
    private Button _btnInjectDrivers = null!;
    private ComboBox _cboUsbDrives = null!;
    private Button _btnRefreshUsb = null!;
    private Button _btnInstallToHdd = null!;
    private Button _btnMakeUsbBoot = null!;

    // Shared controls
    private RichTextBox _rtbLog = null!;

    private string _currentAnhdvDir = WinPeService.DefaultAnhdvDir;
    private bool _isAnhdvFound = false;

    public WinPeBootForm()
    {
        InitializeComponents();
        CheckStatusOnInit();
    }

    private void InitializeComponents()
    {
        Text = "Tạo WinPE Boot - bộ AnhDV";
        Size = new Size(640, 530);
        MinimumSize = new Size(600, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        try
        {
            var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(icoPath)) Icon = new Icon(icoPath);
            else Icon = SystemIcons.Application;
        }
        catch
        {
            Icon = SystemIcons.Application;
        }
        MaximizeBox = false;
        MinimizeBox = true;
        DoubleBuffered = true;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = _bgForm
        };

        // 1. Controls stacked with standard WinForms Docking model
        // Top controls:
        var pnlTabs = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _bgForm,
            Margin = new Padding(0, 0, 0, 8)
        };
        pnlTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        _btnTabAnhdv = CreateTabButton("▶ Bộ AnhDV Boot (Dựng sẵn)", true);
        _btnTabAnhdv.Margin = new Padding(0, 0, 4, 0);
        _btnTabAnhdv.Click += (_, _) => SwitchTab(true);

        _btnTabCustomWim = CreateTabButton("Bộ tự tạo từ file WIM", false);
        _btnTabCustomWim.Margin = new Padding(4, 0, 0, 0);
        _btnTabCustomWim.Click += (_, _) => SwitchTab(false);

        pnlTabs.Controls.Add(_btnTabAnhdv, 0, 0);
        pnlTabs.Controls.Add(_btnTabCustomWim, 1, 0);

        _pnlAnhdv = BuildAnhdvPanel();
        _pnlAnhdv.Dock = DockStyle.Top;

        _pnlCustomWim = BuildCustomWimPanel();
        _pnlCustomWim.Dock = DockStyle.Top;
        _pnlCustomWim.Visible = false;

        var lblLogTitle = new Label
        {
            Text = "Nhật ký tiến trình:",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = _textDark,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0, 8, 0, 4)
        };

        // Bottom control:
        _btn25GbWinPe = CreateThemedButton("© 25Gb WinPE", _btnGold, _btnGoldBorder, _textDark, FontStyle.Bold, 44);
        _btn25GbWinPe.Dock = DockStyle.Bottom;
        _btn25GbWinPe.Click += async (_, _) => await OnClick25GbWinPe();

        // Middle control: Fill log box
        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.LogBg,
            ForeColor = ThemeHelper.LogText,
            Font = new Font("Consolas", 9.25f, FontStyle.Regular),
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true,
            Margin = new Padding(0, 4, 0, 8)
        };

        // Add to mainPanel in exact Z-order:
        // Controls added with Dock=Top are stacked in reverse order of addition
        mainPanel.Controls.Add(_rtbLog);
        mainPanel.Controls.Add(_btn25GbWinPe);
        mainPanel.Controls.Add(lblLogTitle);
        mainPanel.Controls.Add(_pnlCustomWim);
        mainPanel.Controls.Add(_pnlAnhdv);
        mainPanel.Controls.Add(pnlTabs);

        Controls.Add(mainPanel);
        ThemeHelper.ApplyTheme(this);
    }

    private Panel BuildAnhdvPanel()
    {
        var pnl = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4)
        };
        pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Row 0: Subtitle
        var lblDesc = new Label
        {
            Text = "Tải bộ AnhdvBoot rồi chọn kiểu boot vào WinPE (chạy trên Windows chính).",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = _textBrown,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnl.Controls.Add(lblDesc, 0, 0);

        // Row 1: Big Button: Tải & giải nén tất cả
        _btnDownloadAll = CreateThemedButton("Tải & giải nén tất cả (AnhdvBoot + Apps + Tool)", _btnGold, _btnGoldBorder, _textDark, FontStyle.Bold, 44);
        _btnDownloadAll.Dock = DockStyle.Top;
        _btnDownloadAll.Margin = new Padding(0, 4, 0, 6);
        _btnDownloadAll.Click += (_, _) => OnClickDownloadAll();
        pnl.Controls.Add(_btnDownloadAll, 0, 1);

        // Row 2: Two action buttons in a row
        var pnlActions = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 6)
        };
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        _btnBootOnce = CreateThemedButton("▶ Boot 1 lần (vào PE lần tới)", _btnGold, _btnGoldBorder, _textDark, FontStyle.Bold, 40);
        _btnBootOnce.Margin = new Padding(0, 0, 4, 0);
        _btnBootOnce.Click += async (_, _) => await OnClickBoot(bootOnce: true);

        _btnBootPermanent = CreateThemedButton("∞ Boot vĩnh viễn (tới khi cài xong)", _btnGold, _btnGoldBorder, _textDark, FontStyle.Bold, 40);
        _btnBootPermanent.Margin = new Padding(4, 0, 0, 0);
        _btnBootPermanent.Click += async (_, _) => await OnClickBoot(bootOnce: false);

        pnlActions.Controls.Add(_btnBootOnce, 0, 0);
        pnlActions.Controls.Add(_btnBootPermanent, 1, 0);
        pnl.Controls.Add(pnlActions, 0, 2);

        return pnl;
    }

    private Panel BuildCustomWimPanel()
    {
        var pnl = new Panel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 4)
        };

        var lblDesc = new Label
        {
            Text = "Tạo phân vùng boot WinPE hoặc USB cứu hộ từ file WIM (boot.wim / winpe.wim).",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = _textBrown,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnl.Controls.Add(lblDesc);

        // Wim File Selector row
        var pnlWimFile = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = _bgForm,
            Margin = new Padding(0, 2, 0, 6)
        };
        pnlWimFile.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlWimFile.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115f));
        pnlWimFile.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115f));

        _txtWimPath = new TextBox
        {
            Dock = DockStyle.Fill,
            Height = 28,
            Font = new Font("Segoe UI", 9f),
            PlaceholderText = @"Chọn file WIM (C:\WinPE\boot.wim hoặc sources\boot.wim)..."
        };

        _btnBrowseWim = CreateSmallButton("Chọn WIM...", () => BrowseWimFile());
        _btnAutoDetectWim = CreateSmallButton("Tự tìm WIM", () => AutoDetectWimFile());

        pnlWimFile.Controls.Add(_txtWimPath, 0, 0);
        pnlWimFile.Controls.Add(_btnBrowseWim, 1, 0);
        pnlWimFile.Controls.Add(_btnAutoDetectWim, 2, 0);
        pnl.Controls.Add(pnlWimFile);

        // Options row: Driver Integration & USB
        var pnlOptions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 6)
        };

        _chkInjectVmd = new CheckBox
        {
            Text = "Tích hợp Intel RST (VMD)",
            Checked = true,
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textDark,
            Margin = new Padding(0, 4, 16, 4)
        };

        _chkInjectWifi = new CheckBox
        {
            Text = "Tích hợp Driver Wi-Fi máy hiện tại",
            Checked = true,
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textDark,
            Margin = new Padding(0, 4, 16, 4)
        };

        _btnInjectDrivers = CreateSmallButton("Nạp Driver vào WIM", async () => await OnClickInjectDrivers());
        _btnInjectDrivers.Width = 160;

        pnlOptions.Controls.Add(_chkInjectVmd);
        pnlOptions.Controls.Add(_chkInjectWifi);
        pnlOptions.Controls.Add(_btnInjectDrivers);
        pnl.Controls.Add(pnlOptions);

        // USB row & Action row
        var pnlActions = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 6)
        };
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        _btnInstallToHdd = CreateThemedButton("▶ Cài vào Bootloader ổ cứng", _btnGold, _btnGoldBorder, _textDark, FontStyle.Bold, 38);
        _btnInstallToHdd.Margin = new Padding(0, 0, 4, 0);
        _btnInstallToHdd.Click += async (_, _) => await OnClickInstallCustomWimToHdd();

        _btnMakeUsbBoot = CreateThemedButton("Tạo USB Boot WinPE", _btnGold, _btnGoldBorder, _textDark, FontStyle.Bold, 38);
        _btnMakeUsbBoot.Margin = new Padding(4, 0, 0, 0);
        _btnMakeUsbBoot.Click += async (_, _) => await OnClickCreateUsbBoot();

        pnlActions.Controls.Add(_btnInstallToHdd, 0, 0);
        pnlActions.Controls.Add(_btnMakeUsbBoot, 1, 0);
        pnl.Controls.Add(pnlActions);

        // USB selector row
        var pnlUsb = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 4)
        };

        var lblUsb = new Label
        {
            Text = "Chọn USB mục tiêu:",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textDark,
            Margin = new Padding(0, 6, 8, 4)
        };

        _cboUsbDrives = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 240,
            Font = new Font("Segoe UI", 9f),
            Margin = new Padding(0, 2, 8, 4)
        };

        _btnRefreshUsb = CreateSmallButton("Quét lại USB", () => RefreshUsbDrives());

        pnlUsb.Controls.Add(lblUsb);
        pnlUsb.Controls.Add(_cboUsbDrives);
        pnlUsb.Controls.Add(_btnRefreshUsb);
        pnl.Controls.Add(pnlUsb);

        return pnl;
    }

    private void CheckStatusOnInit()
    {
        var (found, path, msg) = _peService.CheckAnhdvBootStatus();
        _isAnhdvFound = found;
        _currentAnhdvDir = path;

        if (found)
        {
            Log($"[Sẵn Sàng] {msg}");
        }
        else
        {
            // Exact message as shown in user's screenshot
            Log("⚠ Chưa có AnhdvBootTool. Bấm 'Tải AnhdvTools' để chọn nơi lưu, hoặc bấm 'Bắt đầu' để tự tải về C:\\AnhdvBootTool.");
        }

        RefreshUsbDrives();
    }

    private void SwitchTab(bool isAnhdv)
    {
        if (isAnhdv)
        {
            Text = "Tạo WinPE Boot - bộ AnhDV";
            _btnTabAnhdv.Text = "▶ Bộ AnhDV Boot (Dựng sẵn)";
            _btnTabAnhdv.BackColor = ThemeHelper.IsDark ? ThemeHelper.CardBg : Color.White;
            _btnTabAnhdv.FlatAppearance.BorderColor = _tabActiveBorder;
            _btnTabAnhdv.FlatAppearance.BorderSize = 2;

            _btnTabCustomWim.Text = "Bộ tự tạo từ file WIM";
            _btnTabCustomWim.BackColor = _bgForm;
            _btnTabCustomWim.FlatAppearance.BorderColor = _tabInactiveBorder;
            _btnTabCustomWim.FlatAppearance.BorderSize = 1;

            _pnlAnhdv.Visible = true;
            _pnlCustomWim.Visible = false;
        }
        else
        {
            Text = "Tạo WinPE Boot - tự tạo từ file WIM";
            _btnTabAnhdv.Text = "Bộ AnhDV Boot (Dựng sẵn)";
            _btnTabAnhdv.BackColor = _bgForm;
            _btnTabAnhdv.FlatAppearance.BorderColor = _tabInactiveBorder;
            _btnTabAnhdv.FlatAppearance.BorderSize = 1;

            _btnTabCustomWim.Text = "▶ Bộ tự tạo từ file WIM";
            _btnTabCustomWim.BackColor = ThemeHelper.IsDark ? ThemeHelper.CardBg : Color.White;
            _btnTabCustomWim.FlatAppearance.BorderColor = _tabActiveBorder;
            _btnTabCustomWim.FlatAppearance.BorderSize = 2;

            _pnlAnhdv.Visible = false;
            _pnlCustomWim.Visible = true;

            if (string.IsNullOrWhiteSpace(_txtWimPath.Text))
            {
                AutoDetectWimFile();
            }
        }
    }

    private void OnClickDownloadAll()
    {
        var exePath = Path.Combine(_currentAnhdvDir, "AnhdvBootTool.exe");
        if (File.Exists(exePath))
        {
            var res = MessageBox.Show(
                $"Đã tìm thấy AnhdvBootTool tại:\n{exePath}\n\nBạn có muốn khởi chạy AnhdvBootTool.exe ngay không?",
                "AnhdvBootTool Có Sẵn",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    Log($"Đã khởi chạy AnhdvBootTool: {exePath}");
                }
                catch (Exception ex)
                {
                    Log($"[Lỗi khởi chạy]: {ex.Message}");
                }
                return;
            }
        }

        // Show options dialog matching: "Bấm 'Tải AnhdvTools' để chọn nơi lưu, hoặc bấm 'Bắt đầu' để tự tải về C:\AnhdvBootTool."
        using var choiceForm = new Form
        {
            Text = "Tải & Thiết lập bộ Anhdv Boot",
            Size = new Size(680, 420),
            MinimumSize = new Size(620, 380),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = _bgForm,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblPrompt = new Label
        {
            Text = "Chọn phương thức thiết lập bộ cứu hộ Anhdv Boot:",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textDark,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter
        };
        choiceForm.Controls.Add(lblPrompt);

        var pnlBtns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24, 6, 24, 16)
        };
        pnlBtns.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        pnlBtns.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        pnlBtns.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        pnlBtns.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

        var btnStart = CreateThemedButton("Khởi tạo & Tự động tải về C:\\AnhdvBootTool", _btnGold, _btnGoldBorder, _textDark, FontStyle.Bold, 48);
        btnStart.Click += (_, _) =>
        {
            choiceForm.Close();
            Directory.CreateDirectory(WinPeService.DefaultAnhdvDir);
            _currentAnhdvDir = WinPeService.DefaultAnhdvDir;
            Log($"[Bắt đầu] Đã khởi tạo thư mục: {_currentAnhdvDir}");
            OpenUrl("https://anhdvboot.com/tai-ve-anhdv-boot/");
            Log("Đã mở trình duyệt tải bộ Anhdv Boot. Vui lòng tải và lưu tệp vào C:\\AnhdvBootTool.");
            CheckStatusOnInit();
        };

        var btnWeb = CreateThemedButton("Tải AnhdvTools: Chọn nơi lưu & mở trang tải", _btnSand, _btnSandBorder, _textDark, FontStyle.Bold, 48);
        btnWeb.Click += (_, _) =>
        {
            choiceForm.Close();
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục lưu bộ Anhdv Boot",
                UseDescriptionForTitle = true
            };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                _currentAnhdvDir = fbd.SelectedPath;
                Log($"[Tải AnhdvTools] Thư mục lưu được chọn: {_currentAnhdvDir}");
            }
            OpenUrl("https://anhdvboot.com/tai-ve-anhdv-boot/");
            Log("Đã mở trình duyệt tới trang tải bộ Anhdv Boot chính thức.");
            CheckStatusOnInit();
        };

        var btnExtract = CreateThemedButton("Chọn file nén / ISO (.7z, .zip, .iso) để giải nén", _btnSand, _btnSandBorder, _textDark, FontStyle.Bold, 48);
        btnExtract.Click += async (_, _) =>
        {
            choiceForm.Close();
            await ExtractAnhdvPackage();
        };

        var btnPickFolder = CreateThemedButton("Đã có sẵn thư mục AnhdvBoot (Chọn thư mục)", _btnSand, _btnSandBorder, _textDark, FontStyle.Bold, 48);
        btnPickFolder.Click += (_, _) =>
        {
            choiceForm.Close();
            ChooseAnhdvFolder();
        };

        pnlBtns.Controls.Add(btnStart, 0, 0);
        pnlBtns.Controls.Add(btnWeb, 0, 1);
        pnlBtns.Controls.Add(btnExtract, 0, 2);
        pnlBtns.Controls.Add(btnPickFolder, 0, 3);

        choiceForm.Controls.Add(pnlBtns);
        ThemeHelper.ApplyTheme(choiceForm);
        choiceForm.ShowDialog(this);
    }

    private async Task ExtractAnhdvPackage()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Gói cứu hộ (*.zip;*.iso;*.7z;*.rar)|*.zip;*.iso;*.7z;*.rar|Tất cả tệp (*.*)|*.*",
            Title = "Chọn tệp nén hoặc ISO Anhdv Boot đã tải về"
        };

        if (ofd.ShowDialog() != DialogResult.OK) return;

        var file = ofd.FileName;
        var targetDir = _currentAnhdvDir;
        Directory.CreateDirectory(targetDir);

        Log($"Bắt đầu giải nén gói: {Path.GetFileName(file)} -> {targetDir}...");

        await Task.Run(() =>
        {
            try
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".zip")
                {
                    ZipFile.ExtractToDirectory(file, targetDir, overwriteFiles: true);
                    Invoke(() => Log("✅ Đã giải nén tệp zip thành công vào " + targetDir));
                }
                else
                {
                    // Use PowerShell / 7-Zip or Robocopy to extract
                    var script = $"Expand-Archive -LiteralPath '{file}' -DestinationPath '{targetDir}' -Force -ErrorAction SilentlyContinue";
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(60000);
                    Invoke(() => Log("✅ Quá trình trích xuất hoàn tất vào " + targetDir));
                }

                Invoke(() =>
                {
                    CheckStatusOnInit();
                    MessageBox.Show($"Đã giải nén hoàn tất vào thư mục:\n{targetDir}", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                Invoke(() => Log($"[Lỗi giải nén]: {ex.Message}"));
            }
        });
    }

    private void ChooseAnhdvFolder()
    {
        using var fbd = new FolderBrowserDialog
        {
            Description = "Chọn thư mục chứa bộ Anhdv Boot hoặc AnhdvBootTool",
            UseDescriptionForTitle = true
        };

        if (fbd.ShowDialog() == DialogResult.OK)
        {
            _currentAnhdvDir = fbd.SelectedPath;
            Log($"Đã chọn thư mục Anhdv Boot: {_currentAnhdvDir}");
            CheckStatusOnInit();
        }
    }

    private async Task OnClickBoot(bool bootOnce)
    {
        var actionName = bootOnce ? "Boot 1 lần (vào PE lần tới)" : "Boot vĩnh viễn (tới khi cài xong)";
        Log($"Bắt đầu thiết lập {actionName}...");

        // 1. Locate WIM file
        var wim = _peService.FindAvailableWinPeWim(_currentAnhdvDir);
        if (string.IsNullOrEmpty(wim) || !File.Exists(wim))
        {
            var browse = MessageBox.Show(
                "Chưa tìm thấy tệp ảnh WinPE (boot.wim / winpe.wim) trên các đường dẫn mặc định.\n\n" +
                "Bạn có muốn duyệt file để chọn tệp boot.wim của bộ Anhdv Boot hoặc bộ cứu hộ không?",
                "Chọn Tệp WinPE",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (browse == DialogResult.Yes)
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "Ảnh khởi động WinPE (*.wim;*.esd)|*.wim;*.esd|Tất cả tệp (*.*)|*.*",
                    Title = "Chọn tệp boot.wim"
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    wim = ofd.FileName;
                }
            }
        }

        if (string.IsNullOrEmpty(wim) || !File.Exists(wim))
        {
            Log("❌ Hủy bỏ: Chưa có tệp boot.wim để nạp vào BCD.");
            MessageBox.Show("Vui lòng tải hoặc cung cấp file boot.wim của bộ cứu hộ!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Log($"Sử dụng tệp WinPE: {wim}");

        // 2. Configure BCD
        SetControlsEnabled(false);
        try
        {
            var result = await Task.Run(() => _peService.ConfigureWinPeBcd(wim, bootOnce, "Anhdv Boot WinPE Rescue", Log));
            if (result.Success)
            {
                if (bootOnce)
                {
                    var askRestart = MessageBox.Show(
                        "Đã thiết lập Boot 1 lần vào WinPE thành công!\n\n" +
                        "Máy tính sẽ tự động vào WinPE trong lần khởi động tiếp theo.\n" +
                        "Bạn có muốn KHỞI ĐỘNG LẠI MÁY NGAY BÂY GIỜ không?",
                        "Cấu Hình Thành Công",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (askRestart == DialogResult.Yes)
                    {
                        Log("Đang gửi lệnh khởi động lại máy tính...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "shutdown.exe",
                            Arguments = "/r /t 5 /c \"Khoi dong lai vao moi truong cuu ho WinPE...\"",
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Đã thêm menu khởi động WinPE vĩnh viễn vào Windows Boot Manager!\n\n" +
                        "Mỗi khi khởi động lại hoặc bật máy, bạn có thể chọn 'Anhdv Boot WinPE Rescue' để cứu hộ máy.\n" +
                        "Sau khi xử lý xong, bạn có thể dùng tính năng 'Dọn boot + ổ WinPE' để xóa khỏi menu.",
                        "Cấu Hình Thành Công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show($"Lỗi cấu hình BCD: {result.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private async Task OnClick25GbWinPe()
    {
        var confirm = MessageBox.Show(
            "Công cụ sẽ tự động co ổ C: lại 25GB (25,600 MB) và tạo phân vùng mới 'WINPE' (định dạng NTFS).\n\n" +
            "Phân vùng này dùng để chứa bộ WinPE cứu hộ, file ISO Windows và các phần mềm cần thiết.\n\n" +
            "Bạn có muốn tiếp tục thực hiện không?",
            "Xác Nhận Tạo Phân Vùng 25Gb WinPE",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        Log("Bắt đầu tạo phân vùng 25Gb WinPE...");
        SetControlsEnabled(false);

        try
        {
            var result = await Task.Run(() => _peService.Create25GbWinPePartition(Log));
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Lỗi: {result.Message}\n\nLưu ý: Nếu ổ C bị khóa dung lượng co dãn, hãy chạy nút 'Fix limit chia drive' trên màn hình chính.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    // --- Tab 2: Custom WIM Handlers ---

    private void BrowseWimFile()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Ảnh Windows PE (*.wim;*.esd)|*.wim;*.esd|Tất cả tệp (*.*)|*.*",
            Title = "Chọn tệp WIM nguồn"
        };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            _txtWimPath.Text = ofd.FileName;
            Log($"Đã chọn file WIM: {ofd.FileName}");
        }
    }

    private void AutoDetectWimFile()
    {
        var wim = _peService.FindAvailableWinPeWim(_currentAnhdvDir);
        if (!string.IsNullOrEmpty(wim))
        {
            _txtWimPath.Text = wim;
            Log($"Đã tự động phát hiện file WIM: {wim}");
        }
        else
        {
            Log("Chưa phát hiện file WIM tự động. Vui lòng bấm 'Chọn WIM' để duyệt file.");
        }
    }

    private async Task OnClickInjectDrivers()
    {
        var wim = _txtWimPath.Text.Trim();
        if (string.IsNullOrEmpty(wim) || !File.Exists(wim))
        {
            MessageBox.Show("Vui lòng chọn file WIM hợp lệ trước!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Log("Bắt đầu tích hợp Driver vào WIM bằng DISM...");
        SetControlsEnabled(false);

        try
        {
            var res = await Task.Run(() => _peService.InjectDriversIntoWim(wim, _chkInjectVmd.Checked, _chkInjectWifi.Checked, Log));
            if (res.Success)
            {
                MessageBox.Show(res.Message, "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi Tích Hợp Driver", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private async Task OnClickInstallCustomWimToHdd()
    {
        var wim = _txtWimPath.Text.Trim();
        if (string.IsNullOrEmpty(wim) || !File.Exists(wim))
        {
            MessageBox.Show("Vui lòng chọn file WIM nguồn trước khi cài vào Bootloader!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var res = MessageBox.Show(
            "Bạn muốn chọn kiểu Boot nào?\n\n" +
            "• Bấm [Yes]: Boot 1 lần (vào PE ở lần khởi động kế tiếp, sau đó tự về lại Win thường)\n" +
            "• Bấm [No]: Boot vĩnh viễn (thêm vào Menu Bootloader khi bật máy)",
            "Chọn Kiểu Boot WinPE",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (res == DialogResult.Cancel) return;

        bool bootOnce = (res == DialogResult.Yes);
        SetControlsEnabled(false);

        try
        {
            var bcdRes = await Task.Run(() => _peService.ConfigureWinPeBcd(wim, bootOnce, "Custom WinPE Rescue", Log));
            if (bcdRes.Success)
            {
                MessageBox.Show(bcdRes.Message, "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(bcdRes.Message, "Lỗi BCD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private async Task OnClickCreateUsbBoot()
    {
        var wim = _txtWimPath.Text.Trim();
        if (string.IsNullOrEmpty(wim) || !File.Exists(wim))
        {
            MessageBox.Show("Vui lòng chọn file WIM nguồn trước!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cboUsbDrives.SelectedItem == null)
        {
            MessageBox.Show("Không tìm thấy USB được chọn. Vui lòng cắm USB và bấm 'Quét USB'!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var usbStr = _cboUsbDrives.SelectedItem.ToString() ?? string.Empty;
        var driveLetter = usbStr.Split(' ')[0];

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn cài đặt bộ cứu hộ WinPE lên ổ USB [{driveLetter}] không?\n\n" +
            "Hệ thống sẽ sao chép bootloader và nạp file WIM vào USB.",
            "Xác Nhận Tạo USB Boot",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        SetControlsEnabled(false);
        try
        {
            var res = await Task.Run(() => _peService.CreateUsbBoot(driveLetter, wim, Log));
            if (res.Success)
            {
                MessageBox.Show(res.Message, "Tạo USB Boot Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi Tạo USB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private void RefreshUsbDrives()
    {
        _cboUsbDrives.Items.Clear();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.DriveType == DriveType.Removable && d.IsReady)
                {
                    var label = string.IsNullOrEmpty(d.VolumeLabel) ? "USB Drive" : d.VolumeLabel;
                    var gb = d.TotalSize / (1024 * 1024 * 1024);
                    _cboUsbDrives.Items.Add($"{d.Name.TrimEnd('\\')} ({label} - {gb} GB)");
                }
            }

            if (_cboUsbDrives.Items.Count > 0)
            {
                _cboUsbDrives.SelectedIndex = 0;
            }
        }
        catch { }
    }

    // --- Helpers ---

    private void Log(string msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(msg));
            return;
        }

        var time = DateTime.Now.ToString("HH:mm:ss");
        _rtbLog.AppendText($"[{time}] {msg}\n");
        _rtbLog.SelectionStart = _rtbLog.Text.Length;
        _rtbLog.ScrollToCaret();
    }

    private void SetControlsEnabled(bool enabled)
    {
        _btnDownloadAll.Enabled = enabled;
        _btnBootOnce.Enabled = enabled;
        _btnBootPermanent.Enabled = enabled;
        _btn25GbWinPe.Enabled = enabled;
        _btnInstallToHdd.Enabled = enabled;
        _btnMakeUsbBoot.Enabled = enabled;
    }

    private Button CreateTabButton(string text, bool isActive)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textDark,
            BackColor = isActive ? Color.White : _bgForm,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = isActive ? _tabActiveBorder : _tabInactiveBorder;
        btn.FlatAppearance.BorderSize = isActive ? 2 : 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 248, 242);
        return btn;
    }

    private Button CreateThemedButton(string text, Color bg, Color border, Color textClr, FontStyle style, int height)
    {
        var btn = new Button
        {
            Text = text,
            Height = height,
            BackColor = bg,
            ForeColor = textClr,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, style),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 0, 4)
        };
        btn.FlatAppearance.BorderColor = border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = _btnGoldHover;
        return btn;
    }

    private Button CreateSmallButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Height = 28,
            BackColor = _btnSand,
            ForeColor = _textDark,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 0, 2, 0)
        };
        btn.FlatAppearance.BorderColor = _btnSandBorder;
        btn.FlatAppearance.MouseOverBackColor = _btnSandHover;
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
