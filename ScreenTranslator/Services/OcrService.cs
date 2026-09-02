using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace ScreenTranslator.Services;

/// <summary>
/// Windows 10/11 内置 WinRT OCR：本地运行、免费，识别一块区域通常 50~150ms。
/// </summary>
public static class OcrService
{
    public static async Task<string> RecognizeAsync(Bitmap bitmap)
    {
        var engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("zh-Hans-CN"))
                     ?? OcrEngine.TryCreateFromUserProfileLanguages();

        if (engine == null)
            throw new InvalidOperationException("系统没有可用的 OCR 语言包，请在 Windows 设置中添加语言。");

        using var softwareBitmap = ConvertToSoftwareBitmap(bitmap);
        var result = await engine.RecognizeAsync(softwareBitmap);
        return result.Text ?? "";
    }

    /// <summary>System.Drawing.Bitmap → WinRT SoftwareBitmap (BGRA8)</summary>
    private static SoftwareBitmap ConvertToSoftwareBitmap(Bitmap bmp)
    {
        var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int byteCount = Math.Abs(data.Stride) * bmp.Height;
            var bytes = new byte[byteCount];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, byteCount);

            var sb = new SoftwareBitmap(BitmapPixelFormat.Bgra8, bmp.Width, bmp.Height,
                                        BitmapAlphaMode.Premultiplied);
            sb.CopyFromBuffer(bytes.AsBuffer());
            return sb;
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }
}
