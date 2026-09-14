# Better Unturned Experience V1 Requirements Specification

> **SUPERSEDED / 历史资料，不是现行契约**（2026-09-14 落标）：本文是 V1 架构期的需求规格，边界与假设（如「不动态加载外部功能 DLL」）已被后续阶段超越，不得作为实施依据。
> 现行生态契约唯一事实源 = `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`；人类开发者入口 = `docs/developer/README.md`。本文件保留仅作历史决策资料，不删除。

> Infrastructure file; authored by GPT.  
> Status: ready-for-agent  
> Stage: Requirements Specification  
> Input: Wayfinder decision package that passed the joint consistency review  
> Language: English agent-execution copy  
> Human-readable Chinese mirror: [spec.zh-CN.md](spec.zh-CN.md)

## Problem Statement

Better Unturned Experience must move from completed architecture decisions into an implementable requirements stage. Its product goal is to provide a standardized, open, customizable and highly compatible plugin framework for improving vanilla Unturned features, with multi-creator source collaboration. Its first integrated implementation, Better Item Interaction, improves dragging, candidate placement and placement feedback in vanilla inventory and container grids. Both must remain compatible with singleplayer, SteamP2PFriends Host/Client and U3DS.

The product serves two user groups. Players use one settings entry and the available vanilla-experience enhancements. Developers contribute, compose and maintain features through stable shared contracts, the feature-definition pipeline and the module-registration mechanism. The framework must not require developers to duplicate settings menus, network protocols or lifecycle infrastructure, and a fault in one feature must not break other features or vanilla gameplay.

The primary risk is not a lack of feature ideas but responsibility drift when frontend, backend and shared contracts meet concrete Unturned types and members. The frontend could reimplement inventory rules, the backend could leak UI types, or both sides could adopt different function names, coordinate semantics, state machines or error-handling order. Harmony hooks without fixed-source evidence may also fail on the client or U3DS.

This specification therefore divides the work into three layers: first freeze the shared logic and exact interfaces used by both sides; then have Gemini independently research frontend U3-SDK adapter facts; and have GPT independently research backend U3-SDK and authority-chain facts. Research may add adapter implementation facts but may not silently rewrite the shared contract. Every shared-interface change requires renewed GPT/Gemini review.

## Solution

Establish one requirements specification with three clear seams:

1. **Shared-contract seam**: maintained by GPT; freezes feature lifecycle, settings, events, candidate evaluation, status projection, error codes, versions and intended-item-center coordinate semantics. Gemini and GPT depend only on this seam, not on each other's internals.
2. **Frontend U3-SDK adapter seam**: researched and maintained by Gemini; converts Unturned Glazier/Sleek UI, pointer, grab offset, rotation input, menu lifecycle and native inventory projection into shared-contract inputs and read-only presentation.
3. **Backend U3-SDK adapter seam**: researched and maintained by GPT; converts Unturned `PlayerInventory`, `Items`, native RPC, server revalidation, threading, storage context and network capabilities into shared-contract state and authoritative operations.

V1 is delivered as one aggregate DLL. Feature authors integrate through source contributions and normalized feature definitions; V1 does not dynamically load external feature DLLs. Final inventory submission continues through the native Unturned authority path. The plugin creates neither a parallel inventory protocol nor optimistic inventory mutation.

### Product acceptance outcomes

- **Better Item Interaction acceptance**: in supported inventory and container grids, a player can successfully drag, preview, automatically rotate and place through native authority. When no candidate exists, the operation is cancelled and the original position is preserved. Vanilla interaction remains usable after disablement or fault isolation.
- **Better Unturned Experience framework acceptance**: Better Item Interaction registers through the framework and works in practice. At least one additional integration-validation module supplied by the project developer must prove that the stable public contract, unified settings entry, lifecycle/isolation model and single-DLL linker can host another plugin feature while both run correctly. This validation module may be a non-release fixture and does not add a second launch feature.
- “Runs correctly” requires separate runtime evidence for the applicable singleplayer, SteamP2PFriends Host/Client and U3DS roles, tied to the same Candidate DLL SHA-256. A successful build or static review cannot replace player-observable runtime acceptance.

