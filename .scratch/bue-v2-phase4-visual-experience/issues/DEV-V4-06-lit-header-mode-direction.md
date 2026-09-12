# DEV-V4-06：LIT 标题栏与 mode/direction Choice

Type: task
Status: resolved（2026-09-12 红测先行（编译红 94=CS1061×24+CS0117×70）→运行时红暴露再启用粘滞 ShuttingDown/中文档位 UTF-8 上限/FileSettingMissing 迁移窗口冲突三缺陷→修复 GREEN；双轴审查三轮闭环：R1 Spec 1 blocking（拆除失败清引用伪装成功）→失败页引用保留+BUE-LIT-TEARDOWN+重试自愈；R2 Standards 1 blocking（三处日志/注释与失败路径相反）+Spec 1 blocking（显示名=Q56 冻结项，描述句子才归 07）→日志如实化+DisplayNameKey=整理模式/整理方向+描述空不画；R3 双轴 0 blocking（Standards CLEAN/Spec 仅 1 deferrable 文档拆分）；构建 0/0+全套 7 绿；官方先行消费锚②=LIT 设置页真实消费两条 Choice（含接线级真实协议组）；必要修复=Core TryRead 缺键即损坏门移除（digest 仍为完整性门，否则 facet 回归后迁移静默跳过、升级丢停用偏好）+迁移组回归锚；本票按纪律不授候选/RELEASES/CaseId，契约仍 2.1。审计 `audit/2026-09-12/DEV-V4-06/audit-report.md`）
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: DEV-V4-02
Spec: `../spec.md`（「LIT 标题栏与全局模式/方向（V4-T5 → DEV-V4-06）」节）

## What to build

玩家在背包 Hands/Backpack/Vest/Shirt/Pants 标题栏只看到一颗「整理」：左键整理当前栏，Ctrl+左键按已保存的全局模式和方向整理全身（不含仓储栏）。模式和方向在 LIT 设置里用循环切换改，默认同类+降序。功能未在运行中时不画按钮；停用或隔离后已打开页面上的按钮必须拆掉。

## Scope

- 注入 headers[0..4]，60×60，PositionOffset_X=-130。不注入 STORAGE。不留锁定空位。不登记 ClientUi satellite。U3DS 不武装。
- 九态：仅 Running 新开页注入；Disabled/Stopped/Isolated/Incompatible 拆除；过渡态不新增，已有则点击安全回退、不报假成功。可用性由生命周期事实决定，不由 patch 私有布尔。
- 两条 ClientPreference Choice：`inventorytidy.mode`（同类/空间/大件，默认同类）、`inventorytidy.direction`（降序/升序，默认降序=大件优先）。旧每页内存丢弃。
- 点击只读同一 revision 的已保存 ClientPreference 快照，不读面板草稿。
- 完整中文描述句子归 07；本票钉键、档位、默认、Choice 穿过 02 控件。
- `inventorytidy.enabled` 退役依赖 04，但本票设置页只留模式与方向（enabled 缺席红测可与 04 对拍；若 04 未合入则本票不得把 enabled 当总开关继续画成可编辑生命周期）。
- 不做：O-LIT-1；StrategyId 选择器；STORAGE；锁定按钮。

## 验收条件

- [x] 红测先行：仅 Running 注入；停用后拆除；mode/direction 两条 Choice 默认与档位；点击读同一 revision 快照、不读草稿；STORAGE 不注入
- [x] 标题栏不再注入模式/方向按钮；全套测试 0 警告 0 错误
- [x] 官方先行消费锚：LIT 设置页真实消费两条 Choice（T1 检验点 ②）
- [x] 双轴独立审查 CLEAN（三轮闭环，R3 双轴 0 blocking）
- [x] **候选纪律**：不授候选、不加 RELEASES、不授 CaseId

## Comments

- 2026-09-12：实施结票。移除失败语义按 Q55 红线做成「引用保留+留痕+重试」；显示名/描述按 Q56/T6 拆分（显示名本票落、描述句子 07 落）；附带修复 Start 同实例再启用粘滞与 Core 文件层缺键即损坏门（迁移窗口），均见审计报告。
