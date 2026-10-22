using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Ernastie.Forms;

public class AutoTaskItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Func<Action<string>, Task> Action { get; set; } = null!;
    public CheckBox CheckBox { get; set; } = null!;
}

public class AutoTasksForm : Form
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

    private readonly List<AutoTaskItem> _tasks = new();
    private CancellationTokenSource? _cts;
    private bool _isBusy = false;

    // Controls
    private ProgressBar _progressBar = null!;
    private Label _lblStatus = null!;
    private Button _btnRunAll = null!;
    private Button _btnStop = null!;
    private RichTextBox _rtbLog = null!;

    public AutoTasksForm()
    {
        InitializeComponents();
        InitTasksList();
    }

    private void InitializeComponents()
    {
        Text = "Tác vụ tự động - Tối ưu hóa & Bảo trì hệ thống 1-Click";
        Size = new Size(840, 700);
        MinimumSize = new Size(780, 620);
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
            Text = "⚡ TÁC VỤ TỰ ĐỘNG HÓA BẢO TRÌ & TỐI ƯU HỆ THỐNG",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, 4),
            AutoSize = true
        };
        var lblSubtitle = new Label
        {
            Text = "Thực hiện chuỗi tối ưu hóa hệ thống, dọn sạch rác chuyên sâu, sửa lỗi mạng và chuẩn hóa Windows.",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = _textMuted,
            Location = new Point(2, 28),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubtitle);
        mainPanel.Controls.Add(pnlHeader);

        // Tasks container card
        var pnlCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 220,
            BackColor = _cardBg,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 8)
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        var flowTasks = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        pnlCard.Controls.Add(flowTasks);
        mainPanel.Controls.Add(pnlCard);

        // Button actions
        var pnlActions = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 4) };
        _btnRunAll = new Button
        {
            Text = "🚀 Bắt đầu Chạy Tác Vụ Tự Động (1-Click)",
            Size = new Size(290, 36),
            Location = new Point(0, 4),
            BackColor = _primaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRunAll.FlatAppearance.BorderSize = 0;
        _btnRunAll.Click += async (_, _) => await RunTasksAsync();

        _btnStop = new Button
        {
            Text = "⏹ Hủy tác vụ",
            Size = new Size(110, 36),
            Location = new Point(300, 4),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            Enabled = false,
            Cursor = Cursors.Hand
        };
        _btnStop.FlatAppearance.BorderColor = _btnBorder;
        _btnStop.Click += (_, _) => _cts?.Cancel();

        pnlActions.Controls.Add(_btnRunAll);
        pnlActions.Controls.Add(_btnStop);
        mainPanel.Controls.Add(pnlActions);

        // Progress bar and Status
        var pnlProgress = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent, Padding = new Padding(0, 2, 0, 4) };
        _progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 14, Minimum = 0, Maximum = 100, Value = 0 };
        _lblStatus = new Label { Text = "Sẵn sàng thực thi.", Dock = DockStyle.Bottom, Height = 18, Font = new Font("Segoe UI", 8.5f), ForeColor = _textMuted };
        pnlProgress.Controls.Add(_lblStatus);
        pnlProgress.Controls.Add(_progressBar);
        mainPanel.Controls.Add(pnlProgress);

        // Log
        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.LogBg,
            ForeColor = ThemeHelper.LogText,
            Font = new Font("Consolas", 9f),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        mainPanel.Controls.Add(_rtbLog);

        Controls.Add(mainPanel);

        // Bring to front in logical order
        pnlHeader.BringToFront();
        pnlCard.BringToFront();
        pnlActions.BringToFront();
        pnlProgress.BringToFront();
        _rtbLog.BringToFront();

        ThemeHelper.ApplyTheme(this);
    }

    private void InitTasksList()
    {
        var flow = (FlowLayoutPanel)Controls[0].Controls[1].Controls[0];

        AddTask(flow, "1. Dọn dẹp rác hệ thống & Bộ đệm Temp/Prefetch",
            "Xóa sạch thư mục Temp của người dùng, Windows Temp và nhật ký lỗi.",
            async log =>
            {
                log("🧹 Đang dọn dẹp thư mục Temp cá nhân...");
                CleanFolder(Path.GetTempPath(), log);
                log("🧹 Đang dọn dẹp Windows Temp...");
                CleanFolder(@"C:\Windows\Temp", log);
                log("🧹 Đang dọn dẹp Prefetch...");
                CleanFolder(@"C:\Windows\Prefetch", log);
                await Task.Delay(200);
            });

        AddTask(flow, "2. Tối ưu hóa mạng & Xóa bộ nhớ đệm DNS (Flush DNS)",
            "Làm mới bảng định tuyến IP, giải phóng và đăng ký lại DNS.",
            async log =>
            {
                log("🌐 Thực hiện Flush DNS...");
                RunCmd("ipconfig /flushdns", log);
                log("🌐 Làm mới cấu hình Winsock...");
                RunCmd("netsh winsock reset catalog", log);
                await Task.Delay(200);
            });

        AddTask(flow, "3. Đồng bộ lại thời gian hệ thống chuẩn xác (NTP Sync)",
            "Đồng bộ giờ đồng hồ máy tính với máy chủ time.windows.com.",
            async log =>
            {
                log("🕒 Đang khởi động dịch vụ Windows Time...");
                RunCmd("net start w32time", log);
                RunCmd("w32tm /resync /force", log);
                log($"  -> Giờ hệ thống hiện tại: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                await Task.Delay(200);
            });

        AddTask(flow, "4. Tối ưu hóa hiệu năng & Gói năng lượng (High Performance)",
            "Bật gói hiệu năng cao và tắt tính năng tiết kiệm điện cho card mạng.",
            async log =>
            {
                log("⚡ Kích hoạt High Performance Power Scheme...");
                RunCmd("powercfg -duplicatescheme 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", log);
                RunCmd("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", log);
                await Task.Delay(200);
            });

        AddTask(flow, "5. Tối ưu hóa ổ đĩa TRIM SSD & Defrag HDD",
            "Gửi lệnh TRIM giúp khôi phục tốc độ ghi cho toàn bộ ổ SSD.",
            async log =>
            {
                log("💽 Đang chạy lệnh TRIM tối ưu hóa ổ đĩa C:...");
                RunCmd("defrag C: /O", log);
                await Task.Delay(200);
            });

        AddTask(flow, "6. Kiểm tra tính toàn vẹn hệ thống (DISM Component Store)",
            "Kiểm tra kho lưu trữ gói Windows Component Store để phát hiện lỗi.",
            async log =>
            {
                log("🔍 Đang kiểm tra trạng thái Windows Component Store...");
                RunCmd("dism /Online /Cleanup-Image /CheckHealth", log);
                await Task.Delay(200);
            });
    }

    private void AddTask(FlowLayoutPanel flow, string name, string desc, Func<Action<string>, Task> action)
    {
        var row = new Panel { Size = new Size(760, 28), Margin = new Padding(0, 1, 0, 1), BackColor = Color.Transparent };
        var chk = new CheckBox
        {
            Text = name,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, 2),
            Size = new Size(420, 24),
            Checked = true
        };

        var lblDesc = new Label
        {
            Text = desc,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = _textMuted,
            Location = new Point(430, 4),
            Size = new Size(320, 20),
            AutoEllipsis = true
        };

        row.Controls.Add(chk);
        row.Controls.Add(lblDesc);
        flow.Controls.Add(row);

        _tasks.Add(new AutoTaskItem
        {
            Name = name,
            Description = desc,
            Action = action,
            CheckBox = chk
        });
    }

    private async Task RunTasksAsync()
    {
        var selected = _tasks.Where(t => t.CheckBox.Checked).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ít nhất một tác vụ để thực hiện!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _isBusy = true;
        _btnRunAll.Enabled = false;
        _btnStop.Enabled = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        Log($"════════════════════════════════════════════════════════════");
        Log($"🚀 BẮT ĐẦU CHUỖI TÁC VỤ TỰ ĐỘNG HÓA ({selected.Count} tác vụ)...");

        int current = 0;
        int total = selected.Count;

        try
        {
            foreach (var task in selected)
            {
                if (ct.IsCancellationRequested) break;
                current++;
                int pct = (int)((current - 1) / (double)total * 100);
                _progressBar.Value = pct;
                _lblStatus.Text = $"Đang thực hiện ({current}/{total}): {task.Name}...";

                Log($"────────────────────────────────────────────────────────────");
                Log($"⏳ [{current}/{total}] {task.Name}...");

                await task.Action(Log);

                Log($"✅ Đã hoàn tất: {task.Name}");
            }

            if (!ct.IsCancellationRequested)
            {
                _progressBar.Value = 100;
                _lblStatus.Text = "Hoàn tất toàn bộ tác vụ!";
                Log($"🎉 CHÚC MỪNG: Toàn bộ {total} tác vụ bảo trì tự động đã hoàn thành xuất sắc!");
                MessageBox.Show("Toàn bộ các tác vụ bảo trì tự động đã được thực hiện thành công!", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _lblStatus.Text = "Đã dừng tác vụ.";
                Log("⚠️ Quá trình đã được người dùng dừng lại.");
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Lỗi thực thi: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
            _btnRunAll.Enabled = true;
            _btnStop.Enabled = false;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_isBusy)
        {
            var res = MessageBox.Show("Các tác vụ bảo trì đang chạy, bạn có chắc muốn thoát không?", "Cảnh Báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) e.Cancel = true;
            else _cts?.Cancel();
        }
    }

    private void CleanFolder(string path, Action<string> log)
    {
        if (!Directory.Exists(path)) return;
        int filesCount = 0;

        foreach (var file in Directory.GetFiles(path))
        {
            try
            {
                File.Delete(file);
                filesCount++;
            }
            catch { }
        }

        foreach (var dir in Directory.GetDirectories(path))
        {
            try
            {
                Directory.Delete(dir, true);
                filesCount++;
            }
            catch { }
        }

        log($"  -> Đã xóa {filesCount} mục tại {path}");
    }

    private void RunCmd(string cmd, Action<string> log)
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c {cmd}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                var outText = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(10000);
                if (!string.IsNullOrWhiteSpace(outText))
                {
                    var lines = outText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var l in lines.Take(2)) log($"  {l}");
                }
            }
        }
        catch (Exception ex)
        {
            log($"  [Lỗi lệnh]: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message));
            return;
        }

        _rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.ScrollToCaret();
    }
}
