using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype {
public sealed partial class PrototypePreparationUI {
    private sealed class UiItemAnimation { public Image icon,glow; public float started; public PrototypeItemAnimation config; public Vector2 origin;public int lane; }
    private UiItemAnimation[] itemAnimationPool;
    private void ShowItemAnimation(PrototypeItemDefinition definition) {
        if(content==null || definition==null)return;
        var config=PrototypeItemAnimationDefaults.Resolve(definition);
        if(config.preset==PrototypeItemAnimationPreset.None)return;
        if(itemAnimationPool==null){
            itemAnimationPool=new UiItemAnimation[8];
            for(int i=0;i<itemAnimationPool.Length;i++){
                var root=new GameObject("ItemActionAnimation"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));root.transform.SetParent(content,false);
                var icon=root.GetComponent<Image>();icon.raycastTarget=false;icon.preserveAspect=true;icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(0,1);icon.rectTransform.sizeDelta=new Vector2(68,68);
                var ring=new GameObject("ActionGlow",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));ring.transform.SetParent(root.transform,false);
                var glow=ring.GetComponent<Image>();glow.raycastTarget=false;glow.preserveAspect=true;glow.sprite=PrototypeUiSkin.SpriteFor("StatusFx/shield_0");glow.rectTransform.sizeDelta=new Vector2(125,125);ring.transform.SetAsFirstSibling();root.SetActive(false);
                itemAnimationPool[i]=new UiItemAnimation {icon=icon,glow=glow,lane=i};
            }
        }
        Vector2 origin=new Vector2(450,-440);
        if(playerPortrait!=null){Vector3 center=content.InverseTransformPoint(playerPortrait.rectTransform.TransformPoint(playerPortrait.rectTransform.rect.center));origin=new Vector2(center.x-content.rect.xMin,center.y-content.rect.yMax);}
        int count=Mathf.Min(config.count,itemAnimationPool.Length);
        for(int n=0;n<count;n++){
            UiItemAnimation chosen=null;foreach(var value in itemAnimationPool)if(!value.icon.gameObject.activeSelf){chosen=value;break;}
            if(chosen==null)break;
            chosen.config=config;chosen.origin=origin;chosen.started=Time.unscaledTime;chosen.icon.sprite=PrototypeUiSkin.SpriteFor("Icons/"+definition.iconId);chosen.icon.enabled=config.showIcon && chosen.icon.sprite!=null;chosen.glow.enabled=config.intensity>0;chosen.icon.gameObject.SetActive(true);chosen.icon.transform.SetAsLastSibling();
        }
        UpdateItemAnimations();
        if(hoverPanel!=null && hoverPanel.gameObject.activeSelf)hoverPanel.SetAsLastSibling();
    }
    private void UpdateItemAnimations(){
        if(itemAnimationPool==null)return;
        foreach(var value in itemAnimationPool){
            if(!value.icon.gameObject.activeSelf)continue;
            var config=value.config;float t=(Time.unscaledTime-value.started)/config.duration;
            if(t>=1){value.icon.gameObject.SetActive(false);continue;}
            float phase=value.lane*2.4f+t*3f;Vector2 offset=new Vector2(Mathf.Cos(phase),Mathf.Sin(phase))*35;
            if(config.preset==PrototypeItemAnimationPreset.Heal)offset=new Vector2((value.lane%3-1)*22,30+t*90);
            else if(config.preset==PrototypeItemAnimationPreset.Absorb)offset*=2*(1-t);
            else if(config.preset==PrototypeItemAnimationPreset.Forge)offset=new Vector2((value.lane%3-1)*28,Mathf.Sin(t*Mathf.PI)*70);
            else if(config.preset==PrototypeItemAnimationPreset.Growth || config.preset==PrototypeItemAnimationPreset.Shield)offset=Vector2.zero;
            else if(config.preset!=PrototypeItemAnimationPreset.Orbit)offset*=1+t;
            value.icon.rectTransform.anchoredPosition=value.origin+offset;
            value.icon.transform.localRotation=Quaternion.Euler(0,0,config.rotation+(config.preset==PrototypeItemAnimationPreset.Absorb?t*180:0));
            float scale=config.scale*(.7f+Mathf.Sin(t*Mathf.PI)*.45f);value.icon.transform.localScale=Vector3.one*Mathf.Min(scale,3);
            var color=config.tint;color.a*=Mathf.Min(1,(1-t)*3);value.icon.color=color;
            value.glow.rectTransform.localScale=Vector3.one*(.6f+t*1.3f);color.a*=Mathf.Clamp01(config.intensity)*.45f;value.glow.color=color;
        }
    }
}}