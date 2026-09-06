> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-03: 面向多创作者协同的前端模块注册与拓展标准

- Type: grilling
- Status: resolved
- Author: Gemini
- Blocked by: GPT-08, GPT-09, GPT-14

## Question

如何为 GitHub 多创作者协同提供规范的前端扩展注册中心与 UI 故障隔离机制，使第三方创作者能够安全贡献自定义 HUD/View 扩展而不破坏整体插件稳定性及 U3DS 运行？

## Answer

1. **V1 源码贡献与内部组件注册（对齐 JCR-06 & GPT-14）**：
   - V1 统一采用源码贡献汇入单 DLL，不提供动态加载外部 DLL 的公共运行时 UI SPI。
   - 创作者 UI 模块作为 `internal` 类型，通过构建期生成的显式注册表装配。
   - 内部扩展接口定义为 `internal interface IClientUiFeatureComponent`（包含 `OnUiInitialized`、`OnInventoryOpened`、`OnInventoryClosed`、`OnUiDestroyed`）。
2. **U3DS 结构隔离与运行门禁（对齐 JCR-09）**：
   - 运行门禁：所有 UI 注册与组件实例化严格封装在 `!Application.isBatchMode` 之后。
   - 结构隔离：`CoreShared` 与 `ClientUi` 源码闭包分离；Core/Contracts 的公共签名、基类、特性、静态构造函数和 type-token 中绝不包含任何 Glazier/Sleek 类型。
   - 构建门禁：编译后执行 DLL IL 可达性审计，并在发布前通过真实 U3DS 无图形环境启动验收。
3. **故障沙盒与自清理**：
   - 每个 UI 组件的生命周期回调均由独立 `try-catch` 包裹；抛出异常时自动隔离该组件、注销其 UI 元素并记录 `DiagnosticId`，主插件与原版游戏继续安全运行。

*（注：本票据设计在 Wayfinder 阶段已闭环；生产代码实现与真实 U3DS 门禁有待开发阶段实际验证）*

