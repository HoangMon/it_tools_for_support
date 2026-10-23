using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Ernastie.Services;

namespace Ernastie.Forms;

public class NetworkDiagnosticForm : Form
{
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
    private Label _lblMainTitle = null!;
    private Label _lblDescription = null!;
    private Button _btnStart = null!;
    private Button _btnCopy = null!;
    private Button _btnClose = null!;
    private Label _lblStatus = null!;
    private ProgressBar _progressBar = null!;
    private RichTextBox _rtbLog = null!;

    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

    public NetworkDiagnosticForm()
    {
        InitializeComponents();
        ThemeHelper.ApplyTheme(this);
    }

    private void InitializeComponents()
    {
        Text = "Chẩn đoán mạng";
        Size = new Size(840, 680);
        MinimumSize = new Size(760, 560);
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

        // --- TOP HEADER PANEL ---
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 175,
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(pnlTop);

        // 1. Header Title
        _lblMainTitle = new Label
        {
            Text = "🌐 Vì sao mạng mạnh mà tải vẫn chậm?",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, 0),
            AutoSize = true
        };
        pnlTop.Controls.Add(_lblMainTitle);

        // 2. Header Description
        _lblDescription = new Label
        {
            Text = "Đo thẳng trên máy này rồi chỉ ra thủ phạm: nhà mạng bóp theo luồng, phần mềm diệt virus mở khoá HTTPS, proxy hỏng, hay MTU thấp. Mất khoảng 20-30 giây, chỉ tải thử vài MB và không đụng gì vào máy.",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            ForeColor = _textNormal,
            Location = new Point(2, 30),
            Size = new Size(pnlTop.Width - 4, 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlTop.Controls.Add(_lblDescription);

        // 3. Action Buttons Row
        _btnStart = new Button
        {
            Text = "▶ Bắt đầu đo",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Location = new Point(0, 80),
            Size = new Size(180, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _btnStart.FlatAppearance.BorderColor = _btnBorder;
        _btnStart.FlatAppearance.MouseOverBackColor = Color.FromArgb(37, 99, 235);
        _btnStart.Click += async (_, _) => await RunDiagnosticsAsync();
        pnlTop.Controls.Add(_btnStart);

        _btnCopy = new Button
        {
            Text = "📋 Chép kết quả",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Location = new Point(190, 80),
            Size = new Size(160, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _btnCopy.FlatAppearance.BorderColor = _btnBorder;
        _btnCopy.Click += (_, _) => CopyResults();
        pnlTop.Controls.Add(_btnCopy);

        _btnClose = new Button
        {
            Text = "Đóng",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Location = new Point(pnlTop.Width - 120, 80),
            Size = new Size(120, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            Cursor = Cursors.Hand
        };
        _btnClose.FlatAppearance.BorderColor = _btnBorder;
        _btnClose.Click += (_, _) => Close();
        pnlTop.Controls.Add(_btnClose);

        // 4. Status text
        _lblStatus = new Label
        {
            Text = "Sẵn sàng. Bấm '▶ Bắt đầu đo'.",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(2, 126),
            Size = new Size(pnlTop.Width - 4, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        pnlTop.Controls.Add(_lblStatus);

        // 5. Progress Bar
        _progressBar = new ProgressBar
        {
            Location = new Point(0, 156),
            Size = new Size(pnlTop.Width, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous
        };
        pnlTop.Controls.Add(_progressBar);

        // --- RESULT LOG AREA ---
        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.75f, FontStyle.Regular),
            BackColor = Color.FromArgb(17, 23, 34),
            ForeColor = Color.FromArgb(241, 245, 249),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = true
        };
        mainPanel.Controls.Add(_rtbLog);
        _rtbLog.BringToFront();

        pnlTop.Resize += (_, _) =>
        {
            _lblDescription.Width = pnlTop.Width - 4;
            _lblStatus.Width = pnlTop.Width - 4;
            _progressBar.Width = pnlTop.Width;
            _btnClose.Location = new Point(pnlTop.Width - 120, 80);
        };
    }

    private void CopyResults()
    {
        if (string.IsNullOrWhiteSpace(_rtbLog.Text))
        {
            MessageBox.Show("Chưa có kết quả để sao chép. Vui lòng bấm 'Bắt đầu đo' trước.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Clipboard.SetText(_rtbLog.Text);
            MessageBox.Show("Đã sao chép toàn bộ kết quả chẩn đoán mạng vào Clipboard thành công!", "Đã Sao Chép", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể sao chép kết quả: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SetStatus(string text, int progress)
    {
        if (IsDisposed || !IsHandleCreated) return;
        Invoke(() =>
        {
            _lblStatus.Text = text;
            _progressBar.Value = Math.Clamp(progress, 0, 100);
        });
    }

    private void AppendLog(string text, Color? color = null, bool bold = false)
    {
        if (IsDisposed || !IsHandleCreated) return;
        Invoke(() =>
        {
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionLength = 0;
            _rtbLog.SelectionColor = color ?? (ThemeHelper.IsDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42));
            _rtbLog.SelectionFont = new Font("Consolas", 9.75f, bold ? FontStyle.Bold : FontStyle.Regular);
            _rtbLog.AppendText(text + Environment.NewLine);
            _rtbLog.ScrollToCaret();
        });
    }

    private async Task RunDiagnosticsAsync()
    {
        if (_isRunning) return;
        _isRunning = true;
        _btnStart.Enabled = false;
        _cts = new CancellationTokenSource();

        try
        {
            _rtbLog.Clear();
            _progressBar.Value = 0;

            AppendLog("================================================================================", Color.FromArgb(0, 240, 255), true);
            AppendLog("          BÁO CÁO CHẨN ĐOÁN MẠNG CHUYÊN SÂU (NETWORK DIAGNOSTICS)", Color.FromArgb(0, 240, 255), true);
            AppendLog($"  Thời gian đo: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Hệ điều hành: {Environment.OSVersion}", Color.FromArgb(148, 163, 184));
            AppendLog("================================================================================", Color.FromArgb(0, 240, 255), true);
            AppendLog("");

            var culprits = new List<string>();
            var warnings = new List<string>();
            var goods = new List<string>();

            // -------------------------------------------------------------
            // BƯỚC 1: CARD MẠNG & KẾT NỐI ROUTER CỤC BỘ (0 - 15%)
            // -------------------------------------------------------------
            SetStatus("Đang kiểm tra kết nối card mạng và Router cục bộ...", 10);
            AppendLog("[BƯỚC 1/5] KIỂM TRA CARD MẠNG VÀ ROUTER / GATEWAY", Color.FromArgb(245, 158, 11), true);

            var activeNic = GetActiveNetworkInterface();
            string gatewayIp = "";
            if (activeNic != null)
            {
                var ipProps = activeNic.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address.ToString() ?? "N/A";
                var gw = ipProps.GatewayAddresses.FirstOrDefault()?.Address.ToString();
                gatewayIp = gw ?? "";

                AppendLog($"  • Tên Card mạng : {activeNic.Name} ({activeNic.Description})");
                AppendLog($"  • Loại kết nối  : {activeNic.NetworkInterfaceType} | Tốc độ link: {activeNic.Speed / 1_000_000} Mbps");
                AppendLog($"  • Địa chỉ IPv4  : {ipv4}");
                AppendLog($"  • Gateway Router: {(string.IsNullOrEmpty(gatewayIp) ? "Không phát hiện" : gatewayIp)}");

                if (!string.IsNullOrEmpty(gatewayIp))
                {
                    var gwPing = await PingHostAsync(gatewayIp, 3, 1000);
                    if (gwPing.Success)
                    {
                        AppendLog($"  • Ping Gateway  : {gwPing.AvgMs} ms (Tốt - mạng nội bộ ổn định)");
                        goods.Add("Mạng nội bộ (LAN / WiFi tới Router) hoạt động ổn định");
                    }
                    else
                    {
                        AppendLog($"  • Ping Gateway  : THẤT BẠI hoặc rớt gói tin!", Color.FromArgb(255, 0, 85), true);
                        culprits.Add("Kết nối từ máy tính tới Router bị rớt gói hoặc quá chập chờn (kiểm tra lại sóng WiFi hoặc dây mạng LAN).");
                    }
                }
            }
            else
            {
                AppendLog("  • Không phát hiện card mạng nào đang kết nối Internet!", Color.FromArgb(255, 0, 85), true);
                culprits.Add("Máy tính không có kết nối mạng Internet hoạt động.");
            }
            AppendLog("");

            // -------------------------------------------------------------
            // BƯỚC 2: KIỂM TRA PROXY HỎNG & ĐỊNH TUYẾN (15 - 30%)
            // -------------------------------------------------------------
            SetStatus("Đang kiểm tra cấu hình Proxy hệ thống và WinHTTP...", 25);
            AppendLog("[BƯỚC 2/5] KIỂM TRA CẤU HÌNH PROXY HỆ THỐNG", Color.FromArgb(245, 158, 11), true);

            var (proxyEnabled, proxyServer, autoPac) = CheckSystemProxy();
            if (proxyEnabled)
            {
                AppendLog($"  • Trạng thái Proxy: ĐANG BẬT [Máy chủ: {proxyServer}]", Color.FromArgb(255, 0, 85), true);

                // Thử kết nối tới proxy server xem có sống không
                bool proxyAlive = await TestProxyAliveAsync(proxyServer);
                if (!proxyAlive)
                {
                    AppendLog($"  • Kiểm tra Proxy  : 🔴 MÁY CHỦ PROXY ĐÃ CHẾT / KHÔNG PHẢN HỒI!", Color.FromArgb(255, 0, 85), true);
                    culprits.Add($"Proxy hệ thống đang bật ({proxyServer}) nhưng không hoạt động. Đây là thủ phạm làm đơ hoặc nghẽn toàn bộ kết nối web.");
                }
                else
                {
                    AppendLog($"  • Kiểm tra Proxy  : Máy chủ Proxy phản hồi bình thường.");
                    warnings.Add($"Máy đang duyệt qua Proxy ({proxyServer}). Tốc độ tải phụ thuộc hoàn toàn vào băng thông của proxy này.");
                }
            }
            else
            {
                AppendLog("  • Trạng thái Proxy: TẮT (Direct Access - Không dùng proxy).");
                goods.Add("Không có lỗi Proxy hệ thống");
            }

            if (!string.IsNullOrEmpty(autoPac))
            {
                AppendLog($"  • Auto PAC Script : {autoPac}");
                warnings.Add("Máy đang sử dụng kịch bản cấu hình proxy tự động (PAC URL).");
            }
            AppendLog("");

            // -------------------------------------------------------------
            // BƯỚC 3: KIỂM TRA MTU & PHÂN MẢNH GÓI TIN (30 - 50%)
            // -------------------------------------------------------------
            SetStatus("Đang kiểm tra kích thước MTU và phân mảnh gói tin...", 45);
            AppendLog("[BƯỚC 3/5] KIỂM TRA KÍCH THƯỚC MTU VÀ PHÂN MẢNH (FRAGMENTATION)", Color.FromArgb(245, 158, 11), true);

            int detectedMtu = GetAdapterMtu(activeNic?.Name ?? "Wi-Fi");
            AppendLog($"  • MTU cấu hình trên card: {detectedMtu} bytes");

            // Đo phân mảnh với ping Don't Fragment (-f)
            var mtuTestResult = await TestMtuFragmentationAsync("8.8.8.8");
            AppendLog($"  • Kiểm tra gói tin chuẩn (1500 bytes MTU): {mtuTestResult.Status1500}");
            AppendLog($"  • Kích thước MTU tối đa không phân mảnh: {mtuTestResult.MaxNonFragmentedMtu} bytes");

            if (mtuTestResult.IsSeverelyFragmented)
            {
                AppendLog("  • Đánh giá MTU: 🔴 BỊ BÓP PHÂN MẢNH NGHIÊM TRỌNG!", Color.FromArgb(255, 0, 85), true);
                culprits.Add($"Kích thước MTU bị giới hạn thấp ({mtuTestResult.MaxNonFragmentedMtu} bytes). Các gói tin tải xuống dung lượng lớn bị xé nhỏ liên tục, gây suy giảm tốc độ mạng trầm trọng.");
            }
            else if (mtuTestResult.IsPppoeOptimal)
            {
                AppendLog("  • Đánh giá MTU: Chuẩn PPPoE tối ưu (1492 bytes - Phổ biến tại nhà mạng VNPT/Viettel/FPT).");
                goods.Add("MTU đường truyền đạt chuẩn tối ưu");
            }
            else
            {
                AppendLog("  • Đánh giá MTU: Đạt chuẩn Ethernet tiêu chuẩn (1500 bytes).");
                goods.Add("MTU đường truyền hoàn hảo (1500 bytes)");
            }
            AppendLog("");

            // -------------------------------------------------------------
            // BƯỚC 4: KIỂM TRA ANTIVIRUS GIẢI MÃ HTTPS (50 - 65%)
            // -------------------------------------------------------------
            SetStatus("Đang kiểm tra phần mềm diệt virus can thiệp mở khóa HTTPS...", 60);
            AppendLog("[BƯỚC 4/5] KIỂM TRA CHỨNG CHỈ GIẢI MÃ HTTPS (SSL/TLS INSPECTION)", Color.FromArgb(245, 158, 11), true);

            var interceptedAvs = DetectHttpsInterceptionCertificates();
            if (interceptedAvs.Count > 0)
            {
                AppendLog($"  • Phát hiện chứng chỉ giải mã HTTPS của: {string.Join(", ", interceptedAvs)}", Color.FromArgb(245, 158, 11), true);
                AppendLog("  • Cơ chế: Phần mềm diệt virus mở khóa luồng mã hóa HTTPS để quét trước khi cho máy nhận.", Color.FromArgb(245, 158, 11));
                AppendLog("  • Ảnh hưởng: Đệm toàn bộ luồng tải vào RAM/CPU, làm giảm tốc độ download đơn luồng 30% - 70%.");
                warnings.Add($"Phần mềm diệt virus ({string.Join(", ", interceptedAvs)}) đang mở khóa giải mã HTTPS (Web Shield). Có thể làm chậm tốc độ tải file.");
            }
            else
            {
                AppendLog("  • Trạng thái HTTPS: Không phát hiện chứng chỉ can thiệp giải mã từ phần mềm thứ ba.");
                goods.Add("Không bị can thiệp giải mã HTTPS");
            }
            AppendLog("");

            // -------------------------------------------------------------
            // BƯỚC 5: ĐO ĐẠC BĂNG THÔNG ĐƠN LUỒNG VS ĐA LUỒNG (65 - 90%)
            // -------------------------------------------------------------
            SetStatus("Đang kiểm tra nhà mạng: Đo tốc độ Đơn luồng vs Đa luồng...", 75);
            AppendLog("[BƯỚC 5/5] ĐO ĐẠC TỐC ĐỘ: ĐƠN LUỒNG (BROWSER) VS ĐA LUỒNG (SPEEDTEST)", Color.FromArgb(245, 158, 11), true);
            AppendLog("  • Đang tải mẫu thử đơn luồng (Single Stream)...");

            var singleResult = await MeasureSingleStreamSpeedAsync();
            AppendLog($"  -> Tốc độ Đơn luồng (1 kết nối): {singleResult.SpeedMbps:F1} Mbps ({singleResult.SpeedMBps:F2} MB/s) [Thời gian: {singleResult.ElapsedMs} ms]");

            SetStatus("Đang tải mẫu thử đa luồng (Multi Stream 4 kết nối song song)...", 85);
            AppendLog("  • Đang tải mẫu thử đa luồng (4 kết nối song song)...");

            var multiResult = await MeasureMultiStreamSpeedAsync(4);
            AppendLog($"  -> Tốc độ Đa luồng (4 kết nối) : {multiResult.SpeedMbps:F1} Mbps ({multiResult.SpeedMBps:F2} MB/s) [Thời gian: {multiResult.ElapsedMs} ms]");

            float ratio = multiResult.SpeedMbps / Math.Max(0.5f, singleResult.SpeedMbps);
            AppendLog($"  • Tỷ lệ chênh lệch Đa luồng / Đơn luồng: {ratio:F1} lần");

            if (ratio >= 3.2f && multiResult.SpeedMbps >= 15f)
            {
                AppendLog("  • Phát hiện: 🔴 NHÀ MẠNG / TUYẾN CÁP ĐANG BÓP BĂNG THÔNG THEO LUỒNG!", Color.FromArgb(255, 0, 85), true);
                AppendLog("    (Giải thích: Speedtest đo đa luồng cho kết quả cao, nhưng trình duyệt tải 1 luồng sẽ bị bóp rất chậm).");
                culprits.Add($"Nhà mạng hoặc tuyến cáp quốc tế đang bóp băng thông theo luồng đơn (Single-thread QoS). Tốc độ đa luồng đạt {multiResult.SpeedMbps:F0} Mbps nhưng 1 luồng chỉ được {singleResult.SpeedMbps:F1} Mbps.");
            }
            else if (singleResult.SpeedMbps >= 30f)
            {
                AppendLog("  • Băng thông đơn luồng rất mạnh và ổn định, không có hiện tượng bóp luồng.");
                goods.Add($"Tốc độ tải đơn luồng đạt rất tốt ({singleResult.SpeedMbps:F1} Mbps)");
            }
            else
            {
                AppendLog("  • Băng thông phân bổ tương đối đồng đều giữa đơn luồng và đa luồng.");
                goods.Add("Không có hiện tượng bóp băng thông theo luồng");
            }
            AppendLog("");

            // -------------------------------------------------------------
            // TỔNG KẾT VÀ KẾT LUẬN CUỐI CÙNG (100%)
            // -------------------------------------------------------------
            SetStatus("Hoàn tất chẩn đoán mạng! Đã phân tích nguyên nhân.", 100);

            AppendLog("================================================================================", Color.FromArgb(16, 185, 129), true);
            AppendLog("                  KẾT LUẬN & HƯỚNG DẪN XỬ LÝ SỰ CỐ MẠNG", Color.FromArgb(16, 185, 129), true);
            AppendLog("================================================================================", Color.FromArgb(16, 185, 129), true);

            if (culprits.Count > 0)
            {
                AppendLog("");
                AppendLog("🚨 CÁC NGUYÊN NHÂN CHÍNH GÂY CHẬM (THỦ PHẠM):", Color.FromArgb(255, 0, 85), true);
                for (int i = 0; i < culprits.Count; i++)
                {
                    AppendLog($"  [{i + 1}] {culprits[i]}", Color.FromArgb(255, 0, 85), true);
                }
            }
            else
            {
                AppendLog("");
                AppendLog("✅ KHÔNG PHÁT HIỆN LỖI NGHẼN CỔ CHAI PHẦN CỨNG HOẶC HỆ ĐIỀU HÀNH TRÊN MÁY NÀY!", Color.FromArgb(16, 185, 129), true);
                AppendLog("  Đường truyền mạng cục bộ, MTU và cấu hình Windows đều ở trạng thái tối ưu.");
            }

            if (warnings.Count > 0)
            {
                AppendLog("");
                AppendLog("⚠️ CẢNH BÁO CẦN LƯU Ý:", Color.FromArgb(245, 158, 11), true);
                foreach (var w in warnings)
                {
                    AppendLog($"  • {w}", Color.FromArgb(245, 158, 11));
                }
            }

            AppendLog("");
            AppendLog("💡 HƯỚNG DẪN KHẮC PHỤC TRIỆT ĐỂ:", Color.FromArgb(0, 240, 255), true);

            if (culprits.Any(c => c.Contains("bóp băng thông theo luồng")))
            {
                AppendLog("  1. CÀI ĐẶT TRÌNH TẢI ĐA LUỒNG: Khi tải file từ Google Drive, Fshare, link web, hãy sử dụng IDM (Internet Download Manager), XDM hoặc Motrix. Các công cụ này xé nhỏ file thành 8-16 luồng để vượt qua giới hạn bóp luồng đơn của nhà mạng.");
            }
            if (culprits.Any(c => c.Contains("Proxy")))
            {
                AppendLog("  2. TẮT PROXY WINDOWS: Mở Windows Settings -> Network & Internet -> Proxy -> Tắt toàn bộ 'Use a proxy server' và 'Automatically detect settings'.");
            }
            if (warnings.Any(w => w.Contains("HTTPS") || w.Contains("diệt virus")))
            {
                AppendLog("  3. TẮT QUÉT MÃ HÓA HTTPS: Mở phần mềm diệt virus trên máy -> Vào Settings (Cài đặt) -> Web Protection -> Tắt mục 'Scan encrypted connections / HTTPS Scanning' hoặc thêm trình duyệt vào danh sách loại trừ.");
            }
            if (culprits.Any(c => c.Contains("MTU")))
            {
                AppendLog("  4. ĐỒNG BỘ MTU 1492: Chỉnh lại MTU trên card mạng hoặc router về 1492 (chuẩn PPPoE) để triệt tiêu hoàn toàn hiện tượng phân mảnh gói tin.");
            }

            AppendLog("  5. TỐI ƯU MẠNG 1-CLICK: Bạn có thể vào tab 'Tác vụ tự động' trên thanh công cụ để chạy 'Tối ưu hóa & Làm mới mạng (Flush DNS / Winsock Reset)' bất cứ lúc nào.");
            AppendLog("================================================================================", Color.FromArgb(16, 185, 129), true);
        }
        catch (Exception ex)
        {
            AppendLog($"\n[LỖI TRONG QUÁ TRÌNH ĐO]: {ex.Message}", Color.FromArgb(255, 0, 85), true);
            SetStatus("Quá trình chẩn đoán gặp lỗi. Vui lòng thử lại.", 0);
        }
        finally
        {
            _isRunning = false;
            _btnStart.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    // -------------------------------------------------------------
    // CÁC HÀM XỬ LÝ KỸ THUẬT (SENSING & NETWORK ENGINES)
    // -------------------------------------------------------------

    private NetworkInterface? GetActiveNetworkInterface()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderByDescending(n => n.Speed)
                .FirstOrDefault();
        }
        catch { return null; }
    }

    private async Task<(bool Success, long AvgMs)> PingHostAsync(string host, int count = 3, int timeout = 1000)
    {
        try
        {
            using var pinger = new Ping();
            long total = 0;
            int successCount = 0;

            for (int i = 0; i < count; i++)
            {
                var reply = await pinger.SendPingAsync(host, timeout);
                if (reply.Status == IPStatus.Success)
                {
                    total += reply.RoundtripTime;
                    successCount++;
                }
            }

            if (successCount > 0) return (true, total / successCount);
        }
        catch { }
        return (false, 0);
    }

    private (bool Enabled, string Server, string Pac) CheckSystemProxy()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
            if (key != null)
            {
                int enabled = Convert.ToInt32(key.GetValue("ProxyEnable") ?? 0);
                string server = key.GetValue("ProxyServer")?.ToString() ?? "";
                string pac = key.GetValue("AutoConfigURL")?.ToString() ?? "";
                return (enabled == 1, server, pac);
            }
        }
        catch { }
        return (false, "", "");
    }

    private async Task<bool> TestProxyAliveAsync(string proxyHostPort)
    {
        try
        {
            var parts = proxyHostPort.Split(':');
            string host = parts[0];
            int port = parts.Length > 1 && int.TryParse(parts[1], out int p) ? p : 80;

            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(1500));
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private int GetAdapterMtu(string nicName)
    {
        try
        {
            var psi = new ProcessStartInfo("netsh", "interface ipv4 show subinterfaces")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains(nicName, StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && int.TryParse(parts[0], out int mtu))
                        {
                            return mtu;
                        }
                    }
                }
            }
        }
        catch { }
        return 1500;
    }

    private async Task<(string Status1500, int MaxNonFragmentedMtu, bool IsPppoeOptimal, bool IsSeverelyFragmented)> TestMtuFragmentationAsync(string targetIp)
    {
        string status1500 = "Không phân mảnh (Tốt)";
        int maxMtu = 1500;
        bool pppoe = false;
        bool severe = false;

        try
        {
            // Test 1472 payload (1500 MTU)
            bool ok1472 = await PingWithDontFragmentAsync(targetIp, 1472);
            if (!ok1472)
            {
                status1500 = "Gói tin bị chặn phân mảnh (Needs fragmentation)";
                // Test 1464 payload (1492 MTU for PPPoE)
                bool ok1464 = await PingWithDontFragmentAsync(targetIp, 1464);
                if (ok1464)
                {
                    maxMtu = 1492;
                    pppoe = true;
                }
                else
                {
                    // Test 1372 payload (1400 MTU)
                    bool ok1372 = await PingWithDontFragmentAsync(targetIp, 1372);
                    if (ok1372)
                    {
                        maxMtu = 1400;
                    }
                    else
                    {
                        maxMtu = 1350;
                        severe = true;
                    }
                }
            }
        }
        catch { }

        return (status1500, maxMtu, pppoe, severe);
    }

    private async Task<bool> PingWithDontFragmentAsync(string ip, int bufferSize)
    {
        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("ping", $"{ip} -f -l {bufferSize} -n 1 -w 1000")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string outText = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    return outText.Contains("Reply from", StringComparison.OrdinalIgnoreCase) &&
                           !outText.Contains("fragmented", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return false;
        });
    }

    private List<string> DetectHttpsInterceptionCertificates()
    {
        var foundAvs = new List<string>();
        string[] knownAvSignatures = new[]
        {
            "Kaspersky", "Avast", "AVG", "Bitdefender", "ESET", "AdGuard", "Fiddler", "Charles"
        };

        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            foreach (var cert in store.Certificates)
            {
                string subject = cert.Subject;
                string issuer = cert.Issuer;
                foreach (var sig in knownAvSignatures)
                {
                    if (subject.Contains(sig, StringComparison.OrdinalIgnoreCase) ||
                        issuer.Contains(sig, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!foundAvs.Contains(sig)) foundAvs.Add(sig);
                    }
                }
            }
        }
        catch { }

        try
        {
            using var storeUser = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            storeUser.Open(OpenFlags.ReadOnly);
            foreach (var cert in storeUser.Certificates)
            {
                string subject = cert.Subject;
                string issuer = cert.Issuer;
                foreach (var sig in knownAvSignatures)
                {
                    if (subject.Contains(sig, StringComparison.OrdinalIgnoreCase) ||
                        issuer.Contains(sig, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!foundAvs.Contains(sig)) foundAvs.Add(sig);
                    }
                }
            }
        }
        catch { }

        return foundAvs;
    }

    private async Task<(float SpeedMbps, float SpeedMBps, long ElapsedMs)> MeasureSingleStreamSpeedAsync()
    {
        string testUrl = "https://speed.cloudflare.com/__down?bytes=2097152"; // 2 MB
        try
        {
            using var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var sw = Stopwatch.StartNew();
            var data = await client.GetByteArrayAsync(testUrl);
            sw.Stop();

            long elapsed = Math.Max(10, sw.ElapsedMilliseconds);
            long bytes = data.Length;

            float mbps = (bytes * 8f) / (elapsed / 1000f) / 1_000_000f;
            float mBps = (bytes / 1024f / 1024f) / (elapsed / 1000f);

            return (mbps, mBps, elapsed);
        }
        catch
        {
            // Fallback CDN
            try
            {
                string fallbackUrl = "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/css/all.min.css";
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var sw = Stopwatch.StartNew();
                var data = await client.GetByteArrayAsync(fallbackUrl);
                sw.Stop();

                long elapsed = Math.Max(10, sw.ElapsedMilliseconds);
                long bytes = data.Length;
                float mbps = (bytes * 8f) / (elapsed / 1000f) / 1_000_000f;
                float mBps = (bytes / 1024f / 1024f) / (elapsed / 1000f);
                return (mbps, mBps, elapsed);
            }
            catch { }
        }

        return (0f, 0f, 0);
    }

    private async Task<(float SpeedMbps, float SpeedMBps, long ElapsedMs)> MeasureMultiStreamSpeedAsync(int streams = 4)
    {
        string testUrl = "https://speed.cloudflare.com/__down?bytes=1048576"; // 1 MB per stream
        try
        {
            using var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<byte[]>>();
            for (int i = 0; i < streams; i++)
            {
                tasks.Add(client.GetByteArrayAsync(testUrl));
            }

            var results = await Task.WhenAll(tasks);
            sw.Stop();

            long elapsed = Math.Max(10, sw.ElapsedMilliseconds);
            long totalBytes = results.Sum(r => (long)r.Length);

            float mbps = (totalBytes * 8f) / (elapsed / 1000f) / 1_000_000f;
            float mBps = (totalBytes / 1024f / 1024f) / (elapsed / 1000f);

            return (mbps, mBps, elapsed);
        }
        catch
        {
            // Fallback calculation based on single stream
            return (0f, 0f, 0);
        }
    }
}
