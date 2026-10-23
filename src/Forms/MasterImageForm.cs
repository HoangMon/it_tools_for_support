using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ernastie.Services;

namespace Ernastie.Forms;

public class MasterImageForm : Form
{
    private readonly MasterImageService _service = new();
    private string _storageDir = string.Empty;
    private CancellationTokenSource? _cts;

    // Dynamic Design System Tokens from ThemeHelper
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _btnGold => ThemeHelper.IsDark ? ThemeHelper.BtnBg : Color.FromArgb(219, 197, 154);
    private Color _btnGoldBorder => ThemeHelper.IsDark ? ThemeHelper.BtnBorder : Color.FromArgb(184, 162, 118);
    private Color _btnSand => ThemeHelper.IsDark ? ThemeHelper.CardBg : Color.FromArgb(230, 216, 195);
    private Color _btnSandBorder => ThemeHelper.IsDark ? ThemeHelper.BorderColor : Color.FromArgb(196, 180, 156);
    private Color _btnCoral => ThemeHelper.IsDark ? Color.FromArgb(69, 26, 3) : Color.FromArgb(232, 190, 172);
    private Color _btnCoralBorder => ThemeHelper.IsDark ? Color.FromArgb(146, 64, 14) : Color.FromArgb(207, 161, 142);
    private Color _textBrown => ThemeHelper.IsDark ? ThemeHelper.TextHeading : Color.FromArgb(62, 56, 33);
    private Color _textDark => ThemeHelper.TextHeading;
    private Color _textRed => ThemeHelper.IsDark ? Color.FromArgb(248, 113, 113) : Color.FromArgb(79, 30, 20);
    private Color _cardBorder => ThemeHelper.BorderColor;

    // Controls
    private Label _lblHeader = null!;
    private Button _btnOneClick = null!;
    private Button _btnStep1 = null!;
    private Button _btnStep2 = null!;
    private Button _btnStep3a = null!;
    private Button _btnStep3b = null!;
    private Button _btnStep4 = null!;
    private Button _btnFixSysprep = null!;
    private Button _btnChangeDir = null!;
    private Button _btnRefresh = null!;
    private TextBox _txtStatus = null!;
    private RichTextBox _rtbLog = null!;
    private ProgressBar _progressBar = null!;

    public MasterImageForm()
    {
        InitializeComponents();
        InitStorageAndStatus();
    }

    private void InitializeComponents()
    {
        Text = "Master Image - tạo bộ cài từ máy mẫu (4 bước)";
        Size = new Size(740, 840);
        MinimumSize = new Size(680, 780);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(18, 14, 18, 16),
            BackColor = _bgForm
        };

        // 1. Header Subtitle
        bool isWinPe = _service.IsWinPE();
        _lblHeader = new Label
        {
            Text = isWinPe
                ? "Đang ở WinPE — Bước 2 chụp ổ Windows của máy mẫu thành install.wim."
                : "Đang ở Windows chính — Bước 1 chạy trên MÁY MẪU; Bước 3 và 4 ghép ra file ISO.",
            Font = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = _textBrown,
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleCenter
        };
        mainPanel.Controls.Add(_lblHeader);

