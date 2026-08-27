# 02：DEV-16B BUE 内置插件管理面板与设置编辑

**What to build:** 让玩家从主菜单和游戏内暂停菜单打开 BUE 自有管理面板，查看 BUE 功能与所有已加载 BepInEx 插件，使用收藏/A-Z/Z-A 排序，并安全编辑 BUE 设置和普通插件的基础配置。

**Blocked by:** 01：DEV-16A 单 DLL Runtime Composition Root 与 Client/Headless 装配

**Status:** ready-for-human

> 实施接管：原由 Gemini 负责的 ClientUi 代码现由 GPT 接手维护。本票已完成管理面板模型、设置编辑、持久化与生命周期 Seam；真实 Glazier/Sleek 控件注入仍待后续运行时接线票验证。

- [ ] 主菜单和暂停菜单均出现“BUE 插件管理”入口，UI 树重建后能安全重挂载。
- [ ] 面板只展示 BepInEx Chainloader 已成功加载的插件，不扫描或主动加载任意 DLL。
- [ ] BUE 功能条目显示 FeatureId、名称、版本、FeatureState、PresentationState 和 Settings Facet；普通插件条目显示 GUID、名称、版本和公开配置信息。
- [ ] 收藏使用稳定 FeatureId/GUID；收藏优先，收藏内部按加入顺序，未收藏条目按 A→Z 或 Z→A 排列。
- [ ] 排序偏好、收藏列表和收藏顺序跨重启持久化；取消后重新收藏进入序列末尾。
- [ ] Better Item Interaction 的增强交互开关和自动旋转可见、可编辑，并由 SettingsRuntime 保持唯一权威。
- [ ] 普通 BepInEx 插件的 bool、数字和字符串 ConfigEntry 可安全编辑；不支持类型只读，需重启的变更明确提示。
- [ ] 不提供运行时强制启用、禁用、卸载或热重载；检测外部 UnturnedPluginManager 时只显示兼容提示。
- [ ] 面板和仓库保留 `35117+Deepseek-v4-falsh-0731`、来源仓库、提交 `9b75730`、许可记录和致谢。
- [x] Release 编译、排序/持久化/配置编辑测试、UI 重建测试、静态门禁和独立审计通过（真实 UI 运行仍待人工验证）。
