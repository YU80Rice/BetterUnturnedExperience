# DEV-V7-01：换成标签归拢、组内大件优先、左上紧凑

Type: task
Status: resolved
Parent: spec.md（V2 第七阶段规格·现有官方功能玩法手感定界与接线）
Blocked by: None (can start immediately)
Spec: `../spec.md`（「整理方法（V7-T2 → DEV-V7-01）」节 + 共享规则）
Red: `--bue-v7-01-stable-label-compact-red`

## What to build

玩家点「整理」（当前栏、Ctrl+全身、容器标题栏、入包失败恢复、快转接收侧）得到同一套摆法：按固定用途把同类归拢，同一类里大件优先，从左上紧凑排列，空位留到右下，图标正向可读。放不下则格子完全不动。设置里不再出现「整理方向」。

## Scope

- 深层排版模块替换行带：规划不改真实背包；失败零提交。内部标识 `StableLabelCompact`，不进玩家界面、不进契约。
- 20 档玩家用途标签顺序沿用，不重开分类。分类失败进「其他」，不得丢物。
- 不拆散 = 名单连续 + 已占用四连通 + 标签关闭后冻结。补边只允许未围住的外轮廓。大件旁边尚未围住的空格可填。
- 整理只走两个可读正向；永不选倒置。与更好的物品交互共享正向产品定义，本票不改拖入预览。
- 五个整理入口只换规划策略，仍走各自原有范围、权限、会话和原版提交。
- `inventorytidy.direction` 从面板描述符移除；旧值可读，不再决定算法。标题栏仍只留「整理」。

## 隔离（禁止顺手带走）

本票只替换排版与规划。禁止改：原版整理提交路径；快转源页恒等离场；整理后同 ID 合匣；容器会话权限；入包/快转恢复的触发条件。禁止为保旧绿把新方法做成行带换皮。行带几何旧断言必须改成新结构钉。

## 验收条件

- [x] 红测先行：名单连续、四连通、关闭冻结、1×3 旁 1×1 可补边、已围住的洞不可填、放不下零提交、确定性、只走两个正向。先红后绿。组名 `--bue-v7-01-stable-label-compact-red`
- [x] 五个整理入口官方先行消费同一规划出口（策略标识可测）。宿主点击不可测则具名接缝缺口，实机随 07
- [x] 面板不再把 direction 当有效算法设置；旧档可读不炸
- [x] 手册 LIT 行本票不改（归 06）；行为已换
- [x] 双轴独立审查（standards-reviewer / Spec-Reviewer，每轮全新实例）CLEAN
- [x] **候选纪律**：不授候选 / RELEASES / CaseId

## Answer

- 红测先行：初始构建先因缺少 `StableLabelCompactStrategy` / `StableLabelCompactLayout` 编译失败；接入新纯域布局与策略后，`--bue-v7-01-stable-label-compact-red` 通过，验证标签顺序、确定性排序、四连通、开放外轮廓补边、内部洞拒绝、两个相对进入姿态正向、失败清零和默认策略接线。
- 实现：新增 `StableLabelCompactStrategy` 与 `StableLabelCompactLayout`；默认模块切换到 `StableLabelCompact`。五个入口仍经 `InventoryTidyModule.Strategy` / `ManualTidyService.PreparePage`；快转源页继续 `PreparePageLeave` 恒等离场；原版范围、权限、会话、提交和恢复触发条件未改。
- 退役 direction：`inventorytidy.direction` 从 LIT 设置描述符和 registration settings facet 移除；旧存档值仍可读，旧 wire 帧与 `TidyMode` / `sortDescending` 占位保持兼容，旧值不再决定算法。
- 回归绿测：V7 专项、DEV-V5-04 入包失败恢复、DEV-V5-05 快转恢复均 `0 failures`；Plugin 全套通过。
- FULLSUITE：构建、Contracts、Network、Placement、Settings、ClientUi、Plugin、Release 全部通过；17 步中 16 pass、0 failed、1 个既有 `ContractTypes.cs:Glazier` KNOWN-BASELINE（不计失败）；Firewall violations=0。
- 双轴独立复审：Standards reviewer = `CLEAN`；Spec-Reviewer 首轮提出的旋转/策略出口意见经字段语义与生产调用链复核判定为误报，fresh Spec-Reviewer 最终 = `CLEAN`。未发现遗漏、范围蔓延或错误实现阻断。
- 具名非阻断气味：`TryReadSavedTidyPreference` 保留快照 revision 读取导致的轻微死代码/臆测通用性气味；`PreferredRotation` 与旋转字节仍是基础类型表达；测试中固定占位 `sortDescending=true` 有基本类型偏执气味；旧行带实现为历史测试兼容而保留在生产编译清单，默认生产策略已唯一切换为 `StableLabelCompact`。以上均未构成规范或规格阻断。
- 本票未修改手册 LIT 行、契约 2.1、候选、RELEASES、CaseId，也未触碰用户既有 `CONTEXT.md`、ADR、`artifacts/u3ds-seven-seam/` 或 `docs/third-party/unturned-plugin-dev/`。
