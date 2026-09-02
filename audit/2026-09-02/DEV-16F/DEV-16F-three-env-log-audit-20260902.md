# DEV-16F 三环境实机日志审计 — 2026-09-02

> CaseId：`DEV-16F`（R2：默认物品栏/上衣/背心/裤子/背包/容器/后备箱 拿起源 + AREA(8) 地面 / 装备槽(0/1) 源解耦）
> 候选 DLL：`audit/2026-09-02/artifacts/DEV-16F-R2-20260902/BetterUnturnedExperience.dll`
> SHA-256（r2-dll-sha256.txt & 三端日志内嵌一致）：`332C51A1D1A893A5732DB3F51FF7E7B45ADF8A31A7D88F8EB8CF00BC035C86E3`
> 审计性质：**只读日志审计**；未修改任何代码。

---

## 0. 审计结论速览（TL;DR）

| # | 交付标签 | 日志文件 | 判定 |
|---|---|---|---|
| 1 | 本地联机客机 (CLIENT) | `UMM-诊断包_20260902_203145\LogOutput.log` | ❌ **不可用** — BUE 零行，快照为陈旧崩溃日志 |
| 2 | 本地联机主机 (HOST) | `UMM-诊断包_20260902_203220\LogOutput.log` | ✅ **健康**（Client 角色），1 次增强拖入提交至 page=3 |
| 3 | U3DS 测试-客户端 | `UMM-诊断包_20260902_203552\LogOutput.log` | ✅ **健康**（Client 角色），9 次增强拖入、8 次 Submitted |
| 4 | U3DS 服务器主机 | `...\UMM-v2.2.0-win-x64\LogOutput.log` | ✅ **健康**（Headless 无头安全），无客户端 UI |

**总体裁决：不建议以三环境为「关闭」依据 —— 但无任何运行异常/崩溃/隔离阻断项。**
- **无阻断缺陷**：四端均无 `BootstrapFailed` / `errorType=` / `Isolate` / `DEV-15D-CLEANUP-INCOMPLETE` / BUE 异常；U3DS 服务端严格无头。
- **评审凭据缺口（非缺陷，属证据不足）**：
  1. **本地联机客机（环境 1）无任何 BUE 证据** —— 该 `LogOutput.log` 是陈旧崩溃会话快照（内容起始 14:17:44，仅加载 SteamP2PFriends 后被截断），诊断摘要记录退出码 `-805306369`（非正常退出）。无法证明客机端增强拖入。
  2. **R2 的 AREA(8) / 装备槽(0/1) 源**：浏览器日志的 `event=drag-started` **不打印源 page**；只能证明「9 次拖入全部 `enhanced=True` 且 8 次以 `outcome=Submitted` 提交」→ 源必须 `page ≤ AREA(8)`（R2 源门），但**无法从日志文本区分**源具体为 2–7 网格 / AREA / 装备槽。
  3. **切片 B 目标页 {4,5,6,7} 渲染为「未实际作为放置目标被拖入」**：三份客户端日志中，所有 `placement-decision` 的目标均为 **page=3**（背包）；页 4/6 有 surface dispatch（日志 2）、页 5/7 持续 `surface-not-ready`，均**未被作为真正放置目标提交**。

> 判定建议：**DEV-16F 可在「无缺陷三环境运行确认」意义上通过，但若把「R2 AREA/装备拿起源实机确认」与「切片 B(4–7) 目标实机提交」作为关闭硬前置，则证据不足，应补测。** 下文给出逐端证据与逐条对应关系，供裁决者自行判定「关闭」边界。

---

## 1. 环境指纹（所有端一致）

- 候选 DLL：`BetterUnturnedExperience.dll` · 内嵌 `sha256=332C51A1D1A893A5732DB3F51FF7E7B45ADF8A31A7D88F8EB8CF00BC035C86E3`（与 `r2-dll-sha256.txt` 逐字一致）
- BepInEx 5.4.23.5 · Unturned 3.26.3.10 · Unity 2022.3.62/2022.3.62f3
- 传输：`SteamP2PFriends`；U3DS 服务端无头（BatchMode）

