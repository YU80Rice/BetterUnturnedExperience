# DEV-V2-17：平台——自动握手与会话生命周期（Q5）

Type: task
Status: resolved（2026-09-07，双轴最终 CLEAN：Standards=R2、Spec=R3；候选 4f303e44…8ca2）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-16（established 快照语义须先落地，已 resolved）
Spec: `../spec.md`（「平台：会话建立 = 网络模块自动握手」一节）

## What to build

功能模块零握手负担：连接建立后的会话握手（Hello/Ack/Reject）、断线清理、重连换代际全部由网络模块自动完成；公开会话集合里永远只有可以立即收发的已建立会话，`Connected` 只在握手完成后触发。

## Scope

- 握手流程：transport connected → 运行时发 Hello → 服务器 Ack/Reject → Connected → 功能收发。
- 运行时职责：断线清理、重连新会话身份与代际、超时重探退避、重复 Hello 去重、版本不兼容 fail-closed、无 ghost session。
- 会话匹配：Ack 按 peer + 代际 / nonce 匹配（废弃「第一个未建立会话」匹配法）。
- 公开面只含 `Sessions`（established）、`Connected`、`Disconnected`、`GenerationChanged`、发送结果；`Connected` 仅握手完成后触发；Connected/Disconnected 锁外执行。

## 验收条件

- [x] 红测先行：握手全流程 / 重复 Hello 去重 / Ack 按 peer+代际匹配 / 断线清理与重连换代际 / Reject fail-closed——先红后绿（编译红 104 错 `red-compile-build.log` + 桩级运行时红 26 条覆盖八组 `red-runtime-transcript.log` + 修复轮红 1 条 `red-fixround-transcript.log` + 绿 `fix2-round-build.log`）
- [x] 功能模块视角断言：假 transport 下 Connected 时序正确（握手前不触发），pending 会话不可见（`--bue-v2-handshake-red` 流程/时序组：连接前零帧、pending 期 Sessions 空快照 + 显式 NoSession、Ack 重放不重复触发 Connected）
- [x] 生命周期事件锁外执行（回调中不持状态锁的可重入断言）（Connected/Disconnected 阻塞探针：回调阻塞期间外线线程可取得状态锁）
- [x] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN（7/7 PASS、0 警告 0 错误；fresh 链 R1 双 NOT CLEAN→修复→R2 双 CLEAN→round3 结构修复→R3-Spec CLEAN；Standards 轴由 Spec-Reviewer 类型兜底实例执行，破例具名登记于结单延期 5）

## 实施结单（2026-09-07，agent /implement，全链闭环）

- **交付**：自动握手全语义——transport connected→自动 Hello→Ack 同步建立/Reject 版本 fail-closed→Connected 仅建立后触发；`INetworkTransport` 生命周期缝（PeerConnected/PeerDisconnected/ConnectedPeers）；控制帧 20B 载荷 [steamId8][major2][minor2][nonce8]（nonce=发起方 SessionId=连接代际），Ack/Reject 定向；**Ack/Reject 按「帧头 sender（传输层权威）+nonce」匹配**（废弃「第一个未建立会话」）；重复 Hello 幂等重 Ack；断线清理（established→Disconnected 锁外，pending 静默，无 ghost）；漏断线替换与重连换代际（每 peer 单活会话；GenerationChanged 于被替换死对象在后继建立时触发，supersededByPeer 统一三路）；超时重探退避 1s→8s 封顶（TickHandshake runtime 级公开驱动缝）；模块重启用重建（发起方 ConnectedPeers 重探 / 响应方复位 Reject→发起方自愈；开关本身零生命周期事件沿 14 冻结）；控制帧发送+全部生命周期回调+时钟采样锁外（兑现 16 延期#9）；`IConnectionSession` 仅注释补事件时机语义，契约面零变更（2.0 维持，SDK 免登记经双轮核验）。
- **红绿链**：编译红 104 错→桩级红 26 条（八组收集式）→绿；修复轮红 1 条（帧头权威）→绿；修复轮2（时钟锁外+volatile）结构修复无新红；全套 7/7 PASS 0 警告（`fix2-round-build.log` + `green-*.log`×7）。
- **候选身份**：`BetterUnturnedExperience.dll` SHA-256 `4f303e4426868a48daa62a6ef41056f4201bfa4543b1d38f0fd8305654ce8ca2`（356352 字节，两轮 Rebuild 一致）。不继承 14/15/16 发布批准。
- **审查链**：R1 双轴 NOT CLEAN（Spec:BLOCKER 帧头身份可伪造面；S:BLOCKING 时钟锁内调用+SMELL Removed 竞态；两轮分别 fresh，Standards 因 standards-reviewer 类型 6 连败由 Spec-Reviewer 类型兜底实例执行，破例具名）→修复（帧头 sender 权威+时钟锁外+volatile）→R2 双轴全新实例双 **CLEAN**→round3 结构修复→R3（fresh，仅 Spec 终审）**CLEAN**。**双轴最终 CLEAN（Standards=R2，Spec=R3）**。
- **具名延期**：生产 transport 生命周期绑定+ConnectedPeers 实装+TickHandshake 生产泵接线=DEV-V2-18（三轮独立具名一致）；IConnectionSession.Send 桩沿 16；生产会话创建路径属 21/22；锁探针超时窗口沿 14/16 模式。报告：`audit/2026-09-06/DEV-V2-17/`（结单+R1/R2/R3 五判词+全套红绿证据）。

## Comments

- **2026-09-06/07 开工**：/implement claim（Blocked by DEV-V2-16 已 resolved；前沿唯一）。
- **2026-09-06 R1-Spec**：fresh 实例判词 NOT CLEAN（BLOCKER：控制帧 peer 身份取自可伪造 payload steamId 而非帧头 sender；INFO：适配器接线延期 18）。修复：HandleHello/HandleAck/HandleReject 以帧头 sender 为 peer 权威（与 DATA 派发同源），payload steamId 降级声明性；新增「帧头权威」红测先红（`red-fixround-transcript.log` 1 条）后绿。
- **2026-09-07 R1-Standards**：standards-reviewer 类型连续 6 次派发失败（上游基础设施错误，其绑定模型上游不可用；同期 Spec-Reviewer 多次成功）——按预先宣告并经用户认可的兜底路径，由 Spec-Reviewer 类型 fresh 实例执行完整 Standards 清单（破例具名，Fresh-instance 规则本体全程保持）。判词 NOT CLEAN（BLOCKING：monotonicMilliseconds 锁内调用；SMELL：SubscriptionRecord.Removed 无同步读写；DEFERRABLE：adapter→18）。修复：时钟采样四处移锁外（nowMs 参数下传）、Removed 改 volatile；全套 7/7 PASS 0 警告复跑，候选刷新 `4f303e44…8ca2`。
- **2026-09-07 R2**：双轴全新实例。Standards **CLEAN**（R1 两条修复核验成立；DEFERRABLE 1 并入结单延期 1）；Spec **CLEAN**（R1 BLOCKER 修复核验成立；INFO 1=延期 18）。
- **2026-09-07 round3 + R3（闭环）**：round3 增量=纯结构修复（时钟位置+volatile），Spec R3（fresh）确认 R2 CLEAN 依赖面无语义破坏+验收四条逐条对证据（`R3-spec.md`）**CLEAN**。**双轴最终 CLEAN（Standards=R2，Spec=R3），本票 resolved**。候选 `4f303e44…8ca2`（356352 字节）不继承既往发布批准，RELEASES 换标随实机验收（DEV-V2-24 或用户实机验收）。
