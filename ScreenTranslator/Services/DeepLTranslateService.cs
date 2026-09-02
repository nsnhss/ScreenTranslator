using System.Net.Http;
using System.Text.Json;
using ScreenTranslator.Services.Translate;

namespace ScreenTranslator.Services;

/// <summary>
/// DeepL API Free（api-free.deepl.com），翻译质量最高。
/// 注意：2026 年起新用户为 Developer 计划，共 100 万字符（不按月重置）。
/// </summary>
public class DeepLTranslateService : ITranslateEngine
{
    private const string Url = "https://api-free.deepl.com/v2/translate";

    private readonly string _authKey;
    private static readonly HttpClient Http = new();

    public string Name => "deepl";

    public DeepLTranslateService(string authKey)
    {
        _authKey = authKey;
    }

    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang = "zh")
    {
        if (string.IsNullOrWhiteSpace(_authKey))
            throw new InvalidOperationException("未配置 DeepL 密钥，请在 config.txt 中填写 DeepLAuthKey");

        string to = targetLang.ToUpperInvariant() switch
        {
            "ZH" => "ZH",
            "EN" => "EN",
            "JA" => "JA",
            "KO" => "KO",
            var other => other
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Url);
        request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {_authKey}");
        var fields = new Dictionary<string, string>
        {
            ["text"] = text,
            ["target_lang"] = to
        };
        if (sourceLang != "auto")
            fields["source_lang"] = sourceLang.ToUpperInvariant();
        request.Content = new FormUrlEncodedContent(fields);

        using var response = await Http.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"DeepL API 错误 [{(int)response.StatusCode}] {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
                  .GetProperty("translations")[0]
                  .GetProperty("text").GetString() ?? "";
    }
}
