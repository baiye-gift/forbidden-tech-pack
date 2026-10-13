# Forbidden Technology Pack Test Matrix

Automated checks may be recorded as PASS only when the corresponding command has been run successfully. Full in-game cases remain **PENDING** until they are executed in Oxygen Not Included and supported by a Player.log excerpt or an explicit observation note. A limited startup smoke test does not complete a content-mode row.

## 2026-10-11 — 0.2.0 最终修复与五台画风复核

本节为当前候选结果，下节为改造前历史检查，不将旧 ZIP 校验值当作当前包。独立审查详情见 [审查记录](reviews/2026-10-11-forbidden-tech-review.md)。

| 检查 | 状态 | 证据与边界 |
|---|---|---|
| Portable | PASS | `test.ps1 -Suite Portable` 退出 0；184 核心、73 生产行为、18 实际付款适配器、16 研究连接宿主，元素 YAML、素材与打包合同通过 |
| 正常付款到部分欠压 | PASS（宿主） | 原生付款通知同步结算、普通回调不可消费回执、三种负载分摊相位与 10000 周期时钟；不等于运行真实 CircuitManager/Unity |
| 真实库存与回液批量 | PASS（宿主/原生接口） | 5 kg 正常批量、2 秒尾料、混合满仓疏通；6 kg 候选实际只排 0.01 kg 后立即/0.2 秒禁止再排，2 秒允许；保持原生运输，游戏实际液路仍 PENDING |
| 原生六套检查 | PASS（托管/反射/IL） | ElementCatalog、AnalyzerAdapter、AnalyzerRecipe、OptionsLocalization、SafeRemoval、CrusherConfig 全部退出 0；包含电网 epoch、周期时钟、液路签名、凝聚配方、原质轨道过滤 |
| 安全移除边界 | PASS（宿主/IL） | 移除后反应炉生成受限；五台在建计划返还库存及原生 Cancel；待取消计划仍阻止完成 |
| 五台美术与动画 | PASS（作者素材/构建） | 已查看五台统一工业轮廓预览，移除固定假料位条；源素材状态/机械占地检查通过，六组 KAnim 构建成功。运行部件动作、画风融入游戏仍 PENDING |
| 独立审查 | PASS（范围内） | 两轮共 5 项 P2 已修复并独立复核闭环；独立重跑 73/18 与安全移除宿主，无确认剩余 P1/P2 |
| 最终 ZIP | PASS | `pack-release.ps1` 退出 0；最终 DLL/PLib 与六组 KAnim 重建、构建目录及 ZIP 解压复验通过；1,943,795 字节，当前 SHA256 见 CHANGELOG。下节旧同名候选大小/哈希仅为历史值 |
| Unity/Sim、实际电网/管路、存读档、中文布局、游戏画面 | PENDING | 本轮未启动游戏或加载殖民地。研究原生构造既有 Unity ECall 限制仍在；不宣称 All 通过 |

最终检查日志为 `obj/verification-repair-final-portable.log`、`obj/verification-repair-final-native.log`、`obj/verification-repair-final-package.log`。三项分别执行，均退出 0；`git diff --check` 通过。新实机案例须覆盖普通满功率连续运行转欠压、载入 10000 周期以上存档、回液管仅有 0.01 kg 空位、混合热液满仓、移除开始后生成建筑及保留未取消施工计划。

## 2026-10-11 — 0.2.0 质能工程初始候选（历史）

新增原质湮灭反应炉、质能凝聚器及原质实体/菜单图标；继承 0.1.1 修复。下列托管或作者素材检查不等于实机验收。最终组合命令沿用下面六套原生探针加 Portable；研究注册原生构造另行运行并如实记录失败。

