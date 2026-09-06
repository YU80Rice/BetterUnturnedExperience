# DEV-03 独立审计报告（Round 2）

**作者：GPT（独立审计子任务）**  
**日期：2026-08-24**  
**审计对象：** `DEV-03 SettingsRuntime + 原子持久化 + LocalLoopback`  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**前轮报告：** `DEV-03-Independent-Audit-Round1.md`（保留，不覆盖）

## 一、最终裁定

**判定：FAIL（3 个阻断项）**

Round 1 的主要修复均能在源码中找到，独立 Release、两套测试和 Token Scan 均通过；但仍有一个连接代际安全回归、一个描述器/策略语义缺口，以及 TDD/持久化验收覆盖不足。因此 DEV-03 不能标记 `resolved`。

## 二、独立复跑证据

### 2.1 Release Rebuild

```text
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe \
  BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：**PASS**，5 个项目，`0 errors / 0 warnings`，退出码 `0`。

### 2.2 测试

```text
tests\BetterUnturnedExperience.Settings.Tests\bin\Release\BetterUnturnedExperience.Settings.Tests.exe
tests\BetterUnturnedExperience.Contracts.Tests\bin\Release\BetterUnturnedExperience.Contracts.Tests.exe
```

结果：均 **PASS**：`DEV-03 settings runtime tests: PASS`、`DEV-02 definition linker tests: PASS`。

### 2.3 Token Scan

```text
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Contracts
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Core
```

结果：**PASS**，分别扫描 2 与 4 个 C# 文件。

### 2.4 本轮产物 SHA-256

| 产物 | SHA-256 |
| --- | --- |
| `tests/BetterUnturnedExperience.Settings.Tests/bin/Release/BetterUnturnedExperience.Settings.Tests.exe` | `22D130EB0A441CB917D96C485A30F00FB1110F428552E2E1A0D7130B1E53D7A3` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `8B7DD9B96B37D6CB90760DE4D93F01204074144CC44505F070B557FEA77E923E` |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `31FA2A9A0840CE12E7D2C496A8B1FA13A3433FF340C2E0F6B9B7B200DAF531F6` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |

## 三、Round 1 修复核对

| Round 1 项目 | 状态 | 证据 |
| --- | --- | --- |
| 默认/范围/Step/Choice 基本校验 | 已增强但不完整 | `SettingsRuntime.cs:439-464` |
| RequestId 冲突不覆盖 replay、满载不淘汰 | 已修复 | `:267-285`；满载返回 `RateLimited` |
| Policy 先验证再替换、拒绝旧 generation | 已修复一部分 | `:289-305`；但 Clear 后代际墓碑问题仍存在，见 B-02 |
| nested AllowedValues 深拷贝 | 已修复静态实现 | `:462-468` |
| unknown scope fail-closed | 已修复 | `:251`、`:271`、`:470` |
| 新增基础 TDD | 已补但仍不完整 | `Program.cs:86-90`、`:124-141` |

## 四、阻断项

### B-01：Descriptor/Policy 浮点和策略范围语义仍可非法通过

**位置：** `src/BetterUnturnedExperience.Core/Settings/SettingsRuntime.cs:407-415`、`:439-458`。

Round 2 已增加默认值、Kind、整数 Step 和部分 Float Step 校验，但仍未拒绝：

- descriptor `Minimum` / `Maximum` 为 `NaN` 或 Infinity；
- Float `Minimum > Maximum` 中含非有限值导致比较失效；
- Server policy 的 Minimum/Maximum 为 NaN/Infinity；
- policy Minimum > Maximum；
- Choice policy 携带非法 range/step，或 AllowedValues 重复/空文本等未定义语义。

`ValueValid` 对边界只做比较，NaN 会使 `<`/`>` 均为 false，从而被视为有效；`PolicyCompatible` 只检查 policy 的 Kind/Step 部分条件，没有完成完整 range/choice 语义校验。这样仍违反“类型/范围/Choice 语义非法时原子拒绝”。

**修复建议：** 抽取统一 metadata validator：先验证所有 numeric option 的 finite、Kind、min≤max、step>0；Choice policy 必须验证非空、同 Kind、无重复，并拒绝不适用的 range/step；将同一 validator 用于 descriptor 和 policy。

### B-02：ClearSessionOverlay 清除代际墓碑，断开后旧 generation 可重新生效

**位置：** `SettingsRuntime.cs:309-317`、`:345-352`。

`ClearSessionOverlay(currentGeneration)` 在清除政策和 replay 后把 `overlayGeneration` 设为 `0`。此后同一个旧 generation 或更小的旧 generation 会被 `ApplyServerPolicy` 接受：

- `ApplyServerPolicy` 的倒退判断仅在 `overlayGeneration != 0` 时执行；
- 清除后 `overlayGeneration == 0`，旧回调可重新建立 overlay；
- `ActivateConnectionGeneration` 也会把任意新传入值当作可接受代际。

这破坏 RT-06 的 stale session 防护：断开/清理后旧排队回调不能污染未来会话，ConnectionGeneration 必须有不可回退的已见代际/墓碑。

**修复建议：** 分离 `currentGeneration` 与 `overlayGeneration`；清理只清除 overlay/replay，不清除已见 generation。对 `generation <= lastSeenGeneration`（除非是明确当前仍活动的同代 policy 更新）fail-closed，并补充 disconnect → clear → old callback/policy rejection 测试。

### B-03：TDD 与持久化验收仍不完整

**位置：** `tests/BetterUnturnedExperience.Settings.Tests/Program.cs` 全文；实现 `SettingsRuntime.cs:91-219`。

当前新增测试覆盖了一个非法默认值、一个非法 policy、代际 replay 清理和满窗口，但仍没有覆盖工单 Acceptance 明确要求的关键路径：

- NaN/Infinity 的 descriptor 与 policy boundary；
- min>max 的 policy；Choice policy 重复/非法 range/step；
- `ClearSessionOverlay` 后旧 generation 的 policy/replay 被拒绝；
- nested descriptor/policy/snapshot 集合被外部修改后的隔离效果；
- future schema 文件被隔离并安全默认；
- 文件写入/read-back/替换失败时上一份有效 target 保留；
- 迁移/版本化文档行为；
- RequestId conflict 后原 payload 仍返回最初 replay 结果。

此外，`FileSettingsPersistence.TryRead` 对浮点字段使用 `float.TryParse`，未显式拒绝 NaN/Infinity；即便 `LoadScope` 后续回退值，文件仍可能被当作格式有效，而未作为损坏文档隔离。必须补实现校验或明确格式语义，并加回归测试。

**修复建议：** 先补齐上述测试，必要时修复 File decoder；在测试可证明“失败不改变旧文件、未来 schema quarantine、旧代际拒绝”后重新走完整审计。

## 五、已通过的关键项

- 同功能 descriptor 注册、重复 SettingId 和 Feature mismatch：静态通过。
- 多字段 candidate 在 persistence 成功前不写回；单 runtime `lock(sync)` 线性化：静态通过。
- RequestId 冲突不再覆盖原 record，满 ReplayLimit 返回 `RateLimited`：源码核对通过。
- descriptor/policy `AllowedValues` 已做数组快照和 `ReadOnlyCollection` 包装：源码核对通过。
- LocalLoopback 仅进程内转发，不依赖 LMN：源码核对通过。
- Contracts/Core 无 Unity、Glazier、Sleek、LMN、BepInEx、Harmony 或 Unturned native token：扫描通过。
- 未发现 DEV-04～DEV-07、ClientUi、network codec 或 LMN production adapter 越界实现。

## 六、非阻断建议

1. `FileSettingsPersistence.GetPath` 仍以原始 FeatureId 直接构造文件名；上游 Registry 约束应在此 seam 之外得到保证，但建议本地再次 canonical 校验或使用安全 slug/hash。
2. `SettingsPersistenceLoadResult.DiagnosticId` 仍未传递至 `LastPersistenceDiagnosticId`，损坏隔离后诊断可见性不足。
3. `Fingerprint` 仍是分隔符字符串拼接；SettingId/Text 若允许分隔符，建议改为长度前缀的确定性编码。
4. 多个 runtime 实例共享同一文件时，File persistence 没有跨实例 CAS/锁；若不支持该用法应在 seam 文档明确。

## 七、放行条件

修复 B-01～B-03 后，重新执行：

1. Release Rebuild（0 errors / 0 warnings）；
2. DEV-03 与 Contracts 测试 EXE；
3. Contracts/Core token scan；
4. 独立子智能体审计；
5. Gemini 前端消费复核。

在此之前不得关闭 DEV-03，不得宣称设置运行时已满足跨代际安全或持久化故障验收。

