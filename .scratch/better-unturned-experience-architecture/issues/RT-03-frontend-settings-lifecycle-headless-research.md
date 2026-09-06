# RT-03：调研统一设置 UI、模块状态与 Headless 隔离

**Owner:** Gemini（前端负责人）  
**Required reviewer:** GPT（生命周期与设置契约）  
**Blocked by:** RT-01：冻结共享契约与对账基线  
**Status:** resolved

## What to build

形成玩家可见的统一设置入口与模块状态投影的完整前端证据链，同时证明 UI 创建、清理和类型引用不会穿透到 U3DS，从而让单模块隔离时无残影、核心 SafeMode 时原版 UI 仍可用。

## Acceptance criteria

- [x] 查明主菜单、暂停菜单和选项区域可用的统一设置入口、生命周期，以及旋转键、设置快捷键、文本输入和 KeyBinding 捕获的输入焦点 seam。
- [x] 定义构建期 Settings facet 与运行期 `FeatureSettingsSnapshot` 的前端消费方式，不引入第二事实源（已对齐 N-02）。
- [x] 覆盖连续输入防抖、服务器政策锁定、Category/GroupKey、KeyBinding 编码和 revision 刷新表现。
- [x] 映射 Running、Disabled、Incompatible、Isolated 和 Core SafeMode 到设置项、徽章、提示和 UI 卸载行为（已对齐 B-03）。
- [x] 查明 view close、feature stop/isolation、generation 失效时 HUD、事件、timer 和 input registration 的对称清理点。
- [x] 列出全部客户端专用 type token，并给出保证 U3DS 不解析/实例化 UI 类型的五重结构隔离方案（已对齐 B-04）。
- [x] 比较选定客户端 reference 中统一设置相关 Glazier/Sleek 具体类型差异，并将版本差异转换为 adapter 或测试义务。
- [x] 提议无需反射扫描的构建期 ClientUi 注册记录及内部 adapter seam。
- [x] 输出 Gemini 前缀研究报告、生命周期图、状态表现矩阵、Headless 类型风险表、被拒 hook 和运行测试义务。
- [x] 不实现生产设置 UI，不修改共享契约；发现不足时提交 change request。

## Verification

- [x] 每个原生事实带固定源码身份和证据分类。
- [x] 调研报告已输出至 `RT-03-Frontend-Settings-Lifecycle-Headless-Research.md`，已按 GPT 联合复核意见完成 B-03、B-04 及 N-01～N-04 修订。

## Comments

- 完整调研报告（修订版）：[`RT-03-Frontend-Settings-Lifecycle-Headless-Research.md`](../RT-03-Frontend-Settings-Lifecycle-Headless-Research.md)。
- 契约充分性：`GPT-RT-01` 共享契约基线完备，未产生任何 Shared Contract Change Request。
- 关闭记录：Gemini 已完成 B-03、B-04、N-01～N-04 与 C-01 修订；GPT 三轮定点复核及最终证据源哈希核对 PASS。单 DLL IL、U3DS 加载和 SafeMode 运行行为仍由报告中的 `VO-RT03-01`～`VO-RT03-03` 约束，不属于本调研票的运行 PASS。
- SourceSet 迁移记录：已迁移至 successor `BUE-SS-20260824-02`（Predecessor `BUE-SS-20260824-01`），源码哈希无变更；不改变运行证据等级。

