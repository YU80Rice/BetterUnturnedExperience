# GPT 水印：DEV-16D 运行链修复 R2

## 一、问题定位

实机日志 `UMM-诊断包_20260830_171453` 已确认部署 DLL SHA-256 为 `D1F77B9976DC2284836E6352059C0AF3BBB24502A9500C5E5A2292C087401EAD`，但无 `plugin-update`、`runtime-pump-tick` 或 `GPT-WATERMARK event=preview-visible`。`PlayerUI.Update` 的库存轮询和 surface 分发却成功命中，拖拽开始后仍为 `preview-stale previewGen=0`。

## 二、修复

把 `InventoryDragPreviewAdapter.Tick()` 接入已被真实日志证明可靠的 `InventorySurfaceLifecycleAdapter.PlayerUIUpdatePostfix()`。插件自身 Update 继续保留，Harmony `updateDraggedItem` 继续作为辅助路径。这样预览 Poll 与原生库存 surface 在同一已命中的主线程心跳内执行。

## 三、验证

- Release 全解决方案：0 errors / 0 warnings。
- 七个测试项目：全部 PASS。
- `Verify-NoUiTokens.ps1`：Contracts 2、Core 10、ClientUi 11 文件全部 PASS。
- 当前 DLL SHA-256：`F1C123E21B748A51CE1AEE799436263482A84C1F626A7EA107F2F2C28435C84F`。

## 四、交付边界

尚未获得新 DLL 的真实客户端验证，因此不能宣称 DEV-16D 已关闭。人工测试必须部署本轮 DLL，并使用新 Case ID 采集日志，重点确认 `GPT-WATERMARK event=preview-visible`、Candidate/LocallyInvalid 状态和自动旋转读数。

## 五、独立审查

本轮已派发独立审查；审查结论返回前不视为正式发布闭环完成。