## User Stories

1. As a player, I want to drag an item without precisely targeting one grid cell, so that inventory management requires less fine mouse control.
2. As a player, I want to see the exact footprint that would be occupied, so that I understand the candidate placement before releasing the mouse.
3. As a player, I want legal placements displayed differently from blocked placements, so that invalid drops are understandable before submission.
4. As a player, I want items to remain in their original location when no candidate exists, so that the enhancement never causes avoidable item loss.
5. As a player, I want automatic rotation only when the nearby space requires it, so that the preview does not continuously flip in open space.
6. As a player, I want my manual rotation input to preserve the point where I grabbed the item, so that the dragged icon does not jump unexpectedly.
7. As a player, I want a dragged item near an edge to align naturally inside the container, so that edge placement feels predictable.
8. As a player, I want the enhanced preview to disappear immediately after submission, so that no stale overlay remains.
9. As a player, I want delayed server projection to update silently, so that ordinary latency does not produce false failure messages.
10. As a player, I want the original inventory to remain usable when the enhancement is unavailable, so that the plugin never blocks normal gameplay.
11. As a player, I want one settings entry for the whole product, so that every contributed feature does not create a separate menu.
12. As a player, I want local preferences preserved when a server policy overrides them, so that leaving the server restores my preferred values.
13. As a player, I want server-controlled settings visibly locked, so that I know why a local control is not effective.
14. As a player, I want module failures shown as localized, actionable status, so that raw exceptions do not appear in the UI.
15. As a player, I want a failure in one feature to leave other features working, so that one creator's defect does not disable the entire product.
16. As a player, I want the original game to remain available when the core enters SafeMode, so that a plugin failure does not intentionally terminate the game.
17. As a singleplayer user, I want the same feature rules as multiplayer, so that singleplayer is not a separate special implementation.
18. As a SteamP2PFriends host, I want authoritative settings and inventory validation to execute locally, so that host behavior matches dedicated-server rules.
19. As a SteamP2PFriends client, I want only server-confirmed settings and native inventory projection treated as facts, so that the client cannot invent authority.
20. As a U3DS operator, I want the server to load without resolving Glazier or client-only types, so that the same DLL is safe in headless operation.
21. As a U3DS operator, I want feature failures logged with FeatureId, version and DiagnosticId, so that failures are supportable without a graphical UI.
22. As a feature creator, I want a permanent FeatureId independent of directory and display name, so that refactoring and localization do not break compatibility.
23. As a feature creator, I want static settings descriptors compiled into the definition artifact, so that I do not implement a custom settings menu.
24. As a feature creator, I want feature-scoped settings, logging, events and capabilities, so that I cannot accidentally access another feature's scope.
25. As a feature creator, I want all long-lived registrations tracked by the feature lifetime, so that isolation can clean them up reliably.
26. As a feature creator, I want an internal ClientUi registration path generated at build time, so that I do not rely on reflection or class-name discovery.
27. As a feature creator, I want required dependencies started in a deterministic order, so that startup behavior is reproducible.
28. As a feature creator, I want optional dependencies queried through capabilities, so that missing optional features do not prevent startup.
29. As a feature creator, I want contract compatibility rules expressed with structured versions, so that display strings do not decide interoperability.
30. As a feature creator, I want malformed declarations rejected during definition compilation, so that normal runtime does not discover avoidable repository errors.
31. As a frontend maintainer, I want one candidate evaluator call to return all rendering facts, so that the Presenter does not reimplement placement rules.
32. As a frontend maintainer, I want pointer-to-intended-center conversion owned by the ClientUi adapter, so that UI coordinates do not leak into the evaluator.
33. As a frontend maintainer, I want the native forward rotation transform fixed to Unturned `rot+1`, so that corner and edge grabs do not jump backward.
34. As a frontend maintainer, I want static Settings facets separated from runtime snapshots, so that UI structure and current values never become competing facts.
35. As a frontend maintainer, I want every timer and callback guarded by the current generation, so that stale UI work cannot modify a new interaction.
36. As a frontend maintainer, I want exact U3-SDK UI hook evidence, so that Harmony patches are based on fixed source rather than guessed names.
37. As a frontend maintainer, I want clipping, Z-order and UI Scale behavior documented, so that overlays remain aligned at different resolutions.
38. As a frontend maintainer, I want U3DS type-token restrictions documented, so that runtime `batchmode` checks are not mistaken for structural isolation.
39. As a backend maintainer, I want the native inventory RPC call chain documented end-to-end, so that the plugin never duplicates or bypasses authoritative validation.
40. As a backend maintainer, I want ownership, page, capacity, occupancy and equipment-slot checks mapped to fixed source, so that adapter assumptions are reviewable.
41. As a backend maintainer, I want the Unity/game-thread mutation points identified, so that network callbacks never mutate Unturned state off-thread.
42. As a backend maintainer, I want storage-session and container lifetime rules documented, so that stale container references cannot be treated as authority.
43. As a backend maintainer, I want client and U3DS reference differences recorded, so that shared code stays within the actual type intersection.
44. As a backend maintainer, I want LMN channel and handler capabilities verified against fixed source, so that the network adapter uses supported transport behavior.
45. As a backend maintainer, I want settings persistence roots and file semantics verified, so that client and server values cannot overwrite one another.
46. As a tester, I want contract behavior tested through public interfaces, so that tests remain stable when internal adapters change.
47. As a tester, I want frontend research claims tagged as source, IL or runtime evidence, so that a source match is not reported as gameplay PASS.
48. As a tester, I want backend research claims tagged by environment and hash, so that evidence from one role cannot prove another.
49. As a release approver, I want CandidateBuild identity separated from environment evidence, so that compilation does not silently become release approval.
50. As a release approver, I want SP, P2P Host/Client and U3DS evidence tied to the same DLL SHA-256, so that the released binary is the tested binary.
51. As a project maintainer, I want shared interface changes reviewed by both GPT and Gemini, so that neither side breaks the other through unilateral changes.
52. As a project maintainer, I want research outputs to end in explicit adapter recommendations and rejected alternatives, so that later agents do not repeat the same investigation.
53. As a project maintainer, I want unresolved source/version differences clearly recorded, so that uncertainty is converted into a test obligation rather than hidden.
54. As a project maintainer, I want the requirements specification to avoid production-path assumptions, so that implementation tickets can select exact files after research.

