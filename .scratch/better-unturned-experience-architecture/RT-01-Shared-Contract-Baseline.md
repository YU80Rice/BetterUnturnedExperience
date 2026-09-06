# GPT-RT-01：共享契约与对账基线

> 作者：GPT  
> 状态：accepted-frozen（GPT 独立审计 PASS；Gemini ACCEPT）  
> 契约基线：BUE-V1-RT01-20260824  
> 适用任务：RT-02、RT-03、RT-04、RT-05、RT-06  
> 证据等级：需求/架构静态基线；不是生产编译或运行证据

## 1. 目的与权威顺序

本文件把已通过联合 Wayfinder 复审和双语需求审计的共享 seam 汇总为单一执行基线。RT-02～RT-05 可以补充 native adapter 事实，但不能静默改变本文件中的公共 token、字段、状态、坐标、算法或证据边界。

发生冲突时按以下顺序裁定：

1. 最新人工明确决定；
2. `spec.md` 与 `spec.zh-CN.md` 的共同语义；
3. 已 resolved 的 GPT/Gemini Wayfinder 决策；
4. 旧架构规格中的非冲突细节；
5. 研究 Agent 的提案。

本轮发现并修复一项旧文档漂移：`Shared-Contract-Spec.md` 的两个 DTO 代码块曾漏写 `RevisionScope`，但最新双语规格、线路字段顺序和事务粒度均要求该字段。现已补齐：

- `ModuleConfigChangedEvent.RevisionScope`
- `ModuleConfigRejectedEvent.RevisionScope`

## 2. 依赖与事实所有权

```text
Feature Definition Linker
        ↓ 生成单一定义产物
Runtime Admission / SettingsRuntime / Lifecycle / ClientUi adapters
        ↓ 依赖
Contracts
```

- Contracts 不依赖任何 adapter；前端和后端 adapters 依赖 Contracts。
- Contracts 只能使用 .NET Framework 4.7.2 兼容领域类型，不得引用 BepInEx、Harmony、Glazier、Sleek、Steamworks、LaunchMultiplayerNet 或 Unturned 具体类型。
- 运行时模块不得声明或改写 FeatureId、版本、依赖、设置 schema、事件所有权、能力和入口绑定；这些事实只来自链接后的单一定义产物。
- `IFeatureBootstrap` 不提供 UI root、native inventory、LMN connection 或任意模块查找。
- GPT 维护共享契约；Gemini 是所有前端消费变更的强制 reviewer；人工决定必须可追溯。

## 3. 冻结的 interface

| Interface | 精确成员 | Required behavior |
| --- | --- | --- |
| `IFeatureModule` | `FeatureStartResult Start(IFeatureBootstrap bootstrap)` | 仅在可信准入、设置 bootstrap 完成且状态进入 Starting 后调用。 |
| `IFeatureModule` | `void Stop(FeatureStopReason reason)` | 每个 lifecycle generation 最多调用一次；lifetime lease 清理作为兜底。 |
| `IFeatureLifetime` | `bool TryTrack(IDisposable registration)` | 跟踪长期注册；拒绝 null；Closing 后迟到注册立即释放。 |
| `IScopedFeatureSettings` | `FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)` | 返回当前功能作用域的完整不可变快照。 |
| `IScopedFeatureSettings` | `bool TryGet(string settingId, out SettingValue value, out uint revision)` | 读取一个确认值；不能接受任意 FeatureId。 |
| `IScopedFeatureSettings` | `SettingChangeResult Submit(ScopedSettingChangeRequest request)` | 执行单功能、单 revision scope 的原子设置事务。 |
| `IFeatureEventSubscriber` | `IDisposable Subscribe<TEvent>(Action<TEvent> handler)` | 只能订阅链接后 consumption facet 允许的事件。 |
| `IOwnedFeatureEventPublisher` | `bool TryPublish<TEvent>(string declaredEventId, TEvent value)` | 只允许当前功能拥有、已声明且事件类型完全匹配的发布。 |
| `IFeatureLogger` | `void Info(string eventName, string diagnosticId)` | 输出功能作用域信息诊断。 |
| `IFeatureLogger` | `void Warning(string eventName, FrameworkErrorCode error, string diagnosticId)` | 输出稳定错误码，不向玩家暴露原始 payload。 |
| `IFeatureLogger` | `void Error(string eventName, FrameworkErrorCode error, string diagnosticId, Exception exception)` | 异常只进入内部诊断；普通 UI 只消费安全投影。 |
| `IDependencyCapabilityView` | `bool Has(string declaredDependencyId, string capabilityId, ushort minimumVersion)` | 只能查询链接后的本地 dependency id。 |
| `IDependencyCapabilityView` | `bool TryGet(string declaredDependencyId, out NegotiatedFeatureView feature)` | 仅在依赖状态、能力和 generation 当前有效时返回投影。 |
| `IPlacementCandidateEvaluator` | `ItemPlacementPreview Evaluate(PlacementCandidateInput input)` | 纯、同步、无状态、无 Unity 副作用，不持有 occupancy；生产目标为每次调用零分配。 |