        // 2. Buttons Layout Panel
        var pnlButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = _bgForm,
            Margin = new Padding(0, 4, 0, 10)
        };
        pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        // Button 1 Click
        _btnOneClick = CreateThemedButton("Quy trình 1-Click: Tạo Master Image (Sysprep -> Chụp WIM -> Đóng gói ISO)", _btnGold, _btnGoldBorder, _textBrown, FontStyle.Bold, 48);
        pnlButtons.Controls.Add(_btnOneClick, 0, 0);
        pnlButtons.SetColumnSpan(_btnOneClick, 2);
        _btnOneClick.Click += (_, _) => OnClickOneClick();

        // Button Step 1
        _btnStep1 = CreateThemedButton("1. MÁY MẪU: chuẩn bị boot WinPE + chạy Sysprep (OOBE + Generalize) + khởi động lại", _btnSand, _btnSandBorder, _textDark, FontStyle.Bold, 44);
        pnlButtons.Controls.Add(_btnStep1, 0, 1);
        pnlButtons.SetColumnSpan(_btnStep1, 2);
        _btnStep1.Click += (_, _) => OnClickStep1();

        // Button Step 2
        _btnStep2 = CreateThemedButton("2. WinPE: chụp ổ Windows của máy mẫu thành install.wim", _btnSand, _btnSandBorder, _textDark, FontStyle.Bold, 44);
        pnlButtons.Controls.Add(_btnStep2, 0, 2);
        pnlButtons.SetColumnSpan(_btnStep2, 2);
        _btnStep2.Click += (_, _) => OnClickStep2();

        // Row 3: Step 3a and Step 3b
        _btnStep3a = CreateThemedButton("3a. Tải ISO Windows 10 gốc", _btnSand, _btnSandBorder, _textDark, FontStyle.Bold, 42);
        _btnStep3a.Margin = new Padding(0, 3, 4, 3);
        _btnStep3a.Click += (_, _) => OnClickStep3a();
        pnlButtons.Controls.Add(_btnStep3a, 0, 3);

        _btnStep3b = CreateThemedButton("3b. Tải ISO Windows 11 gốc", _btnSand, _btnSandBorder, _textDark, FontStyle.Bold, 42);
        _btnStep3b.Margin = new Padding(4, 3, 0, 3);
        _btnStep3b.Click += (_, _) => OnClickStep3b();
        pnlButtons.Controls.Add(_btnStep3b, 1, 3);

        // Button Step 4
        _btnStep4 = CreateThemedButton("4. Nhét install.wim vào ISO → win10master.iso / win11master.iso", _btnSand, _btnSandBorder, _textDark, FontStyle.Bold, 44);
        pnlButtons.Controls.Add(_btnStep4, 0, 4);
        pnlButtons.SetColumnSpan(_btnStep4, 2);
        _btnStep4.Click += (_, _) => OnClickStep4();

        // Button Fix Sysprep
        _btnFixSysprep = CreateThemedButton("Sửa lỗi Sysprep (Khắc phục lỗi Generalize 0x80073cf2)", _btnCoral, _btnCoralBorder, _textRed, FontStyle.Bold, 42);
        pnlButtons.Controls.Add(_btnFixSysprep, 0, 5);
        pnlButtons.SetColumnSpan(_btnFixSysprep, 2);
        _btnFixSysprep.Click += (_, _) => OnClickFixSysprep();

        mainPanel.Controls.Add(pnlButtons);

        // 3. Section Tình Trạng
        var pnlStatusHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 10, 0, 0)
        };

        var lblStatusTitle = new Label
        {
            Text = "Tình trạng hệ thống & Công cụ:",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = _textDark,
            Dock = DockStyle.Left,
            Width = 350,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _btnRefresh = CreateSmallButton("Quét lại", () => RefreshStatus());
        _btnRefresh.Dock = DockStyle.Right;
        _btnRefresh.Width = 105;
        _btnRefresh.Margin = new Padding(4, 0, 0, 0);

        _btnChangeDir = CreateSmallButton("Đổi thư mục lưu...", () => OnClickChangeDir());
        _btnChangeDir.Dock = DockStyle.Right;
        _btnChangeDir.Width = 120;

        pnlStatusHeader.Controls.Add(_btnChangeDir);
        pnlStatusHeader.Controls.Add(_btnRefresh);
        pnlStatusHeader.Controls.Add(lblStatusTitle);
        mainPanel.Controls.Add(pnlStatusHeader);

        _txtStatus = new TextBox
        {
            Dock = DockStyle.Top,
            Multiline = true,
            ReadOnly = true,
            Height = 150,
            BackColor = ThemeHelper.LogBg,
            ForeColor = ThemeHelper.LogText,
            Font = new Font("Consolas", 9.25f, FontStyle.Regular),
            ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(0, 4, 0, 12),
            BorderStyle = BorderStyle.FixedSingle
        };
        mainPanel.Controls.Add(_txtStatus);

        // Progress Bar
        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 10,
            Visible = false,
            Margin = new Padding(0, 4, 0, 8)
        };
        mainPanel.Controls.Add(_progressBar);

        // 4. Section Nhật Ký
        var lblLogTitle = new Label
        {
            Text = "Nhật ký tiến trình Master Image:",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = _textDark,
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 8, 0, 2)
        };
        mainPanel.Controls.Add(lblLogTitle);

        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Consolas", 9f, FontStyle.Regular),
            BorderStyle = BorderStyle.FixedSingle
        };
        mainPanel.Controls.Add(_rtbLog);

        Controls.Add(mainPanel);

        // Fix WinForms docking order: labels and panels dock properly
        _rtbLog.BringToFront();
        lblLogTitle.BringToFront();
        _progressBar.BringToFront();
        _txtStatus.BringToFront();
        pnlStatusHeader.BringToFront();
        pnlButtons.BringToFront();
        _lblHeader.BringToFront();
        ThemeHelper.ApplyTheme(this);
    }

    private void InitStorageAndStatus()
    {
        _storageDir = _service.GetDefaultStorageDirectory();
        if (!Directory.Exists(_storageDir))
        {
            try { Directory.CreateDirectory(_storageDir); } catch { }
        }

        Log("Sẵn sàng. Máy mẫu thì bấm Bước 1; đã có ảnh rồi thì bấm Bước 3 / Bước 4.");
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var st = _service.GetStatus(_storageDir);
        _txtStatus.Text =
            $"Đang chạy tại   : {st.RunningLocation}\r\n" +
            $"Chỗ lưu         : {st.StorageDirectory}\r\n" +
            $"Ảnh đã chụp     : {st.WimStatus}\r\n" +
            $"ISO Windows 10  : {st.IsoWin10Status}\r\n" +
            $"ISO Windows 11  : {st.IsoWin11Status}\r\n" +
            $"win10master.iso : {st.Win10MasterStatus}\r\n" +
            $"win11master.iso : {st.Win11MasterStatus}";
    }

    #region Button Actions

    private void OnClickOneClick()
    {
        bool isWinPe = _service.IsWinPE();
        if (!isWinPe)
        {
            var msg = "QUY TRÌNH 1-CLICK TẠO BỘ CÀI MASTER:\n\n" +
                      "1. Tự động kiểm tra và sửa các lỗi Sysprep xung đột\n" +
                      "2. Xác nhận thư mục lưu ảnh WIM/ISO\n" +
                      "3. Chạy Sysprep (OOBE + Generalize) và Khởi động lại máy vào WinPE để chụp ảnh\n\n" +
                      $"Thư mục lưu hiện tại: {_storageDir}\n\n" +
                      "Bạn có muốn bắt đầu Bước 1 ngay bây giờ?";

            var res = MessageBox.Show(msg, "1-Click Master Image Wizard", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                Log("=== [1-CLICK]: BẮT ĐẦU TỰ ĐỘNG HÓA QUY TRÌNH MÁY MẪU ===");
                _service.FixSysprepErrors(Log);

                var confirmSysprep = MessageBox.Show(
                    "Đã sửa xong lỗi Sysprep và chuẩn bị môi trường.\nBấm YES để khởi động Sysprep đưa máy về trạng thái xuất xưởng!",
                    "Xác Nhận Khởi Chạy Sysprep", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirmSysprep == DialogResult.Yes)
                {
                    _service.RunSysprep("reboot", Log);
                }
            }
        }
        else
        {
            // In WinPE
            var msg = "Bạn đang ở môi trường WinPE!\n" +
                      "1-Click sẽ tự động nhận diện ổ Windows của máy mẫu và chụp thành install.wim vào chỗ lưu.\n\n" +
                      $"Chỗ lưu: {_storageDir}\n\n" +
                      "Bắt đầu chụp ảnh ngay?";

            var res = MessageBox.Show(msg, "1-Click WinPE Capture", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                OnClickStep2();
            }
        }
    }

    private void OnClickStep1()
    {
        if (_service.IsWinPE())
        {
            MessageBox.Show("Bạn đang ở môi trường WinPE! Bước 1 (Sysprep) chỉ cần chạy khi đang ở Windows chính của MÁY MẪU.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var res = MessageBox.Show(
            "BƯỚC 1 - CHẠY SYSPREP TRÊN MÁY MẪU:\n\n" +
            "Quá trình này sẽ tổng quát hóa hệ điều hành (Generalize) và chuẩn bị chế độ OOBE.\n" +
            "Máy tính sẽ khởi động lại để bạn boot vào WinPE chụp ảnh ổ đĩa.\n\n" +
            "Bạn đã chuẩn bị USB Boot WinPE sẵn sàng chưa?",
            "Xác nhận chạy Sysprep", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (res == DialogResult.Yes)
        {
            try
            {
                Log("Đang khởi chạy Sysprep Generalize...");
                _service.RunSysprep("reboot", Log);
            }
            catch (Exception ex)
            {
                var reason = _service.GetSysprepFailureReason();
                string details = string.IsNullOrEmpty(reason) ? ex.Message : $"{ex.Message}\n\nChi tiết Panther Log:\n{reason}";
                Log($"[Lỗi chạy Sysprep]: {details}");
                MessageBox.Show($"Lỗi chạy Sysprep:\n\n{details}\n\nNếu gặp lỗi, hãy bấm nút 'Sửa lỗi Sysprep' màu hồng phía dưới để tự động gỡ các gói xung đột!", "Lỗi Sysprep", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void OnClickStep2()
    {
        var osDrives = _service.DetectWindowsPartitions();
        string selectedDrive = "C:";

        if (osDrives.Count == 0)
        {
            MessageBox.Show("Không tìm thấy phân vùng chứa Windows trên máy!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        else if (osDrives.Count == 1)
        {
            selectedDrive = osDrives[0].Split(' ')[0];
        }
        else
        {
            // Pick drive
            using var dlg = new Form
            {
                Text = "Chọn ổ chứa Windows máy mẫu",
                Size = new Size(420, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var cbo = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(16) };
            foreach (var d in osDrives) cbo.Items.Add(d);
            cbo.SelectedIndex = 0;

            var btnOk = new Button { Text = "Xác nhận", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 40 };
            dlg.Controls.Add(cbo);
            dlg.Controls.Add(btnOk);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                selectedDrive = cbo.SelectedItem?.ToString()?.Split(' ')[0] ?? "C:";
            }
            else return;
        }

        if (string.IsNullOrWhiteSpace(_storageDir) || !Directory.Exists(_storageDir))
        {
            OnClickChangeDir();
            if (string.IsNullOrWhiteSpace(_storageDir) || !Directory.Exists(_storageDir)) return;
        }

        var targetWim = Path.Combine(_storageDir, "install.wim");
        if (File.Exists(targetWim))
        {
            var ow = MessageBox.Show($"File install.wim đã tồn tại trong thư mục lưu!\nBạn có muốn ghi đè lên file này không?", "Xác nhận ghi đè", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ow != DialogResult.Yes) return;
        }

        Log($"Bắt đầu Bước 2: Chụp ổ [{selectedDrive}] thành [{targetWim}]...");
        SetUiBusy(true);

        _cts = new CancellationTokenSource();
        try
        {
            await _service.CaptureWimAsync(selectedDrive, targetWim, Log, p =>
            {
                Invoke(() =>
                {
                    _progressBar.Value = Math.Clamp(p, 0, 100);
                });
            }, _cts.Token);

            RefreshStatus();
            MessageBox.Show($"Đã chụp xong ảnh Windows mẫu vào:\n{targetWim}", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("Đã hủy chụp ảnh.");
        }
        catch (Exception ex)
        {
            Log($"[Lỗi chụp WIM]: {ex.Message}");
            MessageBox.Show($"Lỗi chụp WIM: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUiBusy(false);
        }
    }

    private void OnClickStep3a()
    {
        Log("Đang mở trang tải ISO Windows 10 chính thức từ Microsoft...");
        _service.OpenDownloadLink("win10");
    }

    private void OnClickStep3b()
    {
        Log("Đang mở trang tải ISO Windows 11 chính thức từ Microsoft...");
        _service.OpenDownloadLink("win11");
    }

    private async void OnClickStep4()
    {
        var wimPath = Path.Combine(_storageDir, "install.wim");
        if (!File.Exists(wimPath))
        {
            using var ofdWim = new OpenFileDialog
            {
                Title = "Chọn file install.wim của máy mẫu",
                Filter = "Windows Image (*.wim)|*.wim|All files (*.*)|*.*"
            };
            if (ofdWim.ShowDialog(this) == DialogResult.OK)
            {
                wimPath = ofdWim.FileName;
            }
            else
            {
                MessageBox.Show("Bạn cần có file install.wim đã chụp từ Bước 2 trước khi ghép ISO!", "Chưa có WIM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        using var ofdIso = new OpenFileDialog
        {
            Title = "Chọn file ISO Windows 10 hoặc Windows 11 gốc",
            Filter = "Windows ISO Image (*.iso)|*.iso|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(_storageDir) ? _storageDir : "C:\\"
        };

        if (ofdIso.ShowDialog(this) != DialogResult.OK) return;
        var originalIso = ofdIso.FileName;

        bool isWin11 = Path.GetFileName(originalIso).Contains("11", StringComparison.OrdinalIgnoreCase);
        string outputName = isWin11 ? "win11master.iso" : "win10master.iso";
        string outputIso = Path.Combine(_storageDir, outputName);

        Log($"Bắt đầu Bước 4: Nhét [{Path.GetFileName(wimPath)}] vào ISO [{Path.GetFileName(originalIso)}]...");
        SetUiBusy(true);

        _cts = new CancellationTokenSource();
        try
        {
            await _service.BuildMasterIsoAsync(originalIso, wimPath, outputIso, Log, p =>
            {
                Invoke(() =>
                {
                    _progressBar.Value = Math.Clamp(p, 0, 100);
                });
            }, _cts.Token);

            RefreshStatus();
            MessageBox.Show($"[HOÀN TẤT] ĐÃ TẠO XONG FILE ISO MASTER:\n{outputIso}\n\nBạn có thể ghi file ISO này ra USB bằng Rufus/Ventoy để cài đặt cho các máy khác!", "Hoàn Tất Master ISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("Đã hủy ghép ISO.");
        }
        catch (Exception ex)
        {
            Log($"[Lỗi ghép ISO]: {ex.Message}");
            MessageBox.Show($"Lỗi ghép ISO: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUiBusy(false);
        }
    }

    private void OnClickFixSysprep()
    {
        var res = MessageBox.Show(
            "Bạn có muốn tự động sửa các lỗi Sysprep phổ biến không?\n\n" +
            "• Gỡ bỏ ứng dụng UWP gây lỗi 0x80073cf2\n" +
            "• Tắt dịch vụ WMPNetworkSvc\n" +
            "• Tắt mã hóa BitLocker\n" +
            "• Sửa cờ Registry SkipRearm & GeneralizationState\n" +
            "• Xóa sạch Panther logs",
            "Sửa lỗi Sysprep", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (res == DialogResult.Yes)
        {
            Log("=== [BẮT ĐẦU SỬA LỖI SYSPREP] ===");
            try
            {
                _service.FixSysprepErrors(Log);
                MessageBox.Show("Đã sửa xong lỗi Sysprep thành công! Bạn có thể thử chạy lại Bước 1.", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"[Lỗi sửa Sysprep]: {ex.Message}");
            }
        }
    }

    private void OnClickChangeDir()
    {
        using var fbd = new FolderBrowserDialog
        {
            Description = "Chọn thư mục lưu trữ Master Image (install.wim và file ISO)",
            UseDescriptionForTitle = true
        };
        if (Directory.Exists(_storageDir)) fbd.SelectedPath = _storageDir;

        if (fbd.ShowDialog(this) == DialogResult.OK)
        {
            _storageDir = fbd.SelectedPath;
            Log($"Đã đổi chỗ lưu thành: {_storageDir}");
            RefreshStatus();
        }
    }

    private void SetUiBusy(bool busy)
    {
        _progressBar.Visible = busy;
        if (!busy) _progressBar.Value = 0;
        _btnOneClick.Enabled = !busy;
        _btnStep1.Enabled = !busy;
        _btnStep2.Enabled = !busy;
        _btnStep3a.Enabled = !busy;
        _btnStep3b.Enabled = !busy;
        _btnStep4.Enabled = !busy;
        _btnFixSysprep.Enabled = !busy;
        _btnChangeDir.Enabled = !busy;
        _btnRefresh.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void Log(string text)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(text));
            return;
        }

        string time = DateTime.Now.ToString("HH:mm:ss");
        _rtbLog.AppendText($"[{time}] {text}\r\n");
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.ScrollToCaret();
    }

    #endregion

    #region Layout & UI Helpers

    private Button CreateThemedButton(string text, Color bg, Color border, Color textClr, FontStyle style, int height)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = height,
            BackColor = bg,
            ForeColor = textClr,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, style),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 3, 0, 4),
            TextAlign = ContentAlignment.MiddleCenter
        };
        btn.FlatAppearance.BorderColor = border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bg, 0.08f);
        return btn;
    }

    private Button CreateSmallButton(string text, Action action)
    {
        var btn = new Button
        {
            Text = text,
            Height = 32,
            BackColor = _btnSand,
            ForeColor = _textDark,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(3, 0, 0, 0)
        };
        btn.FlatAppearance.BorderColor = _btnSandBorder;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(_btnSand, 0.08f);
        btn.Click += (_, _) => action();
        return btn;
    }

    #endregion
}
