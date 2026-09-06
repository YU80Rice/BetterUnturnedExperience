# GPT-DEV-06 独立审计报告（Round 4）

## 1. 审计元数据

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-06-network-codec-ready-fence.md`
- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 连接上下文裁定：`SCR-RT05-001` 方案 A
- 审计轮次：Round 4；本轮只读审计，未修改源码、LMN 或工单状态
- 审计范围：显式消息 kind 白名单、ReadyFrameFence dispatch/切代线性化、并发与异常策略、构建/测试、隔离边界、DEV-06 范围边界

## 2. 最终判定

**PASS（本轮独立审计无阻断项）。**

Round 3 的两个阻断均已闭环：

1. `0x0105`、`0x0106` 已加入 Contract envelope encode/decode 拒绝回归测试；
2. `ReadyFrameFence` 已引入独立 `dispatchSync`：状态锁不跨越外部 handler，`ReplaceBinding` 与 handler 提交共享同一线性化 seam；并发切代及 handler 异常策略已有测试覆盖。

## 3. 执行证据

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

实际输出为：

```text
DEV-02 definition linker tests: PASS
DEV-03 settings runtime tests: PASS
DEV-04 placement evaluator tests: PASS
DEV-05 ClientUi tests: PASS
DEV-06 network tests: PASS
ALL TESTS: PASS
```

### 3.3 显式消息白名单

`BueEnvelopeCodec.IsSupportedKind(ushort)` 当前为显式 `switch`，允许集合固定为：

```text
0x0001, 0x0002, 0x0003, 0x0004,
0x0101, 0x0102, 0x0103, 0x0104,
0x0201
```

网络测试现已覆盖：

- `0x0005` encode reject；
- `0x0105`、`0x0106` Contract envelope encode reject；
- `0x0105`、`0x0106` Contract envelope decode reject；
- `0x0105` fenced-kind decode reject。

均断言稳定的 `UnknownMessageKind` 或 `InvalidMessageKind`，未知编号在进入 handler 前拒绝。

### 3.4 ReadyFrameFence 线性化与异常策略

当前实现采用两级受控边界：

- `sync`：只保护 binding、replay、read 状态；
- `dispatchSync`：串行化状态检查/登记与外部 handler 的提交顺序，并与 `ReplaceBinding` 共用。

`TryAccept` 的顺序为：

1. 在 `dispatchSync` 下读取并校验当前 binding；
2. 在 `sync` 下登记 replay/read；
3. 释放 `sync`，执行外部 handler；
4. handler 异常时：只读请求释放 in-flight 槽位；写 mutation 保留 replay 占位并 fail-closed；
5. `ReplaceBinding` 只有在当前 dispatch 完成后才能切代并清空旧窗口。

测试覆盖了阻塞 handler 与并发 `ReplaceBinding`：替换线程在 handler 释放前不能完成；释放后旧 binding 的 frame 被 `StaleGeneration` 拒绝，旧 replay/read 状态为零，新代可复用旧 RequestId。测试也覆盖了读 handler 异常释放槽位、写 mutation handler 异常保留 replay 占位。

该模型没有把状态锁跨越外部代码，且 `ReplaceBinding`/handler 入口共享明确的 V1 dispatch seam；满足当前单写者/受控执行上下文要求。

### 3.5 静态隔离与依赖方向

`eng/Verify-NoUiTokens.ps1` 扫描结果：

- Contracts：PASS（2 个 C# 文件）
- Core：PASS（9 个 C# 文件）
- Transport：PASS（2 个 C# 文件）

额外扫描 Core/Transport 的 `.cs` 与 `.csproj`，未发现：

```text
UnityEngine, SDG.Unturned, Glazier, Sleek, BepInEx,
Harmony, LaunchMultiplayerNet, Steamworks,
Assembly.GetTypes, PatchAll
```

`LMN` 仅出现在实验性适配器的命名/文件边界，不存在 LMN 类型或协议源码引用。依赖方向静态通过：`Contracts <- Core <- Transport`；未见反向项目引用。

### 3.6 当前产物 SHA-256

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `068A6DD7D14C0FA004E36C38F4728ADC76A2701B40F11FE3CACE14F297E3F23F` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `2A78538DD468A8FF358C227DE6B43241CD59C20C2E626247660DF4AD24D1197C` |
| `src/BetterUnturnedExperience.Transport/bin/Release/BetterUnturnedExperience.Transport.dll` | `595BFE6BFF1B07AF95B77215DB3C82B62B71E1CAEC09DF76ED6913ED1843B1E4` |
| `tests/BetterUnturnedExperience.Network.Tests/bin/Release/BetterUnturnedExperience.Network.Tests.exe` | `753D74732FB4F2AB7541E02FA70B1209911B0C200BDF860AC88474229552706D` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |

关键源码哈希：

| 文件 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Core/Network/BueEnvelopeCodec.cs` | `040AB034AB781DC82F376AEDBBD455B3486BEE2E9C4E1D65EACDABA04C59C282` |
| `src/BetterUnturnedExperience.Core/Network/BueNetworkProtocol.cs` | `2C6D9913442C7FD40617BA623C98A944430357EA2A2F49F16E627BF9F39B2CC4` |
| `tests/BetterUnturnedExperience.Network.Tests/Program.cs` | `F31AEEA842E9EFC4024319A080BADC09822FEA35FDA7D7FE1B9F910F935AB78E` |

