# DEV-03 SettingsRuntime 实施报告

**作者：GPT**  
**Baseline:** `BUE-V1-RT01-20260824`  
**SourceSet:** `BUE-SS-20260824-02`  
**状态：等待独立审计 Round 3 与 Gemini 消费复核**

## 需求执行概述

按 TDD Red → Green 实现 SettingsRuntime、版本化原子文件持久化和进程内 LocalLoopback；未实现 DEV-04～DEV-07、LMN、ClientUi 或三环境运行逻辑。

## 源码溯源

| 需求 | 落实 |
|---|---|
| 完整快照、revision、三 authority | `src/BetterUnturnedExperience.Core/Settings/SettingsRuntime.cs` `SettingsRuntime.GetSnapshot/Submit/BuildSnapshot` |
| 多字段原子提交 | `SettingsRuntime.Apply`：候选副本→持久化→内存替换 |
| RequestId 幂等/冲突/固定 replay 容量 | `SettingsRuntime.Submit/Remember` |
| 本地原子持久化与损坏隔离 | `FileSettingsPersistence.Load/TryCommit` |
| Policy session overlay 与 generation watermark | `ApplyServerPolicy/ClearSessionOverlay/ActivateConnectionGeneration` |
| LMN-free local path | `LocalLoopbackSettingsTransport.Send` |
| TDD | `tests/BetterUnturnedExperience.Settings.Tests/Program.cs` |

## TDD 与验证

- 初始 Red：缺少 `BetterUnturnedExperience.Core.Settings` 实现，测试编译失败。
- Green：实现后 DEV-03 测试 PASS。
- Release Rebuild：5 项目，`0 errors / 0 warnings`。
- Contracts 测试：PASS。
- DEV-03 测试：PASS。
- Contracts/Core UI/native token scan：PASS。
- 最终日志：[DEV-03-build-final.log](./DEV-03-build-final.log)、[DEV-03-tests-final.log](./DEV-03-tests-final.log)。

## 当前产物哈希

| 产物 | SHA-256 |
|---|---|
| Contracts DLL | `31FA2A9A0840CE12E7D2C496A8B1FA13A3433FF340C2E0F6B9B7B200DAF531F6` |
| Core DLL | `929AD5FBDF84DF045BCAE834DF31301AA0AAFFAC67DA1C521F699B471E5378E3` |
| Plugin DLL | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |
| Settings test EXE | `C43D887EDDEE3BE5CF0B7E7385C352BEBE3E7902F265C8AC6ED7278F4AFDF7E3` |

## 审计状态

Round 1 FAIL（B-01～B-06）；Round 2 FAIL（Float policy finite/range、stale generation watermark、TDD evidence）。已修复并重新编译；Round 3 独立审计尚未返回。未标记 `resolved`，不宣称运行环境或发布通过。

独立审计 Round 3：等待子智能体返回；未宣称 PASS。
