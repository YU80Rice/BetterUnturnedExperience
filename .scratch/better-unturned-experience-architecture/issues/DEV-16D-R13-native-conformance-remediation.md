# DEV-16D-R13：Native Conformance Remediation

**Parent:** 04：DEV-16D 拖拽预览、真实图标、原生提交与投影收敛

**What to build:** 在已冻结的 DEV-16D 实现方向上完成原生符合性修复，使 Backpack、普通 Storage 与车辆 Trunk 的拖入预览、占据判断、自动旋转、真实图标和原生提交使用同一套 U3-SDK 语义，并在不确定时安全回退到原生交互。

**Blocked by:** None (can start immediately; consumes the frozen DEV-16D baseline and the approved Native Conformance Remediation specification).

**Status:** claimed

**责任变更：**相关前端实现原由 Gemini 负责，现由 GPT 接手。

- [ ] 以 `ItemJar` 左上角坐标、资产 footprint 和旋转展开唯一的 occupancy snapshot；不再把压缩的物品列表当作逐格网格。
- [ ] Preview evaluator 与 native swap guard 共同消费同一个 snapshot；同容器移动排除可验证的来源 footprint，跨容器移动不排除目标容器物品，身份不确定时 Fail-Closed 并回退原生。
- [ ] 保持已确认的原生注入 seam：当前 live `SleekItems` 的 `scroll → grid → itemsPanel` 层级、`itemsPanel` 内容挂载、顶层浮动图标、`PlayerUI.Update` heartbeat、`dragPivot`、可逆的 `onPlacedItem` 包装和原生提交路径。
- [ ] 维持页面矩阵：Backpack、普通 Storage 与车辆 Trunk 进入增强路径；Primary/Secondary、Hands、Vest、Shirt、Pants、AREA、拖出地面和未知页面保持原生 Pass-Through。
- [ ] 统一 pointer domain、viewport、grid/content 原点、UI scale、cell size、scroll、抓取偏移和旋转语义；scroll 只转换一次，越界、父链失效或非有限输入立即隐藏并回退。
- [ ] 恢复并验证 Candidate 绿色占据框、LocallyInvalid 红色占据框、Hidden 隐藏分流，以及真实 `ItemJar`/`ItemAsset` 图标身份、质量/状态、旋转和过期回调保护。
- [ ] 自动旋转仅在设置快照开启且当前方向无合法候选时尝试顺时针 90°；正方形 footprint 跳过无意义搜索，关闭开关时绝不改变方向。
- [ ] 合法普通网格释放只调用原生 `sendDragItem` 适配路径；非法候选、原生交换、装备、AREA、拖出、未知页面、功能关闭和异常路径均保持原生行为。
- [ ] 先新增并实际确认 occupancy、旋转 footprint、来源排除、单一事实源、页面分流、native hierarchy/scroll/scale、视觉分流、delegate/提交和生命周期红测失败，再以最小修复转绿。
- [ ] 完成 Release 编译、全套测试、`git diff --check`、UI/native token 门禁、单 DLL 闭包门禁和 Headless 隔离验证，结果为 0 errors / 0 warnings 且全部通过。
- [ ] 以本轮增量派发全新的 Standards 与 Spec 双轴独立审查；任一轴 FAIL 时记录阻断并循环修复，直到两轴均为 CLEAN。
- [ ] 仅在双轴 CLEAN 后生成新的单 DLL、SHA-256、CandidateBuild、BuildIdentity、LoadSetIdentity 和 CaseId；不得继承 R12 身份或旧运行证据。
- [ ] 在本工单完成前不得将 DEV-16D 标记为 `resolved`，不得交付新的 DLL 进行 DEV-16E 实机资格验证。

**Out of scope:** 不修改 DEV-16A、DEV-16B、DEV-16C 或 DEV-16E；不修改 U3-SDK、Unturned、BepInEx 或 SteamP2PFriends；不替换 `Glazier.Root`、不重写原生库存权威、不建立平行库存 RPC、不扩大到未裁决页面。