## Implementation Decisions

### Layer 1 — Shared frontend/backend contract

#### Contract ownership and dependency direction

- GPT owns the shared contract; Gemini is a required reviewer for every field, function, event or compatibility change consumed by the frontend.
- Contracts contain only .NET Framework-compatible domain types. They do not reference BepInEx, Harmony, Glazier, Sleek, Steamworks, LMN or Unturned concrete types.
- Frontend and backend adapters depend on Contracts. Contracts do not depend on either adapter.
- Runtime module declarations are forbidden. Identity, dependency, settings schema, event ownership, capability and entry binding come from the linked definition artifact.

#### Frozen module function names

| Interface | Exact member | Required behavior |
| --- | --- | --- |
| `IFeatureModule` | `FeatureStartResult Start(IFeatureBootstrap bootstrap)` | Called only after trusted admission, settings bootstrap and transition to Starting. |
| `IFeatureModule` | `void Stop(FeatureStopReason reason)` | Called at most once per lifecycle generation; semantic cleanup is backed by lifetime lease cleanup. |
| `IFeatureLifetime` | `bool TryTrack(IDisposable registration)` | Tracks long-lived registrations; rejects null and immediately disposes late registrations after Closing. |
| `IScopedFeatureSettings` | `FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)` | Returns the complete immutable snapshot for the current feature scope. |
| `IScopedFeatureSettings` | `bool TryGet(string settingId, out SettingValue value, out uint revision)` | Reads one confirmed value without accepting an arbitrary FeatureId. |
| `IScopedFeatureSettings` | `SettingChangeResult Submit(ScopedSettingChangeRequest request)` | Performs one atomic feature-scoped setting transaction. |
| `IFeatureEventSubscriber` | `IDisposable Subscribe<TEvent>(Action<TEvent> handler)` | Subscribes only to events allowed by the linked consumption facet. |
| `IOwnedFeatureEventPublisher` | `bool TryPublish<TEvent>(string declaredEventId, TEvent value)` | Publishes only declared events owned by the current feature and matching the exact event type. |
| `IFeatureLogger` | `void Info(string eventName, string diagnosticId)` | Emits feature-scoped informational diagnostics. |
| `IFeatureLogger` | `void Warning(string eventName, FrameworkErrorCode error, string diagnosticId)` | Emits stable warning codes without player-facing raw payloads. |
| `IFeatureLogger` | `void Error(string eventName, FrameworkErrorCode error, string diagnosticId, Exception exception)` | Records the exception internally; ordinary UI receives only safe projections. |
| `IDependencyCapabilityView` | `bool Has(string declaredDependencyId, string capabilityId, ushort minimumVersion)` | Checks only a linked local dependency id. |
| `IDependencyCapabilityView` | `bool TryGet(string declaredDependencyId, out NegotiatedFeatureView feature)` | Returns the current negotiated dependency projection if visible. |
| `IPlacementCandidateEvaluator` | `ItemPlacementPreview Evaluate(PlacementCandidateInput input)` | Pure, synchronous, stateless candidate evaluation; no Unity side effects and production target of zero per-call allocation. |

