# DEV-V5-06 评审链（弹药后备 HUD）

日期：2026-09-16 · 轴：Standards（standards-reviewer）/ Spec（Spec-Reviewer）fresh 实例并行 · 纪律：docs/agents/output-review-loop.md

## 增量概要

- 新深模块（Lir/Hud，全 internal，不扩契约 2.1，不新 FeatureId）：
  - `AmmoReserveProjection.cs` —— 零引擎类型纯投影：N=身上五页∧口径匹配∧amount>0 备用匣本数（页闸 2..6 入投影=「不含容器/地面」seam 可测；枪上匣=类型无弹量字段结构排除）；M=Σ备用余弹+Σ匹配箱（主路径 supplies 命中即弃 fallback；fallback 仅口径资产+零跳过——谓词 `MagSuppliesMatch` 与压弹服务共用单源）；文案冻结常量「备匣 {0} · 备弹 {1}」，0/0 不早退。
  - `AmmoReserveHudEngine.cs` —— 真机观察面（NoInlining）：equipment.state[GunStateIndices.MAGAZINE_ID..]→枪上匣资产、ItemGunAsset.magazineCalibers + !requiresNonZeroAttachmentCaliber（原版单击 R 参数同源）、身上 2..6 条目扁平化；supplies 集 = `AmmoRepackService.CollectCompatibleAmmoIds`（升 internal 共享）。
  - `AmmoReserveHudSurface.cs` —— 呈现面：读数标签注入原版 `UseableGun.infoBox`（右下弹药信息区）左列下半（ammoLabel 大字号占左列整高、右列=firemode/attach 两行——与 LHT 顶栏尸潮条不同象限零争抢）；Text 仅变化时写、IsVisible 仅翻转时写；随 infoBox 同创建同销毁（卸枪无残留）；功能注销=HideAll。几何常量单源。
  - `AmmoReserveHudAdapter.cs` —— 闸+线：入口只看 `InPlaceReloadModule.ActiveModule/Started/ShuttingDown`（登记=唯一开关，类型自身零功能 bool，红测 4h 反射钉）；Scanner/Apply/HideAll 三 ForTests 缝；全链 try/catch 隔离（HUD 永不打断原版 updateInfo）。
  - `AmmoReserveHudBinder.cs` —— 绑定唯一事实源（04 教训：显式 MethodBase 解析、非属性名绑定）：`UseableGun.updateInfo` 全声明扫描要求恰一命中，漂移即抛。
  - `AmmoReserveHudPatch.cs` —— 纯转发 postfix 面（零状态零开关）。
- 模块接线 `InPlaceReloadModule`：InstallPatches 三面同代共存（core 两权威面 + HUD 画面面），半装失败互撤归零 + RevokeAll；`InstallCoreReloadPatches` 加宿主 installer 缝（无缝=逐字旧形）；UninstallPatches 补 catch（04 实证 ECall 再JIT 教训）+ finally 注销+RevokeAll；HeadlessDecision 只挡 HUD 面（U3DS 权威压弹面照常）。
- 服务重构 `AmmoRepackService`：CollectCompatibleAmmoIds private→internal（HUD 单源消费）；FindCaliberMatch 交集内层循环改调投影层单源谓词（行为等价，逐字语义核对见红测 2d/5c）。
- 测试：`DevV5AmmoReserveHudTests.cs` 五组（N 定义/M 定义/文案冻结/生命周期登记即开关与停画/官方先行消费与 binder 宿主可证）+ Program.cs 旗 `--bue-v5-06-ammo-hud-red` + 全套挂线。

## 红→绿证据链

- 编译红（类型缺席 CS0246）：`red-compile-run.txt`。
- 五组全绿：`tests/.../BetterUnturnedExperience.Plugin.Tests.exe --bue-v5-06-ammo-hud-red` → ALL GREEN。
- 生命周期组首轮 9 红根因 = 宿主真 Patch() 在 ECall 边界抛、到不了 HUD 面（既有 LIR 组不依赖登记故不炸）——修复=core 面同走 installer 缝（04/05 同律，真装真撤具名留 08）。
- 终态：sln Rebuild 0 错误 0 警告（`green-sln-rebuild-final.txt`）；七套全绿（`green-fullsuite-*-run.txt` ×7）；六门禁（`gate-Verify-*-final.txt`）：Core PASS/Contracts 命中与 V5-05 基线逐字相同（`diff` 证）、其余四门 PASS。

