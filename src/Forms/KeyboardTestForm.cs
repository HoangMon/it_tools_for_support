using System.Drawing;
using System.Windows.Forms;
using Ernastie.Services;

namespace Ernastie.Forms;

public class KeyboardTestForm : Form
{
    private readonly Dictionary<Keys, Button> _keyButtons = new();
    private readonly HashSet<Keys> _pressedKeys = new();
    private Label _lblStatus = null!;
    private Label _lblLastKey = null!;
    private Button _btnReset = null!;
    private Button _btnClose = null!;

    public KeyboardTestForm()
    {
        Text = "⌨️ Kiểm tra bàn phím (Keyboard Tester)";
        Size = new Size(1060, 560);
        MinimumSize = new Size(980, 500);
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9.25f);
        Icon = SystemIcons.Application;

        InitializeComponents();
        ThemeHelper.ApplyTheme(this);

        KeyDown += KeyboardTestForm_KeyDown;
    }

    private void InitializeComponents()
    {
        BackColor = ThemeHelper.BgForm;
        ForeColor = ThemeHelper.TextNormal;

        // Top Header
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            Padding = new Padding(16, 10, 16, 8),
            BackColor = ThemeHelper.CardBg
        };
        Controls.Add(pnlTop);

        var lblTitle = new Label
        {
            Text = "⌨️ Kiểm tra toàn bộ phím bấm (Keyboard Hardware Test)",
            Font = new Font("Segoe UI", 11.25f, FontStyle.Bold),
            ForeColor = ThemeHelper.TextHeading,
            AutoSize = true,
            Location = new Point(14, 8)
        };
        pnlTop.Controls.Add(lblTitle);

        _lblStatus = new Label
        {
            Text = "Đã kiểm tra: 0 phím | Nhấn lần lượt từng phím trên bàn phím của bạn",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ThemeHelper.TextMuted,
            AutoSize = true,
            Location = new Point(16, 34)
        };
        pnlTop.Controls.Add(_lblStatus);

        _lblLastKey = new Label
        {
            Text = "Phím vừa nhấn: (Chưa có)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ThemeHelper.PrimaryBlue,
            AutoSize = true,
            Location = new Point(540, 34)
        };
        pnlTop.Controls.Add(_lblLastKey);

        _btnReset = new Button
        {
            Text = "🔄 Làm mới",
            Size = new Size(110, 32),
            Location = new Point(pnlTop.Width - 235, 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BtnBg,
            ForeColor = ThemeHelper.TextHeading,
            Cursor = Cursors.Hand
        };
        _btnReset.FlatAppearance.BorderColor = ThemeHelper.BtnBorder;
        _btnReset.Click += (_, _) => ResetKeyboard();
        pnlTop.Controls.Add(_btnReset);

        _btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(95, 32),
            Location = new Point(pnlTop.Width - 115, 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeHelper.BtnBg,
            ForeColor = ThemeHelper.TextHeading,
            Cursor = Cursors.Hand
        };
        _btnClose.FlatAppearance.BorderColor = ThemeHelper.BtnBorder;
        _btnClose.Click += (_, _) => Close();
        pnlTop.Controls.Add(_btnClose);

        // Keyboard Canvas Container
        var pnlKeyboard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 16),
            BackColor = ThemeHelper.BgForm,
            AutoScroll = true
        };
        Controls.Add(pnlKeyboard);
        pnlKeyboard.BringToFront();

        BuildKeyboardLayout(pnlKeyboard);
    }

    private void BuildKeyboardLayout(Panel parent)
    {
        int keyW = 48;
        int keyH = 44;
        int gap = 5;
        int startX = 20;
        int startY = 16;

        // Row 0: Esc, F1..F12, PrtSc, Scroll, Pause
        int x = startX;
        int y = startY;
        AddKey(parent, "Esc", Keys.Escape, x, y, 52, keyH);
        x += 52 + gap + 16;

        for (int i = 1; i <= 4; i++)
        {
            AddKey(parent, $"F{i}", Keys.F1 + (i - 1), x, y, keyW, keyH);
            x += keyW + gap;
        }
        x += 12;
        for (int i = 5; i <= 8; i++)
        {
            AddKey(parent, $"F{i}", Keys.F5 + (i - 5), x, y, keyW, keyH);
            x += keyW + gap;
        }
        x += 12;
        for (int i = 9; i <= 12; i++)
        {
            AddKey(parent, $"F{i}", Keys.F9 + (i - 9), x, y, keyW, keyH);
            x += keyW + gap;
        }
        x += 16;
        AddKey(parent, "PrtSc", Keys.PrintScreen, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "ScrLk", Keys.Scroll, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "Pause", Keys.Pause, x, y, keyW, keyH);

        // Row 1: `~, 1..0, -, =, Backspace, Ins, Home, PgUp
        x = startX;
        y += keyH + gap + 8;
        AddKey(parent, "~ `", Keys.Oemtilde, x, y, keyW, keyH); x += keyW + gap;
        for (int i = 1; i <= 9; i++)
        {
            AddKey(parent, $"{i}", Keys.D0 + i, x, y, keyW, keyH);
            x += keyW + gap;
        }
        AddKey(parent, "0", Keys.D0, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "-", Keys.OemMinus, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "=", Keys.Oemplus, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "Backspace", Keys.Back, x, y, 92, keyH); x += 92 + gap + 16;
        AddKey(parent, "Ins", Keys.Insert, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "Home", Keys.Home, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "PgUp", Keys.PageUp, x, y, keyW, keyH);

        // Row 2: Tab, Q..P, [, ], \, Del, End, PgDn
        x = startX;
        y += keyH + gap;
        AddKey(parent, "Tab", Keys.Tab, x, y, 70, keyH); x += 70 + gap;
        var r2Letters = new[] { Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P };
        foreach (var k in r2Letters)
        {
            AddKey(parent, k.ToString(), k, x, y, keyW, keyH);
            x += keyW + gap;
        }
        AddKey(parent, "[", Keys.OemOpenBrackets, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "]", Keys.OemCloseBrackets, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "\\", Keys.OemBackslash, x, y, 70, keyH); x += 70 + gap + 16;
        AddKey(parent, "Del", Keys.Delete, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "End", Keys.End, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "PgDn", Keys.PageDown, x, y, keyW, keyH);

        // Row 3: Caps, A..L, ;, ', Enter
        x = startX;
        y += keyH + gap;
        AddKey(parent, "Caps Lock", Keys.CapsLock, x, y, 86, keyH); x += 86 + gap;
        var r3Letters = new[] { Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L };
        foreach (var k in r3Letters)
        {
            AddKey(parent, k.ToString(), k, x, y, keyW, keyH);
            x += keyW + gap;
        }
        AddKey(parent, ";", Keys.OemSemicolon, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "'", Keys.OemQuotes, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "Enter", Keys.Enter, x, y, 107, keyH);

        // Row 4: Shift, Z..M, ,, ., /, Shift, Up
        x = startX;
        y += keyH + gap;
        AddKey(parent, "Shift", Keys.ShiftKey, x, y, 114, keyH); x += 114 + gap;
        var r4Letters = new[] { Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M };
        foreach (var k in r4Letters)
        {
            AddKey(parent, k.ToString(), k, x, y, keyW, keyH);
            x += keyW + gap;
        }
        AddKey(parent, ",", Keys.Oemcomma, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, ".", Keys.OemPeriod, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "/", Keys.OemQuestion, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "R-Shift", Keys.RShiftKey, x, y, 132, keyH); x += 132 + gap + 16 + keyW + gap;
        AddKey(parent, "▲", Keys.Up, x, y, keyW, keyH);

        // Row 5: Ctrl, Win, Alt, Space, Alt, Win, Menu, Ctrl, Left, Down, Right
        x = startX;
        y += keyH + gap;
        AddKey(parent, "Ctrl", Keys.ControlKey, x, y, 65, keyH); x += 65 + gap;
        AddKey(parent, "Win", Keys.LWin, x, y, 55, keyH); x += 55 + gap;
        AddKey(parent, "Alt", Keys.Menu, x, y, 55, keyH); x += 55 + gap;
        AddKey(parent, "Space", Keys.Space, x, y, 290, keyH); x += 290 + gap;
        AddKey(parent, "Alt", Keys.RMenu, x, y, 55, keyH); x += 55 + gap;
        AddKey(parent, "Win", Keys.RWin, x, y, 55, keyH); x += 55 + gap;
        AddKey(parent, "Fn/Menu", Keys.Apps, x, y, 55, keyH); x += 55 + gap;
        AddKey(parent, "Ctrl", Keys.RControlKey, x, y, 65, keyH); x += 65 + gap + 16;
        AddKey(parent, "◄", Keys.Left, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "▼", Keys.Down, x, y, keyW, keyH); x += keyW + gap;
        AddKey(parent, "►", Keys.Right, x, y, keyW, keyH);
    }

    private void AddKey(Panel parent, string text, Keys key, int x, int y, int w, int h)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            BackColor = ThemeHelper.BtnBg,
            ForeColor = ThemeHelper.TextHeading,
            Enabled = false,
            TabStop = false
        };
        btn.FlatAppearance.BorderColor = ThemeHelper.BtnBorder;
        parent.Controls.Add(btn);

        if (!_keyButtons.ContainsKey(key))
        {
            _keyButtons[key] = btn;
        }
    }

    private void KeyboardTestForm_KeyDown(object? sender, KeyEventArgs e)
    {
        var key = e.KeyCode;
        _pressedKeys.Add(key);

        if (_keyButtons.TryGetValue(key, out var btn))
        {
            btn.BackColor = Color.FromArgb(16, 185, 129); // Vibrant emerald green
            btn.ForeColor = Color.White;
        }

        _lblLastKey.Text = $"Phím vừa nhấn: {key} (Code: {(int)key})";
        _lblStatus.Text = $"Đã kiểm tra: {_pressedKeys.Count} phím | Tất cả các phím hoạt động đều sáng màu xanh lá";
        e.Handled = true;
    }

    private void ResetKeyboard()
    {
        _pressedKeys.Clear();
        foreach (var kvp in _keyButtons)
        {
            kvp.Value.BackColor = ThemeHelper.BtnBg;
            kvp.Value.ForeColor = ThemeHelper.TextHeading;
        }
        _lblStatus.Text = "Đã làm mới! Nhấn lần lượt từng phím trên bàn phím của bạn";
        _lblLastKey.Text = "Phím vừa nhấn: (Chưa có)";
    }
}
