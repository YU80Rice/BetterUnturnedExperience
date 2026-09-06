# 工作区只读进度审计 — 2026-09-04

> 性质:**信息性进度审计**(只读,未改动任何现有文件;本文件为唯一新增)。
> 按输出评审循环口径的自我标注:本报告是进度汇报类审计笔记,不是工单交付产物;若需升格为正式审计产出(独立 Standards/Spec 双轴),须走 `docs/agents/output-review-loop.md` 全流程——此处**具名延后**。
> 审计方法:`git status/log/diff/ls-files`(只读)+ 交叉核对 `.scratch/bue-v2-lmn-adoption/`(wayfinder 地图、8 张实施票、handoff、delivery 报告)与 `CONTEXT.md`、`docs/agents/*.md`。

## 1. 项目目标(一句话)

把 BUE 发展为 Unturned 的开放运行时平台,V2 第一阶段 = 将 LMN(LaunchMultiplayerNet)官方纳入为 BUE 网络模块(「吃掉并消化」)+ 发布 `BueNetworkApi`,满足 `CONTEXT.md`「LMN 纳入完成标准」8 条(来源:`CONTEXT.md:97-99`)。

## 2. 进度总表

| 阶段 | 状态 | 证据 |
|---|---|---|
| V1 时代(DEV-01..16,物品交互/管理面板/单 DLL) | ✅ 已冻结,人工批准 2026-09-03 | 提交 `f1ae85c`、`6cbe83f`;`.scratch/bue-v2-lmn-adoption/map.md` 前置 |
| V2 wayfinder 决策(T1..T8 共 7 票) | ✅ 7/7 全解决,地图走完 | `.scratch/bue-v2-lmn-adoption/map.md:4`;T2 契约人工批准冻结(`7494da1`) |
| V2 第一阶段 spec + 8 张实施票 | ✅ 已发布并拆票 | `9ecac9f`(spec)、`e70c786`(to-tickets) |
| DEV-V2-01 SDK 传输基线锁定 | ✅ resolved(双轴 CLEAN) | `dc309ca`;票 Status 行;`audit/2026-09-03/DEV-V2-01/Delivery-*.md` |
| DEV-V2-02 BueNetworkApi 契约类型 | ✅ resolved(双轴 CLEAN) | `4a37d4b`;Delivery 同上 |
| DEV-V2-03 BueNetworkApi 运行时 | ✅ resolved(双轴 R3 CLEAN) | `c9027b7`;Delivery 同上 |
| DEV-V2-04 LMN 接管决策核 | ✅ resolved(用户拍板:只交决策核,接线归 06) | `87d535a`;Delivery 同上 |
| DEV-V2-05 LMN V1 数字频道兼容层 | ✅ resolved(双轴 R1 CLEAN;真机 no-op 验证=可选项待补) | `bc078df`;Delivery §2-§4 |
| **DEV-V2-06 接线 + 空迁移 + 网络模块设置** | ⏳ **ready-for-agent(设计已定稿,未落码)** | 票 Status;`handoffs/handoff-DEV-V2-06-20260903.md` |
| DEV-V2-07 三环境实机验证(单人/U3DS/P2P) | ⏳ ready-for-agent(人工环节) | 票 Status |
| DEV-V2-08 LIT/LIR/LHT 迁移验证 | ⏳ ready-for-agent | 票 Status;map.md T8 回填「验证未闭环」 |

**当前位置:V2 第一阶段 8 张实施票完成 5 张(01–05),停在 06 门口。** 06 的三接缝决策、组件契约签名、九段红测设计、已知坑清单已在 handoff 文档中定稿(handoff-DEV-V2-06 §三接缝决策/组件契约/红测设计),且开工前快照已冻结(undo-savepoint `20260903-214900-57b2`)——即"设计就绪、代码未动"。

## 3. 最新快照提交核对(HEAD = `bc078df`,DEV-V2-05)

