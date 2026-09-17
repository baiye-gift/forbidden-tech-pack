# 物质重构器设计规格

日期：2026-09-17  
阶段：Forbidden Technology Pack 第二阶段 / 原质场与维度耦合  
稳定 ID：`BaiyeMatterReconstructor`

## 1. 设计目标

物质重构器是第二阶段第一台正式建筑，用来验证“原质场与维度耦合”体系可以在不破坏第一阶段闭环的前提下扩展出新的物质工程能力。

它不是第二台物质编译器。

- 物质编译器：`原质 → 已解析目标材料`
- 物质重构器：`现实基质 + 少量原质 → 已解析目标材料`

物质重构器的核心设定是：**原质用来撬动现实。**

原质不并入最终产物质量。它被消耗来暂时削弱现实基质当前的结构锚定，让设备能够把这部分已经存在的质量重新写成目标结构。

基础质量关系固定为：

`1000 kg 现实基质 + X kg 原质 → 1000 kg 目标材料`

其中 X 是重构成本。无论平衡预设如何变化，目标产物质量都不得超过现实基质质量。

## 2. 选择的配方模型

考虑过三种方案：

1. **固定材料对白名单**：最可控，但每增加材料或 DLC 都需要维护大量人工映射，容易把建筑做成“另一台配方机”。
2. **所有已解析固体任意互转**：实现和理解最简单，但会直接架空物质编译器，也容易形成无差别资源转换。
3. **层级基质重构**：目标材料必须已解析，同时要求一个与目标层级匹配的现实基质；原质承担结构改写成本。

采用方案 3。

它保留三条明确差异：

- 物质编译器不需要现实基质，但原质成本高；
- 物质重构器需要大量现实基质，但原质成本低；
- 玩家仍需要先获得并分析目标材料，不能通过重构器跳过探索和解锁。

## 3. 重构层级

沿用现有 `MaterialTier` 与 `ElementCatalogAdapter` 分类，不另外建立第二套材料分级系统。

首版采用以下层级关系，每批现实基质固定为 1000 kg：

| 目标层级 | 允许的现实基质层级 | 基础原质成本 | 结果 |
| --- | --- | ---: | --- |
| Common | Common | 50 kg | 1000 kg 目标材料 |
| OreOrOrganic | Common | 75 kg | 1000 kg 目标材料 |
| Industrial | OreOrOrganic | 100 kg | 1000 kg 目标材料 |
| Rare | Industrial | 200 kg | 1000 kg 目标材料 |
| Endgame | Rare | 300 kg | 1000 kg 目标材料 |

说明：

- 这是“现实复杂度阶梯”，不是化学反应表。普通矿物、矿石、有机物、工业材料、稀有材料只是现实结构复杂度的工程分类。
- 现实基质提供全部最终质量；原质只负责撬开和重写结构。
- 原质成本应用现有 `CostMultiplier`，但必须把配置 UI 文案从“编译成本倍率”调整为覆盖编译与重构的“物质工程成本倍率”。
- `Balanced / Strong / Extreme / Custom` 继续通过现有 `PackOptions` 解析，不新增第二套平衡预设。
- `AllowIndustrial / AllowRare / AllowEndgame` 继续决定相应目标层级是否可生成重构配方。
- 每个重构批次加工时间固定为 120 s。首版不按目标层级改变加工时间，先把平衡变量控制在原质成本上。

## 4. 基质层级 Tag 机制

为了做到“一条目标配方可以接受同层级的任意实体基质”，又避免为每个目标 × 每个基质元素生成组合配方，首版引入只服务于物质重构器的稳定 Tag：

- `BaiyeReconstructorSubstrateCommon`
- `BaiyeReconstructorSubstrateOreOrOrganic`
- `BaiyeReconstructorSubstrateIndustrial`
- `BaiyeReconstructorSubstrateRare`

规则：

