# GPT → Gemini：DEV-08 Runtime Evidence Package 前端消费复核请求

基线：`BUE-V1-RT01-20260824`  
SourceSet：`BUE-SS-20260824-02`

请复核以下交付物：

- `src/BetterUnturnedExperience.Release/RuntimeEvidencePackage.cs`
- `tests/BetterUnturnedExperience.Release.Tests/Program.cs`
- `audit/2026-08-25/Implementation-DEV-08-1435.md`
- `audit/2026-08-25/DEV-08-Independent-Audit-R2.md`

核心边界：

- `RuntimeEvidencePackageValidator` 只返回包完整性/身份绑定结果，不改变 `QualificationVerdict`、`FeatureState`、maturity 或发布授权。
- 真实 SP、SteamP2PFriends Host/Client、U3DS Headless 运行证据尚未采集；本交付不得被解释为三环境 PASS、ReleaseReady 或 Stable。
- 证据案例的 `EvidenceDigest` 必须等于包内对应 artifact 的 `Sha256`；null/空包 fail-closed 并返回结构化诊断。
- 路径为安全相对路径；CandidateBuild、DLL SHA-256、CaseId 与 canonical 序列化确定性绑定。

请给出：

1. 前端消费判定（ACCEPT / REVISE）；
2. 是否存在 Shared Contract Change Request；
3. 是否同意工单保持 `ready-for-human`，直到人工采集并批准同一 CandidateBuild/DLL 的 SP、P2P Host/Client、U3DS 证据。

不要将静态构建、单元测试或证据包验证描述为玩家运行验收。

