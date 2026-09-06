# DEV-15：Better Item Interaction 正式功能实现规格

**状态：** `ready-for-agent`（仅规格与拆分完成，尚未授权生产编码）  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**依赖：** RT-02、RT-04、RT-06、DEV-01～DEV-14、SCR-GPT18-001  
**官方 FeatureId：** `io.github.yu80rice.bue.better-item-interaction`

## 1. 目的

把 DEV-14 的官方注册 tracer bullet 扩展为第一个可供玩家实际使用的 BUE 功能：在玩家通过 G/F 打开的背包、普通容器和车辆后备箱网格中，提供更清晰的拖拽落点预览、可配置的自动 90° 旋转和受控的原生放置提交。

本功能改善玩家的拖入体验，但不取得库存权威，不建立第二套库存模型，也不替换 Unturned 原生库存 RPC。

## 2. 玩家可见行为

### 2.1 支持的拖入路径

V1 支持：

1. 地面物品 → 玩家背包网格或普通容器网格；
2. 玩家背包网格 → 普通容器网格；
3. 普通容器网格 → 玩家背包网格或另一普通容器网格；
4. 同一背包、箱子或车辆后备箱内的移动与旋转。

V1 不增强：

- 拖出到地面或丢弃；
- 快捷装备槽、特殊区域和其他未裁决页面；
- 自动交换、自动重排、批量整理或堆叠规则。

目标为特殊页面时，增强层必须原生放行。

### 2.2 默认设置

设置中心提供两个 `ClientLocal` 设置：

- `Enabled`：默认开启；关闭后恢复原生拖拽，可再次开启；
- `AutoRotate`：默认开启；关闭后仅尝试当前方向。

设置变更对下一次拖拽生效。拖拽中切换开关不会中途改变当前状态；释放或取消后清理增强状态。

### 2.3 预览表现

- 合法候选：绿色半透明占据框和跟随光标的物品图标；
- 自动旋转候选：按最终旋转方向显示绿色预览；
- 无合法候选：红色解释性占据框，不提交；
- 光标离开有效网格：隐藏预览；
- 释放后的原生投影等待：仅短暂弱高亮/淡出，不显示服务器拒绝、回滚或技术错误；
- 功能隔离或环境不可用：提示“增强物品交互暂不可用，已恢复原生拖拽”，不影响原版与其他 BUE 功能。

## 3. 不可变权威边界

1. 候选评估器只生成预览和原生提交参数。
2. 合法普通网格候选最终调用 `sendDragItem(...)`。
3. 服务端仍由 `ReceiveDragItem` 进行所有权、边界、占用、装备、容器和资产校验。
4. BUE 不复制库存 RPC，不通过 LMN 发送平行库存命令。
5. BUE 不进行乐观库存写入、不执行本地伪回滚、不把单独 `onInventoryAdded` 或 `onInventoryRemoved` 当作 ACK。
6. 原生 callback 调用栈结束后，框架拥有的 relay 才能发布 generation-gated 只读快照。

## 4. 拖拽状态与失效

主状态机固定为：

```text
Idle → Dragging → Hovering → Dropping → AwaitingProjection → Idle
```

- `Dragging`：原生已抓取，尚未得到可渲染目标；
- `Hovering`：存在当前帧预览；
- `Dropping`：释放事件正在执行受控提交；
- `AwaitingProjection`：等待原生库存模型投影；2 秒仅是视觉预算；
- 任何失败、取消、关闭或隔离均清理增强状态并回到原生可用路径。

以下事件立即使当前增强拖拽失效：关闭窗口、切换容器、角色死亡、换服、重连、`StorageGeneration` 变化、`DragGeneration` 变化、功能关闭、原生拖拽已结束或 UI Seam 冲突。

旧代际释放回调和迟到投影必须静默丢弃，不得触碰新容器或新拖拽。

## 5. 坐标、候选与预览规则

实现必须直接消费已冻结的 `Item-Placement-Algorithm-Spec.md`：

1. 前端 adapter 根据 pointer、UI Scale、滚动、footprint 与 `grabOffsetInFootprint` 计算 `intendedItemCenterGrid`；
2. Forward 旋转的抓取偏移为 `(H - gy, gx)`；提交旋转使用 `(rotation + 1) & 3`；
3. 候选优先级固定为：`current-local → automatic-90-local → current-expanded → automatic-90-expanded`；
4. 正方形物品跳过无意义的自动旋转搜索；
5. 超出容器返回 `Hidden/OutsideGrid`；全满或无候选返回 `LocallyInvalid`；
6. 自动旋转关闭时绝不检查旋转方向；
7. 算法不接收或重算 grab offset，不依赖时间、帧率、随机数或集合迭代顺序。

## 6. 原生 UI 与适配器职责

### 6.1 `ClientInventoryInteractionAdapter`