1. `ElementCatalogAdapter.Build()` 完成并得到当前活动元素规则后，游戏侧做一次基质 Tag 注册；
2. 对每个活动、可储存的实体固体元素，根据其现有 `MaterialRule.Tier`，把对应基质 Tag 加到该元素生成的实体 prefab；
3. 原质永远不获得这些基质 Tag；
4. DLC 不活动或被材料分类排除的元素不获得 Tag；
5. Tag 注册只在元素目录建立阶段执行一次，不做逐帧扫描；
6. 不修改元素 ID、物态、materialCategory 或原有 oreTags，只附加本 Mod 自己的 KPrefabID Tag；
7. 如果某个元素无法安全获得 prefab/KPrefabID，则记录明确警告并把该元素排除出重构基质，不静默把它映射到错误层级。

`ComplexRecipe` 的现实基质 ingredient 直接使用上述层级 Tag，因此每个已分析目标只需要一条重构配方，不产生目标 × 基质元素的组合爆炸。

## 5. 配方生成与解锁规则

### 5.1 目标材料

只有同时满足以下条件的元素才能成为目标：

- 已存在于当前 `ElementCatalogAdapter.Rules`；
- 当前 DLC / 内容模式中有效；
- 对应材料层级未被配置禁用；
- 已经通过物质分析仪记录结构模板；
- 不是原质本身；
- 不是被 `MaterialClassifier` 排除的特殊对象、食物、种子、生物、任务物品等。

### 5.2 现实基质

现实基质不是指定的单一元素，而是与目标层级对应的材料层级 Tag。

例如 Industrial 目标需要 1000 kg 带 `BaiyeReconstructorSubstrateOreOrOrganic` 的实体材料作为质量锚点。

不允许把原质本身作为现实基质。

如果玩家使用与目标本身相同的元素作为允许层级基质，结果仍会消耗原质，因此只会造成资源浪费，不形成增殖循环；首版不为这种无意义操作增加特殊禁止逻辑。

### 5.3 动态刷新

物质分析仪新解锁目标材料后，重构器需要刷新可用配方。

规则与现有物质编译器一致：

- 允许刷新空闲设备的配方表；
- 当前工作单已经开始时，不允许替换当前 recipe 或清空 build storage；
- 保存/载入后根据持久化的分析结果重新生成配方；
- 旧存档中已开始的合法工作单必须能够继续完成。

### 5.4 温度语义

重构不能成为免费的冷热删除器。

- 结果配方必须使用游戏原生“按输入平均温度”语义，不允许把产物无条件重置到 20°C 或其它固定温度；
- 原质参与平均温度计算可以接受，但其质量远小于现实基质，不改变“现实基质承担主要热状态”的原则；
- 建筑自身仍额外产生固定过程热；
- 首版不自行实现跨材料比热严格能量补偿，但禁止任何手写的产物固定温度逻辑。

## 6. 建筑参数

首版固定参数：

- 尺寸：4 × 4
- 建造位置：地面
- 建造时间：180 s
- 建造材料：
  - 600 kg `RefinedMetal`
  - 200 kg `Ceramic`
  - 100 kg `Glass`
- 基础耗电：4800 W × `PowerMultiplier`
- 基础发热：24 kDTU/s × `HeatMultiplier`
- 装饰：负面 TIER1
- 噪音：`NOISE_POLLUTION.NOISY.TIER5`
- 建造菜单：`Refining`
- KAnim：`baiye_matter_reconstructor_kanim`

这台建筑不需要冷却管道。热管理压力通过建筑自身持续发热体现，避免第一台二阶段建筑就重复物质编译器的液冷系统。

## 7. 输入、输出与物流

### 7.1 输入

需要两类固体输入：

- 现实基质；
- 原质。

首版使用同一个固体运输输入口进入 Fabricator 输入存储，不为两种材料建立两条独立运输轨道。

同时支持复制人手动供料。

