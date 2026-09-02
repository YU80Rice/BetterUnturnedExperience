# Runtime Verbosity Walk — Better Unturned Experience (Plugin project)

Date: 2026-09-02 · Mode: read-only codebase exploration (no files modified)

## User policy (restated verbatim intent)

> 能不能在启动游戏、注入插件的每个阶段和阶段正常，就播报正常加载，有错误就播报哪里出错了；然后进入游戏就不用再具体播报，只需要出错时再打印错误原因就行？

Mapped to three buckets:

| Bucket | Meaning | Policy |
|---|---|---|
| `LOAD_ONE_SHOT` | 加载/注入阶段 one-shot: plugin boot, wiring enable/disable, hooks-installed, surface dispatch, patches | **KEEP always** — announce "正常加载" per stage; if a stage errors, that line belongs to ERROR_ALWAYS |
| `RUNTIME_RECURRING` | 游戏内运行时「正常」事件, no info value to end users: drag-started, drag-cancelled, placement-decision, placement-passthrough, native-inventory-snapshot, inventory-events-subscribed, placed-item-delegate-rebound, GPT-WATERMARK preview-*/readout/evaluated/visible | **SILENT during normal play** — gate behind a debug flag / throttle to error-only / remove |
| `ERROR_ALWAYS` | 错误/隔离: ReportCleanupIncomplete, ReportCleanupFailure, EmitDiagnosticOnce, BootstrapFailed, wiring-disabled diagnostics, any Isolate/exception path | **Always print + carry the reason** |

**Scope note:** `BueManagementPanelRuntime.cs` (51 lines) contains **zero** direct log emissions — all panel logging lives in `BueNativeManagementPanel.cs`. The whole `BetterUnturnedExperience.ClientUi` project emits **no** logs at all (verified by grep); every log line in this system is emitted from the Plugin project files walked below.

---

## Master table — every production log emission

Prefixes (exact):
- `[BUE-UI-TRACE]` → plugin boot / management panel (diagnosticId `BUE-MANAGEMENT-TRACE-001/002/003`, `BUE-BOOTSTRAP-*`, `BUE-CLIENTUI-*`)
- `[BUE-INVENTORY]` → lifecycle adapter (`BUE-INVENTORY-001/003/004/005`)
- `[BUE-DRAG]` → drag adapter (`BUE-DRAG-001/002/003`) and GPT-WATERMARK lines
- `BUE no-op fixture` → NoOp fixture

Legend — gate column:
- `once` = fires exactly once (a `_logged` bool / first-call guard)
- `state-transition` = only on readiness/state change (steady state silent)
- `500ms` = `ShouldEmitDiagnostic` throttle (state/reason change OR ≥500ms)
- `%120` = `count == 1 || count % 120 == 0` heartbeat throttle
- `per-drag` = once per drag (rising/falling edge)
- `per-placement` = once per `onPlacedItem` invocation (NOT throttled)
- `per-event` = once per native inventory add/remove/update event (NOT throttled)

### A. BetterUnturnedExperiencePlugin.cs

