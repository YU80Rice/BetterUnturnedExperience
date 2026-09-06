# GPT-SCR-RT05-002：SourceSet manifest 勘误与 U3DS 引用候选登记

> 作者：GPT  
> SourceTicket：RT-05  
> PredecessorSourceSetId：`BUE-SS-20260824-01`  
> 状态：`accepted`  
> 关闭说明：successor `BUE-SS-20260824-02` 已获批并冻结；本文件中的“候选”描述仅保留历史提案语境。

| Field | Value |
| --- | --- |
| ChangeRequestId | `SCR-RT05-002` |
| AffectedTokens | RT-01 §10.1 SourceSet manifest reproduction text；U3DS `Assembly-CSharp.dll` 与 BepInEx reference 状态。 |
| CurrentBaseline | 文本规定按 `StringComparer.Ordinal` 排序，但冻结 digest `7151D22E...` 实际由 PowerShell 文化排序复现；U3DS 两项 reference 为 `UNRESOLVED`。 |
| Evidence | 相同 44 文件与逐文件 hash：legacy digest `7151D22EF361B560F44A32963F82CD973AF64D7721F9D109C5492D1D7D864DE6`；真正 ordinal digest `4F290955FCDA53BFF54E2983BECA4B08337D29266D5C3FB48BD75A4BFA62F2AF`。发现 U3DS Assembly-CSharp SHA-256 `1AD761B15AD754259BEA8D3053B86105FAB16E09B0A122A101807C87B582060A`，BepInEx SHA-256 `2674D3AECF3097BEE817ABE7E8BBCC42BF583DF51402069D5FCD4FBED55017CE`。 |
| ProposedChange | 保留旧 SourceSet 不变以维持审计链；发布 successor，明确 UTF-8、`/` 相对路径、`StringComparer.Ordinal`、LF 且无末尾 LF；记录 predecessor、legacy/corrected digest。纳入已采证的 U3DS Assembly-CSharp 与重新部署的 BepInEx `5.4.23.5`/Preloader 静态引用。RT-02～RT-05 随后统一迁移并复核受影响结论。 |
| CompatibilityImpact | 不改变产品 wire token；改变研究证据身份与双端引用交集结论。候选显示 `Dedicator.IsDedicatedServer` 存在 field/property ABI 差异，统一 DLL 必须使用正式交集门禁。 |
| TestImpact | successor manifest 确定性复算；client/U3DS 双 reference compile/metadata intersection；U3DS 实际加载；禁止 client-only `Unturned.LiveConfig.Runtime` 和不一致 member kind 泄漏。 |
| GPTDecision | `ACCEPTED`：`BUE-SS-20260824-02` 已发布；等待 RT-02/RT-03 完成 Gemini-owned 迁移记录。 |
| GeminiReview | `ACCEPT IN PRINCIPLE`；见 `handoffs/RT05-LMN-Boundary-Review.md`。 |
| HumanTrace | 2026-08-24：人工开发者完成 U3DS BepInEx 5.4.23.5 重新部署与动态验证，并授权验证无异常后重写批准包、继续下一步；GPT 新日志复核 PASS。 |

## 候选引用

| Role | Absolute path | Identity | MVID | SHA-256 |
| --- | --- | --- | --- | --- |
| U3DS Assembly | `E:\Steam\steamapps\common\U3DS\Unturned_Data\Managed\Assembly-CSharp.dll` | `Assembly-CSharp, Version=0.0.0.0` | `fc3b6b54-0730-4773-b9ff-874a73eea7d2` | `1AD761B15AD754259BEA8D3053B86105FAB16E09B0A122A101807C87B582060A` |
| U3DS BepInEx | `E:\Steam\steamapps\common\U3DS\BepInEx\core\BepInEx.dll` | `BepInEx, Version=5.4.23.5` | `d1b92069-86c2-41dc-ad96-bb21eee55a97` | `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70` |
| U3DS BepInEx Preloader | `E:\Steam\steamapps\common\U3DS\BepInEx\core\BepInEx.Preloader.dll` | `BepInEx.Preloader, Version=5.4.23.5` | `79f09773-f5a2-4b6e-9759-c0f39217b691` | `55D3895351A9D16B63B6F35F1C01B44AC650979E853D0BD3A442B92A082AF64F` |

