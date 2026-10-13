# 原创分层资源

`source-atlas.png` 于 2026-10-11 使用图像生成工具创作，来源为本项目的三台物质设备分层需求，无原版图集复制。三行分别为分析仪、粉碎机、编译器；各行固定外壳、工作部件、辅助部件。

安装 Pillow 后，在工程根运行 `python scripts/build-layered-assets.py` 可从图集重建 PNG、SCML、菜单图标及作者预览。脚本内有明确裁切框、尺寸及部件位置，可作为制作入口。只有修改源图或布局才需要执行此步骤；常规构建直接编译已提交的 SCML。

随后执行 `build-assets.ps1` 或 `build.ps1 -GamePath <游戏目录>` 重建 `packaging/anim`。工具保持既有 KAnim 基名及 ui_0 symbol。源图不含原版人物或游戏 DLL。

`preview/buildings.png` 与三个 GIF 为部件装配/运动预览，不是游戏截图。冷却管颜色不作为真实料位；工作状态由原生制作状态机控制。

`mass-energy-atlas.png` 现仅用于原质实体与菜单图符。两座质能建筑使用 `annihilation-v2-atlas.png`、`condenser-v2-atlas.png`，分别提供屏蔽腔/闸板/注入机构、横向转换通道/感应组件/压坯机构；取消原来与编译器相似的环形机器构图。

五台设备按用户新要求统一粗描边、简化明暗与暖白陶瓷/深色钢结构，去掉固定假液位罐、发光装饰条和伪料位屏。保留机械通风口、绝缘管、导线和物理镜片。未提供真实料位绑定的部件不表现液面或填充条；库存由原生侧栏显示。生成提示与来源见 `mass-energy-v2-prompts.md`。

运行 `python scripts/build-mass-energy-assets.py` 重建两台新建筑、原质 PNG/SCML、菜单图标、静态预览及 GIF。原质保持 `baiye_proto_matter` 基名，增加独立 `ui` 符号。`preview/mass-energy-buildings.png` 与 `preview/proto-matter-icons.png` 仅用于作者验看；原质小图展示 128/64/32/24 像素辨识度，不替代游戏验收。
