using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype
{
    // Cosmetic pools cannot discard or delay authoritative gameplay events.
    public sealed class PrototypeCombatPresentation : MonoBehaviour
    {
        private sealed class ActorVisual
        {
            public PrototypeActor actor; public SpriteRenderer portrait; public Color original;
            public SpriteRenderer shield; public SpriteRenderer[] heals; public SpriteRenderer[] flames;
            public float healUntil;public Vector3 portraitScale;
        }
        private sealed class HitFlash {public LineRenderer line;public Vector3 position;public PrototypeHitVisual style;public float start;}
        private static readonly AnimationCurve CrescentWidth=new AnimationCurve(new Keyframe(0,.02f),new Keyframe(.35f,.85f),new Keyframe(.6f,1),new Keyframe(1,.02f));
        private static readonly AnimationCurve UniformWidth=AnimationCurve.Linear(0,1,1,1);
        private readonly List<HitFlash> hits=new List<HitFlash>(24);
        private sealed class Flash
        {
            public GameObject root; public SpriteRenderer sprite; public LineRenderer line,halo,core; public GameObject model, modelSource;
            public PrototypeActor source,target; public PrototypeWeaponDefinition weapon;
            public float started,duration; public int index,total;public string itemId; public PrototypeItemAnimation animation;
        }
        private sealed class NumberVisual {public Text text;public Vector3 origin;public float start,amount;public int actorId;public PrototypeDamageKind kind;}
        private struct PendingNumber {public PrototypeActor actor;public PrototypeDamageKind kind;public float amount;}
        private PrototypeAudioPlayback audioPlayback;
        private PrototypeBattle battle;
        private readonly List<ActorVisual> actors=new List<ActorVisual>(2);
        private readonly List<Flash> flashes=new List<Flash>(32);
        private readonly List<NumberVisual> numbers=new List<NumberVisual>(48);
        private readonly Dictionary<int,PendingNumber> pending=new Dictionary<int,PendingNumber>(16);
        private Sprite[] burnFrames,shieldFrames;
        private float nextFlush;
        private RectTransform numberViewport,numberRoot;
        public void BindNumberViewport(RectTransform viewport,Font font)
        {
            if(numberViewport==viewport)return;
            numberViewport=viewport;
            var oldRoot=numbers[0].text.transform.parent.gameObject;
            numberRoot=new GameObject("ScreenCombatNumbers",typeof(RectTransform)).GetComponent<RectTransform>();
            numberRoot.SetParent(viewport,false);numberRoot.anchorMin=Vector2.zero;numberRoot.anchorMax=Vector2.one;
            numberRoot.offsetMin=numberRoot.offsetMax=Vector2.zero;
            foreach(var n in numbers){n.text.transform.SetParent(numberRoot,false);n.text.font=font;n.text.gameObject.SetActive(false);}
            ReleaseObject(oldRoot);
        }
        private Material lineMaterial;
        private Material LineMaterial {get{if(lineMaterial==null){lineMaterial=new Material(Shader.Find("Sprites/Default"));lineMaterial.hideFlags=HideFlags.DontSave;}return lineMaterial;}}
        private void OnDestroy(){if(numberRoot!=null)ReleaseObject(numberRoot.gameObject);if(lineMaterial!=null){if(Application.isPlaying)Destroy(lineMaterial);else DestroyImmediate(lineMaterial);}}
        public void Initialize(PrototypeBattle owner)
        {
            battle=owner;audioPlayback=gameObject.AddComponent<PrototypeAudioPlayback>();audioPlayback.Initialize();
            burnFrames=new[]{PrototypeUiSkin.SpriteFor("StatusFx/burn_0"),PrototypeUiSkin.SpriteFor("StatusFx/burn_1"),PrototypeUiSkin.SpriteFor("StatusFx/burn_2"),PrototypeUiSkin.SpriteFor("StatusFx/burn_3")};
            shieldFrames=new[]{PrototypeUiSkin.SpriteFor("StatusFx/shield_0"),PrototypeUiSkin.SpriteFor("StatusFx/shield_1"),PrototypeUiSkin.SpriteFor("StatusFx/shield_2"),PrototypeUiSkin.SpriteFor("StatusFx/shield_3")};
            for(int i=0;i<24;i++){var go=new GameObject("PooledHit_"+i);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=LineMaterial;line.useWorldSpace=true;line.loop=true;line.positionCount=16;line.sortingOrder=25;go.SetActive(false);hits.Add(new HitFlash {line=line});}
            var font=PrototypeFont.Default;
            var root=new GameObject("PooledCombatNumbers",typeof(RectTransform),typeof(Canvas)); root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.WorldSpace; canvas.sortingOrder=30;
            root.transform.rotation=Quaternion.Euler(90,0,0); root.transform.position=new Vector3(0,1.6f,0); root.transform.localScale=Vector3.one*.01f;
            for(int i=0;i<48;i++)
            {
                var go=new GameObject("CombatNumber_"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)); go.transform.SetParent(root.transform,false);
                var text=go.GetComponent<Text>(); text.font=font; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
                text.horizontalOverflow=HorizontalWrapMode.Overflow; text.rectTransform.sizeDelta=new Vector2(250,64);text.fontStyle=FontStyle.Bold;
                var outline=go.AddComponent<Outline>();outline.effectColor=new Color(.015f,.025f,.04f,.95f);outline.effectDistance=new Vector2(2,-2);
                go.SetActive(false); numbers.Add(new NumberVisual {text=text});
            }
            for(int i=0;i<32;i++)
            {
                var go=new GameObject("PooledActivation_"+i); go.transform.SetParent(transform,false);
                var sprite=go.AddComponent<SpriteRenderer>(); sprite.sortingOrder=20; go.SetActive(false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=LineMaterial;line.useWorldSpace=true;line.enabled=false;line.sortingOrder=21;line.numCapVertices=4;
                var halo=CreateCrescentLayer(go,"CrescentGlow",19);var core=CreateCrescentLayer(go,"CrescentCore",23);
                flashes.Add(new Flash {root=go,sprite=sprite,line=line,halo=halo,core=core});
            }
        }
        private static void ReleaseObject(GameObject value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private LineRenderer CreateCrescentLayer(GameObject root,string name,int order){var go=new GameObject(name);go.transform.SetParent(root.transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=LineMaterial;line.useWorldSpace=true;line.sortingOrder=order;line.positionCount=48;line.enabled=false;line.widthCurve=new AnimationCurve(new Keyframe(0,.03f),new Keyframe(.35f,.85f),new Keyframe(.6f,1),new Keyframe(1,.03f));return line;}
        public void BindActors(PrototypeActor player,PrototypeActor enemy)
        {
            foreach(var a in actors)
            { if(a.shield!=null) ReleaseObject(a.shield.gameObject); foreach(var e in a.heals) if(e!=null) ReleaseObject(e.gameObject); foreach(var e in a.flames) if(e!=null) ReleaseObject(e.gameObject); }
            actors.Clear(); Bind(player); Bind(enemy);
        }
        private void Bind(PrototypeActor actor)
        {
            var portrait=actor.transform.Find("ReplaceableCharacterBitmap")?.GetComponent<SpriteRenderer>();
            var go=new GameObject("ShieldShell"); go.transform.SetParent(actor.transform,false);
            var ring=go.AddComponent<SpriteRenderer>();ring.sprite=shieldFrames[0];ring.sortingOrder=18;ring.enabled=false;
            go.transform.localRotation=Quaternion.Euler(90,0,0);go.transform.localPosition=Vector3.up*1.12f;
            var heals=new SpriteRenderer[4];var flames=new SpriteRenderer[4];var sprite=PrototypeUiSkin.SpriteFor("BattleArt/UI/status_heal");
            for(int i=0;i<4;i++)
            {
                var particle=new GameObject("HealParticle_"+i); particle.transform.SetParent(actor.transform,false);
                heals[i]=particle.AddComponent<SpriteRenderer>(); heals[i].sprite=sprite; heals[i].sortingOrder=15; heals[i].enabled=false;
                particle.transform.localRotation=Quaternion.Euler(90,0,0);
                var flame=new GameObject("BurnOverlay_"+i);flame.transform.SetParent(actor.transform,false);
                flames[i]=flame.AddComponent<SpriteRenderer>();flames[i].sprite=burnFrames[0];flames[i].sortingOrder=17;flames[i].enabled=false;
                flame.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            actors.Add(new ActorVisual {actor=actor,portrait=portrait,original=portrait == null ? Color.white : portrait.color,portraitScale=portrait==null?Vector3.one:portrait.transform.localScale,shield=ring,heals=heals,flames=flames});
        }
        public void ShowHit(PrototypeActor target,PrototypeHitVisual style)
        {
            if(style==PrototypeHitVisual.None||target==null)return;foreach(var hit in hits)if(!hit.line.gameObject.activeSelf){hit.position=target.transform.position+Vector3.up*1.25f;hit.style=style;hit.start=battle.Elapsed;hit.line.gameObject.SetActive(true);return;}
        }
        public void PlayHit(string id,float volume){audioPlayback.Play(id,volume);}
        public void Number(PrototypeActor actor,float amount,PrototypeDamageKind kind)
        {
            // Ordinary hit types share one visual lane; authoritative damage records keep their original kinds.
            if(kind==PrototypeDamageKind.Impact||kind==PrototypeDamageKind.Ranged)kind=PrototypeDamageKind.Melee;
            if(amount<=0) return; int key=actor.Id*100+(int)kind;
            pending.TryGetValue(key,out var value); value.actor=actor; value.kind=kind; value.amount+=amount; pending[key]=value;
        }
        public void Pulse(PrototypeActor actor,PrototypeDamageKind kind)
        { foreach(var a in actors) if(a.actor==actor && kind==PrototypeDamageKind.Heal) a.healUntil=battle.Elapsed+.6f; }
        public void ShowActivation(PrototypeActor source,PrototypeActor target,PrototypeItemDefinition definition,int level)
        {
            var animation=PrototypeItemAnimationDefaults.Resolve(definition);
            audioPlayback.Play(PrototypeAudioLibrary.ResolveRelease(definition),definition.releaseVolume);
            if(animation.preset==PrototypeItemAnimationPreset.None)return;
            int total=animation.count;
            for(int index=0;index<total;index++) {
            Flash chosen=null; foreach(var f in flashes) if(!f.root.activeSelf) {chosen=f;break;} if(chosen==null) return;
            chosen.animation=animation;chosen.itemId=definition.itemId;chosen.index=index;chosen.total=total;chosen.source=source; chosen.target=target; chosen.weapon=definition.weapon; chosen.started=battle.Elapsed;
            chosen.duration=animation.duration;
            chosen.sprite.sprite=PrototypeUiSkin.SpriteFor("Icons/"+definition.iconId);
            GameObject prefab=null;
            if(definition.weapon!=null) prefab=level>=4 && definition.weapon.legendaryModelPrefab!=null ? definition.weapon.legendaryModelPrefab : definition.weapon.modelPrefab;
            if(prefab!=chosen.modelSource)
            {
                if(chosen.model!=null) Destroy(chosen.model);
                chosen.model = prefab == null ? null : Instantiate(prefab,chosen.root.transform); chosen.modelSource=prefab;
                if(chosen.model!=null) foreach(var collider in chosen.model.GetComponentsInChildren<Collider>()) collider.enabled=false;
            }
            chosen.sprite.enabled=animation.showIcon && prefab==null && chosen.sprite.sprite!=null;
            if(chosen.model!=null) {chosen.model.SetActive(true); chosen.model.transform.localScale=definition.weapon.modelScale;}
            bool specialTrail=animation.preset==PrototypeItemAnimationPreset.Crescent || animation.preset==PrototypeItemAnimationPreset.Growth;
            bool lineStyle=definition.weapon!=null && (definition.weapon.visualStyle==PrototypeAttackVisual.Beam || definition.weapon.visualStyle==PrototypeAttackVisual.Shockwave);
            chosen.halo.enabled=chosen.core.enabled=animation.preset==PrototypeItemAnimationPreset.Crescent;chosen.line.widthCurve=animation.preset==PrototypeItemAnimationPreset.Crescent?CrescentWidth:UniformWidth;
            chosen.line.enabled=lineStyle || specialTrail;
            chosen.line.enabled=true;chosen.root.SetActive(true); Animate(chosen);
            }
        }
        private static Color ColorFor(PrototypeDamageKind kind)
        {
            switch(kind)
            {
                case PrototypeDamageKind.Poison:return new Color(.45f,1,.25f);
                case PrototypeDamageKind.Burn:return new Color(1,.42f,.15f);
                case PrototypeDamageKind.Heal:return new Color(.3f,1,.62f);
                case PrototypeDamageKind.Shield:
                case PrototypeDamageKind.ShieldAbsorbed:return new Color(.35f,.83f,1);
                case PrototypeDamageKind.SuddenDeath:return new Color(1,.3f,.28f);
                default:return new Color(1,.93f,.82f);
            }
        }
        private void Update()
        {
            if(battle==null) return; float now=battle.Elapsed;
            foreach(var hit in hits)
            {
                if(!hit.line.gameObject.activeSelf)continue;float t=(now-hit.start)/.4f;if(t>=1||!battle.IsFighting){hit.line.gameObject.SetActive(false);continue;}
                Color color=hit.style==PrototypeHitVisual.Poison?new Color(.3f,1,.25f):hit.style==PrototypeHitVisual.Burn?new Color(1,.3f,.08f):hit.style==PrototypeHitVisual.Frost?new Color(.3f,.9f,1):new Color(1,.85f,.3f);color.a=1-t;
                hit.line.startColor=hit.line.endColor=color;hit.line.startWidth=hit.line.endWidth=.04f*(1-t);
                for(int i=0;i<16;i++){float angle=i*Mathf.PI*2/16;float radius=(.12f+t*.9f)*(hit.style==PrototypeHitVisual.Shockwave?1:i%2==0?1:.45f);hit.line.SetPosition(i,hit.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));}
            }
            foreach(var a in actors)
            {
                if(a.actor==null) continue; var state=a.actor.State;
                if(a.portrait!=null)
                {
                    a.portrait.transform.rotation=Quaternion.Euler(90,0,0);
                    a.portrait.transform.localScale=Vector3.Lerp(a.portrait.transform.localScale,a.portraitScale*a.actor.SlimeVisualScale,Mathf.Clamp01(Time.deltaTime*9));
                    a.portrait.color = a.actor.IsInvulnerable ? new Color(.55f,.85f,1,.65f) : state.Poison>0 ? Color.Lerp(a.original,new Color(.24f,.63f,.30f),.55f) : a.original;
                }
                a.shield.enabled=(state.Shield>0 || a.actor.IsInvulnerable) && a.actor.IsAlive;
                a.shield.sprite=shieldFrames[(int)(now*4)%shieldFrames.Length];
                a.shield.color=new Color(1,1,1,Mathf.Clamp(.5f+state.Shield/400,.5f,.9f));
                float diameter=a.actor.Radius*2*1.65f;
                if(a.shield.sprite!=null)a.shield.transform.localScale=Vector3.one*(diameter/a.shield.sprite.bounds.size.x);
                for(int i=0;i<4;i++)
                {
                    float phase=(now*1.8f+i*.25f)%1f;
                    var heal=a.heals[i]; heal.enabled=a.healUntil>now && a.actor.IsAlive; heal.color=new Color(.3f,1,.62f,1-phase);
                    heal.transform.localPosition=new Vector3((i-1.5f)*.23f,1.05f,.65f+phase*.45f);
                    heal.transform.localScale=Vector3.one*(.025f+.012f*(1-phase));
                    var flame=a.flames[i]; flame.enabled=i==0 && state.Burn>0 && a.actor.IsAlive;
                    flame.sprite=burnFrames[(int)(now*6)%burnFrames.Length];flame.color=new Color(1,1,1,.7f);
                    flame.transform.localPosition=new Vector3(0,1.10f,-.04f);
                    if(flame.sprite!=null)flame.transform.localScale=Vector3.one*(diameter/flame.sprite.bounds.size.x);
                }
            }
            foreach(var f in flashes)
            {
                if(!f.root.activeSelf) continue;
                if(!battle.IsFighting || now-f.started>=f.duration) {f.root.SetActive(false);continue;} Animate(f);
            }
            float numberTime=Time.unscaledTime;
            if(numberTime>=nextFlush || !battle.IsFighting)
            {
                nextFlush=numberTime+.12f;
                foreach(var pair in pending)
                {
                    var value=pair.Value;if(value.actor==null) continue;
                    NumberVisual free=null;
                    foreach(var n in numbers) if(n.text.gameObject.activeSelf && n.actorId==value.actor.Id && n.kind==value.kind) {free=n;break;}
                    if(free==null) foreach(var n in numbers) if(!n.text.gameObject.activeSelf) {free=n;break;}
                    if(free==null) break;
                    bool merge=free.text.gameObject.activeSelf && free.actorId==value.actor.Id && free.kind==value.kind && numberTime-free.start<.55f;
                    if(!merge)
                    {
                        int lane=value.kind==PrototypeDamageKind.Poison?1:value.kind==PrototypeDamageKind.Burn?3:
                            value.kind==PrototypeDamageKind.Heal?2:value.kind==PrototypeDamageKind.Shield?4:value.kind==PrototypeDamageKind.ShieldAbsorbed?5:
                            value.kind==PrototypeDamageKind.SuddenDeath?6:value.kind==PrototypeDamageKind.Exhaustion?7:0;
                        free.origin=value.actor.transform.position+Vector3.up*1.6f+Vector3.forward*(value.actor.Radius+.12f+lane/2*.34f)+Vector3.right*(lane%2==0?-.45f:.45f);
                        if(numberViewport!=null)
                        {
                            Vector3 point=battle.ArenaCamera.WorldToViewportPoint(value.actor.transform.position);
                            Rect rect=numberViewport.rect;
                            // Clamp the whole stack before offsetting lanes, so rows cannot collapse at an arena edge.
                            float x=Mathf.Clamp((point.x-.5f)*rect.width,rect.xMin+157,rect.xMax-157)+(lane%2==0?-67:67);
                            float y=Mathf.Clamp((point.y-.5f)*rect.height+72,rect.yMin+40,rect.yMax-190)+lane/2*45;
                            free.origin=new Vector3(x,y,0);
                        }
                        free.start=numberTime;free.amount=0;free.actorId=value.actor.Id;free.kind=value.kind;
                    }
                    free.amount+=value.amount;
                    string label=value.kind==PrototypeDamageKind.Heal?"治疗 +":value.kind==PrototypeDamageKind.Shield?"护盾 +":value.kind==PrototypeDamageKind.ShieldAbsorbed?"吸收 −":value.kind==PrototypeDamageKind.Poison?"毒 −":value.kind==PrototypeDamageKind.Burn?"火 −":value.kind==PrototypeDamageKind.SuddenDeath?"耗损 −":"−";
                    free.text.text=label+free.amount.ToString(free.amount<10?"0.##":"0.#");
                    free.text.fontSize=Mathf.RoundToInt(Mathf.Clamp(32+Mathf.Log(1+free.amount)*2,32,44));free.text.color=ColorFor(value.kind);free.text.gameObject.SetActive(true);
                }
                pending.Clear();
            }
            foreach(var n in numbers)
            {
                if(!n.text.gameObject.activeSelf) continue; float t=(numberTime-n.start)/1.15f;
                if(t>=1) {n.text.gameObject.SetActive(false);continue;}
                if(numberViewport!=null)n.text.rectTransform.anchoredPosition=(Vector2)n.origin+Vector2.up*t*32;
                else n.text.transform.position=n.origin+Vector3.forward*t*.6f;
                var c=n.text.color;c.a=t<.55f?1:(1-t)/.45f;n.text.color=c;
            }
        }
        private void Animate(Flash f)
        {
            if(f.source==null){f.root.SetActive(false);return;}
            if(!(f.animation.preset==PrototypeItemAnimationPreset.Crescent || f.animation.preset==PrototypeItemAnimationPreset.Growth)){AnimateConfigured(f);return;}
            float t=Mathf.Clamp01((battle.Elapsed-f.started)/f.duration); var source=f.source.transform.position;
            var toward = f.target == null ? Vector3.forward : f.target.transform.position-source; toward.y=0; toward.Normalize();
            float angle=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg+f.animation.rotation;
            float visualScale=(f.weapon==null?1:f.weapon.visualScale)*f.animation.scale;
            float length=f.weapon==null?.65f:f.weapon.visualSize; var position=source+toward*(f.source.Radius+.22f);
            if(f.weapon!=null && f.weapon.kind==PrototypeWeaponKind.Melee)
            {
                length=f.weapon.visualSize; position=source+toward*(f.source.Radius+f.weapon.range*.5f);
                if(f.weapon.attackShape==PrototypeAttackShape.Thrust) position+=toward*(Mathf.Sin(t*Mathf.PI)*.25f);
                else {float windup=Mathf.Clamp01(f.weapon.hitDelay/f.duration);float swing=Mathf.Clamp01(t/Mathf.Max(.01f,windup*2));angle+=Mathf.Lerp(-100,100,swing);if(f.animation.preset==PrototypeItemAnimationPreset.Crescent)position=source+toward*Mathf.Min(2.4f,f.weapon.range*.3f);}
            }
            else position+=toward*Mathf.Sin(t*Mathf.PI)*.12f;
            if(f.weapon!=null && f.weapon.visualStyle==PrototypeAttackVisual.Volley && f.target!=null)
            {
                float phase=(f.index/(float)f.total-.5f)*Mathf.PI;var start=source+new Vector3(Mathf.Sin(phase),0,Mathf.Cos(phase))*(f.source.Radius+1.1f);
                float flight=Mathf.Clamp01((t-.15f-f.index*.01f)/.7f);position=Vector3.Lerp(start,f.target.transform.position,flight);length=f.weapon.visualSize;
                var direction=f.target.transform.position-position;angle=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            }
            if(f.weapon!=null && f.weapon.visualStyle==PrototypeAttackVisual.Shockwave){position=source;length=(.5f+t*3)*visualScale;}
            f.root.transform.position=position+Vector3.up*1.1f; f.root.transform.rotation=Quaternion.Euler(90,angle,0);
            if(f.model!=null) {f.root.transform.rotation=Quaternion.Euler(0,angle,0); f.root.transform.localScale=Vector3.one*visualScale*f.weapon.visualSize;}
            else if(f.sprite.sprite!=null) f.root.transform.localScale=Vector3.one*(visualScale*length/Mathf.Max(f.sprite.sprite.bounds.size.x,f.sprite.sprite.bounds.size.y));
            if(f.weapon!=null && f.weapon.visualStyle==PrototypeAttackVisual.Beam && f.target!=null && f.sprite.sprite!=null)
            {
                var end=f.target.transform.position;f.root.transform.position=(source+end)*.5f+Vector3.up*1.1f;
                f.root.transform.localScale=new Vector3(.12f*visualScale,Vector3.Distance(source,end)/f.sprite.sprite.bounds.size.y,1);
            }
            if(f.line.enabled && (f.animation.preset==PrototypeItemAnimationPreset.Crescent || f.animation.preset==PrototypeItemAnimationPreset.Growth))
            {
                bool scythe=f.animation.preset==PrototypeItemAnimationPreset.Crescent;float windup=scythe?Mathf.Clamp01((f.weapon?.hitDelay??.3f)/f.duration):.1f;
                float swing=Mathf.Clamp01(t/Mathf.Max(.01f,windup*2));
                f.line.loop=!scythe;f.line.positionCount=48;f.line.startWidth=scythe?.24f:.10f;f.line.endWidth=scythe?.04f:.10f;
                Color glow=scythe?new Color(.82f,.52f,1,Mathf.Min(1,(1-t)*3)):new Color(.35f,1,.5f,1-t);f.line.startColor=glow;f.line.endColor=scythe?new Color(1,1,1,glow.a):glow;
                float distance=f.target==null?4:Vector3.Distance(source,f.target.transform.position);
                float reach=(scythe?Mathf.Clamp(distance*.55f,1.6f,3.4f):f.source.Radius*f.source.SlimeVisualScale*(1+t*2))*f.animation.scale;
                if(f.animation.tint!=Color.white){glow=f.animation.tint;glow.a*=Mathf.Min(1,(1-t)*3);f.line.startColor=f.line.endColor=glow;}
                f.line.widthMultiplier=f.animation.intensity;
                var center=scythe?source+toward*(distance*.45f):source;
                float forwardAngle=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg+f.animation.rotation;
                if(scythe){f.line.startWidth=f.line.endWidth=.32f*f.animation.intensity;f.halo.widthMultiplier=.85f*f.animation.intensity;f.core.widthMultiplier=.11f*f.animation.intensity;var haloTint=f.animation.tint==Color.white?new Color(.65f,.25f,1,glow.a*.28f):new Color(glow.r,glow.g,glow.b,glow.a*.28f);f.halo.startColor=f.halo.endColor=haloTint;f.core.startColor=f.core.endColor=new Color(1,.93f,1,glow.a);}
                for(int i=0;i<48;i++){float arc=scythe?(forwardAngle-70+140*(i/47f)*Mathf.Max(.08f,swing))*Mathf.Deg2Rad:i*Mathf.PI*2/48;var point=center+new Vector3(Mathf.Sin(arc)*reach,1.2f,Mathf.Cos(arc)*reach);f.line.SetPosition(i,point);if(scythe){f.halo.SetPosition(i,point);f.core.SetPosition(i,point+Vector3.up*.02f);}}

            }
            else if(f.line.enabled)
            {
                bool beam=f.weapon.visualStyle==PrototypeAttackVisual.Beam;f.line.loop=!beam;f.line.positionCount=beam?2:40;f.line.startWidth=f.line.endWidth=(beam?.14f:.065f)*f.weapon.effectScale;
                var lineColor=beam?new Color(.3f,.9f,1,1-t):new Color(1,.72f,.3f,1-t);f.line.startColor=f.line.endColor=lineColor;
                if(beam){f.line.SetPosition(0,source+Vector3.up*1.15f);f.line.SetPosition(1,(f.target==null?source+toward*3:f.target.transform.position)+Vector3.up*1.15f);}
                else for(int i=0;i<40;i++){float theta=i*Mathf.PI*2/40;float radius=(.3f+t*2.8f)*f.weapon.effectScale;f.line.SetPosition(i,source+new Vector3(Mathf.Cos(theta)*radius,1.1f,Mathf.Sin(theta)*radius));}
            }
            var c=f.animation.tint; c.a*=Mathf.Min(1,(1-t)*4); f.sprite.color=c;
        }

        private void AnimateConfigured(Flash f)
        {
            var a=f.animation;float t=Mathf.Clamp01((battle.Elapsed-f.started)/f.duration);
            float pulse=Mathf.Sin(t*Mathf.PI);var source=f.source.transform.position;
            var target=f.target==null?source+Vector3.forward*2:f.target.transform.position;
            var direction=target-source;direction.y=0;if(direction.sqrMagnitude<.001f)direction=Vector3.forward;direction.Normalize();
            var side=new Vector3(direction.z,0,-direction.x);
            float phase=f.index*Mathf.PI*2/Mathf.Max(1,f.total)+a.rotation*Mathf.Deg2Rad;
            float angle=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            float radius=(f.source.Radius+.4f)*a.scale;
            Vector3 center=source,icon=source+direction*radius,trailOrigin=source;
            bool arc=false,ray=false;float reach=radius*(.4f+t*1.5f);float spin=phase;
            switch(a.preset)
            {
                case PrototypeItemAnimationPreset.Sweep: arc=true;spin+=Mathf.Lerp(-1.4f,1.4f,t);icon=source+direction*Mathf.Cos(spin)*radius+side*Mathf.Sin(spin)*radius;angle+=spin*Mathf.Rad2Deg;break;
                case PrototypeItemAnimationPreset.Thrust: ray=true;icon=source+direction*(radius+pulse*a.scale);break;
                case PrototypeItemAnimationPreset.Smash: center=source+direction*radius;icon=center+Vector3.forward*(1-t)*a.scale;reach=radius*(.15f+Mathf.Max(0,t-.35f)*2);angle+=Mathf.Lerp(-80,30,t);break;
                case PrototypeItemAnimationPreset.Recoil: ray=true;icon=source+direction*radius-direction*pulse*.35f*a.scale;break;
                case PrototypeItemAnimationPreset.Orbit: spin+=t*Mathf.PI*3;icon=source+(direction*Mathf.Cos(spin)+side*Mathf.Sin(spin))*radius;reach=radius;angle+=spin*Mathf.Rad2Deg;break;
                case PrototypeItemAnimationPreset.Shield: reach=radius*(.9f+pulse*.2f);icon=source+side*radius;break;
                case PrototypeItemAnimationPreset.Heal: icon=source+side*Mathf.Sin(phase)*radius+Vector3.forward*t*a.scale;reach=radius*(.3f+t*.5f);break;
                case PrototypeItemAnimationPreset.Poison: center=target;icon=target+side*Mathf.Sin(phase+t*7)*radius*.3f;reach=radius*(.3f+t);break;
                case PrototypeItemAnimationPreset.Flame: center=target;icon=target+Vector3.forward*pulse*a.scale*.6f;reach=radius*(.25f+pulse*.7f);break;
                case PrototypeItemAnimationPreset.Forge: icon=source+side*Mathf.Sin(t*12)*.12f*a.scale;angle+=Mathf.Sin(t*10)*25;reach=radius*(.2f+pulse);break;
                case PrototypeItemAnimationPreset.Absorb: icon=Vector3.Lerp(target,source,t);reach=radius*(1-t);break;
                default: icon=source+side*Mathf.Cos(phase)*radius*.25f;break;
            }
            bool beam=f.weapon!=null && f.weapon.visualStyle==PrototypeAttackVisual.Beam && a.preset==PrototypeItemAnimationPreset.Recoil;
            bool volley=f.weapon!=null && f.weapon.visualStyle==PrototypeAttackVisual.Volley && a.preset==PrototypeItemAnimationPreset.Orbit;
            if(beam){ray=true;icon=source+direction*radius;}
            if(volley)
            {
                float spread=(f.index/(float)Mathf.Max(1,f.total)-.5f)*Mathf.PI;
                trailOrigin=source+(direction*Mathf.Cos(spread)+side*Mathf.Sin(spread))*(f.source.Radius+1.1f)*a.scale;
                float flight=Mathf.Clamp01((t-.12f-f.index*.007f)/.72f);icon=Vector3.Lerp(trailOrigin,target,flight);
                var heading=target-icon;if(heading.sqrMagnitude>.001f)angle=Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg;
                ray=true;
            }
            f.root.transform.position=icon+Vector3.up*1.15f;
            f.root.transform.rotation=Quaternion.Euler(f.model==null?90:0,angle+a.rotation,0);
            float size=(f.weapon==null?.85f:f.weapon.visualSize)*a.scale*(.75f+.25f*pulse);
            f.root.transform.localScale=Vector3.one*(f.model!=null || f.sprite.sprite==null?size:size/Mathf.Max(.01f,Mathf.Max(f.sprite.sprite.bounds.size.x,f.sprite.sprite.bounds.size.y)));
            var tint=a.tint;tint.a*=Mathf.Min(1,(1-t)*3);f.sprite.color=tint;
            f.line.loop=!arc&&!ray;f.line.widthCurve=arc?CrescentWidth:UniformWidth;
            f.line.positionCount=ray?3:32;f.line.startWidth=f.line.endWidth=(.035f+.035f*pulse)*a.intensity*a.scale;
            f.line.startColor=f.line.endColor=tint;
            if(ray)
            {
                if(beam){f.line.startWidth=f.line.endWidth=.14f*a.intensity*a.scale;f.line.SetPosition(0,source+Vector3.up*1.2f);f.line.SetPosition(1,Vector3.Lerp(source,target,.5f)+Vector3.up*1.2f);f.line.SetPosition(2,target+Vector3.up*1.2f);}
                else {f.line.SetPosition(0,(volley?Vector3.Lerp(trailOrigin,icon,.65f):trailOrigin)+Vector3.up*1.2f);f.line.SetPosition(1,icon+Vector3.up*1.2f);f.line.SetPosition(2,icon+direction*.45f*a.scale+Vector3.up*1.2f);}
            }
            else for(int i=0;i<32;i++)
            {
                float theta=arc?spin+Mathf.Lerp(-1,1,i/31f):i*Mathf.PI*2/32+phase;
                float r=reach;
                if(a.preset==PrototypeItemAnimationPreset.Burst || a.preset==PrototypeItemAnimationPreset.Forge)r*=i%2==0?1:.55f;
                if(a.preset==PrototypeItemAnimationPreset.Poison || a.preset==PrototypeItemAnimationPreset.Flame)r*=1+.15f*Mathf.Sin(theta*5+t*12);
                f.line.SetPosition(i,center+new Vector3(Mathf.Sin(theta)*r,1.2f,Mathf.Cos(theta)*r));
            }
        }

    }
}
