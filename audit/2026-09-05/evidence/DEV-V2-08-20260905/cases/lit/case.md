# CaseId DEV-V2-08-LIT-20260905（LaunchInventoryTidy）

频道：`com.yu80rice.launchinventorytidy.net` ｜ 部署指纹：env/sp/deploy-fingerprint.txt（=kit 五件全对）

## 环境 SP（2026-09-05，单会话；结束 ≈ 15:47:34Z / 23:47+0800）
- 锚行（LogOutput.log，已归档 env/sp/）：L1157 `RequestTidy(reqId=1…)`；L1169–1173 完整链 `TidyCommitted → HotkeyFlowAck → ACK 处理完成`；L1452/1465–1469 reqId=15（15s 限频抑制 13 条，机制按设计工作）
- 行为确认（用户口头）：打乱背包后按键看到物品实际重排，功能可正常使用
- 截图：未采集（gap，以日志锚 + 口头确认代偿）
- 观察项 O-LIT-1（非本票 finding）：排列算法质量用户主观认为有问题，用户拍板「后续再说」→ 记入官方纳入票议程；与 V2 迁移无关（网络链完整）
- SP 段结论：**通过**（网络链证据完整；业务效果用户确认可见）

## 环境 P2P：待采集
## 环境 U3DS：待采集

## 环境 P2P（2026-09-06 晨，双端单会话；客机包 08:51:30+0800 / 主机包 08:52:02+0800）
- 锚行（env/p2p-client/、env/p2p-host/）：客机 L？`RequestTidy(reqId=1, page=5)` → `<- 服务器 TidyCommitted(reqId=1)`；主机 `-> 客机 TidyCommitted(reqId=1/2…)` + `服务器已提交整理` ×4（客机 3 + 主机自整理 loopback 1，主机自发的 `RequestTidy` ×1 佐证）——**跨端命名频道请求-响应闭环，reqId 双端对齐**
- 会话门：双端 `takeover-patch installed` 各 1 + `lmn2-frame-release result=released` 各 1 + `lmn2-delegate` 双端 0 ✓
- 无重复派发：客机 RequestTidy ×3（限频被拒另计，主机 `[RateLimit]` ×5 为防护性拒绝，良性）
- 截图：未采集（gap，以日志锚 + 用户「双端功能无异常」确认代偿）
- P2P 段结论：**通过**

## 环境 U3DS（2026-09-06 上午，客户端包 09:05:50+0800 / 服务器日志 09:06+0800）
- 锚行（env/u3ds/、env/u3ds-client/）：U3DS `加载成功（无界面）` + `takeover-patch installed` + `[Runtime] startupRole=DedicatedServer; clientUi=False`；客户端 `RequestTidy(reqId=1, page=2)` → U3DS `服务器已提交整理（reqId=1, page=2）` + `-> 客机 TidyCommitted(reqId=1)` → 客户端 `<- 服务器 TidyCommitted(reqId=1)`——**跨端闭环经专用无头服务器，reqId 对齐**
- 会话门：双端 release 锚各 1 + delegate 双端 0 ✓；U3DS 退出线 `takeover-patch removed decision=hand-back-to-lmn`（正常清理）
- 零误报 ✓（U3DS 唯一命中=LIT RateLimit 防护 ×1+退出 BeginQuiesce）
- 截图：未采集（gap）
- U3DS 段结论：**通过**
- **CaseId 总结论：SP/P2P/U3DS 三环境全过，无 V2 迁移缺陷；O-LIT-1（排列算法质量）为非迁移观察项→官方纳入票议程**
