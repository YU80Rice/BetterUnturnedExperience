# GPT：RT-06 联合 Seam 与实施就绪包

> 作者：GPT  
> 契约基线：`BUE-V1-RT01-20260824`  
> SourceSet：`BUE-SS-20260824-02`  
> 证据边界：本文是实施规格，不是生产运行 PASS

## 1. 收敛结论

RT-01～RT-05 的共享契约、前端 U3-SDK 调研、后端原生库存权威、运行时/网络/设置调研已对账。没有需要将 Glazier、Sleek、Unity 或 LMN native type 提升到公共 Contracts 的理由。

`SCR-RT05-001` 裁定：

- BUE V1 在 Ready 后 application frame 上必须验证 `ConnectionGeneration + SnapshotId + HandshakeNonceBinding`。
- 任何服务器权威设置 mutation 前必须经过 frame fence；失配一律 fail-closed。
- replay window 按 `ConnectionGeneration` 分域，切代清理，固定容量，容量满时拒绝新唯一请求，不淘汰旧记录制造重放窗口。
- `RequestId` 仅是当前连接代际内的关联/幂等键，不是连接身份或授权。
- LMN connection-bound token 已证明可行，但需修改 LMN 和 SourceSet，仅作未来 defense-in-depth，不阻塞 BUE V1。

## 2. 冻结的内部 Adapter Seams

| Seam | 输入 / 输出 | 线程与 lifetime | 错误边界 |
| --- | --- | --- | --- |
| `ClientInventoryInteractionAdapter` | pointer/grab/native page → 纯值候选输入；合法候选 → native `sendDragItem` | Unity game thread；跟随 dashboard/drag generation | 普通网格才允许增强；AREA、equipment、special branch 原生放行 |
| `NativeInventoryProjectionRelay` | native callbacks → callback 栈结束后的只读快照 | game thread；绑定 drag/container generation | 不把单个 add/remove 解释为 BUE ACK；歧义时跟随最新原生事实 |
| `SettingsPresentationAdapter` | static Settings Facet + dynamic `FeatureSettingsSnapshot` → UI | client UI thread；按 feature scope 对称卸载 | 150ms/PointerUp 仅防抖；不静默改写本地偏好 |
| `FeatureRuntimeAdapter` | compiled definition/catalog → scoped bootstrap/lifecycle | 启动拓扑序，停止逆序；lifecycle generation 独立 | 模块故障局部隔离；核心不变量失败进 SafeMode |
| `INetworkTransport` adapters | BUE application frames ↔ LocalLoopback/LMN | receive 只解析排队；mutation 在受控执行上下文 | LMN 缺失/不兼容不影响 core/local-only feature |
| `ReadyFrameFence` | frame + current ready context → accept/reject | connection generation 切代原子更换；replay 有界 | 在 SettingsRuntime 之前拒绝 stale/mismatch/replay/full；不声称加密认证 |
| `HeadlessCompositionGate` | compiled environment facets → client UI registration/no-op | composition root；启动时一次判定 | `ClientUiAvailable && !Application.isBatchMode && !Headless`；Core/Contracts 零 UI token |

## 2.1 Ready-frame fence V1 规范线路编码

本节是 `SCR-RT05-001` 方案 A 的 normative wire schema。多字节整数全部 little-endian；固定字节数组不带长度前缀。

### Ready 建立

`0x0004 SessionReadyEvent` 仍使用 RT-01 的 10-byte Contract envelope，其 payload 在 V1 精确为 48 bytes：

```text
offset  width  field
0       8      u64 ConnectionGeneration (non-zero)
8       8      u64 SnapshotId (non-zero, unique inside generation)
16      16     ClientNonce (exact bytes from accepted Hello)
32      16     ServerNonce (exact bytes from accepted Snapshot)
```

只有在客户端同时验证 generation、snapshot 和两个 nonce 与当前握手候选视图完全相等后，才能原子发布 Ready。`0x0004` 本身不携带下述 fence prefix，也不进 replay window。

### Ready 后 fenced payload

`0x0101`～`0x0104` 与网络形式 `0x0201` 的 Contract envelope 不变；它们的 payload 必须以下列 52-byte prefix 开始，之后紧跟 RT-01 冻结的 message-specific payload：

