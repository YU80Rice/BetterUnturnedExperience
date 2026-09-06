# RT-01 共享契约基线交付报告 - v0.32

## 【需求执行概述】

完成 RT-01 共享契约与对账基线，修复旧 DTO 漂移，冻结 RT-02～RT-05 共用的不可变 SourceSet 和证据格式，并生成交付 Gemini 的逐项复核文档。

## 【源码溯源清单（Traceability Matrix）】

| RT-01 要求 | 落实位置 |
| --- | --- |
| 接口签名与 Required behavior | `RT-01-Shared-Contract-Baseline.md` §3 |
| bootstrap、DTO/interface/enum | §3～§4 |
| Kind、方向、字段和禁用消息 | §5 |
| 状态、2/3/8 秒规则 | §6 |
| 坐标域与旋转公式 | §7 |
| Local-Fit Priority | §8 |
| Shared Contract Change Request | §9 |
| SourceSet 与 evidence taxonomy | §10 |
| 英中及旧文档对账 | §11 |
| Gemini 逐项复核 | `handoffs/to-RT-01-review.md` |

## 【变更清单】

- 新增 `RT-01-Shared-Contract-Baseline.md`。
- 新增 `handoffs/to-RT-01-review.md`。
- 修复 `Shared-Contract-Spec.md` 中两个 config result event 缺少 `RevisionScope` 的漂移。
- 更新 RT-01 验收清单和 `map.md` 状态。

## 【验证记录】

- 生产编译：N/A；本轮仅静态契约和证据基线，不包含生产代码。
- 机械对账：14 个 interface 成员、8 个 bootstrap 属性、消息 Kind、五个禁用消息、坐标公式和状态 token 均存在。
- 来源复算：U3-SDK commit/三项 anchor SHA-256、Client Assembly/BepInEx identity+SHA-256、LMN 44 文件 manifest digest 均与基线一致。
- 链接：Gemini handoff 五个阅读入口全部有效。

## 【子智能体独立审核记录】

### 第 1 轮：FAIL

- DTO/enum 可达面未完全纳入单一基线。
- 只定义 SourceSet schema，未发布实际冻结身份。
- Gemini handoff 未要求复核 DTO 与依赖/定义产物 seam。

修复：增加逐 token 规范引用、实际 SourceSet manifest、UNRESOLVED 所有权和 Gemini 复核问题。

### 第 2 轮：FAIL

- SourceSet 允许同 ID 原地 amendment，破坏不可变身份。

修复：任何内容变化必须发布 successor ID，旧 ID 永久可重建；记录 Shared Contract 文件 SHA-256。

### 第 3 轮：FAIL → 定点修复后 PASS

- 初始 ID 强制规则与 successor 最终 ID 冲突。

修复：四张研究票初始使用 01；若有 successor，关闭前必须统一迁移到同一最新批准 ID 并重验受影响结论。

最终判定：PASS，阻断项为零。

## 【偏离与妥协说明】

无产品需求偏离。U3DS 独立 `Assembly-CSharp.dll` 与 BepInEx reference 当前为 `UNRESOLVED`，已指定 RT-04/RT-05 Owner；补齐前禁止对应 IL/二进制兼容声明。

## 【后续测试建议】

1. Gemini 按 handoff 十项问题返回 `ACCEPT` 或结构化 blocker。
2. Gemini 接受前保持 RT-01 `claimed`，RT-02～RT-05 继续 blocked。
3. Gemini 接受后由 GPT 记录复核、将 RT-01 设为 `resolved`，再释放四张研究票。

## 【最终结论】

GPT 撰写与独立审计已完成，可交 Gemini 复核；本报告不代表 RT-01 已关闭，也不代表生产编译或三环境运行 PASS。


