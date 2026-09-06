# PT-RT05-001B：LMN connection-bound receive token 最小代码 spike

**Owner:** GPT  
**Status:** resolved  
**SourceSet:** `BUE-SS-20260824-02`  
**Type:** throwaway prototype；禁止进入生产程序集

## Question

LMN 是否能在不留下 revoke/dequeue 竞态的前提下，为排队 callback 捕获连接绑定 token，并在 disconnect 后保证旧 callback 无法进入消费者 mutation？

## Acceptance

- 使用与 LMN dispatcher/session 顺序相同的最小并发模型。
- 覆盖 callback 先出队、revoke 先完成、二者交错、token reuse、同 SteamID 新连接。
- 明确线性化点和 token 生命周期所有者。
- 不以普通 bool 或当前 SteamID lookup 假装不可伪造 capability。
- 若必须修改 LMN API，列出最小 API diff、兼容影响和新 SourceSet 义务。
- 输出执行日志、原型 hash、结论与残余风险。
- 独立审计 PASS 后才可作为 SCR 选型证据。

## Non-goals

- 不把 LMN 升格为认证、授权或完整协议生命周期。
- 不修改稳定发布 DLL。

## Answer

GPT 已按 TDD 完成一次性 spike，未修改 LMN 或任何稳定发布 DLL。第 1 轮独立审计为 FAIL；三项阻断现已修复。模型覆盖 revoke-before-dequeue、dequeue-before-revoke、真实并发下已准入 mutation 与 revoke、同 SteamID current map 安全重连全序、late-revoke 反例、撤销后 token reuse；Release build 为 `0 warnings / 0 errors`，主测试 `12/12 PASS`，独立 consumer probe `1/1 PASS`。

原型表明方案 B 只有在以下条件同时成立时才封闭竞态：opaque 引用 capability 使用 admission counter；consumer Action 在 manager/capability 锁外执行；`ConnectionSessionManager` 在 manager lock 内先 `CloseAdmission` 再移除 current mapping，并在 manager lock 外等待 quiescence；consumer context 不暴露 revoke。普通 bool、SteamID current lookup、RequestId 或 generation 数字均不能替代该 capability。

结果、TDD 日志、最小 LMN API diff、SourceSet successor 义务与残余风险见 `../PT-RT05-001B-LMN-Connection-Token-Spike.md`。第 2 轮独立审计 `PASS`：主原型 `12/12 PASS`，ConsumerProbe `1/1 PASS`，两者 Release build 均为 `0 warnings / 0 errors`，七项哈希全部匹配。本票仅作为 `PROTOTYPE_ONLY` 选型证据，不授权修改 LMN 或发布。

## Comments

- 2026-08-24 GPT：实现与自测完成，移交独立审计；未触碰用户现有 LMN 工作树改动。
- 2026-08-24 GPT：独立审计 Round 1 FAIL；已按 TDD 修复真实并发、锁分层和完整 current-map 时序三个阻断，并增加独立 consumer assembly 加固验证；等待 Round 2。
- 2026-08-24 GPT：独立审计 Round 2 `PASS`，报告 `../../../../audit/2026-08-24/Implementation-v0.38-2106.md`。

