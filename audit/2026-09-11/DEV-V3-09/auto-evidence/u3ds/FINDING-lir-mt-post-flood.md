# DEV-V3-09 三环境验收·U3DS 实机发现：LIR→平台 dispatcher 每帧投递日志风暴

## 候选身份
- Candidate SHA-256 = `471EB245ED3E914B5BBE585E5842E3AF32EBD84335A13EF8E620AEAC46658FB0`（601600 B，3× `-t:Rebuild` 逐字节一致，见 `candidate-sha256.txt`）
- CaseId = `DEV-V3-09-CANDIDATE-20260911`
- 部署指纹双端 `certutil` = 候选值（client 新增 / U3DS 替换 v7 `a1b339bf…71359`），全 MATCH

## U3DS headless 启动证据（正例，Steam 未登录态）
- `event=assembly-identity … sha256=471EB245…658FB0` = 候选绑定，精确匹配
- 五个官方 2.0 时代功能全部 `accepted=True reason=None diagnosticId=BUE-REG-ACCEPT`：BII / BUE Network / LIT / LIR / LHT
  → **2.0 模块在 2.1 宿主继续可注册 = 非破坏回归，实机成立**
- DEV-V3-07 统一 `BUE diagnostic-summary` 按 featureId 聚合运行（11 行：BUE-LIFE-STATE / BUE-LIFE-ACCEPT / BUE-LIT-START / BUE-LIFE-ISOLATE）
- DEV-V3-04 主线程 dispatcher 运行（LIR 官方先行消费者）
- DEV-V3-03 生命周期隔离运行：`io.github.yu80rice.bue.network` + `network.v1compat` 因 Steam 未登录传输不可初始化 → `event=feature-isolated stage=start-result error=ModuleStartFailed` → `to=Isolated`；**单功能失败被隔离、其余功能继续运行**（隔离兜底实机正例）；MT dispatcher + settings-registry 随之 `owner-invalidated reason=start-result`
- `[Error` 级行数 = 0（无未处理错误）

## 发现的缺陷（阻塞候选）：每帧投递 + 每投递一行 Debug 日志 = 日志风暴
**现象**：`LogOutput.log` 以约 63 行/秒增长（实测 <2 分钟 5461→7595 行，全部同一行式），会话时长可达数十万行。
```
[Debug:BUE] [BUE-MT] event=main-thread result=posted feature=io.github.yu80rice.bue.in-place-reload generation=3 diagnosticId=BUE-MT-ACCEPT
```
**根因链**：
1. `src/BetterUnturnedExperience.Lir/InPlaceReloadModule.cs:240` — `OnHostTick` 每宿主帧无条件调 `NetService.Drain()`；
2. `src/BetterUnturnedExperience.Lir/Net/LirRepackNetwork.cs:167-176` — 接线态（2.1）下 `Drain()` 每帧 `seam.Post(DrainOnce)`，**即使队列为空**（DrainOnce 空转）；
3. `src/BetterUnturnedExperience.Core/Dispatch/MainThreadDispatcherRuntime.cs:198` — 每次**成功投递**都 `Emit` 一行 Debug `result=posted BUE-MT-ACCEPT`。
迁移前（未接线基线）`Drain()` 走 `DrainOnce()` 内联，无每帧投递、无每投递派发日志——**风暴为 DEV-V3-04 dispatcher 迁移新引入**，仅在 seam 接线 + LogLevels=All 时显现（无人空服每帧空投也照发）。

**先例定性**：与 F-C 逐帧风暴（V2-25 8333→1 条）同类=发布前必修的日志风暴缺陷；用户明确视刷屏为缺陷（SPF 域）。

## 修复方案（待裁决；均经 grep 确认不破坏既有红测——无任何测试断言「每投递一行 Debug 日志」）
- **F1（LIR 侧，推荐）**：`Drain()` 仅在 repack 队列有实际待办（requests/successes 非空 / TTL 待续）时投递，空闲帧不投。改 LIR 内部 + 加 `HasPendingWork`，不触碰 2.1 dispatcher 冻结面。红测锚：空闲宿主 N 拍 → 0 投递。
- **F2（dispatcher 侧）**：成功投递不再逐条 Emit Debug，仅对拒绝/容量/异常留痕（或经摘要聚合）。根治**任何**每帧消费者，但改变 DEV-V3-04 冻结可观察日志行为=契约影响面，须重开红测口径。
- **F3（F1+F2）**：双保险，改动面最广。

**无论 F1/F2/F3**：源码变更 → 重跑全套 → 双轴审查 → **重出候选（新 SHA-256/CaseId 作废本候选）→ 重做三环境验收**（含 Steam 登录后的网络缝完整验收）。

## 停等
本发现阻塞 2.1 候选定身。修复路径需用户裁决（F2 触及 2.1 dispatcher 冻结面，属契约影响决定）。
