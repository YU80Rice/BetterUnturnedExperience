# 划定前端、后端与共享契约所有权

Type: grilling
Status: resolved
Author: GPT
Blocked by: GPT-01

## Question

前端、后端和共享契约分别覆盖什么范围，由谁维护？

## Answer

Gemini 负责玩家可见和可操作的游戏内 UI、HUD、设置外壳及交互反馈。GPT 负责规则、状态、持久化、权限、服务端逻辑、网络同步及共享契约。共享契约包含协议、DTO、事件名、错误码和版本兼容规则，前端可以提出字段需求，但不能绕过契约依赖后端内部实现。
