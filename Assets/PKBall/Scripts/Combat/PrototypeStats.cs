using System;
using System.Collections.Generic;
using UnityEngine;

namespace PKBall.Prototype
{
    public enum PrototypeStat { Health, Speed, ImpactBonus, ImpactTakenReduction, MeleeDamageBonus, RangedDamageBonus, DamageBonus, DamageReduction, StartingShield, Gold }
    public enum PrototypeModifierScope { RunPermanent, WhileEquipped, CombatTimed }

    [Serializable]
    public struct PrototypeStatModifier
    {
        public string sourceId;
        public PrototypeStat stat;
        public float amount;
        public PrototypeModifierScope scope;
    }

    public sealed class PrototypeStatSnapshot
    {
        public float health = PrototypeRun.BaseHealth;
        public float speed = 100f;
        public float impactBonus;
        public float impactTakenReduction;
        public float meleeDamageBonus;
        public float rangedDamageBonus;
        public float damageBonus, damageReduction, startingShield;
        public readonly List<PrototypeStatModifier> sources = new List<PrototypeStatModifier>();

        public float ImpactTakenMultiplier => Mathf.Clamp(1f - impactTakenReduction, 0.4f, 1f);
        public void Add(PrototypeStatModifier modifier)
        {
            sources.Add(modifier);
            switch (modifier.stat)
            {
                case PrototypeStat.Health: health += modifier.amount; break;
                case PrototypeStat.DamageBonus: damageBonus += modifier.amount; break;
                case PrototypeStat.DamageReduction: damageReduction = 1f - (1f-damageReduction)*(1f-Mathf.Clamp01(modifier.amount)); break;
                case PrototypeStat.StartingShield: startingShield += modifier.amount; break;
                case PrototypeStat.Speed: speed += modifier.amount; break;
                case PrototypeStat.ImpactBonus: impactBonus += modifier.amount; break;
                case PrototypeStat.ImpactTakenReduction: impactTakenReduction += modifier.amount; break;
                case PrototypeStat.MeleeDamageBonus: meleeDamageBonus += modifier.amount; break;
                case PrototypeStat.RangedDamageBonus: rangedDamageBonus += modifier.amount; break;
            }
        }
    }

    public static class PrototypeStatCalculator
    {
        public static PrototypeStatSnapshot Calculate(PrototypeRun run)
        {
            PrototypeStatSnapshot stats = new PrototypeStatSnapshot {health=run.BodyHealth};
            for (int i = 0; i < run.PermanentModifiers.Count; i++)
                {
                var m=run.PermanentModifiers[i];
                if(m.stat == PrototypeStat.Health) stats.sources.Add(m); else stats.Add(m);
            }
            for (int i = 0; i < run.Equipped.Count; i++)
            {
                PrototypeItemInstance item = run.Equipped[i];
                if (item == null || item.definition == null) continue;
                AddEquipment(stats,item,run.SlotEffects[i],item.Name + " / " + PrototypeRun.SlotOrder[i]);
            }
            foreach (var modifier in run.PendingBattleModifiers)
                stats.Add(new PrototypeStatModifier {sourceId="下一场药水", stat=modifier.stat, amount=modifier.amount, scope=PrototypeModifierScope.CombatTimed});
            stats.health = Mathf.Max(1f, stats.health);
            stats.speed = Mathf.Max(1f, stats.speed);
            stats.impactTakenReduction = Mathf.Clamp(stats.impactTakenReduction, 0f, 0.6f);
            return stats;
        }

        public static void AddEquipment(PrototypeStatSnapshot stats, PrototypeItemInstance item,
            PrototypeSlotEffect slotEffect=PrototypeSlotEffect.None, string sourceLabel=null)
        {
            if(item==null || item.definition==null)return;
                PrototypeItemDefinition definition = item.definition;
                string source = sourceLabel ?? item.Name;
                float scale = item.LevelMultiplier;
                Add(stats, source, PrototypeStat.Health, definition.healthBonus * scale + item.inheritedHealth);
                Add(stats, source, PrototypeStat.Speed, definition.speedBonus * scale + item.inheritedSpeed);
                Add(stats, source, PrototypeStat.ImpactBonus, definition.impactBonus * item.EffectMultiplier + item.inheritedImpact);
                Add(stats, source + " / 吞并成长", PrototypeStat.ImpactBonus, item.permanentDamage);
                Add(stats, source, PrototypeStat.ImpactTakenReduction,
                    definition.impactTakenReduction * item.EffectMultiplier + item.inheritedReduction);
                switch (slotEffect)
                {
                    case PrototypeSlotEffect.Vital:
                        Add(stats, source + " / 活力格", PrototypeStat.Health,
                            8f + definition.healthBonus * scale * .2f); break;
                    case PrototypeSlotEffect.Swift:
                        Add(stats, source + " / 迅捷格", PrototypeStat.Speed,
                            4f + definition.speedBonus * scale * .2f); break;
                    case PrototypeSlotEffect.Guarded:
                        Add(stats, source + " / 守护格", PrototypeStat.ImpactTakenReduction, .05f);
                        break;
                }
        }

        private static void Add(PrototypeStatSnapshot stats, string source, PrototypeStat stat, float amount)
        {
            if (Mathf.Abs(amount) < 0.0001f) return;
            stats.Add(new PrototypeStatModifier
            {
                sourceId = source, stat = stat, amount = amount,
                scope = PrototypeModifierScope.WhileEquipped
            });
        }
    }
}
