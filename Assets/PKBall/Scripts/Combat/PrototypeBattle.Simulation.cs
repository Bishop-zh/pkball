using System.Collections.Generic;
using UnityEngine;

namespace PKBall.Prototype
{
    public readonly struct PrototypeDamageRecord
    {
        public readonly long eventId, rngSequence;
        public readonly int step, targetId;
        public readonly float amount;
        public readonly PrototypeDamageKind kind;
        public PrototypeDamageRecord(long id,int step,int targetId,float amount,PrototypeDamageKind kind,long rngSequence)
        {eventId=id;this.step=step;this.targetId=targetId;this.amount=amount;this.kind=kind;this.rngSequence=rngSequence;}
    }
    public sealed partial class PrototypeBattle
    {
        private struct ActorContact
        {
            public PrototypeActor a,b;
            public float speed;
            public Vector3 direction;
        }
        private readonly List<ActorContact> contacts = new List<ActorContact>(4);
        private readonly PrototypeCombatRandom combatRandom = new PrototypeCombatRandom();
        public int StepIndex { get; private set; }
        public uint CombatSeed { get; private set; }
        public long RandomSequence => combatRandom.Sequence;
        private double simulationTime;
        private readonly PrototypeDamageRecord[] damageTrace = new PrototypeDamageRecord[256];
        public long DamageEventCount { get; private set; }
        public PrototypeDamageRecord[] RecentDamageRecords()
        {
            int count=(int)System.Math.Min(DamageEventCount,damageTrace.Length);
            var result=new PrototypeDamageRecord[count];
            for(int i=0;i<count;i++)result[i]=damageTrace[(int)((DamageEventCount-count+i)%damageTrace.Length)];
            return result;
        }
        private void TraceDamage(PrototypeActor target,float amount,PrototypeDamageKind kind)
        {
            long id=++DamageEventCount;
            damageTrace[(int)((id-1)%damageTrace.Length)]=new PrototypeDamageRecord(id,StepIndex,target.Id,amount,kind,RandomSequence);
        }
        public float NextCombatRandom() => combatRandom.NextValue();

        // Physics callbacks only collect contacts. Drain at the next simulation boundary.
        public void ResolveActorCollision(PrototypeActor a, PrototypeActor b, float relativeNormalSpeed)
        {
            if(!IsFighting || a==null || b==null || !a.IsAlive || !b.IsAlive)return;
            if(a.Id>b.Id){var swap=a;a=b;b=swap;}
            var contact=new ActorContact {a=a,b=b,speed=relativeNormalSpeed,direction=(b.transform.position-a.transform.position).normalized};
            for(int i=0;i<contacts.Count;i++)
            {
                if(contacts[i].a!=a || contacts[i].b!=b)continue;
                if(relativeNormalSpeed>contacts[i].speed)contacts[i]=contact;
                return;
            }
            contacts.Add(contact);
        }

        private void DrainContacts()
        {
            contacts.Sort(CompareContacts);
            foreach(var contact in contacts)
            {
                if(contact.a==null || contact.b==null || !contact.a.IsAlive || !contact.b.IsAlive)continue;
                ProcessActorCollision(contact.a,contact.b,contact.speed,contact.direction);
            }
            contacts.Clear();
        }
        private static int CompareContacts(ActorContact a,ActorContact b)
        {
            int order=a.a.Id.CompareTo(b.a.Id);
            return order!=0 ? order : a.b.Id.CompareTo(b.b.Id);
        }
    }
}
