# DEV-V7-07 本轮实施审计（候选准备轮）

日期：2026-09-22  
状态：**候选准备完成，等待 SP/P2P 人工三环境门与用户批准；未关单、未授 CaseId、未更新 RELEASES、未生成正式交付包**。

## 范围与隔离

本票是验证与发布票，不夹带生产修复。HEAD 基线为 DEV-V7-06 提交 `d57ccc6`；本轮没有修改生产源码、契约、玩家手册或用户既有工作树改动。用户既有 `CONTEXT.md`、`docs/adr/0003-bue-auto-rotation-edge-fit-decision.md`、`docs/third-party/unturned-plugin-dev/` 未纳入本轮。

本地过程证据目录：`audit/2026-09-22/DEV-V7-07/`。候选构建目录：`artifacts/release-candidate/DEV-V7-07-r1..r3/`。两者按仓库纪律被 `.gitignore` 忽略，不进入公开树。

## 红/绿与静态门

- `eng/Run-FullSuite.ps1` 完整运行通过：Build exit=0；Contracts、Network、Placement、Settings、ClientUi、Plugin、Release 七套测试均 exit=0；DeveloperHandbook、ContractDocs、NoUiTokens:Core、RefreshModelDeduped、SpecV4R9Ingested、TestFixturesTracked、TestRunnerHostDlls 均 PASS；NoUiTokens:Contracts 仅有已登记的 `ContractTypes.cs:Glazier` known baseline；Firewall `violations=0`。
- 汇总：`steps=17 pass=16 failed=0 known-baseline=1`，`FULLSUITE: PASS`。
- FULLSUITE 原始目录：`artifacts/fullsuite/20260922-212642/`。该轮没有新增红测组；符合票面“全套回归 + 实机门”。

## 候选身份

发布期以 `eng/Invoke-ReleaseRepack.ps1 -Rebuild -OutDir ...` 连续执行三轮八工程合并。三轮每轮均 exit=0、输出 762880 字节，逐字节一致：

`3588EB7D18017DFB012950CB6B51668E552768C713195E7B30C17CDFCA35F8C6`

逐轮证据：

- `audit/2026-09-22/DEV-V7-07/rebuilds/rebuild-1-sha256.txt`
- `audit/2026-09-22/DEV-V7-07/rebuilds/rebuild-2-sha256.txt`
- `audit/2026-09-22/DEV-V7-07/rebuilds/rebuild-3-sha256.txt`
- `audit/2026-09-22/DEV-V7-07/candidate-sha256-final.txt`

候选只作为待人工复核的候选准备物，不等同于已发布候选。当前未改 `audit/RELEASES.md`，未创建 `publish/第七阶段-正式交付版本/`。

## U3DS 权威门

使用 V7 候选临时替换装置脚本的上一阶段固定路径，验证结束后已恢复原 `DEV-V6-10-r3` 文件；没有残留备份或改动旧发布基线。

- `Run-U3dsSevenSeam.ps1` build exit=0。
- 运行时主 DLL deployed/runtime `assembly-identity` 均为上述 V7 SHA-256。
- `BUE-NoOpProbe-results.txt`：probe-loaded、bootstrap/events/lifecycle/network/hosttick/settings/logger 七步全部 `Passed`、`chain-complete`。
- U3DS 判决：`U3DS-SEVEN-SEAM: PASS (identity-bound)`。
- 证据：`audit/2026-09-22/DEV-V7-07/u3ds/`。
- `[ARM] ... found=False` 为既有 headless 诊断信息，不是本装置失败判据。无 Steam 时 network feature-isolated/NoOp warning/error 也仅为探针预期诊断；七缝旁路、候选身份和链完成判据均通过。

## 三环境人工复核准备

最小流程已写入 `audit/2026-09-22/DEV-V7-07/three-env-manual-checklist.md`，包括部署身份核对、SP 玩家可见行为、P2P 主机/客机权威边界、U3DS 权威行为与人工记录要求。

当前状态：

- [ ] SP：尚未由用户完成人工画面/手感复核。
- [ ] P2P：尚未由用户完成主机/客机跨端复核。
- [x] U3DS headless 子门：七缝及身份绑定已通过。
- [ ] 独立服务器人工权威行为：整理提交、2 级被动压弹、技能扣经验尚未由玩家/维护者复核。
- [ ] 用户人工批准：未发生。
- [ ] RELEASES 新行 / CaseId / 正式交付包：按纪律不得提前执行。

发现缺陷时必须退回 DEV-V7-01..06 对应行为票或文案票；本票不接生产修复。

## 双轴审查链

本轮以两个全新实例独立审查当前增量：

- Standards reviewer：**CLEAN**，无硬性违规；3 项判断性 smell（重复候选留痕的命名/边界澄清、`final` 文件名与未完成人工门的状态语义、哈希文字重复）不阻断。
- Spec reviewer：**CLEAN（当前增量）**；确认 FULLSUITE、候选身份和 U3DS 证据满足当前准备轮，但指出 SP/P2P、用户批准、RELEASES、正式交付包和最终关单仍是后置门，不能宣称整票完成。

审查规则依据：`docs/agents/output-review-loop.md`。因本轮新增的是候选准备证据与人工流程，不存在生产代码修复轮；后续 SP/P2P 通过并形成发布工件后，必须对最终增量重新派发两个 fresh reviewer 实例。

## 下一步门槛

1. 用户按 `three-env-manual-checklist.md` 完成 SP 与 P2P 人工复核并回收日志/诊断包。
2. 若发现缺陷，退回对应票修复，重新走红测、FULLSUITE、三轮候选和双轴审查；不得在本票塞补丁。
3. 仅当三环境均通过且用户明确批准后，追加 `audit/RELEASES.md` 新行、生成第七阶段正式交付包、授 CaseId，并将本票关单。
