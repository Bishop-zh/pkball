using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    /// <summary>UGUI main menu assembled from the generated transparent UI atlas.</summary>
    public sealed class PrototypeMainMenuUI : MonoBehaviour
    {
        private static readonly Color Ink = new Color(.045f, .09f, .15f);
        private static readonly Color Cream = new Color(1f, .96f, .82f);
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        [SerializeField] private PrototypeBattle battle;
        [SerializeField] private Font font;
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private Text recordText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Text modeDetail;
        [SerializeField] private Image[] selections = new Image[3];
        [SerializeField] private PrototypeHistoryMenu historyMenu;
        public void BuildHistoryView()
        {
            if(canvasRoot==null) return;
            historyMenu=canvasRoot.GetComponent<PrototypeHistoryMenu>()??canvasRoot.AddComponent<PrototypeHistoryMenu>();
            historyMenu.Build();
        }
        public void BuildBroadcastView()
        {if(canvasRoot==null)return;var editor=canvasRoot.GetComponent<PrototypeBroadcastEditor>()??canvasRoot.AddComponent<PrototypeBroadcastEditor>();editor.Build();}
        private void BindBroadcast() {BuildBroadcastView();canvasRoot.GetComponent<PrototypeBroadcastEditor>().Bind(battle);}
        private readonly string[] visualIds = { "player_warrior", "player_mage", "player_engineer" };
        private readonly string[] visualNames = { "战士", "法师", "工程师" };

        public void Initialize(PrototypeBattle owner)
        {
            battle = owner;
            if (canvasRoot != null) { BindSavedView(); BuildHistoryView(); historyMenu.Bind(battle); BindBroadcast(); Refresh(); return; }
            font = PrototypeFont.Default;
            if (FindObjectOfType<EventSystem>() == null)
            {
                var events = new GameObject("MainMenuEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }
            canvasRoot = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            RectTransform root = (RectTransform)canvasRoot.transform;

            Image background = Image("Landscape", root, SpriteFor("UI/menu_background"));
            Fill(background.rectTransform);
            background.raycastTarget = false;
            Image shade = Image("ReadableOverlay", root, null);
            Fill(shade.rectTransform);
            shade.color = new Color(.025f, .055f, .10f, .24f);
            shade.raycastTarget = false;

            RectTransform title = Element("TitlePlaque", root, new Vector2(0, -10),
                new Vector2(770, 250), SpriteFor("UI/title_plaque"));
            title.pivot = new Vector2(.5f, 1f);
            Label("GameTitle", title, "弹 珠 角 斗 场", 65, new Vector2(0, -102),
                new Vector2(710, 100), TextAnchor.MiddleCenter, Cream);
            Label("GameSubtitle", title, "P K   B A L L", 26, new Vector2(0, -174),
                new Vector2(460, 40), TextAnchor.MiddleCenter, new Color(.39f, .89f, .95f));

            RectTransform modes = Rect("ModeCards", root);
            Place(modes, new Vector2(0, -300), new Vector2(920, 585));
            ModeCard(modes, "match_banner", "匹配模式", "开始一场冒险", 0, 0, 310, 445, true);
            Label("FutureModes", modes, "更多玩法筹备中", 22, new Vector2(0, -478),
                new Vector2(640, 45), TextAnchor.MiddleCenter, Cream);

            RectTransform journey = Panel("JourneyPanel", root, new Vector2(28, -382),
                new Vector2(335, 380));
            Label("JourneyHeader", journey, "旅 程 档 案", 32, new Vector2(0, -30),
                new Vector2(300, 55), TextAnchor.MiddleCenter, Cream);
            recordText = Label("JourneyRecord", journey, "", 29, new Vector2(0, -112),
                new Vector2(285, 210), TextAnchor.UpperCenter, Color.white);

            RectTransform appearance = Panel("AppearancePanel", root, new Vector2(1555, -382),
                new Vector2(337, 610));
            Label("AppearanceHeader", appearance, "角 色 外 观", 30, new Vector2(0, -28),
                new Vector2(300, 55), TextAnchor.MiddleCenter, Cream);
            Label("AppearanceHint", appearance, "仅改变外观，成长从白板开始", 20,
                new Vector2(0, -83), new Vector2(310, 40), TextAnchor.MiddleCenter, Color.white);
            for (int index = 0; index < 3; index++)
            {
                int selected = index;
                RectTransform card = Element("Skin_" + visualIds[index], appearance,
                    new Vector2(-98 + index * 98, -186), new Vector2(90, 100),
                    SpriteFor("UI/portrait_frame"));
                card.pivot = new Vector2(.5f, 1f);
                Image frame = card.GetComponent<Image>();
                selections[index] = frame;
                Button select = card.gameObject.AddComponent<Button>();
                select.transition = Selectable.Transition.ColorTint;
                select.onClick.AddListener(() =>
                {
                    battle.SelectPlayerVisual(visualIds[selected]);
                    Refresh();
                });
                Image portrait = Image("Portrait", card, SpriteFor("Art/" + visualIds[index]));
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                Place(portrait.rectTransform, new Vector2(0, -45), new Vector2(72, 72),
                    new Vector2(.5f, 1f), new Vector2(.5f, .5f));
                Label("SkinName", appearance, visualNames[index], 20,
                    new Vector2(-98 + index * 98, -294), new Vector2(90, 38),
                    TextAnchor.MiddleCenter, Color.white);
            }
            selectionText = Label("Selection", appearance, "", 22, new Vector2(0, -350),
                new Vector2(300, 56), TextAnchor.MiddleCenter, Cream);
            Label("ModeNote", appearance, "当前为本地模拟对战", 20,
                new Vector2(0, -455), new Vector2(300, 90), TextAnchor.MiddleCenter, Color.white);

            Label("Footer", root, "选择外观  ·  配置装备  ·  弹射战斗", 22,
                new Vector2(0, -1019), new Vector2(1100, 40),
                TextAnchor.MiddleCenter, Cream);
            BuildHistoryView(); historyMenu.Bind(battle); BindBroadcast();
            Refresh();
        }

        private void BindSavedView()
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                button.onClick.RemoveAllListeners();
                if (button.name == "匹配模式") button.onClick.AddListener(battle.EnterMatchMode);
                for (int i = 0; i < visualIds.Length; i++)
                {
                    int index = i;
                    if (button.name == "Skin_" + visualIds[i])
                        button.onClick.AddListener(() => { battle.SelectPlayerVisual(visualIds[index]); Refresh(); });
                }
            }
        }

        public void Show(bool visible)
        {
            if (canvasRoot == null) return;
            if (visible) { Refresh(); if(historyMenu!=null)historyMenu.Refresh(); }
            canvasRoot.SetActive(visible);
        }

        private void Refresh()
        {
            if (battle == null || battle.Run == null || recordText == null) return;
            PrototypeRun run = battle.Run;
            recordText.text = $"第 {run.Day} 天\n" +
                (run.Endless ? $"无尽累计  {run.Wins} 胜\n" : $"胜场  {run.Wins} / 10\n") +
                $"精力  {run.Endurance} / 10\n金币  {run.Gold}\n保命{(run.LastStandUsed?"已用":"可用")} · 战败扣{run.DefeatCost}";
            if (modeDetail != null)
                modeDetail.text = run.Parked || run.GameOver ? "开始新对局" : run.Day > 1 || run.Wins > 0 ?
                    $"继续第 {run.Day} 天" : "开始一场冒险";
            for (int i = 0; i < selections.Length; i++)
                if (selections[i] != null)
                    selections[i].color = visualIds[i] == battle.PlayerVisualId ?
                        Color.white : new Color(.56f, .65f, .72f, .85f);
            for (int i = 0; i < visualIds.Length; i++)
                if (visualIds[i] == battle.PlayerVisualId)
                {
                    selectionText.text = "当前外观：" + visualNames[i];
                    break;
                }
        }

        private void ModeCard(Transform parent, string spriteName, string title, string detail,
            float x, float y, float width, float height, bool enabled)
        {
            RectTransform card = Element(title, parent, new Vector2(x, y),
                new Vector2(width, height), SpriteFor("UI/" + spriteName));
            card.pivot = new Vector2(.5f, 1f);
            if (enabled)
            {
                Button button = card.gameObject.AddComponent<Button>();
                button.onClick.AddListener(battle.EnterMatchMode);
            }
            else card.GetComponent<Image>().color = new Color(.75f, .78f, .82f, .9f);
            Label("ModeName", card, title, enabled ? 38 : 30,
                new Vector2(0, -height * .43f), new Vector2(width - 35, 65),
                TextAnchor.MiddleCenter, Cream);
            Text detailText = Label("ModeDetail", card, detail, enabled ? 23 : 19,
                new Vector2(0, -height * .59f), new Vector2(width - 35, 48),
                TextAnchor.MiddleCenter, Cream);
            if (enabled) modeDetail = detailText;
        }

        private void WoodLabel(Transform parent, string caption, float y)
        {
            RectTransform wood = Element(caption, parent, new Vector2(0, y),
                new Vector2(278, 77), SpriteFor("UI/wood_button"));
            wood.pivot = new Vector2(.5f, 1f);
            wood.GetComponent<Image>().raycastTarget = false;
            Label("Caption", wood, caption, 23, new Vector2(0, -39),
                new Vector2(250, 52), TextAnchor.MiddleCenter, Cream);
        }

        private RectTransform Panel(string name, Transform parent, Vector2 topLeft, Vector2 size)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            Image panel = rect.gameObject.AddComponent<Image>();
            panel.color = new Color(.035f, .075f, .12f, .83f);
            panel.raycastTarget = false;
            return rect;
        }

        private RectTransform Element(string name, Transform parent, Vector2 point, Vector2 size, Sprite sprite)
        {
            Image image = Image(name, parent, sprite);
            Place(image.rectTransform, point, size);
            image.preserveAspect = true;
            return image.rectTransform;
        }

        private Text Label(string name, Transform parent, string value, int size,
            Vector2 point, Vector2 dimensions, TextAnchor alignment, Color color)
        {
            RectTransform rect = Rect(name, parent);
            Place(rect, point, dimensions);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private static Image Image(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rect, Vector2 point, Vector2 size,
            Vector2? anchor = null, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(.5f, 1f);
            rect.pivot = pivot ?? new Vector2(.5f, 1f);
            rect.anchoredPosition = point;
            rect.sizeDelta = size;
        }

        private static void Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private Sprite SpriteFor(string resource)
        {
            if (sprites.TryGetValue(resource, out Sprite cached)) return cached;
            Texture2D texture = Resources.Load<Texture2D>(resource);
            if (texture == null) return null;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f);
            sprites.Add(resource, sprite);
            return sprite;
        }

        private void OnDestroy()
        {
            foreach (Sprite sprite in sprites.Values) if (sprite != null) Destroy(sprite);
        }
    }
}
