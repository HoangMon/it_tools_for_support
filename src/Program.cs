using System.Windows.Forms;

namespace Ernastie;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Prevent JIT dialogs and unhandled crash dialogs
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ThreadException: {e.Exception}\n\n");
            }
            catch { }

            MessageBox.Show($"Thông báo: {e.Exception.Message}", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UnhandledCrash: {ex}\n\n");
                }
                catch { }

                MessageBox.Show($"Lỗi hệ thống: {ex.Message}\n\nChi tiết:\n{ex.StackTrace}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        Application.Run(new MainForm());
    }
}