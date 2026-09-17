# 禁忌科技建筑包：第二阶段开发与 GPT 接手指南

本文档用于把“禁忌科技建筑包”交给后续 GPT 或其他开发者继续开发。它记录第一阶段已经形成的工程基线、第二阶段新增建筑的标准流程，以及本阶段实际踩过的坑。

后续开发应以本文档、当前代码和 `docs/test-matrix.md` 三者为准。旧计划文档可以用于理解设计缘由，但不得用旧计划中的示例代码覆盖当前实现。

## 1. 第二阶段的目标与边界

第二阶段不是另建多个 Mod，而是在同一个 `ForbiddenTechnologyPack` 中继续增加“禁忌建筑”。玩家只加载一个 Mod，并在同一个中文配置界面中启用、禁用和调整各模块。

必须保持以下边界：

- 静态 ID 保持 `Baiye.ForbiddenTechnologyPack`，不能改名或另建第二个包。
- 现有物质分析仪、质量粉碎机、物质编译器和原质元素是第一阶段基线，不应为了新增建筑而重写。
- 每个新模块必须有独立开关；关闭后只隐藏新的研究/建造入口，不能破坏已经建造的实例和存档数据。
- 配置界面、建筑名称、说明、状态提示和安全移除提示以中文为主，同时保留英文 PO 翻译。
- 继续支持原版与全部 DLC，但“配置声明支持”不等于“已经实测支持”。每种内容模式都要在测试矩阵中单独记录。
- 新建筑默认使用实体材料、实体输出和游戏原生物流；不要用无形计数器替代玩家可观察、可运输的资源，除非设计文档明确要求。
- 不把开发说明、本机路径、日志、调试文件或个人配置写进面向普通玩家的 `README.md` 或发布包。

第二阶段开始写代码前，应先为每个候选建筑补充一页短设计，至少回答：输入、输出、空间尺寸、建造材料、耗电/发热、自动化端口、物流端口、故障状态、与 DLC 的关系、配置项、存档数据、安全移除方式和测试方法。没有明确这些内容时，不应直接创建 `IBuildingConfig`。

## 2. 当前基线

### 2.1 已有内容

| 内容 | 稳定 ID / 资源名 | 当前用途 |
|---|---|---|
| Mod | `Baiye.ForbiddenTechnologyPack` | 唯一整合包 |
| 原质 | `BaiyeForbiddenProtoMatter` / `baiye_proto_matter_kanim` | 可储存、可运输的实体固体中间产物 |
| 物质分析仪 | `BaiyeMatterAnalyzer` / `baiye_matter_analyzer_kanim` | 分析实体样本并永久解锁材料 |
| 质量粉碎机 | `BaiyeMassCrusher` / `baiye_mass_crusher_kanim` | 将合格固体转换为原质，支持运输轨道 |
| 物质编译器 | `BaiyeMatterCompiler` / `baiye_matter_compiler_kanim` | 将原质编译为已解锁材料，带固体物流和液体冷却 |
| 研究 | `BaiyeForbiddenMatterEngineering` | 解锁现有三个建筑 |

现有建筑占地分别为 3×3、4×4 和 5×5，位于建造菜单的 `Refining` 分类。研究节点优先接在 `MatterDeconstruction` 后，找不到时回退到 `HighTempForging`，并放在整个研究树右边缘之外，避免与原版或 DLC 节点重叠。

### 2.2 中文配置基线

`src/Game/Options/ForbiddenTechOptions.cs` 已提供中文 PLib 配置页，包括：

- 相对平衡、强力（默认）、极度超模、自定义四种预设；
- 总模块开关和三个建筑的独立开关；
- 原质回收率、编译成本、耗电、发热倍率；
- 工业、稀有、终局材料范围；
- 是否消耗分析样本；
- “准备安全移除”操作。

新增模块必须继续在这个类型中增加中文选项，不要另做第二个配置窗口。选项名称、说明和分组都要是自然中文，并补充 `OptionsLocalizationRuntimeTests` 或等价测试。

### 2.3 当前验证状态

截至 2026-09-17：

- `test.ps1 -Suite All` 的 152 项自动化检查已经通过；
- 四套原创 KAnim 可以编译，DLL、发布目录、ZIP 和安装目录结构可以验证；
- 通过 Steam 启动时，当前 DLC 模式下 DLL 与动画能够加载，主菜单可响应，没有本 Mod 导致的致命错误；
- 建造菜单问号缩略图、建筑像贴画以及建筑浮空问题已在资源契约层修复；
- 完整的殖民地内行为、各 DLC 内容模式、存档重载、安全移除和压力测试仍有待按 `docs/test-matrix.md` 执行。

