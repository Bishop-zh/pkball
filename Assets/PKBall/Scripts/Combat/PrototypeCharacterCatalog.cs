using System;
using UnityEngine;

namespace PKBall.Prototype
{
    [Serializable] public sealed class PrototypeCharacterProfile
    {
        public string id, tier, story, hook, counter, artNote;
        public PrototypeBroadcastSide character;
    }
    [Serializable] public sealed class PrototypeShowcaseMatch
    {
        public string id, title, leftId, rightId, premise;
    }
    [Serializable] public sealed class PrototypeCharacterData
    {
        public int version;
        public PrototypeCharacterProfile[] characters;
        public PrototypeShowcaseMatch[] matches;
    }
    // Built-ins are shipped resources; loading always returns a copy. User files stay independent.
    public static class PrototypeCharacterCatalog
    {
        private static PrototypeCharacterData data;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { data=null; }
        private static PrototypeCharacterData Data
        {
            get
            {
                if(data!=null)return data;
                var resource=Resources.Load<TextAsset>("Data/characters");
                if(resource==null)throw new FormatException("缺少内置角色目录");
                var parsed=JsonUtility.FromJson<PrototypeCharacterData>(resource.text);
                if(parsed==null || parsed.version!=1 || parsed.characters==null || parsed.matches==null)
                    throw new FormatException("内置角色目录版本无效");
                data=parsed;return data;
            }
        }
        public static PrototypeCharacterProfile[] Characters=>JsonUtility.FromJson<PrototypeCharacterData>(JsonUtility.ToJson(Data)).characters;
        public static PrototypeShowcaseMatch[] Matches=>JsonUtility.FromJson<PrototypeCharacterData>(JsonUtility.ToJson(Data)).matches;
        public static PrototypeCharacterProfile Find(string id)
        {
            foreach(var profile in Data.characters)if(profile.id==id)
                return JsonUtility.FromJson<PrototypeCharacterProfile>(JsonUtility.ToJson(profile));
            throw new ArgumentException("未知内置角色："+id);
        }
        public static PrototypeBroadcastConfig Match(string id)
        {
            foreach(var match in Data.matches)if(match.id==id)
            {
                var config=new PrototypeBroadcastConfig {title=match.title,left=Find(match.leftId).character,right=Find(match.rightId).character};
                config.Validate();return config;
            }
            throw new ArgumentException("未知内置对战："+id);
        }
    }
}
