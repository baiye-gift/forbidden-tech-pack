# 第二阶段开发进度跟踪

> **用途：这是第二阶段开发的实时断点文档。** 发生网络中断、会话丢失或换人接手时，以本文件 + 当前 Git HEAD + 测试结果为准，不从头重跑已经完成的开发任务。

最后更新：2026-09-18  
当前分支：`feature/baiye-20260917-phase2-protomatter-field`

## 1. 当前阶段结论

第二阶段主题：**原质场与维度耦合**。

当前开发重点：**物质重构器（Matter Reconstructor）**。

物质重构器不是从头开发状态。主体代码已经存在，当前已经完成 Core 规则、基质 Tag、配方、BuildingConfig、运行组件、研究注册、菜单注册、本地化、安全移除、SCML/图标源及资源恢复链。

当前真正的断点是：

**Task 7：用户本机 ONI 完整集成验证 + 游戏内验证。**

不要重新执行 Task 1–5。

---

## 2. 关键设计文档

- 第二阶段总体开发文档：`docs/phase-2-protomatter-field-development.md`
- 长期开发约束：`docs/phase-2-development-guide.md`
- 物质重构器设计规格：`docs/superpowers/specs/2026-09-17-matter-reconstructor-design.md`
- 物质重构器实施计划：`docs/superpowers/plans/2026-09-17-matter-reconstructor.md`
- 测试矩阵：`docs/test-matrix.md`

核心设定已经确认：

`1000 kg 现实基质 + X kg 原质 -> 1000 kg 目标材料`

原质的作用是**撬动现实、削弱现实基质当前的结构锚定并允许重新写入目标结构**，原质本身不计入最终产物质量。

---

## 3. 当前任务状态

| 任务 | 当前状态 | 已实现内容 | 仍需验证/处理 |
| --- | --- | --- | --- |
| Phase 2 共用原质干扰框架 | `LOCAL_VERIFIED` | `ForbiddenTechDevice`、事件驱动干扰管理、一阶段分析仪/粉碎机/编译器接入 | 后续仍需实际游戏里验证干扰表现 |
| Task 1 重构 Core Policy | `PORTABLE_VERIFIED` | 层级映射、1000 kg 基质/产物质量规则、原质成本、倍率验证 | 随本机 All 再验证一次 |
| Task 2 重构配方与已分析目标过滤 | `PORTABLE_VERIFIED` | `ReconstructorRecipes`、分析解锁过滤、原质独立消耗 | 实际游戏配方列表、工作单刷新 |
| Task 3 现实基质层级 Tag | `PORTABLE_VERIFIED` | Common / OreOrOrganic / Industrial / Rare 稳定 Tag，复用现有材料分类 | 实际游戏元素 prefab Tag 与输送行为 |
| Task 4 Matter Reconstructor 建筑与运行组件 | `PORTABLE_VERIFIED` | 4×4 BuildingConfig、4800 W、24 kDTU/s、输入/输出存储、固体轨道、电力、自动化、`ForbiddenTechDevice` | 用户本机 ONI DLL 编译 + 游戏内运行 |
| Task 5 注册、二阶段研究、本地化、安全移除 | `PORTABLE_VERIFIED` | `BaiyeForbiddenProtoFieldEngineering`、菜单、本地化、配置标签、安全移除接入 | 本机 runtime tests + 游戏研究树/安全移除 |
| Task 6 KAnim / 图标 / 资源链 | `PORTABLE_VERIFIED` | SCML、manifest、body/UI sprite Base64 源、自动恢复脚本、build-assets 映射、资源契约 | 本机 `build-assets.ps1` 实际 KAnim 编译 |
| Task 7 完整集成验证 | `IN_PROGRESS` | GitHub Portable CI 已通过 | 本机 All、build、安装、实际游戏测试、更新 test matrix |
| 熵流偏转器 | `NOT_STARTED` | 已有稳定 ID / 配置规划 | 等物质重构器完成游戏验证后再开始 |
| 物质湮灭堆 | `NOT_STARTED` | 已有稳定 ID / 原质干扰系统设计 | 等前两台建筑稳定后开发 |

---

## 4. 已确认的验证证据

### 4.1 共用原质干扰框架

用户本机曾在接入 `ForbiddenTechDevice` / 原质干扰管理器后执行：

```text
Wrote ...\ForbiddenTechnologyPack.dll
Built package: ...\dist\ForbiddenTechnologyPack
TOTAL: 206 passed
```

该次验证发生在物质重构器主体开发之前，因此它证明的是**Phase 2 共用框架 + 一阶段三台建筑干扰接入**可以在当前 ONI DLL 上编译和通过当时的完整测试，不能用它代替物质重构器的最终验证。

### 4.2 物质重构器 Portable 验证

在资源恢复链修复后，GitHub Actions `Feature verification` 的 `core-and-asset-contracts` job 已通过。

验证覆盖：

- Core tests；
- 资源源文件契约；
- 重构器资产契约；
- Phase 2 options / registration 契约；
- 原质干扰源码契约；
- 重构基质 Tag 契约；
- 重构配方契约；
- 重构器 BuildingConfig 契约；
- 重构器集成契约。

**这不等于本机 ONI DLL 编译通过，也不等于游戏内可用。**

---

## 5. 重要提交锚点

