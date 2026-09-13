# POST-P4-05 审查链：显式种类无条目不回落红测补钉

- 票：`.scratch/bue-post-phase4-closure/issues/05-kind-no-fallback-red-test.md`（enhancement·测试缺口，生产行为已由 DEV-V4-09 F4 修妥）
- 增量：`tests/BetterUnturnedExperience.ClientUi.Tests/DevV4DraftTests.cs` +62 行（新用例 `ExplicitKindWithoutMatchingRowOpensNoDraft` + `Run()` 接入）。**src/ 零改动**（`git diff --exit-code -- src/` 实证），不触契约（仍 2.1），非发布票不授候选。

## 验收对照

- [x] **常跑断言**：新用例经 `DevV4DraftTests.Run()`（Program.cs:19 常跑）接入 F4 碰撞组尾部。
- [x] **绿钉+曾能失败证明**：生产已满足 → 基线即绿（`green-sln-rebuild.log.txt` 0 错 0 警 + ClientUi 全组 PASS）；再以两处临时突变证伪——
  - M1=删 `ManagementPanel.cs` 「显式功能种类无条目→return」守卫：`red-mutation-M1-run.log.txt` exit=1，红在本案第 1 段断言「插件唯一行+显式功能种类=如实无草稿（不回落插件命名空间）」；
  - M2=插件种类早退改为「未命中漏给 features-first」：`red-mutation-M2-run.log.txt` exit=1，红在本案第 2 段断言「功能唯一行+显式插件种类=如实无草稿（不回落功能命名空间）」。
  两次突变均随后还原，终态 rebuild=`green-final-sln-rebuild.log.txt`（0/0）。
- [x] **不改变 F4 生产语义**：单参 features-first 兼容锚在本案第 3 段作对照钉（同 id 单参仍成稿，证明 1)/2) 是拒绝回落而非拒绝打开）；第 4 段钉 `TryLeaveDetail` 带目标种类导航到无条目行→离开后如实无草稿。
- [x] **全套测试绿**：7 个测试 exe `green-fullsuite-*-run.txt` 全 exit=0；`eng/Verify-TestFixturesTracked.ps1` PASS（gate-verify-fixtures.txt）。
- [ ] 范围外：收藏/徽章（票 03）、路由改动——零触碰。

## 用例钉法说明

四段单行形态：①只插件行×显式功能种类→无草稿（HasOpenDetail/OpenStableId/双通道编辑拒/IsDirty/SaveDraft=NoChanges 权威源零写）②只功能行×显式插件种类→无草稿+编辑拒 ③同 id 单参对照锚 ④脏功能草稿 Save 导航到不存在的插件种类行=受理、只写功能源、落点无草稿。「不渲染另一行的设置」在模型缝如实钉：原生详情页由草稿渲染（BueNativeManagementPanel.cs:971/1319 以行 (StableId, Kind) 调 OpenDetail），无草稿=无详情页。

## 双轴链（每轮全新实例，standards-reviewer / Spec-Reviewer）

- **R1 Standards：CLEAN**。无硬违规；2 条判断类气味具名递延（本票为纯测试票，递延不阻断关单）：
  1. 断言不对称（DevV4DraftTests.cs 第 2 段未镜像第 1 段的 IsDirty/SaveDraft 面；其证伪牙在「功能编辑拒」，`DraftEditPluginConfig` 拒不能单独证伪 M2）；
  2. 计数器跨段共享（第 4 段 `BatchCalls==1` 依赖 1–3 段零写，顺序依赖有注释但累计语义未如碰撞组点明）。
- **R1 Spec：CLEAN**。三条验收逐条 PASS+证据；范围核查零蔓延；无须递延项。

链=单轮 R1 双轴全 CLEAN，无 finding→无修复轮。

## 门禁基线说明

`eng/Verify-NoUiTokens.ps1` 运行留档为参考件（gate-verify-nouitokens.txt），不作本票验收门禁：其 FAIL=既有基线漂移，已在 POST-P4-04 关单具名为仓库级另案（audit/2026-09-13/POST-P4-04/review-loop.md:35），本票 src 零改动不引入、不加重。
