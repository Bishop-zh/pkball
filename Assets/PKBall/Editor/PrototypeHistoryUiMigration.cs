using UnityEditor;
using UnityEngine;
namespace PKBall.Prototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeHistoryUiMigration
    {
        private static bool quitting;
        static PrototypeHistoryUiMigration()
        {EditorApplication.quitting+=()=>quitting=true;EditorApplication.delayCall+=Apply;}
        private static void Apply()
        {
            if(quitting)return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
            {EditorApplication.delayCall+=Apply;return;}
            Patch("Assets/PKBall/Prefabs/Screens/MainMenu.prefab",true);
            Patch("Assets/PKBall/Prefabs/Screens/PrototypeBattle.prefab",false);
        }
        private static void Patch(string path,bool menu)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)return;
            if(menu && asset.GetComponentInChildren<PrototypeHistoryMenu>(true)!=null)return;
            if(!menu)
            {var v=asset.GetComponentInChildren<PrototypeBattleView>(true);if(v==null||new SerializedObject(v).FindProperty("itemDetails").objectReferenceValue!=null)return;}
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(menu)root.GetComponentInChildren<PrototypeMainMenuUI>(true).BuildHistoryView();
                else root.GetComponentInChildren<PrototypeBattleView>(true).BuildDetailsView();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
