> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-DEV-07-Candidate-Qualification-Review：DEV-07 候选构建与三环境资格门禁终审复核报告

> **作者**: Gemini（前端负责人 / 验收消费方）  
> **接收方**: GPT（后端负责人、架构与发布门禁） / 人工开发者  
> **复核方法论**: `diagnosing-bugs`（边界不变性审计、时间窗重叠诊断、跨环境同哈希防伪、Fail-Closed 防御） + `codebase-design`（深度模块与资格评估 Seam）  
> **复核对象**: DEV-07 交付物（`CandidateBuildDescriptor`, `EvidenceCase`, `QualificationPolicy`, `QualificationEvaluator`, `BetterUnturnedExperience.Release.Tests`, `Implementation-DEV-07-1340.md`）  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **SourceSet 身份**: `BUE-SS-20260824-02`  
> **CandidateBuild DLL SHA-256**: `7AFAECF1C569AA57150E7CE113810E18271AE6DF9850D7C4682779DB1336ED7A`  
> **判定结论**: **ACCEPT（候选构建与三环境资格终审全量通过，无阻断异议，正式签署验收 DEV-07）**  

---

## 一、 核心复核结论与判定矩阵

| 审查维度 | 判定结果 | 前端/验收消费核查说明 |
| :--- | :---: | :--- |
| **1. BuildIdentity 纯净身份** | **`ACCEPT`** | `BuildIdentity` 仅由 source、definition、artifact、toolchain、client/u3ds reference-set 生成确定性 SHA-256；DLL hash 与运行状态不污染构建身份。 |
| **2. 完备的 EvidenceCase 溯源** | **`ACCEPT`** | 包含 CaseId、环境角色、部署来源、环境指纹、版本、UTC 时间窗、命令、证据引用/摘要与诊断，支持前端状态中心完整溯源。 |
| **3. U3DS Headless 核心义务不可豁免** | **`ACCEPT`** | `QualificationPolicy` 强制 `U3dsHeadless == true`（试图禁用抛 `ArgumentException`）；只有 `U3dsClientUi` 允许政策性 `NotApplicable`。 |
| **4. P2P Host/Client 多用例配对** | **`ACCEPT`** | 严格校验 P2P 双端必须具有相同 `CaseId`、相同 Candidate 身份、相同 `DllSha256` 且 UTC 时间窗真实重叠（`Overlaps`），杜绝拼凑证据。 |
| **5. 资格状态严密性 (Fail-Closed)** | **`ACCEPT`** | `QualificationVerdict`（`Fulfilled`, `Failed`, `Missing`, `Stale`, `NotApplicable`）语义清晰，前端绝不把 `Fulfilled` 伪报为“已发布”或“已获发布授权”。 |
| **6. 跨模块隔离与零类型泄漏** | **`ACCEPT`** | Release 模块仅依赖 BCL 基础集合与加密库；Contracts/Core/ClientUi/Transport/Release 零 UI/Native/LMN 类型泄漏。 |

---

## 二、 深度缺陷诊断与边界审计（Diagnosing Bugs & Boundary Audit）

经对 `CandidateBuild.cs` 与 `Qualification.cs` 源码进行细致入微的防御性与缺陷审计：

### 1. 时间窗重叠诊断（`Overlaps`）
* **数学与边界验证**：
  ```csharp
  private static bool Overlaps(EvidenceCase left, EvidenceCase right)
  {
      return left.StartedUtc <= right.EndedUtc && right.StartedUtc <= left.EndedUtc;
  }
  ```
  * 强制前置检查 `startedUtc.Kind == DateTimeKind.Utc && endedUtc.Kind == DateTimeKind.Utc && endedUtc >= startedUtc`。
  * 闭区间重叠算法完全正确无偏，杜绝了时区混乱、时间倒流或伪造单端证据冒充双端联调的情况。

### 2. 跨代际与篡改防御（Hash Mismatch & Stale Detection）
* 当某环境证据的 `DllSha256` 与当前评估的 `candidate.DllSha256` 不一致时，`EvaluateRole` 精确返回 `QualificationVerdict.Stale`；
* 缺少对应环境记录返回 `QualificationVerdict.Missing`；
* P2P 存在记录但未找到有效重叠配对时，Host 与 Client 均被 Fail-Closed 降级为 `QualificationVerdict.Failed`。

### 3. 确定性规范化与序列化（`SerializeCanonical`）
* `EvidenceCase.SerializeCanonical()` 与 `CandidateBuildDescriptor.Canonical()` 采用长度前缀 `length:value\n` 格式进行确定性拼接，完全消除字段值内含分隔符导致的注入歧义。

---

## 三、 前端 Presenter 状态中心消费对齐

1. **设置与状态中心展示**：
   * 前端设置面板（`SleekFeatureSettingsModal`）和模块状态徽章将消费 `QualificationResult`：
     - 若 `U3dsHeadless` 为 `Fulfilled`：标记“服务端核心已验证”；
     - 若 `U3dsClientUi` 为 `NotApplicable`：灰度隐藏或提示“UI 仅限客户端”；
     - 若 P2P 双端为 `Fulfilled`：展示“P2P 联机双机认证通过（同 CaseId & 同 DLL 哈希）”。
2. **证据边界严格维持**：
   * 前端确认：DEV-07 交付的资格评估引擎与测试套件证明了**验收门禁与证据链校验算法的正确性**，但**不代表已经获得真实游戏环境（SP、SteamP2PFriends Host/Client、U3DS）的运行 PASS 或发布授权**。

---

## 四、 终审结论与签字

* **前端判定**：**`ACCEPT`（全量通过，无异议，无修改项）**。
* **工单状态**：同意正式将 [`DEV-07-candidate-build-three-environment-gate.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/DEV-07-candidate-build-three-environment-gate.md) 标记为 **`resolved`**。
* **全流程闭环**：**DEV-01 ～ DEV-07 全部研发、架构、网络、表现层与交付资格门禁已 100% 闭环并完成双智能体终审！**

---

*报告完。作者: Gemini*


