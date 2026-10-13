"""Deterministic atlas slicing, layout and SCML compilation source preparation.

The art comes from the project-bound AI-generated sprite atlases, not procedural art.
Requires Pillow. Produces editable sprite files, SCML and illustrative previews.
"""
from pathlib import Path
import math
import xml.etree.ElementTree as ET
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
ATLAS = Image.open(ROOT / 'assets/mass-energy-atlas.png').convert('RGBA')
NEW_ATLASES = [Image.open(ROOT / ('assets/' + name)).convert('RGBA') for name in
               ['annihilation-v2-atlas.png', 'condenser-v2-atlas.png']]
NAMES = ['proto_annihilation_reactor', 'energy_matter_condenser']
SIZES = [500, 400]
PART_NAMES = [('shield_left', 'shield_right', 'injector_left', 'injector_right'), ('induction_carriage', 'collection_ram')]
TARGETS = [[(45, 205), (45, 205), (105, 55), (105, 55)], [(102, 48), (68, 86)]]
POSITIONS = [[(190, 218), (246, 218), (100, 424), (336, 424)], [(203, 156), (319, 112)]]
PREVIEW_DIR = ROOT / 'assets/preview'
PREVIEW_DIR.mkdir(exist_ok=True)

def crop(box, atlas=ATLAS, normalized=True):
    w, h = atlas.size
    if normalized:
        box = tuple(round(v * (w if i % 2 == 0 else h) / 1280) for i, v in enumerate(box))
    img = atlas.crop(box)
    bbox = img.getchannel('A').point(lambda v: 255 if v > 32 else 0).getbbox()
    if not bbox: raise ValueError(f'Empty atlas cell {box}')
    return img.crop(bbox)

def fit(img, size):
    img = img.copy(); img.thumbnail(size, Image.Resampling.LANCZOS)
    return img

def transforms(row, phase, state='working_loop'):
    if row == 0:
        active = state in ('working_loop', 'charging_loop')
        closure = 1 if active else phase if state == 'working_pre' else 1-phase if state in ('working_pst','working_pst_complete') else 0
        stroke = 5 * (1-math.cos(2*math.pi*phase)) if active else 0
        (xl,yl),(xr,yr),(il,iy),(ir,_) = POSITIONS[row]
        return [(xl+8*closure,yl,0),(xr-8*closure,yr,0),(il+stroke,iy,0),(ir-stroke,iy,0)]
    (x1,y1),(x2,y2)=POSITIONS[row]
    return [(x1+15*math.sin(2*math.pi*phase),y1,0),(x2,y2-5*(1-math.cos(2*math.pi*phase)),0)]

def compose(row, sprites, phase):
    s = SIZES[row]; result = sprites[0].copy()
    for img, (x, y, angle) in zip(sprites[1:], transforms(row, phase)):
        rotated = img.rotate(angle, Image.Resampling.BICUBIC, expand=True)
        result.alpha_composite(rotated, (round(x-rotated.width/2), round(s-y-rotated.height/2)))
    return result

