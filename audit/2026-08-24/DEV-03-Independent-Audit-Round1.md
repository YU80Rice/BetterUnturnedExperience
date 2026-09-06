# DEV-03 独立审计报告（Round 1）

**作者：GPT（独立审计子任务）**  
**日期：2026-08-24**  
**审计对象：** `DEV-03 SettingsRuntime + 原子持久化 + LocalLoopback`  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**审计方式：** 工单/契约/实现只读审查，独立 Release Rebuild、测试 EXE、Contracts/Core token scan。未修改生产代码。

## 一、最终裁定

**判定：FAIL（4 个阻断项）**

当前构建、现有测试和类型隔离扫描通过，但实现尚未满足 DEV-03 的安全语义：描述器非法默认值可被接受，策略覆盖不是失败原子操作，RequestId replay 窗口会淘汰旧记录且冲突会覆盖原记录，设置快照/描述器中的嵌套集合未做深层不可变隔离。当前不能标记 `resolved`，也不能进入依赖 DEV-03 的后续集成验收。

## 二、独立复跑证据

### 2.1 Release Rebuild

命令：

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

结果：均 **PASS**：

```text
DEV-03 settings runtime tests: PASS
DEV-02 definition linker tests: PASS
```

### 2.3 Token scan

```text
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Contracts
eng\Verify-NoUiTokens.ps1 -SourceRoot src\BetterUnturnedExperience.Core
```

结果：**PASS**，分别扫描 2 与 4 个 C# 文件。

### 2.4 本轮产物哈希

| 产物 | SHA-256 |
| --- | --- |
| `tests/BetterUnturnedExperience.Settings.Tests/bin/Release/BetterUnturnedExperience.Settings.Tests.exe` | `C43D887EDDEE3BE5CF0B7E7385C352BEBE3E7902F265C8AC6ED7278F4AFDF7E3` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `929AD5FBDF84DF045BCAE834DF31301AA0AAFFAC67DA1C521F699B471E5378E3` |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `31FA2A9A0840CE12E7D2C496A8B1FA13A3433FF340C2E0F6B9B7B200DAF531F6` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |

## 三、逐项结果

| 审计维度 | 判定 | 证据 |
| --- | --- | --- |
| 描述器属于同一 Feature、SettingId 重复拒绝 | PASS（静态） | `SettingsRuntime.cs:419-432`。 |
| 原子多字段提交与 revision 线性化 | 部分 PASS | `:267-369` 在单个 runtime 的 `lock(sync)` 中先构造 candidate，持久化成功后再替换状态；但策略覆盖和 replay 还有阻断项。 |
| 初始快照完整性/外部输入隔离 | **FAIL（B-04）** | descriptors 的 `AllowedValues` 与 policy 的 `AllowedValues` 只复制外层集合，嵌套列表仍可被外部修改。 |
| Descriptor 类型/范围/Choice 语义校验 | **FAIL（B-01）** | `ValidateDescriptors` 未验证默认值在范围/Choice 内、range/step 的 Kind、浮点有限性与 step 合法性。 |
| ExpectedRevision/未知键/类型/authority 失败不改状态 | 部分 PASS | `:345-369` 覆盖基本路径；非法 enum scope 未拒绝，见 B-05。 |
| RequestId 幂等/冲突/窗口 | **FAIL（B-02）** | `:267-284`、`:371-376` 在冲突时覆盖旧 replay，并在满容量时淘汰旧记录。 |
| 文件版本化、临时文件、重读、同卷替换 | PASS（静态） | `FileSettingsPersistence.cs:130-165`；需补充故障测试。 |
| 损坏/未来 schema 安全默认与隔离 | 部分 PASS | `:110-128` 能校验摘要/schema 并隔离文件，但 DEV-03 测试未覆盖未来 schema/写入失败保留旧文件。 |
| ServerPolicy overlay 与 generation | **FAIL（B-03）** | `:287-303` 先切代清空旧 overlay，再验证新 policy；新旧 generation 也未拒绝倒退。 |
| LocalLoopback 不依赖 LMN | PASS | `:451-459` 仅进程内调用 `SettingsRuntime.Submit`，无 LMN/UI/native 引用。 |
| Contracts/Core Headless 隔离 | PASS | 独立 token scan 通过。 |
| 未实现 DEV-04～DEV-07 / LMN | PASS（源码范围） | Settings 目录内无候选算法、ClientUi、network codec 或 LMN adapter。 |
| TDD 完整度 | **FAIL（B-06）** | 未覆盖 descriptor 非法默认/范围语义、overlay 失败原子性、replay 满容量/冲突保留、深层快照隔离、未来 schema 与文件写入失败。 |

