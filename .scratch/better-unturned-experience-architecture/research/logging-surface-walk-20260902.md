# BUE / Better Item Interaction — Log Surface Walk (2026-09-02)

Scope: production `src/BetterUnturnedExperience.Plugin/**/*.cs` + `src/BetterUnturnedExperience.ClientUi/**/*.cs`.
Goal: map every log emission so a logging-normalization refactor can keep only one-shot "xxx loaded / xxx failed, reason: yyy" diagnostics and drop/gate the per-frame / per-drag flood.

> Method note: only the **Plugin** project emits to the BepInEx log. The **ClientUi** project contains **zero** `Logger.*`/`log?.Log*` calls (verified by grep for `LogInfo|LogWarning|LogError|LogDebug|\.log\?\.|log\?\.|Logger\.` across all ClientUi `.cs` — no matches). All ClientUi observability is pumped back through plugin-side seams: `LoggingInventoryProjectionSink` (InventoryProjectionSink.cs) and `BueNativeManagementPanel.LogTrace`. `WeaverProtocol`/`ClientUiTypes` event counters also exist but do not write to the log directly.

---

## 1. Complete inventory of log emission sites

Prefix legend used below:
- `[BUE-UI-TRACE]` — management panel / plugin lifecycle trace channel (`event=...`)
- `[BUE-INVENTORY]` — inventory surface lifecycle adapter
- `[BUE-DRAG]` — drag preview / projection
- `GPT-WATERMARK` — sub-tag inside `[BUE-DRAG]` dragging per-frame readouts
- unprefixed — bootstrap/registration status lines

### 1a. BetterUnturnedExperiencePlugin.cs (bootstrap / lifecycle)

| File:Line | Level | Exact string prefix / token | Frequency class |
|---|---|---|---|
| :52 | Info | `[BUE-UI-TRACE] ... diagnosticId=BUE-BOOTSTRAP-002 event=runtime-gate decision=...` | ONCE per Awake (load) |
| :63 | Warn | `BUE client UI composition unavailable diagnosticId=BUE-CLIENTUI-001` | ONCE (one-shot failure) |
| :112 | Info | `BUE inventory lifecycle wiring enabled diagnosticId=BUE-INVENTORY-001` | ONCE (load) |
| :114 | Warn | `BUE inventory lifecycle wiring disabled diagnosticId=BUE-INVENTORY-003 diagnostics=...` | ONCE (one-shot failure w/ reason) |
| :125 | Warn | `BUE drag preview wiring disabled because inventory lifecycle is unavailable diagnosticId=BUE-DRAG-003` | ONCE (one-shot failure) |
| :128-130 | Info/Warn | `BUE drag preview wiring enabled/disabled diagnosticId=BUE-DRAG-001/-003 diagnostics=...` | ONCE (load) |
| :131 | Info | `BUE client UI composition ready featureId=... diagnosticId=BUE-CLIENTUI-002` | ONCE (load) |
| :136 | Info | `Better Unturned Experience ... status=BootstrapReady decision=... diagnosticId=BUE-BOOTSTRAP-001` | ONCE (load) |
| :137 | Info | `Better Item Interaction ... accepted=... reason=... diagnosticId=BUE-REG-ACCEPT` | ONCE (load) |
| :141 | Error | `Better Unturned Experience ... status=BootstrapFailed ... errorType=...` | ONCE (one-shot failure) |
| :147 | Info | `[BUE-UI-TRACE] ... BUE-MANAGEMENT-TRACE-002 event=start-entered` | ONCE (Start) |
| :159 | Info | `... BUE-MANAGEMENT-TRACE-003 event=runtime-pump-created` | ONCE |
| :164 | Warn | `... event=runtime-pump-create-failed errorType=...` | ONCE (one-shot failure) |
| :187 | Info | `... event=runtime-pump-tick count=1` | ONCE (first tick only) |
| :201-202 | Warn | `... event=runtime-pump-failed / event=runtime-pump-isolated fallback=NativeUi` | ONCE (event-driven isolation fail) |
| :221 | Info | `[BUE-UI-TRACE] ... BUE-MANAGEMENT-TRACE-001 event=plugin-update count=N` | REPEATS every 120 plugin-Updates (`count == 1 || count % 120 == 0`) |
| :240 | Info | `Better Unturned Experience ... status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003` | ONCE (completion) |
| :245 | Error | `... status=RuntimeCompletionIsolated decision=Isolate errorType=...` | ONCE (one-shot failure) |
| :266 | Warn | `BUE client UI refresh after completion isolated errorType=... BUE-CLIENTUI-004` | ONCE (one-shot failure) |
| :291 | Info | `... BUE-MANAGEMENT-TRACE-002 event=assembly-identity path=... sha256=...` | ONCE (load) |
| :295 | Warn | `... event=assembly-identity-failed errorType=...` | ONCE (one-shot failure) |
| :319 | Warn | `[BUE-UI-TRACE] ... event=host-destroyed state=preserved patches-kept=true BUE-CLIENTUI-005` | ONCE (host teardown, non-quit) |
| :323 | Warn | `... event=host-destroyed-preserve-failed errorType=...` | ONCE (one-shot failure) |
| :340 | Warn | `BUE client UI teardown isolated errorType=... BUE-CLIENTUI-003` | ONCE (quit teardown fail) |

