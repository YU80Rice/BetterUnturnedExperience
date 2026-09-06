# GPT：统一 successor SourceSet 批准包

> 作者：GPT  
> 日期：2026-08-24  
> 状态：`approved`  
> SourceSetId：`BUE-SS-20260824-02`  
> Predecessor：`BUE-SS-20260824-01`

## 1. 人工裁定

人工开发者已于 2026-08-24 明确要求：动态验证无异常后重写复核报告与批准包，并继续下一步。GPT 已核对新 U3DS 日志无异常，因此以下事项获批：

1. 接受 `SCR-RT05-002`；
2. 发布不可变 successor `BUE-SS-20260824-02`；
3. 将 U3DS `Assembly-CSharp.dll` 与已重新部署并静态核验的 U3DS BepInEx `5.4.23.5` 从 `CANDIDATE_UNFROZEN` 升级为冻结引用；
4. 要求 RT-02～RT-05 在进入 RT-06 前统一迁移并重验受影响结论。

本批准不代表 BUE/LMN 在 U3DS、SP 或 P2P 运行通过，也不批准生产发布。

## 2. 冻结组件

| Component | 冻结身份 | 证据边界 |
| --- | --- | --- |
| U3-SDK source | `D:\Agent-工作目录\U3-SDK`；commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb` | `SOURCE_CONFIRMED`；tracked source clean，未跟踪 `audit/` 不属于源码集 |
| Client Assembly-CSharp | `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\Assembly-CSharp.dll`；SHA-256 `E1146353E5C9BFF901EE94829640D88919C5E89F6A6B90B22C73ABF5C1608F94` | 仅客户端 IL reference |
| Client BepInEx | `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\BepInEx.dll`；`5.4.23.5`；SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70` | 仅客户端 IL reference |
| U3DS Assembly-CSharp | `E:\Steam\steamapps\common\U3DS\Unturned_Data\Managed\Assembly-CSharp.dll`；identity `Assembly-CSharp, Version=0.0.0.0`；MVID `fc3b6b54-0730-4773-b9ff-874a73eea7d2`；SHA-256 `1AD761B15AD754259BEA8D3053B86105FAB16E09B0A122A101807C87B582060A` | 批准后可作 U3DS IL reference；不是运行证据 |
| U3DS BepInEx | `E:\Steam\steamapps\common\U3DS\BepInEx\core\BepInEx.dll`；identity `BepInEx, Version=5.4.23.5, Culture=neutral, PublicKeyToken=null`；MVID `d1b92069-86c2-41dc-ad96-bb21eee55a97`；SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70` | 可作为 U3DS IL reference；不是运行证据 |
| U3DS BepInEx Preloader | `E:\Steam\steamapps\common\U3DS\BepInEx\core\BepInEx.Preloader.dll`；identity `BepInEx.Preloader, Version=5.4.23.5, Culture=neutral, PublicKeyToken=null`；MVID `79f09773-f5a2-4b6e-9759-c0f39217b691`；SHA-256 `55D3895351A9D16B63B6F35F1C01B44AC650979E853D0BD3A442B92A082AF64F` | 引导程序集静态身份与 U3DS 动态加载均已核验 |
| LMN V5 source snapshot | 根 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`；base commit `e50082f983ac7cd33e008cca1a1a1dc3d9498d3f`；44-file ordinal digest `4F290955FCDA53BFF54E2983BECA4B08337D29266D5C3FB48BD75A4BFA62F2AF` | 工作树存在受保护演进；身份由逐文件内容摘要而非 commit 单独确定 |

U3DS 与 Unturned 客户端 BepInEx `core/` 的 18 个文件集合、文件名及逐文件 SHA-256 已由 GPT 现场比较，全部一致；`winhttp.dll`、`doorstop_config.ini` 与 `.doorstop_version` 也逐文件一致。Doorstop 已启用并指向 `BepInEx\core\BepInEx.Preloader.dll`。新 `LogOutput.log`（2026-08-24 20:22:46 +08:00；SHA-256 `E3F621373381E711536F54AB7ED25112AD584CD6BB970E44BC94CA9A0269F8EA`）确认 BepInEx/Preloader `5.4.23.5` 与 Chainloader 完整启动且无异常；日志为 `0 plugins to load`，不证明 BUE/LMN 插件运行。

## 3. Canonical LMN manifest 算法

1. 从 LMN 根递归纳入扩展名 `.cs`、`.csproj`、`.props`、`.targets`、`.md`。
2. 排除任何路径段 `.git`、`bin`、`obj`、`.scratch`、`audit`。
3. 相对路径统一为 `/`。
4. 每个文件生成：`relative/path<TAB>lowercase-sha256`。
5. 使用 `StringComparer.Ordinal` 排序完整记录。
6. 使用 LF 连接，末尾不追加 LF。
7. 对 UTF-8 bytes 计算 SHA-256。

当前结果：44 文件，`4F290955FCDA53BFF54E2983BECA4B08337D29266D5C3FB48BD75A4BFA62F2AF`。

旧 SourceSet 的 `7151D22EF361B560F44A32963F82CD973AF64D7721F9D109C5492D1D7D864DE6` 永久保留为历史 digest；不得原地改写为新算法结果。

## 4. 已知交集风险

- Client `Dedicator.IsDedicatedServer` 与 U3DS candidate 存在 property/field member-kind 差异；同名不代表 ABI 相容。
- 客户端 BepInEx reference 为 `5.4.23.5`；U3DS 也必须对齐到精确 `5.4.23.5`。不得用“5.23”模糊版本字符串代替程序集版本核对。
- Contracts 继续限制为纯 .NET Framework 4.7.2 表面，不允许 Unity、BepInEx、Harmony、Steamworks、LMN、SDG 或 UI 类型泄漏。
- 单 DLL 必须经过 client/U3DS 双引用交集编译、IL 可达性检查和真实 U3DS 加载验证。
- LMN 仍是实验性 `LmnTransportAdapter`，不是 BUE 核心强制依赖；本 SourceSet 批准不改变该边界。

## 5. 批准后的迁移动作

| Ticket | 迁移责任 | 必须重验 |
| --- | --- | --- |
| RT-02 | Gemini | Headless/UI 结论引用新 SourceSet；不得升级为运行 PASS |
| RT-03 | Gemini | U3DS 类型隔离与双引用差异；保留 VO-RT03-01～03 |
| RT-04 | GPT | 原生库存源码结论通常不受影响；记录 successor 并确认 anchors 未变 |
| RT-05 | GPT | U3DS refs 改为冻结 IL 输入；更新双端交集和关闭门禁 |

迁移不得覆盖旧 SourceSet 历史，也不得把静态引用升级成运行证据。

## 6. 人工批准记录

U3DS BepInEx 静态部署与动态引导均复核 PASS。人工批准已经满足，由 GPT：

- 已将本文件状态改为 `approved`；
- 已发布 `BUE-SS-20260824-02-Manifest.md`；
- 已将 `SCR-RT05-002` 标记 `accepted`；
- 正在分别向 Gemini 与 GPT 研究票发布迁移任务；
- 完成统一迁移后继续 `SCR-RT05-001` 原型裁定。

