# GPT-DEV-11 独立审计报告 R1

## 1. 审计范围

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-11-noop-external-feature-host-bridge.md`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 审计对象：`BueRuntimeHost`、BUE Plugin bootstrap、独立 No-op BepInEx Fixture、FeatureRegistrationRuntime、Plugin.Tests、相关工程引用与静态门禁。
- 审计性质：只读独立审计。本轮未修改生产代码、测试代码、LMN、U3DS 或原版内容。

## 2. 最终判定

**FAIL（1 项阻断）**

当前不得将 DEV-11 标记为 `ready-for-human` 或 `resolved`，也不得宣称独立第三方 DLL 已完成真实 BepInEx 装载验证。阻断项修复后，必须以同一源码快照重新执行 Release rebuild、全套测试、静态隔离/引用扫描和独立审计。

## 3. 阻断项

### B-01：No-op Fixture 没有在实际 BepInEx 入口 `Awake` 中注册

- 证据：`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs:7-12`
- 工单要求：独立 Fixture 作为 BepInEx plugin entry，并在 `Awake` 中仅调用 `BueRuntimeHost.Register`。
- 当前事实：`NoOpFeaturePlugin` 继承 `BaseUnityPlugin` 并声明了 `BepInPlugin`/`BepInDependency`，但类内没有 `Awake`。注册逻辑位于独立静态方法 `NoOpFeaturePlugin.RegisterWithBue()`（第 11 行）及 `NoOpFeatureRegistration.Register()`（第 16-19 行）。
- 测试缺口：`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:20-29` 直接调用 `NoOpFeatureRegistration.Register()`，绕过了真实 BepInEx 生命周期，因此只能证明手动调用桥成功，不能证明独立插件入口装载后自动注册。
- 影响：在真实 Chainloader 场景中，Fixture 被发现/实例化后不会登记到 BUE Catalog；DEV-11 的核心 tracer-bullet 目标未成立。
- 修复建议：在 `NoOpFeaturePlugin` 内增加受控 `Awake`，其唯一业务动作应是调用 `NoOpFeatureRegistration.Register()`（必要时仅记录结构化结果）；增加公开生命周期测试 seam 或受控测试夹具，证明入口调用确实触发注册，且不得恢复目录扫描、反射发现或直接调用 `IFeatureModule.Start`。

## 4. 已通过检查

| 检查项 | 判定 | 证据 |
|---|---|---|
| Release solution rebuild | PASS | `dotnet build BetterUnturnedExperience.sln -c Release --no-restore`，exit 0，0 errors，0 warnings；输出覆盖 13 个项目。 |
| 全套测试 | PASS | 7 个 `*.Tests.exe` 全部 exit 0：ClientUi、Contracts/DEV-10、Network、Placement、Plugin/DEV-11、Release/DEV-08、Settings。 |
| Host 缺失失败闭合 | PASS（手动桥路径） | `BueRuntimeHost.Register` 在 `current == null` 时返回 `HostUnavailable`；Plugin.Tests 第 19-21 行验证。 |
| Host 初始化与注册桥 | PASS（手动桥路径） | `BueRuntimeHost.Bind` + `OpenRegistration` 后，Fixture 静态注册返回 Accepted；Plugin.Tests 第 22-27 行验证。 |
| Catalog 冻结后晚注册 | PASS（手动桥路径） | `FeatureRegistrationRuntime.Register` 在非 `RegistrationOpen` 返回 `PhaseClosed`；Plugin.Tests 第 27-29 行验证。 |
| 注册失败不污染 Catalog | PASS（Core 既有测试） | Contracts.Tests 第 105-106、119-120 行覆盖非法 artifact 与 getter 异常；失败后冻结 Catalog 保持为空。 |
| 阶段/并发二次检查 | PASS（静态） | `FeatureRegistrationRuntime.cs:62-98` 在解析外部 registration 前后均复核阶段并在同一锁内检查重复 FeatureId；快照化后再写入字典。 |
| 显式注册/无目录扫描 | PASS（静态） | DEV-11 相关源码未发现 `Assembly.GetTypes`、`GetAssemblies`、目录枚举、`LoadFrom`、`PatchAll` 或 Harmony discovery。`obj` 生成的框架属性文件中的 `System.Reflection` 不属于生产源逻辑。 |
| Fixture 直接引用闭包 | PASS（IL/metadata 静态） | `BetterUnturnedExperience.NoOpFixture.dll` 直接 AssemblyRef：`BepInEx 5.4.23.5`、`BetterUnturnedExperience 0.0.0.0`、`BetterUnturnedExperience.Contracts 0.0.0.0`、`mscorlib`；未直接引用 `BetterUnturnedExperience.Core`。 |
| Fixture 私有 Core namespace | PASS（源码/引用静态） | `NoOpFeaturePlugin.cs` 仅使用 `BepInEx`、`BetterUnturnedExperience.Contracts`、`BetterUnturnedExperience.Plugin`；工程无 `BetterUnturnedExperience.Core` ProjectReference。 |
| Contracts/Core 类型隔离 | PASS | `eng/Verify-NoUiTokens.ps1`：Contracts 2 个 C# 文件 PASS；Core 10 个 C# 文件 PASS。 |
| Release 类型隔离 | PASS（语义扫描） | 通用 token 脚本将证据字段 `BepInExVersion` 误报为 token；进一步检查 `using`、ProjectReference、外部类型模式及 forbidden discovery 均未发现 BepInEx 类型引用。该字段名不构成类型泄漏。 |
| 失败隔离与快照 | PASS（静态/Core 测试） | `FeatureRegistrationRuntime` 仅在完整验证后写入 registration dictionary，并复制为 immutable-ish snapshot；异常返回结构化拒绝原因，不污染既有 Catalog。 |

## 5. 产物哈希（本次 Release rebuild 后）

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `4B0AEC997496994CEFBF15441063E1CD3CBF481B5116C4CAEF035D3CB9A5077E2` |
| `src/BetterUnturnedExperience.NoOpFixture/bin/Release/BetterUnturnedExperience.NoOpFixture.dll` | `2F44776CD80003BFB6DA0E2A6647BF76AFE339B0A6E8E19EECA3DDC90E35E09F` |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `1FCB65E8620D39DF3B7F6A8DF65C642F15128348B2A5A87E55D82544339DE8CD` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `630B86C7B4713994677D431C14F52D8C9678949B91725CE7B81259273B9ABECF` |

## 6. 证据边界

本报告只证明当前源码的构建、测试、静态依赖/类型门禁，以及手动调用公开注册桥的行为。它不证明：

- 真实 BepInEx Chainloader clean-install 装载与 `Awake` 顺序；
- U3DS、单人、SteamP2PFriends Host/Client 运行；
- ClientUi satellite、LoadSetIdentity、Better Item Interaction 或任何发布资格。

## 7. 复审前置条件

修复 B-01 后，重新构建并加入真实入口路径测试；然后再次执行：

1. `dotnet build BetterUnturnedExperience.sln -c Release --no-restore`；
2. 7 个测试可执行文件；
3. Contracts/Core/Release 语义隔离扫描与 Fixture 直接 AssemblyRef/IL 扫描；
4. 独立 DEV-11 R2 审计。

只有 R2 无阻断后，才可交 Gemini 复核并将工单推进至 `ready-for-human`。