## 突变（各证红→还原复绿）

- M1 匣当弹药源 → 红×2（2a/2i）；M2 弃主路径优先 → 红×2（2b/2k 侧）；M3 空匣计入 → 红×1（1b）；M4 页闸删（两处行删）→ 红×2（1a/2j）——首轮 sed 表达式未命中 = 假绿，按行重删才证红（过程如实记）；M5 allowZero 恒真 → 红×1（1d）；M6 零口径不跳 → 红×2（2c/2d）；M7 Stop 不 RevokeAll → 红×2（4c/4d）；M8 headless 也装 HUD → 红×2（4f）。

## 具名接缝缺口（真机面，随 08 收口，不许假称已验）

1. 真 Glazier 注入与 infoBox 左列下半几何不撞原版弹药大字号（唯一无一手运行时反证的面；卸枪销毁路径有 :3749 RemoveChild 一手依据）。
2. 真 updateInfo 调用频度下逐帧全扫的性能与写纪律稳态（结构上仅变化才写）。
3. 真背包/真资产库观察（engine 面宿主零接触）。
4. 真机「停用即裸文本」端到端（补丁体注销后原版自清）。
5. P2P 客机本地观察面正确性（HUD 纯本地，无网络面）。
6. （R2-Spec 采纳补记）真机长会话下 `live` 死引用回收依赖真实 GC 时序（宿主单测可强制 Collect，真机不可控）——弱引用语义正确性由结构保证，回收时延真机面未验。

## 判断记录（递延候选，待双轴对表）

- J1 U3DS 门控取 LIT 先例 `BueRuntimeCompletionChain.HeadlessDecision`（非 LHT 每拍 isBatchMode）：本票面是补丁登记闸不是绘制拍，与 03/04/05 同律。
- J2 文案「备匣 {0} · 备弹 {1}」：票面词「备用匣本数/后备总发数」的 UI 紧凑形；玩家手册「后备 HUD」措辞同步归 08 票外收口（spec 补充说明既列项）。
- J3 无当前匣（枪上无弹匣）：N 照枪口径算、箱侧恒 0（M 定义「给当前匣供弹」无当前匣即无供弹对象）——07/08 真机若判读不同再裁。
- J4 M 主/备分侧求和丢弃「主路径命中时仍计 fallback 和」=现网 per-mag candidateBoxLists 律的集合化；发数与序无关（现网扣量序不影响总量）。

## R1

- Standards（agent_601d503d，fresh）：**CLEAN**——硬违例 0；判断性气味 6 条列名：
  1. 「FindCaliberMatch 重构引入零口径跳过（原文无）」——**驳回（误读）**：`git show HEAD:…AmmoRepackService.cs` 原循环 :763 逐字即有 `if (magCal == 0) continue;`，`MagSuppliesMatch` 与原双重循环等价（双侧非空守卫 + 跳零 + 等值交集），纯提取非改义。证据钉死，无需回归补丁。
  2. 主/备集合化和 vs 现网逐匣扣量序——采认识：发数总量与序无关（现网扣量终态=每匣至多 Max、箱耗尽即零和），静态可推演；**真机交叉验证具名递延随 08**。
  3. surface `live` 列表弱引用槽位无界增长——**已修**：新 Slot 注入时摊销清扫死引用（R2 增量）。
  4. CWT+live 双结构不变量——行为正确（评审自证复位路径成立），**合并递延**（避免修复轮引入新风险；已注释双结构职责）。
  5. 双谓词刻意不同源——评审确认文档化差异成立，免记。
  6. `Post` 命名与仓库 `Postfix` 惯例发散——**已修**：`Post`→`Postfix`（binder 查找串/异常文案/测试 5a 同步；R2 增量）。
