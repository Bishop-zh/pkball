using System;
using UnityEngine;
namespace PKBall.Prototype {
    public static class PrototypeEffectDisplay {
        public static readonly string[] Names={"附加伤害","护盾","治疗","中毒","灼烧","再生","学徒冷却缩减","激发学徒","复放药水","敌人最大生命伤害","无敌","本场最大生命成长","自身最大生命伤害","消耗护盾转伤害","激发标签装备"};
        public static readonly string[] TriggerNames={"释放时","命中时","命中燃烧目标","护盾破碎时"};
        public static PrototypeEffectDefinition[] Copy(PrototypeEffectDefinition[] effects) {
            if(effects==null)return new PrototypeEffectDefinition[0];
            var result=new PrototypeEffectDefinition[effects.Length];
            for(int i=0;i<effects.Length;i++){var e=effects[i];result[i]=new PrototypeEffectDefinition{id=e.id,kind=e.kind,amount=e.amount,growthPerActivation=e.growthPerActivation,matchTag=e.matchTag,criticalChance=e.criticalChance,criticalMultiplier=e.criticalMultiplier,trigger=e.trigger,triggerCooldown=e.triggerCooldown,levelAmounts=e.levelAmounts==null?null:(float[])e.levelAmounts.Clone(),levelGrowthAmounts=e.levelGrowthAmounts==null?null:(float[])e.levelGrowthAmounts.Clone()};}return result;
        }
        // Shared shop and battle card: one level and one current instance, including earned growth.
        public static string Tooltip(PrototypeItemInstance item, int previewLevel = 0)
        {
            if (item == null || item.definition == null) return "空装备格";
            var d = item.definition;
            int level = d.ResolveLevel(previewLevel > 0 ? previewLevel : item.level);
            bool preview = level != item.level;
            string body = preview ? item.SummaryAtLevel(level) : item.StatSummary();
            float scale = Mathf.Pow(2, level - 1);
            string headline = "";
            if (d.kind == PrototypeItemKind.Equipment)
            {
                if (item.HasActive) headline = "冷却 <b>" + item.BaseCooldown.ToString("0.#") + "s</b>";
                if (d.weapon != null)
                {
                    float damage = (d.weapon.damage * scale + item.WeaponGrowth) * item.encounterOutputScale;
                    if (damage > 0) headline += "    伤害 <b>" + damage.ToString("0.#") + "</b>";
                    string weaponLine = (d.weapon.kind == PrototypeWeaponKind.Melee ? "近战" : "远程") + " " + damage.ToString("0") + "伤 / " + item.BaseCooldown.ToString("0.0") + "秒";
                    body = body.Replace(weaponLine, "");
                }
                if ((d.weapon == null || d.weapon.damage <= 0) && d.effects != null)
                {
                    for (int i = 0; i < d.effects.Length; i++)
                    {
                        var effect = d.effects[i];
                        if (effect == null || ((int)effect.kind > (int)PrototypeEffectKind.Regeneration && !Percent(effect.kind))) continue;
                        float output = preview ? (effect.BaseAt(level) + item.GrowthAmount(effect)) * item.encounterOutputScale : item.EffectValue(i);
                        string label = effect.kind == PrototypeEffectKind.GrowBattleMaxHealth ? "生命成长" :
                            effect.kind == PrototypeEffectKind.OwnMaxHealthDamage ? "生命伤害" : Names[(int)effect.kind];
                        headline += (label.Length > 6 ? "\n" : "    ") + label + " <b>" + (Percent(effect.kind) ? (output * 100).ToString("0.#") + "%" : output.ToString("0.#")) + "</b>";
                        break;
                    }
                }
                body = body.Replace("冷却 " + d.activationCooldown.ToString("0.#") + "秒  ", "");
            }
            var visibleLines = new System.Collections.Generic.List<string>();
            foreach (string line in body.Split('\n'))
                if (!line.TrimStart().StartsWith("成长 Buff ·")) visibleLines.Add(line);
            body = string.Join("\n", visibleLines);
            if (!string.IsNullOrEmpty(d.Classification)) body = body.Replace(d.Classification, "");
            string color = d.rarity == PrototypeItemRarity.Legendary ? "FFD576" : d.rarity == PrototypeItemRarity.Epic ? "DCACFF" : d.rarity == PrototypeItemRarity.Rare ? "9BD4FF" : "D8D2C8";
            string title = "<size=36><b>" + d.displayName + "</b></size>\n<color=#" + color + "><size=22>" + d.RarityText + "  ·  Lv." + level + (preview ? "  升级预览" : "") + "</size></color>";
            if (headline.Length > 0) title += "\n\n<size=30>" + headline + "</size>";
            string mechanic = d.itemId == "green_flying_sword" ? "每次释放召唤一把飞剑，持续穿刺敌人。再次释放时，场上全部飞剑共同齐射。" :
                d.itemId == "slime_core" ? "每次核心发动，体型继续增大，最大显示为初始的 2.2 倍。" :
                d.itemId == "death_scythe" ? "超远距离挥出巨型镰刀，横扫路径上的敌人。" : "";
            title += "\n<color=#AB9680>━━━━━━━━━━━━━━━━</color>\n";
            if (mechanic.Length > 0) title += "<color=#FFE5B5>" + mechanic + "</color>\n\n";
            title += body.Trim();
            if (d.tags != null && d.tags.Length > 0) title += "\n\n<color=#E9CCA5><size=20>【" + string.Join("】  【", d.tags) + "】</size></color>";
            return title;
        }
        public static bool Percent(PrototypeEffectKind kind)=>kind==PrototypeEffectKind.TargetMaxHealthDamage||kind==PrototypeEffectKind.GrowBattleMaxHealth||kind==PrototypeEffectKind.OwnMaxHealthDamage||kind==PrototypeEffectKind.MasterCooldown||kind==PrototypeEffectKind.ConsumeShieldDamage;
        public static string Unit(PrototypeEffectKind kind)=>Percent(kind)?"比例（0.05 = 5%）":kind==PrototypeEffectKind.Invulnerability?"秒":kind==PrototypeEffectKind.Regeneration?"每秒": "数值";
        public static string Summary(PrototypeItemInstance item,int index) {
            var e=item.definition.effects[index];float value=item.EffectValue(index);
            string amount=Percent(e.kind)?(value*100).ToString("0.##")+"%":value.ToString("0.##")+(e.kind==PrototypeEffectKind.Invulnerability?"秒":e.kind==PrototypeEffectKind.Regeneration?"/秒":"");
            if(e.kind==PrototypeEffectKind.MasterCooldown)amount=(Mathf.Clamp01((e.HasLevelAmounts||e.amount>0?e.BaseAt(item.level):PrototypeEffectMath.MasterReduction(item.level))+item.GrowthAmount(e))*100).ToString("0.##")+"%（最低1秒）";
            if(e.kind==PrototypeEffectKind.TriggerApprentices)amount=e.HasLevelAmounts||e.amount>0?Mathf.Max(0,Mathf.FloorToInt(item.EffectValue(index))).ToString()+"件学徒":item.level==4?"全部学徒":item.LevelMultiplier.ToString("0")+"件学徒";
            if(e.kind==PrototypeEffectKind.ReplayPotions)amount="全部已吸收配方";
            string text=TriggerNames[(int)e.trigger]+" · "+Names[(int)e.kind]+" · "+amount;
            if(e.trigger!=PrototypeEffectTrigger.Release)text+=" · 触发间隔 "+e.triggerCooldown.ToString("0.##")+"秒";
            if(!string.IsNullOrEmpty(e.matchTag))text+=" · 匹配 "+e.matchTag;
            if(e.criticalChance>0 && (e.kind==PrototypeEffectKind.Damage||e.kind==PrototypeEffectKind.TargetMaxHealthDamage||e.kind==PrototypeEffectKind.OwnMaxHealthDamage||e.kind==PrototypeEffectKind.ConsumeShieldDamage))text+=" · 暴击 "+(e.criticalChance*100).ToString("0.##")+"% ×"+e.criticalMultiplier.ToString("0.##");
            if(e.GrowthAt(item.level)>0)text+=" · 每次触发成长 +"+e.GrowthAt(item.level).ToString("0.##");string color=e.kind==PrototypeEffectKind.Damage?"FF7777":e.kind==PrototypeEffectKind.Shield?"E3E8F2":e.kind==PrototypeEffectKind.Heal?"89F29A":e.kind==PrototypeEffectKind.Poison?"70D858":e.kind==PrototypeEffectKind.Burn?"FF954B":"F5DB82";return "<color=#"+color+">"+text+"</color>";
        }
    }
}