### 1b. BueNativeManagementPanel.cs (`[BUE-UI-TRACE]` via `LogTrace`, all `diagnosticId=BUE-MANAGEMENT-TRACE-001`)

| File:Line | event= | Frequency class |
|---|---|---|
| :158 | `constructed` | ONCE |
| :166 | `initialize-complete` | ONCE |
| :179 | `tick-isolated` (errorType/message) | ONCE per failure (event-driven) |
| :209 | `heartbeat source=Update count=N` | REPEATS every 120 Updates (`count==1 || count%120==0`) — **parallel to plugin :221 `plugin-update`** |
| :215 | `host-ui-tick source=VanillaUiUpdate` | ONCE (first vanilla UI tick) |
| :220 | `first-tick source=...` | ONCE |
| :262/268/274 | `patch-installed target=MenuUI.escapeMenu/PlayerUI.escapeMenu/MenuUI.closeAll` | ONCE (load) |
| :287 | `patch-missing target=...` | ONCE (one-shot failure) |
| :293 | `patch-installed target=...` (4 x Postfix targets in PatchRebuildHooks) | ONCE (load) |
| :297 | `patch-failed target=... errorType=...` | ONCE per failure |
| :305 | `constructor-postfix source=Harmony` | ONCE per UI-rebuild (event-driven, rare) |
| :316 | `surface-opened source=Harmony` | ONCE per surface open (event-driven, rare) |
| :351/370/375 | `create-button-begin / add-child-success / entry-failed surface=MenuDashboardUI` | ONCE per menu-open (event-driven) |
| :413 | `container-state surface=MenuWorkshopUI ...` | ONCE per menu-open (event-driven) |
| :432/434/448/453 | `create-button-* / add-child-success / entry-failed surface=MenuWorkshopUI` | ONCE per menu-open |
| :470 | `container-state surface=PlayerPauseUI ...` | ONCE per menu-open |
| :489/491/503/508 | `create-button-* / add-child-success / entry-failed surface=PlayerPauseUI` | ONCE per menu-open |
| :956 | `warning reasonCode=ManagementFailure message=...` | ONCE per failure |
| :961 | `entry-isolated surface=... errorType=...` | ONCE per isolation failure |

→ All `BueNativeManagementPanel` emissions are one-shot-per-lifecycle-event. **None are per-frame** except `heartbeat`, which is throttled to 1 per 120 Updates and duplicates plugin :221.

### 1c. InventorySurfaceLifecycleAdapter.cs (`[BUE-INVENTORY]`, log via `log?.Log*`)

| File:Line | Exact string | Frequency class |
|---|---|---|
| :879 | `[BUE-INVENTORY] event=polling-hook-installed target=PlayerUI.Update diagnosticId=BUE-INVENTORY-001` | ONCE (load) |
| :1128 | `[BUE-INVENTORY] event=surface-not-ready page=<n> diagnosticId=BUE-INVENTORY-004` | **PER FRAME while dashboard open and surface build returns null** → **TOP SPAM SOURCE** |
| :1136 | `[BUE-INVENTORY] event=surface-context-dispatched kind=... page=... generation=... viewportOrigin=...` | ONCE per fresh session dispatch (per open/generation) — not per-frame. Verified in real log: only 2 emissions |
| :1161 | `[BUE-INVENTORY] event=surface-discarded reason=... BUE-INVENTORY-005` | ONCE per discard (event-driven) |
| :1230 | `[BUE-INVENTORY] event=surface-not-ready reason=native-hierarchy-incomplete page=... BUE-INVENTORY-004` | **PER FRAME** (same flood path as :1128, distinct reason) |
| :1255 | `[BUE-INVENTORY] event=surface-not-ready reason=scroll-viewport-not-laid-out page=... BUE-INVENTORY-004` | transient; throttled only by first-frames; usually stops after layout |

