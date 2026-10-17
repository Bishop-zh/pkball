using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype
{
    public sealed partial class PrototypePreparationUI
    {
        [SerializeField] private int modernLayoutVersion;
        public int ModernLayoutVersion=>modernLayoutVersion;
        [SerializeField] private Text[] resourceValues;
        [SerializeField] private Text stageCaption, rarityHint, activityText;
        [SerializeField] private RectTransform activityPanel, activityContent;
        [SerializeField] private ScrollRect activityScroll;
        [SerializeField] private Image[] eventArt;
        private int logVersion=-1,logCount=-1;
        private static void Move(RectTransform rect,Vector2 pos,Vector2 size)
        {rect.anchoredPosition=pos;rect.sizeDelta=size;}
        private Image ModernImage(string name,Transform parent,string resource,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>();image.sprite=PrototypeUiSkin.SpriteFor(resource);image.raycastTarget=false;image.preserveAspect=true;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);Move(rect,pos,size);return image;
        }
        public void BuildModernView()
        {
            if(content==null)return;
            modernLayoutVersion=4;
            if(activityPanel==null)
            {
                resourceValues=new Text[4];string[] ids={"endurance_star","victory_trophy","gold_coin","level_badge"};
                for(int i=0;i<4;i++)
                {
                    ModernImage("ResourceIcon"+i,content,"PreparationArt/"+ids[i],new Vector2(1100+i*185,-20),new Vector2(48,48));
                    resourceValues[i]=Label("ResourceValue"+i,content,"",29,new Vector2(1156+i*185,-23),new Vector2(117,48),TextAnchor.MiddleLeft);
                }
                stageCaption=Label("DayStage",content,"",20,new Vector2(1090,-74),new Vector2(785,32),TextAnchor.MiddleRight);
                rarityHint=Label("ShopRarityHint",shopGroup,"",20,new Vector2(870,-185),new Vector2(965,56),TextAnchor.UpperLeft);
                activityPanel=Panel("ActivityLog",content,new Color(1,1,1,.97f),new Vector2(840,-655),new Vector2(1050,140));
                var panel=activityPanel.GetComponent<Image>();panel.sprite=PrototypeUiSkin.SpriteFor("PreparationArt/log_panel");panel.type=Image.Type.Sliced;panel.raycastTarget=false;
                ModernImage("LogIcon",activityPanel,"PreparationArt/log_scroll",new Vector2(15,-8),new Vector2(28,28));
                Label("LogTitle",activityPanel,"旅程日志 · 滚轮查看",20,new Vector2(55,-8),new Vector2(650,28),TextAnchor.MiddleLeft);
                var viewport=Panel("LogViewport",activityPanel,new Color(0,0,0,.01f),new Vector2(20,-40),new Vector2(1010,87));
                viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
                var go=new GameObject("LogContent",typeof(RectTransform));go.transform.SetParent(viewport,false);activityContent=(RectTransform)go.transform;
                activityContent.anchorMin=new Vector2(0,1);activityContent.anchorMax=new Vector2(1,1);activityContent.pivot=new Vector2(0,1);activityContent.sizeDelta=new Vector2(0,87);
                activityText=Label("LogLines",activityContent,"",18,Vector2.zero,new Vector2(1005,87),TextAnchor.UpperLeft);
                activityText.horizontalOverflow=HorizontalWrapMode.Wrap;activityText.verticalOverflow=VerticalWrapMode.Overflow;
                activityScroll=activityPanel.gameObject.AddComponent<ScrollRect>();activityScroll.viewport=viewport;activityScroll.content=activityContent;activityScroll.horizontal=false;
                activityScroll.movementType=ScrollRect.MovementType.Clamped;activityScroll.scrollSensitivity=28;
                eventArt=new Image[3];for(int i=0;i<3;i++)eventArt[i]=ModernImage("EventArt",choiceButtons[i].transform,"PreparationArt/shop_event",new Vector2(96,-70),new Vector2(128,105));
            }
            EnsureDragTargets();
            headline.gameObject.SetActive(false);
            Move(choicePanel,new Vector2(840,-120),new Vector2(1050,520));
            Move(choiceTitle.rectTransform,new Vector2(20,-15),new Vector2(1010,60));choiceTitle.fontSize=30;
            for(int i=0;i<3;i++)
            {
                bool twoEvents=run==null||run.IsEventStage;
                Move((RectTransform)choiceButtons[i].transform,new Vector2(18+i*(twoEvents?510:338),-80),new Vector2(twoEvents?488:320,415));
                if(twoEvents){var text=choiceButtons[i].GetComponentInChildren<Text>();Move(text.rectTransform,new Vector2(24,-185),new Vector2(440,170));Move(eventArt[i].rectTransform,new Vector2(180,-70),new Vector2(128,105));}
                var frame=(RectTransform)offerPortraits[i].transform.parent.Find("MonsterFrame");Move(frame,new Vector2(65,-68),new Vector2(190,190));
                Move(offerPortraits[i].rectTransform,new Vector2(75,-78),new Vector2(170,170));
                Move(tierMedals[i].rectTransform,new Vector2(255,-14),new Vector2(48,48));
                Move(tierLabels[i].rectTransform,new Vector2(16,-365),new Vector2(288,32));
            }
            var shopPanel=shopGroup.Find("ShopPanel") as RectTransform;if(shopPanel!=null)shopPanel.sizeDelta=new Vector2(1050,520);
            for(int i=0;i<4;i++)Move((RectTransform)shopButtons[i].transform,new Vector2(865+i%2*500,-255-i/2*150),new Vector2(470,135));
            Move((RectTransform)rerollButton.transform,new Vector2(865,-580),new Vector2(470,60));
            Move((RectTransform)actionButton.transform,new Vector2(1410,-580),new Vector2(430,60));
            Move(contextPortrait.transform.parent as RectTransform,new Vector2(870,-285),new Vector2(160,160));
            Move(contextPortrait.rectTransform,new Vector2(8,-8),new Vector2(144,144));
            Move(selectedMonsterSummary.rectTransform,new Vector2(1060,-285),new Vector2(770,160));
            Move(opponentLoadoutTitle.rectTransform,new Vector2(870,-460),new Vector2(950,40));
            for(int i=0;i<contextItemIcons.Length;i++)Move(contextItemIcons[i].transform.parent as RectTransform,new Vector2(870+i*242,-507),new Vector2(225,62));
            var equipTitle=equipmentGroup.Find("EquipTitle")?.GetComponent<Text>();if(equipTitle!=null)equipTitle.text="阵容与装备";
            foreach(var button in new[]{rerollButton,expandButton,statsToggle,sellOreButton,actionButton})
            {var image=button.GetComponent<Image>();image.sprite=PrototypeUiSkin.SpriteFor("PreparationArt/"+(button==actionButton?"coral_button":"cyan_button"));image.type=Image.Type.Simple;image.preserveAspect=false;}
        }
        private string EventIcon(PrototypeEventKind kind)
        {return kind==PrototypeEventKind.Shop?"shop_event":kind==PrototypeEventKind.MasterTraining?"training_event":kind==PrototypeEventKind.Enchant?"enchant_event":kind==PrototypeEventKind.Injections?"potion_event":kind==PrototypeEventKind.Rest?"endurance_star":"gold_coin";}
        private void RefreshModernView()
        {
            BuildModernView();
            resourceValues[0].text=run.Endurance+"/10";resourceValues[1].text=run.Wins+"/10";resourceValues[2].text=run.Gold.ToString();resourceValues[3].text=run.Level.ToString();
            stageCaption.text=$"第 {run.Day} 天 · {StageName(run.Stage)} · 匹配败扣 {run.DefeatCost} · 保命{(run.LastStandUsed?"已用":"可用")}";
            rarityHint.text=run.ShopRarityHint;
            for(int i=0;i<3;i++){var surface=choiceButtons[i].GetComponent<Image>();surface.sprite=PrototypeUiSkin.SpriteFor("PreparationArt/log_panel");surface.type=Image.Type.Sliced;surface.preserveAspect=false;eventArt[i].gameObject.SetActive(run.IsEventStage);eventArt[i].sprite=PrototypeUiSkin.SpriteFor("PreparationArt/"+EventIcon(run.EventAt(i)));}
            if(logVersion!=run.Version||logCount!=run.ActivityLog.Count)
            {
                activityText.text=run.ActivityLog.Count==0?"旅程开始。选择事件，奖励与结算将在这里记录。":string.Join("\n",run.ActivityLog);
                float height=Mathf.Max(87,activityText.preferredHeight+4);activityText.rectTransform.sizeDelta=new Vector2(1005,height);activityContent.sizeDelta=new Vector2(0,height);
                Canvas.ForceUpdateCanvases();activityScroll.verticalNormalizedPosition=0;logVersion=run.Version;logCount=run.ActivityLog.Count;
            }
        }
    }
}
