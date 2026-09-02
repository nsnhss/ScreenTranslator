using System.IO;
using System.Text;

namespace ScreenTranslator.Services;

public class AppConfig
{
    /// <summary>翻译引擎: tencent / baidu / deepl / google</summary>
    public string Engine { get; set; } = "tencent";

    // ===== 各引擎密钥 =====
    public string TencentSecretId { get; set; } = "";
    public string TencentSecretKey { get; set; } = "";
    public string BaiduAppId { get; set; } = "";
    public string BaiduSecretKey { get; set; } = "";
    public string DeepLAuthKey { get; set; } = "";

    /// <summary>快捷键修饰键，例如 "Alt"、"Ctrl+Alt"</summary>
    public string HotkeyModifiers { get; set; } = "Alt";

    /// <summary>快捷键主键，WPF Key 枚举名，例如 "Q"</summary>
    public string HotkeyKey { get; set; } = "Q";

    /// <summary>目标语言（zh / en / ja / ko）</summary>
    public string TargetLang { get; set; } = "zh";

    /// <summary>源语言（auto 自动识别，或 zh / en / ja / ko）。
    /// 日文含大量汉字，自动识别容易误判为中文，看日文内容建议设为 ja</summary>
    public string SourceLang { get; set; } = "auto";
}

/// <summary>
/// 配置读写：程序目录下的 config.txt（key=value 格式，# 开头为注释）。
/// 参考 XUnity.AutoTranslator 的方式，直接用记事本编辑即可。
/// </summary>
public static class ConfigService
{
    /// <summary>配置文件路径（程序 exe 所在目录）</summary>
    public static readonly string ConfigPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");

    public static string DataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenTranslator");

    public static AppConfig Load()
    {
        var config = new AppConfig();

        if (!File.Exists(ConfigPath))
        {
            Save(config);   // 首次运行生成模板
            return config;
        }

        foreach (var rawLine in File.ReadAllLines(ConfigPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            int eq = line.IndexOf('=');
            if (eq <= 0) continue;

            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim();

            switch (key)
            {
                case "Engine": config.Engine = value; break;
                case "TencentSecretId": config.TencentSecretId = value; break;
                case "TencentSecretKey": config.TencentSecretKey = value; break;
                case "BaiduAppId": config.BaiduAppId = value; break;
                case "BaiduSecretKey": config.BaiduSecretKey = value; break;
                case "DeepLAuthKey": config.DeepLAuthKey = value; break;
                case "HotkeyModifiers": config.HotkeyModifiers = value; break;
                case "HotkeyKey": config.HotkeyKey = value; break;
                case "TargetLang": config.TargetLang = value; break;
                case "SourceLang": config.SourceLang = value; break;
            }
        }
        return config;
    }

    public static void Save(AppConfig config)
    {
        var content = $$"""
            # ==========================================================
            #  ScreenTranslator 配置文件
            #  修改后保存即生效（改快捷键也不用重启）
            # ==========================================================

            # ---------- 翻译引擎 ----------
            # 可选: tencent(腾讯) / baidu(百度) / deepl / google(免费,无需密钥)
            Engine={{config.Engine}}

            # 腾讯翻译: https://console.cloud.tencent.com/cam/capi
            # 每月 500 万字符免费
            TencentSecretId={{config.TencentSecretId}}
            TencentSecretKey={{config.TencentSecretKey}}

            # 百度翻译: https://fanyi-api.baidu.com
            # 个人认证后每月 100 万字符免费
            BaiduAppId={{config.BaiduAppId}}
            BaiduSecretKey={{config.BaiduSecretKey}}

            # DeepL: https://www.deepl.com/pro-api
            # 翻译质量最高，免费额度有限
            DeepLAuthKey={{config.DeepLAuthKey}}

            # ---------- 截图翻译快捷键 ----------
            # 修饰键可选: Ctrl / Alt / Shift / Win，多个用 + 连接
            # 主键: A-Z、F1-F12、D0-D9 等
            HotkeyModifiers={{config.HotkeyModifiers}}
            HotkeyKey={{config.HotkeyKey}}

            # ---------- 源语言 ----------
            # auto 自动识别，或 zh / en / ja / ko
            # 注意：日文含大量汉字，自动识别容易误判为中文，
            #       看日文内容时建议设为 ja
            SourceLang={{config.SourceLang}}

            # ---------- 目标语言 ----------
            # zh 中文 / en 英文 / ja 日文 / ko 韩文
            TargetLang={{config.TargetLang}}
            """;

        // UTF-8 带 BOM + CRLF 换行：保证记事本/旧版 Windows 正确显示
        File.WriteAllText(ConfigPath,
            content.Replace("\n", Environment.NewLine),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public static string HotkeyDisplay(AppConfig c) => $"{c.HotkeyModifiers}+{c.HotkeyKey}";
}
