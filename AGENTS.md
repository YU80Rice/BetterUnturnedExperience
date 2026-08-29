## Agent skills

### Issue tracker

Issues and specs live as local Markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical five-role triage vocabulary is used. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` + `docs/adr/`). See `docs/agents/domain.md`.

## 产物输出循环审查规则（硬性要求，2026-08-29 用户确立）

每一轮输出产物（生产源码修改、测试、DLL 产物、审计报告）在正式提交/输出前，必须执行以下循环，直到**双轴审查均无阻断发现**才视为正式输出：

1. **TDD 先行**：在预约定 seam 上先写红测试，再最小实现转绿；无红测试不得修改生产代码。纯宿主无法构造测试 seam 时，必须在工单/审计中记录 seam 缺失，不得静默跳过。
2. **双轴独立审查**：按 `/code-review` 原生流程并行派出 Standards 与 Spec 两个独立上下文子代理，审查对象为当前轮增量 diff，每轴产出简报；禁止以单会话串行自审替代（上下文互相污染）。
3. **循环**：任一轴有发现 → 修复 → 修复后的增量**必须重新投入双轴审查**；循环直到 Standards 轴无硬违规且 Spec 轴无缺失/偏差。判断性 smell 须逐条显式记录为「可推迟/待裁决」，不得静默，不算阻断。
4. **闭合记录**：正式输出时在对应审计报告记录完整循环链（轮次、发现、修复、复审结论）；静态验证（构建/测试/静态门禁）是每轮基线，**不能替代**双轴审查。
5. **产物身份**：审查通过后才计算/记录新 DLL 的 SHA-256 与 CandidateBuild/CaseId；审查未通过期间产生的中间 DLL 不授予 CaseId。
