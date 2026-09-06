# Requirements Specification 双语发布报告 - v0.30

## 【需求执行概述】

将已完成 Wayfinder 决策包发布为一式两份的 `ready-for-agent` 需求规格：英文版供执行 Agent 使用，中文版供人工开发者审阅；补齐产品目标、使用者、验收结果和禁止修改原版/逆向/滥用边界。

## 【源码溯源清单（Traceability Matrix）】

| 需求 | 落实位置 |
| --- | --- |
| 整体框架目标 | `.scratch/better-unturned-experience-architecture/spec.md` 与 `spec.zh-CN.md` 的 Problem Statement/问题陈述 |
| “更好的物品交互”目标 | 两份规格的 Problem Statement、产品验收结果 |
| 玩家与开发者两类使用者 | 两份规格的问题陈述和用户故事 |
| 功能与框架两层验收 | 两份规格的 Product acceptance outcomes/产品验收结果 |
| 不修改原版内容、不逆向或滥用 | 两份规格的 Out of Scope/不在范围内 |
| 三层规格结构 | 两份规格的 Layer 1～3/第一层～第三层 |
| 英中双语同步 | 两份规格头部互链及 `map.md` Notes |
| 人工确认最高测试 seam | 两份规格 Testing Decisions/测试决策及 `map.md` |

## 【变更清单】

- 修改英文 Agent 执行版 `.scratch/better-unturned-experience-architecture/spec.md`。
- 新增中文人工阅读版 `.scratch/better-unturned-experience-architecture/spec.zh-CN.md`。
- 更新 `.scratch/better-unturned-experience-architecture/map.md` 的双语发布与同步规则。
- 更新上一轮 `Implementation-v0.29-1632.md` 的确认状态。

## 【验证记录】

- 生产编译：N/A，本轮仅需求规格文档，不包含生产代码。
- 静态检查：英文版无中文说明残留；14 个冻结函数、6 个消息 Kind、坐标旋转公式和三层标题在双语文件中存在。
- 链接检查：`spec.md`、`spec.zh-CN.md`、`map.md` 的 Markdown 相对链接全部有效。
- 状态检查：英中两版均为 `ready-for-agent`。

## 【子智能体独立审核记录】

### 第 1 轮：FAIL

发现中文镜像压缩造成三类遗漏：

1. `0x0102`、`0x0103`、`0x0104` 缺少部分 Feature/RevisionScope/KnownRevision 字段。
2. Gemini 前端调研遗漏 container change、投影源/目标识别和音效调用裁定。
3. 测试决策遗漏完整最高 seam 列表及 grabOffset 闭域与 intended-center 半开域区分。

以上均已逐字段补齐。

### 第 2 轮：PASS

- 判定：PASS。
- 阻断项：无。
- 产品目标、使用者、两层验收、三层职责、冻结签名/消息/坐标/算法、调研交付、测试矩阵、范围外事项和证据边界双语一致。
- 所有相对链接有效。
- 非阻断建议：后续 CI 增加双语契约 token 与消息字段集合自动对账。

## 【偏离与妥协说明】

无需求偏离。中文版用户故事按受众合并为更易读的条目，但全部实质义务仍在后续实施、调研和测试章节中完整保留；精确函数签名和字段名不翻译。

## 【后续测试建议】

1. `/to-tickets` 只分解共享契约基线、Gemini 前端 U3-SDK 调研、GPT 后端 U3-SDK 调研，暂不进入生产编码。
2. 增加双语规格 token/消息字段集合对账脚本。
3. 后续 CandidateBuild 继续分别收集 SP、SteamP2PFriends Host/Client、U3DS 同哈希运行证据。

## 【最终结论】

需求规格双语发布完成，独立审计 PASS，可进入 `/to-tickets`；不代表生产编译、运行或发布验收完成。
