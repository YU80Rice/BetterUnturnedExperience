# DEV-V2-08：LIT/LIR/LHT 生态迁移验证

Type: task
Status: ready-for-agent
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-07-three-env-network-validation
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「生态与前置」）

## Scope

完成 LIT/LIR/LHT 的 V2 迁移验证闭环（T8 修正：源码已迁 V2 但从未测试、归档未闭环区）：

- 对 LaunchInventoryTidy（v3.0.1）、LaunchInPlaceReload（v3.0.0）、LaunchHordeTracker（v3.0.0）三个已迁 V2 源码（`Archive\2-未闭环验证项目`）逐个：编译、部署到 BUE 环境、实机验证其网络功能（命名频道收发）在 BUE 网络模块下正常。
- 确认三个插件作为 BUE 官方功能（吃掉消化）工作：不再需要独立 LMN DLL，走 BUE 注册/生命周期/隔离路径。
- 验证记录：每个插件一个 CaseId，绑定 V2 网络层 DLL 的 LoadSetIdentity。
- 若发现 V2 迁移缺陷（如 API 面不匹配、会话语义差异），记录为修复票（属于各自官方纳入实施，不在本票内修）。

## 验收条件

- [ ] 三个插件在 BUE 网络模块下实机收发正常（或记录明确缺陷 + 修复票）。
- [ ] 迁移验证闭环：V1 兼容层"已知生态"维度的验证义务解除（T4 Q4）。
- [ ] 证据/日志归档到 `.scratch/bue-v2-lmn-adoption/` 或 `audit/`。

## 不做

- 不修改三个插件源码（发现缺陷另立修复票）；不重新实现它们的业务功能。
