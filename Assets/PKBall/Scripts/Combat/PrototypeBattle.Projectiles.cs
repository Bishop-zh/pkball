using System.Collections.Generic;
using UnityEngine;

namespace PKBall.Prototype
{
    public sealed partial class PrototypeBattle
    {
        private readonly List<PrototypeProjectile> activeProjectiles = new List<PrototypeProjectile>(64);
        private struct OrdinaryProjectile
        {
            public PrototypeActor source, target;
            public PrototypeAttackPayload payload;
            public PrototypeProjectileFlight flight;
            public PrototypeProjectile view;
        }
        private readonly List<OrdinaryProjectile> ordinaryProjectiles = new List<OrdinaryProjectile>(128);
        private readonly Stack<PrototypeProjectile> projectilePool = new Stack<PrototypeProjectile>();
        private struct PersistentSword
        {
            public PrototypeActor source, target;
            public PrototypeAttackPayload payload;
            public PrototypeSwordFlight flight;
            public PrototypeProjectile view;
        }
        private readonly List<PersistentSword> persistentSwords = new List<PersistentSword>(128);
        public bool ProjectileVisualsEnabled { get; set; } = true;

        public void FireProjectile(PrototypeActor source,PrototypeActor target,PrototypeWeaponDefinition definition,float damage)
        { FirePayload(source,target,new PrototypeAttackPayload {damage=damage,kind=PrototypeDamageKind.Ranged,visual=definition,hitVisual=definition.hitVisual},definition.projectileSpeed); }
        public void FirePayload(PrototypeActor source,PrototypeActor target,PrototypeAttackPayload payload,float speed)
        {
            speed=Mathf.Max(.1f,speed);
            var direction=(target.transform.position-source.transform.position).normalized;
            var origin=source.transform.position+direction*(source.Radius+.06f)+Vector3.up*.12f;
            PrototypeProjectile view = null;
            if (ProjectileVisualsEnabled && activeProjectiles.Count < 96)
            {
                view = projectilePool.Count > 0 ? projectilePool.Pop() : CreateProjectile();
                activeProjectiles.Add(view);
                view.Fire(this,source,origin,target,speed,payload);
            }
            ordinaryProjectiles.Add(new OrdinaryProjectile
            {
                source=source, target=target, payload=payload, view=view,
                flight=new PrototypeProjectileFlight {position=origin, speed=speed, startsAt=Elapsed+(payload.visual?.hitDelay??0)}
            });
        }

        private void AdvanceOrdinaryProjectiles(float deltaTime)
        {
            // New reaction projectiles begin next step, never recursively in this batch.
            int remaining = ordinaryProjectiles.Count;
            int index = 0;
            while (remaining-- > 0)
            {
                var shot = ordinaryProjectiles[index];
                bool cancelled = shot.source == null || !shot.source.IsAlive || shot.target == null || !shot.target.IsAlive;
                bool hit = !cancelled && shot.flight.Advance(Elapsed, deltaTime, shot.target.transform.position, shot.target.Radius);
                if (cancelled || hit)
                {
                    ordinaryProjectiles.RemoveAt(index);
                    if (shot.view != null) ReturnProjectile(shot.view);
                    if (hit) ApplyPayload(shot.source, shot.target, shot.payload);
                }
                else
                {
                    ordinaryProjectiles[index++] = shot;
                    if (shot.view != null) shot.view.Present(shot.flight.position, shot.target.transform.position);
                }
            }
        }

        private PrototypeProjectile CreateProjectile()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "PooledProjectile";
            visual.transform.SetParent(arenaRoot);
            visual.transform.localScale = Vector3.one * 0.18f;
            Collider projectileCollider = visual.GetComponent<Collider>();
            projectileCollider.enabled = false;
            // Disabled colliders remain on pooled projectiles.
            visual.GetComponent<Renderer>().material = CreateMaterial(new Color(1f, 0.88f, 0.4f));
            return visual.AddComponent<PrototypeProjectile>();
        }

        public const int FlyingSwordLimitPerActor=16;
        public int LogicalFlyingSwordCount(PrototypeActor owner){int count=0;foreach(var p in persistentSwords)if(p.source==owner)count++;return count;}
        public int FlyingSwordCount(PrototypeActor owner) {int count=0;foreach(var p in activeProjectiles)if(p.IsPersistentSword && p.Owner==owner)count++;return count;}
        public void ReleaseFlyingSword(PrototypeActor source,PrototypeActor target,PrototypeAttackPayload payload,float speed)
        {
            // Each blade owns a release-time snapshot; autonomous hits cannot recursively trigger equipment.
            payload.suppressHitTriggers=true;
            int index = LogicalFlyingSwordCount(source);
            var origin = source.transform.position+Vector3.up*1.15f;
            PrototypeProjectile view = null;
            if(ProjectileVisualsEnabled && FlyingSwordCount(source)<FlyingSwordLimitPerActor)
            {
                view=projectilePool.Count>0?projectilePool.Pop():CreateProjectile();
                activeProjectiles.Add(view);view.Fire(this,source,origin,target,Mathf.Max(.1f,speed),payload);view.MakePersistent(index);
            }
            persistentSwords.Add(new PersistentSword
            {
                source=source,target=target,payload=payload,view=view,
                flight=new PrototypeSwordFlight {position=origin,index=index,speed=Mathf.Max(.1f,speed),hitDelay=payload.visual?.hitDelay??0}
            });
            // Releasing a sword intentionally rallies ALL of this owner's logical swords.
            for (int i=0;i<persistentSwords.Count;i++)
            {
                var sword=persistentSwords[i];
                if(sword.source!=source)continue;
                sword.target=target;sword.flight.Rally(Elapsed);persistentSwords[i]=sword;
            }
        }

        private void AdvancePersistentSwords(float deltaTime)
        {
            int remaining=persistentSwords.Count;
            int index=0;
            while(remaining-->0)
            {
                var sword=persistentSwords[index];
                if(sword.source==null || !sword.source.IsAlive || sword.target==null || !sword.target.IsAlive)
                {
                    persistentSwords.RemoveAt(index);
                    if(sword.view!=null)ReturnProjectile(sword.view);
                    continue;
                }
                bool hit=sword.flight.Advance(Elapsed,deltaTime,sword.source.transform.position,sword.source.Radius,
                    sword.target.transform.position,sword.target.Radius);
                persistentSwords[index++]=sword;
                if(sword.view!=null)sword.view.Present(sword.flight.position,sword.flight.position+sword.flight.heading);
                if(hit)ApplyPayload(sword.source,sword.target,sword.payload);
            }
        }

        private void ClearProjectiles()
        {
            ordinaryProjectiles.Clear();persistentSwords.Clear();scheduledAttacks.Clear();
            while(activeProjectiles.Count>0)ReturnProjectile(activeProjectiles[activeProjectiles.Count-1]);
        }

        public void ReturnProjectile(PrototypeProjectile projectile)
        {
            if(!activeProjectiles.Remove(projectile))return;
            projectile.gameObject.SetActive(false);projectilePool.Push(projectile);
        }

    }
}
