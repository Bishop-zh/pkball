using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeCombatEffectsUiMigration
    {
        private const string Path="Assets/PKBall/Prefabs/Screens/PrototypeBattle.prefab";
        private static bool quitting;
        static PrototypeCombatEffectsUiMigration()
        {
            EditorApplication.quitting+=()=>quitting=true;
            EditorApplication.delayCall+=ApplyWhenIdle;
        }
        private static Text TextElement(string name,Transform parent,Font font,Vector2 pos,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);
            var t=go.GetComponent<Text>();t.font=font;t.fontSize=fontSize;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
            t.rectTransform.anchorMin=t.rectTransform.anchorMax=new Vector2(.5f,.5f);t.rectTransform.sizeDelta=size;t.rectTransform.anchoredPosition=pos;return t;
        }
        private static void ApplyWhenIdle()
        {
            if(quitting) return;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {EditorApplication.delayCall+=ApplyWhenIdle;return;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            var saved=prefab==null?null:prefab.GetComponentInChildren<PrototypeBattleView>(true);if(saved==null) return;
            var data=new SerializedObject(saved);
            if(data.FindProperty("playerStatus").objectReferenceValue!=null &&
                data.FindProperty("playerSlots").GetArrayElementAtIndex(0).FindPropertyRelative("levelText").objectReferenceValue!=null) return;
            var root=PrefabUtility.LoadPrefabContents(Path);
            try
            {
                var view=root.GetComponentInChildren<PrototypeBattleView>(true);var so=new SerializedObject(view);
                for(int side=0;side<2;side++)
                {
                    string prefix=side==0?"player":"enemy";
                    var panel=((Image)so.FindProperty(side==0?"leftPanel":"rightPanel").objectReferenceValue).transform;
                    var font=((Text)so.FindProperty(prefix+"Vitals").objectReferenceValue).font;
                    if(so.FindProperty(prefix+"Status").objectReferenceValue==null)
                        so.FindProperty(prefix+"Status").objectReferenceValue=TextElement("CombatStatuses",panel,font,new Vector2(0,-385),new Vector2(300,62),18);
                    var slots=so.FindProperty(prefix+"Slots");
                    for(int i=0;i<slots.arraySize;i++)
                    {
                        var slot=slots.GetArrayElementAtIndex(i);var icon=(Image)slot.FindPropertyRelative("icon").objectReferenceValue;
                        var tile=(RectTransform)icon.transform.parent;tile.sizeDelta=new Vector2(132,100);tile.anchoredPosition=new Vector2(i%2==0?-75:75,125-i/2*106);
                        ((Text)slot.FindPropertyRelative("label").objectReferenceValue).fontSize=14;
                        if(slot.FindPropertyRelative("levelText").objectReferenceValue==null)
                            slot.FindPropertyRelative("levelText").objectReferenceValue=TextElement("ItemLevel",tile,font,new Vector2(-39,34),new Vector2(48,20),14);
                        if(slot.FindPropertyRelative("production").objectReferenceValue!=null) continue;
                        var progress=new GameObject("ProductionProgress",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));progress.transform.SetParent(tile,false);
                        var bar=progress.GetComponent<Image>();bar.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PKBall/Resources/BattleArt/UI/energy_fill.png");
                        bar.color=new Color(.35f,.95f,.65f);bar.type=Image.Type.Filled;bar.fillMethod=Image.FillMethod.Horizontal;bar.raycastTarget=false;
                        bar.rectTransform.anchorMin=bar.rectTransform.anchorMax=new Vector2(.5f,.5f);bar.rectTransform.sizeDelta=new Vector2(102,4);bar.rectTransform.anchoredPosition=new Vector2(0,-23);
                        progress.SetActive(false);slot.FindPropertyRelative("production").objectReferenceValue=bar;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Path);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
