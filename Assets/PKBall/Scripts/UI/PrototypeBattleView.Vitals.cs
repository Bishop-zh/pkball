using UnityEngine;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    public sealed partial class PrototypeBattleView
    {
        private PrototypeBattleVitals leftVitals,rightVitals;
        private Image overtimeWash;
        private Text overtimeText;
        private void BuildReadableVitals()
        {
            if(leftVitals==null)leftVitals=PrototypeBattleVitals.Create(leftPanel.transform,playerName.font);
            if(rightVitals==null)rightVitals=PrototypeBattleVitals.Create(rightPanel.transform,enemyName.font);
            playerHealth.transform.parent.gameObject.SetActive(false);enemyHealth.transform.parent.gameObject.SetActive(false);
            playerVitals.gameObject.SetActive(false);enemyVitals.gameObject.SetActive(false);
            playerStatus.gameObject.SetActive(false);enemyStatus.gameObject.SetActive(false);
            foreach(var portrait in new[]{playerPortrait,enemyPortrait})portrait.rectTransform.anchoredPosition=new Vector2(-133,351);
            foreach(var name in new[]{playerName,enemyName}){name.rectTransform.anchoredPosition=new Vector2(54,350);name.rectTransform.sizeDelta=new Vector2(247,64);}
            foreach(var energy in new[]{playerEnergy,enemyEnergy})((RectTransform)energy.transform.parent).anchoredPosition=new Vector2(0,-432);
            if(overtimeWash!=null)return;
            overtimeWash=PrototypeBattleVitals.Box(transform,"OvertimeScreenEffect",Color.clear,Vector2.zero,Vector2.one);
            for(int i=0;i<4;i++)
            {
                var edge=PrototypeBattleVitals.Box(overtimeWash.transform,"RedEdge",new Color(1,.15f,.04f,.65f),Vector2.zero,Vector2.one);
                if(i<2){edge.rectTransform.anchorMin=new Vector2(i,0);edge.rectTransform.anchorMax=new Vector2(i,1);edge.rectTransform.sizeDelta=new Vector2(8,0);}
                else {edge.rectTransform.anchorMin=new Vector2(0,i-2);edge.rectTransform.anchorMax=new Vector2(1,i-2);edge.rectTransform.sizeDelta=new Vector2(0,8);}
            }
            var banner=PrototypeBattleVitals.Box(overtimeWash.transform,"OvertimeBanner",new Color(.28f,.035f,.02f,.96f),new Vector2(.5f,1),new Vector2(.5f,1));
            banner.rectTransform.sizeDelta=new Vector2(820,54);banner.rectTransform.anchoredPosition=new Vector2(0,-114);
            overtimeText=PrototypeBattleVitals.Label(banner.transform,playerName.font,23);
            overtimeWash.gameObject.SetActive(false);
        }
        private void RefreshReadableVitals()
        {
            if(leftVitals==null)return;
            battle.Presentation?.BindNumberViewport(arenaViewport.rectTransform,playerName.font);
            leftVitals.Refresh(battle.Player);rightVitals.Refresh(battle.Enemy);
            bool overtime=battle.IsFighting&&battle.Elapsed>=battle.Balance.suddenDeathStart;
            overtimeWash.gameObject.SetActive(overtime);
            if(overtime)
            {
                overtimeWash.color=new Color(1,.12f,.02f,.035f+.025f*(1+Mathf.Sin(Time.unscaledTime*4)));
                overtimeText.text=$"决胜耗损  −{battle.Balance.OvertimeDamage(battle.Elapsed):0.0} 生命/秒\n穿透护盾与无敌 · 治疗/获盾剩余 {battle.Balance.RecoveryMultiplier(battle.Elapsed):0%}";
            }
        }
    }
}
