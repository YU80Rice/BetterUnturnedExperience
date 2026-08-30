# DEV-16D RuntimeFix 1915

## 结果

完成坐标诊断和滚动 seam 初步接线；Release 构建、全套测试和静态门禁通过，但最终独立审查仍为 `FAIL`，本文件不是正式交付报告。

## 根因与修复

- 原实现把 `SleekItems.PositionOffset_*` 当成屏幕 viewport 原点，导致候选持续 `OutsideGrid → Hidden`。
- 当前从 live `SleekItems` 读取归一化光标，转换为统一的 `CellPixelSize * UiScale` 本地像素域。
- `ScrollPixelsY` 从私有 `ISleekScrollView` 实时读取并做异常隔离；支持页面显式提供 `ScrollPixelsX` seam，因 `SleekItems` 内容宽度与视口一致而 fail-closed 为 0。
- 浮动图标挂到 `PlayerUI.container` 顶层，避免网格视口裁剪。
- 保留 GPT 水印坐标诊断，并按状态变化/500ms 节流；同帧驱动去重；delegate、ActiveAdapter 和异步图标请求均有生命周期清理/代际保护。

## TDD

- 红测：缺少 `preview-input-readout` 时失败。
- 绿测：结构化坐标读数测试通过。
- 新增滚动 seam 红测：缺少 `ReadScrollPixelsY` 时失败；实现后通过。

## 验证

- Release solution：0 errors / 0 warnings。
- 7 个测试程序：全部 PASS。
- `Verify-NoUiTokens.ps1`：Contracts/Core/ClientUi 全部 PASS。
- `git diff --check`：通过。

## 审查

- Standards/Spec 审查仍发现阻断：`SleekItems` 内部 `itemsPanel/grid/horizontalScrollView` 尚未按真实父级拆出；当前 viewport/scroll 尺寸为推导值，缺少真实滚动行为测试和运行证据。
- 因此不能授予 CandidateBuild/CaseId，也不能让用户进行实机复测。

## 产物

- 文件：[BetterUnturnedExperience-DEV16D-1915.dll](./artifacts/BetterUnturnedExperience-DEV16D-1915.dll)
- 大小：198,656 bytes
- SHA-256：`9F93F3824CB1DCB1EA0220B51A35556506E85C5BD87DB3BA0A1DE5EF5D855C2E`
- CandidateBuild：`DEV16D-GPT-20260830-1915`
- CaseId：`DEV16D-20260830-51FBAFE`

## 实机复测

当前不可开始实机复测。下一轮必须先完成真实 `itemsPanel`/顶层挂载和 viewport/scroll 行为测试，再重新执行双轴审查。
