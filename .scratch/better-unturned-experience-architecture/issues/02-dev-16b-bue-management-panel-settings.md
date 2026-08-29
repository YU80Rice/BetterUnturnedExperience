# 02：DEV-16B BUE 内置插件管理面板与设置编辑

**What to build:** 让玩家从主菜单和游戏内暂停菜单打开 BUE 自有管理面板，查看 BUE 功能与所有已加载 BepInEx 插件，使用收藏/A-Z/Z-A 排序，并安全编辑 BUE 设置和普通插件的基础配置。

**Blocked by:** 01：DEV-16A 单 DLL Runtime Composition Root 与 Client/Headless 装配

**Status:** resolved

> 实施接管：原由 Gemini 负责的 ClientUi 代码现由 GPT 接手维护。本票静态实现与自动化门禁已完成；真实客户端运行证据仍需人工采集。

> 2026-08-28 运行修复：针对 `UMM-诊断包_20260828_081850` 中启动时容器为空且无运行期注入事件的问题，BUE 已补充 `MenuWorkshopUI.open` 与 `PlayerPauseUI.open` 的 Harmony Postfix；该修复覆盖 UI 先于插件构造的时序，仍待人工实机确认按钮可见与可点击。

> 2026-08-28 R5 修复：`UMM-诊断包_20260828_090329` 对应截图实际处于 `MenuDashboardUI` 主菜单；已新增 Dashboard 构造/open Hook、入口按钮和容器变更时对称清理。请以 R5 DLL 的启动 `assembly-identity` SHA-256 作为部署确认依据。

- [x] 主菜单和暂停菜单均已接入“BUE 插件管理”入口，UI 树重建可安全重挂载（待实机确认可见性）。
- [x] 面板只读取 BepInEx Chainloader 已成功加载的插件，不扫描或主动加载任意 DLL。
- [x] BUE 功能条目与普通插件条目模型已分别提供稳定身份、版本、状态和公开设置数据（待实机确认显示）。
- [x] 收藏使用稳定 FeatureId/GUID；收藏优先、收藏内部按加入顺序，未收藏条目支持 A→Z/Z→A。
- [x] 排序偏好、收藏列表和收藏顺序已实现文件持久化；取消后重新收藏进入序列末尾。
- [x] Better Item Interaction 的增强交互开关和自动旋转已接入 BUE 设置编辑 Seam，并保持单一设置状态源（待实机确认控件交互）。
- [x] 普通插件 bool、数字和字符串 ConfigEntry 支持受限编辑；不支持类型只读、范围/长度校验、需重启标记和保存失败回滚已实现。
- [x] 未提供运行时强制启用、禁用、卸载或热重载；检测外部 UnturnedPluginManager 时仅显示兼容提示。
- [x] 面板和仓库保留 `35117+Deepseek-v4-falsh-0731`、来源仓库、提交 `9b75730`、许可记录和致谢。
- [x] Release 编译、排序/持久化/配置编辑测试、UI 重建测试、静态门禁和独立审计通过（真实 UI 运行仍待人工验证）。

## Comments

### 2026-08-29 R10 基线固化（agent）

- Release 构建 **0 错误 / 0 警告**；7/7 测试运行器 PASS（日志归档 `audit/2026-08-29/tests-*.log`）；`Verify-NoUiTokens.ps1` 三个 SourceRoot PASS；`git diff --check` CLEAN。
- 新 DLL：`artifacts/DEV-16B-management-panel-runtime-fix-r10-20260829/BetterUnturnedExperience.dll`（168960 bytes，SHA-256 `AF065D83…64CBE`，CaseId `DEV-16B-R10-20260829`；完整值见 `audit/2026-08-29/r10-dll-sha256.txt`）。
- 审计：`audit/2026-08-29/Implementation-DEV-16B-R10-baseline-0950.md`（含真机 UMM 诊断包六边界判别矩阵与部署步骤）。
- 状态保持 `ready-for-human`：等待人工部署 r10 DLL 并回传诊断包；`plugin-update` 是否 >0 为第一判据。

### 2026-08-29 R10b 屏障修复（agent，双轴审查驱动）

- **修复**（TDD 红→绿）：新增 `BueRuntimeCompletionBarrier`（未就绪可重试 / 异常永久隔离 + `LastFailure` 保留 + 首次隔离回调），`TryCompleteRuntime` 经屏障执行；日志补 `decision=Isolate errorType=`；`CompleteRuntime` 成功后的 Refresh 副作用局部隔离（`BUE-CLIENTUI-004`），RuntimeReady 输出不再被 UI 刷新失败阻塞。
- **新产物**：`artifacts/DEV-16B-management-panel-runtime-fix-r10b-20260829/BetterUnturnedExperience.dll`（169984 bytes，SHA-256 `D8F9AF51…12D5`，CaseId `DEV-16B-R10B-20260829`；完整值 `audit/2026-08-29/r10b-dll-sha256.txt`）。验证全绿：构建 0/0、7/7 测试（R10b 轮独立留存 `audit/2026-08-29/tests-r10b-*.log`，测试 exe SHA-256 见 `r10b-tests-exe-sha256.txt`）、NoUiTokens×3、`git diff --check`。
- **循环审查链**：R10b 增量双轴审查（Standards 1 硬违规 + Spec 3 缺失）→ 修复 → 第三轮复审（Standards CLEAN；Spec 3 项记录/证据偏差）→ 记录修正（16 断言、溯源表述、独立 r10b 测试日志、判别矩阵补 RuntimeCompletionIsolated 行）。
- **待规格裁决**（双轴子代理审查发现，未修改）：① AcceptableValueList 条目只读 vs 规格要求的校验编辑路径；② 第三方条目显示名/版本/运行状态模型缺口（`FeatureState` 恒 Running，ContractTypes 无字段）；③ 面板打开时整页遮蔽宿主页面的规格授权；④ `IsAlive` 反射失败返回 false 的 fail 方向（无纯宿主测试 seam）。
- 状态保持 `ready-for-human`：请部署 **r10b** 产物并回传 UMM 诊断包；判别矩阵见 `audit/2026-08-29/Implementation-DEV-16B-R10-baseline-0950.md` 第 5/7/9 节。

