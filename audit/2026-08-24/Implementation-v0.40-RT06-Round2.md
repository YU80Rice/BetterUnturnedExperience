# RT-06 第 2 轮独立重审报告

> 审计者：独立审计 Agent  
> 日期：2026-08-24（Asia/Shanghai）  
> 前轮报告：`Implementation-v0.39-2112.md`  
> 判定：**FAIL（原阻断已关闭，但发现 1 个新的收敛阻断）**

## 一、原阻断复核

新增 §2.1 已关闭上一轮“wire schema 不精确”的阻断：

| Requirement | Result |
| --- | --- |
| Contract envelope 是否改变 | PASS：保持 RT-01 的 10-byte envelope |
| `0x0004` 精确 payload | PASS：48 bytes，两个 `u64` + 两个 16-byte nonce |
| Ready 后 prefix | PASS：52 bytes，字段顺序、宽度、little-endian、Flags/Reserved 均冻结 |
| 消息方向与范围 | PASS：`0x0101/0104` client→server；`0x0102/0103/0201` server→client |
| bootstrap/Contract decoder 分流 | PASS：`BUEB` 优先且非法不回退；magic 不匹配才进入 Contract decoder |
| 握手消息分流 | PASS：`0x0001`～`0x0004` 禁止 Ready fence；`0x0004` 精确 48-byte payload |
| mismatch 处理 | PASS：未知版本、非零保留位、短 payload 或 binding mismatch 均静默 fail-closed、限频诊断、不放大回复 |
| 能力协商 | PASS：显式 capability `betterunturned.ready-frame-fence.v1` |
| 旧端降级 | PASS：同 Major 缺能力时只禁用插件网络设置/状态；Major 不兼容走 BUEB Reject；原版连接和本地功能继续 |
| nonce 语义 | PASS：明确只是握手关联，不是 MAC、认证、授权或 sender identity |

上述规则与 RT-01 的 10-byte little-endian envelope、GPT-11 的 BUEB 优先分流、双 128-bit nonce、能力交集和静默降级语义一致。

## 二、新阻断项

### B-01：replay window 满载时同时封死 `0x0104`，与冻结的 8 秒快照收敛规则冲突

- 涉及位置：
  - `RT-06-Joint-Seam-Implementation-Readiness.md §2.1:66-70`
  - 同文件设置时序：`3s` 重试后 `8s` 通过 `0x0104` 拉取快照
  - RT-01：`0x0104` 必须使用新的非零 RequestId，只读且不增加 revision
  - GPT-11：`0x0104` 有独立 rate limit，并承担当前可见快照读取
- 根因：§2.1 要求 `0x0101` 和 `0x0104` 在 fence 后进入当前 generation 的同一个固定容量 RequestId replay window；窗口满时，所有新唯一请求都 `ReplayWindowFull` fail-closed。可是 8 秒恢复必须为 `0x0104` 创建新的非零 RequestId。窗口一旦满载，该恢复请求也永远无法进入，且规范又禁止淘汰或轮换旧记录，导致本连接代际没有任何收敛出口。
- 影响：一个合法长连接达到 replay capacity 后，不仅新设置写入失败，连只读全量快照恢复也失败；前端只能重复超时，违反 RT-01/GPT-13 的 3s/8s 最终收敛语义。
- 修复建议：冻结不会重新打开写入重放窗口的恢复策略，例如：
  1. 写入 `0x0101` 使用严格有界、保留终态的 replay window；
  2. 只读 `0x0104` 使用独立的小型有界 replay/rate-limit 域，或定义在 fence/权限/范围校验后即使写窗口满仍允许安全只读快照；
  3. 明确 `0x0104` 同 RequestId 幂等、不同 payload 冲突及独立容量行为；
  4. 增加“写 replay window 已满后，新的 `0x0104` 仍能返回完整当前快照且不增加 revision”的规范测试。

在该冲突关闭前，RT-06 仍不具备开放 SettingsRuntime/network production ticket 的条件。

## 三、其他一致性结论

- 方案 A 的精确安全性质仍被正确限制为：旧/失配 frame 在 mutation/projection 前拒绝；不声称防伪、认证或授权。
- `0x0004` 增加的 nonce bytes 是 wire-level Ready binding；建议明确共享 `SessionReadyEvent` 进程内 DTO 仍只投影 `ConnectionGeneration + SnapshotId`，避免实现者误以为公共 DTO 已扩字段。本项可作为非阻断文字修正。
- Server→client status/settings projection 也受相同 fence，符合 Gemini 只消费当前 Ready context 的原则。
- 未发现任何 SP、P2P Host/Client、U3DS runtime PASS 越级声明。

## 四、最终结论

上一轮唯一阻断已经由新增 §2.1 实质关闭，线路字段、decoder 分流、能力协商和旧端降级均已可唯一实现；但 replay 满载规则封死了 `0x0104` 的既定恢复通道，产生新的最终一致性阻断。判定 **FAIL**，修复 B-01 后需第 3 轮重审。

