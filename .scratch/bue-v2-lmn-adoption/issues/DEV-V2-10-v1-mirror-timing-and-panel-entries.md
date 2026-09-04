# DEV-V2-10：V1 镜像时机修复（bootstrap 早于 LMN 建表）+ 面板网络条目缺失

Type: task
Status: resolved（2026-09-04，agent；红测先行 + 三轮双轴审查 R1/R2 FINDINGS → R3 双 CLEAN 闭环）
Parent: spec-V2-phase1-lmn-adoption（DEV-V2-07 配置 B 复核 `audit/2026-09-04/DEV-V2-07/configB-verification-r1.md`）
Blocked by: 无（DEV-V2-05/06 已 resolved）
Blocks: DEV-V2-07（三环境验收——P5 零误报 + P2/B4-B6/P4b 面板四步）

## 发现（实机四端复现，证据见 DEV-V2-07 configB 归档）

**F-A（error 级，违反 P5 零误报）**：BepInEx 按文件名序加载，BUE（B）先于 LMN（L）bootstrap。`MirrorLegacyHandlersSafe` 在 LMN 尚未建表时运行，抛 `ArgumentException`（四端各一条）：
`BUE 错误：[BUE-V2NET] event=v1-table-mirror result=failed errorType=ArgumentException decision=drop-path diagnosticId=BUE-V2NET-002`
镜像全程失效，V1 全靠自愈链（BUE 前缀放行 → LMN 原生前缀送达，`unknown-channel-dropped` 实为让位非丢帧）扛住。功能未坏，但：(a) ERROR 行违反 P5；(b) 镜像守护价值（频道表同步/面板重镜像）失效。

**F-B（阻塞面板四步）**：管理面板侧栏缺「BUE 网络模块」/「BUE V1 兼容层」条目（`ClientUiCompositionRoot.ToManagementEntry` L98-99 的目录映射存在，但条目未出现在 `GetEntries()`），接管卡（接管状态/配置迁移/可逆钮「让我改回独立 LMN」）不可达。疑与 F-A 同根：`RefreshManagementPanel` 仅在 Initialize（L68）与 `BetterUnturnedExperiencePlugin.cs:278`（条件调用）执行，时机可能早于网络功能注册；或 `bueFeatures` 组装未含网络条目。

## Scope

1. 红测（先行）：构造「BUE 先载、LMN 表晚建、ch250 晚注册」时序锚点（复用 DEV-V2-05 no-op fixture 形状）——红：bootstrap 镜像抛 `ArgumentException` 且错误行出现；红：面板条目在注册完成前缺失。
2. 修复 F-A：镜像在 LMN 未就绪时静默延后/惰性重试（Info 以下、零 ERROR 行），或注册事件驱动重镜像；错误行只允许留给真故障（`result=failed` 语义收紧）。
3. 修复 F-B：网络功能注册后刷新面板条目（或注册时主动通知面板刷新），确保「BUE 网络模块」「BUE V1 兼容层」条目在主菜单与游戏内部板均可见、接管卡可达。
4. 全量门禁：Release 重建 0/0、七运行器、NoUiTokens。

## 验收条件

- [ ] 红测 observed red → green（两 finding 各至少一锚点）。
- [ ] 七测试运行器 exit=0；全解决方案 0 error/0 warning。
- [ ] 时序锚点测试证明：LMN 晚注册场景下镜像最终生效且**无 ERROR 行**。
- [ ] 面板条目测试证明：注册完成后 `GetEntries()` 含两条网络条目。

## 不做

- 不改候选身份流程：修复走新候选（新 DLL/新 BuildIdentity），DEV-V2-07 已归档实机证据保持原样入复核记录。
- 不动 BueNetworkApi 契约面。

## Comments

> 2026-09-04 建票并认领（agent）：DEV-V2-07 配置 B 四端实机复核触发。P4a 实质通过（V1 双向互通，机制=自愈链），本票修守护层与时序，不推翻功能结论。

> 2026-09-04 resolved（agent）：闭环记录见 `audit/2026-09-04/DEV-V2-10/DEV-V2-10-closing-report.md`。要点：
> - **F-B 根因修正**：非「刷新时机早于注册」，实机日志两场均为 `BUE Network Module … accepted=False reason=InvalidDefinitionArtifact (BUE-REG-004)`——注册件 `ArtifactPayloadDigest` 为手抄伪值，与 payload "BUE-NET-V1" 真实 SHA-256 不符，注册被运行时拒绝，目录里根本没有网络条目。修复=摘要改为从 payload 计算（`ComputePayloadDigest`，不再手抄）+ V1 兼容层按规格 L74 立为自己的官方注册件（`bue.network.v1compat`，此前 `ToManagementEntry` 的映射是死映射）。
> - **F-A 修复**：LMN 未就绪（类型解析不到/表为空）→ `result=deferred` 一次 + 插件 Update 节流重试（300 tick），表空同样保持 pending（真实时序：LMN Awake→旧插件 Awake 才建表注册）；`result=failed` 语义收紧为真形状故障。零 ERROR 行走生产路由断言（BindProductionLog + BueRuntimeLog）。
> - **审查驱动的加固**（R2/R3）：面板锚点改生产时序（Initialize 前置刷新无条目 → CompleteRuntime → 完成后刷新两条目出现，红在条目级）；模块关闭=零镜像（H3）；开局禁用重开后 patch 无条件重臂（H4，手册 B6）；v1compat payload 拼写 CUE→BUE 修正 + 文本钉死（H5）。
> - **门禁**：Release -t:Rebuild 0/0、七运行器 exit=0、NoUiTokens Core18/ClientUi11、`git diff --check` 干净。红观察 8 份 + 绿 4 旗标全部留档 audit/2026-09-04/DEV-V2-10/。
> - **具名延期**：主菜单/游戏内两入口的端到端条目可达性断言（现有 hook/模型级证据覆盖，实机 P2/B4-B6/P4b 补采为最终验证）。
> - kit runner 官方定义表同步（network 摘要更正 + v1compat 入表）属身份工具跟踪官方定义集；候选身份在提交后授予（新 DLL/新 BuildIdentity），DEV-V2-07 归档证据未动。实机复测（面板四步 + 零错误行）由人工执行。