> All three `surface-not-ready` sites share **one logical diagnosticId `BUE-INVENTORY-004`** and are emitted from the same per-frame `Poll()` (driven by `PlayerUIUpdatePostfix` → `RunGuardedPoll` → `Poll`, runs every `PlayerUI.Update` frame). This is the dominant flood.

### 1d. InventoryDragPreviewAdapter.cs (`[BUE-DRAG]`, log via `log?.Log*`)

| File:Line | Exact string | Frequency class |
|---|---|---|
| :114 | `[BUE-DRAG] event=hooks-installed targets=updateDraggedItem diagnosticId=BUE-DRAG-001` | ONCE (load) |
| :184 | `[BUE-DRAG] event=attach-grid-failed diagnosticId=BUE-DRAG-003` | ONCE per attach failure (isolates) |
| :214 | `[BUE-DRAG] event=placed-item-delegate-rebound page=... diagnosticId=BUE-DRAG-001` | Once per fresh session dispatch (repeats each open); **duplicates `surface-context-dispatched` from 1c:1136** |
| :220 | `[BUE-DRAG] event=attach-grid-failed diagnosticId=BUE-DRAG-003` | ONCE per failure |
| :615 | `[BUE-DRAG] event=drag-started generation=... enhanced=...` | ONCE per drag start |
| :625 | `[BUE-DRAG] event=drag-cancelled diagnosticId=BUE-DRAG-001` | ONCE per drag cancel |
| :644 | `[BUE-DRAG] GPT-WATERMARK event=preview-hidden reason=outside-viewport generation=...` | **REPEATS while dragging** — throttled to ≤2/s by `ShouldEmitDiagnostic` (500ms cooldown + state-change) |
| :652 | `... GPT-WATERMARK event=preview-hidden reason=surface-not-native ...` | **REPEATS while dragging** (throttled) |
| :670 | `... GB` `event=preview-evaluated generation=... state=...` | **REPEATS while dragging** (throttled) — 32 in real log |
| :672 | `... event=preview-visible generation=... state=...` | **REPEATS while dragging** (throttled) — 29 in real log |
| :679 | `... event=preview-input-rejected generation=...` | **REPEATS while dragging** (throttled) |
| :708 | `... GPT-WATERMARK event=preview-input-readout ... pointerScreen=... uiCoordinates=...` (very verbose numeric readout) | **REPEATS while dragging** (throttled) — 32 in real log |
| :741 | `[BUE-DRAG] event=inventory-events-subscribed diagnosticId=BUE-DRAG-001` | ONCE per player subscribe/rebind (re-fires when `Player.LocalPlayer` changes; on rebind) |
| :796 | `[BUE-DRAG] event=placement-passthrough reason=enhanced-off ...` | ONCE per drag release (event-driven) |
| :802 | `... event=placement-passthrough reason=preview-stale previewGen=... dragGen=...` | ONCE per drag release |
| :814 | `... event=placement-passthrough reason=native-swap ...` | ONCE per drag release |
| :822 | `... event=placement-decision page=... x=... y=... outcome=...` | ONCE per drag release |

### 1e. InventoryProjectionSink.cs (`LoggingInventoryProjectionSink`, `[BUE-DRAG] ... BUE-DRAG-002`)

| File:Line | Exact string | Frequency class |
|---|---|---|
| :23 | `[BUE-DRAG] event=projection-submitted dragGeneration=... containerGeneration=...` (Warn) | ONCE per drag release with awaiting projection |
| :30 | `[BUE-DRAG] event=native-inventory-snapshot dragGeneration=... revision=...` | **REPEATS per native inventory event while a projection is awaiting** (18 in real log) |
| :37 | `[BUE-DRAG] event=projection-timed-out BUE-DRAG-002` (Warn) | ONCE per timeout (event-driven) |

---

## 2. Bucket classification

### Bucket A — One-shot lifecycle / load diagnostics (KEEP / normalize)

