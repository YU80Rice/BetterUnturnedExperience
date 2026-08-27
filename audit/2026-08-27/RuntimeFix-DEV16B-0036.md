# 🛠️ DEV-16B 运行时修复执行报告 - 2026-08-27

### 一、问题定位与修复策略

- **根因**：BUE 读取了 `MenuDashboardUI.container`，而用户实际可见且 `UnturnedPluginManager` 已验证可工作的入口页面是 `MenuWorkshopUI.container`；原 BUE 构造函数 Postfix 为空，未执行构造后注入。
- **修复策略**：改接 `MenuWorkshopUI`；构造完成后通过 Harmony Postfix 立即注入；保留 Update 轮询；补齐 ESC/closeAll 生命周期补丁；失败时清理按钮和失效容器。

### 二、代码变更

- `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs`
  - `MenuDashboardUI` → `MenuWorkshopUI`。
  - Postfix 调用活动实例 `Tick(TickSource.Harmony)`。
  - 增加 `MenuUI.escapeMenu`、`PlayerUI.escapeMenu`、`MenuUI.closeAll` 补丁。
  - 诊断日志统一包含 `plugin`、`featureId`、`version`、`environmentRole`、`scenario`、`diagnosticId`、`event`、`reasonCode`。
  - UI 创建失败时解绑事件、移除子节点并清空引用；销毁时清理已挂载按钮；失效容器不再复用。
- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`
  - Update 调用使用 `TickSource.Update`。

### 三、编译与测试

- Release 编译：`0 errors / 0 warnings`。
- 7 个测试程序全部 PASS：ClientUi、Contracts、Network、Placement、Plugin、Release、Settings。

### 四、独立审查记录

- **Spec 轴**：主菜单目标和构造后接线缺陷已修复；DEV-16B 其余管理面板交互功能不在本轮运行时接线修复中宣称完成。
- **Standards 轴**：诊断字段、失败清理、失效容器保护已修复；无新增阻断项。

### 五、部署产物

- `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- SHA-256：以构建后实际文件为准，部署前请重新计算并记录。

### 六、最终结论

- **状态**：`ready-for-human`
- **说明**：代码修复、编译、自动化测试和独立审查完成；真实游戏中按钮显示仍需人工部署验证，不能提前标记 DEV-16B `resolved`。
