# GPT-DEV-13 独立审计报告 R1

## 一、审计结论

- **判定：PASS**
- **阻断项：0**
- **工单建议状态：`ready-for-human`**
- **审计范围：** DEV-13「独立 No-op Feature 注册运行时与 Catalog Barrier 冒烟」的源码、测试、Release 构建、staging 产物及静态边界。
- **证据边界：** 本审计不把静态/单元/staging 证据升级为真实 BepInEx 双 DLL 客户端运行证据；不宣称 U3DS、SP、SteamP2PFriends P2P、ClientUi Satellite 或 Better Item Interaction 已验收。

## 二、审计输入

工单：

`D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/DEV-13-noop-feature-runtime-registration-smoke.md`

基线：`BUE-V1-RT01-20260824`

SourceSet：`BUE-SS-20260824-02`

重点变更文件：

- `src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs`
- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`
- `src/BetterUnturnedExperience.Plugin/BueRuntimeHost.cs`
- `src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`
- `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`

staging 目录：

`D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/artifacts/DEV-13-noop-registration-20260825/`

## 三、逐项审计

### 1. 需求符合性：PASS

| 要求 | 核验事实 |
| :--- | :--- |
| Host 启动并开放注册 | `BetterUnturnedExperiencePlugin.Awake` 创建 runtime、`BueRuntimeHost.Bind`，随后调用 `OpenRegistration`（`BetterUnturnedExperiencePlugin.cs:13-22`）。 |
| Awake 完成后的 host-owned barrier | `Start` 通过内部 `CurrentRuntime` 调用 `CompleteRuntime`；barrier 在 `FeatureRegistrationRuntime.cs:117-125` 原子构建 Catalog 并进入 `RuntimeReady`。Unity `Start` 晚于所有插件 `Awake` 的入口时序满足工单目标。 |
| No-op 只走公开注册桥 | No-op 的 `Awake` 仅调用 `BueRuntimeHost.Register`（`NoOpFeaturePlugin.cs:13-16, 25-30`），没有扫描、反射发现或私有 Core 调用。硬依赖 BUE Host 的 BepInEx 标记位于 `NoOpFeaturePlugin.cs:7-8`。 |
| Host 不可用/晚注册 fail-closed | 测试在 `BueRuntimeHost.Clear` 后验证 `HostUnavailable`，barrier 后验证 `PhaseClosed`（`Plugin.Tests/Program.cs:21-32`）。 |
| Catalog 确定性及拒绝规则 | runtime 按 FeatureId、DefinitionSetDigest、ArtifactPayloadDigest 的 Ordinal 规范顺序生成 Catalog；重复、非法 artifact、兼容性、工厂及 satellite 错误均结构化拒绝（`FeatureRegistrationRuntime.cs:62-98, 143-198`）。既有 Contracts 测试继续覆盖乱序/revision/非法输入。 |

### 2. 线程安全与时序：PASS

- `registrations`、`Phase` 与 `Catalog` 的状态迁移均由同一 `sync` 锁保护。
- `Register` 将外部 registration getter/校验置于锁外执行，避免外部实现重入时持锁；完成快照后重新加锁复核阶段，若 barrier 已关闭则拒绝，不会把迟到注册写入 Catalog。
- `CompleteRuntime` 在同一锁内执行“构建 Catalog + 进入 RuntimeReady”，不会暴露半完成状态；重复调用返回 `false`，不会重算或漂移 Catalog。
- `FeatureDefinitionArtifact` 构造时复制 payload，satellite 元数据在 `ClientUiSatelliteSnapshot` 中复制，降低外部可变对象造成的注册事实漂移风险。

### 3. Fail-closed 与错误语义：PASS

- `HostStarting`、`CoreSafeMode`、非 `RegistrationOpen` 阶段均拒绝注册。
- 空/非法定义、空 factory、不兼容 contract、非法 satellite 都不进入 `registrations`。
- registration getter 抛异常时安全返回拒绝结果，不向调用方泄漏异常，也不污染既有登记。
- plugin barrier 失败记录 `BootstrapFailed/BUE-BOOTSTRAP-002`，只有成功完成 barrier 才输出 `RuntimeReady/BUE-BOOTSTRAP-003`（`BetterUnturnedExperiencePlugin.cs:30-39`）。

### 4. AssemblyRef、公开 ABI 与 Headless 隔离：PASS

- 本次 Release 重建成功；`BetterUnturnedExperience.Plugin`、No-op fixture 及其测试均通过。
- DEV-13 测试确认 `FeatureId`/`IFeatureRegistration` 与 `BueRuntimeHost` 的运行时类型身份一致，并确认主程序集不引用 `BetterUnturnedExperience.Core` 或 `BetterUnturnedExperience.Contracts`；fixture 不引用这两个运行时程序集且引用主 BUE 程序集。
- Core/Contracts 源码静态禁用 token 扫描无命中：未发现 `UnityEngine`、Glazier、Sleek、LMN、`Assembly.GetTypes`、`PatchAll`、动态程序集加载等越界内容。
- No-op 无 UI satellite；本票未引入 ClientUi 或原生 UI 类型，符合 U3DS 排除边界。

### 5. 范围控制：PASS

未发现对 LMN、BepInEx、U3DS 或 Unturned 原版内容的修改；未实现 Better Item Interaction、真实第三方玩法、动态 DLL 扫描、ClientUi satellite 或三环境验收。No-op module factory 仅作为注册输入，未越界启动真实功能。

## 四、独立复测记录

### Release 构建

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /m /v:minimal
```

结果：**PASS，0 errors，0 warnings**。

### 七个测试项目

结果：**7/7 PASS**。

- DEV-05 ClientUi tests: PASS
- DEV-10 registration runtime tests: PASS
- DEV-06 network tests: PASS
- DEV-04 placement evaluator tests: PASS
- DEV-13 external registration barrier tests: PASS
- DEV-08 runtime evidence package tests: PASS
- DEV-03 settings runtime tests: PASS

### staging 哈希

| 产物 | SHA-256 | 结论 |
| :--- | :--- | :--- |
| `BetterUnturnedExperience.dll` | `2CC63E1142FBAAFE8F13019757E8A39F6354A41A98C9F75827FA036D4EDDEC21` | PASS，与提交证据一致 |
| `BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` | PASS，与提交证据一致 |

## 五、非阻断建议

1. 进行人工 clean-install 双 DLL BepInEx 客户端冒烟，采集包含 `BootstrapReady`、No-op `accepted=true`、`RuntimeReady` 的完整日志，并核对部署后两枚 DLL 的 SHA-256。
2. 人工验证无 BUE Host、晚注册、重复注册及 BUE 启动失败时的日志可读性；这些属于运行证据，不影响本轮静态/单元审计结论。
3. 后续若允许外部模块直接观察 Catalog，应继续保持 `BueRuntimeHost` 为唯一公开入口，避免把 `FeatureRegistrationRuntime` 的内部实现类型扩展成第三方依赖。

## 六、最终裁定

DEV-13 的实现、测试、程序集闭包与 Headless 静态边界均通过独立审计，**PASS**。当前仅具备进入人工真实客户端双 DLL 冒烟的资格；在该运行验证完成前，工单不得标记 `resolved`，也不得宣称三环境或完整功能验收通过。
