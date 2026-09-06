# GPT-DEV-15C 独立审计 R2

## 一、结论

**总体判定：FAIL / BLOCKED（流程门禁阻断；R1 生产代码本身 PASS）。**

`f80d2d1` 的 Projection Relay 修复在并发、代际、消费者异常和 revision 过滤方面通过本轮代码审计；`2843dde` 的交接内容与 Gemini R1 `ACCEPT` 证据已核对。但 DEV-15C 工单当前错误地标记为 `resolved`，而验收清单仍全部未勾选，且关闭项引用旧版 Gemini 报告。因此在补齐关闭证据前，不得正式关闭本票，也不得进入 DEV-15D 的验收宣称。

## 二、审计对象与固定点

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-15C-projection-relay-awaiting-projection.md`
- 规格：`.scratch/better-unturned-experience-architecture/spec-DEV-15-better-item-interaction.md`
- 修复提交：`f80d2d15b6219f66f15f89467ea5b555ed263196`
- 交接提交：`2843dded17f8c369e6dd54f22871313cadfbf254`
- 实现：`src/BetterUnturnedExperience.ClientUi/InventoryProjectionRelay.cs`
- 测试：`tests/BetterUnturnedExperience.ClientUi.Tests/Dev15CTests.cs`
- Gemini 复核：`.scratch/better-unturned-experience-architecture/handoffs/DEV-15C-Remediation-Review-R1.md`（`ACCEPT`）

## 三、双轴审计结果

### 规格轴：PASS（实现范围）

| 要求 | 结果 | 证据 |
|---|---|---|
| callback 只入队，Pump 受控消费 | PASS | `TryCapture/TryEnqueue` 只写固定环形队列；`Pump` 才调用 consumer。 |
| Pump 与 Bind/Invalidate 线性化 | PASS | `pumpSync → sync` 锁序覆盖 `Pump`、`Bind`、`Invalidate`，未发现反向锁序。 |
| 旧代际/旧容器静默丢弃 | PASS | `MatchesGenerationAndContainer` 在消费前校验 Drag/Kind/Page/Session。 |
| NativeRevision 防倒灌 | PASS | 小于当前 revision 的快照计入 dropped；绑定重置时 revision 归零。 |
| consumer 异常 fail-closed | PASS | 捕获异常后立即失效绑定、清空队列并停止本次 Pump。 |
| fingerprint 歧义不生成拒绝 | PASS | `AwaitingProjectionController.Apply` 返回 `ObservedLatestFact`，不产生 ACK/回滚结论。 |
| 2 秒仅视觉预算 | PASS | `Tick` 只结束等待态；未发现拒绝、重试或伪回滚路径。 |

### 标准轴：代码 PASS，流程 FAIL

- 代码未发现锁序反转或明显 UI/Native 类型泄漏；Release 与静态门禁均通过。
- **硬阻断**：工单第 4 行为 `Status: resolved`，但第 42–50 行验收项仍全部为 `[ ]`；`RuntimeFix-DEV15C-2230.md` 也明确写明等待 GPT/Gemini 复核。当前状态与审计事实自相矛盾。
- 关闭项第 50 行仍引用旧 `DEV-15C-Projection-Relay-Review.md`，没有引用当前 R1 文件。
- 判断性建议（不构成代码阻断）：`Pump` 在持有 `pumpSync` 时调用外部 consumer，后续可补充长调用/重入压力测试；`ProjectionPumpDecision` 当前未使用，可在后续清理。

## 四、验证回路

### 构建

命令：

```text
dotnet build BetterUnturnedExperience.sln --configuration Release --nologo
```

结果：`0 errors / 0 warnings`。

### 测试

7 个 Release 测试程序全部返回 `0`：Contracts、Settings、Placement、Network、ClientUi、Plugin、Release。ClientUi 输出：

```text
DEV-05/DEV-15A/DEV-15B/DEV-15C ClientUi tests: PASS
```

### 静态门禁

```text
Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.ClientUi  -> PASS (9 files)
Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Contracts -> PASS (2 files)
Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Core      -> PASS (10 files)
```

### 当前文件 SHA-256

| 文件 | SHA-256 |
|---|---|
| `InventoryProjectionRelay.cs` | `B1C77BBB87026870AD3250C7957201393770420E2507DABF6ECAD5600413C5FA` |
| `Dev15CTests.cs` | `4DE22427074922890BB95E9A646DCB085826A87035C8FB0A077FD9F71598B4E3` |
| `to-DEV-15C-Remediation-R1.md` | `83A7108EE762547C31F62C2971372E2E03147078C37A914EE7F12794C8EC2E53` |
| `DEV-15C-Remediation-Review-R1.md` | `A3E32FFD216C0D3CBEEEEBF61D95043C98E2E8323B716848F1273CEF01901468` |

## 五、必须完成的解除条件

1. 将 DEV-15C 工单从错误的 `resolved` 改回未关闭状态（建议 `claimed`），直到双端证据回填完成。
2. GPT 独立审计通过后，勾选 GPT 审计项；将 Gemini 关闭项引用更新为 `DEV-15C-Remediation-Review-R1.md`，再由维护者确认是否正式关闭。
3. 建议补充一条测试：以 `INativeInventoryProjectionSource` 接口引用调用 `TryCapture`，而不是只通过具体 `TryEnqueue` 验证；该项不改变本轮代码 PASS，但应在关闭前补齐证据。

## 六、证据边界

本报告只证明纯 C# Projection Relay 与 AwaitingProjection Seam 的静态/单元测试闭环；不证明真实 Unity/Harmony callback、单人、SteamP2PFriends、U3DS、实机库存收敛或发布资格。