因此，第二阶段可以继续设计和开发，但不得把“能到主菜单”写成“所有玩法已经验收”。

## 3. 代码与资源地图

| 路径 | 职责 | 新建筑通常是否要改 |
|---|---|---|
| `src/Core/ModIdentity.cs` | 所有已发布稳定 ID | 是，增加 ID，不改旧 ID |
| `src/Core/RegistrationPolicy.cs` | 根据配置决定注册哪些建筑 | 是 |
| `src/Core/*Policy.cs` | 不依赖 Unity 的业务规则 | 推荐新增，便于纯单元测试 |
| `src/Game/Options/ForbiddenTechOptions.cs` | 唯一中文配置页 | 是 |
| `src/Game/Registration/BuildingRegistration.cs` | 建造菜单可见性和分类 | 是 |
| `src/Game/Registration/ForbiddenResearchRegistration.cs` | 研究节点、前置和解锁建筑 | 视研究设计而定 |
| `src/Game/Buildings/<Module>/` | 建筑 Config、运行组件和状态机 | 是 |
| `src/Game/Recipes/RecipeRegistry.cs` | 当前三台物质设备的动态配方 | 仅当新建筑复用这条生产链时 |
| `src/Game/Save/` | 解锁和持久化数据 | 需要新存档状态时 |
| `src/Game/Safety/` | 安全移除和自定义对象清理 | 有自定义建筑/元素时必须改 |
| `src/Game/Localization/STRINGS.cs` | 代码内本地化键 | 是 |
| `packaging/translations/zh.po` | 中文翻译 | 是 |
| `packaging/translations/en.po` | 英文翻译 | 是 |
| `assets/scml/<module>/` | 原始 PNG 与 SCML | 是 |
| `assets/animation-manifest.json` | 必须存在的动画状态 | 是 |
| `build-assets.ps1` | SCML 到 KAnim 的编译与输出布局 | 是，目前源目录映射是显式表 |
| `tests/` | 纯逻辑、源码契约、游戏程序集探针 | 是 |
| `docs/test-matrix.md` | 必须人工执行的游戏内验收 | 是 |

发布目录 `packaging/anim` 和 `dist/ForbiddenTechnologyPack` 都是构建产物。不要直接手工修补其中的文件；应修改 `assets/scml`、C# 源码或构建脚本后重新生成。

## 4. 新增一个禁忌建筑的标准流程

以下顺序既是实现顺序，也是后续 GPT 的工作约束。

### 4.1 先定义最小设计契约

在代码之前确定：

1. 建筑的唯一职责，避免与现有三台机器重复。
2. 稳定 ID、英文资源基名和中文名称。
3. 输入/输出的元素、质量、温度和疾病数据如何处理。
4. 占地、建造位置、建造材料、功率、发热、装饰和噪声。
5. 手动搬运、固体轨道、液体管道、气体管道和自动化端口中的哪些是必需的。
6. 正常、待机、工作、阻塞、缺少关键资源、过热等状态。
7. 对原版和各 DLC 的依赖，以及 DLC 不存在时的降级行为。
8. 配置开关、数值范围和预设行为。
9. 中途断电、禁用、拆除、保存/读取和停用 Mod 时的处理方式。

稳定 ID 一旦发布就不得改名。资源名建议使用 `baiye_<snake_case_name>`，代码 ID 使用 `Baiye<PascalCaseName>`。

### 4.2 先写可脱离游戏运行的规则

把换算、材料过滤、容量判断、状态选择等纯逻辑放在 `src/Core`，再在 `tests/*.cs` 中写边界测试。至少覆盖：

- 零、负数、`NaN` 和无穷值；
- 容量刚好足够和差一个极小量；
- 未知元素、自定义元素和禁止循环处理的元素；
- 各平衡预设和自定义参数上下限；
- 功能开关关闭时的注册计划。

不要为了测试纯逻辑去初始化 Unity 或 `ElementLoader`。命令行探针无法安全执行部分 Unity ECall；能用字符串 ID、受控映射或纯函数验证的内容，应保持在 Core 层。

### 4.3 接入 ID、配置和注册

新增建筑至少要同步修改：

