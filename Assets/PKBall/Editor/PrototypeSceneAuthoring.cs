using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PKBall.Prototype.Editor
{
    // Used by the development process. The delivered project already contains the saved scenes.
    public static class PrototypeSceneAuthoring
    {
        private const string Root = "Assets/PKBall";
        private static readonly Dictionary<string, Sprite> SavedSprites = new Dictionary<string, Sprite>();
        private static int materialIndex;

        public static void CreateScenes()
        {
            Folder(Root + "/Scenes");
            Folder(Root + "/Prefabs/Screens");
            Folder(Root + "/Prefabs/Actors");
            Folder(Root + "/Art/SceneSprites");
            Folder(Root + "/Art/SceneMaterials");
            SavedSprites.Clear();
            materialIndex = 0;
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (PrototypeSceneView view in Enum.GetValues(typeof(PrototypeSceneView)))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject root = new GameObject("PKBall_GameRoot");
                PrototypeBattle battle = root.AddComponent<PrototypeBattle>();
                battle.BuildEditorScene(view);
                PersistResources(root, view.ToString());
                GameObject actor = Find(root.transform, "Player").gameObject;
                GameObject actorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Actors/Combatant.prefab");
                if (actorPrefab == null)
                    actorPrefab = PrefabUtility.SaveAsPrefabAsset(actor, Root + "/Prefabs/Actors/Combatant.prefab");
                battle.SetActorTemplate(actorPrefab);
                string name = PrototypeSceneSession.SceneName(view);
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, Root + "/Prefabs/Screens/" + name + ".prefab", InteractionMode.AutomatedAction);
                string path = Root + "/Scenes/" + name + ".unity";
                if (!EditorSceneManager.SaveScene(scene, path)) throw new Exception("Could not save " + path);
                if (view == PrototypeSceneView.MainMenu)
                    EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity", true);
                scenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log("PKBall authored " + path + " with " + root.GetComponentsInChildren<Transform>(true).Length + " editable objects.");
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateSavedScenes();
            string mainPath = Root + "/Scenes/MainMenu.unity";
            EditorSceneManager.OpenScene(mainPath);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(mainPath);
            Selection.activeGameObject = GameObject.Find("PKBall_GameRoot");
            Debug.Log("PKBall authored scenes ready. Open project and press Play; no generation menu is required.");
        }

        public static void ValidateSavedScenes()
        {
            foreach (PrototypeSceneView view in Enum.GetValues(typeof(PrototypeSceneView)))
            {
                string path = Root + "/Scenes/" + PrototypeSceneSession.SceneName(view) + ".unity";
                Scene scene = EditorSceneManager.OpenScene(path);
                var root = scene.GetRootGameObjects()[0];
                if (root.GetComponent<PrototypeBattle>() == null) throw new Exception("Missing scene controller: " + path);
                if (root.GetComponentsInChildren<Canvas>(true).Length != 3) throw new Exception("Missing saved canvases: " + path);
                if (root.GetComponentsInChildren<Button>(true).Length < 40) throw new Exception("Missing saved buttons: " + path);
                foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                    if (graphic.GetComponent<CanvasRenderer>() == null)
                        throw new Exception("Missing CanvasRenderer: " + graphic.name);
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    if (image.sprite != null && !EditorUtility.IsPersistent(image.sprite))
                        throw new Exception("Unsaved UI sprite: " + image.name);
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                        throw new Exception("Missing script: " + transform.name);
                Debug.Log("PKBall saved scene validated: " + path);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            throw new Exception("Missing authored object " + name);
        }

        private static void PersistResources(GameObject root, string prefix)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite != null) image.sprite = SaveSprite(image.sprite);
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.sprite != null) renderer.sprite = SaveSprite(renderer.sprite);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || EditorUtility.IsPersistent(materials[i])) continue;
                    string path = Root + "/Art/SceneMaterials/" + prefix + "_" + materialIndex++ + ".mat";
                    var material = new Material(materials[i]);
                    AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(path));
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                PhysicMaterial material = collider.sharedMaterial;
                if (material != null && !EditorUtility.IsPersistent(material))
                    AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Root + "/Art/SceneMaterials/" + prefix + "_Bounce.physicMaterial"));
            }
        }

        private static Sprite SaveSprite(Sprite source)
        {
            if (EditorUtility.IsPersistent(source)) return source;
            string key = AssetDatabase.GetAssetPath(source.texture) + source.border + source.pixelsPerUnit;
            if (SavedSprites.TryGetValue(key, out Sprite existing)) return existing;
            string path = Root + "/Art/SceneSprites/" + source.texture.name + "_" + (int)source.border.x + ".asset";
            existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing == null)
            {
                existing = UnityEngine.Object.Instantiate(source);
                existing.name = source.texture.name;
                AssetDatabase.CreateAsset(existing, AssetDatabase.GenerateUniqueAssetPath(path));
            }
            SavedSprites[key] = existing;
            return existing;
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    [InitializeOnLoad]
    internal static class PrototypeEditorEntry
    {
        static PrototypeEditorEntry()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                const string path = "Assets/PKBall/Scenes/MainMenu.unity";
                var entry = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (entry == null) return;
                EditorSceneManager.playModeStartScene = entry;
                Scene current = SceneManager.GetActiveScene();
                if (!current.isDirty && (string.IsNullOrEmpty(current.path) || current.name == "SampleScene"))
                    EditorSceneManager.OpenScene(path);
            };
        }
    }
}
