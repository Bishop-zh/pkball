"""Author the first broadcast roster using the existing item/effect schemas.

Run from the project root. Stable IDs and existing effect IDs are preserved.
"""
import copy
import json
import pathlib
import re
import uuid

ROOT = pathlib.Path(__file__).resolve().parents[1]
DATA = ROOT / 'Assets/PKBall/Resources/Data'
ITEMS = ROOT / 'Assets/PKBall/Resources/Items'
doc = json.loads((DATA / 'items.json').read_text(encoding='utf-8-sig'))
entries = {e['item']['itemId']: e for e in doc['items']}
changed = []

def effect(key, kind, amount, trigger=0, growth=0, levels=None, cooldown=1):
    return dict(id=key, kind=kind, amount=amount, trigger=trigger,
                triggerCooldown=cooldown, growthPerActivation=growth,
                levelAmounts=levels or [amount*x for x in (1,2,4,8)],
                levelGrowthAmounts=[growth*x for x in (1,2,4,8)],
                matchTag='', criticalChance=0, criticalMultiplier=2)

def add(id, name, icon, description, cd, effects, tags, preset, color, speed=0):
    entry = copy.deepcopy(entries['shield_emitter'])
    d = entry['item']
    d.update(itemId=id, displayName=name, iconId=icon, description=description,
             activationCooldown=cd, effects=effects, tags=tags, price=16,
             minShopDay=2, shopWeight=2, rarity=1, healthBonus=0, speedBonus=speed,
             impactBonus=0, impactTakenReduction=0, acquisition=0,
             bossItem=False, bossExclusive=False, fixedLevel=0)
    d['animation'].update(preset=preset, tint=dict(zip(('r','g','b','a'),color)),count=1)
    entry.update(assetId=id, hasWeapon=False, weapon=None)
    if id in entries:
        doc['items'][doc['items'].index(entries[id])] = entry
    else:
        doc['items'].append(entry)
    entries[id]=entry
    changed.append(id)

add('antler_guard','青角护符','ram','绿角斥候把脱落的角磨成护符。周期获盾并少量治疗；不额外触发撞击。',4,
    [effect('antler:shield',1,24),effect('antler:heal',2,3)],['护符','盾'],9,[.45,.85,.35,1])
add('spore_lantern','矿孢灯','apprentice_venom','孢子矿怪照亮废矿的活体灯。每次释放向敌人叠毒并为自身获盾。',5,
    [effect('spore:poison',3,.35),effect('spore:shield',1,12)],['灯','毒'],11,[.65,.8,.2,1])
add('storm_feather','电羽针','crossbow','电羽球脱落的导电羽。周期直伤，无麻痹或连锁；装备速度加成随等级翻倍。',4,
    [effect('feather:damage',0,20)],['电','装置'],6,[.35,.8,1,1],speed=3)
add('red_duel_banner','赤角战旗','spear','赤角斗兽的挑战旗。每次释放同时造成直伤并获盾，不存在低血隐藏增伤。',5,
    [effect('banner:damage',0,14),effect('banner:shield',1,20)],['旗','盾'],8,[1,.35,.25,1])
add('saw_flywheel','锯齿飞轮','wheel','野猪背负的旧锯轮。每次释放直伤；随后该词条永久成长，沿用装备成长账本。',3.5,
    [effect('saw:damage',0,21,growth=.12)],['装置','成长'],7,[.85,.65,.3,1])
add('frost_mirror','霜壳镜','frost_shell','冰壳蟹的镜面甲。周期获盾；自身护盾由正数被打至零时反击直伤，独立冷却3秒。冰晶不冻结。',6,
    [effect('mirror:shield',1,28),effect('mirror:break',0,12,trigger=3,cooldown=3)],['盾','霜'],9,[.45,.85,1,1])
add('crown_command','失冠敕令','holy_bell','失冠构造体反复执行的守城令。周期获盾并消耗当前盾10%转伤；按词条顺序先加盾后消耗。',6,
    [effect('crown:shield',1,32),effect('crown:convert',13,.1,levels=[.1,.1,.1,.1])],['盾','装置'],8,[1,.8,.3,1])
add('furnace_bellows','余火风箱','furnace_armor','熔炉巨灵留下的风箱。每次释放为敌人加火并为自己获盾；火不是持续固定秒伤。',5,
    [effect('bellows:burn',4,3,levels=[3,4.8,7.2,10.8]),effect('bellows:shield',1,16)],['火','装置'],12,[1,.45,.15,1])
