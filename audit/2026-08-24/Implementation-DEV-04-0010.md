# DEV-04 PlacementCandidateEvaluator 实施报告

**作者：GPT**  
**Baseline:** `BUE-V1-RT01-20260824`  
**SourceSet:** `BUE-SS-20260824-02`  
**状态：等待独立审计与 Gemini 消费复核**

## 需求执行概述

按 TDD Red → Green 实现纯 C# `IPlacementCandidateEvaluator`，覆盖几何中心投影、边缘 clamp、Local-Fit Priority 四级阶梯、自动旋转、确定性扩展排序、失败状态和 Release 热路径零分配测试。

## 源码溯源

| 需求 | 落实 |
|---|---|
| Local-Fit current → rotated → expanded current → expanded rotated | `src/BetterUnturnedExperience.Core/Placement/PlacementCandidateEvaluator.cs:Evaluate` |
| 中心投影与边缘 clamp | `PlacementCandidateEvaluator.Project` |
| 占用/边界判断 | `PlacementCandidateEvaluator.Fits` |
| 距离平方、Y/X 稳定排序 | `PlacementCandidateEvaluator.Search` |
| 隐藏/Occupied/OutsideGrid 输出 | `Evaluate` failure branches |
| 公共构造 seam | `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` placement DTO constructors |
| 表驱动与零分配复测 | `tests/BetterUnturnedExperience.Placement.Tests/Program.cs` |

## TDD 记录

- RED：测试项目首先因 `BetterUnturnedExperience.Core.Placement` 实现缺失而编译失败。
- GREEN：加入 evaluator 后通过；追加奇数当前旋转、边界、障碍狭缝、满网格、超尺寸、确定性排序和热路径分配测试后仍通过。

## 验证结果

- Release solution rebuild：0 errors / 0 warnings。
- DEV-02 tests：PASS。
- DEV-03 tests：PASS。
- DEV-04 placement tests：PASS。
- Contracts/Core UI-native token scan：PASS（2 / 5 C# files）。

## 产物哈希

- Core DLL：`6F502668701A34FB12E707E29086BBF9A2B2F3F391C88A6E616F168788359B08`
- Placement test EXE：`08D4EADF723C3237BFA27DD4CD0D26DDB33A0FEB40694952FD387E5D27DA0834`
- Contracts DLL：`31FA2A9A0840CE12E7D2C496A8B1FA13A3433FF340C2E0F6B9B7B200DAF531F6`
- Plugin DLL：`AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC`

## 证据边界

本报告只证明源码、单元测试和静态门禁；不证明 Glazier、Unturned 原生提交链、SP、SteamP2PFriends、U3DS 或发布授权。
