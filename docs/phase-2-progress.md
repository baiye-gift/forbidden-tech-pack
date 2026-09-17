# 第二阶段开发进度跟踪

> **权威实时断点文档。** 网络中断、换会话或换人接手时，先看本文件，再对照当前 Git HEAD；如果 HEAD 比本文件记录更新，先审计提交，不允许从头重复开发。

最后更新：2026-09-18  
当前分支：`feature/baiye-20260917-phase2-protomatter-field`  
本次进度审计基准：`ce1ed88e`（已同步熵流偏转器 / 湮灭堆实施计划）

## 1. 当前结论

第二阶段主题：**原质场与维度耦合 / Proto-Matter Field & Dimensional Coupling**。

当前开发顺序已经推进到：

1. 共用原质干扰框架：已完成；
2. 物质重构器：代码与本机 All 已完成，等待最终游戏内验证；
3. 熵流偏转器：主体代码已完成，资源链刚补齐，等待当前 Portable CI 结论与最终本机/游戏验证；
4. 物质湮灭堆：Core 状态机已完成并 Portable 验证通过，下一断点是 **Game Runtime**。

**禁止重新开发重构器、熵流偏转器 Core/Controller/Config/注册/安全移除，也禁止重新创建湮灭堆 Core Policy。**

---

## 2. 关键文档

### 总体
- `docs/phase-2-protomatter-field-development.md`
- `docs/phase-2-development-guide.md`
- `docs/test-matrix.md`

### 物质重构器
- Spec：`docs/superpowers/specs/2026-09-17-matter-reconstructor-design.md`
- Plan：`docs/superpowers/plans/2026-09-17-matter-reconstructor.md`

### 熵流偏转器
- Spec：`docs/superpowers/specs/2026-09-18-entropy-flux-diverter-design.md`
- Plan：`docs/superpowers/plans/2026-09-18-entropy-flux-diverter.md`

### 物质湮灭堆
- Spec：`docs/superpowers/specs/2026-09-18-matter-annihilation-reactor-design.md`
- Plan：`docs/superpowers/plans/2026-09-18-matter-annihilation-reactor.md`

---

## 3. 当前任务状态

| 模块 | 状态 | 已实现 | 下一步 |
| --- | --- | --- | --- |
| Phase 2 共用原质干扰框架 | `LOCAL_VERIFIED` | `ForbiddenTechDevice`、事件驱动 `ProtoMatterInterferenceManager`、一阶段三台建筑接入 | 最终游戏内验证暂停/恢复、重叠源、存档行为 |
| 物质重构器 | `LOCAL_VERIFIED` | Core、配方、层级 Tag、4×4 Config/Runtime、研究、菜单、本地化、安全移除、SCML/图标、资源链 | 最终 Codex/用户游戏内验证 |
| 熵流偏转器 Core | `PORTABLE_VERIFIED` | `EntropyFluxPolicy`：等 DTU 转移、热平衡 clamp、1 K 相变保护、按实际 DTU 消耗原质 | 最终本机/游戏验证 |
| 熵流偏转器 Runtime | `CODE_DONE` | 4×4、双液体主/副管路、两个 10 kg buffer、原质 storage、`processedPair` 防重复、干扰暂停 | 当前资源 HEAD 的 Portable CI；之后本机 ONI DLL + 游戏验证 |
| 熵流偏转器集成 | `CODE_DONE` | Refining 菜单、Phase-2 研究解锁、本地化、安全移除 | 同上 |
| 熵流偏转器资源 | `CI_PENDING` | SCML、body/UI Base64 PNG、manifest、build-assets 映射、asset contract、恢复 PNG ignore | 等当前 Feature verification 结论 |
| 湮灭堆 Core | `PORTABLE_VERIFIED` | `ReactorState` + `AnnihilationReactorPolicy`，完整状态转换、退相干损失/热脉冲/干扰参数 | 不再修改，除非后续 Runtime 暴露明确契约缺口 |
| 湮灭堆 Runtime | `IN_PROGRESS` | 已有稳定 ID、配置开关、本地化文本、`ForbiddenTechDevice.IsInterfered` | **先写 `AnnihilationReactorSourceContractTests.ps1` 红灯，然后实现 Config + Controller** |
| 湮灭堆注册/安全移除 | `NOT_STARTED` | `BuildingRegistration` 已有 Power 分类 helper，但 reactor 尚未列入 implemented ids | Runtime 完成后接入 |
| 湮灭堆 KAnim | `NOT_STARTED` | Spec 已定义视觉与动画状态 | 最后资源阶段实现 |

---

## 4. 已验证证据

### 4.1 共用框架
用户本机完整测试曾得到：

```text
Wrote ...\ForbiddenTechnologyPack.dll
Built package: ...\dist\ForbiddenTechnologyPack
TOTAL: 206 passed
```

因此共用原质干扰框架可在当时当前 ONI DLL 上编译通过，但仍不等同于游戏行为验证。

### 4.2 物质重构器
用户本机 2026-09-18 重新执行 All：

