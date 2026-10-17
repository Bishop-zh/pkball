using System;
using UnityEngine;
using System.Collections.Generic;

namespace PKBall.Prototype
{
    [Serializable]
    public sealed class PrototypeItemInstance
    {
        [NonSerialized] public float encounterOutputScale = 1f;
        public long instanceId;
        public PrototypeItemDefinition definition;
        public int level = 1;
        public int quantity=1;
        public int investedGold;
        public float permanentDamage;
        public int lastSwallowDay;
        public bool breakthroughGranted;
        public int productionProgress;
        public float growth;
        public List<PrototypeItemModifier> modifiers = new List<PrototypeItemModifier>();
        public bool modifierMigrationDone;
        public void EnsureModifiers()
        {
            if (modifiers == null) modifiers = new List<PrototypeItemModifier>();
            PrototypeModifierLedger.EnsureEffectIds(definition);
            if (modifierMigrationDone || definition == null) return;
            PrototypeModifierLedger.Migrate(modifiers, definition, growth, cooldownMultiplier);
            modifierMigrationDone = true;
        }
        public float GrowthAmount(string sourceId) { EnsureModifiers(); return PrototypeModifierLedger.Amount(modifiers, sourceId); }
        public float GrowthAmount(PrototypeEffectDefinition effect)
        {
            EnsureModifiers();
            int index = System.Array.IndexOf(definition.effects, effect);
            return index < 0 ? 0 : GrowthAmount(PrototypeModifierLedger.EffectKey(index, effect));
        }
        public void AddGrowth(string sourceId, PrototypeEffectKind kind, float amount, int stacks = 1)
        { EnsureModifiers(); PrototypeModifierLedger.Add(modifiers, sourceId, kind, amount, stacks); }
        public void AddActivationGrowth(float scale, int stacks = 1)
        { EnsureModifiers(); PrototypeModifierLedger.AddActivation(modifiers, definition, scale, stacks, level); growth += scale; }
        public float CooldownFactor { get { EnsureModifiers(); return PrototypeModifierLedger.Cooldown(modifiers); } }
        public void ReduceCooldown(float rate)
        {
            EnsureModifiers();
            float original = definition.weapon == null ? definition.activationCooldown : definition.weapon.cooldown;
            float old = CooldownFactor;
            float next = Mathf.Max(1f / Mathf.Max(1f, original), old * (1f-rate));
            AddGrowth("cooldown", PrototypeEffectKind.MasterCooldown, Mathf.Log(next/old));
            cooldownMultiplier = next;
        }
        public void MergeModifiersFrom(PrototypeItemInstance source)
        {
            EnsureModifiers(); source.EnsureModifiers();
            float old = CooldownFactor;
            float next = Mathf.Min(old, source.CooldownFactor);
            PrototypeModifierLedger.Merge(modifiers, source.modifiers, 1);
            PrototypeModifierLedger.Add(modifiers, "cooldown", PrototypeEffectKind.MasterCooldown, Mathf.Log(next/old), 0);
            cooldownMultiplier = next;
            growth += source.growth;
        }
        public PrototypeInheritedProgram CreateInheritedProgram()
        {
            EnsureModifiers();
            var program = new PrototypeInheritedProgram {definitionId=definition.itemId, level=level,
                scale=LevelMultiplier, growth=growth, cooldownMultiplier=CooldownFactor, modifierMigrationDone=true};
            foreach (var modifier in modifiers) if (modifier != null)
                program.modifiers.Add(new PrototypeItemModifier {sourceId=modifier.sourceId,kind=modifier.kind,amount=modifier.amount,stacks=modifier.stacks});
            return program;
        }
        public float EffectValue(int index)
        {
            var effect = definition.effects[index];
            return (effect.BaseAt(level) + GrowthAmount(effect)) * ((int)effect.kind <= (int)PrototypeEffectKind.Regeneration ? encounterOutputScale : 1f);
        }
        public float cooldownMultiplier = 1f;
        public float inheritedHealth, inheritedSpeed, inheritedImpact, inheritedReduction;
        public readonly List<PrototypeInheritedProgram> inherited = new List<PrototypeInheritedProgram>();
        public readonly List<PrototypePotionRecipe> recipes = new List<PrototypePotionRecipe>();
        public float GoldRemainder;
        public bool HasActive => definition.HasActive || inherited.Count > 0;
        private static readonly Dictionary<string,PrototypeItemDefinition> inheritedDefinitions = new Dictionary<string,PrototypeItemDefinition>();
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearDefinitionCache(){inheritedDefinitions.Clear();}
        public bool HasTag(string tag)
        {
            if (definition.HasTag(tag)) return true;
            foreach (var program in inherited)
            {
                if(!inheritedDefinitions.TryGetValue(program.definitionId,out var d))
                {d=PrototypeItemRepository.Find(program.definitionId);inheritedDefinitions[program.definitionId]=d;}
                if (d != null && d.HasTag(tag)) return true;
            }
            return false;
        }

