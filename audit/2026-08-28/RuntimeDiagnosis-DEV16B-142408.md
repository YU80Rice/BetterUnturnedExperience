# DEV-16B 运行诊断：R9 仍无管理面板

日期：2026-08-28
诊断包：`UMM-诊断包_20260828_142408`
部署候选：`artifacts/DEV-16B-management-panel-runtime-fix-r9-20260828/BetterUnturnedExperience.dll`

## 结论

本次部署身份正确，BUE 已被 BepInEx 加载并完成客户端初始化；但 `Awake` 完成后的持续运行链没有被真实宿主触发。故障发生在按钮注入之前，不是按钮位置、文本、`CreateButton` 返回值或 `AddChild` 挂载失败。

## 证据

`LogOutput.log` 中程序集身份为：

- 路径：`E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll`
- SHA-256：`0C15885698D0762491B9C39C8E5578B5C50EBECFD71C2EC7200AA375B8DA7A9C`

已出现：

- `runtime-gate decision=Client`
- `constructed dashboardField=True workshopField=True pauseField=True`
- 11 个 Harmony `patch-installed`
- `initialize-complete`
- `first-tick source=Initialize`
- `runtime-pump-created`
- `BootstrapReady`

初始化阶段两个容器均为 `null` 且 `active=False`，这是启动时容器尚未建立的正常状态。

未出现：

| 事件 | 次数 |
| :--- | ---: |
| `plugin-update` | 0 |
| `runtime-pump-tick` | 0 |
| `host-ui-tick` | 0 |
| `surface-opened` | 0 |
| `create-button-begin` | 0 |
| `create-button-result` | 0 |
| `add-child-success` | 0 |
| `entry-failed` | 0 |
| `RuntimeReady` | 0 |

`Client.log` 证明游戏随后确实进入主菜单、加载 PEI，并在 06:24:01 从游戏内暂停菜单退出；因此不是 BUE 启动后游戏立即崩溃或进程提前结束。

截图与日志一致：截图中的 Workshop 页面和游戏内暂停/设置页面均保持原生界面，没有 BUE 入口。

## 对照结论

`UnturnedPluginManager` 的可靠路径是 `PluginManagerPlugin.Update()` 每帧直接调用 `PluginManagerUI.Tick()`，并以 Harmony 构造钩子作快速路径。它没有依赖另一个 `GameObject` 的 `MonoBehaviour.Update()`。

BUE R9 虽然已实现插件 `Update`、独立 RuntimePump 和原生 UI Harmony 更新钩子，但本包证明三条运行路径都没有产生任何运行事件。Harmony 的 `patch-installed` 只证明补丁登记成功，不能证明回调实际执行。

## 排名假设（均待下一轮单变量验证）

1. **BepInEx/Unity 没有调度 BUE 的插件 `Update`**：若把运行驱动改为与已知工作的 UPM 同形态，并在进入 `Tick` 前记录硬证据，预期首先出现 `plugin-update`。
2. **独立 RuntimePump 的 `MonoBehaviour.Update` 未被调度**：若仅保留插件自身 Update 并移除/旁路独立泵，预期 `plugin-update` 出现而 `runtime-pump-tick` 仍缺失。
3. **手动 Harmony 补丁登记成功但回调未实际命中**：若改为可验证的固定补丁入口并记录回调，预期出现 `surface-opened` 或 `host-ui-tick`；当前无证据支持进入 `CreateButton`。
4. **BUE 宿主对象在场景切换期间被销毁或禁用**：若为 `OnDestroy`、`OnDisable`、`activeInHierarchy` 和 `enabled` 增加一次性诊断，预期能看到生命周期终止证据；本包目前未出现这些事件。

## 当前判定

`DEV-16B` 继续保持 `ready-for-human`。本次真实运行未通过按钮可见性门禁；在获得新的运行链证据前，不应标记 `resolved`。本诊断未修改生产代码。
