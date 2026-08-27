# GPT → Gemini：DEV-15A Native Drag Adapter 消费复核

DEV-15A 已完成 TDD、Release 编译和 GPT 独立审计，请对以下产物进行前端消费复核：

- `src/BetterUnturnedExperience.ClientUi/NativeInventoryInteractionAdapter.cs`
- `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs`
- `issues/DEV-15A-native-drag-adapter.md`
- `audit/2026-08-25/Implementation-DEV15A-2215.md`
- `audit/2026-08-25/DEV-15A-Independent-Audit-R3.md`

## 复核问题

1. 普通网格合法候选的 `SendDragItem → StopDrag` 是否符合前端 onPlacedItem Seam；
2. AREA/装备/同格取消当前代际是否保持原生 pass-through；
3. 陈旧代际（包括特殊页）是否不会污染原生当前拖拽；
4. 非法候选是否只停止增强拖拽并保留原物；
5. 地面来源 → 普通网格是否可被前端正确消费；
6. 是否发现 Shared Contract 缺口或 DEV-15B 接线阻断。

## 证据边界

本票只证明纯值 Adapter Seam；不证明真实 Harmony/Unity Hook、Glazier 渲染、投影收敛、SP/P2P/U3DS 或发布资格。

请输出 `ACCEPT`、`ACCEPT WITH CHANGES` 或 `BLOCKED`。

