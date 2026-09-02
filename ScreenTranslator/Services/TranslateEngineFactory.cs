using ScreenTranslator.Services.Translate;

namespace ScreenTranslator.Services;

/// <summary>根据配置创建翻译引擎</summary>
public static class TranslateEngineFactory
{
    public static ITranslateEngine Create(AppConfig config) =>
        config.Engine.Trim().ToLowerInvariant() switch
        {
            "baidu" => new BaiduTranslateService(config.BaiduAppId, config.BaiduSecretKey),
            "deepl" => new DeepLTranslateService(config.DeepLAuthKey),
            "google" => new GoogleTranslateService(),
            _ => new TencentTranslateService(config.TencentSecretId, config.TencentSecretKey),
        };

    public static readonly string[] SupportedEngines = { "tencent", "baidu", "deepl", "google" };
}