- 只在普通背包/容器网格进入增强路径；
- 读取原生 drag state 和当前容器快照；
- 将屏幕坐标转换为纯 C# 候选输入；
- 合法候选调用原生 `sendDragItem`；
- 快捷槽、AREA、丢弃和特殊分支直接 pass-through；
- 不向 Contracts/Core 泄漏 Unity、Glazier、Sleek 或 Unturned 类型。

### 6.2 `NativeInventoryProjectionRelay`

- 仅由框架持有原生库存观察入口；
- 捕获异常、按代际失效、排队只读快照；
- 不发布 remove/add 中间态为最终结论；
- 使用 `DragGeneration + SessionGeneration + item fingerprint` 做高置信收敛；
- 歧义时跟随最新原生事实，不弹窗、不伪造拒绝。

### 6.3 ClientUi

- 顶层容器承载浮动物品图标，网格容器承载占据框；
- 关闭、换容器、隔离和 SafeMode 时对称卸载；
- 不扫描程序集、不使用全局反射、不实例化未注册卫星；
- UI 卫星缺失时，核心与设置 Facet 仍可工作并投影为 `PresentationDegraded/HeadlessOnly`。

## 7. 故障与兼容性策略

若候选计算、预览绘制、释放适配器或投影 relay 抛出异常，或检测到不可安全判定的第三方库存 Seam 冲突：

- 只隔离 Better Item Interaction；
- 卸载其自定义 UI；
- 停止新的增强回调；
- 恢复原生拖拽；
- 输出限频结构化诊断；
- 不影响 BUE Host、设置中心、其他功能或原版游戏。

禁止使用程序集扫描、类名猜测、动态 DLL 发现或全局 `PatchAll()` 来“探测兼容性”。

## 8. 性能与线程门禁

- `PlacementCandidateEvaluator.Evaluate` Release 热路径连续调用分配必须为 0 字节；
- 拖拽更新路径禁止 LINQ、闭包、装箱、临时集合和逐帧字符串日志；
- UI 图元必须复用或池化；
- 原生适配器与投影 relay 在 Unity 游戏线程执行；
- Headless/U3DS 不得解析或实例化 ClientUi 类型；
- 若引擎内部仍产生不可消除分配，必须单独记录，不能宣称整体 0 GC。

## 9. 实施拆分

### DEV-15A：Native Drag Adapter

读取原生拖拽上下文、识别普通网格与特殊分支、接入受控释放 Seam；先完成测试替身和 pass-through 矩阵。

### DEV-15B：Coordinate + Preview Wiring

接入坐标转换、Evaluator、绿色/红色占据框和浮动物品图标；完成 UI Scale、滚动、裁剪、旋转和零分配测试。

### DEV-15C：Projection Relay + AwaitingProjection

实现 callback 栈后快照、generation/session/fingerprint 绑定、2 秒视觉预算和迟到投影收敛。

### DEV-15D：Settings + Lifecycle + Isolation

接入 `Enabled`/`AutoRotate`、9 态生命周期、功能隔离、SafeMode、UI 缺失降级和原生回退。

### DEV-15E：Qualification Evidence

以同一候选 DLL 哈希分别采集单人、SteamP2PFriends Host/Client 与 U3DS 证据，执行 Gemini 消费复核、独立审计和发布资格裁决。

## 10. 验收矩阵

### 静态/编译

- Contracts/Core/Release 无 UI/Native/LMN 类型泄漏；
- 统一 DLL 构建 0 errors，尽量 0 warnings；
- 所有新增 seam 有对应单元测试和故障注入测试；
- 官方 FeatureId 固定为 `io.github.yu80rice.bue.better-item-interaction`。

### 玩法行为

- 背包内移动/旋转；
- 背包↔箱子；
- 背包↔车辆后备箱；
- 地面→背包/容器；
- 无效位置、满容器、边界 clamp；
- 关闭/换容器/死亡/重连后的陈旧回调；
- 关闭功能后的原生回退；
- 快捷槽、AREA、丢弃等特殊分支仍由原生处理。

### 环境证据

每个环境必须绑定同一 Candidate DLL SHA-256 与新 CaseId；单人、SteamP2PFriends Host、SteamP2PFriends Client、U3DS 证据互不替代。插件无报错不等于功能通过。

## 11. 明确不做

- 修改或替换 Unturned/BepInEx/U3DS/LMN；
- 新增库存网络协议或第二库存数据库；
- 增强拖出、丢弃、自动交换、自动重排、批量整理；
- 目录扫描或动态发现第三方插件；
- 未经证据支持的 U3DS、P2P 或发布资格声明；
- 在 DEV-15 完成前宣称 Better Item Interaction 已完成。

## 12. 未决验证义务

- `VO-RT02-01`：生产 Hook 特殊分支放行；
- `VO-RT02-02`：Release 热路径 0 GC；
- `VO-RT02-03`：三环境拖拽与投影同哈希联调；
- U3DS 独立程序集身份、Headless 类型可达性和真实加载；
- 原生投影延迟/顺序和 native rejection 原因不可知边界。

这些是实施和运行门禁，不是规格阶段的默认通过项。

