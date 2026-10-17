using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private PrototypeBattle battle;
        [SerializeField] private PrototypeBattleView modularView;
        [SerializeField] private Text playerText;
        [SerializeField] private Text enemyText;
        [SerializeField] private Text resultText;
        [SerializeField] private Text statsText;
        [SerializeField] private Image playerHealthFill;
        [SerializeField] private Image enemyHealthFill;
        [SerializeField] private Image playerEnergyFill;
        [SerializeField] private Image enemyEnergyFill;
        [SerializeField] private Text speedButtonText;
        private float nextRefresh;
        [SerializeField] private Font font;
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private Image playerPortrait;
        [SerializeField] private Image enemyPortrait;

        public void Show(bool visible)
        {
            if (modularView != null) { modularView.gameObject.SetActive(visible); if (canvasRoot != null) canvasRoot.SetActive(false); return; }
            if (canvasRoot != null) canvasRoot.SetActive(visible);
        }

        public void Initialize(PrototypeBattle owner)
        {
            battle = owner;
            if (modularView != null) { modularView.Bind(owner); if (canvasRoot != null) canvasRoot.SetActive(false); return; }
            if (canvasRoot != null)
            {
                foreach (Button button in GetComponentsInChildren<Button>(true))
                {
                    button.onClick.RemoveAllListeners();
                    if (button.name == "NewRunButton") button.onClick.AddListener(battle.NewRun);
                    if (button.name == "SpeedButton") button.onClick.AddListener(battle.ToggleSpeed);
                    if (button.name == "RestartButton") button.onClick.AddListener(battle.Restart);
                }
                Refresh(); return;
            }
            font = PrototypeFont.Default;
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject events = new GameObject("PrototypeEventSystem");
                events.transform.SetParent(transform);
                events.AddComponent<EventSystem>();
                events.AddComponent<StandaloneInputModule>();
            }

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvasRoot = canvasObject;
            canvasObject.transform.SetParent(transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform top = Panel("TopStatus", canvasObject.transform, new Color(0.035f, 0.055f, 0.09f, 0.88f));
            Anchor(top, new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0f, 120f));
            playerPortrait = Portrait(top, "PlayerPortrait", new Vector2(25f, -12f),
                "Art/" + battle.PlayerVisualId);
            enemyPortrait = Portrait(top, "EnemyPortrait", new Vector2(-25f, -12f),
                "Art/" + battle.Run.CurrentEncounter.visualId, true);
            playerText = Label("Player", top, "玩家", 27, TextAnchor.MiddleLeft, Color.white);
            Anchor((RectTransform)playerText.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(125f, -12f), new Vector2(390f, 42f));
            enemyText = Label("Enemy", top, "对手", 27, TextAnchor.MiddleRight, Color.white);
            Anchor((RectTransform)enemyText.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-125f, -12f), new Vector2(390f, 42f));
            CreateBar(top, new Vector2(125f, -72f), new Vector2(390f, 20f),
                new Color(0.15f, 0.77f, 0.83f), out playerHealthFill);
            CreateBar(top, new Vector2(125f, -102f), new Vector2(390f, 10f),
                new Color(0.9f, 0.72f, 0.27f), out playerEnergyFill);
            CreateBar(top, new Vector2(-125f, -72f), new Vector2(390f, 20f),
                new Color(0.93f, 0.37f, 0.34f), out enemyHealthFill, true);
            CreateBar(top, new Vector2(-125f, -102f), new Vector2(390f, 10f),
                new Color(0.9f, 0.72f, 0.27f), out enemyEnergyFill, true);

            statsText = Label("BattleClock", top, "", 25, TextAnchor.MiddleCenter, Color.white);
            Anchor((RectTransform)statsText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(420f, 50f));

            RectTransform bottom = Panel("BottomControls", canvasObject.transform,
                new Color(0.035f, 0.055f, 0.09f, 0.88f));
            Anchor(bottom, new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 128f));
            RectTransform goldLine = Panel("BrassDivider", bottom, PrototypeUiSkin.Gold);
            goldLine.anchorMin = new Vector2(0f, 1f);
            goldLine.anchorMax = Vector2.one;
            goldLine.offsetMin = new Vector2(0f, -4f);
            goldLine.offsetMax = Vector2.zero;
            resultText = Label("Instructions", bottom, "", 24, TextAnchor.MiddleLeft, Color.white);
            Anchor((RectTransform)resultText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(30f, 0f), new Vector2(1120f, 95f));
            Button("NewRunButton", bottom, "新的一局", new Vector2(-470f, 0f),
                new Vector2(175f, 68f), battle.NewRun);
            speedButtonText = Button("SpeedButton", bottom, "速度 1×", new Vector2(-265f, 0f),
                new Vector2(175f, 68f), battle.ToggleSpeed);
            Button("RestartButton", bottom, "重开  R", new Vector2(-60f, 0f),
                new Vector2(175f, 68f), battle.Restart);
            Refresh();
        }

        private void Update()
        {
            if (modularView != null || battle == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            Refresh();
        }

        private void Refresh()
        {
            if (battle==null || battle.Run==null || battle.Player==null || battle.Enemy==null || playerText==null || enemyText==null || resultText==null) return;
            if (playerPortrait != null)
                playerPortrait.sprite = PrototypeUiSkin.SpriteFor("Art/" + battle.PlayerVisualId);
            if (enemyPortrait != null)
                enemyPortrait.sprite = PrototypeUiSkin.SpriteFor("Art/" + battle.Run.CurrentEncounter.visualId);
            PrototypeActor player = battle.Player;
            PrototypeActor enemy = battle.Enemy;
            playerText.text = $"玩家  生命 {player.Health:0}/{player.MaximumHealth:0}  能量 {player.Energy:0}";
            enemyText.text = $"对手  生命 {enemy.Health:0}/{enemy.MaximumHealth:0}  能量 {enemy.Energy:0}";
            SetBar(playerHealthFill, player.Health / player.MaximumHealth, false);
            SetBar(enemyHealthFill, enemy.Health / enemy.MaximumHealth, true);
            SetBar(playerEnergyFill, player.Energy / battle.Balance.maximumEnergy, false);
            SetBar(enemyEnergyFill, enemy.Energy / battle.Balance.maximumEnergy, true);
            speedButtonText.text = battle.SpeedMultiplier > 1f ? "速度 2×" : "速度 1×";
            statsText.text = $"战斗时间  {battle.Elapsed:0.0} 秒";
            resultText.text = battle.Result +
                $"\n局内生命 {battle.Run.Endurance}/10    " +
                (battle.Run.Endless ? $"无尽累计 {battle.Run.Wins} 胜" : $"胜场 {battle.Run.Wins}/10");
        }

        private RectTransform Panel(string label, Transform parent, Color color)
        {
            GameObject obj = new GameObject(label, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return (RectTransform)obj.transform;
        }

        private static void Decorate(Transform parent, string name, string resource,
            Vector2 position, Vector2 size, Vector2 anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            Anchor(rect, anchor, anchor, position, size);
            Image image = obj.GetComponent<Image>();
            PrototypeUiSkin.SetSliced(image, resource, 42);
            image.raycastTarget = false;
        }

        private static Image Portrait(Transform parent, string name, Vector2 position, string resource,
            bool rightAligned = false)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            Vector2 anchor = rightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            Anchor(rect, anchor, anchor, position, new Vector2(88f, 88f));
            Image image = obj.GetComponent<Image>();
            image.sprite = PrototypeUiSkin.SpriteFor(resource);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Text Label(string label, Transform parent, string content, int size, TextAnchor alignment, Color color)
        {
            GameObject obj = new GameObject(label, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text value = obj.GetComponent<Text>();
            value.font = font;
            value.fontSize = size;
            value.text = content;
            value.color = color;
            value.alignment = alignment;
            value.raycastTarget = false;
            value.horizontalOverflow = HorizontalWrapMode.Wrap;
            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = PrototypeUiSkin.Navy;
            shadow.effectDistance = new Vector2(2f, -2f);
            return value;
        }

        private void CreateBar(Transform parent, Vector2 position, Vector2 size, Color fillColor,
            out Image fill, bool rightAligned = false)
        {
            RectTransform track = Panel("BarTrack", parent, Color.white);
            Vector2 anchor = rightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            Anchor(track, anchor, anchor, position, size);
            track.GetComponent<Image>().color = new Color(.22f, .32f, .39f, .9f);
            RectTransform inset = Panel("BarInset", track, new Color(.07f, .11f, .18f));
            inset.anchorMin = Vector2.zero;
            inset.anchorMax = Vector2.one;
            float padding = size.y >= 15f ? 3f : 1f;
            inset.offsetMin = new Vector2(3f, padding);
            inset.offsetMax = new Vector2(-3f, -padding);
            RectTransform fillRect = Panel("BarFill", inset, fillColor);
            Anchor(fillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill = fillRect.GetComponent<Image>();
            fill.type = Image.Type.Simple;
        }

        private static void SetBar(Image fill, float fraction, bool rightAligned)
        {
            RectTransform rect = (RectTransform)fill.transform;
            fraction = Mathf.Clamp01(fraction);
            rect.anchorMin = new Vector2(rightAligned ? 1f - fraction : 0f, 0f);
            rect.anchorMax = new Vector2(rightAligned ? 1f : fraction, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Text Button(string label, Transform parent, string text, Vector2 position,
            Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            Anchor(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), position, size);
            Image background = obj.GetComponent<Image>();
            PrototypeUiSkin.SetSliced(background, "UI/stat_pill", 18);
            background.color = Color.white;
            obj.GetComponent<Button>().onClick.AddListener(action);
            Text labelText = Label("Label", obj.transform, text, 22, TextAnchor.MiddleCenter,
                PrototypeUiSkin.Cream);
            Anchor((RectTransform)labelText.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return labelText;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = min == max ? min :
                min.y == max.y ? new Vector2(0.5f, min.y) : new Vector2(0.5f, 0.5f);
            if (min == max)
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
            else if (min.y == max.y)
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
            else
            {
                rect.offsetMin = position;
                rect.offsetMax = -position;
            }
        }
    }
}
