# case: u3ds（U3DS Headless 专用服务器）— CaseId `DEV-V2-24-20260908`

> 采集时填实全部 TODO；不要改动本文件结构。对应手册 §6（配置 A）。
> U3DS 无 UI，证据以日志为准；表现面（HUD/面板）在客户端侧采。

- collector: TODO
- gameVersion / bepInExVersion: TODO
- startedUtc / endedUtc: TODO
- 部署配置: TODO（应 = 配置 A：仅 `BetterUnturnedExperience.dll`）

## 部署指纹

- TODO：逐件哈希输出 + LogLevels 行 + assembly-identity 行原文（= `3CBD6268…9E4D`）

## 锚行摘录（U3DS 侧）

| 手册步骤 | 锚行 | 出现/缺失 | 行号 |
|---|---|---|---|
| U1 启动锚 | `Better Unturned Experience 加载成功（无界面）` + `event=runtime-gate decision=Headless … BUE-BOOTSTRAP-002` + `status=BootstrapReady decision=Headless` | TODO | TODO |
| U1 注册锚 | `BUE … accepted=True … BUE-REG-ACCEPT` ×5（含 BUE Network Module）+ 三功能注册行（TidyNet/RepackNet/HordeNet） | TODO | TODO |
| U1 防双装基线 | 全程 **零** `BUE-PLATFORM-001` 行 | TODO | — |
| U2 LIT 服务器权威 | `[TidyNet] 快捷键快照已验证…` + `-> 客机 TidyCommitted(reqId=N…)`（reqId 对齐） | TODO | TODO |
| U3 LIR 派发 | `dispatcher summary: dispatches=…` | TODO | TODO |
| U4 LHT 广播 | `广播 Update: …` + `尸潮爆发 @ …`（管理员信标，满月夜，可分离采集） | TODO | TODO |
| U5 收尾 | 零误报；控制台正常退出无崩溃 | TODO | — |

## 截图 / 附件清单

- TODO（U3DS 控制台照片/截图可选；客户端侧截图归 p2p-client 或本文件注明）

## 结论

- TODO
