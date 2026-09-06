# CaseId DEV-V2-08-LHT-20260905（LaunchHordeTracker）

频道：`io.github.yu80rice.launchhordetracker.horde-status` ｜ 部署指纹：env/sp/deploy-fingerprint.txt

## 环境 SP（2026-09-05，单会话）
- 启动锚：L198–203 `[Dependency] AssemblyVersion=5.0.0.0` / `[HordeNet] 已注册` / `[Lifecycle] loaded` / `[Network]` / `[Runtime] startupRole=Client` 全齐
- 意外完整生命周期：用户本局触发真实尸潮信标——L755/757 `/horde` 命令注册；L1882 `尸潮爆发 @ nav=0 epoch=1 地点=Belfast Airport 发起人=DiDATUT 总数=100`；L1883/1892 `广播 Update epoch=1 seq=1/2`（服务器侧，Debug）；L2201 `尸潮结束`；L2203 `广播 Clear epoch=1 seq=63`——Update→Clear 完整闭环
- SP 边界（手册 §3 注 2）：无远程客户端，客户端接收/HUD 证据归 P2P/U3DS
- 截图：未采集（gap）
- SP 段结论：**通过**（服务器侧广播链 + 满生命周期）

## 环境 P2P：待采集（信标广播 → Client HUD 截图 = 本 CaseId 核心证据；满月夜可后补）
## 环境 U3DS：待采集

## 环境 P2P（2026-09-06 晨）—— 本 CaseId 核心证据
- 锚行：主机 `尸潮爆发 @ nav=7 epoch=1 地点=Charlottetown 发起人=易烨不会玩FPS 总数=100`（客机玩家放置的信标）；主机 `广播 Update` ×10 + `广播 Clear: epoch=1 seq=11`；**客机 `收到 Update` ×10（seq=1..10 连续）+ `收到 Clear: epoch=1 seq=11`**
- **恰好一次实锤：主机广播 10 = 客机收到 10，seq 连续无缺无重复键（不可靠通道上 1:1）；Clear 双端对齐**
- 会话门：双端 release 锚 + delegate 零 ✓
- 客机 HUD：用户确认双端功能无异常（HUD 截图未采集，gap）
- P2P 段结论：**通过**（网络接收 + 恰好一次 + 生命周期完整）

## 环境 U3DS（2026-09-06 上午）
- 锚行：U3DS `尸潮爆发 @ nav=10 epoch=1 地点=Alberton 发起人=DiDATUT 总数=100` → `广播 Update` ×16 → `尸潮结束` → `广播 Clear: epoch=1 seq=17`；**客户端 `收到 Update` ×16（1:1 无重复键）**
- 时序注记：客户端先于尸潮结束离场（客户端包 09:05:50 / 服务器 09:06），故本端无 `收到 Clear` 行——Clear 接收已由 P2P 轮实锤（epoch=1 seq=11 对齐）
- 会话门：双端 release 锚 + delegate 零 ✓
- U3DS 段结论：**通过**
- **CaseId 总结论：三环境全过——SP 服务器侧广播+满生命周期（seq=63）、P2P 客机收播+Clear 对齐+恰好一次、U3DS 专用服务器 16:16 1:1；无迁移缺陷**
