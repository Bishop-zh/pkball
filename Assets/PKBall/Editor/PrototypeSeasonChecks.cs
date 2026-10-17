using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace PKBall.Prototype.Editor
{
    public static class PrototypeSeasonChecks
    {
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static int assertions;
        private static void Require(bool value,string reason){if(!value)throw new Exception(reason);assertions++;}
        [MenuItem("PKBall/Checks/Season one content")]
        public static void Run()
        {
            assertions=0;PrototypeItemRepository.Reset();PrototypeCharacterCatalog.Reset();
            var ids=new HashSet<string>();
            foreach(var p in PrototypeCharacterCatalog.Characters)
            {
                Require(ids.Add(p.id),"duplicate character: "+p.id);
                var roundtrip=new PrototypeBroadcastConfig {left=p.character,right=p.character}.Copy();roundtrip.Validate();
                Require(roundtrip.left.slots.Length==8,"eight editor slots");
                foreach(var s in roundtrip.left.slots)
                {
                    if(string.IsNullOrEmpty(s.itemId))continue;
                    var definition=PrototypeItemRepository.Find(s.itemId);
                    Require(PrototypeUiSkin.SpriteFor("Icons/"+definition.iconId)!=null,"missing icon "+s.itemId);
                    var item=PrototypeBroadcastSession.CreateItem(s,100);
                    Require(item!=null&&item.definition.itemId==s.itemId,"broadcast item creation "+s.itemId);
                }
                foreach(var owned in PrototypeBroadcastSession.Owned)if(owned!=null)Object.DestroyImmediate(owned);
                PrototypeBroadcastSession.Owned.Clear();
                var isolated=PrototypeCharacterCatalog.Find(p.id);isolated.character.slots[0].itemId="";
                Require(PrototypeCharacterCatalog.Find(p.id).character.slots[0].itemId!="","immutable built-in "+p.id);
                var monster=new PrototypeMonsterPreset {id=Guid.NewGuid().ToString("N"),character=p.character,tier=PrototypeEncounterTier.Normal};
                monster.Validate();var scaled=monster.ScaleToDay(2);
                Require(scaled.health>p.character.health,"monster pool growth "+p.id);
            }
            foreach(var m in PrototypeCharacterCatalog.Matches)PrototypeCharacterCatalog.Match(m.id).Copy().Validate();
            var run=new PrototypeRun(true,true);
            for(int t=0;t<3;t++)for(int v=0;v<PrototypeEncounterCatalog.VariantCount(t);v++)
            {
                var e=PrototypeEncounterCatalog.Get(run,t,false,v);
                Require(PrototypeEncounterCatalog.BuiltinVariant(t,e.monsterId)==v,"stable encounter ID");
                foreach(var id in e.itemIds)Require(run.Find(id)!=null,"encounter reference "+id);
                if(t==0)foreach(var id in e.itemIds)Require(!run.Find(id).bossItem,"ordinary encounter cannot start with boss core");
            }
            foreach(var id in new[]{"antler_guard","spore_lantern","storm_feather","red_duel_banner","saw_flywheel","frost_mirror","crown_command","furnace_bellows","storm_capacitor","thorn_seed","salvage_clock","slime_mucus","slime_core"})
            {
                var live=run.Find(id);var fallback=Resources.Load<PrototypeItemDefinition>("Items/"+id);
                Require(live!=null&&fallback!=null&&live.effects.Length==fallback.effects.Length,"fallback exists "+id);
                for(int i=0;i<live.effects.Length;i++)
                {
                    Require(live.effects[i].id==fallback.effects[i].id,"stable effect ID "+id);
                    Require(live.effects[i].trigger==fallback.effects[i].trigger,"fallback trigger "+id);
                    for(int level=1;level<=4;level++)Require(Mathf.Approximately(live.effects[i].BaseAt(level),fallback.effects[i].BaseAt(level)),"fallback level table "+id);
                }
                for(int level=1;level<=4;level++)CheckActivation(id,level);
            }
            PrototypeFoundationChecks.Run();PrototypeBalanceChecks.Run();
            Directory.CreateDirectory("Artifacts/SeasonOne");File.WriteAllText("Artifacts/SeasonOne/content-checks.txt","PASS: "+assertions+" season assertions; roster roundtrip, visuals, independent copies, monster pool scaling, encounter IDs, fallback tables, real Actor releases at levels 1-4. Foundation and balance checks also passed.");
            Debug.Log("PKBall SEASON CHECKS PASSED: "+assertions);
        }
        private static void CheckActivation(string id,int level)
        {
            var root=new GameObject("SeasonFixture");var balance=ScriptableObject.CreateInstance<PrototypeBalance>();
            var battle=root.AddComponent<PrototypeBattle>();
            try
            {
                PrototypeBalanceChecks.Set(battle,"Run",new PrototypeRun(true,true));PrototypeBalanceChecks.Set(battle,"Balance",balance);
                battle.ProjectileVisualsEnabled=false;typeof(PrototypeBattle).GetField("arenaRoot",Members).SetValue(battle,root.transform);
                var actors=new PrototypeActor[2];
                for(int i=0;i<2;i++)
                {
                    var go=new GameObject("Actor",typeof(Rigidbody),typeof(SphereCollider));go.transform.SetParent(root.transform);go.transform.position=Vector3.right*i;
                    actors[i]=go.AddComponent<PrototypeActor>();actors[i].Initialize(battle,balance,i+1,10000,100,0,1,.5f,null,null);
                    typeof(PrototypeActor).GetField("launched",Members).SetValue(actors[i],true);
                }
                PrototypeBalanceChecks.Set(battle,"Player",actors[0]);PrototypeBalanceChecks.Set(battle,"Enemy",actors[1]);PrototypeBalanceChecks.Set(battle,"IsFighting",true);
                PrototypeBalanceChecks.Set(actors[0],"Health",5000f);
                var d=PrototypeItemRepository.Find(id);actors[0].AddEquipment(new PrototypeItemInstance {instanceId=1,definition=d,level=d.ResolveLevel(level)},0);
                var flights=typeof(PrototypeBattle).GetMethod("AdvanceOrdinaryProjectiles",Members);
                for(int step=1;step<=500;step++)
                {
                    PrototypeBalanceChecks.Set(battle,"Elapsed",step*.02f);actors[0].CombatTick(battle.Elapsed);actors[1].CombatTick(battle.Elapsed);flights.Invoke(battle,new object[]{.02f});
                }
                Require(actors[0].State.Shield>0||actors[0].Health>5000||actors[1].Health<10000,"no actual release "+id);
                if(id=="frost_mirror"||id=="thorn_seed")
                {
                    float before=actors[1].Health,poison=actors[1].State.Poison;
                    actors[0].ReceiveDamage(actors[0].State.Shield+1,PrototypeDamageKind.Melee);
                    for(int n=0;n<100;n++)flights.Invoke(battle,new object[]{.02f});
                    Require(id=="frost_mirror"?actors[1].Health<before:actors[1].State.Poison>poison,"shield break effect "+id);
                }
            }
            finally{PrototypeBalanceChecks.Set(battle,"IsFighting",false);Object.DestroyImmediate(root);Object.DestroyImmediate(balance);}
        }
    }
}
