# Better Unturned Experience — BepInEx Prerequisite Framework Runtime Specification

> **SUPERSEDED / 历史资料，不是现行契约**（2026-09-14 落标）：本文是开放运行时的早期提案；其设想的独立 SDK 包拆分现行结论为暂缓（见 SDK §4），运行时生态形态已按后续阶段落地，本文不再作为实施依据。
> 现行生态契约唯一事实源 = `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`；人类开发者入口 = `docs/developer/README.md`。本文件保留仅作历史决策资料，不删除。

> Proposal specification; authored by GPT.
> Status: ready-for-agent
> Supersedes the current “V1 no external feature DLL” decision only after explicit human, GPT and Gemini acceptance.
> Chinese human-readable mirror: [spec-open-runtime-feature-framework.zh-CN.md](spec-open-runtime-feature-framework.zh-CN.md)

## Problem Statement

Better Unturned Experience currently assumes that feature authors contribute source code into one aggregate DLL. That gives the framework strong control, but it does not deliver the intended experience: BUE should be a large prerequisite framework plugin running on BepInEx, with its own official features and a public runtime for independently installed feature plugins.

The current boundary also creates a false expectation that every feature author must repeat the framework's entire singleplayer, SteamP2PFriends and U3DS compatibility investigation. BUE should remove that infrastructure uncertainty by publishing stable contracts, SDK tooling, conformance tests, lifecycle rules, settings integration and environment adapter guidance. It must not, however, claim that a feature's own gameplay behavior is proven merely because the framework was tested.

## Solution

Make Better Unturned Experience a public prerequisite framework plugin and feature host on top of BepInEx. BepInEx remains responsible for the lowest-level plugin discovery and assembly loading. BUE provides the higher-level public contracts, feature registration, lifecycle, settings, compatibility and unified management runtime. Independent feature DLLs are installed beside BUE and declare BUE as a dependency; BUE does not replace BepInEx, directly load arbitrary DLLs, scan arbitrary assemblies or infer feature classes by reflection.

LMN is the model for the transport layer: it provides an open, reusable network framework. BUE is the larger host framework around that idea. It covers module loading, public contracts, lifecycle/isolation, settings, capability negotiation, environment adaptation, unified UI, diagnostics and conformance tooling; LMN remains an optional transport adapter rather than the definition of BUE itself.

The runtime is divided into these products:

- `BetterUnturnedExperience.dll`: the installed BepInEx prerequisite framework plugin, public Contracts surface, runtime registry, lifecycle/isolation, settings aggregation, transport adapters, unified management UI and official feature modules.
- `BetterUnturnedExperience.SDK`: developer-time package/repository containing templates, Definition Linker tooling, analyzers, conformance tests and documentation. It is not required as a separate runtime install when its types are compile-time forwarded to the stable BUE API surface.
- Official feature modules: V1 official features maintained by the project, including Better Item Interaction, are shipped inside the BUE Host distribution with independent FeatureIds and definition records; they do not require a second official DLL in the player installation.
- Third-party feature DLLs: independently versioned BepInEx plugins that reference the public BUE API, declare BUE as a prerequisite and register explicitly at startup.
- Optional client UI satellite DLLs: client-only assemblies may be shipped separately from a feature's all-environment core DLL. They are process-gated and never required by U3DS core loading.

The highest test seam is **explicit feature registration and unified management runtime**. At this seam BUE accepts a generated feature registration, validates its public definition artifact and compatibility range, composes an immutable runtime catalog, starts the feature through a feature-scoped bootstrap, exposes its Settings Facet in the unified UI, and isolates failures locally.

### Product acceptance outcomes

- A player can install `BetterUnturnedExperience.dll` plus one or more independent feature DLLs in the BepInEx plugins directory. BUE remains usable if no feature DLL is present.
- An accepted feature appears in the BUE management list with its identity, version, status, settings, compatibility state and safe diagnostics.
- Better Item Interaction remains the first official feature and continues to use the native Unturned inventory authority chain.
- A third-party feature can be developed without editing BUE source, without copying BUE internals and without implementing a private settings menu or lifecycle protocol.
- BUE's framework conformance evidence covers the framework seams once. Each feature still supplies targeted evidence for its own behavior and declared environment targets; no feature may inherit gameplay PASS, release authorization or native authority correctness automatically from BUE.
- A malformed, incompatible or failing feature is rejected or isolated without preventing BUE, other accepted features or vanilla gameplay from continuing where the process remains safe.

## User Stories

