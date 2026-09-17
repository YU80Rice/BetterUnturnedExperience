# SP 轮1 结果与定性（2026-09-16，候选 bb33c2dd，包 UMM-诊断包_20260916_232607）

## 通过腿

- S1 身份：assembly-identity=BB33C2DD… 绑定 ✓；6×accepted=True。
- S2/S3 整理：背包/全身/容器（世界箱+后备箱）整理有效、容器不波及身上 ✓（G02-1/G03-1 部分销账）。
- S4 面板：三模式档已退役 ✓。
- 用户裁定：整理排序观感与预想不符=**不阻断，具名递延后改**（记 F5）。

## 缺陷（阻断候选）

- **F1 升级不可用**：分区按钮可见（截图：等级 0 阶梯+「升级到 1 级 · 花费 125 经验」，余额 3689 充足），点击无任何日志痕迹。静态可分三假设：①按钮 Enabled=false 未挂处理器（xp 读数异常）②Glazier `OnClicked +=` 直挂真机不触发（LIT 真机验证用反射 AddEventHandler——07 直挂形状=G07-1 未验缺口）③HandleSkillUpgradeRequest 静默门。→ 探针 dc948b54 区分。
- **F2 技能窗判定 NRE×7**：双击 R 每次触发「技能窗判定异常（本击不拦截，闸门层仍守）: Object reference」——fail-open 兜住（双击仍成交），但 **0 级合并窗从未武装**=US15/16 核心链真机不成立。静态排查：窗口路径唯一未防护接触=`CharacterKeyOfPlayer` 的 `player.channel.owner`（其余全有 try/catch 或纯数据）。→ 探针补全栈定位。
- **F3 HUD 后备数更新不实时**：拾取弹药箱后 M 不变，开关背包/换弹才更新。设计频度=updateInfo 触发点；spec 未承诺即时。拟定性=具名观察递延（用户如裁定要即时则进修复轮）。
- **F4 分区清节点异常**：仅 PluginStopping 拆除时出现（fail-soft 已兜住、注销成功）。拟定性=关机会话拆除噪声，具名观察，不阻断。
- **F5 整理排序观感**：用户当场裁定不阻断递延。
- **F6 分区无原版技能图标**：spec 未承诺图标（票面只要求「战斗区下方追加分区」）。外观项具名递延。

## 探针（非候选，dc948b54…9bbd，仅 SP 端）

4 处 [V508-PROBE]：分区重建 level/xp/buttonEnabled/target；OnClicked 到达；升级请求入口全门+角色判定+决定；TryBeginRepackWindow catch 改全栈。诊断毕撤除。
