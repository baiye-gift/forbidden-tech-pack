# Debug Session: build-menu-missing-icons
- **Status**: [OPEN]
- **Issue**: ForbiddenTechnologyPack 的 `Matter Analyzer`、`Mass Crusher`、`Matter Compiler` 在建造界面显示问号图标，而不是正常图标。
- **Expected**: 建造界面显示对应建筑的可识别图标。
- **Actual**: 建造界面显示缺失图标占位符。

## Reproduction Steps
1. 启动《缺氧》并启用 ForbiddenTechnologyPack。
2. 打开建造界面对应分类。
3. 观察 `Matter Analyzer`、`Mass Crusher`、`Matter Compiler` 的条目图标。

## Hypotheses
| ID | Hypothesis | Likelihood | Effort | Expected Signal |
|----|------------|------------|--------|-----------------|
| A | 建筑动画缺少 UI 预览所需的 `ui`/`placeSymbol` 符号，建造菜单因此回退到问号图 | High | Low | `Player.log` 出现 `ui placeSymbol [ ui ] is missing` 或同类 UI 符号缺失日志 |
| B | 打包后的 KAnim 文件名正确，但内部 build/symbol 名称与 `BuildingDef.AnimFiles` 期望不一致 | High | Medium | 日志有动画加载成功但 UI 取图失败；或资源内名称与代码引用不一致 |
| C | 这三个建筑没有设置专用菜单图标，游戏默认从动画取图，而当前动画首帧不适合作为菜单图 | Medium | Low | 代码里无显式图标注册；建筑运行正常但菜单只缺图 |
| D | 安装目录里实际运行的动画资源不是当前源码生成版本，导致 UI 仍在读旧资源 | Medium | Low | 本地 mod 目录与 `dist/packaging` 内容不一致 |
| E | 问号图来自别的 mod/UI 补丁冲突，不是本 mod 动画本身的问题 | Low | Medium | 禁用其他 UI 相关 mod 后消失，或日志指向其他 mod 覆盖建造按钮渲染 |

## Evidence
- `Player.log` 明确出现：
  - `baiye_matter_analyzer_kanim ui placeSymbol [ ui ] is missing`
  - `baiye_mass_crusher_kanim ui placeSymbol [ ui ] is missing`
  - `baiye_matter_compiler_kanim ui placeSymbol [ ui ] is missing`
- 游戏反编译代码显示建造菜单图标来自 `BuildingDef.GetUISprite()` -> `Def.GetUISpriteFromMultiObjectAnim(..., "ui")`，会直接查找符号名 `ui`。
- 三个 SCML 源文件均只有 `body` timeline，没有 `ui` timeline，因此与运行时取图规则不匹配。
- 已为 3 个 SCML 补入 `ui` timeline，并完成本地构建，构建成功。
- 自动安装步骤被沙箱限制拦截，未能直接写入 `C:\Users\Administrator\Documents\Klei\OxygenNotIncluded\mods\local\ForbiddenTechnologyPack`。

## Conclusion
- A: ✅ Confirmed。根因是动画资源缺少建造菜单所需的 `ui` 符号。
- B: ❌ Rejected。不是文件名/目录不匹配，KAnim 文件已正确打包。
- C: ✅ Confirmed。建造菜单确实依赖默认 `ui` 符号取图。
- D: ⏳ Inconclusive。当前还未把修复后的产物成功装进本地 mod 目录验证。
- E: ❌ Rejected。现有证据已足以定位到本 mod 自身资源定义问题。
