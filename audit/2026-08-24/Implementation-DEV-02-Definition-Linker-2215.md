# DEV-02 Definition Linker 实施报告

**作者：GPT**  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**状态：** GPT 实施完成，等待独立审计与 Gemini 复核

## 一、实施范围

本轮实现了构建期 `FeatureDefinitionLinker`、不变 `CompiledIdentityCatalog`、与当前 Catalog 和 Feature record 绑定的引用类 `FeatureAdmissionHandle`与 `FeatureLoadGate`。没有实现 DEV-03～DEV-07，没有修改 LMN。

## 二、可追溯矩阵

| 需求 | 落地 |
| --- | --- |
| 全仓原子链接 | `src/BetterUnturnedExperience.Core/Definitions/FeatureDefinitionLinker.cs:FeatureDefinitionLinker.Link` 检查空集、identity/binding 片段、schema、重复片段和必需依赖 |
| 稳定排序与 digest | 按 FeatureId、FragmentKind 与固定数值 canonicalize，SHA-256 转为 `Digest256`，不包含时间、路径、用户或随机值 |
| 不可变 Catalog | `CompiledIdentityCatalog` 使用 `ReadOnlyCollection` / `ReadOnlyDictionary`，只提供查询视图 |
| opaque Admission | `FeatureAdmissionHandle` 为不可 default 的 sealed reference，构造器 internal，绑定 catalog 实例与 record，不表示授权或 Running |
| 无效功能拒绝 | `FeatureLoadGate.TryAdmit` 对未编译身份返回 false，不创建 handle |
| Headless 边界 | Contracts/Core 只引用 System/System.Core，复用 DEV-01 token scan |

## 三、TDD 记录

### RED

在 linker 源码存在前，测试引用 `BetterUnturnedExperience.Core.Definitions` 与 `FeatureDefinitionLinker` 时编译失败：`CS0234`。

### GREEN

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

4 项目：`0 errors / 0 warnings`。

```text
tests\BetterUnturnedExperience.Contracts.Tests\bin\Release\BetterUnturnedExperience.Contracts.Tests.exe
```

`DEV-02 definition linker tests: PASS`。覆盖空集失败、完整片段链接、不同输入顺序 digest 一致、重复片段失败、未知功能不准入和已知功能 opaque handle。

Contracts/Core token scan：均 `PASS`。

## 四、产物哈希

| 产物 | SHA-256 | 字节 |
| --- | --- | ---: |
| `BetterUnturnedExperience.Contracts.dll` | `5327DA78B954009D684FE38932FCCA9EB8A65EC7536B2A487DCFCA98C547B264` | 26624 |
| `BetterUnturnedExperience.Core.dll` | `1BA39EF2D03BF2A3D1FE947A7CD79F36D845E239ACFDE4DCAC370F21B4937F4E` | 12800 |
| `BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` | 3584 |
| `BetterUnturnedExperience.Contracts.Tests.exe` | `FFE2FE184B6B66DF804D2F574E63401F6C9F86BD6F8334C94DEF48A0F2C53730` | 10240 |

## 五、证据边界

DEV-02 PASS 只能证明构建期链接与静态句柄行为；不证明任何 SP、SteamP2PFriends 或 U3DS 运行、BepInEx 加载、设置、库存或网络兼容。

## 六、状态

GPT 实施与自测完成；独立审计 Round 1/2 分别提示测试覆盖不足，已补齐失败分支、稳定 diagnostics、handle 绑定、不变量和输入隔离测试，重建哈希已更新，等待 Round 3 轻量复审与 Gemini 复核。
