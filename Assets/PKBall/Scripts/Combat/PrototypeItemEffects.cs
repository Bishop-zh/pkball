using System;
using System.Collections.Generic;
using UnityEngine;

namespace PKBall.Prototype
{
    public enum PrototypeEffectKind { Damage, Shield, Heal, Poison, Burn, Regeneration, MasterCooldown, TriggerApprentices, ReplayPotions, TargetMaxHealthDamage, Invulnerability, GrowBattleMaxHealth, OwnMaxHealthDamage, ConsumeShieldDamage, TriggerTaggedEquipment }
    public enum PrototypeEffectTrigger { Release, Hit, BurningHit, ShieldBroken }
    public enum PrototypePotionLifetime { RunPermanent, NextBattle }
    [Serializable] public sealed class PrototypeEffectDefinition
    {
        public string id;
        public PrototypeEffectTrigger trigger;
        public float triggerCooldown = 1f;
        public float[] levelAmounts, levelGrowthAmounts;
        public PrototypeEffectKind kind;
        public float amount;
        public float growthPerActivation;
        public string matchTag;
        public float criticalChance, criticalMultiplier;
        public bool HasLevelAmounts => levelAmounts != null && levelAmounts.Length == 4;
        public bool IsRatio => kind == PrototypeEffectKind.TargetMaxHealthDamage || kind == PrototypeEffectKind.OwnMaxHealthDamage || kind == PrototypeEffectKind.GrowBattleMaxHealth || kind == PrototypeEffectKind.Invulnerability || kind == PrototypeEffectKind.MasterCooldown || kind == PrototypeEffectKind.ConsumeShieldDamage;
        public float BaseAt(int level) => HasLevelAmounts ? levelAmounts[Mathf.Clamp(level,1,4)-1] : amount * (IsRatio ? 1 : Mathf.Pow(2,Mathf.Clamp(level,1,4)-1));
        public float GrowthAt(int level) => levelGrowthAmounts != null && levelGrowthAmounts.Length == 4 ? levelGrowthAmounts[Mathf.Clamp(level,1,4)-1] : growthPerActivation * Mathf.Pow(2,Mathf.Clamp(level,1,4)-1);
    }
    // One aggregate record per source and effect, regardless of stack count.
    [Serializable] public sealed class PrototypeItemModifier
    {
        public string sourceId;
        public PrototypeEffectKind kind;
        public float amount;
        public int stacks;
    }
    public static class PrototypeModifierLedger
    {
        public static string EffectKey(int index, PrototypeEffectKind kind) => "effect:" + index + ":" + kind;
        public static string EffectKey(int index, PrototypeEffectDefinition effect) => string.IsNullOrEmpty(effect.id) ? EffectKey(index, effect.kind) : effect.id;
        public static void EnsureEffectIds(PrototypeItemDefinition definition)
        {
            if (definition == null || definition.effects == null) return;
            for (int i=0; i<definition.effects.Length; i++)
                if (definition.effects[i] != null && string.IsNullOrEmpty(definition.effects[i].id))
                    definition.effects[i].id = EffectKey(i, definition.effects[i].kind);
        }
        public static float Amount(List<PrototypeItemModifier> records, string sourceId)
        {
            float amount = 0;
            if (records != null) foreach (var record in records)
                if (record != null && record.sourceId == sourceId) amount += record.amount;
            return amount;
        }
        public static void Add(List<PrototypeItemModifier> records, string sourceId, PrototypeEffectKind kind, float amount, int stacks = 1)
        {
            if (amount == 0) return;
            foreach (var record in records) if (record != null && record.sourceId == sourceId && record.kind == kind)
            { record.amount += amount; record.stacks += stacks; return; }
            records.Add(new PrototypeItemModifier { sourceId=sourceId, kind=kind, amount=amount, stacks=stacks });
        }
        public static void Migrate(List<PrototypeItemModifier> records, PrototypeItemDefinition definition, float growth, float cooldown)
        {
            if (definition != null) AddActivation(records, definition, growth, 0);
            if (cooldown > 0 && cooldown != 1) Add(records, "cooldown", PrototypeEffectKind.MasterCooldown, Mathf.Log(cooldown), 0);
        }
        public static void AddActivation(List<PrototypeItemModifier> records, PrototypeItemDefinition definition, float scale, int stacks = 1, int level = 1)
        {
            if (definition == null) return;
            EnsureEffectIds(definition);
            if (definition.weapon != null) Add(records, "weapon", PrototypeEffectKind.Damage, definition.weapon.growthPerActivation * scale, stacks);
            if (definition.effects == null) return;
            for (int i=0; i<definition.effects.Length; i++)
            {
                var effect = definition.effects[i];
                if(effect.trigger != PrototypeEffectTrigger.Release)continue;
                float amount=effect.levelGrowthAmounts != null && effect.levelGrowthAmounts.Length==4 ? effect.GrowthAt(level)*scale/Mathf.Pow(2,level-1) : effect.growthPerActivation*scale;
                Add(records, EffectKey(i, effect), effect.kind, amount, stacks);
            }
        }
        public static float Cooldown(List<PrototypeItemModifier> records) => Mathf.Exp(Amount(records, "cooldown"));
        public static void Merge(List<PrototypeItemModifier> target, List<PrototypeItemModifier> source, float fraction)
        {
            if (source == null) return;
            foreach (var record in source) if (record != null && record.sourceId != "cooldown")
                Add(target, record.sourceId, record.kind, record.amount * fraction, record.stacks);
        }
    }
    [Serializable] public sealed class PrototypeInheritedProgram
    {
        public string definitionId;
        public int level = 1;
        public float cooldownMultiplier = 1;
        public float scale;
        public float growth;
        public List<PrototypeItemModifier> modifiers = new List<PrototypeItemModifier>();
        public bool modifierMigrationDone;
        public void EnsureModifiers(PrototypeItemDefinition definition)
        {
            if (modifiers == null) modifiers = new List<PrototypeItemModifier>();
            PrototypeModifierLedger.EnsureEffectIds(definition);
            if (modifierMigrationDone || definition == null) return;
            PrototypeModifierLedger.Migrate(modifiers, definition, growth, cooldownMultiplier);
            modifierMigrationDone = true;
        }
        public float GrowthAmount(string sourceId) => PrototypeModifierLedger.Amount(modifiers, sourceId);
        public float CooldownFactor => modifierMigrationDone ? PrototypeModifierLedger.Cooldown(modifiers) : cooldownMultiplier;
        public void AddActivationGrowth(PrototypeItemDefinition definition, float scale)
        { EnsureModifiers(definition); PrototypeModifierLedger.AddActivation(modifiers, definition, scale, 1, level); growth += scale; }
        public void ReduceCooldown(PrototypeItemDefinition definition, float rate)
        {
            EnsureModifiers(definition);
            float original = definition.weapon == null ? definition.activationCooldown : definition.weapon.cooldown;
            float old = CooldownFactor;
            float next = Mathf.Max(1f / Mathf.Max(1f, original), old * (1f-rate));
            PrototypeModifierLedger.Add(modifiers, "cooldown", PrototypeEffectKind.MasterCooldown, Mathf.Log(next/old));
            cooldownMultiplier = next;
        }
        // Frequency credit for indivisible behavior; persistent, never rounded away.
        public float triggerCredit, productionCredit, cadenceCredit;
    }
    [Serializable] public sealed class PrototypePotionRecipe
    {
        public PrototypeStat stat;
        public PrototypePotionLifetime lifetime;
        public float amount;
    }
    [Serializable] public sealed class PrototypePendingModifier
    {
        public PrototypeStat stat;
        public float amount;
    }
    public struct PrototypeAttackPayload
    {
        public string sourceDefinitionId;
        public bool suppressHitTriggers;
        public string hitSoundId;public float hitVolume;
        public float damage, poison, burn, knockback;
        public PrototypeWeaponDefinition visual;public PrototypeHitVisual hitVisual;
        public Vector3 push;
        public PrototypeDamageKind kind;
        public PrototypeItemInstance item;
        public bool produceOre;
        public float oreCredit;
    }
    public static class PrototypeEffectMath
    {
        public static float Absorption(int level) => level <= 1 ? .1f : level == 2 ? .2f : level == 3 ? .5f : 1f;
        public static float MasterReduction(int level) => level <= 1 ? .05f : level == 2 ? .1f : level == 3 ? .2f : .5f;
        public static void MergeRecipe(List<PrototypePotionRecipe> list, PrototypeStat stat, PrototypePotionLifetime life, float value)
        {
            if (value == 0) return;
            foreach (var recipe in list) if (recipe.stat == stat && recipe.lifetime == life) { recipe.amount += value; return; }
            list.Add(new PrototypePotionRecipe {stat=stat, lifetime=life, amount=value});
        }
        public static void MergeProgram(List<PrototypeInheritedProgram> list, PrototypeInheritedProgram source, float fraction)
        {
            var definition = PrototypeItemRepository.Find(source.definitionId);
            source.EnsureModifiers(definition);
            foreach (var p in list) if (p.definitionId == source.definitionId && p.level == source.level)
            {
                p.EnsureModifiers(definition);
                float cooldown = Mathf.Min(p.CooldownFactor, source.CooldownFactor);
                PrototypeModifierLedger.Merge(p.modifiers, source.modifiers, fraction);
                PrototypeModifierLedger.Add(p.modifiers, "cooldown", PrototypeEffectKind.MasterCooldown, Mathf.Log(cooldown/p.CooldownFactor), 0);
                p.cooldownMultiplier = cooldown;
                p.scale += source.scale * fraction; p.growth += source.growth * fraction;
                p.cadenceCredit += source.cadenceCredit * fraction;
                p.triggerCredit += source.triggerCredit * fraction; p.productionCredit += source.productionCredit * fraction; return;
            }
            var merged = new PrototypeInheritedProgram {definitionId=source.definitionId, level=source.level, cooldownMultiplier=source.cooldownMultiplier, scale=source.scale*fraction,
                growth=source.growth*fraction, triggerCredit=source.triggerCredit*fraction, productionCredit=source.productionCredit*fraction, cadenceCredit=source.cadenceCredit*fraction, modifierMigrationDone=source.modifierMigrationDone};
            PrototypeModifierLedger.Merge(merged.modifiers, source.modifiers, fraction);
            if (source.modifierMigrationDone && source.CooldownFactor != 1)
                PrototypeModifierLedger.Add(merged.modifiers, "cooldown", PrototypeEffectKind.MasterCooldown, Mathf.Log(source.CooldownFactor), 0);
            list.Add(merged);
        }
    }
}
