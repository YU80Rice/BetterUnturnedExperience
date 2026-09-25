# DEV-V7-06R 结单审计

日期：2026-09-25
票据：`issues/22-DEV-V7-06R-copy-reconcile.md`
状态：resolved

## 1. 交付范围

本票只对当前有效玩家可见文案和文案门禁重新对账：

- README 功能一览同步 BII/LIT/LIR 的 V7 最终行为；
- BII AutoRotate 设置描述同步“两个正向、文字保持可读”；
- 06R 红测扩展到 README、技能行完整投影和设置降级表面；
- 记录文案逐字对账、红绿证据、受影响回归、FULLSUITE 和双轴审查。

## 2. 未修改清单

本轮没有修改：整理算法；弹药观察或 HUD 计算；被动压弹调度器；技能账作用域或技能行布局；BII 预览、ghost 生命周期、颜色或旋转；SDK；公开契约；FeatureId；`audit/RELEASES.md`；`publish/` 正式交付包；候选或 CaseId。

历史 `publish/`、V5 兼容投影、`.scratch/` 研究/票据和既有会话开始前的其他未提交改动均未作为本票修复对象。

## 3. TDD 链

- 首轮基线旧门禁通过，证明原 DEV-V7-06 门禁未覆盖 06R 缺口。
- 扩展 06R 门禁后，未改文案时构建成功但断言失败：`README BII 最终文案`，exit=1；详见 `audit/2026-09-22/DEV-V7-06R/red-run.txt`。
- 修复 README 当前功能表和 BII AutoRotate 描述后，06R 专测构建与运行均 exit=0；详见 `green-v7-06-run.txt`。

## 4. 验证

- ClientUi source Rebuild、ClientUi.Tests Rebuild/运行：exit=0，全部 ClientUi groups PASS。
- Plugin.Tests Rebuild/无参运行：exit=0；既有 V7-02/V7-03 与历史回归 PASS。
- V7-02、V7-03、V7-04、V7-05 专测均 exit=0。
- `git diff --check`：PASS。
- FULLSUITE：exit=0；`steps=17 pass=16 failed=0 known-baseline=1`；唯一已知基线为 `Gates:NoUiTokens:Contracts -> ContractTypes.cs:Glazier`；`[Firewall] PASS violations=0`；`FULLSUITE: PASS`。

完整输出见 `audit/2026-09-22/DEV-V7-06R/green-affected-regressions.txt` 和 `green-fullsuite-run.txt`。

## 5. 文案逐字对账

| 对账面 | 规格锚 | 结果 |
|---|---|---|
| 玩家手册 BII/LIT/LIR 三行 | `spec.md:136-140` | PASS；含绿/红框、两个正向、可读文字、StableLabelCompact 语义、总弹药和 2 级每 8 秒五页空/未满匣 |
| README BII/LIT/LIR 三行 | V7-06R scope；`spec.md:136-140` | PASS；本票修复并由红测逐字钉住 |
| ClientUi 对照表 | `spec.md:142-146` | PASS；BII/LIT/LIR 已冻结，尸潮/Network/NoOp 未改 |
| 身上/容器 Tooltip 与成功句 | `spec.md:148-153` | PASS |
| 技能页与设置降级面 | `spec.md:111-120,154` | PASS；名称、0/1/2 描述、125/150、Full、三格锁条、主机确权和升级入口共用投影 |
| 整理 direction | `spec.md:156-157` | PASS；有效描述符移除，旧值读取兼容保留 |
| 旧第五阶段承诺 | 06R:20-26 | PASS；当前有效玩家表面扫描为零；历史归档和 V5 兼容代码按边界保留 |

行为不一致的路由已记录：总弹药/刷新退回 DEV-V7-02；被动压弹退回 DEV-V7-03；技能行/作用域退回 DEV-V7-04；BII ghost/绿红框/正向退回 DEV-V7-05。本票未修改生产行为。

## 6. 双轴审查

Round 1 使用两个全新独立实例：

- Standards：CLEAN；无硬阻断。重复冻结字面量数组和 README 子串门禁仅为明确标注的非阻塞 smell。
- Spec：CLEAN；未发现缺失、范围蔓延或语义偏差。

报告链：`review-loop.md`、`standards-axis-report-r1.md`、`spec-axis-report-r1.md`。

## 7. 候选纪律与结论

DEV-V7-06R 不授候选、不改 `RELEASES`、不授 CaseId、不生成正式交付包。候选和三环境实机门仍只属于 DEV-V7-07。06R 满足红绿、逐字对账、旧文案扫描、FULLSUITE 和双轴 CLEAN，关闭为 resolved。
