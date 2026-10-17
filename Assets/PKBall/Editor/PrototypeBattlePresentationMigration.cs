using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeBattlePresentationMigration
    {
        private const string Path="Assets/PKBall/Prefabs/Screens/PrototypeBattle.prefab";
        static PrototypeBattlePresentationMigration(){EditorApplication.delayCall+=ApplyWhenIdle;}
        private static void ApplyWhenIdle()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=ApplyWhenIdle;return;}
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Path);if(asset==null)return;
            var view=asset.GetComponentInChildren<PrototypeBattleView>(true);if(view==null)return;
            var data=new SerializedObject(view);var panel=(Image)data.FindProperty("leftPanel").objectReferenceValue;
            if(panel!=null && Mathf.Approximately(panel.rectTransform.sizeDelta.x,420))return;
            Apply();
        }
        public static void Apply()
        {
            var root=PrefabUtility.LoadPrefabContents(Path);
            try{var view=root.GetComponentInChildren<PrototypeBattleView>(true);view.ApplyPresentationLayout();view.BuildDetailsView();PrefabUtility.SaveAsPrefabAsset(root,Path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Debug.Log("PKBall battle presentation: enlarged panels and contained metric badges applied.");
        }
    }
}