`IFeatureBootstrap` 恰好暴露：

| Property | Type |
| --- | --- |
| `Identity` | `FeatureScopeIdentity` |
| `LifecycleGeneration` | `ulong` |
| `Settings` | `IScopedFeatureSettings` |
| `Events` | `IFeatureEventSubscriber` |
| `OwnedEvents` | `IOwnedFeatureEventPublisher` |
| `Logger` | `IFeatureLogger` |
| `Dependencies` | `IDependencyCapabilityView` |
| `Lifetime` | `IFeatureLifetime` |

## 4. 冻结的共享 DTO 形状

以下列出研究任务必须使用的字段集合。字段名和类型不能由 RT-02～RT-05 改写。

为避免复制全部嵌套定义后再次漂移，本基线以 `Shared-Contract-Spec.md` **版本 `0.1.0-draft`、2026-08-24 RT-01 修订态、文件 SHA-256 `E70D079476D164BB82B5AB53909D4BB929F441C142E93FE5B0FCFB50BA5FEFCB`**建立以下逐 token 规范引用。表中章节的 C# code block 及 enum 数值全部属于本基线；其他旧文档文字只有在不与本文件和双语需求规格冲突时才可作为说明。该文件 hash 变化时，必须发布新的共享契约基线，不得沿用本引用。

| Normative token set | 精确规范来源 | 本基线裁定 |
| --- | --- | --- |
| `FeatureId`, `ContractVersion`, `FeatureDependency`, `FeatureScopeIdentity`, `Digest256`, `FeatureStopReason`, `FeatureStartResult` | Shared Contract §2.1 | 全部字段、类型、enum 顺序和值冻结。 |
| `IFeatureModule`, `IFeatureBootstrap`, `IFeatureLifetime`, settings/events/logger/dependency interfaces | Shared Contract §2.2 | 以本文件 §3 的精确签名与 Required behavior 为准。 |
| `FeatureState`, `FeatureStatusView` | Shared Contract §2.3 | 全部字段及九个 enum 值冻结。 |
| `ItemGridPosition`, `ContainerReference`, `ContainerKind` | Shared Contract §3.1 | 全部字段及 enum 值冻结。 |
| `ItemPlacementIntent`, `ItemPlacementPreview`, `PlacementCandidateInput`, `IGridOccupancyView`, `IPlacementCandidateEvaluator` | Shared Contract §3.2 | 全部字段/成员冻结；坐标语义以本文件 §7 为准。 |
| `PlacementPreviewState`, `PlacementReason` | Shared Contract §3.3 | enum 名称、底层类型和显式数值全部冻结；未知值安全降级。 |
| `DragInteractionState`, `DragInteractionView` | Shared Contract §3.4 | 全部字段和五个状态值冻结；状态语义以本文件 §6 为准。 |
| `UpdateModuleConfigCommand`, `ScopedSettingChangeRequest`, `RequestModuleConfigSnapshotCommand`, `SettingMutation`, `ModuleConfigChangedEvent`, `ModuleConfigRejectedEvent`, status events, settings DTO/enums | Shared Contract §4.1 | 全部字段、类型、enum 顺序和值冻结；两个 config result event 必须包含 `RevisionScope`。 |
| capability/negotiation/handshake/chunk DTO 与 enum | Shared Contract §5.0 | 全部字段、类型、enum 顺序和值冻结；线路语义仍受本文件 §5 限制。 |
| `FrameworkErrorCode` | Shared Contract §6 | 名称、底层类型、显式数值和保留区段冻结。 |

