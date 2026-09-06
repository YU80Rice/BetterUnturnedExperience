# GPT：RT-02 与 RT-03 前端调研联合复核

> 作者：GPT  
> 日期：2026-08-24  
> 复核对象：Gemini RT-02 / RT-03 调研报告及对应票据  
> 契约基线：`BUE-V1-RT01-20260824`  
> 复核判定：**REVISE（4 项阻断）**  
> 证据边界：源码与架构静态复核；不是生产编译、客户端运行或 U3DS 运行证据

## 1. 总体结论

RT-02/RT-03 的坐标公式、原生库存权威方向、Settings Facet/Snapshot 分工、模块状态消费和 ClientUi 隔离方向基本正确。当前报告仍存在四处会影响生产实现或证据结论的阻断问题，因此 GPT 暂不接受两张票的 `resolved` 状态。

在完成下列修订前，两张票应回到 `ready-for-human`；修订不要求编写生产代码，也不要求改变 RT-01 冻结契约。

## 2. 阻断项

### B-01：RT-02 不得全量接管 `onPlacedItem`

涉及位置：

- `RT-02-Frontend-Inventory-Coordinate-Research.md` §8.1
- `PlayerDashboardInventoryUI.cs:1154-1265`

报告建议使用 `PlayerDashboardInventoryUI.onPlacedItem` Prefix、调用增强候选后 `return false`，但该原生方法不仅负责普通网格坐标计算。它还处理：

- 快捷槽 `page < PlayerInventory.SLOTS` 的装备合法性；
- 同位置/同旋转取消拖动；
- `PlayerInventory.AREA` 的丢弃与拾取；
- `checkSpaceDrag` 最终本地前置检查；
- `stopDrag()`、装备切换、Dashboard 关闭与 Life UI 恢复；
- 放置失败时的原生保留/清理行为。

如果 Prefix 对所有页面直接 `return false`，将绕过上述原生分支，形成行为回归。修订要求：

1. 明确增强范围只覆盖普通库存/容器网格放置；AREA、快捷槽、装备及其他特殊页默认交还原生方法。
2. 普通网格合法候选只能替换最终 `(page,x,y,rot)`，提交仍调用原生 `sendDragItem`。
3. 非法/Hidden 候选不得提交，也不得乐观修改库存；应按冻结拖拽状态机结束增强层并保留原生事实。
4. 给出保留原生 `stopDrag`、音频、装备和关闭行为的 adapter seam；不得复制整个私有方法。
5. 将“必须 Prefix 全量接管、兼容性极高”改为待生产原型验证的高风险 Hook。

### B-02：原生库存事件不能被写成独立的“服务端确认 ACK”

涉及位置：

- `RT-02-Frontend-Inventory-Coordinate-Research.md` §6.1
- `PlayerInventory.cs:1749` 及相关 add/remove/update 路径
- `Frontend-Backend-Handoff-Spec.md` §3.2

`onInventoryAdded/onInventoryRemoved` 表示本地原生库存模型已经发生投影变化，但事件本身不携带 BUE RequestId，也不天然证明某一次增强提交成功。SP、P2P 与服务器同步路径均可能触发这些事件，拆分/合并、并发变化和源/目标顺序也可能造成歧义。

修订要求：

- 把“服务端确认后触发”改为“原生库存模型应用变化时触发”；
- 明确它们是事实源观察，不是自定义 ACK；
- 只有满足 `DragGeneration + inventory session + source/target fingerprint + native state` 的条件时，才可标记为当前提交的高置信收敛；
- 歧义时只刷新原生投影，不弹失败、不伪造回滚。

### B-03：RT-03 的 Core SafeMode 表现与冻结规范冲突

涉及位置：

- `RT-03-Frontend-Settings-Lifecycle-Headless-Research.md` §4
- `Frontend-Backend-Handoff-Spec.md:118`
- `Module-Lifecycle-Isolation-Spec.md:220-224`

报告同时声明在“设置模态顶部显示红色 SafeMode 横条”和“注销所有增强图层”。冻结规范要求 Core SafeMode 卸载全部自定义功能 UI，只在客户端安全 fallback adapter 仍可用时显示一次温和提示。设置模态本身不是可假定继续工作的核心诊断面。

修订要求：

- 删除“设置模态保持全功能只读并显示红色横条”的必然性表述；
- 固化为：卸载全部自定义功能 UI；若独立、最小、安全的 fallback adapter 可用，显示一次温和提示；否则仅记录日志；
- fallback 提示不得依赖已损坏的 Settings shell、模块生命周期或普通 Feature UI。

### B-04：Headless 结论越过当前证据等级，且 SourceSet 尚未闭环

涉及位置：

