# GPT：BUE SourceSet Manifest `BUE-SS-20260824-02`

> 作者：GPT  
> 状态：`approved-frozen`  
> SourceSetId：`BUE-SS-20260824-02`  
> PredecessorSourceSetId：`BUE-SS-20260824-01`  
> 批准日期：2026-08-24

## 1. 不可变组件身份

| Component | Identity |
| --- | --- |
| U3-SDK | 根 `D:\Agent-工作目录\U3-SDK`；commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`；tracked source clean，未跟踪 `audit/` 排除 |
| Client Assembly-CSharp | `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\Assembly-CSharp.dll`；SHA-256 `E1146353E5C9BFF901EE94829640D88919C5E89F6A6B90B22C73ABF5C1608F94` |
| Client BepInEx | `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\BepInEx.dll`；`5.4.23.5`；SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70` |
| U3DS Assembly-CSharp | `E:\Steam\steamapps\common\U3DS\Unturned_Data\Managed\Assembly-CSharp.dll`；identity `Assembly-CSharp, Version=0.0.0.0`；MVID `fc3b6b54-0730-4773-b9ff-874a73eea7d2`；SHA-256 `1AD761B15AD754259BEA8D3053B86105FAB16E09B0A122A101807C87B582060A` |
| U3DS BepInEx | `E:\Steam\steamapps\common\U3DS\BepInEx\core\BepInEx.dll`；identity `BepInEx, Version=5.4.23.5, Culture=neutral, PublicKeyToken=null`；MVID `d1b92069-86c2-41dc-ad96-bb21eee55a97`；SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70` |
| U3DS BepInEx Preloader | `E:\Steam\steamapps\common\U3DS\BepInEx\core\BepInEx.Preloader.dll`；identity `BepInEx.Preloader, Version=5.4.23.5, Culture=neutral, PublicKeyToken=null`；MVID `79f09773-f5a2-4b6e-9759-c0f39217b691`；SHA-256 `55D3895351A9D16B63B6F35F1C01B44AC650979E853D0BD3A442B92A082AF64F` |
| LMN V5 source snapshot | 根 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`；base commit `e50082f983ac7cd33e008cca1a1a1dc3d9498d3f`；44-file ordinal digest `4F290955FCDA53BFF54E2983BECA4B08337D29266D5C3FB48BD75A4BFA62F2AF` |

## 2. LMN manifest 算法

- 纳入 `.cs/.csproj/.props/.targets/.md`。
- 排除路径段 `.git/bin/obj/.scratch/audit`。
- 相对路径使用 `/`。
- 记录格式：`relative/path<TAB>lowercase-sha256`。
- 使用 `StringComparer.Ordinal` 排序。
- UTF-8、LF 连接、末尾无 LF。
- 当前记录数：44。
- 最终摘要：`4F290955FCDA53BFF54E2983BECA4B08337D29266D5C3FB48BD75A4BFA62F2AF`。

## 3. 证据边界

- SourceSet 冻结源码和 reference identities，不证明运行或发布。
- U3DS BepInEx 动态引导证据位于 `U3DS-BepInEx-5.4.23.5-Redeployment-Review.md`；该日志显示 `0 plugins to load`，不证明 BUE/LMN。
- LMN 工作树包含受保护演进，因此必须同时引用 base commit 与 ordinal content digest。
- 旧 SourceSet 与 legacy digest 永久保留，不得原地改写。

## 4. 已知 ABI 风险

- `Dedicator.IsDedicatedServer` 在 client/U3DS reference 中存在 member-kind 差异；不得按同名假设二进制兼容。
- Contracts 只使用双方证明存在的纯 .NET Framework 4.7.2 表面。
- 单 DLL 仍需双引用编译、IL 可达性扫描与真实 U3DS 插件加载验证。

