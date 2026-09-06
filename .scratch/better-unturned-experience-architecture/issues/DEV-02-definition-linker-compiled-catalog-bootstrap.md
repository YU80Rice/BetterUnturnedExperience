# DEV-02：Definition Linker + Compiled Catalog + Bootstrap

**Owner:** GPT  
**Required reviewer:** Gemini  
**Status:** resolved
**Baseline:** `BUE-V1-RT01-20260824`  
**SourceSet:** `BUE-SS-20260824-02`  
**Depends on:** DEV-01 (`resolved`), RT-06 (`resolved`)

## Scope

实现构建期功能片段的最小链接器、稳定排序的不变 Compiled Identity Catalog 和绑定当前 Catalog 与 Feature record 的最小 Admission/Bootstrap 句柄。将链接失败作为构建期诊断，不在运行时重新解析 manifest 或 Git/event ledger。

## Acceptance

- [ ] 同一全仓片段集在身份重复、required fragment 缺失、schema 不支持、深度重复或悬空引用时原子失败，不生成成功 Catalog。
- [ ] 成功链接使用稳定排序，产生非随机的 DefinitionSetDigest；相同输入在不同文件枚举顺序下产物一致。
- [ ] Compiled Catalog 是不变视图；运行时只接受已编译记录，不访问原始 manifest、Git、DNS 或审批系统。
- [ ] Admission handle 绑定当前 Catalog 实例与 Feature record；不使用可 default 的 struct permit，不把 handle 解释为权限、版本兼容或 Running。
- [ ] Contracts/Core 仍不引用 Unity、Glazier、Sleek、LMN、BepInEx、Harmony 或 Unturned native 类型。
- [ ] TDD 覆盖稳定排序、重复/缺失诊断、digest 稳定性、不可变 catalog 和无效运行时访问。
- [ ] 不实现 DEV-03～DEV-07，不修改 LMN，不宣称三环境运行 PASS。
- [ ] 独立子智能体审计与 Gemini 消费复核通过后才关闭。

## Non-goals

设置迁移与持久化、模块启停/隔离状态机、库存算法、网络 codec/Ready-frame fence、ClientUi 装配、U3DS 运行验收和发布证据。

## Execution record

- TDD RED：缺少 Definition Linker 类时 `CS0234`。
- TDD GREEN：4 项目 `0 errors / 0 warnings`；`DEV-02 definition linker tests: PASS`；Contracts/Core token scan PASS。
- 独立审计 Round 1 `FAIL`：实现通过，但测试未覆盖 schema/missing fragment/dangling reference/failure catalog null/diagnostic 稳定性与 handle 不变量不变量量。已补齐对应测试并重建，等待 Round 2。
- 独立审计 Round 2 `FAIL`：上述分支已通过，仍需补充 Catalog/FragmentKinds/输入快照不可变与完整 diagnostics 顺序对比；已补齐，测试 EXE 哈希更新为 `FFE2FE184B6B66DF804D2F574E63401F6C9F86BD6F8334C94DEF48A0F2C53730`，等待 Round 3。
- 报告：`../../audit/2026-08-24/Implementation-DEV-02-Definition-Linker-2215.md`。
- Gemini handoff：`../handoffs/to-DEV-02-review.md`。
- 等待：独立子智能体审计与 Gemini 消费复核。

## Closure

- GPT 独立审计：`DEV-02-Independent-Audit-Round3-Final.md` — PASS。
- Gemini 消费复核：`handoffs/DEV-02-Definition-Linker-Review.md` — ACCEPT。
- 关闭依据：实现、Release 构建、Contracts/Core UI-token scan、TDD 测试与双端复核均通过；不代表三环境运行验收。


