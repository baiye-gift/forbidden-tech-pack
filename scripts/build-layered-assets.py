"""Deterministic atlas slicing, layout and SCML compilation source preparation.

The art itself is the original AI-generated source-atlas.png, not procedural art.
Requires Pillow. Produces editable sprite files, SCML and illustrative previews.
"""
from pathlib import Path
import math
import xml.etree.ElementTree as ET
from PIL import Image
from collections import deque

ROOT = Path(__file__).resolve().parents[1]
ATLAS = Image.open(ROOT / 'assets/source-atlas.png').convert('RGBA')
NAMES = ['matter_analyzer', 'mass_crusher', 'matter_compiler']
BOXES = [
    [(0, 0, 470, 420), (470, 0, 900, 420), (900, 0, 1280, 420)],
    [(0, 420, 500, 835), (500, 420, 845, 835), (845, 420, 1280, 835)],
    [(0, 835, 485, 1280), (485, 835, 850, 1280), (850, 835, 1280, 1280)],
]
SIZES = [300, 400, 500]
PART_NAMES = [('scanner', 'sample_tray'), ('rotor', 'press'), ('emitter', 'output_hatch')]
TARGETS = [[(142, 90), (115, 50)], [(185, 185), (140, 57)], [(235, 235), (170, 72)]]
POSITIONS = [[(150, 156), (150, 62)], [(200, 150), (294, 150)], [(250, 212), (250, 62)]]
PREVIEW_DIR = ROOT / 'assets/preview'
PREVIEW_DIR.mkdir(exist_ok=True)

def main_component_bbox(alpha):
    # Generated atlas edges can contain a disconnected sliver from the next cell.
    # Determine the sprite bounds from its main connected silhouette; source art
    # remains untouched and this does not paint or invent any image pixels.
    w,h=alpha.size; pixels=bytearray(alpha.point(lambda v:255 if v>32 else 0).tobytes())
    largest=0; result=None
    for seed in range(w*h):
        if not pixels[seed]:continue
        pixels[seed]=0; pending=deque([seed]); count=0; xmin=w; ymin=h; xmax=ymax=0
        while pending:
            i=pending.popleft(); y,x=divmod(i,w);count+=1
            xmin=min(xmin,x);xmax=max(xmax,x);ymin=min(ymin,y);ymax=max(ymax,y)
            for other in ((i-1 if x else -1),(i+1 if x+1<w else -1),(i-w if y else -1),(i+w if y+1<h else -1)):
                if other>=0 and pixels[other]:pixels[other]=0;pending.append(other)
        if count>largest:largest=count;result=(xmin,ymin,xmax+1,ymax+1)
    return result

def crop(box):
    w, h = ATLAS.size
    box = tuple(round(v * (w if i % 2 == 0 else h) / 1280) for i, v in enumerate(box))
    img = ATLAS.crop(box)
    bbox = main_component_bbox(img.getchannel('A'))
    if not bbox: raise ValueError(f'Empty atlas cell {box}')
    return img.crop(bbox)

def fit(img, size):
    img = img.copy(); img.thumbnail(size, Image.Resampling.LANCZOS)
    return img

def transforms(row, phase):
    (x1, y1), (x2, y2) = POSITIONS[row]
    if row == 0:
        x1 += 18 * math.sin(2 * math.pi * phase)
        return [(x1, y1, 0), (x2, y2, 0)]
    if row == 1:
        x2 -= 13 * (1 - math.cos(2 * math.pi * phase))
        return [(x1, y1, 360 * phase), (x2, y2, 0)]
    return [(x1, y1, -180 * phase), (x2, y2 + 5 * math.sin(2 * math.pi * phase), 0)]

def compose(row, sprites, phase):
    s = SIZES[row]; result = sprites[0].copy()
    for img, (x, y, angle) in zip(sprites[1:], transforms(row, phase)):
        rotated = img.rotate(angle, Image.Resampling.BICUBIC, expand=True)
        result.alpha_composite(rotated, (round(x-rotated.width/2), round(s-y-rotated.height/2)))
    return result