因此，`FeatureDependency`、`Digest256`、`FeatureStopReason`、`ContainerKind`、`PlacementPreviewState`、`PlacementReason`、`IGridOccupancyView`、`CoreRuntimeStatusView`、`SettingMutation`、`SettingValue`、`FeatureSettingsSnapshot`、`NegotiatedFeatureView`、`SettingRevisionScope` 等嵌套 token 均是本基线的直接组成部分，不允许研究 Agent 自行补形状或选取旧版本。

| Type | Frozen fields |
| --- | --- |
| `FeatureId` | `string Value` |
| `ContractVersion` | `ushort Major`, `ushort Minor` |
| `FeatureScopeIdentity` | `FeatureId Id`, `Version FeatureVersion`, `string CurrentSlug`, `string DefinitionSetId`, `Digest256 DefinitionSetDigest` |
| `FeatureStartResult` | `bool Started`, `FrameworkErrorCode Error`, `string DiagnosticId` |
| `FeatureStatusView` | `FeatureId Feature`, `FeatureState State`, `FrameworkErrorCode Error`, `FeatureStopReason StopReason`, `string DiagnosticId`, `ulong StateRevision` |
| `ItemGridPosition` | `byte Page`, `byte X`, `byte Y`, `byte Rotation` |
| `ContainerReference` | `ContainerKind Kind`, `byte Page`, `uint SessionGeneration` |
| `ItemPlacementIntent` | `uint DragGeneration`, `ItemGridPosition Source`, `ContainerReference TargetContainer`, `ItemGridPosition Candidate` |
| `ItemPlacementPreview` | `uint DragGeneration`, `PlacementPreviewState State`, `ItemGridPosition Candidate`, `byte Width`, `byte Height`, `PlacementReason Reason` |
| `PlacementCandidateInput` | `uint DragGeneration`, `ItemGridPosition Source`, `ContainerReference TargetContainer`, `float CursorGridX`, `float CursorGridY`, `byte ItemWidth`, `byte ItemHeight`, `byte CurrentRotation`, `bool AllowAutomaticRotation`, `IGridOccupancyView Occupancy` |
| `DragInteractionView` | `uint DragGeneration`, `DragInteractionState State`, `ItemPlacementPreview Preview` |
| `ScopedSettingChangeRequest` | `ulong RequestId`, `SettingRevisionScope RevisionScope`, `uint ExpectedRevision`, `IReadOnlyList<SettingMutation> Mutations` |
| `UpdateModuleConfigCommand` | `ulong RequestId`, `FeatureId Feature`, `SettingRevisionScope RevisionScope`, `uint ExpectedRevision`, `IReadOnlyList<SettingMutation> Mutations` |
| `ModuleConfigChangedEvent` | `ulong RequestId`, `FeatureId Feature`, `SettingRevisionScope RevisionScope`, `uint Revision`, `FeatureSettingsSnapshot Snapshot` |
| `ModuleConfigRejectedEvent` | `ulong RequestId`, `FeatureId Feature`, `SettingRevisionScope RevisionScope`, `FrameworkErrorCode Error`, `uint CurrentRevision`, `FeatureSettingsSnapshot Snapshot` |
| `RequestModuleConfigSnapshotCommand` | `ulong RequestId`, `FeatureId Feature`, `SettingRevisionScope RevisionScope`, `uint KnownRevision` |
| `FeatureStatusChangedEvent` | `FeatureStatusView Status` |
| `CoreRuntimeStatusChangedEvent` | `CoreRuntimeStatusView Status` |
| `SessionReadyEvent` | `ulong ConnectionGeneration`, `ulong SnapshotId` |

