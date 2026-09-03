# V1 整体冻结快照 — 2026-09-03

> 性质：V1（BUE 基础架构 + Better Item Interaction 功能）整体闭环关闭冻结快照。
> 关闭依据：DEV-16G D 实机验收通过 + 用户 2026-09-03 批准 V1 闭环收尾（DEV-07/08/09/DEV-15E-HUMAN 以 DEV-16E/F/G 实机证据作为覆盖）。
> 本快照**非发布/Stable 授权**；发布仍按 real-machine-test-loop.md / 发布授权边界由人工开发者批准。

## 1. 最终交付身份（V1 唯一事实）

- **正式 DLL**：`audit/2026-09-02/artifacts/DEV-16G-D-20260903/BetterUnturnedExperience.dll`
- **SHA-256**：`D13F9A12F0076E9BBA25378EBBC5EA0536C298C3E514FE9F1D2207E0836108CE`（237568 bytes）
- **源码提交**：`6c7066a`（DEV-16G D 实现）、`0ee5d67`（交付报告）、`b383877`（DEV-16G 关闭 + 冻结）、`6cbe83f`（V1 收尾工单更新）
- **实机部署哈希核对**：逐字一致 ✓（`UMM-诊断包_20260903_130139`）
- **BepInEx 入口**：`io.github.yu80rice.betterunturnedexperience`（唯一 `[BepInPlugin]`，版本 0.0.0 预发布）

## 2. 工单状态表（V1 全链）

| 阶段 | 工单 | 状态 |
|---|---|---|
| 基础框架 | DEV-01~06（骨架/Definition Linker/设置/落点/ClientUi/网络 codec） | resolved |
| 资格门禁 | DEV-07（CandidateBuild 三环境）/ DEV-08（证据包）/ DEV-09（引导入口） | **resolved**（2026-09-03 人工批准） |
| 外部注册 | DEV-10~13（Host 桥 / No-op / 单 DLL 闭包 / 注册冒烟） | resolved |
| 官方功能 | DEV-14（BII 官方注册）/ DEV-15A~E（拖拽适配/预览接线/投影/生命周期隔离/资格证据） | resolved |
| 真实证据 | DEV-15E-HUMAN（三环境人工证据） | **resolved**（2026-09-03 人工批准，哈希更新至 D13F9A12） |
| 真实运行时 | DEV-16（总工单）/ DEV-16A~G | **closed**（2026-09-03） |
| 日志规范化 | DEV-16G A/B/C/D | closed（2026-09-03） |

## 3. V1 能力范围（已验收）

- BUE 单 DLL 运行时：管理面板、设置 Facet、收藏/排序、主菜单/暂停菜单入口。
- Better Item Interaction：绿色/红色占据框、真实浮动物品图标、原生拖入、自动旋转、投影收敛、拿起源解耦、目标页 2~7 扩展。
- 故障隔离：功能局部隔离 + 原生回退；U3DS Headless 不实例化 UI。
- 日志策略（D1-D13）：加载一条聚合成功行；游戏内成功静默；错误带"BUE 错误："前缀 + 结构化 reason；`LogLevels=Debug` 恢复全量。

## 4. 三环境边界

- 单人 + SteamP2P Host/Client + U3DS Headless 实机证据已采集（DEV-16E/F，含最终 DLL 哈希核对）。
- 所有 DLL 归档均非发布/Stable 授权；发布需人工批准 BuildIdentity/DLL hash。

## 5. 下一步（V2，仅方向记录，未开工）

- 依据 `CONTEXT.md`（BUE V2 产品方向）：开放运行时平台；V2 第一阶段 = LMN 网络模块官方纳入 + `BueNetwork`/`BueNetworkApi`（接管、配置迁移、V1 过渡兼容、三环境网络层验证）。
- map.md Proposed frontier：GPT-18（前置框架运行时与独立功能注册）`needs-triage`，被 `SCR-GPT18-001`（ready-for-human）阻塞。
- V2 开工需人工开发者明确立项指令。

## 6. 关联归档

- 冻结快照：`snapshots/DEV-16G-closure-freeze-20260903.md`、`snapshots/DEV-16D-implementation-state-freeze-20260901.md`
- 交付报告：`audit/2026-09-02/DEV-16G/Delivery-DEV16G-{A,B,C,D}-*.md`
- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-*.md`（全部 resolved/closed）
- DLL：`audit/2026-09-02/artifacts/DEV-16G-D-20260903/BetterUnturnedExperience.dll`
