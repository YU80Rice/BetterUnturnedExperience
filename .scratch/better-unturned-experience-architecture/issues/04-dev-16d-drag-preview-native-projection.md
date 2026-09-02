# 04：DEV-16D 拖拽预览、真实图标、原生提交与投影收敛

**What to build:** 让玩家在受支持的网格拖入物品时看到绿色/红色占据框和真实物品图标，自动旋转与设置开关生效，合法操作沿原生库存权威链提交，并在原生投影到达后收敛。

**Blocked by:**

- 02：DEV-16B BUE 内置插件管理面板与设置编辑
- 03：DEV-16C 原生库存 UI 生命周期与容器上下文接线

**Status:** resolved

> 2026-09-02 关闭：DEV-16D 范围内全部实现轮（R12 + R13 remediation + R7-BAND 边缘感应带 + ROTGRAB 当前空间 grabOffset）已完成、实机验证通过、双轴审查 CLEAN。验收清单逐项核对如下（含边界注释）。DEV-16E 阻塞解除。

- [x] 玩家背包、普通容器和车辆后备箱之间，以及地面物品到这些网格的拖入路径进入增强预览。
      —— 背包(3)/Storage(7)/后备箱(7) 之间增强拖入已实现并经 R7 实机验证（surface dispatch page 3/7、绿色框/图标/提交/投影）。**边界注释**：从 AREA/装备页等"拿起源"的拖入按 remediation 规格保持原生 Pass-Through（spec-DEV-16D 第 17、89-94 行），非缺陷；"拿起源解耦 + 目标页扩展（VEST/SHIRT/PANTS）"已由用户拍板为三环境之后的新工单（见 DEV-16D-R13-R7 工单 2026-09-02 决议）。
- [x] 合法候选显示绿色占据框；局部无效候选显示红色占据框；超出视口、代际失配、容器切换或关闭时立即隐藏。
      —— R13 视觉分流（ValidGreen/InvalidRed/Hidden）+ 代际/几何/关闭清理测试全 PASS；R7 实机确认绿/红框与隐藏行为。
- [x] 浮动物品图标绑定真实 ItemJar/ItemAsset 身份并沿原生图标刷新路径取得纹理，跟随抓取点和旋转。
      —— R44/R45 原生图标 seam（TryGetNativeIconPlacement/TryGetIconScreenPosition）+ ROTGRAB 修复后横/竖物品拿起均有浮动图标（R7 实机确认）。
- [x] 自动旋转严格受 SettingsRuntime 快照控制；关闭增强交互后立即 Pass-Through 到原生拖拽，重新开启后恢复增强路径。
      —— R13 rotation 测试 + R7-BAND 边缘感应带（band=clamp(1.0,dim*0.15,2.0) 按轴独立）+ R7 实机确认；enhanced-off → placement-passthrough 日志确认。
- [x] 普通网格合法释放只调用原生 `sendDragItem` 适配路径；快捷槽、装备、AREA、交换、拖出地面和未知页面保持原生 Pass-Through。
      —— R13 release 门控（源页/目标页/回调页/预览代际四重校验）+ passthrough 测试；NativeInventoryInteractionAdapter.HandleRelease 全路径覆盖。
- [x] 提交后进入 AwaitingProjection，消费原生库存更新并按会话/指纹收敛；超时只影响视觉等待，不伪造回滚或客户端权威。
      —— DEV-15C 投影中继（代际/容器/物品指纹绑定 + 2s 视觉预算）+ R13 stale/投影测试。
- [x] 不新增平行库存 RPC、不修改 Unturned 原生库存权威、不执行客户端库存写入。
      —— 架构约束贯穿 DEV-15A~D/R13；提交唯一路径 `sendDragItem → ReceiveDragItem`。
- [x] 预览热路径和图元池化满足已冻结的性能门禁；拖拽、真实图标、投影、代际失配和故障隔离测试通过。
      —— Placement.Tests 10,000 次热路径 0 分配断言；R13 全部回归 + 七项目测试 PASS。
- [x] Release 编译、全套测试、静态门禁、GPT 独立审计和 Gemini 前端消费复核通过。
      —— Release 0/0、七项目全 PASS、UI/native token 门禁 PASS、diff-check 0；R13 双轴（Standards/Spec）独立审查多轮 CLEAN；Gemini 前端消费复核按 R13 责任变更（GPT 接手前端实现）由各轮双轴审查替代，实机体验由用户逐轮确认。

### 2026-08-30 R29 修复（真机读数驱动，R24 产物时序错误纠正）

