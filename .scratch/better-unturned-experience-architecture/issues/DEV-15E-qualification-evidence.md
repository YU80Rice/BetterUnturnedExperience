# DEV-15E：Better Item Interaction Qualification Evidence

Type: task
Status: resolved
Owner: GPT（后端开发与项目维护）
Required reviewer: Gemini（前端消费与验收）
Human approval: 真实运行证据采集完成后由人工开发者批准
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Depends on: DEV-15A、DEV-15B、DEV-15C、DEV-15D、DEV-07、DEV-08

## 目标

建立 DEV-15 的资格证据编排 seam：对一个明确的 `CandidateBuildDescriptor` 与
`RuntimeEvidencePackage` 执行“包完整性 → 同候选资格裁决 → 三环境必需义务”闭环，
输出可审计的技术资格结果。该 seam 只裁决技术资格，不自动授予发布授权、Stable、
ReleaseReady 或玩家运行成功。

## 实施范围

- 新增 `QualificationEvidenceGate`，组合既有 `RuntimeEvidencePackageValidator` 与
  `QualificationEvaluator`，不复制其内部规则；
- 输出不可变的包校验结果、各环境 `QualificationVerdict`、候选 `BuildIdentity` 与 DLL
  SHA-256 绑定信息；
- 明确 `EvidencePackageInvalid`、`QualificationIncomplete`、`TechnicallyQualified`
  三类门禁状态；所有异常与缺证据路径 Fail-Closed；
- 默认政策必须覆盖单人、SteamP2PFriends Host、SteamP2PFriends Client、U3DS Headless，
  U3DS Client UI 可为 `NotApplicable`；
- 增加确定性、同哈希、P2P 同 CaseId/重叠时间窗、缺证据、陈旧证据和坏包测试；
- 生成 Gemini 前端消费交接文档与 GPT 独立审计报告。

## 不在本票

- 不启动或控制 Unturned、SteamP2PFriends 或 U3DS；
- 不伪造、推断或替代人工运行证据；
- 不修改 LMN、U3DS、BepInEx、Unity、原生库存 RPC 或共享 Contracts；
- 不把静态测试、DLL 构建、日志无报错或单一环境结果解释为三环境通过；
- 不实现发布授权、版本发布或 ReleaseReady 状态写入。

## 验收条件

- [x] TDD：每个公开门禁 seam 先有失败测试，再以最小实现转绿；
- [x] 包无效、候选不匹配、空/缺失案例、坏哈希、重复 CaseId、缺失 artifact 引用均拒绝；
- [x] 同一候选 DLL hash 的 SP、P2P Host/Client、U3DS Headless 全 Fulfilled 时才输出
      `TechnicallyQualified`；P2P 必须同 CaseId 且 UTC 时间窗重叠；
- [x] 缺任一必需环境、Stale 或 Failed 时输出 `QualificationIncomplete`；
- [x] 技术资格结果不改变 `FeatureState`、成熟度或发布授权；
- [x] Release 编译 0 errors / 0 warnings；DEV-15A～DEV-15D 与既有 Release 测试全部 PASS；
- [x] 独立审计首轮发现的 null policy 阻断已修复，并重新编译/测试；复审 PASS；
- [x] Gemini 前端消费复核 ACCEPT（`DEV-15E-Qualification-Evidence-Review.md`），正式关闭本票；
- [ ] 真实 SP、SteamP2PFriends Host/Client、U3DS 运行证据仍由人工采集，并绑定本票
      实际 Candidate/DLL SHA-256。

## 证据边界

本票通过仅证明“资格门禁引擎可正确裁决输入证据”。在人工采集同一候选哈希的
真实四角色证据前，不得宣称 Better Item Interaction 已完成、三环境通过或具备发布资格。

## Comments

- 已领取，开始按 TDD Red → Green 实施。
- 组合器实现：`src/BetterUnturnedExperience.Release/QualificationEvidenceGate.cs`。
- DEV-08 校验器修正：同一 CaseId 允许且仅允许 SteamP2P Host + Client 配对；同角色重复仍拒绝。
- 当前静态/自动化结果 PASS，但真实运行证据仍未采集，工单保持 `ready-for-human`。

## Answer

DEV-15E 自动化资格门禁已实现并通过 GPT 独立复审：`QualificationEvidenceGate` 先验证
证据包，再调用既有 `QualificationEvaluator`；只有同一候选 DLL SHA-256 下的单人、
SteamP2PFriends Host/Client（同 CaseId 且时间窗重叠）与 U3DS Headless 全部
`Fulfilled` 时才输出 `TechnicallyQualified`。坏包、缺证据、陈旧证据、无效政策均
Fail-Closed。当前票据保持 `ready-for-human`，等待人工采集真实四角色证据和 Gemini
前端消费复核；本票不构成三环境运行通过或发布授权。

