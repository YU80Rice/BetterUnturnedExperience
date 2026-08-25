# DEV-13：独立 No-op Feature 注册运行时与 Catalog Barrier 冒烟

Type: task
Status: ready-for-human
Owner: GPT（总维护者/后端运行时）
Required reviewer: Gemini（前端消费、公开 ABI 与 Headless 边界）
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Depends on: DEV-10、DEV-11、DEV-12、SCR-GPT18-001、GPT-18

## 目标

在 DEV-12 单 DLL Host 已通过真实客户端启动冒烟的基础上，完成一个独立 `BetterUnturnedExperience.NoOpFixture.dll` 的公开 ABI 运行时 tracer bullet：BepInEx 依赖顺序先启动 BUE Host，随后 No-op 插件在 `RegistrationOpen` 阶段显式注册；所有插件 `Awake` 完成后由 BUE Host 在生命周期 barrier 处冻结 Catalog 并进入 `RuntimeReady`。

本票只验证公开注册与统一 Catalog 运行时 Seam，不实现 Better Item Interaction、Glazier UI、LMN 或第三方真实玩法。

## 实施范围

- BUE Host `Awake → RegistrationOpen`、Unity `Start → CatalogFrozen → RuntimeReady` 的确定性生命周期闭环。
- No-op Fixture 继续通过唯一公开 `BueRuntimeHost.Register(IFeatureRegistration)` 注册，不扫描目录、不反射发现、不直接调用私有 Core 类型。
- 保持 BUE 主 DLL 为唯一运行时 ABI；No-op Fixture 不引用 `BetterUnturnedExperience.Core.dll` 或 `BetterUnturnedExperience.Contracts.dll`。
- 增加纯逻辑生命周期测试、公开 ABI/AssemblyRef 回归测试和可部署的 BUE + No-op staging 资产。

## 明确不做

- 不实现或迁移 Better Item Interaction。
- 不修改 LMN、BepInEx、U3DS 或 Unturned 原版内容。
- 不引入自动目录扫描、`Assembly.GetTypes()`、类名猜测、全局 `PatchAll()` 或运行时动态 DLL 加载。
- 不宣称 U3DS、单人、SteamP2PFriends Host/Client、ClientUi Satellite 或三环境功能验收通过。

## TDD 验收条件

1. Red：生命周期测试在 Host 只打开 `RegistrationOpen`、未执行 barrier 时不得声称 `RuntimeReady`；Green：BUE Host 的实际入口在所有依赖插件 `Awake` 后执行 barrier，进入 `CatalogFrozen → RuntimeReady`。
2. No-op Fixture 在 BUE Host 可用时注册成功；在 Host 不可用或 barrier 关闭后 fail-closed 返回结构化 reason/DiagnosticId。
3. 乱序构造多个注册输入时 Catalog 顺序与 revision 确定；重复 FeatureId、无效 Definition、晚注册均拒绝。
4. 主 BUE DLL 与 No-op Fixture 的 AssemblyRef/类型身份唯一；运行时不要求 Core/Contracts DLL。
5. Release 构建 0 errors / 0 warnings；现有全部测试与新增 DEV-13 测试 PASS。
6. 生成 BUE + No-op staging 目录、DLL 哈希和交给 Gemini 的复核简报；真实客户端部署验证前工单保持 `ready-for-human`。

## 运行证据边界

本票的静态、单元和 staging 证据不替代真实客户端双 DLL BepInEx 冒烟；人工运行后才可关闭本票。即使运行通过，也不代表 U3DS、SP/P2P 或 Better Item Interaction 功能通过。

## GPT 实施进度（2026-08-25）

- TDD Red：`CompleteRuntime()` 缺失测试稳定编译失败。
- TDD Green：新增 Host-owned 原子 barrier；BUE Plugin `Start()` 在依赖插件 `Awake` 完成后推进 `RuntimeReady`。
- Release：0 errors / 0 warnings；7 项测试全部 PASS。
- staging 产物：`artifacts/DEV-13-noop-registration-20260825/`。
- 实施报告：`audit/2026-08-25/Implementation-DEV13-RegistrationBarrier-1925.md`。
- Gemini 复核交接：`handoffs/GPT-to-Gemini-DEV13-NoOp-Registration-Barrier.md`。
- 独立审计 R1：`audit/2026-08-25/GPT-DEV-13-Independent-Audit-R1.md`，判定 `PASS`。
- 当前状态为 `ready-for-human`，等待 Gemini 复核及真实 BUE + No-op 双 DLL 客户端冒烟。
