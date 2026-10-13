# Changelog

## 2026-10-14 — GitHub 修复分支已同步

- 用户明确要求同步此前已完成更新。同步目标为 `baiye-gift/forbidden-tech-pack` 的 `codex/repair-forbidden-tech`，基线/默认分支为 `feature/forbidden-technology-pack` 的 `573028b`；未覆盖远端已有提交。
- 同步范围：前述 0.1.1/0.2.0 修复与新增质能建筑的 `src/`、`tests/`、`scripts/`、`assets/`、`packaging/`、构建/打包脚本、`.gitignore`、`AGENTS.md`、`README.md`、`DESIGN.md`、`docs/` 和本日志；包含 10 月 12 日已完成新版 `preview.png`，保留独立审查记录及旧 debug 文档归档/备份脚本删除。`obj/`、`dist/`、`release/`、第三方本地依赖不上传，本次无新生产行为修改。
- 同步前重新验证：Portable 退出 0，184 核心、73 生产宿主、18 付款适配器、16 研究连接宿主及素材/打包合同通过；原生 ElementCatalog、AnalyzerAdapter、AnalyzerRecipe、OptionsLocalization、SafeRemoval、CrusherConfig 六套检查退出 0，当前游戏 DLL/PLib 与六组 KAnim 重建成功。首条六套检查命令误用 PowerShell CLI 数组传参，未进入检查；改为直接脚本数组调用后完整通过。`git diff --check` 通过，仅既有 PO 行尾提示，CS0437/过时接口警告保留。
- 构建会重建本地分发目录，检查后从项目根恢复已确认封面；本次不重打 release ZIP，10 月 11 日候选 ZIP 的历史校验值仍保持。未安装、发布或运行 Unity/Sim；原生研究构造的既有 Unity ECall 限制及实机验收待办不变。
- 已将实现提交 `9feccb7` 推送至远端 `codex/repair-forbidden-tech`，并创建草稿 PR [#1](https://github.com/baiye-gift/forbidden-tech-pack/pull/1)，基于默认分支 `feature/forbidden-technology-pack`；已附到当前聊天。源代码、新版封面、编译素材、测试与独立审查记录均已同步；默认分支尚未合并，未创建发布或上传 ZIP。本条同步结果通过后续日志提交同分支推送。

## 2026-10-12 — 工坊封面第二版，简化背景与光效

- 用户要求七张封面降低AI观感，确认现有建筑素材/简化平涂背景/调整标题方案。本包属于这次七图范围。实际替换项目根 `preview.png` 和 `dist/ForbiddenTechnologyPack/preview.png`，更新本日志；公共资产、脚本、备份和完整结果见 `../art/workshop-previews/2026-10-12-v2/`及 `../docs/ai-change-log.md`。
- 直接使用当前原质湮灭炉与物质编译器的透明PNG进行浅纸色排版，移除首版生成的车间背景、紫色光晕与额外场景道具；保留真实建筑轮廓和中文/英文标题，未修改源建筑素材。本轮未调用生成模型。
- 2份旧图备份至公共 `backups-before-v2/ForbiddenTechnologyPack-{project,dist}.png`，逐项SHA-256核对后替换。新版512×512、214899字节，项目根/分发根/公共上传图哈希一致；分发目录24个其他文件、引用源素材未变。七图共同118个其他模组文件及mods.json保持，渲染/替换脚本最终退出0，目视确认文字与主体。
- `git -C ForbiddenTechnologyPack diff --check`退出0，仅既有PO行尾转换提示；公共新资产另经图像/哈希检查。只变更封面与记录，无需编译、逻辑/托管或Unity/实机测试。仍未安装本mod，未上传发布、重打原release ZIP或修改游戏运行资源。工坊端接受和显示未验证。

## 2026-10-12 — 补齐创意工坊封面

- 用户指出此前按六个已安装白夜模组制作封面时漏掉禁忌科技建筑包；本次在同一套封面要求内补齐。检查当前0.2.0为分发候选，`mods/local`没有本模组；制作前说明以原质湮灭炉为主、沿用统一风格和512×512规格，并保存分发根目录。
- 实际新增 `preview.png`、`dist/ForbiddenTechnologyPack/preview.png`。工作区公共资产目录 `../art/workshop-previews/2026-10-12/ForbiddenTechnologyPack/` 新增 `illustration.png`、`generation.json`、`preview-1024.png`、`preview.png`；公共 `export-previews.ps1` 支持单mod与独立检查报告，新增 `forbidden-verification.json`，更新公共README及 `../docs/ai-change-log.md`。
- 使用内置ImageGen一次生成原质湮灭炉为主、物质编译器与质能凝聚器为辅的工业卡通插画，参考本项目最新建筑预览；紫色能量/原质配色。原生底图1254×1254，系统字体准确排版“禁忌科技建筑包 / FORBIDDEN TECHNOLOGY”，排版原图1024×1024，最终上传PNG512×512、601983字节。完整提示词与参考记录在generation.json。
- 检查：底图与最终图均目视检查，主体完整、标题不遮挡且无截字；最终PNG正常解码。工作区、项目根目录和分发根目录三份上传图SHA-256相同：`D2E7E9CF460AE9FC060D29B1E5EF862A23A0A58BEC1AD8D3098663BCB500674E`。分发目录原有24个文件逐项哈希未变。导出脚本退出0。本次仅封面资产，无需编译、逻辑或原生托管测试，未运行Unity/游戏验收；未安装或上传发布，现有release ZIP未重打包。

- 最后复核：512×512/601983字节与原六份报告完整性检查通过；`git -C ForbiddenTechnologyPack diff --check`退出0，只有既有PO行尾转换提示。新图与公共资产未跟踪，另按图像/哈希检查验证；报告明确本次目标是分发目录而非已安装目录。

## 2026-10-11 — 0.2.0 再核查修复、五台建筑画风与真实排放（本地候选）

- 用户批准修复、清理与湮灭炉重做，随后明确将凝聚器同等纳入；要求完成后使用独立智能体审查。保留 ID、保存字段、Storage 顺序、占地、端口及原定能量/质量平衡。
- 运行文件：`src/Game/Buildings/Common/FabricatorPowerBudget.cs` 使用 Powered Postfix / Unpowered Prefix 在同一原生付款通知中结算，普通加工回调不得消费回执；保留原生 PoweredFlag 和其他运行条件，删除无引用耗电累计字段。新增 `Common/SimulationTime.cs`，用 CircuitManager.Sim200msLast 的 long epoch 统一冷却运输预算，整数周期与周期内时间组成 double 时间，避免长殖民地 float 精度损失。`Compiler/MatterCompiler.cs` 将原质轨道过滤覆盖湮灭炉，删除未使用的产物筛选参数。`Compiler/CoolantController.cs` 分离状态刷新与排液，正常回液/不安全液体排出共用原生 200 ms / 2 kg 上限；保留旧热液迁移字段。`Analyzer/MatterAnalyzerConfig.cs`、`Crusher/MassCrusherConfig.cs`、`Compiler/MatterCompilerConfig.cs`、`MassEnergy/MassEnergyBuildingSupport.cs` 移除冲突的 PoweredActiveController；`MassEnergy/ProtoAnnihilationReactor.cs` 使用独立充能动画。`Localization/STRINGS.cs`、`packaging/translations/{zh,en}.po` 删除未使用旧冷却状态文案。
- 安全移除：`MassEnergy/ProtoAnnihilationReactor.cs` 在生成与回调执行 SafeRemovalStarted 守卫；`Safety/SafeRemovalController.cs` 按五个稳定建筑 ID 发现并统计在建计划，返还施工库存后发原生 Cancel，未实际移除的计划继续阻止完成。避免移除后运行及遗漏施工计划。
- 排放：新增 `src/Core/OutputBatchPolicy.cs`；`CoolantController.cs` 根据真实热仓候选质量累积至 5 kg，停工 2 秒排尾料，混合满仓允许疏通；观察原生 Dispense 的实际质量变化，任何实际小包排出后至少间隔 2 秒，含候选大库存却被管路部分接受的情况。采用原生液路传输，不生成碎块。复核五台固体输出：编译器 10/100 kg、粉碎机整批预设、凝聚器 500 秒/1 kg，保留原生轨道分包。
- 素材/文档：新增 `assets/{annihilation-v2-atlas,condenser-v2-atlas}.png`、`assets/mass-energy-v2-prompts.md`，修改 `assets/source-atlas.png`、`scripts/{build-layered-assets,build-mass-energy-assets}.py`、`assets/animation-manifest.json`、五台建筑的 SCML/源 PNG/UI、作者 PNG/GIF 预览与对应 `packaging/anim/forbidden_technology/*`。五台统一粗轮廓、简化分色、哑光象牙/钢/铜/紫工业外观；移除绘死的液罐液位及无数据关联的装饰条，保留机械线圈/光学镜头。湮灭炉采用立式屏蔽腔、对向注入机构与侧置换热器，凝聚器采用横向通道与底部压坯机构，部件独立运动。源图切片取主连通轮廓排除相邻单元杂边。更新 `README.md`、`DESIGN.md`、`assets/README.md`、`docs/test-matrix.md` 及审查报告。
- 清理：删除根目录 11 份未被调用的 build-assets/verify-package 备份脚本；两份 debug 文档移到 `docs/archive/` 保存排查历史；`.gitignore` 排除 codegraph 缓存和生成审计报告。必要图集、源部件、编译资源保留。
- 清理补充：`src/Core/AnalyzerPolicy.cs` 删除仅旧测试引用的 ShouldConsumeSample，`tests/AnalyzerPolicyTests.cs` 改验证实际 RecipePlanFactory 的消耗合同；删除两台旧机械部件 PNG，原 `mass-energy-atlas.png` 因原质仍引用而保留。测试改写首次误用不允许 Extreme 回环的 Iron 规则导致检查失败，修正为实际支持该预设的 Diamond 后完整重跑通过，未放宽生产平衡限制。
- 测试文件：更新 `tests/runtime/ProductionHost.cs`、`tests/ProductionRuntimeTests.ps1`，新增 `tests/runtime/PaymentBehavior.cs`，覆盖连续正常付款到部分欠压、三种负载分摊相位、长殖民地时钟、自动化/满端、不可重复付款、回液批量及大库存被管路微量接受。`tests/{SafeRemovalRuntimeTests,CrusherConfigContractTests}.ps1` 增加在建计划退款/取消/计数与原生 epoch、时钟、液路方法签名核对；`tests/AssetSourceContractTests.ps1` 增加新机械状态及占地边界检查。
- 最终检查：Portable 退出 0，184 项核心、73 项生产宿主、18 项实际付款适配器、16 项研究连接宿主通过，元素 YAML、素材与打包合同通过；ElementCatalogRuntimeTests、AnalyzerAdapterContractTests、AnalyzerRecipeRuntimeTests、OptionsLocalizationRuntimeTests、SafeRemovalRuntimeTests、CrusherConfigContractTests 六套原生检查退出 0。当前游戏程序集 DLL/PLib 合并及六组 KAnim 构建通过。独立智能体两轮发现共 5 项 P2，修复后独立重跑 73/18 和安全移除宿主并复现大库存 0.01 kg 排放间隔；确认范围内无剩余 P1/P2，详见 `docs/reviews/2026-10-11-forbidden-tech-review.md`。
- 本轮未运行 Unity/Sim、真实供电/管路调度、殖民地/DLC 或存读档；原生研究注册构造的既有 Unity ECall 限制仍在，不宣称 All 或实机通过。既有 CS0437/过时 API 编译警告保留。未安装、提交/推送或发布。
- 最终本地候选包：`pack-release.ps1 -GamePath <游戏目录>` 退出 0；DLL/PLib 与六组 KAnim 重建、构建目录校验和 ZIP 解压复验通过；`release/ForbiddenTechnologyPack-0.2.0.zip` 为 1,943,795 字节，SHA256 `E09645DF9D310B1243D71FD886BB7003603D889305DCE7A466AB79AD2D82E5AA`。下节同名旧候选大小/哈希仅保留历史记录，0.1.1 ZIP 保留。`git diff --check` 通过；PO 文件仅有 Git 行尾转换提示。

## 2026-10-11 — 0.2.0 质能工程本地候选（尚未安装或发布）

- 用户批准两台新增建筑及原质 UI 改进；范围与能量合同见 `docs/superpowers/plans/2026-10-11-mass-energy-engineering.md`。
- 核心文件：新增 `src/Core/MassEnergyBalance.cs`；修改 `ModIdentity.cs`、`PackOptions.cs`、`RegistrationPolicy.cs`。48 kW 总发电、8 kW 运行、40 kW 净电力、0.02 kg/s 燃料、200 kDTU/s 废热；凝聚 18 MJ/kg、36 kW、500 秒/1 kg、12 kDTU/s。原质可回收质能预算以游戏缩放计 12 MJ/kg，凝聚的 18 MJ/kg 输入高于电/热全回收；既有材料预设不降低这套成本。
- 建筑文件：新增 `src/Game/Buildings/MassEnergy/` 下 `ProtoAnnihilationReactor.cs`、`ProtoAnnihilationReactorConfig.cs`、`EnergyMatterCondenser.cs`、`EnergyMatterCondenserConfig.cs`、`MassEnergyBuildingSupport.cs`、`MassEnergyStatus.cs`。反应炉 5×5、120 kg 原质仓、96 kJ 原生 Generator 缓冲；600 kJ 有偿启动进度序列化，启动只排热不扣燃料或发电，实际燃料回执决定发电量，满电/缺料/冷却异常停机；实际反应驱动部件动画及 Spaced Out! 原生辐射。凝聚器 4×4、原生无原料订单/进度、20 kg 成品仓，付费与排热完成整批后才生成 1 kg，取消不退电或撤热。沿用原生过热损坏/维修，未增加周期耗材磨损。
- 集成文件：修改 `src/Game/Recipes/RecipeRegistry.cs`、`Registration/{BuildingRegistration,ForbiddenResearchRegistration}.cs`、`Safety/SafeRemovalController.cs`、`Options/ForbiddenTechOptions.cs`、`Localization/STRINGS.cs`、`packaging/translations/{zh,en}.po`、`scripts/sync-localization.py`。新增稳定 ID、独立模块/建筑开关、质能研究；新节点只接禁忌物质工程并保留明确研究费用；发电设备进电力菜单，凝聚设备进精炼菜单。状态/说明读取真实付款、库存、冷却与进度；双语生成条目共 45 项，选项共 17 项。安全移除覆盖两台新建筑、燃料/成品与全部冷热库存，延迟销毁按原生销毁标记计数。
- 原质与美术文件：新增 `assets/mass-energy-atlas.png`、`scripts/build-mass-energy-assets.py`、两台新建筑 `assets/scml/{proto_annihilation_reactor,energy_matter_condenser}/*`、`proto_matter/ui_0.png` 和新作者预览；修改原质 SCML/实体 PNG、`assets/animation-manifest.json`、`build-assets.ps1`，编译对应 `packaging/anim/forbidden_technology/` 两个新增 KAnim 三件组及原质三件组。原质改为深色实体壳/紫色晶粒/青色内层，独立菜单图标；128/64/32/24 像素作者预览已查看。固定建筑外壳与约束环/活塞/聚焦透镜/托架动作分离。保持原质元素和动画 ID，不更换已有库存。
- 测试文件：新增 `tests/MassEnergyBalanceTests.cs`、`tests/ResearchTopologyRuntimeTests.ps1`；修改 `CoreTestProgram.cs`、`RegistrationPolicyTests.cs`、`ProductionRuntimeTests.ps1`、`runtime/ProductionHost.cs`、`ElementYamlTests.ps1`、`AssetSourceContractTests.ps1`、`AnalyzerRecipeRuntimeTests.ps1`、`OptionsLocalizationRuntimeTests.ps1`、`SafeRemovalRuntimeTests.ps1`、`CrusherConfigContractTests.ps1` 及 `test.ps1`。宿主覆盖反应炉与凝聚器的实际付费/排热/产物、完整批次精度、销毁后反应炉回调和新研究连接；原生配方构造验证无原料、显式温度与单个成品；原生反射/IL 检查实际扣料、Generator 及保存字段。
- 版本/文档文件：修改 `packaging/mod_info.yaml`、`src/Game/ModEntryPoint.cs` 为 0.2.0，修改 `pack-release.ps1` 使打包保留旧版本 ZIP；更新 `README.md`、`DESIGN.md`、`assets/README.md`、`docs/test-matrix.md`、本日志及实施计划。0.1.1 修复及其兼容合同继续保留。
- 最终自动检查：`test.ps1 -Suite Portable,ElementCatalogRuntimeTests,AnalyzerAdapterContractTests,AnalyzerRecipeRuntimeTests,OptionsLocalizationRuntimeTests,SafeRemovalRuntimeTests,CrusherConfigContractTests -GamePath <游戏目录>` 退出 0；184 项核心逻辑、50 项生产宿主、16 项研究连接宿主及原生六套检查通过，包括新凝聚配方、新安全移除和最终双语加载；六组 KAnim/当前程序集编译及 PLib 合并成功。UI strict 静态审计零项、git diff --check 无空白错误。
- 本地 ZIP：`pack-release.ps1 -GamePath <游戏目录>` 退出 0，构建目录、压缩包及解压后复验全部通过；`release/ForbiddenTechnologyPack-0.2.0.zip`（2,253,065 字节），SHA256 `5A5A03E12CF5CDDE1477C729D2E108E1B2AA0BC2D12FD529CAE0FFB814A4345B`。0.1.1 ZIP 保留。这是本地候选包，不代表安装/发布或实机签核。
- 明确限制：原生 ResearchRegistrationRuntimeTests 单独重跑仍在 `DlcManager.CheckPlatformSubscription → IsExpansion1Active → Tech..ctor` 抛出 Unity ECall SecurityException；未绕过、未宣称 All 通过。仍有既有 CS0437/过时接口编译警告。未安装、提交/推送或发布；Unity/Sim、殖民地/DLC、供电调度、存读档、辐射屏蔽、中文原生布局与真实动画占格均未验证，详见验证矩阵。

## 2026-10-11 — 0.1.1 本地修复候选（尚未安装或发布）

- 基线：GitHub baiye-gift/forbidden-tech-pack，默认分支 feature/forbidden-technology-pack 的 573028b；本地分支 codex/repair-forbidden-tech。用户批准修复现有三台建筑，新增建筑仅讨论候选，未实现。
- 生产/库存文件：`src/Game/Buildings/{Analyzer,Crusher,Compiler}` 中三台类与 Config、`Common/FabricatorSupport.cs`、`src/Game/Safety/SafeRemovalController.cs`。空配方不进入原生越界路径；解锁事件延迟订阅及忙闲刷新；恢复已保存目标索引；允许取消已预留订单；已产成品的输出不依赖当前解锁；粉碎机和编译器自动运行、满库通过 Operational 停机。保留 ID、占地、端口、原有字段及前四个 Storage 顺序。
- 付款/热量文件：新增 `src/Core/PaidWorkLedger.cs`、`Common/FabricatorPowerBudget.cs`。只依据原生 CircuitManager 实际付款回执推进自动加工，瞬时额度不保存且不复用。`Compiler/CoolantController.cs` 对同一有效加工区间加热并用原生 Storage 拆分转存；追加第五个热液回流缓冲，每 0.2 秒最多转存 2 kg，冷端残留保留其热量；堵塞不增加进度/热量，旧待排热液标记原量原温迁移，拆毁/安全移除覆盖新增库存。
- 原生界面/说明文件：新增 `Common/FabricatorStatus.cs`、`src/Game/Localization/TranslationLoader.cs`；修改 `Localization/STRINGS.cs`、`src/Game/Recipes/RecipeRegistry.cs`、`src/Game/ModEntryPoint.cs`、`packaging/translations/{zh,en}.po`，新增 `scripts/sync-localization.py`。共用真实库存与进度状态，缺料/堵塞/冷却给出办法；配方显示实际损耗和成本，按原料命名粉碎配方。定向加载 PO 后重新注册本模组文字树，修复 LocString 字段与原生 Strings 表不同步，保留英文回退。
- 美术文件：新增 `assets/source-atlas.png`、`assets/README.md`、`assets/preview/*`、六个工作部件 PNG、`scripts/build-layered-assets.py`；修改三台 `assets/scml/*` 的 SCML/外壳/菜单图标及对应 `packaging/anim/forbidden_technology/*` 三组 KAnim。外壳固定，扫描头滑动、转子和压杆运动、发射环和出料机构运动，保留动画状态与 ui_0。作者预览不代表游戏截图或实机验收。
- 工程/文档文件：新增 `AGENTS.md`、`DESIGN.md`；修改 `README.md`、本日志、`docs/test-matrix.md`、`packaging/mod_info.yaml`、`build-assets.ps1`、`build.ps1`、`pack-release.ps1`。版本为 0.1.1，打包版本从元数据取得，递归删除前限定解析输出路径；kanimal 在已安装 .NET 6/8 上使用 Major roll-forward。
- 测试文件：新增 `tests/PaidWorkLedgerTests.cs`、`tests/ProductionRuntimeTests.ps1`、`tests/runtime/ProductionHost.cs`；修改 `test.ps1`、`tests/CoreTestProgram.cs`、`AssetSourceContractTests.ps1`、`ReleaseWorkflowContractTests.ps1` 及六个原生 runtime 探针。原生探针改为 .NET 6 + Major roll-forward；付款目标/冷却保存及原生 zh/en 加载增设真实托管检查。
- 实际检查：最终 Portable + ElementCatalogRuntimeTests、AnalyzerAdapterContractTests、AnalyzerRecipeRuntimeTests、OptionsLocalizationRuntimeTests、SafeRemovalRuntimeTests、CrusherConfigContractTests 组合退出 0；165 项核心逻辑及 24 项生产宿主行为通过，元素/素材/打包合同、原生付款/冷却接口、分析完成、两种语言定向加载与安全移除托管检查通过。当前游戏程序集 DLL/PLib 合并、四组 KAnim 构建成功。strict UI 静态审计零项，git diff --check 通过。
- 明确限制：All 在原有 ResearchRegistrationRuntimeTests 的 Unity ECall 上失败（DlcManager/Tech 构造路径、SecurityException），未绕过错误或声称研究实机通过。编译仍有既有 CS0437/过时 API 警告。本轮未安装、未提交/推送、未发布、未运行 Unity/Sim、未测试殖民地/DLC/真实存读档/原生调度时序/机械动画占格/中文界面排版。游戏验收见验证矩阵。
- 本地 ZIP：`pack-release.ps1 -GamePath <游戏目录>` 退出 0，构建目录校验、压缩与解压后复验均通过；`release/ForbiddenTechnologyPack-0.1.1.zip`，SHA256 `CCAF5168E57D0A87762E00B5407BC6137E380D9306EDA02E42569E01383A82F7`。这是本地候选包，尚无安装/发布签核。

## 0.1.0

- Added Matter Analyzer for persistent material discovery and compiler recipe unlocks.
- Added Mass Crusher for converting eligible solids into Proto-Matter with manual and conveyor logistics.
- Added Matter Compiler for reconstructing unlocked solids with safe liquid-coolant gating and protected output handling.
- Added Proto-Matter as a transportable intermediate material.
- Added base-game and supported-DLC content filtering.
- Added Balanced, Strong, Extreme, and clamped Custom configuration modes plus per-building enable switches.
- Added automation ports and solid conveyor support for the production chain.
- Added safe coolant checks that pause processing without deleting coolant or consuming an unsafe batch.
- Added two-stage Safe Removal for preparing existing colonies before the mod is disabled.
- Added original KAnim assets and English/Simplified Chinese localization.
