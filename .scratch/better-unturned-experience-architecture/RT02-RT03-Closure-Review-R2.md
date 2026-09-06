# GPT：RT-02 / RT-03 关闭复核（第二轮）

> 作者：GPT  
> 日期：2026-08-24  
> 判定：**REVISE（原 B-01～B-04 已关闭；新增 1 项关闭门禁）**  
> 契约基线：`BUE-V1-RT01-20260824`

## 1. 原阻断项结论

| 项目 | 第二轮判定 | 说明 |
| --- | --- | --- |
| B-01 `onPlacedItem` 全量接管 | PASS | 已限定普通网格，原生特殊分支放行，并标记为高风险生产原型 seam。 |
| B-02 投影事件误作 ACK | PASS | 已改为原生模型变化观察，并加入 generation/session/fingerprint 高置信关联边界。 |
| B-03 Core SafeMode UI 冲突 | PASS | 已卸载自定义功能 UI，仅允许独立最小 fallback 提示。 |
| B-04 Headless 证据越级 | PASS | 已降级为设计义务，明确 successor SourceSet、IL 与真实 U3DS 门禁。 |
| N-02～N-04 | PASS | Settings 术语、私有字段 accessor 和 LMN 降级表现均已同步。 |

## 2. 新增关闭门禁 C-01：EvidenceClass 与逐条证据身份仍不符合 RT-01

两份报告头仍使用：

- `PROTOTYPE_CONFIRMED`
- `RUNTIME_GATE`

但 RT-01 §10.2 冻结的证据枚举是：

- `SOURCE_CONFIRMED`
- `IL_CONFIRMED`
- `PROTOTYPE_ONLY`
- `BUILD_CONFIRMED`
- `RUNTIME_CONFIRMED`
- `RELEASE_CONFIRMED`
- `UNRESOLVED`

禁止创建近义枚举。应将：

- `PROTOTYPE_CONFIRMED` → `PROTOTYPE_ONLY`
- `RUNTIME_GATE` → 对尚无证据的义务标为 `UNRESOLVED`，并单列后续 verification obligation

此外，报告级 Evidence Manifest 目前只给出 U3-SDK commit 和客户端 DLL hash，没有让每一条 native evidence 无歧义关联到：

- 相对源码路径及 SHA-256，或程序集 identity + SHA-256；
- EnvironmentRole；
- EnvironmentLimits；
- CapturedBy/At。

不要求在每一行机械复制全部字段，但必须增加一个公共 `EvidenceSourceId` 表，例如 `U3SRC-PDINV-01`、`CLIENT-IL-01`，每条调用链记录引用对应 ID。源码记录至少包含 `relative path + SHA-256 + commit`；IL 记录必须包含程序集 identity、SHA-256 和实际 IL symbol/offset。若没有真实 IL 检查记录，不得仅因报告头列出 DLL hash 就把结论标为 `IL_CONFIRMED`。

## 3. 非阻断措辞校正

Core SafeMode 的目标是尽可能保持原版游戏可继续，不应把静态设计写成“100% 保持可用”的运行保证。建议改为：

> 不主动修改原版游戏与菜单；在 CLR/进程仍可安全继续的条件下尽可能恢复原版体验。生产候选仍需运行验证。

## 4. 最终状态

- RT-02：继续 `ready-for-human`
- RT-03：继续 `ready-for-human`
- 不需要 Shared Contract Change Request；这是对既有 RT-01 证据规则的机械对齐。
- Gemini 完成 C-01 后，GPT 只需执行一次定点检查；若无新问题即可将两张调研票标记 `resolved`。

