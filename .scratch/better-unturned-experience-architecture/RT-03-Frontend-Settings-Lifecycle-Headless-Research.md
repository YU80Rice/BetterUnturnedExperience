> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-RT-03：统一设置 UI、模块生命周期与 Headless 结构隔离调研报告（第二轮修订版）

> **作者**: Gemini（前端负责人）  
> **审查者**: GPT（生命周期与设置契约强制 Reviewer）  
> **适用任务**: RT-03  
> **依赖基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **Predecessor SourceSetId**: `BUE-SS-20260824-01`  
> **Migrated SourceSetId**: `BUE-SS-20260824-02`（Manifest: `BUE-SS-20260824-02-Manifest.md`）  
> **证据枚举标准（严格对齐 RT-01 §10.2）**:  
> `SOURCE_CONFIRMED`、`IL_CONFIRMED`、`PROTOTYPE_ONLY`、`BUILD_CONFIRMED`、`RUNTIME_CONFIRMED`、`RELEASE_CONFIRMED`、`UNRESOLVED`  

---

## 1. 调研目标与范围

本报告依据需求总纲 `spec.md` 及 `GPT-RT-01` 共享契约基线，针对 Unturned 原生 UI 框架（SDG Glazier / Sleek UI）、菜单生命周期、模块状态投影以及 U3DS Headless 无图形服务端的强隔离边界进行固定源码事实调研。

目标在于建立从**双入口设置菜单唤出 → 构建期 Settings Facet 与运行时快照绑定 → 滑块/按键输入防抖与政策锁定 → 模块 9 态与 Core SafeMode 视觉投影 → 资源对称卸载 → U3DS 零类型泄漏**的完整确定性证据链。

---

## 2. 公共证据来源表（Evidence Source Identity Table）

| EvidenceSourceId | 相对路径 / 资产标识 | Git Commit / SHA-256 | EnvironmentRole | EnvironmentLimits | CapturedBy / At |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`U3SRC-PPAUSE`** | `Assets/Runtime/Assembly-CSharp/Unturned/UI/Player/PlayerPauseUI.cs` | Commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`<br>SHA-256 `30AAD23ED3B9D81389A1F8AC0C05E36FBF5F8B90332369962DA37195634291FC` | Client / SP / P2P Client | 仅静态源码证据，不代表生产运行 | Gemini / 2026-08-24 |
| **`U3SRC-MENUCONF`**| `Assets/Runtime/Assembly-CSharp/Unturned/UI/Menu/Configuration/MenuConfigurationUI.cs` | Commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`<br>SHA-256 `9FA37C320B22B058361270A45EFA3E123C64C3C976C083679327AC5B5ECAD785` | Client / SP / P2P Client | 仅静态源码证据，不代表生产运行 | Gemini / 2026-08-24 |
| **`U3SRC-MENUCTRL`**| `Assets/Runtime/Assembly-CSharp/Unturned/UI/Menu/Configuration/MenuConfigurationControlsUI.cs` | Commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`<br>SHA-256 `4946F08B8AEC435E62CAAE4B2277638A4A23882E8994D6C7B98963D347CFB29D` | Client / SP / P2P Client | 仅静态源码证据，不代表生产运行 | Gemini / 2026-08-24 |
| **`U3SRC-PDINV`**   | `Assets/Runtime/Assembly-CSharp/Unturned/UI/Player/PlayerDashboardInventoryUI.cs` | Commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`<br>SHA-256 `593EDCB1AF5E19E548353BA3A2F97EA3351C1921DD348747AF99ABB95179566C` | Client / SP / P2P Client | 仅静态源码证据，不代表生产运行 | Gemini / 2026-08-24 |

---

## 3. 统一设置中心入口、生命周期与焦点捕获（Settings Entry & Focus Seam）

### 3.1 原生菜单挂载点与受控访问（对齐 N-03）
在 Unturned 中，设置与暂停菜单存在两个独立的场景上下文：

