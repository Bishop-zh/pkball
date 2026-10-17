using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype
{
    public sealed class PrototypeBroadcastEditor : MonoBehaviour
    {
        [Serializable] private sealed class Numeric {public string key;public InputField input;}
        [Serializable] private sealed class AudioChoice {public string key;public Dropdown dropdown;public Button preview;[NonSerialized]public List<string> ids;}
        [SerializeField] private List<AudioChoice> audioChoices=new List<AudioChoice>();
        private PrototypeAudioPlayback audioPreview;
        [Serializable] private sealed class Choice {public string key;public Button button;public Image image;public Text text;public PrototypeEffectPreviewGraphic effect;}
        [SerializeField] private Dropdown itemAnimationPreset;
        [SerializeField] private Button itemAnimationToggle,itemAnimationReset,itemAnimationIcon;
        [SerializeField] private Image itemAnimationSwatch;
        [SerializeField] private Text itemAnimationState;
        [SerializeField] private List<Numeric> animationNumeric=new List<Numeric>();
        [SerializeField] private int layoutVersion=11;
        [SerializeField] private GameObject panel,gallery,monsterPoolDialog;
        [SerializeField] private Button addMonster,monsterPoolCancel,monsterPoolConfirm;
        [SerializeField] private Dropdown monsterPoolTier;
        [SerializeField] private InputField monsterPoolReferenceDay;
        [SerializeField] private Image monsterPoolPortrait;
        [SerializeField] private Text monsterPoolName,monsterPoolError;
        [SerializeField] private Button galleryClose,addAbsorbEquipment,addAbsorbPotion;
        [SerializeField] private Text runtimePreview;
        [SerializeField] private RectTransform absorptionList,affixList;
        [SerializeField] private Button addAffix,resetAffixes;
        [SerializeField] private Button entry,back,start,save,leftTab,rightTab,saveCharacter,portraitButton,legacyButton,chooseItem;
        [SerializeField] private InputField title,sideName,health,speed,shield,bonus,reduction,gallerySearch;
        [SerializeField] private Dropdown preset,characterPreset,level,weaponKind;
        [SerializeField] private Text message,summary,portraitName,galleryTitle,selectedTitle;
        [SerializeField] private Image preview,selectedIcon;
        [SerializeField] private Button[] slotButtons,propertyTabs;
        [SerializeField] private Image[] slotIcons;
        [SerializeField] private Text[] slotNames;
        [SerializeField] private RectTransform galleryContent,propertyViewport;
        [SerializeField] private ScrollRect propertyScroll;
        [SerializeField] private GameObject[] propertyPages;
        [SerializeField] private List<Numeric> numeric=new List<Numeric>();
        [SerializeField] private List<Choice> choices=new List<Choice>();
        private enum GalleryTarget { Portrait, Item, Projectile, Hit, Shape, Release, Legacy, AbsorbEquipment, AbsorbPotion }
        private sealed class Option {public string id,name;public Sprite sprite;public int value;public PrototypePreviewShape shape=PrototypePreviewShape.None;public Color color=Color.white;}
        private PrototypeBattle battle;
        private PrototypeBroadcastConfig config=new PrototypeBroadcastConfig();
        private readonly List<string> visuals=new List<string>();
        private PrototypeCharacterProfile[] builtinCharacters;
        private PrototypeShowcaseMatch[] builtinMatches;
        private string[] savedCharacterNames,savedMatchNames;
        private void RefreshEffectLabels()
        {
            // Existing authored scenes retain labels; update those as well as runtime-built UI.
            var captions=new Dictionary<string,string> {
                {"每次命中附加毒层","毒覆盖·填值改为释放"},
                {"每次命中附加燃烧","火覆盖·填值改为释放"},
                {"每次释放增加自身护盾","盾覆盖·填值改为释放"},
                {"每次释放治疗自身","治疗覆盖·填值改为释放"},
                {"每次释放增加本场再生","再生覆盖·填值改为释放"}
            };
            foreach(var text in propertyPages[2].GetComponentsInChildren<Text>(true))
            {
                if(captions.TryGetValue(text.text,out var caption))text.text=caption;
                else if(text.text.StartsWith("数值留空保留原效果"))
                    text.text="留空保留原触发；填值改为释放，0关闭同类效果。\n默认值为同类一级总量，非每次命中量。\n精确触发、等级表：向下滚动编辑词条。";
            }
            foreach(var text in new[]{characterPreset.captionText,characterPreset.itemText})
            {text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=20;}
        }
        private readonly List<Option> options=new List<Option>();
        private readonly Dictionary<GameObject,bool> hiddenSiblings=new Dictionary<GameObject,bool>();
        private bool slotSelected;private bool filling;private int sideIndex,slotIndex,pageIndex;private GalleryTarget target;
        public void Build()
        {
            if(panel!=null)return;
            entry=ButtonAt(transform,"主播模式",720,962,480,62);
            panel=Box(transform,"BroadcastEditor",0,0,1920,1080,new Color(.025f,.055f,.09f,1)).gameObject;
            Label(panel.transform,"主播单挑 · 双方配置",40,18,1100,64,36);
            back=ButtonAt(panel.transform,"返回主页",1640,26,220,56);
            title=Field(panel.transform,"对战名称",40,103,"自定义单挑",450);
            preset=Drop(panel.transform,"对战预设",550,103,470);
            save=ButtonAt(panel.transform,"保存预设",1060,137,260,48);
            start=ButtonAt(panel.transform,"开始单挑",1360,137,500,48);
            leftTab=ButtonAt(panel.transform,"红方配置",40,212,255,55);rightTab=ButtonAt(panel.transform,"蓝方配置",325,212,255,55);
            Box(panel.transform,"角色区",40,290,540,710,new Color(.05f,.11f,.17f));
            sideName=Field(panel.transform,"选手名称",60,302,"红方",500);
            portraitButton=ButtonAt(panel.transform,"",225,390,170,170);portraitButton.GetComponent<Image>().color=new Color(.08f,.17f,.23f);
            preview=Box(portraitButton.transform,"角色预览",8,8,154,154,Color.white);preview.preserveAspect=true;preview.raycastTarget=false;
            portraitName=Label(panel.transform,"",60,563,500,33,22);portraitName.alignment=TextAnchor.MiddleCenter;
            health=Field(panel.transform,"本体生命",60,610,PrototypeRun.BaseHealth.ToString("0"),235);speed=Field(panel.transform,"速度数值",325,610,"100",235);
            shield=Field(panel.transform,"初始护盾",60,694,"0",235);bonus=Field(panel.transform,"伤害加成（0.5 = 50%）",325,694,"0",235);
            reduction=Field(panel.transform,"直伤减免（0～1）",60,778,"0",235);characterPreset=Drop(panel.transform,"角色预设",325,778,235);
            saveCharacter=ButtonAt(panel.transform,"保存当前角色",325,865,235,46);
            Label(panel.transform,"点击头像选择外观 · 装备生命额外加算",60,913,500,30,19);
            addMonster=ButtonAt(panel.transform,"加入怪物池",60,953,500,40);
            Box(panel.transform,"装备区",610,212,450,788,new Color(.05f,.11f,.17f));Label(panel.transform,"装备",632,225,390,46,28);
            Label(panel.transform,"单击选中，再次点击更换装备",632,279,400,36,20);
            slotButtons=new Button[8];slotIcons=new Image[8];slotNames=new Text[8];
            for(int i=0;i<8;i++)
            {
                float x=633+(i%2)*205,y=338+(i/2)*145;
                var image=Box(panel.transform,"EquipmentCell_"+i,x,y,183,130,new Color(.08f,.17f,.23f));var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;slotButtons[i]=button;
                slotIcons[i]=Box(image.transform,"Icon",47,5,89,89,Color.white);slotIcons[i].preserveAspect=true;slotIcons[i].raycastTarget=false;
                slotNames[i]=Label(image.transform,"选择装备",8,93,167,32,19);slotNames[i].alignment=TextAnchor.MiddleCenter;slotNames[i].resizeTextForBestFit=true;slotNames[i].resizeTextMinSize=15;slotNames[i].resizeTextMaxSize=19;
            }
            legacyButton=ButtonAt(panel.transform,"旧预设扩展装备",633,947,388,40);legacyButton.gameObject.SetActive(false);
            Box(panel.transform,"属性区",1090,212,770,788,new Color(.05f,.11f,.17f));selectedTitle=Label(panel.transform,"装备属性",1110,223,710,44,28);
            chooseItem=ButtonAt(panel.transform,"更换当前装备",1110,276,720,74);selectedIcon=Box(chooseItem.transform,"Icon",12,7,60,60,Color.white);selectedIcon.preserveAspect=true;selectedIcon.raycastTarget=false;
            var caption=chooseItem.GetComponentInChildren<Text>();caption.rectTransform.anchoredPosition=new Vector2(90,0);caption.rectTransform.sizeDelta=new Vector2(610,74);caption.alignment=TextAnchor.MiddleLeft;
            propertyTabs=new Button[5];var tabNames=new[]{"基础数值","攻击表现","命中效果","成长吸收","道具动画"};
            for(int i=0;i<5;i++)propertyTabs[i]=ButtonAt(panel.transform,tabNames[i],1110+i*146,366,138,47);
            var viewport=Box(panel.transform,"属性滚动区",1110,431,720,521,new Color(.035f,.085f,.13f));propertyViewport=viewport.rectTransform;viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;propertyScroll=viewport.gameObject.AddComponent<ScrollRect>();propertyScroll.viewport=propertyViewport;propertyScroll.horizontal=false;propertyScroll.movementType=ScrollRect.MovementType.Clamped;
            propertyPages=new GameObject[5];for(int i=0;i<5;i++)propertyPages[i]=Box(viewport.transform,"PropertyPage_"+i,0,0,720,i==1?670:490,Color.clear).gameObject;
            Num(0,"damage","基础直伤",0,0);Num(0,"cooldown","冷却（秒）",1,0);Num(0,"range","攻击距离",0,1);Num(0,"visualSize","武器长度",1,1);Num(0,"healthBonus","装备生命",0,2);Num(0,"speedBonus","装备速度",1,2);Num(0,"effectMultiplier","默认效果倍率",0,3,"1");Num(0,"effectScale","光效大小",1,3);
            level=Drop(propertyPages[0].transform,"道具等级",10,380,340);Label(propertyPages[0].transform,"留空使用默认属性\n伤害及装备加成随等级提升",380,394,330,74,19);
            weaponKind=Drop(propertyPages[1].transform,"攻击类型",10,0,340);Pick("attackShape","近战动作",1,0,1);Num(1,"projectileSpeed","弹速（0.1～200）",0,1);Num(1,"projectileSize","弹体大小（0.05～3）",1,1);
            Pick("projectileVisual","弹体图像",0,2,1);Pick("visualStyle","释放表现",1,2,1);Num(1,"hitDelay","出手延迟（秒）",0,3);Num(1,"animationDuration","动作持续（秒）",1,3);Num(1,"knockback","击退力度（0～50）",0,5);Num(1,"visualCount","齐射视觉数量（1～24）",1,5);AudioPick(1,"releaseSoundId","射击 / 释放音效",376);Num(1,"releaseVolume","释放音量（0～1）",1,6,"1");
            summary=Label(propertyPages[1].transform,"",10,685,690,100,19);((RectTransform)propertyPages[1].transform).sizeDelta=new Vector2(720,795);
            Pick("hitVisual","命中特效",0,0,2);Num(2,"poison","每次命中附加毒层",1,0);Num(2,"burn","每次命中附加燃烧",0,1);Num(2,"shield","每次释放增加自身护盾",1,1);Num(2,"heal","每次释放治疗自身",0,2);Num(2,"regeneration","每次释放增加本场再生",1,2);
            AudioPick(2,"hitSoundId","命中音效",300);Num(2,"hitVolume","命中音量（0～1）",1,4,"1");((RectTransform)propertyPages[2].transform).sizeDelta=new Vector2(720,600);
            Label(propertyPages[2].transform,"数值留空保留原效果，填 0 关闭该效果。\n命中光效不自动附加状态：毒、火由右侧数值控制。\n劈砍/穿刺决定击退方向；力度单独配置。",10,480,690,100,19);
            Num(3,"growth","初始叠层次数",0,0,"0");Num(3,"cooldownMultiplier","永久冷却倍率",1,0,"1");
            runtimePreview=Label(propertyPages[3].transform,"",10,95,690,195,19);runtimePreview.alignment=TextAnchor.UpperLeft;
            addAbsorbEquipment=ButtonAt(propertyPages[3].transform,"添加吸收装备",10,305,340,43);addAbsorbPotion=ButtonAt(propertyPages[3].transform,"添加吸收药水",380,305,340,43);
            absorptionList=Box(propertyPages[3].transform,"AbsorptionList",10,365,690,100,Color.clear).rectTransform;
            Label(panel.transform,"滚轮滚动属性；武器只在释放时显示",1110,959,720,30,19);
            message=Label(panel.transform,"",40,1018,1820,48,21);
            affixList=Box(propertyPages[2].transform,"AffixList",10,685,690,100,Color.clear).rectTransform;
            addAffix=ButtonAt(propertyPages[2].transform,"添加词条",10,610,330,43);resetAffixes=ButtonAt(propertyPages[2].transform,"恢复默认词条",370,610,340,43);
            BuildItemAnimation();BuildGallery();BuildMonsterPoolDialog();panel.SetActive(false);
        }
        private void BuildItemAnimation()
        {
            var p=propertyPages[4].transform;
            itemAnimationToggle=ButtonAt(p,"开启自定义动画",10,10,340,48);
            itemAnimationReset=ButtonAt(p,"恢复道具默认",380,10,330,48);
            itemAnimationState=Label(p,"",10,68,700,62,20);
            itemAnimationPreset=Drop(p,"专属动画预设",10,142,700);
            itemAnimationPreset.AddOptions(new List<string>{"自动按道具匹配","关闭动画","挥扫","穿刺","重击","后坐力","爆发","环绕","扩张环","护盾","治疗","毒液","火焰","锻造","吞噬","成长","巨型月牙"});
            AnimationNum("duration","持续时间（0.05～10秒）",0,242);AnimationNum("scale","动画尺寸（0.1～8）",1,242);
            AnimationNum("intensity","轨迹强度（0～3）",0,336);AnimationNum("count","视觉数量（1～24）",1,336);
            AnimationNum("rotation","额外旋转角度（-360～360）",0,430);
            itemAnimationIcon=ButtonAt(p,"",380,464,330,43);
            Label(p,"动画主色（RGB，0～1）",10,532,690,30,21);
            foreach(var key in new[]{"red","green","blue"})animationNumeric.Add(new Numeric{key=key,input=Field(p,key=="red"?"红":key=="green"?"绿":"蓝",10+(key=="red"?0:key=="green"?1:2)*230,572,"1",210)});
            itemAnimationSwatch=Box(p,"动画颜色预览",10,674,690,38,Color.white);
            Label(p,"释放、触发词条和命中时播放道具专属动画。\n这些参数只控制表现；伤害、射程和状态数值在其他页配置。\n武器弹体、命中特效和音效可与此动画叠加。",10,728,690,112,20);
            ((RectTransform)p).sizeDelta=new Vector2(720,860);
        }
        private void AnimationNum(string key,string caption,int col,float y)=>animationNumeric.Add(new Numeric{key=key,input=Field(propertyPages[4].transform,caption,10+col*370,y,"1",340)});
        private void BindItemAnimation()
        {
            Wire(itemAnimationToggle,()=>Try(()=>{Read();Current.animation=Current.animationSchemaVersion==0?PrototypeItemAnimationDefaults.Resolve(Definition):null;Current.animationSchemaVersion=Current.animation==null?0:1;Fill();}));
            Wire(itemAnimationReset,()=>Try(()=>{Read();Current.animation=null;Current.animationSchemaVersion=0;Fill();}));
            Wire(itemAnimationIcon,()=>Try(()=>{Read();EnsureItemAnimation();Current.animation.showIcon=!Current.animation.showIcon;Fill();}));
            itemAnimationPreset.onValueChanged.RemoveAllListeners();itemAnimationPreset.onValueChanged.AddListener(v=>{if(filling)return;Try(()=>{Read();EnsureItemAnimation();Current.animation.preset=(PrototypeItemAnimationPreset)v;Fill();});});
            foreach(var n in animationNumeric){var field=n;n.input.onEndEdit.RemoveAllListeners();n.input.onEndEdit.AddListener(_=>{if(filling)return;Try(()=>{EnsureItemAnimation();Read();FillItemAnimation();});});}
        }
        private void EnsureItemAnimation(){if(Current.animationSchemaVersion==0 || Current.animation==null)Current.animation=PrototypeItemAnimationDefaults.Resolve(Definition);Current.animationSchemaVersion=1;}
        private void ReadItemAnimation(PrototypeBroadcastSlot slot)
        {
            if(slot.animationSchemaVersion==0 || slot.animation==null)return;
            var a=slot.animation;foreach(var n in animationNumeric){float v=Number(n.input);switch(n.key){case "red":a.tint.r=v;break;case "green":a.tint.g=v;break;case "blue":a.tint.b=v;break;case "count":if(v!=Mathf.Floor(v))throw new FormatException("视觉数量需要整数");a.count=(int)v;break;default:typeof(PrototypeItemAnimation).GetField(n.key).SetValue(a,v);break;}}
        }
        private void FillItemAnimation()
        {
            bool was=filling;filling=true;var a=PrototypeBroadcastDefaults.Animation(Current,Definition);bool custom=Current.animationSchemaVersion==1 && Current.animation!=null;
            itemAnimationToggle.GetComponentInChildren<Text>().text=custom?"关闭自定义 · 沿用默认":"开启自定义动画";
            itemAnimationState.text=(custom?"当前：自定义动画":"当前：道具默认动画")+" · 参数显示实际值\n修改参数自动开启自定义，恢复默认后跟随道具配置。";
            itemAnimationPreset.SetValueWithoutNotify((int)a.preset);itemAnimationIcon.GetComponentInChildren<Text>().text=a.showIcon?"显示道具图像：开":"显示道具图像：关";
            foreach(var n in animationNumeric){float v=n.key=="red"?a.tint.r:n.key=="green"?a.tint.g:n.key=="blue"?a.tint.b:Convert.ToSingle(typeof(PrototypeItemAnimation).GetField(n.key).GetValue(a));n.input.SetTextWithoutNotify(F(v));n.input.interactable=Definition!=null;}
            itemAnimationSwatch.color=a.tint;itemAnimationPreset.interactable=itemAnimationToggle.interactable=itemAnimationReset.interactable=itemAnimationIcon.interactable=Definition!=null;filling=was;
        }
        private void Num(int page,string key,string caption,int col,int row,string value="-1"){numeric.Add(new Numeric {key=key,input=Field(propertyPages[page].transform,caption,10+col*370,row*94,value,340)});}
        private void Pick(string key,string caption,int col,int row,int page)
        {
            var parent=propertyPages[page].transform;float x=10+col*370,y=row*94;Label(parent,caption,x,y,340,30,21);
            var button=ButtonAt(parent,"选择",x,y+34,340,43);var text=button.GetComponentInChildren<Text>();text.rectTransform.anchoredPosition=new Vector2(55,0);text.rectTransform.sizeDelta=new Vector2(270,43);text.alignment=TextAnchor.MiddleLeft;text.fontSize=20;
            var image=Box(button.transform,"Thumbnail",5,3,37,37,Color.white);image.preserveAspect=true;image.raycastTarget=false;
            var fx=EffectBox(button.transform,5,3,37,37);choices.Add(new Choice {key=key,button=button,image=image,text=text,effect=fx});
        }
        private void BuildGallery()
        {
            gallery=Box(panel.transform,"ImagePicker",0,0,1920,1080,new Color(.015f,.04f,.07f,1f)).gameObject;
            galleryTitle=Label(gallery.transform,"选择图像",65,36,1450,60,34);galleryClose=ButtonAt(gallery.transform,"关闭",1620,41,230,52);
            gallerySearch=Field(gallery.transform,"按名称筛选",65,114,"",1785);
            var viewport=Box(gallery.transform,"GalleryViewport",65,218,1785,798,new Color(.04f,.09f,.14f));viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            galleryContent=Box(viewport.transform,"GalleryContent",0,0,1785,798,Color.clear).rectTransform;scroll.viewport=viewport.rectTransform;scroll.content=galleryContent;
            gallerySearch.onValueChanged.AddListener(_=>RenderGallery());gallery.SetActive(false);
        }
        private void BuildMonsterPoolDialog() {
            monsterPoolDialog=Box(panel.transform,"MonsterPoolDialog",0,0,1920,1080,new Color(.008f,.018f,.03f,.91f)).gameObject;
            var card=Box(monsterPoolDialog.transform,"MonsterPoolCard",625,190,670,700,new Color(.045f,.10f,.155f));
            Label(card.transform,"加入排位怪物池",32,22,606,48,30);
            monsterPoolPortrait=Box(card.transform,"Portrait",275,85,120,120,Color.white);monsterPoolPortrait.preserveAspect=true;monsterPoolPortrait.raycastTarget=false;
            monsterPoolName=Label(card.transform,"",32,215,606,42,25);monsterPoolName.alignment=TextAnchor.MiddleCenter;
            monsterPoolTier=Drop(card.transform,"怪物类别",32,278,280);monsterPoolTier.AddOptions(new List<string>{"普通","稀有","BOSS"});
            monsterPoolReferenceDay=Field(card.transform,"基准天数",352,278,"1",286);
            Label(card.transform,"保存当前角色、装备和词条。\n基准天数：这套配置达到当前强度的天数。\n生命每天 +12%，输出每天 +8%，早于基准则降低。\n同名、同类别会更新已有怪物。",32,382,606,133,21);
            monsterPoolError=Label(card.transform,"",32,521,606,63,19);monsterPoolError.color=new Color(1,.6f,.48f);
            monsterPoolCancel=ButtonAt(card.transform,"取消",32,614,280,48);monsterPoolConfirm=ButtonAt(card.transform,"确认加入",352,614,286,48);
            monsterPoolDialog.SetActive(false);
        }
        private void BindMonsterPoolDialog() {
            Wire(addMonster,()=>Try(()=>{Read();monsterPoolPortrait.sprite=PrototypeUiSkin.SpriteFor("Art/"+Side.visualId);monsterPoolPortrait.enabled=monsterPoolPortrait.sprite!=null;monsterPoolName.text=Side.name;monsterPoolError.text="";monsterPoolDialog.SetActive(true);monsterPoolDialog.transform.SetAsLastSibling();}));
            Wire(monsterPoolCancel,()=>monsterPoolDialog.SetActive(false));
            Wire(monsterPoolConfirm,()=>{try{float day=Number(monsterPoolReferenceDay);if(day!=Mathf.Floor(day)||day<1||day>10000)throw new FormatException("基准天数需要 1～10000 的整数");var tier=(PrototypeEncounterTier)monsterPoolTier.value;PrototypeMonsterPool.Save(Side,tier,(int)day);monsterPoolDialog.SetActive(false);message.text="已加入怪物池："+Side.name+" · "+monsterPoolTier.options[monsterPoolTier.value].text+"（当前类别 "+PrototypeMonsterPool.Count(tier)+" 个）";}catch(Exception e){monsterPoolError.text=e.Message;}});
        }
        public void Bind(PrototypeBattle owner)
        {
            battle=owner;Build();BindItemAnimation();BindAudio();BindMonsterPoolDialog();Wire(addAffix,()=>Try(()=>{Read();MakeAffixesEditable();var effects=new List<PrototypeEffectDefinition>(Current.effectsOverride);effects.Add(new PrototypeEffectDefinition{id=Guid.NewGuid().ToString("N"),kind=PrototypeEffectKind.Damage,amount=10,criticalMultiplier=2});Current.effectsOverride=effects.ToArray();Fill();Page(2);}));Wire(resetAffixes,()=>Try(()=>{Read();Current.overridesEffects=false;Current.effectsOverride=null;foreach(var key in new[]{"poison","burn","shield","heal","regeneration"})typeof(PrototypeBroadcastSlot).GetField(key).SetValue(Current,-1f);Fill();Page(2);}));Wire(galleryClose,()=>gallery.SetActive(false));Wire(addAbsorbEquipment,()=>Try(()=>{Read();OpenGallery(GalleryTarget.AbsorbEquipment);}));Wire(addAbsorbPotion,()=>Try(()=>{Read();OpenGallery(GalleryTarget.AbsorbPotion);}));if(PrototypeBroadcastSession.Draft!=null)config=PrototypeBroadcastSession.Draft.Copy();visuals.Clear();
            foreach(var id in new[]{"player_warrior","player_mage","player_engineer"})if(PrototypeUiSkin.SpriteFor("Art/"+id)!=null)visuals.Add(id);
            foreach(var t in Resources.LoadAll<Texture2D>("Art"))if(t.name.StartsWith("monster_")&&!visuals.Contains(t.name))visuals.Add(t.name);
            level.ClearOptions();level.AddOptions(new List<string>{"一级","二级","三级","四级"});weaponKind.ClearOptions();weaponKind.AddOptions(new List<string>{"沿用道具","近战","远程"});
            Wire(entry,()=>{slotSelected=false;ShowPanel(true);Fill();RefreshPresets();RefreshCharacters();});Wire(back,()=>Try(()=>{Read();PrototypeBroadcastSession.Draft=config.Copy();ShowPanel(false);}));
            Wire(leftTab,()=>SelectSide(0));Wire(rightTab,()=>SelectSide(1));Wire(portraitButton,()=>Try(()=>{Read();OpenGallery(GalleryTarget.Portrait);}));
            Wire(save,()=>Try(()=>{Read();PrototypeBroadcastStore.Save(config);RefreshPresets();message.text="已保存对战预设："+config.title;}));Wire(saveCharacter,()=>Try(()=>{Read();PrototypeBroadcastStore.SaveCharacter(Side);RefreshCharacters();message.text="已保存角色预设："+Side.name;}));Wire(start,()=>Try(()=>{Read();battle.BeginBroadcast(config);}));
            Wire(chooseItem,()=>Try(()=>{Read();OpenGallery(GalleryTarget.Item);}));Wire(legacyButton,()=>Try(()=>{Read();OpenGallery(GalleryTarget.Legacy);}));
            for(int i=0;i<8;i++){int index=i;Wire(slotButtons[i],()=>Try(()=>{Read();bool reopen=slotSelected&&slotIndex==index;slotIndex=index;slotSelected=true;Fill();if(reopen)OpenGallery(GalleryTarget.Item);}));}
            for(int i=0;i<propertyTabs.Length;i++){int index=i;Wire(propertyTabs[i],()=>Try(()=>{Read();Page(index);}));}
            foreach(var choice in choices){var key=choice.key;Wire(choice.button,()=>Try(()=>{Read();OpenGallery(key=="projectileVisual"?GalleryTarget.Projectile:key=="hitVisual"?GalleryTarget.Hit:key=="attackShape"?GalleryTarget.Shape:GalleryTarget.Release);}));}
            preset.onValueChanged.RemoveAllListeners();preset.onValueChanged.AddListener(i=>{if(filling||i==0)return;Try(()=>{config=i<=builtinMatches.Length?PrototypeCharacterCatalog.Match(builtinMatches[i-1].id):PrototypeBroadcastStore.Load(savedMatchNames[i-builtinMatches.Length-1]);sideIndex=slotIndex=0;slotSelected=false;Fill();message.text="已载入对战："+config.title;});});
            characterPreset.onValueChanged.RemoveAllListeners();characterPreset.onValueChanged.AddListener(i=>{if(filling||i==0)return;Try(()=>{Read();var role=i<=builtinCharacters.Length?PrototypeCharacterCatalog.Find(builtinCharacters[i-1].id).character:PrototypeBroadcastStore.LoadCharacter(savedCharacterNames[i-builtinCharacters.Length-1]);if(sideIndex==0)config.left=role;else config.right=role;slotIndex=0;slotSelected=false;Fill();message.text="已载入角色："+role.name;});});
            level.onValueChanged.RemoveAllListeners();level.onValueChanged.AddListener(_=>{if(!filling)Try(()=>{Read();RefreshGrid();RefreshDefaultReferences();RefreshRuntimePreview();});});weaponKind.onValueChanged.RemoveAllListeners();weaponKind.onValueChanged.AddListener(_=>{if(!filling)Try(()=>{Read();RefreshDefaultReferences();RefreshRuntimePreview();});});
            gallerySearch.onValueChanged.RemoveAllListeners();gallerySearch.onValueChanged.AddListener(_=>RenderGallery());
            foreach(var n in numeric){n.input.onEndEdit.RemoveAllListeners();n.input.onEndEdit.AddListener(_=>{if(!filling)Try(()=>{Read();RefreshGrid();RefreshDefaultReferences();RefreshRuntimePreview();});});}
            RefreshEffectLabels();if(PrototypeBroadcastSession.OpenEditor){PrototypeBroadcastSession.OpenEditor=false;ShowPanel(true);}Fill();RefreshPresets();RefreshCharacters();
        }
        private PrototypeBroadcastSide Side=>sideIndex==0?config.left:config.right;
        private PrototypeBroadcastSlot Current=>Side.slots[slotIndex];
        private PrototypeItemDefinition Definition=>PrototypeItemRepository.Find(Current.itemId);
        private void RefreshDefaultReferences(){foreach(var n in numeric){var text=n.input.placeholder as Text;if(text!=null){text.text=F(PrototypeBroadcastDefaults.Value(Current,Definition,n.key))+"（默认）";text.color=new Color(.65f,.75f,.8f);}}}
        private void AudioPick(int page,string key,string caption,float y){var parent=propertyPages[page].transform;var dropdown=Drop(parent,caption,10,y,540);var button=ButtonAt(parent,"试听",570,y+34,140,43);audioChoices.Add(new AudioChoice {key=key,dropdown=dropdown,preview=button});}
        private void BindAudio(){if(audioPreview==null){var go=new GameObject("SoundPreview");go.transform.SetParent(panel.transform,false);audioPreview=go.AddComponent<PrototypeAudioPlayback>();}
            foreach(var choice in audioChoices){var c=choice;c.ids=new List<string>{"",PrototypeAudioLibrary.None};foreach(var e in PrototypeAudioLibrary.All)c.ids.Add(e.id);
                c.dropdown.onValueChanged.RemoveAllListeners();c.dropdown.onValueChanged.AddListener(i=>{if(filling)return;Try(()=>{Read();typeof(PrototypeBroadcastSlot).GetField(c.key).SetValue(Current,c.ids[i]);RefreshAudio();});});
                Wire(c.preview,()=>Try(()=>{Read();string id=(string)typeof(PrototypeBroadcastSlot).GetField(c.key).GetValue(Current);var d=Definition;if(string.IsNullOrEmpty(id))id=c.key=="releaseSoundId"?PrototypeAudioLibrary.ResolveRelease(d):PrototypeAudioLibrary.ResolveHit(d);float volume=c.key=="releaseSoundId"?Current.releaseVolume*(d?.releaseVolume??1):Current.hitVolume*(d?.hitVolume??1);audioPreview.Play(id,volume,true);}));}}
        private void RefreshAudio(){bool previous=filling;filling=true;foreach(var c in audioChoices){var labels=new List<string>{"默认 · "+PrototypeAudioLibrary.Name(c.key=="releaseSoundId"?PrototypeAudioLibrary.ResolveRelease(Definition):PrototypeAudioLibrary.ResolveHit(Definition)),"静音"};for(int i=2;i<c.ids.Count;i++)labels.Add(PrototypeAudioLibrary.Name(c.ids[i]));c.dropdown.ClearOptions();c.dropdown.AddOptions(labels);int index=c.ids.IndexOf((string)typeof(PrototypeBroadcastSlot).GetField(c.key).GetValue(Current)??"");c.dropdown.SetValueWithoutNotify(Mathf.Max(0,index));}filling=previous;}
        private void SelectSide(int i){Try(()=>{Read();sideIndex=i;slotIndex=0;slotSelected=false;Fill();});}
        private void Try(Action action){try{action();}catch(Exception e){message.text="<color=#FF987A>"+e.Message+"</color>";}}
        private static float Number(InputField field){if(!float.TryParse(field.text,NumberStyles.Float,CultureInfo.InvariantCulture,out var value)||float.IsNaN(value)||float.IsInfinity(value))throw new FormatException("请输入有效数值："+field.name);return value;}
        private void Read()
        {
            var candidate=config.Copy();var side=sideIndex==0?candidate.left:candidate.right;candidate.title=title.text;side.name=sideName.text;side.health=Number(health);side.speed=Number(speed);side.shield=Number(shield);side.damageBonus=Number(bonus);side.damageReduction=Number(reduction);
            var slot=side.slots[slotIndex];slot.level=Definition==null?level.value+1:Definition.ResolveLevel(level.value+1);slot.weaponKind=weaponKind.value-1;
            foreach(var n in numeric){var field=typeof(PrototypeBroadcastSlot).GetField(n.key);float value=string.IsNullOrWhiteSpace(n.input.text)&&(n.key=="growth"||n.key=="cooldownMultiplier"||n.key=="effectMultiplier"||n.key=="releaseVolume"||n.key=="hitVolume")?PrototypeBroadcastDefaults.Value(Current,Definition,n.key):OverrideNumber(n.input);if(field.FieldType==typeof(int)){if(value!=Mathf.Floor(value))throw new FormatException(n.input.name+"需要整数");field.SetValue(slot,(int)value);}else field.SetValue(slot,value);}
            ReadItemAnimation(slot);candidate.Validate();config=candidate;
        }
        private void Fill()
        {
            filling=true;title.text=config.title;sideName.text=Side.name;health.text=F(Side.health);speed.text=F(Side.speed);shield.text=F(Side.shield);bonus.text=F(Side.damageBonus);reduction.text=F(Side.damageReduction);
            preview.sprite=PrototypeUiSkin.SpriteFor("Art/"+Side.visualId);preview.color=Color.white;portraitName.text=VisualName(Side.visualId)+"  ·  点击更换";
            if(Definition!=null)Current.level=Definition.ResolveLevel(Current.level);level.interactable=Definition==null||Definition.fixedLevel==0;level.value=Current.level-1;weaponKind.value=Current.weaponKind+1;
            foreach(var n in numeric)n.input.text=Optional(Convert.ToSingle(typeof(PrototypeBroadcastSlot).GetField(n.key).GetValue(Current)));
            leftTab.GetComponent<Image>().color=sideIndex==0?new Color(.16f,.45f,.55f):new Color(.07f,.16f,.23f);rightTab.GetComponent<Image>().color=sideIndex==1?new Color(.16f,.45f,.55f):new Color(.07f,.16f,.23f);
            selectedTitle.text=Definition==null?"装备属性":Definition.displayName+" · "+Definition.RarityText;selectedIcon.sprite=Definition==null?null:PrototypeUiSkin.SpriteFor("Icons/"+Definition.iconId);selectedIcon.enabled=selectedIcon.sprite!=null;
            summary.text=Definition==null?"选择装备后，设置它的攻击类型和表现。":"刺击向前击退，劈砍带侧向冲量。\n图像大小独立于攻击距离；弹速决定飞行时间。\n释放表现与伤害分别配置。";
            FillItemAnimation();RefreshChoices();RefreshAudio();RefreshGrid();RefreshDefaultReferences();RefreshRuntimePreview();RefreshAffixes();Page(pageIndex);filling=false;
        }
        private void RefreshRuntimePreview()
        {
            foreach(Transform child in absorptionList){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            bool gear=Current.itemId=="devourer",injector=Current.itemId=="super_injector";
            addAbsorbEquipment.gameObject.SetActive(gear);addAbsorbPotion.gameObject.SetActive(gear||injector);
            if(Definition==null){runtimePreview.text="选择装备后查看实际属性。";return;}
            int ownedStart=PrototypeBroadcastSession.Owned.Count;
            try{var item=PrototypeBroadcastSession.CreateItem(Current,-999);float damage=item.definition.weapon==null?0:item.definition.weapon.damage*item.LevelMultiplier+item.WeaponGrowth;
                for(int i=0;i<item.definition.effects.Length;i++)if(item.definition.effects[i].kind==PrototypeEffectKind.Damage)damage+=item.EffectValue(i);
                runtimePreview.text=$"开战时预览 · Lv.{item.level} · 叠层 {Current.growth:0.##} 次\n本体直伤 {damage*(1+Side.damageBonus):0.##}（含选手增伤） · 冷却 {item.BaseCooldown:0.###} 秒\n装备生命 +{item.definition.healthBonus*item.LevelMultiplier+item.inheritedHealth:0.##} · 速度 +{item.definition.speedBonus*item.LevelMultiplier+item.inheritedSpeed:0.##}";
                for(int i=0;i<item.definition.effects.Length;i++)runtimePreview.text+="\n"+PrototypeEffectDisplay.Summary(item,i);
                runtimePreview.rectTransform.sizeDelta=new Vector2(690,Mathf.Max(195,80+item.definition.effects.Length*30));
            }
            finally{for(int i=PrototypeBroadcastSession.Owned.Count-1;i>=ownedStart;i--){Destroy(PrototypeBroadcastSession.Owned[i]);PrototypeBroadcastSession.Owned.RemoveAt(i);}}
            float absorbY=110+runtimePreview.rectTransform.sizeDelta.y;addAbsorbEquipment.GetComponent<RectTransform>().anchoredPosition=new Vector2(10,-absorbY);addAbsorbPotion.GetComponent<RectTransform>().anchoredPosition=new Vector2(380,-absorbY);absorptionList.anchoredPosition=new Vector2(10,-absorbY-60);
            int row=0;if(Current.absorbed!=null)foreach(var record in Current.absorbed){var a=record;var d=PrototypeItemRepository.Find(a.itemId);int sourceLevel=d.ResolveLevel(a.level);float y=row++*78;
                Label(absorptionList,d.displayName+" · Lv."+sourceLevel+" ×"+a.count+" · 吸收 "+(PrototypeEffectMath.Absorption(Current.level)*100).ToString("0")+"%",0,y,505,33,19);
                var minus=ButtonAt(absorptionList,"−",510,y,50,33);Wire(minus,()=>Try(()=>{Read();var match=Current.absorbed.Find(x=>x.itemId==a.itemId&&x.level==a.level);if(--match.count<=0)Current.absorbed.Remove(match);Fill();}));
                var plus=ButtonAt(absorptionList,"+",570,y,50,33);Wire(plus,()=>Try(()=>{Read();Current.absorbed.Find(x=>x.itemId==a.itemId&&x.level==a.level).count++;Fill();}));
                var remove=ButtonAt(absorptionList,"×",625,y,65,33);Wire(remove,()=>Try(()=>{Read();Current.absorbed.RemoveAll(x=>x.itemId==a.itemId&&x.level==a.level);Fill();}));
                string desc=d.kind==PrototypeItemKind.Injection?PrototypeItemDefinition.StatName(d.injectionStat)+" +"+PrototypeItemDefinition.FormatAmount(d.injectionStat,d.injectionAmount*a.count*PrototypeEffectMath.Absorption(Current.level)):"输出权重 ×"+(Mathf.Pow(2,sourceLevel-1)*a.count*PrototypeEffectMath.Absorption(Current.level)).ToString("0.###")+ (d.weapon==null?"":" · 直伤 "+(d.weapon.damage*Mathf.Pow(2,sourceLevel-1)*a.count*PrototypeEffectMath.Absorption(Current.level)*(1+Side.damageBonus)).ToString("0.##"));Label(absorptionList,desc,0,y+34,690,31,17);}
            if(row==0)Label(absorptionList,gear||injector?"尚未吸收装备或药水":"此装备没有吸收机制",0,0,690,35,19);
            absorptionList.sizeDelta=new Vector2(690,Mathf.Max(80,row*78));((RectTransform)propertyPages[3].transform).sizeDelta=new Vector2(720,Mathf.Max(490,absorbY+70+row*78));
        }
        private void MakeAffixesEditable() {
            if(Current.overridesEffects)return;
            int ownedStart=PrototypeBroadcastSession.Owned.Count;
            try{var item=PrototypeBroadcastSession.CreateItem(Current,-998);Current.effectsOverride=PrototypeEffectDisplay.Copy(item.definition.effects);Current.overridesEffects=true;Current.effectMultiplier=1;
                foreach(var key in new[]{"poison","burn","shield","heal","regeneration"})typeof(PrototypeBroadcastSlot).GetField(key).SetValue(Current,-1f);
                foreach(var n in numeric)if(n.key=="effectMultiplier")n.input.SetTextWithoutNotify("1");else if(n.key=="poison"||n.key=="burn"||n.key=="shield"||n.key=="heal"||n.key=="regeneration")n.input.SetTextWithoutNotify("");
            }finally{for(int i=PrototypeBroadcastSession.Owned.Count-1;i>=ownedStart;i--){Destroy(PrototypeBroadcastSession.Owned[i]);PrototypeBroadcastSession.Owned.RemoveAt(i);}}
        }
        private void RefreshAffixes() {
            foreach(Transform child in affixList){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            addAffix.interactable=resetAffixes.interactable=Definition!=null;
            if(Definition==null){affixList.sizeDelta=new Vector2(690,50);return;}
            var effects=Current.overridesEffects?Current.effectsOverride:Definition.effects;
            Label(affixList,Current.overridesEffects?"自定义词条 · 数值按一级填写，特殊百分比效果固定":"继承默认词条 · 修改任一参数后转为自定义",0,0,690,35,19);
            int count=effects==null?0:effects.Length;
            for(int i=0;i<count;i++){
                int index=i;var e=effects[i];float y=45+i*640;
                var kind=Drop(affixList,"词条 "+(i+1),0,y,560);kind.AddOptions(new List<string>(PrototypeEffectDisplay.Names));kind.SetValueWithoutNotify((int)e.kind);
                var remove=ButtonAt(affixList,"删除",575,y+34,115,43);Wire(remove,()=>Try(()=>{Read();MakeAffixesEditable();var list=new List<PrototypeEffectDefinition>(Current.effectsOverride);list.RemoveAt(index);Current.effectsOverride=list.ToArray();Fill();Page(2);}));
                kind.onValueChanged.AddListener(v=>{if(filling)return;Try(()=>{Read();MakeAffixesEditable();Current.effectsOverride[index].kind=(PrototypeEffectKind)v;Current.effectsOverride[index].id=Guid.NewGuid().ToString("N");Fill();Page(2);});});
                AffixField(index,"amount","数值 · "+((e.kind==PrototypeEffectKind.MasterCooldown||e.kind==PrototypeEffectKind.TriggerApprentices)?"0 沿用等级策略":PrototypeEffectDisplay.Unit(e.kind)),e.amount,0,y+85);
                AffixField(index,"growthPerActivation","每次触发永久成长",e.growthPerActivation,350,y+85);
                AffixField(index,"criticalChance","暴击概率（0～1）",e.criticalChance,0,y+170);
                AffixField(index,"criticalMultiplier","暴击倍率",e.criticalMultiplier,350,y+170);
                var tag=Field(affixList,"匹配标签（例如 剑 / 学徒）",0,y+255,e.matchTag??"",690);
                var trigger=Drop(affixList,"触发条件",0,y+340,330);trigger.AddOptions(new List<string>(PrototypeEffectDisplay.TriggerNames));trigger.SetValueWithoutNotify((int)e.trigger);
                trigger.onValueChanged.AddListener(v=>{if(filling)return;Try(()=>{Read();MakeAffixesEditable();UpdateAffix(index,"trigger",(PrototypeEffectTrigger)v);RefreshRuntimePreview();});});
                AffixField(index,"triggerCooldown","触发间隔秒（释放用装备冷却）",e.triggerCooldown,350,y+340);
                AffixLevels(index,"levelAmounts","Lv.1–4绝对数值（逗号分隔，空沿倍率）",e.levelAmounts,y+425);
                AffixLevels(index,"levelGrowthAmounts","Lv.1–4每次触发成长（逗号分隔）",e.levelGrowthAmounts,y+510);
                tag.onEndEdit.AddListener(v=>{if(filling)return;Try(()=>{Read();MakeAffixesEditable();UpdateAffix(index,"matchTag",v);RefreshRuntimePreview();});});
            }
            // Each row includes matching tags; leave space before the following row.
            affixList.sizeDelta=new Vector2(690,Mathf.Max(60,45+count*640));
            ((RectTransform)propertyPages[2].transform).sizeDelta=new Vector2(720,745+affixList.sizeDelta.y);
        }
        private void AffixLevels(int index,string key,string caption,float[] values,float y)
        {
            var field=Field(affixList,caption,0,y,values==null?"":string.Join(",",Array.ConvertAll(values,F)),690);
            field.onEndEdit.AddListener(v=>{if(filling)return;Try(()=>{
                float[] parsed=null;if(!string.IsNullOrWhiteSpace(v))
                {
                    var parts=v.Replace('，',',').Split(',');if(parts.Length!=4)throw new FormatException("需要填写四个等级数值");
                    parsed=new float[4];for(int i=0;i<4;i++)parsed[i]=float.Parse(parts[i],System.Globalization.CultureInfo.InvariantCulture);
                }
                Read();MakeAffixesEditable();UpdateAffix(index,key,parsed);RefreshRuntimePreview();
            });});
        }
        private void AffixField(int index,string key,string caption,float value,float x,float y) {
            var field=Field(affixList,caption,x,y,F(value),330);
            field.onEndEdit.AddListener(_=>{if(filling)return;Try(()=>{float next=Number(field);Read();MakeAffixesEditable();UpdateAffix(index,key,next);RefreshRuntimePreview();RefreshDefaultReferences();});});
        }
        private void UpdateAffix(int index,string key,object value) {
            var candidate=config.Copy();var side=sideIndex==0?candidate.left:candidate.right;typeof(PrototypeEffectDefinition).GetField(key).SetValue(side.slots[slotIndex].effectsOverride[index],value);candidate.Validate();config=candidate;
        }
        private void RefreshGrid()
        {
            for(int i=0;i<8;i++)
            {
                var slot=Side.slots[i];var d=PrototypeItemRepository.Find(slot.itemId);slotIcons[i].sprite=d==null?null:PrototypeUiSkin.SpriteFor("Icons/"+d.iconId);slotIcons[i].enabled=slotIcons[i].sprite!=null;
                slotNames[i].text=d==null?"选择装备":d.displayName+" · Lv."+slot.level;var tint=d==null?new Color(.15f,.27f,.33f):PrototypeItemRepository.RarityColor(d.rarity);
                slotNames[i].color=d==null?new Color(.65f,.75f,.8f):tint;slotButtons[i].GetComponent<Image>().color=Color.Lerp(new Color(.04f,.10f,.15f),slotSelected&&i==slotIndex?new Color(.2f,.85f,.9f):tint,slotSelected&&i==slotIndex?.38f:.15f);
            }
            bool legacy=Side.slots.Length==10 && (!string.IsNullOrEmpty(Side.slots[8].itemId)||!string.IsNullOrEmpty(Side.slots[9].itemId));legacyButton.gameObject.SetActive(legacy);
        }
        private void Page(int i){pageIndex=i;for(int j=0;j<propertyPages.Length;j++){propertyPages[j].SetActive(j==i);propertyTabs[j].GetComponent<Image>().color=j==i?new Color(.13f,.4f,.48f):new Color(.07f,.17f,.24f);}propertyScroll.content=(RectTransform)propertyPages[i].transform;propertyScroll.verticalNormalizedPosition=1;}
        private void OpenGallery(GalleryTarget kind)
        {
            target=kind;options.Clear();
            if(kind==GalleryTarget.AbsorbEquipment||kind==GalleryTarget.AbsorbPotion){galleryTitle.text=kind==GalleryTarget.AbsorbEquipment?"添加吸收装备（一级）":"添加吸收药水";foreach(var d in PrototypeItemRepository.All)if(d.kind==(kind==GalleryTarget.AbsorbEquipment?PrototypeItemKind.Equipment:PrototypeItemKind.Injection)&&d.itemId!="devourer")options.Add(new Option{id=d.itemId,name=d.displayName,sprite=PrototypeUiSkin.SpriteFor("Icons/"+d.iconId)});}gallerySearch.SetTextWithoutNotify("");
            if(kind==GalleryTarget.Portrait){galleryTitle.text="选择角色图像";foreach(var id in visuals)options.Add(new Option {id=id,name=VisualName(id),sprite=PrototypeUiSkin.SpriteFor("Art/"+id)});}
            if(kind==GalleryTarget.Item){galleryTitle.text="选择装备";options.Add(new Option {id="",name="卸下装备",shape=PrototypePreviewShape.None,color=new Color(.65f,.75f,.8f)});foreach(var d in PrototypeItemRepository.All)if(d.kind==PrototypeItemKind.Equipment)options.Add(new Option{id=d.itemId,name=d.displayName+" · "+d.RarityText,sprite=PrototypeUiSkin.SpriteFor("Icons/"+d.iconId),color=PrototypeItemRepository.RarityColor(d.rarity)});}
            if(kind==GalleryTarget.Projectile){galleryTitle.text="选择弹体图像";options.Add(StyleOption(kind,-1));for(int i=0;i<5;i++)options.Add(StyleOption(kind,i));foreach(var d in PrototypeItemRepository.All)if(d.kind==PrototypeItemKind.Equipment)options.Add(new Option {id=d.iconId,value=-2,name=d.displayName+"图像",sprite=PrototypeUiSkin.SpriteFor("Icons/"+d.iconId)});}
            if(kind==GalleryTarget.Hit||kind==GalleryTarget.Shape||kind==GalleryTarget.Release){galleryTitle.text=kind==GalleryTarget.Hit?"选择命中特效（动态预览）":kind==GalleryTarget.Shape?"选择近战动作":"选择释放表现（动态预览）";options.Add(StyleOption(kind,-1));int count=kind==GalleryTarget.Hit?6:kind==GalleryTarget.Shape?3:4;for(int i=0;i<count;i++)options.Add(StyleOption(kind,i));}
            if(kind==GalleryTarget.Legacy){galleryTitle.text="旧预设扩展装备";for(int i=8;i<Side.slots.Length;i++){var d=PrototypeItemRepository.Find(Side.slots[i].itemId);options.Add(new Option {value=i,name=d==null?"空扩展格":d.displayName,sprite=d==null?null:PrototypeUiSkin.SpriteFor("Icons/"+d.iconId)});}}
            gallery.SetActive(true);gallery.transform.SetAsLastSibling();RenderGallery();
        }
        private Option StyleOption(GalleryTarget kind,int value)
        {
            int actual=value;var weapon=Definition?.weapon;
            if(value<0)actual=kind==GalleryTarget.Projectile?(int)(weapon?.projectileVisual??PrototypeProjectileVisual.Orb):kind==GalleryTarget.Hit?(int)(weapon?.hitVisual??Definition?.hitVisual??PrototypeHitVisual.None):kind==GalleryTarget.Shape?(int)(weapon?.attackShape??PrototypeAttackShape.Sweep):(int)(weapon?.visualStyle??PrototypeAttackVisual.Single);
            var o=new Option {value=value,color=new Color(1,.83f,.35f)};
            if(kind==GalleryTarget.Projectile){o.name=new[]{"圆弹","箭矢","飞剑","能量弹","炮弹"}[actual];string icon=actual==1?"spear":actual==2?"greatsword":actual==3?"crossbow":null;o.sprite=icon==null?null:PrototypeUiSkin.SpriteFor("Icons/"+icon);o.shape=PrototypePreviewShape.Circle;}
            if(kind==GalleryTarget.Shape){o.name=new[]{"穿刺","劈砍","重击"}[actual];o.sprite=PrototypeUiSkin.SpriteFor("Icons/"+new[]{"spear","greatsword","hammer"}[actual]);}
            if(kind==GalleryTarget.Hit){o.name=new[]{"无特效","火花","毒液","燃烧","冰晶","冲击环"}[actual];o.shape=actual==0?PrototypePreviewShape.None:actual==5?PrototypePreviewShape.Wave:PrototypePreviewShape.Star;o.color=actual==2?new Color(.3f,1,.25f):actual==3?new Color(1,.3f,.08f):actual==4?new Color(.3f,.9f,1):new Color(1,.85f,.3f);}
            if(kind==GalleryTarget.Release){o.name=new[]{"单次挥击/发射","剑阵齐射","光束","冲击环"}[actual];o.shape=actual==0?PrototypePreviewShape.Star:actual==1?PrototypePreviewShape.Volley:actual==2?PrototypePreviewShape.Beam:PrototypePreviewShape.Wave;o.color=actual==2?new Color(.3f,.9f,1):new Color(1,.85f,.3f);}
            if(value<0)o.name="沿用 · "+o.name;return o;
        }
        private void RefreshChoices()
        {
            foreach(var c in choices){var kind=c.key=="projectileVisual"?GalleryTarget.Projectile:c.key=="hitVisual"?GalleryTarget.Hit:c.key=="attackShape"?GalleryTarget.Shape:GalleryTarget.Release;int value=(int)typeof(PrototypeBroadcastSlot).GetField(c.key).GetValue(Current);var o=StyleOption(kind,value);
                if(kind==GalleryTarget.Projectile&&!string.IsNullOrEmpty(Current.projectileIconId)){o.sprite=PrototypeUiSkin.SpriteFor("Icons/"+Current.projectileIconId);o.name="自选弹体";}
                c.text.text=o.name;c.image.sprite=o.sprite;c.image.enabled=o.sprite!=null;c.effect.gameObject.SetActive(o.sprite==null);c.effect.shape=o.shape;c.effect.color=o.color;c.effect.SetVerticesDirty();}
        }
        private void RenderGallery()
        {
            foreach(Transform child in galleryContent){child.gameObject.SetActive(false);Destroy(child.gameObject);}int index=0;string filter=gallerySearch.text.Trim();
            foreach(var o in options)
            {
                if(filter.Length>0 && o.name.IndexOf(filter,StringComparison.OrdinalIgnoreCase)<0)continue;var choice=o;float x=(index%9)*196,y=(index/9)*173;
                var button=ButtonAt(galleryContent,"",x+5,y+7,185,159);button.GetComponent<Image>().color=new Color(.07f,.17f,.24f);Wire(button,()=>Try(()=>ApplyOption(choice)));
                if(o.sprite!=null){var image=Box(button.transform,"Thumbnail",37,5,111,111,Color.white);image.sprite=o.sprite;image.preserveAspect=true;image.raycastTarget=false;}
                else{var effect=EffectBox(button.transform,37,5,111,111);effect.shape=o.shape;effect.color=o.color;}
                var text=button.GetComponentInChildren<Text>();text.text=o.name;text.color=o.color;text.fontSize=19;text.resizeTextForBestFit=true;text.resizeTextMinSize=15;text.resizeTextMaxSize=19;text.rectTransform.anchoredPosition=new Vector2(7,-115);text.rectTransform.sizeDelta=new Vector2(171,40);index++;
            }
            galleryContent.sizeDelta=new Vector2(1785,Mathf.Max(798,Mathf.Ceil(index/9f)*173));
        }
        private void ApplyOption(Option o)
        {
            if(target==GalleryTarget.AbsorbEquipment||target==GalleryTarget.AbsorbPotion){if(Current.absorbed==null)Current.absorbed=new List<PrototypeBroadcastAbsorption>();int sourceLevel=PrototypeItemRepository.Find(o.id).ResolveLevel(1);var found=Current.absorbed.Find(a=>a.itemId==o.id&&a.level==sourceLevel);if(found==null)Current.absorbed.Add(new PrototypeBroadcastAbsorption {itemId=o.id,level=sourceLevel});else found.count++;}
            else if(target==GalleryTarget.Portrait)Side.visualId=o.id;
            else if(target==GalleryTarget.Item){Side.slots[slotIndex]=new PrototypeBroadcastSlot {itemId=o.id};}
            else if(target==GalleryTarget.Projectile){if(o.value==-2){Current.projectileIconId=o.id;Current.projectileVisual=0;}else{Current.projectileVisual=o.value;Current.projectileIconId="";}}
            else if(target==GalleryTarget.Hit)Current.hitVisual=o.value;
            else if(target==GalleryTarget.Shape)Current.attackShape=o.value;
            else if(target==GalleryTarget.Release)Current.visualStyle=o.value;
            else{slotIndex=o.value;Fill();OpenGallery(GalleryTarget.Item);return;}
            gallery.SetActive(false);Fill();
        }
        private static PrototypeEffectPreviewGraphic EffectBox(Transform parent,float x,float y,float w,float h)
        {var image=Box(parent,"EffectPreview",x,y,w,h,Color.clear);var go=image.gameObject;DestroyImmediate(image);var graphic=go.AddComponent<PrototypeEffectPreviewGraphic>();graphic.raycastTarget=false;return graphic;}
        private static string VisualName(string id)
        {switch(id){case "player_warrior":return "战士";case "player_mage":return "法师";case "player_engineer":return "工程师";case "monster_reaper":return "死神";case "monster_slime":return "史莱姆";case "monster_normal":return "普通怪物";case "monster_elite":return "精英怪物";case "monster_boss":return "首领怪物";case "monster_frost":return "寒霜怪物";case "monster_furnace":return "熔炉怪物";case "monster_thorn":return "荆棘怪物";case "monster_saw":return "锯齿野猪";case "monster_spark":return "电光飞龙";case "monster_spore":return "孢子守卫";case "monster_storm":return "风暴龙";default:return "怪物外观 "+id.Replace("monster_","");}}
        private void RefreshPresets(){filling=true;preset.ClearOptions();var list=new List<string>{"选择对战预设"};builtinMatches=PrototypeCharacterCatalog.Matches;savedMatchNames=PrototypeBroadcastStore.Names();foreach(var m in builtinMatches)list.Add("【首季】"+m.title);foreach(var name in savedMatchNames)list.Add("【我的】"+name);preset.AddOptions(list);preset.SetValueWithoutNotify(0);filling=false;}
        private void RefreshCharacters(){filling=true;characterPreset.ClearOptions();var list=new List<string>{"选择角色预设"};builtinCharacters=PrototypeCharacterCatalog.Characters;savedCharacterNames=PrototypeBroadcastStore.CharacterNames();foreach(var p in builtinCharacters)list.Add("【内置·"+p.tier+"】"+p.character.name);foreach(var name in savedCharacterNames)list.Add("【我的】"+name);characterPreset.AddOptions(list);characterPreset.SetValueWithoutNotify(0);filling=false;}
        private void ShowPanel(bool visible)
        {
            if(audioPreview!=null)audioPreview.Stop();
            if(visible){hiddenSiblings.Clear();foreach(Transform child in transform){if(child.gameObject==panel)continue;hiddenSiblings[child.gameObject]=child.gameObject.activeSelf;child.gameObject.SetActive(false);}panel.SetActive(true);}
            else{gallery.SetActive(false);monsterPoolDialog.SetActive(false);panel.SetActive(false);foreach(var pair in hiddenSiblings)if(pair.Key!=null)pair.Key.SetActive(pair.Value);hiddenSiblings.Clear();}
        }
        private static string Optional(float v)=>v<0?"":F(v);
        private static float OverrideNumber(InputField field)=>string.IsNullOrWhiteSpace(field.text)?-1:Number(field);
        private static string F(float v)=>v.ToString("0.###",CultureInfo.InvariantCulture);
        private static void Wire(Button b,Action a){b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>a());}
        private static Image Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);var image=go.GetComponent<Image>();image.color=color;return image;
        }
        private static Text Label(Transform parent,string value,float x,float y,float w,float h,int size=22)
        {
            var image=Box(parent,"Label",x,y,w,h,Color.clear);var go=image.gameObject;DestroyImmediate(image);var t=go.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=new Color(1,.96f,.84f);t.text=value;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;
        }
        private static Button ButtonAt(Transform p,string caption,float x,float y,float w,float h){var image=Box(p,caption,x,y,w,h,new Color(.09f,.25f,.34f));var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;Label(image.transform,caption,16,0,w-32,h,24).alignment=TextAnchor.MiddleCenter;return b;}
        private static InputField Field(Transform p,string caption,float x,float y,string value,float w)
        {
            Label(p,caption,x,y,w,30,21);var image=Box(p,caption,x,y+34,w,43,new Color(.02f,.055f,.085f));var text=Label(image.transform,value,12,0,w-24,43,23);var f=image.gameObject.AddComponent<InputField>();f.textComponent=text;f.targetGraphic=image;f.text=value=="-1"?"":value;var placeholder=Label(image.transform,value=="-1"?"默认":"",12,0,w-24,43,21);placeholder.color=new Color(.4f,.55f,.63f);f.placeholder=placeholder;return f;
        }
        private static Dropdown Drop(Transform p,string caption,float x,float y,float w)
        {
            Label(p,caption,x,y,w,30,21);var image=Box(p,caption,x,y+34,w,43,new Color(.09f,.23f,.30f));var drop=image.gameObject.AddComponent<Dropdown>();drop.targetGraphic=image;
            drop.captionText=Label(image.transform,"",12,0,w-42,43,21);Label(image.transform,"▼",w-32,0,28,43,18);
            var template=Box(image.transform,"Template",0,45,w,290,new Color(.025f,.075f,.12f));var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;
            var view=Box(template.transform,"Viewport",0,0,w,290,Color.white);view.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var content=Box(view.transform,"Content",0,0,w,43,Color.clear);var row=Box(content.transform,"Item",0,0,w,43,new Color(.1f,.23f,.3f));
            var toggle=row.gameObject.AddComponent<Toggle>();toggle.targetGraphic=row;var check=Box(row.transform,"Check",4,14,10,10,new Color(.3f,.9f,.9f));toggle.graphic=check;
            drop.itemText=Label(row.transform,"",22,0,w-30,43,20);scroll.viewport=view.rectTransform;scroll.content=content.rectTransform;
            drop.template=template.rectTransform;template.gameObject.SetActive(false);return drop;
        }
    }
}
