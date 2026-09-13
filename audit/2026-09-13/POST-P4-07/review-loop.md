# POST-P4-07 审查链：管理面板 refreshModel 三处复制收成一处

- 票：`.scratch/bue-post-phase4-closure/issues/07-refresh-model-dedup.md`（enhancement·重构：F2 保存钮/确认留页/确认离开（脏刷新含）三条路径的「先重建目录再画当前详情」收成单一入口）
- 增量：`src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs`（`RefreshCatalogThenPaintCurrentDetail` + 三路径改调）、新增 `eng/Verify-RefreshModelDeduped.ps1`（本票红测缝=方法切片区域绑定）。不触契约（仍 2.1），不授候选，`audit/RELEASES.md`/`publish/` 零触碰。tests/ 零改动（F2 行为仍由既有 `SaveCommittedLifecycleIntentFlag` 钉）。
- 固定点：HEAD `85f8e6a`。R1 冻结 diff=`review-freeze-r1.txt`。清单=`changed-files-r1.txt`。

## 验收对照

- [x] **F2 回归组仍绿**：ClientUi `SaveCommittedLifecycleIntentFlag` 随全套 PASS；helper 门仍是 `forceRefresh || CommittedLifecycleIntent`（设置-only / 被拒意图不刷新）。
- [x] **三路径不再各写一份刷新序列**：审查可指出单一入口 `RefreshCatalogThenPaintCurrentDetail`（save-stay=Full；confirm-stay=Details；confirm-leave=Full 或关面板 None）。
- [x] **全套测试绿；双轴 CLEAN**：Rebuild 0/0 + 7/7 + fixture 门禁 PASS；R1 双轴全新实例全 CLEAN。
- [x] **不授候选**。

## 红绿链（红测缝=eng 门禁）

- 具名 seam gap：`BueNativeManagementPanel` 是 Glazier 适配器，纯宿主不可构造（F3/F4 先例）。行为面继续钉在 ClientUi 模型组；结构面=新门禁禁止三路径内联 `refreshModel()`。
- 红=门禁对未抽入口的面板：`red-gate-run.txt` exit=1、7 违例（helper 缺失 + Commit/ResolveConfirm 内联 + 两段残句）。
- 绿=抽入口后：`green-gate-run.txt` exit=0。
- 突变（临时树，正文不回潮）：M1 保存腿回内联 / M2 改 helper 签名 / M3 丢掉 `forceRefresh ||` 门 / M4 改 F2 测试签名=`red-mutation-M1..M4-run.txt` 各 exit=1。
- 全套：Rebuild 0 警 0 错（`green-sln-rebuild.log.txt`）；7 exe 全绿（`green-fullsuite-*-run.txt`）；`Verify-TestFixturesTracked.ps1` PASS（`gate-verify-fixtures.txt`）。

## 双轴链（每轮全新实例，standards-reviewer / Spec-Reviewer）

- **R1**：Standards CLEAN（硬性 0；4 条气味审视后均不成立，无递延项）+ Spec CLEAN（单一入口/F2 门/确认三选一/范围外两段同构均 PASS）。无 finding→无修复轮。

链=单轮 R1 双轴全 CLEAN。

## 具名说明

- Open / 干净 `RequestRefresh` 仍直接 `refreshModel(); Render();`——不是 F2 生命周期门，票第三条=脏刷新走确认离开的 `wasRefresh`。
- 关面板离开：`AfterCommitPaint.None` 先刷目录再 `Close()`，不画一帧（与抽入口前顺序相同）。
- 插件草稿填充两段同构按票未升格。
- 不授 SHA-256 / CaseId / RELEASES 行。
