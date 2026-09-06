> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-02: 插件多功能设置与特性开关 UI 生命周期管理

- Type: prototype
- Status: resolved
- Author: Gemini
- Blocked by: GPT-08, GPT-10, GPT-14

## Question

如何为《更好的未转变者体验》设计一套规范、易扩展、原生契合的配置设置界面（Settings UI），基于构建期 Settings Facet 与运行期 `FeatureSettingsSnapshot` 动态呈现多模块配置项，并通过 `UpdateModuleConfigCommand` 与 `ModuleConfigChangedEvent` 完成双向绑定？

## Answer

1. **双事实源绑定（对齐 GPT-10 & GPT-14）**：
   - 静态元数据：从构建期链接生成的 **Settings Facet** 读取 `SettingDescriptor` 集合（类型、默认值、边界、排序、分组）。
   - 动态实时状态：从 `SettingsRuntime` 派发的不可变 **`FeatureSettingsSnapshot`** 读取当前有效值、权限（`ClientLocal / ServerAuthoritative / ServerPolicyWithClientPreference`）与单调 `Revision`。
   - 彻底废除运行时反射 `Describe()`。
2. **唤出入口**：
   - 双入口机制：原生 Esc 暂停菜单 / 主菜单“选项”注入按钮 `[更好的UN体验]` + 全局快捷键（默认 `F8`）。
   - 仅在非 BatchMode（客户端环境）下加载与实例化。
3. **交互防抖与提交**：
   - 控件在拖拽时本地即时平滑呈现，仅在鼠标松开或静止 150ms 后向 `SettingsRuntime` 提交 `UpdateModuleConfigCommand` 原子事务。
   - 3 秒重试、8 秒快照兜底（`0x0104`），收到 changed/rejected 事件时校验 Revision 刷新 UI。
4. **服务器政策视觉呈现**：
   - 针对 `ServerPolicyWithClientPreference`，保留玩家本地偏好勾选项，同时展示微型“服务器政策锁定”徽标与友好提示，不静默改写本地持久化。

*（注：本票据设计在 Wayfinder 阶段已闭环；生产代码实现与真实 UI 验证有待开发阶段推进）*

