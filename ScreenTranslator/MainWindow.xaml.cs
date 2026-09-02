using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ScreenTranslator.Services;

namespace ScreenTranslator;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly HotkeyService _hotkey = new();
    private readonly CacheService _cache = new();
    private AppConfig _config = new();
    private bool _busy;
    private bool _uiReady;
    private bool _reallyExit;

    private System.Windows.Forms.NotifyIcon? _tray;
    private FileSystemWatcher? _configWatcher;
    private DateTime _lastWatchEvent = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        _config = ConfigService.Load();

        // 引擎/语言下拉框：选中当前配置的值
        SelectComboByTag(EngineBox, _config.Engine);
        SelectComboByTag(SourceLangBox, _config.SourceLang);
        SelectComboByTag(TargetLangBox, _config.TargetLang);

        AutoStartCheck.IsChecked = AutoStartService.IsEnabled();
        _uiReady = true;

        InitTray();
        InitConfigWatcher();   // config.txt 保存后自动重载，改快捷键不用重启

        _hotkey.HotkeyPressed += async () => await RunTranslateFlowAsync();
        SourceInitialized += (_, _) => RegisterHotkey();
        Closed += (_, _) =>
        {
            _hotkey.Dispose();
            _configWatcher?.Dispose();
            _tray?.Dispose();
        };

        UpdateStatus();
    }

    // ================= 配置文件监视（热重载） =================

    private void InitConfigWatcher()
    {
        var dir = Path.GetDirectoryName(ConfigService.ConfigPath)!;
        _configWatcher = new FileSystemWatcher(dir, Path.GetFileName(ConfigService.ConfigPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        _configWatcher.Changed += OnConfigFileChanged;
        _configWatcher.Created += OnConfigFileChanged;
        _configWatcher.Renamed += OnConfigFileChanged;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // 记事本保存会触发多次事件，400ms 内去抖
        var now = DateTime.Now;
        if ((now - _lastWatchEvent).TotalMilliseconds < 400) return;
        _lastWatchEvent = now;

        Dispatcher.BeginInvoke(new Action(ReloadConfigFromFile));
    }

    private void ReloadConfigFromFile()
    {
        try
        {
            var newConfig = ConfigService.Load();

            // 内容没变化（比如是程序自己写回的）就不处理，避免循环触发
            if (newConfig.Engine == _config.Engine
                && newConfig.HotkeyModifiers == _config.HotkeyModifiers
                && newConfig.HotkeyKey == _config.HotkeyKey
                && newConfig.TargetLang == _config.TargetLang
                && newConfig.SourceLang == _config.SourceLang
                && newConfig.SourceLang == _config.SourceLang
                && newConfig.TencentSecretId == _config.TencentSecretId
                && newConfig.TencentSecretKey == _config.TencentSecretKey
                && newConfig.BaiduAppId == _config.BaiduAppId
                && newConfig.BaiduSecretKey == _config.BaiduSecretKey
                && newConfig.DeepLAuthKey == _config.DeepLAuthKey)
                return;

            bool hotkeyChanged = newConfig.HotkeyModifiers != _config.HotkeyModifiers
                                 || newConfig.HotkeyKey != _config.HotkeyKey;
            _config = newConfig;

            // 同步下拉框（不触发保存事件）
            _uiReady = false;
            SelectComboByTag(EngineBox, _config.Engine);
            SelectComboByTag(SourceLangBox, _config.SourceLang);
            SelectComboByTag(TargetLangBox, _config.TargetLang);
            _uiReady = true;

            // 快捷键变化时重新注册，立即生效
            if (hotkeyChanged) RegisterHotkey();

            UpdateStatus();
        }
        catch
        {
            // 文件可能正在被写入，忽略本次事件
        }
    }

    private static void SelectComboByTag(System.Windows.Controls.ComboBox box, string tag)
    {
        foreach (ComboBoxItem item in box.Items)
            if ((string)item.Tag == tag) { item.IsSelected = true; return; }
        ((ComboBoxItem)box.Items[0]).IsSelected = true;
    }

    // ================= 托盘图标 =================

    private void InitTray()
    {
        _tray = new System.Windows.Forms.NotifyIcon
        {
            Text = "ScreenTranslator 截图翻译",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
                   ?? System.Drawing.SystemIcons.Application,
            Visible = true
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) => ShowMainWindow());
        menu.Items.Add("退出", null, (_, _) => ExitApp());
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        _reallyExit = true;
        if (_tray != null) _tray.Visible = false;
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>点 X 关闭窗口 = 隐藏到托盘，不退出程序</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_reallyExit)
        {
            e.Cancel = true;
            Hide();
            _tray?.ShowBalloonTip(2000, "ScreenTranslator",
                "程序已最小化到右下角托盘，快捷键仍可用。右键托盘图标可退出。",
                System.Windows.Forms.ToolTipIcon.Info);
        }
        base.OnClosing(e);
    }

    /// <summary>最小化也收进托盘</summary>
    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized) Hide();
        base.OnStateChanged(e);
    }

    // ================= 界面事件 =================

    private void OnEngineChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady || EngineBox.SelectedItem is not ComboBoxItem item) return;
        _config.Engine = (string)item.Tag;
        ConfigService.Save(_config);   // 同步写回 config.txt
        UpdateStatus();
    }

    private void OnLangChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady) return;
        if (SourceLangBox.SelectedItem is ComboBoxItem src)
            _config.SourceLang = (string)src.Tag;
        if (TargetLangBox.SelectedItem is ComboBoxItem tgt)
            _config.TargetLang = (string)tgt.Tag;
        ConfigService.Save(_config);   // 同步写回 config.txt
        UpdateStatus();
    }

    private void OnAutoStartClick(object sender, RoutedEventArgs e)
    {
        try
        {
            AutoStartService.SetEnabled(AutoStartCheck.IsChecked == true);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"⚠ 设置开机自启失败：{ex.Message}";
        }
    }

    // ================= 快捷键 =================

    private void RegisterHotkey()
    {
        try
        {
            var key = (System.Windows.Input.Key)Enum.Parse(
                typeof(System.Windows.Input.Key), _config.HotkeyKey);
            _hotkey.Register(this, _config.HotkeyModifiers, key);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"⚠ {ex.Message}";
        }
    }

    // ================= 配置文件 =================

    /// <summary>用记事本打开 config.txt（参考 XUnity 的配置方式）</summary>
    private void OnOpenConfig(object sender, RoutedEventArgs e)
    {
        ConfigService.Load();   // 确保文件存在
        System.Diagnostics.Process.Start("notepad.exe", ConfigService.ConfigPath);
    }

    private void UpdateStatus()
    {
        var (entries, size) = _cache.Stats();
        StatusText.Text =
            $"快捷键：{ConfigService.HotkeyDisplay(_config)}    " +
            $"缓存：{entries} 条 / {size / 1024.0:F1} KB\n" +
            $"配置文件：{ConfigService.ConfigPath}\n" +
            $"API 密钥请在 config.txt 中填写";
    }

    // ================= 核心流程 =================

    private async void OnTranslateNow(object sender, RoutedEventArgs e)
        => await RunTranslateFlowAsync();

    private async Task RunTranslateFlowAsync()
    {
        if (_busy) return;   // 防止重复触发
        _busy = true;

        try
        {
            // 1. 重新读取配置文件（记事本改完保存即可生效，不用重启）
            _config = ConfigService.Load();

            // 2. 瞬间截全屏 —— "暂停画面"
            using var screen = CaptureService.CaptureFullScreen();

            // 3. 全屏冻结窗口 + 框选
            var overlay = new OverlayWindow(screen);
            overlay.ShowDialog();
            if (overlay.SelectedRegion is not { } region) return;

            // 4. 裁剪选区（物理像素）
            using var cropped = CaptureService.Crop(screen, region);

            // 结果窗定位锚点（WPF 逻辑坐标）
            var dip = overlay.SelectedRegionDip!.Value;
            var anchor = new System.Windows.Point(
                SystemParameters.VirtualScreenLeft + dip.X,
                SystemParameters.VirtualScreenTop + dip.Bottom);

            // 5. WinRT OCR
            string text = (await OcrService.RecognizeAsync(cropped)).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                new ResultWindow("（未识别到文字）", "请尝试框选更清晰的文字区域", true, anchor).Show();
                return;
            }

            // 6. 查缓存 → 未命中才调翻译 API
            var engine = TranslateEngineFactory.Create(_config);
            string from = _config.SourceLang, to = _config.TargetLang;
            bool fromCache = true;
            string? result = _cache.TryGet(text, engine.Name, from, to);

            if (result == null)
            {
                fromCache = false;
                result = await engine.TranslateAsync(text, from, to);
                _cache.Put(text, result, engine.Name, from, to);
                UpdateStatus();
            }

            // 7. 显示结果
            new ResultWindow(text, result, fromCache, anchor).Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "翻译失败",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _busy = false;
        }
    }
}
