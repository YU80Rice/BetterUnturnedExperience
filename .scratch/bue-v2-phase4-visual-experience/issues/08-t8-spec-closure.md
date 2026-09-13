# V4-T8 第四阶段规格闭包与实施票拆分

- **Ticket**: V4-T8
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-11 Q71–Q75 定音；地图到达 /to-spec 出口）
- **Blocked By**: V4-T1, V4-T2, V4-T3, V4-T4, V4-T5, V4-T6, V4-T7
- **Map**: [map.md](../map.md)

## Question

地图关闭票。T1..T7 全部 resolved 之后，把本图收成可交 `/to-spec` 的输入，并拆实施票边界（本票仍是决策，不写 spec 正文）：

1. **范围扫拢**：本阶段实现 / 后续可视化 / 永久不做 三张表，对照开图 Notes 与各票 Answer，有无漏项或偷运。
2. **实施拆票建议**：一票一表面还是一票一缝（草稿、控件、启停、LIT 标题栏、文案、外部目录、验收）？候选纪律是否沿用「中间票不授候选、终票唯一对外」？
3. **实机验收谁做**：可视化必须看画面——是否仍「agent 代部署、用户只做游戏内编号步骤」？要不要截图落盘当证据（用户一次一张图）？
4. **雾区处置**：锁定页、选择器、分类导航、i18n、`bue.network` 文案——进 Out of scope 还是留「尚未明确」交下一张图。
5. **出口**：fresh 会话 `/to-spec`，输入 = 本图 + T1..T7 Answer + R1/R2 报告 + CONTEXT.md；不重开已关闭争议。

## Answer

2026-09-11 用户 Q71–Q75 定音。本票交付=范围三层表、九张实施票依赖、验收职责与证据矩阵、雾区清空、/to-spec 出口。不写 spec 正文、不重开 T1..T7。

### Q71 — 三层范围

**本阶段做（DEV-V4）**

未保存草稿与配置保存；描述行、基础配置控件与 Cycle；功能级启停目标意图；生命周期机目标提交与空操作；六项官方 legacy lifecycle alias；LIT 标题栏只留「整理」+ `inventorytidy.mode`/`direction`；官方 chrome 对照表与仍可见设置中文；外部配置描述/Cycle/草稿；NoOp Toggle+Choice。契约 2.1 只消费，不扩 `IFeatureRegistration`。

**后续可视化（另开图）**

锁定页；ItemList/BlueprintList/CreatureList；Category 导航；程序集/配置路径；面板 i18n；`bue.network` 良性隔离文案；统一色板与跨表面尺寸语言；功能级描述可选 facet（若做则 Minor 2.2）。

**本阶段运行环境排除**

U3DS 不构造本阶段客户端表面；验收只要求不画、不抛、不留脏草稿。未来若另开服务端管理表面，不受本条「永久」误伤。

**永久产品不变量（改目的地才能动）**

不建 BueUi SDK 替代 Glazier；不把 O-LIT-1 排序规则混入本阶段视觉工作；不注入 STORAGE；不热卸载外部插件；不用草稿改写 `ServerAuthority`；不后台自动保存；不把外部配置编辑扩展成任意代码执行。

「不给外部插件进程级启停」= 当前产品能力边界，列入本图 Out of scope；若未来重做外部插件生命周期 seam，须另开图，不得在 DEV-V4 偷运。

### Q72 — 九张实施票

一票一缝。票号冻结，后续不得临时重排。

```text
DEV-V4-01  未保存草稿与配置保存模型
DEV-V4-02  描述行、基础控件与 Cycle
DEV-V4-03  生命周期目标提交与空操作语义
DEV-V4-04  官方 legacy enabled 迁移 adapter
DEV-V4-05  功能级启停详情页表面
DEV-V4-06  LIT 标题栏与 mode/direction Choice
DEV-V4-07  官方文案、设置中文与 NoOp Choice
DEV-V4-08  外部配置同等升级
DEV-V4-09  客户端画面验收与唯一对外候选
```

依赖：

```text
01 → 02, 03, 08
03 → 04, 05
02 → 05, 06, 07, 08
04 → 05
06 → 07
01..08 → 09
```

**候选纪律**：01..08 各自红绿+双轴 CLEAN，**不授候选、不加 RELEASES、不授 CaseId**，中间 DLL ≠ 对外部署物。09 基于全部合入源码生成本阶段**唯一**对外候选；契约仍 2.1。实施中若必须新增公开成员：停当前候选链，另开 2.2 契约决策，不得在实施票顺手扩面。

### Q73 — 实机职责与证据

沿用「agent 代部署、用户只做游戏内编号步骤」。

Agent：部署指定构建；核 SHA-256 / LoadSetIdentity；清旧 DLL/日志/设置；配日志级别；启 SP/P2P/U3DS；回收 UMM `LogOutput.log`；把截图与 CaseId、环境、构建身份、步骤编号、阶段（before/dirty/saved/failure）落盘绑定；核 U3DS 不组面板、不武装 LIT patch、不留草稿。

用户：按编号点界面、改设置、在指定状态截图。一次一张图；正式证据以落盘文件为准，聊天贴图不是唯一证据。

画面案例至少覆盖：初始；编辑未保存；保存成功；保存失败；换条/关面板确认框；停用后整理按钮已拆；LIT Cycle；外部描述/Cycle/失败提示；NoOp Toggle+Choice；U3DS 无面板不抛不留草稿。

环境：SP 先覆盖完整面板+草稿+启停+整理按钮；P2P 只补跨端点（如停网络模块不影响整理）；U3DS 只验收 Headless 负面不变量，不要服务端面板截图。

### Q74 — 雾区清空

闭包后地图 **Not yet specified 为空**。条目分别进入：

- 后续可视化：锁定、选择器、Category、i18n、网络隔离文案、统一视觉语言、描述可选 facet；
- 当前明确排除：STORAGE、O-LIT-1、热卸载、外部进程级启停、草稿改 ServerAuthority、自动保存、U3DS 不组本阶段表面；
- 永久不变量：见 Q71。

整理按钮 `60×60` / `PositionOffset_X = -130` 是 LIT 表面实现约束，**不升格为全局 UI 契约**。本阶段不建立统一设计语言。

### Q75 — 出口

T8 结票 → 地图 Status=completed → **fresh 会话 `/to-spec`**。输入=本图 + T1..T8 Answer + R1/R2 + CONTEXT.md。/to-spec 只把已冻结决策写成规格：不重开 T1..T7、不新增表面、不新增公开契约成员、不把中间构建写成候选、不把 DEV-V4-* 写成已完成。实施票号与依赖以 Q72 九票为准。

## Impact

- 地图到达 Destination 四条；Status completed。
- `/to-tickets` 必须按九票+候选纪律，不得把 01..08 写成可发布候选。
- CONTEXT.md：本票无新术语。

## Comments

- 2026-09-11：用户将 Q72 收成九票（生命周期空操作、legacy alias、启停 UI 分缝），Q71 把 U3DS 从永久项改为阶段排除后结票。