附加不变量：

- `ContainerReference.SessionGeneration` 只拒绝过期 UI 上下文，不授予容器访问权。
- `ItemPlacementIntent`、`ItemPlacementPreview` 是进程内类型，不证明服务器接受。
- `FeatureSettingsSnapshot` 必须是单一 `FeatureId + SettingRevisionScope` 的完整不可变快照；前端不能拼接局部权威状态。
- `RequestId` 只做当前 connection generation 内的关联/幂等，不是身份、权限或授权。
- revision、LifecycleGeneration、ConnectionGeneration 和 CatalogGeneration 是独立排序域。

## 5. 冻结的消息表

| Kind | Type | Direction/scope | Required fields and rules |
| --- | --- | --- | --- |
| `0x0101` | `UpdateModuleConfigCommand` | Ready 后 client→server；ClientPreference 仅进程内 | `RequestId`, `Feature`, `RevisionScope`, `ExpectedRevision`, 完整 mutations；网络只允许 `ServerAuthority`。 |
| `0x0102` | `ModuleConfigChangedEvent` | Ready 后 server→client；确认的本地变更也可进程内 | 回显 `RequestId`；包含 `Feature`, `RevisionScope`, 新 `Revision`, 完整 snapshot。 |
| `0x0103` | `ModuleConfigRejectedEvent` | Ready 后 server→client | 回显 `RequestId`；包含 `Feature`, `RevisionScope`, stable error, `CurrentRevision`, 完整当前 snapshot。 |
| `0x0104` | `RequestModuleConfigSnapshotCommand` | Ready 后 client→server | 新的非零 `RequestId`, `Feature`, `RevisionScope`, `KnownRevision`；只读且不能增加 revision。 |
| `0x0201` | `FeatureStatusChangedEvent` | server→client，仅该客户端可见的 server/negotiated state；另允许进程内投影 | 完整 `FeatureStatusView`；前端按 revision 和安全 diagnostic identity 去重。 |
| process-local | `CoreRuntimeStatusChangedEvent` | core→本地 frontend；V1 不注册 LMN | 完整 `CoreRuntimeStatusView`，revision 单调递增。 |
| `0x0004` | `SessionReadyEvent` | capabilities channel 上 server→client | `ConnectionGeneration`, `SnapshotId`；之前禁止 settings/status 网络消息。 |

所有网络消息使用共享 envelope：`u16 contractMajor`, `u16 contractMinor`, `u16 messageKind`, `u32 payloadLength`, `bytes payload`。多字节整数 little-endian；字符串 UTF-8 且长度前置、有界；先验证再分配/构造 DTO。

V1 明确不注册以下库存插件网络消息：

- `CommitItemPlacementCommand`
- `RequestRotateItemCommand`
- `ItemPlacementCommittedEvent`
- `ItemPlacementRejectedEvent`
- `InventoryStateUpdatedEvent`

最终库存提交使用 Unturned 原生 `(page, x, y, rot)` 路径；成功/失败的事实来自原生库存投影，不创建平行 ACK 或回滚数据库。

## 6. 冻结的状态、超时与设置收敛

### FeatureState

```text
Discovered, Incompatible, Disabled, Starting, Running,
Isolating, Isolated, Stopping, Stopped
```

- ModuleRuntime/Lifecycle 是 `FeatureState`、`StateRevision` 和 feature status event 的唯一写入者。
- Runtime Admission 只提供静态准入，不写 lifecycle state、不读动态设置。
- SettingsRuntime 完成 migration/validation 后，Lifecycle 才能选择 Starting 或 Disabled。

### DragInteractionState

```text
Idle → Dragging → Hovering → Dropping → AwaitingProjection → Idle
```

- 每次新 Dragging 使用新的非零 `DragGeneration`；view close、role change、module isolation 和新拖动使旧 generation 失效。
- 释放无合法候选：回 Idle，保留原位置。
- 释放合法候选：锁定候选，调用原生 submission adapter；不乐观修改库存。
- 进入 AwaitingProjection 时立即清理增强拖动图层。
- 2.0 秒只结束增强 UI 等待，不推断拒绝、不回滚、不弹网络失败；迟到原生投影继续成为事实。

