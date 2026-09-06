# GPT-Network-Capability-Versioning-Spec：网络能力协商、版本降级与状态投影

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT-11 决策基线；实现、编译与三环境运行未验证**  
**输入确认:** 人工开发者于 2026-08-24 接受全部七项网络政策

## 1. 目标与事实边界

本规范冻结单人、SteamP2PFriends 与 U3DS 共用的能力模型、应用层握手、版本降级、消息边界、状态投影和不可信客户端边界。

本地 `LaunchMultiplayerNet` V5 源码已静态确认存在 V2 命名频道、频道版本 Hello/Ack 与可靠发送 API，依据包括 `Protocol/HandshakeProtocol.cs`、`Routing/NamespacedTransport.cs` 和 `NAMED_CHANNEL_STANDARD.md`。这些静态事实不证明本插件握手已经实现或在任何环境运行通过。

LMN 握手只回答“双方是否接受某命名频道及频道版本”。本插件必须在已接受的 `betterunturned.core.capabilities` 频道上执行自己的应用握手，才能判断框架、契约、功能和能力是否兼容。频道可用不是身份认证、权限授予或功能运行证明。

## 2. 统一能力模型

能力由稳定 `FeatureId + CapabilityId + CapabilityVersion` 标识。`CapabilityId` 使用小写 ASCII 标识，规范化后匹配 `^[a-z0-9][a-z0-9._-]{0,95}$`；显示名称不是身份。

每个能力声明包含提供者 FeatureId/FeatureVersion、CapabilityId/Version、方向、要求等级、环境掩码及所需 Contract。方向为 `LocalOnly / ServerToClient / Bidirectional`；要求为 `Optional / RequiredForFeature / RequiredForSession`。

`RequiredForSession` 在 V1 默认禁止模块自行声明。当前没有任何功能能以插件或能力缺失阻止原版连接。

服务器为每个客户端生成 `NegotiatedFeatureView`：本地/远端版本、可用能力交集、功能是否启用、降级原因和裁定 revision。客户端只能消费裁定结果，不能自行宣布服务器权威能力已启用。

单人环境把本地客户端清单与本地权威清单送入同一个纯裁定器，得到与多人相同的结果；不得维护“单人默认全部兼容”的特供分支。

## 3. 三阶段应用握手

### 3.1 状态机与消息

```text
LMN channel accepted
  → CapabilityHello
  → CapabilitySnapshot
  → CapabilityAck
  → SessionReadyEvent
```

1. 客户端发送新的 `ConnectionGeneration`、128-bit 随机 `ClientNonce`、框架/契约版本、功能和能力清单。
2. 服务器从连接对象确定发送者，验证边界并计算交集；生成 `ServerNonce`，回显 generation/client nonce 并返回裁定快照。
3. 客户端验证 generation、两个 nonce、完整性与兼容性，原子发布候选协商视图，发送 Ack 回显 server nonce 与 snapshot id。
4. 服务器收到有效 Ack 后将会话置为 Ready 并发送 `SessionReadyEvent`；之后才发送服务器设置快照和可见功能状态。

Nonce 只绑定本次应用握手和拒绝旧包，不是密码学身份认证。权限仍来自当前 Unturned/LMN 连接上下文。

### 3.2 超时、重试与重连

- 应用握手总预算 10 秒。
- 客户端在第 0、2、5 秒最多发送三次相同 generation/nonce 的 Hello；重复包幂等。
- 服务器对相同 generation/nonce 重放相同 Snapshot，不重复创建会话状态。
- 超时只禁用依赖插件网络能力的功能，不踢出玩家、不阻断原版连接。
- 重连、换服或 transport identity 改变时生成新 generation/nonce，并清除旧能力、RequestId 缓存、分片缓存、设置覆盖和状态投影。

## 4. 版本兼容与降级

- Contract Major 相同才能解析应用消息；Minor 差异按已知必需前缀与显式能力降级。
- 本地 Minor 较高不得推定远端支持新字段；未知必需能力禁用所属功能，未知可选能力忽略。
- Feature Major 不同：只令该功能 `Incompatible/FeatureVersionMismatch`。
- Feature Major 相同、Minor/Patch 不同：共同能力满足全部 `RequiredForFeature` 才启用。
- 版本使用结构化整数，不比较显示字符串。
- required dependency 继续服从 GPT-09 DAG 与级联规则。
- 远端未安装插件、缺少能力频道或握手超时：视为没有插件网络能力，原版连接继续。

