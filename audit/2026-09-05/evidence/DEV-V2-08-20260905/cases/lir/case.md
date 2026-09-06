# CaseId DEV-V2-08-LIR-20260905（LaunchInPlaceReload）

频道：`com.yu80rice.launchinplacereload.repack` ｜ 部署指纹：env/sp/deploy-fingerprint.txt

## 环境 SP（2026-09-05，单会话）
- 锚行：L207 `UnityMainThreadId=1 recorded…`；L210 `已注册命名频道=… 服务器端+客户端处理器`；L1812 `dispatcher summary: dispatches=0 … rejected=1`（SP 主机直调路径不入队，dispatches=0 为预期；rejected=1 = 一次尝试被冷却闸门静默拒，防护设计）；L3003 `已注销命名频道`（退出 OnDestroy 路径，LMN 先行清表故 removed=False，良性）
- 行为确认（用户口头）：双击三次均看到「一键压弹：成功压入 N 发子弹」toast（SP 成功路径仅 UI 无日志，toast 即证据）
- 截图：未采集（gap）
- SP 段结论：**通过**

## 环境 P2P：待采集（客户端双击 R → 跨端命名频道请求，为本 CaseId 核心证据）
## 环境 U3DS：待采集

## 环境 P2P（2026-09-06 晨）
- 锚行：主机 `dispatcher summary: dispatches=1` ×2（跨端请求真实入队派发）+ `rejected=0`；客机 `dispatcher summary: dispatches=1`（收到成功回包）
- 计数语义：主机派发 2 = 客机两次双击请求；客机回包 1 = 另一次为 `RepackOutcome.NoChange`（弹匣已满，按设计不回包不弹 toast，代码路径 NoChange 不 Forget）——用户确认双端功能无异常
- 会话门：同 LIT（双端 release 锚 + delegate 零）
- 截图：未采集（gap）
- P2P 段结论：**通过**
