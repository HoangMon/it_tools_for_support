using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ernastie.Models;
using Ernastie.Services;

namespace Ernastie.Forms;

public class SettingsForm : Form
{
    private readonly SettingsService _settingsService = SettingsService.Instance;

    // Theme Colors
    private static readonly Color BgLight = Color.FromArgb(248, 250, 252);
    private static readonly Color CardBgLight = Color.White;
    private static readonly Color CardBorderLight = Color.FromArgb(226, 232, 240);
    private static readonly Color TextHeadingLight = Color.FromArgb(15, 23, 42);
    private static readonly Color TextMutedLight = Color.FromArgb(71, 85, 105);

    private static readonly Color BgDark = Color.FromArgb(11, 14, 20);
    private static readonly Color CardBgDark = Color.FromArgb(17, 23, 34);
    private static readonly Color CardBorderDark = Color.FromArgb(32, 42, 60);
    private static readonly Color TextHeadingDark = Color.FromArgb(241, 245, 249);
    private static readonly Color TextMutedDark = Color.FromArgb(148, 163, 184);

    private static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);

    private Panel _pnlHeader = null!;
    private Panel _pnlBottom = null!;
    private Panel _pnlBody = null!;
    private Label _lblTitle = null!;
    private Label _lblSub = null!;

    private GroupBox _grpTheme = null!;
    private RadioButton _radLightTheme = null!;
    private RadioButton _radDarkTheme = null!;
    private Button _btnApplyTheme = null!;

    private GroupBox _grpLang = null!;
    private RadioButton _radLangVi = null!;
    private RadioButton _radLangEn = null!;
    private Button _btnApplyLang = null!;

    private GroupBox _grpApp = null!;
    private Label _lblAppDesc = null!;
    private TextBox _txtAppFolder = null!;
    private Button _btnBrowseApp = null!;
    private Button _btnOpenApp = null!;
    private Button _btnDefaultApp = null!;

    private GroupBox _grpDriver = null!;
    private Label _lblDriverDesc = null!;
    private TextBox _txtDriverFolder = null!;
    private Button _btnBrowseDriver = null!;
    private Button _btnOpenDriver = null!;
    private Button _btnDefaultDriver = null!;

    private Button _btnSave = null!;
    private Button _btnClose = null!;

    public SettingsForm()
    {
        InitializeUi();
        LoadCurrentSettings();
    }

    private void InitializeUi()
    {
        Text = "Cài Đặt Hệ Thống / Application Settings";
        Size = new Size(680, 680);
        MinimumSize = new Size(640, 640);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);

        // Top Header
        _pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(20, 14, 20, 10)
        };
        _pnlHeader.Paint += (s, e) =>
        {
            var border = _radDarkTheme != null && _radDarkTheme.Checked ? CardBorderDark : CardBorderLight;
            using var pen = new Pen(border, 1);
            e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
        };

        _lblTitle = new Label
        {
            Text = "CÀI ĐẶT HỆ THỐNG / APPLICATION SETTINGS",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Dock = DockStyle.Top,
            AutoSize = true
        };

        _lblSub = new Label
        {
            Text = "Tùy chỉnh chế độ màu sáng/tối toàn diện, ngôn ngữ và đường dẫn thư mục",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
            Dock = DockStyle.Bottom,
            AutoSize = true
        };

        _pnlHeader.Controls.Add(_lblTitle);
        _pnlHeader.Controls.Add(_lblSub);
        Controls.Add(_pnlHeader);

        // Bottom Action Bar
        _pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            Padding = new Padding(20, 12, 20, 12)
        };
        _pnlBottom.Paint += (s, e) =>
        {
            var border = _radDarkTheme != null && _radDarkTheme.Checked ? CardBorderDark : CardBorderLight;
            using var pen = new Pen(border, 1);
            e.Graphics.DrawLine(pen, 0, 0, _pnlBottom.Width, 0);
        };

        _btnSave = new Button
        {
            Text = "Lưu & Đóng",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = PrimaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(140, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += (_, _) => SaveAndClose();

        _btnClose = new Button
        {
            Text = "Đóng",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 38),
            Dock = DockStyle.Right,
            Margin = new Padding(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        _btnClose.Click += (_, _) => Close();

        var pnlBottomInner = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        pnlBottomInner.Controls.Add(_btnSave);
        pnlBottomInner.Controls.Add(_btnClose);
        _pnlBottom.Controls.Add(pnlBottomInner);
        Controls.Add(_pnlBottom);

        // Scrollable Body
        _pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20, 16, 20, 16)
        };
        Controls.Add(_pnlBody);
        _pnlBody.BringToFront();

        int curY = 10;

        // --- 1. THEME MODE CARD (EXACTLY 2 CHOICES + APPLY BUTTON) ---
        _grpTheme = CreateCardGroup("Chế độ màu giao diện (Theme Mode)", ref curY, 90, _pnlBody);

        _radLightTheme = new RadioButton
        {
            Text = "Sáng (Light Mode)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(20, 36),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        _radDarkTheme = new RadioButton
        {
            Text = "Tối (Dark Mode)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(180, 36),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        _btnApplyTheme = new Button
        {
            Text = "Áp dụng ngay",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            BackColor = PrimaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(360, 30),
            Size = new Size(220, 36),
            Cursor = Cursors.Hand
        };
        _btnApplyTheme.FlatAppearance.BorderSize = 0;
        _btnApplyTheme.Click += (_, _) => ApplyThemeOnly();

        _grpTheme.Controls.Add(_radLightTheme);
        _grpTheme.Controls.Add(_radDarkTheme);
        _grpTheme.Controls.Add(_btnApplyTheme);

        // --- 2. LANGUAGE CARD ---
        _grpLang = CreateCardGroup("Ngôn ngữ hiển thị (Language)", ref curY, 90, _pnlBody);

        _radLangVi = new RadioButton
        {
            Text = "Tiếng Việt (Mặc định)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(20, 36),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        _radLangEn = new RadioButton
        {
            Text = "English (International)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(180, 36),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        _btnApplyLang = new Button
        {
            Text = "Áp dụng ngay",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            BackColor = PrimaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(360, 30),
            Size = new Size(220, 36),
            Cursor = Cursors.Hand
        };
        _btnApplyLang.FlatAppearance.BorderSize = 0;
        _btnApplyLang.Click += (_, _) => ApplyLanguageOnly();

        _grpLang.Controls.Add(_radLangVi);
        _grpLang.Controls.Add(_radLangEn);
        _grpLang.Controls.Add(_btnApplyLang);

        // --- 3. APP FOLDER CARD ---
        _grpApp = CreateCardGroup("Thư mục ứng dụng (Application Folder)", ref curY, 110, _pnlBody);

        _lblAppDesc = new Label
        {
            Text = "Thư mục cài đặt và chứa tài nguyên hệ thống của E-TOOLBOX:",
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(16, 28),
            AutoSize = true
        };

        _txtAppFolder = new TextBox
        {
            Location = new Point(16, 52),
            Size = new Size(390, 26),
            Font = new Font("Segoe UI", 9f)
        };

        _btnBrowseApp = CreateActionButton("Duyệt...", new Point(414, 50), () =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục ứng dụng",
                SelectedPath = Directory.Exists(_txtAppFolder.Text) ? _txtAppFolder.Text : AppDomain.CurrentDomain.BaseDirectory
            };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                _txtAppFolder.Text = fbd.SelectedPath;
            }
        });

        _btnOpenApp = CreateActionButton("Mở thư mục", new Point(494, 50), () =>
        {
            var p = _txtAppFolder.Text.Trim();
            if (Directory.Exists(p)) Process.Start("explorer.exe", p);
            else MessageBox.Show("Thư mục không tồn tại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        });

        _btnDefaultApp = CreateActionButton("Mặc định", new Point(560, 50), () =>
        {
            _txtAppFolder.Text = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
        });

        _grpApp.Controls.Add(_lblAppDesc);
        _grpApp.Controls.Add(_txtAppFolder);
        _grpApp.Controls.Add(_btnBrowseApp);
        _grpApp.Controls.Add(_btnOpenApp);
        _grpApp.Controls.Add(_btnDefaultApp);

        // --- 4. DRIVER FOLDER CARD ---
        _grpDriver = CreateCardGroup("Thư mục sao lưu Driver (Driver Backup Folder)", ref curY, 110, _pnlBody);

        _lblDriverDesc = new Label
        {
            Text = "Thư mục mặc định dùng để sao lưu và nạp phục hồi toàn bộ Driver phần cứng:",
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(16, 28),
            AutoSize = true
        };

        _txtDriverFolder = new TextBox
        {
            Location = new Point(16, 52),
            Size = new Size(390, 26),
            Font = new Font("Segoe UI", 9f)
        };

        _btnBrowseDriver = CreateActionButton("Duyệt...", new Point(414, 50), () =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục lưu trữ Driver",
                SelectedPath = Directory.Exists(_txtDriverFolder.Text) ? _txtDriverFolder.Text : (Directory.Exists(@"D:\") ? @"D:\" : @"C:\")
            };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                _txtDriverFolder.Text = fbd.SelectedPath;
            }
        });

        _btnOpenDriver = CreateActionButton("Mở thư mục", new Point(494, 50), () =>
        {
            var p = _txtDriverFolder.Text.Trim();
            if (!Directory.Exists(p))
            {
                try { Directory.CreateDirectory(p); } catch { }
            }
            if (Directory.Exists(p)) Process.Start("explorer.exe", p);
            else MessageBox.Show("Thư mục không tồn tại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        });

        _btnDefaultDriver = CreateActionButton("Mặc định", new Point(560, 50), () =>
        {
            _txtDriverFolder.Text = Directory.Exists(@"D:\") ? @"D:\backupdriver" : @"C:\backupdriver";
        });

        _grpDriver.Controls.Add(_lblDriverDesc);
        _grpDriver.Controls.Add(_txtDriverFolder);
        _grpDriver.Controls.Add(_btnBrowseDriver);
        _grpDriver.Controls.Add(_btnOpenDriver);
        _grpDriver.Controls.Add(_btnDefaultDriver);
    }

    private GroupBox CreateCardGroup(string title, ref int y, int height, Panel parent)
    {
        var grp = new GroupBox
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(10, y),
            Size = new Size(parent.ClientSize.Width - 40, height),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        parent.Controls.Add(grp);
        y += height + 16;
        return grp;
    }

    private Button CreateActionButton(string text, Point loc, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = new Size(68, 28),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private void LoadCurrentSettings()
    {
        var s = _settingsService.Settings;

        if (s.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase))
        {
            _radLightTheme.Checked = true;
        }
        else
        {
            _radDarkTheme.Checked = true;
        }

        if (s.Language.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            _radLangEn.Checked = true;
        }
        else
        {
            _radLangVi.Checked = true;
        }

        _txtAppFolder.Text = s.AppFolder;
        _txtDriverFolder.Text = s.DriverFolder;

        ApplyFormTheme(s.Theme);
        ApplyFormLanguage(s.Language);
    }

    private void ApplyThemeOnly()
    {
        var s = _settingsService.Settings;
        s.Theme = _radLightTheme.Checked ? "Light" : "Dark";
        _settingsService.SaveSettings(s);

        ApplyFormTheme(s.Theme);

        bool isEn = s.Language.Equals("en", StringComparison.OrdinalIgnoreCase);
        string modeName = (s.Theme == "Light") 
            ? (isEn ? "LIGHT" : "SÁNG") 
            : (isEn ? "DARK" : "TỐI");
        string msg = isEn 
            ? $"Switched application interface to {modeName} mode successfully!" 
            : $"Đã chuyển toàn bộ giao diện sang chế độ {modeName} thành công!";
        string title = isEn ? "Apply Theme" : "Áp Dụng Giao Diện";
        MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ApplyLanguageOnly()
    {
        var s = _settingsService.Settings;
        s.Language = _radLangEn.Checked ? "en" : "vi";
        _settingsService.SaveSettings(s);

        ApplyFormLanguage(s.Language);

        bool isEn = s.Language.Equals("en", StringComparison.OrdinalIgnoreCase);
        string msg = isEn
            ? "Successfully applied English language to the entire toolbox!"
            : "Đã chuyển toàn bộ giao diện E-TOOLBOX sang Tiếng Việt thành công!";
        string title = isEn ? "Apply Language" : "Áp Dụng Ngôn Ngữ";
        MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ApplyFormLanguage(string lang)
    {
        bool isEn = lang.Equals("en", StringComparison.OrdinalIgnoreCase);

        Text = isEn ? "Application Settings - E-TOOLBOX" : "Cài Đặt Hệ Thống / Application Settings";
        _lblTitle.Text = isEn ? "APPLICATION SETTINGS" : "CÀI ĐẶT HỆ THỐNG / APPLICATION SETTINGS";
        _lblSub.Text = isEn
            ? "Customize light/dark theme, system language, and directory paths"
            : "Tùy chỉnh chế độ màu sáng/tối toàn diện, ngôn ngữ và đường dẫn thư mục";

        _grpTheme.Text = isEn ? "Theme Mode" : "Chế độ màu giao diện (Theme Mode)";
        _radLightTheme.Text = isEn ? "Light (Light Mode)" : "Sáng (Light Mode)";
        _radDarkTheme.Text = isEn ? "Dark (Dark Mode)" : "Tối (Dark Mode)";
        _btnApplyTheme.Text = isEn ? "Apply Theme Now" : "Áp dụng Giao diện ngay";

        _grpLang.Text = isEn ? "Display Language" : "Ngôn ngữ hiển thị (Language)";
        _radLangVi.Text = isEn ? "Vietnamese (Tiếng Việt)" : "Tiếng Việt (Mặc định)";
        _radLangEn.Text = isEn ? "English (International)" : "English (International)";
        _btnApplyLang.Text = isEn ? "Apply Language Now" : "Áp dụng Ngôn ngữ ngay";

        _grpApp.Text = isEn ? "Application Folder" : "Thư mục ứng dụng (Application Folder)";
        _lblAppDesc.Text = isEn
            ? "Installation and system resource directory for E-TOOLBOX:"
            : "Thư mục cài đặt và chứa tài nguyên hệ thống của E-TOOLBOX:";
        if (_btnBrowseApp != null) _btnBrowseApp.Text = isEn ? "Browse..." : "Duyệt...";
        if (_btnOpenApp != null) _btnOpenApp.Text = isEn ? "Open" : "Mở thư mục";
        if (_btnDefaultApp != null) _btnDefaultApp.Text = isEn ? "Default" : "Mặc định";

        _grpDriver.Text = isEn ? "Driver Backup Folder" : "Thư mục sao lưu Driver (Driver Backup Folder)";
        _lblDriverDesc.Text = isEn
            ? "Default directory used to backup and restore hardware drivers:"
            : "Thư mục mặc định dùng để sao lưu và nạp phục hồi toàn bộ Driver phần cứng:";
        if (_btnBrowseDriver != null) _btnBrowseDriver.Text = isEn ? "Browse..." : "Duyệt...";
        if (_btnOpenDriver != null) _btnOpenDriver.Text = isEn ? "Open" : "Mở thư mục";
        if (_btnDefaultDriver != null) _btnDefaultDriver.Text = isEn ? "Default" : "Mặc định";

        _btnSave.Text = isEn ? "Save & Close" : "Lưu & Đóng";
        _btnClose.Text = isEn ? "Close" : "Đóng";
    }

    private void ApplyFormTheme(string theme)
    {
        bool isLight = theme.Equals("Light", StringComparison.OrdinalIgnoreCase);

        Color bg = isLight ? BgLight : BgDark;
        Color card = isLight ? CardBgLight : CardBgDark;
        Color border = isLight ? CardBorderLight : CardBorderDark;
        Color textHead = isLight ? TextHeadingLight : TextHeadingDark;
        Color textMuted = isLight ? TextMutedLight : TextMutedDark;
        Color btnBg = isLight ? Color.FromArgb(241, 245, 249) : Color.FromArgb(24, 34, 52);
        Color inputBg = isLight ? Color.White : Color.FromArgb(20, 28, 42);

        BackColor = bg;
        _pnlHeader.BackColor = card;
        _lblTitle.ForeColor = textHead;
        _lblSub.ForeColor = textMuted;

        _pnlBottom.BackColor = card;
        _btnClose.BackColor = btnBg;
        _btnClose.ForeColor = textHead;
        _btnClose.FlatAppearance.BorderColor = border;

        _pnlBody.BackColor = bg;

        var groups = new[] { _grpTheme, _grpLang, _grpApp, _grpDriver };
        foreach (var g in groups)
        {
            if (g == null) continue;
            g.BackColor = card;
            g.ForeColor = textHead;
        }

        _radLightTheme.ForeColor = textHead;
        _radDarkTheme.ForeColor = textHead;
        _radLangVi.ForeColor = textHead;
        _radLangEn.ForeColor = textHead;

        _lblAppDesc.ForeColor = textMuted;
        _lblDriverDesc.ForeColor = textMuted;

        _txtAppFolder.BackColor = inputBg;
        _txtAppFolder.ForeColor = textHead;
        _txtDriverFolder.BackColor = inputBg;
        _txtDriverFolder.ForeColor = textHead;

        _btnApplyTheme.BackColor = PrimaryBlue;
        _btnApplyTheme.ForeColor = Color.White;
        if (_btnApplyLang != null)
        {
            _btnApplyLang.BackColor = PrimaryBlue;
            _btnApplyLang.ForeColor = Color.White;
        }

        // Action buttons
        foreach (Control c in _grpApp.Controls)
        {
            if (c is Button b && b != _btnApplyTheme && b != _btnApplyLang)
            {
                b.BackColor = btnBg;
                b.ForeColor = textHead;
                b.FlatAppearance.BorderColor = border;
            }
        }
        foreach (Control c in _grpDriver.Controls)
        {
            if (c is Button b && b != _btnApplyTheme && b != _btnApplyLang)
            {
                b.BackColor = btnBg;
                b.ForeColor = textHead;
                b.FlatAppearance.BorderColor = border;
            }
        }

        _pnlHeader.Invalidate();
        _pnlBottom.Invalidate();
    }

    private void SaveAndClose()
    {
        var newSettings = new AppSettingsModel
        {
            Theme = _radLightTheme.Checked ? "Light" : "Dark",
            Language = _radLangEn.Checked ? "en" : "vi",
            AppFolder = string.IsNullOrWhiteSpace(_txtAppFolder.Text) ? AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/') : _txtAppFolder.Text.Trim(),
            DriverFolder = string.IsNullOrWhiteSpace(_txtDriverFolder.Text) ? (Directory.Exists(@"D:\") ? @"D:\backupdriver" : @"C:\backupdriver") : _txtDriverFolder.Text.Trim()
        };

        try
        {
            if (!Directory.Exists(newSettings.DriverFolder))
            {
                Directory.CreateDirectory(newSettings.DriverFolder);
            }
        }
        catch { }

        _settingsService.SaveSettings(newSettings);

        DialogResult = DialogResult.OK;
        Close();
    }
}
