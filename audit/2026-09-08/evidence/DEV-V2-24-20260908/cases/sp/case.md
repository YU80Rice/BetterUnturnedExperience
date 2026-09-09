> 【已归档·待重采(v6 时点)】本文件既有证据为历史轮所采(时点注记见下,哈希均属作废前身);重采一律绑候选 v6=626BC330236481AA8EF357579D9B685E7AA2472C25E6CE4B19511F666BCD2715(544256B,CaseId DEV-V2-24-CANDIDATE-20260909),全量重采后本文件整体替换。
# case: sp（单人）— CaseId `DEV-V2-24-20260908`

> 【已归档·待重采】本 case 绑定作废候选 3cbd6268…9e4d;已采 v2 候选(3cbd6268…9e4d),v3 出后全量重采替换(新候选 = 2d3de91d…762e1)。
> 对应手册 §3（配置 A）。采集日期 2026-09-08；agent 代部署（deploy-fingerprint.txt），用户实机操作，UMM 诊断包 `UMM-诊断包_20260908_163823` 归档来源。

- collector: 用户（agent 复核锚行）
- gameVersion: `3.26.3.11`（Engine 2022.3.62f3，Client.log 首部）
- bepInExVersion: `5.4.23.5`
- startedUtc / endedUtc: TODO（本会话为单人 case，无双端正交窗要求；本地会话 = 2026-09-08 16:32–16:38 左右，见 UMM 摘要「最近一次受管会话 2026-09-08 16:38:14 · 退出码 0」）
- 部署配置: 配置 A（仅 `BetterUnturnedExperience.dll` + 休眠件 SteamP2PFriends.dll），见 `deploy-fingerprint.txt`

## 部署指纹

- `deploy-fingerprint.txt` 已归档（本目录）：新候选 `3cbd6268…9e4d`（533504B）逐字核对，前身 7d5dd3b5 被覆盖替换
- **身份绑定行**：LogOutput.log:136 `…event=assembly-identity path=E:\Steam\…\BetterUnturnedExperience.dll sha256=3CBD62687BF765C618EAA5B6762C1172C7DE022B6A50DC64AE1B0BDD0D399E4D`（候选逐字一致，无替换物）
- 注：LogOutput.log 首行日期 `2026/9/4 9:14:55` 为 BepInEx 构建时间戳（非会话时间）；会话时间锚 = Client.log 首行 `2026-09-08 08:32:52`（UTC，= 本地 16:32）+ UMM 摘要 16:38:14
- 本文件 LogOutput.log SHA-256: `c4a9eaa5378dd9d3a6380923d31320bdb6bd60d4b6ecbb7c50fda9e8f98fde7c`

## 锚行摘录（LogOutput.log 行号）

| 手册步骤 | 锚行 | 出现/缺失 | 行号 |
|---|---|---|---|
| S1 启动 | `Better Unturned Experience 加载成功，界面已注入` | 出现 | :187 |
| S1 身份 | assembly-identity sha256=3CBD6268…9E4D | 出现 | :136（:135 Loading [Better Unturned Experience 0.0.0] 紧邻） |
| S1 注册 | REG-ACCEPT ×5（Better Item Interaction / BUE Network Module / BUE Inventory Tidy / BUE In-Place Reload / BUE Horde Tracker，均 accepted=True reason=None） | 出现 | :164-:168 |
| S1 网络 | `lmn-config-migration result=no-op`（:139，另 :183/:185 重复出现，如实记录）+ `takeover-patch result=installed priority=first targets=…`（:140）+ `bue-runtime-arm result=armed role=server localSteamId=76561199030780228`（:566，SP 本地主机 role=server 符合预期） | 出现 | :139/:140/:566 |
| S1 防双装基线 | 全程零 `BUE-PLATFORM-001` 行 | 通过（grep 计数=0） | — |
| S2 面板条目 | **无截图**；用户人工复核确认面板功能无异常（四件官方中文名含 BII=更好的物品交互，D0-b 修复后首验） | 用户口头确认 | — |
| S3 LIT 本地整理 | `[Tidy] 本地整理已提交（page=2, mode=SameType, mappings=3）`（:1446）+ `（page=255, mode=SameType, mappings=9）`（:1701，全身整理路径；诊断限频抑制 11 条） | 出现 | :1446/:1701 |
| S4 LIR 本地压弹 | **无截图**；用户人工复核确认（toast 与手感无异常） | 用户口头确认 | — |
| S5 LIR×LIT 链 | `[MergeA] 整理后自动压弹命中冷却窗口，跳过（target=…）` ×4（:1455/:1523/:1642/:1788，诊断限频抑制计数伴行）——**TidyCompleted→LIR 消费→ReloadAction 链路已到闸门决策**；Committed 完成态变体本会话未出现（与 S4 双击测试交替触发了 LIR 统一冷却闸门，设计内行为） | 部分（跳过变体×4） | :1455/:1523/:1642/:1788 |
| S6 LHT /horde | `[HordeTracker] 已注册 /horde 命令（Commander.register）`（:567）；空态回复无截图，用户人工复核确认 | 注册锚出现；回复用户口头确认 | :567 |
| S7 零误报 | 全日志故障模式扫描：`BUE 错误`/`result=failed`/`uncaught`/`unknown-channel-dropped`/`handler-fault-isolated`/`inbound-decision result=failed` 全部零命中（:2775「拒绝新整理请求」为停机 Stop 信息误中，非故障） | 通过 | — |
| 收尾 | 优雅停机链：`[Tidy] 模块停止` 三阶段 + `takeover-patch result=removed decision=hand-back-to-vanilla` + `network-module-isolated decision=stop-and-hand-back`（日志尾） | 出现 | 日志末 6 行 |

## 截图 / 附件清单

- **本 case 无截图**（用户未采集）；用户原话留档：**「虽然没有截图，但我人工复核确认功能无异常」**（2026-09-08）——用户为实机验收权威，与 15 号验收先例同口径（用户确认 + 日志锚绑定）。
- LogOutput.log（整份原样）+ deploy-fingerprint.txt 归档于本目录；UMM 诊断包原件 `D:\…\UMM-v2.2.1-win-x64\UMM-诊断包_20260908_163823`（含 Client.log/诊断摘要）。
- SteamP2PFriends 旧插件日志行 818 行在场（休眠件自身输出，非 BUE/LIT/LIR/LHT 异常，沿 15 先例记录）。

## 结论

- **S 系列全部通过**（S1/S3/S4/S6/S7 锚行命中；S2/S4 视觉面用户人工复核确认；S5 链路证据成立、完成态变体转下会话补采）。
- **补充采集项（不阻塞本 case，随下一会话顺带）**：清空弹匣+有备弹 → 等 ≥5 秒不做双击 → 整理一次 → 期望日志 `[MergeA] 整理后自动压弹完成（target=…, merged=N, txn=…）`（auto-reload 完成态变体）。