---

## 2. 逐端审计

### 2.1 环境 1 — 本地联机客机（CLIENT）`UMM-诊断包_20260902_203145\LogOutput.log` → **不可用**

**硬证据缺口：该日志文件内没有任何 BUE 行。**

- 文件 102 行，最后一行中断于 `[Info  `（第 102 行尾部截断）；第 13 行 `2 plugins to load`，第 14 行加载 `SteamP2PFriends`，之后全部是 SteamP2PFriends/HarmonyX 启动告警，**从未出现 Better Unturned Experience 的任何 Bootstrap 行**。
- 时间线错位：`LogOutput.log` 首行 `BepInEx ... (2026/9/2 14:17:44)`，而诊断摘要时间为 `20:31:45`；说明该文件是 **14:17 陈旧会话** 的 BepInEx 日志被诊断包快照，并非 20:31 被测会话的实时日志。
- 诊断包内 `UMM-诊断摘要.txt` 原文：
  > `结论：上次受管游戏会话未正常退出` / `最近一次受管会话：2026-09-02 20:31:41 · 模组环境 · 退出码 -805306369` / `未发现可摘录的异常线索。`
- 包内含 `Client.log`（12:24 会话）与 `Client_Prev.log`（06:24 会话），均为陈旧文件，与 20:31 测试不匹配。

**裁决**：日志 1 无法作为「本地联机客机增强拖入正常」的证据。退出码 `-805306369`（0xCFFFFFFF）表明该诊断摘录指向一次**非正常退出**（虽未匹配到崩溃细节）。**此乃三方证据中的明显缺口**（见 §5）。

---

### 2.2 环境 2 — 本地联机主机（HOST）`UMM-诊断包_20260902_203220\LogOutput.log` → **健康（Client 渲染角色）**

**身份线（R2 哈希一致）**
- L132 `event=assembly-identity path=E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll sha256=332C51A1...`
- L133 `event=runtime-gate decision=Client batchMode=False headless=False`
- L134–146 `environmentRole=Client scenario=RemoteClient`（后续 L32949 变为 `scenario=LocalAuthorityOrHost`，即主机实例的渲染侧）
- L156 `status=BootstrapReady decision=Client`
- L157 `Better Item Interaction ... accepted=True reason=None`

**接线 / surface**
- L152 `BUE inventory lifecycle wiring enabled`
- L153 `[BUE-DRAG] event=hooks-installed targets=updateDraggedItem`
- L154 `BUE drag preview wiring enabled`
- L2054/2056/2058/2061 `placed-item-delegate-rebound page=2/3/4/6`
- L2055/2057/2059/2062 `surface-context-dispatched page=2(5x3)/3(5x7)/4(5x3)/6(4x3)` —— **页 5、页 7 从未派发（持续 `surface-not-ready page=5/7`，全日志 44,576 行）**

**增强拖入活动（仅 1 次）**
- L32882 `event=drag-started generation=1 enhanced=True canRun=True sinkBound=True`
- L32883/32884/32885 … → `preview-input-readout ... state=Candidate` + `preview-evaluated` + `preview-visible`（多次，共 preview-visible×5）
- L33407 `event=projection-submitted dragGeneration=1 containerGeneration=1`
- L33408 `event=placement-decision page=3 x=1 y=1 outcome=Submitted` ← **唯一一次提交，目标页 3**
- L33409 `event=drag-cancelled`
- `placement-passthrough`：**0 次**；无 `BootstrapFailed`/`Isolate`/`errorType`/`DEV15D-CLEANUP-INCOMPLETE`
- L33405–33406（SteamP2PFriends）`[InventoryUI-Reconcile] repaired page=4 ... repaired page=3` —— 主机侧网络渲染对账，非 BUE，反映投影收敛在 host 侧可被 reconcile 接受（无 per-item 失败）

