# V5-R1 开发文档读者分层现状盘点

- **Ticket**: V5-R1
- **Date**: 2026-09-14
- **Type**: research（一手来源；只查证不改文档）
- **Scope**: 玩家手册、SDK、`CONTEXT.md`、`AGENTS.md`、`docs/adr/`、各阶段 `.scratch/*/spec.md`、架构目录 `.scratch/better-unturned-experience-architecture/`
- **Method**: 读文件头原话 + `wc -l` 行数量级 + 标题检索。不引用二手转述。行号以 2026-09-14 工作区文件为准。

---

## 1. 读者声明与长度量级

量级约定：短文 ≤80 行；中篇 80–200 行；长文 200–400 行；超长 >400 行。

### 1.1 玩家手册

- **路径**：`docs/BetterUnturnedExperience-Player-Handbook.md`（54 行，短文）
- **文件头原话**（L1–4）：

> # Better Unturned Experience · 玩家安装与升级手册（真机版）
> 适用版本：Phase-4 可视化体验正式交付版 + POST-P4 可见修复……
> 本手册面向玩家；开发与验收证据见 `audit/RELEASES.md` 行 13 所绑归档……

- **实际读者**：玩家安装/升级/故障自查。六节全是部署面（§1 这是什么 / §2 安装 / §3 从旧部署升级 / §4 防双装 / §5 联机 / §6 故障自查），无模块生命周期、无 `BueRuntimeHost.Register`。
- **入口**：根 `README.md` L33 指向本文件为「详细安装与升级」。

### 1.2 SDK（开发者契约）

- **路径**：`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`（641 行，超长；仓库内唯一 `docs/sdk/` 文件）
- **文件头原话**（L1–4）：

> # BUE 开发者契约与 SDK 引用（程序集身份 · 防双装 · 契约版本）
> 面向第三方生态功能开发者。DEV-12 建立程序集身份规则，DEV-V2-23 按冻结八节大纲扩写为完整开发者契约。
> 本文是 BUE 第三方契约的**唯一事实源**（不设第二文档目录）；措辞与运行时实现（`BueRuntimeHost` / `BuePlatformDoubleInstallCheck`）同仓库演进。

- **§1 适用范围再声明**（L8）：「**读这份文档的人**：想把功能接入 BUE 运行时的第三方开发者（生态功能作者）。」
- **结构**：正文八节（适用范围 / 承诺 / 不承诺 / 编译期引用 / FAQ / 双装 / 契约版本 / 实机验证）+ 附录 A/B/C（Phase-3 平台服务、码表、迁移）。附录定位（L233）：「正文八节冻结不动……第三阶段……的全部开发者承诺在此成文。……文档不独立创造契约」。
- **长度构成**：约 L1–230 为 2.0 正文+版本账；L231–642 为 2.1 附录。对人读而言，接入六步在 §4（L55–70），其余是契约登记与码表。

### 1.3 CONTEXT.md

- **路径**：仓库根 `CONTEXT.md`（461 行，超长）
- **文件头原话**（L1–5）：

> # 更好的未转变者体验
> 基础设施文件；当前内容由 GPT 撰写。
> 这是一个公开、开放、支持多创作者协作的《未转变者》原版体验优化插件语境。

- **实际读者**：领域词汇表，不是手册。`docs/agents/domain.md` L7 规定 agent 探索前读它。自身把「给人看的开发手册 / 给 AI 的执行规格」收成词条（L169–175），但**没有**写成那份手册。
- **结构**：L7「项目与功能」、L367「协作边界」、L449「运行环境」——词条体，夹杂决策过程（「V3-T7 定音」「V3-T4 定音」）。

### 1.4 AGENTS.md

- **路径**：仓库根 `AGENTS.md`（21 行，短文）
- **文件头原话**（L1–5）：

> ## Agent skills
> ### Issue tracker
> Issues and specs live as local Markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

