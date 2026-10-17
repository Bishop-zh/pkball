using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PKBall.Prototype.Editor
{
    // Permanent regression entry point. All runs are transient; no player save is written.
    public static class PrototypeFoundationChecks
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly List<string> passed = new List<string>();
        private static void Require(bool value, string name)
        {
            if (!value) throw new Exception(name);
            passed.Add(name);
        }
        private static object Field(object owner, string name) => owner.GetType().GetField(name, Members).GetValue(owner);
        private static void Set(object owner, string name, object value) => owner.GetType().GetProperty(name, Members).SetValue(owner, value);
        private static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Members).Invoke(owner, args);

        [MenuItem("PKBall/Checks/Foundation regression")]
        public static void Run()
        {
            passed.Clear();
            Directory.CreateDirectory("Artifacts/ArchitectureAudit");
            try
            {
                CheckContentIdentity();
                CheckSaves();
                CheckBuffs();
                CheckProjectiles();
                CheckAnimations();
                CheckPreparationBoundaries();
                CheckBattleReceipts();
                CheckSwordBudgets();
                CheckSimulationOrder();
                CheckStatSources();
                File.WriteAllLines("Artifacts/ArchitectureAudit/foundation-checks.txt", passed);
                Debug.Log("PKBall foundation checks PASSED: " + passed.Count);
            }
            catch (Exception error)
            {
                File.WriteAllText("Artifacts/ArchitectureAudit/foundation-checks.txt", "FAILED\n" + error);
                throw;
            }
        }

        private static void CheckContentIdentity()
        {
            var data=JsonUtility.FromJson<PrototypeItemDataFile>(Resources.Load<TextAsset>("Data/items").text);
            var items=new HashSet<string>();
            bool valid=true;
            foreach(var entry in data.items)
            {
                valid &= !string.IsNullOrEmpty(entry.item.itemId) && items.Add(entry.item.itemId);
                var ids=new HashSet<string>();
                if(entry.item.effects!=null)foreach(var effect in entry.item.effects)
                    valid &= effect!=null && !string.IsNullOrEmpty(effect.id) && ids.Add(effect.id);
            }
            Require(valid && items.Count>0,"JSON catalogue has unique item and explicit effect identities");
            valid=true;
            foreach(var definition in Resources.LoadAll<PrototypeItemDefinition>("Items"))
            {
                var ids=new HashSet<string>();
                if(definition.effects!=null)foreach(var effect in definition.effects)
                    valid &= effect!=null && !string.IsNullOrEmpty(effect.id) && ids.Add(effect.id);
            }
            Require(valid,"Fallback assets have explicit unique effect identities");
            var effects=PrototypeItemRepository.Find("master_flame").effects;
            Require(effects[0].id==PrototypeModifierLedger.EffectKey(0,effects[0].kind) &&
                effects[1].id==PrototypeModifierLedger.EffectKey(1,effects[1].kind),"Frozen legacy identities preserve old ledger keys");
            var records=new List<PrototypeItemModifier>();
            PrototypeModifierLedger.Add(records,effects[0].id,effects[0].kind,7);
            Require(PrototypeModifierLedger.Amount(records,PrototypeModifierLedger.EffectKey(1,effects[0]))==7,"Effect reorder preserves historical growth lookup");
        }

        private static void CheckSaves()
        {
            var run = new PrototypeRun(true, true);
            PrototypeCheckFixtures.Bag(run).Add(new PrototypeItemInstance { instanceId=40, definition=run.Find("spear"), level=1 });
            string valid = run.ExportSnapshot();
            Require(run.ImportSnapshot(valid), "Current save round trip");
            Require(run.Bag.Count==1 && run.Bag[0].instanceId==40, "Round trip preserves identity");
            Require((long)Field(run,"nextId")>40, "Next identity repaired above existing items");
            PrototypeCheckFixtures.PendingRewards(run).Add(run.Bag[0]);
            string duplicate = run.ExportSnapshot();
            PrototypeCheckFixtures.PendingRewards(run).Clear();
            string before = run.ExportSnapshot();
            Require(!run.ImportSnapshot(duplicate), "Duplicate identity across containers rejected");
            Require(before==run.ExportSnapshot(), "Rejected duplicate preserves current state");
            Require(!run.ImportSnapshot(valid.Replace("\"definitionId\": \"spear\"", "\"definitionId\": \"missing_item\"")), "Unknown definition rejected");
            Require(before==run.ExportSnapshot(), "Unknown definition preserves current state");
            Require(!run.ImportSnapshot(valid.Replace("\"id\": 40", "\"id\": 0")), "Zero identity rejected");
            Require(before==run.ExportSnapshot(), "Invalid identity preserves current state");
            Require(!run.ImportSnapshot(valid.Replace("\"id\": 40", "\"id\": 9223372036854775807")), "Identity overflow rejected");
            var monsterSlots=(PrototypeMonsterPreset[])Field(run,"customMonsterOffers");
            monsterSlots[0]=new PrototypeMonsterPreset {id="invalid",character=new PrototypeBroadcastSide()};
            string malformedMonster=run.ExportSnapshot();monsterSlots[0]=null;
            string monsterBefore=run.ExportSnapshot();
            Require(!run.ImportSnapshot(malformedMonster) && monsterBefore==run.ExportSnapshot(),"Invalid custom monster rejected before state mutation");
            string recoveryDirectory = Path.Combine("Artifacts/ArchitectureAudit", "save-recovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(recoveryDirectory);
            string recoveryFile = Path.GetFullPath(Path.Combine(recoveryDirectory,"run.json"));
            File.WriteAllText(recoveryFile,duplicate);
            var recovered = new PrototypeRun(false,false,recoveryFile);
            recovered.FlushSave();
            string[] backups = Directory.GetFiles(recoveryDirectory,"*.rejected-*.json");
            Require(backups.Length==1 && File.ReadAllText(backups[0])==duplicate,"Rejected disk save preserved before replacement checkpoint");
        }

        private static void CheckBuffs()
        {
            var buffs = new PrototypeBuffManager();
            buffs.AddOrRefresh("a",PrototypeStat.Speed,4,0,100);
            buffs.AddOrRefresh("a",PrototypeStat.Speed,4,50,100);
            buffs.AddOrRefresh("b",PrototypeStat.Speed,2,0,100);
            Require(buffs.Sum(PrototypeStat.Speed)==6 && buffs.Count==2,"Refresh and independent sources");
            buffs.Advance(100);
            Require(buffs.Sum(PrototypeStat.Speed)==4,"Independent source expires at exact step");
            buffs.Advance(150);
            Require(buffs.Sum(PrototypeStat.Speed)==0,"Refreshed source expires at exact step");
            buffs.AddStack("health",PrototypeStat.Health,10);
            buffs.AddStack("health",PrototypeStat.Health,15);
            buffs.AddStatus(PrototypeEffectKind.Poison,5);
            Require(buffs.Sum(PrototypeStat.Health)==25 && buffs.Count==1,"Growth aggregates without per-stack entries");
            buffs.Clear();
            Require(buffs.Count==0 && buffs.StatusAmount(PrototypeEffectKind.Poison)==0,"Combat clear removes modifiers and statuses");
        }

        private sealed class Arena : IDisposable
        {
            public readonly GameObject root = new GameObject("FoundationCheck");
            public readonly PrototypeBalance balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            public readonly PrototypeBattle battle;
            public readonly PrototypeActor source, target;
            public Arena()
            {
                battle=root.AddComponent<PrototypeBattle>();
                Set(battle,"Run",new PrototypeRun(true,true));
                Set(battle,"Balance",balance);
                battle.GetType().GetField("arenaRoot",Members).SetValue(battle,root.transform);
                source=Actor(1,Vector3.zero); target=Actor(2,Vector3.right*5);
                Set(battle,"Player",source);Set(battle,"Enemy",target);Set(battle,"IsFighting",true);
            }
            private PrototypeActor Actor(int id, Vector3 position)
            {
                var go=new GameObject("Actor",typeof(Rigidbody),typeof(SphereCollider));
                go.transform.SetParent(root.transform);go.transform.position=position;
                var actor=go.AddComponent<PrototypeActor>();
                actor.Initialize(battle,balance,id,1000,100,0,1,.5f,null,null);
                return actor;
            }
            public void Step(int step)
            {
                Set(battle,"Elapsed",step*.02f);
                target.transform.position=new Vector3(5+step*.01f,0,Mathf.Sin(step*.08f));
                Call(battle,"AdvanceOrdinaryProjectiles",.02f);
            }
            public void Dispose()
            {
                Set(battle,"IsFighting",false);
                foreach (var asset in (IEnumerable)Field(battle,"runtimeAssets")) if(asset is Object value) Object.DestroyImmediate(value);
                Object.DestroyImmediate(root);Object.DestroyImmediate(balance);
            }
        }
        private static void CheckProjectiles()
        {
            using(var one=new Arena()) using(var many=new Arena())
            {
                var payload=new PrototypeAttackPayload {damage=1,kind=PrototypeDamageKind.Ranged};
                one.battle.FirePayload(one.source,one.target,payload,5);
                for(int i=0;i<97;i++)many.battle.FirePayload(many.source,many.target,payload,5);
                Require(((IList)Field(many.battle,"activeProjectiles")).Count==96,"Visual budget stays at 96");
                Require(((IList)Field(many.battle,"ordinaryProjectiles")).Count==97,"Overflow retains every logical projectile");
                bool equal=true;
                for(int step=1;step<=150;step++)
                {
                    one.Step(step);many.Step(step);
                    equal &= Mathf.Abs((1000-one.target.Health)*97-(1000-many.target.Health))<.01f;
                }
                Require(equal,"Moving target: visible and overflow shots hit on identical steps");
                Require(many.target.Health==903,"All 97 hits settle exactly once");
                Require(((IList)Field(many.battle,"ordinaryProjectiles")).Count==0,"Completed logical shots removed");
                for(int i=0;i<97;i++)many.battle.FirePayload(many.source,many.target,payload,5);
                many.source.ReceiveDamage(1000,PrototypeDamageKind.Poison);
                many.Step(151);
                Require(many.target.Health==903 && ((IList)Field(many.battle,"ordinaryProjectiles")).Count==0,"Owner death cancels visible and overflow shots equally");
            }
            using(var arena=new Arena())
            {
                arena.battle.FirePayload(arena.source,arena.target,new PrototypeAttackPayload {damage=1},5);
                arena.target.ReceiveDamage(1000,PrototypeDamageKind.Poison);arena.Step(1);
                Require(((IList)Field(arena.battle,"ordinaryProjectiles")).Count==0,"Target death cancels logical shots");
                arena.source.Buffs.AddOrRefresh("source",PrototypeStat.Speed,3,0,1);
                arena.target.Buffs.AddOrRefresh("target",PrototypeStat.Speed,3,0,1);
                Set(arena.target,"Health",1000f);Set(arena.battle,"Elapsed",0f);
                Call(arena.battle,"FixedUpdate");
                Require(arena.source.Buffs.Count==0 && arena.target.Buffs.Count==0,"Battle expires both actors buffs before combat ticks");
            }
            var flight=new PrototypeProjectileFlight {position=Vector3.zero,speed=10,startsAt=1};
            Require(!flight.Advance(.5f,.02f,Vector3.right*5,.5f) && flight.position==Vector3.zero,"Hit delay prevents early movement");
        }

        private static void CheckPreparationBoundaries()
        {
            var run=new PrototypeRun(true,true);
            run.Buy(0);run.Equip(0,0);run.Buy(1);run.AddOre(2);
            var operations=new Dictionary<string,Func<bool>>
            {
                {"buy",()=>run.Buy(2)}, {"buy to slot",()=>run.BuyToSlot(2,1)},
                {"equip",()=>run.Equip(0,1)}, {"unequip",()=>run.Unequip(0)},
                {"sell",()=>run.Sell(0)}, {"sell equipped",()=>run.SellEquipped(0)},
                {"merge",()=>run.MergeBag(0,1)}, {"move bag",()=>run.MoveBag(0,1)},
                {"move equipped",()=>run.MoveEquipped(0,1)}, {"ore",()=>run.SellOre()},
                {"claim",()=>run.ClaimGenerated()}, {"reroll",()=>run.Reroll()},
                {"upgrade",()=>run.Expand()}, {"injection",()=>run.UseInjection(0)},
                {"event",()=>run.ChooseEvent(0)}, {"advance event",()=>run.AdvanceEvent()},
                {"monster",()=>run.SelectMonster(0)}
            };
            foreach(var phase in new[]{PrototypeRunPhase.Aiming,PrototypeRunPhase.Fighting,PrototypeRunPhase.Resolving})
            {
                Set(run,"Phase",phase);
                string before=run.ExportSnapshot();int version=run.Version;
                foreach(var operation in operations)Require(!operation.Value(),phase+" rejects "+operation.Key);
                run.Restock();run.AddGold(99,"test");run.AddOre(99);run.EndDay(true);run.HealEndurance(3);
                Require(before==run.ExportSnapshot() && version==run.Version,phase+" rejection leaves run unchanged");
            }
            Set(run,"Phase",PrototypeRunPhase.Preparation);
            Require(run.Unequip(0),"Preparation still allows valid inventory operations");
            Require(((IList<PrototypeItemInstance>)run.Bag).IsReadOnly && ((IList<PrototypeItemInstance>)run.Equipped).IsReadOnly,"Inventory container views are read only");
            string invalidBefore=run.ExportSnapshot();
            Require(!run.MergeBag(0,0) && invalidBefore==run.ExportSnapshot(),"Failed merge consumes nothing");
            Set(run,"Stage",PrototypeDayStage.Monster);run.SelectMonster(0);
            Require(run.BeginAiming() && !run.CanEditPreparation,"Aiming locks preparation");
            run.CancelAiming();Require(run.CanEditPreparation,"Cancelling aim unlocks preparation");
        }

        private static void CheckBattleReceipts()
        {
            var run=new PrototypeRun(true,true);Set(run,"Stage",PrototypeDayStage.Monster);run.SelectMonster(0);
            Require(run.StartCombat("battle-a"),"First battle start accepted");
            run.RecordCombatGold(7);run.RecordOreCredit(2);
            string inProgress=run.ExportSnapshot();
            Require(!run.StartCombat("battle-a") && !run.StartCombat("battle-b") && inProgress==run.ExportSnapshot(),"Repeated start preserves pending rewards");
            int before=run.Gold;
            Require(run.ResolveCombat("battle-a",false) && run.Gold==before+7 && run.OreCount==2,"Battle rewards settle once");
            string settled=run.ExportSnapshot();
            Require(!run.ResolveCombat("battle-a",true) && !run.StartCombat("battle-a") && settled==run.ExportSnapshot(),"Old battle ID cannot restart or settle again");
            var restored=new PrototypeRun(true,true);Require(restored.ImportSnapshot(settled),"V8 receipt round trip");
            Require(!restored.StartCombat("battle-a"),"Receipt survives save reload");
            var interrupted=new PrototypeRun(true,true);Require(interrupted.ImportSnapshot(inProgress),"Interrupted battle restores pending rewards");
            Require(interrupted.Gold==before+7 && interrupted.OreCount==2 && interrupted.Wins==0,"Interrupted recovery grants no victory");
            string recovered=interrupted.ExportSnapshot();interrupted.CommitCombat();
            Require(recovered==interrupted.ExportSnapshot() && !interrupted.StartCombat("battle-a"),"Recovered battle cannot credit again");
            var legacy=new PrototypeRun(true,true);
            string v7=inProgress.Replace("\"version\": 8","\"version\": 7").Replace("\"activeBattleId\": \"battle-a\"","\"activeBattleId\": \"\"");
            Require(legacy.ImportSnapshot(v7) && legacy.Gold==before+7 && legacy.OreCount==2,"V7 interrupted battle migrates to a receipt");
            // An old completion must not finish a newer fight, even when the day stage repeats.
            Set(restored,"Stage",PrototypeDayStage.Monster);restored.SelectMonster(0);restored.StartCombat("battle-b");
            string next=restored.ExportSnapshot();Require(!restored.ResolveCombat("battle-a",true) && next==restored.ExportSnapshot(),"Stale result cannot resolve a newer battle");
        }

        private static void CheckSwordBudgets()
        {
            foreach(int count in new[]{15,16,17,32})
            using(var visible=new Arena())using(var hidden=new Arena())
            {
                hidden.battle.ProjectileVisualsEnabled=false;
                Set(visible.target,"Health",100000f);Set(hidden.target,"Health",100000f);
                visible.target.State.AddShield(3);hidden.target.State.AddShield(3);
                for(int i=0;i<count;i++)
                {
                    var payload=new PrototypeAttackPayload {damage=i+1,kind=PrototypeDamageKind.Ranged};
                    visible.battle.ReleaseFlyingSword(visible.source,visible.target,payload,4+i*.1f);
                    hidden.battle.ReleaseFlyingSword(hidden.source,hidden.target,payload,4+i*.1f);
                }
                Require(visible.battle.LogicalFlyingSwordCount(visible.source)==count && visible.battle.FlyingSwordCount(visible.source)==Math.Min(16,count),count+" swords keep independent logic and bounded visuals");
                bool equal=true;
                for(int step=1;step<=600;step++)
                {
                    foreach(var arena in new[]{visible,hidden})
                    {
                        Set(arena.battle,"Elapsed",step*.02f);Set(arena.battle,"StepIndex",step);
                        arena.target.transform.position=new Vector3(5+Mathf.Sin(step*.03f),0,Mathf.Sin(step*.08f));
                        if(step==250)arena.battle.ReleaseFlyingSword(arena.source,arena.target,new PrototypeAttackPayload {damage=7,kind=PrototypeDamageKind.Ranged},9);
                        Call(arena.battle,"AdvancePersistentSwords",.02f);
                    }
                    equal &= visible.target.Health==hidden.target.Health && visible.target.State.Shield==hidden.target.State.Shield && visible.battle.DamageEventCount==hidden.battle.DamageEventCount;
                }
                Require(equal && visible.battle.DamageEventCount>count*2,count+" swords: visible and hidden repeated hits match at every step");
                var a=visible.battle.RecentDamageRecords();var b=hidden.battle.RecentDamageRecords();
                bool same=a.Length==b.Length;
                for(int i=0;i<a.Length && same;i++)same=a[i].eventId==b[i].eventId && a[i].step==b[i].step && a[i].amount==b[i].amount;
                Require(same,count+" swords have identical per-hit damage trace across budgets");
                visible.source.ReceiveDamage(1000,PrototypeDamageKind.Poison);Call(visible.battle,"AdvancePersistentSwords",.02f);
                Require(visible.battle.LogicalFlyingSwordCount(visible.source)==0 && visible.battle.FlyingSwordCount(visible.source)==0,count+" swords clear when owner dies");
                Call(hidden.battle,"ClearProjectiles");Require(hidden.battle.LogicalFlyingSwordCount(hidden.source)==0,"Logical summons clear without renderers");
            }
            var flight=new PrototypeSwordFlight {position=Vector3.zero,speed=100,hitDelay=.2f};flight.Rally(0);
            Require(!flight.Advance(.1f,.02f,Vector3.zero,.5f,Vector3.right,.5f),"Sword windup respects its own snapshot");
            Require(flight.Advance(.2f,.02f,Vector3.zero,.5f,Vector3.right,.5f),"Sword hits after windup");
            flight.Rally(.3f);
            Require(!flight.Advance(.4f,.02f,Vector3.zero,.5f,Vector3.right,.5f) && flight.Advance(.5f,.02f,Vector3.zero,.5f,Vector3.right,.5f),"A new summon rally interrupts the old sword cooldown with a new windup");
        }

        private static void CheckSimulationOrder()
        {
            var a=new PrototypeCombatRandom(123);var b=new PrototypeCombatRandom(123);bool equal=true;
            for(int i=0;i<1000;i++){float x=a.NextValue();UnityEngine.Random.value.ToString();equal &= x==b.NextValue() && x>=0 && x<1;}
            Require(equal && a.Sequence==1000,"Combat random is repeatable and independent of Unity global random");
            using(var arena=new Arena())
            {
                arena.battle.ResolveActorCollision(arena.target,arena.source,6);
                arena.battle.ResolveActorCollision(arena.source,arena.target,8);
                Require(arena.source.Health==1000 && arena.target.Health==1000,"Collision collection does not mutate health");
                Call(arena.battle,"FixedUpdate");
                Require(arena.battle.StepIndex==1 && arena.battle.ImpactCount==1 && arena.source.Health<1000 && arena.target.Health<1000,"One battle step drains a merged actor pair once");
                Require(arena.battle.RecentDamageRecords().Length==2,"Both contact damage events recorded before victory evaluation");
                Require(typeof(PrototypeActor).GetMethod("FixedUpdate",Members)==null && typeof(PrototypeProjectile).GetMethod("FixedUpdate",Members)==null,"Actors and projectile views have no independent combat FixedUpdate");
            }
        }

        private static void CheckStatSources()
        {
            using(var arena=new Arena())
            {
                arena.source.ApplyStat(PrototypeStat.Speed,5,"a");arena.source.ApplyStat(PrototypeStat.Speed,7,"b");
                var snapshot=arena.source.Buffs.GetSnapshot();
                Require(arena.source.SpeedStat==112 && snapshot.Length==2,"Recipe speed is tracked by source");
                arena.source.Buffs.RemoveSource("a");
                Require(arena.source.SpeedStat==107 && snapshot.Length==2,"Removing one source preserves another and detached snapshots");
                arena.source.ConfigureStats(new PrototypeStatSnapshot {damageReduction=.2f});
                arena.source.ApplyStat(PrototypeStat.DamageReduction,.1f,"a");
                arena.source.ApplyStat(PrototypeStat.DamageReduction,.1f,"a");
                arena.source.ApplyStat(PrototypeStat.DamageReduction,.2f,"b");
                float before=arena.source.Health;arena.source.ReceiveDamage(100,PrototypeDamageKind.Melee);
                Require(Mathf.Abs(before-arena.source.Health-51.84f)<.001f,"Source-aware reduction preserves multiplicative stacking");
                arena.source.Buffs.RemoveSource("a");before=arena.source.Health;arena.source.ReceiveDamage(100,PrototypeDamageKind.Melee);
                Require(Mathf.Abs(before-arena.source.Health-64)<.001f,"Removing reduction source restores the remaining multiplier");
                arena.source.Buffs.Clear();Require(arena.source.SpeedStat==100,"Clearing combat buffs restores base speed");
            }
            var run=new PrototypeRun(true,true);run.Buy(0);run.Equip(0,1);
            run.Equipped[1].permanentDamage=3;
            var shared=new PrototypeStatSnapshot {health=run.BodyHealth};PrototypeStatCalculator.AddEquipment(shared,run.Equipped[1]);
            var main=run.CalculateStats();
            Require(main.health==shared.health && main.speed==shared.speed && main.impactBonus==shared.impactBonus,"Campaign and broadcast equipment contributions share formulas");
            bool roundTrips=true;
            for(int i=0;i<100;i++)roundTrips &= run.Unequip(1) && run.Equip(0,1);
            Require(roundTrips,"100 equipment round trips succeed");
            var after=run.CalculateStats();Require(after.health==main.health && after.speed==main.speed && after.impactBonus==main.impactBonus,"100 equipment round trips preserve stats");
        }

        private static void CheckAnimations()
        {
            using(var arena=new Arena())
            {
                var presentation=arena.root.AddComponent<PrototypeCombatPresentation>();presentation.Initialize(arena.battle);
                var definition=Object.Instantiate(arena.battle.Run.Find("spear"));
                try
                {
                    definition.animation=new PrototypeItemAnimation {preset=PrototypeItemAnimationPreset.Crescent};
                    presentation.ShowActivation(arena.source,arena.target,definition,1);
                    var flashes=(IList)Field(presentation,"flashes");
                    var first=flashes[0];
                    Require(((LineRenderer)Field(first,"halo")).enabled && ((LineRenderer)Field(first,"line")).positionCount==48,"Crescent works on a non-scythe item");
                    definition.animation.preset=PrototypeItemAnimationPreset.Growth;
                    presentation.ShowActivation(arena.source,arena.target,definition,1);
                    Require(((LineRenderer)Field(flashes[1],"line")).positionCount==48,"Growth works on a non-slime item");
                    definition.animation.preset=PrototypeItemAnimationPreset.None;
                    presentation.ShowActivation(arena.source,arena.target,definition,1);
                    Require(!((GameObject)Field(flashes[2],"root")).activeSelf,"None allocates no activation visual");
                    Require(arena.target.Health==1000,"Animation selection does not apply damage");
                }
                finally {Object.DestroyImmediate(definition);}
            }
        }
    }
}