1. `ModIdentity`：增加稳定建筑 ID。
2. `ForbiddenTechOptions`、`RawOptions`、`ResolvedOptions` 和 `PackOptions.Resolve`：增加独立开关及必要参数。
3. `RegistrationPolicy`：只有总模块和新建筑开关都开启时才列出新 ID。
4. `BuildingRegistration`：把新 ID 放入完整清单，根据计划控制 `ShowInBuildMenu`，只添加一次菜单项。
5. `ForbiddenResearchRegistration`：按设计把新 ID 加到现有研究，或创建不重叠的新研究节点。
6. `SafeRemovalController`：发现、停用、清空存储、转换自定义资源并拆除新建筑。

不要只在建造菜单中隐藏建筑而忽略已建实例。配置关闭必须保证已有实例仍可反序列化，且内部资源不会消失。

### 4.4 实现 BuildingDef 和运行组件

建筑目录建议为 `src/Game/Buildings/<ModuleName>/`，至少包含 `<Name>Config.cs` 和 `<Name>.cs`。

实现时优先复用游戏原生组件：

- `ComplexFabricator` 处理配方和队列；
- `Storage` 保存实体材料，并明确过滤器、容量、UI 可见性和是否允许手动取出；
- `SolidConduitConsumer` / `SolidConduitDispenser` 处理固体物流；
- `ConduitConsumer` / `ConduitDispenser` 处理液体或气体；
- `Operational`、`LogicOperationalController` 和 `PoweredActiveController.Def` 处理运行条件；
- `CopyBuildingSettings` 和 `Prioritizable` 保持原版交互习惯。

反射访问游戏私有字段只能集中封装，并在字段缺失时明确抛出 `MissingFieldException`。不要静默继续，因为游戏更新后的静默数据错乱比明确启动失败更危险。

任何完成配方后会动态刷新列表的建筑，都必须防止在 `CompleteWorkingOrder` 期间替换队列。现有分析仪的做法是：捕获已完成配方、让基类完成、更新解锁、确认当前没有活动订单后再刷新，并处理重复队列、无限队列和失败完成。

### 4.5 本地化

每个建筑至少增加：

- `NAME`；
- `DESC`；
- `EFFECT`；
- 自动化端口的 `NAME`、`ACTIVE`、`INACTIVE`；
- 所有自定义状态项的名称和提示；
- 研究名称、描述和搜索词（如新增研究）。

键要同时存在于 `STRINGS.cs`、`zh.po` 和 `en.po`。Mod 入口必须继续使用当前已经验证的全局 `STRINGS` 注册方式，不要换回在当前游戏版本会扫描到空命名空间的 PLib 程序集扫描方案。

配置页文字直接来自 `ForbiddenTechOptions` 的中文 `Option` 特性；新增选项后必须用运行时本地化测试证明中文名称、说明和分组可读取。

### 4.6 KAnim 和建造缩略图

这是本项目最容易使游戏无法启动的部分。每个建筑应有：

```text
assets/scml/<module>/
├── baiye_<name>.scml
├── baiye_<name>_0.png
└── ui_0.png
```

并在 `assets/animation-manifest.json` 和 `build-assets.ps1` 的源目录映射中增加 `baiye_<name>`。

建筑动画至少要根据运行组件提供：

- `off`
- `idle`
- `working_pre`（非循环）
- `working_loop`
- `working_pst`
- `working_pst_complete`（非循环）
- `ui`

若代码会播放 `blocked`、`overheat`、`no_coolant` 或其他状态，也必须写入 SCML 和清单。代码、SCML、清单三者的名字必须完全一致。

关键命名契约：

- `IBuildingConfig` 引用 `baiye_<name>_kanim`；
- SCML 的 entity 名和编译文件基名使用 `baiye_<name>`，不要在这里追加 `_kanim`；
- 建造菜单专用源图片必须字面命名为 `ui_0.png`；
- SCML 动画名必须为 `ui`，timeline 名必须为 `ui_0`；
- 建筑本体源图使用 `baiye_<name>_0.png`，不要让 UI 图替代本体。

正确的编译输出布局是：

```text
packaging/anim/forbidden_technology/baiye_<name>/
├── baiye_<name>.png
├── baiye_<name>_anim.bytes
└── baiye_<name>_build.bytes
```

不要改成 `anim/assets`，不要把输出文件重命名为 `baiye_<name>_kanim_*`，也不要把空目录提交给打包流程。以上三种情况都曾导致 `Missing Anim`，严重时在世界加载阶段触发 `First anim file needs to be non-null` 并使游戏启动失败。

