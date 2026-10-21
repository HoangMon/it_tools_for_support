using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Ernastie.Forms;

public class ScreenTestForm : Form
{
    private readonly Color[] _colors = new[]
    {
        Color.White,
        Color.Black,
        Color.Red,
        Color.Lime,
        Color.Blue,
        Color.Yellow,
        Color.Cyan,
        Color.Magenta,
        Color.LightGray,
        Color.Gray,
        Color.DarkGray
    };

    private int _colorIndex = 0;
    private bool _showHelp = true;
    private readonly System.Windows.Forms.Timer _helpTimer;

    public ScreenTestForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        BackColor = _colors[_colorIndex];

        _helpTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _helpTimer.Tick += (_, _) =>
        {
            _showHelp = false;
            _helpTimer.Stop();
            Invalidate();
        };
        _helpTimer.Start();

        KeyDown += ScreenTestForm_KeyDown;
        MouseClick += ScreenTestForm_MouseClick;
        Paint += ScreenTestForm_Paint;
    }

    private void ScreenTestForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Q)
        {
            Close();
        }
        else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down)
        {
            NextColor();
        }
        else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up)
        {
            PrevColor();
        }
    }

    private void ScreenTestForm_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            PrevColor();
        }
        else
        {
            NextColor();
        }
    }

    private void NextColor()
    {
        _colorIndex = (_colorIndex + 1) % _colors.Length;
        BackColor = _colors[_colorIndex];
        Invalidate();
    }

    private void PrevColor()
    {
        _colorIndex = (_colorIndex - 1 + _colors.Length) % _colors.Length;
        BackColor = _colors[_colorIndex];
        Invalidate();
    }

    private void ScreenTestForm_Paint(object? sender, PaintEventArgs e)
    {
        if (!_showHelp) return;

        string helpText = $"Màu [{_colorIndex + 1}/{_colors.Length}] - Bấm Chuột / Phím Cách để đổi màu | Phím Mũi Tên chuyển tiếp | Bấm ESC để thoát";
        using var font = new Font("Segoe UI", 12f, FontStyle.Bold);
        var size = e.Graphics.MeasureString(helpText, font);

        int boxW = (int)size.Width + 30;
        int boxH = (int)size.Height + 16;
        int boxX = (ClientSize.Width - boxW) / 2;
        int boxY = 24;

        using var brushBg = new SolidBrush(Color.FromArgb(190, 0, 0, 0));
        e.Graphics.FillRectangle(brushBg, boxX, boxY, boxW, boxH);
        using var penBorder = new Pen(Color.FromArgb(200, 255, 255, 255), 1);
        e.Graphics.DrawRectangle(penBorder, boxX, boxY, boxW, boxH);

        using var brushText = new SolidBrush(Color.White);
        e.Graphics.DrawString(helpText, font, brushText, boxX + 15, boxY + 8);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _helpTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
