using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

namespace PKBall.Prototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeAuthoredSceneCheck
    {
        private static int phase;
        private static double changedAt;
        private static double startedAt;
        private static PrototypeRun run;
        private static Vector3 launchPosition;
        private static bool preparationOpened;
        private static string broadcastBattleId;
        private static float beforeDoubleSpeed;

        static PrototypeAuthoredSceneCheck()
        {
            if (!SessionState.GetBool("PKBall.AuthoredCheck", false)) return;
            phase = 0;
            startedAt = changedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        public static void Start()
        {
            SessionState.SetBool("PKBall.AuthoredCheck", true);
            phase = 0;
            startedAt = changedAt = EditorApplication.timeSinceStartup;
            EditorSceneManager.OpenScene("Assets/PKBall/Scenes/MainMenu.unity");
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SeedTransientSession()
        {
            if (!SessionState.GetBool("PKBall.AuthoredCheck",false)) return;
            PrototypeMonsterPool.SetDirectoryForTests("Artifacts/ArchitectureAudit/empty-scene-monster-pool");
            PrototypeSceneSession.Run=new PrototypeRun(true,true);
        }

        private static void Require(bool condition, string reason)
        { if (!condition) throw new Exception("PKBall scene check: " + reason); }

        private static void Next()
        { phase++; changedAt = EditorApplication.timeSinceStartup; }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - startedAt > 90) throw new Exception("Scene check timed out at phase " + phase);
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - changedAt < 1.2) return;
                PrototypeBattle battle = UnityEngine.Object.FindObjectOfType<PrototypeBattle>();
                string scene = SceneManager.GetActiveScene().name;
                switch (phase)
                {
                    case 0:
                        run = new PrototypeRun(true, true);
                        PrototypeCheckFixtures.Bag(run).Add(new PrototypeItemInstance { instanceId = 9001, definition = run.Find("spear"), level = 2 });
                        run.Equip(0, 0);
                        PrototypeSceneSession.Run = run;
                        SceneManager.LoadScene("MainMenu"); Next(); break;
                    case 1:
                        Require(scene == "MainMenu" && battle != null, "main menu did not load");
                        Require(ReferenceEquals(battle.Run, run), "menu lost run data");
                        Require(UnityEngine.Object.FindObjectsOfType<Canvas>(true).Length == 3, "duplicate canvases");
                        battle.EnterMatchMode(); Next(); break;
                    case 2:
                        Require(scene == "Preparation" && battle.IsPreparing, "preparation did not load");
                        Require(ReferenceEquals(battle.Run, run) && run.Equipped[0].level == 2, "equipment changed during scene transition");
                        if (!preparationOpened)
                        {
                            foreach (Button button in battle.GetComponentsInChildren<Button>(true))
                                if (button.name == "Choice0") { button.onClick.Invoke(); break; }
                            Require(run.EventChosen, "saved event button was not rebound");
                            preparationOpened = true;
                            changedAt = EditorApplication.timeSinceStartup;
                            return;
                        }
                        Canvas.ForceUpdateCanvases();
                        Button slot = null;
                        Button bag = null;
                        foreach (Graphic graphic in battle.GetComponentsInChildren<Graphic>(true))
                            Require(graphic.GetComponent<CanvasRenderer>() != null, "missing CanvasRenderer on " + graphic.name);
                        foreach (Button button in battle.GetComponentsInChildren<Button>(true))
                        {
                            if (button.name == "SlotFR") slot = button;
                            if (button.name == "Bag0") bag = button;
                        }
                        Require(slot != null && bag != null, "missing interaction targets");
                        var pointer = new PointerEventData(EventSystem.current);
                        pointer.position = RectTransformUtility.WorldToScreenPoint(null, slot.transform.Find("Icon").position);
                        var hits = new List<RaycastResult>();
                        EventSystem.current.RaycastAll(pointer, hits);
                        Require(hits.Exists(hit => hit.gameObject == slot.gameObject), "mouse raycast did not hit the ring sector: point=" + pointer.position + ", screen=" + Screen.width + "x" + Screen.height + ", depth=" + slot.GetComponent<Graphic>().depth + ", hits=" + string.Join(",", hits.ConvertAll(hit => hit.gameObject.name)));
                        ExecuteEvents.Execute(slot.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                        Require(run.Equipped[0] == null && run.Bag.Count > 0, "pointer click did not unequip the item");
                        pointer.pointerDrag = bag.gameObject;
                        ExecuteEvents.Execute(slot.gameObject, pointer, ExecuteEvents.dropHandler);
                        Require(run.Equipped[0] != null && run.Equipped[0].level == 2, "item drop did not re-equip the item");
                        Debug.Log("PKBall UI RAYCAST, POINTER CLICK AND EQUIPMENT DROP PASSED");
                        run.AdvanceEvent(); run.ChooseEvent(0, 0); run.AdvanceEvent(); run.SelectMonster(0);
                        battle.BeginAiming(); Next(); break;
                    case 3:
                        Require(scene == "PrototypeBattle" && !battle.IsPreparing, "battle did not load");
                        Require(ReferenceEquals(battle.Run, run), "battle lost run data");
                        Require(Mathf.Approximately(battle.Player.MaximumHealth, run.CalculateStats().health), "actor stats were not rebound");
                        // Floating numbers now share the HUD canvas instead of adding a world canvas.
                        Require(UnityEngine.Object.FindObjectsOfType<Canvas>(true).Length == 4, "battle duplicated UI");
                        Require(UnityEngine.Object.FindObjectsOfType<PrototypeBattleVitals>(true).Length == 2,"missing or duplicated readable vitality panels");
                        int numberLayers=0;foreach(var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))if(t.name=="ScreenCombatNumbers")numberLayers++;
                        Require(numberLayers==1,"missing or duplicated floating-number layer");
                        Require(battle.GetComponentInChildren<PrototypeBattleView>(true) != null, "modular view missing");
                        launchPosition = battle.Player.transform.position;
                        typeof(PrototypeBattle).GetMethod("StartFight", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(battle, new object[] { Vector3.right, 1f });
                        Next(); break;
                    case 4:
                        Require(battle.IsFighting && battle.Elapsed > .5f, "combat did not simulate");
                        Require(Vector3.Distance(launchPosition, battle.Player.transform.position) > .1f, "actor did not move");
                        Require(new Vector2(battle.Player.transform.position.x, battle.Player.transform.position.z).magnitude <= battle.CircularArenaRadius, "player escaped circular arena");
                        if (SessionState.GetBool("PKBall.CombatArtPreview", false))
                        {
                            SessionState.SetBool("PKBall.CombatArtPreview", false);
                            SessionState.SetBool("PKBall.AuthoredCheck", false);
                            EditorApplication.update -= Tick;
                            EditorApplication.isPaused = true;
                            Debug.Log("PKBall COMBAT ART LIVE PREVIEW READY");
                            return;
                        }
                        battle.Enemy.ReceiveDamage(battle.Enemy.MaximumHealth * 2f, PrototypeDamageKind.Melee);
                        Next(); break;
                    case 5:
                        if (scene != "Preparation") return;
                        Require(ReferenceEquals(battle.Run, run), "combat result lost run data");
                        Require(run.Stage == PrototypeDayStage.EventThree, "combat result did not advance the day flow");
                        Require(run.Equipped[0].level == 2, "combat result lost equipped item");
                        battle.ReturnToMenu(); Next(); break;
                    case 6:
                        Require(scene == "MainMenu" && ReferenceEquals(battle.Run, run), "return to menu lost the session");
                        Debug.Log("PKBall AUTHORED PLAY FLOW PASSED: menu -> preparation -> battle -> preparation -> menu; saved buttons, equipment and stats preserved.");
                        var config=new PrototypeBroadcastConfig();
                        config.left.slots[0]=new PrototypeBroadcastSlot {itemId="green_flying_sword",cooldown=1,range=20};
                        battle.BeginBroadcast(config);Next();break;
                    case 7:
                        Require(battle.IsBroadcast && battle.IsFighting,"broadcast failed to start");
                        if(battle.Elapsed<3)return;
                        Require(battle.LogicalFlyingSwordCount(battle.Player)>0,"broadcast did not summon logical swords");
                        broadcastBattleId=battle.Run.ActiveBattleId;
                        beforeDoubleSpeed=battle.Elapsed;battle.ToggleSpeed();Next();break;
                    case 8:
                        Require(battle.Elapsed-beforeDoubleSpeed>1.5f && Time.timeScale==2,"double speed did not advance simulation");
                        battle.Enemy.ReceiveDamage(battle.Enemy.MaximumHealth*2,PrototypeDamageKind.Melee);Next();break;
                    case 9:
                        Require(!battle.IsFighting && battle.Run.Phase==PrototypeRunPhase.Preparation,"broadcast did not close its battle receipt");
                        Require(battle.LogicalFlyingSwordCount(battle.Player)==0,"finished broadcast retained summons");
                        battle.Restart();Next();break;
                    case 10:
                        Require(battle.IsBroadcast && battle.IsFighting && battle.Run.Phase==PrototypeRunPhase.Fighting,"broadcast replay did not start");
                        Require(battle.Run.ActiveBattleId!=broadcastBattleId,"broadcast replay reused a completed battle ID");
                        battle.ReturnToMenu();Next();break;
                    case 11:
                        Require(scene=="MainMenu" && !battle.IsBroadcast && ReferenceEquals(battle.Run,run),"leaving broadcast lost campaign session");
                        Debug.Log("PKBall BROADCAST REPLAY AND DOUBLE SPEED PASSED");
                        System.IO.Directory.CreateDirectory("Artifacts/ArchitectureAudit");
                        System.IO.File.WriteAllText("Artifacts/ArchitectureAudit/scene-flow.txt","PASS: menu/preparation/battle/menu; UI raycast, click and drop; campaign session; broadcast swords; double speed; cleanup; replay; campaign restore.");
                        SessionState.SetBool("PKBall.AuthoredCheck", false);
                        EditorApplication.update -= Tick;
                        EditorApplication.isPlaying = false;
                        EditorApplication.Exit(0);
                        break;
                }
            }
            catch (Exception error)
            {
                SessionState.SetBool("PKBall.AuthoredCheck", false);
                Debug.LogException(error);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(1);
            }
        }
    }
}
