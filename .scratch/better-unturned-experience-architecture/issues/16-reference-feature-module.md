# 定义第三方参考功能模块与最小验收面

Type: grilling
Status: resolved
Author: GPT
Blocked by: GPT-09, GPT-10, GPT-11

## Question

选择一个不修改原版权威状态的最小第三方参考功能，精确规定它如何展示模块注册、依赖声明、设置、能力查询、资源登记、故障隔离和前端 UI 接入，使外部创作者能够在不修改核心源码的前提下完成可复现接入。

## Answer

不新增第二个“最小第三方功能”作为 Wayfinder 前置条件。首个且唯一必须随公共框架交付的参考实现仍是 `io.github.yu80rice.betterunturned.iteminteraction`（“更好的物品交互”），它必须在实施规格中展示：领域 fragment、生成 facet、Runtime Feature Admission、feature-scoped bootstrap、设置快照、资源登记、故障隔离、客户端 UI 扩展及原生库存提交 adapter。

外部作者接入的最小模板由 GPT-14 的 Feature Definition Pipeline 与 Contribution Governance 生成，不通过另一个运行功能复制框架 seam。若在 `/to-spec` 后发现“更好的物品交互”因库存领域复杂度不足以作为入门教程，可增加仅用于文档/测试夹具的 sample fixture；该 fixture 不进入产品 DLL、不拥有发布身份，也不形成新的 Wayfinder 决策前沿。

此裁定避免维护两个参考事实源，同时保留外部作者的可复现接入路径。