### Settings convergence

- 三秒无终态：使用同一 `RequestId` 重试一次。
- 八秒无终态：使用新的非零 `RequestId` 经 `0x0104` 请求完整快照。
- 重试/拒绝/无实际值变化不增加 revision；相同 RequestId 不同 payload 返回 `RequestIdConflict`。

## 7. 冻结的坐标语义

- 容器和 footprint 原点：左上；X 向右，Y 向下。
- `grabOffsetInFootprint`：当前朝向 footprint 左上到 pointer 的连续坐标，闭域 `[0,W] × [0,H]`。
- `CursorGridX/Y`：历史字段名，冻结语义是 intended item center，不是原始 pointer。

```text
intendedItemCenterGrid = pointerGrid + (currentFootprintCenter - grabOffsetInFootprint)
```

- intended center 的容器有效域：半开 `[0,containerWidth) × [0,containerHeight)`；越界返回 `Hidden/OutsideGrid`。
- forward/native `rot+1`：`(gx, gy) → (H - gy, gx)`，新 footprint 为 `H × W`。
- backward/native `rot-1`：`(gx, gy) → (gy, W - gx)`，新 footprint 为 `H × W`。
- 前端必须先转换 grab offset，再重新计算 intended center。
- 四次 forward 旋转必须在明确浮点容差内恢复 footprint 和 grab offset；测试必须区分 grab offset 闭域和 center 半开域。

## 8. Local-Fit Priority

1. 局部当前朝向能放下：立即返回，不检查旋转。
2. 局部当前失败且局部旋转能放下：返回局部旋转候选。
3. 两个局部候选都失败：按中心距离平方、Y、X 搜索全部当前朝向候选。
4. 只有当前朝向全局无候选，才搜索旋转朝向。
5. 不自动交换、重排或排序已占用物品。
6. evaluator 只返回 preview；最终提交使用原生库存路径。

Occupancy snapshot 必须排除当前拖动物品自身 footprint。RT-02 不得复制一套前端候选算法；RT-04 不得把 preview 当作服务器授权。

## 9. 契约变更流程

研究 Agent 发现不足时创建 Shared Contract Change Request，至少包含：

| Field | Requirement |
| --- | --- |
| `ChangeRequestId` | 稳定唯一 ID，例如 `SCR-RT02-001` |
| `RaisedBy` / `SourceTicket` | Agent 与 RT 票号 |
| `AffectedTokens` | 精确类型、成员、Kind、状态或公式 |
| `CurrentBaseline` | 当前冻结语义 |
| `ProposedChange` | 建议新语义，不得直接落入实现 |
| `Evidence` | 固定源码/IL/原型/运行证据引用 |
| `CompatibilityImpact` | frontend、backend、wire、persisted data、U3DS |
| `TestImpact` | 新增/修改的公共 seam 测试 |
| `GPTDecision` | Accept/Reject/NeedsEvidence + 理由 |
| `GeminiReview` | 前端可消费性 Accept/Block + 理由 |
| `HumanTrace` | 需要人工决定时的引用 |
| `Status` | proposed/accepted/rejected/superseded |

只有 GPT 明确 Accept、Gemini 对前端消费无阻断、文档和双语 token 同步后，变更才成为新基线。未接受请求不得进入 RT-02～RT-05 的实现建议。

## 10. RT-02～RT-05 统一源码身份和证据格式

### 10.1 冻结 SourceSet manifest

> Successor notice（2026-08-24）：`BUE-SS-20260824-02` 已批准冻结。完整不可变清单见 `BUE-SS-20260824-02-Manifest.md`。本节以下 `BUE-SS-20260824-01` 内容作为历史 predecessor 永久保留，不得原地改写；RT-02～RT-05 须迁移到 successor 后进入 RT-06。

本轮 SourceSetId：`BUE-SS-20260824-01`。