**裁决**：主机端干净运行；增强接线全部 enabled；Page 3–6 获 surface dispatch；有一次完整增强拖拽（gen1 → Candidate → `Submitted` 至背包）。但**仅一次提交、目标仅 page=3**；页 5/7 自始至终 `not-ready`（会话内未打开上衣页/储物容器）——因此本端并未实际拖入页 4/5/6/7 目标。

---

### 2.3 环境 3 — U3DS 测试-客户端 `UMM-诊断包_20260902_203552\LogOutput.log` → **健康（Client 渲染角色），增强拖入证据最全**

**身份线**
- L132 `assembly-identity ... sha256=332C51A1...`
- L133 `runtime-gate decision=Client batchMode=False headless=False`
- L156 `status=BootstrapReady decision=Client`
- L157 `Better Item Interaction ... accepted=True`

**接线 / surface**
- L152 `inventory lifecycle wiring enabled` / L153 `hooks-installed` / L154 `drag preview wiring enabled`
- L308/310 `placed-item-delegate-rebound page=2/3`
- L309/311 `surface-context-dispatched page=2(5x3)/3(7x4)` —— 本端仅页 2、3 为 live surface；页 4/5/6/7 持续 `surface-not-ready`（13,768 行）

**增强拖入活动（9 次完整拖拽，最充分）**
- `event=drag-started generation=1..9`（L1430/3104/4047/5278/6566/7633/8680/10186/11132）**全部 `enhanced=True canRun=True sinkBound=True`**
- `preview-input-readout` → `state=Candidate` + `preview-visible`：**preview-visible 共 29 次、preview-evaluated 32 次**（candidate seam 稳定到达）
- `event=placement-decision` **共 9 次**：
  - **8 次 `outcome=Submitted`**：L2838(gen1,page3,x5,y3) / L3749(gen2,x2,y3) / L4780(gen3,x6,y2) / L6224(gen4,x4,y3) / L7175(gen5,x3,y3) / L8356(gen6,x2,y1) / L10744(gen8,x2,y1) / L11803(gen9,x4,y1)
  - **1 次 `outcome=PassThrough`**：L9029(gen7,page3,x3,y1)
  - 0 次 `Cancelled`
- `event=projection-submitted`：gen1,2,3,4,5,6,8,9 共 8 次（gen7 以 PassThrough 结束，无投影提交）
- `event=placement-passthrough reason=enhanced-off`：**1 次**（L9736，触发于 gen7 尾部、`drag-cancelled` L9737 之前）——见 §3 判读
- `drag-cancelled`：9 次
- 无 `BootstrapFailed` / `Isolate` / `errorType=` / `DEV15D-CLEANUP-INCOMPLETE` / BUE 异常行；会话尾部 `SteamP2PFriends 已卸载`（正常退出）

**裁决**：本端为增强拖入最充分的证据：9 次拖拽全部进入增强流（`enhanced=True`），8 次经 BUE 判决策略提交（`Submitted`）、1 次 PassThrough，无异常、无隔离。**唯一不足：所有提交目标均为 page=3；源 page 未打印（见 §3）**。

---

### 2.4 环境 4 — U3DS 服务器主机 `...\UMM-v2.2.0-win-x64\LogOutput.log` → **健康（Headless 无头安全）**

完整 BUE 行清单（20 行日志中仅这些）：
- L14 `Loading [Better Unturned Experience 0.0.0]`
- L15 `event=assembly-identity path=E:\Steam\steamapps\common\U3DS\BepInEx\plugins\BetterUnturnedExperience.dll sha256=332C51A1...`（**同哈希**）
- L16 `event=runtime-gate decision=Headless batchMode=True headless=True` ← **正确的无头分流**
- L17 `status=BootstrapReady decision=Headless`
- L18 `Better Item Interaction ... accepted=True reason=None`（服务端仍注册 Catalog，但不建 UI）
- L20 `event=host-destroyed state=preserved patches-kept=true`（BUE-CLIENTUI-005 清理守卫，**非 UI 创建**，预期）

