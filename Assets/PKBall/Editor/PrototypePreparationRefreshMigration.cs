using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
namespace PKBall.Prototype.Editor
{
    [InitializeOnLoad] public static class PrototypePreparationRefreshMigration
    {
        private static bool quitting;
        static PrototypePreparationRefreshMigration(){EditorApplication.quitting+=()=>quitting=true;EditorApplication.delayCall+=Apply;}
        public static void Apply()
        {
            if(quitting)return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Apply;return;}
            const string marker="Assets/PKBall/Art/Atlases/PreparationRefresh.spriteatlas";
            if(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(marker)!=null){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PKBall/Prefabs/Screens/Preparation.prefab");
                if(prefab.GetComponentInChildren<PrototypePreparationUI>(true).ModernLayoutVersion<4){var saved=PrefabUtility.LoadPrefabContents("Assets/PKBall/Prefabs/Screens/Preparation.prefab");
                    try{saved.GetComponentInChildren<PrototypePreparationUI>(true).BuildModernView();PrefabUtility.SaveAsPrefabAsset(saved,"Assets/PKBall/Prefabs/Screens/Preparation.prefab");}finally{PrefabUtility.UnloadPrefabContents(saved);}}return;}
            foreach(string folder in new[]{"PreparationArt","StatusFx"})foreach(string path in Directory.GetFiles("Assets/PKBall/Resources/"+folder,"*.png"))
            {
                var t=AssetImporter.GetAtPath(path) as TextureImporter;if(t==null)continue;
                t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.spritePixelsPerUnit=100;
                t.textureCompression=TextureImporterCompression.Uncompressed;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=1024;
                t.spriteBorder=path.Contains("button")?new Vector4(60,20,60,20):path.Contains("panel")?new Vector4(20,20,20,20):Vector4.zero;t.SaveAndReimport();
            }
            var atlas=new SpriteAtlas();atlas.SetIncludeInBuild(true);atlas.SetPackingSettings(new SpriteAtlasPackingSettings{padding=4,enableRotation=false,enableTightPacking=false});
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings{generateMipMaps=false,filterMode=FilterMode.Bilinear,sRGB=true});
            var setting=atlas.GetPlatformSettings("DefaultTexturePlatform");setting.maxTextureSize=2048;setting.format=TextureImporterFormat.RGBA32;atlas.SetPlatformSettings(setting);
            atlas.Add(new Object[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/PKBall/Resources/PreparationArt"),AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/PKBall/Resources/StatusFx")});
            string prep="Assets/PKBall/Prefabs/Screens/Preparation.prefab";var root=PrefabUtility.LoadPrefabContents(prep);
            try{root.GetComponentInChildren<PrototypePreparationUI>(true).BuildModernView();PrefabUtility.SaveAsPrefabAsset(root,prep);}finally{PrefabUtility.UnloadPrefabContents(root);}
            string combat="Assets/PKBall/Prefabs/Screens/PrototypeBattle.prefab";root=PrefabUtility.LoadPrefabContents(combat);
            try
            {
                var battle=root.GetComponentInChildren<PrototypeBattle>(true);var data=new SerializedObject(battle);data.FindProperty("circularArenaRadius").floatValue=4.05f;data.ApplyModifiedPropertiesWithoutUndo();
                foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))if(sprite.name=="SkinArenaFloor"||sprite.name=="SkinArenaRim")sprite.transform.localScale*=8.7f/7.9f;
                PrefabUtility.SaveAsPrefabAsset(root,combat);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.CreateAsset(atlas,marker);AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
            Debug.Log("PKBall PREPARATION REFRESH IMPORTED: independent alpha UI/FX, modern saved view, larger arena.");
        }
    }
}
