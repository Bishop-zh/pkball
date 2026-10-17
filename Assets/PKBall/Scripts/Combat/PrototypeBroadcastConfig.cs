using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace PKBall.Prototype
{
    [Serializable] public sealed class PrototypeBroadcastAbsorption {public string itemId;public int level=1,count=1;}
    [Serializable] public sealed class PrototypeBroadcastSlot
    {
        public int animationSchemaVersion;public PrototypeItemAnimation animation;
        public string releaseSoundId="",hitSoundId="";public float releaseVolume=1,hitVolume=1;
        public float growth=0,cooldownMultiplier=1;
        [HideInInspector] public float monsterHealthScale=1,monsterOutputScale=1;
        public List<PrototypeBroadcastAbsorption> absorbed=new List<PrototypeBroadcastAbsorption>();
        public string projectileIconId="";
        public string itemId=""; public int level=1;
        public bool overridesEffects;
        public PrototypeEffectDefinition[] effectsOverride;
        // -1 uses the catalogue value. Overrides describe level-one values, then level scaling applies.
        public float damage=-1,cooldown=-1,range=-1,visualScale=-1,effectMultiplier=1,visualSize=-1,effectScale=-1;
        public float projectileSpeed=-1,projectileSize=-1,hitDelay=-1,knockback=-1,animationDuration=-1;
        public float poison=-1,burn=-1,shield=-1,heal=-1,regeneration=-1;
        public int weaponKind=-1,projectileVisual=-1,attackShape=-1,visualStyle=-1,hitVisual=-1,visualCount=-1;
        public int healthBonus=-1,speedBonus=-1;
    }
    [Serializable] public sealed class PrototypeBroadcastSide
    {
        public string name="选手",visualId="player_warrior";
        public float health=PrototypeRun.BaseHealth,speed=100,shield,damageBonus,damageReduction;
        public PrototypeBroadcastSlot[] slots=new PrototypeBroadcastSlot[8];
        public PrototypeBroadcastSide(){for(int i=0;i<slots.Length;i++)slots[i]=new PrototypeBroadcastSlot();}
    }
    [Serializable] public sealed class PrototypeBroadcastConfig
    {
        public int version=3;public string title="自定义单挑";
        public PrototypeBroadcastSide left=new PrototypeBroadcastSide {name="红方"};
        public PrototypeBroadcastSide right=new PrototypeBroadcastSide {name="蓝方",visualId="player_mage"};
        public PrototypeBroadcastConfig(){left.slots[0].itemId="spear";left.slots[1].itemId="apprentice_shield";right.slots[0].itemId="crossbow";right.slots[1].itemId="apprentice_venom";}
        public void Validate()
        {
            if((version<1 || version>3) || left==null || right==null)throw new FormatException("配置版本或双方数据不正确");
            if(version<3){foreach(var side in new[]{left,right})if(side.slots!=null)foreach(var slot in side.slots)if(slot!=null){slot.releaseVolume=slot.hitVolume=1;if(slot.cooldownMultiplier<=0)slot.cooldownMultiplier=1;}version=3;}
            foreach(var side in new[]{left,right})
            {
                if(!Finite(side.health)||side.health<1||side.health>100000000 || !Finite(side.speed)||side.speed<1||side.speed>10000)throw new FormatException("生命或速度超出范围");
                if(!Finite(side.shield)||side.shield<0||!Finite(side.damageBonus)||side.damageBonus<0 || !Finite(side.damageReduction)||side.damageReduction<0||side.damageReduction>1)throw new FormatException("护盾/增伤/减伤不正确");
                if(string.IsNullOrEmpty(side.name)||side.name.Length>30)throw new FormatException("选手名需要1～30字");
                if(string.IsNullOrEmpty(side.visualId)||Resources.Load<Texture2D>("Art/"+side.visualId)==null && PrototypeUiSkin.SpriteFor("Art/"+side.visualId)==null)throw new FormatException("找不到选手图像");
                if(side.slots==null||(side.slots.Length!=8 && side.slots.Length!=10))throw new FormatException("配置需要8个装备位置（兼容旧版10格）");
                foreach(var s in side.slots)
                {
                    if(s!=null && s.overridesEffects && s.effectsOverride!=null) foreach(var e in s.effectsOverride)
                    {if(e==null || (int)e.trigger<0 || (int)e.trigger>3 || !Finite(e.triggerCooldown) || e.triggerCooldown<0 || e.triggerCooldown>10000 || !ValidLevels(e.levelAmounts) || !ValidLevels(e.levelGrowthAmounts) || (e.id!=null&&e.id.Length>160) || (int)e.kind<0 || (int)e.kind>14 || !Finite(e.amount) || e.amount<0 || e.amount>100000000 || !Finite(e.growthPerActivation) || e.growthPerActivation<0 || e.growthPerActivation>100000000 || !Finite(e.criticalChance) || e.criticalChance<0 || e.criticalChance>1 || !Finite(e.criticalMultiplier) || e.criticalMultiplier<0 || e.criticalMultiplier>1000 || (e.matchTag!=null&&e.matchTag.Length>80))throw new FormatException("词条参数超出范围");}
                    if(s!=null && s.animationSchemaVersion==0)s.animation=null;
                    if(s!=null && (s.animationSchemaVersion<0 || s.animationSchemaVersion>1 || s.animationSchemaVersion==1 && s.animation==null))throw new FormatException("道具动画配置版本不正确");
                    if(s!=null && s.animation!=null)ValidateAnimation(s.animation);
                    if(s==null||s.level<1||s.level>4)throw new FormatException("装备等级需要1～4级");
                    if(!Finite(s.monsterHealthScale)||s.monsterHealthScale<.05f||s.monsterHealthScale>1000||!Finite(s.monsterOutputScale)||s.monsterOutputScale<.05f||s.monsterOutputScale>1000)throw new FormatException("怪物成长倍率无效");
                    if(!PrototypeAudioLibrary.Valid(s.releaseSoundId)||!PrototypeAudioLibrary.Valid(s.hitSoundId)||!Finite(s.releaseVolume)||s.releaseVolume<0||s.releaseVolume>1||!Finite(s.hitVolume)||s.hitVolume<0||s.hitVolume>1)throw new FormatException("音效不存在，或音量不在0～1范围内");
                    if(!Finite(s.growth)||s.growth<0||s.growth>1000000||!Finite(s.cooldownMultiplier)||s.cooldownMultiplier<=0||s.cooldownMultiplier>1000)throw new FormatException("叠层次数0～100万，冷却倍率大于0且不超过1000");
                    if(s.absorbed!=null)foreach(var a in s.absorbed){var source=a==null?null:PrototypeItemRepository.Find(a.itemId);if(source==null||source.kind==PrototypeItemKind.Material||(s.itemId=="super_injector"&&source.kind!=PrototypeItemKind.Injection)||a.level<1||a.level>4||a.count<1||a.count>1000000||a.itemId=="devourer")throw new FormatException("吸收记录不正确");}
                    if(s.absorbed!=null&&s.absorbed.Count>0&&s.itemId!="devourer"&&s.itemId!="super_injector")throw new FormatException("此装备不支持吸收配置");
                    if(!string.IsNullOrEmpty(s.itemId) && (PrototypeItemRepository.Find(s.itemId)==null || PrototypeItemRepository.Find(s.itemId).kind!=PrototypeItemKind.Equipment))throw new FormatException("未知装备："+s.itemId);
                    foreach(float value in new[]{s.damage,s.cooldown,s.range,s.visualScale,s.visualSize,s.effectScale,s.projectileSpeed,s.projectileSize,s.hitDelay,s.knockback,s.animationDuration,s.poison,s.burn,s.shield,s.heal,s.regeneration})if(!Finite(value)||(value < 0 && value != -1) || value>100000000)throw new FormatException("装备覆盖属性不正确");
                    if(s.weaponKind < -1 || s.weaponKind>1 || s.projectileVisual < -1 || s.projectileVisual>4 || s.attackShape < -1 || s.attackShape>2 || s.visualStyle < -1 || s.visualStyle>3 || s.hitVisual < -1 || s.hitVisual>5 || s.visualCount < -1 || s.visualCount==0 || s.visualCount>24)throw new FormatException("攻击表现选项不正确");
                    if(s.projectileSpeed!=-1 && (s.projectileSpeed<.1f||s.projectileSpeed>200) || s.projectileSize!=-1 && (s.projectileSize<.05f||s.projectileSize>3) || s.animationDuration!=-1 && (s.animationDuration<.05f||s.animationDuration>10) || s.hitDelay>10 || s.knockback>50)throw new FormatException("弹速0.1～200，弹体大小0.05～3，动作0.05～10秒，延迟0～10秒，击退0～50");
                    if(!Finite(s.effectMultiplier)||s.effectMultiplier<0||s.effectMultiplier>1000)throw new FormatException("效果倍率需要0～1000");
                    if(s.cooldown!=-1 && s.cooldown<1 || s.range!=-1 && (s.range<.1f||s.range>20) || s.visualScale!=-1 && (s.visualScale<.1f||s.visualScale>6) || s.visualSize!=-1 && (s.visualSize<.1f||s.visualSize>6) || s.effectScale!=-1 && (s.effectScale<.1f||s.effectScale>6) || s.healthBonus< -1||s.speedBonus< -1)throw new FormatException("冷却≥1秒，距离0.1～20，图像大小0.1～6；留空使用默认值");
                }
            }
        }
        private static void ValidateAnimation(PrototypeItemAnimation a)
        {
            if(!Enum.IsDefined(typeof(PrototypeItemAnimationPreset),a.preset) || !Finite(a.duration) || a.duration<.05f || a.duration>10 || !Finite(a.scale) || a.scale<.1f || a.scale>8 || !Finite(a.intensity) || a.intensity<0 || a.intensity>3 || a.count<1 || a.count>24 || !Finite(a.rotation) || a.rotation< -360 || a.rotation>360)throw new FormatException("道具动画：持续0.05～10秒，尺寸0.1～8，轨迹强度0～3，数量1～24，旋转-360～360");
            foreach(float v in new[]{a.tint.r,a.tint.g,a.tint.b,a.tint.a})if(!Finite(v)||v<0||v>1)throw new FormatException("动画颜色需要0～1");
        }
        private static bool ValidLevels(float[] levels)
        {if(levels==null || levels.Length==0)return true;if(levels.Length!=4)return false;foreach(float v in levels)if(!Finite(v)||v<0||v>100000000)return false;return true;}
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public PrototypeBroadcastConfig Copy()=>JsonUtility.FromJson<PrototypeBroadcastConfig>(JsonUtility.ToJson(this));
    }
    public static class PrototypeBroadcastStore
    {
        public static string DirectoryPath=>Path.Combine(Application.persistentDataPath,"broadcast-presets");
        private static string PathFor(string name){if(string.IsNullOrWhiteSpace(name)||name.Length>60||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name=="."||name=="..")throw new ArgumentException("配置名包含非法字符");return Path.Combine(DirectoryPath,name+".json");}
        public static void Save(PrototypeBroadcastConfig data){data.Validate();data.version=3;Directory.CreateDirectory(DirectoryPath);string path=PathFor(data.title),temp=path+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data,true));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
        public static string[] Names(){if(!Directory.Exists(DirectoryPath))return new string[0];var files=Directory.GetFiles(DirectoryPath,"*.json");for(int i=0;i<files.Length;i++)files[i]=Path.GetFileNameWithoutExtension(files[i]);Array.Sort(files);return files;}
        public static string CharacterDirectory=>Path.Combine(Application.persistentDataPath,"broadcast-characters");
        [Serializable] private sealed class CharacterFile {public int version=2;public PrototypeBroadcastSide character;}
        private static string CharacterPath(string name){string safe=Path.GetFileName(PathFor(name));return Path.Combine(CharacterDirectory,safe);}
        public static string[] CharacterNames(){if(!Directory.Exists(CharacterDirectory))return new string[0];var files=Directory.GetFiles(CharacterDirectory,"*.json");for(int i=0;i<files.Length;i++)files[i]=Path.GetFileNameWithoutExtension(files[i]);Array.Sort(files);return files;}
        public static void SaveCharacter(PrototypeBroadcastSide side)
        {
            new PrototypeBroadcastConfig {left=side,right=side}.Validate();Directory.CreateDirectory(CharacterDirectory);string path=CharacterPath(side.name),temp=path+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(new CharacterFile {character=side},true));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
        }
        public static PrototypeBroadcastSide LoadCharacter(string name)
        {
            var file=JsonUtility.FromJson<CharacterFile>(File.ReadAllText(CharacterPath(name)));if(file==null || (file.version!=1&&file.version!=2) || file.character==null)throw new FormatException("角色预设无效");
            new PrototypeBroadcastConfig {version=file.version==1?2:3,left=file.character,right=file.character}.Validate();return file.character;
        }
        public static PrototypeBroadcastConfig Load(string name){var result=JsonUtility.FromJson<PrototypeBroadcastConfig>(File.ReadAllText(PathFor(name)));if(result==null)throw new FormatException("配置为空");result.Validate();return result;}
    }
    public static class PrototypeBroadcastSession
    {
        public static PrototypeBroadcastConfig Config,Draft;
        public static PrototypeRun Campaign;
        public static bool OpenEditor;
        public static readonly List<UnityEngine.Object> Owned=new List<UnityEngine.Object>();
        public static void ClearAssets(){foreach(var a in Owned)if(a!=null)UnityEngine.Object.Destroy(a);Owned.Clear();}
        public static PrototypeItemInstance CreateItem(PrototypeBroadcastSlot slot,int id)
        {
            if(string.IsNullOrEmpty(slot.itemId))return null;
            var d=UnityEngine.Object.Instantiate(PrototypeItemRepository.Find(slot.itemId));Owned.Add(d);
            if(slot.animationSchemaVersion==1 && slot.animation!=null)d.animation=slot.animation.Clone();
            if(!string.IsNullOrEmpty(slot.releaseSoundId))d.releaseSoundId=slot.releaseSoundId;if(!string.IsNullOrEmpty(slot.hitSoundId))d.hitSoundId=slot.hitSoundId;d.releaseVolume*=slot.releaseVolume;d.hitVolume*=slot.hitVolume;
            if(slot.healthBonus>=0)d.healthBonus=slot.healthBonus;if(slot.speedBonus>=0)d.speedBonus=slot.speedBonus;
            if(d.weapon==null && slot.weaponKind>=0){d.weapon=ScriptableObject.CreateInstance<PrototypeWeaponDefinition>();d.weapon.damage=0;d.weapon.cooldown=4;Owned.Add(d.weapon);}
            if(d.weapon!=null)
            {
                d.weapon=UnityEngine.Object.Instantiate(d.weapon);Owned.Add(d.weapon);
                if(slot.weaponKind>=0)d.weapon.kind=(PrototypeWeaponKind)slot.weaponKind;
                if(slot.projectileVisual>=0 || !string.IsNullOrEmpty(slot.projectileIconId))d.weapon.projectileIconId=slot.projectileIconId;
                if(slot.projectileSpeed>=0)d.weapon.projectileSpeed=slot.projectileSpeed;if(slot.projectileSize>=0)d.weapon.projectileSize=slot.projectileSize;
                if(slot.hitDelay>=0)d.weapon.hitDelay=slot.hitDelay;if(slot.animationDuration>=0)d.weapon.animationDuration=slot.animationDuration;if(slot.knockback>=0)d.weapon.knockbackImpulse=slot.knockback;
                if(slot.projectileVisual>=0)d.weapon.projectileVisual=(PrototypeProjectileVisual)slot.projectileVisual;if(slot.attackShape>=0)d.weapon.attackShape=(PrototypeAttackShape)slot.attackShape;
                if(slot.visualStyle>=0)d.weapon.visualStyle=(PrototypeAttackVisual)slot.visualStyle;if(slot.visualCount>=1)d.weapon.visualCount=slot.visualCount;if(slot.hitVisual>=0)d.weapon.hitVisual=(PrototypeHitVisual)slot.hitVisual;
                if(slot.damage>=0)d.weapon.damage=slot.damage;if(slot.cooldown>=0)d.weapon.cooldown=slot.cooldown;
                if(slot.visualSize>=0)d.weapon.visualSize=slot.visualSize;if(slot.effectScale>=0)d.weapon.effectScale=slot.effectScale;
                if(slot.range>=0)d.weapon.range=slot.range;if(slot.visualScale>=0)d.weapon.visualScale=slot.visualScale;
            }
            else if(slot.cooldown>=0)d.activationCooldown=slot.cooldown;
            PrototypeModifierLedger.EnsureEffectIds(d);
            if(slot.overridesEffects)d.effects=PrototypeEffectDisplay.Copy(slot.effectsOverride);
            if(slot.damage>=0 && d.weapon==null && d.effects!=null){d.effects=JsonUtility.FromJson<PrototypeItemData>(JsonUtility.ToJson(d)).effects;foreach(var e in d.effects)if(e.kind==PrototypeEffectKind.Damage)e.amount=slot.damage;}
            if(d.effects!=null){d.effects=JsonUtility.FromJson<PrototypeItemData>(JsonUtility.ToJson(d)).effects;foreach(var effect in d.effects){effect.amount*=slot.effectMultiplier;effect.growthPerActivation*=slot.effectMultiplier;if(effect.levelAmounts!=null)for(int i=0;i<effect.levelAmounts.Length;i++)effect.levelAmounts[i]*=slot.effectMultiplier;if(effect.levelGrowthAmounts!=null)for(int i=0;i<effect.levelGrowthAmounts.Length;i++)effect.levelGrowthAmounts[i]*=slot.effectMultiplier;}}
            var effects=new List<PrototypeEffectDefinition>(d.effects??new PrototypeEffectDefinition[0]);
            OverrideEffect(effects,PrototypeEffectKind.Poison,slot.poison);OverrideEffect(effects,PrototypeEffectKind.Burn,slot.burn);OverrideEffect(effects,PrototypeEffectKind.Shield,slot.shield);OverrideEffect(effects,PrototypeEffectKind.Heal,slot.heal);OverrideEffect(effects,PrototypeEffectKind.Regeneration,slot.regeneration);d.effects=effects.ToArray();PrototypeModifierLedger.EnsureEffectIds(d);
            if(slot.hitVisual>=0)d.hitVisual=(PrototypeHitVisual)slot.hitVisual;
            var result=new PrototypeItemInstance {definition=d,level=d.ResolveLevel(slot.level),instanceId=id,cooldownMultiplier=slot.cooldownMultiplier};
            result.AddActivationGrowth(result.LevelMultiplier*slot.growth,slot.growth>0?Mathf.Max(1,Mathf.FloorToInt(slot.growth)):0);
            if(slot.absorbed!=null)foreach(var a in slot.absorbed){var source=PrototypeItemRepository.Find(a.itemId);float fraction=PrototypeEffectMath.Absorption(slot.level)*a.count;int sourceLevel=source.ResolveLevel(a.level);float scale=Mathf.Pow(2,sourceLevel-1);
                if(source.kind==PrototypeItemKind.Injection)PrototypeEffectMath.MergeRecipe(result.recipes,source.injectionStat,source.potionLifetime,source.injectionAmount*fraction);
                else{result.inheritedHealth+=source.healthBonus*scale*fraction;result.inheritedSpeed+=source.speedBonus*scale*fraction;result.inheritedImpact+=source.impactBonus*scale*fraction;result.inheritedReduction+=source.impactTakenReduction*scale*fraction;PrototypeEffectMath.MergeProgram(result.inherited,new PrototypeInheritedProgram {definitionId=source.itemId,level=sourceLevel,scale=scale},fraction);}}
            // Pool scaling applies after normal overrides and level-based absorption.
            d.healthBonus=Mathf.RoundToInt(d.healthBonus*slot.monsterHealthScale);
            result.inheritedHealth*=slot.monsterHealthScale;
            result.encounterOutputScale=slot.monsterOutputScale;
            foreach(var recipe in result.recipes)
            {
                if(recipe.stat==PrototypeStat.Health)recipe.amount*=slot.monsterHealthScale;
                else if(recipe.stat==PrototypeStat.StartingShield)recipe.amount*=slot.monsterOutputScale;
            }
            return result;
        }
        private static void OverrideEffect(List<PrototypeEffectDefinition> effects,PrototypeEffectKind kind,float value)
        {if(value<0)return;float growth=0;foreach(var e in effects)if(e.kind==kind)growth=e.growthPerActivation;effects.RemoveAll(e=>e.kind==kind);if(value>0)effects.Add(new PrototypeEffectDefinition {kind=kind,amount=value,growthPerActivation=growth});}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void Reset(){Config=null;Draft=null;Campaign=null;OpenEditor=false;Owned.Clear();}
    }
}

