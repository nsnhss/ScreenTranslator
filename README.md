# ScreenTranslator 截图翻译

按下全局快捷键 → 屏幕画面冻结 → 鼠标框选区域 → WinRT OCR 识别 → 腾讯翻译 API 翻译成中文 → 悬浮窗显示译文。

## 技术栈

- **C# / WPF**（.NET 8，`net8.0-windows10.0.19041.0`）
- **WinRT OCR**（Windows 10/11 系统内置，本地运行，50~150ms）
- **可切换翻译引擎**：腾讯 / 百度 / DeepL / Google 免费接口（在 config.txt 中用 `Engine=` 切换）
- **SQLite 本地缓存**（按「原文+引擎」缓存，命中 0ms 返回，不消耗 API 额度）
- 全部配置走程序目录下的 **`config.txt`**（参考 XUnity.AutoTranslator 的配置方式）

## 编译运行

```powershell
cd ScreenTranslator
dotnet run
```

发布为单文件 exe：

```powershell
dotnet publish -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -o publish
```

## 使用方法

1. 打开程序目录下的 **`config.txt`**（主窗口里也有「打开配置文件」按钮，会用记事本直接打开）：
   ```
   SecretId=你的SecretId
   SecretKey=你的SecretKey
   ```
   - 开通地址：腾讯云控制台 → 搜索「机器翻译 TMT」→ 开通（每月 500 万字符免费）
   - 密钥获取：https://console.cloud.tencent.com/cam/capi
2. **保存文件即可**，不用重启程序，下次按快捷键时自动生效
3. 快捷键也可以在 config.txt 里改（默认 `Alt+Q`），改快捷键后需重启程序

## 数据文件

- `config.txt` — 配置文件（**在程序 exe 所在目录**，用记事本直接改）
- `%AppData%\ScreenTranslator\cache.db` — SQLite 翻译缓存

## 常见问题

- **快捷键注册失败**：快捷键被其他程序占用，换一个组合
- **OCR 识别为空**：确认 Windows 已安装中文语言包（设置 → 时间和语言 → 语言）
- **API 报错 AuthFailure**：检查 SecretId/SecretKey 是否正确，机器翻译服务是否已开通
