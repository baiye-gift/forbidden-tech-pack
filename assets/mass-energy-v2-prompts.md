# 质能建筑第二版素材来源

2026-10-11，使用内置 imagegen，透明背景。原始生成结果复制到项目中，原图保存；Python 仅做确定性切片、镜像、缩放、SCML 编排和预览合成。

## 湮灭炉

源：`annihilation-v2-atlas.png`。提示词：

> Transparent landscape three-part 2D Oxygen Not Included industrial sprite atlas. Fixed shell of a compact 5x5 proto-matter annihilation reactor: rectangular heavy axial shield chamber, angular segmented ceramic armor, dark graphite steel ribs, amber hazard accents, asymmetric right heat-exchanger manifold, rectangular bottom energy converter, paired opposed feed sockets. Narrow vertical black rectangular cavity; no large circular portal, glowing sphere, twin transparent bottles, circular pedestal, labels or glow clouds. Separate tall segmented shield blade and horizontal fuel injector piston; isolated transparent gutters, crisp thick outlines, straight-on side view, no perspective distortion. Fixed shell has empty working-part sockets.

## 凝聚器

源：`condenser-v2-atlas.png`。提示词：

> Transparent landscape three-part 2D Oxygen Not Included industrial sprite atlas. Fixed shell of a compact 4x4 energy-matter condenser: horizontal stepped rectangular energy conversion tunnel from a ribbed left electrical transformer to an enclosed right condensation chamber, lower collection press socket and solid output drawer. Ivory ceramic, navy steel, copper windings, muted violet conduits and compact cyan coolant fittings. Separate horizontal travelling induction carriage and vertical downward collection press ram. No circular window, spherical core, floating hexagonal crystal, twin tall glass columns or round pedestal. Isolated transparent sprites, crisp dark outlines, flat side view, no labels or perspective distortion.

布局以实际生成图的部件边界切片，不使用提示词请求的区域作为已经满足的保证。完整运行预览见 `preview/mass-energy-buildings.png` 及两份 GIF；它们不是游戏截图。

## 五台建筑统一画风编辑

使用内置 imagegen 编辑 `source-atlas.png` 和上述两张质能图集，保持各部件所在切片、比例、孔洞、透明间隔与机械用途。提示要求：符合 Oxygen Not Included 原生粗描边、宽机械结构和两三层平面明暗；减少写实划痕和细小螺钉；暖白陶瓷、深色钢结构、铜色线圈。全部固定料位/液位/长发光条改为实体面板、通风口、绝缘管和机械夹具。编译器双青色满液罐改为不透明管路；凝聚器青色三条槽改为通风栅、液管改为不透明管；湮灭炉琥珀灯条改为通风口与短警示斜纹。紫色用作哑光绝缘材料，扫描头仅保留小型物理镜片。没有库存驱动的部件不绘制液面或填充条。

编译器发射环另做定向编辑：四条紫色长亮条改为深色夹具/通风口，内圈改为无光晕的哑光紫色绝缘边，其他八部件不变。最终源文件仍使用原名，由原有切片脚本重建全部 PNG/SCML/图标/预览。