## 四、阻断项

### B-01：非法 Descriptor 语义可登记

**位置：** `src/BetterUnturnedExperience.Core/Settings/SettingsRuntime.cs:419-432`。

`ValidateDescriptors` 仅检查 FeatureId、SettingId、非零 schema、默认值 Kind、Choice 列表非空/去重和部分 min/max 关系。它没有调用完整值验证，也没有验证：

- Integer/Float 默认值是否落在 Minimum/Maximum 内；
- Choice 默认值是否属于 AllowedValues；
- Minimum/Maximum/Step 的 `SettingValue.Kind` 是否与 descriptor Kind 一致；
- Float 的默认值、边界、Step 是否为有限值；
- Step 是否为正且适用于该数值类型。

因此例如默认 Integer=99、范围 0..10 的 descriptor，或默认 Choice=`blue`、AllowedValues=`dark/light` 的 descriptor，会被构造成功，初始快照携带非法默认值，违反“类型/范围/Choice 语义非法时原子拒绝”。

**修复建议：** 将 descriptor 元数据先规范化为深层不可变结构，再对默认值与所有 range/choice 元数据执行同一套严格校验；非法输入在 runtime 建立前整体拒绝，并补充对应 TDD。

### B-02：RequestId replay 窗口与冲突记录违反幂等边界

**位置：** `SettingsRuntime.cs:267-284`、`:371-376`。

两处问题：

1. 已存在 RequestId 的不同 payload 进入 `RequestIdConflict` 分支后调用 `Remember`，会用冲突 fingerprint 和冲突结果覆盖原始 replay 记录。随后原始 payload 重放不再返回最初结果。
2. `while (state.Replay.Count > ReplayLimit) state.Replay.Remove(state.Replay.Keys.First())` 会在容量满时淘汰旧记录。RT-06 已冻结“容量满拒绝新唯一请求，不淘汰旧记录制造重放窗口”；淘汰后旧 RequestId 可再次作为新请求执行并推进 revision。

**修复建议：** RequestId conflict 只返回拒绝，不写入 replay；当 replay 已达固定容量且是新 RequestId 时，返回容量/限流型稳定错误并保持原缓存。补充 conflict-after-conflict、满容量和旧 id 重放测试。

### B-03：ServerPolicy overlay 应用不是原子且允许代际倒退

**位置：** `SettingsRuntime.cs:287-303`、`:336-343`。

`ApplyServerPolicy(newGeneration, policy)` 先调用 `ActivateConnectionGenerationCore` 清除旧 overlay/replay，再逐项验证新 policy。若新代际 policy 中途包含非法 descriptor/policy，方法返回 `false`，但原来的有效 overlay 已被清空。另一个问题是没有保存/比较当前 generation，旧的较小 generation 在新 generation 之后仍可被接受并覆盖当前 overlay。

**修复建议：** 先复制并完整验证 policy（包括 range/choice/Kind/finite 语义），验证成功后以一次锁内提交切换代际；对 `generation < current` 直接拒绝并保持所有旧状态。补充“新代际部分非法不改变旧 overlay”和“倒退代际被拒绝”测试。

### B-04：Snapshot/Descriptor 的嵌套集合没有深层不可变隔离

**位置：** `SettingsRuntime.cs:240-248`、`:293-302`、`:380-393`；契约 `ContractTypes.cs:90-98`。

