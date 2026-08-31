# DEV-16D RuntimeFix - late surface drag rearm and native scroll domain

代码曾由 Gemini 负责，现由 GPT 接手。

## 一、问题定位与修复策略

本轮修复针对 DEV-16D 的两个可复现边界：

1. 原生网格指针已经处于 `GridContentLocal`（已包含滚动）的坐标域时，候选计算再次加滚动，造成落点偏移。
2. `updateDraggedItem` 先于 `PlayerUI.Update` 生命周期轮询到达时，surface 尚未发布；若该帧被提交，后续同帧无法重试。surface 到达后 `OnInventoryOpened` 又会无条件结束 Presenter 拖拽，导致后续更新始终无法显示预览。

最小修复：

- `InventoryPreviewWiring.TryCreateCandidateInput` 按 `PointerCoordinateSpace` 选择滚动量；`GridContentLocal` 不再二次加滚动，Screen/Viewport 路径保持一次滚动补偿。
- `InventoryDragPreviewAdapter.ShouldCommitPollFrame` 在“拖拽中且 surface 未就绪”时不推进 `lastPollFrame`，保留后续生命周期回调的重试机会。
- `BetterItemInteractionUiComponent` 保存当前拖拽代际，在 surface 绑定完成后重新 `BeginDrag`；关闭、释放、取消路径统一清零代际。未改变原生库存提交、投影收敛、Headless 分流或 ABI。

## 二、源码溯源清单

| 需求/根因 | 实现与测试 |
| :--- | :--- |
| 已滚动网格内容坐标不重复补偿 | `src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs:227-237`; `AssertGridContentPointerDoesNotDoubleApplyScroll` |
| surface 未就绪时保留同帧重试 | `src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs:413-436`; `AssertDragTickDefersFrameCommitUntilSurfaceReady` |
| surface 晚到不结束活动拖拽 | `src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs:307-325,333-346,370-380,417-482`; `BetterItemInteractionUiComponentRearmsDragWhenSurfaceArrivesAfterDragStart` |
| 原生回退/代际清理 | `OnInventoryClosed`, `OnDragReleased`, `OnDragCancelled`; ClientUi lifecycle suite |

## 三、TDD 红绿证据

- 红测：`audit/2026-09-01/DEV16D-r12-red.log`。修复前 R12 测试命中断言 `surface arrival preserves the active presenter drag and renders a preview`，原因是 `OnInventoryOpened` 的无条件清理结束了活动 Presenter 拖拽。
- 既有 R10/R11 红测：分别覆盖 GridContentLocal 二次滚动与 surface 未就绪帧提交；修复后已纳入默认 Plugin 测试套件。
- 绿测：`audit/2026-09-01/tests-dev16d-r12.log`，7 个测试项目全部退出码 0，全部 PASS。

## 四、编译与静态门禁

- Release 解决方案构建：`audit/2026-09-01/build-dev16d-r12.log`；MSBuild 18.9.1，`Configuration=Release`，源码 DLL 哈希前后保持一致，构建输出无错误/警告。
- 全套测试：`audit/2026-09-01/tests-dev16d-r12.log`；ClientUi、Contracts、Network、Placement、Plugin、Release、Settings 全部 PASS。
- UI/native token 门禁与 `git diff --check`：`audit/2026-09-01/static-gates-dev16d-r12.log`；ClientUi 11、Contracts 2、Core 10 个 C# 文件均 PASS。

## 五、双轴独立审查

冻结基准：`43d05ef91bf61f87dcba38774f78dcc9b4b60bc3`；本轮提交：`b698562f5d3fb4cff56b1eecd665b28a4548269f`。

| 审查轮次 | Standards | Spec | 结论 |
| :--- | :--- | :--- | :--- |
| R12 | CLEAN | CLEAN | 无阻断项，可归档候选 DLL |

Standards 审查确认无硬性规范违规、无阻断级代码气味；Spec 审查确认滚动域、按帧重试、拖拽代际重臂均符合 DEV-16D 与 `spec-DEV-16-runtime-clientui-management-panel.md`。审查期间工作区无漂移，审查完成后子智能体已停止。

非阻断建议：`ShouldCommitPollFrame` 将来可改名为 `ShouldAdvanceLastPollFrame`；诊断读数 helper 可与候选计算共享同一坐标域策略。两项不影响本轮交付。

## 六、正式产物与身份

- 源 DLL：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- 归档 DLL：`audit/2026-09-01/artifacts/BetterUnturnedExperience.dll`
- 候选 staging：`artifacts/DEV-16D-drag-preview-r12-20260901/BetterUnturnedExperience.dll`
- 文件大小：`222720` bytes
- SHA-256：`142AC39F769EF23EE75406F3DE84F21B59597B8F1BA72191650B128CF624C5E4`
- CandidateBuild：`DEV-16D-R12-CLEAN-20260901`
- CaseId：`DEV-16D-R12-20260901`
- SourceSnapshotId：`b698562f5d3fb4cff56b1eecd665b28a4548269f`
- DefinitionSetDigest：`A6351887957F7D463521BD4355899DDBE62409E9B2DE15A2FCF52207C8B492F1`
- ArtifactPayloadDigest：`142AC39F769EF23EE75406F3DE84F21B59597B8F1BA72191650B128CF624C5E4`
- BuildIdentity：`A5173A9766780D84FDB23052E2A4EA13CD80AB18F5398E21E9F08DAFBD7CFE16`
- ToolchainIdentity：`MSBuild-18.9.1+a81b43525|.NETFramework-4.7.2|CSharp-10`
- ClientReferenceSetId / U3dsReferenceSetId：`Libs-ReferenceSet-CA9AFA1D47CFE1DCA74431CCA755366F8BA51F0401BB79DD7058AE1DED3C6A75`

上述 CandidateBuild/CaseId 仅绑定本轮双轴 CLEAN 的静态候选产物；单人、SteamP2PFriends Host/Client、U3DS Headless 的真实运行资格仍需用同一 DLL 哈希另行采集，不能由本轮静态证据代替。

## 七、部署与复测

1. 完全退出 Unturned，将唯一文件 `audit/2026-09-01/artifacts/BetterUnturnedExperience.dll` 复制到游戏目录 `BepInEx/plugins/BetterUnturnedExperience.dll`，替换旧 BUE DLL。
2. 不要部署 `BetterUnturnedExperience.ClientUi.dll`、`Contracts.dll`、`Core.dll` 或 NoOpFixture；本轮玩家侧仍是单 DLL。
3. 用以下命令核对部署文件：

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath "E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll"
```

应得到：`142AC39F769EF23EE75406F3DE84F21B59597B8F1BA72191650B128CF624C5E4`。
4. 启动单人客户端，启用 Better Item Interaction，依次验证玩家背包、普通容器和车辆后备箱：指针在网格内移动时应出现绿色/红色占据框，浮动物品图标应跟随抓取点；旋转开关打开时按原生顺时针 90°候选尝试；关闭功能时应立即回到原生拖拽。
5. 复测结束后导出新的 UMM 诊断包，并保留 `BepInEx/LogOutput.log`；运行证据必须标注 CandidateBuild `DEV-16D-R12-CLEAN-20260901` 与 CaseId `DEV-16D-R12-20260901`。

## 八、最终结论

DEV-16D 本轮红测→绿测、Release 构建、7 项测试、静态门禁和 Standards/Spec 双轴审查均已 CLEAN。**现在可以开始实机复测。**
