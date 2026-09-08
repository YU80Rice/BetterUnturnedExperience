# DEV-V2-22 红绿链证据

票据:`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-22-lir-adoption.md`(2026-09-08 /implement 会话)
锚点:`--bue-v2-lir-red`(tests/BetterUnturnedExperience.Plugin.Tests/Program.cs,收集式七组:叠加点 guard / Stop 只撤自身 / 事件消费 / HostTick 双击驱动 / 整理→压弹全链 / 半注册回滚+首帧延迟 / enabled 原生回退)

## 链条

| 阶段 | 证据文件 | 观测 |
|---|---|---|
| 1. 编译红(红测先行,LIR 类型不存在) | `compile-red.log` | **15 错**(CS0234 命名空间缺失 + CS0246 缺类型) |
| 2. 桩级红(类型就位、体为桩) | `stub-red-run.log` | **34 条失败**(七组收集式,exit 1) |
| 3. 实现轮迭代 | `impl-red-run1.log` → `impl-red-run2.log` | 运行时红 7 → 1(角色探针注入缝/重臂语义/DrainAll/RequestId 重放/检查顺序五处修正) |
| 4. 实现全绿 | `impl-green-run.log` | **ALL GREEN**(0 failures,exit 0) |
| 5. 修复轮红→绿(R1-Spec DEVIATION-1 新断言) | `fix1-red-run.log` → `fix1-green-run.log` | **2 红**(新断言「非法范围(6..2)拒绝」+ 水印回归连带,同一根因)→ **ALL GREEN** |
| 6. F2(SMELL)修复后复验 | `fix2-anchor.log` | **ALL GREEN** |

## 全套测试(最终态)

- 解 Rebuild:0 错 0 警告(`sln-rebuild-r2.log`、`fix2-build.log`,TreatWarningsAsErrors=true)
- 7/7 PASS:Contracts / Network / Plugin / Placement / Settings / ClientUi / Release(直跑 exe)

## 附注

- 桩级红属于 DEV-V2-19 先例链(编译红→桩级红→绿);DEV-V2-21 曾因编译红未独立锚点被记流程偏差,本票两锚点均独立留痕。
- 修复轮红锚点沿 DEV-V2-21 R10 先例:新断言先红留证再转绿。
- 七组红测不安装任何 Harmony 补丁;叠加点按规格钉在 guard 输入输出(spec Testing Decisions #5)。
