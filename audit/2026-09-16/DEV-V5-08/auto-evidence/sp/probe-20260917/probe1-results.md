# 探针 #1 结果（dc948b54，包 UMM-诊断包_20260917_000914，2026-09-17）

identity=DC948B54… ✓（探针构建绑定行）。

## 事实

1. `分区重建 level=0 xp=3689 buttonEnabled=True target=1`（×3 页开）——**F1 假设①（按钮禁用）与假设③（静默门）排除**：按钮已挂处理器。
2. 两次点击 → 两次 `升级执行异常（拒绝，账零改）: Object reference`——**点击送达、ExecuteUpgrade 内抛 NRE**（假设②也排除——Glazier 直挂 OnClicked 真机可用）。
3. 双击 R → `技能窗判定异常` **全栈**：
   ```
   System.NullReferenceException
     at LirSkillEngineHooks.CharacterKeyOfPlayer (Player player) [0x00015]
     at LirSkillEngineHooks.CharacterKeyOf (UInt64 steamId) [0x00016]
     at LirSkillEngineHooks.TryBeginRepackWindow (...)
   ```
4. 入口探针行（升级请求进入/OnClicked 到达/角色判定）未出现=**LogDiagnostic 全局节流吞并**（「此前抑制 6 条重复诊断」佐证）——非代码缺失；教训=诊断探针须用不节流通道。

## 结论

F1 与 F2 **同一根因**：`CharacterKeyOfPlayer` 真机 NRE（G07-4「引擎身份真值」具名缺口以缺陷形态兑现）。该方法四步解引用（channel/owner/playerID/characterName）在现码均有判空，静态不可辨哪环为 null（IL 偏移分析到界）→ 探针 #2=e5ef12ac：逐环判空+LogError（不节流）打点，一次真机操作定环。

## 探针 #2 部署

SP 端 = e5ef12ac91e0f92226094126186221cae94757c226f4670aef1e8264ef5300c4（非候选）。