add('storm_capacitor','雷鳞电容','arc_cannon','机械龙的储能鳞片。周期直伤与获盾，不消耗已有护盾，不附带眩晕。',5,
    [effect('capacitor:damage',0,28),effect('capacitor:shield',1,12)],['电','装置'],6,[.4,.65,1,1])
add('thorn_seed','荆棘种匣','plague_distiller','园丁把毒种封在护盾里。周期获盾；破盾给敌人叠毒，独立冷却3秒，不是每次受伤反毒。',4,
    [effect('thorn:shield',1,20),effect('thorn:break',3,.2,trigger=3,cooldown=3)],['盾','毒'],11,[.35,.8,.4,1])
add('salvage_clock','拾荒发条','devourer','拾荒机匠重装的旧发条。周期造成直伤并治疗；不会自动吸收装备或生成金币。',6,
    [effect('salvage:damage',0,18),effect('salvage:heal',2,6)],['装置','治疗'],13,[1,.72,.35,1])

# Seven mucus items used to multiply maximum HP by 1.05 seven times per cycle.
# Keep the identity but price the complete eight-slot build, not a single item.
for id, values, desc in [
    ('slime_mucus',[[.01,.013,.016,.02],[.015,.02,.025,.03]],'史莱姆分体组织：释放时先增长本场最大生命，再以自身最大生命造成伤害。八团组合需按整套累计成长比较。'),
    ('slime_core',[[.02]*4,[.06]*4],'八团史莱姆的统一意识。固定四级：每4秒先增加本场最大生命2%，再造成自身最大生命6%的伤害。')]:
    d=entries[id]['item'];d['description']=desc
    for e,v in zip(d['effects'],values):e['amount']=v[0];e['levelAmounts']=v
    changed.append(id)

def scalar(v):
    if isinstance(v,bool):return '1' if v else '0'
    if isinstance(v,str):return json.dumps(v,ensure_ascii=True)
    return str(v)

def yaml_fields(d, indent=2):
    lines=[]
    for k,v in d.items():
        prefix=' '*indent+k+':'
        if isinstance(v,dict):
            lines.append(prefix);lines.extend(yaml_fields(v,indent+2))
        elif isinstance(v,list):
            lines.append(prefix+(' []' if not v else ''))
            for row in v:
                if isinstance(row,dict):
                    block=yaml_fields(row,indent+2);block[0]=' '*indent+'- '+block[0].lstrip();lines.extend(block)
                else:lines.append(' '*indent+'- '+scalar(row))
        elif v is not None:lines.append(prefix+' '+scalar(v))
    return lines

template=(ITEMS/'shield_emitter.asset').read_text(encoding='utf-8')
header=template.split('  itemId:')[0]
for id in changed:
    path=ITEMS/(id+'.asset')
    old=path.read_text(encoding='utf-8') if path.exists() else header
    head=re.sub(r'  m_Name:.*','  m_Name: '+id,old.split('  itemId:')[0])
    # Definitions touched here have no weapon. Keep their own visual/audio references in JSON.
    body=dict(entries[id]['item']);body.pop('animationSchemaVersion',None)
    path.write_text(head+'\n'.join(yaml_fields(body))+'\n  weapon: {fileID: 0}\n',encoding='utf-8')
    meta=path.with_suffix('.asset.meta')
    if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'pkball/items/'+id).hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
(DATA/'items.json').write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Authored',len(changed),'items; total',len(doc['items']))

# The original encounter order and visual-based stable IDs must not move.
campaign=ROOT/'Assets/PKBall/Scripts/Combat/PrototypeEncounterCatalog.cs'
source=campaign.read_text(encoding='utf-8-sig')
loadouts={
 '绿角冲锋兽':'ram,boots,antler_guard,spear,padding,antler_guard',
 '孢子矿怪':'pickaxe,spore_lantern,apprentice_venom,padding,shield_emitter,spore_lantern',
 '电羽球':'crossbow,afterburner,storm_feather,boots,padding,storm_feather',
 '青竹剑灵':'apprentice_sword,sword_case,green_flying_sword,green_flying_sword,boots,padding',
 '赤丹药修':'random_brewer,blood_gourd,red_potion_furnace,health_lab,padding,shield_emitter',
 '赤角斗兽':'spear,plating,ram,red_duel_banner,red_duel_banner,padding',
 '锯齿野猪':'hammer,boots,afterburner,saw_flywheel,saw_flywheel,antler_guard',
 '冰壳蟹':'crossbow,shield_emitter,padding,frost_mirror,frost_mirror,plating',
 '王冠构造体':'hammer,core,plating,crown_command,crown_command,shield_emitter',
 '熔炉巨灵':'flame_axe,afterburner,ram,furnace_bellows,furnace_bellows,furnace_armor',
 '雷霆机械龙':'arc_cannon,shield_emitter,core,storm_capacitor,storm_capacitor,plating',
}
for name,loadout in loadouts.items():
    source=re.sub(r'(new MonsterProfile\("'+name+r'", "[^"]+", "[^"]+", ")[^"]+',lambda m:m[1]+loadout,source)
