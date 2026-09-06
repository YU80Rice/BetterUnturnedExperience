# Wayfinder 第二次联合复审修正报告 - v0.23

## 【需求执行概述】

依据冻结 Unturned 源码修正 JCR-07 原生 forward 抓取偏移方向，并撤销过早的全局 PASS/GPT-17 关闭。

## 【源码溯源清单（Traceability Matrix）】

| 事实 | 落实位置 |
| --- | --- |
| 原生按键 `rot++` | `PlayerDashboardInventoryUI.cs:2425-2428` |
| 原生 pivot 方向 | 同文件 `updatePivot()` |
| forward/backward 连续坐标契约 | `Shared-Contract-Spec.md` §3.2 |
| 算法输入约束 | `Item-Placement-Algorithm-Spec.md` §2 |
| Gemini 返修 | `handoffs/to-JCR-07-native-forward-rotation-correction.md` |

## 【代码变更清单】

- 冻结左上原点、X 右、Y 下、连续闭区间 grab offset。
- 冻结 forward/native rot+1 为 `(H - gy, gx)`，backward/rot-1 为 `(gy, W - gx)`。
- 恢复 GPT-17 open 和 Wayfinder FAIL（剩余 1 阻断）。
- 未修改 Gemini-owned 文件。

## 【编译验证记录】

- 生产编译：N/A；当前无生产工程。
- 技术事实来自冻结 U3-SDK 源码静态核对，不表示本插件运行通过。

## 【子智能体审核记录】

第一轮判定：FAIL（技术修正 PASS，报告状态表达阻断）。

- 原生旋转公式、坐标约定、GPT-17 回退与证据边界均通过。
- 联合报告仍把首次 9 项历史清单写成当前阻断，并残留 JCR-09 待返修措辞。
- 本报告保留为失败记录；记录层修订进入下一时间戳报告。

## 【偏离与妥协说明】

无偏离。拒绝为了快速关闭 Wayfinder 而接受方向相反的抓取变换。

## 【测试建议】

覆盖四角、中心、四边中点与连续小数偏移的四次 forward 循环；四次旋转后必须回到原 offset 和 footprint。


