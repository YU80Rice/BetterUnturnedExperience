# GPT → Gemini：DEV-06 前端消费复核请求

## 交付物

- 实施报告：`audit/2026-08-25/Implementation-DEV-06-1255.md`
- 独立终审：`audit/2026-08-25/DEV-06-Independent-Audit-R4.md`
- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-06-network-codec-ready-fence.md`
- SourceSet：`BUE-SS-20260824-02`
- 基线：`BUE-V1-RT01-20260824`
- `SCR-RT05-001`：方案 A

## 请复核的前端消费边界

1. `ReadyFrameFence` 以 generation、snapshot、双 nonce 和 52-byte prefix 作为前置拒绝边界；`RequestId` 只用于代际内 replay/in-flight 幂等，不代表身份、授权或认证。
2. `0x0101` 写 replay window 容量 128；`0x0104` 独立 read in-flight 容量 16，写窗口满载时仍可请求快照；切代清空旧状态。
3. `dispatchSync` 与 `ReplaceBinding` 共享线性化 seam；状态锁不跨越外部 handler。读 handler 异常释放 in-flight，写 mutation 异常保留 replay 占位并 fail-closed。
4. `LocalLoopbackTransport` 是单人/进程内通道；`LmnTransportAdapter` 只是实验性委托适配器，回调只入队、由 `Pump()` 受控派发。
5. Contract kind 显式白名单仅允许 `0x0001..0x0004`、`0x0101..0x0104`、`0x0201`；未知 kind 在进入 DTO handler 前拒绝。
6. Contracts/Core/Transport 不引用 Unity、Glazier、Sleek、BepInEx、Harmony、Unturned、LMN 类型；未复制原生库存 RPC，未修改 LMN。

## 复核要求

- 确认前端 Presenter/设置同步消费不把 fence 或 `RequestId` 解释为权限、身份或库存 ACK；
- 确认与 RT-02/RT-03/RT-04/RT-05/RT-06 共享契约一致；
- 如发现契约不足，仅提交 Shared Contract Change Request，不在前端建立平行契约；
- 输出 `DEV-06-Network-Review.md`，明确 `ACCEPT` 或阻断项；
- 不将本交付误表述为真实 LMN、SteamP2PFriends、U3DS、库存链或发布通过。

## 当前 GPT 判定

GPT Round 4 独立审计：`PASS`。DEV-06 当前为 `ready-for-human`，等待 Gemini 消费复核和人工批准后再关闭。

