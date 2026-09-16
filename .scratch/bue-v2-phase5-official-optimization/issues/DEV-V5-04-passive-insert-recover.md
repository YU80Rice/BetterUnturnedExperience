# DEV-V5-04：拾取与合成入包恢复

Type: task
Status: resolved（2026-09-15 双轴评审链闭合：R1-Spec 2缺口=1误读驳回+1方向采纳逼出真机级 binder 修复；R2 双 CLEAN）
Parent: spec.md（V2 第五阶段规格·现有官方功能优化定界与接线）
Blocked by: DEV-V5-01, DEV-V5-02
Spec: `../spec.md`（「入包恢复与快速转移恢复」入包分支）

## What to build

玩家捡东西或合成时，若原版因为碎洞说塞不进身上五页，主机会把这五页连同这件新东西一起交给统一排版再试一次。仍塞不进则东西留原处，背包不动。成功了不当成点了「整理」，不自动压弹。

## Scope

- 入包恢复 adapter。只接线已验证的主机拾取/合成 `tryAddItem` 失败。其它获得路径不做。
- 只整理玩家五页。排除主副手、AREA、容器页。
- 仅原版第一次失败触发；成功放入不排。消费 02 计划。跟随背包整理生命周期，无独立开关。禁止 Prefix 内死开关。不挂拖放预览。
- 成功不发布整理完成。禁止吞物（报成功却没放入）。
- 快速转移归 05，本票不接 `sendDragItem`。

## 验收条件

- [x] 红测先行：仅 tryFindSpace 失败触发；成功放入不排；仍放不下则原版失败 + 背包未改；不发整理完成；功能停路径不执行。先红后绿（编译红 red-compile-errors.txt → 六组 ALL GREEN；突变 M1/M2/M4/M5 各证红）
- [x] 官方先行消费：真实拾取/合成失败路径经 02 计划（组4：策略出口计数+逐格对照 tagged-row-band-v1 计划+分类器接缝；委托链 U3-SDK 逐字行号钉 binder 注释；R1-Spec 追问逼出 InsertRecoverBinder 显式绑定+1d/1f/M7 宿主可证）
- [x] 双轴独立审查 CLEAN（评审链 audit/2026-09-15/DEV-V5-04/review-loop.md：R1-Spec 2缺口处置→R2 Standards 0硬/5具名递延 + Spec CLEAN）
- [x] **候选纪律**：不授候选 / RELEASES / CaseId（本票无候选痕迹）

## 设计（2026-09-15 认领定案·供双轴评审）

事实基线（U3-SDK 一手查证）：拾取 RPC `ItemManager.ReceiveTakeItemRequest`（SERVERSIDE）自动入包走
`PlayerInventory.tryAddItem(item,true)→tryAddItemAuto(Item,4×bool)`（先主副手/衣着自动穿，再页 2..6 顺序
`Items.tryAddItem`，全败返回 false→原版发 SPACE、地面物留下）；合成 RPC `PlayerCrafting.ReceiveCraft`
（ONLY_FROM_OWNER）产物走 `forceAddItem(item,true)→tryAddItemAuto`（false→dropItem 原地掉回）。
`tryAddItemAuto`/`forceAddItem` 同时是 NPC 奖励/商店/稻草人/收获等一切获得路径的共用汇聚点
（T5 红线：不能因共用类型自动享受）→ 恢复必须显式接线到两条已验证 RPC，不是挂类型。

- **三处补丁**（随 LIT 生命周期：Start 登记、Stop/隔离 注销；U3DS 也登记——入包恢复=权威行为非画面，
  story 25 / T1 Headless 裁决）：
  1. `ReceiveTakeItemRequest` 与 2. `ReceiveCraft`：prefix/finalizer 开合已验证上下文计数器
  `InsertRecoverScope`（finalizer 保证异常路径也闭合）；
  3. `tryAddItemAuto` 的 **postfix**：`__result==true` 原样返回（成功放入不排；postfix 天然只在原版失败
  后执行 = 「仅原版第一次 tryFindSpace 失败」触发面）；失败且 scope 在开 → `InsertRecoverAdapter.TryRecover`
  恰好一次；恢复提交成功 → `__result=true`（原版调用方按成功入包继续：拾取销毁地面物、合成不掉落）。
- **恢复本体 = 五页整理（含待加入物品）+ 这一次放入，同一事务原子提交**：规划器消费 02 唯一出口
  （ITidyStrategy→tagged-row-band-v1，不复制排版）；候选页 = 活动页 2..6 按页升序（复现原版自动入包页序，
  不升格全身挑选）；待加入物品进入候选页计划；五页全部可排版 → 提交。事务与按钮整理同源：
  `ManualTidyService` 提取 `CommitPreparations`（行为零变化）供两条路径共用，`PreparePage/CommitPage/
  守恒验证`扩展可选待加入物品——**待加入物品不在提交结果内 = 不算成功（禁吞物）；提交失败回滚 = 零修改**。
  任一页放不下 / 提交 Rejected → 原版失败 + 背包不动。
- **成功不发布 TidyCompleted、不触发压弹/合匣**（不新增公开事件）；内部诊断走日志。
- **门禁**：非已验证上下文 / 模块未登记（生命周期=唯一开关，无独立开关、无 Prefix 内死开关）/
  熔断开 / item null 或 asset null 或 **isPro**（原版门禁照抄，恢复从不绕行）/ 方向偏好不可读（=点击
  同 revision Q59 规则，不发明默认）/ 无活动页 → 全部拒绝 = 零修改。
- **成功尾巴复刻原版语义**：autoEquipUseable 时装可用物尾（`ServerEquip`，仅真机）；快捷键=本地玩家
  背包复用 LocalTidyExecutor 既有链重绑，远端玩家服务器侧 `_hotkeys` 为 null 无从重绑 = 与原版自身
  移动物品同款遗留（具名，非本票扩面）；listen-host 投影 reconcile 与按钮路径同锚。
- **具名范围限定（R1-Spec 追问澄清）**：行为位选 `tryAddItemAuto` 而非 `Items.tryAddItem`，因两条票内
  路径都在此汇聚——拾取 `tryAddItem(item,true)`(PI.cs:464) → `tryAddItem(item,auto,true)`(PI.cs:468) →
  `tryAddItemAuto`(PI.cs:494)；合成 `forceAddItem(item,true)`(PI.cs:597) → `forceAddItemAuto` →
  `tryAddItemAuto`(PI.cs:607)。v1.4.0 当年挂 `Items.tryAddItem` 会顺带打到 AREA 预览/指定坐标入包等
  非票内面，本票不抄那个形状。`to_page!=255` 的指定坐标拾取（PI.cs:405 五参重载）不是碎洞自动入包
  失败场景，属「其它获得路径不做」的具名排除。
- 快速转移（`sendDragItem`）不接；拖放预览不挂；其它获得路径不做（票面 Scope）。
