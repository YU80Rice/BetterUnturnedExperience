# T7：开发者契约与防双装设计

Type: grilling
Status: open
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 01

## Question

把「Forge-like 身份承诺」落成可验收的平台契约（用户拍板 Q9=是）：

1. 契约内容：第三方引用 BUE 前置**不受 BUE DLL 文件名影响**——按 T1 实证结论定承诺边界与措辞（含编译期 HintPath 的开发者指引）;
2. 防双装检测：第三方误拷 BUE DLL 进自己发布目录 → 同 GUID 双实例时，BUE 侧给结构化诊断（诊断 id、决策点、可恢复行为）而非静默异常;机制挂在哪里（Chainloader 时机?宿主自检?）;
3. 契约的验收面：写进开发者文档的哪一层、红测锚怎么钉。

产出：契约 + 防双装设计决策，可交 `/to-spec`。
