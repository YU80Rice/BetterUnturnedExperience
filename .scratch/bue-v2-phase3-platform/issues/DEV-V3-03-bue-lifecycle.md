# DEV-V3-03：BueLifecycle 统一状态投影与资源接线（TryTrack+只读状态查询+面板启停 seam）

Type: task
Status: ready-for-agent
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

- [ ] 红测先行：假模块驱动状态机（Start 抛异常→Isolated 不扩散；TryTrack 逆序 Dispose；容量上限拒绝；UserDisabled 停/新代际启；隔离后查询仍可用；Dependencies Has/TryGet），各先红后绿（先例=NoOp+假模块宿主测试）
- [ ] 矩阵接线两侧红测：Lifetime/Dependencies 接线前 null+接线后可用
- [ ] 官方先行消费锚：官方功能走真 UserDisabled 面板启停 seam 与 TryTrack（NoOp probe 扩展为生态契约侧对照）
- [ ] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）