```text
offset  width  field
0       1      u8 FenceVersion = 1
1       1      u8 Flags = 0
2       2      u16 Reserved = 0
4       8      u64 ConnectionGeneration
12      8      u64 SnapshotId
20      16     ClientNonce
36      16     ServerNonce
52      N      MessageSpecificPayload
```

- client→server：`0x0101`、`0x0104`；server 必须在解码 DTO/分配可变长对象前比较当前 receive-time Ready context。
- server→client：`0x0102`、`0x0103`、`0x0201`；client 必须在投影前比较当前 Ready context。
- 进程内的 ClientPreference event 与 `CoreRuntimeStatusChangedEvent` 不使用 wire prefix。
- Flags/Reserved 非零、FenceVersion 未知、payload 短于 52 bytes、任一绑定值失配：整帧静默拒绝，只写限频结构化诊断，不回送可被放大的错误帧，不进 DTO handler/mutation/projection。
- `0x0101` 在 fence 通过后检查当前 generation 的有界写入 replay window（V1 容量 128）。同代重复返回已缓存结果或拒绝重入；写窗口满时使用内部 reason `ReplayWindowFull` fail-closed，不调用 mutation。
- 只读 `0x0104` **不占用也不查询 `0x0101` 写入 replay window**。它使用当前 generation 独立的有界 in-flight 域（V1 容量 16）：接受时登记 RequestId，同 RequestId 并发重复合并/拒绝重入，快照响应写入 transport queue 后立即释放槽位，连接切代时整域清理。它不增加 revision、不调用 mutation，因此完成后同 RequestId 再次执行只会返回当前快照。这保证 `0x0101` 写窗口满载时，RT-13 的 8 秒 `0x0104` 最终收敛仍可用。
- `0x0104` in-flight 域自身满载时限频拒绝新请求，但已完成请求必须释放槽位，不得因历史 RequestId 进入永久满载。前端仍按 RT-13 的超时/快照收敛，不新增弹窗错误码。

### Decoder 分流与版本降级

1. capabilities channel 先检查固定 ASCII magic `BUEB`。magic 匹配时只进 bootstrap decoder；bootstrap 字段非法时直接拒绝，不回退到 Contract envelope。
2. magic 不匹配才进 RT-01 Contract envelope decoder。`0x0001`～`0x0004` 是握手消息，不允许加 52-byte Ready fence prefix；`0x0004` 必须精确使用上述 48-byte payload。
3. 握手能力交集必须包含标识 `betterunturned.ready-frame-fence.v1`，才允许双方进入“可网络写设置/Status projection” Ready 模式。
4. 同 Major 但对端 Minor/能力不支持 fence V1 时，连接和原版游戏保持可用；BUE 本地功能可继续，网络权威设置置为 `Unavailable/Degraded` 只读，不得发送无 fence 的 `0x0101`～`0x0201`。
5. Major 不兼容仍按 BUEB Reject/原版静默降级处理。不得为兼容旧端在同一 kind 上接受 fenced/unfenced 两种 payload。

两个 nonce 只是握手关联绑定，不是 MAC，不替代 transport sender identity、认证、授权或服务器政策检查。

## 3. Better Item Interaction 端到端时序

1. Client adapter 从原生 drag state 读取 pointer、page、footprint、rotation 与 `grabOffsetInFootprint`。
2. Presenter 计算 `intendedItemCenterGrid = pointerGrid + (currentFootprintCenter - grabOffsetInFootprint)`。
3. 纯 C# evaluator 按 Local-Fit Priority 返回 preview：局部当前 → 局部旋转 → 全局当前 → 全局旋转。
4. UI 只渲染 preview；不修改原生 inventory model。
5. 释放时，普通网格合法候选调用原生 `sendDragItem`；特殊分支放行原生逻辑。
6. 服务器继续使用 `ReceiveDragItem` 权威验证；BUE/LMN 不建立平行库存 RPC。
7. Client 进入 `AwaitingProjection`；2.0s 只结束增强视觉等待，不推断拒绝、不回滚、不弹错误。
8. native projection 无论是否迟到都是唯一事实源；只在 generation/session/fingerprint 高置信匹配时收敛本次 drag。

## 4. 设置、网络、生命周期与 Headless 时序

