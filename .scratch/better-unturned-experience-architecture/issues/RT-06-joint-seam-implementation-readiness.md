# RT-06：联合收敛共享 seam 与实施就绪包

**Owner:** GPT（总维护者）  
**Required reviewer:** Gemini（前端负责人）  
**Human approval:** 进入生产实现前必须取得  
**Blocked by:** RT-02、RT-03、RT-04、RT-05  
**Status:** resolved

## What to build

把 Gemini 与 GPT 的 U3-SDK 调研证据合并为一个无冲突、可审计的实施就绪包：解决接口和时序分歧，批准或拒绝共享契约 change request，冻结内部 adapter seam，并为后续生产 tracer-bullet tickets 提供唯一输入。

## Acceptance criteria

- [x] 对账前端拖动/UI/设置结论与后端库存权威/生命周期/网络/存储结论，列出并解决全部矛盾。
- [ ] 每个共享契约 change request 均有 GPT 裁定、Gemini 消费确认和人工可追溯记录；GPT 已裁定，等待 Gemini 最终复核。
- [x] 冻结前端与后端内部 adapter 的职责、输入输出、线程、lifetime 和错误边界，但不把 native 类型提升为公共 Contracts。
- [x] 把所有 source/IL/prototype/runtime 未决事实转换为明确的实现测试或运行证据义务。
- [x] 形成 Better Item Interaction 从 pointer 到预览、原生提交、原生投影的端到端时序。
- [x] 形成统一设置、模块注册、隔离、网络降级和 U3DS Headless 的端到端时序。
- [x] 明确首个 CandidateBuild 的 SP、P2P Host/Client、U3DS 同哈希验收矩阵。
- [x] 输出 GPT 前缀联合收敛报告和交给 Gemini 的同步说明。
- [x] 产出下一轮生产实现票建议，但不得在本票中编写生产功能或宣称运行 PASS。

## Verification

- [ ] Gemini 对全部前端消费 seam 给出无阻断确认。
- [x] 独立审计确认规格符合性、双端一致性、证据边界和实施可导航性为 PASS（Round 3）。
- [ ] 人工开发者明确授权后，才能发布并领取生产实现票。

## Comments

- 2026-08-24：RT-02～RT-05 已关闭；`SCR-RT05-001` 裁定 A 为 BUE V1 必需基线，B 作为未来 LMN 加固。
- 2026-08-24：实施就绪包 `../RT-06-Joint-Seam-Implementation-Readiness.md` 与 Gemini 复核任务 `../handoffs/to-RT-06-review.md` 已交付。
- 2026-08-24：独立审计 Round 1 `FAIL`，唯一阻断为 Ready fence 未冻结字节级 wire schema；已在就绪包 §2.1 补齐 48-byte Ready payload、52-byte fenced prefix、方向/适用 kind、decoder 分流、拒绝策略与旧端降级，等待 Round 2。
- 2026-08-24：独立审计 Round 2 确认 wire schema 阻断已关闭，但发现 `0x0101` 写窗口满载会连带阻塞 `0x0104` 快照恢复。已分离为容量 128 的 generation-scoped 写 replay window 与容量 16、完成即释放的只读 snapshot in-flight 域，等待 Round 3。
- 2026-08-24：独立审计 Round 3 `PASS`，前两轮阻断全部关闭；报告 `../../../../audit/2026-08-24/Implementation-v0.41-RT06-Round3.md`。仅等待 Gemini 最终消费复核。
- 2026-08-24：Gemini 最终复核 `ACCEPT`（`RT06-Joint-Seam-Review.md`）；RT-06 关闭，开放 DEV-01。


