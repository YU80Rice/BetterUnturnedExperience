# GPT 水印：DEV-16D 增强预览最小修复

日期：2026-08-30

## TDD 链

- 红测快照：`tests/BetterUnturnedExperience.Plugin.Tests/Fixtures/dev16d-r36-no-preview.log`。
- 红测命令：`BetterUnturnedExperience.Plugin.Tests.exe --dev16d-red`。
- 红测结果：FAIL，`drag start must reach a visible red/green preview projection`。
- 最小修复：在 `BetterUnturnedExperiencePlugin.OnPluginUpdateTick` 通过插件自身主线程 Update 驱动 `InventoryDragPreviewAdapter.Tick()`；Harmony `updateDraggedItem` 仅作为快速路径保留。
- 可见投影日志增加明确 `GPT-WATERMARK event=preview-visible` 标记。
- 绿测快照：`tests/BetterUnturnedExperience.Plugin.Tests/Fixtures/dev16d-fixed-preview.log`。
- 绿测结果：插件运行测试 PASS；修复后的回归契约通过。

## 修改范围

- `src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs`
- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`
- `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`
- `tests/BetterUnturnedExperience.Plugin.Tests/BetterUnturnedExperience.Plugin.Tests.csproj`
- GPT 水印快照夹具：`tests/BetterUnturnedExperience.Plugin.Tests/Fixtures/*`

## 构建与 DLL

Release 构建：0 errors / 0 warnings。

当前 DLL：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`

SHA-256：`D1F77B9976DC2284836E6352059C0AF3BBB24502A9500C5E5A2292C087401EAD`

## 交付边界

本轮修复证明了可靠的插件 Update 驱动 seam，并将“无预览”锁定为回归测试；尚未完成真实游戏环境的新 DLL 冒烟验证，因此 DEV-16D 不能据此关闭。部署测试必须使用上述新哈希对应的 DLL，并生成新的 Case ID 证据。

## 审查状态

生产代码已完成最小修复和本地 TDD 验证；真实客户端、车辆后备箱、普通容器、真实物品纹理刷新仍需人工验证。此前 DEV-16D 尚有投影收敛和真实图标路径的独立审查风险，不能用本轮 Update 驱动修复替代。
