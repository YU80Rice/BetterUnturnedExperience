# DEV-V5-01 审查闭环记录（给人看的开发手册入口）

日期：2026-09-14　会话：/implement V5-01　基线 HEAD：`e4894ac`

## 交付物

- `docs/developer/README.md`（入口页，36 行）——四层读者表、阅读顺序、三条权威规则；
- `docs/developer/BetterUnturnedExperience-Developer-Handbook.md`（手册，97 行）——一张总图（两层结构+生命周期主线，text 块）+ 短章一「模块结构」+ 短章二「最小接入流程」（六步方向，零字段表/码表）+ 短章三「NoOp 范例导读」（导读唯一范例，必要代码 vs 测试辅助切分，范例地位声明含「不扩展 SDK 契约/以 SDK 为准」原话）；
- `eng/Verify-DeveloperHandbook.ps1`——文档门禁（红测先行载体，票面注明「文档门禁即可」）；
- `README.md`「给生态开发者」——开发手册条目置于 SDK 与 NoOp 之前（官方先行消费）；
- 6 份点名 `.scratch` 长文历史横幅（SUPERSEDED/历史资料 + SDK/新手册双指针，正文零删改）：V1 spec.md、spec.zh-CN.md、Shared-Contract-Spec.md、Module-Lifecycle-Isolation-Spec.md、spec-open-runtime-feature-framework.md、.zh-CN.md。

## 红绿链

1. **红**：门禁先于文档落地运行 → 34 项违例（`red-gate-run.txt`），含 H-ENTRY/H-HANDBOOK 缺失、README 顺序缺、6×SC-MARK 缺。
2. **绿**：手册+入口+README+横幅落地 → `gate PASS`（`green-gate-run.txt`；定稿复跑 `green-gate-run-final.txt`）。
3. **突变证红**（各单项命中、还原复绿）：
   - M1 README 两条目顺序颠倒 → `FAIL README-ORDER`（`red-mutation-M1-run.txt`）；
   - M2 摘除 Shared-Contract-Spec.md 横幅 SUPERSEDED 行 → 仅 `FAIL SC-MARK`（`red-mutation-M2-run.txt`）；
   - M3 手册尾部注入 `BUE-REG-001` → 仅 `FAIL BAN-CODES`（`red-mutation-M3-run.txt`）；
   - M4 手册 SDK 链接断 → 仅 `FAIL LINK-SDK-HB`（`red-mutation-M4-run.txt`）。
4. **静态基线**：src/tests 零改动；解决方案 `-t:Rebuild` Release **0 错误 0 警告**（`green-sln-rebuild.log.txt`，MSBuild=VS18 Insiders）；7 测试套件直跑全 exit=0 尾行 PASS（`green-fullsuite-*.txt`，summary `green-fullsuite-summary.txt`）。

## 双轴审查（fresh-instance 规则：每轮两个全新实例并行派发）

### R1（增量 `git diff 685ca31..81e2dfc`，两笔提交：685ca31 开图工件补录 / 81e2dfc 本票实施）

- **Standards 轴**（agent_196b1039…，standards-reviewer 新实例）：**CLEAN，无硬性违反**。判断性气味 2 条，见下方具名递延。报告 `standards-axis-report-r1.md`。
- **Spec 轴**（agent_ff7da194…，Spec-Reviewer 新实例）：**CLEAN，无 gap/deviation**。逐条核对票面/T2 Answer Q1–Q5/禁止清单/候选纪律，语义核对（注册≠触发冻结、订阅与登记的先后、清理归宿主）无确凿错误。报告 `spec-axis-report-r1.md`。
- 冻结件 `review-freeze-r1.txt`（394 行 diff）；变更清单 `changed-files-r1.txt`。
- 发现=0 → 无需修复轮；R1 即终轮。

## 具名递延（判断性气味，不阻塞）

1. **门禁行数硬帽数值（entry≤150 / handbook≤300）**——T2 只裁「一图三短章」未给数值，实现者自设上界作为短章纪律的机器化落实；数值选择留待第三方评审反馈可再议。
2. **README 与手册双份「以 SDK 为准」声明**——T2 对入口页与手册分别要求权威声明（门禁 AUTH-SDK-HB/AUTH-SDK-EN 对应），双份系有意为之，非漂移风险。

## 候选纪律

本票不产正式候选 DLL、不更新 `audit/RELEASES.md`、不授 CaseId。diff 无 publish/、无 RELEASES、无 DLL 改动（R1 Spec 轴已核）。

## 评审包边界（交用户目视后外请）

`docs/developer/`（两文件）+ 根 README「给生态开发者」节。SDK 为被引用契约来源不整本重审；AI 执行规格（`.scratch/`）不进评审包。「手册稳定」= T2 形态冻结 + 本目录交付 + 用户目视可外发。评审不挡 DEV-V5-02..08 开工。