| # | Line | event= / prefix | Bucket | Gate | Notes |
|---|---|---|---|---|---|
| 1 | 52 | `[BUE-UI-TRACE] … BUE-BOOTSTRAP-002 event=runtime-gate` | LOAD_ONE_SHOT | once (Awake) | boot gate decision |
| 2 | 63 | `BUE client UI composition unavailable BUE-CLIENTUI-001` (Warning) | ERROR_ALWAYS | once | wiring-disabled boot diagnostic; carries no detail beyond "unavailable" — add reason |
| 3 | 112 | `BUE inventory lifecycle wiring enabled BUE-INVENTORY-001` | LOAD_ONE_SHOT | once | wiring enabled |
| 4 | 114 | `BUE inventory lifecycle wiring disabled BUE-INVENTORY-003 diagnostics=` (Warning) | ERROR_ALWAYS | once | wiring-disabled; carries `GateDiagnostics` reason ✓ |
| 5 | 125 | `BUE drag preview wiring disabled because inventory lifecycle is unavailable BUE-DRAG-003` (Warning) | ERROR_ALWAYS | once | wiring-disabled; reason implicit in text |
| 6 | 128–130 | `BUE drag preview wiring enabled|disabled BUE-DRAG-001/003` (ternary LogInfo) | enabled→LOAD_ONE_SHOT; disabled→ERROR_ALWAYS | once | disabled branch carries `GateDiagnostics` ✓ |
| 7 | 131 | `BUE client UI composition ready BUE-CLIENTUI-002` | LOAD_ONE_SHOT | once | |
| 8 | 136 | `Better Unturned Experience … status=BootstrapReady BUE-BOOTSTRAP-001` | LOAD_ONE_SHOT | once | |
| 9 | 137 | `Better Item Interaction featureId=… accepted=… reason=…` | LOAD_ONE_SHOT | once | registration result |
| 10 | 141 | `… status=BootstrapFailed BUE-BOOTSTRAP-001` (LogError) | ERROR_ALWAYS | once (catch) | carries `errorType` only — add message for "哪里出错了" |
| 11 | 147 | `[BUE-UI-TRACE] BUE-MANAGEMENT-TRACE-002 event=start-entered` | LOAD_ONE_SHOT | once (Start) | |
| 12 | 159 | `[BUE-UI-TRACE] BUE-MANAGEMENT-TRACE-003 event=runtime-pump-created` | LOAD_ONE_SHOT | once | |
| 13 | 164 | `… event=runtime-pump-create-failed` (Warning) | ERROR_ALWAYS | once (catch) | carries errorType only |
| 14 | 187 | `… event=runtime-pump-tick count=1` | LOAD_ONE_SHOT | `%120` (fires at count==1) | one-shot pump start confirmation |
| 15 | 201 | `… event=runtime-pump-failed` (Warning) | ERROR_ALWAYS | exception path | carries errorType only |
| 16 | 202 | `… event=runtime-pump-isolated fallback=NativeUi` (Warning) | ERROR_ALWAYS | isolation path | |
| 17 | 221 | `[BUE-UI-TRACE] BUE-MANAGEMENT-TRACE-001 event=plugin-update count=` | RUNTIME_RECURRING | `%120` | heartbeat every 120 frames — silent in normal play |
| 18 | 240 | `Better Unturned Experience … status=RuntimeReady BUE-BOOTSTRAP-003` | LOAD_ONE_SHOT | once | completion barrier |
| 19 | 245 | `… status=RuntimeCompletionIsolated decision=Isolate` (LogError) | ERROR_ALWAYS | exception path | carries errorType only |
| 20 | 266 | `BUE client UI refresh after completion isolated BUE-CLIENTUI-004` (Warning) | ERROR_ALWAYS | exception path | carries errorType only |
| 21 | 291 | `[BUE-UI-TRACE] BUE-MANAGEMENT-TRACE-002 event=assembly-identity` | LOAD_ONE_SHOT | once | |
| 22 | 295 | `… event=assembly-identity-failed` (Warning) | ERROR_ALWAYS | exception path | carries errorType only |
| 23 | 319 | `… event=host-destroyed state=preserved patches-kept=true BUE-CLIENTUI-005` (**Warning**) | LOAD_ONE_SHOT | once per host sweep | benign condition logged as Warning → severity mismatch (should be Info); silent OK |
| 24 | 323 | `… event=host-destroyed-preserve-failed` (Warning) | ERROR_ALWAYS | exception path | |
| 25 | 340 | `BUE client UI teardown isolated BUE-CLIENTUI-003` (Warning) | ERROR_ALWAYS | exception path | |

### B. BueNativeManagementPanel.cs (all via `LogTrace` → `[BUE-UI-TRACE] … BUE-MANAGEMENT-TRACE-001 event=<name>`)

