using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace PKBall.Prototype
{
    public enum PrototypeDamageKind { Impact, Melee, Ranged, Exhaustion, SuddenDeath, Poison, Burn, Heal, Shield, ShieldAbsorbed }

    public sealed partial class PrototypeBattle : MonoBehaviour
    {
        public PrototypeBalance Balance { get; private set; }
        [SerializeField] private float circularArenaRadius;
        public float CircularArenaRadius => circularArenaRadius;
        public Camera ArenaCamera => arenaCamera;
        public PhysicMaterial BounceMaterial { get; private set; }
        public PrototypeActor Player { get; private set; }
        public PrototypeActor Enemy { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsFighting { get; private set; }
        public string Result { get; private set; } = "拖动鼠标决定发射方向和力度，松开开战";
        public int ImpactCount { get; private set; }
        public PrototypeCombatPresentation Presentation { get; private set; }
        private readonly PrototypeAttackScheduler scheduledAttacks = new PrototypeAttackScheduler();
        public PrototypeRun Run { get; private set; }
        public bool IsPreparing { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public string PlayerVisualId { get; private set; } = "player_warrior";


        private readonly List<Object> runtimeAssets = new List<Object>();
        private readonly Dictionary<PrototypeDamageKind, float> playerDamageByKind = new Dictionary<PrototypeDamageKind, float>();
        private readonly Dictionary<PrototypeDamageKind, float> enemyDamageByKind = new Dictionary<PrototypeDamageKind, float>();
        [SerializeField] private Transform arenaRoot;
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private PrototypeHud hud;
        [SerializeField] private PrototypePreparationUI preparationUI;
        [SerializeField] private PrototypeMainMenuUI mainMenu;
        private bool enteredMatch;
        private float lastPairContact = -100f;
        private bool aiming;
        private Vector3 aimPoint;
        private Vector3 startPoint;
        private bool autoPlaySmoke;
        private float nextDiagnosticAt;
        private bool rebuilding;
        private float nextCheckpoint;

        [SerializeField] private bool authoredScene;
        [SerializeField] private PrototypeSceneView sceneView;
        [SerializeField] private GameObject actorTemplate;
        [SerializeField] private GameObject previewPlayer;
        [SerializeField] private GameObject previewEnemy;
        [SerializeField] private PhysicMaterial authoredBounce;

        public void BuildEditorScene(PrototypeSceneView view)
        {
            sceneView = view;
            Run = new PrototypeRun(true, true);
            enteredMatch = view != PrototypeSceneView.MainMenu;
            BuildPrototype();
            arenaCamera.transform.SetParent(transform, true);
            authoredScene = true;
            authoredBounce = BounceMaterial;
            previewPlayer = Player.gameObject;
            previewEnemy = Enemy.gameObject;
            ApplySceneView();
        }

        public void SetActorTemplate(GameObject prefab) { actorTemplate = prefab; }

        private void ApplySceneView()
        {
            IsPreparing = sceneView == PrototypeSceneView.Preparation;
            enteredMatch = sceneView != PrototypeSceneView.MainMenu;
            mainMenu.Show(sceneView == PrototypeSceneView.MainMenu);
            preparationUI.Show(IsPreparing);
            hud.Show(sceneView == PrototypeSceneView.Battle);
            arenaRoot.gameObject.SetActive(sceneView == PrototypeSceneView.Battle);
        }

        private void InitializeSavedScene()
        {
            autoPlaySmoke = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-pkball-autoplay") >= 0;
            Run = PrototypeSceneSession.Run ?? (PrototypeSceneSession.Run = new PrototypeRun(autoPlaySmoke, autoPlaySmoke));
            Balance = Resources.Load<PrototypeBalance>("PrototypeBalance");
            BounceMaterial = authoredBounce;
            PlayerVisualId = PlayerPrefs.GetString("PKBall.PlayerVisual", "player_warrior");
            if (previewPlayer != null) { previewPlayer.SetActive(false); Destroy(previewPlayer); }
            if (previewEnemy != null) { previewEnemy.SetActive(false); Destroy(previewEnemy); }
            BuildActors();
            hud.Initialize(this);
            preparationUI.Initialize(this, Run);
            mainMenu.Initialize(this);
            ApplySceneView();
            if (sceneView == PrototypeSceneView.Battle)
            {
                if (IsBroadcast) { Result="主播单挑"; StartFight(Vector3.right,1); return; }
                if (!Run.CanFight && !autoPlaySmoke) { ChangeScene(PrototypeSceneView.Preparation); return; }
                Result = "从场地拖动鼠标并松开，发射角色";
                if (autoPlaySmoke) StartFight(Vector3.right, 1f);
            }
        }

        private void ChangeScene(PrototypeSceneView view)
        {
            if (view != PrototypeSceneView.Battle) Run.CancelAiming();
            PrototypeSceneSession.Run = Run;
            Time.timeScale = 1f;
            SceneManager.LoadScene(PrototypeSceneSession.SceneName(view));
        }

        private static void RemoveGenerated(Object asset)
        {
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }

        private void Start()
        {
            if (authoredScene) InitializeSavedScene();
            else BuildPrototype();
        }

        public PrototypeActor OpponentOf(PrototypeActor actor)
        {
            return actor == Player ? Enemy : Player;
        }

        public Material CreateMaterial(Color color)
        {
            Material template = Resources.Load<Material>("PrototypeLit");
            Shader shader = template == null ? Shader.Find("Universal Render Pipeline/Lit") : null;
            if (template == null && shader == null) shader = Shader.Find("Standard");
            if (template == null && shader == null) return null;
            Material material = template != null ? new Material(template) : new Material(shader);
            material.color = color;
            runtimeAssets.Add(material);
            return material;
        }

        private void BuildPrototype()
        {
            autoPlaySmoke = System.Array.IndexOf(System.Environment.GetCommandLineArgs(),
                "-pkball-autoplay") >= 0;
            if (Run == null) Run = new PrototypeRun(autoPlaySmoke, autoPlaySmoke);
            PlayerVisualId = PlayerPrefs.GetString("PKBall.PlayerVisual", "player_warrior");
            if (PlayerVisualId != "player_warrior" && PlayerVisualId != "player_mage" &&
                PlayerVisualId != "player_engineer") PlayerVisualId = "player_warrior";
            if (autoPlaySmoke && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),
                "-pkball-smoke-loadout") >= 0)
            {
                Run.BuyToSlot(0, 1);
                Run.BuyToSlot(1, 8);
            }
            Balance = Resources.Load<PrototypeBalance>("PrototypeBalance");
            if (Balance == null) Balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            BounceMaterial = new PhysicMaterial("PKBall Prototype Bounce")
            {
                bounciness = 1f,
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                frictionCombine = PhysicMaterialCombine.Minimum
            };
            runtimeAssets.Add(BounceMaterial);

            arenaRoot = new GameObject("PrototypeArena").transform;
            arenaRoot.SetParent(transform);
            SetupCameraAndLight();
            BuildFloorAndWalls();
            BuildActors();
            BuildAimLine();
            hud = new GameObject("PrototypeHUD").AddComponent<PrototypeHud>();
            hud.transform.SetParent(transform);
            hud.Initialize(this);
            preparationUI = new GameObject("PreparationUI").AddComponent<PrototypePreparationUI>();
            preparationUI.transform.SetParent(transform);
            preparationUI.Initialize(this, Run);
            mainMenu = new GameObject("MainMenuUI").AddComponent<PrototypeMainMenuUI>();
            mainMenu.transform.SetParent(transform);
            mainMenu.Initialize(this);
            Result = "拖动鼠标决定发射方向和力度。";
            IsPreparing = !autoPlaySmoke && enteredMatch;
            preparationUI.Show(IsPreparing);
            hud.Show(autoPlaySmoke || enteredMatch);
            mainMenu.Show(!autoPlaySmoke && !enteredMatch);
            if (autoPlaySmoke) StartFight(Vector3.right, 1f);
        }

        public void EnterMatchMode()
        {
            if (Run.Parked || Run.GameOver) { Run=new PrototypeRun(true); PrototypeSceneSession.Run=Run; }
            Run.MarkStarted();
            if (authoredScene) { ChangeScene(PrototypeSceneView.Preparation); return; }
            enteredMatch = true;
            IsPreparing = true;
            RefreshPreparationActors();
            mainMenu.Show(false);
            hud.Show(true);
            preparationUI.Show(true);
        }

        public void ReturnToMenu()
        {
            if (IsBroadcast) { LeaveBroadcast(); return; }
            if (IsFighting) return;
            Run.CancelAiming();
            if (authoredScene) { ChangeScene(PrototypeSceneView.MainMenu); return; }
            enteredMatch = false;
            IsPreparing = false;
            preparationUI.Show(false);
            hud.Show(false);
            mainMenu.Show(true);
        }

        public void SuspendRun()
        {
            if (IsFighting) return;
            Run.Park();
            if (authoredScene) ChangeScene(PrototypeSceneView.MainMenu);
            else ReturnToMenu();
        }
        public bool RestoreHistory(string id)
        {
            if (IsFighting || Run.HasActiveRun) return false;
            var restored=PrototypeRunHistory.Restore(id);
            if (restored == null) return false;
            Run=restored; PrototypeSceneSession.Run=restored;
            if (authoredScene) ChangeScene(PrototypeSceneView.MainMenu);
            else Restart();
            return true;
        }
        public void SelectPlayerVisual(string visualId)
        {
            if (visualId != "player_warrior" && visualId != "player_mage" &&
                visualId != "player_engineer") return;
            PlayerVisualId = visualId;
            PlayerPrefs.SetString("PKBall.PlayerVisual", visualId);
            PlayerPrefs.Save();
            if (IsPreparing) RefreshPreparationActors();
        }

        private void SetupCameraAndLight()
        {
            arenaCamera = Camera.main;
            if (arenaCamera == null)
            {
                GameObject cameraObject = new GameObject("PrototypeCamera");
                cameraObject.tag = "MainCamera";
                arenaCamera = cameraObject.AddComponent<Camera>();
                cameraObject.transform.SetParent(arenaRoot);
            }
            arenaCamera.transform.position = new Vector3(0f, 20f, 0f);
            arenaCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            arenaCamera.orthographic = true;
            arenaCamera.orthographicSize = 4.65f;
            arenaCamera.backgroundColor = new Color(0.065f, 0.09f, 0.15f);
            arenaCamera.clearFlags = CameraClearFlags.SolidColor;

            Light light = FindObjectOfType<Light>();
            if (light == null)
            {
                GameObject lightObject = new GameObject("PrototypeLight");
                lightObject.transform.SetParent(arenaRoot);
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }
            light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            light.intensity = 1.25f;
        }

        private void BuildFloorAndWalls()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(arenaRoot);
            floor.transform.position = new Vector3(0f, -0.68f, 0f);
            floor.transform.localScale = new Vector3(1.22f, 1f, 0.82f);
            floor.GetComponent<Renderer>().material = CreateMaterial(new Color(.065f, .13f, .19f));
            Collider floorCollider = floor.GetComponent<Collider>();
            floorCollider.enabled = false;
            RemoveGenerated(floorCollider);

            CreateWall("NorthWall", new Vector3(0f, 0f, Balance.arenaHalfDepth + 0.25f),
                new Vector3(Balance.arenaHalfWidth * 2f + 1f, 1.5f, 0.5f));
            CreateWall("SouthWall", new Vector3(0f, 0f, -Balance.arenaHalfDepth - 0.25f),
                new Vector3(Balance.arenaHalfWidth * 2f + 1f, 1.5f, 0.5f));
            CreateWall("EastWall", new Vector3(Balance.arenaHalfWidth + 0.25f, 0f, 0f),
                new Vector3(0.5f, 1.5f, Balance.arenaHalfDepth * 2f + 1f));
            CreateWall("WestWall", new Vector3(-Balance.arenaHalfWidth - 0.25f, 0f, 0f),
                new Vector3(0.5f, 1.5f, Balance.arenaHalfDepth * 2f + 1f));
            BuildArenaMarkings();
        }

        private void BuildArenaMarkings()
        {
            var gold = new Color(.79f, .48f, .14f);
            var cyan = new Color(.12f, .54f, .62f);
            float x = Balance.arenaHalfWidth - .22f;
            float z = Balance.arenaHalfDepth - .22f;
            CreateMarking("BrassArenaRim", new[] {
                new Vector3(-x, -.655f, -z), new Vector3(x, -.655f, -z),
                new Vector3(x, -.655f, z), new Vector3(-x, -.655f, z)
            }, gold, .065f, true);
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = ring == 0 ? 1.15f : .46f;
                var points = new Vector3[48];
                for (int i = 0; i < points.Length; i++)
                {
                    float angle = i * Mathf.PI * 2f / points.Length;
                    points[i] = new Vector3(Mathf.Cos(angle) * radius, -.651f,
                        Mathf.Sin(angle) * radius);
                }
                CreateMarking("CenterRing" + ring, points, ring == 0 ? gold : cyan,
                    ring == 0 ? .045f : .04f, true);
            }
            CreateMarking("StartingLine", new[] {
                new Vector3(0f, -.652f, -z), new Vector3(0f, -.652f, z)
            }, new Color(.18f, .38f, .44f), .025f, false);
        }

        private void CreateMarking(string name, Vector3[] points, Color color, float width, bool loop)
        {
            GameObject mark = new GameObject(name, typeof(LineRenderer));
            mark.transform.SetParent(arenaRoot, false);
            LineRenderer line = mark.GetComponent<LineRenderer>();
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.useWorldSpace = false;
            line.loop = loop;
            line.widthMultiplier = width;
            line.numCornerVertices = 3;
            line.material = CreateMaterial(color);
        }

        private void CreateWall(string label, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = label;
            wall.transform.SetParent(arenaRoot);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Collider>().material = BounceMaterial;
            wall.GetComponent<Renderer>().material = CreateMaterial(new Color(.16f, .25f, .35f));
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = label + "BrassTrim";
            trim.transform.SetParent(arenaRoot);
            trim.transform.position = position + Vector3.up * .78f;
            trim.transform.localScale = new Vector3(scale.x * .97f, .08f, scale.z * .97f);
            trim.GetComponent<Renderer>().material = CreateMaterial(new Color(.65f, .40f, .15f));
            Collider trimCollider = trim.GetComponent<Collider>();
            trimCollider.enabled = false;
            RemoveGenerated(trimCollider);
        }

        private void BuildActors()
        {
            if (PrototypeBroadcastSession.Config != null) { BuildBroadcastActors(); return; }
            PrototypeStatSnapshot stats = Run.CalculateStats();
            Player = CreateActor("Player", 1,
                new Vector3(-2.7f, 0f, 0f), stats.health, stats.speed,
                stats.impactBonus, stats.ImpactTakenMultiplier, 0.69f,
                new Color(0.1f, 0.72f, 0.84f),
                null, null, 1f, 1f, Run.Adjacent("boots", "spear"));
            Player.ConfigureStats(stats);
            for (int i=0;i<Run.Equipped.Count;i++)
            {
                var item=Run.Equipped[i];if(item==null || !Run.IsSlotOpen(i)) continue;
                float bonus=item.definition.weapon?.kind==PrototypeWeaponKind.Melee?stats.meleeDamageBonus:stats.rangedDamageBonus;
                Player.AddEquipment(item,i,bonus,Run.SlotEffects[i]==PrototypeSlotEffect.Sharpened?1.2f:1,
                    Run.SlotEffects[i]==PrototypeSlotEffect.Echo? .85f:1);
            }
            PrototypeEncounter encounter = Run.CurrentEncounter;
            if (encounter.character != null)
            {
                int ownedBefore = PrototypeBroadcastSession.Owned.Count;
                Enemy = BroadcastActor(encounter.character, 2, new Vector3(2.7f, 0f, 0f));
                int created = PrototypeBroadcastSession.Owned.Count - ownedBefore;
                runtimeAssets.AddRange(PrototypeBroadcastSession.Owned.GetRange(ownedBefore, created));
                PrototypeBroadcastSession.Owned.RemoveRange(ownedBefore, created);
            }
            else
            {
            Enemy = CreateActor(encounter.name, 2,
                new Vector3(2.7f, 0f, 0f), encounter.health,
                encounter.speed, encounter.impactBonus, encounter.impactTakenMultiplier, 0.65f,
                encounter.tier == PrototypeEncounterTier.Normal ? new Color(.30f, .67f, .32f) :
                    encounter.tier == PrototypeEncounterTier.Elite ? new Color(.91f, .34f, .26f) :
                    new Color(.38f, .23f, .62f), null, null);
            for(int i=0;i<encounter.itemIds.Length;i++)
            {
                var d=Run.Find(encounter.itemIds[i]);if(d==null) continue;
                Enemy.AddEquipment(new PrototypeItemInstance {definition=d,instanceId=-(i+1),level=d.ResolveLevel(1)},i,encounter.weaponDamageMultiplier-1);
            }
            }
            if (Presentation == null && (!authoredScene || sceneView == PrototypeSceneView.Battle))
            {
                var effects = new GameObject("CombatPresentation");effects.transform.SetParent(arenaRoot,false);
                Presentation=effects.AddComponent<PrototypeCombatPresentation>();Presentation.Initialize(this);
            }
            Presentation?.BindActors(Player,Enemy);
        }

        private PrototypeWeaponDefinition MakeWeapon(string label, PrototypeWeaponKind kind, float cooldown,
            float damage, float range, float projectileSpeed, Color color, bool impact, float impactMultiplier)
        {
            PrototypeWeaponDefinition definition = ScriptableObject.CreateInstance<PrototypeWeaponDefinition>();
            definition.displayName = label;
            definition.kind = kind;
            definition.cooldown = cooldown;
            definition.damage = damage;
            definition.range = range;
            definition.projectileSpeed = projectileSpeed;
            definition.prototypeColor = color;
            definition.modifiesImpact = impact;
            definition.impactMultiplier = impactMultiplier;
            runtimeAssets.Add(definition);
            return definition;
        }

        private static PrototypeWeaponDefinition LoadWeapon(string assetName)
        {
            return Resources.Load<PrototypeWeaponDefinition>("Weapons/" + assetName);
        }

        private PrototypeActor CreateActor(string label, int id, Vector3 position, float health, float speed,
            float impactBonus, float impactTaken, float radius, Color color,
            PrototypeWeaponDefinition melee, PrototypeWeaponDefinition ranged,
            float meleeFactor = 1f, float rangedFactor = 1f, bool speedOnMeleeHit = false,
            float impactEffectFactor = 1f, bool meleeOnRight = false)
        {
            GameObject body = actorTemplate != null ? Instantiate(actorTemplate) :
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Transform oldRing = body.transform.Find("FacingEquipmentRing");
            if (oldRing != null) { oldRing.gameObject.SetActive(false); RemoveGenerated(oldRing.gameObject); }
            body.name = label;
            body.transform.SetParent(arenaRoot);
            body.transform.position = position;
            body.transform.localScale = Vector3.one;
            // Keep the Rigidbody and collider; only hide the primitive's rendering.
            var physicalRenderer=body.GetComponent<Renderer>();if(physicalRenderer!=null) physicalRenderer.enabled=false;
            Rigidbody rigidbody = body.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = body.AddComponent<Rigidbody>();
            PrototypeActor actor = body.GetComponent<PrototypeActor>();
            if (actor == null) actor = body.AddComponent<PrototypeActor>();
            actor.Initialize(this, Balance, id, health, speed, impactBonus, impactTaken, radius,
                melee, ranged, meleeFactor, rangedFactor, speedOnMeleeHit,
                impactEffectFactor, meleeOnRight);
            // The visual is scaled, so use a local collider radius that yields the requested world radius.
            body.GetComponent<SphereCollider>().radius = radius;
            AttachPrototypeSprite(actor.transform,IsBroadcast ? (id==1?PrototypeBroadcastSession.Config.left.visualId:PrototypeBroadcastSession.Config.right.visualId) : id == 1 ? PlayerVisualId : Run.CurrentEncounter.visualId);
            return actor;
        }

        private void AttachPrototypeSprite(Transform parent, string visualId)
        {
            PrototypeVisualDefinition visual = Resources.Load<PrototypeVisualDefinition>("Visuals/" + visualId);
            Texture2D texture = visual == null ? null : visual.portrait;
            if (texture == null) texture = Resources.Load<Texture2D>("Art/" + visualId);
            if (texture == null) texture = Resources.Load<Texture2D>("Art/character-disc-cyan");
            if (texture == null) return;
            GameObject image = new GameObject("ReplaceableCharacterBitmap");
            image.transform.SetParent(parent, false);
            image.transform.localPosition = new Vector3(0f,.85f,0f);
            image.transform.localRotation = Quaternion.Euler(90f,0f,0f);
            image.transform.localScale = Vector3.one;
            SpriteRenderer renderer = image.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            runtimeAssets.Add(renderer.sprite);
            renderer.color = visual == null ? Color.white : visual.tint;
            renderer.sortingOrder=10;
            // Portraits have transparent padding. Match the visible body to the collider diameter.
            float diameter=parent.GetComponent<PrototypeActor>().Radius*2*1.24f;
            float extent=Mathf.Max(renderer.sprite.bounds.size.x,renderer.sprite.bounds.size.y);
            image.transform.localScale=Vector3.one*(diameter/extent);
        }

        private void BuildAimLine()
        {
            GameObject line = new GameObject("AimPreview");
            line.transform.SetParent(arenaRoot);
            aimLine = line.AddComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.startWidth = 0.08f;
            aimLine.endWidth = 0.02f;
            Material lineMaterial = CreateMaterial(Color.cyan);
            if (lineMaterial != null) aimLine.material = lineMaterial;
            aimLine.startColor = Color.cyan;
            aimLine.endColor = Color.white;
            aimLine.enabled = false;
        }

        private void Update()
        {
            if (!enteredMatch && !autoPlaySmoke) return;
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (IsFighting || IsPreparing) return;
            if (Player == null || !Player.IsAlive || Enemy == null || !Enemy.IsAlive) return;
            if (Input.GetMouseButtonDown(0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) &&
                TryPointerOnArena(out Vector3 initialPoint) &&
                (circularArenaRadius <= 0 || new Vector2(initialPoint.x, initialPoint.z).magnitude <= circularArenaRadius))
            {
                aiming = true;
                startPoint = Player.transform.position;
                aimLine.enabled = true;
            }
            if (aiming && Input.GetMouseButton(0) && TryPointerOnArena(out Vector3 point))
            {
                aimPoint = point;
                aimLine.SetPosition(0, startPoint + Vector3.up * 0.9f);
                aimLine.SetPosition(1, point + Vector3.up * 0.9f);
            }
            if (aiming && Input.GetMouseButtonUp(0))
            {
                aiming = false;
                aimLine.enabled = false;
                Vector3 direction = aimPoint - startPoint;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.04f) direction = Enemy.transform.position - startPoint;
                float power = Mathf.Clamp(direction.magnitude / 4f, 0.5f, 1f);
                StartFight(direction, power);
            }
        }

        private bool TryPointerOnArena(out Vector3 point)
        {
            Ray ray;
            PrototypeBattleView view = hud.GetComponentInChildren<PrototypeBattleView>(true);
            if (view != null) { if (!view.TryPointerRay(Input.mousePosition, out ray)) { point = Vector3.zero; return false; } }
            else ray = arenaCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = Vector3.zero;
            return false;
        }

        private void StartFight(Vector3 direction, float power)
        {
            if (IsFighting || !Run.StartCombat()) return;
            IsPreparing = false;
            if (preparationUI != null) preparationUI.Show(false);
            IsFighting = true;
            Time.timeScale = SpeedMultiplier;
            Elapsed = 0f;
            StepIndex=0;simulationTime=0;contacts.Clear();
            CombatSeed=PrototypeCombatRandom.SeedFor(Run.ActiveBattleId);combatRandom.Reset(CombatSeed);
            DamageEventCount=0;
            Result = "战斗中";
            Player.Launch(direction, power);
            Vector3 towardPlayer = Player.transform.position - Enemy.transform.position;
            Enemy.Launch(Quaternion.Euler(0f, 13f, 0f) * towardPlayer, 0.9f);
        }

        public void BeginAiming()
        {
            if (IsFighting || !Run.CanFight) return;
            if (!Run.BeginAiming()) return;
            if (authoredScene) { ChangeScene(PrototypeSceneView.Battle); return; }
            RefreshPreparationActors();
            IsPreparing = false;
            preparationUI.Show(false);
            Result = "从场地拖动鼠标并松开，发射角色";
        }

        public void ToggleSpeed()
        {
            SpeedMultiplier = SpeedMultiplier > 1f ? 1f : 2f;
            if (IsFighting) Time.timeScale = SpeedMultiplier;
        }

        private void FixedUpdate()
        {
            if (!IsFighting) return;
            StepIndex++;
            simulationTime+=Time.fixedDeltaTime;Elapsed=(float)simulationTime;
            Player.Buffs.Advance(StepIndex);
            Enemy.Buffs.Advance(StepIndex);
            Player.MotionTick(Time.fixedDeltaTime);Enemy.MotionTick(Time.fixedDeltaTime);
            DrainContacts();
            Player.CombatTick(Elapsed);Enemy.CombatTick(Elapsed);
            while(scheduledAttacks.TryTake(Elapsed,out var pending))
                ApplyPayload(pending.source,pending.target,pending.payload);
            AdvanceOrdinaryProjectiles(Time.fixedDeltaTime);
            AdvancePersistentSwords(Time.fixedDeltaTime);
            if(Time.unscaledTime>=nextCheckpoint)
            {nextCheckpoint=Time.unscaledTime+5f;Run.CheckpointCombat();}
            if (autoPlaySmoke && Elapsed >= nextDiagnosticAt)
            {
                Debug.Log($"PKBall smoke t={Elapsed:0.0} player={Player.Health:0.0} " +
                          $"enemy={Enemy.Health:0.0} impacts={ImpactCount}");
                nextDiagnosticAt += 5f;
            }
            if (Elapsed >= Balance.suddenDeathStart)
            {
                float rate = Balance.OvertimeDamage(Elapsed);
                Player.ReceiveDamage(rate * Time.fixedDeltaTime, PrototypeDamageKind.SuddenDeath);
                Enemy.ReceiveDamage(rate * Time.fixedDeltaTime, PrototypeDamageKind.SuddenDeath);
            }
            if (!Player.IsAlive || !Enemy.IsAlive || Elapsed >= Balance.maximumBattleTime)
                FinishFight();
        }

        private void ProcessActorCollision(PrototypeActor a, PrototypeActor b, float relativeNormalSpeed, Vector3 fromAtoB)
        {
            if (!IsFighting || Elapsed - lastPairContact < Balance.impactPairCooldown) return;
            float strength = Mathf.Max(0f, relativeNormalSpeed - Balance.impactThreshold);
            if (strength <= 0f) return;
            lastPairContact = Elapsed;
            ImpactCount++;
            float dealtByA = ImpactDamage(a, b, strength, fromAtoB);
            float dealtByB = ImpactDamage(b, a, strength, -fromAtoB);
            a.SpendImpactEnergy(strength);
            b.SpendImpactEnergy(strength);
            DealDamage(a, b, dealtByA, PrototypeDamageKind.Impact);
            DealDamage(b, a, dealtByB, PrototypeDamageKind.Impact);
            a.ShowPassiveCollision(b);b.ShowPassiveCollision(a);
        }

        private float ImpactDamage(PrototypeActor attacker, PrototypeActor victim, float strength, Vector3 attackDirection)
        {
            float raw = (Balance.impactBaseDamage + Balance.impactSpeedDamage * strength) *
                        Mathf.Sqrt(attacker.EffectiveMass) * (1f + attacker.ImpactDamageBonus) *
                        victim.ImpactTakenMultiplier * attacker.ImpactMultiplierFor(attackDirection);
            return Mathf.Min(raw, victim.MaximumHealth * Balance.impactMaxTargetHealthFraction);
        }

        public void DealDamage(PrototypeActor source, PrototypeActor target, float amount,
            PrototypeDamageKind kind, PrototypeItemInstance weaponItem = null)
        {
            if (!IsFighting || target == null || !target.IsAlive) return;
            if (source == Player && kind == PrototypeDamageKind.Melee && amount > 0f)
                Run.RecordPickaxeHit(weaponItem);
            target.ReceiveDamage(amount, kind);
        }

        public void ReportDamage(PrototypeActor target, float amount, PrototypeDamageKind kind)
        {
            TraceDamage(target,amount,kind);
            Dictionary<PrototypeDamageKind, float> ledger = target == Enemy ? playerDamageByKind : enemyDamageByKind;
            ledger[kind] = ledger.TryGetValue(kind, out float previous) ? previous + amount : amount;
        }

        public float DamageBy(PrototypeActor attacker, PrototypeDamageKind kind)
        {
            Dictionary<PrototypeDamageKind, float> ledger = attacker == Player ? playerDamageByKind : enemyDamageByKind;
            return ledger.TryGetValue(kind, out float value) ? value : 0f;
        }

        public bool HasLineOfSight(PrototypeActor source, PrototypeActor target)
        {
            Vector3 direction = target.transform.position - source.transform.position;
            float distance = direction.magnitude;
            if (distance <= source.Radius + target.Radius + 0.2f) return true;
            Vector3 origin = source.transform.position + direction.normalized * (source.Radius + 0.05f);
            return Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance) &&
                   hit.collider.GetComponent<PrototypeActor>() == target;
        }

        public void ScheduleAttack(PrototypeActor source,PrototypeActor target,PrototypeAttackPayload payload,float delay)
        { scheduledAttacks.Add(new PrototypeScheduledAttack {source=source,target=target,payload=payload,hitAt=Elapsed+Mathf.Max(0,delay)}); }
        public void ApplyPayload(PrototypeActor source,PrototypeActor target,PrototypeAttackPayload payload)
        {
            if(!IsFighting || target==null || !target.IsAlive || target.IsInvulnerable) return;
            bool burningBeforeHit = target.State.Burn > 0;
            Presentation?.PlayHit(payload.hitSoundId,payload.hitVolume);
            // Payload is a release-time snapshot. Later instance growth cannot change an arrow already in flight.
            DealDamage(source,target,payload.damage,payload.kind);
            if(target.IsAlive)
            {
                target.State.AddPoison(payload.poison);target.State.AddBurn(payload.burn);
                target.ApplyKnockback(payload.push,payload.knockback);
                Presentation?.ShowHit(target,payload.hitVisual);
            }
            source?.NotifyItemHit(payload,target,burningBeforeHit);
            if(source==Player && payload.produceOre) source.RecordProduced(payload.item,Run.RecordOreCredit(payload.oreCredit));
        }
        private void FinishFight()
        {
            IsFighting = false;
            ClearProjectiles();
            contacts.Clear();
            Time.timeScale = 1f;
            IsPreparing = true;
            Player.GetComponent<Rigidbody>().velocity = Vector3.zero;
            Enemy.GetComponent<Rigidbody>().velocity = Vector3.zero;
            float playerRatio = Player.Health / Player.MaximumHealth;
            float enemyRatio = Enemy.Health / Enemy.MaximumHealth;
            Result = playerRatio > enemyRatio ? "胜利！按 R 再来一场" :
                playerRatio < enemyRatio ? "失败！按 R 再来一场" : "平局！按 R 再来一场";
            if (autoPlaySmoke) Debug.Log("PKBall smoke result: " + Result +
                $" melee={DamageBy(Player, PrototypeDamageKind.Melee):0.0}" +
                $" ranged={DamageBy(Player, PrototypeDamageKind.Ranged):0.0}");
            if (autoPlaySmoke || IsBroadcast) Run.CommitCombat();
            if (!autoPlaySmoke && !IsBroadcast)
            {
                Run.ResolveCombat(playerRatio >= enemyRatio);
                StartCoroutine(ReturnToPreparation());
            }
        }

        private IEnumerator ReturnToPreparation()
        {
            yield return new WaitForSeconds(2f);
            if (authoredScene) ChangeScene(PrototypeSceneView.Preparation);
            else Restart();
        }

        public void Restart()
        {
            if (IsBroadcast) { Time.timeScale=1; SceneManager.LoadScene("PrototypeBattle"); return; }
            if (rebuilding) return;
            if (authoredScene) { ChangeScene(sceneView == PrototypeSceneView.Battle ? PrototypeSceneView.Preparation : sceneView); return; }
            Time.timeScale = 1f;
            rebuilding = true;
            StartCoroutine(RebuildNextFrame());
        }

        public void NewRun()
        {
            Time.timeScale = 1f;
            SpeedMultiplier = 1f;
            if (Run.Started && !Run.Parked) Run.Park();
            Run = new PrototypeRun(true);
            PrototypeSceneSession.Run=Run;
            Restart();
        }

        public void RefreshPreparationActors()
        {
            if (!IsPreparing || Player == null || Enemy == null) return;
            Player.gameObject.SetActive(false);
            Enemy.gameObject.SetActive(false);
            Destroy(Player.gameObject);
            Destroy(Enemy.gameObject);
            BuildActors();
        }

        private IEnumerator RebuildNextFrame()
        {
            if (IsFighting) Run.CommitCombat();
            IsFighting = false;
            if (arenaRoot != null) Destroy(arenaRoot.gameObject);
            if (hud != null) Destroy(hud.gameObject);
            if (preparationUI != null) Destroy(preparationUI.gameObject);
            if (mainMenu != null) Destroy(mainMenu.gameObject);
            foreach (Object asset in runtimeAssets)
                if (asset != null) Destroy(asset);
            runtimeAssets.Clear();
            yield return null;
            projectilePool.Clear();activeProjectiles.Clear();ordinaryProjectiles.Clear();persistentSwords.Clear();scheduledAttacks.Clear();Presentation=null;
            playerDamageByKind.Clear();
            enemyDamageByKind.Clear();
            Player = null;
            Enemy = null;
            Elapsed = 0f;
            ImpactCount = 0;
            lastPairContact = -100f;
            nextDiagnosticAt = 0f;
            BuildPrototype();
            rebuilding = false;
        }

        private void OnApplicationPause(bool paused) {if(paused && IsFighting) Run?.CheckpointCombat();}
        private void OnApplicationQuit() {if(IsFighting) Run?.CheckpointCombat();Run?.FlushSave();}
        private void OnDestroy() {if(IsFighting) Run?.CommitCombat();Time.timeScale = 1f;}
    }

    public static class PrototypeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene") return;
            if (Object.FindObjectOfType<PrototypeBattle>() != null) return;
            new GameObject("PKBall_Prototype").AddComponent<PrototypeBattle>();
        }
    }
}
