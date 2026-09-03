# 交付报告 — DEV-V2-04：独立 LMN 接管决策核

> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-04-lmn-takeover.md`
> 阶段：V2 第一阶段实施第 4 票（/implement + /tdd 红测 + 双轴独立审查）
> 性质：决策核实现（纯 C# Host 内部；非发布授权；**用户拍板边界 = 本票只交决策核**）
> 对照决策票：`.scratch/bue-v2-lmn-adoption/issues/V2-T5-lmn-coexistence-takeover.md`（resolved，机制冻结）

## 1. 交付内容

用户拍板（2026-09-03）：DEV-V2-04 只交付接管**决策核**；真实接线（Harmony `Priority.First` Prefix、Chainloader 绑定、面板可逆钮）归 DEV-V2-06。

`src/BetterUnturnedExperience.Core/Network/` 下两个纯 C# 类（经 Plugin 嵌入编译）：

| 能力 | 实现 |
|---|---|
| 帧识别 | `LmnFrameClassifier.IsLmnFrame(byte[])` — V1 legacy "MOD"（0x4D 0x4F 0x44）+ V2 named "LMN2"（0x4C 0x4D 0x4E 0x32）（LMN `ModRouter.cs:13-24`）；null/截断（不足魔数长）返回 false（不误判） |
| 检测 seam | `LmnTakeoverCoordinator(Func<bool> isStandaloneLmnLoaded)` — 检测探针注入；生产绑定 host 已加载插件注册表 `ContainsKey(LMN_GUID)`，测试注入 stub；协调器自身零目录扫描、零 host/bootstrap 类型接触、零引擎 token |
| 停用语义 | `ShouldShortCircuit(frame)` = `active && IsLmnFrame(frame)` — 仅接管激活 **且** 命中 MOD/LMN2 帧时短路（→ Priority.First Prefix `return false`）；非 LMN 帧恒放行 |
| 激活门槛 | `Refresh()`（调用方=网络模块初始化时）从探针读 status；探针 false → 恒不激活 → 恒放行（无 LMN 零误报）；可逆性完全由（激活状态 × 探针）决定 |
| 红线 | **未引入** `[BepInIncompatibility(LMN_GUID)]`（避免连坐 LIT/LIR/LHT 等硬依赖插件误伤） |

交付边界：不触碰 BepInEx/Engine 类型；不实现运行时卸载（BepInEx 5 不支持）；不删除/修改 LMN DLL。

## 2. TDD 循环（红测真实抓到的缺陷）

| 红测阶段 | 抓到的问题 |
|---|---|
| 编译红 CS0234 | `LmnFrameClassifier` / `LmnTakeoverCoordinator` 不存在 → 实现 |
| 帧分类红 | 截断帧（长度不足魔数）误判 → 长度门槛防护 |
| 激活门槛红 | 探针 false 时仍短路（误接管）→ Refresh 只在探针 true 才置 active |
| **token 扫描红** | `LmnTakeoverCoordinator.cs` doc 注释含字面 "BepInEx"（区分大小写 `Contains('BepInEx')`）→ 重述为 "host's loaded-plugin registry ContainsKey(LMN_GUID)" / "host/bootstrap types" / "Priority.First harmony prefix"（小写 harmony 规避 `Contains('Harmony')`）→ Core=13 文件、ClientUi=11 文件零命中 |

红测锚点：`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` `AssertBueTakeover()`（L1662-1703），经 `--bue-takeover-red` 及 no-arg 全套调用。断言：MOD/LMN2 分类、非 LMN/截断不分类、默认未激活、无 LMN 零误报、inactive 时 LMN 帧放行、active 时 MOD/LMN2 短路、active 时非 LMN 放行。

## 3. 验证矩阵

| 项 | 结果 |
|---|---|
| Release 构建（MSBuild r2 19:51:03） | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 exit=0（Plugin.Tests 19:51:03 重建通过；Release.Tests 为 09/01 陈旧构建，不受本票影响） |
| `--bue-takeover-red` | ✅ exit 0 |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 4. 双轴独立审查（R1，对照 T5 决策票逐项核对）

| 轴 | 对照依据 | 轮次 | 处置 |
|---|---|---|---|
| Standards | 仓库编码规范/并发/安全/样式 | R1 | **CLEAN** — 无 BLOCKER/WARNING；红线（超分级 纯 C# 零 token、无 BepInIncompatibility、无目录扫描）逐字面证据通过；null/截断/长度序/常量正确；风格与邻文件一致。可延后：(a) lock-free bool `active`（DEV-V2-06 接线须改 volatile/锁）、(b) doc "harmony" 小写措辞 |
| Spec | T5 Q1-Q6 决策 + DEV-V2-04 票 Scope/验收 | R1 | **CLEAN** — 6 项验收条件全 PASS；无 scope-creep（未ship DEV-V2-05/06/07 内容）；决策核 100% 纯 C#（Core.dll 引用仅 mscorlib/System.Core/System/Contracts）。验收 #28 已补勾（r2 0/0 + 7 运行器 + token 扫描证据已 log） |

- 双轴 R1 **CLEAN 汇合**，本票决策核**交付**。
- 可延后项（按名登记，不阻断）：(a) `LmnTakeoverCoordinator.active` 无 volatile——决策核不接线成立，DEV-V2-06 真实接线须改同步；(b) 文档 "harmony" 小写措辞（taste 级）。取 Blocker：无。
- 移交范围已在本票与 DEV-V2-06 票双向登记：Harmony Prefix → 实际 patch、Chainloader 绑定、面板「已由 BUE 接管」+「让我改回独立 LMN」可逆钮、三环境实机接管语义 → DEV-V2-06 / DEV-V2-07。

## 5. 解锁

- **DEV-V2-04 决策核**完成，接线面移交 **DEV-V2-06（网络模块接线）** 阻塞已备齐。
- 交付边界：纯 C# 决策核已验证；真实 Harmony Prefix / Chainloader / 面板可逆钮接线（DEV-V2-06）与三环境实机接管语义（DEV-V2-07）前，不宣称接管在宿主运行时生效。