- **真机判读**（`UMM-诊断包_20260830_140936`）：r24 产物部署（身份 `B7A0100F…` ✓）→ `hooks-failed: HarmonyException: IL Compile Error`（**16D Activate 的 catch**）→ `drag-started`=0。
- **根因**：r24 目录的 DLL 构建于 `ActiveAdapter = this` 修复（R25 轮）**之前**——R25 修复只进了源码与新 r25 目录，r24 目录未刷新——**用户部署的是缺修复版本**。
- **修复**：r24 目录产物刷新为含修复构建（SHA-256 `D57423E4…D696`，与源码 HEAD 一致）；全套 7/7 PASS。
- **交互模式澄清（用户）**：Unturned 为**左键点击选中后图标随鼠标**（非按住拖动）——BUE 的 isDragging 边沿检测（false→true=选中）与该模式吻合，无需改交互模型。
- 状态：`in-progress`——**请部署刷新后的 r24 产物**（哈希 `D57423E4…D696`）并重复拖拽测试。

### 2026-09-01 R12 修复与双轴审查

- **提交**：`b698562f5d3fb4cff56b1eecd665b28a4548269f`（基线 `43d05ef91bf61f87dcba38774f78dcc9b4b60bc3`）。
- **修复**：`GridContentLocal` 不重复加滚动；surface 未就绪时保留拖拽帧；surface 晚到时保存并恢复 Presenter 拖拽代际；关闭/释放/取消清零代际。
- **红测→绿测**：R12 surface-late-drag 回归测试由失败转绿；R10/R11 回归与默认 7 项测试全部 PASS。
- **Release/门禁**：0 errors / 0 warnings；Contracts 2、Core 10、ClientUi 11 个 C# 文件 UI/native token 门禁 PASS；`git diff --check` PASS。
- **双轴审查**：Standards `CLEAN`；Spec `CLEAN`；无阻断项。
- **正式候选**：`audit/2026-09-01/artifacts/BetterUnturnedExperience.dll`，SHA-256 `142AC39F769EF23EE75406F3DE84F21B59597B8F1BA72191650B128CF624C5E4`，CandidateBuild `DEV-16D-R12-CLEAN-20260901`，CaseId `DEV-16D-R12-20260901`。
- **下一步**：人工单人实机复测；真实客户端/P2P/U3DS 资格证据仍不继承静态审查结果。

## Comments

### 2026-09-01 DEV-16D implementation freeze

本工单未关闭。根据 `snapshots/DEV-16D-implementation-state-freeze-20260901.md` 的只读审计，当前实现方向与 U3-SDK 原生注入 seam 基本一致，但仍有一个阻断级实现问题；另附一个页面覆盖事实记录：

1. `InventorySurfaceLifecycleAdapter.cs:149-166` 使用 `Items.items[index]` 作为逐格占据判断，不符合 U3-SDK `ItemJar` 旋转 footprint/`slots[,]` 语义；且与 `InventoryDragPreviewAdapter.IsSwapOntoOccupied` 存在两套占据事实源。
2. 覆盖事实（非当前阻断）：`InventorySurfaceLifecycleAdapter.cs:894-976,991-1052` 当前只接入 Backpack 及共享的 Storage/Trunk page 7，Hands、Vest、Shirt、Pants 页面尚未进入增强预览接线；DEV-16D 规格要求装备页保持原生 Pass-Through，如需扩大范围必须另立需求变更。

本次冻结仅记录实现状态和修复方向，不修改生产源码、不构建、不生成 DLL。后续 Agent 必须按“统一 footprint occupancy → 补齐页面映射 → 红测/绿测 → Release/全套测试/静态门禁 → 全新的 Standards + Spec 双轴审查”的顺序推进。双轴未 CLEAN 前不得把 DEV-16D 标记为 `resolved`，也不得交付新 DLL 供实机测试。

仓库化冻结记录：`snapshots/DEV-16D-implementation-state-freeze-20260901.md`。

### 2026-09-01 Native Conformance Remediation 规格

已根据冻结快照和 U3-SDK 调研发布实施规格：`spec-DEV-16D-native-conformance-remediation.md`。

该规格是 DEV-16D 的符合性修复增量，不废弃 DEV-16A、DEV-16B、DEV-16C 或 DEV-16E。其首要实现目标是统一 `ItemJar footprint → occupancy snapshot`，保持现有原生 UI 注入 seam 和页面 Pass-Through 矩阵；DEV-16E 继续等待新的 DEV-16D 候选产物和证据。
