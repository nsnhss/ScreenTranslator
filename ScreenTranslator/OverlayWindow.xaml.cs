using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenTranslator.Services;

namespace ScreenTranslator;

/// <summary>
/// 全屏冻结窗口：把静态截图铺满全屏（画面暂停效果），
/// 用户拖拽框选后返回选区坐标。
/// WPF 用逻辑坐标（DIP），截图为物理像素，通过 _scaleX/_scaleY 换算。
/// </summary>
public partial class OverlayWindow : Window
{
    private System.Windows.Point _start;
    private bool _dragging;
    private readonly double _scaleX;
    private readonly double _scaleY;

    /// <summary>框选结果（截图物理像素坐标），取消时为 null</summary>
    public System.Drawing.Rectangle? SelectedRegion { get; private set; }

    /// <summary>框选结果（WPF 逻辑坐标，相对虚拟屏幕原点），用于结果窗定位</summary>
    public Rect? SelectedRegionDip { get; private set; }

    public OverlayWindow(System.Drawing.Bitmap frozenScreen)
    {
        InitializeComponent();

        // 覆盖整个虚拟屏幕（多显示器），WPF 逻辑坐标
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        // 物理像素 / 逻辑单位 的缩放比（如 125% 缩放下为 1.25）
        _scaleX = frozenScreen.Width / Width;
        _scaleY = frozenScreen.Height / Height;

        FrozenImage.Source = CaptureService.ToBitmapSource(frozenScreen);
        FrozenImage.Width = Width;
        FrozenImage.Height = Height;

        DimLayer.Width = Width;
        DimLayer.Height = Height;

        // 确保窗口拿到键盘焦点，ESC 才能生效
        Loaded += (_, _) =>
        {
            Activate();
            Keyboard.Focus(this);
        };
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(RootCanvas);
        _dragging = true;
        SelectionRect.Visibility = Visibility.Visible;
        RootCanvas.CaptureMouse();
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_dragging) return;
        var current = e.GetPosition(RootCanvas);

        double x = Math.Min(_start.X, current.X);
        double y = Math.Min(_start.Y, current.Y);
        double w = Math.Abs(current.X - _start.X);
        double h = Math.Abs(current.Y - _start.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        RootCanvas.ReleaseMouseCapture();

        var end = e.GetPosition(RootCanvas);
        double x = Math.Min(_start.X, end.X);
        double y = Math.Min(_start.Y, end.Y);
        double w = Math.Abs(end.X - _start.X);
        double h = Math.Abs(end.Y - _start.Y);

        // 太小的框选视为单击：不退出，只是清除选区重新框
        if (w < 10 || h < 10)
        {
            SelectionRect.Visibility = Visibility.Collapsed;
            return;
        }

        SelectedRegionDip = new Rect(x, y, w, h);
        SelectedRegion = new System.Drawing.Rectangle(
            (int)(x * _scaleX), (int)(y * _scaleY),
            (int)(w * _scaleX), (int)(h * _scaleY));
        Close();
    }

    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        SelectedRegion = null;
        Close();
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            SelectedRegion = null;
            Close();
        }
    }
}
