# DEV-V2-14 · R2' Spec 轴报告（fresh-instance 验证轮）

- 轮次：R2'（R2 因继承 R1 上下文依 output-review-loop Fresh-instance 规则 425c2aa 作废后，全新实例重派）
- 审查轴：Spec-Reviewer（全新子代理实例，非 SendMessage 续用）
- 审查对象：`git diff fef9738..48937d2`
- 日期：2026-09-06
- 判定：**VERDICT: BLOCKED**（3×BLOCKER；实施者裁定见文末附注——三项均属 DEV-V2-16/17/18 票面 Scope，越界发现，移交 fresh R2'' 独立核验裁定）

---

本实例为全新上下文,结论独立推导

- [BLOCKER] **未按规格将线帧魔数从 BUE2 改为 BUE1。** Spec 原文："魔数常量 BUE2 → BUE1（4 字节帧头布局不变）"；且 T3 红测面明确要求"魔数 BUE1 帧编解码回归"。但增量文件 `src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:34` 仍为 `private const string FrameMagic = "BUE2";`，注释 `:23` 也保留 BUE2。该项属于本规格明确冻结面，非 DEV-V2-18 才开始的生产绑定实现。

- [BLOCKER] **公开 `Sessions` 未收窄为 established-only。** Spec 原文："公开会话集合只含已建立会话"，停用语义进一步要求"`Sessions` 为空快照"。实现 `src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:101-103` 直接返回 `sessions.Values`；`StartSession` 在握手完成前即写入 `sessions`（`:217-222`），因此 pending 会话会暴露给功能。现有结单所称"会话快照 established-only"与代码不符，属于错误实现。

- [BLOCKER] **Ack 匹配仍采用"第一个未建立会话"，违反 peer + 代际/nonce 语义。** Spec 原文："Ack 按 peer + 代际 / nonce 匹配（废弃「第一个未建立会话」）"。实现 `src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:311-315` 明确遍历并选取第一个 `PeerSteamId` 相同且未建立的会话，未校验代际或 nonce；代码注释也写明 `match the FIRST un-established session`。并发/重复握手下可能错误建立会话。

- [DEFERRABLE] **`IFeatureBootstrap.Network` 尚未接入真实功能启动路径。** Spec 原文："生命周期绑 `IFeatureBootstrap` / `IFeatureLifetime`"；票面要求"从功能启动参数获得永非 null 的 `IBueNetworkApi`"。本增量仅新增组合记录 `src/BetterUnturnedExperience.Core/Registration/FeatureBootstrap.cs:19-42`，未发现生产 Start 路径构造/注入。结单已具名归入 DEV-V2-21/22，且本票验收的假 adapter 注入与非空契约面已覆盖，故按明确延期处理，不计当前 BLOCKER。

- [INFO] **红测证据可追。** `audit/2026-09-06/DEV-V2-14/` 含编译红、桩阶段、修复阶段及 7 份绿测日志；红态逐字 transcript 已在结单报告 §"红绿链"引用，身份文件也存在。证据归档项本身通过。

VERDICT: BLOCKED

---

## 实施者裁定（附注，非审查者原文；提交 fresh R2'' 独立核验）

三项 BLOCKER 经对照工单拆分核实，均为下游票 Scope 原文明载的义务，非本票（DEV-V2-14）验收面：

1. **魔数 BUE2→BUE1 + 全仓 BUE2 清扫**：DEV-V2-18 票面 Scope 第 19 行逐字：「魔数 BUE2 → BUE1（4 字节帧头布局不变）；全仓『BUE2』字样清扫（常量、注释、测试、文档）」（票题即含「魔数 BUE1」；spec 落地顺序将其列于最后一步「帧消费/接管拆分」）。DEV-V2-14 票面 Scope/验收条件不含魔数项。
2. **Sessions 收窄 established**：DEV-V2-16 票面 Scope 逐字：「`Sessions` 语义收窄为 established 快照（**登记条目③**）；pending 会话仅内部可见」。SDK 文档契约版本演化节亦明载「后续票逐条追加（…④ Sessions 收窄 established…）」——即本票登记面明确不含④。本票停用语义义务「Sessions 空快照」由 SetModuleActive 清会话满足（停用时无 pending 可暴露）。
3. **Ack 按 peer+代际/nonce 匹配**：DEV-V2-17 票面 Scope 逐字：「会话匹配：Ack 按 peer + 代际 / nonce 匹配（**废弃「第一个未建立会话」匹配法**）」。

另核：审查者引「现有结单所称『会话快照 established-only』与代码不符」——全仓 grep 显示结单报告/票面/SDK 文档均无 established-only 表述（唯一 established 字样即上述「后续票追加④」行），该子项引证有误；核心观察（pending 会话在启用态可见）属实但归 16/17。

裁定处置：三项不构成本票缺陷，不作代码变更（增量保持 48937d2，候选身份 8d044c7a…2f7b 不变）；按裁定传达第 6 条「BLOCKED → 修复轮流程」执行为**审查合同修复**（scope brief 校正）+ 派全新 Spec R2'' 独立核验本裁定（核对 16/17/18 票面 Scope 行与本票 Scope/验收条件），Standards 轴 R2' CLEAN 对同一 diff 继续有效。
