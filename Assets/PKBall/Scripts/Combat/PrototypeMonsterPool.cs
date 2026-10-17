using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace PKBall.Prototype
{
    [Serializable] public sealed class PrototypeMonsterPreset
    {
        public string id;
        public PrototypeEncounterTier tier;
        public int referenceDay=1;
        public float healthGrowth=.12f,outputGrowth=.08f;
        public PrototypeBroadcastSide character;
        public PrototypeMonsterPreset Copy()=>JsonUtility.FromJson<PrototypeMonsterPreset>(JsonUtility.ToJson(this));
        public void Validate()
        {
            if(string.IsNullOrEmpty(id)||!Guid.TryParseExact(id,"N",out _)||(int)tier<0||(int)tier>2||referenceDay<1||referenceDay>10000||!ValidRate(healthGrowth)||!ValidRate(outputGrowth)||character==null)
                throw new FormatException("怪物预设参数无效");
            new PrototypeBroadcastConfig {left=character,right=character}.Validate();
        }
        private static bool ValidRate(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value)&&value>=0&&value<=1;
        public PrototypeBroadcastSide ScaleToDay(int day)
        {
            Validate();
            var side=Copy().character;
            float hp=Mathf.Clamp(Mathf.Pow(1+healthGrowth,Mathf.Clamp(day,1,10000)-referenceDay),.05f,1000);
            float output=Mathf.Clamp(Mathf.Pow(1+outputGrowth,Mathf.Clamp(day,1,10000)-referenceDay),.05f,1000);
            side.health=Mathf.Clamp(side.health*hp,1,100000000);
            side.shield*=output;
            foreach(var slot in side.slots) if(slot!=null)
            { slot.monsterHealthScale=hp;slot.monsterOutputScale=output; }
            return side;
        }
    }
    public static class PrototypeMonsterPool
    {
        private static List<PrototypeMonsterPreset> cached;
        private static string directoryOverride;
        public static string DirectoryPath=>directoryOverride??Path.Combine(Application.persistentDataPath,"monster-pool");
        public static void SetDirectoryForTests(string directory){directoryOverride=directory;Reset();}
        public static void Reset(){cached=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeReset(){directoryOverride=null;Reset();}
        private static void Ensure()
        {
            if(cached!=null)return;
            cached=new List<PrototypeMonsterPreset>();
            if(!Directory.Exists(DirectoryPath))return;
            var paths=Directory.GetFiles(DirectoryPath,"*.json");Array.Sort(paths,StringComparer.Ordinal);
            var seen=new HashSet<string>();
            foreach(string path in paths)
            {
                try
                {
                    var preset=JsonUtility.FromJson<PrototypeMonsterPreset>(File.ReadAllText(path));
                    if(preset==null)throw new FormatException("预设为空");preset.Validate();
                    if(Path.GetFileNameWithoutExtension(path)!=preset.id || !seen.Add(preset.id))throw new FormatException("预设ID与文件不一致或重复");
                    cached.Add(preset);
                }
                catch(Exception error){Debug.LogWarning("跳过损坏怪物预设 "+Path.GetFileName(path)+": "+error.Message);}
            }
            cached.Sort((a,b)=>string.CompareOrdinal(a.id,b.id));
        }
        public static IReadOnlyList<PrototypeMonsterPreset> All(PrototypeEncounterTier tier)
        {Ensure();var result=new List<PrototypeMonsterPreset>();foreach(var preset in cached)if(preset.tier==tier)result.Add(preset.Copy());return result.AsReadOnly();}
        public static int Count(PrototypeEncounterTier tier){Ensure();int count=0;foreach(var preset in cached)if(preset.tier==tier)count++;return count;}
        public static PrototypeMonsterPreset Find(string id){Ensure();foreach(var preset in cached)if(preset.id==id)return preset.Copy();return null;}
        public static PrototypeMonsterPreset Save(PrototypeBroadcastSide side,PrototypeEncounterTier tier,int referenceDay)
        {
            Ensure();
            var previous=cached.Find(p=>p.tier==tier&&p.character.name==side?.name);
            var preset=new PrototypeMonsterPreset {id=previous?.id??Guid.NewGuid().ToString("N"),tier=tier,referenceDay=referenceDay,
                healthGrowth=previous?.healthGrowth??.12f,outputGrowth=previous?.outputGrowth??.08f,
                character=side==null?null:JsonUtility.FromJson<PrototypeBroadcastSide>(JsonUtility.ToJson(side))};
            preset.Validate();Directory.CreateDirectory(DirectoryPath);
            string path=Path.Combine(DirectoryPath,preset.id+".json"),temporary=path+".tmp";
            try
            {
                File.WriteAllText(temporary,JsonUtility.ToJson(preset,true));
                if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
            }
            finally {if(File.Exists(temporary))File.Delete(temporary);}
            Reset();Ensure();return preset.Copy();
        }
    }
}
