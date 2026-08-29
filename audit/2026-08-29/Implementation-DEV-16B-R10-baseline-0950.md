# DEV-16B R10 基线固化审计

- 日期：2026-08-29 09:50（Asia/Shanghai）
- 执行者：DeepSeek Harness Agent（本轮会话）
- 基线：`master` @ `2bf61ce` + 7 个未提交修改文件（R10 方向，mtime 2026-08-28 15:12–15:38）
- 输入：`BUE-Project-Handoff-2026-08-29.md`、`audit/2026-08-28/RuntimeDiagnosis-DEV16B-142408.md`、RT-07 核验报告

## 结论

**静态基线固化 PASS；运行时未证明；发布未授权。**

R10 方向的未提交修改（插件自身 `Update()` 直驱 driver、三路注入局部隔离、Runtime tick 去重/重入/fail-closed、Runtime Catalog 消费、ConfigEntry 兼容编辑）已通过完整 Release 构建、全部 7 个测试运行器、静态门禁与 `git diff --check`。新 DLL 已落 r10 产物目录并记录 SHA-256。DEV-16B 的真实客户端按钮可见性门禁**仍未通过**——唯一缺口是人工 clean-install 部署 + UMM 诊断包证据。

## 1. 构建

- 命令：`dotnet build BetterUnturnedExperience.sln -c Release`
- 结果：**0 错误 / 0 警告**（日志：本目录 `build.log` 摘要见会话记录；构建为增量，确认 R10 源码已在产物基线内）

## 2. 测试（7/7 PASS，日志归档本目录）

| 测试运行器 | 结果 | 覆盖 |
|---|---|---|
| Contracts.Tests | PASS | DEV-10 registration runtime |
| Network.Tests | PASS | DEV-06 network codec/fence |
| Placement.Tests | PASS | DEV-04 placement evaluator |
| Settings.Tests | PASS | DEV-03 settings runtime |
| ClientUi.Tests | PASS | DEV-05/15A/15B/15C/15D/16B ClientUi |
| Release.Tests | PASS | DEV-15E qualification evidence |
| Plugin.Tests | PASS | DEV-14/DEV-16B plugin runtime（含 R10 新增：driver 转发、三路隔离、同帧去重、重入拒绝、fail-closed、panel→seam、Catalog 投影、parent 重绑、单 DLL 闭包、SDK 身份） |

单 DLL AssemblyRef/ABI 闭包检查由 `AssertSingleDllAssemblyClosure` / `AssertExternalSdkAssemblyIdentity` 在 Plugin.Tests 内覆盖并通过。

## 3. 静态门禁

- `eng/Verify-NoUiTokens.ps1`：Contracts（2 文件）/ Core（10 文件）/ ClientUi（11 文件）全部 **PASS**（日志：`noUiTokens-gate.log`）
- `git diff --check`：**CLEAN**（仅 CRLF 转换提示，无空白错误）

## 4. 新 DLL 产物（不继承旧证据）

- 目录：`artifacts/DEV-16B-management-panel-runtime-fix-r10-20260829/`
- 文件：`BetterUnturnedExperience.dll`，**168960 bytes**（r9 为 159744 bytes；+9216 bytes 为 R10 新增 driver/dispatcher/adapter 代码）
- SHA-256：`AF065D83B04D604113CB96DDB57B7A39CD98F1BEC1456DA303ECD6A1F1064CBE`
- 二进制验证：`BuePluginUpdateDriver`、`BueRuntimeTickDispatcher`、`BueButtonInjectionCoordinator`、`PresentationDegraded` 类型名均存在于 DLL 元数据
- **CaseId**：`DEV-16B-R10-20260829`（新 CandidateBuild；旧 r9 证据 `0C158856…` 仅作对照，不得引用为本轮证据）
- 部署边界不变：客户端只部署此单一主 DLL；不部署 Contracts/Core/ClientUi 独立 DLL；U3DS Headless 不得创建 UI。

## 5. 判别矩阵（真机 UMM 诊断包读数方法）

部署 r10 DLL 后，按以下顺序读 `LogOutput.log` 中的 `[BUE-UI-TRACE]` 事件，第一处非零即定位故障边界：

| 顺序 | 事件 | 非零含义 | 仍为零时的结论 |
|---|---|---|---|
| 1 | `plugin-update` | Unity 已调度插件 `Update()`，驱动边界通过 | 假设 #2 成立：宿主不调度插件 Update → 需生命周期探针 |
| 2 | `runtime-pump-tick` | 独立泵 MonoBehaviour 被调度 | 前者非零而此为零属预期（r10 已隔离泵依赖） |
| 3 | `host-ui-tick` | Harmony `MenuUI/PlayerUI.Update` postfix 实际命中 | 假设 #3 成立：Harmony 登记成功但 detour 未生效 |
| 4 | `create-button-begin` | 容器非空、注入开始 | 容器时序边界：检查 `surface-opened` 与容器重建 |
| 5 | `create-button-result` / `add-child-success` | 按钮创建/挂载成功 | 注入实现边界 |
| 6 | 截图可见按钮 | 可见性门禁通过 | 可见性/布局边界 |

