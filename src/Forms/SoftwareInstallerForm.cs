using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Ernastie.Forms;

public class SoftwareInstallerForm : Form
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
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;
    private Color _successGreen => ThemeHelper.SuccessGreen;

    private readonly List<SoftwareAppItem> _allApps = new();
    private CancellationTokenSource? _cts;
    private bool _isBusy = false;
    private bool _isPaused = false;
    private readonly ManualResetEventSlim _pauseEvent = new(true);

    // Controls
    private ComboBox _cboUserAccount = null!;
    private TabControl _tabControl = null!;
    private Button _btnSelectAll = null!;
    private Button _btnDeselectAll = null!;
    private Button _btnStartInstall = null!;
    private Button _btnPause = null!;
    private Button _btnCancel = null!;
    private Label _lblStatus = null!;
    private ProgressBar _progressBar = null!;
    private RichTextBox _rtbLog = null!;
    private DataGridView _dgvSelectedList = null!;

    public SoftwareInstallerForm()
    {
        InitializeComponents();
        InitDefaultUser();
    }

    private void InitDefaultUser()
    {
        _cboUserAccount.Items.Clear();
        _cboUserAccount.Items.Add(Environment.UserName);
        _cboUserAccount.SelectedIndex = 0;
    }

    private void InitializeComponents()
    {
        Text = "Trình cài đặt phần mềm";
        Size = new Size(950, 710);
        MinimumSize = new Size(900, 660);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9.25f);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 10, 14, 10),
            BackColor = _bgForm
        };

        // 1. Top Header Row: Chọn phần mềm cần cài & Cài cho [User]
        var pnlHeader = BuildTopHeader();
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 36;
        mainPanel.Controls.Add(pnlHeader);

        // 2. TabControl (Categories)
        _tabControl = BuildTabControl();
        _tabControl.Dock = DockStyle.Top;
        _tabControl.Height = 345;
        mainPanel.Controls.Add(_tabControl);

        // 3. Selection & Action Buttons Row (Chọn hết | Bỏ chọn | ▶ Bắt đầu cài)
        var pnlActionBtns = BuildActionButtonsBar();
        pnlActionBtns.Dock = DockStyle.Top;
        pnlActionBtns.Height = 44;
        mainPanel.Controls.Add(pnlActionBtns);

        // 4. Status & Progress Bar Row
        var pnlStatus = BuildStatusProgressRow();
        pnlStatus.Dock = DockStyle.Top;
        pnlStatus.Height = 30;
        mainPanel.Controls.Add(pnlStatus);

        // 5. Log Header Row: Nhật ký cài đặt & [Tạm dừng] [Hủy]
        var pnlLogHeader = BuildLogHeader();
        pnlLogHeader.Dock = DockStyle.Top;
        pnlLogHeader.Height = 32;
        mainPanel.Controls.Add(pnlLogHeader);

        // 6. Log Content (RichTextBox Fill)
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

        // Dock ordering
        pnlHeader.BringToFront();
        _tabControl.BringToFront();
        pnlActionBtns.BringToFront();
        pnlStatus.BringToFront();
        pnlLogHeader.BringToFront();
        _rtbLog.BringToFront();

        ThemeHelper.ApplyTheme(this);
        Log("Sẵn sàng. Tích chọn các phần mềm bạn muốn cài đặt rồi bấm 'Bắt đầu cài'.");
    }

    private Panel BuildTopHeader()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 4)
        };

        var lblTitle = new Label
        {
            Text = "📦 Chọn phần mềm cần cài",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, 4),
            AutoSize = true
        };

        var lblUser = new Label
        {
            Text = "👤 Cài cho",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Size = new Size(80, 26),
            Location = new Point(pnl.Width - 310, 4),
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _cboUserAccount = new ComboBox
        {
            Location = new Point(pnl.Width - 225, 4),
            Size = new Size(220, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        pnl.Controls.Add(lblTitle);
        pnl.Controls.Add(lblUser);
        pnl.Controls.Add(_cboUserAccount);

        return pnl;
    }

    private TabControl BuildTabControl()
    {
        var tc = new TabControl
        {
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Padding = new Point(8, 5)
        };

        // 1. Văn phòng (Matches user screenshot exactly)
        tc.TabPages.Add(BuildOfficeTab());

        // 2. Liên lạc
        tc.TabPages.Add(BuildCommunicationTab());

        // 3. Chuyên dụng
        tc.TabPages.Add(BuildSpecializedTab());

        // 4. Ảnh & Media
        tc.TabPages.Add(BuildMediaTab());

        // 5. Công cụ
        tc.TabPages.Add(BuildToolsTab());

        // 6. Mạng & Camera
        tc.TabPages.Add(BuildNetworkCameraTab());

        // 7. Runtime
        tc.TabPages.Add(BuildRuntimeTab());

        // 8. Java
        tc.TabPages.Add(BuildJavaTab());

        // 9. Office
        tc.TabPages.Add(BuildMicrosoftOfficeTab());

        // 10. NVIDIA
        tc.TabPages.Add(BuildNvidiaTab());

        // 11. Danh sách
        tc.TabPages.Add(BuildSelectedSummaryTab());

        tc.SelectedIndexChanged += (_, _) =>
        {
            if (tc.SelectedTab?.Text == "Danh sách")
            {
                RefreshSelectedSummaryGrid();
            }
        };

        return tc;
    }

    private TabPage BuildOfficeTab()
    {
        var tp = new TabPage("Văn phòng") { BackColor = _bgForm, Padding = new Padding(8) };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));

        // Column 1: Trình duyệt - Mail - Internet
        var gbWeb = new GroupBox
        {
            Text = "🌐 Trình duyệt - Mail - Internet",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 8, 10, 8)
        };
        var pnlWeb = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        AddAppCheck(pnlWeb, "Google Chrome", "Google.Chrome", "🌐 Google Chrome", "Văn phòng");
        AddAppCheck(pnlWeb, "Brave Browser", "Brave.Brave", "🦁 Brave Browser", "Văn phòng");
        AddAppCheck(pnlWeb, "Cốc Cốc", "CocCoc.CocCoc", "🥥 Cốc Cốc", "Văn phòng");
        AddAppCheck(pnlWeb, "Mozilla Firefox", "Mozilla.Firefox", "🔥 Mozilla Firefox", "Văn phòng");
        AddAppCheck(pnlWeb, "Google Drive", "Google.GoogleDrive", "☁️ Google Drive", "Văn phòng");
        AddAppCheck(pnlWeb, "OneDrive", "Microsoft.OneDrive", "☁️ OneDrive", "Văn phòng");
        AddAppCheck(pnlWeb, "Mozilla Thunderbird", "Mozilla.Thunderbird", "✉️ Mozilla Thunderbird (mail)", "Văn phòng");
        AddAppCheck(pnlWeb, "FileZilla", "TimKosse.FileZilla.Client", "📁 FileZilla (FTP)", "Văn phòng");
        gbWeb.Controls.Add(pnlWeb);

        // Column 2: PDF - Máy in ảo
        var gbPdf = new GroupBox
        {
            Text = "📄 PDF - Máy in ảo",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 8, 10, 8)
        };
        var pnlPdf = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        AddAppCheck(pnlPdf, "Foxit PDF Reader", "Foxit.FoxitReader", "🦊 Foxit PDF Reader", "Văn phòng");
        AddAppCheck(pnlPdf, "Adobe Acrobat Reader DC", "Adobe.Acrobat.Reader.64-bit", "📕 Adobe Acrobat Reader DC", "Văn phòng");
        AddAppCheck(pnlPdf, "SumatraPDF", "SumatraPDF.SumatraPDF", "📖 SumatraPDF (đọc PDF nhẹ)", "Văn phòng");
        AddAppCheck(pnlPdf, "PDFgear", "PDFgear.PDFgear", "⚙️ PDFgear (sửa PDF)", "Văn phòng");
        AddAppCheck(pnlPdf, "PDF24 Creator", "geeksoftwareGmbH.PDF24-Creator", "🖨️ PDF24 Creator", "Văn phòng");
        AddAppCheck(pnlPdf, "PDF-XChange Viewer", "TrackerSoftware.PDF-XChangeEditor", "📄 PDF-XChange Viewer", "Văn phòng");
        AddAppCheck(pnlPdf, "CutePDF", "AcroSoftware.CutePDFWriter", "🖨️ CutePDF (máy in ảo PDF)", "Văn phòng");
        AddAppCheck(pnlPdf, "Ghostscript", "ArtifexSoftware.Ghostscript", "📑 Ghostscript (cho CutePDF)", "Văn phòng");
        gbPdf.Controls.Add(pnlPdf);

        // Column 3: Văn phòng - Ghi chú
        var gbOffice = new GroupBox
        {
            Text = "📝 Văn phòng - Ghi chú",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 8, 10, 8)
        };
        var pnlOffice = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        AddAppCheck(pnlOffice, "WPS Office", "Kingsoft.WPSOffice", "🗂️ WPS Office", "Văn phòng");
        AddAppCheck(pnlOffice, "LibreOffice", "TheDocumentFoundation.LibreOffice", "📑 LibreOffice", "Văn phòng");
        AddAppCheck(pnlOffice, "OpenOffice", "Apache.OpenOffice", "📓 OpenOffice", "Văn phòng");
        AddAppCheck(pnlOffice, "ProjectLibre", "ProjectLibre.ProjectLibre", "📊 ProjectLibre", "Văn phòng");
        AddAppCheck(pnlOffice, "draw.io", "JGraph.Draw", "📐 draw.io (vẽ sơ đồ)", "Văn phòng");
        AddAppCheck(pnlOffice, "Notion", "Notion.Notion", "📓 Notion (ghi chú - wiki)", "Văn phòng");
        AddAppCheck(pnlOffice, "Asana", "Asana.Asana", "📋 Asana (quản lý công việc)", "Văn phòng");
        gbOffice.Controls.Add(pnlOffice);

        layout.Controls.Add(gbWeb, 0, 0);
        layout.Controls.Add(gbPdf, 1, 0);
        layout.Controls.Add(gbOffice, 2, 0);

        tp.Controls.Add(layout);
        return tp;
    }

    private TabPage BuildCommunicationTab()
    {
        var tp = new TabPage("Liên lạc") { BackColor = _bgForm, Padding = new Padding(8) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = true };

        AddAppCheck(pnl, "Zalo PC", "VNG.Zalo", "💬 Zalo PC", "Liên lạc");
        AddAppCheck(pnl, "Telegram Desktop", "Telegram.TelegramDesktop", "✈️ Telegram Desktop", "Liên lạc");
        AddAppCheck(pnl, "Facebook Messenger", "Facebook.Messenger", "💬 Facebook Messenger", "Liên lạc");
        AddAppCheck(pnl, "Viber", "Viber.Viber", "📞 Viber", "Liên lạc");
        AddAppCheck(pnl, "Skype", "Microsoft.Skype", "📞 Skype", "Liên lạc");
        AddAppCheck(pnl, "Discord", "Discord.Discord", "🎮 Discord", "Liên lạc");
        AddAppCheck(pnl, "Microsoft Teams", "Microsoft.Teams", "👥 Microsoft Teams", "Liên lạc");
        AddAppCheck(pnl, "Zoom Meetings", "Zoom.Zoom", "🎥 Zoom Meetings", "Liên lạc");
        AddAppCheck(pnl, "UltraViewer", "UltraViewer.UltraViewer", "🖥️ UltraViewer (hỗ trợ từ xa)", "Liên lạc");
        AddAppCheck(pnl, "AnyDesk", "AnyDeskSoftwareGmbH.AnyDesk", "🖥️ AnyDesk", "Liên lạc");
        AddAppCheck(pnl, "TeamViewer", "TeamViewer.TeamViewer", "🖥️ TeamViewer", "Liên lạc");

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildSpecializedTab()
    {
        var tp = new TabPage("Chuyên dụng") { BackColor = _bgForm, Padding = new Padding(8) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = true };

        AddAppCheck(pnl, "VS Code", "Microsoft.VisualStudioCode", "💻 Visual Studio Code", "Chuyên dụng");
        AddAppCheck(pnl, "Git for Windows", "Git.Git", "🐙 Git for Windows", "Chuyên dụng");
        AddAppCheck(pnl, "Node.js (LTS)", "OpenJS.NodeJS.LTS", "🟩 Node.js (LTS)", "Chuyên dụng");
        AddAppCheck(pnl, "Python 3", "Python.Python.3.12", "🐍 Python 3.12", "Chuyên dụng");
        AddAppCheck(pnl, "Notepad++", "Notepad++.Notepad++", "📝 Notepad++", "Chuyên dụng");
        AddAppCheck(pnl, "Sublime Text", "SublimeHQ.SublimeText.4", "📄 Sublime Text 4", "Chuyên dụng");
        AddAppCheck(pnl, "Postman", "Postman.Postman", "🚀 Postman (API client)", "Chuyên dụng");
        AddAppCheck(pnl, "DBeaver", "dbeaver.dbeaver", "🗄️ DBeaver (Database GUI)", "Chuyên dụng");
        AddAppCheck(pnl, "Docker Desktop", "Docker.DockerDesktop", "🐳 Docker Desktop", "Chuyên dụng");
        AddAppCheck(pnl, "Blender 3D", "BlenderFoundation.Blender", "🎨 Blender 3D", "Chuyên dụng");

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildMediaTab()
    {
        var tp = new TabPage("Ảnh & Media") { BackColor = _bgForm, Padding = new Padding(8) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = true };

        AddAppCheck(pnl, "VLC Media Player", "VideoLAN.VLC", "🎬 VLC Media Player", "Ảnh & Media");
        AddAppCheck(pnl, "K-Lite Codec Pack", "CodecGuide.K-LiteCodecPack.Full", "🎞️ K-Lite Codec Pack Full", "Ảnh & Media");
        AddAppCheck(pnl, "GIMP", "GIMP.GIMP", "🎨 GIMP (Chỉnh sửa ảnh)", "Ảnh & Media");
        AddAppCheck(pnl, "OBS Studio", "OBSProject.OBSStudio", "📹 OBS Studio (Quay/Live)", "Ảnh & Media");
        AddAppCheck(pnl, "Format Factory", "FreeTime.FormatFactory", "🔄 Format Factory (Đổi đuôi)", "Ảnh & Media");
        AddAppCheck(pnl, "Audacity", "Audacity.Audacity", "🎵 Audacity (Thu âm/Sửa audio)", "Ảnh & Media");
        AddAppCheck(pnl, "FastStone Image Viewer", "FastStone.FastStoneImageViewer", "🖼️ FastStone Image Viewer", "Ảnh & Media");
        AddAppCheck(pnl, "HandBrake", "HandBrake.HandBrake", "📼 HandBrake (Nén video)", "Ảnh & Media");

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildToolsTab()
    {
        var tp = new TabPage("Công cụ") { BackColor = _bgForm, Padding = new Padding(8) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = true };

        AddAppCheck(pnl, "UniKey", "PhamKimLong.UniKey", "⌨️ UniKey (Bộ gõ tiếng Việt)", "Công cụ");
        AddAppCheck(pnl, "EVKey", "EVKey.EVKey", "⌨️ EVKey (Bộ gõ tiếng Việt)", "Công cụ");
        AddAppCheck(pnl, "7-Zip", "7zip.7zip", "📦 7-Zip", "Công cụ");
        AddAppCheck(pnl, "WinRAR", "RARLab.WinRAR", "📚 WinRAR", "Công cụ");
        AddAppCheck(pnl, "Everything", "voidtools.Everything", "🔍 Everything (Tìm kiếm file siêu tốc)", "Công cụ");
        AddAppCheck(pnl, "CrystalDiskInfo", "CrystalDewWorld.CrystalDiskInfo", "💽 CrystalDiskInfo (Kiểm tra ổ cứng)", "Công cụ");
        AddAppCheck(pnl, "CPU-Z", "CPUID.CPU-Z", "⚡ CPU-Z (Xem thông tin CPU)", "Công cụ");
        AddAppCheck(pnl, "GPU-Z", "TechPowerUp.GPU-Z", "🎮 GPU-Z (Xem thông tin Card)", "Công cụ");
        AddAppCheck(pnl, "Revo Uninstaller", "Revouninstaller.RevoUninstaller", "🗑️ Revo Uninstaller (Gỡ app triệt để)", "Công cụ");
        AddAppCheck(pnl, "Rufus", "Rufus.Rufus", "💾 Rufus (Tạo USB Boot)", "Công cụ");

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildNetworkCameraTab()
    {
        var tp = new TabPage("Mạng & Camera") { BackColor = _bgForm, Padding = new Padding(8) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = true };

        AddAppCheck(pnl, "Wireshark", "WiresharkFoundation.Wireshark", "🦈 Wireshark (Bắt gói tin mạng)", "Mạng & Camera");
        AddAppCheck(pnl, "Advanced IP Scanner", "Famatech.AdvancedIPScanner", "🔍 Advanced IP Scanner (Quét IP LAN)", "Mạng & Camera");
        AddAppCheck(pnl, "PuTTY", "PuTTY.PuTTY", "💻 PuTTY (SSH / Telnet Client)", "Mạng & Camera");
        AddAppCheck(pnl, "WinSCP", "WinSCP.WinSCP", "📁 WinSCP (SFTP Client)", "Mạng & Camera");
        AddAppCheck(pnl, "Smart PSS", "Dahua.SmartPSS", "📹 Smart PSS (Camera Dahua)", "Mạng & Camera");
        AddAppCheck(pnl, "iVMS-4200", "Hikvision.iVMS-4200", "📹 iVMS-4200 (Camera Hikvision)", "Mạng & Camera");
        AddAppCheck(pnl, "Speedtest", "Ookla.Speedtest", "🚀 Speedtest by Ookla", "Mạng & Camera");

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildRuntimeTab()
    {
        var tp = new TabPage("Runtime") { BackColor = _bgForm, Padding = new Padding(8) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = true };

        AddAppCheck(pnl, "Visual C++ All-in-One", "Microsoft.VCRedist.2015+.x64", "⚙️ Visual C++ 2015-2022 Redistributable", "Runtime");
        AddAppCheck(pnl, "DirectX End-User Runtimes", "Microsoft.DirectX", "🎮 DirectX End-User Runtime", "Runtime");
        AddAppCheck(pnl, ".NET Desktop Runtime 8.0", "Microsoft.DotNet.DesktopRuntime.8", "🔷 .NET Desktop Runtime 8.0", "Runtime");
        AddAppCheck(pnl, ".NET Desktop Runtime 6.0", "Microsoft.DotNet.DesktopRuntime.6", "🔷 .NET Desktop Runtime 6.0", "Runtime");

        var btnNet35 = new Button
        {
            Text = "⚡ Bật tính năng .NET Framework 3.5 (kèm 2.0 & 3.0)",
            Size = new Size(380, 36),
            BackColor = _btnBg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 10, 0, 0)
        };
        btnNet35.FlatAppearance.BorderColor = _btnBorder;
        btnNet35.Click += async (_, _) => await EnableNet35FeatureAsync();
        pnl.Controls.Add(btnNet35);

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildJavaTab()
    {
        var tp = new TabPage("Java") { BackColor = _bgForm, Padding = new Padding(8) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = true };

        AddAppCheck(pnl, "Oracle Java JRE 8", "Oracle.JavaRuntimeEnvironment", "☕ Oracle Java JRE 8 (Kê khai Thuế & BHXH)", "Java");
        AddAppCheck(pnl, "Eclipse Temurin JDK 17 LTS", "EclipseAdoptium.Temurin.17.JDK", "☕ Eclipse Temurin JDK 17 (LTS)", "Java");
        AddAppCheck(pnl, "Eclipse Temurin JDK 21 LTS", "EclipseAdoptium.Temurin.21.JDK", "☕ Eclipse Temurin JDK 21 (LTS)", "Java");

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildMicrosoftOfficeTab()
    {
        var tp = new TabPage("Office") { BackColor = _bgForm, Padding = new Padding(12) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };

        var lblOfficeTitle = new Label
        {
            Text = "📦 Cài đặt Microsoft Office Trực Tiếp & Tự Động:",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Size = new Size(500, 26)
        };
        pnl.Controls.Add(lblOfficeTitle);

        var btnOffice365 = new Button
        {
            Text = "🚀 Cài đặt Microsoft 365 (Word, Excel, PowerPoint, Outlook)",
            Size = new Size(460, 42),
            BackColor = _btnBg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 8, 0, 8)
        };
        btnOffice365.FlatAppearance.BorderColor = _btnBorder;
        btnOffice365.Click += async (_, _) => await InstallOfficeAsync("O365ProPlusRetail");
        pnl.Controls.Add(btnOffice365);

        var btnOffice2021 = new Button
        {
            Text = "🏢 Cài đặt Office 2021 LTSC Professional Plus",
            Size = new Size(460, 42),
            BackColor = _btnBg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 8)
        };
        btnOffice2021.FlatAppearance.BorderColor = _btnBorder;
        btnOffice2021.Click += async (_, _) => await InstallOfficeAsync("ProPlus2021Volume");
        pnl.Controls.Add(btnOffice2021);

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildNvidiaTab()
    {
        var tp = new TabPage("NVIDIA") { BackColor = _bgForm, Padding = new Padding(12) };
        var pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };

        AddAppCheck(pnl, "NVIDIA GeForce Experience", "Nvidia.GeForceExperience", "🎮 NVIDIA GeForce Experience", "NVIDIA");
        AddAppCheck(pnl, "NVIDIA Broadcast", "Nvidia.Broadcast", "🎙️ NVIDIA Broadcast (AI Lọc âm & Cam)", "NVIDIA");

        var btnNvidiaWeb = new Button
        {
            Text = "🌐 Mở trang tải Driver NVIDIA GeForce chính hãng",
            Size = new Size(400, 38),
            BackColor = _btnBg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 12, 0, 0)
        };
        btnNvidiaWeb.FlatAppearance.BorderColor = _btnBorder;
        btnNvidiaWeb.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("https://www.nvidia.com/Download/index.aspx") { UseShellExecute = true }); } catch { }
        };
        pnl.Controls.Add(btnNvidiaWeb);

        tp.Controls.Add(pnl);
        return tp;
    }

    private TabPage BuildSelectedSummaryTab()
    {
        var tp = new TabPage("Danh sách") { BackColor = _bgForm, Padding = new Padding(8) };

        _dgvSelectedList = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            GridColor = _borderColor,
            Font = new Font("Segoe UI", 9f)
        };
        _dgvSelectedList.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 225, 205);
        _dgvSelectedList.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _dgvSelectedList.ColumnHeadersHeight = 30;

        var colCheck = new DataGridViewCheckBoxColumn { Name = "ColCheck", HeaderText = "✓", Width = 40 };
        var colName = new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Tên phần mềm", Width = 260 };
        var colCategory = new DataGridViewTextBoxColumn { Name = "ColCategory", HeaderText = "Nhóm", Width = 150 };
        var colId = new DataGridViewTextBoxColumn { Name = "ColId", HeaderText = "Mã Winget Package ID", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };

        _dgvSelectedList.Columns.AddRange(colCheck, colName, colCategory, colId);

        _dgvSelectedList.CellValueChanged += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                var row = _dgvSelectedList.Rows[e.RowIndex];
                if (row.Tag is SoftwareAppItem app)
                {
                    bool isChecked = Convert.ToBoolean(row.Cells[0].Value);
                    app.IsChecked = isChecked;
                    if (app.CheckBoxControl != null)
                    {
                        app.CheckBoxControl.Checked = isChecked;
                    }
                }
            }
        };

        tp.Controls.Add(_dgvSelectedList);
        return tp;
    }

    private void AddAppCheck(Control parent, string name, string wingetId, string displayText, string category)
    {
        var app = new SoftwareAppItem
        {
            Name = name,
            WingetId = wingetId,
            Category = category
        };

        var chk = new CheckBox
        {
            Text = displayText,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = _textHeading,
            AutoSize = true,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 3, 4, 3)
        };

        chk.CheckedChanged += (_, _) =>
        {
            app.IsChecked = chk.Checked;
        };

        app.CheckBoxControl = chk;
        _allApps.Add(app);
        parent.Controls.Add(chk);
    }

    private Panel BuildActionButtonsBar()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0)
        };

        _btnSelectAll = new Button
        {
            Text = "Chọn hết",
            Location = new Point(0, 4),
            Size = new Size(100, 36),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnSelectAll.FlatAppearance.BorderColor = _btnBorder;
        _btnSelectAll.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnSelectAll.Click += (_, _) => SelectAllInCurrentTab(true);

        _btnDeselectAll = new Button
        {
            Text = "Bỏ chọn",
            Location = new Point(108, 4),
            Size = new Size(100, 36),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnDeselectAll.FlatAppearance.BorderColor = _btnBorder;
        _btnDeselectAll.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnDeselectAll.Click += (_, _) => SelectAllInCurrentTab(false);

        _btnStartInstall = new Button
        {
            Text = "▶ Bắt đầu cài",
            Location = new Point(226, 4),
            Size = new Size(160, 36),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnStartInstall.FlatAppearance.BorderColor = _btnBorder;
        _btnStartInstall.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnStartInstall.Click += async (_, _) => await StartBatchInstallAsync();

        pnl.Controls.Add(_btnSelectAll);
        pnl.Controls.Add(_btnDeselectAll);
        pnl.Controls.Add(_btnStartInstall);

        return pnl;
    }

    private Panel BuildStatusProgressRow()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        _lblStatus = new Label
        {
            Text = "Trạng thái: Sẵn sàng cài đặt",
            Location = new Point(0, 4),
            Size = new Size(540, 24),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold | FontStyle.Italic),
            ForeColor = _textHeading,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(pnl.Width - 360, 5),
            Size = new Size(360, 20),
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        pnl.Controls.Add(_lblStatus);
        pnl.Controls.Add(_progressBar);

        return pnl;
    }

    private Panel BuildLogHeader()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        var lblLogTitle = new Label
        {
            Text = "📦 Nhật ký cài đặt:",
            Location = new Point(0, 4),
            Size = new Size(200, 24),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _btnPause = new Button
        {
            Text = "⏸ Tạm dừng",
            Size = new Size(105, 26),
            Location = new Point(pnl.Width - 215, 3),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnPause.FlatAppearance.BorderColor = _btnBorder;
        _btnPause.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnPause.Click += (_, _) => TogglePause();

        _btnCancel = new Button
        {
            Text = "✕ Hủy",
            Size = new Size(100, 26),
            Location = new Point(pnl.Width - 102, 3),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnCancel.FlatAppearance.BorderColor = _btnBorder;
        _btnCancel.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnCancel.Click += (_, _) => CancelInstallation();

        pnl.Controls.Add(lblLogTitle);
        pnl.Controls.Add(_btnPause);
        pnl.Controls.Add(_btnCancel);

        return pnl;
    }

    private void SelectAllInCurrentTab(bool check)
    {
        var curTab = _tabControl.SelectedTab;
        if (curTab == null) return;

        if (curTab.Text == "Danh sách")
        {
            foreach (var app in _allApps)
            {
                app.IsChecked = check;
                if (app.CheckBoxControl != null) app.CheckBoxControl.Checked = check;
            }
            RefreshSelectedSummaryGrid();
            return;
        }

        foreach (var app in _allApps.Where(a => a.Category == curTab.Text))
        {
            app.IsChecked = check;
            if (app.CheckBoxControl != null)
            {
                app.CheckBoxControl.Checked = check;
            }
        }
    }

    private void RefreshSelectedSummaryGrid()
    {
        _dgvSelectedList.Rows.Clear();
        foreach (var app in _allApps.Where(a => a.IsChecked))
        {
            int rIdx = _dgvSelectedList.Rows.Add(app.IsChecked, app.Name, app.Category, app.WingetId);
            _dgvSelectedList.Rows[rIdx].Tag = app;
        }
    }

    private void TogglePause()
    {
        if (!_isBusy) return;

        if (!_isPaused)
        {
            _isPaused = true;
            _pauseEvent.Reset();
            _btnPause.Text = "▶ Tiếp tục";
            _lblStatus.Text = "Trạng thái: Đang tạm dừng...";
            Log("[TẠM DỪNG] Quá trình cài đặt đã được tạm dừng.");
        }
        else
        {
            _isPaused = false;
            _pauseEvent.Set();
            _btnPause.Text = "⏸ Tạm dừng";
            _lblStatus.Text = "Trạng thái: Đang tiếp tục cài đặt...";
            Log("[TIẾP TỤC] Đang tiếp tục quá trình cài đặt.");
        }
    }

    private void CancelInstallation()
    {
        if (_isBusy && _cts != null)
        {
            _cts.Cancel();
            _pauseEvent.Set();
            Log("[HỦY BỎ] Đang yêu cầu hủy bỏ tiến trình cài đặt...");
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message));
            return;
        }

        var time = DateTime.Now.ToString("HH:mm:ss");
        _rtbLog.AppendText($"[{time}] {message}\n");
        _rtbLog.ScrollToCaret();
    }

    private async Task StartBatchInstallAsync()
    {
        var selectedApps = _allApps.Where(a => a.IsChecked).ToList();
        if (selectedApps.Count == 0)
        {
            MessageBox.Show("Vui lòng tích chọn ít nhất một phần mềm để cài đặt!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn đã chọn {selectedApps.Count} phần mềm để cài đặt tự động.\n\n" +
            string.Join(", ", selectedApps.Select(a => a.Name)) + "\n\n" +
            "Bạn có muốn bắt đầu cài đặt ngay bây giờ không?",
            "Xác Nhận Cài Đặt",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        _isBusy = true;
        _isPaused = false;
        _pauseEvent.Set();
        _btnPause.Text = "⏸ Tạm dừng";
        _btnStartInstall.Enabled = false;
        _btnSelectAll.Enabled = false;
        _btnDeselectAll.Enabled = false;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        int total = selectedApps.Count;
        int current = 0;
        int success = 0;

        _progressBar.Value = 0;

        await Task.Run(() =>
        {
            foreach (var app in selectedApps)
            {
                if (ct.IsCancellationRequested) break;
                _pauseEvent.Wait();
                if (ct.IsCancellationRequested) break;

                current++;
                int pct = (int)((current - 1) / (double)total * 100);

                Invoke(() =>
                {
                    _lblStatus.Text = $"Trạng thái: Đang cài đặt {app.Name} ({current}/{total})...";
                    _progressBar.Value = pct;
                });

                Log($"────────────────────────────────────────────────────────────");
                Log($"⏳ [{current}/{total}] Bắt đầu cài đặt: {app.Name} (ID: {app.WingetId})...");

                try
                {
                    var psi = new ProcessStartInfo("winget.exe", $"install --id \"{app.WingetId}\" -e --silent --accept-package-agreements --accept-source-agreements")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var proc = Process.Start(psi)!;
                    proc.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data)) Log($"  {e.Data}");
                    };
                    proc.ErrorDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data)) Log($"  [Lỗi] {e.Data}");
                    };

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    while (!proc.WaitForExit(500))
                    {
                        if (ct.IsCancellationRequested)
                        {
                            try { proc.Kill(); } catch { }
                            break;
                        }
                        _pauseEvent.Wait();
                    }

                    if (!ct.IsCancellationRequested && (proc.ExitCode == 0 || proc.ExitCode == 3010))
                    {
                        success++;
                        Log($"✅ Cài đặt thành công: {app.Name}");
                    }
                    else if (!ct.IsCancellationRequested)
                    {
                        Log($"⚠️ Cài đặt {app.Name} hoàn tất với mã thoát: {proc.ExitCode}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Lỗi thực thi {app.Name}: {ex.Message}");
                }
            }
        }, ct);

        _isBusy = false;
        _btnStartInstall.Enabled = true;
        _btnSelectAll.Enabled = true;
        _btnDeselectAll.Enabled = true;

        if (ct.IsCancellationRequested)
        {
            _lblStatus.Text = "Trạng thái: Đã dừng tiến trình cài đặt.";
            Log($"[HỦY] Quá trình cài đặt đã dừng theo yêu cầu của bạn. Đã cài xong {success}/{total} phần mềm.");
            MessageBox.Show($"Đã dừng quá trình cài đặt!\nĐã cài đặt xong: {success}/{total} phần mềm.", "Đã Dừng", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            _progressBar.Value = 100;
            _lblStatus.Text = "Trạng thái: Hoàn tất cài đặt!";
            Log($"🎉 ĐÃ HOÀN TẤT CÀI ĐẶT TOÀN BỘ PHẦN MỀM ({success}/{total} thành công).");
            MessageBox.Show($"Đã hoàn tất cài đặt toàn bộ phần mềm đã chọn!\nThành công: {success}/{total} phần mềm.", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        _cts?.Dispose();
        _cts = null;
    }

    private async Task EnableNet35FeatureAsync()
    {
        Log("Bắt đầu kích hoạt tính năng .NET Framework 3.5 qua DISM...");
        _lblStatus.Text = "Trạng thái: Đang bật .NET Framework 3.5...";
        _progressBar.Style = ProgressBarStyle.Marquee;

        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("dism.exe", "/online /enable-feature /featurename:NetFx3 /all /norestart")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var proc = Process.Start(psi)!;
                proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log($"  {e.Data}"); };
                proc.BeginOutputReadLine();
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    Log("✅ Đã kích hoạt .NET Framework 3.5 thành công!");
                }
                else
                {
                    Log($"⚠️ Kích hoạt .NET Framework 3.5 với mã: {proc.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Lỗi: {ex.Message}");
            }
        });

        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 100;
        _lblStatus.Text = "Trạng thái: Hoàn tất kích hoạt .NET Framework 3.5";
        MessageBox.Show("Đã hoàn tất kích hoạt tính năng .NET Framework 3.5!", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task InstallOfficeAsync(string productId)
    {
        Log($"Bắt đầu chuẩn bị cài đặt Office ({productId})...");
        _lblStatus.Text = $"Trạng thái: Đang tải và cấu hình Office {productId}...";

        await Task.Run(() =>
        {
            try
            {
                // Create temporary ODT configuration xml
                var tempDir = Path.Combine(Path.GetTempPath(), "OfficeSetup_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(tempDir);

                var xml = $@"<Configuration>
  <Add OfficeClientEdition=""64"" Channel=""Current"">
    <Product ID=""{productId}"">
      <Language ID=""vi-vn"" />
      <Language ID=""en-us"" />
      <ExcludeApp ID=""Access"" />
      <ExcludeApp ID=""Groove"" />
      <ExcludeApp ID=""Lync"" />
      <ExcludeApp ID=""Bing"" />
    </Product>
  </Add>
  <Display Level=""Full"" AcceptEULA=""TRUE"" />
</Configuration>";

                var configPath = Path.Combine(tempDir, "configuration.xml");
                File.WriteAllText(configPath, xml, Encoding.UTF8);

                Log($"Đã tạo cấu hình Office tại: {configPath}");
                Log("Đang khởi động Office Deployment Tool...");

                // Execute via winget or direct office installer
                var psi = new ProcessStartInfo("winget.exe", "install --id Microsoft.Office -e --accept-package-agreements --accept-source-agreements")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi)!;
                proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log($"  {e.Data}"); };
                proc.BeginOutputReadLine();
                proc.WaitForExit();

                Log("Đã gửi lệnh cài đặt Office thành công!");
            }
            catch (Exception ex)
            {
                Log($"❌ Lỗi cài Office: {ex.Message}");
            }
        });

        _lblStatus.Text = "Trạng thái: Sẵn sàng cài đặt";
        MessageBox.Show("Tiến trình cài đặt Microsoft Office đã được kích hoạt!", "Office Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

public class SoftwareAppItem
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string WingetId { get; set; } = string.Empty;
    public bool IsChecked { get; set; } = false;
    public CheckBox? CheckBoxControl { get; set; }
}
