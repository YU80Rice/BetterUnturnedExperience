# DEV-V4-03 审计·生命周期目标提交与空操作语义

- 票：`.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-03-lifecycle-target-submit.md`
- 规格：`spec.md`「生命周期目标提交（V4-T4 → DEV-V4-03）」+「共享规则（V4-T1）」；裁决 `issues/04-t4-feature-toggle.md`
- 日期：2026-09-12；会话：Phase-4 实施票 03（依赖 01=aaf7271、02=1382015 均已 resolved，阻塞解除）
- 构建：MSBuild（VS 18 Insiders）Release `-t:Rebuild` → **0 警告 / 0 错误**（`final-build-console.txt`）
- 测试：**全套 7 运行器全 PASS**（`green-fullsuite.txt`，ClientUi 标签含 DEV-V4-03）；静态门：`Verify-NoUiTokens.ps1` Core 28 文件 PASS、模型两文件 UI/native token=0、`git diff --check` exit 0（`static-gates.txt`）
- **候选纪律（本票 03）**：不产正式候选 DLL、不更新 RELEASES、不授 CaseId。契约仍 2.1（Contracts/SDK 零入 diff）。

## 交付摘要

把面板启停缝 `BueFeatureStartRuntime.SetFeatureEnabled` 的入参语义从「动作重放」升格为「**目标状态提交**」：面板只提交目标启用/停用，机按提交时权威状态解释。修的是机，不是面板 if——ClientUi 零改动（`ManagementPanel.cs` 不入 diff，无按 Isolated 写死的停用分支）。

解释表（机内，`internal static` 面不变）：

| 提交时权威状态 | 目标启用 | 目标停用 |
|---|---|---|
| Running | **空操作成功**（enable-noop/already-running，现网 invalid-state 拒） | UserDisabled 停止（不变） |
| Stopped/Disabled | 启用·新代际（不变） | **空操作成功**（disable-noop/already-stopped，现网 not-running 拒） |
| Isolated | 恢复·新代际（不变） | **空操作成功·保持隔离**（disable-noop/isolated-kept：不走会失败的 disable、不新开代际，现网 not-running 拒） |
| Starting/Stopping/Isolating/Discovered/Incompatible/未注册 | 显式失败 | 显式失败 |

不允许转换 → 失败：调用方（01 草稿模型）把该启停意图留在草稿并报「未保存：功能启停失败。」（跨源部分成功语义，ClientUi 锚 `ToggleFailureKeepsIntentInDraft`）。目标差异投影不进 SDK；结构化日志新增 `enable-noop` / `disable-noop`（reason=already-running / already-stopped / isolated-kept），与既有 `event=feature-panel` 词汇同形。

## 红测先行链

1. 红测锚（先于实现落盘）：
   - `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` 新增「目标提交空操作」组 A–F：A Running+启用空操作 / B 已停+停用空操作 / C Isolated+停用空操作（修订与代际三不变）/ D Isolated+启用恢复新代际 / E Starting 过渡期重入双向拒绝 / F 未注册启用拒绝。
   - 旧锚翻转：NoOp「隔离缝」组原「对 Isolated 发停用=not-running 拒」翻转为空操作成功+保持隔离（StateRevision 与隔离态逐项稳定）。
   - 官方先行消费锚扩展：官方 LIT Running 再提交启用=空操作成功、实例不变（无新代际）。
   - `tests/BetterUnturnedExperience.ClientUi.Tests/DevV4DraftTests.cs` 增 `ToggleFailureKeepsIntentInDraft`（拒绝→草稿保留）。
2. **红已观察**：`red-run.txt` —— Plugin.Tests 红在「目标提交：Running 再提交启用=空操作成功（现网 invalid-state 拒，先红）」，即现网失败码路径（B/C 对应 `disable-rejected not-running`）。
3. **绿**：最小实现（仅 `BueFeatureStartRuntime.SetFeatureEnabled` 分发表 + 方法文档）后双测试 exe 绿，全套 7 工程 exit=0。

## 双轴独立审查链

- **Round 1**（2026-09-12，两个全新实例，diff=`dev-v4-03-tracked.diff`）：
  - Standards 轴（standards-reviewer）：无 finding。日志词汇同形、锁外诊断、无 FeatureId 散表/第二事实源、ClientUi 无分支回流、契约面未动、测试只测外显行为复用既有夹具 → **CLEAN**。
  - Spec 轴（Spec-Reviewer）：七项核对全过（三类空操作红→绿、恢复新代际、不允许转换失败+草稿保留、修机不修面板、官方锚、范围红线、候选纪律）→ **CLEAN**。
- 无修复轮次；终态即 Round 1 双 CLEAN。

## 边界与移交

- 详情页「启用」开关外观、状态投影九态中文、目标与当前分离展示 = DEV-V4-05；legacy `*.enabled` 迁移 = DEV-V4-04（将消费本票目标提交语义）；本票不授候选/CaseId。