All of the following are the "loaded / failed, reason:" candidates:
- Bootstrap/registration: `runtime-gate`, `BootstrapReady` (:136), `BootstrapFailed` (:141), `accepted=... reason=...` (:137), `RuntimeReady` (:240), `RuntimeCompletionIsolated` (:245)
- Wiring one-shot: `composition ready` (:131), `composition unavailable` (:63), `lifecycle wiring enabled/disabled` (:112/:114), `drag preview wiring enabled/disabled` (:128-130), `teardown isolated` (:340)
- Panel one-shots (1b table): constructed/initialize-complete/patch-installed/patch-missing/patch-failed/container-state/create-button-*/add-child-success/entry-failed/host-ui-tick/first-tick
- Inventory one-shots: `polling-hook-installed` (:879), `surface-context-dispatched` (:1136), `surface-discarded` (:1161)
- Drag one-shots: `hooks-installed` (:114), `drag-started` (:615), `drag-cancelled` (:625), `inventory-events-subscribed` (:741), `placement-*` (:796-822), `placed-item-delegate-rebound` (:214)

### Bucket B — High-frequency spam (REMOVE or gate behind debug switch)

| Spam site | Reason to drop | Measured (real log) |
|---|---|---|
| **`surface-not-ready`** ×3 sites (:1128/:1230/:1255) `BUE-INVENTORY-004` | fires EVERY `PlayerUI.Update` frame while dashboard open + surface not buildable | **13,768 lines = 97.3% of all BUE log output** |
| **`[BUE-DRAG] GPT-WATERMARK preview-*`** (:644/:652/:670/:672/:679/:708) | fires every drag tick, throttled only to ≤2/s | 32+32+29 ≈ 93 total but during active drag only |
| `native-inventory-snapshot` (:30) | fires per native inventory event while awaiting projection | 18 |
| `plugin-update` (:221) + `heartbeat` (:209) | two parallel 120-tick heartbeats, redundant | 89 + 89 = **178** |

---

## 3. Structured "load failure reason" already present (one-shot)

The refactor can lean on these existing structured reason strings — many already human-readable "failed, reason:" probes:

- **`GateDiagnostics`** — `InventorySurfaceLifecycleAdapter.cs:824`. Two structured sources assigned in:
  - ctor (:858-862): `featureId=... errorCode=NativeHierarchyIncompatible diagnosticId=BUE-INVENTORY-003 reason=reflection-members-missing native inventory preserved`
  - live poll (:1098-1100): `... errorCode=NativeHierarchyIncompatible ... reason=live-parent-chain-invalid page=<n>`
  - hook failure (:883): `polling-hook-failed: <Type>: <Message>`
  - Consumed one-shot at plugin :114 (`wiring disabled ... diagnostics=<GateDiagnostics>`) — **already logged at the right moment**. ✔
- **`InventoryDragPreviewAdapter.GateDiagnostics`** (:51) — assigned `grid-attach-rejected-isolated` / `grid-attach-failed: Type: msg` (:169/:183/:197/:219). Consumed one-shot at plugin :130 (`wiring disabled ... diagnostics=<GateDiagnostics>`). ✔
- **`LastPollDiagnostics` / `LastCleanupDiagnostics`** (static, both adapters) — these are **NOT logged by default**; they are only surfaced via `GateDiagnostics` at wiring time or via the `Describe*` seams. They contain the richest structured failure text (e.g. `BuildCleanupIncompleteDiagnostics` → `featureId=... errorCode=CleanupIncomplete diagnosticId=BUE-DEV15D-CLEANUP-INCOMPLETE stage=<s> detail=<d>`, `poll failed: Type: msg`, `convergence-event-failed: ...`, `inventory-hook-unpatch-failed`, `grid-detach-failed`). **Gap: these run-time/cleanup failures are only stored in static fields and never emitted to the log — a one-shot "reason:" the refactor could surface.**
- **`InventoryLifecycleGate.Evaluate`** (`InventoryLifecycle.cs:225-254`) — returns `Diagnostics` string: `inventory-lifecycle-gate: headless branch...` / `inventory-lifecycle-gate: native members missing, BUE wiring disabled, native drag preserved -> PlayerDashboardInventoryUI.active, ...` (names each missing member). This is the canonical structured "failed, reason:" for wiring. It flows into `GateDiagnostics` then plugin :114. ✔
- **`DescribeTickGate`** (`InventoryDragPreviewAdapter.cs:522`) → `lifecycle-gate reason=can-run/blocked lifecycleCanRun=...`; **`DescribeAdapterGate`** (:530 and `InventorySurfaceLifecycleAdapter.cs:956`) → `adapter-gate reason=disabled/isolated/live`; **`DescribeNoActiveSession`** (`InventorySurfaceLifecycleAdapter.cs:1051`) → `no-active-session reason=disconnected/dashboard-closed/tracker-inactive/generation-unknown`. These are **pure helpers that return strings but have NO logging call sites** — they are dead-as-logged (used only in tests). The refactor can wire them to the one-shot places that actually log.
- **`FeatureRegistrationResult`** (`ContractTypes.cs`, `FeatureRegistrationRuntime.cs:66-97`) — `BUE-REG-ACCEPT` / `BUE-REG-00X` reason tokens for accept/reject, already surfaced once at plugin :137. ✔
- **`BootstrapGuard.Decide`** (`BootstrapGuard.cs:7`) returns `Unavailable/Client/Headless` — surfaced at plugin :52 (runtime-gate) and :136. ✔

