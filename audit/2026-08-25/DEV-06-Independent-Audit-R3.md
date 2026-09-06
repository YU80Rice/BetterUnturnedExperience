# GPT-DEV-06 独立审计报告（Round 3）

## 1. 审计元数据

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-06-network-codec-ready-fence.md`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 连接上下文裁定：`SCR-RT05-001` 方案 A
- 审计轮次：Round 3；本轮仅审计，未修改源码、LMN 或工单状态
- 审计范围：显式消息 kind 白名单、ReadyFrameFence 切代/handler 线性化、构建与测试、隔离边界、DEV-06 范围边界

## 2. 最终判定

**FAIL（2 项阻断，DEV-06 当前不得关闭，也不得交 Gemini 作最终 ACCEPT）。**

本轮确认 R2 的两个实现修复已经进入当前源码：

1. `BueEnvelopeCodec.IsSupportedKind` 已改为显式 `switch` 白名单；
2. `ReadyFrameFence.TryAccept` 已把 handler 调用移入同步区，使 `ReplaceBinding` 无法越过正在提交的旧帧。

但是，DEV-06 的独立审计门禁仍未满足：

1. 网络测试源没有补齐 R2 要求的 `0x0105`、`0x0106` Contract envelope encode/decode 拒绝回归断言；
2. 网络测试源没有并发切代压力断言，且当前实现将可重入外部 handler 直接置于状态锁内，缺少明确的受控提交 seam。

## 3. 构建与测试证据

### 3.1 Release 重建

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：**PASS，exit 0，0 errors，0 warnings**。

### 3.2 全套测试

| 测试 | 结果 |
|---|---|
| DEV-02 Contracts/Definition Linker | PASS |
| DEV-03 SettingsRuntime | PASS |
| DEV-04 PlacementEvaluator | PASS |
| DEV-05 ClientUi | PASS |
| DEV-06 Network | PASS |

测试输出分别为 `DEV-02 definition linker tests: PASS`、`DEV-03 settings runtime tests: PASS`、`DEV-04 placement evaluator tests: PASS`、`DEV-05 ClientUi tests: PASS`、`DEV-06 network tests: PASS`，汇总 `ALL TESTS: PASS`。

### 3.3 静态隔离与依赖方向

`Verify-NoUiTokens.ps1`：

- Contracts：PASS（2 个 C# 文件）
- Core：PASS（9 个 C# 文件）
- Transport：PASS（2 个 C# 文件）

对 Core/Transport 源码及项目文件进行的额外扫描中，未发现 `UnityEngine`、`SDG.Unturned`、`Glazier`、`Sleek`、`BepInEx`、`Harmony`、`LaunchMultiplayerNet`、`Steamworks`、`Assembly.GetTypes` 或 `PatchAll`。`LMN` 仅出现于实验适配器的类名/文件名，不是 native/LMN 类型引用。

项目依赖方向静态通过：`Contracts <- Core <- Transport`；Core 未反向引用 Transport，Contracts 未引用 UI、native、LMN 或 adapter。

### 3.4 当前 Release 产物 SHA-256

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `068A6DD7D14C0FA004E36C38F4728ADC76A2701B40F11FE3CACE14F297E3F23F` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `1B4E7445492B1EBC854158B8FFFAB94E26B98AF70750B343E15F891988A0FCE4` |
| `src/BetterUnturnedExperience.Transport/bin/Release/BetterUnturnedExperience.Transport.dll` | `595BFE6BFF1B07AF95B77215DB3C82B62B71E1CAEC09DF76ED6913ED1843B1E4` |
| `tests/BetterUnturnedExperience.Network.Tests/bin/Release/BetterUnturnedExperience.Network.Tests.exe` | `2CCDA372D352925819A255645445F9EA07BD940C4C690DC43904FA4FB018D625` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |

### 3.5 当前关键源码 SHA-256

| 文件 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Core/Network/BueEnvelopeCodec.cs` | `040AB034AB781DC82F376AEDBBD455B3486BEE2E9C4E1D65EACDABA04C59C282` |
| `src/BetterUnturnedExperience.Core/Network/BueNetworkProtocol.cs` | `ECA6F7BD6DEE274D344F9865714F06F166B9604771FF5D417929FAA7A2AAABF1` |
| `src/BetterUnturnedExperience.Transport/LmnTransportAdapter.cs` | `9FBAA24974879079079BCDCD697DD59CD731617602B9D89574B13D5587BE3E64` |
| `tests/BetterUnturnedExperience.Network.Tests/Program.cs` | `BFE7F9E9D12074AE0BCC3048F21D9BFA2AEEC4B8F44BF209D4F906F05D4AD9EE` |

## 4. 阻断项

### B-01：显式白名单代码已修复，但回归测试仍不完整

位置：`src/BetterUnturnedExperience.Core/Network/BueEnvelopeCodec.cs`、`tests/BetterUnturnedExperience.Network.Tests/Program.cs`。

当前 `IsSupportedKind(ushort)` 使用显式 `switch`，源码满足协议白名单要求。对当前 Release DLL 的独立黑盒探针也确认 `0x0005`、`0x0105`、`0x0106`、`0x0202` 的 `TryEncode` 均返回 `false / UnknownMessageKind`。

