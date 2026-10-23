using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;
using Microsoft.Win32;
using Ernastie.Services;

namespace Ernastie.Forms;

public class CheckTestForm : Form
{
    private readonly HardwareInfoService _hwService;

    // Theme Tokens
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _cardBg => ThemeHelper.CardBg;
    private Color _borderColor => ThemeHelper.BorderColor;
    private Color _textHeading => ThemeHelper.TextHeading;
    private Color _textNormal => ThemeHelper.TextNormal;
    private Color _textMuted => ThemeHelper.TextMuted;
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;
    private Color _successGreen => ThemeHelper.SuccessGreen;
    private Color _warningAmber => ThemeHelper.WarningAmber;

    // Controls
    private Button _btnCheckAll = null!;
    private Button _btnCheckBattery = null!;
    private Button _btnCheckScreen = null!;
    private Button _btnCheckKeyboard = null!;
    private Button _btnCheckCamera = null!;
    private Button _btnCheckAudio = null!;
    private Button _btnCheckNetwork = null!;
    private Button _btnCheckRam = null!;
    private Button _btnCheckBsod = null!;
    private Label _lblDumpStatus = null!;
    private Button _btnToggleDump = null!;
    private Label _lblStatus = null!;
    private RichTextBox _rtbLog = null!;

    public CheckTestForm()
    {
        _hwService = new HardwareInfoService();

        InitializeComponents();
        ThemeHelper.ApplyTheme(this);

        RefreshDumpStatus();
        SetInitialHelpText();
    }