输入存储容量：2800 kg，可容纳两个最高成本 Endgame 批次所需的 `2 × (1000 kg 基质 + 300 kg 原质)`，并保留少量缓冲。

### 7.2 输出

输出为目标实体材料。

- 输出存储容量：1100 kg；
- 支持复制人取出；
- 支持固体运输轨道自动输出；
- 输出被堵塞时停止下一批，不允许吞掉已完成产物或继续无限堆积。

### 7.3 端口

- 1 个固体运输输入口；
- 1 个固体运输输出口；
- 1 个电力输入；
- 1 个自动化输入；
- 首版不增加自动化输出。

自动化输入沿用 `LogicOperationalController.PORT_ID`：绿色运行，红色停机。

## 8. 原质干扰行为

物质重构器必须挂载 `ForbiddenTechDevice`。

受到原质干扰时：

- 通过统一 `Operational` requirement 暂停；
- 当前工作进度保留；
- `inStorage`、`buildStorage`、`outStorage` 不被清空；
- 当前配方不被替换；
- 不发生质量损失；
- 干扰解除后恢复原工作单。

不为物质重构器单独 Patch Fabricator 工作流程。

## 9. 研究与注册

物质重构器属于第二阶段研究：

`BaiyeForbiddenProtoFieldEngineering`

中文研究名：**原质场工程**。

研究关系：

`禁忌物质工程 → 原质场工程`

研究成本固定为：

- `basic`: 200
- `advanced`: 120

研究节点放在现有 `BaiyeForbiddenMatterEngineering` 右侧，并继续使用避让已有节点的布局算法。

首版物质重构器实现后：

- `RegistrationPlan.Phase2BuildingIds` 继续表达“配置允许注册的第二阶段稳定 ID”，不承担判断代码是否已经实现的职责；
- 游戏侧研究和建造菜单注册必须将 `plan.Phase2BuildingIds` 与“当前已实现二阶段建筑清单”求交集；
- 首版已实现清单只有 `BaiyeMatterReconstructor`；
- 尚未完成的熵流偏转器和物质湮灭堆不得因为已有稳定 ID 而提前出现在 UI；
- `ReconstructorEnabled=false` 时隐藏研究解锁中的该建筑和建造菜单入口，但不得删除存档中已有建筑实例。

## 10. 配置

继续使用唯一的 `ForbiddenTechOptions` 页面。

物质重构器相关配置首版只包含：

- `ReconstructorEnabled` 独立开关；
- 复用全局 `PowerMultiplier`；
- 复用全局 `HeatMultiplier`；
- 复用 `CostMultiplier` 作为原质加工成本倍率；
- 复用 `AllowIndustrial / AllowRare / AllowEndgame` 控制目标层级。

不新增专门的尺寸、批量、速度或重构效率滑块，避免配置膨胀。

## 11. 存档数据

物质重构器首版不新增独立自定义存档对象。

需要依赖并验证：

- `ComplexFabricator` 工作单与队列；
- `Storage` 内容；
- 第一阶段已有的材料分析解锁状态；
- `ForbiddenTechDevice` 干扰状态可以在加载后由当前干扰源安全重建，而不是序列化失效引用。

不得序列化 `GameObject`、运行时组件引用或干扰源对象引用。

## 12. 安全移除

“准备安全移除”必须覆盖物质重构器。

处理顺序：

1. 停止设备继续开新工作单；
2. 释放现实基质、普通产物与其它普通材料，保持其原元素、质量、温度和疾病信息；
3. `inStorage` / `buildStorage` 中的原质继续走现有原质安全转换规则；
4. 移除自定义建筑实例；
5. 保存到新存档并重新载入验证后，才允许用户关闭 Mod。

不得因为配置关闭 `ReconstructorEnabled` 自动销毁已有建筑。

## 13. DLC 行为

