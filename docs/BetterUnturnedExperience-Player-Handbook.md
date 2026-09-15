# Better Unturned Experience · 玩家安装与升级手册（真机版）

> 适用版本：Phase-4 可视化体验正式交付版 + POST-P4 可见修复（`BetterUnturnedExperience.dll`，641536 字节，SHA-256 `af1f50c9…16d0`，完整值见 `audit/RELEASES.md` 候选台账行 13）。对外契约仍为 2.1。
> 本手册面向玩家；开发与验收证据见 `audit/RELEASES.md` 行 13 所绑归档（`audit/2026-09-13/POST-P4-CANDIDATE/`）。

## 1. 这是什么

**只装一个 DLL**，就能在单人 / Steam 好友联机（SteamP2PFriends）/ U3DS 独立服务器三种环境获得四个官方功能，无需再装独立 LMN 或任何功能分包：

| 功能 | 用法 |
|---|---|
| 更好的物品交互（BII） | 背包内直接拖拽物品，拖拽预览与保活由 BUE 接管 |
| 背包整理（LIT） | 背包页点「整理」按钮；Ctrl+点击 = 整理全身（不含仓储栏）。整理只有一种方法：物品按固定用途分段（远程武器、弹匣、医疗用品、近战武器、投掷物、弹药、弹药箱、枪械配件、食物、饮水、工具、服装与防护装备、背包与容器、燃料、补给与制作材料、建造、设施与陷阱、地图与指南针、钥匙、其他），各段按从左上到右下的顺序摆放，空位收在右下角。旧「同类/空间/大件」三模式已退役：旧存档里的这两项设置仍能读取并自动归一，不再决定整理结果；管理面板只保留「整理方向」（降序/升序），它只影响完全相同条件物品的收尾顺序 |
| 更好的换弹体验（LIR） | 持枪**双击换弹键**一键压弹（toast 显示压入发数）；整理后自动压弹 |
| 更好的尸潮播报（LHT） | 服务器管理员输入 `/horde` 启动尸潮；全体玩家 HUD 实时显示爆发地点与剩余数 |

## 2. 安装（全新玩家）

1. 安装 BepInEx 5.x 前置（Unturned 根目录 `BepInEx/` 结构就位）；
2. 把 `BetterUnturnedExperience.dll` 放进 `BepInEx/plugins/`；
3. 启动游戏即可。客户端与 U3DS 服务器**各放一份**（联机时两端都要装）；
4. 建议在 `BepInEx/config/BepInEx.cfg` 的 `[Logging.Disk]` 设 `LogLevels = All`——BUE 的诊断行多为 Debug 级，出问题时日志才可见。

启动日志（`BepInEx/LogOutput.log`）会出现身份锚行：`event=assembly-identity path=…\BetterUnturnedExperience.dll sha256=…`——这行绑定您正在运行的版本，报问题时请附上它。

## 3. 从旧部署升级（BUE + 独立 LMN + 三插件）

旧部署 = `BetterUnturnedExperience.dll` + `LaunchMultiplayerNet.dll` + 三个功能插件 DLL。升级到单 DLL：

1. **删除三个功能插件 DLL**（它们的全部能力已并入 BUE 单 DLL）；
2. **更新 `BetterUnturnedExperience.dll` 为当前候选**；
3. **独立 LMN（`LaunchMultiplayerNet.dll`）保留语义**：
   - 您还装着**依赖 LMN 数字频道的 V1 旧插件**（V1 时代的功能插件）→ **保留** `LaunchMultiplayerNet.dll`，BUE 会与它共存（BUE 放行 LMN 原生派发，实测回环正常）；
   - 您没有任何 V1 旧插件 → 可删可留；删掉即「裸 BUE」形态；
   - **裸 BUE（无独立 LMN）下，依赖 LMN 数字频道的 V1 旧插件不会收发**——这是已承认边界，不是缺陷；
4. 旧配置与存档类数据（如 LIT 统计）保留在磁盘持久化文件中，升级不清除。

## 4. 防双装与身份注意事项

- `plugins/` 里**只保留一份** `BetterUnturnedExperience.dll`。同名冲突副本的结局分两层：**标准 BepInEx 装载路径**下，Mono 运行时按程序集身份折叠，副本根本加载不进来（类型绑定始终由先装载的正牌副本承担）——这是引擎层的天然保护；**非标准装载路径**（绕过身份绑定的加载器、字节数组注入等）进来的副本，才会触发 `BUE double-install detected diagnosticId=BUE-PLATFORM-001` 的 Warning 行与管理面板底部的红色状态行，提示**移除非官方副本后重启游戏**。BUE 只提示、**永不删除或改动任何文件**；
- DLL **改名放进来也能运行**（BepInEx 按插件 GUID 识别，不认文件名——实机已验证），但身份行会显示改名后的路径；不建议改名，避免排查混乱；
- 不要把 BUE DLL 打包捆绑进其它插件的发布物。

## 5. 联机与服务器要点

- **SteamP2PFriends 联机**：主机与客机都装 BUE；会话自动建立，无需配置；
- **U3DS 独服**：`BepInEx/plugins/` 放同一份 DLL 即可，无头模式自动完成启动链（含被游戏清扫后的自愈重挂）；`/horde` 仅管理员有回复——启动前在 `Config.json` 的 `Owner` 写入 SteamID 或控制台 `Admin <SteamID>` 授权；
- 无 GSLT 令牌的直连服务器（FakeIP 场景）已支持：客户端与服务器身份自动收敛，四功能照常。

## 6. 故障自查三步

1. 确认 `LogOutput.log` 有身份锚行且 sha256 与 RELEASES 台账一致（版本没装错）；
2. 确认 `LogLevels = All` 已开，把 `[BUE-` 开头的行连同身份行一起反馈；
3. 管理面板里看功能状态与「启用」开关（改完需点「保存配置」），确认是「功能关闭」还是「功能故障」。联机时 **BUE Network Module** 显示「可继续通信」、没有启用开关，是已知良性投影，不是功能挂了——跨端整理/压弹仍以业务锚为准。
