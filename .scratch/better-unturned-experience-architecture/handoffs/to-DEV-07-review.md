# GPT → Gemini：DEV-07 CandidateBuild/Qualification 前端消费复核请求

## 交付物

- 实施报告：`audit/2026-08-25/Implementation-DEV-07-1340.md`
- 独立终审：`audit/2026-08-25/DEV-07-Independent-Audit-R2.md`
- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-07-candidate-build-three-environment-gate.md`
- CandidateBuild DLL：`7AFAECF1C569AA57150E7CE113810E18271AE6DF9850D7C4682779DB1336ED7A`
- Release.Tests EXE：`1D24CEB43609B73CF344FF240EA9C9DDEAEDFBB3CEE125455FA17441437A6A60`

## 请复核

1. `BuildIdentity` 是否只代表 source/definition/artifact/toolchain/reference-set 身份，且不把 DLL hash、运行状态或发布授权混入。
2. `EvidenceCase` 是否足以让前端区分 CandidateBuild、DLL hash、环境角色、版本、部署来源、UTC 时间窗、证据引用与诊断摘要。
3. `QualificationPolicy` 的 `NotApplicable` 是否只来自政策；U3DS Headless 是否始终为必需 Core 义务，只有 U3DS Client UI 可按政策 NotApplicable。
4. `QualificationResult` 的 `Missing/Stale/Failed/Fulfilled` 是否不会被前端误显示为“插件已发布”或“运行成功”。
5. SteamP2PFriends Host/Client 是否必须同 CaseId、同 DLL hash、同候选身份且时间窗重叠。
6. 是否与 DEV-03/DEV-05/DEV-06 的设置、生命周期、网络与 Headless 消费边界一致。

## 明确证据边界

本次仅完成 CandidateBuild/证据/资格静态模块、构建与单元测试；尚无真实 SP、SteamP2PFriends Host/Client、U3DS Headless 运行证据，不得将本交付表述为 ReleaseReady、Stable、整体可发布或三环境 PASS。

如发现契约不足，请提交 Shared Contract Change Request，不要在前端创建平行资格状态。