- **实际读者**：仓库内 agent。操作规则指针，不讲产品、不讲接入。配套 `docs/agents/` 七份共 256 行（`output-review-loop.md` 27 行、`domain.md` 51 行、`issue-tracker.md` 31 行等），英文/中英操作 SOP，读者仍是 agent。

### 1.5 `docs/adr/`

三份，全是已接受架构决策，无「面向玩家/生态作者」声明：

| 文件 | 行数 | 文件头原话 |
|---|---|---|
| `docs/adr/0001-better-item-interaction-inward-drag-authority.md` | 5 | L1「# Better Item Interaction 只增强拖入并保留原生库存权威」；L3「**Status: accepted**」 |
| `docs/adr/0002-bue-owned-single-dll-management-panel.md` | 33 | L1「# BUE 内置单 DLL 管理面板与运行时接线」；L3「**Status: accepted**」；L5「## Context」起决策记录 |
| `docs/adr/0003-bue-auto-rotation-edge-fit-decision.md` | 56 | L1「# 自动旋转对称语义与空位边缘贴边……」；L3–4 Status/Date；L6「## Context」含 `/grill-me` 访谈过程 |

ADR-0003 把 grilling 问答写进给人看的正文（L15「经 `/grill-me` 多轮访谈（Q1/Q3/…）」；L37「Revision 2026-09-02」继续实机反馈）。读者默认是后续实施者/评审，不是第一次接入的生态作者。

### 1.6 各阶段 `.scratch/*/spec.md`

阶段规格统一形态：`Type: spec` / `Status: ready-for-agent`，读者是 agent 与开图会话。这与 `CONTEXT.md` L173–174「给 AI 的执行规格」词条一致。

| 阶段 | 路径 | 行数 | 文件头原话 |
|---|---|---|---|
| V1 架构需求 | `.scratch/better-unturned-experience-architecture/spec.md` | 317 | L1「# Better Unturned Experience V1 Requirements Specification」；L3–8「Infrastructure file; authored by GPT. / Status: ready-for-agent / … / Language: English agent-execution copy / Human-readable Chinese mirror: spec.zh-CN.md」 |
| V1 中文镜像 | 同目录 `spec.zh-CN.md` | 253 | L1「# 《更好的未转变者体验》V1 需求规格（中文版）」；L3–7「基础设施镜像文件……面向人工开发者的简体中文副本 / 英文 Agent 执行权威版：spec.md」 |
| Phase-1 LMN | `.scratch/bue-v2-lmn-adoption/spec-V2-phase1-lmn-adoption.md` | 147 | L1「# V2 第一阶段规格：LMN 官方纳入与 BueNetworkApi」；L3–7 Type/Status/Parent/Source/Author（`/to-spec` 合成） |
| Phase-2 | `.scratch/bue-v2-phase2-official-adoption/spec.md` | 263 | L1「# V2 第二阶段规格：三插件官方纳入与平台首公里」；L3–7 Type/Status ready-for-agent |
| Phase-3 | `.scratch/bue-v2-phase3-platform/spec.md` | 185 | L1「# BUE Phase-3 规格：生态开发者平台能力定界与接线」；L3「Status: ready-for-agent」；L4–5 来源=地图+决策票；澄清修订与票 Answer 冲突时以本规格为准 |
| Phase-4 | `.scratch/bue-v2-phase4-visual-experience/spec.md` | 172 | L1「# BUE Phase-4 规格：可视化体验定界与接线」；L3「Status: ready-for-agent」 |
| 历史草案 | `.scratch/better-un-experience-architecture/spec.md` | 36 | L1「# 更好的未转变者体验（Better Unturned Experience）架构规格书」；L3「**SUPERSEDED / 历史草案**：该文件包含 GPT-15 前的旧接口假设，不得作为实施规格。」 |

Phase-5 目录只有 `map.md` + `issues/`，**没有** `spec.md`（本阶段尚未 `/to-spec`）。

### 1.7 架构目录 `.scratch/better-unturned-experience-architecture/`

