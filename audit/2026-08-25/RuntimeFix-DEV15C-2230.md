# DEV-15C 缺陷修复执行报告

## 一、问题定位与修复策略

- **TOCTOU**：Pump 读取 binding 后释放同步边界，切代可能让旧快照调用 consumer；用 `pumpSync` 将 Pump 与 Bind/Invalidate 线性化。
- **并发乱序**：多个 Pump 可同时消费；同一 `pumpSync` 保证 FIFO 和单消费者。
- **异常继续消费**：consumer 异常后原实现仅计数；现在立即 Invalidate、清空队列并停止本次 Pump。
- **指纹语义**：规格要求歧义投影跟随最新原生事实；工单改为“失配不完成 ACK，可走 latest-fact 观察”。

## 二、核心变更

- `InventoryProjectionRelay.cs`：新增 `INativeInventoryProjectionSource`、绑定参数非零校验、Pump 串行锁、consumer 异常隔离、NativeRevision 单调过滤。
- `Dev15CTests.cs`：新增 consumer 异常清理、旧 NativeRevision 丢弃测试。
- DEV-15C 工单：恢复未验收状态，记录修复义务。

## 三、编译与自测

- Release：0 errors / 0 warnings。
- 7/7 测试：PASS。
- ClientUi token scan：PASS（9 files）。

## 四、最终结论

修复已完成，等待新的 GPT 独立审计与 Gemini 消费复核；当前不关闭 DEV-15C。
