# DEV-V2-19 R3 Spec 轴审查
## 总判词：NOT CLEAN

### DEVIATION-1：HostTick 回退后会产生错误的后续 DeltaTime

- **Spec 原文**：`.scratch/bue-v2-phase2-official-adoption/spec.md:190`：“序号单调、携带时间增量”；登记冻结面 `.scratch/bue-v2-phase2-official-adoption/spec.md:190` 同时要求 HostTick 携带“时间增量”。SDK 冻结定义进一步明确为 `DeltaTime`“相邻 tick 单调时差”（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:82`）。
- **Diff 证据**：`src/BetterUnturnedExperience.Core/Events/HostTickClock.cs:411-415` 先将负差 `diff` 钳为 0，却随后无条件执行 `lastMilliseconds = nowMs`。因此时间源依次为 100、10、20 时，第二个 tick 的增量为 0，但第三个 tick 会按 20−10 产生 10ms；基线已被回退，增量不再是单调时间的相邻差值。修复应保留不回退的时间基线（例如仅在 `nowMs` 更大时更新 `lastMilliseconds`），并补上回退后的下一 tick 断言。