previews = []
for row, module in enumerate(NAMES):
    name = 'baiye_' + module; folder = ROOT / 'assets/scml' / module; folder.mkdir(exist_ok=True); s = SIZES[row]
    atlas=NEW_ATLASES[row]
    boxes=[[(0,0,985,1024),(1050,0,1536,695),(990,700,1536,1024)],
           [(0,120,1065,930),(1070,180,1536,460),(1070,500,1536,980)]][row]
    shell = fit(crop(boxes[0],atlas,False), (s, s))
    body = Image.new('RGBA', (s, s)); body.alpha_composite(shell, ((s-shell.width)//2, s-shell.height))
    body.save(folder / (name + '_0.png'))
    raw_parts=[crop(box,atlas,False) for box in boxes[1:]]
    if row==0:
        raw_parts=[raw_parts[0],ImageOps.mirror(raw_parts[0]),ImageOps.mirror(raw_parts[1]),raw_parts[1]]
    parts = [fit(img, target) for img,target in zip(raw_parts,TARGETS[row])]
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
    states += ['no_coolant','blocked','overheat','ui']
    if row==0:states.append('charging_loop')
    for aid, state in enumerate(states):
        working = state in ('working_loop','charging_loop'); transition = state in ('working_pre','working_pst','working_pst_complete')
        count=24 if working else 8 if transition else 1
        length=1920 if working else 400 if transition else 800
        anim=ET.SubElement(entity,'animation',id=str(aid),name=state,length=str(length),looping='true' if working or state in ('idle','blocked','overheat','no_coolant') else 'false')
        ml=ET.SubElement(anim,'mainline')
        for k in range(count):
            key=ET.SubElement(ml,'key',id=str(k),time=str(round(k*length/count)))
            for tid in range(1 if state=='ui' else len(sprites)):
                ET.SubElement(key,'object_ref',id=str(tid),timeline=str(tid),key=str(k),z_index=str(tid))
        for tid in range(1 if state=='ui' else len(sprites)):
            symbol='ui_0' if state=='ui' else 'body' if tid==0 else PART_NAMES[row][tid-1]
            timeline=ET.SubElement(anim,'timeline',id=str(tid),name=symbol)
            for k in range(count):
                phase=k/count if working else (k/(count-1) if transition else 0)
                key=ET.SubElement(timeline,'key',id=str(k),time=str(round(k*length/count)))
                x,y,angle=(0,0,0) if state=='ui' or tid==0 else transforms(row,phase,state)[tid-1]
                if state!='ui' and tid>0:x-=s/2
                ET.SubElement(key,'object',folder='0',file=str(len(sprites)) if state=='ui' else str(tid),x=f'{x:.4f}',y=f'{y:.4f}',angle=f'{angle%360:.4f}',scale_x='1',scale_y='1',a='1')
    ET.indent(root,space='  ')
    ET.ElementTree(root).write(folder/(name+'.scml'),encoding='UTF-8',xml_declaration=True)

sheet=Image.new('RGB',(800,500),'#243039')
for i,img in enumerate(previews):
    img=fit(img,(380,450));sheet.paste(img,(i*400+(400-img.width)//2,480-img.height),img)
sheet.save(PREVIEW_DIR/'mass-energy-buildings.png')
print('Layered sprites, SCML and authoring previews generated.')

# Physical pickup and separate UI silhouette: keep the existing element KAnim ID.
folder=ROOT/'assets/scml/proto_matter'
name='baiye_proto_matter'
icons=[fit(crop((0,885,495,1280)),(92,92)),fit(crop((495,885,840,1280)),(92,92))]
for index,img in enumerate(icons):
    canvas=Image.new('RGBA',(96,96));canvas.alpha_composite(img,((96-img.width)//2,(96-img.height)//2))
    canvas.save(folder/(name+'_0.png' if index==0 else 'ui_0.png'))
    icons[index]=canvas
root=ET.Element('spriter_data',scml_version='1.0',generator='ForbiddenTechnologyPack',generator_version='3.0')
f=ET.SubElement(root,'folder',id='0')
for i,filename in enumerate([name+'_0.png','ui_0.png']):
    ET.SubElement(f,'file',id=str(i),name=filename,width='96',height='96',pivot_x='0.5',pivot_y='0.5')
e=ET.SubElement(root,'entity',id='0',name=name)
for i,state in enumerate(['idle','ui']):
    a=ET.SubElement(e,'animation',id=str(i),name=state,length='800',looping='true')
    ml=ET.SubElement(a,'mainline');k=ET.SubElement(ml,'key',id='0',time='0')
    ET.SubElement(k,'object_ref',id='0',timeline='0',key='0',z_index='0')
    tl=ET.SubElement(a,'timeline',id='0',name='body' if i==0 else 'ui_0')
    k=ET.SubElement(tl,'key',id='0',time='0')
    ET.SubElement(k,'object',folder='0',file=str(i),x='0',y='0',angle='0',scale_x='1',scale_y='1',a='1')
ET.indent(root,space='  ');ET.ElementTree(root).write(folder/(name+'.scml'),encoding='UTF-8',xml_declaration=True)
preview=Image.new('RGB',(560,180),'#243039')
for x,size in [(20,128),(210,64),(340,32),(420,24)]:
    icon=fit(icons[1],(size,size));preview.paste(icon,(x,90-icon.height//2),icon)
preview.save(PREVIEW_DIR/'proto-matter-icons.png')
