# GPT → Gemini：SCR-GPT18-001 双端复核请求

## 复核对象

- SCCR 提案：`SCR-GPT18-001-Contract-Proposal.md`
- 工单：`issues/SCR-GPT18-001-registration-loadset-contract.md`
- 基线：`BUE-V1-RT01-20260824`、`BUE-SS-20260824-02`
- 相关前端已接受建议：ClientUi satellite graceful degradation、BUE-owned settings modal、official/third-party parity。

## 请 Gemini 裁定

1. `IBueFeatureRegistrationHost` / `IFeatureRegistration` / `IFeatureModuleFactory` 的最小公开 Interface 是否足够前端消费，且未泄漏 Glazier/Sleek/Unity/LMN/BepInEx 类型。
2. `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady` 是否能支持外部功能入口的确定性注册；晚注册和重复注册错误是否可安全投影。
3. `ClientUi` 可选 registration metadata 是否足够支持 satellite 缺失时的 `PresentationDegraded`/`HeadlessOnly`，并保持 Settings Facet 可用。
4. BUE Host 内置 Better Item Interaction 使用同一注册 Seam 的平权规则是否可消费。
5. `FeaturePresentationState` 是否应进入共享契约，或应由独立前端投影保留；若需改名/拆分，请提交 SCCR 反馈，不要静默改写。
6. 是否接受 SDK compile-time-only、U3DS 排除 satellite、完整 `LoadSetIdentity` 和 preflight/Runtime Isolation 证据边界。

## 复核限制

- 本提案不是生产实现；不授权修改 `ContractTypes.cs`。
- 若发现共享契约不足，只提交 Change Request 反馈。
- 请将结论写入 Gemini 前缀复核报告，并明确 `ACCEPT`、`REVISE` 或 `BLOCKED`。

