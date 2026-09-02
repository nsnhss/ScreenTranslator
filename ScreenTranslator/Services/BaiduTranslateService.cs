using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ScreenTranslator.Services.Translate;

namespace ScreenTranslator.Services;

/// <summary>
/// 百度翻译 API（fanyi-api.baidu.com）。
/// 个人认证后每月 100 万字符免费。
/// </summary>
public class BaiduTranslateService : ITranslateEngine
{
    private const string Url = "https://fanyi-api.baidu.com/api/trans/vip/translate";

    private readonly string _appId;
    private readonly string _secretKey;
    private static readonly HttpClient Http = new();
    private static readonly Random Rng = new();

    public string Name => "baidu";

    public BaiduTranslateService(string appId, string secretKey)
    {
        _appId = appId;
        _secretKey = secretKey;
    }

    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang = "zh")
    {
        if (string.IsNullOrWhiteSpace(_appId) || string.IsNullOrWhiteSpace(_secretKey))
            throw new InvalidOperationException("未配置百度翻译密钥，请在 config.txt 中填写 BaiduAppId / BaiduSecretKey");

        string to = targetLang switch
        {
            "ja" => "jp",
            "ko" => "kor",
            _ => targetLang
        };
        string from = sourceLang switch
        {
            "ja" => "jp",
            "ko" => "kor",
            _ => sourceLang
        };

        string salt = Rng.Next(100000, 999999).ToString();
        string sign = Md5(_appId + text + salt + _secretKey);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["q"] = text,
            ["from"] = from,
            ["to"] = to,
            ["appid"] = _appId,
            ["salt"] = salt,
            ["sign"] = sign
        });

        using var response = await Http.PostAsync(Url, form);
        string body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("error_code", out var errCode))
        {
            string msg = root.TryGetProperty("error_msg", out var m) ? m.GetString() ?? "" : "";
            throw new InvalidOperationException(
                $"百度翻译 API 错误 [{errCode.GetString()}] {msg}");
        }

        var sb = new StringBuilder();
        foreach (var item in root.GetProperty("trans_result").EnumerateArray())
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(item.GetProperty("dst").GetString());
        }
        return sb.ToString();
    }

    private static string Md5(string input)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
