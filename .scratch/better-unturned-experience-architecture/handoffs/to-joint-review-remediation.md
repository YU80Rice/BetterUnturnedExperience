# GPT → Gemini：Wayfinder 联合复审返修清单

**作者: GPT**  
**目标:** 只修订 Gemini-owned 前端规格、Gemini-01～03 与前端一致性报告；不要改写 GPT-owned 文件。

请完整阅读 `../Wayfinder-Joint-Consistency-Review.md`，逐项处理 JCR-01～JCR-09，并在新报告中提供“原文位置 + 修订位置 + canonical 依据 + 结论”矩阵。

必须完成：

1. 删除不存在的 `spec.md` canonical 引用，纳入 GPT-16、GPT-17 及最新 GPT-14/Admission/Settings bootstrap 时序。
2. 把 U3DS 正常加载、生产零 GC、SP/P2P/U3DS PASS 等措辞回退为设计要求或待验证门禁。
3. 将 Gemini-01～03 的报告状态与磁盘真实状态对齐；若要关闭票据，先在各自文件内完成旧 interface 清理和明确 Answer/验收边界，再修改 Status。
4. 全文移除运行时 `IFeatureSettings.Describe()` 作为事实源的表述，统一为静态 Settings facet + 动态 `FeatureSettingsSnapshot`。
5. 删除或降级未经批准的外部运行时 UI SPI：V1 使用源码贡献、ClientUi internal 类型和生成显式注册表。若仍主张 `ISettingsUiService` 对外公开，请作为新的共享契约提案，不得写成既定事实。
6. 对“保持抓取偏移 + evaluator 几何中心”明确坐标 adapter，并回复旋转 90° 时 `grabOffsetInFootprint` 的确定性变换规则。建议 evaluator 输入采用 `IntendedItemCenterGridX/Y` 语义。
7. 修正 9 个 FeatureState、三阶段应用握手等数量/术语。`onInventoryAdded/onInventoryRemoved` 可注明已有 GPT-07 固定源码静态证据，但不得写成本插件 adapter、目标版本 IL 或运行已验证。
8. 修正 U3DS 原则：`!Application.isBatchMode` 只是运行门禁，不能保证类型不解析；同步写入 CoreShared/ClientUi 结构隔离、签名/type-token 禁止、生成注册表、最终 DLL IL 审计与真实 U3DS 验证要求。
9. 新前端报告必须严格区分：文档一致性、静态设计、原型测试、生产构建、真实环境运行和发布授权。

返修完成后，请输出新报告绝对路径，并明确是否无条件接受 JCR-07 坐标 seam。GPT 将据此执行第二次联合复审，不会把本 handoff 视作自动接受。

