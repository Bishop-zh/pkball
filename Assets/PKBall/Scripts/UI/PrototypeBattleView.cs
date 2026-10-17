using System;
using UnityEngine;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    // Saved scene view. Gameplay owns every cooldown; this component only reads snapshots.
    public sealed partial class PrototypeBattleView : MonoBehaviour
    {
        [Serializable] public sealed class Slot
        {
            public Image icon, cooldown, production;
            public Text label, state, count, levelText;
            [NonSerialized] public Text metrics;
            [NonSerialized] public Text[] metricValues;
            [NonSerialized] public Image[] metricTiles;
            [NonSerialized] public PrototypeItemDefinition definition;
            [NonSerialized] public int equipmentIndex, level;
        }
        [SerializeField] private PrototypeBattleSkin skin;
        [SerializeField] private Image background, leftPanel, rightPanel;
        [SerializeField] private Image playerHealth, enemyHealth, playerEnergy, enemyEnergy;
        [SerializeField] private Image playerPortrait, enemyPortrait;
        [SerializeField] private Text playerName, enemyName, playerVitals, enemyVitals, clock, instructions, speedText;
        [SerializeField] private Text playerStatus, enemyStatus;
        [SerializeField] private Button speedButton, restartButton, menuButton;
        [SerializeField] private RawImage arenaViewport;
        [SerializeField] private Slot[] playerSlots, enemySlots;
        private PrototypeBattle battle;
        private Camera arenaCamera;
        [SerializeField] private RectTransform itemDetails;
        [SerializeField] private Text itemDetailsText;
        private PrototypeBattleItemHover detailsOwner;
        private float nextDetailsRefresh;
        public void BuildDetailsView()
        {
            if(itemDetails!=null)
            {
                itemDetails.GetComponent<Image>().color=new Color(.29f,.19f,.15f,.98f);
                itemDetailsText.alignment=TextAnchor.UpperCenter;itemDetailsText.supportRichText=true;
                itemDetailsText.resizeTextForBestFit=false;itemDetailsText.fontSize=22;itemDetails.sizeDelta=new Vector2(480,680);return;
            }
            var go=new GameObject("BattleItemDetails",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            go.transform.SetParent(transform,false);itemDetails=(RectTransform)go.transform;
            itemDetails.anchorMin=itemDetails.anchorMax=new Vector2(.5f,1);itemDetails.pivot=new Vector2(.5f,1);
            itemDetails.sizeDelta=new Vector2(480,680);
            var panel=go.GetComponent<Image>();panel.color=new Color(.29f,.19f,.15f,.98f);panel.raycastTarget=false;
            var label=new GameObject("Details",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));label.transform.SetParent(go.transform,false);
            var rect=(RectTransform)label.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(22,18);rect.offsetMax=new Vector2(-22,-18);
            itemDetailsText=label.GetComponent<Text>();itemDetailsText.font=playerName.font;itemDetailsText.fontSize=22;
            itemDetailsText.color=new Color(1,.96f,.82f);itemDetailsText.alignment=TextAnchor.UpperCenter;itemDetailsText.supportRichText=true;
            itemDetailsText.resizeTextForBestFit=false;itemDetailsText.resizeTextMinSize=16;itemDetailsText.resizeTextMaxSize=22;itemDetailsText.raycastTarget=false;
            go.SetActive(false);
        }
        public PrototypeItemInstance ItemFor(bool own,int index)
        {
            var slots=own?playerSlots:enemySlots;
            if(battle==null || index<0 || index>=slots.Length) return null;
            return (own?battle.Player:battle.Enemy)?.ItemAt(slots[index].equipmentIndex);
        }
        public void ShowItemDetails(PrototypeBattleItemHover source,bool own,int index,int preview)
        {
            var item=ItemFor(own,index);if(item==null) {HideItemDetails(source);return;}
            bool changed=detailsOwner!=source;detailsOwner=source;
            itemDetails.gameObject.SetActive(true);itemDetails.SetAsLastSibling();
            itemDetails.anchoredPosition=new Vector2(own?-315:315,-215);
            if(!changed && Time.unscaledTime<nextDetailsRefresh) return;
            nextDetailsRefresh=Time.unscaledTime+.15f;
            int level=preview==0?item.level:preview;
            var actor=own?battle.Player:battle.Enemy;var slot=(own?playerSlots:enemySlots)[index];
            itemDetailsText.text=PrototypeEffectDisplay.Tooltip(item,level)+
                $"\n<color=#CDBCA6>剩余冷却 {actor.CooldownRemaining(slot.equipmentIndex):0.0}秒 · 本场产物 {actor.ProducedCount(slot.equipmentIndex)}</color>"+
                (own && !battle.IsBroadcast?"\n"+PrototypeDayRules.SlotDescription(battle.Run.SlotEffects[slot.equipmentIndex]):"");
            if(item.definition.itemId=="green_flying_sword")itemDetailsText.text+=$"\n<color=#BBE8CF>在场飞剑 {battle.LogicalFlyingSwordCount(actor)}</color>";
            if(item.definition.itemId=="slime_core")itemDetailsText.text+=$"\n<color=#BBE8CF>本场发动 {actor.SlimeGrowthActivations} 次</color>";
            itemDetails.sizeDelta=new Vector2(480,Mathf.Clamp(itemDetailsText.preferredHeight+42,180,680));
        }
        public void HideItemDetails(PrototypeBattleItemHover source)
        {if(detailsOwner!=source) return;detailsOwner=null;if(itemDetails!=null)itemDetails.gameObject.SetActive(false);}
        private void BindHover(Slot[] slots,bool own)
        {
            for(int i=0;i<slots.Length;i++)
            {
                var go=slots[i].icon.transform.parent.gameObject;var surface=go.GetComponent<Image>();if(surface!=null)surface.raycastTarget=true;
                foreach(var graphic in go.GetComponentsInChildren<Graphic>(true)) if(graphic!=surface)graphic.raycastTarget=false;
                var hover=go.GetComponent<PrototypeBattleItemHover>()??go.AddComponent<PrototypeBattleItemHover>();hover.Bind(this,own,i);
            }
        }
        private RenderTexture renderTarget;
        private float nextMetricsRefresh;
        private float nextTextRefresh;
        private int runVersion = -1;
        public PrototypeBattleSkin Skin => skin;

        public void Bind(PrototypeBattle owner)
        {
            battle = owner;
            arenaCamera = owner.ArenaCamera;
            if (renderTarget == null)
            {
                renderTarget = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
                renderTarget.name = "PKBallArenaViewport";
                renderTarget.Create();
            }
            arenaCamera.targetTexture = renderTarget;
            arenaCamera.rect = new Rect(0, 0, 1, 1);
            arenaCamera.aspect = 1f;
            arenaCamera.orthographicSize = 4.5f;
            arenaCamera.allowHDR = false;
            arenaCamera.clearFlags = CameraClearFlags.SolidColor;
            arenaCamera.backgroundColor = Color.clear;
            arenaViewport.texture = renderTarget;
            speedButton.onClick.RemoveAllListeners(); speedButton.onClick.AddListener(owner.ToggleSpeed);
            restartButton.onClick.RemoveAllListeners(); restartButton.onClick.AddListener(owner.Restart);
            menuButton.onClick.RemoveAllListeners(); menuButton.onClick.AddListener(owner.ReturnToMenu);
            ApplySkin(skin); ApplyPresentationLayout();
            BuildDetailsView(); BindHover(playerSlots,true); BindHover(enemySlots,false);
            BuildMetrics(playerSlots); BuildMetrics(enemySlots);
            BuildReadableVitals();
            BindSlots();
            UpdateText();
        }

        public void ApplySkin(PrototypeBattleSkin value)
        {
            if (value == null) return;
            skin = value;
            background.sprite = value.background;
            leftPanel.sprite = value.playerPanel; rightPanel.sprite = value.enemyPanel;
            playerHealth.sprite = value.playerHealth; enemyHealth.sprite = value.enemyHealth;
            playerEnergy.sprite = value.energyFill; enemyEnergy.sprite = value.energyFill;
            foreach (var s in GetComponentsInChildren<Image>(true))
            {
                switch (s.name)
                {
                    case "SlotSurface": s.sprite = value.itemTile; break;
                    case "HealthFrame": s.sprite = value.healthFrame; break;
                    case "EnergyFrame": s.sprite = value.energyFrame; break;
                    case "CountBadge": s.sprite = value.countBadge; break;
                }
            }
            speedButton.GetComponent<Image>().sprite = value.cyanButton;
            restartButton.GetComponent<Image>().sprite = value.coralButton;
            menuButton.GetComponent<Image>().sprite = value.cyanButton;
            if (battle == null) return;
            foreach (var renderer in battle.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == "SkinArenaFloor") renderer.sprite = value.arenaFloor;
                if (renderer.name == "SkinArenaRim") renderer.sprite = value.arenaRim;
            }
        }

        private void BindSlots()
        {
            runVersion = battle.Run.Version;
            for(int i=0;i<playerSlots.Length;i++)
            {var slot=playerSlots[i];slot.equipmentIndex=battle.IsBroadcast?i:CampaignSlot(i);var item=battle.Player.ItemAt(slot.equipmentIndex);slot.definition=item?.definition;SetSlot(slot,slot.definition,item?.level??1,battle.IsBroadcast||slot.equipmentIndex>=0);}
            for(int i=0;i<enemySlots.Length;i++)
            {var slot=enemySlots[i];slot.equipmentIndex=i;var item=battle.Enemy.ItemAt(i);slot.definition=item?.definition;SetSlot(slot,slot.definition,item?.level??1,i<8||item!=null);}

        }
        private int CampaignSlot(int visualIndex){int n=0;for(int i=0;i<10;i++)if(battle.Run.IsSlotOpen(i)){if(n++==visualIndex)return i;}return -1;}
        private static void SetSlot(Slot slot, PrototypeItemDefinition definition, int level, bool visible)
        {
            slot.level = level;
            slot.icon.transform.parent.gameObject.SetActive(visible);
            slot.icon.sprite = definition == null ? null : PrototypeUiSkin.SpriteFor("Icons/" + definition.iconId);
            slot.icon.enabled = slot.icon.sprite != null;
            slot.label.text = definition == null ? "" : definition.displayName;
            if(slot.levelText!=null) {slot.levelText.text=definition==null?"":"Lv."+level;slot.levelText.color=definition==null?Color.white:PrototypeItemRepository.RarityColor(definition.rarity);}
            var surface=slot.icon.transform.parent.GetComponent<Image>();if(surface!=null)surface.color=definition==null?new Color(.12f,.20f,.24f):Color.Lerp(new Color(.06f,.11f,.16f),PrototypeItemRepository.RarityColor(definition.rarity),.28f);
            slot.count.transform.parent.gameObject.SetActive(false);
            slot.cooldown.gameObject.SetActive(definition != null && (definition.HasActive || definition.itemId == "devourer"));
            if(slot.production!=null) slot.production.gameObject.SetActive(definition!=null && definition.productionInterval>0);
            slot.state.text = "";
        }
        private void Update()
        {
            if (battle == null || battle.Player == null || battle.Enemy == null) return;
            if (runVersion != battle.Run.Version) BindSlots();
            // Continuous masks, but text/layout update only at a bounded rate.
            UpdateSlots(playerSlots, battle.Player, true);
            UpdateSlots(enemySlots, battle.Enemy, false);
            if(Time.unscaledTime>=nextMetricsRefresh){nextMetricsRefresh=Time.unscaledTime+.1f;UpdateMetrics(playerSlots,battle.Player);UpdateMetrics(enemySlots,battle.Enemy);}
            playerEnergy.fillAmount = Mathf.Clamp01(battle.Player.Energy / battle.Balance.maximumEnergy);
            enemyEnergy.fillAmount = Mathf.Clamp01(battle.Enemy.Energy / battle.Balance.maximumEnergy);
            if (Time.unscaledTime < nextTextRefresh) return;
            nextTextRefresh = Time.unscaledTime + .1f;
            UpdateText();
        }
        private void UpdateSlots(Slot[] slots, PrototypeActor actor, bool player)
        {
            foreach (Slot slot in slots)
            {
                if (slot.definition == null) continue;
                float remaining = actor.CooldownRemaining(slot.equipmentIndex);
                float duration = actor.CooldownDuration(slot.equipmentIndex);
                slot.cooldown.fillAmount = duration <= 0 ? 0 : Mathf.Clamp01(remaining / duration);
                string value = remaining > 0 ? remaining.ToString("0.0") + "秒" :
                    slot.definition.weapon != null && slot.definition.weapon.kind == PrototypeWeaponKind.Melee ? "就绪" : "可激活";
                if(duration<=0) value="";
                if (slot.state.text != value) slot.state.text = value;
                if(slot.production!=null && slot.definition.productionInterval>0)
                {
                    float period=slot.definition.ProductionInterval(slot.level);
                    slot.production.fillAmount=1-Mathf.Clamp01(actor.ProductionRemaining(slot.equipmentIndex)/period);
                }
                int count=actor.ProducedCount(slot.equipmentIndex);
                slot.count.transform.parent.gameObject.SetActive(count>0);
                string text=count.ToString(); if(slot.count.text!=text) slot.count.text=text;
            }
        }
        // Shared by the saved prefab migration and runtime, including broadcast battles.
        public void ApplyPresentationLayout()
        {
            LayoutSide(leftPanel,playerSlots,playerName,playerPortrait,playerVitals,playerHealth,playerEnergy,playerStatus);
            LayoutSide(rightPanel,enemySlots,enemyName,enemyPortrait,enemyVitals,enemyHealth,enemyEnergy,enemyStatus);
        }
        private static void LayoutSide(Image panel,Slot[] slots,Text name,Image portrait,Text vitals,Image health,Image energy,Text status)
        {
            panel.rectTransform.sizeDelta=new Vector2(420,900);
            panel.rectTransform.anchoredPosition=new Vector2(panel.rectTransform.anchoredPosition.x<0?-725:725,-12);
            name.fontSize=28; name.rectTransform.sizeDelta=new Vector2(370,44);
            portrait.rectTransform.sizeDelta=new Vector2(98,98);
            vitals.fontSize=23; vitals.rectTransform.sizeDelta=new Vector2(360,34);
            ((RectTransform)health.transform.parent).sizeDelta=new Vector2(350,38);
            health.rectTransform.sizeDelta=new Vector2(304,20);
            ((RectTransform)energy.transform.parent).sizeDelta=new Vector2(350,20);
            energy.rectTransform.sizeDelta=new Vector2(306,8);
            if(status!=null){status.rectTransform.anchoredPosition=new Vector2(0,-405);status.rectTransform.sizeDelta=new Vector2(370,48);status.fontSize=21;}
            for(int i=0;i<slots.Length;i++)
            {
                var slot=slots[i];var cell=(RectTransform)slot.icon.transform.parent;
                cell.sizeDelta=new Vector2(174,104);cell.anchoredPosition=new Vector2(i%2==0?-94:94,68-i/2*108);
                slot.icon.rectTransform.sizeDelta=slot.cooldown.rectTransform.sizeDelta=new Vector2(78,62);
                slot.icon.rectTransform.anchoredPosition=slot.cooldown.rectTransform.anchoredPosition=new Vector2(0,0);
                slot.label.fontSize=17;slot.label.rectTransform.sizeDelta=new Vector2(162,24);slot.label.rectTransform.anchoredPosition=new Vector2(0,-37);
                slot.state.fontSize=18;slot.state.rectTransform.sizeDelta=new Vector2(100,25);slot.state.rectTransform.anchoredPosition=new Vector2(0,-1);
                if(slot.levelText!=null){slot.levelText.fontSize=12;slot.levelText.rectTransform.anchoredPosition=new Vector2(-58,-17);}
                var badge=(RectTransform)slot.count.transform.parent;badge.anchoredPosition=new Vector2(62,-17);badge.sizeDelta=new Vector2(36,24);slot.count.fontSize=17;
                if(slot.production!=null){slot.production.rectTransform.sizeDelta=new Vector2(144,4);slot.production.rectTransform.anchoredPosition=new Vector2(0,-24);}
            }
        }
        private static void BuildMetrics(Slot[] slots)
        {
            foreach(var slot in slots)
            {
                if(slot.metricValues!=null)continue;
                var cell=(RectTransform)slot.icon.transform.parent;
                var go=new GameObject("TopThreeEffects",typeof(RectTransform));go.transform.SetParent(cell,false);
                var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-6);rect.sizeDelta=new Vector2(158,23);
                slot.metricValues=new Text[3];slot.metricTiles=new Image[3];
                for(int i=0;i<3;i++)
                {
                    var tile=new GameObject("Metric"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));tile.transform.SetParent(go.transform,false);
                    var tr=(RectTransform)tile.transform;tr.anchorMin=tr.anchorMax=new Vector2(.5f,.5f);tr.sizeDelta=new Vector2(50,23);
                    var bg=tile.GetComponent<Image>();bg.raycastTarget=false;slot.metricTiles[i]=bg;
                    var label=new GameObject("Value",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));label.transform.SetParent(tile.transform,false);
                    var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(2,0);lr.offsetMax=new Vector2(-2,0);
                    var text=label.GetComponent<Text>();text.font=slot.label.font;text.fontSize=18;text.fontStyle=FontStyle.Bold;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=18;
                    var outline=label.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.8f);outline.effectDistance=new Vector2(1,-1);slot.metricValues[i]=text;
                }
                slot.metrics=slot.metricValues[0];go.SetActive(false);
            }
        }
        private struct RankedMetric {public PrototypeEffectKind kind;public float value,score;}
        private readonly RankedMetric[] topMetrics=new RankedMetric[3];
        private static readonly PrototypeEffectKind[] metricKinds={PrototypeEffectKind.Damage,PrototypeEffectKind.Shield,PrototypeEffectKind.Heal,PrototypeEffectKind.Poison,PrototypeEffectKind.Burn,PrototypeEffectKind.Regeneration};
        private void UpdateMetrics(Slot[] slots,PrototypeActor actor)
        {
            foreach(var slot in slots){if(slot.metrics==null)continue;int count=0;
                if(slot.definition!=null)foreach(var kind in metricKinds){float value=actor.GetEffectValueAt(slot.equipmentIndex,kind);if(value<=0)continue;float score=value*(kind==PrototypeEffectKind.Poison||kind==PrototypeEffectKind.Burn?5:1);int index=0;while(index<count&&topMetrics[index].score>=score)index++;if(index>=3)continue;for(int n=Mathf.Min(count,2);n>index;n--)topMetrics[n]=topMetrics[n-1];topMetrics[index]=new RankedMetric{kind=kind,value=value,score=score};count=Mathf.Min(3,count+1);}
                slot.metrics.transform.parent.parent.gameObject.SetActive(count>0);
                for(int i=0;i<3;i++)
                {
                    var tile=slot.metricTiles[i];tile.gameObject.SetActive(i<count);if(i>=count)continue;
                    var m=topMetrics[i];
                    tile.color=m.kind==PrototypeEffectKind.Damage?new Color(.8f,.16f,.22f):m.kind==PrototypeEffectKind.Shield?new Color(.76f,.8f,.85f):m.kind==PrototypeEffectKind.Burn?new Color(.88f,.35f,.12f):m.kind==PrototypeEffectKind.Poison?new Color(.3f,.62f,.18f):m.kind==PrototypeEffectKind.Heal?new Color(.16f,.65f,.39f):new Color(.65f,.48f,.16f);
                    slot.metricValues[i].color=m.kind==PrototypeEffectKind.Shield?new Color(.10f,.15f,.21f):Color.white;
                    tile.rectTransform.anchoredPosition=new Vector2((i-(count-1)*.5f)*54,0);
                    float v=m.value;string number=v>=1000000?(v/1000000).ToString("0.#")+"m":v>=1000?(v/1000).ToString("0.#")+"k":v.ToString(v<10?"0.##":"0.#");
                    if(slot.metricValues[i].text!=number)slot.metricValues[i].text=number;
                }
            }
        }
        private void UpdateText()
        {
            if (battle?.Player == null || battle.Enemy == null) return;
            playerName.text = battle.LeftDisplayName; enemyName.text = battle.RightDisplayName;
            playerVitals.text = $"生命 {battle.Player.Health:0}/{battle.Player.MaximumHealth:0}";
            enemyVitals.text = $"生命 {battle.Enemy.Health:0}/{battle.Enemy.MaximumHealth:0}";
            RefreshReadableVitals();
            if(playerStatus!=null) playerStatus.text=StatusText(battle.Player.State);
            if(enemyStatus!=null) enemyStatus.text=StatusText(battle.Enemy.State);
            clock.text = battle.IsBroadcast?$"主播单挑     {battle.Elapsed:0.0} 秒":$"第 {battle.Run.Day} 天     {battle.Elapsed:0.0} 秒";
            speedText.text = battle.SpeedMultiplier > 1 ? "速度 2×" : "速度 1×";
            instructions.text = battle.Result;
            menuButton.interactable = battle.IsBroadcast || !battle.IsFighting;
            playerPortrait.sprite = PrototypeUiSkin.SpriteFor("Art/" + battle.LeftVisual);
            enemyPortrait.sprite = PrototypeUiSkin.SpriteFor("Art/" + battle.RightVisual);
        }
        private static string StatusText(PrototypeCombatState state) =>
            $"<color=#F4CE62>盾 {state.Shield:0.#}</color>    <color=#85DF61>毒 {state.Poison:0.#}</color>\n" +
            $"<color=#FF9855>火 {state.Burn:0.#}</color>    <color=#6DECA6>再生 {state.Regeneration:0.#}/秒</color>";
        public bool TryPointerRay(Vector2 screenPosition, out Ray ray)
        {
            ray = default;
            if (arenaCamera == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                arenaViewport.rectTransform, screenPosition, null, out Vector2 local)) return false;
            Rect rect = arenaViewport.rectTransform.rect;
            Vector2 uv = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return false;
            ray = arenaCamera.ViewportPointToRay(uv);return true;
        }
        private void OnDestroy()
        {
            if (arenaCamera != null && arenaCamera.targetTexture == renderTarget) arenaCamera.targetTexture = null;
            if (renderTarget != null) { renderTarget.Release(); Destroy(renderTarget); }
        }
    }
}
