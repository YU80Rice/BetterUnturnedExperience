# DEV-16D-R13 审查阻断记录 R2

## 基准

- 审查基准：`8d093f2`（canonical `ItemJar` footprint occupancy increment）
- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-16D-R13-native-conformance-remediation.md`
- 代码曾由 Gemini 负责，现由 GPT 接手。
- 本文件仅记录当前审查阻断；本轮尚未生成新的候选 DLL。

## Standards / Spec 阻断

1. **双页面 live surface 路由**：`InventorySurfaceLifecycleAdapter` 与 `InventoryDragPreviewAdapter` 只保存一个活动页面/网格，不能在 Backpack 与 Storage/Trunk 两个同时存在的原生 `SleekItems` 之间按目标页面路由。
2. **旋转来源 footprint**：来源排除将可变 `ItemJar.rot` 与原始 `dragFromRot` 强绑定；原生旋转后会错误拒绝 snapshot，且没有冻结旋转前 footprint。
3. **页面 Pass-Through**：释放路径只门禁目标页，AREA/装备来源仍可能进入增强 `sendDragItem` 或地面提交分支。
4. **stale preview 清理**：occupancy 失效/拒绝时仅清除 occupancy 缓存，未同步清空 `LastPreview` 与候选提交状态。

## 必须执行的闭环

- 先为上述四项在公开 seam 增加红色回归测试并记录失败；
- 以最小实现修复并转绿；
- Release 构建、七项目测试、静态门禁、ABI/Headless 门禁；
- 以修复后的新基准派发全新的 Standards + Spec 双轴审查；
- 任一轴 FAIL 继续记录、修复、红绿测、全量验证和复审；
- 双轴 CLEAN 前不生成 CandidateBuild、CaseId 或正式实机 DLL。