若要逆向检查编译结果，可在临时目录执行：

```powershell
tools\kanimal-cli.exe convert -I kanim -O scml -f <atlas.png> <build.bytes> <anim.bytes> -o <temp-directory>
```

然后确认逆向 SCML 中确实存在动画 `ui`、timeline `ui_0` 和图片 `ui_0.png`。

### 4.7 视觉风格和贴地规则

《缺氧》的建筑不是实时三维模型，而是 2D KAnim。目标应是与原版一致的 2.5D 手绘机械感，而不是追求真正 3D。为了避免“贴画感”，图片应具有：

- 明确的前、侧、顶面关系；
- 原版式粗轮廓、局部高光和环境阴影；
- 分层机械结构、管线、铆钉、面板和功能部件；
- 有主次的配色，禁忌科技的紫色/青色只作为发光强调；
- 与占地相符的视觉体量，不越出预计碰撞/占地范围。

贴地契约：

- 本体 sprite 在 SCML 中必须设置 `pivot_y="0"`，以底边为锚点；
- 图片最低的可见像素必须接近画布底边；
- 底部连续透明行最多 2 行；
- 不要靠修改建筑坐标或负偏移去补偿 PNG 内部的大块透明边距。

此前三个建筑底部曾分别残留 11、14、18 行透明像素，因而看起来悬空。当前资源均为 0 行，`AssetSourceContractTests.ps1` 会阻止大于 2 行的资源通过。

现有本体画布为 300×300、400×400、500×500，对应 3×3、4×4、5×5 建筑。新建筑不必机械照抄，但画布、占地和最终游戏内比例必须一同验证。

### 4.8 增加测试，再构建和安装

新增模块至少需要：

- Core 纯逻辑测试；
- ID、配置解析和注册计划测试；
- 源码/程序集契约测试；
- 动画源、动画状态和打包三件套测试；
- 中文配置/本地化测试；
- 存档与安全移除测试；
- `docs/test-matrix.md` 中的游戏内验收项。

推荐顺序：

```powershell
# 1. 不依赖本机游戏程序集的快速检查
.\test.ps1 -Suite Portable

# 2. 全部自动化测试
.\test.ps1 -Suite All -GamePath '<GamePath>'

# 3. 重新构建 DLL 与全部动画
.\build.ps1 -GamePath '<GamePath>'

# 4. 检查发布目录结构
.\verify-package.ps1

# 5. 最后才安装；测试和构建会重建 dist，所以不要提前安装旧产物
.\install.ps1 -GamePath '<GamePath>'

# 6. 准备发布时生成并二次解压验证 ZIP
.\pack-release.ps1 -GamePath '<GamePath>'
```

安装后应比较 `dist/ForbiddenTechnologyPack` 与本地 Mod 安装目录的文件数和 SHA256，确保测试的确是刚构建的版本。发布包应只有一个合并后的 `ForbiddenTechnologyPack.dll`，不能额外携带 `PLib.dll`。

## 5. 游戏启动与 DLC 验证流程

### 5.1 必须通过 Steam 启动

不要直接双击或直接运行 `OxygenNotIncluded.exe` 作为最终验证。直接启动曾出现 `Steam not initialized in time`，并伴随随机或误导性的 YAML 致命错误，无法代表玩家的实际启动方式。

应使用：

```powershell
Start-Process '<SteamPath>\steam.exe' -ArgumentList '-applaunch','457140' -WindowStyle Hidden
```

每次替换 DLL 或动画后都要完全退出游戏再启动。禁用 DLL Mod 后不完全重启，会继续保留旧程序集状态。

### 5.2 `mods.json` 的 DLC 启用规则

`enabled: true` 不保证当前内容模式会加载 Mod。`mods.json` 还会记录 `enabledForDlc`；例如当前为 Spaced Out! 模式时，需要包含 `EXPANSION1_ID`。游戏在连续崩溃后还可能自动禁用 Mod。

原则：

- `install.ps1` 永远不要自动编辑 `mods.json`；
- 测试前先完全关闭游戏；
- 如确需自动化临时修改，先逐字节备份原文件并记录 SHA256；
- 测试结束后逐字节恢复，再验证 SHA256 相同；
- 不要把个人 Mod 启用列表写入仓库或发布包。

### 5.3 日志判定

使用玩家目录中的 `Player.log` 判断加载结果。至少检查：