#### Frozen bootstrap properties

`IFeatureBootstrap` exposes exactly:

| Property | Type |
| --- | --- |
| `Identity` | `FeatureScopeIdentity` |
| `LifecycleGeneration` | `ulong` |
| `Settings` | `IScopedFeatureSettings` |
| `Events` | `IFeatureEventSubscriber` |
| `OwnedEvents` | `IOwnedFeatureEventPublisher` |
| `Logger` | `IFeatureLogger` |
| `Dependencies` | `IDependencyCapabilityView` |
| `Lifetime` | `IFeatureLifetime` |

UI roots, native inventory objects, LMN connection objects and arbitrary module lookup are forbidden.

#### Frozen command and event contract

| Kind | Contract type | Direction/scope | Required correlation and payload facts |
| --- | --- | --- | --- |
| `0x0101` | `UpdateModuleConfigCommand` | client→server after Ready; `ClientPreference` use is process-local | `RequestId`, `Feature`, `RevisionScope`, `ExpectedRevision`, complete mutation list. Network form permits only `ServerAuthority`. |
| `0x0102` | `ModuleConfigChangedEvent` | server→client after Ready; also process-local for confirmed local changes | Echoes `RequestId`; includes `Feature`, `RevisionScope`, new `Revision` and complete `FeatureSettingsSnapshot`. |
| `0x0103` | `ModuleConfigRejectedEvent` | server→client after Ready | Echoes `RequestId`; includes `Feature`, `RevisionScope`, stable error, `CurrentRevision` and complete current snapshot. |
| `0x0104` | `RequestModuleConfigSnapshotCommand` | client→server after Ready | New non-zero `RequestId`, `Feature`, `RevisionScope`, `KnownRevision`; read-only and cannot increment revision. |
| `0x0201` | `FeatureStatusChangedEvent` | server→client only for server/negotiated states visible to that client; process-local projection is separately allowed | Contains complete `FeatureStatusView`; frontend deduplicates by revision and safe diagnostic identity. |
| process-local only | `CoreRuntimeStatusChangedEvent` | core→local frontend; never registered as V1 LMN | Contains complete `CoreRuntimeStatusView` with monotonic revision. |
| `0x0004` | `SessionReadyEvent` | server→client on capabilities channel | Correlates `ConnectionGeneration` and `SnapshotId`; settings/status network messages are forbidden before it. |

All network messages use the shared Contract envelope and the frozen channel mapping. `RequestId` provides transaction correlation/idempotency within a connection generation; it is not identity or authorization. Revision and connection generation remain separate ordering domains.

