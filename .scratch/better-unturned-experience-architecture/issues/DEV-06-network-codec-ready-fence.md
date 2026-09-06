# DEV-06：BUE Network Codec + ReadyFrameFence + Transport seam

Status: resolved
Owner: GPT（后端/共享边界维护）
Consumer reviewer: Gemini（前端）
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Decision: SCR-RT05-001 方案 A

## 目标

建立 BUE 自有应用层网络 seam：确定性 Little-Endian fenced frame 编解码、ReadyFrameFence 代际/快照/双 nonce 校验、有限 replay/in-flight 防重入，以及不依赖 LMN 的 LocalLoopback transport。LMN 仅作为可替换实验性委托适配器，不成为 Core 强制依赖。

## 验收条件

- [x] 52-byte prefix 按冻结布局编码/解码：version、flags、reserved、generation、snapshot、client nonce、server nonce。
- [x] 非法 version/flags/reserved/短帧/绑定失配在 handler 前拒绝。
- [x] `0x0101` 写 replay window 当前代际容量 128；同 RequestId 同 payload 幂等，异 payload 冲突；满载 fail-closed。
- [x] `0x0104` 只读请求使用独立容量 16 的 in-flight 域，不占用写窗口；完成后释放，切代清理。
- [x] 切换 connection generation 原子替换 binding，并清空旧 replay/read 状态。
- [x] `INetworkTransport`、LocalLoopback 与 LMN 委托适配器不引用 LMN/Unity/Unturned/UI 类型。
- [x] TDD Red→Green、Release 0 errors / 0 warnings、网络测试 PASS、Contracts/Core token scan PASS。
- [x] 独立子智能体 Round 4 审计 PASS；交 Gemini 复核；DEV-07 三环境运行/发布门禁未提前声明。

## 不在本票

- 修改 LMN 源码或其协议；
- 原生库存 RPC、`sendDragItem`/`ReceiveDragItem`；
- 真实 SteamP2PFriends/U3DS 运行和发布授权；
- 把 nonce 当作认证/MAC 或权限证明。