- 本 Mod DLL 是否加载；
- `Animation` 资源组是否加载；
- 是否出现与本 Mod ID、建筑 ID 或资源名有关的 `Missing Anim`、`NullReferenceException`、`TypeLoadException`、`YamlException`；
- 是否完成世界生成；
- 进入殖民地后是否才出现 UI、配方、建筑或存档错误。

`WORLDGEN COMPLETE` 只说明世界生成完成，不能排除随后出现的 Mod/UI/实体故障。反过来，日志中的无关 `Missing Anim` 也不能自动归因于本 Mod；应先将哈希与本 Mod 资源哈希对应，再判断来源。

## 6. 已知坑与正确处理

| 症状 | 已确认的常见原因 | 正确处理 | 不要这样做 |
|---|---|---|---|
| 游戏启动/载入失败，日志有 `First anim file needs to be non-null` | 某个动画目录为空，或输出布局/基名被改错 | 检查四/全部资源的 PNG、`_build.bytes`、`_anim.bytes` 三件套和清单 | 只删报错建筑的 C# 注册，或继续猜 DLL 问题 |
| 日志出现本 Mod 的 `Missing Anim` | Config 的 `_kanim` 名、SCML 基名和打包目录不符合契约 | 恢复 `forbidden_technology/<base-name>/<base-name>.*` 布局 | 把编译文件也重命名成 `_kanim` |
| 建造菜单是问号 | 没有独立 `ui` build symbol，或源图被命名成 `<name>_ui_0.png` | 使用 `ui_0.png` + 动画 `ui` + timeline `ui_0` | 用建筑本体动画代替专用 UI symbol |
| 建筑像平面贴纸 | 资源本身缺乏 2.5D 体积、层次、明暗与原版机械细节 | 修改原创本体图，保持 KAnim 2D 工作流 | 试图给 ONI 加实时 3D 模型 |
| 建筑悬空 | PNG 底部透明边距过大，或 pivot 不是底部 | `pivot_y="0"`，最低可见像素到画布底部，透明行 ≤ 2 | 用负坐标硬拉建筑，留下错误源图 |
| 建筑超出砖块/占地 | 画布内容与 BuildingDef 尺寸不匹配 | 同时核对画布、SCML 缩放、占地和游戏截图 | 只缩建造图标或只改 BuildingDef |
| 修改资源后游戏仍显示旧内容 | 只构建未安装、先安装后测试又被测试重建，或游戏未完全退出 | 最终测试/构建后再安装并比对哈希，完全重启 | 在运行中的游戏上反复覆盖 DLL |
| Mod 显示启用但当前 DLC 不加载 | `enabledForDlc` 不包含当前内容 ID，或崩溃后被自动禁用 | 关闭游戏后检查 `mods.json` 当前模式条目 | 让安装脚本永久改用户 Mod 列表 |
| 直接运行 EXE 出现 Steam/YAML 致命错误 | 未通过 Steam 初始化，结果不可靠 | 使用 Steam `-applaunch 457140` | 把这类错误当作已确认的 Mod YAML 缺陷 |
| 命令行程序集探针异常 | 探针触发 Unity 静态初始化/ECall | 将逻辑改为纯函数；仅对特定 `TypeInitializationException` + `SecurityException` 做窄回退 | 捕获所有异常并吞掉，掩盖游戏内真故障 |
| 分析完成后队列重复、丢单或空转 | 在基类完成流程中动态替换 recipe list | 完成后解锁，活动订单为空时再刷新，并测有限/无限重复队列 | 用宽泛 Harmony postfix 无条件刷新 |
| 运输轨道吃掉不支持的材料 | 原生 consumer 先取包，过滤发生得太晚 | 在 `ConduitUpdate` 前按实际包元素做局部前置过滤 | 只依赖 Storage filter 证明不会吞包 |
| 编译器输出堵塞时重复产物或丢失 | 完成、输出容量和保存时机没有作为一个状态处理 | 测阻塞、解堵、保存/读取后恰好一个批次 | 只测正常空输出口 |
| 配置界面出现英文/键名 | 新选项或 STRINGS/PO 未同步，或注册方式改变 | 保持全局 STRINGS 注册并跑中文本地化探针 | 只在 README 中写中文名称 |
| 编译有 `STRINGS` CS0437 警告 | 本 Mod 全局 `STRINGS` 与游戏命名空间同名 | 当前属于已知非致命技术债，确认没有升级为错误即可 | 为消除警告随意改变已验证的本地化根结构 |
| 自动化测试全绿但游戏内坏了 | 探针无法替代 Unity 场景、物流、UI 和序列化 | 执行测试矩阵并保留 Player.log/截图/观察记录 | 把单元测试数当作游戏验收结论 |
| 安全移除后仍无法停用 Mod | 还有原质、建筑或序列化组件残留 | 两次确认、转换/拆除、另存、重载验证后再停用 | 直接关 Mod 后尝试修复坏档 |

