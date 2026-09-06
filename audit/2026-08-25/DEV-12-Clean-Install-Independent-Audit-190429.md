# GPT-DEV-12 人工 clean-install 单 DLL 独立审计

- 审计对象：DEV-12「BUE 单 DLL 运行时程序集闭包」
- 基线：BUE-V1-RT01-20260824
- SourceSet：BUE-SS-20260824-02
- 审计范围：真实客户端 BepInEx 单 DLL启动冒烟
- 审计时间：2026-08-25
- 审计者：GPT（独立复核）

## 一、最终判定

**判定：PASS（人工 clean-install 单 DLL 启动冒烟通过）**

本审计确认：实际客户端插件目录中的 `BetterUnturnedExperience.dll` 与 DEV-12 已审计构建产物为同一 SHA-256；BepInEx 日志完成插件加载并输出 `BootstrapReady`，且未出现工单规定的三类程序集/类型异常。

本报告只证明单 DLL 客户端启动与程序集闭包冒烟。未因此宣称 No-op Fixture 联合注册、U3DS、单人功能、SteamP2PFriends Host/Client、Better Item Interaction 或三环境验收通过。

## 二、核验对象

### 实际客户端插件目录

`E:\Steam\steamapps\common\Unturned\BepInEx\plugins`

目录中的可加载 `.dll`：

```text
BetterUnturnedExperience.dll
```

目录中另有若干 `.dll.disabled` 文件；它们不属于 BepInEx 可加载 DLL，且不包含 `BetterUnturnedExperience.Core.dll` 或 `BetterUnturnedExperience.Contracts.dll`。因此本次 BUE 运行时部署的有效 BUE DLL 数量为 1。

### 诊断包

`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包_20260825_190429`

关键日志：

`LogOutput.log`

## 三、程序集哈希核验

| 文件 | SHA-256 | 判定 |
|---|---|---|
| 实际部署 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll` | `F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C` | PASS |
| DEV-12 staging `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\artifacts\DEV-12-clean-single-dll-20260825\BetterUnturnedExperience.dll` | `F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C` | PASS |

哈希完全一致，人工运行证据可绑定到 GPT-DEV-12 独立审计 R2 所覆盖的静态程序集闭包与 ABI 产物。

## 四、BepInEx 启动证据

来源：`LogOutput.log`

```text
[Info   :   BepInEx] 1 plugin to load
[Info   :   BepInEx] Loading [Better Unturned Experience 0.0.0]
[Info   :Better Unturned Experience] Better Unturned Experience featureId=io.github.yu80rice.betterunturnedexperience status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001
[Message:   BepInEx] Chainloader startup complete
```

判定：

- BepInEx 5.4.23.5 启动并发现 1 个插件：PASS。
- BUE 主插件完成加载并输出 `BootstrapReady`：PASS。
- Chainloader 完成启动：PASS。

## 五、异常关键词门禁

对 `LogOutput.log` 进行不区分大小写的关键词核对：

| 关键词 | 命中数 | 判定 |
|---|---:|---|
| `TypeLoadException` | 0 | PASS |
| `FileNotFoundException` | 0 | PASS |
| `MissingMethodException` | 0 | PASS |
| `Exception` | 0 | PASS |
| `[Error` | 0 | PASS |

`UMM-诊断摘要.txt` 同时记录“没有匹配到 Unity 崩溃、DXVK 初始化失败或 BepInEx 异常的典型模式”。`Client.log` 中存在一条与 BUE 无关的 Workshop 资源内容告警/异常记录；它不属于 BepInEx 插件程序集加载链，未改变本次 BUE 单 DLL 门禁判定。

## 六、验收矩阵

| DEV-12 条目 | 本次证据 | 判定 |
|---|---|---|
| 仅部署 BUE 主 DLL 可启动 | 实际插件目录与 BepInEx `1 plugin to load` / `BootstrapReady` | PASS |
| 主 DLL 不需要 Core/Contracts 运行时 DLL | 实际目录无两者；部署文件哈希绑定至 R2 已审计产物 | PASS |
| 无 `TypeLoadException` / `FileNotFoundException` / `MissingMethodException` | `LogOutput.log` 关键词计数均为 0 | PASS |
| BUE 进程内注册运行时行为 | 本次日志仅证明 BootstrapReady | 未在本次单 DLL冒烟中扩展证明 |
| BUE + No-op Fixture 公开 ABI 注册 | 本次未部署 No-op Fixture | 未验证 |
| U3DS / SP / P2P / Better Item Interaction | 不属于本次场景 | 未验证 |

## 七、审计结论与建议

DEV-12 的“人工 clean-install 单 DLL 客户端启动冒烟”门禁通过。应保留本报告、诊断包和实际部署哈希作为同一 SourceSet 的运行证据。

若要关闭 DEV-12 全部验收条件，还需单独部署并验证 `BetterUnturnedExperience.NoOpFixture.dll` 通过公开 BUE ABI 注册；该联合测试不得用本次 BUE-only 证据替代。

