# GPT-DEV-06 独立审计报告（Round 1）

审计范围：`DEV-06-network-codec-ready-fence.md`、`RT-06-Joint-Seam-Implementation-Readiness.md`、当前 DEV-06 源码/测试/构建产物。

基线：`BUE-V1-RT01-20260824`；SourceSet：`BUE-SS-20260824-02`；方案：`SCR-RT05-001 A`。

## 一、最终判定

**FAIL（存在阻断项，暂不得关闭 DEV-06，也不得宣称网络实现或三环境运行通过）**。

## 二、验证通过项

| 检查项 | 结果 | 证据 |
|---|---|---|
| Release solution 构建 | PASS | `audit/2026-08-25/DEV-06-independent-audit-build.log`；MSBuild exit 0，日志无 warning/error |
| 现有测试套件 | PASS | Contracts、Core/Definition、Settings、Placement、ClientUi、Network 测试均 exit 0；Network 输出 `DEV-06 network tests: PASS` |
| 52-byte prefix 常量与字段布局 | 静态 PASS | `src/BetterUnturnedExperience.Core/Network/BueNetworkProtocol.cs:73-124`，显式 little-endian 写入/读取 |
| 48-byte Ready payload 长度 | 静态/测试 PASS | `BueFrameCodec.ReadyPayloadLength=48`；Network test `Program.cs:32-34` |
| flags/reserved/version/短帧基础拒绝 | PASS | `BueNetworkProtocol.cs:108-118`；Network test `Program.cs:43-47` |
| generation 切换清理两个窗口 | PASS | `BueNetworkProtocol.cs:127-159`；Network test `Program.cs:68-82` |
| projection 不占写 replay window | PASS | `BueNetworkProtocol.cs:147-150`；Network test `Program.cs:72-75` |
| LocalLoopback 基本队列边界 | PASS | `LocalLoopbackTransport.cs:9-14`；Network test `Program.cs:84-89` |
| Contracts/Core 禁止 UI/native 类型 | PASS（静态） | 当前扫描：Unity/Glazier/Sleek/LMN/BepInEx/Harmony 均 0；仅出现项目命名空间文本 `Unturned` |
| 依赖方向 | PASS（静态） | Contracts 无项目引用；Core→Contracts；Transport→Core；未发现反向项目依赖 |

## 三、阻断项

### B-01：RequestId 未被编码/解码，导致 wire replay 身份丢失

- **位置**：`BueNetworkProtocol.cs:64-66`、`108-118`。
- `FencedFrame.FromWire(...)` 固定将 `RequestId` 设为 `0`；`BueFrameCodec.Encode` 也只编码 52-byte fence prefix 与 `Payload`，没有任何 message-specific 解码或 RequestId 保真路径。
- RT-01 明确 `0x0101`/`0x0104` 使用非零 `RequestId`；`ReadyFrameFence` 的 replay/in-flight 判定读取 `frame.RequestId`（`142-156`）。真实线路解码后不同请求会全部变成 `RequestId=0`，造成错误幂等、冲突判定和并发合并。
- 现有测试仅断言 payload 字节，不断言 `decoded.RequestId`（`Network.Tests/Program.cs:35-41`），因此未捕获。
- **修复要求**：按 RT-01 message-specific payload 实现 0101/0104 的非零 RequestId 解码，并增加 encode→decode 保真、不同 RequestId 同代并发、冲突及重试测试；不要用测试绕过协议字段。

### B-02：非法 generation/snapshot/RequestId 未在 decoder 层拒绝

- **位置**：`BueNetworkProtocol.cs:64-66`、`108-118`；`FencedFrame.Create:57-62`。
- `FromWire` 绕过 `ReadyBinding.Create`，接受 wire 中的 zero generation/snapshot；`TryDecode` 没有非零检查；`FencedFrame.Create` 允许 `requestId=0`。
- 冻结规格要求 Ready binding 的 generation/snapshot 非零，0101/0104 使用新的非零 RequestId，并要求先验证再进入 DTO/handler。当前实现把部分非法值推迟到 fence 或完全放行。
- **修复要求**：在 codec 层返回明确的非法字段错误；在 0101/0104 message-specific decoder 层拒绝 `RequestId=0`。补充零 generation、零 snapshot、零 RequestId、nonce mismatch 和 snapshot mismatch 测试。

### B-03：消息类型通过数值区间判定，未知 kind 可被误接收

- **位置**：`BueNetworkProtocol.cs:120`。
- `IsFencedKind` 使用 `kind >= 0x0101 && kind <= 0x0201`，因此任意落在区间内但未定义的 `ushort`（例如 `0x0105`）都会被当作合法 fenced kind；这违反 `UnknownMessageKind`/显式消息集合拒绝规则。
- **修复要求**：改为显式 switch/白名单，仅允许 `0x0101、0x0102、0x0103、0x0104、0x0201`，并增加未知 kind 编解码拒绝测试。

