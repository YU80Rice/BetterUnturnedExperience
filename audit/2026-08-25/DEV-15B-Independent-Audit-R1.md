# GPT-DEV-15B 独立审计报告（R1）

## 一、审计结论

**判定：FAIL（存在 2 项阻断项，暂不可关闭 DEV-15B）**

本轮仅审计当前工作树，未修改生产源码。坐标换算、预览状态清理、代际投影过滤、构建和测试基础闭环均通过；但冻结的 JCR-07 Forward 抓取偏移变换及浮动物品图标抓取锚点尚未落实，属于 DEV-15B 的核心坐标/预览契约缺口。

## 二、审计范围与身份

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-15B-coordinate-preview-wiring.md`
- 功能规格：`.scratch/better-unturned-experience-architecture/spec-DEV-15-better-item-interaction.md`
- 坐标基线：`.scratch/better-unturned-experience-architecture/Item-Placement-Algorithm-Spec.md`、`RT-01-Shared-Contract-Baseline.md`
- 主要实现：`src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs`
- 测试：`tests/BetterUnturnedExperience.ClientUi.Tests/Dev15BTests.cs`
- 审计时间：2026-08-25（Asia/Shanghai）

## 三、验证记录

### 1. Release 编译

命令：

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /m /v:normal
```

结果：**通过，0 errors / 0 warnings**。

### 2. 七套测试

结果：**7/7 PASS**。

- Contracts：PASS
- Settings：PASS
- Placement：PASS
- ClientUi：`DEV-05/DEV-15A/DEV-15B ClientUi tests: PASS`
- Network：PASS
- Plugin：PASS
- Release：PASS

### 3. 静态隔离

- `ClientUi` C# 源码未发现 `UnityEngine`、`Glazier`、`Sleek`、`LMN`、`Unturned`、`BepInEx`、`Harmony`、`Assembly.GetTypes`、`PatchAll`、`System.Linq` 等禁用 token。
- Release `BetterUnturnedExperience.ClientUi.dll` 的 AssemblyRef 为 `mscorlib`、主程序集 `BetterUnturnedExperience`；未发现 UI/引擎/LMN 外部引用。
- 该结果只证明静态隔离，不证明真实 Unity/Glazier 运行。

当前关键 SHA-256：

```text
src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs
53AC361559705F84E93499CA26F280401FFA0BCEC4D4F28FD9E00674993CA04D
tests/BetterUnturnedExperience.ClientUi.Tests/Dev15BTests.cs
45C3D1EAF7D995B4D3B6A6EC90DEF255F7BCC3E21B816818D5EAFFC0D4447F15
src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll
A7DF9BFB1AA595DDA94353AAF1C573B4B2847249D56F4AEED45EB991B1B8A24C
src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll
A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16
```

## 四、阻断项

### B-15B-01：未实现冻结的 Forward 抓取偏移变换

**证据：**`InventoryPreviewWiring.cs:94-112` 仅按旋转奇偶交换 `width/height`，随后直接使用原始 `GrabOffsetX/Y` 计算中心；未执行冻结的：

```text
(gx, gy) -> (H - gy, gx)  // native rot + 1
```

也未在偏移变换后重新计算 `intendedItemCenterGrid`。`Dev15BTests.cs:41-53` 的旋转用例使用对称偏移 `(0.25, 0.25)`，无法暴露该缺陷，也没有四次 Forward 旋转恢复测试。

**影响：**非对称抓取点在自动旋转或原生 `[R]` 旋转后，Evaluator 接收的中心坐标会偏移，造成占据框/最终原生提交位置与玩家实际抓取点不一致，直接违反 JCR-07 与 DEV-15B 坐标验收条件。

**修复建议：**明确 `GrabOffset` 的输入语义；若输入是抓取时冻结的基准方向偏移，在 adapter 中按旋转次数执行确定性 Forward 变换（每次 `rot+1` 使用 `(H-gy,gx)`），再用当前 footprint center 重算 intended center；补充非对称四角、四次旋转闭环和自动旋转切换测试。

### B-15B-02：浮动物品图标未消费抓取锚点

**证据：**`InventoryPreviewWiring.cs:155-166` 的 `PreviewIcon` 只有 `ScreenX/ScreenY/Rotation`；`InventoryPreviewWiring.cs:209-212` 直接以 `input.PointerScreenX/Y` 作为图标位置。没有按冻结前端规则使用 `pointerGrid - grabOffsetInFootprint`（并在旋转时使用变换后的偏移）计算图标锚点。

**影响：**图标会把其原点固定在鼠标指针，而不是保持玩家在物品上的实际抓取位置；非中心抓取和旋转时会出现视觉跳动/错位。该项属于 DEV-15B 明确的“浮动物品图标”与 RT-02 坐标 Seam，不是 DEV-15C 投影职责。

**修复建议：**在不引入 UI/Unity 类型的前提下，把最终渲染所需的纯值锚点（或等价的 screen/grid 偏移）纳入 `PreviewIcon`，由 adapter 使用当前旋转后的 grab offset 计算；补充非中心抓取、UI Scale、滚动和旋转图标位置断言。

## 五、已通过项

- UI Scale、滚动、左上原点及 viewport 外隐藏的基础路径通过现有测试。
- 合法候选输出绿色框，尺寸/Rotation/Reason 直接消费 Evaluator 返回值；未在表现层重算候选。
- 非法候选输出红框并隐藏图标；Hidden 与 `PendingAuthoritativeProjection` 清理全部图元，不伪造拒绝/回滚。
- 当前代际由 `InventoryDragPresenter` 守卫；输出 generation 与输入不一致时隐藏，迟到 evaluator 投影不会污染当前预览。
- 预览热路径测试通过，当前替身 sink/evaluator 场景测得 10000 次 0 字节分配；这不等同于真实 Glazier 图元路径 0 GC 证明。
- 未越界实现 DEV-15C 投影 Relay、DEV-15D 设置/生命周期或真实 Unity Hook/网络逻辑。

## 六、非阻断建议

1. 增加实际 `PlacementCandidateEvaluator` 的端到端预览接线测试；当前 DEV-15B 测试主要使用 `FixedEvaluator`，因此主要证明 Presenter 分流，不证明真实 Evaluator 搜索路径与预览数据的组合。
2. 对 viewport 的 origin/clip 尺寸及坐标输入增加有限性、正尺寸防御测试；当前异常值大多会安全隐藏，但结构体自身未显式校验。
3. 明确 Presenter/adapter 只在 Unity 游戏线程调用的入口断言；线程安全不应通过锁把 UI 热路径变成跨线程调用。

## 七、最终门禁

在 B-15B-01、B-15B-02 修复并重新执行 Release 构建、7 套测试及独立审计前，**不得将 DEV-15B 标记为 resolved，也不得宣称 Better Item Interaction 已具备真实客户端功能或三环境资格**。


