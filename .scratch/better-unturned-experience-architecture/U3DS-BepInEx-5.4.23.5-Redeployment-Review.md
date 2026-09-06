# GPT：U3DS BepInEx 5.4.23.5 重新部署复核

> 作者：GPT  
> 日期：2026-08-24  
> 判定：静态部署 `PASS`；BepInEx 动态引导 `PASS`；BUE/LMN 插件运行 `UNRESOLVED`

## 1. 静态部署复核

- U3DS `BepInEx/core` 与 Unturned 客户端部署的 18 个文件集合完全一致，逐文件 SHA-256 全部一致。
- U3DS `BepInEx.dll`：identity `BepInEx, Version=5.4.23.5, Culture=neutral, PublicKeyToken=null`；MVID `d1b92069-86c2-41dc-ad96-bb21eee55a97`；SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70`。
- U3DS `BepInEx.Preloader.dll`：identity `BepInEx.Preloader, Version=5.4.23.5, Culture=neutral, PublicKeyToken=null`；MVID `79f09773-f5a2-4b6e-9759-c0f39217b691`；SHA-256 `55D3895351A9D16B63B6F35F1C01B44AC650979E853D0BD3A442B92A082AF64F`。
- `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version` 与客户端部署逐文件一致。
- Doorstop `enabled = true`，目标为 `BepInEx\core\BepInEx.Preloader.dll`。

## 2. 动态引导证据

| 字段 | 证据 |
| --- | --- |
| 日志 | `E:\Steam\steamapps\common\U3DS\BepInEx\LogOutput.log` |
| LastWriteTime | `2026-08-24 20:22:46 +08:00` |
| SHA-256 | `E3F621373381E711536F54AB7ED25112AD584CD6BB970E44BC94CA9A0269F8EA` |
| 行数 | 14 |
| BepInEx | `BepInEx 5.4.23.5 - Unturned` |
| Preloader | `Loaded 1 patcher method from [BepInEx.Preloader 5.4.23.5]` |
| Chainloader | `ready`、`started`、`startup complete` |
| 错误扫描 | 未发现 error、exception、fatal、wrong-version、TypeLoadException 或 MissingMethodException |

动态引导判定：`RUNTIME_CONFIRMED`，仅适用于 U3DS 上 BepInEx 5.4.23.5 引导链。

## 3. 明确边界

日志记录 `0 plugins to load`。因此本次证据不证明 BUE 单 DLL、LMN、ClientUi 隔离、设置、生命周期或网络能力运行，也不证明 U3DS 发布门禁通过。这些仍需同一 Candidate DLL SHA-256、CaseId 与新日志另行取证。

## 4. 结论

U3DS BepInEx 版本对齐阻断已解除。上述程序集已具备作为 `BUE-SS-20260824-02` 冻结 U3DS IL references 的条件；动态日志作为独立运行证据附加，不改变 SourceSet 的静态身份语义。
