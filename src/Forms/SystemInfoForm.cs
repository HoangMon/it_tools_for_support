using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Ernastie.Services;

namespace Ernastie.Forms;

public class SystemInfoForm : Form
{
    private readonly HardwareInfoService _hwService = new();
    private DetailedHardwareInfo _info = new();

    // UI/UX Pro Max - Dynamic Design Tokens from ThemeHelper
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _cardBg => ThemeHelper.CardBg;
    private Color _borderColor => ThemeHelper.BorderColor;
    private Color _textHeading => ThemeHelper.TextHeading;
    private Color _textLabel => ThemeHelper.TextMuted;
    private Color _textValue => ThemeHelper.TextHeading;
    private Color _greenActive => ThemeHelper.SuccessGreen;
    private Color _greenBadgeBg => ThemeHelper.IsDark ? Color.FromArgb(5, 46, 22) : Color.FromArgb(220, 252, 231);
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnHover => ThemeHelper.BtnHover;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _primaryBlue => ThemeHelper.PrimaryBlue;
    private Color _primaryHover => ThemeHelper.PrimaryHover;

    private Panel _mainScroll = null!;
    private TableLayoutPanel _masterLayout = null!;

    public SystemInfoForm()
    {
        LoadHardwareData();
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "🖥️ Thông tin hệ thống";
        Size = new Size(1060, 840);
        MinimumSize = new Size(960, 740);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9f);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;
        MaximizeBox = true;
        MinimizeBox = true;
        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.F5)
            {
                ReloadData();
                e.Handled = true;
            }
        };

        _mainScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(18, 14, 18, 18),
            BackColor = _bgForm
        };

        _masterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = _bgForm
        };
        _masterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        RebuildLayout();

        _mainScroll.Controls.Add(_masterLayout);
        Controls.Add(_mainScroll);
        ThemeHelper.ApplyTheme(this);
    }

    private void RebuildLayout()
    {
        _masterLayout.SuspendLayout();
        _masterLayout.Controls.Clear();
        _masterLayout.RowStyles.Clear();

        // 1. Thông tin chung Card
        var cardGeneral = BuildGeneralCard();
        cardGeneral.Dock = DockStyle.Top;
        cardGeneral.Margin = new Padding(0, 0, 0, 12);
        _masterLayout.Controls.Add(cardGeneral, 0, 0);

        // 2. Windows & Bản quyền Card
        var cardWindows = BuildWindowsLicenseCard();
        cardWindows.Dock = DockStyle.Top;
        cardWindows.Margin = new Padding(0, 0, 0, 12);
        _masterLayout.Controls.Add(cardWindows, 0, 1);

        // 3. Card đồ họa Card
        var cardGpu = BuildGpuCard();
        cardGpu.Dock = DockStyle.Top;
        cardGpu.Margin = new Padding(0, 0, 0, 12);
        _masterLayout.Controls.Add(cardGpu, 0, 2);

        // 4. RAM Card
        var cardRam = BuildRamCard();
        cardRam.Dock = DockStyle.Top;
        cardRam.Margin = new Padding(0, 0, 0, 12);
        _masterLayout.Controls.Add(cardRam, 0, 3);

        // 5. Ổ cứng Card
        var cardDisk = BuildDiskCard();
        cardDisk.Dock = DockStyle.Top;
        cardDisk.Margin = new Padding(0, 0, 0, 14);
        _masterLayout.Controls.Add(cardDisk, 0, 4);

        // 6. Bottom Action Buttons
        var pnlActions = BuildBottomButtons();
        pnlActions.Dock = DockStyle.Top;
        pnlActions.Height = 46;
        _masterLayout.Controls.Add(pnlActions, 0, 5);

        _masterLayout.ResumeLayout(true);
    }

    private void ReloadData()
    {
        Cursor = Cursors.WaitCursor;
        try
        {
            LoadHardwareData();
            RebuildLayout();
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void LoadHardwareData()
    {
        _info = _hwService.GetDetailedHardwareInfo();
    }

    #region Card Builders

    private Panel BuildGeneralCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("📋  Thông tin chung");
        card.Controls.Add(lblTitle);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));

        // Row 0
        tbl.Controls.Add(CreateFieldRow("Tên máy", _info.MachineName), 0, 0);
        tbl.Controls.Add(CreateFieldRow("Service Tag", _info.ServiceTag, isMonospace: true), 1, 0);

        // Row 1
        tbl.Controls.Add(CreateFieldRow("Hãng sản xuất", _info.Manufacturer), 0, 1);
        tbl.Controls.Add(CreateFieldRow("Model", _info.Model), 1, 1);

        // Row 2
        tbl.Controls.Add(CreateFieldRow("BIOS", _info.BiosInfo), 0, 2);
        tbl.Controls.Add(new Panel { Height = 28 }, 1, 2);

        // Row 3
        var pnlCpu = CreateFieldRow("CPU", _info.CpuInfo);
        tbl.Controls.Add(pnlCpu, 0, 3);
        tbl.SetColumnSpan(pnlCpu, 2);

        // Row 4
        var pnlBat = CreateFieldRow("Pin laptop", _info.BatteryInfo);
        tbl.Controls.Add(pnlBat, 0, 4);
        tbl.SetColumnSpan(pnlBat, 2);

        card.Controls.Add(tbl);
        lblTitle.BringToFront();
        return card;
    }

    private Panel BuildWindowsLicenseCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("🔑  Windows & Bản quyền");
        card.Controls.Add(lblTitle);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));

        // Row 0
        tbl.Controls.Add(CreateFieldRow("Hệ điều hành", _info.OsName), 0, 0);
        tbl.Controls.Add(CreateFieldRow("Product ID", _info.ProductId, isMonospace: true), 1, 0);

        // Row 1
        tbl.Controls.Add(CreateFieldRow("Version", _info.OsVersion), 0, 1);
        tbl.Controls.Add(CreateFieldRow("OEM Edition", _info.OemEdition), 1, 1);

        // Row 2
        tbl.Controls.Add(CreateFieldRow("Activation", _info.ActivationStatus, isGreen: true), 0, 2);
        tbl.Controls.Add(CreateFieldRow("OEM Key", _info.OemKey, isMonospace: true), 1, 2);

        // Row 3
        var pnlKey = CreateFieldRow("Installed Key", _info.InstalledKey, isMonospace: true);
        tbl.Controls.Add(pnlKey, 0, 3);
        tbl.SetColumnSpan(pnlKey, 2);

        card.Controls.Add(tbl);
        lblTitle.BringToFront();
        return card;
    }

    private Panel BuildGpuCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("🎮  Card đồ họa");
        card.Controls.Add(lblTitle);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (int i = 0; i < _info.Gpus.Count; i++)
        {
            var parts = _info.Gpus[i].Split(new[] { ':' }, 2);
            string label = parts.Length > 0 ? parts[0].Trim() : $"GPU {i + 1}";
            string val = parts.Length > 1 ? parts[1].Trim() : _info.Gpus[i];
            tbl.Controls.Add(CreateFieldRow(label, val));
        }

        card.Controls.Add(tbl);
        lblTitle.BringToFront();
        return card;
    }

    private Panel BuildRamCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("💾  RAM");
        card.Controls.Add(lblTitle);

        int slotCount = Math.Max(1, _info.RamSlots.Count);
        int cols = slotCount <= 2 ? slotCount : 2;
        int rows = (int)Math.Ceiling((double)slotCount / cols);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = cols,
            RowCount = rows,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };

        for (int c = 0; c < cols; c++)
        {
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
        }

        for (int i = 0; i < _info.RamSlots.Count; i++)
        {
            int colIdx = i % cols;
            int rowIdx = i / cols;
            var parts = _info.RamSlots[i].Split(new[] { ':' }, 2);
            string label = parts.Length > 0 ? parts[0].Trim() : $"Khe {i + 1}";
            string val = parts.Length > 1 ? parts[1].Trim() : _info.RamSlots[i];
            tbl.Controls.Add(CreateFieldRow(label, val, labelWidth: 90), colIdx, rowIdx);
        }

        card.Controls.Add(tbl);
        lblTitle.BringToFront();
        return card;
    }

    private Panel BuildDiskCard()
    {
        var card = CreateBaseCard();

        var lblTitle = CreateSectionHeader("💽  Ổ cứng");
        card.Controls.Add(lblTitle);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (int i = 0; i < _info.Disks.Count; i++)
        {
            var parts = _info.Disks[i].Split(new[] { ':' }, 2);
            string label = parts.Length > 0 ? parts[0].Trim() : $"Disk {i}";
            string val = parts.Length > 1 ? parts[1].Trim() : _info.Disks[i];
            tbl.Controls.Add(CreateFieldRow(label, val));
        }

        if (!string.IsNullOrEmpty(_info.UpgradeInfo))
        {
            tbl.Controls.Add(CreateFieldRow("Nâng cấp ổ", _info.UpgradeInfo));
        }

        card.Controls.Add(tbl);
        lblTitle.BringToFront();
        return card;
    }

    private Control BuildBottomButtons()
    {
        var tbl = new TableLayoutPanel
        {
            Height = 48,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };

        var btnShowKey = CreatePrimaryButton("🔑 Mở ShowKeyPlus", () => OpenShowKeyPlusOrModal());
        btnShowKey.Size = new Size(200, 40);
        btnShowKey.Margin = new Padding(0, 0, 12, 0);

        var btnRefresh = CreateStyledButton("🔄 Làm mới (F5)", () => ReloadData());
        btnRefresh.Size = new Size(140, 40);
        btnRefresh.Margin = new Padding(0, 0, 12, 0);

        var btnExit = CreateStyledButton("Thoát", () => Close());
        btnExit.Size = new Size(110, 40);
        btnExit.Margin = Padding.Empty;

        flow.Controls.Add(btnShowKey);
        flow.Controls.Add(btnRefresh);
        flow.Controls.Add(btnExit);

        tbl.Controls.Add(flow, 1, 0);
        return tbl;
    }

    #endregion

    #region Layout & Typography Helpers

    private Label CreateSectionHeader(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = _textHeading,
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Panel CreateFieldRow(string labelText, string valText, bool isGreen = false, bool isMonospace = false, int labelWidth = 135)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };

        // Left Label: Configurable width, docked to Left
        var lbl = new Label
        {
            Text = labelText,
            Dock = DockStyle.Left,
            Width = labelWidth,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = _textLabel,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Right Value: Docked to Fill, claims ALL remaining horizontal room
        var val = new Label
        {
            Text = valText,
            Dock = DockStyle.Fill,
            Font = isMonospace
                ? new Font("Consolas", 9.5f, FontStyle.Regular)
                : new Font("Segoe UI", 9.25f, isGreen ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = isGreen ? _greenActive : _textValue,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        // Adding val first then lbl ensures WinForms Dock logic places lbl on Left and val fills remainder
        pnl.Controls.Add(val);
        pnl.Controls.Add(lbl);
        return pnl;
    }

    private Panel CreateBaseCard()
    {
        var pnl = new Panel
        {
            BackColor = _cardBg,
            Padding = new Padding(18, 12, 18, 14),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Tag = "card"
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };
        return pnl;
    }

    private Button CreateStyledButton(string text, Action action)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = _btnBorder;
        btn.FlatAppearance.MouseOverBackColor = _btnHover;
        btn.Click += (_, _) => action();
        return btn;
    }

    private Button CreatePrimaryButton(string text, Action action)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = _primaryBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = _primaryHover;
        btn.FlatAppearance.MouseOverBackColor = _primaryHover;
        btn.Click += (_, _) => action();
        return btn;
    }

    private void OpenShowKeyPlusOrModal()
    {
        string[] candidates = { "ShowKeyPlus.exe", @"Resources\ShowKeyPlus.exe", @"..\Resources\ShowKeyPlus.exe" };
        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(c) { UseShellExecute = true });
                    return;
                }
                catch { }
            }
        }

        using var modal = new Form
        {
            Text = "🔑 ShowKeyPlus - Chi tiết Bản quyền Windows",
            Size = new Size(620, 440),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = _bgForm,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9f)
        };

        var pnlCard = new Panel
        {
            Location = new Point(18, 18),
            Size = new Size(568, 305),
            BackColor = Color.White,
            Padding = new Padding(18)
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(_borderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        void AddRow(string title, string val, int row, bool isKey = false)
        {
            var l = new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = _textLabel };
            var v = new Label { Text = val, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = isKey ? new Font("Consolas", 9.5f, FontStyle.Bold) : new Font("Segoe UI", 9.25f), ForeColor = isKey ? _primaryBlue : _textValue };
            tbl.Controls.Add(l, 0, row);
            tbl.Controls.Add(v, 1, row);
        }

        AddRow("Tên sản phẩm:", _info.OsName, 0);
        AddRow("Phiên bản:", _info.OsVersion, 1);
        AddRow("Product ID:", _info.ProductId, 2);
        AddRow("Installed Key:", _info.InstalledKey, 3, isKey: true);
        AddRow("OEM Key:", _info.OemKey, 4, isKey: true);
        AddRow("OEM Edition:", _info.OemEdition, 5);

        pnlCard.Controls.Add(tbl);
        modal.Controls.Add(pnlCard);

        var btnCopy = CreatePrimaryButton("📋 Sao chép thông tin bản quyền", () =>
        {
            var text = $"Tên sản phẩm: {_info.OsName}\n" +
                       $"Phiên bản: {_info.OsVersion}\n" +
                       $"Product ID: {_info.ProductId}\n" +
                       $"Installed Key: {_info.InstalledKey}\n" +
                       $"OEM Key: {_info.OemKey}\n" +
                       $"OEM Edition: {_info.OemEdition}\n" +
                       $"Trạng thái: {_info.ActivationStatus}";
            Clipboard.SetText(text);
            MessageBox.Show("Đã sao chép toàn bộ thông tin bản quyền vào Clipboard!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        btnCopy.Location = new Point(18, 340);
        btnCopy.Size = new Size(275, 38);

        var btnClose = CreateStyledButton("Đóng", () => modal.Close());
        btnClose.Location = new Point(305, 340);
        btnClose.Size = new Size(130, 38);

        modal.Controls.Add(btnCopy);
        modal.Controls.Add(btnClose);
        modal.ShowDialog(this);
    }

    #endregion
}
