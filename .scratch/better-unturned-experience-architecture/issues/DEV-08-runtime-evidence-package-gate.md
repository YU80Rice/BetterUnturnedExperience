# DEV-08：Runtime Evidence Package + Three-Environment Gate

Status: ready-for-human
Owner: GPT（总维护者/后端与发布门禁）
Required reviewer: Gemini（前端消费与验收）
Human approval: 真实证据齐全且独立审计通过后仍需人工批准
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Depends on: DEV-01、DEV-02、DEV-03、DEV-04、DEV-05、DEV-06、DEV-07

## 目标

为 DEV-07 建立不可变、可审计的运行证据包 seam：证据包必须绑定一个
CandidateBuild、DLL SHA-256、唯一 CaseId 集合和受控的相对证据路径；导入校验只验证
材料完整性与身份绑定，不把静态构建、单一环境日志或证据包导入自动提升为 ReleaseReady。

## 实施范围

- `RuntimeEvidenceArtifact`：安全相对路径、长度、SHA-256 与证据类型。
- `RuntimeEvidencePackage`：包身份、CandidateBuild、采集者、UTC 创建时间、案例与工件集合。
- `RuntimeEvidencePackageValidator`：Candidate/DLL/CaseId/路径引用/摘要一致性校验。
- 与既有 `QualificationEvaluator` 的窄连接：验证通过后才允许调用资格裁决；不复制资格规则。
- TDD 公共 seam 测试、确定性 canonical 序列化和诊断排序。

## 不在本票

- 不自动启动或控制 Unturned、SteamP2PFriends 或 U3DS。
- 不伪造、推断或替代玩家可观察的运行证据。
- 不修改 LMN、U3DS、原版库存 RPC 或任何游戏客户端文件。
- 不新增共享 Contracts 字段；若发现契约不足，只提交 Change Request。
- 不声明 ReleaseReady、Stable、1.0.0 或正式发布授权。

## 验收条件

- [x] TDD 先有公共 seam 红测，再以最小实现逐条转绿。
- [x] 空包、错误 CandidateBuild、DLL hash 不匹配、重复 CaseId、缺失 artifact 引用均拒绝。
- [x] 工件路径拒绝绝对路径、目录穿越、空路径和重复 canonical 路径。
- [x] 证据摘要必须为 64 位 ASCII 十六进制；包与案例 canonical 序列化确定且排序稳定。
- [x] 验证通过只表示 `EvidencePackageValid`，不改变 `QualificationVerdict`、FeatureState 或发布授权。
- [ ] 真实 SP/P2P Host/Client/U3DS 运行证据仍需人工采集并单独绑定同一 CandidateBuild/DLL。

## 2026-09-03 V1 闭环收尾（待人工批准）

- 本票机制（证据包校验、绑定、资格裁决）已由后续工单实质履行：DEV-15E 自动化证据门禁（resolved）、DEV-16E 三环境证据采集与人工批准（`audit/2026-09-02/DEV-16E-*`）、DEV-16F 三环境人工验收、DEV-16G 实机验收。
- 证据义务被 DEV-16E/F 后续 CandidateBuild/CaseId 覆盖，本票不再另起证据采集。
- **待人工批准**：批准后标记 `resolved`，并入 V1 冻结快照。

## Verification

- [x] Release 0 errors / 0 warnings。
- [x] DEV-02～DEV-08 全套测试 PASS。
- [x] Release/Transport/Core/Contracts 类型隔离与依赖方向扫描 PASS。
- [x] 独立子智能体审计 R1 阻断修复，R2 PASS。
- [x] Gemini 前端消费复核 `ACCEPT`；工单保持 `ready-for-human`，等待人工采集真实三环境证据。

## 证据边界

证据包是归档与绑定容器，不是运行时事实源。每次 DLL/source 变化必须产生新的
CandidateBuild、CaseId 与证据包；旧包只能被判定为 Stale/Mismatched，禁止继承。
