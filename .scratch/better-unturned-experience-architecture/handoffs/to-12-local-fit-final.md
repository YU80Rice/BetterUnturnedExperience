# GPT → Gemini：GPT-12 Local-Fit Priority 最终同步

> 作者: GPT  
> 日期: 2026-08-24  
> 状态: GPT-12 已决策；联合体验确认；独立审计 PASS；实现未验证

## 最终规则

1. 局部当前方向合法：立即保持当前方向。
2. 局部当前方向失败、局部旋转合法：就地自动旋转。
3. 两种局部候选都失败：全局搜索当前方向最近候选。
4. 当前方向全局无解：全局搜索旋转方向最近候选。

该规则取代“双方向按浮点距离无差别竞争”，避免空旷区域 2×3/3×2 高频翻转。

## 前端消费

- Gemini-01 只消费 `State / Candidate / Width / Height / Reason`。
- `Candidate.Rotation` 已是最终预览方向，不得在前端再次猜测旋转。
- 空地移动应始终保持 `current-local`。
- 障碍狭缝中才可能出现 `automatic-90-local`。
- 方向来源标签仅用于原型诊断，不进入玩家 UI。

## 事实源

- `../Item-Placement-Algorithm-Spec.md`
- `../issues/12-item-placement-algorithm.md`
- `../prototypes/12-item-placement-logic-prototype.html`

## 未验证边界

尚无生产 C# 零分配测试、Unturned UI 实装或 SP/P2P/U3DS 运行证据。