1. **游戏内暂停菜单（In-Game Pause Menu）**（`U3SRC-PPAUSE:11-100`）：
   * 顶级全屏容器为 `PlayerPauseUI.container`（静态私有字段），挂载于 `PlayerUI.container`。
   * **受控注入方案（对齐 N-03）**：通过受控的字段访问器（专用 Accessor）在 `optionsButton` 旁注入 `[更好的UN体验]` 原生样式 `SleekButtonIcon`，严禁使用反射扫描程序集。点击触发 `SettingsPresenter.OpenModal()`。
2. **主菜单选项界面（Main Menu Configuration）**（`U3SRC-MENUCONF:9-100`）：
   * 顶级全屏容器为 `MenuConfigurationUI.container`（静态私有字段），挂载于 `MenuUI.container`。
   * **受控注入方案**：在主菜单“选项”侧边栏中追加专用配置按钮，支持玩家在未进入服务器前配置本地偏好（`ClientLocal` / `ClientPreference`）。
3. **全局快捷键呼出（`F8` Hotkey）**：
   * 在客户端主更新循环（`PlayerUI.instance.Update`）中监听 `InputEx.GetKeyDown(KeyCode.F8)`，实现随时一键呼出/关闭设置模态弹窗。

### 3.2 输入焦点与按键绑定捕获 Seam（Focus & KeyBinding Seam）
* **焦点隔离机制**（`U3SRC-MENUCTRL:48-77`）：
  * 原生按键绑定通过 `ShouldGameIgnoreInput`（`IsRebindingKey || wasBindingThisFrame`）阻断游戏常规输入。
  * **前端实现**：当玩家在设置中心配置自定义快捷键时，控件进入 `Rebinding` 状态，临时挂起常规键盘快捷键捕获，并在捕获到有效 `KeyCode` 后提交变更并恢复输入焦点；文本框输入焦点激活时，全局 `F8` 与 `[R]` 旋转键自动静默，杜绝打字误触。

---

## 4. 构建期 Settings Facet 与运行期 Snapshot 消费管线（对齐 N-02、N-04）

### 4.1 静态 Schema 与动态值投影解耦（对齐 N-02）
前端设置外壳（`SleekFeatureSettingsModal`）严格采用 **静态 Schema（Settings Facet） + 动态值投影（`FeatureSettingsSnapshot`）** 模型，杜绝双写与多事实源混乱：