但提交的网络测试源仅覆盖 `0x0005` 的 envelope encode 拒绝，以及 `0x0105` 的 fenced-kind decode 拒绝；没有覆盖 R2 明确要求的 `0x0105`、`0x0106` **Contract envelope** encode/decode 拒绝。黑盒探针不能替代仓库内可重复的 TDD 回归测试，因此该机械门禁仍为阻断。

最小修复：在 `BetterUnturnedExperience.Network.Tests` 的公共 codec 测试中，分别对未知 `BueEnvelopeMessageKind` `0x0105`、`0x0106` 执行 `TryEncode` 和构造合法 header 后执行 `TryDecode`，断言均为 `UnknownMessageKind`；保留 `0x0005` 与现有 fenced-kind 测试。

### B-02：切代顺序有保护，但 handler 仍直接运行在状态锁内，且缺少并发回归测试

位置：`src/BetterUnturnedExperience.Core/Network/BueNetworkProtocol.cs:152-178`；测试：`tests/BetterUnturnedExperience.Network.Tests/Program.cs`。

当前实现把 `handler(frame)` 放在 `lock (sync)` 内。它确实建立了一个可观察的顺序：`ReplaceBinding` 不能在 handler 返回前完成；因此对“handler 已经开始执行后再切代”的交错，旧帧不会在 `ReplaceBinding` 完成之后才进入 handler。这是当前修复的有效部分。

但该方案仍把可重入外部回调直接置于保护 replay/read/binding 的状态锁内，调用方可在 handler 中重入 fence，或阻塞其它线程的切代；handler 抛异常时已登记的 replay/read 槽位也没有明确的失败终态。R2 要求的是受控提交模型并增加并发切代测试，而当前测试只有单线程 `ReplaceBinding` 清空窗口断言，没有并发压力、阻塞 handler、切代等待、旧帧不穿透及异常路径断言。

最小修复建议：增加独立的受控 dispatch seam（例如提交锁/单写者队列），让 binding/replay/read 状态锁不跨越外部 handler；`ReplaceBinding` 与 dispatch 提交必须共享同一线性化顺序，切代时清理尚未提交的旧代工作项。至少补充并发测试：阻塞旧 handler、并发调用 `ReplaceBinding`，断言切代不会越过正在提交的 handler；释放后旧帧不能再次进入 handler，旧 replay/read 状态清空，且无集合并发异常。另补 handler 异常时 slot/replay 的明确策略测试。

## 5. 已核对通过的 DEV-06 项

| 项目 | 判定 |
|---|---|
| 52-byte Ready prefix / 48-byte Ready payload | PASS |
| Little-Endian 字段编码 | PASS |
| BUEB 36-byte bootstrap reject | PASS |
| 10-byte Contract envelope 与 16 KiB payload 上限 | PASS |
| 显式 supported-kind 代码白名单 | PASS（测试覆盖仍见 B-01） |
| version/flags/reserved/短帧/绑定失配拒绝 | PASS |
| 0101 RequestId 非零、128 写 replay window | PASS（单线程） |
| 0104 独立 16 read in-flight window、写满时仍可用 | PASS（单线程） |
| generation 切换清空窗口 | PASS（单线程） |
| LocalLoopback transport | PASS |
| LMN 委托 adapter 入队/Pump 边界 | PASS（实验性静态/单进程测试） |
| Core/Contracts/Transport 类型隔离与依赖方向 | PASS（静态） |
| DEV-06 未实现原生库存 RPC、未修改 LMN、未宣称三环境运行 | PASS |

## 6. 非阻断建议

1. `FencedFrame.Create` 对 `UpdateModuleConfig`/`RequestModuleConfigSnapshot` 的零 RequestId 仍在 `Encode`/`TryAccept` 阶段拒绝；可在 public factory 处提前拒绝并使用稳定的 `InvalidRequestId` 构造错误。
2. `TryDecode` 将零 RequestId 映射为 `InvalidBinding`，建议增加稳定的 codec 诊断值，避免把请求字段错误归入 binding。
3. `LmnTransportAdapter` 仍无队列上限、close/generation 清理和 callback 异常隔离；其作为实验性 adapter 未阻断 DEV-06，但进入 CandidateBuild 前必须另立门禁。
4. 本轮未把 `LmnTransportAdapter` 的实现宣称为真实 LMN 网络运行通过；LMN 工作树状态不作为本轮修改证据。

## 7. 证据边界

本轮证明范围仅为当前源码静态检查、Release 重建、单进程测试、黑盒 codec 探针和哈希采集。未证明：

- SteamP2PFriends Host/Client 运行；
- U3DS 实际加载或 Headless 运行；
- 真实 LMN adapter 网络运行；
- 原生库存权威链；
- 三环境同哈希 CandidateBuild 或发布授权。

## 8. 复审前置条件

修复 B-01、B-02 后，以同一源码快照重新执行 Release 0/0、全套 DEV-02～DEV-06 测试、静态隔离扫描和新的独立 Round 4 审计；只有 Round 4 PASS 后，才可交 Gemini 复核并考虑将 DEV-06 标记 `ready-for-human`。
