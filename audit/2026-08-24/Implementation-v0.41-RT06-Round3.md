# RT-06 第 3 轮独立重审报告

> 审计者：独立审计 Agent  
> 日期：2026-08-24 21:17（Asia/Shanghai）  
> 前轮报告：`Implementation-v0.40-RT06-Round2.md`  
> 判定：**PASS**

## 一、重审目标

定向核验 Round 2 阻断：`0x0101` generation-scoped 写入 replay window 与 `0x0104` 独立 bounded in-flight 域，必须保证写窗口满载时仍可读取完整快照，完成即释放，不调用 mutation、不增加 revision，并且不得改变 Round 2 已冻结的 wire schema。

本审计未编辑实施就绪包或共享契约文件。

## 二、阻断修复核验

| Requirement | Result | Evidence |
| --- | --- | --- |
| 写入 replay window 独立 | PASS | `0x0101` 使用当前 `ConnectionGeneration` 的容量 128 replay window；切代清理；满载只拒绝新唯一写请求 |
| `0x0104` 不被写窗口满载阻断 | PASS | 规范明确不占用、不查询 `0x0101` replay window |
| `0x0104` 自身有界 | PASS | 当前 generation 独立 in-flight 域，V1 容量 16；满载仅限频拒绝新的并发请求 |
| 完成即释放 | PASS | 快照响应写入 transport queue 后立即释放槽位；历史 RequestId 不会造成永久满载 |
| 同 RequestId 并发 | PASS | 合并或拒绝重入；不会创建多个并发 mutation 路径 |
| 完成后相同 RequestId | PASS | 可再次执行并返回当时的当前完整快照；符合只读查询语义 |
| connection generation 隔离 | PASS | 连接切代清空整个 in-flight 域，不跨代重放 |
| 无 mutation | PASS | `0x0104` 明确不调用 mutation；只读取当前发送者可见的单功能/作用域快照 |
| 无 revision 增量 | PASS | 明确不增加 revision；与 RT-01/GPT-11 一致 |
| 写窗口满载后的最终收敛 | PASS | 新 `0x0104` 仍可进入独立域并返回完整快照；8 秒恢复通道重新可达 |
| 生产验证义务 | PASS | 已新增“写窗口满载仍完成快照、完成后释放槽位”的 network/settings integration 门禁 |

Round 2 的唯一阻断已经关闭，没有通过淘汰写 replay 记录或重新开放旧写重放窗口来换取恢复能力。

## 三、wire schema 回归核验

Round 3 只改变服务端内部请求状态域，不改变任何线路字节：

- Contract envelope 仍为固定 10 bytes。
- `0x0004` payload 仍精确 48 bytes。
- Ready 后 `0x0101`～`0x0104`、网络 `0x0201` 仍使用相同 52-byte fence prefix。
- 字段顺序、宽度、little-endian、双 16-byte nonce、Flags/Reserved 约束均未变化。
- BUEB/Contract decoder 分流、capability `betterunturned.ready-frame-fence.v1` 和旧端静默降级均未变化。
- `0x0104` 仍使用 RT-01 冻结的 message-specific payload 和新的非零 RequestId。

因此未引入新的 wire major/minor 或双重 payload 变体。

## 四、三轮门禁总结

| Round | Result | Resolution |
| --- | --- | --- |
| Round 1 | FAIL | 缺少精确 Ready-frame wire schema |
| Round 2 | FAIL | schema 已补齐，但共享 replay window 封死 `0x0104` 恢复 |
| Round 3 | PASS | 独立、generation-scoped、有界 in-flight 域恢复只读收敛且不修改 wire |

## 五、非阻断边界

1. 本 PASS 是实施规格审计，不是 codec、transport、SettingsRuntime 或三环境运行 PASS。
2. `0x0104` 槽位在“响应写入 transport queue”后释放，不保证网络送达；但客户端可按同 RequestId/新请求重试只读查询，不影响权威 revision。
3. 生产实现仍需证明 in-flight 登记、响应排队和释放在受控执行上下文中具备确定性清理，异常路径不得泄漏槽位。
4. Gemini 最终消费确认与人工生产授权仍是 RT-06 的外部门禁，本审计不代替二者。

## 六、最终结论

Round 2 阻断已完整关闭：写 replay window 满载不会封死 `0x0104`，只读请求有独立 generation-scoped bounded in-flight 域，完成释放且不产生 mutation/revision；wire schema 保持不变。按三轮独立审计门禁，RT-06 实施就绪包最终判定 **PASS**。