**Verdict on moment-check:** The wiring-time one-shots (:112/:114/:125/:128-130/:131/:136/:137/:141) are all correctly one-shot. The **only structural gap** is that `LastPollDiagnostics` / `LastCleanupDiagnostics` / `Describe*Gate` / `DescribeNoActiveSession` collect rich per-run failure text that is **never logged**, so a runtime isolation that happens mid-session (not at wiring) has no one-shot "xxx failed, reason:" log line today — everything just silently floods `surface-not-ready` before/around it.

---

## 4. Prefix / diagnosticId token census + 60-second idle estimate

### Distinct code-level tokens (from source grep)
- Section prefixes: `[BUE-UI-TRACE]`, `[BUE-INVENTORY]`, `[BUE-DRAG]`, plus unprefixed bootstrap lines. Sub-tag: `GPT-WATERMARK`.
- distinct `diagnosticId` tokens in production source: `BUE-BOOTSTRAP-001/002/003`, `BUE-CLIENTUI-001/002/003/004/005`, `BUE-INVENTORY-001/003/004/005`, `BUE-DRAG-001/002/003`, `BUE-MANAGEMENT-TRACE-001/002/003`, `BUE-REG-ACCEPT`, `BUE-REG-00{1..9}`, `BUE-DEV15D-CLEANUP-INCOMPLETE` ≈ **20 distinct tokens**.

### Distinct tokens observed in the real LogOutput.log (14,147 BUE lines)
| diagnosticId | count |
|---|---|
| `BUE-INVENTORY-004` | **13,768** |
| `BUE-MANAGEMENT-TRACE-001` | 219 |
| `BUE-DRAG-001` | 126 |
| `BUE-DRAG-002` | 26 |
| `BUE-INVENTORY-001` | 4 |
| `BUE-INVENTORY-005` | 2 |
| `BUE-MANAGEMENT-TRACE-002` | 2 |
| `BUE-BOOTSTRAP-003` / `BUE-BOOTSTRAP-002` / `BUE-CLIENTUI-002` / `BUE-MANAGEMENT-TRACE-003` / `BUE-REG-ACCEPT` / `BUE-BOOTSTRAP-001` | 1 each |

### Total distinct log sites
- **~67 source call sites** (24 plugin + 31 panel LogTrace + 7 drag + 3 projection + 6 inventory + 2 register), collapsing to **~48 distinct literal strings / ~20 diagnosticId tokens**.

### 60-second idle-session estimate
The diagnostic log has no per-line timestamps (BepInEx 5.4 saved-file format drops them), but the page breakdown nails the rate:
`surface-not-ready` emits **1 line per supported page per frame** (pages 4,5,6,7), and the real log shows an almost exactly uniform **3440 lines × 4 pages** while the inventory dashboard was open (plus 4 each on transient pages 2,3 during open animation).

- **While the inventory/dashboard is open** (idle, no drag): 4 `surface-not-ready` lines PER FRAME.
  - @ 60 fps → **~240 lines/sec → ~14,400 lines per minute**.
  - @ 30 fps → ~120 lines/sec → ~7,200 lines/min.
- **Idle with docked display but inventory closed**: `Poll()` early-returns before the surface loop (no active session), so `surface-not-ready` stops; only the two 120-tick heartbeats (`plugin-update` + `heartbeat`, i.e. `BUE-MANAGEMENT-TRACE-001` ×2) keep firing → ~2 lines/sec ≈ 120 lines/min (at 60fps, once per 2s each) that are still non-noise.

**Takeaway:** a meaningfully idle session that sits on the inventory screen emits on the order of **10k–14k `surface-not-ready` lines per minute — the entire log-flood problem is one gate.** Everything else (heartbeats ~178/session, drag preview ~93 per drag-session) is two to three orders of magnitude smaller.

---

## 5. Duplication / friction findings