| # | Line | event= | Bucket | Gate | Notes |
|---|---|---|---|---|---|
| 26 | 158 | `constructed` | LOAD_ONE_SHOT | once | ctor |
| 27 | 166 | `initialize-complete` | LOAD_ONE_SHOT | once | |
| 28 | 179 | `tick-isolated` | ERROR_ALWAYS | isolation path | carries errorType+message ✓ |
| 29 | 209 | `heartbeat source=Update` | RUNTIME_RECURRING | `%120` | panel heartbeat every 120 frames — silent |
| 30 | 215 | `host-ui-tick source=VanillaUiUpdate` | LOAD_ONE_SHOT | once (`hostUiTickLogged`) | |
| 31 | 220 | `first-tick` | LOAD_ONE_SHOT | once (`firstTickLogged`) | |
| 32 | 241 | `warning reasonCode=ManagementFailure unpatch failed…` | ERROR_ALWAYS | exception path | via `Log()` |
| 33 | 262 | `patch-installed target=MenuUI.escapeMenu` | LOAD_ONE_SHOT | once | |
| 34 | 268 | `patch-installed target=PlayerUI.escapeMenu` | LOAD_ONE_SHOT | once | |
| 35 | 274 | `patch-installed target=MenuUI.closeAll` | LOAD_ONE_SHOT | once | |
| 36 | 279 | `warning reasonCode=ManagementFailure UI rebuild hook unavailable…` | ERROR_ALWAYS | wiring-degraded | carries message ✓ |
| 37 | 287 | `patch-missing target=<name>` | ERROR_ALWAYS | wiring-degraded (once per missing target) | carries target ✓ — missing native member |
| 38 | 293 | `patch-installed target=<name>` (generic `PatchPostfix`) | LOAD_ONE_SHOT | once | |
| 39 | 297 | `patch-failed target=<name> errorType=… message=…` | ERROR_ALWAYS | exception path | carries reason ✓ |
| 40 | 305 | `constructor-postfix source=Harmony` | LOAD_ONE_SHOT | per UI rebuild | surface dispatch (keep) |
| 41 | 316 | `surface-opened source=Harmony` | LOAD_ONE_SHOT | per open | surface dispatch (keep) |
| 42 | 351 | `create-button-begin surface=MenuDashboardUI` | LOAD_ONE_SHOT | per rebind | |
| 43 | 370 | `add-child-success surface=MenuDashboardUI` | LOAD_ONE_SHOT | per rebind | |
| 44 | 375 | `entry-failed surface=MenuDashboardUI …` | ERROR_ALWAYS | exception path | carries errorType+message ✓ |
| 45 | 413 | `container-state surface=MenuWorkshopUI …` | LOAD_ONE_SHOT | state-transition (`lastMainContainerState`) | |
| 46 | 432 | `create-button-begin surface=MenuWorkshopUI` | LOAD_ONE_SHOT | per rebind | |
| 47 | 434 | `create-button-result surface=MenuWorkshopUI` | LOAD_ONE_SHOT | per rebind | |
| 48 | 448 | `add-child-success surface=MenuWorkshopUI` | LOAD_ONE_SHOT | per rebind | |
| 49 | 453 | `entry-failed surface=MenuWorkshopUI …` | ERROR_ALWAYS | exception path | ✓ |
| 50 | 454 | `warning … workshop menu entry failed: …` | ERROR_ALWAYS | exception path | via `Log()`; message ✓ |
| 51 | 470 | `container-state surface=PlayerPauseUI …` | LOAD_ONE_SHOT | state-transition (`lastPauseContainerState`) | |
| 52 | 489 | `create-button-begin surface=PlayerPauseUI` | LOAD_ONE_SHOT | per rebind | |
| 53 | 491 | `create-button-result surface=PlayerPauseUI` | LOAD_ONE_SHOT | per rebind | |
| 54 | 503 | `add-child-success surface=PlayerPauseUI` | LOAD_ONE_SHOT | per rebind | |
| 55 | 508 | `entry-failed surface=PlayerPauseUI …` | ERROR_ALWAYS | exception path | ✓ |
| 56 | 509 | `warning … pause menu entry failed: …` | ERROR_ALWAYS | exception path | via `Log()` |
| 57 | 534 | `warning … open management panel failed: …` | ERROR_ALWAYS | exception path | via `Log()` |
| 58 | 894 | `warning … close management panel failed: …` | ERROR_ALWAYS | exception path | via `Log()` |
| 59 | 895 | `warning … restore management origin failed: …` | ERROR_ALWAYS | exception path | via `Log()` |
| 60 | 961 | `entry-isolated surface=… errorType=… message=…` (`LogEntryFailure`) | ERROR_ALWAYS | exception path | ✓ |

