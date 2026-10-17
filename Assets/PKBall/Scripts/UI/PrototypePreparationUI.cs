using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    public sealed partial class PrototypePreparationUI : MonoBehaviour
    {
        [SerializeField] private PrototypeBattle battle;
        private PrototypeRun run;
        [SerializeField] private Font font;
        [SerializeField] private RectTransform canvas;
        [SerializeField] private RectTransform content;
        private int selectedBag = -1;
        [SerializeField] private Text headline;
        [SerializeField] private Text details;
        [SerializeField] private Text notice;
        [SerializeField] private Button[] slotButtons = new Button[10];
        private static readonly string[] SlotNames = { "前右", "右一", "右二", "右三", "右四",
            "左四", "左三", "左二", "左一", "前左" };
        [SerializeField] private Button[] shopButtons = new Button[4];
        [SerializeField] private Text[] shopPrices = new Text[4];
        [SerializeField] private Button[] bagButtons = new Button[12];
        [SerializeField] private Button sellButton;
        [SerializeField] private Button useButton;
        [SerializeField] private Text statsText;
        [SerializeField] private RectTransform statsViewport;
        [SerializeField] private Button statsToggle;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button rerollButton;
        [SerializeField] private Button sellOreButton;
        [SerializeField] private Button expandButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private RectTransform choicePanel;
        [SerializeField] private RectTransform shopGroup;
        [SerializeField] private RectTransform bagGroup;
        [SerializeField] private RectTransform equipmentGroup;
        [SerializeField] private RectTransform statsGroup;
        [SerializeField] private RectTransform contextGroup;
        [SerializeField] private RectTransform contextPanel;
        [SerializeField] private Text contextTitle;
        [SerializeField] private Text contextDescription;
        [SerializeField] private Text eventReward;
        [SerializeField] private Image contextPortrait;
        [SerializeField] private Text opponentLoadoutTitle;
        [SerializeField] private Image[] contextItemIcons = new Image[4];
        [SerializeField] private Text[] contextItemNames = new Text[4];
        [SerializeField] private Button[] choiceButtons = new Button[3];
        [SerializeField] private Image[] offerPortraits = new Image[3];
        [SerializeField] private Image[] tierMedals = new Image[3];
        [SerializeField] private Text[] tierLabels = new Text[3];
        [SerializeField] private Image[] offerItemIcons = new Image[12];
        [SerializeField] private Text[] offerItemNames = new Text[12];
        [SerializeField] private GameObject[] offerItemTiles = new GameObject[12];
        [SerializeField] private Text choiceTitle;
        [SerializeField] private RectTransform choiceItemPanel;
        [SerializeField] private Text choiceItemDetails;
        [SerializeField] private Text selectedMonsterSummary;
        [SerializeField] private Text hoverText;
        [SerializeField] private RectTransform hoverPanel;
        private int selectedSlot = 0;
        private PrototypeHoverKind hoveredKind;
        private int hoveredIndex = -1;
        private int hoverLevel = 1;
        private bool showingStats;
        [SerializeField] private Image playerPortrait;
        private string loadedPortraitId;
        private PrototypeDayStage lastChoiceStage = (PrototypeDayStage)(-1);

        public void Initialize(PrototypeBattle owner, PrototypeRun state)
        {
            battle = owner; run = state;BindInteractionFeedback();
            if (canvas != null) { BindSavedView(); Refresh(); return; }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject root = new GameObject("PreparationCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = (RectTransform)root.transform;
            Canvas display = root.GetComponent<Canvas>();
            display.renderMode = RenderMode.ScreenSpaceOverlay;
            display.sortingOrder = 100;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            content = Panel("Preparation", canvas, new Color(0.035f, 0.055f, 0.085f, 0.985f),
                new Vector2(0, 0), new Vector2(1920, 1080));
            Image background = content.GetComponent<Image>();
            background.sprite = PrototypeUiSkin.SpriteFor("UI/menu_background");
            background.color = new Color(.30f, .39f, .51f, 1f);
            Panel("TitlePlate", content, new Color(.035f, .065f, .095f, .86f),
                new Vector2(28, -16), new Vector2(1000, 76));
            Label("Title", content, "PK BALL  /  准备阶段", 38, new Vector2(40, -25),
                new Vector2(830, 58), TextAnchor.MiddleLeft);
            Button("MainMenu", content, new Vector2(830, -25), new Vector2(190, 58),
                battle.ReturnToMenu).GetComponentInChildren<Text>().text = "返回主菜单";
            headline = Label("Economy", content, "", 23, new Vector2(1080, -18),
                new Vector2(770, 76), TextAnchor.MiddleRight);

            int groupStart = content.childCount;
            Panel("ShopPanel", content, new Color(.045f, .075f, .105f, .91f), new Vector2(840, -120), new Vector2(1050, 665));
            Label("ShopTitle", content, "商店  /  点击或拖拽购买", 30, new Vector2(870, -135),
                new Vector2(960, 50), TextAnchor.MiddleLeft);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                shopButtons[i] = Button("Shop" + i, content,
                    new Vector2(865 + (i % 2) * 500, -210 - (i / 2) * 185),
                    new Vector2(470, 150), () => { run.Buy(index); Refresh(); });
                shopPrices[i] = Label("Price", shopButtons[i].transform, "", 25,
                    new Vector2(336, -43), new Vector2(112, 64), TextAnchor.MiddleCenter);
                shopPrices[i].color = PrototypeUiSkin.Navy;
                PrototypeBagDrag shopDrag = shopButtons[i].gameObject.AddComponent<PrototypeBagDrag>();
                shopDrag.Initialize(this, index, true);
                shopButtons[i].gameObject.AddComponent<PrototypeItemHover>().Initialize(
                    this, PrototypeHoverKind.Shop, index);
            }
            rerollButton = Button("Reroll", content, new Vector2(865, -610), new Vector2(470, 64),
                () => { run.Reroll(); Refresh(); });
            rerollButton.GetComponentInChildren<Text>().text = "刷新商店  ·  2 金币";
            shopGroup = GroupChildren("ShopGroup", groupStart);

            groupStart = content.childCount;
            Panel("BagPanel", content, new Color(.045f, .075f, .105f, .94f), new Vector2(30, -820), new Vector2(1860, 230));
            Label("BagTitle", content, "背包 / 角色数值", 26,
                new Vector2(55, -825), new Vector2(440, 45), TextAnchor.MiddleLeft);
            statsToggle = Button("StatsToggle", content, new Vector2(1600, -825),
                new Vector2(260, 42), () => { showingStats = !showingStats; Refresh(); });
            sellOreButton = Button("SellOre", content, new Vector2(1335, -825),
                new Vector2(245, 42), () => { run.SellOre(); Refresh(); });
            for (int i = 0; i < bagButtons.Length; i++)
            {
                int index = i;
                bagButtons[i] = Button("Bag" + i, content,
                    new Vector2(55 + (i % 6) * 235, -872 - (i / 6) * 86),
                    new Vector2(220, 76), () => SelectBag(index));
                PrototypeBagDrag drag = bagButtons[i].gameObject.AddComponent<PrototypeBagDrag>();
                drag.Initialize(this, index);
                PrototypeItemDrop drop = bagButtons[i].gameObject.AddComponent<PrototypeItemDrop>();
                drop.Initialize(this, PrototypeDropKind.Bag, index);
                bagButtons[i].gameObject.AddComponent<PrototypeItemHover>().Initialize(
                    this, PrototypeHoverKind.Bag, index);
            }
            statsViewport = Panel("StatsViewport", content, Color.clear,
                new Vector2(65, -220), new Vector2(690, 490));
            statsViewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect statsScroll = statsViewport.gameObject.AddComponent<ScrollRect>();
            RectTransform statsContent = Panel("StatsContent", statsViewport, Color.clear,
                Vector2.zero, new Vector2(690, 1500));
            statsContent.GetComponent<Image>().raycastTarget = false;
            statsText = Label("DetailedStats", statsContent, "", 25, Vector2.zero,
                new Vector2(690, 1500), TextAnchor.UpperLeft);
            statsScroll.content = statsContent;
            statsScroll.viewport = statsViewport;
            statsScroll.horizontal = false;
            statsScroll.vertical = true;
            statsScroll.movementType = ScrollRect.MovementType.Clamped;
            details = Label("Details", content, "", 21, new Vector2(1490, -872),
                new Vector2(370, 85), TextAnchor.UpperLeft);
            sellButton = Button("Sell", content, new Vector2(1490, -965), new Vector2(200, 58),
                () => { if (run.Sell(selectedBag)) selectedBag = -1; Refresh(); });
            useButton = Button("Use", content, new Vector2(1710, -965), new Vector2(150, 58),
                () => { if (run.UseInjection(selectedBag)) selectedBag = -1; Refresh(); });
            bagGroup = GroupChildren("BagGroup", groupStart);

            groupStart = content.childCount;
            Panel("EquipmentPanel", content, new Color(.045f, .075f, .105f, .91f), new Vector2(30, -120), new Vector2(780, 665));
            Label("EquipTitle", content, "装备格位  /  前方双格始终面向敌人", 28,
                new Vector2(55, -135), new Vector2(730, 50), TextAnchor.MiddleLeft);
            for (int i = 0; i < 10; i++)
            {
                int index = i;
                GameObject sector = new GameObject("Slot" + PrototypeRun.SlotOrder[i],
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(PrototypeRingSlotGraphic), typeof(Button));
                sector.transform.SetParent(content, false);
                RectTransform sectorRect = (RectTransform)sector.transform;
                sectorRect.anchorMin = sectorRect.anchorMax = new Vector2(0, 1);
                sectorRect.pivot = new Vector2(.5f, .5f);
                sectorRect.anchoredPosition = new Vector2(420, -450);
                sectorRect.sizeDelta = new Vector2(540, 540);
                PrototypeRingSlotGraphic sectorGraphic = sector.GetComponent<PrototypeRingSlotGraphic>();
                sectorGraphic.color = SlotColor(PrototypeSlotEffect.None);
                slotButtons[i] = sector.GetComponent<Button>();
                slotButtons[i].targetGraphic = sectorGraphic;
                slotButtons[i].onClick.AddListener(() => SelectSlot(index));
                Image icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(sector.transform, false);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.enabled = false;
                Label("Text", sector.transform, "", 18, Vector2.zero,
                    new Vector2(70, 28), TextAnchor.MiddleCenter);
                PrototypeItemDrop drop = slotButtons[i].gameObject.AddComponent<PrototypeItemDrop>();
                drop.Initialize(this, PrototypeDropKind.Slot, index);
                slotButtons[i].gameObject.AddComponent<PrototypeItemHover>().Initialize(
                    this, PrototypeHoverKind.Slot, index);
            }
            RectTransform character = Panel("CharacterDrop", content,
                new Color(0.11f, 0.31f, 0.37f), new Vector2(330, -360), new Vector2(180, 180));
            character.GetComponent<Image>().raycastTarget = true;
            Texture2D portraitTexture = Resources.Load<Texture2D>("Art/" + battle.PlayerVisualId);
            if (portraitTexture != null)
            {
                GameObject portrait = new GameObject("PlayerPortrait", typeof(RectTransform), typeof(Image));
                portrait.transform.SetParent(character, false);
                RectTransform portraitRect = (RectTransform)portrait.transform;
                portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0, 1);
                portraitRect.pivot = new Vector2(0, 1);
                portraitRect.anchoredPosition = new Vector2(14, -4);
                portraitRect.sizeDelta = new Vector2(152, 152);
                Image portraitImage = portrait.GetComponent<Image>();
                playerPortrait = portraitImage;
                loadedPortraitId = battle.PlayerVisualId;
                portraitImage.sprite = IconSprite(portraitTexture);
                portraitImage.raycastTarget = false;
            }
            Label("CharacterDropLabel", character, "拖针剂到此", 20, new Vector2(10, -145),
                new Vector2(160, 32), TextAnchor.MiddleCenter);
            PrototypeItemDrop coreDrop = character.gameObject.AddComponent<PrototypeItemDrop>();
            coreDrop.Initialize(this, PrototypeDropKind.Character, -1);
            Label("EquipHint", content, "悬停道具查看详情  ·  滚轮预览升级",
                17, new Vector2(75, -714), new Vector2(690, 38), TextAnchor.MiddleCenter);
            expandButton = Button("Expand", content, new Vector2(75, -752), new Vector2(320, 48),
                () => { run.Expand(); Refresh(); });
            expandButton.GetComponentInChildren<Text>().text = "金币升级";
            claimButton = Button("ClaimRewards", content, new Vector2(420, -752), new Vector2(345, 48),
                () => { run.ClaimGenerated(); Refresh(); });
            equipmentGroup = GroupChildren("EquipmentGroup", groupStart);

            groupStart = content.childCount;
            Panel("StatsPanel", content, new Color(.045f, .075f, .105f, .94f),
                new Vector2(30, -120), new Vector2(780, 665));
            Label("StatsTitle", content, "角色数值  /  来源明细可滚动", 30,
                new Vector2(60, -140), new Vector2(710, 55), TextAnchor.MiddleLeft);
            statsGroup = GroupChildren("StatsGroup", groupStart);
            statsViewport.SetParent(statsGroup, false);

            groupStart = content.childCount;
            contextPanel = Panel("ContextPanel", content, new Color(.045f, .075f, .105f, .91f),
                new Vector2(840, -120), new Vector2(1050, 665));
            contextTitle = Label("ContextTitle", content, "", 31,
                new Vector2(870, -145), new Vector2(950, 55), TextAnchor.MiddleLeft);
            contextDescription = Label("ContextDescription", content, "", 24,
                new Vector2(870, -230), new Vector2(950, 70), TextAnchor.UpperLeft);
            eventReward = Label("EventReward", content, "", 35,
                new Vector2(890, -320), new Vector2(900, 170), TextAnchor.MiddleCenter);
            eventReward.color = PrototypeUiSkin.Cream;
            RectTransform opponentPortrait = Panel("OpponentPortraitFrame", content,
                new Color(.075f, .13f, .18f, .95f), new Vector2(870, -320), new Vector2(185, 185));
            contextPortrait = new GameObject("OpponentPortrait", typeof(RectTransform), typeof(Image))
                .GetComponent<Image>();
            contextPortrait.transform.SetParent(opponentPortrait, false);
            RectTransform opponentRect = (RectTransform)contextPortrait.transform;
            opponentRect.anchorMin = opponentRect.anchorMax = new Vector2(0, 1);
            opponentRect.pivot = new Vector2(0, 1);
            opponentRect.anchoredPosition = new Vector2(8, -8);
            opponentRect.sizeDelta = new Vector2(169, 169);
            contextPortrait.preserveAspect = true;
            contextPortrait.raycastTarget = false;
            selectedMonsterSummary = Label("SelectedMonster", content, "", 22,
                new Vector2(1090, -320), new Vector2(750, 180), TextAnchor.UpperLeft);
            opponentLoadoutTitle = Label("OpponentLoadout", content, "对手装备", 24,
                new Vector2(870, -520), new Vector2(950, 48), TextAnchor.MiddleLeft);
            for (int i = 0; i < contextItemIcons.Length; i++)
            {
                RectTransform tile = Panel("OpponentItem" + i, content,
                    new Color(.075f, .13f, .18f, .95f),
                    new Vector2(870 + i * 242, -570), new Vector2(225, 95));
                Image icon = new GameObject("Icon", typeof(RectTransform), typeof(Image))
                    .GetComponent<Image>();
                icon.transform.SetParent(tile, false);
                RectTransform iconRect = (RectTransform)icon.transform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 1);
                iconRect.pivot = new Vector2(0, 1);
                iconRect.anchoredPosition = new Vector2(12, -17);
                iconRect.sizeDelta = new Vector2(60, 60);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                contextItemIcons[i] = icon;
                contextItemNames[i] = Label("Name", tile, "", 20,
                    new Vector2(78, -10), new Vector2(137, 72), TextAnchor.MiddleLeft);
            }
            contextGroup = GroupChildren("ContextGroup", groupStart);
            notice = Label("Notice", content, "", 22, new Vector2(870, -605),
                new Vector2(950, 85), TextAnchor.MiddleLeft);
            actionButton = Button("NextStage", content, new Vector2(1410, -700),
                new Vector2(430, 68), HandleAdvance);

            choicePanel = Panel("EventChoices", content, new Color(.035f, .065f, .095f, .96f),
                new Vector2(230, -125), new Vector2(1460, 820));
            choiceTitle = Label("ChoiceTitle", choicePanel, "", 36,
                new Vector2(60, -28), new Vector2(1340, 65), TextAnchor.MiddleCenter);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i;
                choiceButtons[i] = Button("Choice" + i, choicePanel,
                    new Vector2(95 + i * 430, -140), new Vector2(400, 590),
                    () => { Choose(index); Refresh(); });
                choiceButtons[i].gameObject.AddComponent<PrototypeMonsterHover>().Initialize(this, index);
                Image portraitFrame = new GameObject("MonsterFrame", typeof(RectTransform), typeof(Image))
                    .GetComponent<Image>();
                portraitFrame.transform.SetParent(choiceButtons[i].transform, false);
                RectTransform frameRect = (RectTransform)portraitFrame.transform;
                frameRect.anchorMin = frameRect.anchorMax = new Vector2(0, 1);
                frameRect.pivot = new Vector2(0, 1);
                frameRect.anchoredPosition = new Vector2(105, -85);
                frameRect.sizeDelta = new Vector2(190, 190);
                portraitFrame.color = new Color(.08f, .13f, .19f, .88f);
                portraitFrame.raycastTarget = false;
                Image portrait = new GameObject("MonsterPortrait", typeof(RectTransform), typeof(Image))
                    .GetComponent<Image>();
                portrait.transform.SetParent(choiceButtons[i].transform, false);
                RectTransform portraitRect = (RectTransform)portrait.transform;
                portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0, 1);
                portraitRect.pivot = new Vector2(0, 1);
                portraitRect.anchoredPosition = new Vector2(115, -95);
                portraitRect.sizeDelta = new Vector2(170, 170);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                offerPortraits[i] = portrait;
                Image medal = new GameObject("TierMedal", typeof(RectTransform), typeof(Image))
                    .GetComponent<Image>();
                medal.transform.SetParent(choiceButtons[i].transform, false);
                RectTransform medalRect = (RectTransform)medal.transform;
                medalRect.anchorMin = medalRect.anchorMax = new Vector2(1, 1);
                medalRect.pivot = new Vector2(1, 1);
                medalRect.anchoredPosition = new Vector2(-24, -22);
                medalRect.sizeDelta = new Vector2(52, 52);
                medal.sprite = PrototypeUiSkin.SpriteFor("UI/tier_medal");
                medal.raycastTarget = false;
                tierMedals[i] = medal;
                Text tierText = Label("Tier", medalRect, new[] { "普", "精", "首" }[i], 19,
                    new Vector2(7, -5), new Vector2(30, 31), TextAnchor.MiddleCenter);
                tierText.color = PrototypeUiSkin.Cream;
                tierLabels[i] = Label("TierName", choiceButtons[i].transform,
                    new[] { "普通", "稀有", "BOSS" }[i], 24,
                    new Vector2(20, -30), new Vector2(130, 45), TextAnchor.MiddleLeft);
                for (int j = 0; j < 4; j++)
                {
                    int itemIndex = j;
                    RectTransform itemTile = Panel("MonsterItem" + j, choiceButtons[i].transform,
                        new Color(.075f, .13f, .18f, .92f),
                        new Vector2(22 + j * 92, -397), new Vector2(82, 78));
                    itemTile.gameObject.AddComponent<Button>().onClick.AddListener(
                        () => ShowMonsterItem(index, itemIndex));
                    itemTile.gameObject.AddComponent<PrototypeMonsterItemHover>()
                        .Initialize(this, index, itemIndex);
                    offerItemTiles[i * 4 + j] = itemTile.gameObject;
                    Image itemIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image))
                        .GetComponent<Image>();
                    itemIcon.transform.SetParent(itemTile, false);
                    RectTransform itemIconRect = (RectTransform)itemIcon.transform;
                    itemIconRect.anchorMin = itemIconRect.anchorMax = new Vector2(0, 1);
                    itemIconRect.pivot = new Vector2(0, 1);
                    itemIconRect.anchoredPosition = new Vector2(9, -5);
                    itemIconRect.sizeDelta = new Vector2(64, 64);
                    itemIcon.preserveAspect = true;
                    itemIcon.raycastTarget = false;
                    offerItemIcons[i * 4 + j] = itemIcon;
                    offerItemNames[i * 4 + j] = Label("Name", itemTile, "", 16,
                        new Vector2(2, -57), new Vector2(78, 20), TextAnchor.MiddleCenter);
                    offerItemNames[i * 4 + j].gameObject.SetActive(false);
                }
            }
            choiceItemPanel = Panel("MonsterItemDetail", choicePanel, new Color(.07f, .12f, .18f, .93f),
                new Vector2(60, -665), new Vector2(1340, 130));
            choiceItemDetails = Label("MonsterItemDetailText", choicePanel,
                "悬停怪物查看属性与装备，点击卡片选择对手", 25,
                new Vector2(85, -678), new Vector2(1290, 102), TextAnchor.UpperLeft);

            hoverPanel = Panel("ItemTooltip", content, new Color(.055f, .10f, .15f, .98f),
                new Vector2(0, 0), new Vector2(560, 210));
            hoverPanel.GetComponent<Image>().raycastTarget = false;
            hoverText = Label("ItemTooltipText", hoverPanel, "", 22,
                new Vector2(22, -17), new Vector2(516, 175), TextAnchor.UpperLeft);

            Refresh();
        }

        private void BindSavedView()
        {
            Bind("MainMenu", battle.ReturnToMenu);
            for (int i = 0; i < shopButtons.Length; i++)
            { int index = i; Rebind(shopButtons[i], () => { run.Buy(index); Refresh(); }); }
            for (int i = 0; i < bagButtons.Length; i++)
            { int index = i; Rebind(bagButtons[i], () => SelectBag(index)); }
            for (int i = 0; i < slotButtons.Length; i++)
            { int index = i; Rebind(slotButtons[i], () => SelectSlot(index)); }
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i;
                Rebind(choiceButtons[i], () => { Choose(index); Refresh(); });
                for (int j = 0; j < 4; j++)
                { int item = j; Rebind(offerItemTiles[i * 4 + j].GetComponent<Button>(), () => ShowMonsterItem(index, item)); }
            }
            Rebind(statsToggle, () => { showingStats = !showingStats; Refresh(); });
            Rebind(sellOreButton, () => { run.SellOre(); Refresh(); });
            Rebind(rerollButton, () => { run.Reroll(); Refresh(); });
            Rebind(expandButton, () => { run.Expand(); Refresh(); });
            Rebind(claimButton, () => { run.ClaimGenerated(); Refresh(); });
            Rebind(sellButton, () => { if (run.Sell(selectedBag)) selectedBag = -1; Refresh(); });
            Rebind(useButton, () => { if (run.UseInjection(selectedBag)) selectedBag = -1; Refresh(); });
            Rebind(actionButton, HandleAdvance);
        }

        private void Bind(string name, UnityEngine.Events.UnityAction action)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
                if (button.name == name) { Rebind(button, action); return; }
        }

        private static void Rebind(Button button, UnityEngine.Events.UnityAction action)
        { button.onClick.RemoveAllListeners(); button.onClick.AddListener(action); }

        public void Show(bool visible)
        {
            if (content != null) content.gameObject.SetActive(visible);
            if (visible) Refresh();
        }

        private void Update()
        {
            UpdateFeedback();
            if (hoverPanel == null || !hoverPanel.gameObject.activeInHierarchy) return;
            hoverPanel.SetAsLastSibling();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    content, Input.mousePosition, null, out Vector2 mouse)) return;
            float left = mouse.x - content.rect.xMin + 22f;
            float top = content.rect.yMax - mouse.y + 16f;
            hoverPanel.anchoredPosition = new Vector2(
                Mathf.Clamp(left, 16f, 1920f - hoverPanel.sizeDelta.x -16),
                -Mathf.Clamp(top, 16f, 1080f - hoverPanel.sizeDelta.y -16));
        }

        private void SelectBag(int index)
        {
            if (selectedBag >= 0 && selectedBag != index && index < run.Bag.Count)
            {
                if (run.MergeBag(selectedBag, index)) { selectedBag = -1; Refresh(); return; }
            }
            selectedBag = index < run.Bag.Count ? index : -1;
            Refresh();
        }

        public void DropItem(int bagIndex, PrototypeDropKind kind, int targetIndex)
        {
            bool changed = kind == PrototypeDropKind.Character ? run.UseInjection(bagIndex) :
                kind == PrototypeDropKind.Slot ? run.Equip(bagIndex, targetIndex) :
                kind == PrototypeDropKind.Shop ? run.Sell(bagIndex):run.MoveBag(bagIndex, targetIndex);
            if (changed) selectedBag = -1;
            Refresh();
        }

        public void DropShop(int shopIndex, PrototypeDropKind kind, int targetIndex)
        {
            if (!run.ShopAvailable) return;
            if (kind == PrototypeDropKind.Slot) run.BuyToSlot(shopIndex, targetIndex);
            else if (kind == PrototypeDropKind.Bag) run.Buy(shopIndex);
            Refresh();
        }

        public bool HasBagItem(int index) => index >= 0 && index < run.Bag.Count;
        public bool HasShopItem(int index) => run.ShopAvailable && index >= 0 &&
            index < run.Shop.Count && run.Shop[index] != null;

        private void Choose(int index)
        {
            bool changed = run.IsEventStage ? run.ChooseEvent(index, selectedSlot) :
                run.Stage == PrototypeDayStage.Monster && run.SelectMonster(index);
            if(changed)
            {
                if(run.IsEventStage && !run.ShopAvailable)run.AdvanceEvent();
                battle.RefreshPreparationActors();
            }
        }

        private void ShowMonsterItem(int tier, int itemIndex)
        {
            if (run.Stage != PrototypeDayStage.Monster || run.SelectedMonsterTier >= 0) return;
            PrototypeEncounter opponent = PrototypeEncounterCatalog.Get(run, tier, false);
            if (itemIndex >= opponent.itemIds.Length) return;
            PrototypeItemDefinition item = run.Find(opponent.itemIds[itemIndex]);
            if (item == null) return;
            if (opponent.character != null && itemIndex < opponent.character.slots.Length)
            {
                int first = PrototypeBroadcastSession.Owned.Count;
                try
                {
                    var actual = PrototypeBroadcastSession.CreateItem(opponent.character.slots[itemIndex], -3000-itemIndex);
                    choiceItemDetails.text = actual == null ? "空装备格" : actual.definition.displayName + " · Lv." + actual.level + "\n" + actual.StatSummary();
                }
                finally
                {
                    for (int i = PrototypeBroadcastSession.Owned.Count-1; i >= first; i--)
                    {
                        var asset = PrototypeBroadcastSession.Owned[i];
                        if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset);
                        PrototypeBroadcastSession.Owned.RemoveAt(i);
                    }
                }
            }
            else choiceItemDetails.text = item.displayName + "  ·  " + item.StatSummary();
        }

        public void ShowMonsterHover(int tier)
        {
            if (run.Stage != PrototypeDayStage.Monster || run.SelectedMonsterTier >= 0) return;
            PrototypeEncounter opponent = PrototypeEncounterCatalog.Get(run, tier, false);
            hoveredIndex=-1;hoverPanel.gameObject.SetActive(true);hoverText.text=opponent.Description(run);
            FitTooltip();
            choiceItemDetails.text = EncounterCardText(opponent);
            for (int j = 0; j < 4; j++)
                offerItemTiles[tier * 4 + j].SetActive(false);
        }

        public void ClearMonsterHover(int tier)
        {
            if (run.Stage != PrototypeDayStage.Monster || run.SelectedMonsterTier >= 0) return;
            for (int j = 0; j < 4; j++) offerItemTiles[tier * 4 + j].SetActive(false);
            choiceItemDetails.text = "悬停怪物查看属性与装备，点击卡片选择对手";
            hoverPanel.gameObject.SetActive(false);
        }

        public void ShowMonsterItemHover(int tier, int itemIndex) => ShowMonsterItem(tier, itemIndex);
        public void ClearMonsterItemHover(int tier) => ShowMonsterHover(tier);

        private void HandleAdvance()
        {
            if (run.GameOver) { battle.NewRun(); return; }
            if (run.Cleared && !run.Endless) { run.EnterEndless(); Refresh(); return; }
            if (run.IsEventStage)
            {
                if (run.AdvanceEvent()) battle.RefreshPreparationActors();
                Refresh();
            }
            else battle.BeginAiming();
        }

        public void SetHover(PrototypeHoverKind kind, int index, int direction)
        {
            if (hoveredIndex != index || hoveredKind != kind)
            {
                hoveredKind = kind; hoveredIndex = index;
                PrototypeItemInstance instance = kind == PrototypeHoverKind.Bag && index < run.Bag.Count
                    ? run.Bag[index] : kind == PrototypeHoverKind.Slot && index < run.Equipped.Count
                        ? run.Equipped[index] : null;
                hoverLevel = instance?.level ?? 1;
            }
            if (kind == PrototypeHoverKind.Slot && run.IsSlotOpen(index)) selectedSlot = index;
            hoverLevel = Mathf.Clamp(hoverLevel + direction, 1, 4);
            RefreshHover();
        }

        public void ClearHover(PrototypeHoverKind kind, int index)
        {
            if (hoveredIndex != index || hoveredKind != kind) return;
            hoveredIndex = -1;
            RefreshHover();
        }

        private void RefreshHover()
        {
            if (hoveredIndex < 0)
            {
                hoverPanel.gameObject.SetActive(false);
                return;
            }
            hoverPanel.gameObject.SetActive(true);
            PrototypeItemDefinition definition = null;
            if (hoveredKind == PrototypeHoverKind.Shop && hoveredIndex < run.Shop.Count)
                definition = run.Shop[hoveredIndex];
            else if (hoveredKind == PrototypeHoverKind.Bag && hoveredIndex < run.Bag.Count)
                definition = run.Bag[hoveredIndex].definition;
            else if (hoveredKind == PrototypeHoverKind.Slot && hoveredIndex < run.Equipped.Count)
                definition = run.Equipped[hoveredIndex]?.definition;
            if (definition == null)
            {
                hoverText.text = hoveredKind == PrototypeHoverKind.Slot && run.IsSlotOpen(hoveredIndex)
                    ? PrototypeDayRules.SlotDescription(run.SlotEffects[hoveredIndex])
                    : "";
                return;
            }
            PrototypeItemInstance hoveredItem = hoveredKind == PrototypeHoverKind.Bag && hoveredIndex < run.Bag.Count ? run.Bag[hoveredIndex] :
                hoveredKind == PrototypeHoverKind.Slot && hoveredIndex < run.Equipped.Count ? run.Equipped[hoveredIndex] : null;
            hoverText.text = PrototypeEffectDisplay.Tooltip(hoveredItem ?? new PrototypeItemInstance { definition = definition, level = definition.ResolveLevel(1) }, hoverLevel) +
                (hoveredKind == PrototypeHoverKind.Slot ? "\n" + PrototypeDayRules.SlotDescription(run.SlotEffects[hoveredIndex]) : "");
            FitTooltip();
        }

        private void SelectSlot(int index)
        {
            selectedSlot = index;
            if (selectedBag >= 0)
            {
                if (run.Equip(selectedBag, index)) selectedBag = -1;
            }
            else run.Unequip(index);
            Refresh();
        }

        private void Refresh()
        {
            if (playerPortrait != null && loadedPortraitId != battle.PlayerVisualId)
            {
                Texture2D portrait = Resources.Load<Texture2D>("Art/" + battle.PlayerVisualId);
                if (portrait != null) playerPortrait.sprite = IconSprite(portrait);
                loadedPortraitId = battle.PlayerVisualId;
            }
            if (run == null) return;
            if (run.Stage != lastChoiceStage)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                    choiceButtons[i].GetComponent<PrototypeMonsterHover>().ResetHover();
                lastChoiceStage = run.Stage;
                hoveredIndex = -1;
            }
            headline.text = $"第 {run.Day} 天 · {StageName(run.Stage)}    金币 {run.Gold}    等级 {run.Level}\n" +
                $"精力 {run.Endurance}/10 · 战败扣 {run.DefeatCost} · 保命{(run.LastStandUsed ? "已用" : "可用")}    " +
                (run.Endless ? $"无尽累计 {run.Wins} 胜" : $"胜场 {run.Wins}/10");
            notice.text = run.Notice;
            expandButton.GetComponentInChildren<Text>().text = $"升级 · {run.UpgradeCost} 金币";
            expandButton.interactable = run.Gold >= run.UpgradeCost;
            rerollButton.interactable = run.ShopAvailable;
            actionButton.interactable = run.GameOver || run.Cleared && !run.Endless ||
                run.IsEventStage && run.EventChosen || run.CanFight;
            actionButton.GetComponentInChildren<Text>().text = run.GameOver ? "本局结束 · 新的一局" :
                run.Cleared && !run.Endless ? "10 胜通关 · 进入无尽" :
                run.IsEventStage ? NextStageName(run.Stage) : "进入战斗 →";
            bool choose = !run.GameOver && (!run.Cleared || run.Endless) &&
                (run.IsEventStage && !run.EventChosen ||
                 run.Stage == PrototypeDayStage.Monster && run.SelectedMonsterTier < 0);
            choicePanel.gameObject.SetActive(choose);
            shopGroup.gameObject.SetActive(!choose && run.ShopAvailable);
            contextGroup.gameObject.SetActive(!choose && !run.ShopAvailable);
            bagGroup.gameObject.SetActive(true);
            equipmentGroup.gameObject.SetActive(!showingStats);
            statsGroup.gameObject.SetActive(showingStats);
            actionButton.gameObject.SetActive(!choose);
            bool selectedMonster = run.Stage == PrototypeDayStage.Monster && run.SelectedMonsterTier >= 0;
            bool showOpponent = selectedMonster || run.Stage == PrototypeDayStage.Rival;
            contextPanel.sizeDelta = new Vector2(1050, 520);
            ((RectTransform)actionButton.transform).anchoredPosition =
                new Vector2(1410, -588);
            notice.gameObject.SetActive(false);
            eventReward.gameObject.SetActive(false);
            eventReward.text = run.Notice.StartsWith("已恢复") ?
                "奖励已结算\n请查看金币、背包与格位" : run.Notice;
            selectedMonsterSummary.gameObject.SetActive(showOpponent);
            contextPortrait.transform.parent.gameObject.SetActive(showOpponent);
            opponentLoadoutTitle.gameObject.SetActive(showOpponent);
            for (int i = 0; i < contextItemIcons.Length; i++)
                contextItemIcons[i].transform.parent.gameObject.SetActive(showOpponent);
            if (showOpponent)
            {
                PrototypeEncounter opponent = run.CurrentEncounter;
                contextPortrait.sprite = PrototypeUiSkin.SpriteFor("Art/" + opponent.visualId);
                selectedMonsterSummary.text = $"<size=29><b>{opponent.name}</b></size>\n" +
                    $"生命 {opponent.health:0}  ·  速度 {opponent.speed:0}\n" +
                    $"撞击 +{opponent.impactBonus * 100f:0}%  ·  武器 ×{opponent.weaponDamageMultiplier:0.00}\n" +
                    $"打法：{opponent.combatStyle}  ·  胜利奖励 {opponent.rewardGold} 金";
                for (int i = 0; i < contextItemIcons.Length; i++)
                {
                    PrototypeItemDefinition item = i < opponent.itemIds.Length ?
                        run.Find(opponent.itemIds[i]) : null;
                    contextItemIcons[i].transform.parent.gameObject.SetActive(item != null);
                    contextItemIcons[i].enabled = item != null;
                    contextItemNames[i].text = item == null ? "" : item.displayName;
                    if (item != null)
                    {
                        contextItemIcons[i].sprite = PrototypeUiSkin.SpriteFor("Icons/" + item.iconId);
                    }
                }
            }
            contextTitle.text = run.Stage == PrototypeDayStage.Monster ? "野怪情报" :
                run.Stage == PrototypeDayStage.Rival ? "本日对手" : "事件结果";
            contextDescription.text = run.Stage == PrototypeDayStage.Monster ?
                "整理装备后进入战斗" :
                run.Stage == PrototypeDayStage.Rival ?
                "当前使用当日镜像对手" :
                "本次事件收获";
            if (choose)
            {
                choiceTitle.text = run.IsEventStage ? "选择今日事件" :
                    "选择野怪 · 悬停查看详情";
                choiceItemPanel.gameObject.SetActive(false);
                choiceItemDetails.gameObject.SetActive(false);
                if (!run.IsEventStage)
                    choiceItemDetails.text = "悬停怪物查看属性与装备，点击卡片选择对手";
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    bool eventChoice = run.IsEventStage;
                    choiceButtons[i].gameObject.SetActive(!eventChoice || i < 2);
                    if (eventChoice && i == 2) continue;
                    RectTransform choiceRect = (RectTransform)choiceButtons[i].transform;
                    choiceRect.anchoredPosition = eventChoice ?
                        new Vector2(235 + i * 525, -155) : new Vector2(95 + i * 430, -155);
                    choiceRect.sizeDelta = eventChoice ? new Vector2(470, 560) : new Vector2(400, 500);
                    Image choiceImage = choiceButtons[i].GetComponent<Image>();
                    PrototypeUiSkin.SetSliced(choiceImage,
                        eventChoice ? "UI/event_choice" : "UI/encounter_choice", 45);
                    choiceImage.color = Color.white;
                    bool showPortrait = !run.IsEventStage;
                    offerPortraits[i].gameObject.SetActive(showPortrait);
                    offerPortraits[i].transform.parent.Find("MonsterFrame").gameObject.SetActive(showPortrait);
                    tierMedals[i].gameObject.SetActive(run.Stage == PrototypeDayStage.Monster);
                    tierLabels[i].gameObject.SetActive(showPortrait);
                    if (showPortrait)
                        offerPortraits[i].sprite = PrototypeUiSkin.SpriteFor("Art/" +
                            PrototypeEncounterCatalog.Get(run, i, false).visualId);
                    PrototypeEncounter offer = showPortrait ?
                        PrototypeEncounterCatalog.Get(run, i, false) : null;
                    for (int j = 0; j < 4; j++)
                    {
                        PrototypeItemDefinition item = offer != null && j < offer.itemIds.Length ?
                            run.Find(offer.itemIds[j]) : null;
                        offerItemTiles[i * 4 + j].SetActive(false);
                        if (item == null) continue;
                        offerItemIcons[i * 4 + j].sprite = PrototypeUiSkin.SpriteFor("Icons/" + item.iconId);
                        offerItemNames[i * 4 + j].text = item.displayName;
                    }
                    Text cardText = choiceButtons[i].GetComponentInChildren<Text>();
                    RectTransform cardRect = (RectTransform)cardText.transform;
                    cardRect.anchoredPosition = showPortrait ? new Vector2(16, -280) : new Vector2(18, -185);
                    cardRect.sizeDelta = showPortrait ? new Vector2(288, 80) : new Vector2(284, 170);
                    cardText.alignment = showPortrait ? TextAnchor.MiddleCenter : TextAnchor.MiddleCenter;
                    cardText.fontSize = showPortrait ? 30 : 29;
                    cardText.text = eventChoice ?
                        $"<size=38><b>{EventName(run.EventAt(i))}</b></size>\n\n" +
                        PrototypeDayRules.EventDescription(run.EventAt(i), run.Day) :
                        "<b>" + PrototypeEncounterCatalog.Get(run, i, false).name + "</b>";
                }
            }
            claimButton.interactable = run.PendingRewards.Count > 0;
            claimButton.gameObject.SetActive(run.PendingRewards.Count > 0);
            expandButton.gameObject.SetActive(true);
            claimButton.GetComponentInChildren<Text>().text =
                "领取生产的针剂  ·  " + run.PendingRewards.Count + " 支";
            statsToggle.GetComponentInChildren<Text>().text = showingStats ? "返回装备" : "角色数值";
            sellOreButton.gameObject.SetActive(false);
            sellOreButton.GetComponentInChildren<Text>().text = "出售金矿 · " + run.OreCount;
            for (int i = 0; i < shopButtons.Length; i++)
            {
                PrototypeItemDefinition item = run.Shop[i];
                bool affordable = item != null && run.Gold >= item.price && run.Bag.Count < 12;
                SetCard(shopButtons[i], item, item == null ? "已售出" :
                    item.displayName+"\n"+item.RarityText,
                    true, false, run.ShopAvailable && affordable);
                shopPrices[i].text = item == null ? "" : item.price + " 金";
                shopPrices[i].color = affordable ? PrototypeUiSkin.Navy : new Color(.50f, .12f, .09f);
            }
            for (int i = 0; i < bagButtons.Length; i++)
            {
                PrototypeItemInstance item = i < run.Bag.Count ? run.Bag[i] : null;
                SetCard(bagButtons[i], item == null ? null : item.definition,
                    item == null ? "" : item.Name+(item.quantity>1?" ×"+item.quantity:""),
                    false, i == selectedBag);
            }
            ArrangeRingSlots();
            for (int i = 0; i < slotButtons.Length; i++)
            {
                bool open = run.IsSlotOpen(i);
                PrototypeItemInstance item = run.Equipped[i];
                SetCard(slotButtons[i], open && item != null ? item.definition : null,
                    open && item != null ? "Lv." + item.level : "", false, false, open);
                slotButtons[i].GetComponent<PrototypeRingSlotGraphic>().color = open ?
                    SlotColor(run.SlotEffects[i]) : new Color(.10f, .14f, .18f, .70f);
            }
            PrototypeItemInstance selected = selectedBag >= 0 && selectedBag < run.Bag.Count ? run.Bag[selectedBag] : null;
            details.text = selected == null ? "点击背包物品查看详细属性；点击已装备格位可卸下。" :
                selected.Name + " · "+selected.definition.RarityText+"\n" +
                selected.StatSummary();
            sellButton.interactable = selected != null;
            sellButton.GetComponentInChildren<Text>().text = selected == null ? "出售" :
                "出售 " + selected.SellPrice + " 金";
            useButton.interactable = selected != null && selected.definition.kind == PrototypeItemKind.Injection;
            useButton.GetComponentInChildren<Text>().text = "使用针剂";
            statsViewport.gameObject.SetActive(showingStats);
            details.gameObject.SetActive(true);
            sellButton.gameObject.SetActive(selected != null);
            useButton.gameObject.SetActive(selected != null && selected.definition.kind == PrototypeItemKind.Injection);
            if (showingStats) RefreshStats();
            RefreshModernView();
            RefreshHover();
        }

        private void RefreshStats()
        {
            PrototypeStatSnapshot s = run.CalculateStats();
            float mass = battle.Balance.EffectiveMass(s.health);
            float targetSpeed = battle.Balance.TargetSpeed(s.speed, s.health);
            string body = $"生命上限  {s.health:0}             速度数值  {s.speed:0}\n\n" +
                $"质量  {mass:0.00}                 实际速度  {targetSpeed:0.0}\n\n" +
                $"撞击伤害  +{s.impactBonus * 100f:0.#}%       撞击减伤  {s.impactTakenReduction * 100f:0.#}%\n\n" +
                $"近战加成  +{s.meleeDamageBonus * 100f:0.#}%         远程加成  +{s.rangedDamageBonus * 100f:0.#}%\n\n" +
                $"本体生命  {run.BodyHealth:0.#}     攻击加成  {s.damageBonus*100:0.#}%\n\n" +
                $"下一场开场护盾  {s.startingShield:0.#}     直伤减免  {s.damageReduction*100:0.#}%\n\n来源明细\n";
            foreach (PrototypeStatModifier modifier in s.sources)
                body += $"{modifier.sourceId}：{PrototypeItemDefinition.StatName(modifier.stat)} +{modifier.amount:0.##}\n";
            statsText.text = body;
        }

        private static string NextStageName(PrototypeDayStage stage)
        {
            switch (stage)
            {
                case PrototypeDayStage.EventOne: return "进入事件二 →";
                case PrototypeDayStage.EventTwo: return "前往野怪 →";
                case PrototypeDayStage.EventThree: return "进入事件四 →";
                default: return "前往匹配战 →";
            }
        }

        private static string EventName(PrototypeEventKind kind)
        {
            switch (kind)
            {
                case PrototypeEventKind.Shop: return "商店";
                case PrototypeEventKind.Gold: return "金币补给";
                case PrototypeEventKind.Ore: return "矿藏";
                case PrototypeEventKind.Pickaxe: return "矿工遗物";
                case PrototypeEventKind.Weapon: return "武器箱";
                case PrototypeEventKind.Injections: return "药剂工坊";
                case PrototypeEventKind.MasterTraining:return "宗师训练";
                case PrototypeEventKind.Rest:return "生命休整";
                case PrototypeEventKind.Trade:return "商旅资助";
                default: return "格位附魔";
            }
        }

        private static string StageName(PrototypeDayStage stage)
        {
            switch (stage)
            {
                case PrototypeDayStage.EventOne: return "事件一";
                case PrototypeDayStage.EventTwo: return "事件二";
                case PrototypeDayStage.Monster: return "野怪";
                case PrototypeDayStage.EventThree: return "事件三";
                case PrototypeDayStage.EventFour: return "事件四";
                default: return "匹配战";
            }
        }

        private static string SlotName(int index)
        {
            return index >= 0 && index < SlotNames.Length ? SlotNames[index] : "格位";
        }

        private void ArrangeRingSlots()
        {
            bool tenSlots = run.Slots >= 10;
            int[] order = tenSlots ? new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 } :
                new[] { 0, 1, 2, 3, 6, 7, 8, 9 };
            for (int i = 0; i < slotButtons.Length; i++)
                slotButtons[i].gameObject.SetActive(tenSlots || i != 4 && i != 5);
            float sweep = 360f / order.Length;
            for (int sector = 0; sector < order.Length; sector++)
            {
                Button button = slotButtons[order[sector]];
                float angle = 90f - sweep * (sector + .5f);
                button.GetComponent<PrototypeRingSlotGraphic>()
                    .Configure(angle, sweep, 145f, 260f);
                float radians = angle * Mathf.Deg2Rad;
                Vector2 center = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 202f;
                RectTransform icon = (RectTransform)button.transform.Find("Icon");
                icon.anchorMin = icon.anchorMax = new Vector2(.5f, .5f);
                icon.pivot = new Vector2(.5f, .5f);
                icon.anchoredPosition = center + new Vector2(0, 10);
                icon.sizeDelta = new Vector2(67, 67);
                RectTransform label = (RectTransform)button.transform.Find("Text");
                label.anchorMin = label.anchorMax = new Vector2(.5f, .5f);
                label.pivot = new Vector2(.5f, .5f);
                label.anchoredPosition = center + new Vector2(0, -33);
                label.sizeDelta = new Vector2(84, 28);
            }
        }

        private string EncounterCardText(PrototypeEncounter opponent)
        {
            return $"<b>{opponent.name}</b>　生命 {opponent.health:0}  ·  速度 {opponent.speed:0}\n" +
                $"撞击 +{opponent.impactBonus * 100f:0}%  ·  武器 ×{opponent.weaponDamageMultiplier:0.00}\n" +
                $"打法：{opponent.combatStyle}  ·  " +
                $"<color=#F4C773>奖励 {opponent.rewardGold} 金</color>";
        }

        private void SetCard(Button button, PrototypeItemDefinition item, string caption,
            bool shop, bool selected = false, bool unlocked = true)
        {
            button.interactable = unlocked && (!shop || item != null);
            bool ringSlot = button.GetComponent<PrototypeRingSlotGraphic>() != null;
            bool smallSlot = button.name.StartsWith("Bag") || button.name.StartsWith("Slot");
            if (!ringSlot)
                button.GetComponent<Image>().color = !unlocked ? new Color(.18f, .23f, .28f, .8f) :
                    selected ? new Color(.35f, .29f, .17f, .96f) :
                    smallSlot ? new Color(.075f, .13f, .18f, .92f) : Color.white;
            Text label = button.GetComponentInChildren<Text>();
            label.text = caption;
            if(smallSlot)label.color=item!=null?PrototypeItemRepository.RarityColor(item.rarity):Color.white;
            else if(item!=null){var rarityColor=PrototypeItemRepository.RarityColor(item.rarity)*.6f;rarityColor.a=1;label.text=caption.Replace(item.RarityText,"<color=#"+ColorUtility.ToHtmlStringRGB(rarityColor)+">"+item.RarityText+"</color>");}
            if(!ringSlot && smallSlot && item!=null && !selected)button.GetComponent<Image>().color=Color.Lerp(new Color(.075f,.13f,.18f),PrototypeItemRepository.RarityColor(item.rarity),.20f);
            Image icon = button.transform.Find("Icon")?.GetComponent<Image>();
            if (icon == null) return;
            icon.sprite = item == null ? null : PrototypeUiSkin.SpriteFor("Icons/" + item.iconId);
            icon.enabled = icon.sprite != null;
        }

        private static Color SlotColor(PrototypeSlotEffect effect)
        {
            switch (effect)
            {
                case PrototypeSlotEffect.Vital: return new Color(.13f, .34f, .24f);
                case PrototypeSlotEffect.Swift: return new Color(.10f, .31f, .39f);
                case PrototypeSlotEffect.Sharpened: return new Color(.42f, .27f, .12f);
                case PrototypeSlotEffect.Echo: return new Color(.30f, .21f, .43f);
                case PrototypeSlotEffect.Guarded: return new Color(.18f, .25f, .44f);
                case PrototypeSlotEffect.Prosperity: return new Color(.42f, .35f, .14f);
                default: return new Color(.15f, .26f, .32f);
            }
        }

        private readonly System.Collections.Generic.Dictionary<Texture2D, Sprite> sprites =
            new System.Collections.Generic.Dictionary<Texture2D, Sprite>();
        private Sprite IconSprite(Texture2D texture)
        {
            if (!sprites.TryGetValue(texture, out Sprite sprite))
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(.5f, .5f), 100f);
                sprites.Add(texture, sprite);
            }
            return sprite;
        }

        private void OnDestroy()
        {
            if(feedbackRun!=null){feedbackRun.Feedback-=ShowFeedback;feedbackRun.ItemAnimationFeedback-=ShowItemAnimation;}
            foreach (Sprite sprite in sprites.Values) if (sprite != null) Destroy(sprite);
        }

        private RectTransform GroupChildren(string name, int firstChild)
        {
            int lastChild = content.childCount;
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(content, false);
            RectTransform group = (RectTransform)go.transform;
            group.anchorMin = group.anchorMax = new Vector2(0, 1);
            group.pivot = new Vector2(0, 1);
            group.anchoredPosition = Vector2.zero;
            group.sizeDelta = new Vector2(1920, 1080);
            // The old controls used screen coordinates; a full-size group preserves them.
            for (int i = firstChild; i < lastChild; i++)
                content.GetChild(firstChild).SetParent(group, false);
            return group;
        }

        private RectTransform Panel(string name, Transform parent, Color color, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            if (name == "ShopPanel" || name == "BagPanel" || name == "EquipmentPanel" ||
                name == "StatsPanel" || name == "ContextPanel" || name == "EventChoices")
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(.54f, .68f, .71f, .44f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return rect;
        }

        private Text Label(string name, Transform parent, string value, int size,
            Vector2 pos, Vector2 dimensions, TextAnchor align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = dimensions;
            Text label = go.GetComponent<Text>();
            label.font = font; label.fontSize = size; label.text = value;
            label.color = Color.white; label.alignment = align;
            label.raycastTarget = false;
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = PrototypeUiSkin.Navy;
            shadow.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        private Button Button(string name, Transform parent, Vector2 pos, Vector2 dimensions,
            UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Panel(name, parent, Color.white, pos, dimensions);
            Image background = rect.GetComponent<Image>();
            string skin = name.StartsWith("Choice") ? "UI/event_choice" :
                name.StartsWith("Shop") ? "UI/shop_row" :
                name.StartsWith("Bag") ? "UI/bag_slot" :
                name.StartsWith("Slot") ? "UI/equip_slot" :
                name == "NextStage" ? "UI/primary_action" : "UI/stat_pill";
            if (name.StartsWith("Bag") || name.StartsWith("Slot"))
            {
                background.color = new Color(.075f, .13f, .18f, .92f);
                Outline outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.44f, .64f, .68f, .52f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
            else PrototypeUiSkin.SetSliced(background, skin, name.StartsWith("Choice") ? 45 : 20);
            Button button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            Image icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(rect, false);
            RectTransform iconRect = (RectTransform)icon.transform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f);
            iconRect.pivot = new Vector2(0, .5f);
            iconRect.anchoredPosition = new Vector2(12, 0);
            iconRect.sizeDelta = new Vector2(50, 50);
            icon.enabled = false;
            icon.raycastTarget = false;
            Text label = Label("Text", rect, "", 21, new Vector2(72, -5),
                new Vector2(dimensions.x - 80, dimensions.y - 10), TextAnchor.MiddleLeft);
            RectTransform labelRect = (RectTransform)label.transform;
            if (name.StartsWith("Shop"))
            {
                iconRect.anchoredPosition = new Vector2(14, 0);
                iconRect.sizeDelta = new Vector2(64, 64);
                labelRect.anchoredPosition = new Vector2(88, -12);
                labelRect.sizeDelta = new Vector2(235, dimensions.y - 24);
                label.fontSize = 23;
            }
            else if (name.StartsWith("Bag"))
            {
                iconRect.anchoredPosition = new Vector2(12, 0);
                iconRect.sizeDelta = new Vector2(63, 63);
                labelRect.anchoredPosition = new Vector2(82, -4);
                labelRect.sizeDelta = new Vector2(dimensions.x - 88, dimensions.y - 8);
                label.fontSize = 21;
            }
            else if (name.StartsWith("Slot"))
            {
                iconRect.sizeDelta = new Vector2(48, 48);
                iconRect.anchoredPosition = new Vector2(7, 0);
                labelRect.anchoredPosition = new Vector2(58, -3);
                labelRect.sizeDelta = new Vector2(dimensions.x - 62, dimensions.y - 6);
                label.fontSize = 17;
            }
            else if (!name.StartsWith("Choice"))
            {
                labelRect.anchoredPosition = new Vector2(8, -4);
                labelRect.sizeDelta = new Vector2(dimensions.x - 16, dimensions.y - 8);
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 23;
            }
            return button;
        }
    }
}
