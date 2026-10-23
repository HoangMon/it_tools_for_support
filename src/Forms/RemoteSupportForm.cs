using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Ernastie.Forms;

public class RemoteToolItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string? ProtocolUri { get; set; }
    public bool IsInstalled => File.Exists(ExePath) || (!string.IsNullOrEmpty(ProtocolUri) && CheckProtocol(ProtocolUri));

    private static bool CheckProtocol(string uri)
    {
        try
        {
            var scheme = uri.Split(':')[0];
            using var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(scheme);
            return key != null;
        }
        catch { return false; }
    }
}

public class RemoteSupportForm : Form
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

    private readonly List<RemoteToolItem> _tools = new();

    public RemoteSupportForm()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Trung tâm Hỗ trợ từ xa (Remote Support)";
        Size = new Size(720, 560);
        MinimumSize = new Size(680, 500);
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
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text = "TRUNG TÂM KẾT NỐI HỖ TRỢ TỪ XA",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, 4),
            AutoSize = true
        };
        var lblSubtitle = new Label
        {
            Text = "Khởi chạy công cụ điều khiển máy từ xa hoặc đọc thông tin bên dưới cho kỹ thuật viên IT.",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = _textMuted,
            Location = new Point(2, 28),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubtitle);
        mainPanel.Controls.Add(pnlHeader);

        // PC Info Card
        var pnlInfo = CreateCardPanel(84);
        pnlInfo.Dock = DockStyle.Top;
        pnlInfo.Padding = new Padding(14, 8, 14, 8);

        var lblInfoTitle = new Label { Text = "THÔNG TIN THIẾT BỊ ĐỂ ĐỌC CHO KỸ THUẬT VIÊN:", Font = new Font("Segoe UI", 8.75f, FontStyle.Bold), ForeColor = _primaryBlue, Dock = DockStyle.Top, Height = 20 };
        var ip = GetLocalIpAddress();
        var pcInfo = $"• Tên máy: {Environment.MachineName}    • Người dùng: {Environment.UserName}\n" +
                     $"• Địa chỉ IP mạng: {ip}    • Hệ điều hành: Windows {(Environment.OSVersion.Version.Build >= 22000 ? "11" : "10")} (Build {Environment.OSVersion.Version.Build})";

        var lblDetails = new Label
        {
            Text = pcInfo,
            Font = new Font("Consolas", 9.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pnlInfo.Controls.Add(lblDetails);
        pnlInfo.Controls.Add(lblInfoTitle);
        mainPanel.Controls.Add(pnlInfo);

        // Remote Tools Card
        var pnlToolsCard = CreateCardPanel(320);
        pnlToolsCard.Dock = DockStyle.Fill;
        pnlToolsCard.Padding = new Padding(14, 10, 14, 10);

        var lblToolsTitle = new Label
        {
            Text = "Danh sách công cụ hỗ trợ điều khiển từ xa:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Top,
            Height = 26
        };
        pnlToolsCard.Controls.Add(lblToolsTitle);

        var flowTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        InitToolsList();

        foreach (var t in _tools)
        {
            AddToolRow(flowTools, t);
        }

        pnlToolsCard.Controls.Add(flowTools);
        mainPanel.Controls.Add(pnlToolsCard);

        Controls.Add(mainPanel);

        pnlHeader.BringToFront();
        pnlInfo.BringToFront();
        pnlToolsCard.BringToFront();

        ThemeHelper.ApplyTheme(this);
    }

    private void InitToolsList()
    {
        _tools.Add(new RemoteToolItem
        {
            Name = "UltraViewer",
            Icon = "•",
            ExePath = @"C:\Program Files (x86)\UltraViewer\UltraViewer_Desktop.exe",
            DownloadUrl = "https://www.ultraviewer.net/en/UltraViewer_setup_6.6_en.exe"
        });

        _tools.Add(new RemoteToolItem
        {
            Name = "AnyDesk",
            Icon = "•",
            ExePath = @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe",
            DownloadUrl = "https://download.anydesk.com/AnyDesk.exe"
        });

        _tools.Add(new RemoteToolItem
        {
            Name = "TeamViewer",
            Icon = "•",
            ExePath = @"C:\Program Files\TeamViewer\TeamViewer.exe",
            DownloadUrl = "https://download.teamviewer.com/download/TeamViewer_Setup_x64.exe"
        });

        _tools.Add(new RemoteToolItem
        {
            Name = "RustDesk",
            Icon = "•",
            ExePath = @"C:\Program Files\RustDesk\rustdesk.exe",
            DownloadUrl = "https://github.com/rustdesk/rustdesk/releases/download/1.3.1/rustdesk-1.3.1-x86_64.exe"
        });

        _tools.Add(new RemoteToolItem
        {
            Name = "Windows Quick Assist (Hỗ trợ nhanh)",
            Icon = "•",
            ExePath = @"C:\Windows\System32\quickassist.exe",
            ProtocolUri = "ms-quick-assist:"
        });
    }

    private void AddToolRow(FlowLayoutPanel parent, RemoteToolItem tool)
    {
        var row = new Panel
        {
            Size = new Size(650, 48),
            Margin = new Padding(0, 3, 0, 4),
            BackColor = Color.Transparent
        };
        row.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
        };

        var lblName = new Label
        {
            Text = $"{tool.Icon}  {tool.Name}",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(4, 12),
            Size = new Size(260, 24),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var isInstalled = tool.IsInstalled;

        var lblStatus = new Label
        {
            Text = isInstalled ? "Đã cài đặt" : "Chưa có trên máy",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = isInstalled ? _successGreen : _textMuted,
            Location = new Point(270, 14),
            Size = new Size(140, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnAction = new Button
        {
            Text = isInstalled ? "▶ Mở ngay" : "⬇ Tải & Chạy nhanh",
            Size = new Size(160, 32),
            Location = new Point(470, 8),
            BackColor = isInstalled ? _primaryBlue : _btnBg,
            ForeColor = isInstalled ? Color.White : _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnAction.FlatAppearance.BorderColor = isInstalled ? _primaryBlue : _btnBorder;
        btnAction.Click += async (_, _) => await LaunchOrDownloadToolAsync(tool, btnAction, lblStatus);

        row.Controls.Add(lblName);
        row.Controls.Add(lblStatus);
        row.Controls.Add(btnAction);

        parent.Controls.Add(row);
    }

    private async Task LaunchOrDownloadToolAsync(RemoteToolItem tool, Button btn, Label lblStatus)
    {
        if (File.Exists(tool.ExePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo(tool.ExePath) { UseShellExecute = true });
                return;
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(tool.ProtocolUri))
        {
            try
            {
                Process.Start(new ProcessStartInfo(tool.ProtocolUri) { UseShellExecute = true });
                return;
            }
            catch { }
        }

        if (string.IsNullOrEmpty(tool.DownloadUrl))
        {
            MessageBox.Show($"Công cụ {tool.Name} không có sẵn trên máy và không thể tải tự động.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        btn.Enabled = false;
        btn.Text = "⏳ Đang tải...";
        lblStatus.Text = "Đang tải về %TEMP%...";

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "RemoteTools");
            Directory.CreateDirectory(tempDir);
            var exeName = Path.GetFileName(tool.DownloadUrl.Split('?')[0]);
            var targetExe = Path.Combine(tempDir, exeName);

            if (!File.Exists(targetExe))
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var data = await client.GetByteArrayAsync(tool.DownloadUrl);
                await File.WriteAllBytesAsync(targetExe, data);
            }

            lblStatus.Text = "Đang khởi chạy...";
            Process.Start(new ProcessStartInfo(targetExe) { UseShellExecute = true });

            lblStatus.Text = "Đang chạy";
            lblStatus.ForeColor = _successGreen;
            btn.Text = "▶ Mở lại";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải công cụ {tool.Name}: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Lỗi tải tệp";
            btn.Text = "⬇ Tải lại";
        }
        finally
        {
            btn.Enabled = true;
        }
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    private Panel CreateCardPanel(int height)
    {
        var pnl = new Panel
        {
            Height = height,
            BackColor = _cardBg,
            Margin = new Padding(0, 0, 0, 8)
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };
        return pnl;
    }
}