V1 does not register `CommitItemPlacementCommand`, `RequestRotateItemCommand`, `ItemPlacementCommittedEvent`, `ItemPlacementRejectedEvent` or `InventoryStateUpdatedEvent` as plugin network messages.

#### Frozen interaction and state logic

- Feature state contains nine values: Discovered, Incompatible, Disabled, Starting, Running, Isolating, Isolated, Stopping and Stopped.
- ModuleRuntime/Lifecycle is the only writer of FeatureState, StateRevision and feature status events.
- Runtime Admission returns static admission decisions but does not write lifecycle state or read user settings.
- SettingsRuntime completes migration and validation before Lifecycle chooses Starting or Disabled.
- The drag interaction sequence is Idle → Dragging → Hovering → Dropping → AwaitingProjection → Idle.
- The AwaitingProjection timeout is 2.0 seconds and controls enhanced UI waiting only.
- Setting submission retries once with the same RequestId after three seconds; after eight seconds a new RequestId requests a complete snapshot through `0x0104`.

#### Frozen placement coordinate semantics

- Container and footprint local origin is top-left; X increases right and Y increases down.
- `grabOffsetInFootprint` is a frontend-only continuous coordinate in the closed domain `[0,W] × [0,H]`.
- `CursorGridX/Y` is a historical Draft name carrying the intended item center, not the raw pointer.
- Intended center is calculated as pointer grid plus current footprint center minus grab offset.
- Intended center is valid inside the container half-open domain `[0,containerWidth) × [0,containerHeight)`; outside returns Hidden/OutsideGrid.
- Native forward `rot+1` transforms grab offset from a W×H footprint to H×W as `(H-gy, gx)`.
- Native backward `rot-1` transforms it as `(gy, W-gx)`.
- Forward corner and center mappings must match the frozen contract, and the intended center is recalculated after transforming the grab offset.

#### Frozen placement algorithm

1. If the local projected candidate fits in the current orientation, return it without checking rotation.
2. If local current fails and local rotated fits, return the local rotated candidate.
3. Otherwise search all current-orientation candidates by squared center distance, then Y, then X.
4. Only when current orientation has no global candidate, search the rotated orientation.
5. Do not automatically exchange or rearrange occupied items.
6. The evaluator returns preview facts only; final submission uses the native inventory path.

#### Research-gated adapter names

Exact native adapter type names, Harmony targets and native event hooks are not frozen by this layer. Gemini and GPT research must recommend stable internal adapter names after tracing fixed U3-SDK source. Any proposed shared member requires a shared-contract revision and dual review; internal adapter naming does not.

### Layer 2 — Gemini frontend U3-SDK research

#### Research objective

Determine the exact client UI seams needed to implement the agreed interaction and settings behavior without leaking UI types into Contracts/Core or assuming runtime behavior from names alone.

#### Required investigation

- Trace inventory UI construction, open, close and destroy lifecycle from client roots to inventory/container views.
- Identify exact drag start, drag update, manual rotate, release, cancel and page/container change methods and their signatures.
- Confirm how the native UI stores source page, source position, rotation, item footprint, pointer offset and drag generation-equivalent state.
- Trace `PlayerDashboardInventoryUI` pivot and visual positioning behavior for rotations 0 through 3.
- Verify the forward `rot++` transformation and record source lines for all four orientations.
- Identify the exact screen/viewport/UI Scale conversion APIs needed to produce pointer grid coordinates.
- Determine cell pixel sizing, scaling, scroll offset, clipping and Z-order behavior for player inventory and storage containers.
- Identify a safe parent/root for the footprint overlay and floating icon without assuming one global root works in every view.
- Trace native inventory projection events used to refresh item views, including their ordering and whether they identify source/target changes.
- Record what is source-confirmed about `onInventoryAdded` and `onInventoryRemoved`, and what remains adapter or runtime verification.
- Identify native placement audio invocation and determine whether the plugin should call it or rely on the native path.
- Verify input focus behavior for the rotate key, settings shortcut and text/key-binding capture.
- Trace main menu, pause menu and options UI extension points for the unified settings entry.
- Verify Glazier/Sleek concrete types and differences across the selected client reference set.
- Identify every client-only type token reachable from proposed UI components.
- Propose generated internal registration records that do not require assembly scanning.
- Recommend exact Harmony patch points only where no existing event/interface seam exists.
- Document compatibility risks with other inventory UI patches and propose Harmony ordering behavior without claiming universal compatibility.