1. As a player, I want to install the BUE framework and feature DLLs side by side, so that I do not need to rebuild one monolithic plugin.
2. As a player, I want BUE to work when no optional feature DLL is present, so that the framework itself is independently useful.
3. As a player, I want every accepted feature visible in one BUE management list, so that I do not hunt through separate settings menus.
4. As a player, I want a feature's settings to use the same value, revision and policy rules as official features, so that third-party settings behave predictably.
5. As a player, I want an incompatible feature to be clearly marked instead of partially starting, so that I know why it is unavailable.
6. As a player, I want one feature failure isolated from other features, so that a bad optional module does not disable vanilla gameplay.
7. As a player, I want local preferences preserved when a server policy temporarily overrides them, so that removing a feature or leaving a server does not erase my preferences.
8. As a player, I want U3DS to load feature core logic without resolving client-only UI types, so that the same installation model remains safe on a headless server.
9. As a player, I want a missing optional client UI satellite to disable only presentation, so that the feature core can remain diagnosable.
10. As a feature author, I want a stable public API and SDK, so that I can develop independently of BUE's private classes.
11. As a feature author, I want a generated registration artifact, so that identity, versions, dependencies, settings and capabilities are not re-described through a second runtime API.
12. As a feature author, I want BUE to call one explicit registration entry point, so that I know exactly when my module becomes visible to the framework.
13. As a feature author, I want BUE to reject duplicate FeatureIds deterministically, so that two modules cannot silently replace each other.
14. As a feature author, I want a permanent FeatureId separate from display names and slugs, so that localization and directory changes do not break compatibility.
15. As a feature author, I want a declared BUE API compatibility range, so that incompatible modules are rejected before execution.
16. As a feature author, I want dependency and capability declarations validated before startup, so that optional integrations do not rely on ad-hoc global lookups.
17. As a feature author, I want a feature-scoped bootstrap, logger, settings view, event view and lifetime, so that I cannot accidentally mutate another feature's state.
18. As a feature author, I want generated Settings Facets consumed by the BUE UI, so that I do not build a parallel configuration window.
19. As a feature author, I want client UI code separated from core code, so that U3DS does not load Glazier/Sleek/Unity-only types.
20. As a feature author, I want a conformance test kit for contracts, lifecycle, settings and registration, so that I can verify framework integration locally.
21. As a feature author, I want BUE's tested adapter seams documented, so that I do not repeat framework-level source archaeology.
22. As a feature author, I want targeted environment obligations derived from my declared capabilities, so that I test the behavior I actually use.
23. As a feature author, I want a feature that only provides local settings to avoid unnecessary network obligations, so that declarations remain proportional to behavior.
24. As a feature author, I want an inventory-authority feature to prove its own native client/server behavior, so that the framework does not hide feature-specific risk.
25. As a maintainer, I want BUE to discover only BepInEx-declared plugin entries, so that arbitrary DLL scanning cannot trigger unsafe type loading.
26. As a maintainer, I want registration to close before the runtime catalog becomes ready, so that late mutation cannot change active identity facts.
27. As a maintainer, I want a deterministic composition order by FeatureId and DefinitionDigest, so that different machines produce the same catalog.
28. As a maintainer, I want an explicit registration rejection reason, so that authors can repair a module without reading private logs.
29. As a maintainer, I want feature code exceptions converted into isolated status and diagnostics, so that the framework remains operational.
30. As a maintainer, I want a core invariant failure to enter Core SafeMode, so that unsafe partial execution is not mistaken for success.
31. As a maintainer, I want the BUE API to evolve semantically, so that minor updates remain compatible and breaking changes are explicit.
32. As a maintainer, I want retired feature identities and settings migrations preserved, so that uninstalling a module does not corrupt unrelated configuration.
33. As a release approver, I want framework evidence separated from feature evidence, so that BUE's three-environment PASS is not copied to untested gameplay.
34. As a release approver, I want each feature artifact and DLL hash bound to its own CandidateBuild, so that a changed module cannot reuse stale evidence.
35. As a server operator, I want a feature to declare U3DS applicability independently from client UI applicability, so that headless support is explicit.
36. As a server operator, I want a feature with invalid core references rejected before startup, so that a UI dependency cannot crash the server later.
37. As a contributor, I want the public contracts and SDK repository to be transparent, so that implementation rules are reviewable rather than tribal knowledge.
38. As a contributor, I want official features and third-party features to use the same registration seam, so that the framework does not contain a privileged hidden path.
39. As a player, I want BUE to identify the author and version of each feature, so that support requests are actionable.
40. As a player, I want removing one feature DLL to leave BUE and the remaining features operational, so that optional content is truly optional.

