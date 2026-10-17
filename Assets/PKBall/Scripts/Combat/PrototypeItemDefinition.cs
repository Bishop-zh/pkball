using UnityEngine;

namespace PKBall.Prototype
{
    public enum PrototypeItemKind { Equipment, Injection, Material }
    public enum PrototypeItemAcquisition { ShopAndLoot, WildOnly }
    public enum PrototypeItemRarity { Common, Rare, Epic, Legendary }

    [CreateAssetMenu(fileName = "PrototypeItem", menuName = "PKBall/Prototype Item")]
    public sealed class PrototypeItemDefinition : ScriptableObject
    {
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public string iconId;
        public PrototypeItemKind kind;
        public PrototypeItemRarity rarity;
        [Min(1)] public int minShopDay = 1;
        [Range(1, 10)] public int shopWeight = 3;
        [Min(0)] public int price = 4;
        public PrototypeItemAcquisition acquisition;
        public bool bossItem;
        public bool bossExclusive;
        [Range(0, 4)] public int fixedLevel;
        public int ResolveLevel(int level) => fixedLevel > 0 ? Mathf.Clamp(fixedLevel, 1, 4) : Mathf.Clamp(level, 1, 4);
        public int healthBonus;
        public int speedBonus;
        public float impactBonus;
        public float impactTakenReduction;
        public PrototypeWeaponDefinition weapon;
        public string releaseSoundId,hitSoundId;public float releaseVolume=1,hitVolume=1;
        public PrototypeHitVisual hitVisual;
        public PrototypeItemAnimation animation = new PrototypeItemAnimation();
        public PrototypeStat injectionStat = PrototypeStat.Health;
        public float injectionAmount = 10f;
        public string generatedItemId;
        public bool randomPotionProducer;
        [Min(0)] public float productionInterval;
        [Min(1)] public float activationCooldown = 4f;
        public PrototypeEffectDefinition[] effects = new PrototypeEffectDefinition[0];
        public string[] tags = new string[0];
        public PrototypePotionLifetime potionLifetime;
        public bool HasActive => weapon != null || (effects != null && System.Array.Exists(effects,e=>e!=null && e.trigger==PrototypeEffectTrigger.Release));
        public bool HasTag(string tag) => tags != null && System.Array.IndexOf(tags, tag) >= 0;
        public float ProductionInterval(int level) => randomPotionProducer ?
            ((productionInterval > 0 ? productionInterval : 16f) * (level <= 1 ? 1f : level == 2 ? .875f : level == 3 ? .75f : .625f)) :
            (level <= 1 ? productionInterval : level == 2 ? productionInterval * .8f : level == 3 ? productionInterval * .6f : productionInterval * .5f);
        public static string RarityName(PrototypeItemRarity value)=>value==PrototypeItemRarity.Common?"普通":value==PrototypeItemRarity.Rare?"稀有":value==PrototypeItemRarity.Epic?"史诗":"传说";
        public string RarityText=>RarityName(rarity);
        public string Classification => tags == null || tags.Length == 0 ? "" : "分类：" + string.Join(" · ", tags);

        public string StatSummary()
        {
            if (kind == PrototypeItemKind.Injection)
                return $"{(potionLifetime == PrototypePotionLifetime.NextBattle ? "下一场战斗" : "本局永久")} {StatName(injectionStat)} +{FormatAmount(injectionStat,injectionAmount)}\n{Classification}";
            string result = "";
            if (healthBonus != 0) result += $"生命 +{healthBonus}  ";
            if (speedBonus != 0) result += $"速度 +{speedBonus}  ";
            if (impactBonus != 0) result += $"撞击伤害 +{impactBonus * 100f:0}%  ";
            if (impactTakenReduction != 0) result += $"撞击减伤 {impactTakenReduction * 100f:0}%  ";
            if (weapon != null && weapon.damage > 0) result += $"{(weapon.kind == PrototypeWeaponKind.Melee ? "近战" : "远程")} {weapon.damage:0}伤 / {weapon.cooldown:0.0}秒";
            result += EffectSummary(ResolveLevel(1), 0);
            return result;
        }

        public string EffectSummary(int level, float growth)
        {
            level = ResolveLevel(level);
            float scale = Mathf.Pow(2, level - 1);
            string result = "";
            var preview=new PrototypeItemInstance {definition=this,level=ResolveLevel(level),growth=growth};
            if(effects!=null)for(int i=0;i<effects.Length;i++)result+="\n"+PrototypeEffectDisplay.Summary(preview,i);
            if(weapon!=null && weapon.growthPerActivation>0) result+=$"每次释放后伤害永久 +{weapon.growthPerActivation*scale:0.#}  ";
            if (HasActive && weapon == null) result += $"冷却 {activationCooldown:0.#}秒  ";
            if (productionInterval > 0) result += $"每 {ProductionInterval(level):0.#}秒产1管{(randomPotionProducer ? "随机短效药" : "针剂")}  ";
            return result + "\n" + Classification;
        }
        public static string FormatAmount(PrototypeStat stat,float amount)
        {return stat==PrototypeStat.ImpactBonus||stat==PrototypeStat.ImpactTakenReduction||stat==PrototypeStat.MeleeDamageBonus||stat==PrototypeStat.RangedDamageBonus||stat==PrototypeStat.DamageBonus||stat==PrototypeStat.DamageReduction ? (amount*100).ToString("0.##")+"%" : amount.ToString("0.##");}
        public static string StatName(PrototypeStat stat)
        {
            switch (stat)
            {
                case PrototypeStat.Health: return "生命";
                case PrototypeStat.DamageBonus: return "攻击伤害";
                case PrototypeStat.DamageReduction: return "直伤减免";
                case PrototypeStat.StartingShield: return "护盾";
                case PrototypeStat.Gold: return "金币";
                case PrototypeStat.Speed: return "速度";
                case PrototypeStat.ImpactBonus: return "撞击伤害";
                case PrototypeStat.ImpactTakenReduction: return "撞击减伤";
                case PrototypeStat.MeleeDamageBonus: return "近战伤害";
                case PrototypeStat.RangedDamageBonus: return "远程伤害";
                default: return "属性";
            }
        }

        public string UpgradePreview(int level)
        {
            level = ResolveLevel(level);
            float scale = level == 1 ? 1f : level == 2 ? 2f : level == 3 ? 4f : 8f;
            float effect = level == 1 ? 1f : level == 2 ? 2f : level == 3 ? 4f : 8f;
            if (kind != PrototypeItemKind.Equipment) return StatSummary();
            string result = bossItem ? $"首领道具 · 固定 Lv.{level} · 野怪获胜掉落  " : (acquisition==PrototypeItemAcquisition.WildOnly||bossExclusive?$"野外道具 · Lv.{level} · 野怪获胜掉落  ":$"Lv.{level}  ");
            if (weapon != null && weapon.damage > 0) result += $"伤害 {weapon.damage * scale:0.#} / 冷却 {weapon.cooldown:0.##}秒  ";
            if (healthBonus != 0) result += $"生命 +{healthBonus * scale:0.#}  ";
            if (speedBonus != 0) result += $"速度 +{speedBonus * scale:0.#}  ";
            if (impactBonus != 0) result += $"撞击 +{impactBonus * effect * 100f:0.#}%  ";
            if (impactTakenReduction != 0)
                result += $"撞击减伤 {impactTakenReduction * effect * 100f:0.#}%  ";
            result += EffectSummary(level, 0);
            if (level == 4 && !bossItem) result += "  /  突破核心专属永久生命 +10";
            return result;
        }
    }
}
