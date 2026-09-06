# DEV-V2-16：平台——会话驱动组播与发送结果语义（Q4）

Type: task
Status: resolved（2026-09-06，双轴 R1→修复→R2→记账→R3→裁定→R4 全 fresh 链闭环：Standards R2 CLEAN / Spec R4 CLEAN；候选身份与链路见 `audit/2026-09-06/DEV-V2-16/DEV-V2-16-closing-report.md`）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-14（契约 Major 升级与登记流程须先建立）
Spec: `../spec.md`（「平台：发送语义 = 会话驱动组播」一节）

## What to build

功能模块的组播变成可推理的动作：`SendToClients` 只向当前已建立 BUE 会话逐一定向发送——未装 BUE 的原版玩家零感知，本地主机身份天然不在远端会话集合（功能模块无需手写跳过本地）；每次发送返回显式结果，部分失败不再需要解析日志。

## Scope

- `SendToClients` 语义重写：established 会话逐一定向，不是无目标帧交底层广播。
- 发送结果冻结：快照空 → `NoSession`；≥1 成功 → `Sent`；有目标全失败 → `LocalTransportUnavailable`；**新增 `PartialFailure` 枚举成员**表达部分失败。
- `Sessions` 语义收窄为 established 快照（登记条目③）；pending 会话仅内部可见。
- `SendToClient` 维持按会话寻址（**不新增** SteamId 重载），校验会话归属本运行时、established、当前连接代际。
- 发送不持状态锁。

## 验收条件

