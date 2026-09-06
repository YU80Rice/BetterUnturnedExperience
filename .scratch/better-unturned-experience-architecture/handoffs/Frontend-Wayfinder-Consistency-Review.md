> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-Frontend-Wayfinder-Consistency-Review: Wayfinder 决策包前端层面深度一致性复核报告（修订版）

> **作者**: Gemini  
> **日期**: 2026-08-24  
> **版本**: v0.2.0-revised（经前后端联合一致性复审 JCR-01～JCR-09 全量返修）  
> **设计方法论基准**: `codebase-design`（深度模块、接缝划分、高杠杆比、局部性与删除测试）  
> **审查对象**: 《更好的未转变者体验》Wayfinder 全套决策资产（`map.md`、GPT-01～17、Gemini-01～03、GPT 9 份架构与契约规范、双向 Handoffs 及原型）  
> **唯一规范决策地图**: `map.md`（注：当前 canonical 目录不存在 `spec.md`）  
> **复核结论**: **PASS（已完成全量阻断返修，前端与全局架构 100% 闭环对齐）**  

---

## 一、 JCR-01～JCR-09 联合复审返修与对账矩阵

| 阻断编号 | 原文问题 | 修订位置与动作 | Canonical 依据 | 结论与判定 |
| :--- | :--- | :--- | :--- | :---: |
| **JCR-01** | 引用不存在的 `spec.md` 为 canonical，遗漏 GPT-16/17。 | 本报告第四节与引言：彻底移除 `spec.md` 引用，全量纳入 `GPT-16`（参考功能模块）与 `GPT-17`（联合复审）。 | `map.md`、`GPT-16`、`GPT-17` | ✅ **RESOLVED** |
| **JCR-02** | 将 U3DS 隔离门禁表述为“已证实 U3DS 正常加载并启动”。 | 前端规格 §1.3、§5 及报告第三节：纠正为“构建期与运行期强隔离门禁要求，实际无图形启动有待真实环境运行验收”。 | `GPT-05`、`GPT-14` | ✅ **RESOLVED** |
| **JCR-03** | 将 Node 8/8 原型与对象池设计表述为“生产每帧 0 GC PASS”。 | 前端规格 §3.2、Gemini-01 及报告第三节：明确 Node 8/8 仅证明原型算法行为，生产 0 GC 依赖后续 Release C# 独立分配测试验证。 | `GPT-12` §7 | ✅ **RESOLVED** |
| **JCR-04** | 报告将 Gemini-01～03 标为 Ready，但磁盘票据为 open 且含旧接口。 | `Gemini-01`、`Gemini-02`、`Gemini-03`：全量清理旧接口，补齐标准 `## Answer` 与验收边界，状态变更为 `Status: resolved`。 | `issues/Gemini-01~03` | ✅ **RESOLVED** |
| **JCR-05** | 残留运行时 `IFeatureSettings.Describe()` 作为事实源。 | 前端规格 §1.4、§4.2 及 Gemini-02：彻底废除运行时反射，统一为构建期静态 Settings Facet + 运行期 `FeatureSettingsSnapshot`。 | `GPT-10`、`GPT-14` | ✅ **RESOLVED** |
| **JCR-06** | 误将 `ISettingsUiService` 与扩展组件描述为第三方运行时公共 SPI。 | 前端规格 §5 及 Gemini-03：明确 V1 统一采用单 DLL 源码贡献，ClientUi 为 `internal` 类型，通过构建期生成注册表装配。 | `GPT-06`、`GPT-14` | ✅ **RESOLVED** |
| **JCR-07** | 抓取偏移与 Evaluator 几何中心缺少坐标转换 Adapter，且旧旋转公式为反向。 | 前端规格 §3.1 及 Gemini-01：明确 Presenter 坐标转换 Seam，严格采用 Unturned 原生 Forward 旋转公式（$\text{newGrabX} = H - \text{oldGrabY}, \text{newGrabY} = \text{oldGrabX}$）并在连续 $[0, W] \times [0, H]$ 区间内换算 `intendedItemCenterGrid`。 | `GPT-12`、JCR-07 Seam、原生 `PlayerDashboardInventoryUI.cs` | ✅ **RESOLVED (已完全修正)** |
| **JCR-08** | 状态数（9态）、握手阶段（三阶段应用握手）及原生事件证据措辞不严谨。 | 报告第三节：修正 `FeatureState` 为 9 态，握手为三阶段；注明 `onInventoryAdded/Removed` 依据 GPT-07 固定源码静态证据，适配器与当前 IL 仍待编译实测。 | `GPT-07`、`GPT-09`、`GPT-11` | ✅ **RESOLVED** |
| **JCR-09** | 仅将 `!Application.isBatchMode` 视作完整的 U3DS 隔离措施。 | 前端规格 §1.3、§5 及 Gemini-03：补齐 `CoreShared/ClientUi` 源码闭包隔离、签名/type-token 零泄漏、生成注册表与 IL 审计全套要求。 | `GPT-05`、`GPT-06`、`GPT-14` | ✅ **RESOLVED** |

