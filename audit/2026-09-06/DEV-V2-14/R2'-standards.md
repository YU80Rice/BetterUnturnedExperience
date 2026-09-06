# DEV-V2-14 · R2' Standards 轴报告（fresh-instance 验证轮）

- 轮次：R2'（R2 因继承 R1 上下文依 output-review-loop Fresh-instance 规则 425c2aa 作废后，全新实例重派）
- 审查轴：standards-reviewer（全新子代理实例，非 SendMessage 续用）
- 审查对象：`git diff fef9738..48937d2`
- 日期：2026-09-06
- 判定：**VERDICT: CLEAN**

---

本实例为全新上下文,结论独立推导。

独立重推导完成：原 BLOCKING 的「停用后建会话/发 Ack」窗口已闭合；剩余为锁外 Connected 在飞窗，不升级。锁探针可证伪派发持锁。四项延期均成立。无新 BLOCKER。

[DEFERRABLE] `src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:172` Unsubscribe 不删空 `List` 键；键数以上界频道数为界，无增长路径。

[DEFERRABLE] `src/BetterUnturnedExperience.Core/Registration/FeatureBootstrap.cs:21` 9 参透传（events/logger/lifetime 等）；本票冻结不变量仅 Network fail-fast，宿主 Start 属 21/22。

[DEFERRABLE] `src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:372-377` 抛异常 handler 留表；冻结未定义逐出，隔离已满足 CONTEXT「单个回调异常不扩散」。

[DEFERRABLE] OnReceive→Handle* 重入窗无外部 seam 可确定性红测（`output-review-loop` 允许具名 seam gap）；锁内重检已落地。DispatchData 锁外段与停用的在飞一帧同构于锁外回调惯例，不升级。

核验（非延期升级）：
- HandleHello 278 / HandleAck 307 / HandleReject 330：OnReceive 放锁后 `SetModuleActive(false)` 再入锁时 `!moduleActive` 直接 return，不建会话、不发 Ack、不改表。HandleHello 的 Connected 仅在锁内已接纳后锁外触发，属在飞。
- 探针 `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:3002-3011`：handler 内 `Task.Run` 外线程取 `Sessions`（其 getter 持 `sync`）。若派发持锁，Pump 线程占 `sync`，外线程 2s 内拿不到锁，`foreignAcquiredInTime=false` 失败——可证伪。Pump 另线程避免自死锁。

全量扫描：契约①②措辞与方向=帧来源一致；`SupportedContractMajor=2` 与五处生产 `MinimumBueContract (2,0)` 对齐；csproj 纳入 FeatureBootstrap；无 2 参 Subscribe 残留；吞异常有冻结注释；`Removed`+句柄闭包幂等。无 Mysterious Name / 实质重复 / Feature Envy / Data Clumps 硬违规。

VERDICT: CLEAN
