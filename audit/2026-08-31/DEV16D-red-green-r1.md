# DEV-16D R1 红→绿证据（GPT）

## 红测（修复前）

基准：`fc3768d`，仅加入回归断言，未修改生产代码。

命令：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' 'tests\BetterUnturnedExperience.Plugin.Tests\BetterUnturnedExperience.Plugin.Tests.csproj' /t:Build /p:Configuration=Release /v:minimal
& 'tests\BetterUnturnedExperience.Plugin.Tests\bin\Release\BetterUnturnedExperience.Plugin.Tests.exe'
```

结果：测试项目编译成功，默认插件回归入口退出码 `1`，失败断言：

`inventory surface exposes a dynamic viewport seam`

同一轮 ClientUi 红测在旧实现上退出码 `1`，失败断言：

`invalid ordinary candidate leaves native drag live for swap/pass-through decision`

这两个失败分别锁定动态 viewport 未接线和无效占用释放提前停止原生拖拽的症状；地面来源专用 seam 在生产接口尚不存在时同样被断言捕获。

## 绿测（修复后）

修复内容：动态 viewport 每次读取当前原生滚动；预览反射/层级异常统一 `IsolatePreviewFailure + HidePreview`；地面来源走 `ItemManager.takeItem` 端口；占用交换在 `isDragging` 仍有效时回退原生。

验证结果：

- `BetterUnturnedExperience.Plugin.Tests.exe`：`DEV-14/DEV-16B plugin runtime tests: PASS`，退出码 `0`。
- `BetterUnturnedExperience.ClientUi.Tests.exe`：`DEV-05/DEV-15A/DEV-15B/DEV-15C/DEV-15D/DEV-16B ClientUi tests: PASS`，退出码 `0`。

本文件仅记录本轮 TDD 证据；正式交付仍须完成 Release 全量验证与新的 Standards/Spec 双轴 CLEAN。