- Spec（agent_94825aa4，fresh）：**CLEAN**——票面 8 项判据逐条满足，缺口清单空。

## R1→R2 增量（修复轮）

- `AmmoReserveHudSurface.cs`：注入新 Slot 时摊销清理 `live` 死引用（气味 3 修复）。
- `AmmoReserveHudPatch.Post`→`Postfix`；`AmmoReserveHudBinder` 查找串与异常文案同步；红测 5a 引用同步（气味 6 修复）。
- 气味 1 以 git HEAD 一手比对驳回（等价性），未改任何生产语义。
- 修复后重建：sln Rebuild 0/0；七套全绿复跑；六门禁复跑全绿；V5-06 组复跑 ALL GREEN。
- 突变链重建（每突变带构建退出码核验，杜绝陈旧二进制假证据）：M1 红2（匣入弹药源/双计）；M2 首轮形触发 CS0219=无效突变，换非恒定条件形复跑红 6（主路径优先律整体失守）；M3 红1；M4a/M4b 红1（页闸两半各自可测）；M5 红1；M6 红2；M7 红2（Stop/开关 off 的 HideAll）；M8 红2（U3DS 不武装+诊断）。过程记录：首轮循环 M8 `if(false)` 触发 CS0162 静默失败跑了陈旧二进制（假证据），构建核验门重建后修正；证据文件 `mutants-evidence.txt`。
- R2 双轴：fresh 实例复核增量（R1 结论见上）。

### R2-Spec 阻断→重启后补派成功（全程具名）

第一轮会话内四次 fresh 派发全部以「Model returned no text, no tool calls, and no usage」空返回失败（agent_a37ad232、agent_46c8acd5、agent_27c4098d、agent_022b1ee0；另一次 agent_53c31059 被我误停未执行，不计）——零 token 失败=路由层故障非提示词问题。根因=已入库坑复发：`spec-reviewer.md` 的 `model:` 被回写为 `gpt-5.6-sol`（2026-09-10 DEV-V3-05 同款「db 回写复发」）。会话内修复落盘=改为当日 Standards 轴实证在跑的 `custom:94b55c9e…:claude-sonnet-5`；子代理定义缓存不热加载（09-10 实证规律）→ 用户裁定「重启后新会话补派」。

**R2-Spec（agent_a1264d1c，fresh，重启后 2026-09-16）**：**CLEAN**——判定 1 增量两改动零规格偏离（live 清理=纯内存卫生不触 N/M/文案/几何/写纪律；Post→Postfix 三处同步一致、接缝身份未变）；判定 2 票面四验收条全部成立（红测链/官方先行消费=真 updateInfo 唯一权威出口/双轴链随本复核补齐/候选纪律 diff 全量核对无 publish/RELEASES/CaseId）；判定 3 缺口清单诚实，**附采纳一条**：具名补记第 6 条缺口（真机长会话 `live` 弱引用回收依赖真实 GC 时序，宿主单测可强制 Collect、真机不可控）。过程记录：mojibake 突变证据文件经判读不影响结论（原文以 UTF-8 写入、控制台混码页，前轮已记 [[bue-build-env-pitfalls]] GBK 坑）。

### R2-Standards（agent_f5cc954f，fresh）

**CLEAN**——0 硬违例；R1 三条处置全部独立复验认可（气味 3 修复迭代安全、气味 1 驳回经 git HEAD 双侧逐字对照成立、气味 6 三处同步一致）；递延 2/4 认可。新增 2 条低权重判断（具名列出，不阻断、不改码）：
- N1 `InPlaceReloadModule` 三组字段（Hud/StartGate/Installer 与既有 Patches/StartGate/Core）结构平行（Data Clumps 倾向）——评审明言未达必拆门槛且与 04/05 平行字段先例同风格，随本票具名留档；
- N2 `UninstallPatches` finally 清位与 `InstallPatches` catch 清位四处近重复——提炼收益有限（触发场景不同），具名留档。
过程记录：R2-Spec 首实例（agent_a37ad232）空返回（模型故障形状，不计入链）→重派实例被误停→fresh 复派（agent_46c8acd5）。

