# case: p2p-client（SteamP2P 客机端，虚拟机）— CaseId `DEV-V2-24-20260908`

> 【已归档·待重采】本 case 绑定作废候选 3cbd6268…9e4d;已采 v2 候选,v3 出后全量重采替换(新候选 = 2d3de91d…762e1)。
> 对应手册 §4（配置 A,Client 侧）。采集日期 2026-09-08;UMM 诊断包 `UMM-诊断包_20260908_165948`。
> **本 case 结果 = 部分 FAIL（LIT 全程不可用 = F-A 的客户端面);LIR/LHT 项通过。**

- collector: 用户（VM 侧自部署,agent 复核锚行）
- gameVersion / bepInExVersion: 3.26.3.11 / 5.4.23.5
- startedUtc / endedUtc: TODO（VM Client 会话;CLIENT_STATE t=214.8s 连入 Host）
- 部署配置: 配置 A（VM plugins 仅候选 BUE）,身份绑定行 :136 = `3CBD6268…9E4D` ✓（path=VM 路径 `C:\Program Files (x86)\Steam\…`;本文件 LogOutput.log SHA-256 `09730512…4f24`）

## 锚行摘录（Client 侧）

| 步骤 | 结果 | 证据 |
|---|---|---|
| P1 启动锚 | 通过 | :294 `bue-runtime-arm result=armed role=client localSteamId=76561199721762479`;:180 LIT 频道注册;:328-:344+ [TidyUI] 反射预热与页 2-5 按钮注入 OK |
| P2 LIT 客机整理 | **FAIL（F-A 客户端面)** | :690 用户点击（Ctrl+全身整理）→ :691 `客户端尚未收到有效服务端 session challenge;本次整理请求未发送。` → :692 `全身 整理被拒绝:尚未建立联机会话或未收到会话 challenge。`;同类拒绝全程 **80 条**（至 :1083-:1084,断线前未恢复)——RequestTidy 从未发出,Host 侧零收到 |
| P3 LIR 客机压弹 | 通过 | :867-:936 `-> 服务器: RequestRepackAmmo(reqId=…215-…222)` ×8 发出（**客机会话已建立**,SendToServer 通道正常);Host :4610 `-> 客机 RepackSuccess(reqId=…222, total=10)` 回包链通;toast 用户确认无异常 |
| P4 LHT HUD | 通过（用户确认) | Host 广播 result=Sent;Client HUD 条与 `/horde` 回复用户确认无异常 |
| P5 零误报 | BUE 侧零故障 | Error 行均属 SPF 旧插件;BUE 零 `BUE 错误：`/`result=failed` |
| 防双装基线 | 通过 | VM 侧零 `BUE-PLATFORM-001` 行 |

## 截图 / 附件清单

- 无本端截图;Host 端幽灵贴图截图见 `../p2p-host/screenshots/`。

## 结论

- 客机侧 BUE 传输/会话/频道全链正常（LIR/LHT 可用为证);LIT 因 **F-A(服务端挑战签发单发失败无重试)** 全程不可用,与 Host 侧 :2889-:2890 单条发送失败互为因果闭环。
- 按 real-machine-test-loop:修复轮出新候选后,本环境与全部环境换绑重采。