### C. InventoryDragPreviewAdapter.cs

| # | Line | event= / prefix | Bucket | Gate | Notes |
|---|---|---|---|---|---|
| 61 | 117 | `[BUE-DRAG] event=diagnostic-failure … BUE-DRAG-003` (Warning) | ERROR_ALWAYS | per failure stage (EmitDiagnosticOnce) | sink for ReportCleanupFailure/Incomplete |
| 62 | 118 | `[BUE-DRAG] event=hooks-installed targets=updateDraggedItem BUE-DRAG-001` | LOAD_ONE_SHOT | once | hooks-installed (keep) |
| 63 | 188 | `[BUE-DRAG] event=attach-grid-failed BUE-DRAG-003` (Warning) | ERROR_ALWAYS | exception path | reason lives in `LastPollDiagnostics` |
| 64 | 218 | `[BUE-DRAG] event=placed-item-delegate-rebound page=N BUE-DRAG-001` | RUNTIME_RECURRING | per dispatch | user-listed silent bucket |
| 65 | 224 | `[BUE-DRAG] event=attach-grid-failed BUE-DRAG-003` (Warning) | ERROR_ALWAYS | exception path | |
| 66 | 643 | `[BUE-DRAG] event=drag-started …` | RUNTIME_RECURRING | per-drag (rising edge) | user-listed |
| 67 | 653 | `[BUE-DRAG] event=drag-cancelled` | RUNTIME_RECURRING | per-drag (falling edge) | user-listed |
| 68 | 672 | `[BUE-DRAG] GPT-WATERMARK event=preview-hidden reason=outside-viewport` | RUNTIME_RECURRING | 500ms (`ShouldEmitDiagnostic`) | user-listed GPT-WATERMARK |
| 69 | 680 | `[BUE-DRAG] GPT-WATERMARK event=preview-hidden reason=surface-not-native` | RUNTIME_RECURRING | 500ms | |
| 70 | 698 | `[BUE-DRAG] GPT-WATERMARK event=preview-evaluated` | RUNTIME_RECURRING | 500ms | |
| 71 | 700 | `[BUE-DRAG] GPT-WATERMARK event=preview-visible` | RUNTIME_RECURRING | 500ms | |
| 72 | 707 | `[BUE-DRAG] GPT-WATERMARK event=preview-input-rejected` | RUNTIME_RECURRING | 500ms | |
| 73 | 736 | `[BUE-DRAG] GPT-WATERMARK event=preview-input-readout …` (`LogPreviewInputReadout`) | RUNTIME_RECURRING | 500ms (inherits `ShouldEmitDiagnostic`) | verbose coordinate readout |
| 74 | 769 | `[BUE-DRAG] event=inventory-events-subscribed` | RUNTIME_RECURRING | per player change | user-listed |
| 75 | 824 | `[BUE-DRAG] event=placement-passthrough reason=enhanced-off` | RUNTIME_RECURRING | per-placement (**no throttle**) | user-listed |
| 76 | 830 | `[BUE-DRAG] event=placement-passthrough reason=preview-stale` | RUNTIME_RECURRING | per-placement (**no throttle**) | |
| 77 | 842 | `[BUE-DRAG] event=placement-passthrough reason=native-swap` | RUNTIME_RECURRING | per-placement (**no throttle**) | |
| 78 | 850 | `[BUE-DRAG] event=placement-decision page=… outcome=…` | RUNTIME_RECURRING | per-placement (**no throttle**) | user-listed |