source=source.replace('八件粘液叠加本场生命并造成生命比例伤害','一核心七粘液，缓慢膨胀并造成自身生命比例伤害')
campaign.write_text(source,encoding='utf-8')

# Story strength is an editorial tier, never an invisible combat multiplier.
stories={
 '绿角冲锋兽':('边境信使长大后的角仍是青色，替山民冲开废城路障；它只是熟练斥候，还无法正面破城。','青角护符保住冲锋后的血线，长矛接敌。','惧怕稳定远程消耗；速度不等于必中。'),
 '孢子矿怪':('废矿守夜人被菌群寄居，矿镐负责开路，矿孢灯负责驱赶盗矿者；离开矿脉后没有无穷养分。','矿灯叠毒与护盾，慢慢把战斗拖进毒区。','治疗清毒、快速进攻；矿镐经济价值不等于当场伤害。'),
 '电羽球':('幼年雷鸟把落雷藏进羽管，是机械龙身旁的送信者；只能放出短促电针，无法呼唤天雷。','电羽针与追踪弩组成快节奏远程压迫。','薄血且没有硬控，高盾能争取反击时间。'),
 '青竹剑灵':('断剑在竹林百年才生出灵智，如今仍是养剑学徒；需要时间结成剑阵，剑灵核心属于未来觉醒。','两把青竹飞剑持续留场，剑匣催动剑类。','启动慢，前期集火能阻止剑阵成形。'),
 '赤丹药修':('山村游医用赤炉炼制便宜丹药，血葫芦保存失败药渣；尚未炼出真正的丹心。','生产与护盾保护慢热输出。','生产收益偏向跨战养成，短局爆发较弱。'),
 '赤角斗兽':('边军退役的角斗冠军，把旧战旗钉在背上；它比青角更能守住阵地，却不再追求速度。','战旗同时攻守，长矛与撞角持续施压。','毒穿盾，拖久后缺少治疗。'),
 '锯齿野猪':('伐木场逃出的驮兽背着尚未停转的飞轮，越跑磨齿越锋利；没有真正的狂暴免伤。','双飞轮稳定直伤并留下永久成长。','缺少厚盾，对高防守构筑需要成长时间。'),
 '冰壳蟹':('寒潭守门者把旧盾磨成镜壳，碎裂时冰片飞散；冰是外形，不会让时间停滞。','霜壳镜破盾反击，盾破时才有戏剧转折。','毒和决胜耗损穿盾，不触发破盾反击。'),
 '八团史莱姆':('八团分体共享一颗核心，吞下的残片只能在本场临时膨胀；它并非永不衰竭的生命神。','一核心七粘液，越打体型和生命越大。','前期爆发和按目标生命打击；决胜削弱成长附带补血。'),
 '瘟疫炼金师':('瘟疫城最后的药剂师把解药研究反转成毒剂，蒸馏器仍会产出偶然有效的药。','毒喷管、蒸馏器和瘟疫之心形成持久压力。','治疗可清毒；缺乏瞬时防御。'),
 '圣钟守卫':('圣城闭门后仍守着钟楼的守卫，敲钟积盾，挥锤时把信仰铸成的一部分护盾化为裁决。','盾先积蓄，再由裁决之锤消耗转伤。','转伤会露出防守空档，毒绕过盾。'),
 '王冠构造体':('失踪国王留下的自动守城机，王冠里只有最后一道敕令；有城防级耐久，没有王权神迹。','敕令先加盾再转伤，重锤守住近身区域。','持续穿盾伤害，慢速时容易被远程消耗。'),
 '熔炉巨灵':('城底熔炉诞生的火灵，把每一口余火送回风箱；离开熔炉后必须在进攻与保温之间取舍。','风箱加火并获盾，火斧补足近战。','护盾折抵火伤更有效，火层会自然衰减。'),
 '雷霆机械龙':('旧城天幕的储能装置获得龙形躯壳，雷鳞可以放电护体；储能器会冷却，没有无限雷暴。','双电容与电弧炮形成周期爆发。','发射间隙可被快攻压制，盾量不如专职守卫。'),
 '小死神':('见习引魂者借用真正死神的一段镰影；能切去生命的一部分，却没有绝对斩杀权。','镰刀按目标最大生命打击，披风提供短暂无敌。','镰刀冷却长；决胜耗损绕过无敌。'),
 '灰烬骑士':('焚城之战后留下的守誓者，余烬王冠只承认仍在战斗的人；铠甲破碎时火焰才泄出。','燃烧目标命中得盾，破盾再次添火。','需要命中与燃烧条件，毒穿盾可打断防守优势。'),
 '霜山龟灵':('负山而行的古老龟灵，每次镇山印都消耗时间凝聚；古老意味着耐力，并非不会失败。','龟甲积盾再生，镇山印借自身最大生命打击。','慢启动且决胜压低恢复，毒与百分比攻击有价值。'),
}
roster=[]
for tier,(start,end) in enumerate([(0,5),(5,11),(11,17)]):
    profiles=re.findall(r'new MonsterProfile\("([^"]+)", "([^"]+)", "[^"]+", "([^"]+)"',source)[start:end]
    for name,visual,loadout in profiles:
        story,hook,counter=stories[name]
        roster.append(dict(id='cast_'+visual.removeprefix('monster_'),tier=['普通','精英','首领'][tier],story=story,hook=hook,counter=counter,artNote='沿用已导入角色美术',character=dict(name=name,visualId=visual,health=[300,360,420][tier],speed=[100,100,95][tier],slots=[dict(itemId=x,level=4 if entries[x]['item'].get('bossItem') else 1) for x in loadout.split(',')]+[dict(itemId='',level=1)]*(8-len(loadout.split(','))))))

