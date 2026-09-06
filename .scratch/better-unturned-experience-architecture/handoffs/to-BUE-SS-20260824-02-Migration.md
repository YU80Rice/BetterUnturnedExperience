# GPT → Gemini：RT-02 / RT-03 SourceSet successor 迁移任务

> 作者：GPT  
> 新 SourceSet：`BUE-SS-20260824-02`  
> Manifest：`../BUE-SS-20260824-02-Manifest.md`  
> 状态：`ready-for-gemini`

请 Gemini 对其拥有的 RT-02、RT-03 产物执行定点迁移：

1. 保留 `BUE-SS-20260824-01` 为 Initial/Predecessor，不覆盖历史。
2. 在两份主报告头新增 `Migrated SourceSetId: BUE-SS-20260824-02`。
3. 核对 U3-SDK commit 与六个前端 EvidenceSource 文件哈希未变化。
4. RT-02 不得把 U3DS BepInEx 引导 PASS 推导为 UI 或 BUE 单 DLL运行 PASS。
5. RT-03 可把 U3DS BepInEx 5.4.23.5 作为冻结 IL reference；仍保留 `VO-RT03-01`～`03`，尤其是单 DLL IL 与真实插件加载。
6. 明确新 U3DS 日志显示 `0 plugins to load`，只证明 BepInEx/Preloader/Chainloader。
7. 更新两张 resolved 票据的 SourceSet 迁移记录，不改变运行证据等级。
8. 输出 `BUE-SS-20260824-02-Migration-Review.md`，逐项给出 PASS/FAIL 与受影响结论。

迁移完成后交回 GPT 定点复核；RT-06 在 RT-02～RT-05 全部记录同一 successor 前不启动。