## Implementation Decisions

- Replace the current “source contribution into one aggregate DLL” as the only supported external integration model with an explicit BUE feature registration model. The old model remains a valid internal development/build option for official features.
- Keep `FeatureId`, identity status, definition artifact, settings facets, feature-scoped bootstrap, lifecycle state, capabilities and evidence as separate facts. External loading must not reintroduce runtime `Describe()` or arbitrary FeatureId lookup.
- Use BepInEx's explicit plugin dependency/entry mechanism as the discovery seam. BepInEx discovers and loads plugin entries; BUE receives explicit registration from those entries. BUE must not call `Assembly.GetTypes()`, scan all DLLs, guess class names, directly load arbitrary DLLs or use global `PatchAll()` to discover features.
- Require each feature core DLL to expose one generated BepInEx plugin entry and one generated BUE registration call. The entry declares a BepInEx dependency on BUE and registers only after the BUE host is ready.
- Permit an optional client-only satellite entry for Glazier/Sleek/Unity presentation. The core DLL must remain within the client/U3DS reference intersection; the satellite must be process-gated and registered separately.
- Define a stable public registration contract containing the generated definition artifact, API compatibility range, module factory, static Settings Facet, dependency/capability declarations, environment intent and optional client UI registration token. The exact member list is a follow-up shared-contract change and requires Gemini review.
- Freeze registration at a deterministic bootstrap barrier before the runtime catalog and Runtime Feature Admission become active. Registrations after the barrier are rejected until restart.
- Compose accepted feature artifacts deterministically by FeatureId, schema version and digest. Duplicate identity, incompatible API, invalid artifact, missing required dependency and forbidden reference are fail-closed registration errors.
- BUE owns the unified management UI. Features provide static descriptors and snapshots, not arbitrary settings windows. Client UI extensions are optional, scoped, explicit and unloadable.
- Use feature-scoped lifecycle and resource registration. A feature exception isolates that feature; a core invariant failure triggers Core SafeMode. BUE must not promise a process sandbox: installed DLL code executes with the host process's privileges.
- Publish a conformance test kit. Framework tests cover registration, catalog composition, settings aggregation, lifecycle isolation, headless type-token gates and transport seams once. Feature tests cover the module's own gameplay behavior and declared environments.
- Keep native inventory authority unchanged. Better Item Interaction and future inventory features must still submit through the native chain and may not create a parallel inventory protocol.
- Add package metadata and support guidance that distinguish framework compatibility from feature behavior compatibility. A feature may target a subset of SP, SteamP2PFriends Host/Client and U3DS, but its declarations and evidence must match.
- Treat the current DEV-09 single BepInEx entry as the BUE framework plugin entry, not as a restriction that all future features must be source-aggregated into that DLL or as a replacement for BepInEx.

### Backend review corrections required before implementation

- The user-facing deployment model and the development assembly graph are different facts. Development may retain Contracts/Core/ClientUi/Transport projects, but the supported BUE Host deployment must have a declared physical artifact model. The recommended V1 model is a source-aggregated BUE Host assembly containing the public BUE ABI, core runtime and official feature modules; external feature DLLs must not require an unlisted Contracts/Core runtime DLL.
- The public registration interface is a shared-contract change, not an implementation detail. It must freeze the registrar, immutable definition artifact, executable factory separation, BUE dependency identity, compatibility range, stable rejection families, registration thread, lifecycle generation, late-registration behavior and client UI token semantics. A factory is executable runtime input and is never serialized into the definition artifact.
- Registration follows an explicit phase machine: `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady`. Only `RegistrationOpen` accepts registrations; feature `Start` is invoked by BUE after Admission, never directly from a feature BepInEx `Awake` callback.
- Client UI satellites are separate deployment assets. A U3DS profile must exclude them by construction; a runtime batch-mode check is necessary but not sufficient. If a single physical DLL is retained, IL/type-token and real U3DS load evidence must prove the stronger claim.
- Framework isolation begins only after BepInEx/CLR successfully loads the feature and BUE receives its registration. Pre-registration loader, type-resolution and static-initializer failures require SDK/CI preflight rejection and must not be reported as runtime `Isolated`.
- Release evidence binds a complete immutable `LoadSetIdentity`: BUE Host hash, every feature DLL hash, optional UI satellite hashes, definition/artifact digests and reference/toolchain identity. A single DLL hash is insufficient once external modules are supported.
- The SDK is compile-time tooling only. Third-party runtime code references the stable public ABI in the BUE Host; the SDK must not become a player-installed runtime dependency.
- If a feature core loads but its optional ClientUi satellite is absent or fails, BUE must project a stable presentation-degraded state (candidate labels include `PresentationDegraded` or `HeadlessOnly`) while continuing to render and edit that feature's Settings Facet through the BUE-owned settings surface.
- The BUE unified settings surface owns its native UI tree. External features provide static Settings Facets only; they cannot inject raw Glazier/Sleek/Unity controls into the settings modal. Custom HUD or gameplay presentation must use the explicit client UI satellite registration seam.
- Official Better Item Interaction must use the same public registration, lifecycle, settings and isolation seam as a third-party feature. The official distribution may bundle it physically, but it must not use a hidden privileged runtime path.
- The Shared Contract Change Request is a hard prerequisite for production implementation. It must freeze the registration, catalog entry, client UI satellite and presentation-degraded projections while preserving DEV-01～DEV-08 contracts.

