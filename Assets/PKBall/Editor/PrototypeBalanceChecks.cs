using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace PKBall.Prototype.Editor
{
    public static class PrototypeBalanceChecks
    {
        private const BindingFlags Members=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        private static int assertions;
        private static void Require(bool value,string message){if(!value)throw new Exception(message);assertions++;}
        internal static void Set(object owner,string name,object value)=>owner.GetType().GetProperty(name,Members).SetValue(owner,value);
        private sealed class Fixture:IDisposable
        {
            public readonly GameObject root=new GameObject("BalanceFixture");
            public readonly PrototypeBattle battle;
            public readonly PrototypeActor source,target;
            public readonly PrototypeBalance balance=ScriptableObject.CreateInstance<PrototypeBalance>();
            public Fixture(string item)
            {
                battle=root.AddComponent<PrototypeBattle>();Set(battle,"Run",new PrototypeRun(true,true));Set(battle,"Balance",balance);
                battle.ProjectileVisualsEnabled=false;
                typeof(PrototypeBattle).GetField("arenaRoot",Members).SetValue(battle,root.transform);
                source=Actor(1,Vector3.zero);target=Actor(2,Vector3.right*1.1f);
                Set(battle,"Player",source);Set(battle,"Enemy",target);Set(battle,"IsFighting",true);
                if(item!=null)source.AddEquipment(new PrototypeItemInstance{instanceId=1,definition=battle.Run.Find(item),level=1},0);
                Set(source,"Health",1000f);
            }
            private PrototypeActor Actor(int id,Vector3 position)
            {
                var go=new GameObject("Actor",typeof(Rigidbody),typeof(SphereCollider));go.transform.SetParent(root.transform);go.transform.position=position;
                var actor=go.AddComponent<PrototypeActor>();actor.Initialize(battle,balance,id,10000,100,0,1,.5f,null,null);
                typeof(PrototypeActor).GetField("launched",Members).SetValue(actor,true);return actor;
            }
            public void Advance30()
            {
                var flights=typeof(PrototypeBattle).GetMethod("AdvanceOrdinaryProjectiles",Members);
                for(int i=1;i<=1500;i++)
                {
                    Set(battle,"Elapsed",i*.02f);source.CombatTick(battle.Elapsed);target.CombatTick(battle.Elapsed);
                    flights.Invoke(battle,new object[]{.02f});
                }
            }
            public void Dispose(){Set(battle,"IsFighting",false);Object.DestroyImmediate(root);Object.DestroyImmediate(balance);}
        }
        public static void Run()
        {
            assertions=0;
            Require(PrototypeRun.BaseHealth==300&&new PrototypeBroadcastSide().health==300,"new character baseline");
            Require(PrototypeBattleVitals.SegmentUnit(300)==100&&PrototypeBattleVitals.SegmentUnit(1500)==500,"readable segment units");
            using(var f=new Fixture(null))
            {
                Require(f.balance.OvertimeDamage(29)==0&&f.balance.OvertimeDamage(30)==3&&f.balance.OvertimeDamage(60)==45,"overtime damage curve");
                Require(Mathf.Approximately(f.balance.RecoveryMultiplier(45),.55f),"overtime recovery curve");
                Set(f.battle,"Elapsed",45f);f.source.AddShield(100);f.source.Heal(100);
                Require(Mathf.Abs(f.source.State.Shield-55)<.01f&&Mathf.Abs(f.source.Health-1055)<.01f,"real shield and healing respect overtime");
                f.source.GrantInvulnerability(20);f.source.ReceiveDamage(10,PrototypeDamageKind.SuddenDeath);
                Require(Mathf.Abs(f.source.Health-1045)<.01f&&Mathf.Abs(f.source.State.Shield-55)<.01f,"overtime bypasses invulnerability and shield");
                f.source.ReceiveDamage(10,PrototypeDamageKind.Melee);Require(Mathf.Abs(f.source.Health-1045)<.01f,"invulnerability still blocks ordinary damage");
                f.source.ApplyStat(PrototypeStat.Health,100);
                Require(Mathf.Abs(f.source.MaximumHealth-10100)<.01f&&Mathf.Abs(f.source.Health-1100)<.01f,"growth keeps full maximum but scales granted health in overtime");
            }
            using(var f=new Fixture("apprentice_spring"))
            {
                f.Advance30();Require(Mathf.Abs(f.source.State.Regeneration-2.17f)<.02f,"real regeneration at 30 seconds");
                Require(f.source.Health-1000>27&&f.source.Health-1000<31,"real regeneration budget matches conservative model");
            }
            using(var f=new Fixture("apprentice_shield")){f.Advance30();Require(Mathf.Abs(f.source.State.Shield-273)<.1f,"real shield release budget");}
            using(var f=new Fixture("apprentice_venom")){f.Advance30();Require(10000-f.target.Health>45&&10000-f.target.Health<54,"real poison budget");}
            using(var f=new Fixture("apprentice_flame")){f.Advance30();Require(10000-f.target.Health>43&&10000-f.target.Health<51,"real burn budget");}
            var assets=Resources.LoadAll<PrototypeItemDefinition>("Items");
            foreach(var asset in assets)
            {
                var live=PrototypeItemRepository.Find(asset.itemId);if(live==null)continue;
                foreach(var effect in asset.effects??Array.Empty<PrototypeEffectDefinition>())
                {
                    var current=Array.Find(live.effects??Array.Empty<PrototypeEffectDefinition>(),e=>e.id==effect.id);
                    if(current==null)continue;
                    for(int level=1;level<=4;level++)Require(Mathf.Approximately(effect.BaseAt(level),current.BaseAt(level))&&Mathf.Approximately(effect.GrowthAt(level),current.GrowthAt(level)),"fallback effect mismatch: "+asset.itemId);
                }
            }
            Directory.CreateDirectory("Artifacts/Balance");File.WriteAllText("Artifacts/Balance/engine-checks.txt",$"PASS: {assertions} assertions; actual Actor/CombatState, overtime, segment units, fallback effect consistency.");
            Debug.Log("PKBall BALANCE CHECKS PASSED: "+assertions);
        }
    }
}
