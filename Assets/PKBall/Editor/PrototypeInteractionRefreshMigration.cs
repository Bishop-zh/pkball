using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
namespace PKBall.Prototype.Editor {
[InitializeOnLoad] public static class PrototypeInteractionRefreshMigration {
static PrototypeInteractionRefreshMigration(){EditorApplication.delayCall+=Apply;}
public static void Apply(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
foreach(string folder in new[]{"Assets/PKBall/Resources/StatusFx","Assets/PKBall/Resources/Icons"})foreach(string path in System.IO.Directory.GetFiles(folder,"*.png")){
if(folder.EndsWith("Icons")&&!path.EndsWith("gold_ore.png"))continue;var t=AssetImporter.GetAtPath(path) as TextureImporter;if(t==null||t.textureType==TextureImporterType.Sprite)continue;
t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.spritePixelsPerUnit=100;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();}
PrototypePreparationRefreshMigration.Apply();AssetDatabase.SaveAssets();}
}}
