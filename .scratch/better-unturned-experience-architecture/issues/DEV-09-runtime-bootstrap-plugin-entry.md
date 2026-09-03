# DEV-09：Runtime Bootstrap + BepInEx Plugin Entry

Status: resolved（2026-09-03 人工批准 V1 闭环收尾：以 DEV-16E/F/G 实机证据作为覆盖）
Owner: GPT（总维护者/后端运行时）
Required reviewer: Gemini（前端消费与 Headless 边界）
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Depends on: DEV-01～DEV-08

## 目标

把当前聚合 DLL 从“可编译骨架”推进为可被 BepInEx 发现并安全启动的最小插件入口。
本票只建立唯一 BepInEx 入口、Bootstrap 状态和最小诊断，不实现库存 Hook、Glazier UI、LMN 网络或玩家功能。

## 验收条件

- [x] TDD 先建立 `BootstrapGuard` 公共 seam 的失败测试，再实现最小代码。
- [x] 聚合 DLL 只有一个 `[BepInPlugin]` 入口，身份为 `io.github.yu80rice.betterunturnedexperience`。
- [x] 入口版本保持预发布 `0.0.0`，不得伪装 Stable/1.0.0。
- [x] `Application.isBatchMode`、Headless 和 UI 可用性由前置守卫分流；U3DS 不实例化 ClientUi。
- [x] 启动异常 fail-closed，记录 FeatureId、DiagnosticId 与状态，不阻断原版进程继续运行（在 CLR 可继续条件下）。
- [x] Plugin 边界可引用 BepInEx/Unity；Contracts/Core/Release 不新增引擎依赖。
- [x] 不注册 Harmony、不解析原生库存、不修改 LMN。

## Verification

- [x] Release 0 errors / 0 warnings。
- [x] DEV-02～DEV-09 测试 PASS。
- [x] 唯一入口、版本、Headless 分流与无 UI/native 泄漏扫描 PASS。
- [x] 独立子智能体审计 PASS，随后交 Gemini 复核。
- [x] 本票完成只代表“可进行 BepInEx 加载冒烟测试”，不代表物品交互或三环境功能 PASS。

## GPT 交付状态

- GPT 独立审计 R2：PASS，报告 `audit/2026-08-25/DEV-09-Independent-Audit-R2.md`。
- Gemini 前端消费复核：ACCEPT（`DEV-09-Bootstrap-Review.md`）；同意工单维持 `ready-for-human`。
- 真实 BepInEx、U3DS、单人、SteamP2PFriends Host/Client 运行证据：未采集。

## 2026-09-03 V1 闭环收尾（待人工批准）

- 本票目标（BepInEx 唯一入口 + BootstrapGuard 分流 + Headless 不实例化 UI）已由后续工单实质履行并实机验证：DEV-12 单 DLL 冒烟、DEV-13 No-op 双 DLL 冒烟、DEV-16A~G 真实运行时（管理面板/库存/拖拽/日志），U3DS Headless 隔离在 DEV-16E 三环境证据中验证（`audit/2026-09-02/DEV-16E-*`）。
- 真实运行证据义务被 DEV-16E/F 后续 CandidateBuild/CaseId 覆盖，本票不再另起采集。
- **待人工批准**：批准后标记 `resolved`，并入 V1 冻结快照。

