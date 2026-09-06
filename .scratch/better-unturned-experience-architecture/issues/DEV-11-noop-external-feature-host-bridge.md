# DEV-11：No-op 外部功能 Fixture 与 BUE Host 注册桥

Type: task
Status: resolved
Author: GPT
Owner: GPT
SourceSet: BUE-SS-20260824-02
Baseline: BUE-V1-RT01-20260824
Depends on: DEV-10, SCR-GPT18-001

## 目标

证明独立 BepInEx 功能 DLL 可以仅通过 BUE 公开 Host registration seam 登记，不修改 BUE 源码、不使用目录扫描、不恢复反射发现、不直接调用 `IFeatureModule.Start`。本票只实现 no-op 外部功能夹具与 Host 桥，不实现 Better Item Interaction 玩法。

## 实施范围

- 在 BUE Plugin 中提供公开的、窄的 `BueRuntimeHost` 注册桥；BUE Plugin `Awake` 初始化唯一 Host 实例并打开 Registration phase。
- 外部 no-op Fixture 作为独立程序集/独立 BepInEx plugin entry，声明 BUE plugin dependency，并在 `Awake` 仅调用 `BueRuntimeHost.Register`。
- Fixture 使用独立 FeatureId、不可变 Definition Artifact 与最小 ModuleFactory；不依赖 BUE 私有 Core 类型。
- 提供测试夹具以验证 Host 已初始化时注册成功、Host 缺失时稳定返回 `HostUnavailable`、重复 FeatureId 被拒绝、冻结后不允许晚注册。
- 保持 ClientUi satellite、LoadSetIdentity、真实 BepInEx clean-install、U3DS/SP/P2P 运行验证留在后续工单。

## 明确不做

- 不实现真实第三方玩法和 Better Item Interaction 迁移。
- 不实现 BUE Host 单 DLL 聚合、发布复制或 SDK 包。
- 不实现真实 BepInEx Chainloader/clean-install 运行证据。
- 不引入 Assembly.GetTypes、全目录 DLL 扫描、全局 PatchAll 或 LMN 修改。
- 不宣称三环境运行或发布资格。

## TDD 验收条件

1. Red → Green：Host 未初始化时外部注册桥返回 `HostUnavailable`，不抛异常。
2. BUE Host 初始化后，独立 no-op Fixture 通过公开桥注册成功。
3. Fixture DLL 的编译引用仅依赖 BUE public Host ABI 与 BepInEx 基础入口，不引用 BUE 私有 Core namespace。
4. BUE Host 的 registration phase 与 DEV-10 一致；冻结后外部晚注册稳定返回 `PhaseClosed`。
5. no-op Fixture 失败不会污染 Host 既有 Catalog，且不影响原有功能。
6. 全仓 Release 编译 0 errors / 0 warnings；全部测试 PASS。
7. 完成 Contracts/Core/Release 类型隔离与 Fixture 引用闭包静态检查。
8. 独立子智能体审计 PASS 后工单才可 `ready-for-human`；真实 clean-install 和三环境证据仍不得宣称。

## 验收记录

- GPT 实施：完成。
- R1 审计：FAIL，阻断 B-01：Fixture 入口未覆盖真实 `Awake` 生命周期。
- 修复：`NoOpFeaturePlugin.Awake → RegisterWithBue → BueRuntimeHost.Register`，并加入 `NoOpFeatureBootstrap.Awake` 测试 seam。
- 最终 Release 编译：PASS，0 errors / 0 warnings。
- 7 个测试项目：全部 PASS。
- Contracts/Core 隔离与 Fixture AssemblyRef/Discovery scan：PASS。
- R2 独立审计：PASS。
- Gemini 前端消费复核：ACCEPT（`DEV-11-NoOp-Fixture-Review.md`）。
- 状态更新：`resolved`；外部 Host 桥接与 No-op 夹具已验收闭环。

