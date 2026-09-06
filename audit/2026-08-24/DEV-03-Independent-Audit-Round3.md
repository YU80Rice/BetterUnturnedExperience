# DEV-03 独立审计报告（Round 3）

**作者：GPT（独立审计子任务）**  
**日期：2026-08-24**  
**审计对象：** `DEV-03 SettingsRuntime + 原子持久化 + LocalLoopback`  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**前轮报告：** `DEV-03-Independent-Audit-Round1.md`、`DEV-03-Independent-Audit-Round2.md`（均保留）

## 一、最终裁定

**判定：FAIL（2 个阻断项）**

Round 2 的三项主要问题已基本修复：Float finite/range 检查、generation watermark、RequestId 原记录保留、未来 schema 隔离和若干嵌套只读测试均已出现，独立构建、测试和 Token Scan 全部通过。

但当前策略校验仍可能扩大静态 Descriptor 的允许范围，且 Round 3 TDD 尚未覆盖完整的策略边界与持久化失败保持旧文件门禁。因此 DEV-03 仍不能标记 `resolved`。

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

### 2.3 Contracts/Core UI-token scan

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

## 三、Round 2 修复核对

| Round 2 项目 | 状态 | 证据 |
| --- | --- | --- |
| Float descriptor/policy finite 与 min/max 检查 | 已增强 | `SettingsRuntime.cs:427-435`、`:460-480` |
| generation watermark | 已增强 | `:232-235`、`:303-335`、`:363-372`；Clear 不再清除 `generationWatermark` |
| RequestId conflict 不覆盖原 replay | 已修复 | `:277-280` 直接返回冲突，不调用 `Remember` |
| replay 满载不淘汰旧记录 | 已修复 | `:282` 返回 `RateLimited` |
| future schema quarantine | 已补测试 | `Program.cs:120-127` |
| descriptor/policy 只读集合 | 已部分补测 | `Program.cs:92-94`、`:158-163` |

## 四、阻断项

### B-01：ServerPolicy 可以放宽静态 Descriptor 边界

**位置：** `src/BetterUnturnedExperience.Core/Settings/SettingsRuntime.cs:427-435`、`:437-458`。

`PolicyCompatible` 只验证 policy 自身的 Kind、finite、min≤max、Step 和 Choice 列表基本形状；它没有验证 policy 是静态 Descriptor 的“收窄覆盖”：

- 数值 policy 的 Minimum/Maximum 可以超出 descriptor.Minimum/Maximum；
- Float/Integer policy 也没有验证 policy Step 与静态边界/静态 Step 的兼容关系；
- Choice policy 只检查值为 Choice、去重和非空，没有验证每个 policy value 属于 descriptor.AllowedValues；
- Choice policy 走早返回路径，未拒绝不适用的 `Minimum`、`Maximum` 或 `Step` 元数据。

随后 `ValueValid` 以 policy min/max 和 policy AllowedValues 替换 descriptor 的边界/集合，可能接受静态 schema 未声明的值，违反 Settings Facet 是静态事实源以及服务器政策只能限制、不能扩展客户端契约的边界。

**建议修复：** 统一策略校验器：

1. policy numeric bounds 必须落在 descriptor bounds 内，且 policy step 不得破坏 descriptor step 语义；
2. Choice policy AllowedValues 必须是 descriptor AllowedValues 的非空子集；
3. Choice/Toggle 必须拒绝所有不适用的 range/step；
4. 补充“policy 放宽范围/引入未声明 Choice/带非法 range”均保持旧 overlay 不变的测试。

### B-02：持久化失败保护与策略边界 TDD 仍不完整

**位置：** `tests/BetterUnturnedExperience.Settings.Tests/Program.cs`、`src/BetterUnturnedExperience.Core\Settings\SettingsRuntime.cs:130-165`。

当前测试新增了 future schema quarantine、一个非法默认、一个非法 policy、代际倒退、Replay 满载及嵌套集合隔离，但仍没有覆盖 DEV-03 Acceptance 中要求的：

- File commit/read-back/replace 失败时上一份有效 target 文件保持不变；
- Float NaN/Infinity descriptor 与 policy boundary；
- policy 放宽静态范围、Choice 子集/越界语义和 Choice range/step；
- `ClearSessionOverlay` 后旧 generation policy/queued replay 不能重新生效；
- descriptor/policy 输入集合在构造/Apply 后被外部修改不会改变 runtime；
- persistence migration/version semantics。

实现使用临时文件、重读校验和替换路径（`SettingsRuntime.cs:130-165`）具备正确方向，但没有故障注入 seam 来证明“替换失败不破坏旧 target”；现有内存 `FailNextCommit` 不能证明 File persistence 的原子替换保护。

**建议修复：** 增加可注入 FileSystem/replace failure seam 或等价受控测试，补齐上述策略矩阵和 generation 清理测试；只有测试能证明旧文件和旧 snapshot 在失败时保持不变，才关闭此门禁。

## 五、已通过的关键项

- 描述器同 Feature、SettingId 唯一、schema 非零且统一、默认值基本校验：源码通过。
- 单 runtime `lock(sync)` 下 candidate 先验证/持久化后写回，成功 revision 单调且多字段事务不部分提交：静态通过。
- RequestId 冲突原记录保留，Replay 满载拒绝新唯一请求：源码与测试通过。
- Clear overlay 不再清除 generation watermark，旧 generation 倒退被拒绝：源码与现有测试通过。
- future schema 文档隔离回退安全默认：测试通过。
- LocalLoopback 仅进程内转发、不依赖 LMN；Contracts/Core 无 UI/native token：通过。
- 未发现 DEV-04～DEV-07、ClientUi、network codec 或 LMN production adapter 越界实现。

## 六、非阻断建议

1. `FileSettingsPersistence.GetPath` 仍直接使用 FeatureId 构造路径名；建议在 persistence seam 内再次执行 canonical identity 校验或使用安全 slug/hash。
2. `LastPersistenceDiagnosticId` 仍未统一记录 Load 的 `DiagnosticId`；损坏/未来 schema 诊断可见性可增强。
3. `Fingerprint` 仍采用分隔符字符串拼接；建议改为长度前缀的确定性编码。
4. 多个 runtime 实例共享同一设置文件时没有跨实例 CAS/锁；如不支持，应在接口契约中明确单实例所有权。

## 七、放行条件

修复 B-01～B-02 后，重新执行 Release Rebuild、两套测试、Token Scan、独立审计，并交 Gemini 做消费复核。此前不得关闭 DEV-03 或宣称设置持久化已通过完整故障验收。

