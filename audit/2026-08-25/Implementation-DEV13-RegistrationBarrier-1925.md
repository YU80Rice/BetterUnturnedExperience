# DEV-13 独立 No-op Feature 注册运行时与 Catalog Barrier 实施报告

## 一、需求执行概述

依据 `spec-open-runtime-feature-framework.md`、`SCR-GPT18-001` 与新建工单 `DEV-13-noop-feature-runtime-registration-smoke.md`，在 DEV-12 单 DLL Host 冒烟通过后，补齐 BUE Host 拥有的注册关闭 barrier：所有依赖插件完成 `Awake` 注册后，由 BUE Host 统一执行 `RegistrationOpen → CatalogFrozen → RuntimeReady`。

## 二、源码溯源矩阵

| 需求点 | 落实位置 |
| :--- | :--- |
| Host-owned 原子 barrier | `src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs`：`CompleteRuntime()` |
| 确定性 Catalog 组合 | `FeatureRegistrationRuntime.BuildCatalogLocked()`，保持 FeatureId/digest Ordinal 排序 |
| Unity 启动后关闭注册 | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`：`Start()` |
| 外部功能只能公开注册 | `src/BetterUnturnedExperience.Plugin/BueRuntimeHost.cs` 与 No-op Fixture `Register()` |
| TDD barrier 断言 | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` |
| 单 DLL 物理 staging | `artifacts/DEV-13-noop-registration-20260825/` |

## 三、TDD 记录

- Red：测试先调用尚不存在的 `CompleteRuntime()`，编译稳定失败：`CS1061 FeatureRegistrationRuntime 未包含 CompleteRuntime`。
- Green：新增 Host-owned `CompleteRuntime()`，原子生成 Catalog 并进入 `RuntimeReady`；Plugin `Start()` 调用该 barrier。
- 回归：No-op 注册成功、Catalog 生成、RuntimeReady 状态和 barrier 后晚注册拒绝均通过。

## 四、构建与测试验证

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /m /v:minimal
```

结果：`0 errors / 0 warnings`。

7 个测试项目全部通过：

```text
DEV-10 registration runtime tests: PASS
DEV-03 settings runtime tests: PASS
DEV-04 placement evaluator tests: PASS
DEV-05 ClientUi tests: PASS
DEV-06 network tests: PASS
DEV-08 runtime evidence package tests: PASS
DEV-13 external registration barrier tests: PASS
```

## 五、产物与哈希

| 产物 | SHA-256 |
| :--- | :--- |
| `artifacts/DEV-13-noop-registration-20260825/BetterUnturnedExperience.dll` | `2CC63E1142FBAAFE8F13019757E8A39F6354A41A98C9F75827FA036D4EDDEC21` |
| `artifacts/DEV-13-noop-registration-20260825/BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` |

主 DLL 仍为唯一 BUE ABI 运行时事实源；No-op Fixture 不引用 `BetterUnturnedExperience.Core` 或 `BetterUnturnedExperience.Contracts` 运行时程序集。

## 六、独立审计门禁

GPT 独立审计 R1：`audit/2026-08-25/DEV-13-Independent-Audit-R1.md`，判定 **PASS，0 阻断**。现交 Gemini 进行前端公开 ABI、统一管理状态与 Headless 消费复核；在真实客户端部署 BUE + No-op Fixture 冒烟前，工单保持 `ready-for-human`，不得标记 `resolved`。

## 七、边界与未宣称事项

本票不证明 U3DS、单人、SteamP2PFriends Host/Client、ClientUi Satellite、Better Item Interaction 或三环境发布资格；不修改 LMN、BepInEx、U3DS 或 Unturned 原版内容。

## 八、人工双 DLL 冒烟 R1 与修复

`UMM-诊断包_20260825_193703` 证明 BUE 与 No-op 均被加载且 No-op 注册成功，但没有 `BUE-BOOTSTRAP-003 status=RuntimeReady`。该证据不足以关闭 barrier 运行门禁。

修复：BUE Plugin 保留 `Start()` 首选路径，并增加一次性 `Update()` fallback，仍只由 BUE Host 调用 `CompleteRuntime()`。修订后 Release 构建和 7/7 测试通过；新 BUE DLL SHA-256 为 `A76202EB3087549695C623C4B008F70E35E424252B79D6CE4C24919290065A64`。等待重新人工冒烟。

