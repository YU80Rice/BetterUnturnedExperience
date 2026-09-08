# case: coexistence（V1 旧插件共存承诺 — BUE + 独立 LMN 部署）— CaseId `DEV-V2-24-20260908`

> 对应手册 §8（配置 B：BUE + LaunchMultiplayerNet.dll + LmnEcosystemFixture.dll）。
> 验证对象 = 「BUE + 独立 LMN」部署下未知 V1 旧插件继续收发；fixture 为普通 LMN 消费方（V1 数字频道 ch250 + V2 命名频道），即旧插件替身。
> 已承认边界随证据记录：裸 BUE（无独立 LMN）环境下 V1 旧插件不工作——这不是缺陷。

- collector: TODO
- gameVersion / bepInExVersion: TODO
- startedUtc / endedUtc: TODO
- 部署配置: TODO（应 = 配置 B 三件，逐件哈希）

## 部署指纹

- TODO：三件哈希输出 + LogLevels 行 + assembly-identity 行原文（= `C9B6B6E4…EB86`）

## 锚行摘录（建议在单人环境采集，约 10 分钟）

| 手册步骤 | 锚行 | 出现/缺失 | 行号 |
|---|---|---|---|
| B1 启动 | takeover-patch installed + `[LMNFIX] ready … lmnOperational=True channels=v1-server=ok;…` | TODO | TODO |
| B2 V1 镜像 | `[BUE-V2NET] event=v1-table-mirror result=mirrored channels=…` | TODO | TODO |
| B3 V1 回环 | 每 10 秒 `[V1FIX] send-broadcast …` + `[V1FIX] recv-from-server kind=ping …` + pong 往返（seq 递增） | TODO | TODO |
| B4 V2 放行 | `[V2FIX]` 回环同构 + `[BUE-V2NET] event=lmn2-frame-release result=released decision=lmn-native-dispatch`（一次性） | TODO | TODO |
| B5 V1 放行 | `[BUE-V2NET] event=v1-frame-release result=released decision=lmn-native-dispatch`（一次性） | TODO | TODO |
| B6 零误报 | 无 `BUE 错误：` 行；无 `unknown-channel-dropped`；原版玩法正常 | TODO | — |

## 截图 / 附件清单

- TODO（本 case 以日志为主；`LogOutput.log` 整份归档于本目录）

## 结论

- TODO（V1 旧插件共存承诺：成立 / 不成立+描述）
