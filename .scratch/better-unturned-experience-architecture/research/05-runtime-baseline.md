# GPT-05 — Unturned 与 BepInEx 运行时技术基线

- 作者：GPT
- 调查日期：2026-08-24
- 调查方式：官方文档、官方安装脚本、本机实际程序集/日志/既有工程交叉验证
- 范围：只读研究；未修改生产代码，未执行游戏或 U3DS 的新一轮运行测试

## 结论摘要

1. **客户端当前已观测基线**是 Unturned `26.3.8`、Unity `2022.3.62f3` / BepInEx `5.4.23.5`、64 位 Windows Mono/CLR `4.0.30319.42000`。
2. **U3DS 当前已观测基线**使用同一 Unity 主版本和 CLR，但 BepInEx 是 `5.4.22.0`，且 U3DS 与客户端的 `Assembly-CSharp.dll` 不同。因此不能用客户端编译/加载结果代替 U3DS 证据。
3. 本工作区已有稳定可构建的 BepInEx 项目采用 **.NET Framework 4.7.2 + C# 10**。这是可采用的工程基线，但 **C# 10 只代表编译器语法选择**，不代表 Unity Mono 拥有现代 .NET 的全部 BCL/API。
4. U3DS 官方脚本以 `-batchmode -nographics` 启动。因此 UI/HUD 前端不得成为共享契约、核心运行时或服务端功能的硬依赖。
5. 当前 U3DS BepInEx 低于客户端，实际 U3DS 日志已对目标 `5.4.23.5` 的插件发出版本不匹配警告。项目必须在“统一部署 BepInEx 版本”与“以两端共同兼容面构建”之间做出明确决策；在新框架双端实际加载前不宣称已兼容。

## 一、已证实事实

### 1.1 客户端运行时

| 项目 | 已观测值 | 证据 |
| --- | --- | --- |
| Unturned 游戏版本 | `26.3.8` | `E:\Steam\steamapps\common\Unturned\Status.json` 的 `Game.Major/Minor/Patch_Version` |
| Unity | `2022.3.62f3 (96770f904ca7)` | `C:\Users\The New Age\AppData\LocalLow\Smartly Dressed Games\Unturned\Player.log`；`Unturned.exe` ProductVersion |
| BepInEx | `5.4.23.5` | `E:\Steam\steamapps\common\Unturned\BepInEx\LogOutput.log`；`BepInEx\core\BepInEx.dll` Assembly/FileVersion |
| CLR | `4.0.30319.42000` | 同上 `LogOutput.log` |
| 平台 | Windows 64-bit | 同上 `LogOutput.log` |
| 注入路径 | Doorstop → `BepInEx.Preloader.dll` → Chainloader | `E:\Steam\steamapps\common\Unturned\doorstop_config.ini` 中 `enabled=true` 且 `target_assembly=BepInEx\core\BepInEx.Preloader.dll`；日志显示 Chainloader 启动 |

可复查文件指纹：

