# DEV-14：官方 Better Item Interaction 公开注册与准入 tracer bullet

Type: task
Status: resolved
Owner: GPT（后端运行时与官方功能装配）
Required reviewer: Gemini（前端消费与官方/第三方平权）
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Depends on: DEV-13 (`resolved`), SCR-GPT18-001（已接受提案）

## 目标

将官方“更好的物品交互”作为 BUE Host 内置功能，通过与第三方完全相同的公开 `IFeatureRegistration` / `BueRuntimeHost.Register` Seam 登记并进入 Runtime Catalog，证明官方功能没有隐藏特权路径。

## 本票范围

- 新增稳定官方 FeatureId：`io.github.yu80rice.bue.better-item-interaction`。
- 官方定义产物、模块工厂和可选 UI 元数据通过公开注册契约提交。
- BUE Host 在 `RegistrationOpen` 阶段登记官方功能；Catalog barrier 后该功能与 No-op/第三方条目一起冻结。
- 以纯 C# tracer bullet 验证官方模块的公开注册身份、digest 校验和确定性 Catalog 顺序。

## 明确不做

- 不接入 Unity/Glazier/Sleek/Harmony 原生库存 Hook。
- 不改变 `sendDragItem → ReceiveDragItem` 原生权威链。
- 不宣称物品拖动 UI、单人、P2P、U3DS 或三环境运行通过。
- 不修改 LMN、BepInEx 或 Unturned 原版内容。

## Acceptance

1. 官方功能必须使用与第三方相同的公开注册入口；不得新增 Host 私有特权 API。
2. 官方 FeatureId、定义 digest 和模块工厂可被 Runtime Catalog 观察，重复/非法登记仍 fail-closed。
3. Catalog 冻结后官方功能不可再次登记或替换。
4. 生产 BUE Host 保持单 DLL ABI 闭包和 Headless 零 UI Token 边界。
5. TDD 测试、Release 构建达到 0 errors / 0 warnings；独立审计 PASS。

## 运行证据边界

本票只证明官方注册与 Catalog 准入 seam，不证明 Better Item Interaction 玩法实现或三环境资格。真实玩法应由后续专票绑定自身 DLL/CaseId 验收。

## GPT 实施记录（2026-08-25）

- TDD Red：官方注册 parity 断言在缺少实现时以 `CS0103` 稳定失败。
- TDD Green：新增 `OfficialFeatureRegistration.cs`，官方功能通过 `BueRuntimeHost.Register()` 登记；Host `Awake` 在 `RegistrationOpen` 阶段调用同一入口。
- Release solution：0 errors / 0 warnings。
- 完整 7 个测试套件：全部 PASS；目标输出 `DEV-14 official registration parity tests: PASS`。
- GPT 独立审计：PASS，无阻断项。
- Gemini 前端消费复核：ACCEPT（`Gemini-DEV-14-Official-Registration-Review.md`）。
- 实施报告：`audit/2026-08-25/Implementation-DEV14-Official-Registration-2043.md`。
- 正式客户端双 DLL 冒烟：PASS；诊断包：`UMM-诊断包_20260825_205630`。
- 实际部署哈希与 staging 哈希一致；日志出现 `BootstrapReady`、官方注册成功、No-op 注册成功及 `RuntimeReady`。
- 最终运行审计：`audit/2026-08-25/GPT-DEV-14-Clean-Install-205630.md`。