- 分支:`master`,无远程跟踪(branch -vv 无 upstream)。**已跟踪文件零未提交改动**(`git diff HEAD --stat` 为空)——工作树在"已跟踪"意义上是干净的。
- `bc078df` 提交内容与交付报告完全吻合:8 文件 = 6 源码(3 个新 Core/Network 文件 + 双 csproj 嵌入列表 + Plugin.Tests Program.cs 红测)+ 票状态更新 + Delivery 报告(对照 `audit/2026-09-03/DEV-V2-05/Delivery-DEV-V2-05-v1-compat-20260903.md` §1/§5 与 `git show --stat`)。
- 评审闭环证据:红(`red-v1-compat-r1.log` CS0234)→ 绿(`green-v1-compat-anchor-r3.log` exit=0)→ 七运行器全绿 + Release 0/0 + token 扫描 PASS(Delivery §3,log 位于 `audit/2026-09-03/DEV-V2-05/`,按「log 不入库」惯例未提交);双轴 R1 双 CLEAN 记录于 Delivery §4,可延后 smell 具名(3 条 Standards + 2 条 Spec 卫生项),符合 output-review-loop 词表。
- 01–05 提交链(Delivery 报告逐票入库于 `audit/2026-09-03/DEV-V2-0X/`)与票 Status 行、handoff-DEV-V2-05/06 的「已知地基」表述三者互相一致,**未发现文档与提交漂移**。

## 4. 工作树状态:未跟踪文件清点(610 个已跟踪文件之外)

| 类别 | 内容 | 判定 |
|---|---|---|
| 文档/工单 | `.scratch/` 大部分(better-unturned-experience-architecture 顶层规格、bue-v2 的 handoffs/ 与一份快照)、`docs/`(agents/adr/research/sdk/third-party)、`issues/` | 应入库的工作资产,建议批量 `git add` |
| **从未提交的源码/构建资产** | `BetterUnturnedExperience.sln`、`eng/Verify-NoUiTokens.ps1`、`src/BetterUnturnedExperience.Transport/`、`src/BetterUnturnedExperience.NoOpFixture/`、`src/...Core/Definitions|Settings|Properties|CoreAssemblyMarker.cs`、Core/Network 三个旧文件(BueEnvelopeCodec/BueNetworkProtocol/LocalLoopbackTransport)、4 个测试项目(Contracts/Network/Placement/Settings.Tests) | **风险最高的一类**:历史会话只显式 add 了当票触碰的文件(handoff-DEV-V2-05/06「硬规则」自证此惯例),导致仓库历史不完整、跨机不可复现 |
| 审计/证据 | `audit/2026-08-24..09-03`(评审报告、构建/测试 log、DIAG DLL、sha256 清单)、`artifacts/DEV-*` | 报告类建议入库;log/DLL 按 04/05 先例留在本地 |
| 构建产物 | 各 `bin/`、`obj/` | 不应入库 |
| .gitignore | **不存在**(全仓未找到任何 .gitignore) | 缺陷:bin/obj/DLL 全靠纪律防误提交 |

## 5. 风险与差距

1. **无 .gitignore**:构建产物与证据 DLL 无防护栏,仅靠「bin/obj 不提交」纪律。
2. **无远程仓库**:master 无 upstream,610 个提交只有本地一份;且大量源码(sln、Transport、4 个测试项目、Core 半数文件)从未入库——磁盘故障即丢。
3. **06 适配债(已在 handoff 具名)**:03 的 `AssertBueNetworkRuntime` 需为帧格式 v2(sender 字段 + 按来源解析)适配;`INetworkTransport.Send` 签名变更波及 LocalLoopbackTransport/LmnTransportAdapter;新 Core 文件必须同时进 Core.csproj + Plugin.csproj 嵌入列表(05 实锤教训)。
4. **真机验证欠账**:05 真机 no-op 验证为拍板可选项(待补);07(三环境)是完成标准的硬门槛,含人工环节。
5. **完成标准对照**:「LMN 纳入完成标准」8 条中,API 内置/V2 频道/V1 兼容层代码面已具备,剩 06(接管生效+设置)、07(三环境验证)、08(迁移验证闭环)三块。

## 6. 下一步(文档承诺)

读 `handoffs/handoff-DEV-V2-06-20260903.md`(开头语已备好)→ 按 `/implement` + `/tdd` 落地 DEV-V2-06(红测锚点 `--bue-config-migration-red`,九段断言,先红后绿)→ 双轴审查 CLEAN → 提交 `feat(DEV-V2-06)` → 07 → 08(顺序拍板见 handoff §后续票)。
