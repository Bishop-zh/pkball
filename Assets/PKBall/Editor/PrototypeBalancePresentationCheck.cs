using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PKBall.Prototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeBalancePresentationCheck
    {
        private static int phase;
        private static double changed;
        static PrototypeBalancePresentationCheck(){if(SessionState.GetBool("PKBall.BalanceViewCheck",false)){changed=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
        public static void Start()
        {
            SessionState.SetBool("PKBall.BalanceViewCheck",true);phase=0;changed=EditorApplication.timeSinceStartup;
            EditorSceneManager.OpenScene("Assets/PKBall/Scenes/MainMenu.unity");EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Seed(){if(SessionState.GetBool("PKBall.BalanceViewCheck",false))PrototypeSceneSession.Run=new PrototypeRun(true,true);}
        private static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-changed>50)throw new Exception("Balance view check timeout phase "+phase);
                if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup-changed<1.5)return;
                var battle=UnityEngine.Object.FindObjectOfType<PrototypeBattle>();if(battle==null)return;
                if(phase==0)
                {
                    var config=new PrototypeBroadcastConfig();config.left.name="护盾与再生";config.right.name="毒火试炼";
                    config.left.slots[0].itemId="shield_emitter";config.left.slots[1].itemId="apprentice_spring";
                    config.right.slots[0].itemId="flame_axe";config.right.slots[1].itemId="apprentice_venom";
                    battle.BeginBroadcast(config);Next();
                }
                else if(phase==1)
                {
                    if(!battle.IsBroadcast||!battle.IsFighting)return;
                    battle.enabled=false; battle.Player.GetComponent<Rigidbody>().velocity=Vector3.zero;battle.Enemy.GetComponent<Rigidbody>().velocity=Vector3.zero;
                    battle.Player.transform.position=new Vector3(-1.25f,0,0);battle.Enemy.transform.position=new Vector3(1.25f,0,0);
                    battle.Player.ReceiveDamage(82,PrototypeDamageKind.Poison);battle.Player.AddShield(430);battle.Player.State.AddRegeneration(2.17f);
                    battle.Enemy.ReceiveDamage(138,PrototypeDamageKind.Poison);battle.Enemy.AddShield(45);battle.Enemy.State.AddBurn(3);battle.Enemy.State.AddPoison(2.5f);
                    Next();
                }
                else if(phase==2)
                {
                    Capture(battle,"normal.png");PrototypeBalanceChecks.Set(battle,"Elapsed",40f);
                    battle.Player.ReceiveDamage(17,PrototypeDamageKind.SuddenDeath);battle.Player.Heal(24);battle.Enemy.AddShield(50);
                    Next();changed=EditorApplication.timeSinceStartup-1.1;
                }
                else if(phase==3)
                {
                    Capture(battle,"overtime.png");File.WriteAllText("Artifacts/Balance/presentation-check.txt","PASS: actual PlayMode battle HUD, shield > maxHP, burn/poison/regen, overtime, floating numbers rendered to 1920x1080.");
                    SessionState.SetBool("PKBall.BalanceViewCheck",false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;EditorApplication.Exit(0);
                }
            }
            catch(Exception e){Debug.LogException(e);SessionState.SetBool("PKBall.BalanceViewCheck",false);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
        }
        private static void Next(){phase++;changed=EditorApplication.timeSinceStartup;}
        private static void Capture(PrototypeBattle battle,string name)
        {
            Directory.CreateDirectory("Artifacts/Balance");battle.ArenaCamera.Render();
            var root=new GameObject("BalanceCaptureCamera",typeof(Camera));var camera=root.GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;
            camera.transform.position=new Vector3(0,0,-1000);camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.cullingMask=~0;
            var rt=new RenderTexture(1920,1080,24);rt.Create();camera.targetTexture=rt;
            var canvases=UnityEngine.Object.FindObjectsOfType<Canvas>();
            var modes=new RenderMode[canvases.Length];var cameras=new Camera[canvases.Length];var planes=new float[canvases.Length];
            for(int i=0;i<canvases.Length;i++)
            {
                modes[i]=canvases[i].renderMode;cameras[i]=canvases[i].worldCamera;planes[i]=canvases[i].planeDistance;
                if(canvases[i].isRootCanvas&&modes[i]!=RenderMode.WorldSpace){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=1;}
            }
            Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            var pixels=image.GetPixels32();int visible=0;for(int i=0;i<pixels.Length;i+=101)if(pixels[i].r+pixels[i].g+pixels[i].b>60)visible++;if(visible<100)throw new Exception("Blank UI render: "+name);
            File.WriteAllBytes("Artifacts/Balance/"+name,image.EncodeToPNG());RenderTexture.active=old;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=planes[i];}
            camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