## Progress

### 2026-09-01 GPT implementation round

- `[x]` Added `ItemJar`-based canonical occupancy snapshot with rotated footprint expansion; the compact `Items.items` collection is only enumerated for jars and is never treated as a per-cell array.
- `[x]` Preview evaluation and native swap guard now consume the same drag-scoped snapshot through `INativeInventoryOccupancyProvider`; same-container source exclusion requires verifiable jar metadata and cross-container targets keep all target occupancy.
- `[x]` Added a GPT-watermarked boundary regression for stale/out-of-grid footprints. It failed before the fix (`TEST_EXIT=1`, `R13 rejects an ItemJar footprint that extends outside the native grid`) and passes after the fail-closed rejection.
- `[x]` Plugin and ClientUi targeted tests pass after the implementation round; full Release build and seven-project suite pass (`0 errors / 0 warnings`, all exit code `0`).
- `[x]` Review round R13-1 completed on the frozen `8d093f2` increment: Standards and Spec both returned `FAIL` with four blocking findings.
- `[ ]` Blocking finding 1: lifecycle dispatch and drag adapter retain only one live `SleekItems` surface, so Backpack↔Storage/Trunk routing is not bidirectional while both native pages exist.
- `[ ]` Blocking finding 2: same-container source exclusion compares the mutable `ItemJar.rot` with the original `dragFromRot`; a native rotation can reject the occupancy snapshot and cannot preserve the source's pre-rotation footprint.
- `[ ]` Blocking finding 3: release routing checks only the target page; AREA and equipment sources can still enter enhanced submission paths, violating the fixed Pass-Through matrix.
- `[ ]` Blocking finding 4: occupancy snapshot rejection clears the cache but leaves `LastPreview`; a later release can consume stale preview state instead of passing through natively.
- `[ ]` Static gates, dual-axis review, reviewed artifact identity and final resolution remain pending; this ticket is intentionally still `claimed`.

### 2026-09-01 GPT R13-2 implementation round

- `[x]` Added pointer-based target routing across both live supported pages; the drag adapter now consumes the page whose native pointer contains the cursor instead of reusing the last registered page.
- `[x]` Added explicit source-page gating for Backpack/Storage only; AREA, equipment, unknown and other pages remain native Pass-Through before preview evaluation.
- `[x]` Hidden/default previews now pass through natively before generation validation, so occupancy invalidation cannot cancel or submit stale state.
- `[x]` Occupancy invalidation clears every live surface provider and the cached preview/drag occupancy without allocating in the hot invalidation path.
- `[x]` Added GPT-watermarked pointer-routing regression; it failed against last-surface routing and passes after the pointer seam was wired.
- `[x]` Release build, seven project test runners, R13 targeted tests, UI/native token gates and `git diff --check` pass with 0 errors / 0 warnings.
- `[ ]` Fresh Standards + Spec review for this increment is pending; no DLL identity has been granted and the ticket remains `claimed`.

### 2026-09-01 GPT R13-3 remediation round (Spec FAIL recorded)

- `[x]` Spec-axis review of `6895858` returned `FAIL`; Standards axis returned `CLEAN`.
- `[ ]` Blocking finding 1: the live occupancy path still forwards mutable `ItemJar.rot` as the source rotation after native rotation; the frozen `dragFromRot` source footprint can therefore be rejected.
- `[ ]` Blocking finding 2: rebuilding one live page calls the global surface discard path and removes the other supported page; Backpack and Storage/Trunk must be independently re-bound.
- `[ ]` Blocking finding 3: session mismatch and related fail-closed paths hide only the visual sink while retaining `LastPreview`; release without another update can consume stale Candidate state.
- `[ ]` Blocking finding 4: release routing validates preview generation before source/target page support; stale visible previews from unsupported pages must return native `PassThrough` first.
- `[ ]` R13-3 red regressions, minimal fixes, fresh Release verification and a new dual-axis review are pending; this ticket remains `claimed` and no DLL may be delivered.

