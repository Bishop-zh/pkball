using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PKBall.Prototype.Editor
{
    // Exercises the authored multi-panel editor, then records real physics battles.
    [InitializeOnLoad] public static class PrototypeSeasonSceneCheck
    {
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static int phase,characterIndex,matchIndex;
        private static double changed;
        private static string results="title,swapped,seed,seconds,result,leftHP,rightHP\n";
        private static T Field<T>(object o,string key)=>(T)o.GetType().GetField(key,Members).GetValue(o);
        static PrototypeSeasonSceneCheck(){if(SessionState.GetBool("PKBall.SeasonScene",false)){changed=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
        public static void Start()
        {
            SessionState.SetBool("PKBall.SeasonScene",true);phase=characterIndex=matchIndex=0;results="title,swapped,seed,seconds,result,leftHP,rightHP\n";changed=EditorApplication.timeSinceStartup;
            Directory.CreateDirectory("Artifacts/SeasonOne");EditorSceneManager.OpenScene("Assets/PKBall/Scenes/MainMenu.unity");EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Seed(){if(SessionState.GetBool("PKBall.SeasonScene",false))PrototypeSceneSession.Run=new PrototypeRun(true,true);}
        private static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
        private static void Next(){phase++;changed=EditorApplication.timeSinceStartup;}
        private static void Capture(PrototypeBattle battle,string name)
        {
            typeof(PrototypeBalancePresentationCheck).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{battle,"../SeasonOne/"+name});
        }
        private static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-changed>100)throw new Exception("Season scene timeout phase "+phase);
                if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup-changed<.5)return;
                var battle=UnityEngine.Object.FindObjectOfType<PrototypeBattle>();if(battle==null)return;
                var editor=UnityEngine.Object.FindObjectOfType<PrototypeBroadcastEditor>(true);
                if(phase==0)
                {
                    if(editor==null)return;
                    Field<Button>(editor,"entry").onClick.Invoke();
                    Require(Field<Dropdown>(editor,"characterPreset").options.Count>=24,"built-in character dropdown");Next();
                }
                else if(phase==1)
                {
                    Field<Dropdown>(editor,"characterPreset").value=++characterIndex;
                    var expected=PrototypeCharacterCatalog.Characters[characterIndex-1];
                    Require(Field<PrototypeBroadcastConfig>(editor,"config").left.name==expected.character.name,"editor selection "+expected.id);
                    Field<Button[]>(editor,"slotButtons")[0].onClick.Invoke();
                    for(int page=0;page<5;page++)Field<Button[]>(editor,"propertyTabs")[page].onClick.Invoke();
                    Require(!Field<Text>(editor,"message").text.Contains("<color="),"editor panel error");
                    changed=EditorApplication.timeSinceStartup;
                    if(characterIndex==PrototypeCharacterCatalog.Characters.Length)Next();
                }
                else if(phase==2)
                {
                    Field<Button>(editor,"rightTab").onClick.Invoke();Field<Dropdown>(editor,"characterPreset").SetValueWithoutNotify(0);Field<Dropdown>(editor,"characterPreset").value=18;
                    Require(Field<PrototypeBroadcastConfig>(editor,"config").right.name=="荆棘园丁","right side selection");
                    Field<Button[]>(editor,"slotButtons")[0].onClick.Invoke();Field<Button[]>(editor,"propertyTabs")[2].onClick.Invoke();Next();
                }
                else if(phase==3)
                {
                    Capture(battle,"editor-affixes.png");
                    Field<Button[]>(editor,"propertyTabs")[4].onClick.Invoke();Next();
                }
                else if(phase==4)
                {
                    Capture(battle,"editor-animation.png");
                    Field<Dropdown>(editor,"preset").value=2;
                    Require(Field<PrototypeBroadcastConfig>(editor,"config").title=="寒潭借甲","built-in match selection");
                    Field<PrototypeBroadcastConfig>(editor,"config").Copy().Validate();Next();
                }
                else if(phase==5)
                {
                    var matches=PrototypeCharacterCatalog.Matches;var config=PrototypeCharacterCatalog.Match(matches[matchIndex/2].id);
                    if(matchIndex%2==1){var swap=config.left;config.left=config.right;config.right=swap;}
                    battle.BeginBroadcast(config);Next();
                }
                else if(phase==6)
                {
                    if(!battle.IsBroadcast||!battle.IsFighting)return;Time.timeScale=6;Next();
                }
                else if(phase==7)
                {
                    if(battle.IsFighting)return;
                    results+=PrototypeBroadcastSession.Config.title+","+(matchIndex%2)+","+battle.CombatSeed+","+battle.Elapsed.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+","+battle.Result.Replace(","," ")+","+battle.Player.Health.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+","+battle.Enemy.Health.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"\n";
                    File.WriteAllText("Artifacts/SeasonOne/showcase-battles.csv",results);
                    matchIndex++;
                    if(matchIndex>=PrototypeCharacterCatalog.Matches.Length*2)
                    {
                        File.WriteAllText("Artifacts/SeasonOne/scene-checks.txt","PASS: 23 editor character selections, five property pages per character, right-side selection, built-in match selection, screenshots and 16 complete physics battles (eight pairs with swapped sides). Single sample per orientation, not a win-rate estimate.");
                        SessionState.SetBool("PKBall.SeasonScene",false);EditorApplication.update-=Tick;Time.timeScale=1;EditorApplication.isPlaying=false;EditorApplication.Exit(0);
                    }
                    else{phase=5;changed=EditorApplication.timeSinceStartup;}
                }
            }
            catch(Exception e){Debug.LogException(e);SessionState.SetBool("PKBall.SeasonScene",false);EditorApplication.update-=Tick;Time.timeScale=1;EditorApplication.Exit(1);}
        }
    }
}