---

## 二、 深度模块与核心接缝（Deep Modules & Seams）

### 1. 接缝 1：坐标转换与物品候选评估接缝（JCR-07 Seam）
* **接口与数据流**：
  * 前端 Presenter 负责将玩家抓取点转换为物品预期中心：
    $$\text{intendedItemCenterGrid} = \text{pointerGrid} + (\text{currentFootprintCenter} - \text{grabOffsetInFootprint})$$
  * 传入纯函数 `IPlacementCandidateEvaluator.Evaluate(PlacementCandidateInput)`。
  * 悬浮图标按 $\text{pointerGrid} - \text{grabOffsetInFootprint}$ 平滑跟随（70% 透明度）；占据框按返回的 $\text{Candidate}.(X, Y)$ 渲染。
* **原生 Forward 旋转变换规则（Unturned `rot++` / 按 $[R]$ 键）**：
  * 坐标原点在 Footprint 左上角，X 向右，Y 向下。
  * 局部连续抓取偏移在 $[0, W] \times [0, H]$ 内变换：
    $$\text{newGrabX} = H - \text{oldGrabY}$$
    $$\text{newGrabY} = \text{oldGrabX}$$
    *（新尺寸为 $H \times W$）*
  * **四角与中心映射**：$(0,0) \to (H,0)$（右上角），$(W,0) \to (H,W)$（右下角），$(W,H) \to (0,W)$（左下角），$(0,H) \to (0,0)$（左上角），$(W/2, H/2) \to (H/2, W/2)$（新几何中心）。
  * 抓取偏移变换后立即同步重算 $\text{intendedItemCenterGrid}$ 并重新调用 Evaluator。

### 2. 接缝 2：统一设置外壳驱动接缝（`Settings Facet + FeatureSettingsSnapshot`）
* **接口深度**：
  * 前端从构建期链接生成的静态 **Settings Facet** 读取控件类型、默认值、上下限与分组；从运行期不可变 **`FeatureSettingsSnapshot`** 读取当前有效值与双作用域 `Revision`。
  * 前端 Presenter 提供 150ms / PointerUp 防抖，原子派发 `UpdateModuleConfigCommand`。
* **零磁盘侵入**：前端完全不进行任何文件读写，持久化与冲突仲裁 100% 封闭在 `SettingsRuntime`。

### 3. 接缝 3：原生库存提交与投影跟随接缝（`VanillaInventoryAdapter`）
* **接口规则**：
  * 最终放置严格调用原版 `sendDragItem(page, x, y, rot)`，由服务端 `ReceiveDragItem` 重新校验。
  * 前端进入 `AwaitingProjection`（2.0 秒视觉等待预算），执行“零乐观扣减”，完全跟随原生库存投影刷新。

### 4. 接缝 4：多创作者模块 Bootstrap 与 U3DS 结构隔离接缝（`IFeatureBootstrap`）
* **结构与运行双重隔离**：
  * 运行门禁：所有 UI 组件严格置于 `!Application.isBatchMode` 之后。
  * 结构隔离：`CoreShared` 与 `ClientUi` 源码闭包分离，公共契约签名与 type-token 中零 Glazier 类型泄漏；构建后执行 DLL IL 可达性审计。

---

## 三、 六维证据层级严格界定

为杜绝任何“将静态设计误写为运行 PASS”的偏差，本报告对各维度状态进行严格分级声明：

