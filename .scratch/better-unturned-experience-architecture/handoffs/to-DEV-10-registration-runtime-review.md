# GPT → Gemini：DEV-10 注册运行时交接复核

## 复核请求

请以 GPT-18、SCR-GPT18-001、`BUE-V1-RT01-20260824` 和 `BUE-SS-20260824-02` 为基线，复核 DEV-10 的 Contracts/Core 注册 tracer bullet。请只审查公开注册与 Catalog seam，不把本票扩大为第三方 DLL 或三环境运行验收。

## 交付物

- 工单：`issues/DEV-10-bue-host-external-registration-tracer-bullet.md`
- 实施报告：`audit/2026-08-25/Implementation-DEV10-1638.md`
- 审计 R1：`audit/2026-08-25/DEV-10-Independent-Audit-R1.md`
- 审计 R2：`audit/2026-08-25/DEV-10-Independent-Audit-R2.md`
- 审计 R3：`audit/2026-08-25/DEV-10-Independent-Audit-R3.md`
- 共享契约：`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`
- 注册实现：`src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs`

## 核心实现

- `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady` 阶段机；CoreSafeMode 只做 fail-closed 拒绝。
- `IFeatureRegistration` 只在 `RegistrationOpen` 接收；重复 FeatureId、非法 artifact、坏 factory、坏 satellite metadata、版本不兼容均返回稳定 reason/DiagnosticId。
- `FeatureDefinitionArtifact` 复制 canonical payload；登记成功时建立内部 `FeatureRegistrationSnapshot`，冻结只消费 snapshot，避免外部 registration 改写导致身份漂移。
- Catalog 按 FeatureId / DefinitionSetDigest / ArtifactPayloadDigest 稳定排序，`CatalogRevision` 与到达顺序无关。
- `FeaturePresentationView` 与 `FeatureState` 分离；无 satellite 只影响 presentation，不改变核心登记。
- Contracts/Core 未引用 Unity、Glazier、Sleek、Unturned、LMN、BepInEx 或 Harmony 类型。

## 审计与构建

- Release solution：0 errors / 0 warnings。
- 全部测试程序：PASS，退出码 0。
- Contracts/Core UI/native token scan：PASS。
- R1 的 B-01 已修复；R2/R3 独立审计 PASS。

## 前端复核问题

1. 是否接受 `FeaturePresentationState` / `FeaturePresentationView` 与 9 态 `FeatureState` 并列消费？
2. 是否接受无 ClientUi satellite 时只投影 `PresentationDegraded`，而不阻断 Core 注册与 Settings Facet 后续消费？
3. 是否接受官方与第三方共用同一个注册 Host seam（本票未实现官方具体功能）？
4. 是否确认本票未宣称第三方 DLL、U3DS/SP/P2P 或发布资格？

请如发现新契约缺口，提交 Shared Contract Change Request；不要直接修改 `ContractTypes.cs`。