#### Required output

- A Gemini-prefixed frontend U3-SDK research report.
- A call-chain table with fixed source identity, type, member, signature, caller, callee and evidence classification.
- A UI lifecycle diagram and coordinate transformation table.
- A proposed internal adapter interface list, clearly separated from shared Contracts.
- A list of rejected hook points and why they are unsafe or shallow.
- An unresolved-facts section converted into implementation tests or runtime evidence obligations.
- No production implementation in the research deliverable.

### Layer 3 — GPT backend U3-SDK research

#### Research objective

Determine the exact native authority, state, threading, persistence and server/client seams needed to implement the public framework and Better Item Interaction without bypassing Unturned validation or creating a second inventory fact.

#### Required investigation

- Trace `sendDragItem` through generated/network dispatch to `ReceiveDragItem` for SP, P2P Host loopback, P2P Client and U3DS.
- For U3DS, record that the client-side `sendDragItem` origin is not executed inside the dedicated-server process; trace the server receive/validation path from its network entry instead.
- Record RPC reliability, ownership restrictions, caller identity source and rate limiting.
- Trace source page/item lookup and every validation of page, coordinates, rotation, capacity, occupancy, asset size, equipment slots and storage access.
- Trace the mutation order after successful validation and identify all failure exits before mutation.
- Verify whether remove/add operations are atomic at the game-thread level and where plugin callbacks would be unsafe.
- Trace `checkSpaceEmpty`, `checkSpaceDrag`, `checkSpaceSwap` and relevant `Items` methods, while preserving the V1 no-auto-swap rule.
- Determine how the currently opened storage/container session is represented and invalidated.
- Identify inventory projection/change events and which are safe for read-only observation.
- Verify the Unity/Unturned thread on which RPC handlers and inventory mutations execute.
- Determine safe queue boundaries for LMN callbacks and feature callbacks.
- Trace BepInEx plugin initialization and shutdown on client and U3DS.
- Compare client and U3DS `Assembly-CSharp`/BepInEx reference identities and reachable type intersections.
- Verify safe Harmony patch targets for read-only context capture and native submission adaptation; reject patches that bypass `ReceiveDragItem`.
- Trace server/local settings storage roots and APIs needed for separated authority scopes.
- Verify LMN V5 named-channel registration, reliable send, sender context, payload limits and handler thread behavior against fixed source.
- Determine which environment facts belong to Runtime Admission and which dynamic facts belong to SettingsRuntime/Lifecycle.
- Identify exact native errors that can be projected as stable framework error families without exposing implementation details.
- Record all assumptions requiring runtime verification with the same candidate DLL hash.

#### Required output

- A GPT-prefixed backend U3-SDK research report.
- End-to-end inventory authority call chains per environment role.
- A validation-and-mutation matrix showing the last safe point before native mutation.
- A threading and queueing diagram.
- A client/U3DS reference intersection and type-leak risk table.
- Proposed internal backend adapter interfaces, separated from shared Contracts.
- Rejected patch points and explicit reasons.
- Runtime evidence obligations for SP, P2P Host/Client and U3DS.
- No production implementation in the research deliverable.

### Research coordination rules

- Gemini and GPT may investigate their U3-SDK layers independently and in parallel.
- Each report must use the same frozen U3-SDK source identity and record it explicitly.
- If either investigation discovers that a shared DTO/member is insufficient, the finding is raised as a shared-contract change request rather than silently implemented.
- A shared-contract change is accepted only after GPT updates the contract and Gemini confirms frontend consumability.
- Source evidence, IL evidence, prototype evidence and runtime evidence must be labeled separately.
- Research completion does not authorize production coding; research findings feed `/to-tickets` and implementation specifications.

## Testing Decisions

