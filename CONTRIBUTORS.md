# Contributors

## 核心开发

- **[YU80Rice](https://github.com/YU80Rice)** — BUE 设计与实现者，仓库维护者。

## 第三方代码贡献者

- **[@35117](https://github.com/35117)** — [UnturnedPluginManager](https://github.com/35117/UnturnedPluginManager)（UPM）作者。
  BUE 的内置管理面板吸收了 UPM 的插件列表、设置展示、UI 重建检测与面板交互的实现思路与**代码成果**，并将其内化为 BUE 单一运行时 DLL 的自有面板。
  作者于 2026-08-27 明确许可本次借用；完整来源、采用提交（`9b75730`）与授权记录见 [docs/third-party/UnturnedPluginManager-attribution.md](docs/third-party/UnturnedPluginManager-attribution.md)。
  边界说明：BUE 运行时不依赖 `UnturnedPluginManager.dll`，也不复用其 BepInEx 插件身份。

> 说明：GitHub 的 Contributors 图表仅由 commit 作者自动统计。35117 的工作经由「借鉴吸收+作者授权」进入 BUE，未以本仓库 commit 形式存在，故不出现在自动图表中——本文件与 README 的致谢节即为正式的贡献认定。

## 鸣谢

- **Unturned / Smartly Dressed Games** — 游戏本体与运行时环境（本项目与其无关联，不含游戏资产）。
- **BepInEx 社区** — 插件框架（BepInEx 5.4.23.5）。
