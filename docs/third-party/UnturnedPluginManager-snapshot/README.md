# PluginManager 插件管理（Unturned BepInEx）

作者：35117+Deepseek-v4-falsh-0731

适用于《Unturned（未转变者）》的 BepInEx 5 插件：在游戏界面内查看、管理全部已加载的 BepInEx 插件，并在线修改它们的配置，无需退出游戏或手改配置文件。

## 版本号规则

版本号格式为 `年.月.日.第几版`，例如 `26.8.11.2` 表示 2026 年 8 月 11 日当天上传的第 2 版。注意版本号必须为 4 段（BepInEx 通过 .NET `System.Version` 解析，最多支持 4 段）。

## 安装

1. 安装 [BepInEx 5](https://docs.bepinex.dev/)（x64 版本）到游戏根目录。
2. 从 [Release](https://github.com/35117/UnturnedPluginManager/releases) 下载 `PluginManagerMod-版本号.zip`，解压后把 `BepInEx` 文件夹覆盖到游戏根目录。
3. 启动游戏。

## 入口

- 主菜单 → 创意工坊：底部"插件管理"按钮（可在配置中改名/隐藏）
- 游戏内 → 暂停菜单：右上角"插件管理"按钮

打开时源界面滑出隐藏，关闭窗口或按 ESC 后恢复原界面（主菜单回创意工坊，游戏内回暂停菜单）。

## 功能

- **插件列表**：显示全部已加载插件（名称 / GUID / 版本 / 程序集 / 配置文件路径），按名称排序
- **配置编辑**：bool 用开关，其余类型用输入框；回车提交保存到 `BepInEx/config/*.cfg`，ESC 还原当前项；操作结果显示在窗口底部状态栏
- **列表型配置专用控件**：配置项定义时在 `ConfigDescription.Tags` 加标记即自动启用"+"选择器
  - `"Unturned.ItemList"`：物品 ID 列表（值格式 `物品ID, 物品ID, ...`）
  - `"Unturned.BlueprintList"`：配方列表（值格式 `所属物品ID:配方编号, ...`）
  - 选择器支持按名称 / ID / 描述搜索（忽略大小写、支持中文），点击条目自动去重排序写入配置并保存
- **列表可视化**：已添加的条目直接显示在配置项下方，每条含物品图标 + 名称 + 原始值
  - 配方条目逐成分显示公式：`[图标] 木材 x 2 + [图标] 石头 x 1 = [图标] 工具台 x 1`
  - 配方选择器自动跳过存在未知物品成分的配方
- **循环切换按钮**：配置项在 Tags 中加 `"Unturned.Cycle:选项1|选项2|..."` 标记后，渲染为循环切换按钮
  - 左键点击：下一档；右键点击：上一档（首尾循环）
  - 例：`"Unturned.Cycle:OFF|1|2|3|4"` 初始 `OFF`，左键依次 `1 → 2 → 3 → 4 → OFF`
  - Tag 不内嵌选项时自动使用配置项的 `AcceptableValueList<string>` 作为档位
- **条目删除**：每条最右侧红色 `X` 按钮可单独移除，自动保存并刷新
- **选择器条目**：同样带物品图标，配方条目显示公式预览

## 插件自身配置

`BepInEx/config/com.trae.pluginmanager.cfg`

| 节 | 键 | 说明 |
|----|----|----|
| General | Enabled | 是否显示入口按钮（默认 true） |
| General | ButtonText | 入口按钮文字（默认"插件管理"） |

## 稳健性

- 进出地图 / 返回主菜单后界面重建自动检测，按钮自动重新注入（Harmony 快速路径 + 每帧轮询兜底）
- ESC 按上下文拦截：主菜单 / 游戏内分别返回创意工坊 / 暂停菜单
- 开始游戏或退出地图时窗口自动关闭，不留残留

## 编译

环境要求：.NET Framework 4.x（BepInEx 5）、C# 5 语法、csc.exe。

运行 `build.bat`，输出 `BepInEx/Plugins/PluginManagerMod.dll`。

依赖（游戏 `Unturned_Data/Managed/` 下）：`Assembly-CSharp.dll`、`SDG.Glazier.Runtime.dll`、`UnityEngine.*.dll`、`netstandard.dll`、`UnturnedDat.dll`，以及 BepInEx 的 `BepInEx.dll`、`0Harmony.dll`。

## 已知问题

"无边框全屏"模式会触发游戏本身的画面黑屏 BUG，与插件无关；测试时建议使用窗口化或全屏模式。
