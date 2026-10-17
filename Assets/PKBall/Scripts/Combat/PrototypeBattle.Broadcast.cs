using UnityEngine;
using UnityEngine.SceneManagement;
namespace PKBall.Prototype
{
    public sealed partial class PrototypeBattle
    {
        public bool IsBroadcast=>PrototypeBroadcastSession.Config!=null;
        public string LeftDisplayName=>IsBroadcast?PrototypeBroadcastSession.Config.left.name:"我方";
        public string RightDisplayName=>IsBroadcast?PrototypeBroadcastSession.Config.right.name:Run.CurrentEncounter.name;
        public string LeftVisual=>IsBroadcast?PrototypeBroadcastSession.Config.left.visualId:PlayerVisualId;
        public string RightVisual=>IsBroadcast?PrototypeBroadcastSession.Config.right.visualId:Run.CurrentEncounter.visualId;
        public void BeginBroadcast(PrototypeBroadcastConfig config)
        {
            config.Validate();PrototypeBroadcastSession.Draft=config.Copy();PrototypeBroadcastSession.Campaign=Run;PrototypeBroadcastSession.Config=config.Copy();
            PrototypeSceneSession.Run=new PrototypeRun(true,true);SceneManager.LoadScene("PrototypeBattle");
        }
        private void LeaveBroadcast()
        {
            Time.timeScale=1;IsFighting=false;PrototypeSceneSession.Run=PrototypeBroadcastSession.Campaign;
            PrototypeBroadcastSession.Config=null;PrototypeBroadcastSession.ClearAssets();PrototypeBroadcastSession.OpenEditor=true;SceneManager.LoadScene("MainMenu");
        }
        private void BuildBroadcastActors()
        {
            PrototypeBroadcastSession.ClearAssets();var c=PrototypeBroadcastSession.Config;
            Player=BroadcastActor(c.left,1,new Vector3(-2.7f,0,0));Enemy=BroadcastActor(c.right,2,new Vector3(2.7f,0,0));
            if(Presentation==null){var go=new GameObject("CombatPresentation");go.transform.SetParent(arenaRoot,false);Presentation=go.AddComponent<PrototypeCombatPresentation>();Presentation.Initialize(this);}
            Presentation.BindActors(Player,Enemy);
        }
        private PrototypeActor BroadcastActor(PrototypeBroadcastSide side,int id,Vector3 position)
        {
            var items=new PrototypeItemInstance[side.slots.Length];var stats=new PrototypeStatSnapshot {health=side.health,speed=side.speed,damageBonus=side.damageBonus,damageReduction=side.damageReduction,startingShield=side.shield};
            for(int i=0;i<items.Length;i++)
            {
                var item=PrototypeBroadcastSession.CreateItem(side.slots[i],id*100+i+1);items[i]=item;if(item==null)continue;
                PrototypeStatCalculator.AddEquipment(stats,item);
            }
            var actor=CreateActor(side.name,id,position,stats.health,stats.speed,stats.impactBonus,stats.ImpactTakenMultiplier,.69f,Color.white,null,null);
            actor.ConfigureStats(stats);for(int i=0;i<items.Length;i++)if(items[i]!=null)actor.AddEquipment(items[i],i,0,1,1);
            return actor;
        }
    }
}