Error seams (no direct line but the code path that emits): `ReportCleanupFailure` (449), `ReportCleanupIncomplete` (457), `EmitDiagnosticOnce` (471) → **ERROR_ALWAYS** (all route through the L117 `DiagnosticLogSink` and are already one-line-per-failure-stage).

### D. InventoryProjectionSink.cs

| # | Line | event= / prefix | Bucket | Gate | Notes |
|---|---|---|---|---|---|
| 79 | 23 | `[BUE-DRAG] event=projection-submitted BUE-DRAG-002` (**Warning**) | RUNTIME_RECURRING | per submit | normal-flow event mislabeled as Warning → demote to Debug/Info |
| 80 | 30 | `[BUE-DRAG] event=native-inventory-snapshot revision=N BUE-DRAG-002` | RUNTIME_RECURRING | per-event (**no throttle**) | user-listed; fires on every native add/remove/update during a drag — potentially high frequency |
| 81 | 37 | `[BUE-DRAG] event=projection-timed-out BUE-DRAG-002` (**Warning**) | ERROR_ALWAYS | once per timeout | abnormal condition — keep; add reason detail |

### E. InventorySurfaceLifecycleAdapter.cs

| # | Line | event= / prefix | Bucket | Gate | Notes |
|---|---|---|---|---|---|
| 82 | 969 | `[BUE-INVENTORY] event=diagnostic-failure … BUE-INVENTORY-003` (Warning) | ERROR_ALWAYS | per failure stage | sink for `EmitDiagnosticOnce` |
| 83 | 970 | `[BUE-INVENTORY] event=polling-hook-installed target=PlayerUI.Update BUE-INVENTORY-001` | LOAD_ONE_SHOT | once | hooks-installed (keep) |
| 84 | 1226 | `[BUE-INVENTORY] event=surface-not-ready page=N reason=<reason> BUE-INVENTORY-004` (gateLine) | ERROR_ALWAYS | state-transition (SurfaceReadinessGate) | carries reason ✓; steady state silent |
| 85 | 1233 | `[BUE-INVENTORY] event=surface-ready page=N BUE-INVENTORY-001` (readyLine) | LOAD_ONE_SHOT | state-transition | surface dispatch (keep) |
| 86 | 1240 | `[BUE-INVENTORY] event=surface-context-dispatched kind=… page=… generation=…` | LOAD_ONE_SHOT | once per page per session | surface dispatch (keep); geometry calibration readout |
| 87 | 1265 | `[BUE-INVENTORY] event=surface-discarded reason=… BUE-INVENTORY-005` | LOAD_ONE_SHOT | per discard (per page) | surface dispatch teardown (keep); reasons: session-closed / native-surface-rebuilt / native-hierarchy-unavailable / native-hierarchy-incompatible |

### F. NoOpFeaturePlugin.cs

| # | Line | event= / prefix | Bucket | Gate | Notes |
|---|---|---|---|---|---|
| 88 | 16 | `BUE no-op fixture featureId=… accepted=… reason=… diagnosticId=…` | LOAD_ONE_SHOT | once (Awake) | registration one-shot (fixture) |

---

## Bucket counts

| Bucket | Count |
|---|---|
| `LOAD_ONE_SHOT` (keep always) | **38** |
| `RUNTIME_RECURRING` (silent in normal play) | **18** |
| `ERROR_ALWAYS` (always print + reason) | **36** |
| **Total emission sites** | **92** |