### B-04：ReadyFrameFence 无并发线性化，切代不是原子操作

- **位置**：`BueNetworkProtocol.cs:127-159`。
- `binding`、`writes`、`reads` 均为普通字段；`ReplaceBinding` 的赋值与 `Clear()` 没有锁，`TryAccept` 也没有锁。并发接收/切代时可能发生 dictionary/hashset 竞态、旧代检查后切代再执行 handler、`Clear` 与 `Add` 交错，甚至抛出集合并发异常。
- RT-06 要求 receive-time context 和 generation 切换具备原子边界；当前实现只在单线程测试中成立。
- **修复要求**：建立明确的锁/单写者执行上下文和 handler 提交语义。不能仅添加“看起来有锁”的代码；必须测试并发切代时旧帧不进入 handler、切代后窗口严格为空，且避免在锁内执行可重入外部 handler。

### B-05：LMN 委托适配器越过 Pump 直接回调，违反接收线程/队列 seam

- **位置**：`src/BetterUnturnedExperience.Transport/LmnTransportAdapter.cs:16-26`。
- 注册的外部 receiver 调用 `Dispatch` 后立即调用 `Receive`；`Pump()` 永远返回 0。RT-06 明确“receive 只解析排队；mutation 在受控执行上下文”，当前任意 LMN 回调线程都能直接触发上层处理。
- **修复要求**：Dispatch 只做拷贝并入队，Pump 在受控线程派发；明确队列上限、切代/关闭时清理和回调异常隔离。适配器仍不得引入 LMN/native 类型。

### B-06：V1 固定容量没有被实现/测试门禁固化

- **位置**：`ReadyFrameFence` 构造函数 `BueNetworkProtocol.cs:132`；Network test `Program.cs:49`、`77-82`。
- 构造函数接受任意 `writeCapacity/readCapacity`，生产调用方可建立非 V1 容量；现有测试从未填满 128 写窗口，也未验证第 129 个新 RequestId fail-closed。测试使用容量 1 的 fence 只能证明可配置窗口，不证明 V1 的 128/16 规范。
- **修复要求**：生产 seam 固定 128/16，或将可配置构造限制为仅测试内部并由公开工厂固定 V1 常量；补齐 128 个唯一写请求、129 个拒绝、旧记录仍可 replay，以及写满后 0104 仍可用并完成释放的测试。

## 四、非阻断但必须纳入后续验证

1. `TryAccept` 在调用外部 handler 前登记写 replay；handler 抛异常时写请求没有终态结果，读请求也不会自动 `CompleteRead`，可能造成槽位永久占用。需定义异常语义并测试 finally/隔离路径。
2. `TryDecode` 没有本层 payload 最大长度门禁；需由 envelope/transport 集成层在分配 DTO 前实施 `PayloadTooLarge`，并提供集成测试。
3. 当前测试未覆盖 `NonZeroReserved`、`StaleSnapshot`、`NonceMismatch`、组合 fence 失配和方向（client→server/server→client）白名单；这些应在修复回归中补齐。
4. 当前证据仅为静态、单进程单元测试和本地构建；没有 SP、SteamP2PFriends Host/Client 或 U3DS 运行证据，也没有真实 LMN 适配运行证据。

## 五、结论与交接

在时间窗较早的 solution 输出中曾出现构建成功，但该日志不覆盖当前新增源码。对当前稳定快照的独立 Network.Tests Rebuild 已失败，因此当前不能确认源码可编译；此前已经运行过的旧 Network.Tests 二进制 PASS 不能替代当前源码构建证据。

当前不可确认：当前快照编译闭环、真实 wire RequestId 语义、并发切代安全、受控接收线程、固定 V1 容量和完整非法字段拒绝。因此判定 **FAIL（至少 7 项阻断）**。修复后必须先冻结工作区，重新 Release Rebuild、运行全套测试、执行同一审计范围的独立 Round 2；Round 2 PASS 后再交 Gemini 复核。

## 六、后续源码变更时间窗说明

审计期间另一个开发过程已将 `BueEnvelopeCodec.cs` 与扩展 Network.Tests 写入工作区；该文件在本轮首次 solution build 后才出现，且当前 Core csproj 尚未包含该 Compile 项。因此本报告的 FAIL 还包括当前工作区的一致性门禁：必须停止并发改动后，以同一稳定快照重新执行 Rebuild，不能把前一时间窗的成功日志当作最新源码证据。当前最新单项目 Rebuild 已复现 CS0246/CS0103（缺失 `BueEnvelope`/`BueBootstrapCodec` 类型或引用）。

另外，当前新增 `BueEnvelopeCodec.IsSupportedKind` 仍使用 `0x0001..0x0004` 区间，`0x0005` 等未知值会被误接收；应改为显式白名单，并把新文件纳入 Core csproj 后重新测试。`BueNetworkProtocol.IsFencedKind` 的区间判定已在当前工作区改为显式白名单，但该修订尚未通过稳定快照的完整编译/审计。

