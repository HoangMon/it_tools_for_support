using System;
using System.Drawing;
using System.Windows.Forms;
using Ernastie.Models;

namespace Ernastie.Services;

/// <summary>
/// Centralized Theme & Design System Helper for Ernastie Support Tool.
/// Synchronizes all pop-ups, dialogs, and panels with the user-selected theme (Light / Dark).
/// </summary>
public static class ThemeHelper
{
    public static bool IsDark => !SettingsService.Instance.Settings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);

    // Dynamic Tokens
    public static Color BgForm => IsDark ? Color.FromArgb(11, 14, 20) : Color.FromArgb(248, 250, 252);
    public static Color CardBg => IsDark ? Color.FromArgb(17, 23, 34) : Color.White;
    public static Color BorderColor => IsDark ? Color.FromArgb(32, 42, 60) : Color.FromArgb(226, 232, 240);
    public static Color TextHeading => IsDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42);
    public static Color TextNormal => IsDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);
    public static Color TextMuted => IsDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
    public static Color BtnBg => IsDark ? Color.FromArgb(24, 34, 52) : Color.FromArgb(241, 245, 249);
    public static Color BtnHover => IsDark ? Color.FromArgb(37, 99, 235) : Color.FromArgb(224, 231, 255);
    public static Color BtnBorder => IsDark ? Color.FromArgb(45, 60, 88) : Color.FromArgb(203, 213, 225);
    public static Color InputBg => IsDark ? Color.FromArgb(20, 28, 42) : Color.White;

    public static Color PrimaryBlue => Color.FromArgb(37, 99, 235);
    public static Color PrimaryHover => Color.FromArgb(29, 78, 216);
    public static Color SuccessGreen => Color.FromArgb(22, 163, 74);
    public static Color WarningAmber => Color.FromArgb(217, 119, 6);
    public static Color DangerRed => Color.FromArgb(220, 38, 38);

    public static Color LogBg => IsDark ? Color.FromArgb(7, 10, 15) : Color.FromArgb(241, 245, 249);
    public static Color LogText => IsDark ? Color.FromArgb(16, 185, 129) : Color.FromArgb(15, 23, 42);

    /// <summary>
    /// Applies the current theme to the form and hooks auto-sync when settings change.
    /// </summary>
    public static void ApplyTheme(Form form)
    {
        if (form == null || form.IsDisposed) return;

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
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _appIcon = new Icon(iconPath);
                    return _appIcon;
                }
                var exePath = Application.ExecutablePath;
                if (System.IO.File.Exists(exePath))
                {
                    _appIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (_appIcon != null) return _appIcon;
                }
            }
            catch { }
            return SystemIcons.Application;
        }
    }

    /// <summary>
    /// Traverses and styles all controls inside the form according to the active theme.
    /// </summary>
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
                    // TableLayoutPanel must never have transparent backcolor
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
                    // Main scroll or root container docked directly to Form
                    if (p.Parent is Form || (p.Parent?.Parent is Form && p.Dock == DockStyle.Fill && p.Tag?.ToString() != "card"))
                    {
                        p.BackColor = bg;
                    }
                    else if (p.Tag?.ToString() == "card" || p.BorderStyle != BorderStyle.None || p.Padding.All >= 10)
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
                }
                else if (c is DataGridView dgv)
                {
                    dgv.BackgroundColor = card;
                    dgv.GridColor = border;
                    dgv.DefaultCellStyle.BackColor = card;
                    dgv.DefaultCellStyle.ForeColor = body;
                    dgv.DefaultCellStyle.SelectionBackColor = dark ? Color.FromArgb(30, 64, 175) : Color.FromArgb(219, 234, 254);
                    dgv.DefaultCellStyle.SelectionForeColor = dark ? Color.White : Color.FromArgb(15, 23, 42);
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = btnBg;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = head;
                    dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = btnBg;
                    dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = head;
                    dgv.RowHeadersDefaultCellStyle.BackColor = btnBg;
                    dgv.RowHeadersDefaultCellStyle.ForeColor = head;
                    dgv.EnableHeadersVisualStyles = false;
                    foreach (DataGridViewRow r in dgv.Rows)
                    {
                        r.DefaultCellStyle.BackColor = card;
                        r.DefaultCellStyle.ForeColor = body;
                    }
                }
                else if (c is ListView lv)
                {
                    lv.BackColor = card;
                    lv.ForeColor = body;
                }
                else if (c is TreeView tv)
                {
                    tv.BackColor = card;
                    tv.ForeColor = body;
                }
                else if (c is CheckedListBox clb)
                {
                    clb.BackColor = card;
                    clb.ForeColor = body;
                }
                else if (c is ListBox lb)
                {
                    lb.BackColor = card;
                    lb.ForeColor = body;
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
                    if (chk.Parent != null && chk.Parent.BackColor == card) chk.BackColor = card;
                }
                else if (c is RadioButton rb)
                {
                    rb.ForeColor = head;
                    if (rb.Parent != null && rb.Parent.BackColor == card) rb.BackColor = card;
                }
                else if (c is Label lbl)
                {
                    if (lbl.ForeColor.R < 50 && lbl.ForeColor.G > 120 && lbl.ForeColor.B > 180)
                    {
                        lbl.ForeColor = dark ? Color.FromArgb(96, 165, 250) : Color.FromArgb(2, 132, 199);
                    }
                    else if (lbl.Font.Italic ||
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
                    else if (lbl.Font.Bold || lbl.Font.Size >= 10f)
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
                    // Detect if this is an intentionally colored accent button (Blue, Green, Red)
                    bool isAccent = (btn.BackColor == PrimaryBlue ||
                                     btn.BackColor == PrimaryHover ||
                                     btn.BackColor == SuccessGreen ||
                                     btn.BackColor == DangerRed ||
                                     btn.BackColor.R > 200 && btn.BackColor.G < 60 && btn.BackColor.B < 60);

                    if (isAccent)
                    {
                        btn.ForeColor = Color.White;
                    }
                    else
                    {
                        btn.BackColor = btnBg;
                        btn.ForeColor = head;
                        btn.FlatAppearance.BorderColor = btnBorder;
                        btn.FlatAppearance.MouseOverBackColor = btnHov;
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
}
