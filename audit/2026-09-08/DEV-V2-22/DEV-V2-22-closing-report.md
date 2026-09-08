# DEV-V2-22 结单报告:LIR 纳入——更好的换弹体验(ReloadContextGuard + 宿主时钟 + 事件消费)

日期:2026-09-08|票据:`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-22-lir-adoption.md`|状态:**resolved(双轴 R3 双 CLEAN)**

## 1. 交付内容

裸 BUE 的原位换弹官方功能模块(旧独立插件 LaunchInPlaceReload 迁入单 DLL,BepInEx 身份与 LMN 硬依赖按构造消失):

- **稳定缝(T5 拍板)**:`ReloadContextGuard`(+`ReloadSlotContext` 值缝)= 旧 P2PAmmoManager 协议的具名化,叠加点红测钉在 guard 输入输出(不装补丁);`IReloadAction`+`LirReloadActionContext`= 换弹策略 adapter 缝,本期唯一 adapter=`AutoReloadAfterTidyAction`(整理后自动压弹,不加新开关);`TidyCompletedConsumer`= 事件消费唯一缝(验 Succeeded+范围 2..6 含倒置范围拒绝+发布者事务号高水位幂等+代际→目标解析(gen=0 本地/会话代际=服务器代执行整理的 peer,旧 postfix 语义经冻结载荷还原)+动作异常隔离)。
- **三枚补丁(模块内部)**:`UseableGunReceiveAttachMagazinePatch` Prefix/Postfix + `ForceAddItemPatch` Prefix,Harmony ID=FeatureId(`io.github.yu80rice.bue.in-place-reload`),`Stop` 只 `UnpatchSelf` 撤自身;旧 `TidyServicePostfixPatch`(反射寻 LIT 类型+跨插件 postfix)由 TidyCompleted 功能事件替代,反射删除。
- **宿主时钟驱动**:模块只订 `HostTick`(无自建 Update 泵),自负责:首帧游戏线程网络初始化(旧延迟语义,落在模块生命周期内)+半注册回滚(任一订阅失败→句柄 Dispose+频道注销,零残留,本地路径存活,不重试)、会话簿 reconcile、dispatcher drain(状态推进+工作 TTL 超时判断)、双击检测+换弹键轮询(0.3s 窗口、触发后下一按只重锚的连击防护、键轮询异常隔离)。
- **网络(功能私有协议)**:`LirRepackWireCodec`(两消息自组 byte[],fail-closed 结构校验,替代旧 ModTransport.BuildNamedMessage)+`LirRepackNetwork`(FeatureId 即频道身份,旧 LMN 频道串退役;客机请求→服务器,服务器回包按会话定向 SendToClient,客户端 pending/replay 待确认表)+`LirRepackDispatcher`(每服务代际一实例:请求按 sender 合并、回包优先、TTL 丢弃、上限 64、节流聚合诊断)+`LirRepackGate`(1.5s 冷却/120s replay 窗口/128 容量 fail-closed/Quarantine,Stop 代际清)+`LirProductionAuthority`(玩家解析走 BueEngineNet 静默反射缝,零 Steamworks 编译引用,LIT 先例)。
- **事务引擎原样迁移**:`AmmoRepackService`(1502 行,压弹+合并事务、快照指纹、RestoreExact 回滚;仅日志缝换 LirRuntime、去 ThreadUtil 断言(LIT 先例)、夹具桩 ExportCanonicalSha256 按「夹具不进玩家 DLL」不迁)。
- **宿主接线**:`InPlaceReloadFeatureRegistration`(BORN inert,哨兵 DefinitionSetDigest 1,0,0,19,负载 "BUE-LIR-V1");面板条目 FeatureId 身份+中文名「更好的换弹体验」+enabled 开关编辑路由;`ToastSink` 缝(生产=LirToast,测试=录制器)。

## 2. 红绿链与审查链(全记录见 `red-evidence.md`、`review-rounds.md`)

