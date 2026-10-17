using UnityEngine;
namespace PKBall.Prototype
{
    // All statuses use battle time. They are combat-only and do not enter the run save.
    public sealed class PrototypeCombatState
    {
        public readonly PrototypeBuffManager Buffs = new PrototypeBuffManager();
        public float Shield => Buffs.StatusAmount(PrototypeEffectKind.Shield);
        public float Poison => Buffs.StatusAmount(PrototypeEffectKind.Poison);
        public float Burn => Buffs.StatusAmount(PrototypeEffectKind.Burn);
        public float Regeneration => Buffs.StatusAmount(PrototypeEffectKind.Regeneration);
        private float nextSecond = 1f, nextBurn = .5f;
        public void AddShield(float amount) { Buffs.AddStatus(PrototypeEffectKind.Shield, amount); }
        public void AddPoison(float amount) { Buffs.AddStatus(PrototypeEffectKind.Poison, amount); }
        public void AddBurn(float amount) { Buffs.AddStatus(PrototypeEffectKind.Burn, amount); }
        public void AddRegeneration(float amount) { Buffs.AddStatus(PrototypeEffectKind.Regeneration, amount); }
        public void CleanseByHealing(float actual) { Buffs.RemoveStatus(PrototypeEffectKind.Poison, actual * .1f); }
        public float Absorb(float damage, PrototypeDamageKind kind)
        {
            if (kind == PrototypeDamageKind.Poison || kind == PrototypeDamageKind.Exhaustion || kind == PrototypeDamageKind.SuddenDeath) return damage;
            float conversion = kind == PrototypeDamageKind.Burn ? 2f : 1f;
            float consumed = Mathf.Min(Shield, damage / conversion);
            Buffs.RemoveStatus(PrototypeEffectKind.Shield, consumed);
            return Mathf.Max(0, damage - consumed * conversion);
        }
        public void Advance(PrototypeActor actor, float now)
        {
            // Stable chronological order even if a future simulation step is larger than .5s.
            while (Mathf.Min(nextSecond, nextBurn) <= now && actor.IsAlive)
            {
                if (nextBurn < nextSecond)
                {
                    nextBurn += .5f; TickBurn(actor);
                }
                else
                {
                    nextSecond += 1f;
                    if (Poison > 0) actor.ReceiveDamage(Poison, PrototypeDamageKind.Poison);
                    if (actor.IsAlive && nextBurn <= now) { nextBurn += .5f; TickBurn(actor); }
                    if (actor.IsAlive && Regeneration > 0) actor.Heal(Regeneration);
                }
            }
        }
        private void TickBurn(PrototypeActor actor)
        {
            if (Burn <= 0) return;
            actor.ReceiveDamage(Burn, PrototypeDamageKind.Burn);
            Buffs.RemoveStatus(PrototypeEffectKind.Burn, Mathf.Max(1, Mathf.Ceil(Burn * .05f)));
        }
    }
}