以下提交用于中断后快速判断进度，不要求逐个回放：

- `288eb3dd` — `feat(phase2): connect compiler to proto-matter interference`
  - Phase 2 共用原质干扰框架完成一阶段三台建筑接入的关键锚点。
- `5d78b96c` — `docs(phase2): tighten matter reconstructor contract`
  - 物质重构器最终设计规格确认后的锚点。
- `5d78b96c..3b60f9e6`
  - 物质重构器主体实现区间，共包含 Core、配方、Tag、BuildingConfig、研究、本地化、安全移除、SCML/图标等开发。
- `3b60f9e6` — `chore(assets): stage reconstructor UI sprite payload`
  - 网络中断前的资源阶段 HEAD。
- `fe18cbf4` — `fix(assets): materialize encoded sprites before tests`
  - 测试前自动把 `.png.b64` 无损恢复为 PNG 工作副本。
- `f18ffc79` — `fix(assets): restore reconstructor sources before KAnim build`
  - KAnim 构建前恢复资源，并补充重构器源目录映射。
- `91cda1ae` — `chore(assets): ignore restored reconstructor PNGs`
  - 恢复生成的 PNG 不污染 Git 工作区。
- `aab2c641` — `docs(assets): document encoded reconstructor sprite sources`
  - 资源恢复链说明；其对应 Portable CI 已通过。

后续只有在新验证或新代码产生时继续追加关键锚点，不需要把每个微小提交都写入这里。

---

## 6. 当前精确下一步

**从这里继续，不重新开发物质重构器。**

用户本地先同步当前分支，然后依次运行：

```powershell
git pull
git log -1 --oneline
git status

.\build-assets.ps1

.\test.ps1 -Suite All -GamePath "D:\steam\steamapps\common\OxygenNotIncluded"

.\build.ps1 -GamePath "D:\steam\steamapps\common\OxygenNotIncluded"
```

如果其中任何一步失败：

1. 在本文件“当前阻塞”中记录第一个真实失败；
2. 只修这个失败；
3. 修复后重新运行对应验证；
4. 不回退重做已完成任务。

如果三步全部通过：

1. 把 Task 1–6 中需要 ONI DLL 的部分升级为 `LOCAL_VERIFIED`；
2. 安装到本地 Mod；
3. 完全退出并重启 ONI；
4. 进入游戏验证物质重构器；
5. 将实际结果写入 `docs/test-matrix.md`；
6. 本文件同步升级对应状态。

---

## 7. 游戏内验证清单（物质重构器）

本机编译通过后，至少验证：

- [ ] 二阶段“原质场工程”研究节点可见，前置为第一阶段“禁忌物质工程”。
- [ ] 物质重构器在 Refining 菜单出现，图标不是问号。
- [ ] 建筑尺寸与落地位置正常，动画不沉入地板。
- [ ] 未分析材料不会直接出现在重构目标中。
- [ ] 新分析材料后，空闲重构器可以刷新合法配方。
- [ ] 已开始工作时刷新配方不会清空当前工作单或吞物料。
- [ ] 1000 kg 现实基质 + 对应原质只生成 1000 kg 目标材料。
- [ ] 原质不会并入产物质量。
- [ ] 手动供料可用。
- [ ] 固体运输输入可用。
- [ ] 固体运输输出可用。
- [ ] 输出阻塞时不会吞产物或无限继续生产。
- [ ] 断电后暂停，恢复供电继续。
- [ ] 自动化红信号暂停，绿信号恢复。
- [ ] 原质干扰期间暂停，解除后恢复，存储和队列不丢失。
- [ ] 工作中保存并重载后状态正确。
- [ ] 拆除时普通材料质量/温度/疾病信息按游戏规则保留。
- [ ] `ReconstructorEnabled=false` 后不再提供新建入口，但旧存档实例不被自动删除。
- [ ] 安全移除流程能处理重构器中的普通材料和原质。
- [ ] `Player.log` 无由本 Mod 引发的重复异常。

---

## 8. 当前阻塞

当前没有已确认的代码阻塞。

**待执行验证不是“已通过”。** 目前物质重构器仍缺：

- 本机 `build-assets.ps1`；
- 本机 `test.ps1 -Suite All`；
- 本机 `build.ps1`；
- 游戏内验证。

---

## 9. 后续强制记录规则

从本文件创建以后，开发过程执行以下规则：

1. **开始新建筑前**：必须先有 spec、implementation plan，并在本文件登记 `NOT_STARTED -> IN_PROGRESS`。
2. **完成一个有意义代码批次后**：记录实现内容和关键 commit。
3. **Portable CI 通过后**：只能标 `PORTABLE_VERIFIED`，不能写“完整通过”。
4. **用户本机 All/build 通过后**：再升级为 `LOCAL_VERIFIED`。
5. **游戏内实际验证后**：再升级为 `IN_GAME_VERIFIED`，同时更新 `test-matrix.md`。
6. **发生失败**：记录第一个真实失败、相关 commit/日志和下一步，不隐藏失败历史。
7. **发生网络中断**：恢复时先对照本文件和 Git HEAD；HEAD 比文档新时先做 diff/commit 审计，不重复执行已存在代码。
8. **每次准备切换到下一台建筑前**：先更新本文件，保证最后一个提交之后始终存在可恢复断点。
