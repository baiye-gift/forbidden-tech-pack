# Forbidden Technology Pack 开发文档索引

本目录是当前开发状态的唯一入口。继续开发、交接或网络中断后恢复时，**先读本文件，再读 `phase-2-progress.md`**，不要仅依赖聊天记录或提交信息推断进度。

## 当前开发分支

`feature/baiye-20260917-phase2-protomatter-field`

## 文档职责

| 文档 | 作用 | 是否持续更新 |
| --- | --- | --- |
| `phase-2-progress.md` | **当前进度、验证状态、最后断点、下一步动作** | 是，开发过程中持续更新 |
| `phase-2-protomatter-field-development.md` | 第二阶段总体架构、世界观、三台建筑与原质干扰系统 | 架构变化时更新 |
| `phase-2-development-guide.md` | 项目长期开发约束、代码边界、测试与安全移除规范 | 规则变化时更新 |
| `superpowers/specs/2026-09-17-matter-reconstructor-design.md` | 物质重构器最终设计规格 | 设计变化时更新 |
| `superpowers/plans/2026-09-17-matter-reconstructor.md` | 物质重构器任务级实施计划 | 实施方案变化时更新 |
| `test-matrix.md` | 游戏内、DLC、存档、安全移除和性能测试矩阵 | 每轮实际验证后更新 |

## 开发恢复顺序

发生网络中断、会话丢失或换人接手时，按以下顺序恢复：

1. 读取 `docs/phase-2-progress.md`，确认当前任务状态和精确下一步。
2. 检查当前 Git 分支、HEAD 和工作区：`git branch --show-current`、`git log -1 --oneline`、`git status`。
3. 如果 HEAD 与进度文档记录不一致，先比较差异，不直接重复执行计划任务。
4. 读取当前建筑的 spec 与 implementation plan。
5. 只从进度文档中第一个未完成/未验证的步骤继续。
6. 每完成一个有意义的开发批次或一次验证，都同步更新 `phase-2-progress.md`。

## 状态定义

进度文档统一使用以下状态，避免“写了代码”和“已经验证”混在一起：

- `NOT_STARTED`：尚未开始。
- `IN_PROGRESS`：正在开发，可能存在失败测试或中间提交。
- `CODE_DONE`：代码/资源已经实现，但尚未经过对应验证。
- `PORTABLE_VERIFIED`：GitHub Portable/Core/源码契约验证通过。
- `LOCAL_VERIFIED`：用户本机 ONI DLL 的 `test.ps1 -Suite All` / build 验证通过。
- `IN_GAME_VERIFIED`：已经在实际游戏中按测试矩阵验证。
- `BLOCKED`：存在明确阻塞，并在进度文档中记录原因。

**只有达到相应验证级别，才能在进度文档中写对应的“已验证”。**
