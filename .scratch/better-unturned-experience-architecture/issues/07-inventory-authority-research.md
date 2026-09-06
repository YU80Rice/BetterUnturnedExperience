# 查明原版库存移动与多人权威调用链

Type: research
Status: resolved
Author: GPT
Blocked by:

## Question

物品栏和容器拖放从客户端 UI 到最终状态变更的原版调用链是什么？单人、SteamP2PFriends 和 U3DS 中的权威执行者、校验点、失败回滚与可安全挂接位置分别在哪里？

## Answer

原版采用服务端权威模型：客户端 UI 仅预检并通过 `sendDragItem` 提交候选；`ReceiveDragItem` 在服务端重新校验所有权、页/坐标、物品存在、容量、占用、资产与装备槽兼容后，才执行 remove/add。单人和 SteamP2PFriends 房主走服务端 loopback，SteamP2PFriends 客机与 U3DS 客户端发往远端服务端。安全挂接点是客户端候选计算/绿色预览与原版 `sendDragItem` 之前，禁止直接写 `Items` 或跳过 `ReceiveDragItem`。

完整证据与运行时未验证边界见 `../research/07-inventory-authority.md`。