new=[
 ('thorn_keeper','荆棘园丁','monster_spore','普通','曾给瘟疫城种药的园丁，如今用盾壳运输毒种；每次种匣碎裂都是下一次播种。','破盾播毒，毒盾混合路线。','对手绕盾打毒会减少播种次数。','thorn_seed,thorn_seed,spore_lantern,poison_dagger,padding,boots'),
 ('scrap_tinker','拾荒机匠','player_engineer','普通','捡回城防报废零件的年轻匠人，发条只够支持一轮修补；梦想修好失冠构造体。','发条治疗与直伤，稳定但不爆发。','缺少大量护盾，快攻能抢在修补前。','salvage_clock,salvage_clock,storm_feather,hammer,padding,boots'),
 ('frost_ranger','霜镜游侠','monster_frost','普通','与冰壳蟹交换镜甲的寒潭猎手，靠镜面掩护弩箭；镜甲不是冰冻法术。','远程追击与破盾反击。','毒穿盾；近身重击可穿过薄盾余量。','frost_mirror,crossbow,storm_feather,boots,padding,antler_guard'),
 ('thunder_courier','逐雷信使','monster_spark','精英','成年的电羽信使偷带了一片机械龙雷鳞，用速度抵达战场，却无法承受龙的全部电流。','三电羽配电容，密集脉冲。','护盾储存与反击，缺少稳定治疗。','storm_feather,storm_feather,storm_feather,storm_capacitor,boots,padding'),
 ('ember_stoker','余火司炉','monster_furnace','精英','熔炉巨灵挑选的司炉人，把火种缝进铠甲；主动添火，甲裂时泄火，始终需要护盾维持。','双风箱与熔炉甲制造火盾交替。','高盾减轻火伤，毒可穿过铠甲。','furnace_bellows,furnace_bellows,furnace_armor,flame_axe,padding,antler_guard'),
 ('uncrowned_warden','失冠禁卫','monster_boss','首领','王冠构造体的最后一位禁卫，守的已不是国王而是残存居民；将城防敕令化为自身盾击。','圣钟积盾、敕令小额转伤、裁决重击。','多次转伤消耗自己的盾，毒和集火利用空档。','crown_command,crown_command,holy_bell,judgement_hammer,plating,padding'),
]
for id,name,visual,tier,story,hook,counter,loadout in new:
    roster.append(dict(id=id,tier=tier,story=story,hook=hook,counter=counter,artNote='复用现有外观；新立绘待制作',character=dict(name=name,visualId=visual,health={'普通':300,'精英':360,'首领':420}[tier],speed=100 if tier!='首领' else 95,slots=[dict(itemId=x,level=1) for x in loadout.split(',')]+[dict(itemId='',level=1)]*2)))
