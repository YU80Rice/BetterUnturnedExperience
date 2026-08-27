# GPT-DEV-13 独立审计报告 R2

## 一、审计结论

- **静态/构建/单元审计：PASS**
- **阻断项：0（代码与测试层）**
- **真实客户端运行门禁：未通过/待重新采集**
- **工单建议状态：`ready-for-human`**
- **审计对象：** DEV-13 R1 修复：BUE Host 的 `Start()` 首选 barrier 与 `Update()` 一次性 fallback。
- **审计边界：** 本报告不把人工诊断包中缺失 `RuntimeReady` 的事实升级为通过；不宣称 U3DS、单人、SteamP2PFriends Host/Client、ClientUi Satellite、Better Item Interaction 或三环境资格通过。

## 二、审计输入与证据

| 项目 | 证据 |
| :--- | :--- |
| 工单 | `.scratch/better-unturned-experience-architecture/issues/DEV-13-noop-feature-runtime-registration-smoke.md` |
| 基线 | `BUE-V1-RT01-20260824` |
| SourceSet | `BUE-SS-20260824-02` |
| R1 修复源文件 | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs` |
| Host 运行时 | `src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs`、`src/BetterUnturnedExperience.Plugin/BueRuntimeHost.cs` |
| 真实人工诊断包 | `D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/启动器/UnturnedModManager/publish/UMM-v2.2.0-win-x64/UMM-诊断包_20260825_193703` |
| R1 复核报告 | `.scratch/better-unturned-experience-architecture/handoffs/DEV-13-Registration-Barrier-Review.md` |

## 三、R1 修复核验

### 1. Host-owned 生命周期与一次性 fallback：PASS

`BetterUnturnedExperiencePlugin.Awake()` 创建并绑定唯一 `FeatureRegistrationRuntime`，调用 `OpenRegistration()` 后仅输出 `BootstrapReady`。`Start()` 与 `Update()` 都只调用内部 `TryCompleteRuntime()`；外部 Fixture 没有推进阶段的公开方法。

`TryCompleteRuntime()` 具备以下守卫：

1. `runtimeReadyLogged` 为真时立即返回；
2. 没有当前 Host runtime 或阶段不是 `RegistrationOpen` 时返回；
3. 只调用 Host-owned `CompleteRuntime()`；
4. 只有 barrier 返回成功才记录一次 `RuntimeReady`。

因此 `Start()` 与 `Update()` 的重复调用不会重复构建或重复发布成功日志，fallback 没有扩大外部插件权限。Unity 主线程生命周期下，`runtimeReadyLogged` 不构成跨线程共享协议；核心状态本身仍由 `FeatureRegistrationRuntime.sync` 保护。

### 2. 原子状态迁移与迟到注册：PASS

`FeatureRegistrationRuntime.CompleteRuntime()` 在同一 `lock (sync)` 内检查 `RegistrationOpen`、构建确定性 Catalog，并迁移到 `RuntimeReady`。重复调用返回 `false`。`Register()` 在读取外部 registration getter 时不持有锁，完成快照后重新检查阶段；若期间 barrier 已关闭，注册被拒绝为 `PhaseClosed`，不会进入 Catalog。

`BuildCatalogLocked()` 采用 FeatureId、DefinitionSetDigest、ArtifactPayloadDigest 的 `Ordinal` 顺序生成只读 Catalog，并计算确定性 revision。外部 registration 的卫星元数据在快照中复制，避免注册后外部对象变更污染 Catalog。

### 3. Fail-closed 与 Headless 边界：PASS

- Host 未绑定：`HostUnavailable`；
- `HostStarting`、`CatalogFrozen`、`RuntimeReady`、`CoreSafeMode` 阶段：拒绝晚注册；
- 空/非法 Definition、空 ModuleFactory、不兼容 Contract、非法 UI Satellite：结构化拒绝；
- registration getter 抛异常：转换为拒绝结果，不向外传播、不写入登记表；
- Core/Contracts/Registration 逻辑未引用 Unity、Glazier、Sleek、LMN，也未使用 `Assembly.GetTypes`、`PatchAll` 或动态程序集加载；
- 主 DLL 的 Unity/BepInEx 引用仅位于 Plugin 入口层，符合 Headless 入口分流设计；No-op 无 Client UI Satellite，未引入表现层类型。

## 四、独立构建与测试复测

命令：

```text
dotnet msbuild D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /m /v:minimal
```

结果：**PASS，0 errors / 0 warnings**。

逐项执行 7 个 Release 测试项目，全部退出码 `0`：

| 测试项目 | 结果 |
| :--- | :---: |
| `BetterUnturnedExperience.Contracts.Tests` | PASS |
| `BetterUnturnedExperience.Settings.Tests` | PASS |
| `BetterUnturnedExperience.Placement.Tests` | PASS |
| `BetterUnturnedExperience.ClientUi.Tests` | PASS |
| `BetterUnturnedExperience.Network.Tests` | PASS |
| `BetterUnturnedExperience.Plugin.Tests` | PASS |
| `BetterUnturnedExperience.Release.Tests` | PASS |

## 五、程序集身份与哈希复测

| 产物 | SHA-256 | 结论 |
| :--- | :--- | :---: |
| `artifacts/DEV-13-noop-registration-20260825/BetterUnturnedExperience.dll` | `A76202EB3087549695C623C4B008F70E35E424252B79D6CE4C24919290065A64` | PASS |
| `artifacts/DEV-13-noop-registration-20260825/BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` | PASS |

独立读取 PE AssemblyRef 的结果：

- 主 `BetterUnturnedExperience.dll` 引用 `mscorlib`、`System`、`System.Core`、`BepInEx`、`UnityEngine.CoreModule`，不引用 `BetterUnturnedExperience.Core` 或 `BetterUnturnedExperience.Contracts`；
- `BetterUnturnedExperience.NoOpFixture.dll` 引用主 `BetterUnturnedExperience`，不引用 Core/Contracts；
- 公开 `FeatureId`/`IFeatureRegistration` 的运行时类型身份仍归属于主 BUE 程序集。

## 六、真实运行证据复核与发布门禁

人工提供的诊断包：

`D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/启动器/UnturnedModManager/publish/UMM-v2.2.0-win-x64/UMM-诊断包_20260825_193703`

`LogOutput.log` 已确认：

```text
status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001
accepted=True reason=None diagnosticId=BUE-REG-ACCEPT
```

但该包**没有**出现：

```text
status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003
```

所以 R1 诊断包只能证明 BUE 与 No-op 被加载、No-op 在 `RegistrationOpen` 阶段注册成功；不能证明 R1 修订后的 `Start()` 或 `Update()` fallback 在真实客户端执行了 barrier。该缺口是人工运行证据缺失，不是本轮静态代码阻断；在新包出现 `BUE-BOOTSTRAP-003` 并核对两枚 DLL 哈希前，DEV-13 不得标记 `resolved`。

## 七、后续人工验证要求

1. 备份并清理旧 BUE/No-op DLL 后，仅部署本轮两枚已核对哈希的 staging 产物；
2. 启动同一客户端，采集完整 `BepInEx/LogOutput.log`；
3. 必须同时看到 `BootstrapReady`、No-op `accepted=True` 和 `RuntimeReady/BUE-BOOTSTRAP-003`；
4. 复核实际部署 DLL SHA-256 与本报告第五节一致；
5. 未满足前，工单保持 `ready-for-human`。通过后仍只代表 DEV-13 客户端双 DLL barrier 冒烟，不代表三环境或完整功能验收。

## 八、最终裁定

**GPT 独立审计 R2：静态/构建/测试 PASS，阻断项 0；真实 RuntimeReady 运行门禁 PENDING。**

R1 的 `Start()` + 一次性 `Update()` fallback 修复未引入外部越权、线程安全、ABI 或 Headless 隔离回归。DEV-13 可以交付人工重新冒烟验证，但在 `BUE-BOOTSTRAP-003` 真实日志出现前不得关闭工单或宣称运行通过。