### 2026-08-29 R11 门禁修复（真机诊断驱动，循环审查闭合）

- **真机判读**（`UMM-诊断包_20260829_194254`）：r10b 部署身份正确，但 `BUE-CLIENTUI-001` ——R10 新增的 `CanBindNativeUi()` 含 `Glazier.Get() != null` 时机性条件，`Glazier.instance` 主菜单构建前恒 null，门禁误杀面板整链（早于 R9 的驱动层问题）。
- **修复**（TDD 红→绿）：门禁收敛为纯成员存在性；新增 `AssertNativeUiGateReflectsMemberPresence` 锁定语义；全套 0/0 构建 + 7/7 测试（`tests-r11-*.log`）。
- **循环审查**：增量双轴并行（Standards 无硬违规 + 2 可推迟 / Spec CLEAN）→ 双轴无阻断，循环闭合。可推迟项：测试 Glazier 前提断言、注释措辞、负向测试——记入审计 §10。
- **新产物**：`artifacts/DEV-16B-management-panel-runtime-fix-r11-20260829/BetterUnturnedExperience.dll`（169984 bytes，SHA-256 `EA77D360…000A`，CaseId `DEV-16B-R11-20260829`；完整值 `audit/2026-08-29/r11-dll-sha256.txt`）。
- 状态保持 `ready-for-human`：请部署 **r11** 产物并回传 UMM 诊断包。预期日志链 `BUE-CLIENTUI-002` → `plugin-update`；若 `plugin-update` 仍为 0，则回到驱动层假设。

### 2026-08-29/30 R12～R21 诊断与修复链（agent，循环审查驱动）

完整判读与修复记录见 `audit/2026-08-29/Implementation-DEV-16B-R10-baseline-0950.md` §11-18 与 RT-08 一手调查。摘要：

- **R12-R15 探针链**（UPM 形状极简 + 全 patch 命中图 + 生存策略）：真机逐层排错——Harmony detour 正常（self-patch 命中）、独立对象不被驱动、`BepInEx_Manager` 被 SetActive(false)+Destroy（`manager-destroyed`）、UPM 对照证实环境健康。
- **R16**：移除 self-revive 诊断仪器（其在 OnDisable 中 SetActive(true) 与引擎禁用流程竞争，导致组件死亡）。
- **真因定案（R18-R19）**：全部 11 个 vanilla patch 真机命中；驱动链死亡真因 = 组件 `OnDestroy` 的 `harmony.UnpatchSelf()` 自废 patch（UPM 不 unpatch 故存活）。修复：`Destroy(bool unpatchHarmony)` 拆分 + `Application.quitting` 区分组件销毁（保活 patch 与面板静态状态）/应用退出（完整清理）。
- **R20/R21 布局**：三按钮统一 200×50（dashboard y=460 列尾避让商店按钮、workshop/pause 对齐 UPM 参数）；面板全屏动态分辨率适配（vanilla `MenuDashboardUI.container` 模式）。
- **正式产物**：`artifacts/DEV-16B-management-panel-ui-layout-r21-20260829/BetterUnturnedExperience.dll`（SHA-256 `2917C303…81BF`，CaseId `DEV-16B-R21-20260829`）。

### 2026-08-30 真机验收通过，工单关闭（agent）

- **真机证据**（`UMM-诊断包_20260830_003650`，部署身份 = r21 `2917C303…81BF`，日志归档 `audit/2026-08-30/`）：`BUE-CLIENTUI-002` → `host-destroyed state=preserved patches-kept=true`（保活生效）→ 三路 `create-button-begin`+`add-child-success`（Dashboard/Workshop/PlayerPause，含进 PEI）→ **用户人工验收：三处"BUE 插件管理"按钮可见、可点，管理面板打开正常、随分辨率动态适配**。
- **完成判定逐项核对**（交接文档第 9 节）：Release 0/0 ✓、全套测试+静态门禁 ✓、新 DLL SHA-256/CandidateBuild/CaseId ✓（r21）、独立审查 PASS ✓（R16-R21 各轮双轴循环审查）、人工 clean-install 确认可见可点 ✓、证据不继承旧 DLL ✓。
- **边界**：本工单关闭 = DEV-16B 范围完成；**发布授权仍需 DEV-15E 资格门禁与三环境证据**（DEV-16C/D/E 待实现）。
- 状态：`ready-for-human` → **`resolved`**。
