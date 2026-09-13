# DEV-V4-09：客户端画面验收与唯一对外候选

Type: task
Status: resolved
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

- [x] 全套测试工程 0 警告 0 错误；01..08 双轴 CLEAN 已合入（每轮 7/7+Rebuild 0/0；f4-rebuild-1..3.txt/f4-fullsuite-v5.txt）
- [x] 唯一候选 SHA-256 三次确定性重建一致（v5=`E20FEA58…AEEE` 636416B）；CaseId `DEV-V4-09-CANDIDATE-20260912` 本票授予；01..08 无 RELEASES 行（RELEASES 仅行 12 一条 DEV-V4）
- [x] SP 画面案例按编号步骤过（①..⑨ 双证据，round3/round4 tally）；P2P 跨端点过（round5-p2p-tally：TidyCommitted 代执行×2+隔离不牵连+启用诚实失败面=已知投影）；U3DS 不画不抛不留草稿（v5 headless 重验 PASS：TidyUI=0/Error=0/.bue-settings 零落盘）
- [x] 证据目录：截图（evidence/ 六张，后轮改走落盘路径）+ UMM LogOutput（auto-evidence/{sp,u3ds,p2p}）+ 部署身份绑定（多端 assembly-identity=E20FEA58…）
- [x] 双轴独立审查 CLEAN（09 修复链 F1/F2/F3/F4+静默短路共八轮，每轮 fresh 实例；含 Spec 复判 1 轮流程澄清）
- [x] 人工批准后加 RELEASES 行并换 publish 交付包（2026-09-13 用户原话「批准，关单」→ 本提交：RELEASES 行 12 当前发布物 + `publish/第4阶段-正式交付版本/` 四件套）

## Comments