- 配方只能引用当前 `ElementCatalogAdapter` 实际收录的活动元素；
- 基质 Tag 只附加给当前活动元素；
- DLC 未启用时，不得残留指向该 DLC 元素的配方；
- DLC 开启后，符合分类且已分析的材料可以进入重构列表；
- DLC 开关前后都必须测试保存/载入；
- 不把“代码里判断 DLC”视为已经完成 DLC 支持，仍需按照 `docs/test-matrix.md` 做实际游戏验证。

## 14. 动画与视觉状态

首版动画至少提供：

- `idle`
- `working_pre`
- `working_loop`
- `working_pst`
- `off`

工作视觉语义：

- 中央保留一个稳定的现实基质块；
- 原质场从外围对基质进行包覆和拉伸，而不是把基质粉碎；
- 工作结束时基质重新稳定为目标材料结构；
- 原质干扰时不增加另一套复杂动画状态，首版通过停机状态和状态面板表达。

建造菜单图标和 KAnim timeline 必须走 `assets/scml` + `assets/animation-manifest.json` + `build-assets.ps1` 生成链，禁止手改 `packaging/anim` 或 `dist`。

## 15. 状态文本

首版至少需要：

- 名称 / 描述 / 效果；
- 自动化端口文本；
- “原质干扰”停机状态；
- “无可用重构配方”；
- “等待现实基质”；
- “等待原质”；
- “输出阻塞”。

中文为第一语言，英文 PO 同步提供。

## 16. 测试要求

### 16.1 Core / Portable

至少覆盖：

- 各目标层级对应的基质层级；
- 1000 kg 基质始终只生成 1000 kg 目标材料；
- 原质成本按层级和 `CostMultiplier` 计算；
- 0、负数、NaN、Infinity 等非法质量被拒绝；
- 未分析目标不会生成配方；
- 被配置禁用的 Industrial / Rare / Endgame 不生成配方；
- DLC 不活动目标不生成配方；
- 基质 Tag 映射不会给原质或被排除元素加 Tag。

### 16.2 游戏 DLL / Contract

至少覆盖：

- BuildingDef 尺寸、功耗、发热、动画名；
- `ComplexFabricator` / Storage / SolidConduitConsumer / SolidConduitDispenser 接线；
- 结果使用原生输入平均温度语义，不固定重置温度；
- `ForbiddenTechDevice` 已挂载；
- 自动化端口；
- Phase 2 研究只解锁“配置允许且已经实现”的建筑；
- 动态配方刷新不会替换进行中的工作单；
- 安全移除包含三套 Fabricator 存储；
- 基质 Tag 注册只执行在元素目录建立阶段，不存在逐帧扫描。

### 16.3 游戏内

至少测试：

1. Common → Common；
2. Common → OreOrOrganic；
3. OreOrOrganic → Industrial；
4. Industrial → Rare；
5. Rare → Endgame；
6. 未分析目标不可用；
7. 原质不足暂停；
8. 现实基质不足暂停；
9. 输出轨道堵塞无物料损失；
10. 断电恢复；
11. 自动化停机恢复；
12. 原质干扰暂停和恢复；
13. 工作中保存/载入；
14. 半成品存储状态下拆除；
15. `ReconstructorEnabled=false` 后旧建筑仍可安全载入；
16. 高温和低温现实基质完成重构后，产物没有被错误重置到固定室温；
17. Base / Spaced Out / Frosty / Bionic / 全 DLC 组合下的研究、目标配方和基质 Tag 过滤。

## 17. 首版明确不做

- 任意两个固体元素直接两两生成配方；
- 为每个目标 × 每个基质元素生成组合配方；
- 液体或气体重构；
- 自动分析未知目标；
- 通过原质增加最终产物质量；
- 原质返还；
- 随机失败或随机产物；
- 单独的重构效率数值配置；
- 自定义冷却回路；
- 对普通建筑产生原质干扰。

这些约束保证物质重构器作为第二阶段第一台建筑，既建立“原质撬动现实”的玩法语言，又不会一次引入过多独立系统。