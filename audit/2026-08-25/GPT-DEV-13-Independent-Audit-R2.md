# GPT-DEV-13 独立审计报告 R2

## 一、审计结论

- **判定：PASS（静态修复审计）**
- **阻断项：0（针对本轮 R1 修复内容）**
- **运行状态：未闭环**
- **工单建议状态：`ready-for-human`**

本轮只审计 R1 修复：BUE Plugin 增加 Host-owned、一次性 `Update()` fallback，并重新核验 Release 构建、7 项测试、程序集哈希、无外部推进 barrier 及 Headless/范围边界。由于人工 R1 真实日志缺少 `RuntimeReady`，本报告不宣称真实客户端运行通过。

## 二、审计输入与变更

工单：

`D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/DEV-13-noop-feature-runtime-registration-smoke.md`

修复文件：

`src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`

修复内容：

- `Start()` 与 `Update()` 均只调用同一个私有 `TryCompleteRuntime()`。
- `runtimeReadyLogged` 成功后置位，保证日志和推进逻辑一次性完成。
- 只有 `BueRuntimeHost.CurrentRuntime` 且当前 phase 为 `RegistrationOpen` 时，BUE Host 才调用 `CompleteRuntime()`。
- No-op/外部功能没有新增 `CompleteRuntime`、`MarkRuntimeReady`、`FreezeCatalog` 或 `CurrentRuntime` 调用。

## 三、逐项审计

### 1. 修复符合性：PASS

`BetterUnturnedExperiencePlugin.cs:32-54` 保持 `Start()` 的原有 Host-owned barrier，同时增加下一帧 `Update()` fallback。fallback 不创建第二套状态机、不直接修改 `Phase`、不绕过 `CompleteRuntime()`，因此能覆盖宿主未及时派发可见 `Start()` 的情况，同时保持原 barrier 语义。

### 2. 一次性与幂等：PASS

`runtimeReadyLogged` 在成功完成 `CompleteRuntime()` 后才置为 `true`；重复 `Start`/`Update` 调用会立即返回。若 runtime 尚未绑定、阶段不是 `RegistrationOpen` 或 barrier 失败，则不记录成功、不伪造 `RuntimeReady`。Core 自身 `CompleteRuntime()` 仍在 `sync` 锁内原子执行 Catalog 构建与阶段迁移，重复调用返回 `false`。

### 3. 外部推进权限与时序：PASS

对 No-op Fixture 源码执行调用点核对，无 `CompleteRuntime`、`MarkRuntimeReady`、`FreezeCatalog` 或 `CurrentRuntime` 引用。No-op 仍仅通过 `BueRuntimeHost.Register(IFeatureRegistration)` 登记；phase 关闭由 BUE Host 内部推进。`Update()` 是 BUE Plugin 自身实例的方法，不向外部功能暴露推进接口。

### 3.1 编译后 IL/反编译核验：PASS

对修订后的 `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` 使用 `ilspycmd` 反编译 `BetterUnturnedExperience.Plugin.BetterUnturnedExperiencePlugin`，确认：

- `Start()` 仅调用 `TryCompleteRuntime()`；
- `Update()` 仅调用同一个 `TryCompleteRuntime()`；
- `TryCompleteRuntime()` 先检查 `runtimeReadyLogged`，再读取内部 `BueRuntimeHost.CurrentRuntime`；
- 仅当 runtime 非空、phase 为 `RegistrationOpen` 且 `CompleteRuntime()` 返回成功时，才写入 `runtimeReadyLogged = true` 并记录 `RuntimeReady`；
- IL 中未出现外部 No-op 类型、公共外部 barrier 调用或第二套 phase 推进路径。

### 4. Fail-closed：PASS

- `runtime == null` 或 phase 非 `RegistrationOpen`：fallback 不推进。
- `CompleteRuntime()` 返回 `false`：不写 `runtimeReadyLogged`，不输出成功日志。
- 成功后只输出一次 `RuntimeReady/BUE-BOOTSTRAP-003`。
- 外部 late registration 仍由 Core 返回 `PhaseClosed`，不污染 Catalog。

### 5. Headless 与越界边界：PASS

Core/Contracts 静态扫描未命中 `Glazier`、`Sleek`、`LMN`、`Assembly.GetTypes`、`PatchAll`、动态程序集加载等禁止项。R1 修复只位于 BUE Plugin 的 Host 生命周期入口，未向 Core/Contracts 引入 UI 或原生类型，也未修改 LMN、BepInEx、U3DS 或 Unturned 原版内容。

### 6. 程序集身份与哈希：PASS

重新构建后的产物哈希：

| 产物 | SHA-256 | 结论 |
| :--- | :--- | :--- |
| `BetterUnturnedExperience.dll` | `A76202EB3087549695C623C4B008F70E35E424252B79D6CE4C24919290065A64` | PASS |
| `BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` | PASS，未因 R1 修复改变 |

哈希与工单 R1 修复记录一致。

## 四、独立复测记录

### Release 构建

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /m /v:minimal
```

结果：**PASS，0 errors / 0 warnings**。

### 全部测试

结果：**7/7 PASS**。

- DEV-05 ClientUi tests: PASS
- DEV-10 registration runtime tests: PASS
- DEV-06 network tests: PASS
- DEV-04 placement evaluator tests: PASS
- DEV-13 external registration barrier tests: PASS
- DEV-08 runtime evidence package tests: PASS
- DEV-03 settings runtime tests: PASS

## 五、未闭环的人工运行门禁

人工双 DLL 冒烟 R1 的日志只有 BUE 与 No-op 被加载、No-op 注册成功，但缺少：

```text
status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003
```

因此本轮修复虽然通过源码、静态、构建、测试与哈希审计，仍必须重新部署 R1 修订后的 BUE DLL 与同哈希 No-op DLL，采集新的完整 BepInEx 日志，确认：

1. BUE `BootstrapReady`；
2. No-op `accepted=True`；
3. BUE `RuntimeReady/BUE-BOOTSTRAP-003`；
4. 无 `TypeLoadException`、`FileNotFoundException`、`MissingMethodException`；
5. 实际部署 DLL 哈希分别匹配上表。

## 六、最终裁定

DEV-13 R1 修复的 Host-owned 一次性 `Update()` fallback 设计与实现通过独立审计，**PASS**。真实客户端 barrier 运行证据仍缺失，故 DEV-13 不得标记 `resolved`，也不得宣称 U3DS、SP/P2P、ClientUi Satellite、Better Item Interaction 或三环境验收通过。