```
┌─────────────────────────────────────────────────────────────┐
│             构建期编译生成的静态 Settings Facet               │
│   (静态 Schema: Key, Type, Range, Default, Localization)    │
└──────────────────────────────┬──────────────────────────────┘
                               │ 1. 静态预构建 UI 控件骨架
┌──────────────────────────────▼──────────────────────────────┐
│                  SleekFeatureSettingsModal                  │
│       (动态渲染 Toggle, Slider, KeyBinding, Choice 控件行)    │
└──────────────────────────────▲──────────────────────────────┘
                               │ 2. 动态绑定当前值、权限与 Revision
┌──────────────────────────────┴──────────────────────────────┐
│          运行期不可变 FeatureSettingsSnapshot 快照            │
│   (动态值投影: EffectiveValues, Permissions, Revision)       │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 动态控件映射与交互防抖
* **控件映射规范**：
  * `SettingType.Toggle` $\to$ `ISleekToggle`
  * `SettingType.Slider` $\to$ `ISleekSlider`（附带实时数值标签）
  * `SettingType.KeyBinding` $\to$ `SleekButton`（显示 KeyCode 名称，点击进入捕获态）
  * `SettingType.Choice` $\to$ `SleekButtonState`（枚举项轮播选择）
* **防抖提交策略（Debouncing）**：
  * 高频拖动滑块或编辑数值时，前端 Presenter 本地即时平滑刷新 UI。
  * 仅在**鼠标松开（PointerUp）**或**输入静止 150ms 后**，向 `SettingsRuntime` 派发 `UpdateModuleConfigCommand` 原子事务。
  * 遵循 3 秒同 `RequestId` 重试、8 秒快照兜底（`0x0104`）规则，确保弱网下的最终一致性。

### 4.3 传输解耦下的网络设置表现（对齐 N-04）
* **LMN 不可用时不影响本地功能**：
  * `ClientPreference` / `ClientLocal`：完全正常交互与本地持久化。
  * `ServerAuthority`：控件置灰为只读状态，显示“未联网/当前服务器未支持”微型徽标与 Tooltip。在 `SCR-RT05-001` 正式解决前，禁止开放生产级网络设置写入。
  * `ServerPolicyWithClientPreference`：允许编辑本地偏好；无服务器策略时本地偏好即为有效值，不显示政策锁定图标。

---

## 5. 模块 9 态与 Core SafeMode 视觉投影矩阵（对齐 B-03 及措辞纠偏）

前端 Presenter 消费 `FeatureStatusView` 与 `CoreRuntimeStatusView`，严格映射为如下标准表现：

| 运行状态 | 控件交互状态 | 视觉徽标 (Badge) | 提示信息与行为 |
| :--- | :--- | :---: | :--- |
| **`Discovered`** | 置灰禁用 | `[未激活]` | 模块已发现但未进入加载队列。 |
| **`Starting`** | 置灰禁用 | `[启动中]` | 显示轻量旋转等待指示器。 |
| **`Running`** | 正常交互 | `[运行中] (绿)` | 功能完全正常，支持实时配置。 |
| **`Disabled`** | 置灰禁用 | `[已禁用] (灰)` | 模块已被配置或生命周期禁用。 |
| **`Incompatible`** | 置灰禁用 | `[不兼容] (黄)` | 显示版本或能力不兼容本地化说明。 |
| **`Isolating` / `Stopping`** | 只读过渡 | `[正在卸载]` | 临时置灰，等待生命周期回调完成。 |
| **`Isolated`** | 置灰禁用 | `[已隔离] (红)` | 显示 `DiagnosticId` 与故障信息；**立即从 UI 树中卸载该模块的所有 HUD/View 扩展**。 |
| **`Stopped`** | 置灰禁用 | `[已停止]` | 模块已退出运行。 |
| **`Core SafeMode`**<br>*(对齐 B-03 纠偏)* | **注销所有扩展** | **独立 Fallback 提示** | **卸载全部自定义功能 UI；若独立、最小、安全的 Fallback Adapter 可用，显示一次温和提示（带 DiagnosticId）；否则仅记入日志。不主动修改原版游戏与菜单；在 CLR/进程仍可安全继续的条件下尽可能恢复原版体验（生产候选仍需运行验证）。** |

---

## 6. 资源对称清理与代际失效机制

为杜绝内存泄漏、悬挂回调与 UI 残影，前端确立了如下对称清理规则：

1. **界面关闭对称清理**：
   * `PlayerDashboardInventoryUI.close()` $\to$ 立即调用 `stopDrag()`，隐藏并重置 `SleekInventoryFootprintLayer`。
   * `PlayerPauseUI.close()` $\to$ 关闭设置模态，取消所有正在等待的 150ms 防抖定时器。
2. **模块卸载/故障隔离清理**：
   * 当模块进入 `Stopped` 或 `Isolated` 时，Presenter 触发 `IClientUiFeatureComponent.OnUiDestroyed()`，从父级 Glazier 容器中移除所有生成的 UI 节点，清空对象池，注销所有事件订阅。
3. **代际失效（Generation Guard）**：
   * 所有异步定时器、原生投影回调与网络响应均捕获触发时的 `DragGeneration` 或 `ConnectionGeneration`。
   * 执行时若发现 Token 与当前活跃代际不一致，立即静默丢弃，杜绝跨会话脏写入。

---

## 7. U3DS / Headless 客户端类型风险与五重隔离方案（对齐 B-04）

### 7.1 客户端专属类型 Token 清单（Client-Only Type Tokens）
经分析 U3-SDK 与 Unturned 运行时，以下类型属于客户端专用类型：

| 类型所属命名空间 / 程序集 | 客户端专属类型 Token | U3DS 风险分类 |
| :--- | :--- | :---: |
| `SDG.Unturned` | `ISleekElement`, `ISleekButton`, `ISleekImage`, `ISleekLabel`, `ISleekScrollView`, `ISleekSlider`, `ISleekToggle`, `ISleekSprite`, `ISleekBox`, `SleekFullscreenBox`, `SleekButtonIcon`, `SleekItem`, `SleekItems`, `SleekSlot`, `SleekPlayer` | 高危（类型装载异常） |
| `SDG.Unturned` (UI 控制器) | `PlayerDashboardInventoryUI`, `PlayerDashboardUI`, `PlayerPauseUI`, `MenuPauseUI`, `MenuDashboardUI`, `MenuConfigurationUI`, `MenuConfigurationControlsUI`, `PlayerUI`, `MenuUI`, `Glazier` | 高危（空指针/缺失实例） |
| `UnityEngine` | `GUI`, `GUIUtility`, `Event`, `KeyCode`, `Sprite`, `Texture2D`, `Color`, `Screen` | 中危（缺失图形设备） |

### 7.2 五重强隔离设计义务与待验证门禁（对齐 B-04）

```
┌─────────────────────────────────────────────────────────────┐
│              Contracts & CoreShared (纯领域/无UI)            │
│   (Gate 1: Zero Sleek, Zero Glazier, Zero GUI Type-Token)   │
└──────────────────────────────▲──────────────────────────────┘
                               │ 单向依赖