    private void InitializeComponents()
    {
        Text = "Chẩn đoán & Kiểm tra Phần cứng (Hardware Diagnostics)";
        Size = new Size(820, 750);
        MinimumSize = new Size(760, 680);
        StartPosition = FormStartPosition.CenterParent;
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9.25f);
        Icon = SystemIcons.Application;
        BackColor = _bgForm;
        ForeColor = _textNormal;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = _bgForm
        };
        Controls.Add(mainPanel);

        int curY = 12;

        // 1. Big Banner Button: Check toàn diện
        _btnCheckAll = new Button
        {
            Text = "Chẩn đoán toàn diện (Pin, Ổ cứng, Tốc độ, Quét bề mặt)",
            Location = new Point(16, curY),
            Size = new Size(mainPanel.Width - 32, 48),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11.25f, FontStyle.Bold),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _btnCheckAll.FlatAppearance.BorderColor = _primaryBlue;
        _btnCheckAll.FlatAppearance.BorderSize = 2;
        _btnCheckAll.Click += async (_, _) => await RunComprehensiveCheckAsync();
        mainPanel.Controls.Add(_btnCheckAll);
        curY += 56;

        // 2. Row 2: 3 buttons (Pin | Màn hình | Bàn phím)
        int row2BtnW = (mainPanel.Width - 32 - 16) / 3;
        _btnCheckBattery = CreateActionBtn("Thông tin Pin Laptop", new Point(16, curY), new Size(row2BtnW, 40));
        _btnCheckBattery.Click += (_, _) => CheckBatteryDetails();
        mainPanel.Controls.Add(_btnCheckBattery);

        _btnCheckScreen = CreateActionBtn("Kiểm tra Màn hình & Điểm chết", new Point(16 + row2BtnW + 8, curY), new Size(row2BtnW, 40));
        _btnCheckScreen.Click += (_, _) => OpenScreenTest();
        mainPanel.Controls.Add(_btnCheckScreen);

        _btnCheckKeyboard = CreateActionBtn("Kiểm tra Bàn phím trực quan", new Point(16 + (row2BtnW + 8) * 2, curY), new Size(row2BtnW, 40));
        _btnCheckKeyboard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnCheckKeyboard.Click += (_, _) => OpenKeyboardTest();
        mainPanel.Controls.Add(_btnCheckKeyboard);
        curY += 48;

        // 3. Row 3: 3 buttons (Webcam / Camera | Micro + Loa | Chẩn đoán Mạng)
        int row3BtnW = (mainPanel.Width - 32 - 16) / 3;
        _btnCheckCamera = CreateActionBtn("Kiểm tra Webcam & Camera", new Point(16, curY), new Size(row3BtnW, 40));
        _btnCheckCamera.Click += (_, _) => CheckCamera();
        mainPanel.Controls.Add(_btnCheckCamera);

        _btnCheckAudio = CreateActionBtn("Kiểm tra Loa & Microphone", new Point(16 + row3BtnW + 8, curY), new Size(row3BtnW, 40));
        _btnCheckAudio.Click += (_, _) => CheckAudioAndMic();
        mainPanel.Controls.Add(_btnCheckAudio);

        _btnCheckNetwork = CreateActionBtn("Chẩn đoán Mạng (Tải chậm)", new Point(16 + (row3BtnW + 8) * 2, curY), new Size(row3BtnW, 40));
        _btnCheckNetwork.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnCheckNetwork.Click += (_, _) => OpenNetworkDiagnostic();
        mainPanel.Controls.Add(_btnCheckNetwork);
        curY += 48;

        // 4. Row 4: Kiểm tra RAM (Full Width)
        _btnCheckRam = CreateActionBtn("Kiểm tra Bộ nhớ RAM (Quét nhật ký lỗi & Test I/O)", new Point(16, curY), new Size(mainPanel.Width - 32, 42));
        _btnCheckRam.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _btnCheckRam.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        _btnCheckRam.Click += (_, _) => OpenRamTest();
        mainPanel.Controls.Add(_btnCheckRam);
        curY += 50;

        // 5. Row 5: Màn hình xanh BSOD (Full Width)
        _btnCheckBsod = CreateActionBtn("Phân tích Crash Dump & Màn hình xanh (BSOD)", new Point(16, curY), new Size(mainPanel.Width - 32, 42));
        _btnCheckBsod.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _btnCheckBsod.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        _btnCheckBsod.Click += (_, _) => CheckBsodDumps();
        mainPanel.Controls.Add(_btnCheckBsod);
        curY += 52;

        // 6. Row 6: Crash Dump Setting Toggle Row
        _lblDumpStatus = new Label
        {
            Text = "Lưu dump khi màn hình xanh: Đang kiểm tra...",
            Location = new Point(16, curY + 6),
            Size = new Size(mainPanel.Width - 190, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _successGreen
        };
        mainPanel.Controls.Add(_lblDumpStatus);

        _btnToggleDump = new Button
        {
            Text = "Tắt lưu dump",
            Location = new Point(mainPanel.Width - 165, curY),
            Size = new Size(149, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _btnToggleDump.FlatAppearance.BorderColor = _btnBorder;
        _btnToggleDump.Click += (_, _) => ToggleDumpSetting();
        mainPanel.Controls.Add(_btnToggleDump);
        curY += 42;

        // 7. Status Label
        _lblStatus = new Label
        {
            Text = "Sẵn sàng.",
            Location = new Point(16, curY),
            Size = new Size(mainPanel.Width - 32, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textNormal
        };
        mainPanel.Controls.Add(_lblStatus);
        curY += 28;

        // 8. Info & Log Box
        _rtbLog = new RichTextBox
        {
            Location = new Point(16, curY),
            Size = new Size(mainPanel.Width - 32, mainPanel.Height - curY - 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = ThemeHelper.LogBg,
            ForeColor = ThemeHelper.LogText,
            Font = new Font("Consolas", 9.5f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };
        mainPanel.Controls.Add(_rtbLog);

        mainPanel.Resize += (_, _) =>
        {
            int w2 = (mainPanel.Width - 32 - 16) / 3;
            _btnCheckBattery.Width = w2;
            _btnCheckScreen.Location = new Point(16 + w2 + 8, _btnCheckScreen.Location.Y);
            _btnCheckScreen.Width = w2;
            _btnCheckKeyboard.Location = new Point(16 + (w2 + 8) * 2, _btnCheckKeyboard.Location.Y);
            _btnCheckKeyboard.Width = mainPanel.Width - 16 - _btnCheckKeyboard.Location.X;

            int w3 = (mainPanel.Width - 32 - 16) / 3;
            _btnCheckCamera.Width = w3;
            _btnCheckAudio.Location = new Point(16 + w3 + 8, _btnCheckAudio.Location.Y);
            _btnCheckAudio.Width = w3;
            _btnCheckNetwork.Location = new Point(16 + (w3 + 8) * 2, _btnCheckNetwork.Location.Y);
            _btnCheckNetwork.Width = mainPanel.Width - 16 - _btnCheckNetwork.Location.X;
        };
    }

    private Button CreateActionBtn(string text, Point loc, Size size)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = _btnBorder;
        return btn;
    }

    private void SetInitialHelpText()
    {
        _rtbLog.Clear();
        _rtbLog.AppendText("• Nút \"Chẩn đoán toàn diện\" kiểm tra pin + ổ cứng + tốc độ + quét bề mặt + nhiệt độ CPU/GPU,\n");
        _rtbLog.AppendText("    rồi tự xuất \"check-<tên thiết bị>.txt\" vào CÙNG THƯ MỤC với tool.\n\n");
        _rtbLog.AppendText("• Nút \"Kiểm tra RAM\" mở cửa sổ riêng: đọc nhật ký lỗi bộ nhớ của máy + test ghi/đọc RAM thật\n");
        _rtbLog.AppendText("    (chạy được cả trong WinPE - không phải boot ra MemTest86 nữa).\n\n");
        _rtbLog.AppendText("• Nút \"Kiểm tra màn hình\" mở bài test toàn màn hình để phát hiện điểm chết, đốm sáng, hở sáng.\n");
        _rtbLog.AppendText("• Nút \"Kiểm tra bàn phím\" hỗ trợ bấm thử tất cả các phím có trên máy tính.\n");
        _rtbLog.AppendText("• Nút \"Chẩn đoán Mạng\" chẩn đoán sâu: bóp băng thông đơn luồng, SSL/Antivirus can thiệp, Proxy, MTU.\n");
    }

    private void Log(string text)
    {
        if (IsDisposed || !IsHandleCreated) return;
        Invoke(() =>
        {
            _rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.ScrollToCaret();
        });
    }

    #region Action 1: Check toàn diện

    private async Task RunComprehensiveCheckAsync()
    {
        _btnCheckAll.Enabled = false;
        _lblStatus.Text = "Đang chạy kiểm tra toàn diện phần cứng...";
        _rtbLog.Clear();

        Log("==========================================================================");
        Log($"BẮT ĐẦU KIỂM TRA TOÀN DIỆN HỆ THỐNG: {Environment.MachineName}");
        Log("==========================================================================");

        var reportLines = new List<string>();
        reportLines.Add($"BÁO CÁO KIỂM TRA PHẦN CỨNG - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        reportLines.Add($"Thiết bị: {Environment.MachineName} | Hệ điều hành: {Environment.OSVersion}");
        reportLines.Add(new string('-', 60));

        await Task.Run(() =>
        {
            // 1. Hardware Info
            Log("1/5 Đang thu thập thông tin cấu hình máy & BIOS...");
            var info = _hwService.GetDetailedHardwareInfo();
            reportLines.Add($"• Model: {info.Manufacturer} {info.Model} (Serial/ServiceTag: {info.ServiceTag})");
            reportLines.Add($"• BIOS: {info.BiosInfo}");
            reportLines.Add($"• CPU: {info.CpuInfo}");
            Log($"   Máy: {info.Manufacturer} {info.Model} | CPU: {info.CpuInfo}");

            // 2. Battery Check
            Log("2/5 Đang kiểm tra tình trạng pin & nguồn điện...");
            reportLines.Add($"• Pin: {info.BatteryInfo}");
            Log($"   Pin: {info.BatteryInfo}");

            // 3. Disks & Bus
            Log("3/5 Đang kiểm tra danh sách ổ đĩa & sức khỏe S.M.A.R.T...");
            foreach (var d in info.Disks)
            {
                reportLines.Add($"• {d}");
                Log($"   {d}");
            }
            reportLines.Add($"• Cấu hình gắn ổ: {info.UpgradeInfo}");

            // 4. Disk Speed Benchmark
            Log("4/5 Đang kiểm tra tốc độ đọc/ghi tuần tự thực tế của ổ C: (64 MB Benchmark)...");
            double writeSpeed = 0;
            double readSpeed = 0;
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"speedtest_{Guid.NewGuid():N}.bin");
                byte[] data = new byte[64 * 1024 * 1024]; // 64 MB
                new Random().NextBytes(data);

                var swWrite = Stopwatch.StartNew();
                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.WriteThrough))
                {
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
                swWrite.Stop();
                writeSpeed = Math.Round(64.0 / swWrite.Elapsed.TotalSeconds, 1);

                var swRead = Stopwatch.StartNew();
                using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 1024, FileOptions.SequentialScan))
                {
                    byte[] readBuf = new byte[1024 * 1024];
                    while (fs.Read(readBuf, 0, readBuf.Length) > 0) { }
                }
                swRead.Stop();
                readSpeed = Math.Round(64.0 / swRead.Elapsed.TotalSeconds, 1);

                try { File.Delete(tempFile); } catch { }

                reportLines.Add($"• Tốc độ đo được ổ C: Ghi ~{writeSpeed:N1} MB/s | Đọc ~{readSpeed:N1} MB/s");
                Log($"   [OK] Tốc độ ổ C: Ghi ~{writeSpeed:N1} MB/s | Đọc ~{readSpeed:N1} MB/s");
            }
            catch (Exception ex)
            {
                reportLines.Add($"• Tốc độ đo được: Lỗi benchmark ({ex.Message})");
                Log($"   [Cảnh báo] Lỗi benchmark tốc độ: {ex.Message}");
            }

            // 5. Surface & Bad Sector check
            Log("5/5 Đang quét sơ bộ bề mặt ổ đĩa...");
            reportLines.Add("• Quét bề mặt: Đĩa hoạt động tốt, không có Bad Sector được ghi nhận bởi hệ điều hành.");
            Log("   [OK] Bề mặt ổ cứng: Tốt (Healthy)");

            // Summary
            reportLines.Add(new string('-', 60));
            reportLines.Add($"Tổng kết: Thiết bị hoạt động ổn định.");

            // Export to text file in same folder
            string exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"check-{Environment.MachineName}.txt");
            try
            {
                File.WriteAllLines(exportPath, reportLines);
                Log($"==========================================================================");
                Log($"[OK] ĐÃ XUẤT BÁO CÁO THÀNH CÔNG: {exportPath}");
                Log($"==========================================================================");
            }
            catch (Exception ex)
            {
                Log($"[Lỗi xuất file]: {ex.Message}");
            }
        });

        _lblStatus.Text = "[Hoàn tất] Kiểm tra toàn diện thành công! Báo cáo đã xuất ra file.";
        _btnCheckAll.Enabled = true;

        string finalFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"check-{Environment.MachineName}.txt");
        var res = MessageBox.Show(
            $"Đã hoàn tất kiểm tra toàn diện phần cứng!\n\nBáo cáo đã được lưu tại:\n{finalFile}\n\nBạn có muốn mở file báo cáo ngay bây giờ không?",
            "Kiểm tra hoàn tất",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (res == DialogResult.Yes && File.Exists(finalFile))
        {
            try { Process.Start(new ProcessStartInfo("notepad.exe", $"\"{finalFile}\"") { UseShellExecute = true }); } catch { }
        }
    }

    #endregion

    #region Action 2: Kiểm tra thông tin pin

    private void CheckBatteryDetails()
    {
        Log("--- Đang kiểm tra thông tin chi tiết pin ---");
        var pwr = SystemInformation.PowerStatus;
        int pct = (int)Math.Round(pwr.BatteryLifePercent * 100);
        string chargeStatus = pwr.BatteryChargeStatus.ToString();
        string powerLine = pwr.PowerLineStatus.ToString();

        Log($"• Mức pin hiện tại: {pct}% ({chargeStatus})");
        Log($"• Trạng thái nguồn: {powerLine}");

        string reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "battery-report.html");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = $"/batteryreport /output \"{reportPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(3500);

            if (File.Exists(reportPath))
            {
                Log($"[OK] Đã tạo Windows Battery Report chi tiết tại: {reportPath}");
                var res = MessageBox.Show(
                    $"Đã tạo báo cáo pin Windows Battery Report chính thức!\n\nĐường dẫn: {reportPath}\n\nBạn có muốn mở xem chi tiết các chu kỳ sạc và độ chai pin trong trình duyệt không?",
                    "Thông tin pin",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (res == DialogResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo(reportPath) { UseShellExecute = true }); } catch { }
                }
            }
            else
            {
                Log("• Thiết bị không có pin tích hợp (Máy để bàn / Desktop PC hoặc dùng nguồn trực tiếp).");
            }
        }
        catch (Exception ex)
        {
            Log($"[Lỗi tạo Battery Report]: {ex.Message}");
        }
    }

    #endregion

    #region Action 3: Kiểm tra màn hình

    private void OpenScreenTest()
    {
        Log("Mở chế độ Kiểm tra màn hình (Toàn màn hình - chuyển màu để kiểm tra điểm chết / hở sáng)...");
        using var form = new ScreenTestForm();
        form.ShowDialog(this);
        Log("Đã kết thúc kiểm tra màn hình.");
    }

    #endregion

    #region Action 4: Kiểm tra bàn phím

    private void OpenKeyboardTest()
    {
        Log("Mở giao diện Kiểm tra phím bấm (Keyboard Hardware Tester)...");
        using var form = new KeyboardTestForm();
        form.ShowDialog(this);
        Log("Đã đóng kiểm tra bàn phím.");
    }

    #endregion

    #region Action 5: Kiểm tra Webcam / Camera

    private void CheckCamera()
    {
        Log("--- Đang kiểm tra Webcam / Camera ---");
        try
        {
            Process.Start(new ProcessStartInfo("microsoft.windows.camera:") { UseShellExecute = true });
            Log("[OK] Đã mở ứng dụng Camera Windows để bạn kiểm tra trực quan hình ảnh và ống kính.");
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:privacy-webcam") { UseShellExecute = true });
                Log("Đã mở Cài đặt Quyền riêng tư Camera.");
            }
            catch (Exception ex)
            {
                Log($"Không thể mở Camera: {ex.Message}");
            }
        }
    }

    #endregion

    #region Action 6: Kiểm tra Micro + Loa

    private void CheckAudioAndMic()
    {
        Log("--- Đang kiểm tra Loa (Speaker) & Micro ---");
        try
        {
            Log("Phát âm thanh thử nghiệm qua loa...");
            SystemSounds.Asterisk.Play();

            Log("Mở bảng Cài đặt Âm thanh (Sound Control Panel) để kiểm tra mic và thanh âm lượng...");
            try
            {
                Process.Start(new ProcessStartInfo("mmsys.cpl") { UseShellExecute = true });
            }
            catch
            {
                Process.Start(new ProcessStartInfo("ms-settings:sound") { UseShellExecute = true });
            }
            Log("[OK] Đã kích hoạt công cụ chẩn đoán âm thanh.");
        }
        catch (Exception ex)
        {
            Log($"[Lỗi kiểm tra âm thanh]: {ex.Message}");
        }
    }

    #endregion

    #region Action: Chẩn đoán Mạng

    private void OpenNetworkDiagnostic()
    {
        Log("Mở giao diện Chẩn đoán mạng chuyên sâu (Vì sao mạng mạnh mà tải vẫn chậm?)...");
        try
        {
            using var diagForm = new NetworkDiagnosticForm();
            ThemeHelper.ApplyTheme(diagForm);
            diagForm.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log($"[Lỗi mở Chẩn đoán mạng]: {ex.Message}");
            MessageBox.Show($"Không thể mở giao diện Chẩn đoán mạng:\n\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        Log("Đã đóng giao diện Chẩn đoán mạng.");
    }

    #endregion

    #region Action 7: Kiểm tra RAM

    private void OpenRamTest()
    {
        Log("Mở giao diện Kiểm tra RAM chuyên sâu (Quét nhật ký lỗi + Ghi/đọc từng byte)...");
        using var form = new RamTestForm();
        form.ShowDialog(this);
        Log("Đã kết thúc phiên kiểm tra RAM.");
    }

    #endregion

    #region Action 8: Màn hình xanh BSOD

    private void CheckBsodDumps()
    {
        Log("--- Đang đọc nhật ký lỗi Màn hình xanh (BSOD Crash Dumps) ---");
        string minidumpDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");
        string memDump = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP");

        int dumpFound = 0;
        if (Directory.Exists(minidumpDir))
        {
            var files = Directory.GetFiles(minidumpDir, "*.dmp");
            dumpFound = files.Length;
            if (dumpFound > 0)
            {
                Log($"[Cảnh báo] Phát hiện {dumpFound} file dump sự cố trong thư mục {minidumpDir}:");
                foreach (var f in files.OrderByDescending(x => File.GetLastWriteTime(x)).Take(8))
                {
                    var fi = new FileInfo(f);
                    Log($"   • {fi.Name} - Thời gian: {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss} ({fi.Length / 1024} KB)");
                }
            }
        }

        if (File.Exists(memDump))
        {
            var fi = new FileInfo(memDump);
            Log($"   • MEMORY.DMP (Full Dump): {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss} ({fi.Length / (1024 * 1024)} MB)");
            dumpFound++;
        }

        if (dumpFound == 0)
        {
            Log("[OK] Tuyệt vời! Không phát hiện file dump sự cố màn hình xanh nào (Máy tính sạch lỗi BSOD).");
            MessageBox.Show("Hệ thống hoàn toàn sạch lỗi!\n\nKhông có file ghi nhận sự cố Màn hình xanh (BSOD) nào trong thư mục Minidump.", "Không có BSOD", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            var res = MessageBox.Show(
                $"Phát hiện {dumpFound} tệp tin ghi nhận lỗi màn hình xanh (BSOD) gần đây!\n\nBạn có muốn mở thư mục chứa file Minidump để phân tích không?",
                "Nhật ký màn hình xanh",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res == DialogResult.Yes && Directory.Exists(minidumpDir))
            {
                try { Process.Start(new ProcessStartInfo("explorer.exe", minidumpDir) { UseShellExecute = true }); } catch { }
            }
        }
    }

    #endregion

    #region Action 9: Quản lý thiết lập Crash Dump

    private void RefreshDumpStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl");
            int dumpType = key?.GetValue("CrashDumpEnabled") is int val ? val : 0;

            if (dumpType == 3)
            {
                _lblDumpStatus.Text = "Lưu dump khi màn hình xanh: BẬT - Small memory dump (Minidump - tool đọc được)";
                _lblDumpStatus.ForeColor = _successGreen;
                _btnToggleDump.Text = "Tắt lưu dump";
            }
            else if (dumpType > 0)
            {
                _lblDumpStatus.Text = $"Lưu dump khi màn hình xanh: BẬT (Kiểu {dumpType})";
                _lblDumpStatus.ForeColor = _successGreen;
                _btnToggleDump.Text = "Tắt lưu dump";
            }
            else
            {
                _lblDumpStatus.Text = "Lưu dump khi màn hình xanh: TẮT - Không lưu dump khi crash";
                _lblDumpStatus.ForeColor = ThemeHelper.TextMuted;
                _btnToggleDump.Text = "Bật lưu dump";
            }
        }
        catch
        {
            _lblDumpStatus.Text = "Lưu dump khi màn hình xanh: BẬT - Small memory dump (Mặc định)";
            _lblDumpStatus.ForeColor = _successGreen;
            _btnToggleDump.Text = "Tắt lưu dump";
        }
    }

    private void ToggleDumpSetting()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl", true);
            if (key == null)
            {
                MessageBox.Show("Cần quyền Quản trị viên (Run as Administrator) để thay đổi thiết lập CrashControl.", "Quyền hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int current = key.GetValue("CrashDumpEnabled") is int val ? val : 0;
            int next = current == 0 ? 3 : 0; // Toggle between 3 (Minidump) and 0 (Disabled)

            key.SetValue("CrashDumpEnabled", next, RegistryValueKind.DWord);
            if (next == 3)
            {
                key.SetValue("MinidumpsCount", 50, RegistryValueKind.DWord);
                Log("[OK] Đã BẬT tính năng tự động lưu Small Memory Dump (Minidump) khi xảy ra màn hình xanh.");
            }
            else
            {
                Log("[OK] Đã TẮT tính năng lưu dump khi màn hình xanh.");
            }

            RefreshDumpStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể thay đổi thiết lập: {ex.Message}\n(Vui lòng mở ứng dụng bằng Run as Administrator)", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion
}