- 2026-09-12 实机验收发现三缺陷（用户 SP 会话 174621 包取证），修复轮同票执行（候选 `c57c666c…` 作废，修复后重建唯一候选）：F1 停用→再启用整理按钮永不复装（补丁挂 PlayerDashboardInventoryUI ctor 一次会话一次，无再注入触发器；修复=Start 尾部对存活仪表盘立即重注入，九态门/在册去重/Q55 红线不变；连带 F1b=Stop 解绑日志缝后 Start 重绑，Lit/Lir/Lht 三处同构）；F2 保存后开关/状态回跳旧值（目录条目机器事实建目录时缓存；修复=DraftSaveReport.CommittedLifecycleIntent + 保存/确认两路径先 refreshModel 再渲染）；F3 外部文本框输入后直接保存=NoChanges（AddTextEditor 只在 Enter 提交；修复=OnTextChanged 逐键静默入草稿，Enter=提交+渲染，Esc=回本帧值+回拨草稿）。
- **具名 seam gap（Standards R1 blocking 闭合）**：F3 的原生接线层（Glazier `ISleekField` 事件挂接）宿主无逐键事件可构造面板实例，无红测锚；语义锚=既有模型组（ExternalConfigParity 的 String 草稿受理/脏判定/三类失败文案）+ 本票结单报告具名披露，实机复测为该接线的最终验证面。
- 2026-09-12 二轮实机：F2/F3 修复确认生效；**F1 修复被证伪**——Start 尾部补注入在真实机器时序下必然无效（Start 运行在生命周期机 Starting 态，Q55 九态门=过渡不新增，自我拒绝；实机 194456 包 gen8 重启用后零注入行取证，首局 5/5 实为首次开背包 ctor 注入）。**用户裁决（原话）「可不可以做成实时注入，打开背包时注入？」**→ 落 map.md V4-R9 增补裁决：Q55 九态门不变，Running 态经既有 Tick 泵（16 拍节流）对存活仪表盘幂等补注入=「Running 注入」的生命周期补偿；非 Running 不新增；在册去重（泵）/覆盖在册（ctor 新仪表盘=旧行为）并存。实现=注入尝试移 Tick 泵 + InjectButtonsInto(skipTrackedPages) 双策略；红测先行（运行时红「Start 期不注入」被旧代码违反→实现→绿）。Standards R3 CLEAN；Spec R3 抓范围蔓延→以 V4-R9 用户裁决闭合。
- 2026-09-12 用户再预警「实时注入岂不是日志要刷疯了」——核实**属实**（稳态全页在册下每次泵尝试仍打「注入完成：共 0/5」+Glazier 行≈3.75 行/秒）→ `ffa84c6` 全页在册静默短路（第四道门 Count>=5 静默 return；部分在册仍重试自愈；Q55 失败页引用保留=短路忠实）。红测=满册 20 拍零 [TidyUI] 行。双轴 Standards R4+Spec R5 双 CLEAN。候选 v4 `e3d199cd…`。
- 2026-09-13 三轮实机（包 001728，v4）SP 判 **PASS**：F1 复验（3 个停→启→拆除→复装周期+用户目视）、静默短路（42 行/45min 全为一次性突发）、⑦（本地整理已提交×4）、⑧（SPF cfg 哈希 0a73933d→579841a8 磁盘实锤）、④（UPM 本机未装→面板不画遗留 cfg=正确负例；改用 SPF：agent 锁 cfg→逐字「未保存：配置文件无法写入。」+草稿保留→解锁哈希不变=零落盘）；47 条 [Error 逐条甄别全良性。归档 `auto-evidence/sp/round3-20260913/`+tally。
- **F4（三轮⑨发现，v4 候选作废）**：NoOp 功能页实机不可达——生态标准形态插件 GUID=功能 id（夹具两行共享 StableId），A-Z 排序插件行在前，`RenderDetails` 按 StableId 单键取首行→两行都渲染插件分支（夹具无 cfg=空页），功能页（探针 Toggle+Choice）永被遮蔽。修复=`d6f3fe2`：(ManagementEntryKind, StableId) 双键路由贯穿（模型 OpenDetail/TryLeaveDetail 加 kind 重载=kind 给定只解析该种类/换种类重置草稿/显式种类无条目不回落，单参=features-first 兼容；原生 selectedKind/pendingNavigationKind 随点击行携带，确认导航/刷新重挂/关闭全路径）。红测=编译红 CS1501×6→CollisionFeatureAndPluginShareStableIdRoutesByKind。双轴：Standards R6 CLEAN（3 deferrable：插件草稿填充同构/收藏星与重启徽章仍单键=同键族残留另票/「不回落」未钉红测）；Spec R6 首轮把票级关单门（⑨实机证据）前置为代码轮门=流程死锁，澄清时序+F3 seam-gap 口径后复判 CLEAN。具名 seam gap（F3 先例）：Glazier 路由层宿主无测面=⑨实机复测终验。**候选 v5=`E20FEA58…AEEE`（636416B）3×一致部署双端，U3DS v5 负面重验 PASS**。
- 2026-09-13 四轮实机（包 114109，v5）⑨ 判 **PASS**（用户原话「你说的功能页和负例均可实现」）：功能行详情页探针开关+档位（甲/乙）可见可操作保存；插件行空页正确负例；磁盘实锤 noop ClientPreference mtime=11:40:48/revision=10/两键 base64 对应；**红利**=用户在生态功能页完成停用→再启用（UserDisabled gen7→gen8 干净拆重装=05 表面在第三方功能成立）。BUE [Error=2=设计内负探针×2 代际。归档 `auto-evidence/sp/round4-20260913/`+tally。**SP 侧①..⑨全闭环，剩 P2P 跨端点（v5）**。
- 2026-09-13 五轮 P2P（主机包 120321/客机包 120223，v5）判 **PASS**（归档 `auto-evidence/p2p/round5-p2p-tally.txt`）。用户报「主机网络模块显示已隔离、点启用提示功能启停失败」——取证判定**非新缺陷非 v5 回归**：①开局 `module-start not-started`→Isolated（stage=start-result）与 v7/v8/2.1 基线逐字同型=V3-09 §4c 已甄别的通用良性投影；②功能实际在线实锤=主机 `[TidyNet] -> 客机 TidyCommitted(reqId=1/2, result=Committed)` 代执行×2+双端 runtime-arm+用户目视客机整理可用——**跨端判据以更强形式成立**（模块处最重生命周期态整理仍照常，「停用不牵连」的显式停用腿已由 SP 的 LIT(F1)+NoOp(红利) 覆盖）；③用户 5 次启用=5 次真实重启尝试 `enable-failed stage=start`→每次干净回 Isolated，面板逐字「未保存：功能启停失败。」+草稿保留+零崩溃=Q44「保存后将尝试启用」诚实失败面按设计工作（隔离原因=启动时序 transport 未就绪，非用户停用，再启动仍不满足）。误导投影文案/判据澄清=V3-09 §6 具名后续另票，非 Phase-4 范围。**全环境（SP/P2P/U3DS）画面判据闭环，停等人工发布批准。**
