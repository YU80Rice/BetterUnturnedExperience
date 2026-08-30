# GPT 水印：DEV-16D 预览 Hidden 状态诊断

## 结论

本次部署 DLL 身份正确：

`C65E23C4B24824851EBBD97537ABC51E1C426207F8CFEF106F4457189BEC13BA`

日志证明预览链已进入 `InventoryPreviewPresenter.Update`：出现大量 `GPT-WATERMARK event=preview-evaluated generation=N state=Hidden`。因此本次失败不是功能开关、库存生命周期、Harmony/Update 驱动或 Sink 挂载未执行。

## 失败边界

```text
PlayerUI.Update
 → surface-context-dispatched
 → drag-started
 → TryCreatePreviewInput 成功
 → preview-evaluated state=Hidden
 → Sink.Hide()
```

没有出现 `preview-visible`，因为 `PlacementCandidateEvaluator` 收到的候选输入被判定为网格外（`PlacementPreviewState.Hidden` / `OutsideGrid`），而不是因为绿色/红色图元无法渲染。

## 关键风险

当前真实 surface 日志仍显示：`viewportOrigin=0,330`、`grid=5x7`、`clip=0x380`、`cellPx=50`。BUE 已修正鼠标 Y 轴和 UI Scale，但仍将 `SleekItems.PositionOffset_*` 直接当成屏幕坐标使用；U3-SDK 的 Sleek 元素坐标属于其父容器坐标系，且原生库存拖拽使用 `PlayerUI.container` 的 viewport/normalized 坐标。该坐标系边界尚未闭合，导致指针转换后可能始终落在 5×7 网格之外。

## 尚未宣称的内容

- 不能宣称真实红/绿预览已工作。
- 不能宣称自动旋转已工作；自动旋转仅在候选输入进入 evaluator 且当前方向无法放置时才会触发。
- 不能宣称真实物品图标刷新已工作；当前没有候选状态，因此图标分支不会执行。

## 下一轮最小动作

在不猜测渲染层的前提下，补充单条结构化读数：`pointerScreen`、转换后的 `pointerGrid`、`grabOffset`、`Viewport.Contains`、`candidateInput` 和 evaluator `Reason`。用该读数确定是坐标原点、Y 轴、UI Scale、网格宽高还是抓取偏移导致 `OutsideGrid`，再写针对性红测并修复。
