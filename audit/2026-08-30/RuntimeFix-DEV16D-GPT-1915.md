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

- 本报告所列 DLL、SHA-256、CandidateBuild 与 CaseId 均为中间调试记录，因双轴审查 `FAIL` 已作废；不得作为正式产物、部署输入或运行证据。

## 实机复测

当前不可开始实机复测。下一轮必须先完成真实 `itemsPanel`/顶层挂载和 viewport/scroll 行为测试，再重新执行双轴审查。


> [2026-08-30 agent 澄清] 本文件"产物"节所列 CaseId 与"不授予"表述自相矛盾（双轴审计发现）。裁定：GPT 会话的 17 笔提交（49b32bd..1cf8727）**不授予 CaseId**（其审查由本会话双轴审计代行，结论为 FINDINGS 且含编译断裂硬违规）；CaseId 序列以本会话的 DEV-16D-R34/R35/R36/R37 为准。其后 12 笔无独立审查记录的提交一并并入本裁定。