- [x] 红测先行：发送结果五值（含 `PartialFailure`）/ established-only 快照 / 外来会话与过期代际被拒 / 无会话 → `NoSession`——先红后绿（编译红 `red-compile-build.log` CS0117 + 运行时红收集 10 条 `red-runtime-transcript.log` + 修复轮红 1 条 `red-fix-round-transcript.log` + 绿 `fix-round-build.log`；「过期代际/Sent/停用联动/超限载荷/频道优先」为旧代码下无红路径或上游已红的回归保持断言，归类经 R2/R3/R4 三轮独立核验成立）
- [x] 与 DEV-V2-14 停用语义联动回归（停用时发送仍 `NoSession`，无新错误码——红测钉住 `SendToClients`/`SendToServer` 停用态返回值）
- [x] 冻结面变更登记条目③追加（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` 条目③④，契约维持 2.0）
- [x] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN（7/7 PASS、0 警告 0 错误；fresh 链 R1→修复→R2（S:CLEAN）→R3→裁定→R4（Spec:CLEAN））

## 实施结单（2026-09-06，agent /implement，全链闭环）

- **交付**：契约③ `NetworkSendResult.PartialFailure=204` + `SendToClients` 会话驱动组播（established 快照逐一定向、五值聚合、发送不持状态锁）+ `SendToClient` 身份/established/代际三重校验（不新增 SteamId 重载）；契约④ `Sessions` 收窄 established 快照（pending 仅内部可见）；`SendToServer` 连带 established 门+锁外发送（单帧 untargeted）；载荷一次性预检（超限保留专用 `PayloadTooLarge`）；SDK 登记条目③④（契约维持 2.0）。
- **红绿链**：编译红 CS0117（`red-compile-build.log`）→ 运行时红收集 10 条覆盖四组+锁外发送（`red-runtime-transcript.log`）→ 绿；修复轮红 1 条（`red-fix-round-transcript.log`）→ 绿 7/7 PASS 0 警告（`fix-round-build.log` + `green-*.log`×7）。
- **候选身份**：`BetterUnturnedExperience.dll` SHA-256 `3e4259e36b2a3a0d48e4df4b6e82829160c2842bb386d0a1c42e3b39bdd1073f`（351232 字节，两轮 Rebuild 一致）。不继承 14/15 发布批准。
- **审查链**：R1 双轴 NOT CLEAN（S:BLOCKING 1+SMELL 4；Spec:BLOCKER 1+GAP 1）→ 修复轮（Established 锁内发布/频道门优先/null 检查移位/注释与重复清理）→ R2 双轴全新实例（S:**CLEAN**，3 DEFERRABLE 并入具名延期；Spec:**BLOCKED**——唯一 BLOCKER 为审查链记账未落盘）→ 记账收尾（结单链+R1/R2 四报告归档，生产代码零改动）→ R3（fresh，仅 Spec：R2 BLOCKER 核实解除+Scope 终审零发现，唯一 BLOCKER=自指记账）→ 实施者裁定（自指记账=规约第 4 步机械动作，沿 DEV-V2-14 先例）→ R4（fresh，仅 Spec，最终核验）：**CLEAN**。**fresh 链 R1→修复→R2→记账→R3→裁定→R4 闭环，双轴最终 CLEAN（Standards=R2，Spec=R4）**，候选身份 `3e4259e3…073f` 全链不变。
- 报告与判词：`audit/2026-09-06/DEV-V2-16/`（结单报告+R1/R2 四份判词+全套红绿证据）。

## Comments

- **2026-09-06 开工**：/implement claim（Blocked by DEV-V2-14 已 resolved；前沿唯一）。
- **2026-09-06 R1**：双轴 fresh 实例并行派发。Standards NOT CLEAN（Established 快照位锁外写入数据竞争=BLOCKING；注释失实/speculative 参数/过滤三处重复/契约注释缺失=SMELL）；Spec NOT CLEAN（SendToClient null 检查先于频道门违反登记优先序=BLOCKER；过期代际先红证据归类异议=GAP）。
- **2026-09-06 修复轮**：Established 置位移锁内（`MarkEstablishedLocked`/`EstablishWithContractLocked`），Connected 仍锁外；SendToClient null 检查移频道门后+红测先红留证；注释修正；`SendTargets` 删 speculative 参数；established 过滤收敛 `EstablishedSnapshot()` 单源（预容量+协变返回）；契约面补冻结注释。修复后全套 7/7 PASS 0 警告，身份刷新 `3e4259e3…073f`。
- **2026-09-06 R2**：双轴全新实例。Standards **CLEAN**（DEFERRABLE 3 并入结单具名延期 6/7/8；越界注记=握手帧锁内发送属 DEV-V2-17）；Spec **BLOCKED**——唯一 BLOCKER=审查链与判词报告归档未完成（output-review-loop 第 4 步记账），Scope 五条+验收①②③内容全部核验通过，R1 GAP-1 经独立核验成立为「回归保持断言」归类（不构成本票 GAP）。
- **2026-09-06 记账收尾 + R3**：结单审查链补录+四份判词归档完成（即 R2-Spec BLOCKER 所指事项），生产代码零改动；R3（全新实例，仅 Spec 轴）复核。
- **2026-09-06 R3 + 实施者裁定 + R4（闭环）**：R3 两层核验——R2 BLOCKER 逐项核实解除、Scope 终审零 GAP/零越界/零错实现；唯一 BLOCKER=R3 判词自身落盘（自指记账）。实施者裁定：判词已返回，落盘=规约第 4 步机械动作（结单 R3 行+R3-spec.md 文末裁定附注为执行凭据），循环派轮将无穷递归非规约意图；沿 DEV-V2-14 先例（裁定+fresh 核验）派 R4 最终核验。**R4（fresh，仅 Spec）：CLEAN**——裁定成立、全链记录真实（五判词与结单逐项对应无美化）、Scope 独立终审零发现。**fresh 链 R1→修复→R2→记账→R3→裁定→R4 闭环，双轴最终 CLEAN，本票 resolved**。候选身份 `3e4259e3…073f`（351232 字节）不继承 14/15 发布批准，RELEASES 换标随实机验收票（DEV-V2-24）或用户实机验收。