runtime 只复制 descriptors 的外层数组，并把 `SettingDescriptor.AllowedValues`、`SettingPolicyView.AllowedValues` 中的调用方 `IReadOnlyList` 引用原样保留。`FeatureSettingsSnapshot.Entries` 虽是 `ReadOnlyCollection`，其 `SettingEntryView.Policy.AllowedValues` 仍可能指向外部可变数组；外部修改 descriptor/policy 输入后，会改变后续校验或已返回快照观察到的内容，违反“快照和输入集合不可被外部后续修改”。

**修复建议：** 在构造 runtime 和应用 policy 时深复制并包装所有 `AllowedValues`；构建 snapshot 时再输出不可变快照；对 descriptor 数组、policy 数组和已返回 snapshot 做 mutation/隔离测试。

### B-05：非法 SettingRevisionScope 被静默当作 ServerAuthority

**位置：** `SettingsRuntime.cs:271`、`:382`、`:397`。

所有非 `ClientPreference` 的 enum 值都落入 server 分支。未受信 command 传入未知 byte 时可能写入/读取 ServerAuthority，而不是返回 `SettingRejected`/稳定错误。这违反 scope/authority fail-closed 边界。

**修复建议：** 入口显式验证 `Enum.IsDefined(typeof(SettingRevisionScope), request.RevisionScope)`；snapshot/overlay 相关 API 对未知 scope 也拒绝或抛出明确参数错误，并补充未知 enum 测试。

### B-06：TDD 未覆盖关键门禁

当前 `tests/BetterUnturnedExperience.Settings.Tests/Program.cs` 覆盖了基本 happy path、一次内存持久化失败、文件损坏回退、基本 policy/generation 和 loopback，但没有覆盖：

- 非法默认/范围/Choice descriptor；
- policy 更新失败原子性和代际倒退；
- RequestId conflict 保留原记录、replay 容量满拒绝；
- descriptor/policy/snapshot 深层集合隔离；
- 非法 scope；
- future schema 文件隔离；
- 文件写入/read-back 失败时保留既有 target 文件。

这些缺口使当前测试绿灯不足以证明 DEV-03 Acceptance。

## 五、非阻断建议

1. `FileSettingsPersistence.GetPath` 使用原始 `FeatureId` 作为文件名；虽然上游 Registry 应约束 FeatureId，但此公共 persistence seam 自身只检查非法文件字符，建议额外验证 canonical identity 或使用安全 slug/hash，防止 `..` 等路径语义逃逸。
2. `LoadScope` 对无法通过 descriptor 验证的已存值静默回退，但不把 `SettingsPersistenceLoadResult.DiagnosticId` 传入 `LastPersistenceDiagnosticId`；建议保留可诊断的 load provenance。
3. `FileSettingsPersistence.TryRead` 对未知额外 setting entry 允许文件整体有效，是否兼容应在 schema policy 中明确；当前只检查 descriptor 缺失，不检查未知键。
4. `Fingerprint` 使用字符串拼接而不是长度前缀/规范二进制编码，SettingId/Text 若允许分隔符可能产生理论碰撞；建议使用确定性长度前缀编码。
5. 多个 `SettingsRuntime` 实例指向同一文件时，File persistence 没有跨实例线性化锁；若未来允许该用法，应补锁或版本 CAS。

## 六、证据边界

- 本报告只证明源码/构建/单元测试审计结果，不证明 SP、SteamP2PFriends Host/Client、U3DS 或发布授权。
- 未执行生产环境运行；没有任何 BUE CandidateBuild 三环境运行 PASS。
- 没有发现本轮实现引用 Unity、Glazier、Sleek、LMN、BepInEx、Harmony 或 Unturned native 类型。

## 七、放行条件

修复 B-01～B-06 后，必须重新执行：

1. Release Rebuild，目标 `0 errors / 0 warnings`；
2. DEV-03 与 Contracts 测试 EXE；
3. Contracts/Core token scan；
4. 独立子智能体审计；
5. Gemini 前端消费复核。

在此之前不得关闭 DEV-03，不得宣称本地设置事务已获得三环境运行资格，也不得提前推进依赖其结果的网络/ClientUi 验收。
