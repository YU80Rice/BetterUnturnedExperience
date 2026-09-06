# GPT-DEV-06 独立终审报告（Round 2）

## 1. 审计范围

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-06-network-codec-ready-fence.md`
- 实施基线：`BUE-V1-RT01-20260824`、`BUE-SS-20260824-02`、`SCR-RT05-001 方案 A`
- 规格：`.scratch/better-unturned-experience-architecture/RT-06-Joint-Seam-Implementation-Readiness.md`
- 源码：`BueNetworkProtocol.cs`、`BueEnvelopeCodec.cs`、`LocalLoopbackTransport.cs`、`LmnTransportAdapter.cs`
- 测试：`BetterUnturnedExperience.Network.Tests` 及全套 DEV-02～DEV-06 测试
- 本轮仅审计，未修改源码或 LMN。

## 2. 最终判定

**FAIL（2 项阻断，当前不得关闭 DEV-06，也不得宣称三环境运行或发布通过）**。

构建和已有测试均通过，但下列协议/并发门禁尚未满足；因此不能以“测试 PASS”替代独立审计 PASS。

## 3. 执行证据

### 3.1 Release 重建

命令：

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release "/p:Platform=Any CPU" /m /v:minimal
```

结果：**PASS，exit 0，0 errors，0 warnings**。

构建日志：[DEV-06-independent-audit-build-current.log](./DEV-06-independent-audit-build-current.log)

### 3.2 全套测试

| 测试 | 结果 |
|---|---|
| DEV-02 Contracts/Definition Linker | PASS |
| DEV-03 SettingsRuntime | PASS |
| DEV-04 PlacementEvaluator | PASS |
| DEV-05 ClientUi | PASS |
| DEV-06 Network | PASS |

网络测试输出为 `DEV-06 network tests: PASS`，但当前测试仍未覆盖本报告第 4 节的两个阻断。

### 3.3 Native token 与依赖扫描

对 Contracts/Core/Transport 的 `.cs` 与 `.csproj` 扫描结果：

- `UnityEngine`：0
- `Glazier`：0
- `Sleek`：0
- `LMN` / `LaunchMultiplayerNet`：0（项目命名空间文本不计入 native token）
- `BepInEx`：0
- `Harmony`：0
- Core 仅引用 Contracts；Transport 仅引用 Core；未见反向项目引用：**PASS（静态）**。

### 3.4 当前构建产物 SHA-256

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `E5C19BF9D701DF00FB4F6532C5D3F52D817751C3A4B53423B6802249B4F31982` |
| `src/BetterUnturnedExperience.Transport/bin/Release/BetterUnturnedExperience.Transport.dll` | `595BFE6BFF1B07AF95B77215DB3C82B62B71E1CAEC09DF76ED6913ED1843B1E4` |
| `tests/BetterUnturnedExperience.Network.Tests/bin/Release/BetterUnturnedExperience.Network.Tests.exe` | `A87888DDE6EC07012BF6715ED05ACEC802DEDFC6CFE8C7013704B5EA7A82055E` |

源码审计摘要：

- `BueNetworkProtocol.cs`: `CAFE120D32677422A5AC6B3EB7D0C57414F1D15536AC796A80A5022447ACF130`
- `BueEnvelopeCodec.cs`: `07F7064B4BFBDB37557DE8FE15C4BA66D3B6D1B966358E0BEC0703BCFBEE2385`
- `LmnTransportAdapter.cs`: `9FBAA24974879079079BCDCD697DD59CD731617602B9D89574B13D5587BE3E64`

## 4. 阻断项

### B-01：Contract Envelope 仍使用数值区间而非显式 kind 白名单

位置：`src/BetterUnturnedExperience.Core/Network/BueEnvelopeCodec.cs:67-71`。

`IsSupportedKind(ushort)` 当前使用：

```csharp
(kind >= 0x0001 && kind <= 0x0004)
|| (kind >= 0x0101 && kind <= 0x0104)
|| kind == 0x0201
```

就当前已冻结的连续编号而言，`0x0001..0x0004` 与 `0x0101..0x0104` 恰好覆盖现有定义，因此 `0x0005`、`0x0105` 当前会被上界拒绝；但实现仍不是规范要求的显式白名单，未来在区间中加入未登记编号时会被静默接受，且代码审计无法把“允许集合”固定为协议枚举集合。RT-06 要求 bootstrap 与 Contract decoder 均采用显式 kind 白名单；未知 kind 必须在进入 envelope/handler 前拒绝。

修复要求：改为对 `0x0001、0x0002、0x0003、0x0004、0x0101、0x0102、0x0103、0x0104、0x0201` 的显式比较或 `switch`，并增加至少 `0x0005`、`0x0105`、`0x0106` 的 encode/decode 拒绝测试。

