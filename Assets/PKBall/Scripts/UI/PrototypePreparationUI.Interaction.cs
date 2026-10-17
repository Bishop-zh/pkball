using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype {
public sealed partial class PrototypePreparationUI {
private PrototypeRun feedbackRun;private Text feedbackText;private float feedbackUntil;
private void BindInteractionFeedback(){if(feedbackRun!=null){feedbackRun.Feedback-=ShowFeedback;feedbackRun.ItemAnimationFeedback-=ShowItemAnimation;}feedbackRun=run;feedbackRun.Feedback+=ShowFeedback;feedbackRun.ItemAnimationFeedback+=ShowItemAnimation;}
private void ShowFeedback(string text){if(feedbackText==null){feedbackText=Label("ActionFeedback",content,"",26,new Vector2(320,-300),new Vector2(450,160),TextAnchor.MiddleCenter);feedbackText.color=new Color(1,.89f,.47f);}
feedbackText.text=text;feedbackUntil=Time.unscaledTime+2.5f;feedbackText.gameObject.SetActive(true);feedbackText.transform.SetAsLastSibling();}
private void UpdateFeedback(){UpdateItemAnimations();if(feedbackText==null)return;float remaining=feedbackUntil-Time.unscaledTime;feedbackText.gameObject.SetActive(remaining>0);if(remaining>0){var c=feedbackText.color;c.a=Mathf.Clamp01(remaining);feedbackText.color=c;feedbackText.rectTransform.anchoredPosition=new Vector2(320,-300-(2.5f-remaining)*20);}}
private void FitTooltip(){
hoverPanel.SetAsLastSibling();hoverText.supportRichText=true;hoverText.fontSize=22;
hoverText.horizontalOverflow=HorizontalWrapMode.Wrap;hoverText.verticalOverflow=VerticalWrapMode.Overflow;
hoverText.rectTransform.anchoredPosition=new Vector2(24,-22);
hoverText.rectTransform.sizeDelta=new Vector2(432,920);
float height=Mathf.Clamp(hoverText.preferredHeight+48,280,980);
hoverPanel.sizeDelta=new Vector2(480,height);hoverText.rectTransform.sizeDelta=new Vector2(432,height-44);
hoverPanel.GetComponent<Image>().color=new Color(.27f,.18f,.14f,.98f);
var border=hoverPanel.GetComponent<Outline>()??hoverPanel.gameObject.AddComponent<Outline>();border.effectColor=new Color(.7f,.6f,.48f,.9f);border.effectDistance=new Vector2(2,-2);
}
public bool HasEquippedItem(int index)=>index>=0&&index<run.Equipped.Count&&run.Equipped[index]!=null;
public void DropEquipped(int slot,PrototypeDropKind kind,int target){if(!HasEquippedItem(slot))return;
if(kind==PrototypeDropKind.Shop)run.SellEquipped(slot);else if(kind==PrototypeDropKind.Bag)run.Unequip(slot);
else if(kind==PrototypeDropKind.Slot)run.MoveEquipped(slot,target);
selectedBag=-1;Refresh();}
private void EnsureDragTargets(){foreach(var button in slotButtons){var drag=button.GetComponent<PrototypeBagDrag>()??button.gameObject.AddComponent<PrototypeBagDrag>();drag.Initialize(this,System.Array.IndexOf(slotButtons,button),false,true);}
var bag=bagGroup.Find("BagPanel");if(bag!=null){var d=bag.GetComponent<PrototypeItemDrop>()??bag.gameObject.AddComponent<PrototypeItemDrop>();d.Initialize(this,PrototypeDropKind.Bag,-1);}
var shop=shopGroup.Find("ShopPanel");if(shop!=null){var d=shop.GetComponent<PrototypeItemDrop>()??shop.gameObject.AddComponent<PrototypeItemDrop>();d.Initialize(this,PrototypeDropKind.Shop,-1);}
foreach(var button in shopButtons){var d=button.GetComponent<PrototypeItemDrop>()??button.gameObject.AddComponent<PrototypeItemDrop>();d.Initialize(this,PrototypeDropKind.Shop,-1);}}
}}