| Component | 状态 | 冻结身份 |
| --- | --- | --- |
| U3-SDK source | RESOLVED | 根 `D:\Agent-工作目录\U3-SDK`；Git commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`；tracked worktree clean（未跟踪 `audit/` 不属于源码集）。 |
| `PlayerInventory.cs` anchor | RESOLVED | `Assets/Runtime/Assembly-CSharp/Unturned/Player/PlayerInventory.cs`；SHA-256 `8485CBF8D4EC75A35D43A20F4BE8D6B401A58A68017B7EBA0E111D663F890FAC`。 |
| `PlayerDashboardInventoryUI.cs` anchor | RESOLVED | `Assets/Runtime/Assembly-CSharp/Unturned/UI/Player/PlayerDashboardInventoryUI.cs`；SHA-256 `593EDCB1AF5E19E548353BA3A2F97EA3351C1921DD348747AF99ABB95179566C`。 |
| `Items.cs` anchor | RESOLVED | `Assets/Runtime/Assembly-CSharp/Unturned/Inventory/Items.cs`；SHA-256 `8CEEB962BE413F4858BC904AA06FE6B3B4B05EE1211791F91EE4E847B44E0D22`。 |
| Client `Assembly-CSharp.dll` reference | RESOLVED | `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\Assembly-CSharp.dll`；assembly `Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`；SHA-256 `E1146353E5C9BFF901EE94829640D88919C5E89F6A6B90B22C73ABF5C1608F94`。 |
| Client BepInEx reference | RESOLVED | `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\BepInEx.dll`；assembly `BepInEx, Version=5.4.23.5, Culture=neutral, PublicKeyToken=null`；SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70`。 |
| U3DS `Assembly-CSharp.dll` reference | UNRESOLVED | 当前工作区未发现独立 U3DS reference。Owner：GPT/RT-04；必须在首次 U3DS IL claim 前登记绝对来源、assembly identity 和 SHA-256。未补齐前只能使用 U3-SDK `SOURCE_CONFIRMED`，不得写 U3DS `IL_CONFIRMED`。 |
| U3DS BepInEx reference | UNRESOLVED | 当前工作区未发现独立 U3DS BepInEx reference。Owner：GPT/RT-05；必须在双端交集结论前登记版本、来源和 SHA-256。未补齐前不得声称 U3DS BepInEx binary compatibility。 |
| LaunchMultiplayerNet source | RESOLVED-WORKTREE | 根 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`；base commit `e50082f983ac7cd33e008cca1a1a1dc3d9498d3f`，但工作区有受保护的未提交演进；按下述 manifest 算法得到 44 文件 digest `7151D22EF361B560F44A32963F82CD973AF64D7721F9D109C5492D1D7D864DE6`。研究必须针对该 digest，不能只引用 base commit。 |

LMN manifest 算法：递归纳入 `.cs/.csproj/.props/.targets/.md`，排除 `.git/bin/obj/.scratch/audit`；对每个文件计算 SHA-256，生成 `relative/path<TAB>lowercase-sha256`，按 ordinal 路径排序，以 LF 连接且末尾不追加 LF，再对 UTF-8 bytes 计算 SHA-256。任何文件变化都会产生新的 SourceSet change request；不得静默继续沿用本 ID。

SourceSet 使用规则：

- RT-02～RT-05 初始使用 `SourceSetId: BUE-SS-20260824-01`。若发布 successor，四张票在关闭前必须统一迁移到同一个最新批准 SourceSetId，并对受影响结论重新验证；最终报告头记录实际最终 ID，可以附 predecessor 历史，但不得出现四份最终报告使用不同 SourceSetId。
- RESOLVED 组件发生 hash/commit 变化时必须暂停对应结论并提交 SourceSet change request。
- `SourceSetId` 是不可变身份；组件集合、状态、路径、revision 或 digest 的任何变化都禁止沿用旧 ID。
- UNRESOLVED 组件只能由表中 Owner 统一补齐；补齐后由 GPT 发布新的 SourceSetId（例如 `BUE-SS-20260824-02`），记录 `PredecessorSourceSetId`、完整新 manifest 和变更摘要，并通知全部并行任务。
- 旧 SourceSet 永久保留且可重建；研究报告必须记录其实际使用的精确 ID。不得用“amendment”让一个 ID 指向两个内容集合。
- 未补齐 U3DS binary reference 不阻止 U3-SDK source 调研，但禁止把 source 事实升级为 U3DS IL、build 或 runtime 事实。

### 10.2 单条证据记录

每条 native/LMN/BepInEx 事实必须填写：

| Field | Requirement |
| --- | --- |
| `SourceSetId` | 本轮统一冻结源码集 ID |
| `Component` | U3-SDK / Assembly-CSharp / BepInEx / LMN 等 |
| `EnvironmentRole` | Client / SP Authority / P2P Host / P2P Client / U3DS |
| `RepositoryOrPackage` | 仓库、SDK 或 reference package 身份 |
| `Revision` | commit、tag、版本；不可用时写明原因 |
| `FileIdentity` | 相对路径 + 文件 SHA-256；程序集则 assembly identity + SHA-256 |
| `Symbol` | 完整 type/member/signature |
| `Location` | 行号或 IL offset |
| `EvidenceClass` | 下列固定枚举之一 |
| `Claim` | 该证据直接支持的最小事实 |
| `EnvironmentLimits` | 不适用或尚未验证的角色 |
| `CapturedBy/At` | Agent 与时间 |

固定 `EvidenceClass`（是正交证据类别，不是可以自动逐级升级的单线等级）：

- `SOURCE_CONFIRMED`：固定源码直接确认；不代表程序集或运行行为已验证。
- `IL_CONFIRMED`：固定程序集 IL 直接确认；必须记录 DLL SHA-256。
- `PROTOTYPE_ONLY`：原型行为；不代表生产集成、零 GC 或三环境 PASS。
- `BUILD_CONFIRMED`：候选编译成功；不代表游戏运行。
- `RUNTIME_CONFIRMED`：指定角色、CaseId、日志和同一 Candidate DLL SHA-256 的运行证据。
- `RELEASE_CONFIRMED`：同一候选满足全部发布门禁并获人工发布授权。
- `UNRESOLVED`：证据不足；必须转换为后续测试或采证义务。

禁止跨类别推导 runtime/release 结论，也禁止用一个环境角色替代另一个角色。

## 11. 双语与来源对账结果

| 对账面 | 结果 |
| --- | --- |
| 14 个共享函数签名及 Required behavior | PASS |
| 8 个 `IFeatureBootstrap` 属性 | PASS |
| `0x0101`～`0x0104`、`0x0201`、`0x0004` | PASS |
| 9 个 FeatureState、5 个 DragInteractionState | PASS |
| 2.0s/3s/8s 时间语义 | PASS |
| grabOffset 闭域、center 半开域、正反旋转公式 | PASS |
| Local-Fit Priority 六条规则 | PASS |
| 五个禁用库存插件消息 | PASS |
| Contracts/adapter 依赖和单一定义产物 | PASS |
| 英文 `spec.md` / 中文 `spec.zh-CN.md` token 对账 | PASS |
| 旧 Shared Contract DTO 漏字段 | 已修复：两个 event 均补入 `RevisionScope` |

## 12. RT-01 关闭门禁

- GPT 静态自检与独立审计必须 PASS。
- Gemini 必须逐项确认前端可消费性；若提出 blocker，RT-01 保持 claimed 并进入修订。
- Gemini 确认后，GPT 才能将 RT-01 标记 resolved，并释放 RT-02～RT-05 frontier。
- 本文件不授权生产编码、DLL 构建或运行 PASS 声明。

### 关闭记录

- GPT 独立静态审计：PASS。
- Gemini 前端消费复核：ACCEPT，阻断项为零。
- Gemini 确认不会擅自修改共享契约，RT-02/RT-03 发现不足时提交 Shared Contract Change Request。
- RT-01：resolved；RT-02～RT-05：解除 RT-01 blocker，可并行领取。

