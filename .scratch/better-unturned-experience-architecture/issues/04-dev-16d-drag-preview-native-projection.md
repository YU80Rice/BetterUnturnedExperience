# 04：DEV-16D 拖拽预览、真实图标、原生提交与投影收敛

**What to build:** 让玩家在受支持的网格拖入物品时看到绿色/红色占据框和真实物品图标，自动旋转与设置开关生效，合法操作沿原生库存权威链提交，并在原生投影到达后收敛。

**Blocked by:**

- 02：DEV-16B BUE 内置插件管理面板与设置编辑
- 03：DEV-16C 原生库存 UI 生命周期与容器上下文接线

**Status:** in-progress

> 2026-08-30 认领（agent）：前置 02（DEV-16B resolved）、03（DEV-16C resolved，`IInventorySurfaceContext` 真实投影与 `PlayerUI.Update` 轮询驱动已交付）。实施遵循 AGENTS.md Output review loop。

- [ ] 玩家背包、普通容器和车辆后备箱之间，以及地面物品到这些网格的拖入路径进入增强预览。
- [ ] 合法候选显示绿色占据框；局部无效候选显示红色占据框；超出视口、代际失配、容器切换或关闭时立即隐藏。
- [ ] 浮动物品图标绑定真实 ItemJar/ItemAsset 身份并沿原生图标刷新路径取得纹理，跟随抓取点和旋转。
- [ ] 自动旋转严格受 SettingsRuntime 快照控制；关闭增强交互后立即 Pass-Through 到原生拖拽，重新开启后恢复增强路径。
- [ ] 普通网格合法释放只调用原生 `sendDragItem` 适配路径；快捷槽、装备、AREA、交换、拖出地面和未知页面保持原生 Pass-Through。
- [ ] 提交后进入 AwaitingProjection，消费原生库存更新并按会话/指纹收敛；超时只影响视觉等待，不伪造回滚或客户端权威。
- [ ] 不新增平行库存 RPC、不修改 Unturned 原生库存权威、不执行客户端库存写入。
- [ ] 预览热路径和图元池化满足已冻结的性能门禁；拖拽、真实图标、投影、代际失配和故障隔离测试通过。
- [ ] Release 编译、全套测试、静态门禁、GPT 独立审计和 Gemini 前端消费复核通过。

### 2026-08-30 R29 修复（真机读数驱动，R24 产物时序错误纠正）

- **真机判读**（`UMM-诊断包_20260830_140936`）：r24 产物部署（身份 `B7A0100F…` ✓）→ `hooks-failed: HarmonyException: IL Compile Error`（**16D Activate 的 catch**）→ `drag-started`=0。
- **根因**：r24 目录的 DLL 构建于 `ActiveAdapter = this` 修复（R25 轮）**之前**——R25 修复只进了源码与新 r25 目录，r24 目录未刷新——**用户部署的是缺修复版本**。
- **修复**：r24 目录产物刷新为含修复构建（SHA-256 `D57423E4…D696`，与源码 HEAD 一致）；全套 7/7 PASS。
- **交互模式澄清（用户）**：Unturned 为**左键点击选中后图标随鼠标**（非按住拖动）——BUE 的 isDragging 边沿检测（false→true=选中）与该模式吻合，无需改交互模型。
- 状态：`in-progress`——**请部署刷新后的 r24 产物**（哈希 `D57423E4…D696`）并重复拖拽测试。