## 7. 安全移除与存档兼容要求

任何新建筑或新元素都会扩大安全移除范围。新增内容后必须：

1. 将新建筑加入安全移除扫描清单。
2. 停止工作和队列，返还未消耗输入。
3. 对所有输入、制作中存储、输出和专用存储分别处理。
4. 将新自定义元素转换为明确的原版替代物，质量、温度和疾病信息尽量保持。
5. 只有所有自定义对象为零时才标记完成并隐藏建造/研究入口。
6. 在游戏内另存为新存档，重新载入成功后，才允许测试关闭 Mod。

不要仅因为 `SafeRemovalRuntimeTests` 通过就宣称安全移除完成。实际存档的 loose object、普通 Storage、轨道包、制作中材料和已建建筑都要验证。

## 8. 第二阶段每个建筑的完成定义

一个新建筑只有满足以下条件才能算完成：

- 设计契约已记录，稳定 ID 已确定；
- 中文配置开关、中文/英文翻译齐全；
- 关闭模块后建造入口正确隐藏，已有实例仍安全；
- 建筑本体、UI 缩略图、所有代码会播放的动画状态齐全；
- 视觉风格与原版协调，建筑贴地且不越界；
- 输入、输出、容量、功耗、发热、自动化和物流行为符合设计；
- 断电、自动化关闭、输入不足、输出堵塞、拆除、保存/读取均无复制或丢失；
- 自定义材料可以被安全移除；
- Portable、All、build、verify、install 和哈希核对通过；
- 至少在原版与所有声称支持的 DLC 内容模式中完成建造和运行测试；
- `Player.log` 无本模块导致的重复异常或致命错误；
- `docs/test-matrix.md` 有明确证据，而不是仅写“应该可以”。

## 9. 当前仍需优先补齐的游戏内验收

在大规模加入第二阶段建筑前，建议先完成第一阶段以下验证，以免新问题与旧问题叠加：

1. 三个建筑在实际建造菜单中的中文名称和缩略图。
2. 三个建筑在不同地砖、缩放和背景下的贴地、占地与原版风格。
3. 分析一种普通固体、重复分析过滤和保存后解锁保留。
4. 粉碎机手动输入、轨道输入/输出、错误材料、输出堵塞。
5. 编译器冷却液临界温度、液体出口堵塞、固体输出堵塞和保存/读取。
6. 各预设、自定义上下限、单建筑开关和重启持久化。
7. 原版、Spaced Out!、Frosty Planet Pack、Bionic Booster Pack 及组合内容模式。
8. 完整安全移除：转换原质、拆除建筑、另存、重载、停用 Mod。
9. 10 台粉碎机与 10 台编译器运行 20 个周期的压力测试。

## 10. 给后续 GPT 的开工提示词

可将下面内容连同仓库交给后续 GPT：

> 继续开发 `ForbiddenTechnologyPack` 的第二阶段。先完整阅读 `docs/phase-2-development-guide.md`、`docs/test-matrix.md` 和当前代码，不要用旧计划示例覆盖现有实现。保持单一 Mod、稳定 ID、同一个中文 PLib 配置页，并支持原版和全部 DLC。开始前先检查工作区状态，保留所有非本任务改动；若仓库存在 `.codegraph`，先使用 CodeGraph 理解代码。先为本次新建筑写最小设计契约和失败测试，再实现 Core 规则、游戏组件、注册、本地化、KAnim、中文配置、安全移除和游戏内矩阵。KAnim 必须遵守 `forbidden_technology/<base-name>/<base-name>.*` 布局，Config 才使用 `<base-name>_kanim`；缩略图必须是 `ui_0.png`、动画 `ui`、timeline `ui_0`；本体 `pivot_y=0` 且底部透明行不超过 2。不要直接启动游戏 EXE，最终验证通过 Steam，并检查 `Player.log`。自动化测试通过不等于游戏内验收通过，不得伪造 DLC、存档、物流或视觉验证结果。

后续 GPT 在每次交付时应明确分开说明：已经由自动化证明的内容、已经在游戏内观察到的内容，以及仍为 PENDING 的内容。
