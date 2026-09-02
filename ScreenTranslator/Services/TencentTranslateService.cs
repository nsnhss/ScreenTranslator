using System.Net.Http;
using ScreenTranslator.Services.Translate;

namespace ScreenTranslator.Services;

/// <summary>
/// 腾讯机器翻译 API（tmt.tencentcloudapi.com，TextTranslate，2018-03-21）。
/// 使用 TC3-HMAC-SHA256 签名，每月 500 万字符免费额度。
/// </summary>
public class TencentTranslateService : ITranslateEngine
{
    private const string Host = "tmt.tencentcloudapi.com";
    private const string Service = "tmt";
    private const string Action = "TextTranslate";
    private const string Version = "2018-03-21";
    private const string Region = "ap-guangzhou";
    private const string Algorithm = "TC3-HMAC-SHA256";

    private readonly string _secretId;
    private readonly string _secretKey;

    // 复用 HttpClient 保持长连接，省掉 TCP/TLS 握手开销
    private static readonly HttpClient Http = new();

    public string Name => "tencent";

    public TencentTranslateService(string secretId, string secretKey)
    {
        _secretId = secretId;
        _secretKey = secretKey;
    }

    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang = "zh")
    {
        var (result, source) = await TranslateInternalAsync(text, sourceLang, targetLang);

        // 源语言 = 目标语言时 API 直接返回原文（例如框选了中文内容），
        // 自动转为翻译成英文兼底
        if (!string.IsNullOrEmpty(source) && source.StartsWith(targetLang))
        {
            string alt = targetLang == "zh" ? "en" : "zh";
            (result, _) = await TranslateInternalAsync(text, sourceLang, alt);
        }
        return result;
    }

    /// <summary>返回 (译文, 检测到的源语言)</summary>
    private async Task<(string Target, string Source)> TranslateInternalAsync(
        string text, string sourceLang, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(_secretId) || string.IsNullOrWhiteSpace(_secretKey))
            throw new InvalidOperationException("未配置腾讯翻译密钥，请在 config.txt 中填写 TencentSecretId / TencentSecretKey");

        string payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            SourceText = text,
            Source = sourceLang,
            Target = targetLang,
            ProjectId = 0
        });

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

        // ===== 第 1 步：拼接规范请求串 =====
        string hashedPayload = Sha256Hex(payload);
        string canonicalHeaders = $"content-type:application/json; charset=utf-8\nhost:{Host}\n";
        string signedHeaders = "content-type;host";
        string canonicalRequest =
            $"POST\n/\n\n{canonicalHeaders}\n{signedHeaders}\n{hashedPayload}";

        // ===== 第 2 步：拼接待签名字符串 =====
        string credentialScope = $"{date}/{Service}/tc3_request";
        string stringToSign =
            $"{Algorithm}\n{timestamp}\n{credentialScope}\n{Sha256Hex(canonicalRequest)}";

        // ===== 第 3 步：计算签名 =====
        byte[] secretDate = HmacSha256(System.Text.Encoding.UTF8.GetBytes("TC3" + _secretKey), date);
        byte[] secretService = HmacSha256(secretDate, Service);
        byte[] secretSigning = HmacSha256(secretService, "tc3_request");
        string signature = ToHex(HmacSha256(secretSigning, stringToSign));

        string authorization =
            $"{Algorithm} Credential={_secretId}/{credentialScope}, " +
            $"SignedHeaders={signedHeaders}, Signature={signature}";

        // ===== 发送请求 =====
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{Host}/");
        request.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("X-TC-Action", Action);
        request.Headers.TryAddWithoutValidation("X-TC-Version", Version);
        request.Headers.TryAddWithoutValidation("X-TC-Region", Region);
        request.Headers.TryAddWithoutValidation("X-TC-Timestamp", timestamp.ToString());
        request.Headers.Host = Host;

        using var response = await Http.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var resp = doc.RootElement.GetProperty("Response");

        if (resp.TryGetProperty("Error", out var err))
        {
            string code = err.GetProperty("Code").GetString() ?? "";
            string msg = err.GetProperty("Message").GetString() ?? "";
            throw new InvalidOperationException($"腾讯翻译 API 错误 [{code}] {msg}");
        }

        return (resp.GetProperty("TargetText").GetString() ?? "",
                resp.TryGetProperty("Source", out var src) ? src.GetString() ?? "" : "");
    }

    private static string Sha256Hex(string data)
        => ToHex(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data)));

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        return hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
    }

    private static string ToHex(byte[] bytes)
        => Convert.ToHexString(bytes).ToLowerInvariant();
}
