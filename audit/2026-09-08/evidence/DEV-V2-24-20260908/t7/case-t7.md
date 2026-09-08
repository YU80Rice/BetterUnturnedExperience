# case: t7（T7 实机五项 + 防双装真机基线）— CaseId `DEV-V2-24-20260908`

> 对应手册 §7（配置 C，**独立会话**采集，不与配置 A/B 混采）。
> 第 4 项 = **红测 + 实机双证**（红测锚 `--bue-v2-platform-red` 已 CLEAN，本文件记实机半）。
> 第 1/2/3/5 项为事实观察记录：无论结果与预期同异，**如实记录即有效证据**；但若第 4 项正向场景未出现 `BUE-PLATFORM-001`，属 finding（停止后续项，报 agent）。

- collector: TODO
- gameVersion / bepInExVersion: TODO
- 每项会话 UTC 时间窗: TODO

## T7-1 改名实机对照（BUE DLL 改名 + NoOpFixture 在场）

- 部署文件名: TODO（如 `BetterUnturnedExperience.r24.dll`）；NoOpFixture.dll 哈希: TODO
- 锚: BepInEx 无「缺少依赖」告警；BUE 加载行 + assembly-identity 行 path=改名路径 sha256=`7D5DD3B5…C223`；NoOpFixture `accepted=True` 行（IL 绑定按程序集名，文件名无关）
- 结果: TODO（通过 / 异常描述）

## T7-2 Mono `LoadFile` 二次探测（Z 变体观察，可与 T7-3/T7-5 同会话）

- 部署: TODO（`BueSameAsmProbe-ZAfter.dll`；BUE 在场）
- 观察: BepInEx 对探针的加载行为——探针 `awake` 行出现（Mono 装载第二副本）？还是探针加载失败/类型缺失（Mono 以身份去重，返回已有 BUE 程序集）？
- 结果: TODO（**记录事实**，两种结果都有效）

## T7-3 同版本程序集最终谁保留（与 T7-2 同会话）

- 观察: 探针与 BUE 同程序集名同版本（0.0.0.0）时，类型绑定最终由哪个副本承担——以 `[PROBE] awake … assemblyName=… location=…` 行的 location 与 BUE selfPath 对照记录
- 结果: TODO（**记录事实**）

## T7-4 不同 GUID + 同程序集名（红测 + 实机双证的正向半）— A 变体

- 部署: TODO（`BueSameAsmProbe-ABefore.dll` + BUE；先于 T7-2 会话单独采）
- 期望: BUE 启动日志出现 Warning 行 `BUE double-install detected diagnosticId=BUE-PLATFORM-001 assembly=BetterUnturnedExperience conflictLocation=<探针路径> selfPath=<BUE 路径> suggestion=移除非官方副本`；管理面板底部红色状态行「错误：检测到 BetterUnturnedExperience 冲突副本（BUE-PLATFORM-001）：请移除非官方副本后重启游戏，详见 BUE 日志。」→ **日志行原文 + 面板截图双留**
- 结果: TODO（001 出现=双证闭环；未出现=finding，停止采集报 agent）
- 摘除探针复测: TODO（重启后 001 消失 = 用户处置路径验证）

## T7-5 Preloader `AssemblyResolve` 观察（与 T7-4 同会话）

- 观察: 部署探针后 Preloader 段（LogOutput/控制台 Preloader 行）有无新增解析错误/异常；只记录，不承诺
- 结果: TODO（**记录事实**）

## 截图 / 附件清单

- TODO（至少：T7-4 面板红色状态行截图 + 对应日志行原文）

## 结论

- TODO（五项逐项 通过/事实记录/finding 标注）
