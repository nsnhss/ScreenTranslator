namespace ScreenTranslator.Services.Translate;

/// <summary>翻译引擎统一接口</summary>
public interface ITranslateEngine
{
    /// <summary>引擎名称（用于缓存 key 和界面显示）</summary>
    string Name { get; }

    Task<string> TranslateAsync(string text, string sourceLang, string targetLang);
}
