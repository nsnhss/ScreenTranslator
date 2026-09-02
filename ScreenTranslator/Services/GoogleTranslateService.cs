using System.Net.Http;
using System.Text;
using System.Text.Json;
using ScreenTranslator.Services.Translate;

namespace ScreenTranslator.Services;

/// <summary>
/// Google 翻译免费网页接口（client=gtx），无需密钥。
/// 注意：非官方接口，稳定性无保障；国内网络环境可能无法访问。
/// </summary>
public class GoogleTranslateService : ITranslateEngine
{
    private static readonly HttpClient Http = new();

    public string Name => "google";

    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang = "zh")
    {
        string to = targetLang switch
        {
            "zh" => "zh-CN",
            _ => targetLang
        };

        string url = "https://translate.googleapis.com/translate_a/single" +
                     $"?client=gtx&sl={sourceLang}&tl={to}&dt=t&q={Uri.EscapeDataString(text)}";

        using var response = await Http.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google 翻译接口错误 [{(int)response.StatusCode}]");

        // 响应是多层嵌套数组：[[["译文","原文",...],...],...]
        using var doc = JsonDocument.Parse(body);
        var sb = new StringBuilder();
        foreach (var segment in doc.RootElement[0].EnumerateArray())
            sb.Append(segment[0].GetString());
        return sb.ToString();
    }
}
