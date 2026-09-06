# DEV-V2-14 · R2'' Spec 轴报告（fresh-instance 核验轮：实施者越界裁定独立核验）

- 轮次：R2''（R2' Spec 轴 BLOCKED 的三项 BLOCKER 经实施者裁定为越界发现后，按修复轮流程派全新实例核验裁定）
- 审查轴：Spec-Reviewer（全新子代理实例，非 SendMessage 续用）
- 审查对象：`git diff fef9738..48937d2`（与 R2' 同一增量，无代码变更）
- 日期：2026-09-06
- 判定：**VERDICT: CLEAN**（第一层：三项越界裁定逐行核实成立；第二层：本票 Scope 内重审无 BLOCKER）

---

本实例为全新上下文,结论独立推导

## 第一层：实施者越界裁定核验

1. **[INFO] 魔数项裁定成立。**
   DEV-V2-18 明确要求"魔数 BUE2 → BUE1"及"全仓『BUE2』字样清扫"
   (`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-18-bue-frame-binding-takeover-split-bue1.md:19`)。DEV-V2-14 的 Scope/验收条件仅覆盖订阅、Network 注入、停用语义及契约升级，无魔数要求
   (`...DEV-V2-14-inbound-subscribe-network-injection.md:15-26`)。规格落地顺序也将"帧消费/接管拆分"置于最后
   (`...spec.md:131-135`)，且 DEV-V2-18 blocked by DEV-V2-17
   (`...DEV-V2-18...md:6`)。裁定忠实。

2. **[INFO] Sessions 收窄裁定成立。**
   DEV-V2-16 逐字要求"`Sessions` 语义收窄为 established 快照；pending 会话仅内部可见"
   (`...DEV-V2-16-session-driven-multicast-send-results.md:17`)；SDK 文档把"④ `Sessions` 收窄 established"列为后续票追加项
   (`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:69`)。DEV-V2-14 仅要求停用时 `Sessions` 空快照
   (`...DEV-V2-14...md:18`)。裁定成立。

3. **[INFO] Ack 匹配裁定成立。**
   DEV-V2-17 明确要求"Ack 按 peer + 代际 / nonce 匹配（废弃'第一个未建立会话'匹配法）"
   (`...DEV-V2-17-auto-handshake-session-lifecycle.md:17`)，并 blocked by DEV-V2-16
   (`...DEV-V2-17...md:6`)。该项属于后续自动握手票。

   结单报告、DEV-V2-14 票面及 SDK 文档中均未出现"会话快照 established-only"这一表述；该引证不成立，但对应代码观察确实属于下游 Sessions/握手范围。

## 第二层：DEV-V2-14 正确 Scope 重审

- **[INFO] 契约①实现匹配。** `ChannelDirection : byte`、三参 `Subscribe`、独立句柄、方向双表、参数 fail-fast、锁外派发、异常隔离及未注册频道订阅均有实现与锚点测试
  (`src/BetterUnturnedExperience.Contracts/ContractTypes.cs:25,300`; `src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:146-173,340-377`)。
- **[INFO] 停用语义匹配。** `SetModuleActive` 清空会话、解绑接收、保留频道/订阅表；发送返回既有 `NoSession`；重新启用复用同一订阅表
  (`...BueNetworkRuntime.cs:184-200`)。
- **[INFO] 契约②及 Major 升级匹配。** `Network` 非空 fail-fast 且实例保持；门槛为 2；生产注册点均为 `(2,0)`，未见旧两参 Subscribe 或 `(1,0)` 残留
  (`...FeatureBootstrap.cs:360-381`; `...FeatureRegistrationRuntime.cs:44`; `...OfficialFeatureRegistration.cs:31,39`; `...NetworkModuleFeatureRegistration.cs:149,158`; `...NoOpFeaturePlugin.cs:35`)。
- **[DEFERRABLE]** `Network` 尚未接入真实生产启动路径；结单明确登记为 DEV-V2-21/22 延期
  (`...audit/2026-09-06/DEV-V2-14/DEV-V2-14-closing-report.md:48`)。功能停止后的自动句柄失效同样采用票面允许的"可安全重复释放"分支
  (`...DEV-V2-14...md:15`; `...BueNetworkRuntime.cs:165-173`)。

因此，三项 R2' BLOCKER 均为越界发现；本票 Scope 内无 BLOCKER。规约要求的 Fresh-instance 双轴流程见
`docs/agents/output-review-loop.md:10-18`。

VERDICT: CLEAN
