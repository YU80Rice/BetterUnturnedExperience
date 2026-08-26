# DEV-15E Qualification Evidence 实施报告

## 一、需求执行概述

完成 DEV-15E 证据资格门禁构建：将既有 `RuntimeEvidencePackageValidator` 与
`QualificationEvaluator` 以窄编排 seam 组合，输出包完整性、环境资格和候选/DLL
身份绑定结果；不写入运行时生命周期、成熟度或发布授权。

## 二、源码溯源清单

| 需求 | 落实 |
| --- | --- |
| 包校验先于资格裁决 | `QualificationEvidenceGate.Evaluate` |
| 同一候选/DLL hash 的 SP、P2P Host/Client、U3DS Headless 全满足才通过 | `AllApplicableRolesFulfilled` + `QualificationEvaluator` |
| P2P 同 CaseId 与重叠时间窗 | `QualificationEvaluator.PairP2p`；`RuntimeEvidencePackageValidator` 允许仅 Host+Client 配对共享 CaseId |
| 坏包、缺证据、陈旧、失败和空 policy Fail-Closed | `EvidencePackageInvalid`、`QualificationIncomplete`、`InvalidPolicy` |
| 不改变发布授权/FeatureState | `QualificationEvidenceGate` 仅返回不可变结果，不持有写入接口 |
| TDD 红绿 | `tests/BetterUnturnedExperience.Release.Tests/Program.cs` 新增完整、缺失、坏包、null policy 与 P2P 配对断言 |

## 三、代码变更清单

- 新增 `.scratch/better-unturned-experience-architecture/issues/DEV-15E-qualification-evidence.md`；
- 新增 `src/BetterUnturnedExperience.Release/QualificationEvidenceGate.cs`；
- 完成 Release evidence/qualification 源码纳入解决方案构建；
- 修正 `RuntimeEvidencePackageValidator`：同 CaseId 仅允许 Host+Client 配对，重复角色/第三角色仍拒绝；
- 扩展 Release 测试为 DEV-15E 门禁测试；
- 新增 Gemini 交接：`.scratch/better-unturned-experience-architecture/handoffs/GPT-to-Gemini-DEV-15E-qualification-evidence.md`。

## 四、编译与测试验证记录

- 命令：`MSBuild.exe BetterUnturnedExperience.sln /t:Build /p:Configuration=Release /v:minimal`
- 结果：`0 errors / 0 warnings`；日志：[DEV-15E-build.log](./DEV-15E-build.log)
- 7 项测试全部 PASS；日志：[DEV-15E-tests.log](./DEV-15E-tests.log)
- Contracts/Core UI-native token scan：PASS；日志：[DEV-15E-token-scan.log](./DEV-15E-token-scan.log)
- Release DLL 引用语义：仅 `mscorlib`、`System.Core`。

## 五、独立审计记录

- Round 1：FAIL，阻断 B-01：`null policy` 抛异常，不满足 Fail-Closed。
- 修复：新增 `QualificationEvidenceGateStatus.InvalidPolicy`，空 policy 返回结构化结果；补回归测试。
- Round 2：PASS，无剩余阻断。独立审计确认 P2P 同 CaseId/同候选/同 DLL hash/重叠窗口、包双向引用、类型隔离均通过。

## 六、产物哈希

| 产物 | SHA-256 |
| --- | --- |
| `QualificationEvidenceGate.cs` | `6AB4BEFBC311BC88119E8E10A3D81CF4E606975268B130F9C86A138168D64446` |
| `RuntimeEvidencePackage.cs` | `8F90C5FB61B00718465155D145400A1DA8443470FF915F6209072992F4BAD87A` |
| `Program.cs` | `59E1AACB6A91F815EF8FC3DFA5E6E4BC5CC28E97EDF72BD2564182037A19BE88` |
| `BetterUnturnedExperience.Release.dll` | `473DFFFC3DBB36F0BEE2EFA22144157E77BF580912B4A84D28512BC34DD01EE3` |

## 七、证据边界与后续动作

本交付只证明资格门禁引擎和自动化测试闭环。尚未证明真实 Unity/Glazier callback、
单人游玩、SteamP2PFriends Host/Client、U3DS Headless、三环境同哈希运行或发布资格。
工单保持 `ready-for-human`，等待人工采集真实四角色证据及 Gemini 前端消费复核。

## 八、提交

`45ee80d Implement DEV-15E qualification evidence gate`
