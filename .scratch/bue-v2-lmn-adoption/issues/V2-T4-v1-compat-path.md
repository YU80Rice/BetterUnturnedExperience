# V1 兼容路径设计

Type: wayfinder:grilling
Status: open
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: V2-T2-scr-gpt18-001-approval（注册骨架冻结后才有兼容面）

## Question

V1 旧插件"兼容接收/运行"的具体机制是什么：数字频道如何继续工作、何时退出？

## 决策要点

1. 兼容接收/运行的具体含义：旧插件用数字频道与 BUE 网络模块通信的映射/桥接方式。
2. 兼容边界：BUE 只为既有 V1 消费方保留兼容能力，不再提供新 V1 注册入口（CONTEXT.md L65-67）。
3. 退出条件：V1 兼容过渡期在主要官方功能和主要生态插件完成 V2 迁移后结束（CONTEXT.md L101-103），由迁移覆盖率决定——本票要定"如何测量覆盖率"。
4. 兼容实现放哪：BueNetworkApi 内？独立兼容层？故障时行为？

## 答案

（resolved 时记录机制 + 退出度量）
