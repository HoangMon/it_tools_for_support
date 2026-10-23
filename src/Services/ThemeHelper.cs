using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Ernastie.Models;

namespace Ernastie.Services;

public enum BadgeType
{
    Success,
    Warning,
    Danger,
    Info,
    Neutral
}

/// <summary>
/// UI/UX Pro Max Centralized Design System & Theme Engine for Ernastie IT Tools.
/// Implements Swiss Modernism 2.0 / Data-Dense Dashboard standards (WCAG AAA compliant).
/// </summary>
public static class ThemeHelper
{
    public static bool IsDark => !SettingsService.Instance.Settings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);

    // ==========================================
    // 1. DESIGN TOKENS (SLATE / OLED PALETTE)
    // ==========================================

    // Surface & Background Tokens
    public static Color BgForm => IsDark ? Color.FromArgb(11, 14, 20) : Color.FromArgb(248, 250, 252);         // #0B0E14 (Obsidian) vs #F8FAFC (Slate 50)
    public static Color CardBg => IsDark ? Color.FromArgb(17, 23, 34) : Color.White;                              // #111722 (Dark Slate) vs #FFFFFF (Pure White)
    public static Color CardHeaderBg => IsDark ? Color.FromArgb(22, 30, 44) : Color.FromArgb(241, 245, 249);    // #161E2C vs #F1F5F9
    public static Color BorderColor => IsDark ? Color.FromArgb(32, 42, 60) : Color.FromArgb(226, 232, 240);     // #202A3C vs #E2E8F0
    public static Color BorderHover => IsDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(203, 213, 225);     // #334155 vs #CBD5E1

    // Text & Content Tokens (WCAG AAA Contrast)
    public static Color TextHeading => IsDark ? Color.FromArgb(248, 250, 252) : Color.FromArgb(15, 23, 42);     // #F8FAFC (Slate 50) vs #0F172A (Slate 900)
    public static Color TextNormal => IsDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);       // #E2E8F0 (Slate 200) vs #1E293B (Slate 800)
    public static Color TextMuted => IsDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);    // #94A3B8 (Slate 400) vs #64748B (Slate 500)

    // Button Tokens
    public static Color BtnBg => IsDark ? Color.FromArgb(22, 31, 46) : Color.FromArgb(241, 245, 249);           // Slate 800-tint vs Slate 100
    public static Color BtnHover => IsDark ? Color.FromArgb(30, 42, 63) : Color.FromArgb(224, 231, 255);        // Slate 700-tint vs Indigo 100
    public static Color BtnBorder => IsDark ? Color.FromArgb(45, 60, 88) : Color.FromArgb(203, 213, 225);       // Border subtle
    public static Color BtnBorderHover => IsDark ? Color.FromArgb(96, 165, 250) : Color.FromArgb(99, 102, 241); // Electric blue / Indigo

    // Input & Form Field Tokens
    public static Color InputBg => IsDark ? Color.FromArgb(14, 19, 29) : Color.White;
    public static Color InputBorder => IsDark ? Color.FromArgb(38, 50, 72) : Color.FromArgb(203, 213, 225);

    // Brand & Status Accents (Carefully calibrated for Dark & Light backgrounds)
    public static Color PrimaryBlue => Color.FromArgb(37, 99, 235);    // #2563EB (Blue 600)
    public static Color PrimaryHover => Color.FromArgb(29, 78, 216);   // #1D4ED8 (Blue 700)
    public static Color SuccessGreen => Color.FromArgb(22, 163, 74);   // #16A34A (Green 600)
    public static Color SuccessHover => Color.FromArgb(21, 128, 61);   // #15803D (Green 700)
    public static Color WarningAmber => Color.FromArgb(217, 119, 6);   // #D97706 (Amber 600)
    public static Color WarningHover => Color.FromArgb(180, 83, 9);    // #B45309 (Amber 700)
    public static Color DangerRed => Color.FromArgb(220, 38, 38);      // #DC2626 (Red 600)
    public static Color DangerHover => Color.FromArgb(185, 28, 28);    // #B91C1C (Red 700)

    // Terminal & Log Console Tokens
    public static Color LogBg => IsDark ? Color.FromArgb(8, 11, 16) : Color.FromArgb(241, 245, 249);
    public static Color LogText => IsDark ? Color.FromArgb(52, 211, 153) : Color.FromArgb(15, 23, 42); // Emerald 400 vs Slate 900

    // Typography System
    public static readonly Font FontBody = new("Segoe UI", 9.25f, FontStyle.Regular);
    public static readonly Font FontBodyBold = new("Segoe UI", 9.25f, FontStyle.Bold);
    public static readonly Font FontHeading = new("Segoe UI", 11f, FontStyle.Bold);
    public static readonly Font FontTitle = new("Segoe UI", 12f, FontStyle.Bold);
    public static readonly Font FontMono = new("Cascadia Mono", 9f, FontStyle.Regular);

    // ==========================================
    // 2. THEME APPLICATION ENGINE
    // ==========================================

    public static void ApplyTheme(Form form)
    {
        if (form == null || form.IsDisposed) return;

        // Enable double buffering on Form to prevent rendering flicker
        EnableDoubleBuffering(form);

        ApplyThemeInternal(form);

        Action<AppSettingsModel>? onSettingsChanged = null;
        onSettingsChanged = _ =>
        {
            if (!form.IsDisposed && form.IsHandleCreated)
            {
                try
                {
                    form.Invoke(() => ApplyThemeInternal(form));
                }
                catch { }
            }
        };

        SettingsService.Instance.SettingsChanged += onSettingsChanged;
        form.FormClosed += (_, _) =>
        {
            SettingsService.Instance.SettingsChanged -= onSettingsChanged;
        };
    }

    private static Icon? _appIcon;
    public static Icon AppIcon
    {
        get
        {
            if (_appIcon != null) return _appIcon;
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (File.Exists(iconPath))
                {
                    _appIcon = new Icon(iconPath);
                    return _appIcon;
                }
                var exePath = Application.ExecutablePath;
                if (File.Exists(exePath))
                {
                    _appIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (_appIcon != null) return _appIcon;
                }
            }
            catch { }
            return SystemIcons.Application;
        }
    }

    public static void ApplyThemeInternal(Form form)
    {
        if (form == null || form.IsDisposed) return;

        try
        {
            form.Icon = AppIcon;
        }
        catch { }

        bool dark = IsDark;
        Color bg = BgForm;
        Color card = CardBg;
        Color border = BorderColor;
        Color head = TextHeading;
        Color body = TextNormal;
        Color muted = TextMuted;
        Color btnBg = BtnBg;
        Color btnBorder = BtnBorder;
        Color btnHov = BtnHover;
        Color inputBg = InputBg;
        Color logBg = LogBg;
        Color logText = LogText;

        form.BackColor = bg;
        form.ForeColor = body;
        if (form.Font.Name != "Segoe UI") form.Font = FontBody;

        void StyleControlTree(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is GroupBox gb)
                {
                    gb.BackColor = card;
                    gb.ForeColor = head;
                    StyleControlTree(gb);
                }
                else if (c is TableLayoutPanel tlp)
                {
                    tlp.BackColor = (tlp.Parent is GroupBox || tlp.Parent?.BackColor == card) ? card : bg;
                    StyleControlTree(tlp);
                }
                else if (c is FlowLayoutPanel flp)
                {
                    flp.BackColor = (flp.Parent is GroupBox || flp.Parent?.BackColor == card) ? card : bg;
                    StyleControlTree(flp);
                }
                else if (c is Panel p)
                {
                    if (p.Parent is Form || (p.Parent?.Parent is Form && p.Dock == DockStyle.Fill && p.Tag?.ToString() != "card"))
                    {
                        p.BackColor = bg;
                    }
                    else if (p.Tag?.ToString() == "card" || p.BorderStyle != BorderStyle.None || p.Padding.All >= 8)
                    {
                        p.BackColor = card;
                    }
                    else if (p.Parent is GroupBox || p.Parent?.BackColor == card)
                    {
                        p.BackColor = card;
                    }
                    else
                    {
                        p.BackColor = (p.Parent != null && p.Parent.BackColor == card) ? card : bg;
                    }
                    StyleControlTree(p);
                }
                else if (c is TextBox tb)
                {
                    tb.BackColor = inputBg;
                    tb.ForeColor = head;
                }
                else if (c is ComboBox cb)
                {
                    cb.BackColor = inputBg;
                    cb.ForeColor = head;
                }
                else if (c is NumericUpDown nud)
                {
                    nud.BackColor = inputBg;
                    nud.ForeColor = head;
                }
                else if (c is RichTextBox rtb)
                {
                    rtb.BackColor = logBg;
                    rtb.ForeColor = logText;
                    try
                    {
                        rtb.Font = new Font("Cascadia Mono", 9f, FontStyle.Regular);
                    }
                    catch
                    {
                        rtb.Font = new Font("Consolas", 9f, FontStyle.Regular);
                    }
                }
                else if (c is DataGridView dgv)
                {
                    dgv.BackgroundColor = card;
                    dgv.GridColor = border;
                    dgv.BorderStyle = BorderStyle.None;
                    dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

                    dgv.DefaultCellStyle.BackColor = card;
                    dgv.DefaultCellStyle.ForeColor = body;
                    dgv.DefaultCellStyle.SelectionBackColor = dark ? Color.FromArgb(30, 64, 175) : Color.FromArgb(219, 234, 254);
                    dgv.DefaultCellStyle.SelectionForeColor = dark ? Color.White : Color.FromArgb(15, 23, 42);

                    dgv.ColumnHeadersDefaultCellStyle.BackColor = dark ? Color.FromArgb(22, 30, 44) : Color.FromArgb(241, 245, 249);
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = head;
                    dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
                    dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = head;
                    dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                    dgv.RowHeadersDefaultCellStyle.BackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
                    dgv.RowHeadersDefaultCellStyle.ForeColor = head;
                    dgv.EnableHeadersVisualStyles = false;

                    dgv.AlternatingRowsDefaultCellStyle.BackColor = dark ? Color.FromArgb(13, 18, 27) : Color.FromArgb(248, 250, 252);
                    dgv.AlternatingRowsDefaultCellStyle.ForeColor = body;
                }
                else if (c is ListView lv)
                {
                    lv.BackColor = card;
                    lv.ForeColor = body;
                    lv.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is TreeView tv)
                {
                    tv.BackColor = card;
                    tv.ForeColor = body;
                    tv.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is CheckedListBox clb)
                {
                    clb.BackColor = card;
                    clb.ForeColor = body;
                    clb.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is ListBox lb)
                {
                    lb.BackColor = card;
                    lb.ForeColor = body;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is TabControl tc)
                {
                    tc.BackColor = bg;
                    foreach (TabPage tp in tc.TabPages)
                    {
                        tp.BackColor = card;
                        tp.ForeColor = head;
                        StyleControlTree(tp);
                    }
                }
                else if (c is CheckBox chk)
                {
                    chk.ForeColor = head;
                    chk.Cursor = Cursors.Hand;
                    if (chk.Parent != null && chk.Parent.BackColor == card) chk.BackColor = card;
                }
                else if (c is RadioButton rb)
                {
                    rb.ForeColor = head;
                    rb.Cursor = Cursors.Hand;
                    if (rb.Parent != null && rb.Parent.BackColor == card) rb.BackColor = card;
                }
                else if (c is Label lbl)
                {
                    if (lbl.Tag is BadgeType badgeType)
                    {
                        StyleBadge(lbl, lbl.Text, badgeType);
                    }
                    else if (lbl.ForeColor.R < 50 && lbl.ForeColor.G > 120 && lbl.ForeColor.B > 180)
                    {
                        lbl.ForeColor = dark ? Color.FromArgb(96, 165, 250) : Color.FromArgb(2, 132, 199);
                    }
                    else if (lbl.Font.Italic ||
                        lbl.Font.Size <= 8.75f ||
                        lbl.Text.StartsWith("Lưu ý") ||
                        lbl.Text.StartsWith("Đường dẫn") ||
                        lbl.Text.StartsWith("Thư mục") ||
                        lbl.Text.StartsWith("Hai nút") ||
                        lbl.Text.StartsWith("Danh sách") ||
                        lbl.Text.StartsWith("Chú ý") ||
                        lbl.Text.StartsWith("Ghi chú"))
                    {
                        lbl.ForeColor = muted;
                    }
                    else if (lbl.Font.Bold || lbl.Font.Size >= 10.5f)
                    {
                        lbl.ForeColor = head;
                    }
                    else
                    {
                        lbl.ForeColor = body;
                    }
                }
                else if (c is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.Cursor = Cursors.Hand;

                    bool isPrimary = (btn.BackColor == PrimaryBlue || btn.BackColor == PrimaryHover || btn.Tag?.ToString() == "primary");
                    bool isSuccess = (btn.BackColor == SuccessGreen || btn.BackColor == SuccessHover || btn.Tag?.ToString() == "success");
                    bool isDanger = (btn.BackColor == DangerRed || btn.BackColor == DangerHover || btn.Tag?.ToString() == "danger" || (btn.BackColor.R > 200 && btn.BackColor.G < 60 && btn.BackColor.B < 60));
                    bool isWarning = (btn.BackColor == WarningAmber || btn.BackColor == WarningHover || btn.Tag?.ToString() == "warning");

                    if (isPrimary)
                    {
                        btn.BackColor = PrimaryBlue;
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.BorderColor = PrimaryHover;
                        btn.FlatAppearance.MouseOverBackColor = PrimaryHover;
                        btn.FlatAppearance.BorderSize = 1;
                    }
                    else if (isSuccess)
                    {
                        btn.BackColor = SuccessGreen;
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.BorderColor = SuccessHover;
                        btn.FlatAppearance.MouseOverBackColor = SuccessHover;
                        btn.FlatAppearance.BorderSize = 1;
                    }
                    else if (isDanger)
                    {
                        btn.BackColor = DangerRed;
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.BorderColor = DangerHover;
                        btn.FlatAppearance.MouseOverBackColor = DangerHover;
                        btn.FlatAppearance.BorderSize = 1;
                    }
                    else if (isWarning)
                    {
                        btn.BackColor = WarningAmber;
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.BorderColor = WarningHover;
                        btn.FlatAppearance.MouseOverBackColor = WarningHover;
                        btn.FlatAppearance.BorderSize = 1;
                    }
                    else
                    {
                        btn.BackColor = btnBg;
                        btn.ForeColor = head;
                        btn.FlatAppearance.BorderColor = btnBorder;
                        btn.FlatAppearance.MouseOverBackColor = btnHov;
                        btn.FlatAppearance.BorderSize = 1;
                    }
                }
                else if (c.HasChildren)
                {
                    StyleControlTree(c);
                }
            }
        }

        StyleControlTree(form);
        form.Invalidate(true);
    }

    // ==========================================
    // 3. UI/UX PRO MAX COMPONENT BUILDERS
    // ==========================================

    /// <summary>
    /// Styles a label as a modern high-contrast badge pill with 1px border.
    /// </summary>
    public static void StyleBadge(Label badge, string text, BadgeType type)
    {
        badge.Text = text;
        badge.Tag = type;
        badge.AutoSize = true;
        badge.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
        badge.Padding = new Padding(8, 4, 8, 4);
        badge.Margin = new Padding(0, 3, 10, 4);

        bool dark = IsDark;
        Color border;

        switch (type)
        {
            case BadgeType.Success:
                badge.ForeColor = dark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61);
                badge.BackColor = dark ? Color.FromArgb(5, 46, 22) : Color.FromArgb(240, 253, 244);
                border = dark ? Color.FromArgb(22, 101, 52) : Color.FromArgb(187, 247, 208);
                break;
            case BadgeType.Warning:
                badge.ForeColor = dark ? Color.FromArgb(251, 191, 36) : Color.FromArgb(180, 83, 9);
                badge.BackColor = dark ? Color.FromArgb(69, 26, 3) : Color.FromArgb(254, 252, 232);
                border = dark ? Color.FromArgb(146, 64, 14) : Color.FromArgb(253, 230, 138);
                break;
            case BadgeType.Danger:
                badge.ForeColor = dark ? Color.FromArgb(248, 113, 113) : Color.FromArgb(185, 28, 28);
                badge.BackColor = dark ? Color.FromArgb(69, 10, 10) : Color.FromArgb(254, 242, 242);
                border = dark ? Color.FromArgb(153, 27, 27) : Color.FromArgb(254, 202, 202);
                break;
            case BadgeType.Neutral:
                badge.ForeColor = dark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(71, 85, 105);
                badge.BackColor = dark ? Color.FromArgb(20, 28, 42) : Color.FromArgb(241, 245, 249);
                border = dark ? Color.FromArgb(45, 60, 88) : Color.FromArgb(203, 213, 225);
                break;
            default: // Info
                badge.ForeColor = dark ? Color.FromArgb(96, 165, 250) : Color.FromArgb(30, 64, 175);
                badge.BackColor = dark ? Color.FromArgb(23, 37, 84) : Color.FromArgb(239, 246, 255);
                border = dark ? Color.FromArgb(30, 64, 175) : Color.FromArgb(191, 219, 254);
                break;
        }

        badge.Paint -= Badge_Paint;
        badge.Paint += Badge_Paint;
        badge.Invalidate();

        void Badge_Paint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, badge.Width - 1, badge.Height - 1);
        }
    }

    private static void EnableDoubleBuffering(Control control)
    {
        try
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
        catch { }
    }
}