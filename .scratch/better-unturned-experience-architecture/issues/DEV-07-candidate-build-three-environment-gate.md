# DEV-07：CandidateBuild + 三环境同哈希验收与发布门禁

Status: ready-for-human
Owner: GPT（总维护者/后端与发布门禁）
Required reviewer: Gemini（前端消费与验收）
Human approval: 必须在 `ready-for-human` 后明确批准
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Depends on: DEV-01、DEV-02、DEV-03、DEV-04、DEV-05、DEV-06

## 目标

建立首个可追溯 CandidateBuild，并针对同一 CandidateBuild/DLL SHA-256 完成单人、SteamP2PFriends Host/Client 与 U3DS Headless 的独立资格证据收集。实现发布门禁的证据绑定与 stale/mismatch 拒绝；不把构建成功、静态扫描或单一环境结果冒充整体运行通过。

## 实施范围

- CandidateBuild/BuildIdentity、DefinitionSetDigest、ArtifactPayloadDigest、工具链与双 reference-set 身份绑定。
- 确定性 Release 构建与 DLL SHA-256 清单。
- 证据案例 schema：CaseId、CandidateBuild、角色、环境指纹、版本、路径、时间窗、原始日志与哈希。
- Qualification Evaluation：`Fulfilled / Failed / Missing / Stale / NotApplicable`。
- 单人、SteamP2PFriends Host/Client、U3DS Headless 的运行验收编排与报告。
- 前端/后端共同功能的最小冒烟路径：加载、设置快照、Better Item Interaction 入口/预览、原生投影边界。

## 不在本票

- 不修改 LMN、SteamP2PFriends 或 U3DS 原版内容。
- 不把 LMN 具体协议提升为 BUE Contracts。
- 不进行逆向或滥用。
- 不在真实运行证据缺失时声明 Stable、1.0.0、ReleaseReady 或 GitHub 发布。

## 验收条件

- [x] 先以 TDD 建立 CandidateBuild/evidence/evaluation seam 的红测，再实现最小生产实现。
- [x] 两次干净构建产生相同 CandidateBuild/BuildIdentity 与 DLL SHA-256；不忽略真实字节差异。
- [x] 最终 DLL、DefinitionSetDigest、ArtifactPayloadDigest、工具链、客户端/U3DS reference-set 身份可追溯绑定。
- [x] 证据案例拒绝缺失 CaseId、角色、时间窗、DLL hash 或候选身份；不同 hash 自动标记 `Stale/Mismatched`。
- [ ] 单人运行证据独立记录并通过其义务。
- [ ] SteamP2PFriends Host 与 Client 使用同一 CaseId、同一 DLL SHA-256，并分别提供日志/指纹。
- [ ] U3DS Headless 实际加载 CandidateBuild，日志无 UI/native TypeLoad/初始化崩溃；UI NotApplicable 不得绕过 Core 安全门禁。
- [x] 运行证据仅更新 Qualification Evaluation，不修改 FeatureState、maturity 或自动授予发布权。
- [ ] Release 资格仍需共享契约复核与人工批准；本票独立审计 PASS 后才可交 Gemini。
- [ ] 真实三环境运行 PASS 前，不得宣称插件整体可发布。

## Verification

- [x] Release 0 errors / 0 warnings。
- [x] 全套 DEV-02～DEV-07 测试 PASS。
- [x] Core/Contracts/Transport/Plugin 静态隔离与依赖扫描 PASS。
- [x] CandidateBuild/证据/evaluation 单元与序列化测试 PASS。
- [x] 独立子智能体 Round 2 审计 PASS。
- [x] Gemini 前端消费复核 `ACCEPT`。
- [ ] 人工批准后才可标记 `resolved`；若三环境证据未齐，仅保持 `ready-for-human` 或 `in-progress`。

## 2026-09-03 V1 闭环收尾（待人工批准）

- 本票机制（CandidateBuild + 三环境资格门禁）已由后续工单实质履行：DEV-16E 采集并人工批准了单人 + SteamP2P Host/Client + U3DS Headless 三环境证据（`audit/2026-09-02/DEV-16E-*`），DEV-16F 三环境人工验收（`UMM-诊断包_20260902_20*`），DEV-16G 实机验收（`UMM-诊断包_20260903_*`）。
- 证据义务被 DEV-16E/F 后续 CandidateBuild/CaseId 覆盖，本票不再另起证据采集。
- **待人工批准**：批准后标记 `resolved`，并入 V1 冻结快照。

## 证据边界

构建、哈希、静态扫描和单元测试不证明单人、P2P 或 U3DS 运行。每次 DLL/source 变化必须产生新的 CandidateBuild、CaseId 与三环境证据，旧证据不得继承。
