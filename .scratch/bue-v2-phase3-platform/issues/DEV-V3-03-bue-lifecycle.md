# DEV-V3-03：BueLifecycle 统一状态投影与资源接线（TryTrack+只读状态查询+面板启停 seam）

Type: task
Status: resolved（2026-09-10 双轴 R1 双 CLEAN 闭环）
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-01（注册桥与 Bootstrap 基线）
Spec: `../spec.md`（「生命周期（V3-T4 → DEV-V3-03）」节）

## What to build

功能的生命周期状态有了唯一事实投影：作者查询自己的状态（状态/修订/隔离原因/代际）拿到只读快照，被隔离时能向用户给出有意义的提示；经面板停用/再启用的功能获得新生命周期代际且旧代际全失效；`TryTrack` 登记的资源在停止时逆序自动释放；单功能故障只隔离该功能，不再散落各组件私有布尔标志。

## Scope

- `FeatureState`/`FeatureStatusView`/`StateRevision` 接线为唯一状态投影；散装布尔标志收编为内部实现；模块不可改状态。
- `IFeatureLifetime` 只读状态查询（成员命名本票定）最小行为面：接线后任何阶段可用、不抛、不返回 null；返回不可变投影快照不暴露内部可变引用；隔离/停止后仍可用并如实返回当时状态；仅限自身 FeatureId。
- `IFeatureLifetime.TryTrack` 接线：停止后按注册逆序 Dispose、单 Dispose 异常隔离进诊断、容量必须有上限（数值本票定，可观察可测试）、已停止/隔离功能不可再登记。
- Dependencies=只读目录能力查询（Has/TryGet），不是求解器；`bootstrap.Lifetime`/`bootstrap.Dependencies` 接线（可用性矩阵两行，红线钉「接线前 null+接线后可用」两侧）。
- 两代际轴分离（LifecycleGeneration vs ConnectionGeneration）；再启用=新代际旧代际全失效；Isolated 不自动重启；面板启停 seam 落地（面板=command adapter，走 UserDisabled）。
- `CoreSafeMode` 只由组合期不变量损坏触发（catalog 冻结失败/核心 capability 组合失败等）；运行期单功能失败永不升级，只走功能级隔离。
- 宿主内部 owner-scoped registration record（01 票建立）承担状态/代际/资源所有权记录。

## 验收条件

- [x] 红测先行：假模块驱动状态机（Start 抛异常→Isolated 不扩散；TryTrack 逆序 Dispose；容量上限拒绝；UserDisabled 停/新代际启；隔离后查询仍可用；Dependencies Has/TryGet），各先红后绿（先例=NoOp+假模块宿主测试）
- [x] 矩阵接线两侧红测：Lifetime/Dependencies 接线前 null+接线后可用
- [x] 官方先行消费锚：官方功能走真 UserDisabled 面板启停 seam 与 TryTrack（NoOp probe 扩展为生态契约侧对照）
- [x] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [x] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）

## Comments

### 2026-09-10 结单（双轴 R1 双 CLEAN 闭环）

R1 双轴（每轴全新实例并行、零上下文）：Standards 轴 **CLEAN**（0 硬伤；5 deferrable 见审计报告 §4：StartedModule.LifetimeView 生产零读取/静态表跨组不复位/失败路径锁外写 Stopped/装配阶梯同构/StopDiagnostic 字段名复用等先例兼容判断题）+ Spec 轴 **CLEAN**（0 gap 0 deviation；七条 Scope 逐条对照票面/规格原文+实现行号证据，验收五条全兑现）。审查期间环境插曲：Spec-Reviewer 钉定模型 gpt-5.6-sol 空返回→改 grok-4.6 后 frontmatter thoughtLevel:medium 不被支持→删行重启后闭环（记忆条 `spec-reviewer-model-broken-2026-09-10`）。全套 7/7 PASS+全方案 Rebuild 0 警 0 错（终版二进制）；证据 `audit/2026-09-10/DEV-V3-03/`（log/diff 磁盘归档不入库，结单报告+final-fullsuite txt 入库）。面板命令 seam 的面板侧按钮接线随 DEV-V3-06 动态路由落地（本票=运行时 seam，票面口径）。

### 2026-09-10 实施中期（未结单）

红绿与验证已完成：行为红 11 条+编译红（`audit/2026-09-10/DEV-V3-03/red-*.log`）→ 绿（`final-fullsuite-*.txt` 七工程全 PASS）→ 全方案 Rebuild 0 警 0 错；交付面=Core `FeatureLifecycleRuntime`（统一状态机+TryTrack 管线+停止边界逆序清理+Dependencies 只读目录查询）+Plugin `BueFeatureStartRuntime` 状态机接线与 `SetFeatureEnabled` 面板启停 seam+Contracts `IFeatureLifetime.CurrentStatus` 加性成员（Minor 2.1）+两 readonly struct 加性构造器+LIT 官方真消费（TryTrack 真实网络句柄+真 UserDisabled 循环）+NoOp probe 生态对照扩展。Standards R1=CLEAN（5 deferrable 见中期报告 §4）。**Spec 轴被环境阻塞**：Spec-Reviewer 钉定模型 gpt-5.6-sol 空返回（CLI 日志实证 turn 0 processing_input 失败），改 grok-4.6 后须应用重启加载；重启后重派全新实例闭环，双 CLEAN 前不提交。面板命令 seam 的面板侧按钮接线随 DEV-V3-06 动态路由落地（本票=运行时 seam，票面口径）。冻结面变更清单（Minor 2.1 批次登记，SDK 条目归 DEV-V3-08 附录 A/B）：①`IFeatureLifetime.CurrentStatus` 只读查询成员；②`FeatureStatusView` 六成员构造器；③`NegotiatedFeatureView` 七参构造器；④诊断码 BUE-LIFE-STATE/ACCEPT/RELEASE/ISOLATE/001..006（附录 B 登记）；⑤Dependencies presence-only 能力投影规则（Has 空能力串+0 版=存在性，非空能力 fail-closed）；⑥容量定值 64/功能/代际；⑦`SetFeatureEnabled` 面板 command seam（宿主内部，非 SDK 契约面）。