```text
Wrote D:\缺氧mod开发\ForbiddenTechnologyPack\dist\ForbiddenTechnologyPack\ForbiddenTechnologyPack.dll
Built package: D:\缺氧mod开发\ForbiddenTechnologyPack\dist\ForbiddenTechnologyPack
Crusher rail compiled contract and live predicate passed (valid, unknown, forbidden, empty IDs). Native rail movement requires in-game validation.
TOTAL: 223 passed
```

因此重构器状态为 `LOCAL_VERIFIED`，不是 `IN_GAME_VERIFIED`。

### 4.3 熵流偏转器
已确认存在并完成：

- `src/Core/EntropyFluxPolicy.cs`
- `src/Game/Buildings/EntropyDiverter/EntropyFluxDiverterConfig.cs`
- `src/Game/Buildings/EntropyDiverter/EntropyFluxDiverterController.cs`
- `tests/EntropyFluxPolicyTests.cs`
- `tests/EntropyDiverterSourceContractTests.ps1`
- Refining 注册 / Phase-2 研究解锁 / 本地化 / safe removal

关键提交锚点：

- `1975147b` — controller
- `12982621` — `ForbiddenTechDevice.IsInterfered`
- `8afc6e14` — BuildingConfig
- `42c4579d` — Refining 注册
- `af80c931` — Phase-2 research unlock
- `433daa35` — 剩余 Phase-2 本地化
- `72b06685` — safe removal
- `0081d3e1` / `9c3fb0ed` — asset contract + Portable routing
- `42ef10d7` / `23809ab2` / `3d8fa26a` — SCML + body/UI payload
- `0611ec13` / `d82d32fc` / `534adebb` — manifest / build mapping / ignored restored PNGs

当前资源 HEAD 的 Feature verification 已排队；在看到成功结论前，熵流偏转器整体不能标记为 `PORTABLE_VERIFIED`。

### 4.4 物质湮灭堆
Core TDD 已完成：

- `f274e85f` — `AnnihilationReactorPolicyTests.cs`
- `b0d0d830` — 注册 Core suite
- `5f37a67f` — `AnnihilationReactorPolicy.cs`

`5f37a67f` 对应 Feature verification 已成功。

当前固定 Core 规则：

```text
Charging: 30 s
Stable fault grace: 5 s
Fluctuating recovery: 10 s
Fluctuating -> Critical: 15 s
Critical recovery: 5 s
Critical -> Decohered: 10 s
CoolingLockout: 30 s
Decoherence Proto-Matter loss: 35%
Heat pulse: 20,000,000 DTU / kg lost × HeatMultiplier
Interference radius: 12 cells
Interference duration: 60 s
```

---

## 5. 当前精确断点

### 熵流偏转器
不要再碰 Tasks 1-3。当前只需：

1. 看最新 entropy-asset HEAD 的 Feature verification；
2. 成功后更新本文件为 `PORTABLE_VERIFIED`；
3. 本机 ONI DLL 编译、真实 conduit、阻塞输出、save/reload、干扰行为全部留给最终 Codex/用户测试。

### 物质湮灭堆
从 `docs/superpowers/plans/2026-09-18-matter-annihilation-reactor.md` **Task 2 第一个未勾选项**开始：

1. 新增 `tests/AnnihilationReactorSourceContractTests.ps1`；
2. 接入 `test.ps1` Portable；
3. 先出现 RED（因为 Config/Controller 尚不存在）；
4. 实现 `MatterAnnihilationReactorConfig.cs`；
5. 实现 `MatterAnnihilationReactorController.cs`；
6. Portable 绿后，再做注册 / safe removal / KAnim。

---

## 6. 最终 Codex / 用户验证边界

开发阶段最多标记到 `PORTABLE_VERIFIED`，除非用户再次提供本机测试证据。

最终联合验证至少包括：

- `./test.ps1 -Suite All -GamePath "D:\steam\steamapps\common\OxygenNotIncluded"`
- `./build-assets.ps1`
- `./build.ps1 -GamePath "D:\steam\steamapps\common\OxygenNotIncluded"`
- 安装当前包后完全重启 ONI
- 研究树 / 建造菜单 / 图标 / 动画
- 熵流偏转器双液体管路、DTU 守恒、相变保护、输出阻塞、save/reload、干扰暂停
- 湮灭堆启动外部供电、冷却、净发电、全部状态转换、退相干一次性副作用、12 格 60 秒干扰、lockout、save/reload
- safe removal
- `Player.log` 不出现本 Mod 重复异常

---

## 7. 强制记录规则

1. 新建筑先有 Spec + Plan；
2. 实现进度同时更新 Plan checkbox 和本文件；
3. Core/source 完成只能写 `CODE_DONE`；
4. GitHub Portable CI 成功后才能写 `PORTABLE_VERIFIED`；
5. 用户/Codex 用真实 ONI DLL 跑完整构建后才能写 `LOCAL_VERIFIED`；
6. 游戏内实际验证后才能写 `IN_GAME_VERIFIED`；
7. 失败时记录第一个真实失败和对应 commit；
8. 网络中断恢复时先看 Git HEAD + 本文件，禁止重复开发已存在代码。