### 2026-09-01 GPT R13-3 implementation result

- `[x]` Red-first regressions were observed before the fixes: stale session retained `LastPreview` (`CLIENT_TEST_EXIT=1`), mutable rotation failed the frozen-source assertion (`PLUGIN_TEST_EXIT=1`), page rebuild cleared the surviving page (`R13_PAGE_RED_EXIT=1`), and stale unsupported source returned `Cancelled` (`R13_PASSTHROUGH_RED_EXIT=1`).
- `[x]` Source-footprint exclusion now derives rotation from the immutable `ItemGridPosition.Rotation` (`dragFromRot`) while current `ItemJar.rot` remains preview-only.
- `[x]` Native surface discard is page-local; the surviving live page remains registered and the plugin detaches/rebinds only the rebuilt page.
- `[x]` Session mismatch, occupancy invalidation, release, cancellation and cleanup all call the canonical `HidePreview()` path, clearing both visuals and `LastPreview` before a possible release.
- `[x]` Native release gates source page and target page before preview generation/state checks; unsupported pages always return native `PassThrough`.
- `[x]` R13 targeted tests are green (`--dev16d-r13-red`, `--dev16d-r13-boundary-red`, `--dev16d-r13-surface-red`, `--dev16d-r13-rotation-red`, `--dev16d-r13-stale-red`, `--dev16d-r13-page-red`, `--dev16d-r13-passthrough-red`, all exit `0`).
- `[x]` Release solution build is `0 errors / 0 warnings`; all seven test runners pass; Contracts/Core/ClientUi UI-native token scans and `git diff --check` pass.
- `[ ]` Fresh Standards + Spec review for R13-3 is pending; no reviewed artifact identity is granted and the ticket remains `claimed`.

### 2026-09-01 GPT R13-4 remediation round (Spec FAIL recorded)

- `[x]` Spec-axis review of `68a189c` returned `FAIL`; Standards axis returned `CLEAN`.
- `[ ]` Blocking finding 1: geometry/coordinate failure paths hide the sink but can retain `LastPreview`; a release before the next update can consume the stale Candidate.
- `[ ]` Blocking finding 2: the native callback page is not carried into `NativeDragAdapterInput`; a callback from Backpack/Storage can submit a Candidate belonging to the other page.
- `[ ]` Blocking finding 3: rebuilding a non-current live page does not invalidate cross-page drag origin, occupancy, preview or generation state; no surviving page does not end the active drag.
- `[ ]` Blocking finding 4: the page-rebuild regression only exercises a dictionary helper, not the component/delegate/preview/generation lifecycle seam required by the specification.
- `[ ]` R13-4 red regressions, minimal fixes, fresh Release verification and a new dual-axis review are pending; this ticket remains `claimed` and no DLL may be delivered.

### 2026-09-01 GPT R13-4 implementation result

- `[x]` Red-first geometry regression was observed before the fix (`ClientUi.Tests.exe` exited `1` with `geometry failure clears LastPreview before release`).
- `[x]` `InventoryGridCoordinateAdapter` now rejects non-finite viewport origin/clip values and overflowed clip bounds before evaluator entry; the presenter clears both `LastPreview` and the visual sink on every geometry/evaluator failure path.
- `[x]` Native release input carries the callback page and rejects page-mismatched candidates before generation/state handling; the runtime adapter forwards the actual callback page.
- `[x]` Any live surface rebuild invalidates the complete in-flight drag dependency graph, including cross-page source/target state, occupancy snapshot, generation and preview, while preserving a surviving supported surface.
- `[x]` Added GPT-watermarked component lifecycle coverage for cross-page rebuild cleanup and callback-page pass-through coverage at the native adapter seam.
- `[x]` ClientUi and Plugin targeted tests, Release solution build, seven test runners, UI/native token scans and `git diff --check` all pass with `0 errors / 0 warnings`.
- `[ ]` Fresh Standards + Spec review for R13-4 is pending; no reviewed artifact identity is granted and the ticket remains `claimed`.