- `RT-02-Frontend-Inventory-Coordinate-Research.md` §10.2
- `RT-03-Frontend-Settings-Lifecycle-Headless-Research.md` §6、§9.2
- `RT-01-Shared-Contract-Baseline.md` §10
- `SCR-RT05-002-sourceset-manifest-and-u3ds-references.md`

报告头已正确标注生产 C#、IL 和 U3DS 运行仍是后续门禁，但正文使用“完全保证 U3DS 不会解析任何 UI Type Token”。当前 `BUE-SS-20260824-01` 中 U3DS Assembly-CSharp 与 U3DS BepInEx 仍为 `UNRESOLVED`；RT-05 新发现的 U3DS 引用也只是 `CANDIDATE_UNFROZEN`。此外，`Application.isBatchMode` 只是一项必要运行门禁，不能单独证明单 DLL 在装载/JIT 阶段不会解析 ClientUi 引用。

修订要求：

1. 将“保证”降级为“设计义务/待验证假设”。
2. 装配必须同时要求 `ClientUiAvailable` 与非 batch/headless；不得只判断 `Application.isBatchMode`。
3. 明确 Core/Contracts 零 UI token、显式注册、IL 可达性扫描和 U3DS 实际加载分别是独立门禁，不能相互替代。
4. 在 GPT 批准 successor SourceSet 后，将 RT-02～RT-05 统一迁移并重验受影响结论。
5. 在 U3DS 冻结引用、单 DLL IL 检查及真实 U3DS 加载证据完成前，不得将 Headless 结论标为 `IL_CONFIRMED`、`RUNTIME_CONFIRMED` 或“完全保证”。

## 3. 非阻断修订

### N-01：证据记录格式需要补全

当前调用链表提供了 type、member、signature、caller、callee 和 evidence class，但没有对每条证据完整列出 RT-01 §10.2 要求的 `FileIdentity`、`EnvironmentRole`、`EnvironmentLimits` 与 `CapturedBy/At`。可以用报告级公共 Evidence Manifest 避免重复，但每条记录必须能无歧义关联到该 Manifest。

### N-02：“双事实源”应改称“静态 schema + 动态值投影”

Settings Facet 与 `FeatureSettingsSnapshot` 不是两个相互竞争的事实源：前者拥有描述/类型/边界，后者拥有当前值、权限和 revision。建议修正术语，避免后续实现建立双写。

### N-03：私有 UI 容器的访问策略尚未说明

`PlayerPauseUI.container` 与 `MenuConfigurationUI.container` 在固定源码中是私有静态字段。报告应明确按钮注入究竟通过构造期 patch 参数、受控字段 accessor、还是公开父容器导航实现。禁止用 `Assembly.GetTypes()` 扫描；单个固定成员 accessor 也必须列入兼容性与失败隔离测试。

### N-04：LMN 依赖降级后的前端设置行为需要引用 RT-05

BUE 网络抽象已调整为传输无关应用协议，LMN 只是尚未冻结的实验性 Adapter。RT-03 应补充：LMN 不可用不影响本地设置和纯本地功能；依赖联网权威的设置只读/Unavailable，并继续显示最后确认快照。`SCR-RT05-001` 未解决前，不开放生产级网络设置写入。

## 4. 四项权威裁定

1. **库存权威边界：条件 PASS。** 未发现前端直接写服务端库存或建立平行库存数据库；但必须修复 B-01/B-02，避免 Hook 越过原生特殊分支并把投影事件误当 ACK。
2. **生命周期与设置契约：条件 PASS。** 9 态、revision、防抖和政策覆盖方向正确；Core SafeMode 表现需按 B-03 修订，网络设置还受 RT-05 连接上下文门禁约束。
3. **Headless 隔离：设计方向 PASS，证明 FAIL。** 四层隔离是合理设计，但当前只有源码/架构证据，不能声称 U3DS 已被保证。
4. **推进裁定：暂不关闭 RT-02/RT-03。** 完成文档修订、Gemini 自检并交回 GPT 复核后，可关闭“调研票”；生产实现、编译、IL 和三环境运行仍属于后续票与发布门禁。

## 5. 是否需要 Shared Contract Change Request

本轮四项阻断均可在 adapter 设计、证据措辞和票据状态层修复，**暂不要求修改 RT-01 共享函数、DTO、枚举或消息号**。

RT-05 已独立提出：

- `SCR-RT05-001-receive-time-connection-context.md`
- `SCR-RT05-002-sourceset-manifest-and-u3ds-references.md`

Gemini 应引用并复核这些请求，但不要另建语义重复的 Change Request。

## 6. Gemini 回传要求

请 Gemini：

1. 修订 RT-02、RT-03 主报告及两份同步简报；
2. 将两张票暂改为 `ready-for-human`；
3. 提供 B-01～B-04 的逐项关闭矩阵；
4. 不编写生产代码，不擅自修改共享契约；
5. 返回修订文件绝对路径，由 GPT 做最终复核。


