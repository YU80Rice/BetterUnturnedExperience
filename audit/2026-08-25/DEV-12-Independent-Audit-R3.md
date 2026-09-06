# GPT-DEV-12 独立运行证据审计 R3

- 审计对象：DEV-12「BUE 单 DLL 运行时程序集闭包」
- 基线：BUE-V1-RT01-20260824
- SourceSet：BUE-SS-20260824-02
- 审计日期：2026-08-25
- 审计类型：人工 clean-install 单 DLL 客户端冒烟最终复核

## 一、最终判定

**判定：PASS（DEV-12 运行证据门禁通过；允许将 DEV-12 标记为 `resolved`）**

本轮人工部署证据证明：在真实 Unturned 客户端 BepInEx 5.4.23.5 环境中，插件目录实际只包含一个活动 `.dll`——`BetterUnturnedExperience.dll`；Core/Contracts DLL 缺失；部署文件哈希与 DEV-12 Release 产物完全一致；BepInEx 成功加载 BUE 并输出 `BootstrapReady`；诊断摘要退出码为 0，且整个诊断包未发现 `TypeLoadException`、`FileNotFoundException` 或 `MissingMethodException`。

## 二、部署目录核验

实际目录：

```text
E:\Steam\steamapps\common\Unturned\BepInEx\plugins
```

活动 DLL 枚举结果：

```text
BetterUnturnedExperience.dll
```

目录中另外存在若干 `.disabled` 文件，但它们不是 BepInEx 活动 DLL，不构成 BUE 运行时依赖。明确确认：

```text
BetterUnturnedExperience.Core.dll      absent
BetterUnturnedExperience.Contracts.dll absent
```

## 三、部署哈希核验

| 文件 | SHA-256 | 判定 |
|---|---|---|
| `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll` | `F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C` | 与 DEV-12 Release 产物一致 |

## 四、真实客户端日志证据

诊断包：

```text
D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包_20260825_190429
```

`LogOutput.log` 关键行：

```text
[Info   :   BepInEx] 1 plugin to load
[Info   :   BepInEx] Loading [Better Unturned Experience 0.0.0]
[Info   :Better Unturned Experience] Better Unturned Experience featureId=io.github.yu80rice.betterunturnedexperience status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001
[Message:   BepInEx] Chainloader startup complete
```

关键日志行位于 `LogOutput.log:13-16`。

诊断摘要 `UMM-诊断摘要.txt`：

```text
结论：未发现明确的致命日志特征
分类：Normal
最近一次受管会话：2026-08-25 19:04:28 · 模组环境，DXVK 已启用 · 退出码 0
```

摘要位于 `UMM-诊断摘要.txt:2-6`。对诊断包所有日志执行异常模式搜索，未发现：

```text
TypeLoadException
FileNotFoundException
MissingMethodException
```

## 五、DEV-12 验收对账

| 验收项 | 判定 | 依据 |
|---|---|---|
| 单 DLL 主产物不依赖 Core/Contracts | PASS | 实际目录无两 DLL；部署主 DLL SHA-256 与构建产物一致；R2 AssemblyRef 扫描已通过 |
| 公开 ABI 类型身份唯一 | PASS | R2 `AssertExternalSdkAssemblyIdentity()` 与 Gemini DEV-12 ACCEPT 均通过 |
| Headless/Contracts/Core 隔离 | PASS（静态） | R2 静态扫描通过；本轮客户端为 Client 决策，不扩展为 U3DS 证据 |
| Release 0 errors/0 warnings | PASS | R2 已复测通过 |
| 7 个现有测试及新增 SDK ABI 回归 | PASS | R2 已复测全 PASS |
| 客户端 clean-install 单 DLL BootstrapReady | PASS | 本轮真实目录、哈希和 BepInEx 日志证据 |
| No-op Fixture 单 DLL 公开注册链 | 未在本轮重测 | 不阻断本轮 BUE 单 DLL 主插件冒烟；应在后续 Fixture 专项冒烟中单独记录 |

## 六、证据边界

本报告仅允许关闭 DEV-12 的“BUE 主 DLL 单文件部署闭包 + 真实客户端 Bootstrap 冒烟”范围。它不证明：

- U3DS Headless 实际加载；
- SteamP2PFriends Host/Client 或 P2P 联机；
- 单人完整玩法；
- Better Item Interaction 功能生效；
- ClientUi 卫星真实装配；
- 三环境资格或发布授权。

## 七、最终裁定

GPT 独立运行证据审计 R3：**PASS**。

DEV-12 可以由总维护者将工单状态更新为 **`resolved`**。关闭后，下一阶段应另立运行/功能工单；不得将 DEV-12 的单 DLL Bootstrap 证据外推为 BUE 全功能或三环境验收通过。
