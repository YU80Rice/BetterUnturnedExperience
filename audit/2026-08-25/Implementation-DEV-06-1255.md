# DEV-06 实施报告：BUE Network Codec、ReadyFrameFence 与 Transport Seam

## 一、任务与基线

- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 连接上下文：`SCR-RT05-001` 方案 A
- 工单：`DEV-06-network-codec-ready-fence.md`
- 当前状态：`ready-for-human`

本票建立 BUE 自有应用层网络 seam；LMN 仅作为实验性委托适配器，不修改 LMN，不复制库存 RPC，不进入 DEV-07 三环境发布验收。

## 二、TDD 与修复闭环

1. Red：先以测试驱动协议、编解码与 fence seam，缺少实现时得到编译失败。
2. Green：实现 52-byte fence、48-byte Ready payload、10-byte Contract envelope、36-byte BUEB reject、replay/in-flight、LocalLoopback 与 LMN delegate adapter。
3. 独立审计 R1/R2/R3：发现并修复 wire RequestId、工程编译纳入、显式 kind 白名单、受控回调线性化、容量回归覆盖及异常资源策略。
4. 最终 Round 4 独立审计：PASS，无阻断项。

## 三、源码溯源

| 需求 | 落实位置 |
|---|---|
| Little-Endian Ready/Contract/BUEB 编解码 | `src/BetterUnturnedExperience.Core/Network/BueNetworkProtocol.cs`、`BueEnvelopeCodec.cs` |
| 52-byte prefix 与 48-byte Ready payload | `BueFrameCodec` |
| 显式 Contract kind 白名单 | `BueEnvelopeCodec.IsSupportedKind` |
| generation/snapshot/双 nonce 校验 | `ReadyBinding`、`ReadyFrameFence.TryAccept` |
| 128 写 replay 与 16 读 in-flight | `ReadyFrameFence` |
| 受控 handler 提交与切代线性化 | `ReadyFrameFence.dispatchSync`、`ReplaceBinding` |
| 本地进程传输 | `LocalLoopbackTransport.cs` |
| LMN 实验性队列适配器 | `src/BetterUnturnedExperience.Transport/LmnTransportAdapter.cs` |
| TDD seam 与并发/异常回归 | `tests/BetterUnturnedExperience.Network.Tests/Program.cs` |

## 四、构建与测试验证

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`0 errors / 0 warnings`。

| 测试 | 结果 |
|---|---|
| DEV-02 Contracts/Definition Linker | PASS |
| DEV-03 SettingsRuntime | PASS |
| DEV-04 PlacementEvaluator | PASS |
| DEV-05 ClientUi | PASS |
| DEV-06 Network | PASS |

静态门禁：Contracts/Core UI token scan PASS；Transport/Network 禁止 token 扫描 PASS；依赖方向 `Contracts <- Core <- Transport` PASS。

## 五、最终产物 SHA-256

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `068A6DD7D14C0FA004E36C38F4728ADC76A2701B40F11FE3CACE14F297E3F23F` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `2A78538DD468A8FF358C227DE6B43241CD59C20C2E626247660DF4AD24D1197C` |
| `src/BetterUnturnedExperience.Transport/bin/Release/BetterUnturnedExperience.Transport.dll` | `595BFE6BFF1B07AF95B77215DB3C82B62B71E1CAEC09DF76ED6913ED1843B1E4` |
| `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll` | `A9387A4DAFEFAA0A5ED809C379599B9F0F71B1490FE73F7A741A762CFC925FC2` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |
| `tests/BetterUnturnedExperience.Network.Tests/bin/Release/BetterUnturnedExperience.Network.Tests.exe` | `753D74732FB4F2AB7541E02FA70B1209911B0C200BDF860AC88474229552706D` |

关键源码：`BueEnvelopeCodec.cs`=`040AB034AB781DC82F376AEDBBD455B3486BEE2E9C4E1D65EACDABA04C59C282`；`BueNetworkProtocol.cs`=`2C6D9913442C7FD40617BA623C98A944430357EA2A2F49F16E627BF9F39B2CC4`；`LmnTransportAdapter.cs`=`9FBAA24974879079079BCDCD697DD59CD731617602B9D89574B13D5587BE3E64`；网络测试源=`F31AEEA842E9EFC4024319A080BADC09822FEA35FDA7D7FE1B9F910F935AB78E`。

## 六、独立审计记录

- R1：FAIL，修复 RequestId、项目纳入、并发、容量、LMN 回调边界等问题。
- R2：FAIL，修复显式 kind 白名单和切代提交线性化。
- R3：FAIL，要求补齐 `0x0105/0x0106` Contract encode/decode 回归及独立 dispatch seam、并发切代、handler 异常测试。
- R4：PASS，报告：`audit/2026-08-25/DEV-06-Independent-Audit-R4.md`。

## 七、证据边界与后续门禁

本报告仅证明静态隔离、Release 构建、单进程测试、受控并发测试及产物哈希。未证明真实 LMN 网络互通、SteamP2PFriends Host/Client、U3DS Headless、原生库存权威链、三环境同哈希 CandidateBuild 或发布授权。下一步交 Gemini 做前端消费复核；Gemini 与人工批准前不得将 DEV-06 标记 `resolved`。