(Counts include the 3 drag-adapter failure seams [#61 via L117 + ReportCleanup*/EmitDiagnosticOnce] and split the ternary at Plugin L128–130 across both buckets. 18 recurring sites is the set to silence; of those, 6 are GPT-WATERMARK preview lines, 4 are placement lines, 2 projection lines, 2 heartbeats, and 1 each for drag-started/cancelled, rebound, subscribed.)

---

## Deep dive 1 — GPT-WATERMARK throttle mechanics (`InventoryDragPreviewAdapter`)

**`ShouldEmitDiagnostic(state, reason)` (lines 712–724)** — the single choke point for all six preview lines:
- Emits iff **any** of: (a) never emitted before (`!hasDiagnostic`), (b) `state` or `reason` changed since the last emission, (c) ≥500 ms elapsed since the last emission (same `state`+`reason` tuple, `Environment.TickCount`).
- Bound: **≤ 2 lines/sec per (state, reason) tuple**. Different tuples (e.g. `Hidden/OutsideGrid` vs `Candidate`) can interleave at the 500 ms cadence.
- Used at L671, L679, L694, L706 to gate: `preview-hidden outside-viewport` (672), `preview-hidden surface-not-native` (680), `preview-evaluated` (698), `preview-visible` (700), `preview-input-rejected` (707).

**`LogPreviewInputReadout` (726–747)** — the verbose coordinate dump (`pointerScreen`, `uiScale`, `uiCoordinates`, `viewportOrigin`, `pointerGrid`, `grabOffset`, `placementReason`, `state`). It is invoked **only inside** the `if (ShouldEmitDiagnostic(state, component.LastPreview.Reason))` block (L694–701), so it **inherits the same 500 ms / state-change throttle**. It never emits on its own.

**GPT-WATERMARK block (660–708)** — structure:
- `TrySelectTargetSurface` fails → `preview-hidden reason=outside-viewport` (gated).
- non-native surface → `preview-hidden reason=surface-not-native` (gated).
- `TryCreatePreviewInput` ok → `preview-evaluated` (+ `preview-visible` when Candidate/LocallyInvalid) (gated).
- else → `preview-input-rejected` (gated).

**Can they move behind a debug switch? Yes — cleanly.** Because `ShouldEmitDiagnostic` is the single gate for all six, adding one verbosity check at the top of `ShouldEmitDiagnostic` (e.g. `if (!RuntimeVerbose) return false;`) silences every preview line at once. The error path (`DiagnosticLogSink`, `attach-grid-failed`, `ReportCleanup*`) is a **separate** static seam and is unaffected.

---

## Deep dive 2 — per-drag / per-frame / per-session emissions in the Poll methods

### `InventorySurfaceLifecycleAdapter.Poll` (1153–1249) — per-frame watcher
- **Steady state (inventory open, surfaces dispatched & ready): SILENT.** Every frame that hits `continue` (L1174, L1206, L1208) emits nothing; `watcher.Feed` (1166) logs nothing.
- **Per state transition** (per page): `surface-not-ready` (1226) / `surface-ready` (1233) — `SurfaceReadinessGate` (821–872) seeds on first observation without emitting, then emits **one line per transition**; a stable state is silent every frame.
- **Per dispatch** (once per page per session): `surface-context-dispatched` (1240).
- **Per discard** (once per discarded page): `surface-discarded` (1265), e.g. on session-close, native-surface-rebuilt, native-hierarchy-incompatible.
- Conclusion: lifecycle Poll is **not** per-frame chatty already; the only recurring candidates are the transition lines, which are transition-gated.

### `InventoryDragPreviewAdapter.Poll` (630–710) — per-drag/per-frame
- `drag-started` (643): rising edge of `isDragging` — once per drag.
- `drag-cancelled` (653): falling edge — once per drag (ESC / drag-out).
- `inventory-events-subscribed` (769, inside `EnsureInventoryEventSubscription` 759–770): once per `Player.LocalPlayer` change.
- GPT-WATERMARK preview lines (672/680/698/700/707/736): ≤1 per 500 ms per (state, reason).
- `placement-passthrough` (824/830/842) and `placement-decision` (850): **once per `onPlacedItem` invocation — NOT throttled**; rapid placing can emit several per second.
- (Adjacent, not in Poll: `native-inventory-snapshot` (ProjectionSink L30) fires per native inventory add/remove/update while a drag is active — potentially the highest-frequency recurring emission.)

---

## Deep dive 3 — recommended seam for a runtime verbosity gate (normal vs verbose-debug)

**Primary recommendation — use BepInEx's own log-level filter (zero new state, settings-driven):**
Demote every `RUNTIME_RECURRING` emission from `LogInfo` → `LogDebug` (sites #17, #29, #64, #66–78, #79, #80). BepInEx `ManualLogSource` respects the `[Logging] Levels` config (default `Fatal,Error,Warning,Message,Info`, **Debug excluded**), so:
- Normal play: all 18 recurring sites are silent.
- Verbose debugging: flip `Levels = Debug` in `BepInEx.cfg` (or a debug build) and they return.
- `LOAD_ONE_SHOT` stays `LogInfo`, `ERROR_ALWAYS` stays `LogWarning`/`LogError` — both bypass the filter exactly as the policy requires.
This is literally the "normal vs verbose-debug" axis the user asked for, reusing a mechanism that already exists and is config-driven. The per-site change is a one-word edit (`LogInfo`→`LogDebug`).

**Alternative — BUE-owned static flag (if an in-game toggle is wanted):**
A single static gate, e.g. `internal static bool RuntimeVerbose` on a small `BueRuntimeLog`/`RuntimeVerbosity` static class, set from the settings runtime (management panel). Two integration points keep it clean:
1. **`ShouldEmitDiagnostic`** (drag adapter, 712) — add `if (!RuntimeVerbose) return false;` at the top → silences all 6 GPT-WATERMARK lines in one place.
2. **Per-site guards** for the non-preview recurring lines (`if (RuntimeVerbose) log?.LogDebug(...)`), or better, route them through a tiny helper `log?.LogDebug` only after the flag.

**Best combined shape:** use `LogDebug` as the mechanism **and** add the static `RuntimeVerbose` flag as an override that forces `LogDebug` lines visible even without editing `BepInEx.cfg` (debug toggle in the management panel). Note the existing seams that already do this job partially and should be preserved: `DiagnosticLogSink` (failure-only, static, test-swappable) and `ShouldEmitDiagnostic` (preview throttle) — the new verbosity gate sits **above** them and must **never** swallow ERROR_ALWAYS output.

---

## Deep dive 4 — Warnings that would be confusing if silent

| Site | Current severity | Verdict |
|---|---|---|
| `event=projection-timed-out` (ProjectionSink L37) | Warning | **Keep loud (ERROR_ALWAYS)** — a real abnormal condition (native convergence timed out). If silenced, the user cannot tell placement confirmation failed. Currently carries only `diagnosticId`, no detail → add the reason (e.g. generation/revision). |
| `event=host-destroyed state=preserved patches-kept=true` (Plugin L319) | Warning | **Severity mismatch, benign.** State is *preserved* — this is normal. Should be Info, not Warning; silencing it is fine (not confusing). |
| `event=projection-submitted` (ProjectionSink L23) | Warning | **Severity mismatch, normal flow.** This is the expected "projection submitted" step; it belongs in RUNTIME_RECURRING (Debug), not Warning. Silencing is fine. |
| `BUE client UI composition unavailable` (L63), `lifecycle wiring disabled` (L114), `drag preview wiring disabled` (L125/130) | Warning | Correct to keep loud (ERROR_ALWAYS, boot-phase wiring-disabled) — these tell the user *why* a stage did not come up. |
| Panel `Log()` sites (L241/279/454/509/534/894/895/961) | **Info** (via `LogTrace("warning", …)`) | Already silent-in-normal-play safe; they are exception paths → keep (ERROR_ALWAYS). |

The only "must stay loud or users get confused" warning is **`projection-timed-out`**; the two severity mismatches (`host-destroyed preserved`, `projection-submitted`) are safe to silence but should be re-leveled to Info/Debug for honesty of the log.
