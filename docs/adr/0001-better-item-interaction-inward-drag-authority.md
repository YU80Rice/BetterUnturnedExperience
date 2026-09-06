# Better Item Interaction 只增强拖入并保留原生库存权威

**Status: accepted**

Better Item Interaction V1 只增强玩家背包、普通容器和车辆后备箱之间以及地面到这些网格的拖入体验，不增强拖出到地面；所有实际库存提交继续走 Unturned 原生 `sendDragItem → ReceiveDragItem`，BUE 只负责候选预览、受控适配和原生投影收敛。这样可以让玩家获得更好的落点反馈，同时避免建立平行库存权威、复制库存 RPC 或在原生非事务 callback 中伪造回滚。
