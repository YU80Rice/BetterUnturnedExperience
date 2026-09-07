# DEV-V2-18 R2 审查报告——Standards 轴（fresh 实例）

审查轮次：R2（2026-09-07）。派发：standards-reviewer 类型全新实例（Fresh-instance 规则）。
审查对象：`round2-increment.diff`（round 1 全量 + 修复轮增量，11 文件）。

## 总判词：CLEAN

## 发现（SMELL 列名不阻断，处理见下）

**SMELL** `NetworkModuleAdapter.cs:789`
`UnpatchSelfSafe` 仍 Emit `decision=hand-back-to-lmn`。BUE-only（探针 false）关模块时 patch 卸的是 BUE 自己的前缀，帧交还 vanilla 丢弃，并无 LMN 可交。双 seam 共用同一诊断词，与决策核注释「两 seam 不共用布尔」不一致。
建议：按 takeover 是否武装分支 token（`hand-back-to-lmn` / `hand-back-to-vanilla`）。

**SMELL** `tests/.../Program.cs:4227-4238`（patch 门组）
注释写「测试宿主无 Assembly-CSharp」，但 Plugin.Tests 与 Plugin 均引用 Assembly-CSharp；前缀已编译期绑定 SDG.Unturned.Provider。`ApplyNetworkPatches` 可能真装 Harmony，本组无 `IsolateAndDetach`、无残留断言；`event=takeover-patch` 计数对 installed / fail-closed 等价。零探测计数钉住了门，注释与残留钉仍不诚实。
建议：改注释为「不依赖 TypeByName 成功」；组末 IsolateAndDetach 或钉 Harmony 残留。

**SMELL** `BueEngineNetBinding.cs:74-94`
latch 注释称「benign race 只会 double-resolve」。实现是 `resolved=true` 后再填 PropertyInfo：并发第二进入者会看到空字段而非双解析。已声明 pump+send、无静态初始化，主线程模型下不构成生产竞态，但注记仍不准确。
建议：改为 latch-before-populate +「未声明 off-main send」。

**INFO** `BueNetworkRuntime.cs:599` OnReceive 仍 `GetString` 比对 `FrameMagic`，编码器走 `MagicBytes`。单源字符串，非第二魔数源。
**INFO** `HostNetworkTransportAdapter.cs:89-91` `Raise*` 仍 public，已标明 test seam。

## 处置
三条 SMELL 全部按建议修复（round3 增量：token 分支 / 注释改写+组末 IsolateAndDetach / latch 注释改写），round3 后旗标 ALL GREEN + 全套 7/7 PASS 0 警告（`round3-build.log`/`round3-transcript.log`/`round3-*.Tests.log`）。两条 INFO 保持原状（单源字符串非第二魔数源；public test seam 已注明）。

## 已核验通过
- ApplyNetworkPatches 仅 `coordinator.TakeoverActive` 时 Refresh；测试探针计数钉零调用（不再用 `!LmnNativeDispatchLive` 冒充）。
- Rearm 与泵同级 try；`BueEngineNet` 五解析器 + Send 降级，对齐 `TryGetConnectionSteamId`；类注释 NEVER THROW。
- 「网络关闭」组钉不消费 / 零会话 / NoSession / 重开恢复；真装移除具名 24。
- `MagicBytes` 由 `FrameMagic` 派生；src/tests/docs 无 BUE2。
- 状态锁不跨发送/回调；Poll 抛则 `lastPolledPeers` 不前进；决策核 catch 交还。
- FakeBueEngine / 手筑帧不把游戏类型写入生产语义；Binding internal + InternalsVisibleTo。
- 六步：BUE 先于 takeover；live 释放只打 MOD/LMN2。
- 角色翻转、21/22/24、数字频道：具名延期，本轴不升格。