判定规则：`plugin-update` > 0 且最终截图无按钮 → 按表继续向右定位；六项全零 → 假设 #1（部署身份错误）复核 `assembly-identity` SHA-256 是否等于本文第 4 节值。

## 6. 已知风险（2026-08-29 R10b 轮更新）

1. ~~`TryCompleteRuntime()` 无异常屏障~~ → **已在 R10b 修复**（`BueRuntimeCompletionBarrier`：未就绪可重试、异常永久隔离并保留 `LastFailure` 诊断；`CompleteRuntime` 成功后的 Refresh 副作用局部隔离，不再阻塞 RuntimeReady 输出）。详见第 9 节。
2. `BueRuntimeTickDispatcher` 在 `log == null`（纯测试宿主）时 frameProvider 恒为 `-1`，同帧去重语义与生产分叉；生产路径不受影响，已有测试锁定哨兵行为。
3. 面板挂载于 vanilla 页面容器（非独立 `SleekWindow`），光标/遮罩/输入焦点需真机确认（交接文档第 8.4 条）。
4. **待规格裁决项（双轴审查发现，未在本轮修改，已记入工单 Comments）**：枚举（AcceptableValueList）条目只读 vs 规格要求的校验编辑路径；第三方条目显示名/版本/运行状态模型缺口（`FeatureState` 恒 Running）；面板打开时整页遮蔽宿主页面的规格授权；`IsAlive` 反射失败返回 false 的 fail 方向。

## 7. 唯一人工步骤（HITL，2026-08-29 更新为 r10b 产物）

1. 备份并替换 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll` 为 **r10b 产物**（部署前删除旧文件，避免残留）。
2. 启动 Unturned → 进入主菜单（Workshop 页与 Dashboard 页各停留数秒）→ 进入游戏 → 打开暂停菜单。
3. 导出 UMM 诊断包，并将包内 `LogOutput.log`（含 `assembly-identity` 行）与截图交回。
4. 预期比对：`assembly-identity` SHA-256 必须等于 `D8F9AF516AC530EDDA93AE61DDBE168375C2F6B96027751C1CB7F6FCEA4212D5`（r10b），否则为部署身份错误。

## 8. 三态声明

- **静态已证明**：R10b 代码编译、7/7 测试、门禁、单 DLL 闭包、diff 卫生、双轴子代理审查（Standards/Spec 独立并行）通过。
- **运行时未证明**：按钮可见性、面板交互、Harmony 命中、容器时序。
- **发布未授权**：DEV-16B 不得标记 resolved/Stable/三环境通过，直至新证据包与资格门禁完整通过。

## 9. R10b 修复记录（2026-08-29，双轴审查驱动的 TDD 轮）

**范围**：仅修复审查发现的阻断项，其余记入 §6.4 待规格裁决。

| 发现来源 | 问题 | 修复 |
|---|---|---|
| Standards（硬违规） | 屏障 catch 裸吞异常，违反「核心安全降级保留最小诊断」 | `BueRuntimeCompletionBarrier` 保存 `LastFailure`；日志补 `errorType=` |
| Standards（smell） | `completionIsolatedLogged` 与屏障幂等重复 | 首次隔离回调 `onFirstFailure` 内聚进屏障，插件字段删除 |
| Spec-② | `RuntimeCompletionIsolated` 缺 Decision 结构化字段 | 日志补 `decision=Isolate`（符合规格 §3 结构化四元组） |
| Spec-③ | `CompleteRuntime` 成功后 Refresh 抛异常 → RuntimeReady 永不输出、退订被跳过 | `TryRefreshAfterCompletion()` 副作用局部隔离（`BUE-CLIENTUI-004`），RuntimeReady 事实输出不再被 UI 刷新失败阻塞 |

TDD 闭环：红（CS0246 类型缺失）→ 绿（`AssertRuntimeCompletionBarrierIsolates` 13 断言，含 LastFailure 保留、回调仅一次、未就绪可重试、完成幂等）。

**新产物**：`artifacts/DEV-16B-management-panel-runtime-fix-r10b-20260829/BetterUnturnedExperience.dll`，169984 bytes，SHA-256 `D8F9AF516AC530EDDA93AE61DDBE168375C2F6B96027751C1CB7F6FCEA4212D5`，**CaseId `DEV-16B-R10B-20260829`**（r10 的 `AF065D83…` 从未被部署，保留作对照）。验证：构建 0/0、7/7 测试 PASS、NoUiTokens 三 SourceRoot PASS、`git diff --check` CLEAN。
