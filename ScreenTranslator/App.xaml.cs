using System.IO;
using System.Threading;
using System.Windows;

namespace ScreenTranslator;

public partial class App : System.Windows.Application
{
    private const string MutexName = "ScreenTranslator_SingleInstance";
    private const string ShowEventName = "ScreenTranslator_ShowWindow";
    private static Mutex? _mutex;

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
    }

    /// <summary>
    /// 单实例运行：第二次启动 exe 时，通知已有实例显示主窗口，自身直接退出。
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            try
            {
                using var showEvent = EventWaitHandle.OpenExisting(ShowEventName);
                showEvent.Set();
            }
            catch { /* 已有实例可能正在退出 */ }
            Shutdown();
            return;
        }

        // 主实例：监听其他实例发来的“显示主窗口”信号
        var showHandle = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        var listener = new Thread(() =>
        {
            while (true)
            {
                showHandle.WaitOne();
                try
                {
                    Dispatcher?.BeginInvoke(() =>
                    {
                        if (MainWindow is MainWindow mw) mw.ShowFromTray();
                    });
                }
                catch { /* 程序正在退出 */ }
            }
        })
        { IsBackground = true };
        listener.Start();

        base.OnStartup(e);
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
