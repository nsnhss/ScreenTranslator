using System.IO;
using System.Windows;

namespace ScreenTranslator;

public partial class App : System.Windows.Application
{
    public App()
    {
        // 全局异常记录到日志文件，方便排查
        DispatcherUnhandledException += (_, e) =>
        {
            Log(e.Exception);
            System.Windows.MessageBox.Show(e.Exception.ToString(), "未处理的异常",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Shutdown();
        };

        // 跟随系统明暗主题
        Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme();
    }

    private static void Log(System.Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScreenTranslator");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch { }
    }
}
