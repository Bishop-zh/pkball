using System.Collections.Generic;
using UnityEngine;

namespace PKBall.Prototype
{
    // Combat-only modifiers are keyed by source. Refreshing never accumulates stale bonuses.
    public sealed class PrototypeBuffManager
    {
        public readonly struct Snapshot
        {
            public readonly string sourceId;
            public readonly PrototypeStat stat;
            public readonly float amount;
            public readonly int expiresAtStep;
            public Snapshot(string sourceId,PrototypeStat stat,float amount,int expiresAtStep)
            {this.sourceId=sourceId;this.stat=stat;this.amount=amount;this.expiresAtStep=expiresAtStep;}
        }
        private struct ActiveBuff
        {
            public string sourceId;
            public PrototypeStat stat;
            public float amount;
            public int expiresAtStep;
        }

        private readonly List<ActiveBuff> active = new List<ActiveBuff>(8);
        // Combat status Buffs aggregate by type. Their tick rules live in CombatState.
        private readonly float[] statuses = new float[(int)PrototypeEffectKind.Regeneration + 1];
        public float StatusAmount(PrototypeEffectKind kind) => statuses[(int)kind];
        public void AddStatus(PrototypeEffectKind kind, float amount)
        { statuses[(int)kind] += Mathf.Max(0, amount); }
        public void RemoveStatus(PrototypeEffectKind kind, float amount)
        { statuses[(int)kind] = Mathf.Max(0, statuses[(int)kind] - Mathf.Max(0, amount)); }

        public void AddOrRefresh(string sourceId, PrototypeStat stat, float amount,
            int currentStep, int durationSteps)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].sourceId != sourceId || active[i].stat != stat) continue;
                active[i] = new ActiveBuff
                {
                    sourceId = sourceId, stat = stat, amount = amount,
                    expiresAtStep = currentStep + durationSteps
                };
                return;
            }
            active.Add(new ActiveBuff
            {
                sourceId = sourceId, stat = stat, amount = amount,
                expiresAtStep = currentStep + durationSteps
            });
        }

        // Lifetime stacks aggregate into one entry per source; no per-trigger allocation.
        public void AddStack(string sourceId, PrototypeStat stat, float amount)
        {
            for (int i=0; i<active.Count; i++)
            {
                if (active[i].sourceId != sourceId || active[i].stat != stat) continue;
                var buff = active[i]; buff.amount += amount; active[i] = buff; return;
            }
            active.Add(new ActiveBuff { sourceId=sourceId, stat=stat, amount=amount, expiresAtStep=int.MaxValue });
        }
        public void Advance(int step)
        {
            for (int i = active.Count - 1; i >= 0; i--)
                if (step >= active[i].expiresAtStep) active.RemoveAt(i);
        }

        public void AddReduction(string sourceId,float amount)
        {
            amount=Mathf.Clamp01(amount);
            for(int i=0;i<active.Count;i++)
            {
                var entry=active[i];
                if(entry.sourceId!=sourceId || entry.stat!=PrototypeStat.DamageReduction)continue;
                entry.amount=1-(1-entry.amount)*(1-amount);active[i]=entry;return;
            }
            AddStack(sourceId,PrototypeStat.DamageReduction,amount);
        }

        public float DamageTakenFraction()
        {
            float fraction=1;
            foreach(var entry in active)if(entry.stat==PrototypeStat.DamageReduction)fraction*=1-Mathf.Clamp01(entry.amount);
            return fraction;
        }

        public void RemoveSource(string sourceId)
        {
            for(int i=active.Count-1;i>=0;i--)if(active[i].sourceId==sourceId)active.RemoveAt(i);
        }

        public Snapshot[] GetSnapshot()
        {
            var result=new Snapshot[active.Count];
            for(int i=0;i<active.Count;i++){var b=active[i];result[i]=new Snapshot(b.sourceId,b.stat,b.amount,b.expiresAtStep);}
            return result;
        }

        public float Sum(PrototypeStat stat)
        {
            float total = 0f;
            for (int i = 0; i < active.Count; i++)
                if (active[i].stat == stat) total += active[i].amount;
            return total;
        }

        public int Count => active.Count;
        public void Clear() { active.Clear(); System.Array.Clear(statuses, 0, statuses.Length); }
    }
}
