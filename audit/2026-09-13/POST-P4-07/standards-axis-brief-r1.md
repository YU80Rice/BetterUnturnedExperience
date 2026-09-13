# POST-P4-07 R1 Standards 轴 brief

固定点：HEAD `85f8e6a`。工作区增量（提交前）。冻结 diff：`audit/2026-09-13/POST-P4-07/review-freeze-r1.txt`（276 行）。清单：`changed-files-r1.txt`。

Diff 命令等价：`git diff HEAD -- src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs .scratch/bue-post-phase4-closure/map.md` + 未跟踪 `eng/Verify-RefreshModelDeduped.ps1`、`.scratch/bue-post-phase4-closure/issues/07-refresh-model-dedup.md`。

## 规范来源

- `AGENTS.md`：红测先行 + 双轴 CLEAN；大写入分批。
- `docs/agents/output-review-loop.md`：红→绿→双轴；seam gap 必须具名。
- `docs/agents/issue-tracker.md`：认领 Status=claimed。
- 仓库无独立 CODING_STANDARDS.md。Plugin 老式 csproj 白名单；本票未加 Compile 项。
- Glazier 原生面板宿主不可构造（F3/F4 先例）：结构门禁作红测缝合法。

## 气味基线（Fowler 第 3 章；判断题，不成文规范压制）

- Mysterious Name → 重命名
- Duplicated Code → 提取共享形态
- Feature Envy → 移到数据所在
- Data Clumps → 打包类型
- Primitive Obsession → 小类型
- Repeated Switches → 多态或映射
- Shotgun Surgery → 聚拢
- Divergent Change → 按原因拆分
- Speculative Generality → 删未要求的抽象
- Message Chains → 隐藏遍历
- Middle Man → 砍掉纯转发
- Refused Bequest → 改组合

仓库覆盖规范。气味永远判断题。跳过工具已强制内容。

## 任务

报告——按文件/hunk 列出——(a) diff 中每一处违反文档化规范的地方：引用规范（文件 + 规则）；以及 (b) 你发现的任何基线气味：命名并引用 hunk。区分硬性违规和判断性差异。400 字以内。不要派子代理。只审冻结 diff。

本票是 F2 三处 refresh 序列去重，不是行为改写。Open / 干净 RequestRefresh 仍可直接 `refreshModel()`（非 F2 三元组）。插件草稿填充两段同构明确不在范围。
