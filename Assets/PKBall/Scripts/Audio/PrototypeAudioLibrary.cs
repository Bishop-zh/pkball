using System;
using System.Collections.Generic;
using UnityEngine;
namespace PKBall.Prototype {
    [Serializable] public sealed class PrototypeSoundEntry { public string id,displayName,phase="both";public float volume=.55f;[NonSerialized]public AudioClip clip; }
    [Serializable] public sealed class PrototypeSoundFile {public PrototypeSoundEntry[] sounds;}
    // Stable IDs separate authoring, preset persistence and playback. New clips are discovered once per session.
    public static class PrototypeAudioLibrary {
        public const string None="none";private static Dictionary<string,PrototypeSoundEntry> byId;private static List<PrototypeSoundEntry> all;
        public static IReadOnlyList<PrototypeSoundEntry> All {get{Ensure();return all;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){byId=null;all=null;}
        static void Ensure(){if(byId!=null)return;byId=new Dictionary<string,PrototypeSoundEntry>();all=new List<PrototypeSoundEntry>();
            var names=new Dictionary<string,PrototypeSoundEntry>();var data=Resources.Load<TextAsset>("Data/sounds");
            if(data!=null)try{var file=JsonUtility.FromJson<PrototypeSoundFile>(data.text);if(file?.sounds!=null)foreach(var e in file.sounds)if(e!=null&&!string.IsNullOrEmpty(e.id)&&!names.ContainsKey(e.id))names.Add(e.id,e);}catch(Exception e){Debug.LogWarning("Sound metadata fallback: "+e.Message);}
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/Sfx")){if(byId.ContainsKey(clip.name)){Debug.LogWarning("Duplicate sound ID: "+clip.name);continue;}if(!names.TryGetValue(clip.name,out var e))e=new PrototypeSoundEntry {id=clip.name,displayName=clip.name};e.clip=clip;if(string.IsNullOrEmpty(e.displayName))e.displayName=e.id;e.volume=float.IsNaN(e.volume)||float.IsInfinity(e.volume)?.55f:Mathf.Clamp01(e.volume);byId.Add(e.id,e);all.Add(e);}all.Sort((a,b)=>string.CompareOrdinal(a.displayName,b.displayName));}
        public static PrototypeSoundEntry Find(string id){Ensure();return !string.IsNullOrEmpty(id)&&byId.TryGetValue(id,out var e)?e:null;}
        public static bool Valid(string id)=>string.IsNullOrEmpty(id)||id==None||Find(id)!=null;
        public static string Name(string id)=>id==None?"静音":Find(id)?.displayName??"无音效";
        public static string ResolveRelease(PrototypeItemDefinition d){if(d==null)return None;if(!string.IsNullOrEmpty(d.releaseSoundId))return d.releaseSoundId;var w=d.weapon;
            if(w!=null){if(w.kind==PrototypeWeaponKind.Melee)return w.attackShape==PrototypeAttackShape.Smash?"heavy_swing":"sword_swing";if(w.visualStyle==PrototypeAttackVisual.Beam)return "laser_fire";if(w.visualStyle==PrototypeAttackVisual.Volley)return "arrow_fire";return d.displayName.Contains("炮")?"cannon_fire":d.displayName.Contains("枪")?"gun_fire":w.projectileVisual==PrototypeProjectileVisual.Bolt?"laser_fire":"arrow_fire";}
            return Effect(d);}
        public static string ResolveHit(PrototypeItemDefinition d){if(d==null)return None;if(!string.IsNullOrEmpty(d.hitSoundId))return d.hitSoundId;string effect=Effect(d);if(effect!=None)return effect;return d.weapon!=null?"metal_hit":None;}
        static string Effect(PrototypeItemDefinition d){if(d.effects!=null)foreach(var e in d.effects)if(e.amount>0){if(e.kind==PrototypeEffectKind.Poison)return "poison_hit";if(e.kind==PrototypeEffectKind.Burn)return "fire_hit";if(e.kind==PrototypeEffectKind.Shield)return "shield_gain";if(e.kind==PrototypeEffectKind.Heal||e.kind==PrototypeEffectKind.Regeneration)return "heal_chime";}return None;}
    }
}
