# DEV-V7-03 被动压弹恢复/实施审计

- Ticket: `17-DEV-V7-03-passive-reload.md`
- Branch: `main`（本票工作已纠正回主线；不授候选）
- Scope: 恢复缺失票据并在当前 `main` 旧 V5-07 实现上实施 2 级独立被动周期；不改 Contracts/SDK/HUD 文案，不授候选、RELEASES、CaseId。
- Baseline: 当前 `main` 仍是“手动成功后排一轮”的旧实现；历史 `1665d5e` 不作为当前交付基线。

## 事实边界

- V7-07 没有 DEV-V7-03 的具名候选缺陷；只有主机权威被动压弹尚未人工复核。唯一具名相邻退回属于 DEV-V7-02 HUD，明确不扩大到本票。
- 宿主外测试不能构造真实 `PlayerInventory`、`ItemMagazineAsset`、`FillTargetItem` 蓝图和 U3DS/P2P 实机；该 seam gap 必须保留并交由 V7-07 主机权威人工门覆盖，不用假观察冒充实机证据。
- 第二轮 Spec 复审提出的 V7-04 账本身份/UI 几何事项属于 DEV-V7-04 scope mismatch，本票不改技能账本或技能 UI；最终 Spec 复审确认不构成本票阻塞。

## 红→绿记录

- 首轮红：新增 `--bue-v7-03-passive-reload-red` 后执行 `dotnet msbuild BetterUnturnedExperience.sln -t:Rebuild -p:Configuration=Release -v:m -nologo`，`BUILD_EXIT_CODE=1`，`ERROR_COUNT=11`；错误集中在新 `Sync`/`TickPassive` seam 尚未接入。
- 中间修复轮：生产程序集先通过；迁移 `DevV5ReloadSkillTests` 的旧三参数入口、一次性指纹/单轮断言和测试假件 `IsPlayerAvailable` 后，Release Rebuild 通过。
- 绿验证轮：Release Rebuild `exit=0`；`--bue-v7-03-passive-reload-red` PASS；`--bue-v5-07-reload-skill-red` PASS；Plugin.Tests 无参全套 PASS（V7-02、V7-03、V5-03、V5-04、V5-05、V5-06、V5-07、V4-08、UPM、DEV-14/16B）。
- 最终修复后再次执行上述 Rebuild、两组单组和 Plugin.Tests 全套，全部 `exit=0`；构建输出无错误。

## 实现与审查链

- 实现：`ReloadAutoRoundScheduler` 改为按玩家持续 8 秒周期，资格由 2 级与存活/可解析判定，资格丢失和代际停止清窗；`InPlaceReloadModule` 在唯一 HostTick 主机侧收集本机/已建立 peer，执行候选同步和被动尝试，按玩家记录同帧手动互斥，客机清表，停用/off→on 清窗；`LirRepackNetwork` 拆出无 requestId 被动主机入口，复用 `AmmoRepackService.TryRepackTransactional` 的权威事务，被动成功不 toast、不发送 `RepackSuccess`，并移除 `wireId=0`/`auto=1` 语义；`IsPlayerAvailable` fail-closed 接入技能钩子；被动 `PlayerMissing`、`RejectedCooldown`、`RestoreFailed`、`RolledBack`、`AbortedStateDrift` 均有结构化主机日志。
- Round 1：全新 `standards-reviewer` = CLEAN（仅非阻断 smell；HostTick、互斥、协议隔离、公共 seam 清洁）。全新 `Spec-Reviewer` = BLOCKING：被动失败结果日志不完整；真实库存/三环境为命名 seam gap。修复：为被动所有权威失败 outcome 增加结构化日志；真实库存/三环境继续交由 V7-07，不伪造宿主证据。
- Round 2：全新 `standards-reviewer` = CLEAN。全新 `Spec-Reviewer` 提出三项，其中 V7-04 账本/UI 两项超出本票范围，`RestoreFailed` 被动日志仍缺结构化字段；未扩大范围翻修 V7-04，补齐被动 `RestoreFailed` 的 `outcome=RestoreFailed`、steam、host/quarantine 字段。
- Round 3（最终）：全新 `standards-reviewer` = CLEAN；全新 `Spec-Reviewer` = CLEAN。最终审查明确：V7-04 scope mismatch 不阻塞；真实 `PlayerInventory`/`FillTargetItem` 与 SP/P2P/U3DS 仍是 V7-07 命名 seam gap，不伪造通过。
- 三轮均使用全新独立审查实例，未续用审查上下文；两轴最终 verdict 均 CLEAN。

## 候选纪律

本票不授候选、不更新 `audit/RELEASES.md`、不生成 CaseId；V7-07 仍负责三环境人工复核和唯一对外发布门。用户已有 DEV-V7-02/CONTEXT/ADR/artifacts/third-party 改动不属于本票，提交时按文件隔离。
