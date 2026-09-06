> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-DEV-08-Evidence-Package-Review：DEV-08 运行证据包与校验器终审复核报告

> **作者**: Gemini（前端负责人 / 验收消费方）  
> **接收方**: GPT（后端负责人、架构与发布门禁） / 人工开发者  
> **复核方法论**: `diagnosing-bugs`（路径逃逸防御、SHA-256 双向比对、结构化诊断、全有或全无 Fail-Closed） + `codebase-design`（不可变包模型、确定性序列化、清晰验证 Seam）  
> **复核对象**: DEV-08 交付物（`RuntimeEvidenceArtifact`, `RuntimeEvidencePackage`, `RuntimeEvidencePackageValidator`, `BetterUnturnedExperience.Release.Tests`, `Implementation-DEV-08-1435.md`）  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **SourceSet 身份**: `BUE-SS-20260824-02`  
> **产物 DLL SHA-256**: `1F4451D0FBE238AE9D5EA4E6BE0CD5CA0880F5D10D1F519FE230D489FF3CE42A`  
> **测试 EXE SHA-256**: `18E4A1D7D40E81E82A0221CA2E1EB416C61B89F9E6590B70D22C44AC56F32680`  
> **判定结论**: **ACCEPT（运行证据包与校验引擎终审全量通过，无阻断异议，无契约缺口，同意工单保持 ready-for-human 待人工采集三环境证据）**  

---

## 一、 核心复核结论与判定矩阵

| 审查维度 | 判定结果 | 前端/验收消费核查说明 |
| :--- | :---: | :--- |
| **1. 相对路径安全防御** | **`ACCEPT`** | `RuntimeEvidenceArtifact` 严格拦截绝对路径（`/`、`:`）、路径遍历（`..`、`.`）与空段；同时允许合法包含点号的文件名（如 `logs/v1..txt`）。 |
| **2. 确定性序列化与 Digest** | **`ACCEPT`** | `SerializeCanonical()` 强制按 `CaseId` 与 `RelativePath` 进行 Ordinal 排序，生成确定性长度前缀流并计算 64 字符 SHA-256 `CanonicalDigest`。 |
| **3. 证据案例与文件双向核验** | **`ACCEPT`** | 校验器严格断言每个 EvidenceCase 的 `EvidenceReference` 必须存在于包内 Artifact 清单，且案例的 `EvidenceDigest` 必须精确等于文件的 `Sha256`。 |
| **4. 防御性结构化诊断 (Fail-Closed)** | **`ACCEPT`** | 包含 11 项结构化错误码（`MissingPackage`, `CandidateMismatch`, `DuplicateCaseId`, `DuplicateArtifactPath`, `MissingArtifactReference`, `EmptyPackage`, `ArtifactDigestMismatch`, `NullEvidenceCase`, `NullArtifact`, `InvalidArtifactDigest`, `Valid`），遇到 null/空/畸变输入安全降级不崩溃。 |
| **5. 语义清晰与发布权限隔离** | **`ACCEPT`** | 证据包校验器仅输出完整性判定，绝不越权修改 `QualificationVerdict`、`FeatureState` 或自动授予发布权限。 |
| **6. 跨模块隔离与零类型泄漏** | **`ACCEPT`** | Release 模块仅依赖 BCL 基础类型；Contracts/Core/ClientUi/Transport/Release 对外部引擎/UI/LMN 类型维持零泄漏。 |

---

## 二、 深度缺陷诊断与路径安全审计（Diagnosing Bugs Audit）

经对 `RuntimeEvidencePackage.cs` 源码进行细致入微的安全与缺陷审计：

### 1. 路径遍历与逃逸拦截验证
* **路径验证规则**：
  ```csharp
  var path = Required(value, nameof(value)).Replace('\\', '/');
  var segments = path.Split('/');
  if (path.StartsWith("/", StringComparison.Ordinal) || path.IndexOf(':') >= 0 || path == "." || Array.Exists(segments, segment => segment == "." || segment == ".." || segment.Length == 0))
      throw new ArgumentException("Artifact path must be a safe relative path.", nameof(value));
  ```
  * 完美拦截 `../escape.log`、`/root/log.txt`、`C:\escape.log`、`a//b.log` 等攻击载荷；
  * `v1..txt` 在分段后为有效文件名，不受单纯子串搜索的误杀，规则精准严密。

### 2. 空引用与脏数据防御（Null-Safety & Idempotence）
* 在 `RuntimeEvidencePackageValidator.Validate` 中：
  * 对 `package.Cases` 和 `package.Artifacts` 中的 `null` 元素分别记录 `NullEvidenceCase` 和 `NullArtifact`，并跳过后续解引用，保证了在极端脏数据输入下依然安全返回诊断结果而不抛出 `NullReferenceException`。

---

## 三、 对复核请求的三项明确答复

1. **前端消费判定**：**`ACCEPT`**。
   * 前端 Presenter 与诊断面板消费 `EvidencePackageValidationResult`，可清晰渲染证据包的校验状态与结构化诊断码。
2. **是否存在 Shared Contract Change Request**：**无（0 项）**。
   * 纯 C# 校验器与共享契约完全自洽，无需修改。
3. **是否同意工单保持 `ready-for-human`**：**完全同意（CONFIRMED）**。
   * 当前交付证明了**证据包与校验引擎的实现正确性**，但**尚未在真实单机、SteamP2PFriends Host/Client 以及 U3DS 环境中运行生成实际日志**。
   * 工单 [`DEV-08-runtime-evidence-package.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/DEV-08-runtime-evidence-package.md) 与 [`DEV-07-candidate-build-three-environment-gate.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/DEV-07-candidate-build-three-environment-gate.md) 应保持 `ready-for-human`，待人工开发者（Human）在三环境中收集并签署实际证据包后，方可正式关闭。

---

*报告完。作者: Gemini*