1. **`BUE-INVENTORY-004` surface-not-ready is emitted from 3 separate code sites** (`InventorySurfaceLifecycleAdapter.cs:1128`, `:1230`, `:1255`) representing the same logical state, with slightly different `reason=` suffixes. A normalizer should collapse these into one throttled "surface not ready, reason=..." line (ideally log once on state *change*, not per frame). This is the single largest duplication-of-effort in the log.

2. **Two parallel 120-tick heartbeats** — plugin `OnPluginUpdateTick` (`plugin-update`, BetterUnturnedExperiencePlugin.cs:219-222) **and** panel `TickCore.Update` (`heartbeat`, BueNativeManagementPanel.cs:207-210) both emit `BUE-MANAGEMENT-TRACE-001` every 120 Updates. They convey the same "plugin is alive" fact and together produced 178 lines in the observed log. One should be removed or demoted to debug.

3. **`surface-context-dispatched` (InventorySurfaceLifecycleAdapter:1136) and `placed-item-delegate-rebound` (InventoryDragPreviewAdapter:214) both fire on the same fresh-session-open event** (the open dispatcher at plugin :80-87 calls `AttachGrid(surface)` right after `OpenInventory(surface)`). Two adapters log the same logical "surface live for session" moment → 2 lines per open. Log count confirms equality: 2 `surface-context-dispatched` vs 2 `placed-item-delegate-rebound`.

4. **Run-time failure diagnostics are collected but never logged (largest refactor gap).** `LastPollDiagnostics`, `LastCleanupDiagnostics`, `DescribeTickGate`, `DescribeAdapterGate`, `DescribeNoActiveSession` all build structured "reason:" strings but have **no production log call site** (they're used by tests / held in statics). A mid-session isolation (e.g. `poll failed: Type: msg`, `inventory-hook-unpatch-failed`, `grid-detach-failed`) leaves **no one-shot failure line** — the surrounding frames just fire `surface-not-ready` until the feature goes silent. This is exactly the "failed, reason: yyy" the parent wants to keep, currently missing.

5. **Success-or-failure asymmetry within one site:** plugin :125 (drag preview disabled) is logged independently of :128-131 which then logs the *same* wiring state again with more detail — a potential double-log (failure path emits :125 then :130). Conversely the run-time Throttle (`ShouldEmitDiagnostic` 500ms) suppresses per-state-changed preview logs repetitively but there is no equivalent "once on transition" gate for `surface-not-ready`, so the **failure/not-ready side floods while the success side is throttled — exactly the asymmetry the user described.** Normalizing the not-ready gate to fire once on state transition (layout-ready vs not-ready vs discarded) fixes it.

6. **Token reuse causing ambiguous grouping:** `BUE-MANAGEMENT-TRACE-001` and `BUE-DRAG-001` each cover many distinct `event=` values, so diagnosticId alone cannot distinguish meaning. A refactor should either give each one-shot a unique token or normalize around `event=` with a single gate-dependent level.

---

## Summary counts
- **Total distinct log sites:** ~67 call sites → ~48 distinct literal strings / ~20 diagnosticId tokens.
- **One-shot vs high-frequency:** ~44 one-shot lifecycle/bucket-A sites vs ~7 high-frequency bucket-B sites (`surface-not-ready`×3, `preview-*`×6, `native-inventory-snapshot`, `plugin-update`, `heartbeat`).
- **Top 3 spam sources by measured lines:**
  1. `surface-not-ready` (`BUE-INVENTORY-004`, :1128/:1230/:1255) — **13,768 lines (97.3%)**; ≈14,400/min with dashboard open at 60fps.
  2. `plugin-update` + `heartbeat` (`BUE-MANAGEMENT-TRACE-001`, plugin:221 + panel:209) — 178 lines/session.
  3. `[BUE-DRAG] GPT-WATERMARK preview-input-readout/evaluated/visible` (:708/:670/:672) — ~90+ per active drag session.
- **Structured failure text already present:** wiring-time one-shots (plugin :114/:130) already surface `GateDiagnostics` and `InventoryLifecycleGate.Evaluate` missing-member lists, `BootstrapGuard.Decide`, `BUE-REG-*`, `NativeHierarchyIncompatible` error codes. **Gap:** `LastPollDiagnostics`/`LastCleanupDiagnostics`/`Describe*Gate`/`DescribeNoActiveSession` are built but never logged.
- **Biggest friction:** `surface-not-ready`'s per-frame flood at 3 sites + the never-logged run-time failure diagnostics (asymmetry where the failure/not-ready side floods and the success/failure-reason side stays silent).