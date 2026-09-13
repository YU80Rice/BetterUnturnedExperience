# V4-T4 功能级启停的面板表面

- **Ticket**: V4-T4
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-11 Q44–Q53 定音）
- **Blocked By**: V4-T1, V4-T2
- **Map**: [map.md](../map.md)

## Question

开图已定：功能级启停要做；意图进未保存草稿；保存时才 `SetFeatureEnabled`；LIT 不再用设置里的 `enabled` 当总开关。本票裁画面与生命周期投影：

1. **按钮长什么样**：详情页一颗「启用/停用」，还是 Minecraft 式开关 + 底部统一「保存配置」？未保存的启停意图如何与当前 `FeatureState` 区分（例如「运行中，保存后将停用」）？
2. **哪些条目有启停**：仅 BUE 功能（官方+生态）？外部 BepInEx 插件明确没有进程级启停（ADR-0002 / UPM 也不热卸载）——是否冻结为「外部条目无此按钮」？
3. **状态文案**：运行中 / 已停用（UserDisabled）/ 已隔离（Isolated）/ 表现降级。隔离原因是否展示？`bue.network` 的良性 Isolated 是否本票处理（地图雾区倾向：否，另案）？
4. **保存时语义**：停用→UserDisabled；再启用→新代际；Isolated 仅经此恢复。草稿里反复拨开关、只保存一次，以最后意图为准还是中间态也要进权威源？
5. **与设置 enabled 退役**：哪些官方功能今天把 enabled 当生命周期代理（LIT 已点名）？退役后磁盘上旧 `enabled=false` 如何迁移到 UserDisabled，以免升级后功能又跑起来？
6. **无面板环境**：U3DS 不能点按钮；默认启用纪律是否完全不动？

事实输入：V4-T2 的草稿边界；模型缝 `TryToggleFeature`（DEV-V3-06）。不重开「要不要做启停」。

## Answer

2026-09-11 用户 Q44–Q53 定音。本票交付=功能级启停的面板表面与生命周期提交语义：面板只编目标，状态投影只展示事实，生命周期机解释一次过渡。现网 `SetFeatureEnabled` 对「已停再停 / 已跑再开 / Isolated+disable」返回失败——空操作成功是生命周期机语义修复，不是 ClientUi 分支。

### Q44 — 画面

详情页一颗「启用」开关 = **保存后的目标状态**（开=启用，关=停用），只进草稿，不立即改运行状态。当前 `FeatureState` 仍是只读投影。目标与现状不一致时 ClientUi 对照两者提示待生效（例如「运行中，保存后将停用」「已隔离，保存后将尝试启用」——不承诺一定成功）。底部统一「保存配置」。无「立即启用/停用」按钮。目标差异投影不进 SDK（Q53）。

### Q45 — 谁有开关

由条目是否拥有**可停止的 BUE 功能生命周期 seam**决定，不按是否官方 DLL。显示：官方功能、经注册桥的生态功能、NoOp、Network、v1compat。不显示：外部 BepInEx 插件、仅 ConfigEntry 兼容展示的普通插件、核心 Host/Contracts、管理面板自身（未登记为目录功能）。网络模块视为可选 BUE 功能，可停；不停用不等于卸 DLL。

### Q46 / Q50 — 状态文案

面板不 `FeatureState.ToString()`。ClientUi 状态投影映射：

| FeatureState | 玩家文案 | 启停开关 |
|---|---|---|
| Discovered | 待启动 | 显示 |
| Starting | 启动中 | 显示 |
| Running | 运行中 | 显示 |
| Stopping | 停用中 | 显示 |
| Isolating | 隔离处理中 | 显示 |
| Isolated | 已隔离 | 显示 |
| Disabled / Stopped（UserDisabled 语义） | 已停用 | 显示 |
| Incompatible | 不可用 | **不显示** |
| 未映射 | 不可用 | 不显示 |

`Disabled`/`Stopped` 须结合停用原因；非 UserDisabled 的停止由投影映射安全文案，不得一律伪装成用户停用。表现状态独立一行。隔离原因有值才显示在功能状态下方，不占空位。`bue.network` 良性隔离文案本票不改。

### Q47 / Q51 — 保存时一次过渡

草稿只保留最后目标（启用或停用），中间拨动无生命周期副作用。保存时面板提交目标状态；生命周期机按**提交时**权威状态解释：

- 目标与当前有效意图一致 → **空操作成功**（含：已 Running 再启用、已 UserDisabled 再停用、**Isolated 且目标停用**——保持隔离，不走会失败的 disable，不新开代际）；
- 目标停用且当前正在运行 → 停用（UserDisabled）；
- 目标启用且当前已停用或已隔离 → 启用/恢复并**新生命周期代际**；
- 不允许的转换 → 失败，该意图留草稿。

ClientUi 不 `if (Isolated && Disabled)`。现网 `SetFeatureEnabled` 的 not-running/invalid-state 失败语义由本阶段生命周期机改为上述空操作成功。

### Q48 / Q52 — 生命周期代理设置

官方不再把设置 `enabled` 当生命周期权威。只对**显式登记的 legacy lifecycle alias** 做一次性幂等迁移：

- 登记：LIT / LIR / LHT / Network / v1compat 的旧 `*.enabled`，以及 BII 的 `Enabled`；
- 不登记：BII `AutoRotate`、NoOp `noop.probe-toggle`、未声明 alias 的任何同名设置、生态功能（除非自愿声明）。

规则：旧值 `false` → 写入 UserDisabled **意图事实**（再由生命周期机按当时状态决定是否实际停用，不在 Isolated 上盲调 disable）；旧值 `true` 或不存在 → 不额外改生命周期；已有新权威记录则以新为准；成功写入新权威前不得丢旧值；成功后旧字段从 schema 与面板退役。不按字段名扫描。

### Q49 — U3DS

不构造面板、不显示开关、不创建客户端草稿。默认启用与 ServerAuthority 纪律不动。客户端未保存意图不影响服务端生命周期。不为 U3DS 增加命令行或其他控制面。

### Q53 — 契约

目标差异（当前/目标/脏/将执行的过渡）不进 SDK。面板拼文案并提交一次目标；生命周期机返回显式结果。公开差异查询若需要，另开可选 facet + Minor 2.2。本票不扩 `IFeatureRegistration`。

## Impact

- `CONTEXT.md`：收紧功能级启停；新增目标启停意图、生命周期代理设置。
- 生命周期机：提交目标状态的空操作/恢复语义（相对现网 SetFeatureEnabled 失败码）属本阶段实施，不是面板 if。
- SDK：零成员增减。
- ADR-0002：外部插件仍无热卸载/进程级启停。

## Comments

- 2026-09-11：用户对 Q44–Q49 做 seam 收紧，Q50–Q53 再钉映射、Isolated 空操作、显式 alias、不进 SDK 后结票。事实：九态枚举、面板现 ToString、SetFeatureEnabled 非幂等、管理面板未入目录、BII Enabled 非 `*.enabled` 字面。
