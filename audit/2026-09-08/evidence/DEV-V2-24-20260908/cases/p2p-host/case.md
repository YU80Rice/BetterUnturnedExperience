# case: p2p-host（SteamP2P 主机端）— CaseId `DEV-V2-24-20260908`

> 采集时填实全部 TODO；不要改动本文件结构。对应手册 §5（配置 A，Host 侧）。
> Host 与 Client 时间窗必须**正交重叠**（不是首尾相触）。

- collector: TODO
- gameVersion / bepInExVersion: TODO
- startedUtc / endedUtc: TODO（`Get-Date -AsUTC` 截图对应）
- 部署配置: TODO（应 = 配置 A：仅 `BetterUnturnedExperience.dll`）

## 部署指纹

- TODO：逐件哈希输出 + LogLevels 行 + assembly-identity 行原文（= `3CBD6268…9E4D`）

## 锚行摘录（Host 侧）

| 手册步骤 | 锚行 | 出现/缺失 | 行号 |
|---|---|---|---|
| P1 启动锚 | S1 全套（加载行 / REG-ACCEPT ×5 / takeover-patch / bue-runtime-arm role=server） | TODO | TODO |
| P2 LIT 服务器权威 | `[TidyNet] 快捷键快照已验证：…` + `[TidyNet] -> 客机 TidyCommitted(reqId=N…)`（reqId 与 Client 对齐） | TODO | TODO |
| P3 LIR 服务器派发 | `[RepackNet] dispatcher summary: dispatches=…`（≥1；5 秒汇总窗） | TODO | TODO |
| P4 LHT 广播 | `[HordeNet] 广播 Update: epoch=… seq=…` + `[HordeTracker] 尸潮爆发 @ …`（Clear 可选） | TODO | TODO |
| P5 零误报 | 无 `BUE 错误：` 行；无重复派发信号；原版玩法正常 | TODO | — |

## 截图 / 附件清单

- TODO（Host 面板条目截图可复用 sp 侧；本端以日志为主）

## 结论

- TODO
