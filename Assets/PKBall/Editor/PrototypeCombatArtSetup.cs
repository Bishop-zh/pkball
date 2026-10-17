using System;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace PKBall.Prototype.Editor
{
    public static class PrototypeCombatArtSetup
    {
        private const string Art = "Assets/PKBall/Resources/BattleArt";
        private static Font font;
        private static PrototypeBattleSkin skin;
        private static readonly string[] Ids = { "apprentice_sword", "apprentice_shield", "apprentice_venom", "apprentice_flame", "apprentice_spring", "master_sword", "master_shield", "master_venom", "master_flame", "master_spring", "master_token", "random_brewer", "super_injector", "poison_dagger", "flame_axe", "shield_emitter" };
        private static readonly string[] Names = { "学徒剑", "学徒盾", "学徒毒典", "学徒火灯", "学徒泉石", "宗师剑", "宗师盾", "宗师毒典", "宗师火灯", "宗师泉石", "宗师令牌", "随机炼药釜", "超级药水注射器", "毒牙匕首", "焰锋斧", "黄铜盾发生器" };
        public static void Preview()
        {
            SessionState.SetBool("PKBall.CombatArtPreview", true);
            PrototypeAuthoredSceneCheck.Start();
        }
        public static void Apply()
        {
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(Art, "*.png", SearchOption.AllDirectories)) ImportSprite(path.Replace('\\', '/'));
            foreach (string path in Directory.GetFiles("Assets/PKBall/Resources/Icons", "*.png")) ImportSprite(path.Replace('\\', '/'));
            Directory.CreateDirectory("Assets/PKBall/Art/Atlases");AssetDatabase.Refresh();
            SpriteAtlas uiAtlas = Atlas("CombatInterface", new[] { Art + "/UI" });
            SpriteAtlas itemsAtlas = Atlas("CombatItems", new[] { Art + "/Items", "Assets/PKBall/Resources/Icons" });
            skin = AssetDatabase.LoadAssetAtPath<PrototypeBattleSkin>(Art + "/DefaultBattleSkin.asset");
            if (skin == null) { skin = ScriptableObject.CreateInstance<PrototypeBattleSkin>();AssetDatabase.CreateAsset(skin, Art + "/DefaultBattleSkin.asset"); }
            skin.background = Sprite("Arena/background");skin.arenaFloor = Sprite("Arena/arena_floor");skin.arenaRim = Sprite("Arena/arena_rim");
            skin.playerPanel = Sprite("UI/player_panel");skin.enemyPanel = Sprite("UI/enemy_panel");skin.itemTile = Sprite("UI/cooldown_surface");
            skin.healthFrame = Sprite("UI/health_frame");skin.playerHealth = Sprite("UI/health_player");skin.enemyHealth = Sprite("UI/health_enemy");
            skin.shieldFill = Sprite("UI/shield_fill");skin.energyFrame = Sprite("UI/energy_frame");skin.energyFill = Sprite("UI/energy_fill");
            skin.countBadge = Sprite("UI/count_badge");skin.titleStrip = Sprite("UI/title_strip");skin.cyanButton = Sprite("UI/button_cyan");skin.coralButton = Sprite("UI/button_coral");
            skin.interfaceAtlas = uiAtlas;skin.itemAtlas = itemsAtlas;EditorUtility.SetDirty(skin);
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeItemArtCatalog>(Art + "/ItemArtCatalog.asset");
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<PrototypeItemArtCatalog>();AssetDatabase.CreateAsset(catalog, Art + "/ItemArtCatalog.asset"); }
            catalog.entries = new PrototypeItemArtCatalog.Entry[Ids.Length];
            for (int i = 0; i < Ids.Length; i++) catalog.entries[i] = new PrototypeItemArtCatalog.Entry { itemId = Ids[i], displayName = Names[i], icon = Sprite("Items/" + Ids[i]) };
            EditorUtility.SetDirty(catalog);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string pathToPrefab = "Assets/PKBall/Prefabs/Screens/PrototypeBattle.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(pathToPrefab);
            try
            {
                PrototypeBattle battle = root.GetComponent<PrototypeBattle>();
                Set(battle, "circularArenaRadius", 3.65f);
                Transform display = Find(root.transform, "PresentationCamera");
                if (display == null) { var go = new GameObject("PresentationCamera", typeof(Camera)); go.transform.SetParent(root.transform, false); display = go.transform; }
                Camera presentation = display.GetComponent<Camera>();
                presentation.cullingMask = 0; presentation.depth = -100; presentation.clearFlags = CameraClearFlags.SolidColor;
                presentation.backgroundColor = PrototypeUiSkin.Navy; presentation.allowHDR = false;
                Transform arena = Find(root.transform, "PrototypeArena");
                foreach (Transform child in arena)
                    if (child.name.Contains("Wall") || child.name == "ArenaFloor" || child.name.StartsWith("CenterRing") || child.name == "StartingLine" || child.name == "BrassArenaRim") child.gameObject.SetActive(false);
                Floor(arena, "SkinArenaFloor", skin.arenaFloor, -.68f, 0);
                Floor(arena, "SkinArenaRim", skin.arenaRim, -.66f, 1);
                PrototypeHud hud = root.GetComponentInChildren<PrototypeHud>(true);
                Transform old = Find(hud.transform, "ModularBattleCanvas");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                RectTransform canvas = Rect("ModularBattleCanvas", hud.transform, Vector2.zero, new Vector2(1920,1080));
                Canvas c = canvas.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=50;
                CanvasScaler scale=canvas.gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                PrototypeBattleView view = canvas.gameObject.AddComponent<PrototypeBattleView>();Set(view,"skin",skin);
                Image bg = Image("Background",canvas,Vector2.zero,new Vector2(1920,1080),skin.background);
                bg.rectTransform.anchorMin=Vector2.zero;bg.rectTransform.anchorMax=Vector2.one;bg.rectTransform.offsetMin=Vector2.zero;bg.rectTransform.offsetMax=Vector2.zero;
                Set(view,"background",bg);
                RectTransform rawRect=Rect("ArenaViewport",canvas,new Vector2(0,10),new Vector2(820,820));
                RawImage raw=rawRect.gameObject.AddComponent<RawImage>();raw.raycastTarget=false;Set(view,"arenaViewport",raw);
                Set(view,"clock",Label("BattleClock",canvas,new Vector2(0,478),new Vector2(700,50),"战斗阶段",28));
                Set(view,"instructions",Label("Instructions",canvas,new Vector2(0,-466),new Vector2(980,70),"从场地拖动并松开，弹射开战",24));
                Side(view,canvas,true);Side(view,canvas,false);view.ApplyPresentationLayout();
                Button speed=Button("SpeedButton",canvas,new Vector2(620,478),"速度 1×",skin.cyanButton);
                Button restart=Button("RestartButton",canvas,new Vector2(815,478),"重开",skin.coralButton);
                Button menu=Button("MenuButton",canvas,new Vector2(-815,478),"返回主菜单",skin.cyanButton);
                Set(view,"speedButton",speed);Set(view,"speedText",speed.GetComponentInChildren<Text>());Set(view,"restartButton",restart);Set(view,"menuButton",menu);
                Set(hud,"modularView",view);
                Transform legacy=Find(hud.transform,"Canvas");if(legacy!=null) legacy.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,pathToPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            BuildGallery(catalog);
            AssetDatabase.SaveAssets();
            SpriteAtlasUtility.PackAtlases(new[]{uiAtlas,itemsAtlas},EditorUserBuildSettings.activeBuildTarget);
            Validate();
            Debug.Log("PKBall COMBAT ART SETUP PASSED: independent skin sprites, 16 item icons, shared UI/item atlases, saved battle prefab.");
        }
        private static void ImportSprite(string path)
        {
            TextureImporter t=AssetImporter.GetAtPath(path) as TextureImporter;if(t==null)return;
            t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.isReadable=false;
            t.spritePixelsPerUnit=100;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;
            string n=Path.GetFileNameWithoutExtension(path);
            t.spriteBorder=n.Contains("panel")?new Vector4(24,24,24,24):n.Contains("frame")?new Vector4(38,12,38,12):n.Contains("button")?new Vector4(36,18,36,18):Vector4.zero;
            t.SaveAndReimport();
        }
        private static SpriteAtlas Atlas(string name,string[] folders)
        {
            string path="Assets/PKBall/Art/Atlases/"+name+".spriteatlas";
            SpriteAtlas atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if(atlas==null){atlas=new SpriteAtlas();AssetDatabase.CreateAsset(atlas,path);}
            atlas.SetIncludeInBuild(true);
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings{padding=4,enableRotation=false,enableTightPacking=false});
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings{generateMipMaps=false,readable=false,filterMode=FilterMode.Bilinear,sRGB=true});
            var packing=atlas.GetPlatformSettings("DefaultTexturePlatform");packing.maxTextureSize=2048;packing.format=TextureImporterFormat.RGBA32;atlas.SetPlatformSettings(packing);
            UnityEngine.Object[] previous=atlas.GetPackables();if(previous.Length>0)atlas.Remove(previous);
            foreach(string folder in folders)atlas.Add(new[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder)});
            EditorUtility.SetDirty(atlas);return atlas;
        }
        private static Sprite Sprite(string relative)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/"+relative+".png");
        private static Transform Find(Transform root,string name){foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
        private static void Set(UnityEngine.Object target,string name,UnityEngine.Object value){var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Set(UnityEngine.Object target,string name,float value){var s=new SerializedObject(target);s.FindProperty(name).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=pos;rt.sizeDelta=size;return rt;
        }
        private static Image Image(string name,Transform parent,Vector2 pos,Vector2 size,Sprite sprite)
        {
            RectTransform rt=Rect(name,parent,pos,size);var image=rt.gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;
            if(sprite!=null&&sprite.border!=Vector4.zero)image.type=UnityEngine.UI.Image.Type.Sliced;
            return image;
        }
        private static Text Label(string name,Transform parent,Vector2 pos,Vector2 size,string content,int fontSize)
        {
            var rt=Rect(name,parent,pos,size);Text t=rt.gameObject.AddComponent<Text>();t.font=font;t.fontSize=fontSize;t.text=content;t.color=PrototypeUiSkin.Cream;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;
        }
        private static Button Button(string name,Transform parent,Vector2 pos,string content,Sprite sprite)
        {
            Image bg=Image(name,parent,pos,new Vector2(180,58),sprite);bg.raycastTarget=true;Button b=bg.gameObject.AddComponent<Button>();b.targetGraphic=bg;Label("Label",bg.transform,Vector2.zero,new Vector2(170,48),content,22);return b;
        }
        private static void Side(PrototypeBattleView view,Transform canvas,bool player)
        {
            float x=player?-735:735;string prefix=player?"Player":"Enemy";
            Image panel=Image(prefix+"EquipmentPanel",canvas,new Vector2(x,-15),new Vector2(350,875),player?skin.playerPanel:skin.enemyPanel);Set(view,player?"leftPanel":"rightPanel",panel);
            Set(view,player?"playerName":"enemyName",Label("Name",panel.transform,new Vector2(0,393),new Vector2(285,42),player?"我方":"对手",25));
            Set(view,player?"playerPortrait":"enemyPortrait",Image("Portrait",panel.transform,new Vector2(0,328),new Vector2(92,92),null));
            Set(view,player?"playerVitals":"enemyVitals",Label("Vitals",panel.transform,new Vector2(0,267),new Vector2(300,32),"生命",21));
            Image frame=Image("HealthFrame",panel.transform,new Vector2(0,238),new Vector2(290,38),skin.healthFrame);
            Image fill=Image("HealthFill",frame.transform,Vector2.zero,new Vector2(248,20),player?skin.playerHealth:skin.enemyHealth);fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillAmount=1;Set(view,player?"playerHealth":"enemyHealth",fill);
            Image energy=Image("EnergyFrame",panel.transform,new Vector2(0,203),new Vector2(290,20),skin.energyFrame);
            Image ef=Image("EnergyFill",energy.transform,Vector2.zero,new Vector2(250,8),skin.energyFill);ef.type=UnityEngine.UI.Image.Type.Filled;ef.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;Set(view,player?"playerEnergy":"enemyEnergy",ef);
            var so=new SerializedObject(view);var slots=so.FindProperty(player?"playerSlots":"enemySlots");slots.arraySize=10;
            for(int i=0;i<10;i++)
            {
                Image tile=Image("SlotSurface",panel.transform,new Vector2(i%2==0?-75:75,125-i/2*120),new Vector2(132,108),skin.itemTile);
                Image icon=Image("Icon",tile.transform,new Vector2(0,13),new Vector2(68,68),null);icon.preserveAspect=true;
                Image mask=Image("CooldownMask",tile.transform,new Vector2(0,13),new Vector2(68,68),skin.itemTile);mask.color=new Color(.015f,.025f,.05f,.8f);mask.type=UnityEngine.UI.Image.Type.Filled;mask.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;
                Text state=Label("CooldownText",tile.transform,new Vector2(0,12),new Vector2(68,22),"",17);
                Text label=Label("ItemName",tile.transform,new Vector2(0,-37),new Vector2(126,24),"",16);
                Image badge=Image("CountBadge",tile.transform,new Vector2(44,-13),new Vector2(34,24),skin.countBadge);
                Text count=Label("Count",badge.transform,Vector2.zero,new Vector2(30,22),"",16);badge.gameObject.SetActive(false);
                SerializedProperty slot=slots.GetArrayElementAtIndex(i);slot.FindPropertyRelative("icon").objectReferenceValue=icon;slot.FindPropertyRelative("cooldown").objectReferenceValue=mask;slot.FindPropertyRelative("label").objectReferenceValue=label;slot.FindPropertyRelative("state").objectReferenceValue=state;slot.FindPropertyRelative("count").objectReferenceValue=count;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Floor(Transform arena,string name,Sprite sprite,float y,int order)
        {
            Transform old=Find(arena,name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(arena,false);go.transform.localPosition=new Vector3(0,y,0);go.transform.localRotation=Quaternion.Euler(90,0,0);
            var sr=go.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;
            go.transform.localScale=new Vector3(7.9f/sprite.bounds.size.x,7.9f/sprite.bounds.size.y,1);
        }
        private static void BuildGallery(PrototypeItemArtCatalog catalog)
        {
            GameObject root=new GameObject("PlannedItemArtGallery");
            try
            {
                RectTransform canvas=Rect("Canvas",root.transform,Vector2.zero,new Vector2(1920,1080));var c=canvas.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;
                var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
                for(int i=0;i<catalog.entries.Length;i++)
                {
                    var entry=catalog.entries[i];Vector2 pos=new Vector2(-660+(i%4)*440,360-(i/4)*240);
                    Image("Art_"+entry.itemId,canvas,pos,new Vector2(160,160),entry.icon);
                    Label("Name_"+entry.itemId,canvas,pos+new Vector2(0,-100),new Vector2(320,35),entry.displayName,25);
                }
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/PKBall/Prefabs/Screens/PlannedItemArtGallery.prefab");
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static void Validate()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PKBall/Prefabs/Screens/PrototypeBattle.prefab");
            var view=prefab.GetComponentInChildren<PrototypeBattleView>(true);if(view==null||view.Skin==null)throw new Exception("Missing saved battle skin view");
            foreach(Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))if(graphic.GetComponent<CanvasRenderer>()==null)throw new Exception("Missing CanvasRenderer: "+graphic.name);
            var catalog=AssetDatabase.LoadAssetAtPath<PrototypeItemArtCatalog>(Art+"/ItemArtCatalog.asset");foreach(var e in catalog.entries)if(e.icon==null)throw new Exception("Missing icon "+e.itemId);
            if(view.Skin.interfaceAtlas==null||view.Skin.itemAtlas==null)throw new Exception("Missing atlases");
        }
    }
}
