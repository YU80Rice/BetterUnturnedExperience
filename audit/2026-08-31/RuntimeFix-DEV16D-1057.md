# DEV-16D 缺陷修复审查阻断记录

## 接管标记

本项目历史 ClientUi 交接曾由 Gemini 负责；本轮由 GPT 接手诊断、TDD、实现与审查闭环。

## 真实证据

- 演示视频：`C:\Users\The New Age\Desktop\演示\QQ20260831-090656-HD.mp4`
- UMM 诊断包：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包\_20260831\_101537`
- 实际部署 DLL：`artifacts/DEV-16D-drag-preview-r44-20260830/BetterUnturnedExperience.dll`
- R44 SHA-256：`8744AA4AFAFE3C98E7F184BADE8567D438AA9958F5584CF7633DF8A69C0E6742`
- R44 来源提交：`1d7749f`

诊断包已证明拖拽链进入 `drag-started`、`preview-input-readout` 与 `preview-visible`，故本轮阻断不在插件加载或开关读取，而在原生库存层级、坐标域和图元挂载契约。

## 红测证据

命令：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' 'tests\BetterUnturnedExperience.Plugin.Tests\BetterUnturnedExperience.Plugin.Tests.csproj' /t:Build /p:Configuration=Release /v:minimal
& 'tests\BetterUnturnedExperience.Plugin.Tests\bin\Release\BetterUnturnedExperience.Plugin.Tests.exe' --dev16d-r44-red
```

结果：测试项目编译成功；红测退出码 `1`，失败断言为：

`R44 regression: live preview clip must match the native scroll viewport`

当前 `ResolveViewport(...)` 固定返回 `gridWidth * 50` / `gridHeight * 50` 的完整内容尺寸，未消费真实 `ISleekScrollView` viewport；同时生产轮询从外层 `SleekItems` 读取指针，而 U3-SDK 原生 `SleekItems.onClickedGrid` 从私有 `grid` 读取指针。该差异可导致滚动重复应用、鼠标落点错域和图标使用 grid-local 坐标却挂到顶层。

## 阻断清单

1. `InventorySurfaceLifecycleAdapter.ResolveViewport` 丢弃原生 viewport 尺寸与有效层级信息。
2. `InventoryDragPreviewAdapter.Poll` 未使用原生私有 `grid` 的归一化指针 seam。
3. `InventoryPreviewVisualSink.ShowIcon` 接收 grid-local 值，却将其直接写入顶层容器，且缺少原生尺寸/锚点语义。
4. 自动旋转与候选计算依赖上述错误坐标输入，不能单独宣称已生效。

本记录仅固定阻断与红测，未在此步骤交付 DLL 或宣称运行时修复完成。
