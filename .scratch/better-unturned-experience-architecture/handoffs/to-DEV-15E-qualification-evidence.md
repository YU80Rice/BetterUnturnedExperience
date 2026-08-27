# GPT → Gemini：DEV-15E Qualification Evidence 前端消费复核交接

## 基线

- 工单：`issues/DEV-15E-qualification-evidence.md`
- 实施提交：`45ee80d Implement DEV-15E qualification evidence gate`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 前置：DEV-15A～DEV-15D、DEV-07、DEV-08

## 交付范围

新增 `BetterUnturnedExperience.Release.QualificationEvidenceGate`，只编排既有证据包
校验与资格裁决，不复制资格规则、不写入 FeatureState/发布授权：

- `EvidencePackageInvalid`：包身份、候选、CaseId、路径或 artifact 完整性失败；
- `QualificationIncomplete`：包合法但必需环境缺失、陈旧或失败；
- `TechnicallyQualified`：当前候选下 SP、P2P Host、P2P Client、U3DS Headless 均
  `Fulfilled`；U3DS Client UI 由默认政策 `NotApplicable`；
- `InvalidPolicy`：政策为空，结构化 Fail-Closed，不抛出异常。

DEV-08 校验器的 CaseId 规则已对齐 DEV-07/DEV-15E：同一 CaseId 允许且仅允许
SteamP2PFriends Host + Client；同角色重复或第三角色复用仍返回 `DuplicateCaseId`。

## 请 Gemini 复核

1. `QualificationEvidenceGateResult` 是否足以供前端展示包完整性、五角色 verdict、
   BuildIdentity 与 DLL SHA-256；
2. `TechnicallyQualified` 是否不会被 UI 误显示为发布授权或玩家运行成功；
3. P2P 同 CaseId、同候选、同 DLL hash、时间窗重叠规则是否消费无歧义；
4. `InvalidPolicy`、`EvidencePackageInvalid` 与 `QualificationIncomplete` 是否适合
   统一管理面板的 Fail-Closed 投影；
5. 是否需要 Shared Contract Change Request（预期：不需要，Release 内部 seam）。

## 自动化证据

- Release 编译：0 errors / 0 warnings；
- 7 个测试程序：全部 PASS；Release 测试输出 `DEV-15E qualification evidence tests: PASS`；
- `Verify-NoUiTokens.ps1`：Contracts 2 files PASS、Core 10 files PASS；
- Release DLL 语义引用审计：仅 BCL（`mscorlib`、`System.Core`），无 Unity/Glazier/Sleek/
  LMN/BepInEx/原生程序集引用。

## 严格边界

本交接不代表真实 Unity/Glazier callback、单人游玩、SteamP2PFriends Host/Client、
U3DS Headless、三环境同哈希通过或发布资格。请在真实证据采集后再更新人工门禁。
