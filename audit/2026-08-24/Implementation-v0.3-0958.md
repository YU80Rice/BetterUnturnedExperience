# GPT → Gemini 进度同步 Prompt 交付报告 - v0.3

## 需求执行概述

生成一份可直接交给前端负责人 Gemini 的同步 Prompt，引导其复读双方设计路径、对照唯一决策地图并输出前端差异报告。

## 源码溯源清单（Traceability Matrix）

| 需求 | 落实位置 |
| --- | --- |
| 同步双方当前进度 | Prompt 第一、二节 |
| 提醒 Gemini 阅读自身设计与路径 | Prompt 第二节 |
| 对齐 GPT 后端与 Gemini 前端职责 | Prompt 第三节 |
| 输出给 GPT 的差异报告 | Prompt 第四节 |
| GPT/Gemini 文件前缀 | Prompt 第一、四、五节 |
| 不覆盖唯一决策地图 | Prompt 第一、四、五节 |

## 变更清单

- 新建 `.scratch/better-unturned-experience-architecture/handoffs/to-progress-sync-prompt.md`。
- 未修改 Gemini 现有设计文件、GPT 决策票、唯一地图或生产代码。

## 验证记录

- 本轮为文档交接，不含可编译源码或构建清单。
- `git diff --check`：通过。
- Prompt 中列出的 GPT、Gemini 和视频路径均经独立审核确认存在。

## 子智能体审核记录

- 审核轮次：1。
- 判定：PASS。
- 阻断项：无。
- 确认内容：职责所有权、GPT-05/06/07 研究结论、静态/运行边界、输出限制和完成标准均准确。

## 偏离与妥协说明

无偏离。Prompt 只授权 Gemini 阅读并创建一份 `Gemini-` 前缀对齐报告，不授权修改唯一地图、GPT 文件或生产代码。

## 后续建议

1. 将 Prompt 全文交给 Gemini。
2. 等待其生成 `to-progress-sync.md`。
3. 使用 GPT-15 决策票审阅其差异报告，并把获批内容合入唯一决策地图。


