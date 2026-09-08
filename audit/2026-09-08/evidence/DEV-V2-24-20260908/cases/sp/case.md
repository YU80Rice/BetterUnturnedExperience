# case: sp（单人）— CaseId `DEV-V2-24-20260908`

> 采集时填实全部 TODO；不要改动本文件结构。对应手册 §4（配置 A）。
> 部署件哈希逐件抄自 `kit/out` 哈希表（手册 §1），与实际部署输出一致才有效。

- collector: TODO（采集人）
- gameVersion: TODO（游戏内版本号或 `Unturned.exe` 属性）
- bepInExVersion: TODO（LogOutput.log 首部 BepInEx 版本行）
- startedUtc / endedUtc: TODO（`Get-Date -AsUTC` 截图对应，`O` 格式）
- 部署配置: TODO（应 = 配置 A：仅 `BetterUnturnedExperience.dll`）

## 部署指纹（deploy-fingerprint.txt 要点）

- TODO：plugins 目录逐件 `certutil -hashfile … SHA256` 输出 + BepInEx.cfg `[Logging.Disk] LogLevels` 行
- TODO：LogOutput.log 中 `event=assembly-identity … sha256=…` 行原文（应 = `3CBD6268…9E4D`）

## 锚行摘录（逐条注明 LogOutput.log 行号）

| 手册步骤 | 锚行 | 出现/缺失 | 行号 |
|---|---|---|---|
| S1 启动 | `Better Unturned Experience 加载成功，界面已注入` | TODO | TODO |
| S1 注册 | `BUE … featureId=… accepted=True … diagnosticId=BUE-REG-ACCEPT`（×5：Better Item Interaction / BUE Network Module / BUE Inventory Tidy / BUE In-Place Reload / BUE Horde Tracker） | TODO | TODO |
| S1 网络 | `event=takeover-patch result=installed … BUE-V2NET-003` + `event=bue-runtime-arm result=armed …` | TODO | TODO |
| S1 防双装基线 | 全程 **零** `BUE-PLATFORM-001` 行 | TODO | — |
| S2 面板四条目 | 截图（含判别点 D0 结果记录） | TODO | — |
| S3 LIT 本地整理 | `[Tidy] 整理按钮补丁已安装（Harmony ID=…）` + `本地整理已提交（page=…）` | TODO | TODO |
| S4 LIR 本地压弹 | toast「一键压弹：成功压入 N 发子弹」截图 | TODO | — |
| S5 LIR×LIT 链 | `[MergeA] 整理后自动压弹完成（…）` | TODO | TODO |
| S6 LHT `/horde` | `[HordeTracker] 已注册 /horde 命令（Commander.register）` + 空态回复 | TODO | TODO |
| S7 零误报 | 无 `BUE 错误：`/`uncaught`/`拒绝` 行 | TODO | — |

## 截图 / 附件清单

- TODO：每行一个文件——路径 + 内容说明 + SHA-256

## 结论

- TODO：通过 / 与期望不符的描述（不符 = 停止采集保留现场，报 agent）
