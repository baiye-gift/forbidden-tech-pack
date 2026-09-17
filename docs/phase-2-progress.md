# 第二阶段开发进度跟踪

> **用途：这是第二阶段开发的实时断点文档。** 发生网络中断、会话丢失或换人接手时，以本文件 + 当前 Git HEAD + 测试结果为准，不从头重跑已经完成的开发任务。

最后更新：2026-09-18  
当前分支：`feature/baiye-20260917-phase2-protomatter-field`

## 1. 当前阶段结论

第二阶段主题：**原质场与维度耦合**。

当前开发重点：**物质重构器（Matter Reconstructor）**。

物质重构器主体代码、资源链和本机 ONI DLL 集成验证均已完成到 `LOCAL_VERIFIED`。当前不再进行重构器代码开发，真正的下一断点是：

**Task 7：安装当前 Phase 2 包并执行物质重构器游戏内验证。**

不要重新执行 Task 1–6，也不要重复处理已经关闭的 SafeRemovalRuntime fixture 问题。

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
| Phase 2 共用原质干扰框架 | `LOCAL_VERIFIED` | `ForbiddenTechDevice`、事件驱动干扰管理、一阶段分析仪/粉碎机/编译器接入 | 游戏内验证干扰暂停/恢复表现 |
| Task 1 重构 Core Policy | `LOCAL_VERIFIED` | 层级映射、1000 kg 基质/产物质量规则、原质成本、倍率验证 | 游戏内验证实际质量行为 |
| Task 2 重构配方与已分析目标过滤 | `LOCAL_VERIFIED` | `ReconstructorRecipes`、分析解锁过滤、原质独立消耗 | 游戏内配方列表、分析后刷新、工作单保持 |
| Task 3 现实基质层级 Tag | `LOCAL_VERIFIED` | Common / OreOrOrganic / Industrial / Rare 稳定 Tag，复用现有材料分类 | 游戏内元素 Tag 与输送行为 |
| Task 4 Matter Reconstructor 建筑与运行组件 | `LOCAL_VERIFIED` | 4×4 BuildingConfig、4800 W、24 kDTU/s、输入/输出存储、固体轨道、电力、自动化、`ForbiddenTechDevice` | 游戏内运行、物流、电力、自动化 |
| Task 5 注册、二阶段研究、本地化、安全移除 | `LOCAL_VERIFIED` | `BaiyeForbiddenProtoFieldEngineering`、菜单、本地化、配置标签、安全移除接入 | 游戏研究树、菜单、配置、安全移除 |
| Task 6 KAnim / 图标 / 资源链 | `LOCAL_VERIFIED` | SCML、manifest、body/UI sprite Base64 源、自动恢复脚本、build-assets 映射、资源契约 | 游戏内图标、尺寸、动画落地效果 |
| Task 7 完整集成验证 | `LOCAL_VERIFIED` | GitHub Portable CI 通过；本机 `test.ps1 -Suite All` 223 项通过；测试过程中真实 ONI DLL 与包重新生成成功 | 安装当前包 + 游戏内验证 + 更新 test matrix |
| 熵流偏转器 | `NOT_STARTED` | 已有稳定 ID / 配置规划 | 等物质重构器游戏内验证后再开始 |
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

该次验证证明 Phase 2 共用框架 + 一阶段三台建筑干扰接入可以在当前 ONI DLL 上编译并通过当时的完整测试。

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

### 4.3 Task 7 首次本机 All 暴露的问题

首次本机 All 时，Mod 本体已经成功完成编译和打包，但 `SafeRemovalRuntimeTests.ps1` 的旧 Phase 1 behavior fixture 没有声明 `MatterReconstructor`，因此出现：

```text
SafeRemovalController.cs(5,46): error CS0234: ... Buildings.Reconstructor ...
```

该问题属于测试夹具过期，不是 Mod 本体缺少 Reconstructor。修复提交：

`be657795` — `test(phase2): cover reconstructor safe-removal runtime fixture`

修复内容：

- Runtime probe 增加 `MatterReconstructor` safe-removal 覆盖；
- behavior fixture 增加 `MatterReconstructor` stub；
- fixture 补齐 `MatterReconstructorId`；
- fixture 补齐 `ProtoFieldResearchId`。

### 4.4 Task 7 本机重新验证成功

2026-09-18，用户在当前 ONI 安装上重新执行完整 All，得到：

```text
Wrote D:\缺氧mod开发\ForbiddenTechnologyPack\dist\ForbiddenTechnologyPack\ForbiddenTechnologyPack.dll
Built package: D:\缺氧mod开发\ForbiddenTechnologyPack\dist\ForbiddenTechnologyPack
Crusher rail compiled contract and live predicate passed (valid, unknown, forbidden, empty IDs). Native rail movement requires in-game validation.
TOTAL: 223 passed
```

因此可以确认：

- 当前 Phase 2 源码可使用用户本机 ONI DLL 编译；
- 重构器相关 runtime/source/Core 契约进入完整 All 并通过；
- KAnim/包构建链可在完整 All 路径中成功执行；
- safe-removal fixture 修复已通过本机重新验证；
- 真实游戏内行为仍然不能由该结果替代。

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
- `be657795` — `test(phase2): cover reconstructor safe-removal runtime fixture`
  - 修复 Task 7 首次本机 All 暴露的旧 Phase 1 fixture，并把 Reconstructor 纳入 runtime probe。

---

## 6. 当前精确下一步

**当前不需要继续编译，也不需要重新开发物质重构器。**

下一步是安装当前 `dist\ForbiddenTechnologyPack` 到本地 Mod 目录，然后完全退出并重启 ONI，开始游戏内验证。

推荐先备份/替换本地 Mod：

```powershell
$src = "D:\缺氧mod开发\ForbiddenTechnologyPack\dist\ForbiddenTechnologyPack"
$dst = "$env:USERPROFILE\Documents\Klei\OxygenNotIncluded\mods\local\ForbiddenTechnologyPack"

if (Test-Path $dst) {
    Copy-Item $dst "$dst.backup-20260918" -Recurse -Force
    Remove-Item $dst -Recurse -Force
}
Copy-Item $src $dst -Recurse -Force
```

然后：

1. 完全退出 ONI；
2. 从 Steam 重新启动；
3. 确认 Mod 启用；
4. 进入已有测试存档或专用测试殖民地；
5. 按第 7 节清单逐项验证；
6. 每次发现第一个真实失败就停止继续扩展测试，记录 Player.log / 现象，先修该失败。

---

## 7. 游戏内验证清单（物质重构器）

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

**当前没有已知代码/编译阻塞。**

旧阻塞：`SafeRemovalRuntimeTests` Phase 1 fixture 缺少 Reconstructor。

状态：`CLOSED`  
修复：`be657795`  
本机重新验证：`TOTAL: 223 passed`

当前仅剩游戏内验证，不能在执行前标记为 `IN_GAME_VERIFIED`。

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
