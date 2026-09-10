# DEV-V3-07：BueDiagnostics 统一诊断（Logger 接线+有界摘要+BUE-* 前缀纪律）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-01（注册桥与 Bootstrap 基线）、DEV-V3-03（BueLifecycle）、DEV-V3-04（BueNetwork）
Spec: `../spec.md`（「诊断（V3-T8 → DEV-V3-07）」节）

## What to build

生态作者的诊断行进入与官方同权的证据链：经注入的 `IFeatureLogger` 写三方法窄面日志，行进同一 LogOutput 与有界诊断摘要，不被静默过滤；玩家被 UMM 导出的日志里直接看到结构化诊断与摘要。原始日志导出仍归 UMM 人工流程——BUE 不建日志复制器、采集器或面板导出动作。

## Scope

- `IFeatureLogger` 接线（可用性矩阵行，红线钉「接线前 null+接线后可用」两侧）：每模块绑定自身 FeatureId 的 view，三方法窄面（Info/Warning/Error）；Logger 异常不得反向破坏模块。
- 有界诊断摘要：按 DiagnosticId 聚合（FeatureId/级别/计数/首末时间），容量受限/输出限频/重启不持久；结构化行写入 BepInEx LogOutput；摘要≠验收授权（CaseId/RELEASES 仍人工）。
- `BUE-*` 平台诊断前缀保留；生态诊断码用 FeatureId 派生前缀；冒用=拒绝写入+诊断。
- 统一 sink 收编：T4 隔离/T5 链路健康/状态投影进统一诊断 sink（分 seam 判据可定位，不互相遮蔽）。
- 不做：日志复制器/诊断包打包器/面板导出动作；诊断附件 API；诊断实时视图（均为后续候选或 Out of Scope）。

## 验收条件

- [ ] 红测先行：sink 捕获（Logger 行→LogOutput 结构化行→摘要计数；BUE-* 前缀拒绝；容量受限降级；限频），各先红后绿（先例=DiagnosticLogSink/Recorder seam 与 `--*-red` 锚点）
- [ ] 矩阵接线两侧红测：Logger 接线前 null+接线后可用；Logger 异常不反向破坏模块红测
- [ ] 官方先行消费锚：官方功能走注入 Logger view 断言（非内部控制面）
- [ ] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）
