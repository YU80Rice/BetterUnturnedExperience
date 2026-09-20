# BUE 内置单 DLL 管理面板与运行时接线

**Status: accepted**

## Context

BUE 的纯 C# Seam、官方功能注册和 ClientUi 测试组件已经完成，但当前主插件仍未接入真实 Unity/Glazier/Unturned 库存运行链。用户需要一个最终只部署 `BetterUnturnedExperience.dll` 的框架，同时能够统一管理 BUE 功能和已加载的 BepInEx 插件。

`35117/UnturnedPluginManager` 提供了可参考并获作者口头许可吸收的插件列表、设置编辑、入口注入和 UI 重建处理实现。其仓库采用提交为 `9b75730`，作者署名为 `35117+Deepseek-v4-falsh-0731`。

## Decision

1. BUE 将 PluginManager 的相关面板能力内化到自己的命名空间、生命周期和单一 BepInEx 入口中；发布物只交付 `BetterUnturnedExperience.dll`。
2. `UnturnedPluginManager.dll` 不作为运行时依赖；若检测到外部同类管理器，只显示兼容提示，不主动卸载或修改它。
3. 管理面板分层显示：
   - BUE 功能条目：使用 FeatureId、Settings Facet、FeatureState 和 PresentationState；
   - 普通 BepInEx 插件条目：使用 GUID、名称、版本、程序集和公开 ConfigEntry 信息。
4. 收藏使用稳定 FeatureId/GUID 作为键；收藏优先，内部按收藏加入顺序排列，未收藏条目按玩家选择的 A→Z 或 Z→A 排列。
5. BUE 自身设置由 `SettingsRuntime` 作为唯一权威源；普通 BepInEx 配置只通过公开 ConfigEntry/ConfigFile API 进行有限编辑。
6. 主菜单和游戏内暂停菜单均提供入口，并处理 Unturned UI 树重建后的安全重挂载。
7. 单 DLL 内可包含 UI 适配代码，但 U3DS Headless 必须在类型创建、UI 构造和客户端库存 Hook 前被门禁隔离。
8. Better Item Interaction 首期仍只增强受支持网格的拖入，最终提交继续走原生 `sendDragItem`，失败时保持原生回退。
9. 允许编辑的普通配置限于 bool、数字和字符串；不支持类型只读，需重启的配置明确提示，不执行热卸载或任意代码。

## Attribution and permission record

BUE 仓库必须保留作者、仓库、采用提交和致谢信息，详见 `docs/third-party/UnturnedPluginManager-attribution.md`。该文件记录了 2026-08-27 作者 `35117` 对 BUE 借用插件管理面板的聊天许可。若作者后续提供正式许可证，以正式许可证补充或替换口头许可记录。

## Consequences

- 正面：玩家只需部署一个 BUE DLL；官方功能与第三方功能使用同一管理体验；后续功能可复用统一面板和设置契约。
- 代价：BUE 必须维护 Unity/Glazier/Unturned 版本适配、UI 重建、ConfigEntry 边界和 Headless 隔离；吸收代码时必须持续保留来源与贡献记录。
- 门禁：DEV-16A～DEV-16E 必须分别通过编译、独立审计、Gemini 前端复核和真实环境验证；新 DLL 产生后，旧 CandidateBuild 与运行证据全部失效。
- 澄清（V6-T1，2026-09-17）：Decision §1 锁的是玩家侧发布物为一个 `BetterUnturnedExperience.dll`（发布面），不锁编译期必须为单程序集（构建面）。构建期真分工程、发布期合并回单 DLL 不构成推翻本 ADR；合并工具由第六阶段工程结构票裁。
