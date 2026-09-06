# DEV-16B 管理面板运行时注入诊断报告

日期：2026-08-28
诊断包：`UMM-诊断包_20260828_122449`
审计基准：`e883f04`（R8）
部署 DLL SHA-256：`9FFCFE53497023872D2EB132BAA037FD891629C77585A694F3BC8A111E21E4AA`

## 一、用户症状

主菜单、创意工坊页面和游戏内暂停菜单均未出现 BUE 管理面板入口；同时没有按钮注入日志。

## 二、反馈回路

已执行确定性日志门禁：检查 `LogOutput.log` 是否同时包含运行泵、宿主 UI 和按钮注入事件。

结果：

| 事件 | 结果 |
|---|---|
| `runtime-gate` | 存在 |
| `runtime-pump-created` | 存在 |
| `plugin-update` | 缺失 |
| `runtime-pump-tick` | 缺失 |
| `host-ui-tick` | 缺失 |
| `surface-opened` | 缺失 |
| `create-button-begin` | 缺失 |
| `add-child-success` | 缺失 |

因此该回路对本次症状为 RED，并且故障发生在 `CreateButton` 之前。

## 三、根因判断

### 结论

当前最有证据支持的根因是：**BUE 的运行期驱动没有被真实 Unity 宿主执行**。主 DLL 的 `Awake`、Harmony 登记和运行泵创建已经发生，但 BUE 自身 `Update`、独立 `BueRuntimePumpBehaviour.Update` 以及 UI Harmony 回调均未发生，所以没有机会读取非空 UI 容器，也没有机会调用 `Glazier.Get().CreateButton()`。

这不是 `CreateButton` 返回空、按钮坐标错误或面板渲染文本错误；这些路径在本次运行中尚未被触及。

### 与 UnturnedPluginManager 的关键差异

`D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/UnturnedPluginManager/PluginManagerMod.cs` 的可运行主路径是：

1. `PluginManagerPlugin` 是公开的 `BaseUnityPlugin`；
2. `PluginManagerPlugin.Update()` 每帧直接调用 `PluginManagerUI.Tick()`；
3. `PluginManagerUI.Tick()` 每帧轮询 `TryAddEntryButton()` 和 `TryAddPauseButton()`；
4. Harmony 构造函数补丁只是快速路径，轮询仍是兜底路径。

BUE 虽然实现了相同意图，但 R8 的真实日志证明两条兜底路径都没有被驱动：插件 `Update` 没有日志，独立泵也没有首 tick。故不能把“`runtime-pump-created`”误当成“泵正在运行”。

## 四、排序后的可证伪假设

1. **独立泵的 Unity 消息没有被识别/调度**：若将泵宿主改为与已知工作的公开插件入口同形态，并保留首 tick 日志，则应出现 `runtime-pump-tick`。
2. **BUE 插件组件本身被禁用或未进入 Unity Update**：若在 `Awake` 记录 `enabled`、`gameObject.activeInHierarchy` 并以已知工作插件方式维持宿主，则应出现 `plugin-update`。
3. **显式 Harmony 回调签名/回调类型在该宿主中未执行**：若只保留与 UPM 相同的公开静态构造函数补丁，并在补丁体内记录日志，应出现 `constructor-postfix`。
4. **UI 容器只在后续页面构建后出现**：若运行期驱动恢复，即使容器初始为 null，后续至少应出现 `container-state ... active=True` 或 `surface-opened`；当前完全没有此类事件。

## 五、审计结论

- **静态实现：部分通过**：目标类型、字段和按钮注入代码存在；R8 还具备泵创建失败回滚。
- **与 UPM 的注入逻辑对照：未通过**：UPM 的可靠主路径是公开插件 `Update` 驱动的每帧轮询；BUE 当前无法证明任何运行期驱动有效。
- **真实运行：FAIL**：缺少 `runtime-pump-tick`/`plugin-update`/按钮成功事件。
- **本轮未修改生产代码**：本次请求为诊断与对照审查，未执行修复。

## 六、下一步修复边界

修复应优先把 BUE 的运行期驱动收敛到一个已被 UPM 证明可工作的主线程入口，并保留一个可验证的冗余路径；首个回归门禁必须是 `plugin-update` 或 `runtime-pump-tick` 出现，随后才审查容器读取和 `add-child-success`。在获得新的运行证据前，DEV-16B 继续保持 `ready-for-human`，不能关闭。

## 七、独立代码审查补充

### Standards

判定：FAIL。

- `OnRuntimePumpTick` 进入隔离后，`BetterUnturnedExperiencePlugin.Update` 与 `BueNativeManagementPanel` 的 Harmony 回调仍可能直接调用 `Tick`；异常隔离没有统一闭合入口。涉及 `BetterUnturnedExperiencePlugin.cs:102-135`、`BueNativeManagementPanel.cs:173-200`。
- 非阻断设计建议：插件入口同时承担启动、泵生命周期、诊断、RuntimeReady 和销毁；可抽取一个统一的 guarded tick seam。

### Spec

判定：FAIL。

- 工单要求收藏/A-Z/Z-A、BUE 设置编辑和普通插件基础配置编辑；当前 `BueNativeManagementPanel.Render`（`BueNativeManagementPanel.cs:442-469`）只写入文本，没有交互控件或提交链。
- 工单要求条目显示状态与公开设置数据；当前只显示名称、稳定 ID、版本及 ConfigEntry 数量，未逐项呈现状态、配置值、只读/需重启标记。
- 工单要求范围/长度校验；`LoadedPluginCatalogAdapter.cs:129-140` 仅提取 Min/Max，未覆盖 `AcceptableValues` 枚举集合约束。
- 非阻断设计建议：Workshop 子页额外入口超出主菜单/暂停菜单的最小要求；多条 Tick 来源存在重复调用，应明确节流或统一调度。
