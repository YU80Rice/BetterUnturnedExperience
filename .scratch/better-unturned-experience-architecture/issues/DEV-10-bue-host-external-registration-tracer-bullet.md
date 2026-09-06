# DEV-10：BUE Host 与外部功能注册运行时 Tracer Bullet

Type: task
Status: resolved
Author: GPT
Owner: GPT
SourceSet: BUE-SS-20260824-02
Baseline: BUE-V1-RT01-20260824
Depends on: SCR-GPT18-001, DEV-01, DEV-02, DEV-09

## 目标

实现 GPT-18/SCR-GPT18-001 的第一条生产垂直切片：在纯 Contracts/Core seam 上建立 BUE Host 的显式外部功能注册运行时。该切片只证明注册阶段、不可变定义事实、确定性 Catalog 冻结和公开注册结果，不实现第三方完整玩法、BepInEx 外部 DLL、ClientUi satellite、LoadSetIdentity 或三环境运行验收。

## 实施范围

- 将已批准的 registration/presentation 共享类型加入 `ContractTypes.cs`。
- 实现 Core 内部的 `FeatureRegistrationRuntime`，提供 `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady` 阶段机。
- `RegistrationOpen` 只接受显式 `IFeatureRegistration`；不扫描目录、不反射发现、不调用 `Assembly.GetTypes()`。
- 从不可变 `FeatureDefinitionArtifact` 的 FeatureId、Definition digest、Artifact digest 和稳定排序生成只读 Catalog 视图与 CatalogRevision。
- 对重复 FeatureId、非法 artifact、重复登记和关闭阶段返回稳定 reason/DiagnosticId；拒绝不污染既有登记。
- 保持 `FeatureDefinitionArtifact` 与 `IFeatureModuleFactory` 分离；factory 仅在登记成功后保留为运行时输入。
- 加入 `FeaturePresentationState` / `FeaturePresentationView` 的纯值投影。

## 明确不做

- 不实现第三方独立 BepInEx DLL 或 no-op fixture。
- 不实现真实 BUE Host 物理程序集聚合/发布复制。
- 不实现 ClientUi satellite 加载、Glazier/Sleek/Unity 适配。
- 不升级 CandidateBuild 为 LoadSetIdentity。
- 不修改 LMN、U3DS 或 Unturned 原版内容。
- 不修改 DEV-01～DEV-09 的历史验收结论。

## TDD 验收条件

1. Red → Green：`HostStarting` 拒绝注册并返回 `HostUnavailable`。
2. `RegistrationOpen` 接受有效 registration，并返回 artifact 中的 FeatureId。
3. 重复 FeatureId 被确定性拒绝为 `DuplicateFeature`，已接受 registration 不被覆盖。
4. `CatalogFrozen` 后注册返回 `PhaseClosed`；再次冻结/进入 RuntimeReady 不改变 CatalogRevision。
5. 以不同到达顺序登记同一组 registration，冻结后的 FeatureId 顺序与 CatalogRevision 一致。
6. 非法/空 artifact 被拒绝为 `InvalidDefinitionArtifact`，不会生成半成品 catalog。
7. `FeaturePresentationView` 与 FeatureState 分离，satellite 缺失可投影 `PresentationDegraded` 或 `HeadlessOnly`。
8. Contracts/Core 新增代码只依赖 BCL 与 Contracts，不引用 Unity、Glazier、Sleek、Unturned、LMN、BepInEx、Harmony。
9. 全仓 Release 编译 0 errors；测试程序 0 failures；新增静态 UI token 门禁保持通过。
10. 完成独立子智能体审计；审计 PASS 后才可将 DEV-10 标记 `ready-for-human`。

## 证据边界

本工单通过只证明注册运行时静态/单元 seam 成立；不证明外部 BepInEx DLL 已可安装，不证明 U3DS/SteamP2PFriends/SP 运行通过，也不证明任何官方或第三方玩法已完成。

## 验收记录

- GPT 实施：完成。
- Release 编译：PASS，0 errors / 0 warnings。
- 全部现有测试与 DEV-10 注册测试：PASS。
- Contracts/Core UI/native token scan：PASS。
- GPT 独立审计 R1：FAIL；已修复 registration snapshot、锁线性化、getter 异常 fail-closed 与变更后不漂移测试。
- GPT 独立审计 R2/R3：PASS。
- Gemini 前端消费复核：ACCEPT（`DEV-10-Registration-Runtime-Review.md`）。
- 状态更新：`resolved`；本工单纯 C# 注册运行时已闭环，不代表 GPT-18 全量完成或三环境运行通过。