**不应出现项（全部通过）**：无 `BUE-CLIENTUI` composition、无 `hooks-installed`、无 `placed-item-delegate`、无 `surface-context-dispatched`、无 `drag-started`、无 `placement-decision`、无 `preview-visible`、无 `[BUE-DRAG]`/`[BUE-INVENTORY]` 客户端 Hook。服务端无 UI 类型解析、无客户端表现层实例化。

**裁决**：U3DS 服务端严格无头安全，三环境资格义务（Headless 分流 + 无客户端 UI/Hook + 同哈希）全部满足。

---

## 3. R2 关键判读（AREA / 装备槽源的可观测性与局限）

**代码事实（只读引用，未修改）**
- `ItemInteractionUiComponent.OnDragStarted` L589：`dragSourcePassThrough = !nativeAdapter.IsEnhancedSourcePage(source.Page)`
- `NativeInventoryInteractionAdapter.IsEnhancedSourcePage` L141–144：`return page <= areaPage;`（0–8 全为有效拿起源）
- `HandleRelease` L114–119：`source.Page == areaPage` → `TakeGroundItem(target)`；L121 其余 → `SendDragItem`；L105–108 同格回放 → PassThrough
- `InventoryDragPreviewAdapter.EvaluatePlacement` L788–796：`!EnhancedDragActive` → `placement-passthrough reason=enhanced-off`（**这是 R2 修复前的 8/0/1 源缺陷签名**）
- `surface-not-ready` 出自 `InventorySurfaceLifecycleAdapter.BuildSurfaceContext==null`（L1128）——目标网格原生层级未构建/未打开时的逐帧轮询噪声，并非功能失败

**可观测到（真实证据）**
- 环境 3 全部 9 次拖拽 `enhanced=True`，其中 8 次 `Submitted`。因 `Submitted` 必经 `OnDragReleased → HandleRelease` 且第一步 `IsEnhancedSourcePage(source.Page)` 须为真，故**本次 8 次提交的源均为 `page ≤ AREA(8)` 的有效源**（若为 >8 越界页会在 L66 返回 PassThrough）。这间接覆盖了 R2「源门放开到 0–8」的主干。
- `event=placement-passthrough reason=enhanced-off` 仅 1 次（环境 3 L9736），且紧邻 `drag-cancelled`：这是增强被关闭的释放瞬间而非整次拖拽 Pass-Through，**不是 R2 修复前的「整段 AREA/装备拖拽逃跑」签名** —— 说明源门本身没有把 0–8 源整段置 tuple、未复现 `enhanced=False` 症状。

**不可观测（诚实报告）**
- `event=drag-started`（`InventoryDragPreviewAdapter.cs` L615–619）只打印 `generation/enhanced/canRun/sinkBound`，**不打印 `source.Page`**；`placement-decision` 只打印 **目标** page。源页来自反射字段 `dragFromPageField`（`ReadDragSource` L869–876），无对应 log 字段。
- 因此**无法从日志文本确认**：这 9 次中的任何一次是否真的源起于 AREA(8)（走 `TakeGroundItem`）或装备槽 0/1（走 `SendDragItem`）——只能证明「源为 0–8 内的有效页」。
- `grabOffset` 在 9 次拖拽中呈多变签名（1.22,2.56 / 2.52,1.16 / … / 0.4,1.38），可反映不同物品几何/旋转，但**不足以判定源页**。
- 未观察到能直接标识 `TakeGroundItem` 或装备源提交的分支级日志（当前诊断仪器不区分二者）。

**结论**：日志证明 R2 的**源门解耦不回归**（无 `enhanced=False`/整段逃跑、提交全部来自有效源页），但**不能单独证明「地面拿起(AREA)→TakeGroundItem」「热键栏拿起(0/1)→SendDragItem」这两条 R2 专属路径被实机命中**。这是**观测性缺口**而非缺陷。若要硬性关闭 R2 的专属路径，需要在 `drag-started` 增加 `sourcePage` 字段并复测。

---