matches=[]
for n,(title,left,right,premise) in enumerate([
 ('青角与矿灯','cast_normal','cast_spore','冲锋能否赶在毒层叠起前结束？'),
 ('寒潭借甲','frost_ranger','thorn_keeper','两种破盾反击：直伤冰片与毒种，谁更适合拉长战斗？'),
 ('拾荒者试剑','scrap_tinker','cast_bamboo_spirit','稳定修补能否撑过剑阵启动？'),
 ('送信者与司炉人','thunder_courier','ember_stoker','电流脉冲对上火盾循环。'),
 ('圣钟审判八团','cast_saint_guard','cast_slime','积盾转伤与生命膨胀的成长对局。'),
 ('见习死神叩山','cast_reaper','cast_frost_tortoise','百分比镰刀对上古老耐力，故事强弱未必决定结果。'),
 ('失冠之战','uncrowned_warden','cast_boss','同属城防传承：裁决流与核心重锤流。'),
 ('雷火遗城','cast_storm','cast_ash_knight','机械雷鳞对上余烬铠甲。'),
]):matches.append(dict(id='episode_'+str(n+1).zfill(2),title=title,leftId=left,rightId=right,premise=premise))
(DATA/'characters.json').write_text(json.dumps(dict(version=1,characters=roster,matches=matches),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

lines=['# 首季角色设定与录制卡','', '2026-10-13；数据源：`Resources/Data/characters.json`。共23个可载入角色（17个旧角色、6个新构筑）、8组对战。', '',
 '## 共用世界观与强度', '', '旧城毁于雷火事故，城防机械、山林灵物与幸存者争夺残留核心。普通是地方生存者，精英是成熟职业或族群强者，首领是古老灵物或城防级存在。小死神只是见习者，史莱姆只是局部群体，禁止把名字解读为绝对无敌。', '',
 '主播故事档本体生命普通300、精英360、首领420；速度100/100/95，通常六件装备、余下两格空置（八团史莱姆八件，小死神四件，以百分比镰刀换取装备数量劣势）。普通装备均一级、已有首领核心固定四级，无隐藏增伤/减伤、无预填历史成长。装备附加生命仍另算。故事档跨档挑战不宣称公平。冒险仍使用原有按天/档位公式，不把主播生命覆盖过去。', '',
 '## 角色卡', '']
for p in roster:
    c=p['character'];lines += ['### '+c['name']+' · '+p['tier'], '', '**稳定ID**：`'+p['id']+'`；**外观**：`'+c['visualId']+'`（'+p['artNote']+'）。', '',p['story'],'','- 视频看点：'+p['hook'],'- 对抗窗口：'+p['counter'],'- 初始配装：'+'、'.join(entries[s['itemId']]['item']['displayName'] for s in c['slots'] if s['itemId'])+'。','']
lines+=['## 首批八期','', '|对战预设|讲述问题|','|---|---|']+[f"|{m['title']}|{m['premise']}|" for m in matches]
lines+=['','## 拍摄步骤','','主菜单 → 主播模式 → 对战预设中的【首季】条目 → 检查红蓝装备 → 开始。角色区的【内置·档位】条目可单独换一方；用户保存条目有独立【我的】前缀。载入是副本，保存仍保存到个人文件，不修改内置目录。所有基础、词条、成长、吸收、音效、动画区继续使用原编辑器。新角色可通过“加入怪物池”保存为自定义野怪。','','录制先做一次完整试打，讲清装备等级、档位、是否有历史成长；剪辑保留逆转的触发原因。建议每组交换左右各试3场再选素材，避免把单局当胜率。前八期只发布机制观察，暂不做绝对战力排行。新立绘制作前明确标记“试作角色/复用外观”。']
(ROOT/'Docs/43-首季角色设定与录制卡.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Authored',len(roster),'characters and',len(matches),'matches')
