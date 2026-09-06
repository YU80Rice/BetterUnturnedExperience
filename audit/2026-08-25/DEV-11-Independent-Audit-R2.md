# GPT-DEV-11 独立审计报告 R2

## 1. 审计范围

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-11-noop-external-feature-host-bridge.md`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 复审对象：R1 阻断 B-01 修订后的 `NoOpFeaturePlugin.Awake`、公开 `BueRuntimeHost`、独立 Fixture AssemblyRef、注册阶段与失败隔离。
- 审计性质：只读独立审计；本轮未修改生产代码、测试代码、LMN、U3DS 或原版内容。

## 2. 最终判定

**PASS（阻断项 0）**

DEV-11 满足本票静态、构建和测试验收条件，可推进为 `ready-for-human` 并交 Gemini 复核。该结论不包含真实 BepInEx clean-install、U3DS/SP/P2P 或发布资格证明。

## 3. R1 阻断关闭情况

| 阻断 | R2 结果 | 证据 |
|---|---|---|
| B-01：Fixture 入口缺少 `Awake`，测试绕过实际生命周期 | **CLOSED** | `src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs:13-18` 现在定义 `private void Awake()`，调用 `RegisterWithBue()`；后者在第 11 行调用 `NoOpFeatureRegistration.Register()`，最终在第 29 行调用 `BueRuntimeHost.Register(...)`。 |

`Awake` 只负责通过公开注册 seam 登记并输出结构化结果日志；未直接调用 `IFeatureModule.Start`，未引入扫描或反射发现。私有 Unity/BepInEx message method 可由运行时消息机制调用，静态源码链闭合。

## 4. 构建与测试证据

### 4.1 Release rebuild

命令：

```text
dotnet build D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\BetterUnturnedExperience.sln -c Release --no-restore
```

结果：**PASS，exit 0，0 errors，0 warnings**。本次构建重新产出 BUE Plugin、No-op Fixture 及其余 solution 项目。

### 4.2 全套测试

7/7 测试可执行文件 exit 0：

| 测试 | 输出 |
|---|---|
| `BetterUnturnedExperience.ClientUi.Tests.exe` | `DEV-05 ClientUi tests: PASS` |
| `BetterUnturnedExperience.Contracts.Tests.exe` | `DEV-10 registration runtime tests: PASS` |
| `BetterUnturnedExperience.Network.Tests.exe` | `DEV-06 network tests: PASS` |
| `BetterUnturnedExperience.Placement.Tests.exe` | `DEV-04 placement evaluator tests: PASS` |
| `BetterUnturnedExperience.Plugin.Tests.exe` | `DEV-11 external fixture tests: PASS` |
| `BetterUnturnedExperience.Release.Tests.exe` | `DEV-08 runtime evidence package tests: PASS` |
| `BetterUnturnedExperience.Settings.Tests.exe` | `DEV-03 settings runtime tests: PASS` |

现有 Plugin.Tests 同时验证 Host 缺失返回 `HostUnavailable`、Host 初始化后 Fixture 通过公开桥注册、Catalog 冻结后晚注册返回 `PhaseClosed`。Core/Contracts.Tests 继续验证非法 registration 不污染 Catalog 及 getter 异常 fail-closed。

## 5. 静态隔离、发现机制与依赖闭包

| 检查项 | 判定 | 证据 |
|---|---|---|
| Contracts UI/native token gate | PASS | `eng/Verify-NoUiTokens.ps1`：2 个 C# 文件，PASS。 |
| Core UI/native token gate | PASS | `eng/Verify-NoUiTokens.ps1`：10 个 C# 文件，PASS。 |
| Release semantic isolation | PASS | `using`、ProjectReference、外部类型模式未发现 Unity、SDG.Unturned、Glazier、Sleek、BepInEx、Harmony、Steamworks；未发现 `Assembly.GetTypes`、`GetAssemblies`、目录枚举或 `PatchAll`。通用脚本对 `BepInExVersion` 证据字段名的历史误报不构成类型泄漏。 |
| DEV-11 discovery scan | PASS | Plugin、NoOpFixture、Core Registration 源码未发现 `Assembly.GetTypes`、`GetAssemblies`、`Directory.GetFiles`/`EnumerateFiles`、`LoadFrom`、`PatchAll`、Harmony discovery。 |
| Fixture private Core reference | PASS | `NoOpFeaturePlugin.cs` 仅引用 BepInEx、Contracts、Plugin；NoOpFixture csproj 无 `BetterUnturnedExperience.Core` ProjectReference。 |
| Fixture direct AssemblyRef | PASS | 当前 DLL 直接引用：`BepInEx 5.4.23.5`、`BetterUnturnedExperience 0.0.0.0`、`BetterUnturnedExperience.Contracts 0.0.0.0`、`mscorlib`；无直接 `BetterUnturnedExperience.Core` AssemblyRef。Contracts 是公开注册契约的共享 ABI 组成，不属于私有 Core。 |
| BUE Host public bridge | PASS | `BueRuntimeHost` 对外仅暴露 `Phase` 与 `Register(IFeatureRegistration)`；Bind/Clear 保持 internal；Host 缺失稳定返回 `HostUnavailable`。 |
| 阶段与失败隔离 | PASS | `FeatureRegistrationRuntime.cs:62-98` 在解析 registration 前后复核阶段，使用快照和锁内重复检查；无效 artifact、factory、contract、satellite 或 getter 异常均拒绝且不写入 Catalog。 |
| 未直接启动模块 | PASS | No-op Module 仅实现接口；Fixture 入口没有 `IFeatureModule.Start` 调用。 |

## 6. 当前 Release 产物 SHA-256

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `4B0AEC997496994CEFBF15441063E1CD3CBF481B5116C4CAEF035D3CB9A5077E2` |
| `src/BetterUnturnedExperience.NoOpFixture/bin/Release/BetterUnturnedExperience.NoOpFixture.dll` | `4C5EB8E6A65DA58AB7A1669E328BDB3652CEA0037E72EE719A0D6FA92FEA4BA7` |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `1FCB65E8620D39DF3B7F6A8DF65C642F15128348B2A5A87E55D82544339DE8CD` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `630B86C7B4713994677D431C14F52D8C9678949B91725CE7B81259273B9ABECF` |

## 7. 证据边界

R2 证明了源码中的 BepInEx `Awake → RegisterWithBue → BueRuntimeHost.Register` 调用链、Release 构建、现有测试和静态依赖/发现门禁。R2 **不证明**：

- 真实 BepInEx clean-install/Chainloader 装载日志；
- U3DS、单人、SteamP2PFriends Host/Client 的真实运行；
- ClientUi satellite、LoadSetIdentity、Better Item Interaction 或发布授权。

## 8. 后续交接

建议将本报告交 Gemini 做前端消费复核，并将 DEV-11 工单推进为 `ready-for-human`。真实 clean-install 与三环境证据继续保留为后续独立门禁。
