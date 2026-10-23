using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Ernastie.Forms;

public class OfficeConfigForm : Form
{
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
    private Color _dangerRed => Color.FromArgb(220, 38, 38);

    private RichTextBox _rtbLog = null!;
    private TextBox _txtUnpkey = null!;
    private TextBox _txtKmsServer = null!;
    private Label _lblOfficeVersion = null!;
    private Label _lblLicenseStatus = null!;
    private ComboBox _cboDetectedKeys = null!;
    private ProgressBar _progressBar = null!;

    public OfficeConfigForm()
    {
        InitializeComponents();
        ThemeHelper.ApplyTheme(this);
        DetectOfficeInstallation();
    }

    private void InitializeComponents()
    {
        Text = "Thiết Lập & Sửa Lỗi Microsoft Office (Office Diagnostics & Licensing)";
        Size = new Size(880, 720);
        MinimumSize = new Size(820, 640);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9.25f);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = _bgForm
        };

        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text = "THIẾT LẬP & XỬ LÝ SỰ CỐ MICROSOFT OFFICE",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = _textHeading,
            AutoSize = true,
            Location = new Point(0, 4)
        };
        var lblSub = new Label
        {
            Text = "Kiểm tra bản quyền OSPP, xóa key lỗi, kích hoạt, sửa lỗi tài khoản / activation token và chuẩn hóa cấu hình",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _textMuted,
            AutoSize = true,
            Location = new Point(0, 30)
        };
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);
        mainPanel.Controls.Add(pnlHeader);

        // Progress bar
        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 4,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 0,
            Visible = false
        };
        mainPanel.Controls.Add(_progressBar);

        // Split container for Left (Actions) and Right (Logs)
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 470,
            IsSplitterFixed = false,
            BackColor = _borderColor,
            Padding = new Padding(0, 10, 0, 0)
        };
        split.Panel1.BackColor = _bgForm;
        split.Panel2.BackColor = _bgForm;

        // Left Content (Cards container)
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 0, 8, 0)
        };
        split.Panel1.Controls.Add(pnlLeft);

        // Right Content (Logs)
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 0, 0, 0)
        };
        var lblLogHeader = new Label
        {
            Text = "📋 NHẬT KÝ THỰC THI (COMMAND OUTPUT)",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Top,
            Height = 24
        };
        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = ThemeHelper.LogBg,
            ForeColor = ThemeHelper.LogText,
            Font = new Font("Consolas", 8.75f),
            BorderStyle = BorderStyle.None
        };
        var btnClearLog = new Button
        {
            Text = "Xóa nhật ký",
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnClearLog.FlatAppearance.BorderColor = _btnBorder;
        btnClearLog.Click += (_, _) => _rtbLog.Clear();

        pnlRight.Controls.Add(_rtbLog);
        pnlRight.Controls.Add(btnClearLog);
        pnlRight.Controls.Add(lblLogHeader);
        split.Panel2.Controls.Add(pnlRight);

        mainPanel.Controls.Add(split);
        Controls.Add(mainPanel);

        // Build Action Cards on the Left
        BuildCards(pnlLeft);
    }

    private void BuildCards(Panel container)
    {
        int y = 0;

        // Card 1: Trạng thái & Kiểm tra bản quyền OSPP
        var cardStatus = CreateCard("📊 Trạng thái & Kiểm tra Bản quyền", ref y, 150, container);
        _lblOfficeVersion = new Label
        {
            Text = "Phiên bản: Đang kiểm tra...",
            Location = new Point(16, 36),
            AutoSize = true,
            ForeColor = _textHeading,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        _lblLicenseStatus = new Label
        {
            Text = "Trạng thái giấy phép: Chưa quét",
            Location = new Point(16, 58),
            AutoSize = true,
            ForeColor = _textMuted
        };

        var btnCheckStatus = CreateStyledButton("🔍 Kiểm tra bản quyền (OSPP /dstatus)", 16, 92, 240, 36, _primaryBlue, Color.White);
        btnCheckStatus.Click += async (_, _) =>
        {
            try { await RunOsppStatusAsync(); }
            catch (Exception ex) { Log($"[Lỗi kiểm tra]: {ex.Message}"); }
        };

        var btnRefreshInfo = CreateStyledButton("Quét lại thông tin", 264, 92, 140, 36, _btnBg, _textHeading);
        btnRefreshInfo.Click += (_, _) =>
        {
            try { DetectOfficeInstallation(); }
            catch (Exception ex) { Log($"[Lỗi quét Office]: {ex.Message}"); }
        };

        cardStatus.Controls.Add(_lblOfficeVersion);
        cardStatus.Controls.Add(_lblLicenseStatus);
        cardStatus.Controls.Add(btnCheckStatus);
        cardStatus.Controls.Add(btnRefreshInfo);

        // Card 2: Quản lý Product Key & KMS
        var cardKey = CreateCard("Quản lý Product Key & Bản quyền", ref y, 190, container);

        var lblSelectKey = new Label
        {
            Text = "Key cài đặt đã phát hiện (5 ký tự cuối):",
            Location = new Point(16, 34),
            AutoSize = true,
            ForeColor = _textHeading
        };
        _cboDetectedKeys = new ComboBox
        {
            Location = new Point(16, 56),
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = _cardBg,
            ForeColor = _textHeading
        };
        _cboDetectedKeys.SelectedIndexChanged += (_, _) =>
        {
            if (_cboDetectedKeys?.SelectedItem != null && _txtUnpkey != null)
                _txtUnpkey.Text = _cboDetectedKeys.SelectedItem.ToString() ?? "";
        };

        var lblOrManual = new Label
        {
            Text = "Hoặc nhập 5 ký tự:",
            Location = new Point(168, 34),
            AutoSize = true,
            ForeColor = _textMuted
        };
        _txtUnpkey = new TextBox
        {
            Location = new Point(168, 56),
            Width = 110,
            MaxLength = 5,
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            BackColor = _cardBg,
            ForeColor = _textHeading,
            CharacterCasing = CharacterCasing.Upper
        };

        var btnUninstallKey = CreateStyledButton("🗑️ Gỡ bỏ Key này (/unpkey)", 288, 54, 150, 30, _dangerRed, Color.White);
        btnUninstallKey.Click += async (_, _) =>
        {
            try { await UninstallProductKeyAsync(); }
            catch (Exception ex) { Log($"[Lỗi gỡ key]: {ex.Message}"); }
        };

        var lblKms = new Label
        {
            Text = "Máy chủ KMS (Host:Port):",
            Location = new Point(16, 96),
            AutoSize = true,
            ForeColor = _textHeading
        };
        _txtKmsServer = new TextBox
        {
            Location = new Point(16, 118),
            Width = 230,
            Text = "kms8.msguides.com",
            BackColor = _cardBg,
            ForeColor = _textHeading
        };

        var btnSetKms = CreateStyledButton("Gán KMS Host", 254, 116, 115, 28, _btnBg, _textHeading);
        btnSetKms.Click += async (_, _) =>
        {
            try { await SetKmsServerAsync(); }
            catch (Exception ex) { Log($"[Lỗi gán KMS]: {ex.Message}"); }
        };

        var btnActivate = CreateStyledButton("Kích hoạt Office (/act)", 16, 152, 230, 32, _successGreen, Color.White);
        btnActivate.Click += async (_, _) =>
        {
            try { await ActivateOfficeAsync(); }
            catch (Exception ex) { Log($"[Lỗi kích hoạt]: {ex.Message}"); }
        };

        cardKey.Controls.Add(lblSelectKey);
        cardKey.Controls.Add(_cboDetectedKeys);
        cardKey.Controls.Add(lblOrManual);
        cardKey.Controls.Add(_txtUnpkey);
        cardKey.Controls.Add(btnUninstallKey);
        cardKey.Controls.Add(lblKms);
        cardKey.Controls.Add(_txtKmsServer);
        cardKey.Controls.Add(btnSetKms);
        cardKey.Controls.Add(btnActivate);

        // Card 3: Xử lý sự cố & Xóa Token lỗi
        var cardFix = CreateCard("Xử lý sự cố & Dọn Cache tài khoản", ref y, 160, container);

        var btnClearToken = CreateStyledButton("Xóa Cache Token kích hoạt lỗi", 16, 36, 210, 34, _btnBg, _textHeading);
        btnClearToken.Click += (_, _) => ClearOfficeLicensingTokens();

        var btnResetAccount = CreateStyledButton("👤 Xóa Tài khoản đăng nhập bị kẹt", 236, 36, 205, 34, _btnBg, _textHeading);
        btnResetAccount.Click += (_, _) => ResetOfficeAccountCredentials();

        var btnQuickRepair = CreateStyledButton("🔧 Chạy sửa lỗi nhanh (Quick Repair)", 16, 78, 210, 34, _btnBg, _textHeading);
        btnQuickRepair.Click += (_, _) => LaunchOfficeRepair(true);

        var btnOnlineRepair = CreateStyledButton("Sửa lỗi trực tuyến (Online Repair)", 236, 78, 205, 34, _btnBg, _textHeading);
        btnOnlineRepair.Click += (_, _) => LaunchOfficeRepair(false);

        var btnOpenAppwiz = CreateStyledButton("Mở Control Panel gỡ / sửa Office", 16, 120, 425, 30, _btnBg, _textHeading);
        btnOpenAppwiz.Click += (_, _) => Process.Start(new ProcessStartInfo("control.exe", "appwiz.cpl") { UseShellExecute = true });

        cardFix.Controls.Add(btnClearToken);
        cardFix.Controls.Add(btnResetAccount);
        cardFix.Controls.Add(btnQuickRepair);
        cardFix.Controls.Add(btnOnlineRepair);
        cardFix.Controls.Add(btnOpenAppwiz);

        // Card 4: Tinh chỉnh giao diện & Cấu hình chuẩn
        var cardTweak = CreateCard("Tinh chỉnh cấu hình & Font mặc định", ref y, 120, container);

        var btnFixFont = CreateStyledButton("📝 Chuẩn hóa Font Word (Times New Roman 13pt)", 16, 36, 425, 32, _btnBg, _textHeading);
        btnFixFont.Click += (_, _) => StandardizeWordFont();

        var btnDisableHwAccel = CreateStyledButton("🖥️ Tắt tăng tốc đồ họa (Fix lỗi chớp/trắng màn)", 16, 74, 425, 32, _btnBg, _textHeading);
        btnDisableHwAccel.Click += (_, _) => ToggleHardwareAcceleration();

        cardTweak.Controls.Add(btnFixFont);
        cardTweak.Controls.Add(btnDisableHwAccel);

        y += 20;
    }

    private Panel CreateCard(string title, ref int y, int height, Panel parent)
    {
        var card = new Panel
        {
            Location = new Point(0, y),
            Width = parent.ClientSize.Width - 16,
            Height = height,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = _cardBg,
            Padding = new Padding(12)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var lblHeader = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(14, 10),
            AutoSize = true
        };
        card.Controls.Add(lblHeader);

        parent.Controls.Add(card);
        y += height + 12;
        return card;
    }

    private Button CreateStyledButton(string text, int x, int y, int width, int height, Color bg, Color fg)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = bg == _btnBg ? _btnBorder : bg;
        btn.FlatAppearance.MouseOverBackColor = bg == _btnBg ? _btnHover : Color.FromArgb(Math.Min(255, bg.R + 25), Math.Min(255, bg.G + 25), Math.Min(255, bg.B + 25));
        return btn;
    }

    #region Office Engine Operations

    private string? FindOsppVbsPath()
    {
        string[] candidates = {
            @"C:\Program Files\Microsoft Office\Office16\OSPP.VBS",
            @"C:\Program Files (x86)\Microsoft Office\Office16\OSPP.VBS",
            @"C:\Program Files\Microsoft Office\root\Office16\OSPP.VBS",
            @"C:\Program Files (x86)\Microsoft Office\root\Office16\OSPP.VBS",
            @"C:\Program Files\Microsoft Office\Office15\OSPP.VBS",
            @"C:\Program Files (x86)\Microsoft Office\Office15\OSPP.VBS",
            @"C:\Program Files\Microsoft Office\Office14\OSPP.VBS",
            @"C:\Program Files (x86)\Microsoft Office\Office14\OSPP.VBS"
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private void DetectOfficeInstallation()
    {
        try
        {
            var ospp = FindOsppVbsPath();
            if (_lblOfficeVersion != null)
            {
                if (ospp != null)
                {
                    string folder = Path.GetDirectoryName(ospp) ?? "";
                    _lblOfficeVersion.Text = $"Phát hiện: {folder}";
                    _lblOfficeVersion.ForeColor = _successGreen;
                    Log($"[Hệ thống] Tìm thấy OSPP.VBS tại: {ospp}");
                }
                else
                {
                    _lblOfficeVersion.Text = "Không tìm thấy OSPP.VBS (Có thể dùng bản Web hoặc chưa cài)";
                    _lblOfficeVersion.ForeColor = _dangerRed;
                    Log("[Cảnh báo] Không tìm thấy file OSPP.VBS trong các thư mục Office tiêu chuẩn.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Lỗi quét Office]: {ex.Message}");
        }
    }

    private async Task<string> ExecuteCscriptAsync(string arguments)
    {
        var ospp = FindOsppVbsPath();
        if (string.IsNullOrEmpty(ospp) || !File.Exists(ospp))
        {
            Log("[Lỗi] Không tìm thấy OSPP.VBS trên máy này!");
            MessageBox.Show("Không tìm thấy bộ quản lý bản quyền OSPP.VBS của Microsoft Office trên máy tính này.", "Không Tìm Thấy Office", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return string.Empty;
        }

        SetBusy(true);
        Log($"[Chạy] cscript.exe //nologo \"{ospp}\" {arguments}");

        try
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = Path.Combine(Environment.SystemDirectory, "cscript.exe"),
                        Arguments = $"//nologo \"{ospp}\" {arguments}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var p = Process.Start(psi);
                    if (p == null) return "Không thể khởi động cscript.exe";

                    string stdout = p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit(30000);

                    return (stdout + "\n" + stderr).Trim();
                }
                catch (Exception ex)
                {
                    return $"Lỗi thực thi: {ex.Message}";
                }
            });
        }
        catch (Exception ex)
        {
            Log($"[Lỗi ngoại lệ Task]: {ex.Message}");
            return $"Lỗi: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RunOsppStatusAsync()
    {
        try
        {
            if (_cboDetectedKeys != null)
            {
                _cboDetectedKeys.Items.Clear();
            }

            var output = await ExecuteCscriptAsync("/dstatus");
            if (string.IsNullOrWhiteSpace(output))
            {
                if (_lblLicenseStatus != null)
                {
                    _lblLicenseStatus.Text = "Trạng thái: Không nhận được phản hồi từ OSPP";
                    _lblLicenseStatus.ForeColor = _textMuted;
                }
                return;
            }

            Log(output);

            if (_lblLicenseStatus != null)
            {
                if (output.Contains("---LICENSED---"))
                {
                    _lblLicenseStatus.Text = "Trạng thái: ✅ ĐÃ KÍCH HOẠT THÀNH CÔNG (LICENSED)";
                    _lblLicenseStatus.ForeColor = _successGreen;
                }
                else if (output.Contains("---NOTIFICATIONS---"))
                {
                    _lblLicenseStatus.Text = "Trạng thái: ⚠️ HẾT HẠN / CẦN KÍCH HOẠT (NOTIFICATIONS)";
                    _lblLicenseStatus.ForeColor = _dangerRed;
                }
                else if (output.Contains("---GRACE---"))
                {
                    _lblLicenseStatus.Text = "Trạng thái: ⏳ ĐANG DÙNG THỬ (GRACE PERIOD)";
                    _lblLicenseStatus.ForeColor = Color.Orange;
                }
                else
                {
                    _lblLicenseStatus.Text = "Trạng thái: Chưa rõ (Xem chi tiết nhật ký bên phải)";
                    _lblLicenseStatus.ForeColor = _textHeading;
                }
            }

            // Parse last 5 characters
            var regex = new Regex(@"Last 5 characters of installed product key:\s*([A-Z0-9]{5})", RegexOptions.IgnoreCase);
            var matches = regex.Matches(output);
            if (_cboDetectedKeys != null)
            {
                foreach (Match m in matches)
                {
                    if (m != null && m.Groups.Count > 1 && !string.IsNullOrEmpty(m.Groups[1].Value))
                    {
                        string key = m.Groups[1].Value.ToUpper();
                        if (!_cboDetectedKeys.Items.Contains(key))
                        {
                            _cboDetectedKeys.Items.Add(key);
                        }
                    }
                }

                if (_cboDetectedKeys.Items.Count > 0)
                {
                    _cboDetectedKeys.SelectedIndex = 0;
                    Log($"[Nhận diện] Đã tìm thấy {_cboDetectedKeys.Items.Count} Product Key đang gắn trên máy.");
                }
                else
                {
                    Log("[Nhận diện] Không tìm thấy Product Key nào trong trạng thái OSPP.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Lỗi kiểm tra bản quyền]: {ex.Message}");
            MessageBox.Show($"Xảy ra lỗi khi kiểm tra bản quyền Office:\n\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task UninstallProductKeyAsync()
    {
        try
        {
            string key = _txtUnpkey?.Text?.Trim() ?? "";
            if (key.Length != 5)
            {
                MessageBox.Show("Vui lòng nhập chính xác 5 ký tự cuối của Product Key cần gỡ bỏ!", "Lỗi Nhập Liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn gỡ bỏ Product Key kết thúc bằng '{key}' không?\n\nThao tác này giúp xóa key cũ xung đột.", "Xác Nhận Gỡ Key", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            var output = await ExecuteCscriptAsync($"/unpkey:{key}");
            if (string.IsNullOrEmpty(output)) return;
            Log(output);

            if (output.Contains("Product key uninstall successful"))
            {
                MessageBox.Show($"Đã gỡ bỏ thành công Product Key '...{key}'!", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RunOsppStatusAsync();
            }
            else
            {
                MessageBox.Show($"Kết quả gỡ key:\n{output}", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"[Lỗi gỡ Product Key]: {ex.Message}");
            MessageBox.Show($"Lỗi gỡ Product Key: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SetKmsServerAsync()
    {
        try
        {
            string server = _txtKmsServer?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(server))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ máy chủ KMS (VD: kms8.msguides.com)!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var output = await ExecuteCscriptAsync($"/sethst:{server}");
            if (string.IsNullOrEmpty(output)) return;
            Log(output);
            MessageBox.Show($"Đã cấu hình KMS Host: {server}\nXem log chi tiết bên cạnh.", "Gán KMS Host", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi gán KMS Host]: {ex.Message}");
            MessageBox.Show($"Lỗi gán KMS Host: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ActivateOfficeAsync()
    {
        try
        {
            Log("[Kích hoạt] Đang gửi yêu cầu kích hoạt Office qua OSPP /act...");
            var output = await ExecuteCscriptAsync("/act");
            if (string.IsNullOrEmpty(output)) return;
            Log(output);

            if (output.Contains("Product activation successful"))
            {
                MessageBox.Show("🎉 Chúc mừng! Microsoft Office đã kích hoạt thành công (Product activation successful)!", "Kích Hoạt Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RunOsppStatusAsync();
            }
            else
            {
                MessageBox.Show("Quá trình kích hoạt trả về kết quả. Vui lòng kiểm tra mã lỗi trong khung nhật ký.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Log($"[Lỗi kích hoạt Office]: {ex.Message}");
            MessageBox.Show($"Lỗi kích hoạt: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearOfficeLicensingTokens()
    {
        var confirm = MessageBox.Show("Thao tác này sẽ xóa cache token bản quyền và thông tin kích hoạt tạm thời của Office 365 / 2016-2021.\n\nSau khi xóa, bạn cần mở lại Word/Excel để kích hoạt hoặc đăng nhập lại.\nTiếp tục?", "Xóa Token Bản Quyền", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            int deletedCount = 0;
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] targetDirs = {
                Path.Combine(localApp, @"Microsoft\Office\16.0\Licensing"),
                Path.Combine(localApp, @"Microsoft\Office\15.0\Licensing"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\OfficeSoftwareProtectionPlatform")
            };

            foreach (var dir in targetDirs)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                        foreach (var f in files)
                        {
                            try { File.Delete(f); deletedCount++; } catch { }
                        }
                        Log($"[Dọn dẹp] Đã xóa cache trong: {dir}");
                    }
                    catch (Exception ex)
                    {
                        Log($"[Lưu ý] Không thể xóa một số tệp trong {dir}: {ex.Message}");
                    }
                }
            }

            Log($"✅ Hoàn tất xóa token kích hoạt lỗi! Tổng số tệp đã xóa: {deletedCount}");
            MessageBox.Show($"Đã dọn sạch {deletedCount} tệp token bản quyền lỗi của Office!", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi xóa token]: {ex.Message}");
            MessageBox.Show($"Xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetOfficeAccountCredentials()
    {
        var confirm = MessageBox.Show("Thao tác này sẽ xóa các tài khoản Microsoft đăng nhập lỗi đang lưu trong Registry và Windows Credential Manager.\n\nGiúp khắc phục triệt để lỗi 'Account Error' hoặc không lưu được phiên đăng nhập.\nTiếp tục?", "Xóa Tài Khoản Lỗi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            // 1. Clear Registry Identities
            string[] identityKeys = {
                @"Software\Microsoft\Office\16.0\Common\Identity\Identities",
                @"Software\Microsoft\Office\15.0\Common\Identity\Identities"
            };

            foreach (var keyPath in identityKeys)
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
                    if (key != null)
                    {
                        var subKeys = key.GetSubKeyNames();
                        foreach (var sub in subKeys)
                        {
                            try { key.DeleteSubKeyTree(sub); } catch { }
                        }
                        Log($"[Registry] Đã xóa cache tài khoản tại HKCU\\{keyPath}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"[Registry] Lỗi dọn {keyPath}: {ex.Message}");
                }
            }

            // 2. Clear Credential Manager Office entries via cmdkey
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"for /f \"tokens=1,2 delims= \" %i in ('cmdkey /list ^| findstr /i \"MicrosoftOffice\"') do cmdkey /delete:%j\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi)?.WaitForExit(5000);

            Log("✅ Đã dọn sạch thông tin xác thực Microsoft Office trong Windows Credential Manager!");
            MessageBox.Show("Đã xóa sạch tài khoản và token xác thực bị kẹt!\nHãy khởi động lại Word/Excel và đăng nhập tài khoản mới.", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi xóa xác thực]: {ex.Message}");
            MessageBox.Show($"Xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LaunchOfficeRepair(bool quickRepair)
    {
        string typeStr = quickRepair ? "QuickRepair" : "FullRepair";
        Log($"[Sửa lỗi Office] Đang khởi chạy quy trình {typeStr}...");

        string[] possiblePaths = {
            @"C:\Program Files\Common Files\Microsoft Shared\ClickToRun\OfficeClickToRun.exe",
            @"C:\Program Files (x86)\Common Files\Microsoft Shared\ClickToRun\OfficeClickToRun.exe"
        };

        string? clickToRun = possiblePaths.FirstOrDefault(File.Exists);
        if (clickToRun != null)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = clickToRun,
                    Arguments = $"scenario=Repair platform=x64 culture=en-us RepairType={typeStr}",
                    UseShellExecute = true
                };
                Process.Start(psi);
                Log($"[Thành công] Đã kích hoạt công cụ Office Click-To-Run Repair ({typeStr}).");
                return;
            }
            catch (Exception ex)
            {
                Log($"[Lỗi kích hoạt ClickToRun]: {ex.Message}");
            }
        }

        // Fallback: Open Appwiz
        Log("[Hướng dẫn] Mở Control Panel để thực hiện Sửa (Change -> Repair)...");
        Process.Start(new ProcessStartInfo("control.exe", "appwiz.cpl") { UseShellExecute = true });
        MessageBox.Show("Đã mở Control Panel (Programs and Features).\nVui lòng chuột phải vào 'Microsoft Office' -> chọn 'Change' -> chọn 'Quick Repair' hoặc 'Online Repair'.", "Hướng Dẫn Sửa Lỗi Office", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void StandardizeWordFont()
    {
        try
        {
            // Set font registry options for Office Word
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Office\16.0\Word\Options"))
            {
                if (key != null)
                {
                    key.SetValue("DefaultFont", "Times New Roman");
                    key.SetValue("DefaultFontSize", 13);
                }
            }

            Log("✅ Đã thiết lập font chữ chuẩn (Times New Roman 13pt) cho Word trong Registry!");
            MessageBox.Show("Đã cấu hình thông số font chữ chuẩn (Times New Roman, cỡ 13) cho Microsoft Word thành công!", "Chuẩn Hóa Font Chữ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi cấu hình font]: {ex.Message}");
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleHardwareAcceleration()
    {
        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Office\16.0\Common\Graphics"))
            {
                if (key != null)
                {
                    var current = key.GetValue("DisableHardwareAcceleration");
                    bool isDisabled = current is int val && val == 1;

                    if (isDisabled)
                    {
                        key.SetValue("DisableHardwareAcceleration", 0, RegistryValueKind.DWord);
                        Log("✅ Đã BẬT lại Tăng tốc phần cứng đồ họa (Hardware Acceleration) cho Office.");
                        MessageBox.Show("Đã BẬT lại tính năng Tăng tốc đồ họa cho Office.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        key.SetValue("DisableHardwareAcceleration", 1, RegistryValueKind.DWord);
                        Log("✅ Đã TẮT Tăng tốc phần cứng đồ họa (Disable Hardware Acceleration) để khắc phục lỗi màn hình trắng/chớp.");
                        MessageBox.Show("Đã TẮT Tăng tốc đồ họa (Hardware Acceleration) để tránh chớp màn hình khi dùng Excel/Word!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Lỗi cấu hình đồ họa]: {ex.Message}");
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetBusy(bool busy)
    {
        if (IsDisposed || Disposing || _progressBar == null || _progressBar.IsDisposed) return;
        _progressBar.Visible = busy;
        _progressBar.MarqueeAnimationSpeed = busy ? 30 : 0;
    }

    private void Log(string message)
    {
        if (IsDisposed || Disposing) return;

        if (InvokeRequired)
        {
            try
            {
                Invoke(() => Log(message));
            }
            catch { }
            return;
        }

        if (_rtbLog == null || _rtbLog.IsDisposed) return;

        var ts = DateTime.Now.ToString("HH:mm:ss");
        _rtbLog.AppendText($"[{ts}] {message}\n");
        _rtbLog.ScrollToCaret();
    }

    #endregion
}
