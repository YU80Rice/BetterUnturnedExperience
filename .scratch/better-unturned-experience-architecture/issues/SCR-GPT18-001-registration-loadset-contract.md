# SCR-GPT18-001：BUE 外部功能注册、物理 Host 与 LoadSet 契约

Type: task
Status: ready-for-human
Author: GPT
Blocked by: GPT-18, DEV-09

## 目的

为 GPT-18 冻结外部 BepInEx 功能插件接入所需的共享契约和物理部署事实，避免第三方作者自行猜测注册方式、程序集依赖、生命周期时序或证据绑定。

## 必须裁定

1. BUE Host 的用户部署程序集模型：公开 ABI、Contracts/Core 运行时依赖和官方功能的物理归属。
2. 外部功能注册 Interface：唯一注册者、不可变 Definition Artifact、可执行 module factory 的分离、BUE dependency identity、兼容范围、稳定拒绝错误族、调用线程和所有权。
3. 注册阶段：`HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady`，包括早注册排队、晚注册拒绝和 lifecycle generation。
4. ClientUi satellite 的独立部署模型、U3DS 排除规则和无 UI Token 的核心 registration。
5. 官方功能内置与独立 DLL 的统一注册/证据规则，禁止隐藏特权路径。
6. `LoadSetIdentity` 的字段、规范化序列化和 CandidateBuild/Runtime Evidence 绑定方式。
7. SDK 为 compile-time tooling；第三方运行时只依赖 BUE Host public ABI，不依赖 SDK DLL 或未声明 Contracts/Core DLL。
8. 表现降级投影（如 `PresentationDegraded` / `HeadlessOnly`）与 Settings Facet 在 ClientUi 卫星缺失时的继续可用语义。
9. BUE 设置模态窗的 UI 主权，以及第三方自定义 HUD 只能通过 ClientUi satellite registration 接入的规则。
10. 官方 Better Item Interaction 与第三方功能完全相同的公开注册和生命周期路径。

## 不允许

- BUE 扫描 plugins 目录或使用 `Assembly.GetTypes()` 发现功能。
- 恢复运行时 `Describe()`、任意 FeatureId 查询或第二套身份事实源。
- 把 BepInEx/CLR 装载前失败伪装成功能运行时 `Isolated`。
- 用单一 BUE DLL 或单一功能 DLL 哈希代表外部模块完整运行环境。

## 验收条件

- GPT 和 Gemini 双端接受更新后的公开注册契约。
- 干净 BepInEx 安装可由 BUE Host 加载一个 no-op 外部功能 DLL，且无隐式 Contracts/Core 运行时依赖。
- 注册阶段在打乱插件加载顺序时确定性一致；`CatalogFrozen` 后注册被拒绝。
- U3DS profile 不部署 ClientUi satellite，并通过 IL/type-token 门禁与实际加载验证。
- LoadSetIdentity 可拒绝混合 BUE、功能和卫星哈希的证据包。
- ClientUi 卫星缺失时，Settings Facet 仍可用，表现状态投影稳定且不注入原生设置控件。
- 官方 Better Item Interaction 与 no-op 第三方 fixture 通过同一注册 Seam。
- 共享契约变更前不开始第三方功能 DLL 生产接入。

## 参考

- `spec-open-runtime-feature-framework.md`
- `18-Independent-Backend-Review-R1.md`
- `Shared-Contract-Spec.md`
- `Feature-Definition-Pipeline-Spec.md`
- `Contribution-Build-Release-Gates-Spec.md`

## Comments

### 2026-08-25 GPT 领取并形成候选契约

已领取本票并完成当前 Contracts/Core、Definition Linker、CandidateBuild、DEV-09 Plugin Entry 与 GPT-18/前端复核事实核对。

候选提案：[SCR-GPT18-001-Contract-Proposal.md](../SCR-GPT18-001-Contract-Proposal.md)

交给 Gemini 的双端复核：[to-SCR-GPT18-001-review.md](../handoffs/to-SCR-GPT18-001-review.md)

本轮只形成共享契约候选，不修改 `ContractTypes.cs`，不修改 DEV-01～DEV-09，不构建 no-op fixture，不修改 LMN。

独立审计：`PASS`（候选提案审计通过，等待 Gemini/人工双端复核，不代表契约已冻结）。

审计报告：[SCR-GPT18-001-Independent-Audit-R1.md](../../../audit/2026-08-25/SCR-GPT18-001-Independent-Audit-R1.md)


