using System.Windows;
using System.Windows.Input;

namespace ScreenTranslator;

/// <summary>翻译结果悬浮窗：置顶显示在框选区域附近，可复制。</summary>
public partial class ResultWindow : Window
{
    public ResultWindow(string source, string target, bool fromCache, System.Windows.Point anchor)
    {
        InitializeComponent();

        SourceText.Text = source;
        TargetText.Text = target;
        BadgeText.Text = fromCache ? "⚡ 缓存命中" : "";

        // 显示在框选区域下方，防止超出屏幕
        ContentRendered += (_, _) =>
        {
            double left = anchor.X;
            double top = anchor.Y + 8;
            double screenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
            double screenBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
            if (left + ActualWidth > screenRight) left = screenRight - ActualWidth - 8;
            if (top + ActualHeight > screenBottom) top = anchor.Y - ActualHeight - 8;
            if (left < SystemParameters.VirtualScreenLeft) left = SystemParameters.VirtualScreenLeft + 8;
            if (top < SystemParameters.VirtualScreenTop) top = SystemParameters.VirtualScreenTop + 8;
            Left = left;
            Top = top;
        };

        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }

    private void OnDragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void OnCopyTarget(object sender, RoutedEventArgs e)
        => System.Windows.Clipboard.SetText(TargetText.Text);

    private void OnCopySource(object sender, RoutedEventArgs e)
        => System.Windows.Clipboard.SetText(SourceText.Text);

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
