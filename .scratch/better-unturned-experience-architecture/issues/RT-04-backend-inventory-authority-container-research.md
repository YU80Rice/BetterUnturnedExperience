# RT-04：调研库存原生权威链与容器状态

**Owner:** GPT（后端与共享契约负责人）  
**Required reviewer:** Gemini（前端交接消费复核）  
**Blocked by:** RT-01：冻结共享契约与对账基线  
**Status:** resolved

## What to build

形成从 Better Item Interaction 的候选释放到 Unturned 原生权威验证、修改和库存投影的完整后端证据链，使 SP、SteamP2PFriends Host/Client 与 U3DS 都不依赖平行库存协议或插件自管回滚。

## Acceptance criteria

- [x] 分环境角色追踪 `sendDragItem`、网络 dispatch 和 `ReceiveDragItem`；U3DS 从服务器网络入口起算。
- [x] 记录 RPC reliability、ownership、caller identity、rate limit 及所有失败入口。
- [x] 逐项追踪 page/item、坐标、rotation、capacity、occupancy、asset footprint、装备位和 storage access 验证。
- [x] 记录成功 mutation 顺序、mutation 前最后安全点、remove/add 原子性和不安全 callback 点。
- [x] 追踪 `checkSpaceEmpty`、`checkSpaceDrag`、`checkSpaceSwap` 与相关 `Items` 成员，同时保持 V1 不自动交换。
- [x] 查明当前 storage/container session 的表示、授权范围、失效和过期引用处理。
- [x] 查明库存投影/变化事件及线程，并划定只读观察与原生提交 adapter seam。
- [x] 只推荐不绕过 `ReceiveDragItem` 的 Harmony hook；列出并拒绝平行 remove/add、客户端权威和重复 RPC 方案。
- [x] 输出 GPT 前缀研究报告、各环境调用链、验证/修改矩阵、容器状态图、内部 adapter seam 和运行证据义务。
- [x] 不实现生产后端，不修改共享契约；发现不足时提交 change request。

## Verification

- [x] 每个原生事实带固定源码身份和 source/IL/prototype/runtime 证据分类。
- [x] Gemini 确认原生投影与 AwaitingProjection 的消费条件完整且无推断性回滚。

## Comments

- 2026-08-24：人工开发者授权 GPT 使用 `/research` 领取 RT-04；限定为 SourceSet `BUE-SS-20260824-01` 的只读 U3-SDK/权威链调研，不编写生产后端。
- 2026-08-24：GPT 完成研究报告与 Gemini 交接文档；独立审计第 1 轮发现 2 项阻断，修订后第 2 轮 `PASS`。等待 Gemini 外部消费确认，故未标记 `resolved`。

## Answer

- 研究报告：`../RT-04-Inventory-Authority-Container-Research.md`
- Gemini 交接：`../handoffs/to-RT-04-review.md`
- 内容审计：第 2 轮 `PASS`
- Shared Contract Change Request：无；未发现必须修改 RT-01 的契约缺口
- 关闭记录：Gemini 在 `handoffs/RT04-Inventory-Authority-Review.md` 给出 `ACCEPT`；确认 Relay 只读快照、`AwaitingProjection` 无推断性回滚、Storage `SessionGeneration` 失效和共享契约充分性。GPT 据此关闭调研票；生产与三环境运行证据仍属后续实施/发布门禁。
- SourceSet 迁移：已迁移到 `BUE-SS-20260824-02`；U3-SDK commit 与库存 anchors 未变化，受影响结论重验 PASS。


