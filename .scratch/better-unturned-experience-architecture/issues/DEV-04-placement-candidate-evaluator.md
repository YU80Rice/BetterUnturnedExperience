# DEV-04：PlacementCandidateEvaluator + Local-Fit Priority

**Owner:** GPT   
**Required reviewer:** Gemini  
**Status:** ready-for-agent  
**Baseline:** `BUE-V1-RT01-20260824`  
**SourceSet:** `BUE-SS-20260824-02`  
**Depends on:** DEV-01 (`resolved`), DEV-02 (`resolved`), DEV-03 (Gemini `ACCEPT`; GPT audit closure pending), RT-06 (`resolved`)

## Scope

实现无 Unity/原生副作用的纯 C# `IPlacementCandidateEvaluator`：几何中心投影、边缘约束、Local-Fit Priority 四级阶梯、自动旋转、确定性扩展排序和完备失败预览。

## Acceptance

- [ ] 光标中心越界返回 `Hidden/OutsideGrid`，不产生候选。
- [ ] 当前朝向局部合法时立即返回 current-local，绝不竞争旋转方向。
- [ ] 当前朝向局部受阻且旋转局部合法时返回 automatic-90-local。
- [ ] 局部均失败时按 current-expanded → automatic-90-expanded 搜索；距离平方、Y、X 排序确定性。
- [ ] 自动旋转关闭或正方形物品不检查旋转。
- [ ] 边缘投影正确 clamp；超尺寸方向不执行负上界 clamp。
- [ ] `Candidate / Width / Height / Rotation / Reason` 始终完整；失败只返回本地预览，不修改库存。
- [ ] `Evaluate` 不使用 LINQ、集合、闭包、字符串或临时堆对象；Release 热路径分配增量为 0 的测试义务单独记录。
- [ ] 覆盖 1x1、1x4、1x5、2x3、3x3、拥挤、狭缝旋转、边界和全满网格。
- [ ] Contracts/Core 无 UI/native 类型泄漏；Release 0 errors / 0 warnings；独立审计 PASS 后再交 Gemini。

## Non-goals

不实现 Glazier/ClientUi、原生 `sendDragItem` adapter、库存权威、网络消息、LMN、SP/P2P/U3DS 运行验收或自动交换/重排。

## Agreed seam under test

`IPlacementCandidateEvaluator.Evaluate(PlacementCandidateInput)`。

## Execution record
- TDD RED→GREEN complete.
- Release build/tests/token scan PASS.
- Independent audit request pending; no resolved claim.
