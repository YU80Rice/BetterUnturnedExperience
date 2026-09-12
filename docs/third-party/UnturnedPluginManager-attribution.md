# UnturnedPluginManager 来源与贡献声明

## 来源

- 项目：`UnturnedPluginManager`
- 作者 GitHub 档案：[@35117](https://github.com/35117)
- 仓库：<https://github.com/35117/UnturnedPluginManager>
- 本地来源快照：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\UnturnedPluginManager`
- 采用时记录的提交：`9b75730`（`v26.8.11.3: 修复游戏内无法打开插件管理界面的问题`）
- 上游源码快照（逐字节一致，blob 哈希可对照上游 `9b75730` 核验）：[`UnturnedPluginManager-snapshot/`](UnturnedPluginManager-snapshot/)，以作者署名的 vendor 提交入本仓库历史，使其贡献出现在 GitHub Contributors 认定中
- 作者后续开发技能包（2026-09-11 归档，文档写到 v26.8.13.3，比本体快照新）：[`unturned-plugin-dev/`](unturned-plugin-dev/)
- 作者署名：`35117+Deepseek-v4-falsh-0731`

## 使用许可记录

2026-08-27，作者 `35117` 在项目协作聊天中针对“借用插件管理面板并用于 BUE 框架”明确回复“用吧用吧”。本记录依据人工开发者提供的原始对话内容建立，用于保存来源、许可事实与审计追溯。

该许可记录不替代作者未来可能补充的正式许可证；BUE 将持续保留作者、仓库、采用提交和致谢信息。

## BUE 中的使用方式

BUE 将吸收其插件列表、设置展示、UI 重建检测与面板交互经验，并将实现内化为 BUE 单一运行时 DLL 的自有管理面板。运行时不依赖 `UnturnedPluginManager.dll`，也不复用其 BepInEx 插件身份 `com.trae.pluginmanager`。

## 致谢

感谢 `35117`（[@35117](https://github.com/35117)）开源并许可 BUE 借用该插件管理面板的实现思路与代码成果。本贡献同步在仓库根 [CONTRIBUTORS.md](../../CONTRIBUTORS.md) 的「第三方代码贡献者」一节与 README「第三方项目致谢」中认定。
