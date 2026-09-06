# RT-06 实施就绪包独立审计报告

> 审计者：独立审计 Agent  
> 日期：2026-08-24 21:12（Asia/Shanghai）  
> 审计方式：只读规格与状态对账  
> 判定：**FAIL（1 个阻断项）**

## 一、审计输入

- `RT-06-Joint-Seam-Implementation-Readiness.md`
- `handoffs/to-RT-06-review.md`
- `change-requests/SCR-RT05-001-receive-time-connection-context.md`
- `issues/RT-05-backend-runtime-network-settings-research.md`
- `issues/RT-06-joint-seam-implementation-readiness.md`
- `map.md`
- PT-RT05-001A、PT-RT05-001B 两轮最终独立审计报告

本审计未编辑上述输入。

## 二、阻断项

### B-01：SCR-RT05-001 宣称已在 RT-06 固化“精确 wire 字段”，但实施就绪包只有语义名称，没有可实现的线路布局

- 涉及位置：
  - `SCR-RT05-001-receive-time-connection-context.md:14-19,23`
  - `RT-06-Joint-Seam-Implementation-Readiness.md:12-18,28-29,48-50,84`
- 事实：原冻结基线 envelope 是 `u16 major + u16 minor + u16 kind + u32 payloadLength + payload`。SCR 明确承认方案 A 会影响共享线路 envelope，并在 HumanTrace 中写“契约精确 wire 字段在 RT-06 实施就绪包中固化”。然而 RT-06 仅写 `ConnectionGeneration + SnapshotId + HandshakeNonceBinding`，没有冻结：
  1. 三个字段位于现有 envelope 的哪一层和固定顺序；
  2. 整数宽度/字节序以及 nonce binding 的精确长度和编码；
  3. 哪些 message kind 必须携带（仅客户端写命令，还是 Ready 后双向 `0x0101`～`0x0201` 全部）；
  4. bootstrap/`0x0004` 是否使用该 frame，以及 Ready 建立前后的 decoder 分流；
  5. mismatch/full 的稳定处理结果与是否发 projection；
  6. wire major/minor 兼容与旧 peer 的确定性拒绝/降级规则。
- 根因：把原型安全性质直接提升成“V1 必需基线”，但没有完成从纯内存对象到冻结线路 token 的规范化步骤。
- 影响：`DEV-06` 的 codec、LocalLoopback 与 LMN adapter 可以各自做出不同但表面符合文字的 frame；Gemini 也无法精确确认 status/settings projection 的消费格式。此时开放 `DEV-01`～`DEV-07` 会把共享契约选择推迟到实现阶段，违背 RT-06 的唯一输入和无冲突目标。
- 修复建议：在 RT-06 包或单独的 GPT 前缀 normative appendix 中冻结精确 Ready-frame schema、适用消息集合、decoder 状态机、兼容/降级规则和稳定拒绝语义；同步修正 SCR 的规范链接。随后由 Gemini 针对该精确 schema 复核，再进行本票独立重审。

在 B-01 修复前，RT-06 不应通过独立审计，也不能开放生产票。

## 三、其余审计维度

| Dimension | Result | Notes |
| --- | --- | --- |
| 规格符合性 | FAIL | 仅因 B-01；其余 RT-06 Acceptance 均有对应内容或正确保留外部门禁 |
| 双端一致性 | PASS（待 Gemini） | pointer→preview→native submit→projection、Settings Facet/Snapshot、9 态生命周期与既有双端边界一致；Gemini 最终确认仍未完成，票据未伪造勾选 |
| 证据边界 | PASS | 明确是实施规格，不是生产运行 PASS；prototype、source/IL 与 runtime 门禁分离 |
| 方案 A 安全性质 | PASS（原型限定） | 仅声称旧 Action 在三重 binding mismatch 时不能到 mutation；replay 按 generation 分域且严格有界；没有宣称认证/授权/防伪 |
| 方案 B 定位 | PASS | 仅 future defense-in-depth，明确需要修改 LMN/SourceSet，不阻塞 V1 |
| native inventory authority | PASS | 最终仍走 `sendDragItem → ReceiveDragItem`，不复制 LMN inventory RPC；AwaitingProjection 不推断拒绝 |
| Headless/LMN 解耦 | PASS（有运行义务） | core/local-only feature 不强依赖 LMN；Core/Contracts 零 UI token 与 U3DS 真实加载均列入门禁；没有只凭 batch mode 宣告安全 |
| 实施顺序 | PASS（除 DEV-06 输入缺口） | DEV-01～07 顺序可导航，先双端编译/IL gate，再 lifecycle/settings/native/UI/network/candidate；B-01 需在开放前补齐 |
| 运行 PASS 表述 | PASS | 没有 CandidateBuild；SP、P2P Host/Client、U3DS 分环境同哈希证据明确未决 |
| 状态收敛 | PASS | RT-05 作为研究票已关闭；RT-06 保持 `ready-for-human`，Gemini/独立审计/人工授权门禁未伪造完成 |

## 四、非阻断建议

1. 将 `SettingsPresentationAdapter` 的“client UI thread”统一表述为“Unity game/main thread 上的 client UI phase”，避免被实现者误读为独立 UI 线程。
2. `HeadlessCompositionGate` 除布尔条件外，在 DEV-01 验收中应明确“gate 判定前不得解析或 JIT 触达 ClientUi 类型”；当前 IL reachability 与真实 U3DS 加载义务足以避免本轮升级为阻断。
3. `native projection` 的 fingerprint 只用于关联并结束本次 `AwaitingProjection`，不得用于拒绝或过滤原生库存事实；可在 DEV-04/05 票据中再明确一次。

## 五、最终结论

RT-06 在 native authority、Headless/LMN 解耦、原型证据边界、同哈希运行门禁和实施顺序方面总体一致，且没有误称运行通过。但 `SCR-RT05-001` 所需的方案 A 精确 wire schema 尚未实际固化，与 SCR 自身声明矛盾，使 network seam 仍不可唯一实现。判定 **FAIL（1 个阻断项）**。


