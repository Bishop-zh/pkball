"""Slice imagegen atlases by connected alpha silhouettes, preserving transparent gutters."""
from pathlib import Path
from collections import deque
import shutil, uuid, re, os
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]/'Assets'/'PKBall'
GEN=Path(os.environ.get('PKBALL_ART_SOURCE', Path(__file__).resolve().parents[1]/'ArtSource'/'FantasyAtlas'))
def slice_sheet(source, target, cols, rows, names, character=False):
    path=ROOT/'Art'/'Generated'/target
    if not path.exists(): path=GEN/source
    im=Image.open(path).convert('RGBA'); a=np.array(im); alpha=a[:,:,3]; h,w=alpha.shape
    seen=np.zeros((h,w),dtype=np.int32); groups=[]; label=0
    for y in range(h):
        for x in np.flatnonzero((alpha[y]>32)&(seen[y]==0)):
            x=int(x)
            if seen[y,x]: continue
            label+=1; q=deque([(x,y)]); seen[y,x]=label; count=0; x0=x1=x; y0=y1=y
            while q:
                px,py=q.pop(); count+=1; x0=min(x0,px);x1=max(x1,px);y0=min(y0,py);y1=max(y1,py)
                for nx,ny in ((px-1,py),(px+1,py),(px,py-1),(px,py+1)):
                    if 0<=nx<w and 0<=ny<h and seen[ny,nx]==0 and alpha[ny,nx]>32:
                        seen[ny,nx]=label;q.append((nx,ny))
            groups.append((count,label,(x0,y0,x1+1,y1+1)))
    main=sorted(groups,reverse=True)[:cols*rows]
    if len(main)!=cols*rows: raise ValueError('Missing silhouettes')
    slots={}
    for count,l,b in main:
        cx=(b[0]+b[2])/2;cy=(b[1]+b[3])/2
        index=min(rows-1,int(cy/h*rows))*cols+min(cols-1,int(cx/w*cols))
        if index in slots:raise ValueError('Overlapping cell assignments: '+repr(main))
        slots[index]=l
    owners={l:l for _,l,_ in main}; boxes={l:b for _,l,b in main}
    for _,l,b in groups:
        if l in owners:continue
        cx=(b[0]+b[2])/2;cy=(b[1]+b[3])/2
        nearest=min(main,key=lambda m: max(m[2][0]-cx,0,cx-m[2][2])**2+max(m[2][1]-cy,0,cy-m[2][3])**2)
        owners[l]=nearest[1]
    atlas=ROOT/'Art'/'Generated'/target;atlas.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,atlas) if path.resolve()!=atlas.resolve() else None
    for index,name in enumerate(names):
        if name is None:continue
        l=slots[index]; member=[k for k,v in owners.items() if v==l]; mask=np.isin(seen,member)
        for _ in range(2):
            padded=np.pad(mask,1);mask=mask|padded[:-2,1:-1]|padded[2:,1:-1]|padded[1:-1,:-2]|padded[1:-1,2:]
        ys,xs=np.nonzero(mask);x0,x1=xs.min(),xs.max()+1;y0,y1=ys.min(),ys.max()+1
        pixels=a[y0:y1,x0:x1].copy();pixels[:,:,3]=np.where(mask[y0:y1,x0:x1],pixels[:,:,3],0)
        cut=Image.fromarray(pixels,'RGBA');size=512 if character else 256; cut.thumbnail((round(size*.9),round(size*.9)),Image.Resampling.LANCZOS)
        out=Image.new('RGBA',(size,size));out.alpha_composite(cut,((size-cut.width)//2,(size-cut.height)//2))
        folder=ROOT/'Resources'/('Art' if character else 'Icons');folder.mkdir(parents=True,exist_ok=True);dst=folder/(name+'.png');out.save(dst)
        meta=Path(str(dst)+'.meta')
        if not meta.exists():
            template=ROOT/'Resources'/('Art/monster_spore.png.meta' if character else 'Icons/bone_talisman.png.meta')
            text=re.sub(r'guid: [a-f0-9]+','guid: '+uuid.uuid4().hex,template.read_text(encoding='utf-8'),count=1)
            text=text.replace('enableMipMap: 1','enableMipMap: 0');meta.write_text(text,encoding='utf-8')
        print(name, 'bbox',tuple(map(int,(x0,y0,x1,y1))), 'transparent',out.getchannel('A').getextrema())
if __name__=='__main__':
    slice_sheet('exec-1f190bb1-327b-4339-a75f-7c38d5448f6c.png','fantasy-ball-characters-v1.png',3,2,['monster_ash_knight','monster_plague_alchemist','monster_saint_guard','monster_bamboo_spirit','monster_red_alchemist','monster_frost_tortoise'],True)
    slice_sheet('exec-058bb198-0805-43c7-ada3-671ef3f7ae16.png','fantasy-west-equipment-v1.png',3,3,['ash_greatsword','furnace_armor','ember_crown','plague_sprayer','plague_distiller','plague_heart','holy_bell','judgement_hammer','saint_seal'])
    slice_sheet('exec-80a5cb3f-9081-44a1-afe0-02780c93d404.png','fantasy-east-equipment-v1.png',4,3,['green_flying_sword','sword_case','sword_spirit_core','slime_core','red_potion_furnace','blood_gourd','alchemy_core',None,'frost_shell','mountain_seal','tortoise_core',None])
    slice_sheet('exec-3dab6b03-b0b4-4ca4-80cc-5bd92268b6a2.png','fantasy-legacy-characters-v2.png',3,2,['player_warrior','player_mage','player_engineer','monster_reaper','monster_slime','monster_normal'],True)