The user-selected three-layer split is the explicitly confirmed highest-level test seam for this specification: shared-contract behavior, frontend U3-SDK adapter behavior and backend U3-SDK adapter behavior. The specification is therefore published as `ready-for-agent`.

- Tests use the highest available seam: shared-contract behavior, evaluator output, lifecycle transition, settings transaction, negotiation result and native-adapter observable behavior.
- Tests do not assert private helper calls, concrete collection types, reflection order or incidental Harmony implementation details.
- Shared contract compilation tests must use both the client and U3DS-compatible reference boundary and must reject UI/native type leakage.
- Lifecycle tests cover every legal and illegal transition, admission batch processing, settings bootstrap ordering, first-exception isolation, dependency cascade and cleanup idempotency.
- Settings tests cover atomic multi-field commit, revision conflict, RequestId replay, migration failure, corrupted persistence, server policy overlay and session-generation invalidation.
- Network tests cover truncated/oversized payloads, unknown messages, nonce/generation mismatch, incomplete snapshots, reconnect and silent plugin absence.
- Placement tests cover 1×1, 1×4, 1×5, 2×3, 3×3, crowded grids, edge clamping, current-local stability and local automatic rotation.
- Coordinate tests cover grab points at four corners, four edge midpoints, center and fractional positions for rotations 0→1→2→3→0.
- Four forward rotations must restore the original footprint and grab offset within an explicitly chosen floating-point tolerance.
- Tests distinguish grab offset closed-domain behavior from intended center half-open container-domain behavior.
- Frontend adapter tests verify UI Scale, scrolling, clipping, input focus, view close, feature isolation and stale-generation callbacks.
- Backend adapter tests verify that invalid input never reaches mutation, native validation remains authoritative and plugin code never performs a parallel remove/add transaction.
- Research reports are accepted only when every claimed native fact includes fixed source identity and evidence class.
- Runtime qualification later requires independent evidence for SP, SteamP2PFriends Host/Client and U3DS using the same candidate DLL SHA-256.
- Existing GPT-12 Node tests are prior art for algorithm behavior only; they are not prior art for production allocation, UI integration or runtime authority.

## Out of Scope

- Modifying or replacing original Unturned client or U3DS files/content; the plugin may only integrate through the approved BepInEx, Harmony, public contract and native game seams without distributing altered original binaries.
- Binary reverse engineering, protection bypass, exploit development, unauthorized access or abusive use. Reading the authorized local U3-SDK/source reference to document compatible adapter seams is permitted research and must remain within this specification's evidence and authority boundaries.
- Production implementation, project scaffolding beyond specification artifacts, DLL building or packaging.
- Directory scanning or implicit dynamic discovery of third-party feature DLLs in V1; external features must be explicit BepInEx plugins registering through the accepted BUE Host contract.
- Uncontrolled native UI injection by external mods; optional UI satellites may expose only the accepted pure-metadata registration contract, while BUE retains unified settings-modal ownership.
- Replacing Unturned inventory RPCs with LMN messages.
- Plugin-managed item rollback, optimistic inventory mutation or a second inventory database.
- Automatic item exchange, automatic rearrangement or inventory sorting.
- Exact Harmony patch targets before the corresponding U3-SDK research is accepted.
- Claiming zero GC without Release C# allocation measurement.
- Claiming U3DS safety from `Application.isBatchMode` alone.
- Treating source, prototype or build evidence as SP/P2P/U3DS runtime PASS.
- Assigning Stable/1.0.0 maturity or publishing a release.

## Further Notes

- The user explicitly confirmed all three highest testing seams. This specification is `ready-for-agent` for ticket decomposition and agent-led U3-SDK research, not production coding.
- `/to-tickets` should create at least three work packages matching the three layers, with the frontend and backend U3-SDK research packages able to run in parallel after the shared-contract baseline ticket is acknowledged.
- Shared-contract changes discovered during research must preserve the single-definition-artifact model and the original inventory authority boundary.
- The first production CandidateBuild must remain pre-release and cannot inherit any runtime evidence from prototypes or unrelated DLL hashes.
