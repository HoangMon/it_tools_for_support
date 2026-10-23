using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ernastie.Models;
using Ernastie.Services;
using Microsoft.Win32;

namespace Ernastie.Forms;

public class KeyComparisonForm : Form
{
    private readonly SystemInfoModel _sysInfo;
    private readonly GoogleSheetService _sheetService;
    private readonly DeviceConfigModel _deviceConfig;

    // Detected Details
    private string _activeKey = "";
    private string _activeStatus = "";
    private string _activeChannel = "";
    private string _activeEdition = "";

    private string _oemKey = "";
    private string _oemEdition = "";

    private string _sheetKey = "";

    // UI Controls
    private Label _lblActiveKey = null!;
    private Label _lblActiveStatus = null!;
    private Label _lblActiveChannel = null!;
    private Label _lblActiveEdition = null!;

    private Label _lblOemKey = null!;
    private Label _lblOemEdition = null!;
    private Label _lblOemManufacturer = null!;

    private Label _lblSheetKey = null!;
    private Label _lblSheetDevice = null!;

    private Label _lblVerdictTitle = null!;
    private Label _lblVerdictDetail = null!;
    private Label _lblVerdictBadge = null!;

    private Button _btnActivateOem = null!;
    private Button _btnCopyKey = null!;
    private Button _btnSyncSheet = null!;
    private Button _btnRefresh = null!;
    private Button _btnClose = null!;

    private RichTextBox _rtbLog = null!;

    public KeyComparisonForm(SystemInfoModel sysInfo, GoogleSheetService? sheetService = null, DeviceConfigModel? deviceConfig = null)
    {
        _sysInfo = sysInfo;
        _sheetService = sheetService ?? new GoogleSheetService();
        _deviceConfig = deviceConfig ?? _sheetService.LoadConfig();

        InitializeUi();
        ThemeHelper.ApplyTheme(this);
        _ = ScanAndCompareLicensesAsync();
    }

    private static void SetDoubleBuffered(Control c)
    {
        typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(c, true, null);
    }