previews = []
for row, module in enumerate(NAMES):
    name = 'baiye_' + module; folder = ROOT / 'assets/scml' / module; s = SIZES[row]
    shell = fit(crop(BOXES[row][0]), (s, s))
    body = Image.new('RGBA', (s, s)); body.alpha_composite(shell, ((s-shell.width)//2, s-shell.height))
    body.save(folder / (name + '_0.png'))
    parts = [fit(crop(BOXES[row][i+1]), TARGETS[row][i]) for i in range(2)]
    for img, symbol in zip(parts, PART_NAMES[row]): img.save(folder / (symbol + '_0.png'))
    sprites = [body] + parts
    assembled = compose(row, sprites, 0)
    icon = fit(assembled, (128,128)); ui = Image.new('RGBA',(128,128))
    ui.alpha_composite(icon, ((128-icon.width)//2,(128-icon.height)//2));ui.save(folder / 'ui_0.png')
    assembled.save(PREVIEW_DIR / (module + '.png')); previews.append(assembled)
    frames = [compose(row, sprites, i / 24) for i in range(24)]
    # GIF is an authoring preview, never used as a game asset.
    bg = []
    for frame in frames:
        canvas = Image.new('RGB',(s,s),'#243039');canvas.paste(frame,mask=frame.getchannel('A'));bg.append(canvas)
    bg[0].save(PREVIEW_DIR/(module+'.gif'),save_all=True,append_images=bg[1:],duration=80,loop=0)
    root = ET.Element('spriter_data',scml_version='1.0',generator='ForbiddenTechnologyPack',generator_version='2.0')
    f = ET.SubElement(root,'folder',id='0')
    filenames = [name+'_0.png'] + [p+'_0.png' for p in PART_NAMES[row]] + ['ui_0.png']
    for i, (img, filename) in enumerate(zip(sprites+[ui],filenames)):
        ET.SubElement(f,'file',id=str(i),name=filename,width=str(img.width),height=str(img.height),pivot_x='0.5',pivot_y='0' if i==0 else '0.5')
    entity = ET.SubElement(root,'entity',id='0',name=name)
    states=['off','idle','working_pre','working_loop','working_pst','working_pst_complete']
    states += [['overheat'],['blocked'],['no_coolant','blocked']][row]+['ui']
    for aid, state in enumerate(states):
        working = state == 'working_loop'; transition = state in ('working_pre','working_pst','working_pst_complete')
        count=24 if working else 8 if transition else 1
        length=1920 if working else 400 if transition else 800
        anim=ET.SubElement(entity,'animation',id=str(aid),name=state,length=str(length),looping='true' if state in ('idle','working_loop','blocked','overheat','no_coolant') else 'false')
        ml=ET.SubElement(anim,'mainline')
        for k in range(count):
            key=ET.SubElement(ml,'key',id=str(k),time=str(round(k*length/count)))
            for tid in range(1 if state=='ui' else 3):
                ET.SubElement(key,'object_ref',id=str(tid),timeline=str(tid),key=str(k),z_index=str(tid))
        for tid in range(1 if state=='ui' else 3):
            symbol='ui_0' if state=='ui' else 'body' if tid==0 else PART_NAMES[row][tid-1]
            timeline=ET.SubElement(anim,'timeline',id=str(tid),name=symbol)
            for k in range(count):
                phase=k/count if working else (k/(count-1)*.08 if transition else 0)
                if state in ('working_pst','working_pst_complete'):phase=.08-phase
                key=ET.SubElement(timeline,'key',id=str(k),time=str(round(k*length/count)))
                x,y,angle=(0,0,0) if state=='ui' or tid==0 else transforms(row,phase)[tid-1]
                if state!='ui' and tid>0:x-=s/2
                ET.SubElement(key,'object',folder='0',file='3' if state=='ui' else str(tid),x=f'{x:.4f}',y=f'{y:.4f}',angle=f'{angle%360:.4f}',scale_x='1',scale_y='1',a='1')
    ET.indent(root,space='  ')
    ET.ElementTree(root).write(folder/(name+'.scml'),encoding='UTF-8',xml_declaration=True)

sheet=Image.new('RGB',(1200,500),'#243039')
for i,img in enumerate(previews):
    img=fit(img,(380,450));sheet.paste(img,(i*400+(400-img.width)//2,480-img.height),img)
sheet.save(PREVIEW_DIR/'buildings.png')
print('Layered sprites, SCML and authoring previews generated.')