### B-02：ReadyFrameFence 的 handler 提交不是切代线性化

位置：`src/BetterUnturnedExperience.Core/Network/BueNetworkProtocol.cs:152-178`。

当前代码在锁内完成 binding/replay/read 判定并登记，随后释放锁，再调用 `handler(frame)`。因此存在以下可观察交错：

1. 线程 A 在旧 binding 下通过 `TryAccept`，登记写请求或 read in-flight；
2. 线程 B 执行 `ReplaceBinding`，清空旧窗口并安装新 binding；
3. 线程 A 在切代之后才执行旧 frame 的 `handler`。

这满足“状态登记”而不满足 RT-06 要求的“receive-time context 与切代的原子提交边界”：旧代帧仍可能进入 mutation/projection handler。当前使用 `lock` 只保护字典/集合，不保护 handler 提交语义；网络测试也没有并发切代压力断言。

修复要求：选择并明确一个受控提交模型，例如让 fence 输出带 generation 的已接纳工作项并由单写者 dispatcher 与 `ReplaceBinding` 同序列化，或在 handler 提交前再次验证不可变 binding token 并在切代时使已接纳但未提交的工作失效。不得把可重入外部 handler 无条件放在锁内；必须增加并发测试，断言切代后旧帧不进入 handler、窗口严格清空且不发生集合并发异常。

## 5. 已核对通过的协议项

| 项目 | 结果 | 证据 |
|---|---|---|
| Ready payload 48 bytes | 静态/测试 PASS | `BueFrameCodec.ReadyPayloadLength=48`；Network.Tests roundtrip |
| Ready fence prefix 52 bytes | 静态/测试 PASS | `PrefixLength=52`，little-endian 字段写入/读取 |
| 10-byte Contract envelope | 测试 PASS | `BueEnvelopeCodec.HeaderLength=10` |
| BUEB reject 36 bytes | 静态/测试 PASS | `BueBootstrapCodec.FrameLength=36` |
| RequestId wire roundtrip | 测试 PASS | `Network.Tests` 对 `decodedRequest.RequestId == 42` 断言 |
| wire/handler 零 RequestId 基础拒绝 | 部分 PASS | `Encode`、`TryDecode`、`TryAccept` 拒绝设置请求零值 |
| Fence version/flags/reserved/短帧 | 测试 PASS | 对应 decoder checks 与回归断言 |
| generation/snapshot 零值 | 静态 PASS | `TryDecode` 拒绝 zero binding |
| 128/16 默认容量 | 测试 PASS | 128 个写请求后第 129 个 fail-closed；读槽位独立且可释放 |
| 写满时 0104 可用 | 测试 PASS | default fence 写满后 snapshot read 仍 Applied |
| 切代清空窗口 | 测试 PASS（单线程） | `ReplaceBinding` 清理 writes/reads |
| LMN adapter 入队/Pump | 测试 PASS | callback 入队时不触发 Receive，Pump 后派发 |
| LocalLoopback | 测试 PASS | 双端队列投递 |

## 6. 需要补强但本轮不单独计阻断的事项

1. `FencedFrame.Create` 对 `UpdateModuleConfig`/`RequestModuleConfigSnapshot` 的零 RequestId 仍允许构造，直到 Encode/TryAccept 才拒绝；建议在 public factory 处立即拒绝，并增加 factory 级测试。
2. `TryDecode` 将零 RequestId 映射为 `FrameDecodeError.InvalidBinding`，建议增加稳定的 `InvalidRequestId` decode error，避免诊断语义混淆。
3. `LmnTransportAdapter` 当前队列无上限、无 close/generation 清理和 callback 异常隔离；若作为实验适配器进入生产候选，应增加资源边界测试。
4. LMN 工作树在本审计开始时已存在大量 tracked/untracked dirty 状态；本轮未修改 LMN，但无法从当前工作树证明其相对于指定 SourceSet 的完整不变性。该事实不改变本轮源码未写入结论，但限制“未修改 LMN”的证据强度。

## 7. 证据边界

本轮证明范围仅为当前源码的静态检查、Release 构建、单进程测试和 SHA-256。没有证明：

- SteamP2PFriends Host/Client 运行；
- U3DS 实际加载与 Headless 运行；
- 真实 LMN 适配器运行；
- 原生库存权威链；
- 三环境同哈希 CandidateBuild 或发布授权。

## 8. 复审前置条件

修复 B-01、B-02 后，必须以同一稳定源码快照重新执行：

1. Release Rebuild，要求 0 errors / 0 warnings；
2. 全套 DEV-02～DEV-06 测试；
3. native token 与依赖方向扫描；
4. 显式 kind、RequestId、并发切代回归测试；
5. 独立 Round 3 审计。

Round 3 PASS 后再交 Gemini 复核；在此之前不得将 DEV-06 标记 `resolved`。

