using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Ernastie.Forms;

public class IrstDriverForm : Form
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
    private Color _warningAmber => ThemeHelper.WarningAmber;
    private Color _warningBg => ThemeHelper.IsDark ? Color.FromArgb(69, 26, 3) : Color.FromArgb(254, 243, 199);
    private Color _successGreen => ThemeHelper.SuccessGreen;
    private Color _successBg => ThemeHelper.IsDark ? Color.FromArgb(5, 46, 22) : Color.FromArgb(220, 252, 231);
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnHover => ThemeHelper.BtnHover;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _inputBg => ThemeHelper.InputBg;
    private Color _textNormal => ThemeHelper.TextNormal;

    private bool _hasVmd = false;
    private string _vmdDeviceName = "Không tìm thấy Intel VMD Controller";
    private string _vmdDeviceId = "N/A";
    private string _detectedDriverPath = string.Empty;
    private RichTextBox _rtbLog = null!;

    public IrstDriverForm()
    {
        InitializeComponents();
        ScanHardwareAndDrivers();
    }

    private void InitializeComponents()
    {
        Text = "💾 Kiểm tra & Cài đặt Driver Intel RST (VMD) - Cho WinPE / Cài Windows";
        Size = new Size(820, 780);
        MinimumSize = new Size(740, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9f);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(18, 14, 18, 18),
            BackColor = _bgForm
        };

        var masterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = _bgForm
        };
        masterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // 1. Hardware Status Card
        var cardStatus = BuildStatusCard();
        cardStatus.Dock = DockStyle.Top;
        cardStatus.Margin = new Padding(0, 0, 0, 12);
        masterLayout.Controls.Add(cardStatus);

        // 2. Action Buttons Card
        var cardActions = BuildActionsCard();
        cardActions.Dock = DockStyle.Top;
        cardActions.Margin = new Padding(0, 0, 0, 12);
        masterLayout.Controls.Add(cardActions);

        // 3. BIOS Guide Card
        var cardBios = BuildBiosGuideCard();
        cardBios.Dock = DockStyle.Top;
        cardBios.Margin = new Padding(0, 0, 0, 12);
        masterLayout.Controls.Add(cardBios);

        // 4. Log Panel
        var cardLog = BuildLogCard();
        cardLog.Dock = DockStyle.Top;
        cardLog.Margin = new Padding(0, 0, 0, 12);
        masterLayout.Controls.Add(cardLog);

        scrollPanel.Controls.Add(masterLayout);
        Controls.Add(scrollPanel);
        ThemeHelper.ApplyTheme(this);
    }

    private Panel BuildStatusCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("🔍  Trạng thái phần cứng Intel VMD / RST");
        card.Controls.Add(lblTitle);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = _cardBg,
            Margin = new Padding(0, 8, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var pnlAlert = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(12, 10, 12, 10),
            Name = "pnlAlert"
        };
        var lblAlertText = new Label
        {
            Name = "lblAlertText",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Đang kiểm tra phần cứng Intel VMD..."
        };
        pnlAlert.Controls.Add(lblAlertText);
        tbl.Controls.Add(pnlAlert);

        var pnlDev = CreateFieldRow("Thiết bị VMD:", "Đang quét...", name: "valDevice");
        tbl.Controls.Add(pnlDev);

        var pnlHwid = CreateFieldRow("Hardware ID:", "Đang quét...", isMonospace: true, name: "valHwid");
        tbl.Controls.Add(pnlHwid);

        var pnlDrv = CreateFieldRow("Gói Driver trên máy:", "Đang tìm kiếm file iaStorVD.inf...", name: "valDriver");
        tbl.Controls.Add(pnlDrv);

        card.Controls.Add(tbl);
        lblTitle.BringToFront();
        return card;
    }

    private Panel BuildActionsCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("⚡  Công cụ xử lý nhanh Driver IRST / VMD");
        card.Controls.Add(lblTitle);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var btnLoadPe = CreateActionButton("⚡ Nạp Driver vào WinPE ngay (drvload)",
            "Chạy drvload.exe nạp driver vào RAM của WinPE để hiện ổ SSD NVMe ngay lập tức",
            () => LoadDriverIntoWinPe());
        btnLoadPe.Margin = new Padding(0, 0, 6, 8);
        tbl.Controls.Add(btnLoadPe, 0, 0);

        var btnInstallWin = CreateActionButton("📦 Cài Driver vào Windows (PnPUtil)",
            "Cài gói iaStorVD.inf trực tiếp vào Windows DriverStore của máy hiện tại",
            () => InstallDriverIntoCurrentWindows());
        btnInstallWin.Margin = new Padding(6, 0, 0, 8);
        tbl.Controls.Add(btnInstallWin, 1, 0);

        var btnDism = CreateActionButton("💉 Tích hợp Driver vào WIM / ISO (DISM)",
            "Tích hợp vĩnh viễn driver VMD vào boot.wim / install.wim bộ cài Win",
            () => InjectDriverIntoWim());
        btnDism.Margin = new Padding(0, 0, 6, 8);
        tbl.Controls.Add(btnDism, 0, 1);

        var btnOpenDir = CreateActionButton("📂 Mở thư mục Driver có sẵn",
            "Mở thư mục chứa gói driver iaStorVD (Gen 11, 12, 13, 14) trên máy",
            () => OpenDriverFolder());
        btnOpenDir.Margin = new Padding(6, 0, 0, 8);
        tbl.Controls.Add(btnOpenDir, 1, 1);

        var btnDownload = CreateActionButton("🌐 Tải Driver Intel RST mới nhất",
            "Mở liên kết tải bộ driver Intel Rapid Storage Technology chính thức",
            () => DownloadLatestRstDriver());
        btnDownload.Margin = new Padding(0, 0, 6, 0);
        tbl.Controls.Add(btnDownload, 0, 2);

        var btnRescan = CreateActionButton("🔄 Quét lại phần cứng & Driver",
            "Làm mới thông tin nhận diện VMD và kiểm tra lại trạng thái ổ đĩa",
            () => ScanHardwareAndDrivers());
        btnRescan.Margin = new Padding(6, 0, 0, 0);
        tbl.Controls.Add(btnRescan, 1, 2);

        card.Controls.Add(tbl);
        lblTitle.BringToFront();
        return card;
    }

    private Panel BuildBiosGuideCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("💡  Cách tắt VMD Controller trong BIOS (Nếu không muốn cài Driver)");
        card.Controls.Add(lblTitle);

        var txtGuide = new TextBox
        {
            Dock = DockStyle.Top,
            Multiline = true,
            ReadOnly = true,
            Height = 160,
            BackColor = _inputBg,
            ForeColor = _textNormal,
            Font = new Font("Segoe UI", 8.75f),
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 8, 0, 0),
            Text =
                "Khi cài Windows trên các dòng máy Intel Gen 11, 12, 13, 14+ nếu không thấy ổ cứng, bạn có thể TẮT VMD trong BIOS:\r\n\r\n" +
                "• HP: Khởi động bấm liên tục F10 -> Vào tab 'Configuration' -> Tìm 'VMD Controller' -> Chọn 'Disabled' -> Bấm F10 Save & Exit.\r\n" +
                "• ASUS: Khởi động bấm F2/Del -> Bấm F7 (Advanced Mode) -> Tab 'Advanced' -> Chọn 'VMD setup menu' -> 'Enable VMD controller' -> Chọn 'Disabled' -> F10 Save.\r\n" +
                "• DELL: Khởi động bấm F2 -> Mục 'Storage' -> Tìm 'SATA/NVMe Operation' -> Chuyển từ 'RAID on' sang 'AHCI / NVMe' -> Apply Changes.\r\n" +
                "• LENOVO: Khởi động bấm F2 -> Tab 'Configuration' -> Tìm 'Intel VMD Controller' -> Chọn 'Disabled' -> F10 Save.\r\n" +
                "• ACER: Khởi động bấm F2 -> Tab 'Main' -> Bấm tổ hợp phím 'Ctrl + S' để hiện cài đặt ẩn -> Tìm 'VMD Controller' -> Chuyển sang 'Disabled' -> F10 Save.\r\n" +
                "• MSI: Khởi động bấm Del -> Bấm tổ hợp phím mở khóa BIOS ẩn: Right Shift + Right Ctrl + Left Alt + F2 -> Tìm VMD Controller -> Disabled."
        };

        card.Controls.Add(txtGuide);
        lblTitle.BringToFront();
        return card;
    }

    private Panel BuildLogCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("📋  Nhật ký thao tác");
        card.Controls.Add(lblTitle);

        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Top,
            Height = 120,
            ReadOnly = true,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 8, 0, 0)
        };

        card.Controls.Add(_rtbLog);
        lblTitle.BringToFront();
        return card;
    }

    #region Scan & Detection

    private void ScanHardwareAndDrivers()
    {
        Log("Bắt đầu quét phần cứng Intel VMD / RST...");
        _hasVmd = false;
        _vmdDeviceName = "Không phát hiện Intel VMD";
        _vmdDeviceId = "N/A";

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Name, Status FROM Win32_PnPEntity WHERE Name LIKE '%VMD%' OR Name LIKE '%RST%' OR DeviceID LIKE '%VEN_8086&DEV_9A0B%' OR DeviceID LIKE '%VEN_8086&DEV_09AB%'");

            foreach (var mo in searcher.Get())
            {
                var name = mo["Name"]?.ToString() ?? "";
                var devId = mo["DeviceID"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    _hasVmd = true;
                    _vmdDeviceName = name;
                    _vmdDeviceId = devId;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Cảnh báo quét WMI]: {ex.Message}");
        }

        // Search for iaStorVD.inf on disk
        _detectedDriverPath = FindVmdDriverOnDisk();

        // Update UI
        UpdateStatusUi();
    }

    private string FindVmdDriverOnDisk()
    {
        string[] candidates = {
            @"D:\driver\RST Drivers Gen 11 12 13\VMD\iaStorVD.inf",
            @"D:\driver\disk driver\F6\iaStorVD.inf",
            @"D:\driver\disk driver\dchu_VMD\iaStorVD.inf",
            @"C:\SWSetup\SP134240\F6\iaStorVD.inf",
            @"C:\SWSetup\SP134240\dchu_VMD\iaStorVD.inf",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "iaStorVD.inf")
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        // Scan D:\driver if available
        try
        {
            if (Directory.Exists(@"D:\driver"))
            {
                var files = Directory.GetFiles(@"D:\driver", "iaStorVD.inf", SearchOption.AllDirectories);
                if (files.Length > 0) return files[0];
            }
        }
        catch { }

        // Scan Windows DriverStore
        try
        {
            var driverStore = @"C:\Windows\System32\DriverStore\FileRepository";
            if (Directory.Exists(driverStore))
            {
                var dirs = Directory.GetDirectories(driverStore, "iastorvd.inf*");
                if (dirs.Length > 0)
                {
                    var inf = Path.Combine(dirs[0], "iaStorVD.inf");
                    if (File.Exists(inf)) return inf;
                }
            }
        }
        catch { }

        return string.Empty;
    }

    private void UpdateStatusUi()
    {
        var pnlAlert = Controls.Find("pnlAlert", true).FirstOrDefault() as Panel;
        var lblAlertText = Controls.Find("lblAlertText", true).FirstOrDefault() as Label;
        var valDevice = Controls.Find("valDevice", true).FirstOrDefault() as Label;
        var valHwid = Controls.Find("valHwid", true).FirstOrDefault() as Label;
        var valDriver = Controls.Find("valDriver", true).FirstOrDefault() as Label;

        if (pnlAlert != null && lblAlertText != null)
        {
            if (_hasVmd)
            {
                pnlAlert.BackColor = _warningBg;
                lblAlertText.ForeColor = _warningAmber;
                lblAlertText.Text = "⚠️ Máy tính ĐANG DÙNG Intel VMD Controller! Cần nạp driver IRST khi cài Win / WinPE để nhận diện ổ SSD.";
            }
            else
            {
                pnlAlert.BackColor = _successBg;
                lblAlertText.ForeColor = _successGreen;
                lblAlertText.Text = "✅ Máy tính không dùng Intel VMD (Chuẩn AHCI/NVMe thông thường). Không cần nạp thêm driver.";
            }
        }

        if (valDevice != null) valDevice.Text = _vmdDeviceName;
        if (valHwid != null) valHwid.Text = _vmdDeviceId;
        if (valDriver != null)
        {
            if (!string.IsNullOrEmpty(_detectedDriverPath))
            {
                valDriver.Text = $"✅ Đã tìm thấy: {_detectedDriverPath}";
                valDriver.ForeColor = _successGreen;
            }
            else
            {
                valDriver.Text = "⚠️ Chưa tìm thấy tệp iaStorVD.inf trên máy (Bấm nút Tải Driver để tải về)";
                valDriver.ForeColor = _warningAmber;
            }
        }

        Log(_hasVmd
            ? $"Phát hiện: {_vmdDeviceName} (Cần driver IRST VMD)"
            : "Không phát hiện phần cứng Intel VMD.");
    }

    #endregion

    #region Button Actions

    private void LoadDriverIntoWinPe()
    {
        string driverInf = GetOrBrowseDriverInf();
        if (string.IsNullOrEmpty(driverInf)) return;

        Log($"Đang thực thi drvload với tệp [{driverInf}]...");
        try
        {
            var psi = new ProcessStartInfo("drvload.exe", $"\"{driverInf}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit();

            var outText = p?.StandardOutput.ReadToEnd()?.Trim();
            if (!string.IsNullOrEmpty(outText)) Log($"[drvload]: {outText}");

            if (p?.ExitCode == 0)
            {
                Log("✅ drvload nạp driver VMD thành công! Ổ cứng NVMe đã được kích hoạt.");
                MessageBox.Show("Đã nạp driver IRST VMD vào WinPE thành công!\nỔ cứng NVMe đã sẵn sàng trong Windows Explorer và Disk Management.", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Log($"[Lỗi drvload]: ExitCode = {p?.ExitCode}. (Lệnh drvload chỉ hoạt động trong môi trường WinPE)");
                MessageBox.Show($"drvload trả về mã lỗi: {p?.ExitCode}.\nLưu ý: Lệnh drvload chỉ chạy trên môi trường WinPE cứu hộ. Nếu đang ở Windows chính, hãy dùng nút 'Cài Driver vào Windows (PnPUtil)'.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"[Lỗi thực thi drvload]: {ex.Message}");
            MessageBox.Show($"Lỗi: {ex.Message}\n(Lệnh drvload chỉ khả dụng trong môi trường WinPE)", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void InstallDriverIntoCurrentWindows()
    {
        string driverInf = GetOrBrowseDriverInf();
        if (string.IsNullOrEmpty(driverInf)) return;

        Log($"Đang cài đặt driver vào hệ thống bằng pnputil: [{driverInf}]...");
        try
        {
            var psi = new ProcessStartInfo("pnputil.exe", $"/add-driver \"{driverInf}\" /install")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit();
            var outText = p?.StandardOutput.ReadToEnd()?.Trim();
            Log(outText ?? "Đã thực thi pnputil.");

            MessageBox.Show("Đã cài đặt driver Intel RST VMD vào hệ thống Windows DriverStore thành công!", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi PnPUtil]: {ex.Message}");
            MessageBox.Show($"Lỗi cài driver: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void InjectDriverIntoWim()
    {
        string driverInf = GetOrBrowseDriverInf();
        if (string.IsNullOrEmpty(driverInf)) return;

        using var ofd = new OpenFileDialog
        {
            Title = "Chọn file WIM bộ cài (boot.wim hoặc install.wim)",
            Filter = "Windows Image File (*.wim)|*.wim|All files (*.*)|*.*"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK) return;
        var wimPath = ofd.FileName;
        var driverDir = Path.GetDirectoryName(driverInf);

        var msg = $"Bạn có muốn tích hợp driver VMD từ:\n[{driverDir}]\n\nVào file bộ cài:\n[{wimPath}]?";
        if (MessageBox.Show(msg, "Xác nhận tích hợp DISM", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var mountDir = Path.Combine(Path.GetDirectoryName(wimPath) ?? @"C:\", "Temp_Mount_Wim");
        Directory.CreateDirectory(mountDir);

        Log($"Bắt đầu mount [{Path.GetFileName(wimPath)}] để tích hợp driver...");

        try
        {
            // Mount
            RunProcessWithLog("dism.exe", $"/Mount-Image /ImageFile:\"{wimPath}\" /Index:1 /MountDir:\"{mountDir}\"");

            // Add-Driver
            Log($"Đang thêm driver VMD vào ảnh WIM...");
            RunProcessWithLog("dism.exe", $"/Image:\"{mountDir}\" /Add-Driver /Driver:\"{driverDir}\" /Recurse");

            // Unmount & Commit
            Log("Đang lưu thay đổi vào file WIM...");
            RunProcessWithLog("dism.exe", $"/Unmount-Image /MountDir:\"{mountDir}\" /Commit");

            try { Directory.Delete(mountDir, true); } catch { }

            Log("🎉 ĐÃ TÍCH HỢP DRIVER IRST VMD VÀO FILE WIM THÀNH CÔNG!");
            MessageBox.Show($"Đã tích hợp xong driver VMD vào {Path.GetFileName(wimPath)} thành công!", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi tích hợp DISM]: {ex.Message}");
            // Discard mount if error
            try
            {
                RunProcessWithLog("dism.exe", $"/Unmount-Image /MountDir:\"{mountDir}\" /Discard");
                Directory.Delete(mountDir, true);
            }
            catch { }
            MessageBox.Show($"Lỗi tích hợp: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenDriverFolder()
    {
        string dir = string.Empty;
        if (!string.IsNullOrEmpty(_detectedDriverPath))
        {
            dir = Path.GetDirectoryName(_detectedDriverPath) ?? string.Empty;
        }

        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            string[] candidates = { @"D:\driver\RST Drivers Gen 11 12 13\VMD", @"D:\driver\disk driver\F6", @"D:\driver" };
            foreach (var c in candidates)
            {
                if (Directory.Exists(c)) { dir = c; break; }
            }
        }

        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            Log($"Đã mở thư mục driver: {dir}");
        }
        else
        {
            MessageBox.Show("Chưa tìm thấy thư mục driver trên máy. Hãy bấm nút 'Tải Driver' để tải bộ driver mới nhất.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void DownloadLatestRstDriver()
    {
        Log("Mở liên kết tải Driver Intel RST F6 VMD chính thức...");
        Process.Start(new ProcessStartInfo("https://www.intel.com/content/www/us/en/download/19755/intel-rapid-storage-technology-driver-installation-software-with-intel-optane-memory-11th-up-to-14th-gen-platforms.html")
        {
            UseShellExecute = true
        });
    }

    private string GetOrBrowseDriverInf()
    {
        if (!string.IsNullOrEmpty(_detectedDriverPath) && File.Exists(_detectedDriverPath))
        {
            return _detectedDriverPath;
        }

        using var ofd = new OpenFileDialog
        {
            Title = "Chọn file driver iaStorVD.inf",
            Filter = "Driver Information (*.inf)|*.inf|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(@"D:\driver") ? @"D:\driver" : "C:\\"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _detectedDriverPath = ofd.FileName;
            UpdateStatusUi();
            return _detectedDriverPath;
        }

        return string.Empty;
    }

    private void RunProcessWithLog(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi);
        p?.WaitForExit();
        var outText = p?.StandardOutput.ReadToEnd()?.Trim();
        if (!string.IsNullOrEmpty(outText)) Log(outText);

        if (p?.ExitCode != 0)
        {
            throw new Exception($"Lệnh '{exe} {args}' thất bại với mã lỗi: {p?.ExitCode}");
        }
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

    #region Layout Helpers

    private Panel CreateBaseCard()
    {
        var pnl = new Panel
        {
            BackColor = _cardBg,
            Padding = new Padding(16, 12, 16, 14),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };
        return pnl;
    }

    private Label CreateSectionHeader(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Panel CreateFieldRow(string labelText, string valText, bool isMonospace = false, string name = "")
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };

        var val = new Label
        {
            Name = name,
            Text = valText,
            Dock = DockStyle.Fill,
            Font = isMonospace ? new Font("Consolas", 9f) : new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textValue,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var lbl = new Label
        {
            Text = labelText,
            Dock = DockStyle.Left,
            Width = 145,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textLabel,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pnl.Controls.Add(val);
        pnl.Controls.Add(lbl);
        return pnl;
    }

    private Button CreateActionButton(string title, string subtitle, Action action)
    {
        var btn = new Button
        {
            Dock = DockStyle.Fill,
            Height = 56,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(10, 8, 10, 8)
        };
        btn.FlatAppearance.BorderColor = _btnBorder;
        btn.FlatAppearance.MouseOverBackColor = _btnHover;

        // Add subtitle below text
        btn.Paint += (s, e) =>
        {
            var g = e.Graphics;
            using var fontSub = new Font("Segoe UI", 7.75f);
            using var brushSub = new SolidBrush(Color.FromArgb(100, 116, 139));
            g.DrawString(subtitle, fontSub, brushSub, new PointF(12, 32));
        };

        btn.Click += (_, _) => action();
        return btn;
    }

    #endregion
}