| 检查 | 状态 | 证据与边界 |
|---|---|---|
| 核心及 Portable | PASS | 最终组合退出 0；184 项核心逻辑、50 项生产/冷却宿主检查、新研究连接宿主 16 项检查通过。宿主执行真实适配源码，但使用确定性的假电网/运输/研究对象 |
| 质能支付与生产 | PASS（宿主） | 零/部分付款、点火完成不重复扣款、0.004 kg 实际扣料、9600 J 生成、满电缓存、热回流堵塞、缺料、自动化关闭、销毁后回调、启动恢复、无输入订单、成品满、取消保留热量、完整 18 MJ/6 MJ/1 kg 批次及单次完成。完整批次数值容许游戏 float 积分误差 |
| 研究连接 | PASS（宿主） / PENDING（实机） | 实际注册源码 16 项：正常/后备前置、模块隐藏、两节点分别拥有建筑、质能节点只接禁忌物质节点、费用重写、重复注册不增加边。原生 Tech 构造未通过 |
| 原生接口与配方 | PASS（托管） | 最终组合六套原生探针通过：当前程序集付款/Storage/Generator 接口、实际扣料 out 参数、反应炉 Serialize 字段、原生 Fabricator/Config；无原料凝聚配方构造、1 kg/500 秒和 Heated 输出温度 |
| 新建筑安全移除 | PASS（宿主/原生 IL） | 新两台发现与计数、返还全部燃料/冷却/成品仓、原质等质量转换、延迟销毁不阻挡完成。实际殖民地转换与原生温度/病菌仍待验收 |
| 中文、英文加载 | PASS（托管） | 最终文本的原生 PO 加载/参数检查通过；17 项中文选项、9 条质能状态、原质说明包含 18 MJ 预算 |
| 原创素材与 KAnim | PASS | 五台建筑和原质共六组 KAnim 构建；固定外壳与独立工作部件素材合同通过。原质 128/64/32/24 像素作者预览已查看；游戏占格、动画驱动与缩略图仍待验收 |
| UI 静态审计 | PASS（静态） | frontend-design-premium strict audit 零项；不替代原生界面排版、库存和交互 |
| 原生研究注册探针 | FAIL（宿主限制） | 单独执行 ResearchRegistrationRuntimeTests，仍在 `DlcManager.CheckPlatformSubscription → IsExpansion1Active → Tech..ctor` 抛出 Unity `SecurityException: ECall methods must be packaged into a system module`，不绕过失败。不能宣称 All 通过 |
| 0.2.0 ZIP 与解压复验 | PASS | `pack-release.ps1` 退出 0；构建目录及 ZIP 解压后校验通过，2,253,065 字节，SHA256 见 CHANGELOG。旧版本 ZIP 保留 |
| 安装、Unity/Sim、殖民地/DLC、存读档 | PENDING | 本轮未安装、启动游戏、推送或发布；所有实际游戏案例尚待执行 |

新增实机验收：5×5/4×4 占格及端口、同格反应炉供电/发电调度与净输出、首次点火/断电/读档、36 kW 部分供电、无原料订单取消及复制设置、冷热库存连续进出与满端停机、每批产出及轨道运输、原质库存小图标及温度/病菌说明、过热损坏与维修、辐射开关/屏蔽、基础版无辐射组件、新研究节点显示与重复注册、新开关重启及新旧五台建筑安全移除。下方 DLC/殖民地矩阵仍全部 PENDING。

## 2026-10-11 — 0.1.1 本地修复候选

此节是 0.1.1 阶段记录：当时只修改既有三台建筑，未实现新增建筑。旧版安装和启动记录不作为此次实机验收；当前结果见上方 0.2.0。

| 检查 | 状态 | 证据与边界 |
|---|---|---|
| Portable | PASS | `test.ps1 -Suite Portable` 随最终组合运行；165 项核心逻辑、24 项实际编译器/冷却控制器适配宿主行为，以及元素 YAML、素材、打包合同通过 |
| 电量、热量与冷却回流 | PASS（逻辑/宿主） | 无付款不增加进度/热量；回执不复用；0.2 秒转存 2 kg；冷/热缓冲合计热量守恒；回流堵塞恢复、旧热液标记迁移、成品堵塞的 Operational 要求 |
| 原生托管检查 | PASS | 最终组合执行 ElementCatalog、AnalyzerAdapter、AnalyzerRecipe、OptionsLocalization、SafeRemoval、CrusherConfig 六套探针；当前游戏电力回执目标、Storage.Transfer 返回值和保存标记反射/IL 合同通过 |
| 原生本地化加载 | PASS | 当前游戏 API 实际加载 zh/en PO，验证替换参数、link，以及定向更新 LocString 和原生文字表；不等于 Unity 中文排版验收 |
| All | FAIL（宿主限制） | ResearchRegistrationRuntimeTests 在 `DlcManager.CheckPlatformSubscription → Application.isEditor → Tech..ctor` 触发 `SecurityException: ECall methods must be packaged into a system module`；其余上述套件随后单独运行通过。不得把 All 报告为通过 |
| 构建与 KAnim | PASS | 当前 ONI 程序集编译并合并 PLib；三台建筑的新分层 KAnim 与原质 KAnim 构建成功。既有 CS0437 命名空间/类型冲突提示与安全移除过时 API 警告仍存在，无编译错误 |
| UI 静态审计 | PASS（静态） | frontend-design-premium strict audit 零项发现；采用原生侧栏/状态，不证明游戏内布局或交互 |
| ZIP 解压复验 | PASS | `pack-release.ps1` 退出 0；0.1.1 ZIP 构建目录和解压后都通过 verify-package，SHA256 记于 CHANGELOG；候选包不等于发布签核 |
| 安装、Unity/Sim、DLC、存读档 | PENDING | 本轮未安装、未启动游戏、未加载测试殖民地，未发布 |

本次需补实机案例：空解锁新建/载入、取消已预留物料订单、不同目标切换、解锁时仍在生产、旧热液标记迁移、断电/自动化多次停启、实际付款调度顺序、连续冷却与回流堵塞、输出轨道、满库后存读档、拆毁与安全移除第五个缓冲、中文侧栏和真实动画占格。所有内容模式仍为下方 PENDING。

最终组合命令：

