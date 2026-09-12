# DEV-V4-09：客户端画面验收与唯一对外候选

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: DEV-V4-01, DEV-V4-02, DEV-V4-03, DEV-V4-04, DEV-V4-05, DEV-V4-06, DEV-V4-07, DEV-V4-08
Spec: `../spec.md`（「实施与发布纪律（V4-T8）」节 + Testing Decisions 第 9 条）

## What to build

本阶段唯一对外候选：玩家在客户端能完整走草稿保存、描述与 Cycle、功能级启停、整理按钮与全局模式/方向、官方中文和外部配置同等升级。U3DS 不画面板、不抛、不留脏草稿。契约仍为 2.1。人工批准后才加 RELEASES 行。

## Scope

- 基于 01..08 全部合入后的源码生成**唯一** Phase-4 对外候选。契约仍 2.1；若发现必须新增公开成员：停本候选链，另开 2.2，不得在本票顺手扩面。
- 实机：agent 代部署；用户只做游戏内编号步骤。截图落盘绑 CaseId、环境（SP/P2P/U3DS）、构建 SHA-256 / LoadSetIdentity、步骤编号、阶段（before/dirty/saved/failure）。聊天贴图不是唯一证据。
- SP：完整管理面板 + 草稿 + 启停 + 整理按钮。
- P2P：只补跨端点（例如停网络模块不影响整理按钮）。
- U3DS：不构造面板、不武装 LIT patch、不留草稿、不抛。
- 画面案例至少：初始；编辑未保存；保存成功；保存失败；换条/关面板确认框；停用后整理按钮已拆；LIT Cycle；外部描述/Cycle/失败提示；NoOp Toggle+Choice；U3DS 负面不变量。
- 01..08 中间构建不得继承为本票候选身份。

## 验收条件

- [ ] 全套测试工程 0 警告 0 错误；01..08 双轴 CLEAN 已合入
- [ ] 唯一候选 SHA-256 三次确定性重建一致；CaseId 本票授予；01..08 无 RELEASES 行
- [ ] SP 画面案例按编号步骤过；P2P 跨端点过；U3DS 不画不抛不留草稿
- [ ] 证据目录：截图 + UMM LogOutput + 部署身份绑定
- [ ] 双轴独立审查 CLEAN
- [ ] 人工批准后加 RELEASES 行并换 publish 交付包（本票执行，01..08 不得抢先）

## Comments

- 2026-09-12 实机验收发现三缺陷（用户 SP 会话 174621 包取证），修复轮同票执行（候选 `c57c666c…` 作废，修复后重建唯一候选）：F1 停用→再启用整理按钮永不复装（补丁挂 PlayerDashboardInventoryUI ctor 一次会话一次，无再注入触发器；修复=Start 尾部对存活仪表盘立即重注入，九态门/在册去重/Q55 红线不变；连带 F1b=Stop 解绑日志缝后 Start 重绑，Lit/Lir/Lht 三处同构）；F2 保存后开关/状态回跳旧值（目录条目机器事实建目录时缓存；修复=DraftSaveReport.CommittedLifecycleIntent + 保存/确认两路径先 refreshModel 再渲染）；F3 外部文本框输入后直接保存=NoChanges（AddTextEditor 只在 Enter 提交；修复=OnTextChanged 逐键静默入草稿，Enter=提交+渲染，Esc=回本帧值+回拨草稿）。
- **具名 seam gap（Standards R1 blocking 闭合）**：F3 的原生接线层（Glazier `ISleekField` 事件挂接）宿主无逐键事件可构造面板实例，无红测锚；语义锚=既有模型组（ExternalConfigParity 的 String 草稿受理/脏判定/三类失败文案）+ 本票结单报告具名披露，实机复测为该接线的最终验证面。
- 2026-09-12 二轮实机：F2/F3 修复确认生效；**F1 修复被证伪**——Start 尾部补注入在真实机器时序下必然无效（Start 运行在生命周期机 Starting 态，Q55 九态门=过渡不新增，自我拒绝；实机 194456 包 gen8 重启用后零注入行取证，首局 5/5 实为首次开背包 ctor 注入）。**用户裁决（原话）「可不可以做成实时注入，打开背包时注入？」**→ 落 map.md V4-R9 增补裁决：Q55 九态门不变，Running 态经既有 Tick 泵（16 拍节流）对存活仪表盘幂等补注入=「Running 注入」的生命周期补偿；非 Running 不新增；在册去重（泵）/覆盖在册（ctor 新仪表盘=旧行为）并存。实现=注入尝试移 Tick 泵 + InjectButtonsInto(skipTrackedPages) 双策略；红测先行（运行时红「Start 期不注入」被旧代码违反→实现→绿）。Standards R3 CLEAN；Spec R3 抓范围蔓延→以 V4-R9 用户裁决闭合。