- `E:\Steam\steamapps\common\Unturned\Unturned.exe`：SHA-256 `3546771573F932377C2948F6542F28EB7CD78908D8647C16FAA7C8529D484304`
- `E:\Steam\steamapps\common\Unturned\UnityPlayer.dll`：SHA-256 `55B527195BAD4EFB4B2F1F17FF5D6F8C69D3C5ED037F9CDABF0844D732190CD9`
- `E:\Steam\steamapps\common\Unturned\BepInEx\core\BepInEx.dll`：SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70`
- `E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll`：4,686,048 bytes，SHA-256 `E1146353E5C9BFF901EE94829640D88919C5E89F6A6B90B22C73ABF5C1608F94`

### 1.2 托管 API 表面

客户端实际 `Unturned_Data\Managed` 包含：

- `mscorlib.dll` AssemblyVersion `4.0.0.0`
- `System.dll` AssemblyVersion `4.0.0.0`
- `netstandard.dll` AssemblyVersion `2.1.0.0`

Unity 2022.3 官方手册说明 Unity 支持 .NET Standard 2.1 与 .NET Framework 4.8 API 兼容性级别，但“兼容性级别”不等于此游戏当前 Mono 运行时能无条件加载任意现代 .NET 产物。对 BepInEx 插件应以实际 Managed 程序集及两端加载结果为最终边界。

官方来源：

- Unity 2022.3 《.NET profile support》：https://docs.unity3d.com/2022.3/Documentation/Manual/dotnetProfileSupport.html
- BepInEx 5.4 《Plugin tutorial: Setting up the development environment》：https://docs.bepinex.dev/v5.4.21/articles/dev_guide/plugin_tutorial/1_setup.html

### 1.3 可用 .NET/C# 工程基线

本工作区两个已有插件工程独立采用了相同组合：

- `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet\LaunchMultiplayerNet.csproj`：`TargetFrameworkVersion=v4.7.2`，`LangVersion=10`，直接引用 BepInEx、`Assembly-CSharp`和 UnityEngine 程序集。
- `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Archive\2-未闭环验证项目\LaunchInventoryTidy\LaunchInventoryTidy.csproj`：同样使用 `v4.7.2` + C# 10。

因此，本项目可以把 **.NET Framework 4.7.2 + C# 10** 作为首版构建基线。但这里的 C# 10 只是 Roslyn 语法级别；生产代码仍只能使用客户端与 U3DS 都存在的 BCL/Unity/Unturned/BepInEx API，并避免生成对两端缺失类型或方法的引用。

### 1.4 U3DS 运行时与加载边界

U3DS 是独立安装与独立运行时：

- 安装根：`E:\Steam\steamapps\common\U3DS`
- `ServerHelper.bat` 启动：`Unturned.exe -batchmode -nographics %*`
- `ExampleServer.bat` 说明传入 `+InternetServer/ServerId` 或 `+LanServer/ServerId` 时以 dedicated server 运行，并直接指向 Smartly Dressed Games 官方托管文档。
- `U3DS_Player.log` 实测显示 `Forcing GfxDevice: Null`、`NullGfxDevice`、Unity `2022.3.62f3` 及 BepInEx 预加载。

U3DS 当前身份：

| 项目 | U3DS | 与客户端关系 |
| --- | --- | --- |
| Unity | `2022.3.62f3` / FileVersion `2022.3.62.9860879` | 版本相同 |
| CLR | `4.0.30319.42000` | 相同 |
| BepInEx | `5.4.22.0` | 低于客户端 `5.4.23.5` |
| `Assembly-CSharp.dll` | 4,509,920 bytes，SHA-256 `1AD761B15AD754259BEA8D3053B86105FAB16E09B0A122A101807C87B582060A` | 与客户端不同 |

其他可复查指纹：

- `E:\Steam\steamapps\common\U3DS\BepInEx\core\BepInEx.dll`：SHA-256 `2674D3AECF3097BEE817ABE7E8BBCC42BF583DF51402069D5FCD4FBED55017CE`
- `E:\Steam\steamapps\common\U3DS\Unturned.exe`：SHA-256 `D91E3C1D8B60016202034C830EF9095B86ADEA8697F7744EFDD20F8F789F52AC`

U3DS `BepInEx\LogOutput.log` 还已对目标 BepInEx `5.4.23.5` 的现有插件显示“targets a wrong version of BepInEx”警告。这证明版本差异已进入实际加载路径，不是纯理论风险。

官方来源：

- Smartly Dressed Games 《Server hosting》：https://docs.smartlydressedgames.com/en/stable/servers/server-hosting.html
- Unity `Application.isBatchMode`：https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-isBatchMode.html

## 二、架构推断（非运行证明）

1. **程序集边界**：应将公共契约、核心运行时、服务端/规则功能与客户端 UI 分开编译。即使最终构建合并为单 DLL，依赖方向也必须保证 U3DS 启动核心时不需解析客户端专用 UI 类型。
2. **UI 加载门禁**：UI/HUD 模块只应在非 batch/headless、相关 UI 类型可用、且客户端生命周期已就绪时激活。不能只靠某一加载早期的 `Provider.isServer/isClient` 值做唯一环境判断。
3. **兼容策略**：共享契约应尽量只依赖 `mscorlib/System` 的两端交集；Unity/Unturned 类型放在适配器边界后。对 `Assembly-CSharp.dll` 应使用客户端与 U3DS 两套引用集做编译/API 检查。
4. **验收分区**：单人、SteamP2PFriends Host/Client、U3DS 必须是独立门禁。SteamP2PFriends 实现的客户端 listen-host 路径不等于官方 U3DS。

## 三、待运行验证

- 新仓库尚无源码或 `.csproj`，因此尚未证明新框架能在客户端 BepInEx `5.4.23.5` 与 U3DS `5.4.22.0` 同时加载。
- 尚未对新产物执行客户端/U3DS 双引用集 API 兼容检查；不能从 `LangVersion=10` 推导每个所用 API 均可运行。
- 尚未验证 UI 软失败、服务端无 UI 启动、模块隔离、设置同步与功能降级路径。
- U3DS 日志中插件加载早期的某些 `isServer/isClient` 指纹不足以判定 hosted 后环境；需在服务器已完成托管后记录新指纹。
- 正式兼容结论必须绑定新框架 DLL 哈希、客户端/U3DS 引用集哈希与同一候选版本日志。

## 四、对后续规格的硬性输入

1. 首版生产工程基线建议固定为 `.NET Framework 4.7.2` + C# 10，并把这两项分别理解为“运行时/API 目标”与“编译器语法级别”。
2. CI/本地构建必须固定引用集来源与 SHA-256，不得无声地从某一当前 Steam 安装复制并宣称兼容另一端。
3. 增加至少两套静态兼容门禁：客户端 Managed/BepInEx 快照、U3DS Managed/BepInEx 快照。
4. 核心和 U3DS 路径不得对 UI 组件产生类型解析硬依赖；UI 初始化失败只禁用对应前端功能并提供可见诊断，不应拖垮核心。
5. 新框架的首次验收必须独立收集：单人、SteamP2PFriends Host/Client、U3DS；任一场景的 PASS 不能代替另一场景。

## 五、证据层级与限制

- **已证实**：上述版本、哈希、启动参数、日志加载事实和既有 `.csproj` 设置。
- **推断**：建议的模块分层、UI 门禁、双引用集检查与 BepInEx 版本策略。它们由已证实运行时差异推导，但尚未被新代码验证。
- **未验证**：新框架的任何实际加载、UI、同步或跨场景行为。
- 所有本机版本与哈希都是 **2026-08-24 快照**；Steam 更新、BepInEx 替换或 U3DS 更新后必须重新采集。
