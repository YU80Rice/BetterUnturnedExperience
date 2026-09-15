# DEV-V5-03：容器会话整理

Type: task
Status: resolved
Parent: spec.md（V2 第五阶段规格·现有官方功能优化定界与接线）
Blocked by: DEV-V5-01, DEV-V5-02
Spec: `../spec.md`（「容器会话整理（V5-T4 → DEV-V5-03）」节）

## What to build

玩家打开世界箱子或已授权后备箱时，标题栏有一颗「整理」，只整理当前这只容器。Ctrl+全身整理仍然只动身上五页。关箱、没权限或内容变了会说原因，格子不动。

## Scope

- 容器会话模块 + 两个 adapter（世界容器、车辆后备箱）。禁止「页号等于 7 就整理」。
- 能力为可用才画按钮。布局由容器标题栏 adapter 提供，不复用服装页固定偏移。文案仍叫「整理」。
- 请求绑定种类、会话、请求者、版本。提交前主机重验。消费 02 的布局计划，不复制排版。
- 虚拟箱 Unsupported、不画、有诊断原因。地面摊、主副手、装备槽不支持。
- 不另造 BUE 整理锁。本票不画容器锁定，不接线入包进箱子的被动整理。
- U3DS 不画按钮，仍执行权威请求。

## 验收条件

- [x] 红测先行：两个 adapter；page=7 不是身份；关箱/换 opener/离座/版本过期零修改且有结构化原因；虚拟箱不画；Ctrl+全身不含容器。先红后绿
- [x] 官方先行消费：真实容器标题栏按钮走会话提交 + 02 计划
- [x] 双轴独立审查 CLEAN
- [x] **候选纪律**：不授候选 / RELEASES / CaseId

## Answer

2026-09-15 实施会话闭环（评审链 `audit/2026-09-15/DEV-V5-03/review-loop.md`，双轴 R1..R4，终态 R4 双 CLEAN）。

**架构（V5-T4 冻结形状落地）**：容器表面 adapter → 当前容器会话事实 → 容器整理请求 → 统一标签分段行带排版（02 出口） → 服务器权威重验与提交。

- **容器会话深模块**（`Lit/Container/LitContainerTidy.cs`，纯 C# 零引擎）：种类/原因码（8 类冻结线协议域，文案单源）、能力投影（T4 Q4 六输入：种类/会话/权限/功能运行/有 adapter/标题栏在，不可用即不画并带原因与诊断）、内容指纹（FNV-1a over 排序后的网格+jar 元组，收集序无关）、**两个 adapter**（世界箱=opener+checkRot 锁组维度；后备箱=驾驶座维度；互不越界，权限步各自 CheckAccess）、按序重验器（会话→种类→权限→版本）。
- **线协议**：功能私有新消息 7（请求=[proto][token][reqId][kind][fingerprint][desc]，**无页号**）与 8（结果=[token][reqId][result][reasonCode]，成功⇔无因）；共享既有会话 challenge/账本/租约（重放命中缓存、与服装页事务互斥答 Busy）；旧服装页协议 page=7 仍畸形。
- **权威**：`ExecuteServerContainerTidy`（U3DS 同样执行）现读真实 Player 挂载→经执行核（重验→ManualTidyService.TidyPage 同事务→原因映射）；失败零修改+结构化中文原因；不新增 BUE 锁、不覆盖原版 opener/驾驶座规则、离座/换座/关箱按原版先拆挂载呈现为 ContainerClosed（R3 时序）。
- **UI**：容器标题栏一颗「整理」——布局/文案由 `LitContainerTitleBarAdapter` 单源（左锚 60×60，不复用服装页 -130），可见性=能力投影每拍翻转同步（稳态零日志），点击走 `module.RequestContainerTidyFromUiClick`（生命周期事实→已保存方向→投影→本地权威或联机）。失败经 toast 单源文案（画面类，U3DS 不画不抛）。Ctrl+全身仍只身上五页；容器整理不发布 TidyCompleted（不触发整理后压弹）。
- **验证**：红先行=17 编译错落盘；新组七组 ALL GREEN（能力投影与两 adapter/指纹/重验矩阵/统一排版消费/线协议/双端全链/官方先行与全身隔离）；六项突变 M1-M6 各证红还原绿；7 套件 exit=0；Rebuild 0/0；6 门禁 exit=0（NoUiTokens：Core PASS，Contracts 命中=V2-02 既有注释自述禁用词，非本票产物，如实双记）。
- **具名接缝缺口（实机随 08）**：真机 Items 提交写面（宿主 `Items.addItem` 触 Assets 静态初始化必抛→执行核留 `PageTransactionForTests` 同语义假事务钉计划消费/坐标落位/零修改结构序）；Glazier 按钮渲染/翻转日志；远程客机虚拟箱识别物理限制（钩子仅服务器可见→参照可得则客户端分类，不可得=声明照发+诊断 `remote-crate-identity-unverified`+权威 fail-closed 带原因零修改——T4 Q4「按钮可见≠授权承诺」允许面）。
- **候选纪律**：不产候选 DLL、不更 RELEASES、不授 CaseId；契约 2.1 零扩面（全部 internal+功能私有消息）；无新 FeatureId。玩家手册 LIT 行改「容器标题栏一颗整理只动这只容器；全身仍不含容器；关箱/失权/内容变化说原因」。

**下站**：前沿=DEV-V5-05（快速转移恢复，依赖 03 已满足）/06/07；04 可并行。08 三环境实机验收覆盖本票具名缺口。