## Testing Decisions

- Test through the highest seam: explicit registration and unified management runtime. Do not test private reflection helpers or incidental assembly enumeration because assembly enumeration is forbidden.
- Test registration success, duplicate identity, invalid FeatureId, API range mismatch, missing dependency, capability mismatch, invalid definition digest, forbidden UI token and late registration.
- Test deterministic catalog composition under shuffled plugin load order and repeated process starts.
- Test that no feature DLL leaves BUE operational, and that one failing feature does not prevent unrelated accepted features or vanilla fallback from remaining available.
- Test settings aggregation using static Facets plus immutable runtime Snapshots, policy overlays, revision conflicts, migration and uninstall cleanup.
- Test lifecycle start/stop/isolation/teardown idempotency and dependency cascade behavior.
- Test headless loading with core-only DLLs and verify that U3DS does not resolve client-only satellite references. Static IL/type-token scans and real U3DS loading remain separate gates.
- Test client UI satellite absence, late registration, close/reopen, generation invalidation and safe teardown.
- Test BUE API minor-version compatibility and explicit major-version rejection.
- Test framework conformance with the same CandidateBuild identity across SP, SteamP2PFriends Host/Client and U3DS where applicable; do not use framework evidence as feature gameplay evidence.
- Test Better Item Interaction separately for native authority, placement preview and projection convergence. Its results must remain bound to its own DLL hash and CaseId.
- Test the physical BUE Host deployment in a clean BepInEx installation with no undeclared Contracts/Core dependency.
- Test registration phase ordering, queueing before `RuntimeReady`, rejection after `CatalogFrozen`, and deterministic results under shuffled BepInEx plugin order.
- Test preflight rejection for missing references, invalid target framework, forbidden UI tokens and static initializer risk before the BUE isolation seam.
- Test composite `LoadSetIdentity` binding and reject evidence assembled from different BUE/feature/satellite hashes.
- Test missing or failed ClientUi satellites: the feature projects presentation degradation, while Settings Facet rendering and editing remain available through the BUE-owned settings surface.
- Test that external features cannot inject native UI controls into the unified settings modal and that official Better Item Interaction uses the same public registration seam.

## Out of Scope

- Strong sandboxing or security isolation of arbitrary third-party C# code. Explicit installation is an opt-in trust decision; BUE can validate contracts and isolate recoverable failures but cannot prevent malicious code from using host privileges.
- Automatic scanning of every DLL in the plugins directory or class-name discovery.
- A public UI ABI that exposes Glazier, Sleek or Unity types through Contracts/Core.
- Replacing BepInEx's loader, bypassing its dependency model or modifying Unturned, U3DS, LMN or the game client.
- Making every third-party feature inherit BUE's runtime PASS, release authorization or native gameplay correctness without feature-specific evidence.
- Automatic inventory exchange, reorder or parallel inventory authority.
- Hot replacement of registered feature DLLs during a running session.
- An external web service, database or remote marketplace for modules.

## Further Notes

- This proposal intentionally conflicts with the current Wayfinder wording that V1 does not dynamically load external feature DLLs. Until this proposal is accepted, the existing wording remains the active baseline and DEV-01 to DEV-09 are not retroactively reinterpreted.
- The next work package should be an architecture change review and shared-contract change request, followed by a registration/catalog prototype before production implementation.
- The first implementation target after approval should be a no-op external feature fixture, then the official Better Item Interaction migration to the same public registration seam.
- The framework can remove infrastructure fog for future authors, but it cannot remove the need to validate each feature's own native hooks, network behavior, performance and supported environment claims.
