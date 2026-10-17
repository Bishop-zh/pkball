"""Slice generated combat art. Cropping preserves generated alpha; no repainting."""
from pathlib import Path
from PIL import Image
import json, shutil
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/PKBall/Art/Generated/CombatV2'
OUT=ROOT/'Assets/PKBall/Resources/BattleArt'
ITEMS=['apprentice_sword','apprentice_shield','apprentice_venom','apprentice_flame','apprentice_spring','master_sword','master_shield','master_venom','master_flame','master_spring','master_token','random_brewer','super_injector','poison_dagger','flame_axe','shield_emitter']
UI=['player_panel','enemy_panel','item_tile','cooldown_surface','health_frame','health_player','health_enemy','shield_fill','energy_frame','energy_fill','count_badge','title_strip','button_cyan','button_coral','status_shield','status_heal']
def extract(im,box,path,square=False):
 tile=im.crop(tuple(round(v) for v in box)).convert('RGBA')
 bounds=tile.getchannel('A').point(lambda a:255 if a>12 else 0).getbbox()
 if bounds is None:raise ValueError(path)
 tile=tile.crop(bounds)
 if square:
  side=max(tile.size);pad=Image.new('RGBA',(side+24,side+24));pad.alpha_composite(tile,((side+24-tile.width)//2,(side+24-tile.height)//2));tile=pad.resize((256,256),Image.Resampling.LANCZOS)
 else:
  pad=Image.new('RGBA',(tile.width+8,tile.height+8));pad.alpha_composite(tile,(4,4));tile=pad
 path.parent.mkdir(parents=True,exist_ok=True);tile.save(path)
 return {'id':path.stem,'path':str(path.relative_to(ROOT)).replace('\\','/'),'size':list(tile.size)}
def main():
 records=[]
 im=Image.open(SOURCE/'items.png');w,h=im.size
 for i,n in enumerate(ITEMS):records.append(extract(im,((i%4)*w/4,(i//4)*h/4,(i%4+1)*w/4,(i//4+1)*h/4),OUT/'Items'/f'{n}.png',True))
 im=Image.open(SOURCE/'ui.png');w,h=im.size
 # Reviewed atlas rows are intentionally unequal: panels, bars, slim bars, buttons.
 rows=[(0,440/1254,[0,.25,.5,.75,1]),(440/1254,690/1254,[0,355/1254,640/1254,945/1254,1]),(690/1254,935/1254,[0,345/1254,625/1254,850/1254,1]),(935/1254,1,[0,.25,.5,.75,1])]
 for i,n in enumerate(UI):
  top,bottom,x=rows[i//4];c=i%4
  records.append(extract(im,(x[c]*w,top*h,x[c+1]*w,bottom*h),OUT/'UI'/f'{n}.png'))
 im=Image.open(SOURCE/'arena.png');w,h=im.size
 for i,n in enumerate(['arena_floor','arena_rim']):records.append(extract(im,(i*w/2,0,(i+1)*w/2,h),OUT/'Arena'/f'{n}.png'))
 shutil.copy2(SOURCE/'background.png',OUT/'Arena'/'background.png')
 (SOURCE/'slices.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
 print('Saved',len(records),'alpha slices and separate background')
if __name__=='__main__':main()