        public string Name => definition == null ? "未知道具" : definition.displayName +
            (definition.kind == PrototypeItemKind.Equipment ? "  Lv." + level : "");
        public float LevelMultiplier => level <= 1 ? 1f : level == 2 ? 2f : level == 3 ? 4f : 8f;
        public float EffectMultiplier => level <= 1 ? 1f : level == 2 ? 2f : level == 3 ? 4f : 8f;
        public float WeaponGrowth => definition.weapon == null ? 0 : GrowthAmount("weapon");
        public float BaseCooldown => Mathf.Max(1f, (definition.weapon == null ? definition.activationCooldown : definition.weapon.cooldown) * CooldownFactor);
        public int SellPrice => definition!=null && definition.itemId=="gold_ore" ? Math.Max(1,definition.price)*quantity : Math.Max(1, investedGold / 2);

        public string SummaryAtLevel(int target)
        {EnsureModifiers();var clone=new PrototypeItemInstance{definition=definition,level=target,growth=growth,cooldownMultiplier=cooldownMultiplier,permanentDamage=permanentDamage,inheritedHealth=inheritedHealth,inheritedSpeed=inheritedSpeed,inheritedImpact=inheritedImpact,inheritedReduction=inheritedReduction,quantity=quantity,encounterOutputScale=encounterOutputScale,modifierMigrationDone=true}; foreach(var modifier in modifiers) clone.modifiers.Add(new PrototypeItemModifier {sourceId=modifier.sourceId,kind=modifier.kind,amount=modifier.amount,stacks=modifier.stacks});clone.inherited.AddRange(inherited);clone.recipes.AddRange(recipes);return clone.StatSummary();}
        private static string ModifierName(PrototypeEffectKind kind)
        {
            switch(kind)
            {
                case PrototypeEffectKind.Damage:return "伤害";
                case PrototypeEffectKind.Shield:return "护盾";
                case PrototypeEffectKind.Heal:return "治疗";
                case PrototypeEffectKind.Poison:return "中毒";
                case PrototypeEffectKind.Burn:return "灼烧";
                case PrototypeEffectKind.Regeneration:return "再生";
                default:return kind.ToString();
            }
        }
        public string StatSummary()
        {
            if (definition == null) return "";
            if(definition.kind==PrototypeItemKind.Material)return $"数量 {quantity} · 出售获得 {SellPrice} 金币";
            if (definition.kind != PrototypeItemKind.Equipment) return definition.StatSummary();
            string summary = "";
            if (definition.healthBonus != 0 || inheritedHealth != 0) summary += $"生命 +{definition.healthBonus * LevelMultiplier+inheritedHealth:0}  ";
            if (definition.speedBonus != 0 || inheritedSpeed != 0) summary += $"速度 +{definition.speedBonus * LevelMultiplier+inheritedSpeed:0}  ";
            if (definition.impactBonus != 0 || permanentDamage != 0 || inheritedImpact != 0)
                summary += $"撞击 +{(definition.impactBonus * EffectMultiplier + permanentDamage + inheritedImpact) * 100f:0.#}%  ";
            if (definition.impactTakenReduction != 0 || inheritedReduction != 0)
                summary += $"撞击减伤 {(definition.impactTakenReduction * EffectMultiplier+inheritedReduction) * 100f:0.#}%  ";
            if (definition.weapon != null)
                summary += $"{(definition.weapon.kind == PrototypeWeaponKind.Melee ? "近战" : "远程")} {((definition.weapon.damage * LevelMultiplier + WeaponGrowth)*encounterOutputScale):0}伤 / {BaseCooldown:0.0}秒";
            if (definition.effects != null) for (int i=0; i<definition.effects.Length; i++) summary += "\n" + PrototypeEffectDisplay.Summary(this,i);
            if (definition.weapon != null && definition.weapon.growthPerActivation > 0)
                summary += $"\n每次释放伤害成长 +{definition.weapon.growthPerActivation * LevelMultiplier:0.#}";
            if (definition.productionInterval > 0)
                summary += $"\n每 {definition.ProductionInterval(level):0.#}秒产1管{(definition.randomPotionProducer ? "随机短效药" : "针剂")}";
            EnsureModifiers();
            foreach (var modifier in modifiers) if (modifier.sourceId != "cooldown" && modifier.amount != 0)
                summary += $"\n成长 Buff · {ModifierName(modifier.kind)} +{modifier.amount:0.##}（{modifier.stacks}层）";

            if (cooldownMultiplier < 1) summary += $"\n永久冷却倍率 ×{cooldownMultiplier:0.###}";
            foreach(var program in inherited)
            {
                var d=PrototypeItemRepository.Find(program.definitionId);
                if(d!=null) summary+=$"\n继承 {d.displayName}：输出权重 {program.scale:0.###} / 成长 {program.growth:0.###}";
            }
            foreach (var recipe in recipes) summary += $"\n每次复放：{PrototypeItemDefinition.StatName(recipe.stat)} +{recipe.amount:0.###}";
            return summary;
        }
    }
}
