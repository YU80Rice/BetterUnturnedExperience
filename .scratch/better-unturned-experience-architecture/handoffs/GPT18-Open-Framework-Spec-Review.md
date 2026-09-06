> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-GPT18-Open-Framework-Spec-Review：GPT-18 与前置运行时框架规格复核报告

> **作者**: Gemini（前端负责人 / UI 表现层与消费端负责人）  
> **接收方**: GPT（后端负责人、架构与发布门禁） / 人工开发者  
> **复核对象**:  
> 1. 工单：[`18-open-runtime-feature-framework.md`](../issues/18-open-runtime-feature-framework.md)  
> 2. 规格说明书：[`spec-open-runtime-feature-framework.md`](../spec-open-runtime-feature-framework.md)（及其中文镜像 `spec-open-runtime-feature-framework.zh-CN.md`）  
> **当前状态**: **ACCEPT（全量同意架构升级方向，提出 4 项前端与表现层补充完善建议）**  

---

## 一、 总体审查结论与态度

**前端立场：`ACCEPT（完全赞同并接受该架构演进方向）`。**

将 BUE 从“单体聚合 DLL 编译模式”升级为**“基于 BepInEx 的公开前置框架插件 + 独立功能模块生态（Prerequisite Framework Runtime）”**，完美契合了模组化生态的终极愿景，同时：
1. **严格保持了零类型泄漏与 Headless 强隔离**：通过“核心 DLL（纯逻辑/无 UI 依赖）+ 进程门禁的可选客户端 UI 卫星 DLL（Glazier/Sleek 表现层）”分层，彻底杜绝了 U3DS 服务端解析 UI 类型崩溃的隐患。
2. **坚持显式依赖与防反射扫描**：坚决禁止 `Assembly.GetTypes()`、类名猜测或全局 `PatchAll()`，利用 BepInEx 显式依赖注入机制实现确定性启动。
3. **设置权威与库存权威不妥协**：第三方功能同样接入统一设置快照（Settings Facet/Snapshot）与原生库存权威链，不引入第二事实源或平行库存 RPC。

---

## 二、 前端与表现层视角下的 4 项细化与完善建议

为了确保后续在进入 Shared Contract Change Request（SCCR）和具体开发时不产生歧义，从前端 Presenter、Glazier 渲染与设置中心的角度提出以下 4 项明确建议：

### 1. 客户端 UI 卫星缺失时的优雅降级（UI Satellite Graceful Degradation）
* **现状分析**：规格书 §49 & §88 提出了可选客户端 UI 卫星 DLL（Client UI Satellite DLL）。
* **完善建议**：
  * 当某功能安装了核心 Core DLL，但其对应的 UI 卫星 DLL 缺失、损坏或初始化抛出异常时，BUE 统一管理面板应将其状态标记为 `PresentationDegraded`（或 `HeadlessOnly` 徽标），并输出温和结构化诊断；
  * 该功能的**设置项（Settings Facet）依然由 BUE 统一设置中心正常加载并允许配置**，不因自定义 UI 卫星缺失而导致设置不可用。

### 2. 统一设置中心与第三方 UI 扩展的隔离边界（Settings vs Custom HUD Seam）
* **现状分析**：规格书 §89 提到了“optional client UI registration token”。
* **完善建议**：
  * BUE 核心拥有**统一设置模态窗（`SleekFeatureSettingsModal`）的主权**，第三方功能只能通过编译期静态生成的 Settings Facet 提供设置项描述（Toggle, Slider, Choice, KeyBinding），禁止第三方模块向设置面板内部注入不受控的原始 Sleek/Unity 控件；
  * 第三方功能的自定义 HUD 或游戏内视觉交互（如自定义物品占据图层），必须通过卫星程序集实现 `IClientUiFeatureComponent`（如 DEV-05 冻结的生命周期 Seam）接入，保持 BUE Contracts 纯 C# 零泄漏。

### 3. 官方功能与第三方功能绝对平权（Dogfooding Principle）
* **完善建议**：
  * 首个参考功能 **“更好的物品交互”（Better Item Interaction）** 必须使用与第三方外部功能**完全相同**的公开注册契约与生命周期接口进行装配；
  * 框架内部不保留任何专属于官方功能的私有后门或特权通道。

### 4. 共享契约变更流程（SCCR 流程规范）
* **完善建议**：
  * 在推进 GPT-18 的生产代码前，必须先输出标准的 **Shared Contract Change Request（SCCR）**，冻结 `IFeatureRegistration`、`IFeatureCatalogEntry`、`IClientUiSatelliteRegistration` 等新增接口；
  * 保持 DEV-01～DEV-08 已验收的静态成果稳定，不进行追溯性破坏。

---

## 三、 结论与签字

* **前端判定**：**`ACCEPT`（同意立项与规格冻结，建议将上述 4 项明确纳入后续 SCCR 细化）**。
* **下一步推进**：同意 GPT、Gemini 与人工开发者共同签署本规格书，准备开启下一阶段的共享契约变更与无操作功能夹具（No-op Fixture）验证！

---

*报告完。作者: Gemini*


