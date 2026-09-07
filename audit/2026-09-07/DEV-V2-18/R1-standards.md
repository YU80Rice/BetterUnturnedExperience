# DEV-V2-18 R1 审查报告——Standards 轴（fresh 实例）

审查轮次：R1（2026-09-07）。派发：standards-reviewer 类型全新实例（首次派发因上游流中断未产出判词，重派后产出，无效派发不构成审查轮）。
审查对象：`round1-increment.diff`（11 文件）。

## 总判词：NOT CLEAN

## 发现

**BLOCKING** `src/BetterUnturnedExperience.Plugin/NetworkModuleAdapter.cs:397` / `:838-867`
ApplyNetworkPatches 成功安装后无条件 `RefreshLmnNativeDispatchLive()`。生产构造探针为 null，走 `ProductionLmnNativeDispatchLive` → `AccessTools.Method` + `Harmony.GetPatchInfo` 扫 LMN owner。ActivateCore / RetryPendingMirror / RefreshSwitches 二次调用均有 `TakeoverActive` 门，唯独安装成功路径没有。方法注释写明「探针 false 时 LMN seam 零反射」，与代码不符。
同票测试 `tests/.../Program.cs:4237-4238` 只用 `!LmnNativeDispatchLive` 冒充「未探测」：生产探测跑完仍为 false，断言仍绿。Plugin.Tests 引用 Assembly-CSharp，TypeByName 可能装上补丁且该组未 IsolateAndDetach。
**修复：** 安装后仅 `coordinator.TakeoverActive` 时刷新；测试钉「未调用生产探测 / 无 Harmony 残留」，不要用结果布尔冒充零动作。

**BLOCKING** `NetworkModuleAdapter.cs:262-266, 421-437`；`BueEngineNetBinding.cs:112-171`
TickNetwork 四级隔离，但 `RefreshSwitches` → `RearmBueRuntime`（`PollPeerState` + `SetModuleActive` → 引擎源/控制帧发送）无 try。`BueEngineNet` 的 GetValue/Send 亦不按 `TryGetConnectionSteamId` 先例吞异常。Binding 注释声称解析器只出现在泵隔离或发送聚合里，漏了开关重武装。面板 Apply 可把引擎异常打进 UI。
**修复：** Rearm 与泵同级隔离；解析器失败降级并保留一发诊断。

**BLOCKING** `tests/.../Program.cs:4138-4392`
工单冻结「网络关闭不消费 BUE 帧且 patch 移除」未进入本红锚。既有 kill-switch 不覆盖新 BUE 消费分支。决策核 `isolated || !networkEnabled` 无回归钉。
**修复：** 增组：关模块后 `ShouldConsumeInbound(BUE)==false`，并钉 unpatch 诊断。

**SMELL** `BueFrameClassifier.cs:18-36` vs `BueNetworkRuntime.cs:599,870`
注释称 FrameMagic 为唯一源，但 `IsBueFrame` 用并行 Magic0-3，编码器/OnReceive 用字符串，可漂移。

**SMELL** `HostNetworkTransportAdapter.cs:89-91`；`BueEngineNet` 静态 `resolved`
生产类型公开测试 Raise*；静态解析无锁也未声明 main-thread（仅 adapter 快照写了）。

**INFO** `green-frame-binding-transcript.log` 空文件符合「绿静默」；不构成代码缺陷。

## 已核验通过
- 运行时锁不跨发送/回调；Poll 事件不持 incoming 锁；Raise 抛则 lastPolledPeers 不前进。
- 决策核 catch 返回 false（hand-back，不 claim consume）。
- FakeBueEngine / 手筑 BUE1 帧不把游戏类型写进生产语义；Binding 为 internal + InternalsVisibleTo。
- 六步结构：BUE 分支先于 takeover 布尔；live 释放只打 MOD/LMN2。
- 角色翻转、21/22/24、数字频道：具名延期，本轴不升格。
- `BueFrameClassifier` 公开面与 `LmnFrameClassifier` 同构，正当。
