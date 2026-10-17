using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype
{
    public sealed class PrototypeHistoryMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button open, park, close, previous, next, restore;
        [SerializeField] private Button[] rows;
        [SerializeField] private Text header, details, message;
        private PrototypeBattle battle;
        private List<PrototypeRunHistory.Record> records=new List<PrototypeRunHistory.Record>();
        private int page;
        private string selected;
        public void Build()
        {
            if(panel!=null) return;
            open=MakeButton("HistoryButton",transform,new Vector2(-700,-770),new Vector2(330,58),"历史对局");
            park=MakeButton("ParkRunButton",transform,new Vector2(-700,-840),new Vector2(330,58),"封存当前对局");
            var root=Rect("HistoryPanel",transform,new Vector2(0,-165),new Vector2(1160,790));panel=root.gameObject;
            var image=panel.AddComponent<Image>();image.color=new Color(.035f,.075f,.12f,.98f);
            header=Label("Header",root,new Vector2(0,-20),new Vector2(1020,55),32);
            close=MakeButton("CloseHistory",root,new Vector2(492,-24),new Vector2(120,48),"关闭");
            rows=new Button[4];
            for(int i=0;i<4;i++) rows[i]=MakeButton("HistoryRow"+i,root,new Vector2(0,-100-i*80),new Vector2(1060,66),"");
            previous=MakeButton("PreviousHistory",root,new Vector2(-390,-430),new Vector2(170,48),"上一页");
            next=MakeButton("NextHistory",root,new Vector2(390,-430),new Vector2(170,48),"下一页");
            details=Label("RecordDetails",root,new Vector2(0,-494),new Vector2(1060,168),22);
            message=Label("HistoryMessage",root,new Vector2(0,-668),new Vector2(1050,40),20);
            restore=MakeButton("RestoreHistory",root,new Vector2(0,-718),new Vector2(330,52),"恢复所选对局");
            panel.SetActive(false);
        }
        public void Bind(PrototypeBattle owner)
        {
            battle=owner;Build();
            BindButton(open,()=>{records=PrototypeRunHistory.List();page=0;selected=null;panel.SetActive(true);panel.transform.SetAsLastSibling();Refresh();});
            BindButton(park,()=>{try{battle.SuspendRun();}catch(Exception e){Debug.LogError("封存失败，当前对局仍保留："+e.Message);}});
            BindButton(close,()=>panel.SetActive(false));
            BindButton(previous,()=>{page=Mathf.Max(0,page-1);Refresh();});
            BindButton(next,()=>{page++;Refresh();});
            BindButton(restore,()=>{try{if(!battle.RestoreHistory(selected)){message.text="当前有进行中的对局，请先封存。";}}catch(Exception e){message.text="恢复失败："+e.Message;}});
            for(int i=0;i<rows.Length;i++){int index=i;BindButton(rows[i],()=>{int n=page*4+index;if(n<records.Count){selected=records[n].id;Refresh();}});}
            panel.SetActive(false);Refresh();
        }
        public void Refresh()
        {
            if(battle==null) return;
            park.interactable=battle.Run.HasActiveRun;
            int pages=Mathf.Max(1,(records.Count+3)/4);page=Mathf.Clamp(page,0,pages-1);
            header.text=$"历史对局  ·  {page+1}/{pages}";
            bool populated=records.Count>0;
            ((RectTransform)panel.transform).sizeDelta=new Vector2(1160,populated?790:360);
            ((RectTransform)panel.transform).anchoredPosition=new Vector2(0,populated?-165:-340);
            details.rectTransform.anchoredPosition=new Vector2(0,populated?-494:-110);
            details.rectTransform.sizeDelta=new Vector2(1060,populated?168:120);
            message.rectTransform.anchoredPosition=new Vector2(0,populated?-668:-240);
            previous.gameObject.SetActive(pages>1);next.gameObject.SetActive(pages>1);restore.gameObject.SetActive(populated);
            previous.interactable=page>0;next.interactable=page<pages-1;
            for(int i=0;i<rows.Length;i++)
            {
                int n=page*4+i;rows[i].gameObject.SetActive(n<records.Count);if(n>=records.Count)continue;
                var r=records[n];string date=DateTime.TryParse(r.savedAt,out var dt)?dt.ToLocalTime().ToString("MM-dd HH:mm"):"";
                rows[i].GetComponentInChildren<Text>().text=$"{date}   {r.status}   第{r.day}天  ·  {r.wins}胜  ·  精力{r.endurance}/10  ·  等级{r.level}";
                rows[i].GetComponent<Image>().color=r.id==selected?new Color(.16f,.36f,.43f):new Color(.08f,.16f,.22f);
            }
            var record=records.Find(r=>r.id==selected);
            restore.interactable=record!=null && !record.finished && !battle.Run.HasActiveRun;
            message.text=battle.Run.HasActiveRun?"当前有进行中的对局；先封存再恢复，避免覆盖。":record!=null && record.finished?"失败记录可以查看，不能复活恢复。":"恢复只更新当前存档，历史快照保持独立。";
            details.text=records.Count==0?"还没有历史对局。封存、失败或通关后会保留记录。":record==null?"选择一条记录查看阵容与成长。":"";
            if(record!=null)
            {
                var run=new PrototypeRun(true,true);
                if(run.ImportSnapshot(record.snapshot))
                {
                    string equipment="";foreach(var item in run.Equipped)if(item!=null)equipment+=(equipment.Length==0?"":"、")+item.Name;
                    details.text=$"本体生命 {run.BodyHealth:0.#}  ·  金币 {run.Gold}  ·  保命{(run.LastStandUsed?"已用":"可用")}\n装备：{(equipment.Length==0?"无":equipment)}\n背包 {run.Bag.Count} 件 · 待领取 {run.PendingRewards.Count} 件";
                }
                else details.text="记录无法解析，当前存档未改变。";
            }
        }
        private static void BindButton(Button b,UnityEngine.Events.UnityAction action)
        {b.onClick.RemoveAllListeners();b.onClick.AddListener(action);}
        private static RectTransform Rect(string name,Transform parent,Vector2 point,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=point;r.sizeDelta=size;return r;
        }
        private static Text Label(string name,Transform parent,Vector2 point,Vector2 size,int fontSize)
        {
            var r=Rect(name,parent,point,size);var t=r.gameObject.AddComponent<Text>();t.font=PrototypeFont.Default;
            t.fontSize=fontSize;t.color=new Color(1,.96f,.82f);t.alignment=TextAnchor.UpperLeft;t.raycastTarget=false;return t;
        }
        private static Button MakeButton(string name,Transform parent,Vector2 point,Vector2 size,string caption)
        {
            var r=Rect(name,parent,point,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.16f,.22f);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;
            var text=Label("Caption",r,Vector2.zero,size-new Vector2(20,0),24);text.text=caption;text.alignment=TextAnchor.MiddleCenter;return b;
        }
    }
}
