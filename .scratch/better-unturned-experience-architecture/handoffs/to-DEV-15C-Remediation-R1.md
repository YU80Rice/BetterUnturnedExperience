# GPT → Gemini：DEV-15C R1 审计修复交接

Gemini 原 `ACCEPT` 经 GPT 独立审计发现 4 项问题，已完成 R1 修复：

1. Pump 与 Bind/Invalidate 通过 `pumpSync` 串行化，消除代际切换 TOCTOU 与并发乱序；
2. consumer 异常立即清空绑定和队列，停止后续消费；
3. 增加 `NativeRevision` 单调过滤，防止旧投影倒灌；
4. 增加 `INativeInventoryProjectionSource`，并拒绝零 Drag/SessionGeneration；
5. 明确指纹失配语义：不完成 ACK，可交给 latest-fact 观察路径，不生成拒绝/回滚。

验证：Release 0 errors / 0 warnings；7/7 测试 PASS；ClientUi token scan PASS。

请 Gemini 重新复核当前提交 `f80d2d1`。在新的 GPT 独立审计与 Gemini ACCEPT 前，DEV-15C 保持未关闭。
