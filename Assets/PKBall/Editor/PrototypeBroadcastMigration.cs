using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace PKBall.Prototype.Editor
{
    [InitializeOnLoad] public static class PrototypeBroadcastMigration
    {
        private static bool quitting;
        static PrototypeBroadcastMigration(){EditorApplication.quitting+=()=>quitting=true;EditorApplication.delayCall+=Apply;}
        public static void Apply()
        {
            if(quitting)return;if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Apply;return;}
            string file="Assets/PKBall/Resources/Data/items.json";
            if(!File.Exists(file))
            {
                var assets=Resources.LoadAll<PrototypeItemDefinition>("Items");var entries=new System.Collections.Generic.List<PrototypeItemDataEntry>();
                foreach(var a in assets)entries.Add(new PrototypeItemDataEntry {assetId=a.itemId,hasWeapon=a.weapon!=null,item=JsonUtility.FromJson<PrototypeItemData>(JsonUtility.ToJson(a)),weapon=a.weapon==null?null:JsonUtility.FromJson<PrototypeWeaponData>(JsonUtility.ToJson(a.weapon))});
                AddWeapon(entries,"crossbow","sword_rain","星落剑阵",PrototypeItemRarity.Legendary,38,PrototypeAttackVisual.Volley,12,48,8,7);
                AddWeapon(entries,"crossbow","prism_laser","棱镜光束",PrototypeItemRarity.Epic,26,PrototypeAttackVisual.Beam,1,24,4,.08f);
                AddWeapon(entries,"hammer","quake_hammer","震荡战锤",PrototypeItemRarity.Rare,18,PrototypeAttackVisual.Shockwave,1,25,4,.22f);
                Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,JsonUtility.ToJson(new PrototypeItemDataFile {items=entries.ToArray()},true));AssetDatabase.ImportAsset(file);PrototypeItemRepository.Reset();
            }
            CreateFallbackAssets(file);
            const string path="Assets/PKBall/Prefabs/Screens/MainMenu.prefab";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset!=null && NeedsView(asset))
            {
                var root=PrefabUtility.LoadPrefabContents(path);try{var old=root.GetComponentInChildren<PrototypeBroadcastEditor>(true);if(old!=null){var so=new SerializedObject(old);foreach(var key in new[]{"panel","entry"}){var obj=so.FindProperty(key).objectReferenceValue;if(obj is Component component)UnityEngine.Object.DestroyImmediate(component.gameObject);else if(obj is GameObject go)UnityEngine.Object.DestroyImmediate(go);}UnityEngine.Object.DestroyImmediate(old);}
                    root.GetComponentInChildren<PrototypeMainMenuUI>(true).BuildBroadcastView();PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        private static void CreateFallbackAssets(string file)
        {
            // New IDs must also survive malformed JSON fallback; never discard their save records.
            var data=JsonUtility.FromJson<PrototypeItemDataFile>(File.ReadAllText(file));if(data?.items==null)return;
            foreach(string id in new[]{"sword_rain","prism_laser","quake_hammer","death_scythe","death_cloak","slime_mucus","bone_talisman"})
            {
                string path="Assets/PKBall/Resources/Items/"+id+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<PrototypeItemDefinition>(path);
                if(existing!=null)
                {
                    continue;
                }
                var entry=Array.Find(data.items,e=>e.item.itemId==id);if(entry==null)continue;
                var original=Resources.Load<PrototypeItemDefinition>("Items/"+entry.assetId);if(original==null)continue;
                var item=UnityEngine.Object.Instantiate(original);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(entry.item),item);item.name=id;
                if(entry.hasWeapon)
                {
                    var weapon=original.weapon==null?ScriptableObject.CreateInstance<PrototypeWeaponDefinition>():UnityEngine.Object.Instantiate(original.weapon);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(entry.weapon),weapon);weapon.name=id;
                    Directory.CreateDirectory("Assets/PKBall/Resources/Weapons");AssetDatabase.CreateAsset(weapon,"Assets/PKBall/Resources/Weapons/"+id+".asset");item.weapon=weapon;
                }
                AssetDatabase.CreateAsset(item,path);
            }
            AssetDatabase.SaveAssets();
        }
        public static void Verify()
        {
            Apply();PrototypeItemRepository.Reset();var run=new PrototypeRun(true,true);
            foreach(var asset in Resources.LoadAll<PrototypeItemDefinition>("Items"))
            {
                var d=PrototypeItemRepository.Find(asset.itemId);if(d==null || (d.weapon==null)!=(asset.weapon==null))throw new Exception("Item migration mismatch: "+asset.itemId);
            }
            foreach(string id in new[]{"sword_rain","prism_laser","quake_hammer","death_scythe","death_cloak","slime_mucus","bone_talisman"})if(Resources.Load<PrototypeItemDefinition>("Items/"+id)==null)throw new Exception("Missing stable-ID fallback "+id);
            Debug.Log("BROADCAST FINAL COMPILE AND ITEM MIGRATION VERIFIED");
        }
        private static bool NeedsView(GameObject root){var view=root.GetComponentInChildren<PrototypeBroadcastEditor>(true);if(view==null)return true;var so=new SerializedObject(view);return so.FindProperty("layoutVersion").intValue<11 || so.FindProperty("slotButtons").arraySize!=8 || so.FindProperty("gallery").objectReferenceValue==null;}

        private static void AddWeapon(System.Collections.Generic.List<PrototypeItemDataEntry> entries,string source,string id,string name,PrototypeItemRarity rarity,int price,PrototypeAttackVisual style,int count,float damage,float cooldown,float delay)
        {
            if(entries.Exists(e=>e.item.itemId==id))return;
            var original=entries.Find(e=>e.item.itemId==source);if(original==null)return;
            var e=JsonUtility.FromJson<PrototypeItemDataEntry>(JsonUtility.ToJson(original));e.assetId=source;e.item.itemId=id;e.item.displayName=name;e.item.rarity=rarity;e.item.price=price;e.item.minShopDay=rarity==PrototypeItemRarity.Legendary?5:rarity==PrototypeItemRarity.Epic?3:1;
            e.item.description="主播表现原型；沿用现有图标，可独立替换素材。";e.item.tags=new[]{style==PrototypeAttackVisual.Beam?"光束":style==PrototypeAttackVisual.Volley?"剑阵":"锤","物理"};
            e.weapon.displayName=name;e.weapon.visualStyle=style;e.weapon.visualCount=count;e.weapon.damage=damage;e.weapon.cooldown=cooldown;e.weapon.hitDelay=delay;
            e.weapon.animationDuration=style==PrototypeAttackVisual.Volley?.85f:style==PrototypeAttackVisual.Beam?.22f:.5f;e.weapon.projectileSpeed=style==PrototypeAttackVisual.Beam?100:12;
            if(style==PrototypeAttackVisual.Volley)e.weapon.hitDelay=.4f;
            entries.Add(e);
        }
    }
}
