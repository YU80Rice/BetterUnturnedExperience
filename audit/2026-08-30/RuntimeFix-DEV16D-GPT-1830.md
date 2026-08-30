# DEV-16D RuntimeFix：Hidden 诊断与坐标驱动修复

## 一、问题定位与修复策略

根因：此前将 `SleekItems.PositionOffset_*`（父容器局部坐标）直接当作屏幕 viewport 原点，导致真实指针转换后候选稳定落入 `OutsideGrid/Hidden`。本轮改为从当前 `SleekItems` 读取归一化光标，映射到同一网格局部像素域，并将 `UiScale` 一次性纳入该域。

同时加入 GPT 水印读数：`pointerScreen`、`uiScale`、`uiCoordinates`、`viewportOrigin`、`pointerGrid`、`grabOffset`、`placementReason`；日志按状态变化或 500ms 节流，避免拖拽热路径日志风暴。补充同帧去重、原生 delegate 对称恢复、ActiveAdapter 销毁清理及异步图标请求 token 隔离。

## 二、TDD 证据

- 红测：`BetterUnturnedExperience.Plugin.Tests.exe --dev16d-diagnostics-red` 在缺少 `event=preview-input-readout` 时失败。
- 绿测：同命令在加入结构化读数后通过。
- 全套测试：7 个 Release 测试程序全部 PASS。

## 三、编译与静态门禁

- Release solution build：0 errors / 0 warnings。
- `Verify-NoUiTokens.ps1`：Contracts 2 files PASS；Core 10 files PASS；ClientUi 11 files PASS。
- `git diff --check`：通过（仅换行符提示）。

## 四、当前候选 DLL

- 源路径：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- 大小：198,144 bytes
- SHA-256：`B0AF13F4E75074CB93376A6FBF3BA39586703800F0ACB05708E3CF685878144D`
- CandidateBuild/CaseId：本轮尚未授予；必须在真实客户端取得新日志后再绑定。

## 五、独立审查记录

- 首轮 Standards/Spec：FAIL，指出逐帧日志、坐标缩放域、生命周期恢复与异步图标代际问题。
- 已修复：日志节流、`UiScale` 同域换算、同帧去重、`AttachGrid/DetachGrid`、异步图标 token、静态 ActiveAdapter 清理。
- 复审仍有一项阻断：`ScrollPixelsX/Y` 目前仍为 `0f`，尚未从实时滚动容器读取；因此本轮不能宣称双轴 CLEAN，也不能把 DLL 归档为正式交付物。

## 六、下一步 QA

使用上述源 DLL 在单人环境重测，并提交包含 `event=preview-input-readout` 的新 UMM 包。重点确认：`state=Candidate` 或 `LocallyInvalid`、`event=preview-visible`、`pointerGrid` 位于目标网格范围内；同时在滚动/父容器偏移场景验证无错位。取得新运行证据并补齐滚动 seam 后，才能进入正式 CandidateBuild/CaseId 归档。
