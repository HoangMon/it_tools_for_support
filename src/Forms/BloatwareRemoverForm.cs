using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Ernastie.Forms;

public class BloatwareRemoverForm : Form
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
    private Color _colorGreen => ThemeHelper.SuccessGreen;
    private Color _colorRed => ThemeHelper.DangerRed;

    // Left Panel Bloatware model
    private class BloatwareItem
    {
        public string DisplayName { get; }
        public int Column { get; }
        public string[] PackagePatterns { get; }
        public CheckBox CheckBox { get; set; } = null!;
        public bool IsInstalled { get; set; }

        public BloatwareItem(string displayName, int column, string[] packagePatterns)
        {
            DisplayName = displayName;
            Column = column;
            PackagePatterns = packagePatterns;
        }
    }

    // Right Panel OEM Software model
    private class OemItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public CheckBox CheckBox { get; set; } = null!;
        public Label SubtitleLabel { get; set; } = null!;
        public bool IsInstalled { get; set; }
        public string? DetectedUninstallCmd { get; set; }
        public string? OfficialToolUrl { get; set; }
        public string? OfficialToolExeName { get; set; }

        public OemItem(string id, string displayName, string? toolUrl = null, string? toolExe = null)
        {
            Id = id;
            DisplayName = displayName;
            OfficialToolUrl = toolUrl;
            OfficialToolExeName = toolExe;
        }
    }

    private readonly List<BloatwareItem> _bloatwareApps = new();
    private readonly List<OemItem> _oemItems = new();

    // UI Controls
    private Panel _pnlLeft = null!;
    private Panel _pnlRight = null!;
    private CheckBox _chkSelectAll = null!;
    private Label _lblCountGreen = null!;
    private Label _lblCountRed = null!;
    private CheckBox _chkPreventReinstall = null!;
    private Button _btnStartBloatware = null!;
    private Button _btnStartOem = null!;
    private Button _btnOpenBatch = null!;

    private bool _isUpdatingCheckAll = false;

    public BloatwareRemoverForm()
    {
        InitializeBloatwareDefinitions();
        InitializeOemDefinitions();
        InitializeComponents();
        _ = LoadStatusAsync();
    }

    private void InitializeBloatwareDefinitions()
    {
        // Column 1 (15 items)
        _bloatwareApps.Add(new BloatwareItem("Xbox Game Bar", 0, new[] { "XboxGamingOverlay", "XboxGameOverlay", "XboxApp", "XboxSpeechToTextOverlay" }));
        _bloatwareApps.Add(new BloatwareItem("Your Phone (Phone Link)", 0, new[] { "YourPhone", "PhoneLink" }));
        _bloatwareApps.Add(new BloatwareItem("3D Viewer", 0, new[] { "Microsoft3DViewer" }));
        _bloatwareApps.Add(new BloatwareItem("OneNote", 0, new[] { "Office.OneNote", "OneNote" }));
        _bloatwareApps.Add(new BloatwareItem("People", 0, new[] { "Microsoft.People", "PeopleExperienceHost" }));
        _bloatwareApps.Add(new BloatwareItem("Solitaire Collection", 0, new[] { "MicrosoftSolitaireCollection", "Solitaire" }));
        _bloatwareApps.Add(new BloatwareItem("Sticky Notes", 0, new[] { "MicrosoftStickyNotes" }));
        _bloatwareApps.Add(new BloatwareItem("Tips", 0, new[] { "Getstarted" }));
        _bloatwareApps.Add(new BloatwareItem("Weather", 0, new[] { "BingWeather" }));
        _bloatwareApps.Add(new BloatwareItem("Maps", 0, new[] { "WindowsMaps" }));
        _bloatwareApps.Add(new BloatwareItem("News", 0, new[] { "BingNews" }));
        _bloatwareApps.Add(new BloatwareItem("Paint 3D", 0, new[] { "MSPaint", "Paint3D" }));
        _bloatwareApps.Add(new BloatwareItem("Skype", 0, new[] { "SkypeApp" }));
        _bloatwareApps.Add(new BloatwareItem("Camera", 0, new[] { "WindowsCamera" }));
        _bloatwareApps.Add(new BloatwareItem("Alarms & Clock", 0, new[] { "WindowsAlarms" }));

        // Column 2 (14 items)
        _bloatwareApps.Add(new BloatwareItem("Mail and Calendar", 1, new[] { "windowscommunicationsapps" }));
        _bloatwareApps.Add(new BloatwareItem("Voice Recorder", 1, new[] { "WindowsSoundRecorder" }));
        _bloatwareApps.Add(new BloatwareItem("Outlook", 1, new[] { "OutlookForWindows" }));
        _bloatwareApps.Add(new BloatwareItem("Office (ứng dụng giới thiệu Office)", 1, new[] { "MicrosoftOfficeHub" }));
        _bloatwareApps.Add(new BloatwareItem("Get Help", 1, new[] { "GetHelp" }));
        _bloatwareApps.Add(new BloatwareItem("Feedback Hub", 1, new[] { "WindowsFeedbackHub" }));
        _bloatwareApps.Add(new BloatwareItem("Groove Music", 1, new[] { "ZuneMusic" }));
        _bloatwareApps.Add(new BloatwareItem("Movies & TV", 1, new[] { "ZuneVideo" }));
        _bloatwareApps.Add(new BloatwareItem("Microsoft Bing", 1, new[] { "BingSearch" }));
        _bloatwareApps.Add(new BloatwareItem("Mixed Reality Portal", 1, new[] { "MixedReality.Portal" }));
        _bloatwareApps.Add(new BloatwareItem("Power Automate", 1, new[] { "PowerAutomateDesktop" }));
        _bloatwareApps.Add(new BloatwareItem("To Do", 1, new[] { "Todos" }));
        _bloatwareApps.Add(new BloatwareItem("Cortana", 1, new[] { "549981C3F10110", "Cortana" }));
        _bloatwareApps.Add(new BloatwareItem("Clipchamp", 1, new[] { "Clipchamp" }));
    }

    private void InitializeOemDefinitions()
    {
        _oemItems.Add(new OemItem("mcafee", "McAfee (mọi thành phần)",
            "https://download.mcafee.com/molbin/iss-loc/SupportTools/MCPR/MCPR.exe", "MCPR.exe"));
        _oemItems.Add(new OemItem("bkav", "Bkav"));
        _oemItems.Add(new OemItem("avg", "AVG Antivirus",
            "https://files-download.avg.com/util/tools/AVG_Clear.exe", "AVG_Clear.exe"));
        _oemItems.Add(new OemItem("avast", "Avast Antivirus",
            "https://files.avast.com/iavs9x/avastclear.exe", "avastclear.exe"));
        _oemItems.Add(new OemItem("wps", "WPS Office"));
        _oemItems.Add(new OemItem("360", "360 Total Security"));
        _oemItems.Add(new OemItem("opera", "Opera / Opera GX"));
    }

    private void InitializeComponents()
    {
        Text = "Gỡ sạch Bloatware";
        Size = new Size(950, 620);
        MinimumSize = new Size(920, 590);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9.25f);
        DoubleBuffered = true;

        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);
            else Icon = SystemIcons.Application;
        }
        catch
        {
            Icon = SystemIcons.Application;
        }

        var mainContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = _bgForm
        };
        Controls.Add(mainContainer);

        // Build Left Card (App & Công cụ)
        _pnlLeft = BuildLeftCard();
        mainContainer.Controls.Add(_pnlLeft);

        // Build Right Card (Phần mềm hãng cài sẵn)
        _pnlRight = BuildRightCard();
        mainContainer.Controls.Add(_pnlRight);

        // Responsive resizing
        mainContainer.Resize += (_, _) => LayoutPanels(mainContainer);
        LayoutPanels(mainContainer);
        ThemeHelper.ApplyTheme(this);
    }

    private void LayoutPanels(Panel container)
    {
        int availableW = container.ClientSize.Width - container.Padding.Horizontal - 10;
        int availableH = container.ClientSize.Height - container.Padding.Vertical;

        int rightW = 318;
        int leftW = availableW - rightW;
        if (leftW < 530)
        {
            leftW = 530;
            rightW = Math.Max(260, availableW - leftW);
        }

        _pnlLeft.Location = new Point(container.Padding.Left, container.Padding.Top);
        _pnlLeft.Size = new Size(leftW, availableH);

        _pnlRight.Location = new Point(container.Padding.Left + leftW + 10, container.Padding.Top);
        _pnlRight.Size = new Size(rightW, availableH);
    }

    private Panel BuildLeftCard()
    {
        var card = new Panel
        {
            BackColor = _cardBg,
            Padding = new Padding(14, 10, 14, 10)
        };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // Header: "📦 App & Công cụ" and "[ ] Tất cả"
        var lblTitle = new Label
        {
            Text = "Ứng dụng mặc định & Công cụ (UWP Apps)",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(14, 10),
            AutoSize = true
        };
        card.Controls.Add(lblTitle);

        _chkSelectAll = new CheckBox
        {
            Text = "Tất cả",
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textHeading,
            Location = new Point(210, 11),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        _chkSelectAll.CheckedChanged += (_, _) =>
        {
            if (_isUpdatingCheckAll) return;
            _isUpdatingCheckAll = true;
            bool check = _chkSelectAll.Checked;
            foreach (var item in _bloatwareApps)
            {
                item.CheckBox.Checked = check;
            }
            _isUpdatingCheckAll = false;
        };
        card.Controls.Add(_chkSelectAll);

        // Two columns of bloatware apps
        int startY = 40;
        int rowH = 26;

        int col1X = 14;
        int col2X = 280;

        int r1 = 0;
        int r2 = 0;

        foreach (var app in _bloatwareApps)
        {
            var chk = new CheckBox
            {
                Text = app.DisplayName,
                Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
                ForeColor = _colorRed, // default red until detected
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            if (app.Column == 0)
            {
                chk.Location = new Point(col1X, startY + r1 * rowH);
                r1++;
            }
            else
            {
                chk.Location = new Point(col2X, startY + r2 * rowH);
                r2++;
            }

            chk.CheckedChanged += (_, _) =>
            {
                if (_isUpdatingCheckAll) return;
                _isUpdatingCheckAll = true;
                _chkSelectAll.Checked = _bloatwareApps.All(a => a.CheckBox.Checked);
                _isUpdatingCheckAll = false;
            };

            app.CheckBox = chk;
            card.Controls.Add(chk);
        }

        // Summary Labels: "Xanh lá = vẫn còn trên máy (X)"   "Đỏ = đã gỡ rồi (Y)"
        int statusY = startY + Math.Max(r1, r2) * rowH + 8;

        _lblCountGreen = new Label
        {
            Text = "Xanh lá = vẫn còn trên máy (0)",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _colorGreen,
            Location = new Point(col1X, statusY),
            AutoSize = true
        };
        card.Controls.Add(_lblCountGreen);

        _lblCountRed = new Label
        {
            Text = "Đỏ = đã gỡ rồi (29)",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _colorRed,
            Location = new Point(col2X, statusY),
            AutoSize = true
        };
        card.Controls.Add(_lblCountRed);

        // Bottom Controls: "[ ] 🚫 Chặn tự cài lại Bloatware" & "[ 🗑️ Bắt đầu ]"
        int bottomY = statusY + 36;

        _chkPreventReinstall = new CheckBox
        {
            Text = "Chặn tự động tải lại Bloatware",
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textHeading,
            Location = new Point(col1X, bottomY + 5),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_chkPreventReinstall);

        _btnStartBloatware = CreateStyledButton("Bắt đầu gỡ bỏ", new Point(col2X, bottomY), new Size(230, 36), true);
        _btnStartBloatware.Click += async (_, _) => await RemoveSelectedBloatwareAsync();
        card.Controls.Add(_btnStartBloatware);

        return card;
    }

    private Panel BuildRightCard()
    {
        var card = new Panel
        {
            BackColor = _cardBg,
            Padding = new Padding(14, 10, 14, 10)
        };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // Header
        var lblTitle = new Label
        {
            Text = "Phần mềm nhà sản xuất (OEM Software)",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(14, 10),
            AutoSize = true
        };
        card.Controls.Add(lblTitle);

        // 7 OEM Items with subtitle line underneath
        int itemY = 40;
        int itemH = 43;

        foreach (var item in _oemItems)
        {
            var chk = new CheckBox
            {
                Text = item.DisplayName,
                Font = new Font("Segoe UI", 9.25f),
                ForeColor = _textMuted,
                Location = new Point(14, itemY),
                Size = new Size(285, 20),
                Cursor = Cursors.Hand
            };

            var lblSub = new Label
            {
                Text = "— không thấy trên máy",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = _textMuted,
                Location = new Point(32, itemY + 20),
                Size = new Size(265, 18)
            };

            item.CheckBox = chk;
            item.SubtitleLabel = lblSub;

            card.Controls.Add(chk);
            card.Controls.Add(lblSub);

            itemY += itemH;
        }

        // Advisory Note
        var lblNote = new Label
        {
            Text = "Gỡ bằng trình gỡ của chính hãng.\nDiệt virus tự bảo vệ, có thể gỡ không hết:\nlúc đó tool tự tải công cụ gỡ của hãng.\nNhớ đóng Opera trước khi gỡ nó.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(68, 64, 60),
            Location = new Point(14, itemY + 6),
            Size = new Size(285, 68)
        };
        card.Controls.Add(lblNote);

        // Button 1: Gỡ mục đã chọn
        int btn1Y = itemY + 80;
        _btnStartOem = CreateStyledButton("Gỡ bỏ mục đã chọn", new Point(14, btn1Y), new Size(285, 36), true);
        _btnStartOem.Click += async (_, _) => await RemoveSelectedOemAsync();
        card.Controls.Add(_btnStartOem);

        // Button 2: Gỡ nhiều phần mềm khác...
        int btn2Y = btn1Y + 42;
        _btnOpenBatch = CreateStyledButton("Gỡ phần mềm hàng loạt (Control Panel)...", new Point(14, btn2Y), new Size(285, 36), false);
        _btnOpenBatch.Click += (_, _) =>
        {
            try
            {
                using var batchForm = new BatchUninstallerForm();
                batchForm.ShowDialog(this);
                _ = LoadStatusAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở trình gỡ nhiều phần mềm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        card.Controls.Add(_btnOpenBatch);

        return card;
    }

    private Button CreateStyledButton(string text, Point loc, Size size, bool isBold)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            Font = new Font("Segoe UI", isBold ? 9.5f : 9.25f, isBold ? FontStyle.Bold : FontStyle.Regular),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderColor = _btnBorder;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = _btnHover;
        return btn;
    }

    #region Detection Engine

    private async Task LoadStatusAsync()
    {
        try
        {
            // Check if prevent reinstall is already enabled
            bool preventEnabled = CheckPreventReinstallEnabled();
            if (!IsDisposed && IsHandleCreated)
            {
                Invoke(() => _chkPreventReinstall.Checked = preventEnabled);
            }

            // 1. Scan UWP AppX packages installed on machine
            var installedPackages = await Task.Run(() =>
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var tempPs1 = Path.Combine(Path.GetTempPath(), $"scan_appx_{Guid.NewGuid():N}.ps1");
                try
                {
                    File.WriteAllText(tempPs1, @"$pkgs = @()
try { $pkgs += (Get-AppxPackage | Select-Object -ExpandProperty Name) } catch {}
try { $pkgs += (Get-AppxPackage -AllUsers | Select-Object -ExpandProperty Name) } catch {}
try { $pkgs += (Get-AppxProvisionedPackage -Online | Select-Object -ExpandProperty DisplayName) } catch {}
$pkgs | Sort-Object -Unique | Out-String", Encoding.UTF8);

                    var psi = new ProcessStartInfo("powershell.exe",
                        $"-NoProfile -ExecutionPolicy Bypass -File \"{tempPs1}\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        using var reader = proc.StandardOutput;
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var trimmed = line.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                set.Add(trimmed);
                            }
                        }
                        proc.WaitForExit(15000);
                    }
                }
                catch { }
                finally
                {
                    try { if (File.Exists(tempPs1)) File.Delete(tempPs1); } catch { }
                }

                return set;
            });

            // 2. Scan OEM apps in Registry & Directories
            var oemDetection = await Task.Run(ScanOemSoftware);

            if (IsDisposed || !IsHandleCreated) return;

            Invoke(() =>
            {
                // Update Bloatware items
                int countGreen = 0;
                int countRed = 0;

                foreach (var app in _bloatwareApps)
                {
                    bool isFound = false;

                    foreach (var pat in app.PackagePatterns)
                    {
                        // Check exact or partial match in package names
                        if (installedPackages.Any(p => p.Contains(pat, StringComparison.OrdinalIgnoreCase)))
                        {
                            isFound = true;
                            break;
                        }
                    }

                    app.IsInstalled = isFound;
                    if (isFound)
                    {
                        countGreen++;
                        app.CheckBox.ForeColor = _colorGreen;
                    }
                    else
                    {
                        countRed++;
                        app.CheckBox.ForeColor = _colorRed;
                    }
                }

                _lblCountGreen.Text = $"Xanh lá = vẫn còn trên máy ({countGreen})";
                _lblCountRed.Text = $"Đỏ = đã gỡ rồi ({countRed})";

                // Update OEM items
                foreach (var item in _oemItems)
                {
                    if (oemDetection.TryGetValue(item.Id, out var det) && det.Found)
                    {
                        item.IsInstalled = true;
                        item.DetectedUninstallCmd = det.UninstallCmd;
                        item.CheckBox.ForeColor = _textHeading;
                        item.SubtitleLabel.Text = string.IsNullOrEmpty(det.Detail) ? "— đang có trên máy" : $"— đang có trên máy ({det.Detail})";
                        item.SubtitleLabel.ForeColor = _colorGreen;
                    }
                    else
                    {
                        item.IsInstalled = false;
                        item.DetectedUninstallCmd = null;
                        item.CheckBox.ForeColor = _textMuted;
                        item.SubtitleLabel.Text = "— không thấy trên máy";
                        item.SubtitleLabel.ForeColor = _textMuted;
                    }
                }
            });
        }
        catch
        {
        }
    }

    private class OemDetectResult
    {
        public bool Found { get; set; }
        public string? Detail { get; set; }
        public string? UninstallCmd { get; set; }
    }

    private Dictionary<string, OemDetectResult> ScanOemSoftware()
    {
        var result = new Dictionary<string, OemDetectResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in _oemItems)
        {
            result[item.Id] = new OemDetectResult { Found = false };
        }

        // Scan Registry Uninstall keys
        var regKeys = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            foreach (var subKeyPath in regKeys)
            {
                try
                {
                    using var uninstKey = root.OpenSubKey(subKeyPath);
                    if (uninstKey == null) continue;

                    foreach (var subName in uninstKey.GetSubKeyNames())
                    {
                        try
                        {
                            using var appKey = uninstKey.OpenSubKey(subName);
                            if (appKey == null) continue;

                            var dispName = appKey.GetValue("DisplayName")?.ToString()?.Trim() ?? "";
                            if (string.IsNullOrEmpty(dispName)) continue;

                            var uninstStr = appKey.GetValue("UninstallString")?.ToString()?.Trim() ??
                                            appKey.GetValue("QuietUninstallString")?.ToString()?.Trim() ?? "";

                            // 1. McAfee
                            if (dispName.Contains("McAfee", StringComparison.OrdinalIgnoreCase))
                            {
                                result["mcafee"].Found = true;
                                result["mcafee"].Detail = dispName;
                                if (!string.IsNullOrEmpty(uninstStr)) result["mcafee"].UninstallCmd = uninstStr;
                            }
                            // 2. Bkav
                            else if (dispName.Contains("Bkav", StringComparison.OrdinalIgnoreCase))
                            {
                                result["bkav"].Found = true;
                                result["bkav"].Detail = dispName;
                                if (!string.IsNullOrEmpty(uninstStr)) result["bkav"].UninstallCmd = uninstStr;
                            }
                            // 3. AVG
                            else if (dispName.Contains("AVG", StringComparison.OrdinalIgnoreCase))
                            {
                                result["avg"].Found = true;
                                result["avg"].Detail = dispName;
                                if (!string.IsNullOrEmpty(uninstStr)) result["avg"].UninstallCmd = uninstStr;
                            }
                            // 4. Avast
                            else if (dispName.Contains("Avast", StringComparison.OrdinalIgnoreCase))
                            {
                                result["avast"].Found = true;
                                result["avast"].Detail = dispName;
                                if (!string.IsNullOrEmpty(uninstStr)) result["avast"].UninstallCmd = uninstStr;
                            }
                            // 5. WPS Office
                            else if (dispName.Contains("WPS Office", StringComparison.OrdinalIgnoreCase) ||
                                     dispName.Contains("Kingsoft Office", StringComparison.OrdinalIgnoreCase))
                            {
                                result["wps"].Found = true;
                                result["wps"].Detail = dispName;
                                if (!string.IsNullOrEmpty(uninstStr)) result["wps"].UninstallCmd = uninstStr;
                            }
                            // 6. 360 Total Security
                            else if (dispName.Contains("360 Total Security", StringComparison.OrdinalIgnoreCase) ||
                                     dispName.Contains("360 Security", StringComparison.OrdinalIgnoreCase) ||
                                     dispName.Contains("360 Safe", StringComparison.OrdinalIgnoreCase))
                            {
                                result["360"].Found = true;
                                result["360"].Detail = dispName;
                                if (!string.IsNullOrEmpty(uninstStr)) result["360"].UninstallCmd = uninstStr;
                            }
                            // 7. Opera / Opera GX
                            else if (dispName.Contains("Opera", StringComparison.OrdinalIgnoreCase))
                            {
                                result["opera"].Found = true;
                                result["opera"].Detail = dispName;
                                if (!string.IsNullOrEmpty(uninstStr)) result["opera"].UninstallCmd = uninstStr;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        // Secondary Folder / Process checks
        try
        {
            var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // McAfee folders
            if (!result["mcafee"].Found &&
                (Directory.Exists(Path.Combine(progFiles, "McAfee")) ||
                 Directory.Exists(Path.Combine(progFilesX86, "McAfee"))))
            {
                result["mcafee"].Found = true;
            }

            // Bkav folders
            if (!result["bkav"].Found &&
                (Directory.Exists(Path.Combine(progFiles, "Bkav")) ||
                 Directory.Exists(Path.Combine(progFilesX86, "Bkav"))))
            {
                result["bkav"].Found = true;
            }

            // AVG folders
            if (!result["avg"].Found &&
                (Directory.Exists(Path.Combine(progFiles, "AVG")) ||
                 Directory.Exists(Path.Combine(progFilesX86, "AVG"))))
            {
                result["avg"].Found = true;
            }

            // Avast folders
            if (!result["avast"].Found &&
                (Directory.Exists(Path.Combine(progFiles, "AVAST Software")) ||
                 Directory.Exists(Path.Combine(progFilesX86, "AVAST Software"))))
            {
                result["avast"].Found = true;
            }

            // WPS Office
            if (!result["wps"].Found &&
                (Directory.Exists(Path.Combine(localApp, "Kingsoft", "WPS Office")) ||
                 Directory.Exists(Path.Combine(progFiles, "Kingsoft", "WPS Office")) ||
                 Directory.Exists(Path.Combine(progFilesX86, "Kingsoft", "WPS Office"))))
            {
                result["wps"].Found = true;
            }

            // 360 Total Security
            if (!result["360"].Found &&
                (Directory.Exists(Path.Combine(progFiles, "360", "Total Security")) ||
                 Directory.Exists(Path.Combine(progFilesX86, "360", "Total Security"))))
            {
                result["360"].Found = true;
            }

            // Opera folders or running process
            if (!result["opera"].Found &&
                (Directory.Exists(Path.Combine(localApp, "Programs", "Opera")) ||
                 Directory.Exists(Path.Combine(localApp, "Programs", "Opera GX")) ||
                 Directory.Exists(Path.Combine(progFiles, "Opera")) ||
                 Directory.Exists(Path.Combine(progFilesX86, "Opera")) ||
                 Process.GetProcessesByName("opera").Length > 0))
            {
                result["opera"].Found = true;
            }
        }
        catch { }

        return result;
    }

    #endregion

    #region Removal Execution

    private async Task RemoveSelectedBloatwareAsync()
    {
        var selected = _bloatwareApps.Where(a => a.CheckBox.Checked).ToList();

        if (selected.Count == 0)
        {
            if (_chkPreventReinstall.Checked)
            {
                var ask = MessageBox.Show(
                    "Bạn chưa tích chọn ứng dụng nào để gỡ. Bạn có muốn áp dụng tính năng 'Chặn tự cài lại Bloatware' vào hệ thống không?",
                    "Chặn Bloatware",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (ask == DialogResult.Yes)
                {
                    ApplyPreventReinstallTweaks();
                    MessageBox.Show("Đã thiết lập Chặn tự động cài lại Bloatware thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất một ứng dụng để gỡ bỏ!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return;
        }

        var sbNames = new StringBuilder();
        foreach (var app in selected.Take(12))
        {
            sbNames.AppendLine($"• {app.DisplayName}");
        }
        if (selected.Count > 12)
        {
            sbNames.AppendLine($"... và {selected.Count - 12} ứng dụng khác");
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn gỡ bỏ {selected.Count} ứng dụng Bloatware sau đây khỏi máy không?\n\n{sbNames}\n" +
            "Quá trình này sẽ gỡ sạch khỏi tài khoản hiện tại và ngăn Windows tự cài lại.",
            "Xác Nhận Gỡ Bloatware",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        _btnStartBloatware.Enabled = false;
        _btnStartBloatware.Text = "⏳ Đang gỡ bỏ...";

        await Task.Run(() =>
        {
            var psScript = new StringBuilder();
            psScript.AppendLine("$ErrorActionPreference = 'SilentlyContinue'");

            foreach (var app in selected)
            {
                foreach (var pat in app.PackagePatterns)
                {
                    // Remove from current user
                    psScript.AppendLine($"Get-AppxPackage *{pat}* | Remove-AppxPackage -ErrorAction SilentlyContinue");
                    // Remove for all users (elevated)
                    psScript.AppendLine($"Get-AppxPackage -AllUsers *{pat}* | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue");
                    // Remove provisioned package
                    psScript.AppendLine($"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.DisplayName -like '*{pat}*' -or $_.PackageName -like '*{pat}*' }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue");
                }
            }

            var tempPs1 = Path.Combine(Path.GetTempPath(), $"clean_bloatware_{Guid.NewGuid():N}.ps1");
            try
            {
                File.WriteAllText(tempPs1, psScript.ToString(), Encoding.UTF8);
                var psi = new ProcessStartInfo("powershell.exe",
                    $"-NoProfile -ExecutionPolicy Bypass -File \"{tempPs1}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(60000);
            }
            catch { }
            finally
            {
                try { if (File.Exists(tempPs1)) File.Delete(tempPs1); } catch { }
            }

            // Apply prevent reinstall if checked
            if (_chkPreventReinstall.Checked)
            {
                ApplyPreventReinstallTweaks();
            }
        });

        MessageBox.Show($"Đã hoàn tất gỡ bỏ {selected.Count} ứng dụng Bloatware đã chọn!", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);

        // Reset check states and reload
        _chkSelectAll.Checked = false;
        foreach (var app in selected)
        {
            app.CheckBox.Checked = false;
        }

        _btnStartBloatware.Enabled = true;
        _btnStartBloatware.Text = "Bắt đầu gỡ bỏ";

        await LoadStatusAsync();
    }

    private async Task RemoveSelectedOemAsync()
    {
        var selected = _oemItems.Where(i => i.CheckBox.Checked).ToList();

        if (selected.Count == 0)
        {
            MessageBox.Show("Vui lòng tích chọn ít nhất một phần mềm hãng để gỡ!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sbNames = new StringBuilder();
        foreach (var item in selected)
        {
            sbNames.AppendLine($"• {item.DisplayName}");
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn gỡ bỏ các phần mềm hãng sau đây không?\n\n{sbNames}\n" +
            "Tool sẽ kích hoạt trình gỡ chính hãng hoặc tải công cụ làm sạch chuyên dụng của hãng.",
            "Xác Nhận Gỡ Phần Mềm Hãng",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        _btnStartOem.Enabled = false;
        _btnStartOem.Text = "⏳ Đang gỡ...";

        foreach (var item in selected)
        {
            try
            {
                // Special handling for Opera
                if (item.Id == "opera")
                {
                    // Kill any active opera process
                    foreach (var p in Process.GetProcessesByName("opera"))
                    {
                        try { p.Kill(); } catch { }
                    }
                    await Task.Delay(500);

                    if (!string.IsNullOrEmpty(item.DetectedUninstallCmd))
                    {
                        RunProcessCommand(item.DetectedUninstallCmd);
                    }
                    else
                    {
                        // Check common opera uninstallers
                        var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        var operaDir = Path.Combine(localApp, "Programs", "Opera");
                        if (Directory.Exists(operaDir))
                        {
                            var installer = Directory.GetFiles(operaDir, "launcher.exe", SearchOption.AllDirectories).FirstOrDefault();
                            if (installer != null)
                            {
                                RunProcessCommand($"\"{installer}\" --uninstall -runsaveduninstaller -silent");
                            }
                        }
                    }
                }
                // Antivirus with dedicated tool (McAfee, Avast, AVG)
                else if (!string.IsNullOrEmpty(item.OfficialToolUrl) && !string.IsNullOrEmpty(item.OfficialToolExeName))
                {
                    // Try registry uninstaller first if available
                    if (!string.IsNullOrEmpty(item.DetectedUninstallCmd))
                    {
                        RunProcessCommand(item.DetectedUninstallCmd);
                    }

                    // Ask if user wants to download/run the official deep removal tool
                    var askCleanTool = MessageBox.Show(
                        $"Phần mềm \"{item.DisplayName}\" có công cụ gỡ sạch chính hãng ({item.OfficialToolExeName}).\n" +
                        "Bạn có muốn tải và chạy công cụ gỡ sạch chính thức này không?",
                        "Công Cụ Gỡ Chính Hãng",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (askCleanTool == DialogResult.Yes)
                    {
                        var tempDir = Path.GetTempPath();
                        var targetExe = Path.Combine(tempDir, item.OfficialToolExeName);

                        if (!File.Exists(targetExe))
                        {
                            _btnStartOem.Text = $"⏳ Đang tải {item.OfficialToolExeName}...";
                            using var client = new HttpClient();
                            var data = await client.GetByteArrayAsync(item.OfficialToolUrl);
                            await File.WriteAllBytesAsync(targetExe, data);
                        }

                        if (File.Exists(targetExe))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = targetExe,
                                UseShellExecute = true,
                                Verb = "runas"
                            });
                        }
                    }
                }
                else
                {
                    // Standard uninstaller
                    if (!string.IsNullOrEmpty(item.DetectedUninstallCmd))
                    {
                        RunProcessCommand(item.DetectedUninstallCmd);
                    }
                    else
                    {
                        MessageBox.Show($"Không tìm thấy tệp gỡ tự động của \"{item.DisplayName}\". Vui lòng kiểm tra lại trong Gỡ nhiều phần mềm.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi gỡ {item.DisplayName}: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        _btnStartOem.Enabled = true;
        _btnStartOem.Text = "Gỡ bỏ mục đã chọn";

        await LoadStatusAsync();
    }

    private static void RunProcessCommand(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                CreateNoWindow = false,
                UseShellExecute = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(30000);
        }
        catch { }
    }

    #endregion

    #region Prevent Reinstall Registry Helpers

    private static bool CheckPreventReinstallEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent");
            if (key?.GetValue("DisableWindowsConsumerFeatures") is int val && val == 1) return true;

            using var cuKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
            if (cuKey?.GetValue("SilentInstalledAppsEnabled") is int silent && silent == 0) return true;
        }
        catch { }
        return false;
    }

    private static void ApplyPreventReinstallTweaks()
    {
        try
        {
            // 1. CloudContent Policy: DisableWindowsConsumerFeatures
            using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
            {
                key?.SetValue("DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord);
                key?.SetValue("DisableSoftLanding", 1, RegistryValueKind.DWord);
            }

            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\CloudContent"))
            {
                key?.SetValue("DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord);
            }

            // 2. ContentDeliveryManager: Disable silent app auto-install & suggestions
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
            {
                if (key != null)
                {
                    key.SetValue("ContentDeliveryAllowed", 0, RegistryValueKind.DWord);
                    key.SetValue("OemPreInstalledAppsEnabled", 0, RegistryValueKind.DWord);
                    key.SetValue("PreInstalledAppsEnabled", 0, RegistryValueKind.DWord);
                    key.SetValue("PreInstalledAppsEverEnabled", 0, RegistryValueKind.DWord);
                    key.SetValue("SilentInstalledAppsEnabled", 0, RegistryValueKind.DWord);
                    key.SetValue("SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord);
                    key.SetValue("SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord);
                    key.SetValue("SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord);
                    key.SetValue("SubscribedContent-314563Enabled", 0, RegistryValueKind.DWord);
                }
            }
        }
        catch { }
    }

    #endregion
}
