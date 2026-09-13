# POST-P4-03 输出审查链

票：`.scratch/bue-post-phase4-closure/issues/03-favorite-restart-dual-key.md`
日期：2026-09-13
固定点：HEAD `fe934ef` 工作区增量（提交前审查），R1 冻结 diff = `review-freeze-r1.txt`（400 行），R2 冻结 diff = `review-freeze-r2.txt`（463 行，清单 `changed-files-r2.txt`）
契约：仍 2.1（`ContractTypes.cs` 不在增量内；新缝全 `internal`，无 SDK 面）
候选：不授（非发布票，面板模型/原生 chrome 修复）

## 红 → 绿

- 接缝：`ManagementPanelModel`（与 F4 同一模型 seam，`internal` + `InternalsVisibleTo`），不断言 Glazier 控件树。
- 红-1（编译）：新测试引用尚不存在的 `ToggleFavorite(string, ManagementEntryKind)` 与 `ShowsRestartBadge` → `CS1501 ×3` + `CS1061 ×3`（实录于会话构建输出）。
- 红-2（断言，先建最小桩 API 过编译后观察）：ClientUi exe FAIL=`InvalidOperationException: 同 id 插件行不得跟亮`——即票面 NoOp 双行抢一颗星的原缺陷。桩=`ToggleFavorite(kind)` 直落单键、徽章只比 StableId；实现后两红转绿。
- 实现（R1）：
  - 收藏按（种类, StableId）分桶：`ToggleFavorite(stableId, kind)`；单参 `ToggleFavorite(stableId)` 改显式 features-first（有功能拨功能行，否则拨插件行），不倒退成两行同亮。
  - 顶栏「需要重启」徽章上移模型：`SaveDraft` 成功/部分失败时按最近一次**尝试写入**的 `(draft.Kind, draft.StableId)` 记录/熄灭（`RememberRestartBadge`），NoChanges 早退不动（继承 Q67）；`ShowsRestartBadge` 仅当**当前草稿**与之同种类同 id 才亮。原生面板删 `restartBadgeStableId`/`TrackRestartBadge`，渲染与收藏按钮（带 `selected.Kind`）只读模型。
- 绿（R1 形态下）：ClientUi PASS；全套 `-t:Rebuild` exit=0、警告/错误 0（`green-rebuild-sln.log` 仅编译器命令行回显含 warnaserror 字样，无 CS 告警行）；7 exe 全绿（首轮 `green-fullsuite-*.txt`）。

## 双轴（Fresh-instance；R1 发现 → 修复 → R2 全新实例复核）

| 轮 | Standards | Spec | 处置 |
|---|---|---|---|
| R1 | CLEAN（硬性无；Primitive Obsession=字符串前缀编码/数据泥团=徽章双字段/FavoriteOrderIndex 两职） | **2 条发现**：① 旧裸 id 运行时回贴（features-first 优先插件兜底）使拆分随功能注册/注销漂移，违背票面「如何拆分须确定且可测」，且未覆盖插件独有 GUID；② 「顶栏/行级」行级一面未钉红测 | 修复 |
| R2 | CLEAN（硬性无；3 条判断性气味具名递延见下） | CLEAN（迁移方案选定+红测钉住；徽章切页/关面板/重挂语义符合「当前详情」；范围核查零蔓延） | 关环 |

### R1 发现 → 修复（R2 验证）

1. **漂移缺陷**：改为票面第二方案**按种类分桶迁移**——`Refresh` 目录就绪时把裸 id **一次性**归桶（同 id 碰撞=功能优先，即票面「只贴功能行」的裁决；无同 id 功能=贴插件行，保住独有 GUID 收藏），改写成 `feature:`/`plugin:` 前缀键并立即持久化；此后不随注册/注销漂移。红测钉住：`LegacyBareFavoriteBindsFeatureRowOnly`（碰撞贴功能）+ `LegacyBareFavoriteForPluginOnlyGuidStaysOnPluginRow`（独有 GUID 贴插件、持久化为前缀键、后注册同名功能不抢星）。前缀=模型自有保留命名空间；BepInEx GUID/FeatureId 为反向 DNS 不含冒号（注释声明，Standards R1/R2 均判与契约零变化相容）。
2. **行级未钉**：`RestartBadgeDoesNotCrossKind` 补两面——插件页 `GetPluginConfigRows` 如实 `RequiresRestart` 行级标记；功能页 `GetSettingRows` 不消费插件配置行（行级标记按投影分工天然不跨种类，F4 分支只消费各自投影）。
3. Standards R1 气味「FavoriteOrderIndex 两职+分支拥挤」随迁移重构消除：键构造收敛到 `FavoriteKey(kind,id)` 单一查表。

## 具名 deferrable（Standards R2 全部判定=判断性、非阻断）

1. 徽章态 `(restartBadgeStableId, restartBadgeKind)` 双字段泥团——私有会话态、两处同写同清，打包小类型无当期收益。
2. 种类分支在 `ToggleFavorite(kind)`/`FavoriteKeyStillPresent`/`MigrateLegacyFavoriteKeys` 重复出现——两值 byte 枚举，多态化属臆测通用性。
3. 新测试 NoOp 同 id 夹具与既有 `CollisionFeatureAndPluginShareStableIdRoutesByKind` 同构重复——该测试文件既有写法（逐案自造夹具）。

## 最终绿与终态

- 全套 `-t:Rebuild` exit=0、0 警告 0 错误；7 exe 全绿：`green-fullsuite-{ClientUi,Contracts,Network,Placement,Plugin,Release,Settings}.txt`。
- 收藏即时、不进草稿、不使草稿变脏的既有不变量由 `DraftScopeExcludesReadOnlyAndServerAuthority` 继续钉住；F4 回归组未动且随全套通过。
- 磁盘格式：`favorite=` 行值可为 `feature:<id>`/`plugin:<id>`（URI 转义照旧）；旧裸 id 文件首次加载即迁移改写。

## 身份

不授 SHA-256 / CaseId / RELEASES 行。本票只动面板模型收藏/徽章与原生 chrome 读取。
