using UnityEngine;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    // Presentation only: no state is inferred from the size or visibility of a bar.
    public sealed class PrototypeBattleVitals : MonoBehaviour
    {
        private Image health, shield, trail;
        private Text healthText, shieldText, scaleText;
        private readonly Text[] statuses = new Text[3];
        private readonly Image[] dividers = new Image[12];
        private float previous = 1, hold, lastRefresh;
        public static PrototypeBattleVitals Create(Transform panel, Font font)
        {
            var root = new GameObject("ReadableVitals", typeof(RectTransform), typeof(PrototypeBattleVitals));
            root.transform.SetParent(panel, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.anchoredPosition = new Vector2(0,258); rect.sizeDelta = new Vector2(370,74);
            var view = root.GetComponent<PrototypeBattleVitals>(); view.Build(font); return view;
        }
        internal static Image Box(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent,false); var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
            image.rectTransform.anchorMin=min; image.rectTransform.anchorMax=max;
            image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero; return image;
        }
        internal static Text Label(Transform parent, Font font, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            go.transform.SetParent(parent,false); var text=go.GetComponent<Text>();
            text.font=font; text.fontSize=size; text.fontStyle=FontStyle.Bold; text.alignment=TextAnchor.MiddleCenter;
            text.color=Color.white; text.raycastTarget=false;
            text.rectTransform.anchorMin=Vector2.zero; text.rectTransform.anchorMax=Vector2.one;
            text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
            var outline=go.AddComponent<Outline>(); outline.effectColor=new Color(0,0,0,.85f); outline.effectDistance=new Vector2(1,-1);
            return text;
        }
        private void Build(Font font)
        {
            var background=Box(transform,"VitalBar",new Color(.08f,.12f,.16f),Vector2.zero,Vector2.one);
            trail=Box(background.transform,"DamageTrail",new Color(1,.55f,.25f),Vector2.zero,new Vector2(1,.73f));
            health=Box(background.transform,"Health",new Color(.15f,.74f,.4f),Vector2.zero,new Vector2(1,.73f));
            shield=Box(background.transform,"ShieldOverlay",new Color(.16f,.65f,1),new Vector2(0,.75f),Vector2.one);
            for(int i=0;i<dividers.Length;i++)dividers[i]=Box(background.transform,"Segment",new Color(.02f,.08f,.1f,.85f),Vector2.zero,Vector2.zero);
            healthText=Label(background.transform,font,30);healthText.rectTransform.anchorMax=new Vector2(1,.73f);
            shieldText=Label(background.transform,font,17);shieldText.rectTransform.anchorMin=new Vector2(0,.74f);
            scaleText=Label(transform,font,16);scaleText.rectTransform.anchorMin=scaleText.rectTransform.anchorMax=new Vector2(.5f,0);
            scaleText.rectTransform.sizeDelta=new Vector2(370,22);scaleText.rectTransform.anchoredPosition=new Vector2(0,-83);
            scaleText.color=new Color(.88f,.81f,.67f);
            Color[] colors={new Color(.2f,.4f,.12f),new Color(.58f,.19f,.06f),new Color(.08f,.36f,.23f)};
            for(int i=0;i<3;i++)
            {
                var tile=Box(transform,"Status"+i,colors[i],new Vector2(i/3f,0),new Vector2((i+1)/3f,0));
                tile.rectTransform.offsetMin=new Vector2(2,-69);tile.rectTransform.offsetMax=new Vector2(-2,-9);
                statuses[i]=Label(tile.transform,font,21);
            }
        }
        // About three initial segments; later max-health growth keeps the original unit.
        public static float SegmentUnit(float initialHealth)
        {
            float target=Mathf.Max(1,initialHealth/3), magnitude=Mathf.Pow(10,Mathf.Floor(Mathf.Log10(target)));
            float scaled=target/magnitude;
            return magnitude*(scaled<=1?1:scaled<=2?2:scaled<=5?5:10);
        }
        public void Refresh(PrototypeActor actor)
        {
            float delta=Mathf.Max(0,Time.unscaledTime-lastRefresh);lastRefresh=Time.unscaledTime;
            float maximum=Mathf.Max(1,actor.MaximumHealth), fraction=Mathf.Clamp01(actor.Health/maximum);
            if(previous-fraction>.015f)hold=Time.unscaledTime+.25f;
            var end=trail.rectTransform.anchorMax;
            if(fraction>=end.x)end.x=fraction;
            else if(Time.unscaledTime>=hold)end.x=Mathf.MoveTowards(end.x,fraction,delta*.65f);
            trail.rectTransform.anchorMax=end;previous=fraction;
            health.rectTransform.anchorMax=new Vector2(fraction,.73f);
            health.color=fraction<.25f?new Color(.95f,.22f,.18f):new Color(.15f,.74f,.4f);
            shield.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(actor.State.Shield/maximum),1);
            healthText.text=$"{actor.Health:0} / {maximum:0}";
            shieldText.text=$"护盾 {actor.State.Shield:0.#}";
            float unit=SegmentUnit(actor.BaseMaximumHealth);
            // Bound the number of dividers even for extreme custom / growth builds.
            while(maximum/unit>12)unit*=2;
            for(int i=0;i<dividers.Length;i++)
            {
                float x=(i+1)*unit/maximum;var line=dividers[i];line.gameObject.SetActive(x<.999f);
                line.rectTransform.anchorMin=new Vector2(x,0);line.rectTransform.anchorMax=new Vector2(x,.73f);
                line.rectTransform.offsetMin=new Vector2(-1,0);line.rectTransform.offsetMax=new Vector2(1,0);
            }
            scaleText.text=$"每格 {unit:0} 生命 · 蓝层为护盾";
            statuses[0].text=$"毒 {actor.State.Poison:0.##}\n每秒穿盾";
            statuses[1].text=$"火 {actor.State.Burn:0.##}\n每半秒衰减";
            statuses[2].text=$"再生 {actor.State.Regeneration:0.##}\n生命 / 秒";
        }
    }
}