## 4. 逐项裁定

| 审计项 | 判定 | 依据 |
|---|---|---|
| 52-byte Ready prefix / 48-byte Ready payload | PASS | `BueFrameCodec` 常量、Little-Endian roundtrip 测试 |
| version/flags/reserved/短帧/绑定失配拒绝 | PASS | `TryDecode` 检查与网络测试 |
| 显式 Contract kind 白名单 | PASS | 显式 switch + 0x0005/0105/0106 encode/decode 回归 |
| 0101 非零 RequestId 与 128 写 replay window | PASS | 网络测试，满载 fail-closed |
| 0104 独立 16 read in-flight window | PASS | 独立容量、重复、释放、写满仍可读测试 |
| 切代原子清空与 handler 提交顺序 | PASS | `dispatchSync` 与阻塞 handler 并发切代测试 |
| handler 异常资源策略 | PASS | read slot 释放、mutation replay 保留测试 |
| LocalLoopback 与 LMN delegate adapter | PASS | 单进程可观察行为测试；仅证明 seam，不证明真实 LMN |
| Contracts/Core/Transport 隔离 | PASS | 静态扫描与项目图 |
| DEV-06 范围控制 | PASS | 未实现库存 RPC、未修改 LMN、未宣称三环境运行 |

## 5. 非阻断建议

1. `FencedFrame.Create` 对设置请求的零 RequestId 仍在 `Encode`/`TryAccept` 才拒绝；可作为后续诊断收敛优化，不阻断 DEV-06。
2. `TryDecode` 可将零 RequestId 从 `InvalidBinding` 细分为独立 codec 诊断值；不影响当前 fail-closed 行为。
3. `LmnTransportAdapter` 的队列上限、关闭/代际清理、callback 异常隔离应在真实 adapter/CandidateBuild 门禁中另行处理；本票仅要求实验性委托 seam。
4. 后续仍需 DEV-07 的 SP、SteamP2PFriends Host/Client、U3DS 同哈希运行与发布门禁；本报告不升级这些证据等级。

## 6. 证据边界

本轮仅证明当前源码的静态边界、Release 构建、单进程测试、受控并发测试及产物哈希。未证明：

- 真实 SteamP2PFriends Host/Client 网络运行；
- U3DS 实际加载或 Headless 运行；
- 真实 LMN 网络互通；
- 原生库存权威链；
- 三环境同哈希 CandidateBuild 或发布授权。

## 7. 审计结论

**Round 4 独立审计 PASS。** DEV-06 具备交付 Gemini 前端消费复核的条件；只有 Gemini 复核通过并由人工批准后，方可将工单推进至 `resolved`。本审计未修改工单状态。