- **地图头**（`map.md` L1–4，80 行）：「# 更好的未转变者体验：前后端职责与可扩展架构决策地图 / 基础设施文件；当前内容由 GPT 撰写。」L15 自称「当前 effort 的唯一规范决策地图」，并声明 `.scratch/better-un-experience-architecture/` 为历史工作区。
- **读者分层（文件头原话）**：
  - 英文执行规格 vs 中文人工镜像：`spec.md` L7–8 / `spec.zh-CN.md` L6–7；开放运行时提案同样双语（`spec-open-runtime-feature-framework.md` L3–6「Proposal specification; authored by GPT / Chinese human-readable mirror」及其 `.zh-CN.md` L3–6）。
  - 子规格普遍写「作者: GPT/Gemini」「状态: …draft / 实现与运行未验证」，例如 `Module-Lifecycle-Isolation-Spec.md` L1–5、`Shared-Contract-Spec.md` L1–6、`Frontend-Architecture-Spec.md` L3–7。
- **长度量级（同目录 `wc -l`）**：`Shared-Contract-Spec.md` 862（最长）；`RT-04` 401、`RT-05` 367、`Contribution-Build-Release-Gates-Spec.md` 344、`RT-01` 319、`spec.md` 317、`Module-Lifecycle-Isolation-Spec.md` 258、`SCR-GPT18-001-Contract-Proposal.md` 260。短文只有 closure/manifest（24–76 行）。合计该目录 markdown 约 7757 行。
- **姊妹历史目录** `.scratch/better-un-experience-architecture/`：`spec.md` 36 行已 SUPERSEDED；`Frontend-Architecture-Spec.md` 155 行；`map.md` 64 行。

### 1.8 对照表（T2 四层 + 现存落点）

| 层（CONTEXT L169–175 / T2 题面） | 今天落点 | 行数量级 | 文件头自称的读者 |
|---|---|---|---|
| 玩家手册 | `docs/BetterUnturnedExperience-Player-Handbook.md` | 54 | 「面向玩家」 |
| SDK 契约 | `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` | 641 | 「面向第三方生态功能开发者」「唯一事实源」 |
| 给人看的开发手册 | **不存在独立文件**（只有 CONTEXT 词条 L169–171） | — | 词条目标=「人类贡献者和生态作者」「一图流」 |
| 给 AI 的执行规格 | 各阶段 `spec.md` + 票面 | 147–317 / 阶段 | `ready-for-agent`；CONTEXT L174「读者是 agent 和开图会话」 |
| 词汇表（非四层之一） | `CONTEXT.md` | 461 | 「基础设施文件」 |
| Agent 操作规则 | `AGENTS.md` + `docs/agents/` | 21 + 256 | Agent skills |
| 架构决策包 | `.scratch/better-unturned-experience-architecture/` | ~7.7k | 基础设施 / draft / agent-execution |

---

## 2. 生态作者今天按规定应读哪一份？是否有第二份「开发规格」抢权威？

### 2.1 规定应读（现行对外入口）

仓库对外入口把生态作者指向 **一份 SDK + 一个活样板**：

1. **根 README「给生态开发者」**（`README.md` L35–41）：「**开发者契约（唯一事实源）**：`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`」；活样板 `src/BetterUnturnedExperience.NoOpFixture/`；平台 API 面继续按契约 2.1 接入。
2. **SDK 自己**（L3–4、L8）：「本文是 BUE 第三方契约的**唯一事实源**（不设第二文档目录）」；读者=生态功能作者。
3. **Phase-2 spec「开发者文档」**（`.scratch/bue-v2-phase2-official-adoption/spec.md` L206–208）：「扩写现有 `docs/sdk/…`，**不新建第二文档目录（避免双事实源）**。大纲八节冻结……」
4. **Phase-3 spec Solution**（`.scratch/bue-v2-phase3-platform/spec.md` L22–24）：「全部语义汇入 SDK 契约文档三个新附录……形成**单一 SDK 契约面**。」「从使用者视角：生态作者读一份文档、对照一个活样板」。
5. **CONTEXT「开发者契约」**（L189–190）：契约面=SDK 文档明确列举并登记的成员。
6. **CONTEXT「给人看的开发手册」避免项**（L171）：「与 SDK 抢权威」。

