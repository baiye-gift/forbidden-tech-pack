# Debug Session: forbidden-tech-new-error
- **Status**: [OPEN]
- **Issue**: ForbiddenTechnologyPack 在修复启动闪退后出现新的运行时报错，具体症状待基于最新日志确认。
- **Debug Server**: Pending startup
- **Log File**: .dbg/trae-debug-log-forbidden-tech-new-error.ndjson

## Reproduction Steps
1. 启动《缺氧》并启用 ForbiddenTechnologyPack。
2. 进入主界面或存档。
3. 观察新的红字、报错弹窗、功能异常或再次崩溃的位置。

## Hypotheses & Verification
| ID | Hypothesis | Likelihood | Effort | Evidence |
|----|------------|------------|--------|----------|
| A | 新报错来自建筑注册链，某个 BuildingDef、Tech 或字符串键仍未完全对齐 | High | Low | Pending |
| B | 新报错来自动画/资源加载链，虽然已能启动，但某个运行时资源仍缺失或命名不一致 | High | Low | Pending |
| C | 新报错来自元素/配方/数据库初始化顺序，进入存档或打开菜单时才触发 | Medium | Medium | Pending |
| D | 新报错不是本 mod 自身，而是被当前模组组合或残留缓存放大 | Medium | Low | Pending |
| E | 新报错来自我们上轮修复后的安装产物与源码不一致，实际运行的 DLL/资源不是当前版本 | Medium | Low | Pending |

## Log Evidence
- `Player.log` shows the mod now loads successfully (`[ForbiddenTechnologyPack] 0.1.0 loaded`), so the previous startup crash is fixed.
- The new failure happens later in UI search cache generation:
  - `ResearchScreenSideBar.OnSpawn -> SearchUtil.CacheTechs_Patch1 -> SearchUtil.CanonicalizePhrase`
  - `PlanScreen.OnSpawn -> SearchUtil.MakeBuildingDefCache -> SearchUtil.CanonicalizePhrase`
- This means at least one search-indexed string (`tech.desc`, `techItem.description`, `def.Desc`, `def.Effect`, or recipe description) is null at runtime.
- Animation warnings still exist (`ui placeSymbol [ ui ] is missing`) but they are non-fatal and occur before the actual NullReference.
- A Ronivan legacy mod patch appears in the stack trace for `SearchUtil.CacheTechs_Patch1`, so cross-mod interaction remains a live hypothesis.

## Verification Conclusion
- A: Confirmed. Runtime logs show our custom research/building strings were resolving to `MISSING.STRINGS...`, and the actual null crash path is the custom recipe `description` field left unset.
- B: Rejected as the primary cause of the new error. Current evidence points to search cache text, not startup animation loading.
- C: Confirmed as the failing stage. The error occurs during tech/building search cache initialization after the game has already started.
- D: Rejected as the root cause. Another mod patches `SearchUtil`, but our own recipe data already provides the null that triggers the crash.
- E: Confirmed earlier for diagnostics only. The instrumented build did run from the installed local mod path, so we verified the live assembly location directly.
