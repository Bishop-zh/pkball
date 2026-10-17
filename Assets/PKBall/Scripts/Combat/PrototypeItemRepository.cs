using System;
using System.Collections.Generic;
using UnityEngine;
namespace PKBall.Prototype
{
    // Stable IDs keep old run saves readable. SO assets bind visuals; JSON owns tunable data.
    [Serializable] public sealed class PrototypeItemData
    {
        public string itemId, displayName, description, iconId, generatedItemId;
        public PrototypeItemKind kind; public PrototypeItemRarity rarity;
        public int minShopDay=1,shopWeight=3,price=4,healthBonus,speedBonus;
        public bool bossExclusive,bossItem; public PrototypeItemAcquisition acquisition; public int fixedLevel;
        public float impactBonus,impactTakenReduction,injectionAmount=10,productionInterval,activationCooldown=4;
        public string releaseSoundId,hitSoundId;public float releaseVolume=1,hitVolume=1;
        public PrototypeHitVisual hitVisual;
        public PrototypeItemAnimation animation;
        public int animationSchemaVersion;
        public PrototypeStat injectionStat; public bool randomPotionProducer;
        public PrototypeEffectDefinition[] effects; public string[] tags; public PrototypePotionLifetime potionLifetime;
    }
    [Serializable] public sealed class PrototypeWeaponData
    {
        public string displayName; public PrototypeWeaponKind kind; public PrototypeAttackShape attackShape;
        public float cooldown=1,damage=10,range=2,projectileSpeed=8,meleeHalfAngle=70,knockbackImpulse=.5f;
        public float impactMultiplier=1,growthPerActivation,hitDelay=.12f,animationDuration=.3f,visualScale=1,visualSize=.8f,effectScale=1;
        public string projectileIconId;public PrototypeProjectileVisual projectileVisual;public float projectileSize=.18f;public PrototypeHitVisual hitVisual;
        public bool modifiesImpact; public PrototypeAttackVisual visualStyle; public int visualCount=1;
    }
    [Serializable] public sealed class PrototypeItemDataEntry {public string assetId;public bool hasWeapon;public PrototypeItemData item;public PrototypeWeaponData weapon;}
    [Serializable] public sealed class PrototypeItemDataFile {public int version=2;public PrototypeItemDataEntry[] items;}
    public static class PrototypeItemRepository
    {
        private static PrototypeItemDefinition[] all;
        private static Dictionary<string,PrototypeItemDefinition> byId;
        public static PrototypeItemDefinition[] All {get {Ensure();return all;}}
        public static PrototypeItemDefinition Find(string id){Ensure();return id!=null && byId.TryGetValue(id,out var d)?d:null;}
        public static void Reset(){all=null;byId=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeReset()=>Reset();
        private static PrototypeItemAnimation ResolveAnimation(PrototypeItemData data,PrototypeItemDefinition original,PrototypeItemDefinition matchingItem)
        {
            var source=matchingItem!=null?matchingItem:original;
            return data.animationSchemaVersion>0 && data.animation!=null?data.animation.Clone():(source.animation!=null?source.animation.Clone():new PrototypeItemAnimation());
        }
        private static void Ensure()
        {
            if(all!=null)return;
            var assets=Resources.LoadAll<PrototypeItemDefinition>("Items");
            var source=new Dictionary<string,PrototypeItemDefinition>();foreach(var a in assets)source[a.itemId]=a;
            byId=new Dictionary<string,PrototypeItemDefinition>();
            foreach(var a in assets)byId[a.itemId]=a;
            var file=Resources.Load<TextAsset>("Data/items");
            if(file!=null)try
            {
                var data=JsonUtility.FromJson<PrototypeItemDataFile>(file.text);
                if(data==null || (data.version!=1&&data.version!=2) || data.items==null)throw new FormatException("Unsupported item data");
                var staged=new Dictionary<string,PrototypeItemDefinition>(byId);
                var seen=new HashSet<string>();
                foreach(var e in data.items)
                {
                    if(e.item==null || string.IsNullOrWhiteSpace(e.item.itemId) || !seen.Add(e.item.itemId))throw new FormatException("Missing/duplicate item ID");
                    if(!source.TryGetValue(e.assetId??e.item.itemId,out var original))throw new FormatException("Unknown visual asset: "+e.assetId);
                    if(data.version==1){e.item.releaseVolume=e.item.hitVolume=1;if(e.weapon!=null&&e.weapon.projectileSize<=0)e.weapon.projectileSize=.18f;}
                    var d=UnityEngine.Object.Instantiate(original);d.hideFlags=HideFlags.DontSave;
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(e.item),d);
                    var animationSource=source.TryGetValue(e.item.itemId,out var matchingItem)?matchingItem:original;
                    d.animation=ResolveAnimation(e.item,original,animationSource);
                    if(e.hasWeapon && e.weapon!=null)
                    {
                        var w=original.weapon==null?ScriptableObject.CreateInstance<PrototypeWeaponDefinition>():UnityEngine.Object.Instantiate(original.weapon);
                        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(e.weapon),w);w.hideFlags=HideFlags.DontSave;
                        w.cooldown=Mathf.Max(.1f,w.cooldown);w.damage=Mathf.Max(0,w.damage);w.range=Mathf.Max(.1f,w.range);
                        w.visualScale=Mathf.Clamp(w.visualScale,.1f,6);w.visualSize=Mathf.Clamp(w.visualSize,.1f,6);w.effectScale=Mathf.Clamp(w.effectScale,.1f,6);w.visualCount=Mathf.Clamp(w.visualCount,1,24);
                        w.projectileSize=Mathf.Clamp(w.projectileSize,.05f,3);
                        w.animationDuration=Mathf.Max(.05f,w.animationDuration);w.hitDelay=Mathf.Max(0,w.hitDelay);w.projectileSpeed=Mathf.Max(.1f,w.projectileSpeed);
                        d.weapon=w;
                    }
                    else d.weapon=null;
                    if(d.bossExclusive)d.acquisition=PrototypeItemAcquisition.WildOnly;
                    staged[d.itemId]=d;
                }
                byId=staged;
            }
            catch(Exception e){Debug.LogWarning("Item JSON rejected; asset defaults retained: "+e.Message);}
            var list=new List<PrototypeItemDefinition>(byId.Values);list.Sort((a,b)=>string.CompareOrdinal(a.itemId,b.itemId));all=list.ToArray();
        }
        public static Color RarityColor(PrototypeItemRarity rarity)=>rarity==PrototypeItemRarity.Common?new Color(.65f,.74f,.80f):rarity==PrototypeItemRarity.Rare?new Color(.2f,.75f,1):rarity==PrototypeItemRarity.Epic?new Color(.75f,.4f,1):new Color(1,.75f,.25f);
    }
}