- 红绿:编译红 15 错(独立锚点)→桩级红 34 条→运行时红 7→1→ALL GREEN;修复轮新断言先红(2 条)→绿;F2 后复验 ALL GREEN。全套 7/7 PASS、0 警告。
- 双轴独立审查(每轮全新实例,无续用):
  - R1 双轴 NOT CLEAN:Standards 2 BLOCKING+4 SMELL+1 INFO(未用 using/服务树 public 泄漏/数据团/准入重复/toast 重复/死检查/静默 catch);Spec GAP-1(停止注销)+DEVIATION-1(非法页范围)。
  - F1:全部落地;GAP-1 以冻结交接缝反驳(DEV-V2-19 F3:宿主 Stop 返回后 `UnsubscribeAll(feature)`,BueFeatureStartRuntime 已落地);DEVIATION-1 修复带红→绿锚点。
  - R2:Standards **CLEAN**(反驳裁定接受;残留 SMELL×3+INFO×1);**Spec R2 派发失败作废**(基础设施失败:模型零输出,无判词,按 fresh-instance 规则不得续用,如实留痕)。
  - F2:SMELL/INFO 全修(面板路由闸补 lirModule——潜在缺陷;计数一律 Interlocked;guard 单结构;请求路径诊断)。
  - **R3 双轴双 CLEAN:Standards CLEAN(3 SMELL 具名可延期)/Spec CLEAN(REBUTTAL-GAP1 维持接受,零 GAP/DEVIATION/SMELL)。**

## 3. 验证与身份

- 候选:`BetterUnturnedExperience.dll` **489984 B**,SHA-256 `a04b52ebc5ba3d240204ec1d50520a785a181e8a7d475232fcde0d426c005d65`,三轮 `-t:Rebuild` 字节一致(`identity-rebuild1/2.log`、`identity-sha256.txt`);CaseId **`DEV-V2-22-CANDIDATE-20260908`**。RELEASES 换标随 DEV-V2-24 实机验收(沿 17/18/19/21 惯例,不继承既往批准)。
- 冲突审查复核(验收③):全仓 src+tests 扫描——`ReceiveAttachMagazine` 除 LIR 补丁零命中;`forceAddItem` 除 LIR 补丁与新红测注释零命中;非 LIR 的 HarmonyPatch 目标仅 `PlayerDashboardInventoryUI`(LIT 面板按钮)一处,与 LIR 三补丁(`UseableGun.ReceiveAttachMagazine`/`PlayerInventory.forceAddItem`)**零交集**(T5 决策 2 事实迁入后复核)。

## 4. 具名延期 / 边界(R3 判词载明,均不阻断)

1. **AutoReloadAfterTidyAction Feature Envy**(只读模块开关并转发)——`IReloadAction` 薄缝的既有形态,未来 ReloadAction 变体增多时随策略治理票收敛。
2. **AmmoRepackService Merge/Repack 事务骨架重复**——旧源保真迁入(行为不变优先),重构随后续治理票。
3. **闸/日志缝/请求序列进程静态**——生产单模块、LIT 同构(LirRuntime.LogSink/RequestId 序列同型);Stop 已清闸与 sinks,dispatcher 已代际化。
4. **实机自验**(双击压弹手感/叠加点真机/P2P 全链)绑 DEV-V2-24 终票三环境验收,与本票候选身份对账。
5. 旧 `P2PAmmoManager`/`RepackRequestGate`/`RepackMainThreadDispatcher` 类型名随迁入消失(以 ReloadContextGuard/LirRepackGate/LirRepackDispatcher 承接),旧频道 `com.yu80rice.launchinplacereload.repack` 退役。

## 5. 档案清单

`compile-red.log` / `stub-build*.log` / `stub-red-run.log` / `impl-build*.log` / `impl-red-run1/2.log` / `impl-green-run.log` / `fix1-build-red.log` / `fix1-red-run.log` / `fix1-green-run.log` / `sln-rebuild-fullsuite.log` / `sln-rebuild-r2.log` / `fix2-build.log` / `fix2-anchor.log` / `round1/2/3-increment.diff` / `identity-rebuild1/2.log` / `identity-sha256(-r1).txt` / `red-evidence.md` / `review-rounds.md` / 本报告。