## 4. 切片 B（{2,3,4,5,6,7} 目标扩展）可观测性

- 已注册/派发：环境 2 派发 page 2/3/4/6；环境 3 派发 page 2/3。说明 `SupportedSurfacePages`/`places` 门控在 ClientUi 上是开启的（页 4/6 在该主机会话能 dispatch）。
- 但**所有真实放置提交目标均为 page=3**：环境 2 的 1 次 `placement-decision page=3`、环境 3 的 9 次 `placement-decision` 全部 `page=3`。
- 页 4/5/6/7 作为**放置目标被实际拖入提交**的证据：**无**。
- 页 5/7 在环境 2、3 中全程 `surface-not-ready`（未打开上衣页/储物/后备箱），`BuildSurfaceContext` 未构建——属「目标未打开」而非 bug，但也意味着该目标面未被实机验证渲染/提交。

**结论**：切片 B 的「目标注册」通过了（多页 dispatch），但「对页 4/5/6/7 目标的真实提交」**未被日志覆盖**。

---

## 5. 逐环境判定 + 关闭建议

### 5.1 是否「干净运行」？

| 环境 | 判定 | 依据 |
|---|---|---|
| 1 本地客机 | ❌ **证据缺失/不可用** | 日志零 BUE 行、陈旧崩溃快照、退出码 −805306369 |
| 2 本地主机 | ✅ 干净 | Client 角色、接线全 enabled、1 次增强拖拽 `Submitted`、无异常 |
| 3 U3DS-客户端 | ✅ 干净 | Client 角色、9 次增强拖拽、8 `Submitted`、无异常无隔离 |
| 4 U3DS 服务器 | ✅ 干净 | Headless 正确分流、同哈希、无任何客户端 UI/Hook |

### 5.2 阻断项

- **无运行崩溃 / 无 BUE 异常 / 无 Isolate / 无 DEV-15C 清除失败** —— 无代码级阻断缺陷可关闭报告中的可延后项（Standards S1/S2/S3、Spec S1/S2/S3/S4）进一步证实为「可延后」。
- **存在证据缺口（放行性）**：
  - B1（**硬缺口**）本地联机**客机**（环境 1）无任何 BUE 运行证据 —— 三方本地联机缺「客机」端有效性证明。严格来说满足「SteamP2P 双端（Host/Client）时间窗重叠」证据义务的只有主机端 + 另一台 RemoteClient（环境 3 亦可视为客户端），环境 1 提供不出独立客机读数。
  - B2（观测缺口）R2 的 AREA(8)/装备槽(0/1) 源命中**无法从日志文本证明**（源 page 未打点）。
  - B3（覆盖缺口）切片 B 的目标页 4/5/6/7 **从未作为放置目标被提交**；页 5/7 全程 `surface-not-ready`。

### 5.3 推荐

- **若「关闭」定义 = 三环境干净运行 + 无崩溃/隔离 + 增强拖入主干工作**：✅ **可关闭**。四端（除环境 1 不可用外）健康；环境 3 证明增强候选缝与提交策略充分工作，且 R2 源门（0–8）不回归。
- **若「关闭」定义 = DEV-16F 完整范围（R2 AREA/装备源 + 切片 B 4–7 目标）均有实机正向读数**：⛔ **暂不关闭**。须补测：
  1. 提供**本地联机客机**的有效 BUE 日志（或明确以环境 2 + 环境 3 作为 SteamP2P Host/Client 双端覆盖）；
  2. 在 `drag-started`（或等效打点）增加 `sourcePage`，实机复测**地面拿起→网格（AREA→TakeGroundItem）**与**热键栏拿起→网格（0/1→SendDragItem）**，断言 `enhanced=True` 且 `outcome=Submitted`；
  3. 实际打开上衣/背心/裤子/储物/后备箱页（4–7），把物品拖入**这些目标页**并断言 `Submitted`。

> 本审计不自动授予发布授权/Stable；发布仍按 `real-machine-test-loop.md` 与三环境资格手册由人工开发者批准。