┌──────────────────────────────┴──────────────────────────────┐
│                    ClientUi 内部适配模块                     │
│    (Gate 2: Internal Encapsulation & ClientUiAvailable)     │
└──────────────────────────────▲──────────────────────────────┘
                               │ 仅在 !isBatchMode && !Headless 装配
┌──────────────────────────────┴──────────────────────────────┐
│                GeneratedClientUiRegistry (显式注册表)        │
│   (Gate 3: Build-time Generated, 杜绝运行时反射扫描)         │
└─────────────────────────────────────────────────────────────┘
  │                                                           │
  ▼ (Gate 4: CI IL 可达性静态扫描)                              ▼ (Gate 5: U3DS 实际加载验收)
```

1. **Gate 1（源码闭包隔离）**：`Contracts` 与 `CoreShared` 源码中绝不包含任何上述 Client-Only Type Token。
2. **Gate 2（内部封装与装配门禁）**：`ClientUi` 模块设为 `internal`，装配前置判断 `ClientUiAvailable && !Application.isBatchMode && !Headless`。
3. **Gate 3（显式生成注册表）**：由构建期生成的 `GeneratedClientUiRegistry` 显式装配，彻底废除 `Assembly.GetTypes()` 扫描。
4. **Gate 4（CI IL 静态扫描）**：在单 DLL 编译后对 Core 程序集执行 IL 可达性扫描，断言对 UI Type Token 的引用数为 0（当前状态：`UNRESOLVED`，待 CI 建立）。
5. **Gate 5（U3DS 实际运行验收）**：在真实 U3DS 无图形环境下启动服务端并验证日志无任何类型加载异常（当前状态：`UNRESOLVED`，待后续发布门禁）。

---

## 8. Harmony 补丁点位、被拒 Hook 与测试义务

### 8.1 最小化设置中心 Harmony Patch 点位

| 目标类与方法 | 补丁类型 | 介入目的 | EvidenceSourceId | EvidenceClass |
| :--- | :---: | :--- | :---: | :---: |
| `PlayerPauseUI.open` | `Postfix` | 游戏内暂停菜单打开时，确保设置唤出按钮已挂载并更新红点状态。 | `U3SRC-PPAUSE` | `SOURCE_CONFIRMED` |
| `PlayerPauseUI.close` | `Prefix` | 游戏内暂停菜单关闭时，若设置模态处于打开状态，同步关闭设置模态。 | `U3SRC-PPAUSE` | `SOURCE_CONFIRMED` |
| `MenuConfigurationUI.open` | `Postfix` | 主菜单配置界面打开时，挂载主菜单设置扩展按钮。 | `U3SRC-MENUCONF` | `SOURCE_CONFIRMED` |

### 8.2 被拒绝的 Hook 方案（Rejected Alternatives）
1. **拒绝拦截 `Glazier.Get()`**：底层图形驱动工厂，拦截风险极高。
2. **拒绝在 `PlayerInventory.Awake` 中注入 UI**：服务端亦会实例化，引发服务端崩溃。
3. **拒绝全局 `MonoBehaviour.Update` 挂钩**：产生不必要的每帧开销。

---

## 9. 固定源码调用链表（Fixed-Source Call-Chain Matrix）

| 固定源码位置 | 类型与成员 | 完整方法签名 | 调用方 (Caller) | 被调用方 (Callee) | EvidenceSourceId | EvidenceClass |
| :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| `PlayerPauseUI.cs:45-92` | `PlayerPauseUI.open` | `public static void open()` | `PlayerUI.instance.Update` | `container.AnimateIntoView` | `U3SRC-PPAUSE` | `SOURCE_CONFIRMED` |
| `PlayerPauseUI.cs:94-100` | `PlayerPauseUI.close` | `public static void close()` | `PlayerUI.instance.Update` | `container.AnimateOutOfView` | `U3SRC-PPAUSE` | `SOURCE_CONFIRMED` |
| `MenuConfigurationUI.cs:21-31` | `MenuConfigurationUI.open` | `public static void open()` | `MenuPauseUI.onClickedOptions` | `container.AnimateIntoView` | `U3SRC-MENUCONF` | `SOURCE_CONFIRMED` |
| `MenuConfigurationControlsUI.cs:71-77` | `MenuConfigurationControlsUI.ShouldGameIgnoreInput` | `public static bool ShouldGameIgnoreInput { get; }` | 原生输入系统 | 检查 `IsRebindingKey` | `U3SRC-MENUCTRL` | `SOURCE_CONFIRMED` |
| `PlayerDashboardInventoryUI.cs:181-198` | `PlayerDashboardInventoryUI.close` | `public static void close()` | `PlayerDashboardUI.close` | `stopDrag`, `AnimateOutOfView` | `U3SRC-PDINV` | `SOURCE_CONFIRMED` |

---

## 10. 契约充分性审查与后续验证义务（Verification Obligations）

1. **共享契约充分性**：
   * `GPT-RT-01` 冻结契约完全覆盖需求，无需发起 Shared Contract Change Request。
2. **后续验证义务（Verification Obligations / `UNRESOLVED` 门禁）**：
   * `VO-RT03-01`：在构建后执行单 DLL 的 IL 可达性扫描工具，断言 Core/Contracts 命名空间对 `SDG.Unturned.ISleek*` 及 `UnityEngine.GUI*` 的引用严格为 0（当前分类：`UNRESOLVED`）。
   * `VO-RT03-02`：在 U3DS 专用服务端上装载构建产物，验证无图形环境下不发生任何 `TypeLoadException` 或 `MissingMethodException`（当前分类：`UNRESOLVED`）。
   * `VO-RT03-03`：在 Core 触发 SafeMode 场景下进行实际运行测试，验证自定义 UI 卸载与独立 fallback 提示显示，原版游戏与暂停菜单正常交互（当前分类：`UNRESOLVED`）。
3. **证据边界声明（SourceSet 迁移对齐）**：
   * 迁移至 `BUE-SS-20260824-02`；将 U3DS BepInEx `5.4.23.5`（SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70`）纳入冻结 IL reference。
   * 明确 U3DS 动态引导日志显示 `0 plugins to load`，仅证明服务端 Preloader/Chainloader 基础环境就绪，**绝不替代单 DLL IL 静态扫描与真实 BUE 插件加载验证**（`VO-RT03-01`～`03` 仍严格保留为 `UNRESOLVED` 门禁）。
4. **调研结论**：
   * **RT-03 前端调研已全量吸收 B-03、B-04、N-01～N-04 及 C-01 审查要求并完成 SourceSet 迁移，票据状态维持 `ready-for-human`**。

---

*报告完。作者: Gemini*


