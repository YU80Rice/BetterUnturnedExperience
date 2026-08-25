# DEV-15A Native Drag Adapter 实施报告

## 需求执行概述

按 DEV-15 正式规格实现纯 C# Native Drag Adapter Seam：普通网格合法候选沿原生路径提交，非法候选停止增强拖拽，特殊页面和当前代际的原生分支保持放行，陈旧代际 fail-closed。

## 源码溯源清单

| 需求点 | 落实位置 |
| --- | --- |
| 普通网格判定 | `src/BetterUnturnedExperience.ClientUi/NativeInventoryInteractionAdapter.cs` `IsOrdinaryGrid` |
| 当前代际守卫 | `NativeInventoryInteractionAdapter.HandleRelease` |
| 合法提交顺序 | `HandleRelease`: `SendDragItem` → `StopDrag` |
| 非法候选保留原物 | `HandleRelease`: 非 `Candidate` 仅 `StopDrag` |
| 特殊分支原生放行 | `HandleRelease`: 非普通网格返回 `PassThrough` |
| 陈旧特殊页零动作 | `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs` stale-special case |
| 合法/非法/同格测试 | 同测试项目 `RunNativeDragAdapterTests` |

## 代码变更清单

- 新增 `NativeInventoryInteractionAdapter.cs` 及纯值输入/输出/原生动作端口；
- 将 ClientUi 项目纳入可编译解决方案并登记该源文件；
- 扩展 ClientUi 测试：普通网格、地面拖入、装备页、AREA、非法候选、同格取消、陈旧普通/特殊页和调用顺序；
- 更新 DEV-15A 工单为 `ready-for-human`，等待 Gemini 消费复核。

## 编译验证记录

命令：

```text
dotnet build BetterUnturnedExperience.sln -c Release --no-restore
```

结果：`0 errors / 0 warnings`。

测试：Release 下 7 个测试项目全部 PASS。

## 关键产物哈希

| 产物 | SHA-256 |
| --- | --- |
| `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll` | `473F4ABB1A924B6B78DD9ABAEFABEBF670EB0D73A3CC5CA09139404C1D355B46` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16` |

## TDD 与独立审核记录

1. Red：测试捕获合法候选先 `StopDrag`、陈旧特殊页 pass-through、同格非法候选 pass-through 三类边界错误；
2. Green：修正为代际先守卫、合法 `SendDragItem → StopDrag`、非法状态先于同格判断；
3. GPT 独立审计 R4：PASS，`GPT-DEV-15A-Independent-Audit-R4.md`（绑定当前提交 `0a44114380941aa758f8e339746f8071a9110b10`）；
4. Standards 轴复核：PASS，未发现硬性标准违规；
5. Spec 轴复核：前两轮发现的上述边界项已修复，最终代码与 DEV-15A 规格一致。

## 偏离与妥协说明

无生产功能越界。普通网格判定继续使用 RT-02 已冻结公式 `page >= SLOTS && page != AREA`；未知页策略不在本票擅自扩展。

## 证据边界与下一步

本票没有真实 Unity/Unturned Hook、SP、SteamP2PFriends 或 U3DS 运行证据；不宣称 Better Item Interaction 功能完成或三环境通过。下一步交 Gemini 进行前端消费复核，之后才可关闭 DEV-15A 并开始 DEV-15B。
