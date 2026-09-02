using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenTranslator.Services;

/// <summary>
/// 屏幕截图：GDI CopyFromScreen 抓取整个虚拟屏幕（支持多显示器）。
/// 清单中 dpiAware=false，WPF 坐标与 GDI 坐标一致，无需缩放换算。
/// </summary>
public static class CaptureService
{
    // PerMonitorV2 进程下 GetSystemMetrics 返回物理像素
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    public static Bitmap CaptureFullScreen()
    {
        int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        }
        return bmp;
    }

    public static Bitmap Crop(Bitmap source, System.Drawing.Rectangle rect)
    {
        var r = System.Drawing.Rectangle.Intersect(
            new System.Drawing.Rectangle(0, 0, source.Width, source.Height),
            new System.Drawing.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));

        // 框选区域向外扩几像素，避免切掉文字边缘
        r.Inflate(2, 2);
        r = System.Drawing.Rectangle.Intersect(
            new System.Drawing.Rectangle(0, 0, source.Width, source.Height), r);

        return source.Clone(r, source.PixelFormat);
    }

    /// <summary>Bitmap → WPF ImageSource（用于全屏冻结画面显示）</summary>
    public static BitmapSource ToBitmapSource(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