框架和功能在线路上统一使用固定 `WireSemanticVersion`：`u16 Major + u16 Minor + u16 Patch`，不编码 prerelease/build metadata，也不直接序列化 `System.Version`。任一组件超过 `65535` 时拒绝注册；显示版本可更丰富，但不参与线路兼容裁定。

Contract Major 不匹配时，服务器不尝试发送正常 CapabilitySnapshot，而发送独立于 Contract envelope 的固定 bootstrap reject 帧。接收方在 capabilities 频道上先检查 bootstrap magic，再尝试普通 Contract envelope：

```text
4 bytes magic = ASCII "BUEB"
u8 bootstrapVersion = 1
u8 bootstrapKind = 1 (HandshakeReject)
u16 payloadLength = 28
u64 connectionGeneration
u64 clientNonceHigh
u64 clientNonceLow
u16 errorCode
u16 supportedContractMajor
```

全部多字节整数 little-endian，总长度必须精确为 36 bytes，不允许尾随字节。magic 不匹配时才进入普通 envelope parser；magic 匹配但 bootstrapVersion、kind、长度、generation 或 nonce 无效时直接丢弃并限频记录，不回退成普通消息。客户端据此立即降级，无需等待 10 秒超时。

## 5. 不可信客户端与服务器权威

客户端 Hello 中的 FeatureId、版本、能力、SteamID、角色、管理员标志和功能状态均不具有授权意义。服务器必须从 transport/connection 上下文识别发送者，只把声明用于兼容交集，并对设置写入重新验证权限、状态、revision、类型和值域。

所有集合、字符串、分片和总内存先验证后分配。客户端自报身份或授权字段必须忽略；畸形输入或模块异常不能传播到其他模块。

## 6. 集合、负载与原子分片上限

| 对象 | V1 硬上限 |
| --- | ---: |
| 单框架消息 payload | 16 KiB |
| 单分片业务数据 | 12 KiB |
| 单快照总长度 | 512 KiB |
| 单快照分片数 | 48 |
| 每端功能数 | 128 |
| 每功能能力数 | 32 |
| 每端能力总数 | 512 |
| 每功能设置描述/快照条目 | 256 |
| 单次设置 command mutations | 64 |
| 同连接同时接收的分片快照 | 2 |
| 同连接分片缓存总量 | 1 MiB |
| FeatureId/CapabilityId UTF-8 | 各 96 bytes |

超过上限拒绝整份消息或快照，不截断后继续协商。

Hello 功能清单、服务器裁定快照和设置快照都可能超过单包上限，因此统一使用分片封装。`0x0001` 和 `0x0002` 的 payload 始终是 `SnapshotChunkEnvelope`；即使逻辑消息只有一片，也必须使用 `ChunkCount=1 / ChunkIndex=0`，收齐后再按 `SnapshotKind` 解码内部 `CapabilityHello` 或 `CapabilitySnapshot`，不存在裸 DTO 线路变体。分片携带 `ConnectionGeneration / SnapshotId / SnapshotKind / ChunkIndex / ChunkCount / TotalLength / ChunkLength / SHA256 / bytes`：

- 重复同内容分片幂等；同索引不同内容使整份快照失败。
- 所有分片元数据必须一致；收齐后校验总长度、内容哈希、集合和版本，再一次性发布。
- 缺片、冲突、超长、哈希失败或 10 秒超时均销毁整份快照。
- generation 改变立即销毁；SHA-256 固定为 32 bytes，只做完整性检查，不替代认证。SnapshotId 是当前 generation 内随机非零 `u64`，只做关联与去重。
- 同一次逻辑 Hello 的第 0、2、5 秒重试复用同一个 SnapshotId、SHA-256 和分片内容，不占用新的并发快照槽。

## 7. 功能状态投影范围