```powershell
.\test.ps1 -Suite @('Portable','ElementCatalogRuntimeTests','AnalyzerAdapterContractTests','AnalyzerRecipeRuntimeTests','OptionsLocalizationRuntimeTests','SafeRemovalRuntimeTests','CrusherConfigContractTests') -GamePath '<游戏目录>'
```

## 2026-09-17 historical automated gates

| Check | Status | Evidence |
|---|---|---|
| Core and contract tests | PASS | `.\test.ps1 -Suite All -GamePath '<GamePath>'` — 152 tests passed on 2026-09-17 |
| Original KAnim compilation | PASS | `build-assets.ps1` generated all four KAnim triplets |
| Game DLL/package build | PASS | `build.ps1` completed against local ONI installation; deprecation warnings remain in explicit safe-removal scans |
| Package structure verification | PASS | `verify-package.ps1` passed after build |
| Local installation | PASS | `install.ps1` verified the package and installed only to `mods\\local\\ForbiddenTechnologyPack` |
| Release ZIP verification | PASS | `pack-release.ps1` rebuilt, SHA256-hashed, extracted, and reverified `ForbiddenTechnologyPack-0.1.0.zip` |

## Recorded smoke test

| Check | Status | Evidence |
|---|---|---|
| Steam main-menu startup | PASS | On 2026-09-17, the current Spaced Out!-enabled mode loaded the DLL and Animation group; the main menu remained responsive and Player.log contained no fatal error attributable to this Mod. This does not validate colony gameplay or other content modes. |

## Content mode

| Mode | Main menu | New colony | Existing colony | Research/build menu | Proto-Matter | Save/reload | Player.log | Status |
|---|---|---|---|---|---|---|---|---|
| Base game only | — | — | — | — | — | — | — | PENDING |
| Spaced Out! only / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| Frosty Planet Pack / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| Bionic Booster Pack / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| All supported DLC enabled | — | — | — | — | — | — | — | PENDING |

Record the exact game build and active content IDs for every executed row. A missing-content exception is a failure, not a skip.

## Building behavior

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Analyze a common solid | Compiler recipe appears after completion | PENDING | |
| Analyze the same solid again | Duplicate analysis recipe is absent | PENDING | |
| Crusher manual input | Correct Proto-Matter mass is produced | PENDING | |
| Crusher rail input/output | Conveyor flow works without invalid material acceptance | PENDING | |
| Crusher blocked output | No input/output mass is lost | PENDING | |
| Compiler common/industrial/rare/endgame solids | Each unlocked tier completes with configured conversion math | PENDING | |
| Power loss during work | Batch pauses and resumes intact | PENDING | |
| Automation disable during work | Batch pauses and resumes intact | PENDING | |
| Near-transition coolant | Compiler pauses; coolant does not change phase or disappear | PENDING | |
| Compiler blocked output + save/reload | Exactly one completed result batch remains after unblock | PENDING | |
| Deconstruct each machine with partial storage | Original element, mass, temperature and disease data are returned | PENDING | |

## Configuration and persistence

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Balanced preset | Values and behavior match preset | PENDING | |
| Strong preset | Values and behavior match preset | PENDING | |
| Extreme preset | Values and behavior match preset | PENDING | |
| Out-of-range Custom values | Values clamp to supported limits | PENDING | |
| Disable Analyzer | Hidden for new construction; existing instance/storage preserved | PENDING | |
| Disable Crusher | Hidden for new construction; existing instance/storage preserved | PENDING | |
| Disable Compiler | Hidden for new construction; existing instance/storage preserved | PENDING | |
| Restart after configuration change | Selected settings persist | PENDING | |
| DLC off/on around an unlocked material | Recipe hides/reappears while serialized unlock ID remains | PENDING | |

## Safe removal

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Loose Proto-Matter | Converted to equal-mass Igneous Rock | PENDING | |
| Stored Proto-Matter | Converted/returned without mass loss | PENDING | |
| Rail Proto-Matter | Converted with zero custom objects remaining | PENDING | |
| Crusher/Compiler Proto-Matter | Converted with storage safely returned | PENDING | |
| First + second confirmation | Destructive action requires both confirmations | PENDING | |
| Save under new name and reload | Colony opens without missing-element errors | PENDING | |
| Disable mod after successful cleanup | Prepared save loads without custom-content dependency | PENDING | |

## Performance soak

Run 10 Mass Crushers and 10 Matter Compilers for 20 production cycles at speed 3.

| Observation | Requirement | Status | Evidence / notes |
|---|---|---|---|
| Repeating exceptions/warnings | None caused by normal operation | PENDING | |
| Per-frame full element scans | None | PENDING | |
| Recipe queue growth | Bounded | PENDING | |
| Stored object growth | Bounded by logistics/output state | PENDING | |
| Catalog construction | Once per game load | PENDING | |
| Safe-removal object scans | Only after explicit confirmation | PENDING | |

## Release sign-off

Release remains **PENDING** until all required in-game rows above are executed for the intended supported content modes. Automated gates alone are not sufficient for publication.