**结论（规定）**：生态作者接入 BUE，按规定应读 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`，并对照 `src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`。玩家手册不是开发入口。

### 2.2 是否存在第二份「开发规格」与 SDK 抢权威？

**契约权威：没有第二份现行对外文档目录。** SDK 与 Phase-2/3 spec、README 三者同句「唯一事实源 / 不新建第二目录」。`docs/sdk/` 只有这一份。

**但仓库里仍有多份会让人当成「开发规格」的长文**，权威声明彼此不完全同一时代：

| 竞争者 | 自称权威 | 与 SDK 关系 | 是否抢现行契约权威 |
|---|---|---|---|
| Phase-3 `spec.md`（185 行） | ready-for-agent；L5「与票 Answer 冲突时以本规格为准」 | L142–147 把语义**汇入** SDK 附录，本身是实施规格 | 对 agent 实施抢票面，不对外替代 SDK |
| Phase-2 `spec.md`（263 行） | ready-for-agent | L206–208 明确扩写现有 SDK、禁止第二目录 | 否；它是 SDK 八节的来源规格 |
| V1 `spec.md` + `spec.zh-CN.md`（317+253） | English agent-execution / 中文人工阅读镜像 | V1「不动态加载外部功能 DLL」（英文 spec L28） | 时代错位：V1 产品边界已被开放运行时提案/后续阶段超越，但仍是双语「给人看」长规格 |
| `spec-open-runtime-feature-framework.md` 双语（149+149） | Proposal；L5 须人工+GPT+Gemini 接受后才取代 V1 | 设想独立 `BetterUnturnedExperience.SDK` 包（英文 L23） | 提案级；现行 SDK 文档写「独立 SDK 程序集拆分——暂缓」（SDK L88） |
| `Shared-Contract-Spec.md`（862） | L5「非 Stable；实现与运行未验证」；L6「唯一决策地图: map.md」 | V1 共享 interface 草案 | 草案，不是 2.1 对外契约 |
| `SCR-GPT18-001-Contract-Proposal.md`（260） | L3「Status: proposed-for-dual-review」 | 注册桥提案；SDK 仍引用 SCR-GPT18-001 为注册桥出处 | 提案；运行时已落地，文档身份仍是 proposal |
| `Module-Lifecycle-Isolation-Spec.md`（258） | L5「GPT-09 决策基线；实现与运行未验证」 | V1 九态状态机 | 与现行 SDK 附录 A.3 并行存在，未标 SUPERSEDED |
| 历史 `.scratch/better-un-experience-architecture/spec.md` | L3 SUPERSEDED | 明确不得作实施规格 | 否 |
| CONTEXT 词条「给人看的开发手册」 | 形态目标，无文件 | L171 禁止与 SDK 抢权威 | 文件尚未诞生，故今天不抢 |

**抢权威的实际形态不是第二份 `docs/sdk/`，而是：**

1. **给人看的开发手册缺席** → 第一次读仓库的人类若不想啃 641 行 SDK，会掉进 `.scratch` 双语规格 / Shared-Contract 862 行 / Phase spec。CONTEXT L171 已点名这是禁止贴进给人看正文的材料——恰恰说明它们现在会冒充「开发规格」。
2. **V1 生命周期规格未退役标**（`Module-Lifecycle-Isolation-Spec.md` L5 仍写「实现与运行未验证」）与 **SDK 附录 A.3 现行生命周期**（契约 2.1，DEV-V3-03）并存。生态作者若按架构目录读，会读到九态表（该文件 L16–28）而非 SDK A.3 的 `FeatureStatusView` / `TryTrack` / 代际规则。
3. **开放运行时提案**仍写独立 SDK 包（`spec-open-runtime-feature-framework.md` L23），与现行「引用主 DLL、不拆 SDK 程序集」（SDK L88；Phase-2 spec L197）并列。提案头要求「明确接受后才取代」，但文件仍 `ready-for-agent`，目录未标 SUPERSEDED。

**一句话**：现行**契约**权威是单一 SDK；现行**给人读的开发规格**权威真空。真空里 V1 双语 spec、Shared-Contract、生命周期隔离 spec、开放运行时提案都可能被当成第二份开发规格。这正是 T2 要裁的分层。

---

## 3. 有没有「一图流 / one-pager / 模块生命周期图」？最接近的短文

### 3.1 检索结果

全仓库 `*.md` 检索「一图流 / one-pager / one pager」：

- **作为已交付文档标题或形态：零命中。**
- 仅出现在**目标声明**里：`CONTEXT.md` L169–170（给人看的开发手册「形态目标是一图流」）；Phase-5 `map.md` L30；本票 `09-r1-docs-landscape.md` L17；T2 `02-t2-developer-handbook.md` L11。

`mermaid` 图：`docs/`、`README.md`、SDK、各阶段 `spec.md`、`Module-Lifecycle-Isolation-Spec.md` **均无**。唯一命中是架构 handoff `.scratch/better-unturned-experience-architecture/handoffs/to-progress-sync.md` L104–122，内容是 GPT/Gemini **票据依赖** `graph TD`，不是模块生命周期、不是接入步骤。

玩家手册、README、ADR、AGENTS 均无生命周期状态机图。SDK §1 有一段 6 行 ASCII 链（L17–25，见 §4），不是状态机图。

### 3.2 最接近「一图流」的短文（按接近度）

「一图流」在 CONTEXT L170 的定义是：短开发说明，讲模块生命周期如何、怎样接入 BUE 生态。按这个尺子，现存最接近、且短的是：

1. **`README.md` §给生态开发者**（L35–41，约 7 行）  
   三句话：官方 vs 生态两层、SDK 唯一事实源、NoOp 活样板、平台 API 面按 2.1。无生命周期图，但是对外最短入口。

2. **SDK §1 生态 DLL 生命周期 ASCII 链**（`docs/sdk/…` L15–25）  
   四步箭头：独立 BepInEx 项目 → HardDependency → 引用主 DLL → `BueRuntimeHost.Register` → 消费平台服务。这是仓库里**唯一**把「接入路径」画成一段图的正文。后面仍要跳到 §4 六步与附录 A。

3. **SDK §4「生态 DLL 的接入六步」**（L55–70）  
   编号清单，不是图；对人读已经是最短可执行接入说明。

4. **SDK 附录 C.6 上架前自检清单**（L627–641，11 项勾选）  
   发布前核对，不是生命周期图，但是短、可执行。

5. **玩家手册**（54 行）  
   短，但读者声明是玩家，内容是安装升级，不回答「怎样接入生态」。

6. **`Module-Lifecycle-Isolation-Spec.md` §2 / §2.1**（L12–47）  
   九态 `enum FeatureState` + 合法转换表。这是最接近「模块生命周期图」的**表格**，但是 V1 draft、258 行全文、头写「实现与运行未验证」，且与现行 2.1 A.3 不是同一份权威。

**不接近、不要误当成 one-pager**：SDK 全文 641 行、CONTEXT 461 行、Shared-Contract 862 行、V1 双语 spec、各阶段 spec。它们都把决策过程或契约登记写进正文。

**结论**：不存在已交付的一图流或模块生命周期图。最接近的短入口是 README L35–41 + SDK L15–25 的 ASCII 链；最接近的状态表是 V1 `Module-Lifecycle-Isolation-Spec.md` §2.1，但那份不是现行契约、也不是短文。T2 要写的「给人看的开发手册」今天是空槽。

---

## 4. 生命周期与接入步骤今天散落在哪些章节

只列路径 + 标题，不抄全文。玩家手册几乎不参与开发接入（仅部署/双装/面板启停观测）。

### 4.1 SDK（现行对外契约面）

| 路径 | 标题 | 内容性质 |
|---|---|---|
| `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` §1 L15–25 | 「一个生态 DLL 的完整生命周期」ASCII 链 | 接入路径总览 |
| 同文件 §4 L55–70 | 「生态 DLL 的接入六步」 | 项目/前置/引用/禁止捆绑/Register/消费服务；L70 一句 `IFeatureModule.Start/Stop` |
| 同文件 附录 A.1 L241 | 「Admission 与 Bootstrap」 | 注册桥、判定顺序、可用性矩阵、活样板登记姿势 |
| 同文件 附录 A.3 L316 | 「生命周期与资源」 | `FeatureStatusView` 唯一投影、`TryTrack`、代际、Isolated 不自动重启、Start/Stop 顺序 |
| 同文件 附录 B.4 L498 | 「生命周期码 BUE-LIFE」 | 诊断码表 |
| 同文件 附录 C.6 L627 | 「生态 DLL 上架前自检清单」 | 11 项人工核对 |
| 同文件 附录定位 L237 | 统一生态契约 probe 链序 | 「注册→Bootstrap→Events→TryTrack/Lifecycle→Network→HostTick→Settings→Logger→停止与隔离」 |

### 4.2 Phase-3 spec（给 AI 的执行规格；语义来源）

| 路径 | 标题 |
|---|---|
| `.scratch/bue-v2-phase3-platform/spec.md` L20「## Solution」 | 八缝接线 + 单一 SDK 契约面 |
| 同文件 L96「### 生命周期（V3-T4 → DEV-V3-03）」 | 状态投影、TryTrack、代际、CoreSafeMode |
| 同文件 L142「### SDK 契约文档（V3-T9 → DEV-V3-08）」 | 附录 A/B/C 总装、NoOp probe、自检清单 |
| 同文件 L160 Testing「3. 生命周期」 | 假模块状态机红测面 |

### 4.3 Phase-2 / Phase-1 spec（接入契约的前身）

| 路径 | 标题 |
|---|---|
| `.scratch/bue-v2-phase2-official-adoption/spec.md` L193「### 开发者契约与 SDK 引用」 | 三段式承诺；L206「### 开发者文档」冻结八节、禁止第二目录 |
| 同文件 L220 Testing「2. 宿主注册与生命周期面」 | `IFeatureRegistration` + Start/Stop |
| `.scratch/bue-v2-lmn-adoption/spec-V2-phase1-lmn-adoption.md` L105「### 生态与前置」 | 第三方按 SCR-GPT18-001；「SDK/文档形态后续定义」 |
| 同文件 L135 Out of Scope | 「第三方生态功能生产接入的完整 SDK/文档工具链」当时不做 |

### 4.4 Phase-4 spec（玩家侧启停表面，不是生态接入教程）

| 路径 | 标题 |
|---|---|
| `.scratch/bue-v2-phase4-visual-experience/spec.md` L82「### 生命周期目标提交（V4-T4 → DEV-V4-03）」 | 面板提交目标启停、空操作成功 |
| 同文件 L88「### 官方 legacy enabled 迁移」 | 旧 enabled → UserDisabled |
| 同文件 L93「### 功能级启停表面」 | 详情页「启用」开关 vs 只读 FeatureState |
| 同文件 L122 文案表 NoOpFixture 行 | 「生态接入样板」——面板呈现，不是 Register 教程 |

### 4.5 NoOpFixture 注释（活样板，不是文档）

路径：`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`（608 行）

| 位置 | 标题性注释 |
|---|---|
| L27–32 `ProbeStepOutcome` XML | 「DEV-V3-08 … unified ecosystem contract probe」；「Sample fixture surface, not SDK contract」 |
| L141–145 | 冻结链序：注册→Bootstrap→Events→TryTrack/Lifecycle→Network→HostTick→Settings→Logger→停止与隔离 |
| L224–226 `Register()` | `BueRuntimeHost.Register(ProbeRegistration)` |
| L271–287 `NoOpModule` XML | Start 走冻结链序；「living counterpart of SDK appendices A/B」 |
| L297 `Start(IFeatureBootstrap)` / L320 `Stop` | 运行期生命周期钩子 |
| L327 Bootstrap seam / L356 Events seam | 分缝注释 |

SDK 附录 A 的示例代码声明「逐字取自」本文件（SDK L234）。

### 4.6 玩家手册（部署，不是开发接入）

`docs/BetterUnturnedExperience-Player-Handbook.md`：

- §2 L17「安装（全新玩家）」
- §3 L26「从旧部署升级」
- §4 L38「防双装与身份注意事项」
- §6 L50「故障自查三步」（面板「启用」开关 + 保存配置）

无 Register、无 Start/Stop、无 FeatureState。

### 4.7 架构目录（V1 生命周期，未标退役）

| 路径 | 标题 |
|---|---|
| `.scratch/better-unturned-experience-architecture/Module-Lifecycle-Isolation-Spec.md` L1 / L8 / L12 / L31 | 「模块生命周期与故障隔离状态机」；§2 功能模块状态；§2.1 合法转换 |
| 同目录 `Backend-Architecture-Spec.md`（约 L36） | 指向上一份为 GPT-09 唯一详细决策源 |
| 同目录 `SCR-GPT18-001-Contract-Proposal.md` L17「### 2.1 三层装载关系」 | BepInEx 装载 ASCII |
| 同目录 `spec-open-runtime-feature-framework.md` L15「## Solution」 | 前置框架 + 独立功能 DLL 的产品切分 |
| `CONTEXT.md` L109「功能状态投影」；L421「生命周期代际」；L425「注册阶段」 | 词汇，不是步骤 |

### 4.8 散布结论

接入步骤的**现行可执行叙述**在 SDK §1 ASCII + §4 六步 + 附录 A.1/A.3 + C.6，并由 NoOpFixture 注释双向锚定。  
生命周期**状态机细节**另有三处：SDK A.3（2.1 现行）、Phase-3 spec §生命周期（agent 规格）、V1 Module-Lifecycle-Isolation-Spec（draft 九态表）。  
玩家手册只覆盖安装与面板开关观测。给人看的「一张图走完生命周期+接入」不存在。

---

## 5. 「雷霆长文」最可能指向哪几份

T2 用户原话（`issues/02-t2-developer-handbook.md` L11）：「仓库里给人看的只有使用和升级手册；开发规格被另一位人类开发者评为『狗屎雷霆长文』」。CONTEXT L171 把「`.scratch` 决策长文、AGENTS.md、架构双语规格」列为给人看的手册必须避开的东西。

判定轴：行数、是否双语、是否把决策过程（grilling 问答、票号定音、澄清修订）写进给人看的正文。AGENTS.md 只有 21 行，不像「雷霆」本体，更像被点名禁止误贴的操作规则。

### 5.1 最可能（按命中轴排序）

| 排序 | 路径 | 行数 | 双语 | 决策过程进正文 | 为何像「给人看的开发规格」 |
|---|---|---|---|---|---|
| 1 | `.scratch/better-unturned-experience-architecture/spec.md` + `spec.zh-CN.md` | 317 + 253 | **是**（头写 English agent-execution / 中文人工阅读副本） | User Stories 43 条 + 三层调研任务；中文版明确「面向人工开发者」 | 唯一自称「给人读」的开发规格对 |
| 2 | `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` | 641 | 否（中文） | §7 按票登记 ①–⑦ + 2.1 批次构成；附录每节「出处：DEV-V3-xx / V3-Tx」；§8 实机补注整段保留 | 对外唯一开发者文档，却把契约账本、码表、实机勘误写进同一文件；接入六步被埋在中间 |
| 3 | `.scratch/better-unturned-experience-architecture/Shared-Contract-Spec.md` | 862 | 否 | 全文 interface/DTO 冻结；头写非 Stable | 仓库单文件最长规格，目录名为「契约」易被当成 SDK |
| 4 | `.scratch/bue-v2-phase3-platform/spec.md` | 185 | 否 | L5 整段「澄清修订」「二轮复查」；L96 起按 V3-T1..T9 重放裁决 | 标题含「生态开发者平台」；Solution 还说「作者读一份文档」——但这份本身是 agent spec |
| 5 | `.scratch/better-unturned-experience-architecture/Module-Lifecycle-Isolation-Spec.md` | 258 | 否 | GPT-09 决策基线；九态+转换表+清理义务 | 生命周期主题，读起来像手册核心，头仍「未验证」 |
| 6 | `CONTEXT.md` | 461 | 否 | 大量词条尾注「V3-Tx 定音」 | 不是手册，但人类打开仓库常先读它 |

### 5.2 次可能（长、决策过程重，但更像研究/实施规格）

- `RT-04-Inventory-Authority-Container-Research.md`（401）、`RT-05-…`（367）：研究笔记，标题不叫开发规格。
- `Contribution-Build-Release-Gates-Spec.md`（344）、`spec-DEV-16*.md`（215–236）：实施规格。
- Phase-2 spec（263）：含「开发者契约」整节，但是 ready-for-agent。
- `spec-open-runtime-feature-framework.md` 双语（149+149）：提案，产品愿景口吻，独立 SDK 包表述已过时。
- ADR-0003（56 行）：短，但 grilling Q 序列进正文，说明「决策过程写进给人看的正文」在仓库里是常态。

### 5.3 不太可能

- 玩家手册 54 行：用户原话承认「给人看的只有使用和升级手册」，抱怨对象是**开发规格**不是它。
- `AGENTS.md` 21 行：太短；CONTEXT 禁止的是把它**贴进**给人看的手册，不是说它本身是雷霆。
- ADR-0001/0002、历史 SUPERSEDED spec（36 行）。

### 5.4 综合判断

「雷霆长文」最像一次把 **V1 双语需求规格（给人读的那份）+ 现行 641 行 SDK 账本 + 架构目录里未退役的 Shared-Contract / 生命周期 spec** 混在一起读的体验。共同特征：

- 没有一份短的「怎样接入」人读手册顶在前面；
- 自称给人读的那份（`spec.zh-CN.md`）仍是 V1、253 行、43 条故事；
- 现行该读的 SDK 把接入六步、版本演化、码表、实机勘误订成同一超长文件；
- `.scratch` 决策长文未标退役，和 SDK 抢「生命周期/契约」主题。

这与 CONTEXT L171 的避免项逐条对齐，可作为 T2 分层时「禁止贴进给人看正文」的现存清单。

---

## 6. 给 T2 的事实摘要（不代替裁决）

1. 四层里三层有文件（玩家手册短、SDK 超长、AI spec 按阶段存在），**给人看的开发手册没有文件**，只有 CONTEXT 词条。
2. 生态作者按规定只应读 SDK + NoOpFixture；契约层无第二 `docs/sdk/`。抢权威来自 `.scratch` 未退役规格，不是第二份对外 SDK。
3. 一图流 / 生命周期图未交付。最接近短入口：README「给生态开发者」+ SDK §1 ASCII 链。
4. 接入与生命周期散落 SDK §1/§4/A.1/A.3/C.6、Phase-3 spec 生命周期节、NoOpFixture 注释、V1 Module-Lifecycle spec、玩家手册仅部署。
5. 「雷霆长文」优先嫌疑：V1 `spec.md`/`spec.zh-CN.md`（双语、面向人工开发者）、SDK 641 行账本、`Shared-Contract-Spec.md` 862 行、Phase-3 spec、V1 生命周期 spec、CONTEXT。

T2 仍需裁：给人看的手册是否只引用 SDK、落点路径、一图流页数、AI 规格是否继续用阶段 `spec.md`。本报告不裁决。