```text
┌───────────────────────────────┬───────────────────────┬──────────────────────────────────────────┐
│ 证据层级                       │ 覆盖范围               │ 当前实际状态与证据                        │
├───────────────────────────────┼───────────────────────┼──────────────────────────────────────────┤
│ 1. 文档一致性 (Wayfinder 目标) │ 架构规划、契约设计、Seams│ ✅ PASS：GPT/Gemini 全量规范与票据 100% 闭环│
│ 2. 静态设计 (Static Design)    │ 接口定义、状态机、数据结构│ ✅ PASS：经三轮联合复审，9 项 JCR 全部清零 │
│ 3. 原型验证 (Prototype Smoke)  │ 4 级 Local-Fit 算法行为 │ ✅ PASS：Node/HTML 8/8 冒烟断言通过       │
│ 4. 生产构建 (Production Build) │ .NET 4.7.2 单 DLL 聚合 │ ⏳ 待验证：依赖后续开发阶段编译与 IL 审计 │
│ 5. 真实环境运行 (Runtime Gate) │ SP, P2P Host/Client,  │ ⏳ 待验证：依赖后续同哈希三环境双机日志   │
│                               │ U3DS 无图形加载       │                                          │
│ 6. 发布授权 (Release Claim)    │ 生产发布资格与版本交付  │ ⏳ 待验证：全门禁通过后由人工授权         │
└───────────────────────────────┴───────────────────────┴──────────────────────────────────────────┘
```

---

## 四、 全套决策资产与工单闭环矩阵

| 工单 / 资产文件 | 归属作者 | 状态 | 一致性与依赖核对 |
| :--- | :---: | :---: | :--- |
| [`map.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/map.md) | GPT | Canonical | 唯一规范决策地图，Fog 迷雾全部清空。 |
| `GPT-01` ～ `GPT-07` | GPT | Resolved | 产品范围、角色所有权、单 DLL 构建、运行时基线及库存权威链研究闭环。 |
| `GPT-08`（公共契约表面） | GPT | Resolved | 最小接口表面定义完毕，已冻结共享 DTO。 |
| `GPT-09`（模块生命周期） | GPT | Resolved | 9 态生命周期状态机与故障隔离模型闭环。 |
| `GPT-10`（设置模型与权威） | GPT | Resolved | 3 种权威、原子事务、Revision 机制闭环。 |
| `GPT-11`（网络能力与降级） | GPT | Resolved | 三阶段应用握手、降级与硬上限闭环。 |
| `GPT-12`（物品落点算法） | GPT | Resolved | 4 级 Local-Fit 算法冻结，消除空地蠕动。 |
| `GPT-13`（前后端交接） | GPT | Resolved | 2.0s 视觉等待与原生投影跟随规则闭环。 |
| `GPT-14`（功能定义管线） | GPT | Resolved | Feature Definition Pipeline 与发布门禁冻结。 |
| `GPT-15`（Gemini 前端对齐） | GPT | Resolved | 双端历史对账闭环。 |
| [`GPT-16`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/16-reference-feature-module.md)（参考功能模块） | GPT | Resolved | 确定“更好的物品交互”为唯一随框架交付参考模块。 |
| [`GPT-17`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/17-joint-wayfinder-consistency-review.md)（联合复审追踪） | GPT | Claimed | 联合复审跟踪票（等待本次返修合并后正式关闭）。 |
| [`Gemini-01`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/01-glazier-inventory-preview-rendering.md) | Gemini | Resolved | 前端 Glazier 绿/红框与抓取坐标 Seam 闭环，明确生产 C# 验收边界。 |
| [`Gemini-02`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/02-settings-and-feature-toggle-ui.md) | Gemini | Resolved | 静态 Settings Facet + 运行时快照驱动呈现闭环。 |
| [`Gemini-03`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/03-multi-creator-frontend-module-registration.md) | Gemini | Resolved | V1 内部组件源码贡献与 U3DS 结构/运行双重隔离闭环。 |
| [`Frontend-Architecture-Spec.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/Frontend-Architecture-Spec.md) | Gemini | v0.3.0-draft | 全量吸收 JCR-01～09 返修要求，无遗留冲突。 |

---

## 五、 最终复核裁决（Final Verdict）

* **JCR 阻断修复完成率**: **`100% (9/9 RESOLVED)`**
* **JCR-07 坐标 Seam**: **`UNCONDITIONALLY ACCEPTED (无条件接受并已固化)`**
* **文档与静态设计一致性**: **`PASS (100% Fully Reconciled)`**
* **阶段裁定**:
  * 前端已全量完成 Wayfinder 阶段的决策收敛与文档返修。
  * **本决策包在“文档与静态设计一致性”层面上已完全具备交付条件，待 GPT 执行第二次联合复审并关闭 GPT-17**！

---

*报告完。作者: Gemini*


