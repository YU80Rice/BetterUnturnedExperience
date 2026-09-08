# case: p2p-client（SteamP2P 客机端）— CaseId `DEV-V2-24-20260908`

> 采集时填实全部 TODO；不要改动本文件结构。对应手册 §5（配置 A，Client 侧）。
> Host 与 Client 时间窗必须**正交重叠**。

- collector: TODO
- gameVersion / bepInExVersion: TODO
- startedUtc / endedUtc: TODO
- 部署配置: TODO（应 = 配置 A：仅 `BetterUnturnedExperience.dll`）

## 部署指纹

- TODO：逐件哈希输出 + LogLevels 行 + assembly-identity 行原文（= `3CBD6268…9E4D`）

## 锚行摘录（Client 侧）

| 手册步骤 | 锚行 | 出现/缺失 | 行号 |
|---|---|---|---|
| P1 启动锚 | S1 全套（加载行 / REG-ACCEPT ×5 / takeover-patch / bue-runtime-arm role=client） | TODO | TODO |
| P2 LIT 客机请求 | `[Tidy] -> 服务器: RequestTidy(reqId=N…)` + `[TidyNet] <- 服务器 TidyCommitted(reqId=N…)` + `-> 服务器 HotkeyFlowAck(reqId=N)`（reqId 与 Host 对齐） | TODO | TODO |
| P3 LIR 客机压弹 | toast「一键压弹：成功压入 N 发子弹」**截图** | TODO | — |
| P4 LHT HUD | `[HordeNet] 收到 Update: epoch=… seq=…` + HUD 条**截图** + `/horde` 回复一致 | TODO | TODO |
| P5 零误报 | 无 `BUE 错误：` 行；`(epoch,seq)` 重复键为零；原版玩法正常 | TODO | — |

## 截图 / 附件清单

- TODO：整理前后背包、toast、HUD 条、面板条目（四件官方中文名齐，含 BII=更好的物品交互）

## 结论

- TODO