`FeatureStatusChangedEvent` 网络方向冻结为 server→client，只发送当前客户端可见或依赖的服务器功能状态、该客户端协商导致的不可用状态，以及本地化错误码、StopReason、状态 revision 和不透明 DiagnosticId。

不发送其他玩家的客户端模块状态、异常堆栈、文件/配置路径、敏感 payload 或未公开服务器功能。客户端本地模块状态继续走进程内事件。最终可用性是“本地 Running 且服务器裁定允许”的交集。

## 8. 前端降级表现

- 握手中：需要网络的功能只读并显示“正在同步”；本地纯 UI 功能可继续。
- 单功能不兼容：只置灰该功能并显示友好版本提示。
- 握手超时或能力频道缺失：按 GPT-13 静默降级；仅在玩家打开设置或尝试使用相关功能时显示行级说明，原版游戏继续。
- Contract Major 不兼容：停止全部插件网络功能，但不声称原版连接失败。
- 普通玩家不看到 nonce、generation、堆栈、路径或原始 payload。

## 9. 消息分配

| kind | 消息 | 方向 |
| --- | --- | --- |
| `0x0001` | CapabilityHello/Chunk | client→server |
| `0x0002` | CapabilitySnapshot/Chunk | server→client |
| `0x0003` | CapabilityAck | client→server |
| `0x0004` | SessionReadyEvent | server→client |
| bootstrap kind `1` | HandshakeReject | server→client；独立 `BUEB` 固定帧，不使用 Contract envelope |
| `0x0101` | UpdateModuleConfigCommand | client→server；Ready 后 |
| `0x0102` | ModuleConfigChangedEvent/SnapshotChunk | server→client；Ready 后 |
| `0x0103` | ModuleConfigRejectedEvent/SnapshotChunk | server→client；Ready 后 |
| `0x0104` | RequestModuleConfigSnapshotCommand | client→server；Ready 后 |
| `0x0201` | FeatureStatusChangedEvent | server→client；Ready 后 |

消息与 LMN 命名频道唯一映射：普通 Contract envelope `0x0001–0x0004` 只走 `betterunturned.core.capabilities`；独立 `BUEB/bootstrapKind=1` 也只走该频道并先于普通 envelope 判别；普通 `0x0005` 保留且 V1 不得发送。`0x0101–0x0104` 只走 `betterunturned.core.settings`；`0x0201` 只走 `betterunturned.core.diagnostics`。这些频道名在 GPT-14 确认长期归属命名空间前仍为 Draft，实现不得允许同一 kind 在多个频道注册。

为避免 bootstrap magic 与普通 envelope 首四字节理论碰撞，普通 Contract 永久禁止 `contractMajor=0x5542` 且 `contractMinor=0x4245` 的组合（little-endian 字节正好为 ASCII `BUEB`）；正常版本从低位连续分配，不会使用该保留组合。

## 10. 测试不变量

- LMN 频道接受但应用 Contract Major 不同，只禁用插件网络能力。
- SP、P2P Host loopback、P2P Client 和 U3DS 使用同一裁定器与消息语义。
- 重复、乱序、旧 generation、nonce 不匹配和超时不会进入 Ready。
- 功能 Major 不同只禁用该功能；Minor 差异按能力交集。
- 客户端伪造管理员、SteamID、状态或能力不能获得权限。
- 分片缺失、冲突、超长、哈希失败和连接切换均不发布部分快照。
- 缺少插件或握手超时不阻止原版连接。
- 普通 Contract envelope `0x0005` 必须拒绝；BUEB 不进入普通 messageKind 分派；client→server 方向的 BUEB Reject 必须拒绝。
- `0x0104` 只能读取当前发送者可见的单功能/作用域快照，使用新的非零 RequestId，受连接 generation 约束，且不得改变 revision。每连接、每 FeatureId/RevisionScope 每 5 秒补充 1 个令牌、突发容量 2；超额返回 `RateLimited`。

## 11. 非目标

- 不把 LMN 或本握手当作认证、加密、反作弊或入服系统。
- V1 不启用 `RequiredForSession`。
- 不同步库存权威状态；库存仍走原版路径。
- 不宣称网络协议已通过编译或三环境运行验证。
