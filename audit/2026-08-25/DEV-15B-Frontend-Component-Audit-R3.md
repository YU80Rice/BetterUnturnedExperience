# GPT-DEV-15B 前端表现组件独立复核 R3

## 判定

**PASS（静态/TDD 门禁）**。R2 的两项阻断已在当前源码中闭环：资产身份进入预览链，surface context 与容器会话代际门禁已接入。DEV-15B 可恢复为 `resolved`，但本结论不等价于真实 Unity/Glazier 运行通过。

## 独立验证

- `dotnet build BetterUnturnedExperience.sln --configuration Release --nologo`：0 errors / 0 warnings。
- Contracts、Placement、Settings、Network、Plugin、Release、ClientUi 7 个测试程序：全部 PASS，返回码 0。
- `Verify-NoUiTokens.ps1`：ClientUi 8 files PASS；Contracts 2 files PASS；Core 10 files PASS。
- 既有 Release `Qualification.cs:BepInEx` 文本命中仍是既有扫描债务，不由 DEV-15B 引入。
- Gemini 报告中的 5 个文件哈希与工作区实算完全一致。

## 关键复核

1. `ItemAssetIdentity` 包含 `ItemId`、`AssetGuid`、`ResourcePath`，从 `InventoryPreviewInput` 传至 `PreviewIcon`，再由 `InventoryPreviewVisualSink.ShowIcon` 写入 `BoundAsset`；隐藏/卸载时清空。
2. `IInventorySurfaceContext` 提供 `CurrentContainer`、网格/顶层容器、Viewport、CellPixelSize、UiScale、滚动和 Occupancy；`TryCreatePreviewInput` 统一消费这些值。
3. `OnDragUpdated` 在 Presenter 前检查 `ContainerKind`、Page、`SessionGeneration`；失配时 fail-closed 隐藏图元。
4. 打开新 surface 会先卸载旧 Sink 并绑定新上下文；关闭/销毁会清理 surface、代际、Sink 和 drag 状态。
5. `PreviewFrame.CellPixelSize` 使用 `CellPixelSize * UiScale`，不存在固定 50 像素假设。

## 证据边界

当前仍只有纯 C# 静态/TDD 证据。尚未证明真实 Glazier/Sleek 适配器、Unity Hook、单人、SteamP2PFriends Host/Client、U3DS Headless 或发布资格。DEV-15C 的投影 Relay 仍未实现。

## 产物哈希

| 产物 | SHA-256 |
|---|---|
| `InventoryPreviewWiring.cs` | `E4EBCA58340F675575F27F9E4A2570DF51C3879B1EA83A4E6CEC8542E2122E26` |
| `ItemInteractionUiComponent.cs` | `BF50869DAE196D06233A8D5718D480404C91339577E9D1CD65841200BB80E954` |
| `Dev15BTests.cs` | `2E322D60CF0109D485D41ED2CA472B512FCE1DB85E271743DFAC8ABEF04F8EF6` |
| `BetterUnturnedExperience.ClientUi.dll` | `E0E5B558FAB161FF689604377AF66C277ECFA7E6A118AA99997047E449D0FE75` |
| `BetterUnturnedExperience.dll` | `A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16` |