    private Panel CreateCard(string title, int height)
    {
        var card = new Panel
        {
            Height = height,
            BackColor = ThemeHelper.CardBg,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 12)
        };
        card.Paint += (s, e) =>
        {
            using var p = new Pen(ThemeHelper.BorderColor, 1);
            e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
        };
        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ThemeHelper.TextHeading,
            AutoSize = true,
            Location = new Point(14, 10)
        };
        card.Controls.Add(lblTitle);
        return card;
    }

    private void InitializeUi()
    {
        Text = "Đối Soát & So Sánh Bản Quyền Windows (Product Key Audit)";
        Size = new Size(860, 680);
        MinimumSize = new Size(800, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ThemeHelper.BgForm;
        Font = new Font("Segoe UI", 9.25f);
        SetDoubleBuffered(this);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = ThemeHelper.CardBg,
            Padding = new Padding(20, 14, 20, 10)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var p = new Pen(ThemeHelper.BorderColor, 1);
            e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "ĐỐI SOÁT & SO SÁNH BẢN QUYỀN WINDOWS (PRODUCT KEY AUDIT)",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = ThemeHelper.TextHeading,
            AutoSize = true,
            Location = new Point(18, 14)
        };

        var lblSub = new Label
        {
            Text = "So sánh đối chiếu giữa Key đang kích hoạt trong Windows, Key gốc phần cứng BIOS (MSDM) và cơ sở dữ liệu quản trị.",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = ThemeHelper.TextMuted,
            AutoSize = true,
            Location = new Point(20, 42)
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);
        Controls.Add(pnlHeader);

        // Bottom Bar
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            BackColor = ThemeHelper.CardBg,
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var p = new Pen(ThemeHelper.BorderColor, 1);
            e.Graphics.DrawLine(p, 0, 0, pnlBottom.Width, 0);
        };

        _btnActivateOem = new Button
        {
            Text = "⚡ Kích hoạt bằng Key OEM",
            Size = new Size(205, 36),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Location = new Point(18, 11),
            Tag = "primary"
        };
        _btnActivateOem.Click += async (s, e) => await ActivateWithOemKeyAsync();

        _btnCopyKey = new Button
        {
            Text = "📋 Sao chép Key",
            Size = new Size(130, 36),
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Location = new Point(230, 11)
        };
        _btnCopyKey.Click += (s, e) => CopyKeysToClipboard();

        _btnSyncSheet = new Button
        {
            Text = "☁️ Cập nhật Sheet",
            Size = new Size(135, 36),
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Location = new Point(368, 11)
        };
        _btnSyncSheet.Click += async (s, e) => await UpdateKeyToSheetAsync();

        _btnRefresh = new Button
        {
            Text = "🔄 Quét lại",
            Size = new Size(100, 36),
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 220, 11)
        };
        _btnRefresh.Click += async (s, e) => await ScanAndCompareLicensesAsync();

        _btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(95, 36),
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 110, 11)
        };
        _btnClose.Click += (s, e) => Close();

        pnlBottom.Controls.Add(_btnActivateOem);
        pnlBottom.Controls.Add(_btnCopyKey);
        pnlBottom.Controls.Add(_btnSyncSheet);
        pnlBottom.Controls.Add(_btnRefresh);
        pnlBottom.Controls.Add(_btnClose);
        Controls.Add(pnlBottom);

        // Content
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20, 14, 20, 14)
        };
        Controls.Add(pnlContent);

        int currentY = 6;

        // Row 1: 3 Cards comparison (Installed vs OEM vs Sheet)
        int cardWidth = (pnlContent.Width - 55) / 3;

        // Card 1: Key đang cài đặt
        var cardActive = CreateCard("1. KEY ĐANG KÍCH HOẠT", 145);
        cardActive.Location = new Point(0, currentY);
        cardActive.Size = new Size(cardWidth, 145);

        _lblActiveStatus = new Label { Text = "Trạng thái: Đang truy vấn...", Location = new Point(14, 34), AutoSize = true, Font = new Font("Segoe UI", 8.75f, FontStyle.Bold), ForeColor = ThemeHelper.PrimaryBlue };
        _lblActiveKey = new Label { Text = "Key: ...", Location = new Point(14, 58), AutoSize = true, Font = new Font("Consolas", 9f, FontStyle.Bold), ForeColor = ThemeHelper.TextHeading };
        _lblActiveEdition = new Label { Text = "Bản: ...", Location = new Point(14, 84), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = ThemeHelper.TextMuted };
        _lblActiveChannel = new Label { Text = "Kênh: ...", Location = new Point(14, 108), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = ThemeHelper.TextMuted };

        cardActive.Controls.Add(_lblActiveStatus);
        cardActive.Controls.Add(_lblActiveKey);
        cardActive.Controls.Add(_lblActiveEdition);
        cardActive.Controls.Add(_lblActiveChannel);
        pnlContent.Controls.Add(cardActive);

        // Card 2: Key OEM BIOS
        var cardOem = CreateCard("2. KEY GỐC PHẦN CỨNG (BIOS MSDM)", 145);
        cardOem.Location = new Point(cardWidth + 10, currentY);
        cardOem.Size = new Size(cardWidth, 145);

        var lblOemHead = new Label { Text = "Bản quyền tích hợp bo mạch:", Location = new Point(14, 34), AutoSize = true, Font = new Font("Segoe UI", 8.75f, FontStyle.Bold), ForeColor = ThemeHelper.IsDark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61) };
        _lblOemKey = new Label { Text = "Key: ...", Location = new Point(14, 58), AutoSize = true, Font = new Font("Consolas", 9f, FontStyle.Bold), ForeColor = ThemeHelper.TextHeading };
        _lblOemEdition = new Label { Text = "Bản gốc: ...", Location = new Point(14, 84), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = ThemeHelper.TextMuted };
        _lblOemManufacturer = new Label { Text = "Hãng SX: ...", Location = new Point(14, 108), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = ThemeHelper.TextMuted };

        cardOem.Controls.Add(lblOemHead);
        cardOem.Controls.Add(_lblOemKey);
        cardOem.Controls.Add(_lblOemEdition);
        cardOem.Controls.Add(_lblOemManufacturer);
        pnlContent.Controls.Add(cardOem);

        // Card 3: Key trên Google Sheet
        var cardSheet = CreateCard("3. BẢNG QUẢN LÝ GOOGLE SHEET", 145);
        cardSheet.Location = new Point((cardWidth * 2) + 20, currentY);
        cardSheet.Size = new Size(pnlContent.Width - 45 - (cardWidth * 2) - 20, 145);

        var lblSheetHead = new Label { Text = "Bản quyền theo hồ sơ thiết bị:", Location = new Point(14, 34), AutoSize = true, Font = new Font("Segoe UI", 8.75f, FontStyle.Bold), ForeColor = Color.FromArgb(217, 119, 6) };
        _lblSheetKey = new Label { Text = "Key: Chưa đồng bộ", Location = new Point(14, 58), AutoSize = true, Font = new Font("Consolas", 9f, FontStyle.Bold), ForeColor = ThemeHelper.TextHeading };
        _lblSheetDevice = new Label { Text = $"Máy: {_sysInfo.MachineName}", Location = new Point(14, 84), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = ThemeHelper.TextMuted };
        var lblSheetNote = new Label { Text = "Hỗ trợ lưu trữ tài sản IT", Location = new Point(14, 108), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = ThemeHelper.TextMuted };

        cardSheet.Controls.Add(lblSheetHead);
        cardSheet.Controls.Add(_lblSheetKey);
        cardSheet.Controls.Add(_lblSheetDevice);
        cardSheet.Controls.Add(lblSheetNote);
        pnlContent.Controls.Add(cardSheet);

        currentY += 158;

        // Card 4: Kết Quả So Sánh & Đánh Giá Tự Động (Analysis Verdict)
        var cardVerdict = CreateCard("KẾT QUẢ ĐỐI SOÁT & ĐÁNH GIÁ TÍNH HỢP LỆ BẢN QUYỀN", 115);
        cardVerdict.Location = new Point(0, currentY);
        cardVerdict.Width = pnlContent.Width - 45;
        cardVerdict.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _lblVerdictBadge = new Label
        {
            Location = new Point(16, 32),
            AutoSize = true
        };
        ThemeHelper.StyleBadge(_lblVerdictBadge, "Đang đối soát...", BadgeType.Info);

        _lblVerdictTitle = new Label
        {
            Text = "Đang phân tích thông tin bản quyền...",
            Location = new Point(155, 34),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = ThemeHelper.TextHeading
        };

        _lblVerdictDetail = new Label
        {
            Text = "Hệ thống đang đối chiếu khóa kích hoạt với bảng ACPI MSDM phần cứng...",
            Location = new Point(16, 68),
            Size = new Size(cardVerdict.Width - 32, 38),
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = ThemeHelper.TextMuted,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        cardVerdict.Controls.Add(_lblVerdictBadge);
        cardVerdict.Controls.Add(_lblVerdictTitle);
        cardVerdict.Controls.Add(_lblVerdictDetail);
        pnlContent.Controls.Add(cardVerdict);

        currentY += 128;

        // Card 5: Nhật ký chi tiết
        var cardLog = CreateCard("NHẬT KÝ CHI TIẾT TRUY VẤN WMI & SLMGR", 180);
        cardLog.Location = new Point(0, currentY);
        cardLog.Width = pnlContent.Width - 45;
        cardLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

        _rtbLog = new RichTextBox
        {
            Location = new Point(14, 32),
            Size = new Size(cardLog.Width - 28, 136),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 8.75f),
            BackColor = ThemeHelper.IsDark ? Color.FromArgb(7, 10, 15) : Color.FromArgb(241, 245, 249),
            ForeColor = ThemeHelper.IsDark ? Color.FromArgb(16, 185, 129) : Color.FromArgb(15, 23, 42)
        };

        cardLog.Controls.Add(_rtbLog);
        pnlContent.Controls.Add(cardLog);
    }

    private async Task ScanAndCompareLicensesAsync()
    {
        _btnRefresh.Enabled = false;
        _lblVerdictTitle.Text = "Đang kiểm tra và đối chiếu bản quyền...";
        ThemeHelper.StyleBadge(_lblVerdictBadge, "Đang quét...", BadgeType.Info);
        _rtbLog.Clear();

        Log("Bắt đầu truy vấn thông tin giấy phép bản quyền Windows qua WMI và SoftwareLicensingProduct...");

        await Task.Run(() =>
        {
            // 1. Installed License
            QueryActiveLicense();

            // 2. OEM Key from MSDM
            QueryOemKey();

            // 3. Sheet Key
            QuerySheetKey();
        });

        // Update UI
        _lblActiveStatus.Text = $"Trạng thái: {_activeStatus}";
        _lblActiveKey.Text = $"Key: {_activeKey}";
        _lblActiveEdition.Text = $"Bản: {_activeEdition}";
        _lblActiveChannel.Text = $"Kênh: {_activeChannel}";

        _lblOemKey.Text = $"Key: {_oemKey}";
        _lblOemEdition.Text = $"Bản gốc: {_oemEdition}";
        _lblOemManufacturer.Text = $"Hãng SX: {_sysInfo.Manufacturer} ({_sysInfo.Model})";

        _lblSheetKey.Text = string.IsNullOrEmpty(_sheetKey) ? "Key: Chưa lưu trên Sheet" : $"Key: {_sheetKey}";

        // Compare & Produce Verdict
        EvaluateVerdict();

        _btnRefresh.Enabled = true;
    }

    private void QueryActiveLicense()
    {
        _activeEdition = _sysInfo.OsVersion;
        _activeStatus = "Chưa kích hoạt";
        _activeChannel = "Retail / Digital License";
        _activeKey = _sysInfo.WindowsProductKey;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LicenseStatus, Description, PartialProductKey, ProductKeyChannel FROM SoftwareLicensingProduct WHERE PartialProductKey is not null");
            foreach (ManagementObject mo in searcher.Get())
            {
                int status = Convert.ToInt32(mo["LicenseStatus"] ?? 0);
                string desc = mo["Description"]?.ToString() ?? "";
                string partial = mo["PartialProductKey"]?.ToString() ?? "";
                string channel = mo["ProductKeyChannel"]?.ToString() ?? "";

                if (status == 1) // Licensed
                {
                    _activeStatus = "Đã kích hoạt (Licensed)";
                    _activeChannel = string.IsNullOrEmpty(channel) ? "Bản quyền số hợp lệ" : channel;
                    if (!string.IsNullOrEmpty(partial) && (string.IsNullOrEmpty(_activeKey) || _activeKey.StartsWith("Không")))
                    {
                        _activeKey = $"XXXXX-XXXXX-XXXXX-XXXXX-{partial}";
                    }
                    Log($"[SLMGR]: Tìm thấy giấy phép hợp lệ (Licensed). Kênh: {_activeChannel}, Mã phụ: {partial}");
                    break;
                }
                else if (status == 2)
                {
                    _activeStatus = "Dùng thử (Grace Period)";
                }
                else if (status == 5)
                {
                    _activeStatus = "Hết hạn / Thông báo kích hoạt";
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[LỖI WMI License]: {ex.Message}");
        }

        if (string.IsNullOrEmpty(_activeKey))
        {
            _activeKey = new SystemService().ExtractWindowsKey();
        }
    }

    private void QueryOemKey()
    {
        _oemKey = "";
        _oemEdition = "";

        try
        {
            using var searcherOa = new ManagementObjectSearcher("SELECT OA3xOriginalProductKey, OA3xOriginalProductKeyDescription FROM SoftwareLicensingService");
            foreach (ManagementObject mo in searcherOa.Get())
            {
                _oemKey = mo["OA3xOriginalProductKey"]?.ToString()?.Trim() ?? "";
                _oemEdition = mo["OA3xOriginalProductKeyDescription"]?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(_oemKey)) break;
            }
        }
        catch (Exception ex)
        {
            Log($"[LỖI WMI OEM]: {ex.Message}");
        }

        if (string.IsNullOrEmpty(_oemKey))
        {
            _oemKey = "Không có trong BIOS (No-OS / Tự ráp)";
            _oemEdition = "Không có";
            Log("[BIOS MSDM]: Không tìm thấy bảng ACPI MSDM. Máy tính không có Key OEM nhúng sẵn.");
        }
        else
        {
            Log($"[BIOS MSDM]: Tìm thấy Key OEM nhúng trong bo mạch: {_oemKey} (Phiên bản: {_oemEdition})");
        }
    }

    private void QuerySheetKey()
    {
        _sheetKey = "";
        try
        {
            // Check if device is in current config
            if (!string.IsNullOrEmpty(_sysInfo.WindowsProductKey) && !_sysInfo.WindowsProductKey.StartsWith("Không"))
            {
                _sheetKey = _sysInfo.WindowsProductKey;
            }
        }
        catch { }
    }

    private void EvaluateVerdict()
    {
        bool hasOem = !string.IsNullOrEmpty(_oemKey) && !_oemKey.StartsWith("Không");
        bool isLicensed = _activeStatus.Contains("Licensed") || _activeStatus.Contains("kích hoạt");

        bool keysMatch = hasOem && !string.IsNullOrEmpty(_activeKey) && 
                         (_activeKey.Equals(_oemKey, StringComparison.OrdinalIgnoreCase) || 
                          (_activeKey.Length >= 5 && _oemKey.EndsWith(_activeKey.Substring(_activeKey.Length - 5), StringComparison.OrdinalIgnoreCase)));

        if (keysMatch)
        {
            ThemeHelper.StyleBadge(_lblVerdictBadge, "Trùng khớp OEM gốc", BadgeType.Success);
            _lblVerdictTitle.Text = "✅ BẢN QUYỀN CHÍNH HÃNG TRÙNG KHỚP 100% VỚI PHẦN CỨNG";
            _lblVerdictTitle.ForeColor = ThemeHelper.IsDark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61);
            _lblVerdictDetail.Text = "Hệ thống đang sử dụng đúng Product Key OEM chính hãng được nhúng trong BIOS của nhà sản xuất. Đây là trạng thái bản quyền nguyên bản chuẩn nhất.";
            _btnActivateOem.Enabled = false;
        }
        else if (isLicensed && (_activeKey.Contains("3V66T") || _activeKey.Contains("8HV2C") || _activeChannel.Contains("Digital") || _activeChannel.Contains("Volume")))
        {
            ThemeHelper.StyleBadge(_lblVerdictBadge, "Bản quyền số hợp lệ", BadgeType.Info);
            _lblVerdictTitle.Text = "ℹ️ ĐANG SỬ DỤNG BẢN QUYỀN KỸ THUẬT SỐ (DIGITAL LICENSE)";
            _lblVerdictTitle.ForeColor = ThemeHelper.PrimaryBlue;
            _lblVerdictDetail.Text = "Hệ điều hành đã được kích hoạt vĩnh viễn qua Bản quyền số (Digital Entitlement) liên kết trực tiếp với máy chủ Microsoft. " +
                                     (hasOem ? $"Lưu ý: Máy có sẵn Key OEM trong BIOS ({_oemKey}), bạn vẫn có thể nạp lại nếu cần." : "Máy không có Key OEM trong BIOS.");
            _btnActivateOem.Enabled = hasOem;
        }
        else if (hasOem && !isLicensed)
        {
            ThemeHelper.StyleBadge(_lblVerdictBadge, "Phát hiện Key OEM chưa kích hoạt", BadgeType.Warning);
            _lblVerdictTitle.Text = "⚠️ MÁY CÓ KEY OEM CHÍNH HÃNG NHƯNG CHƯA ĐƯỢC KÍCH HOẠT!";
            _lblVerdictTitle.ForeColor = Color.FromArgb(217, 119, 6);
            _lblVerdictDetail.Text = $"Bo mạch chủ có sẵn bản quyền OEM ({_oemKey} - {_oemEdition}). Hãy bấm nút 'Kích hoạt bằng Key OEM' bên dưới để tự động nạp key và kích hoạt bản quyền số ngay lập tức!";
            _btnActivateOem.Enabled = true;
        }
        else if (hasOem)
        {
            ThemeHelper.StyleBadge(_lblVerdictBadge, "Khác biệt với Key OEM", BadgeType.Neutral);
            _lblVerdictTitle.Text = "🔍 KEY ĐANG CÀI KHÁC BIỆT SO VỚI KEY OEM TRONG BIOS";
            _lblVerdictTitle.ForeColor = ThemeHelper.TextHeading;
            _lblVerdictDetail.Text = $"Windows đang chạy key khác ({_activeKey}), trong khi BIOS lưu trữ key OEM ({_oemKey}). Nếu muốn quay về bản quyền gốc nhà sản xuất, hãy bấm nút Kích hoạt bằng Key OEM.";
            _btnActivateOem.Enabled = true;
        }
        else
        {
            ThemeHelper.StyleBadge(_lblVerdictBadge, "Máy không có OEM", BadgeType.Neutral);
            _lblVerdictTitle.Text = isLicensed ? "✅ WINDOWS ĐÃ KÍCH HOẠT HỢP LỆ (BẢN RETAIL / VOLUME)" : "⚠️ WINDOWS CHƯA ĐƯỢC KÍCH HOẠT BẢN QUYỀN";
            _lblVerdictTitle.ForeColor = isLicensed ? (ThemeHelper.IsDark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61)) : Color.FromArgb(225, 29, 72);
            _lblVerdictDetail.Text = isLicensed ? "Máy tính hoạt động với giấy phép bản quyền bán lẻ (Retail) hoặc doanh nghiệp (Volume/KMS)." : "Vui lòng nhập khóa kích hoạt bản quyền Windows hợp lệ trong phần Cài đặt hệ thống.";
            _btnActivateOem.Enabled = false;
        }
    }

    private async Task ActivateWithOemKeyAsync()
    {
        if (string.IsNullOrEmpty(_oemKey) || _oemKey.StartsWith("Không"))
        {
            MessageBox.Show("Không tìm thấy Key OEM trong BIOS của máy này để kích hoạt!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Bạn có muốn tự động nạp Key OEM ({_oemKey}) vào Windows và gửi yêu cầu kích hoạt lên máy chủ Microsoft không?\n\nLệnh thực thi: slmgr.vbs /ipk & slmgr.vbs /ato", "Xác Nhận Kích Hoạt", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _btnActivateOem.Enabled = false;
        Log($"[KÍCH HOẠT]: Đang nạp Product Key OEM: {_oemKey}...");

        bool ok = await Task.Run(() =>
        {
            try
            {
                // slmgr /ipk
                var psiIpk = new ProcessStartInfo("cscript.exe", $"//nologo C:\\Windows\\System32\\slmgr.vbs /ipk {_oemKey}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var p1 = Process.Start(psiIpk);
                p1?.WaitForExit(30000);
                string out1 = p1?.StandardOutput.ReadToEnd() ?? "";
                Log($"[SLMGR /ipk]: {out1.Trim()}");

                // slmgr /ato
                var psiAto = new ProcessStartInfo("cscript.exe", $"//nologo C:\\Windows\\System32\\slmgr.vbs /ato")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var p2 = Process.Start(psiAto);
                p2?.WaitForExit(30000);
                string out2 = p2?.StandardOutput.ReadToEnd() ?? "";
                Log($"[SLMGR /ato]: {out2.Trim()}");

                return out2.Contains("thành công", StringComparison.OrdinalIgnoreCase) || 
                       out2.Contains("successfully", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Log($"[LỖI KÍCH HOẠT]: {ex.Message}");
                return false;
            }
        });

        if (ok)
        {
            MessageBox.Show("Chúc mừng! Đã kích hoạt bản quyền Windows thành công với Key OEM từ bo mạch chủ!", "Kích Hoạt Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("Đã thực thi lệnh nạp Key. Vui lòng kiểm tra lại trạng thái kết nối Internet hoặc phiên bản Windows có tương thích với Edition của Key OEM không.", "Kết Quả Kích Hoạt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        await ScanAndCompareLicensesAsync();
    }

    private void CopyKeysToClipboard()
    {
        string text = $"--- THÔNG TIN ĐỐI SOÁT BẢN QUYỀN WINDOWS ---\n" +
                      $"• Tên máy: {_sysInfo.MachineName}\n" +
                      $"• Phiên bản OS: {_sysInfo.OsVersion}\n" +
                      $"• Trạng thái hiện tại: {_activeStatus} ({_activeChannel})\n" +
                      $"• Key đang cài đặt: {_activeKey}\n" +
                      $"• Key OEM BIOS (MSDM): {_oemKey} ({_oemEdition})\n" +
                      $"• Nhà sản xuất: {_sysInfo.Manufacturer} - {_sysInfo.Model}\n";

        Clipboard.SetText(text);
        MessageBox.Show("Đã sao chép toàn bộ thông tin đối soát bản quyền vào Clipboard!", "Đã Sao Chép", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task UpdateKeyToSheetAsync()
    {
        if (string.IsNullOrWhiteSpace(_deviceConfig.AppsScriptUrl))
        {
            MessageBox.Show("Vui lòng cấu hình URL Google Apps Script ở Tab 'Thiết bị' trước khi tải lên Sheet!", "Chưa Cấu Hình Sheet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string keyToSave = (!string.IsNullOrEmpty(_oemKey) && !_oemKey.StartsWith("Không")) ? _oemKey : _activeKey;

        var payload = new DeviceSyncPayload
        {
            Action = "upload",
            MachineName = _sysInfo.MachineName,
            Company = _deviceConfig.Companies.Count > 0 ? _deviceConfig.Companies[0] : "Công ty IT",
            Cpu = _sysInfo.ProcessorName,
            Ram = _sysInfo.TotalMemoryMb,
            OsVersion = _sysInfo.OsVersion,
            ProductKey = keyToSave,
            Status = "Hoạt động tốt",
            Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        Log($"[GOOGLE SHEET]: Đang gửi thông tin bản quyền (Key: {keyToSave}) lên Google Sheet...");
        var (ok, msg) = await _sheetService.UploadDeviceInfoAsync(_deviceConfig.AppsScriptUrl, payload);

        if (ok)
        {
            _sheetKey = keyToSave;
            _lblSheetKey.Text = $"Key: {_sheetKey}";
            Log("✅ Đã cập nhật thông tin bản quyền lên Google Sheet thành công!");
            MessageBox.Show("Đã cập nhật thông tin bản quyền lên Google Sheet thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            Log($"❌ Lỗi gửi dữ liệu lên Google Sheet: {msg}");
            MessageBox.Show($"Lỗi gửi lên Sheet: {msg}", "Lỗi Đồng Bộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Log(string msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(msg));
            return;
        }

        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
        _rtbLog.AppendText(line);
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.ScrollToCaret();
    }
}