1. Definition Linker 在构建期生成单一 compiled definition/catalog，包含 identity、entry binding、dependencies、settings facet 与 environment facet。
2. Runtime 不扫描类型；按 catalog 组装 core，仅在 Headless gate 通过时组装 ClientUi。
3. Local-only settings 由本地 runtime 直接执行；单人使用 loopback/direct path，不强依赖 LMN。
4. 联机 Ready 完成后发布当前 `ConnectionGeneration/SnapshotId/nonce binding`；前端仅消费当前代际投影。
5. ServerAuthority 写入先经 frame fence、权限/政策和 revision 检查，再进 SettingsRuntime 原子事务。
6. 3s 同 RequestId 最多重试一次；8s 通过 `0x0104` 拉取快照收敛，不跨连接代际重放。
7. Feature 失败时按 9 态生命周期投影；局部 `Isolated` 对称卸载 UI，Core SafeMode 卸载全部自定义 UI，仅在独立 fallback 可用时提示一次。

## 5. 转换为实施/运行门禁的未决事实

| Obligation | 验证方式 | 所属 Candidate |
| --- | --- | --- |
| `VO-RT02-01` hook 特殊分支不越权 | C# adapter tests + SP/P2P runtime | Better Item Interaction client adapter |
| `VO-RT02-02` 连续 drag 0 GC 目标 | Release allocation profiler | preview renderer/evaluator |
| `VO-RT02-03` 三环境 drag/projection | 同 DLL 哈希的 SP、P2P Host/Client、U3DS case | CandidateBuild |
| `VO-RT03-01` Core/Contracts 零 UI token | CI IL reachability scan | unified DLL |
| `VO-RT03-02` U3DS 真实加载 | 有 BUE plugin 的新日志，不接受 `0 plugins` | CandidateBuild |
| `VO-RT03-03` SafeMode 卸载 | fault-injection runtime test | client CandidateBuild |
| Ready-frame fence 生产等价性 | 保留 prototype 安全性测试并加 codec/transport integration | network core |
| `0x0101`/`0x0104` 容量域隔离 | 写窗口满载时仍能完成只读快照收敛；`0x0104` 完成后释放 in-flight slot | network/settings integration |
| 原子 settings persistence | temp file + flush + atomic replace + corruption recovery tests | settings runtime |

## 6. 首个 CandidateBuild 同哈希验收矩阵

| 环境 | 必须证明 | 不可代替的证据 |
| --- | --- | --- |
| SP | Core/local settings/Better Item Interaction 启动，拖放与原生投影收敛 | candidate DLL hash + fresh client log + case timeline |
| SteamP2PFriends Host | Host 权威设置、原生 inventory authority、frame fence | Host 与 Client 相同 DLL hash + 共享 CaseId + 双端时间线 |
| SteamP2PFriends Client | Ready/projection/degradation，断线快速重连 stale write 被拒绝 | Client fresh log + 与 Host 匹配的 frame/revision 指纹 |
| U3DS | 零 UI TypeLoad，server settings/feature lifecycle 正常 | candidate DLL hash + 有插件加载的 fresh U3DS log；BepInEx bootstrap 单独不足 |

任一环境 PASS 都不能代替另一环境。任何 DLL/source 改动都使旧运行证据失效。

## 7. 生产 Tracer-bullet 票据顺序

1. `DEV-01` Repository/solution skeleton + Contracts，先建立双端可编译基线和 UI-token CI gate。
2. `DEV-02` Definition Linker + compiled catalog + lifecycle/bootstrap，使用最小 sample fixture，不新增产品功能。
3. `DEV-03` SettingsRuntime + atomic persistence + LocalLoopback，先交付单人本地闭环。
4. `DEV-04` Better Item Interaction evaluator + native adapter contract tests，保留 Local-Fit Priority 测试。
5. `DEV-05` Gemini ClientUi/Glazier integration，消费冻结 DTO，不向 Contracts 泄漏 native type。
6. `DEV-06` BUE protocol codec + ReadyFrameFence + experimental LMN adapter，不复制 inventory RPC。
7. `DEV-07` CandidateBuild + SP/P2P/U3DS 同哈希验收包。

## 8. 当前裁定

- 后端与共享契约层面：`READY FOR GEMINI REVIEW`。
- 生产实现：待 Gemini 对本包无阻断复核且 RT-06 独立审计 PASS 后开放 `DEV-01`。
- 未开放发布：当前仍无 BUE CandidateBuild 的三环境运行证据。
