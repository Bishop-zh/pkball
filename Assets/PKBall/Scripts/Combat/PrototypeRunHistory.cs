using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PKBall.Prototype
{
    public static class PrototypeRunHistory
    {
        [Serializable] public sealed class Record
        {
            public int version=1, day, wins, endurance, level;
            public string id, savedAt, status, snapshot;
            public bool finished;
        }
        public static string DirectoryPath => Path.Combine(Application.persistentDataPath,"PKBallHistory");
        private static string RecordPath(string id)
        {
            if (!Guid.TryParseExact(id,"N",out _)) throw new ArgumentException("Invalid run id");
            return Path.Combine(DirectoryPath,id+".json");
        }
        public static void Store(PrototypeRun run,string status)
        {
            Directory.CreateDirectory(DirectoryPath);
            var record=new Record {id=run.RunId,day=run.Day,wins=run.Wins,endurance=run.Endurance,level=run.Level,
                savedAt=DateTime.UtcNow.ToString("o"),status=status,finished=run.GameOver,snapshot=run.ExportSnapshot()};
            string path=RecordPath(record.id);
            PrototypeRunSaveWriter.Enqueue(path,JsonUtility.ToJson(record,true));
            PrototypeRunSaveWriter.Flush(path);
        }
        public static List<Record> List()
        {
            var result=new List<Record>();
            if (!Directory.Exists(DirectoryPath)) return result;
            foreach(string path in Directory.GetFiles(DirectoryPath,"*.json"))
            {
                try {var r=JsonUtility.FromJson<Record>(File.ReadAllText(path));
                    if(r!=null && r.version==1 && Guid.TryParseExact(r.id,"N",out _) && !string.IsNullOrEmpty(r.snapshot)) result.Add(r);}
                catch(Exception e) {Debug.LogWarning("无法读取历史对局："+Path.GetFileName(path)+" / "+e.Message);}
            }
            result.Sort((a,b)=>string.CompareOrdinal(b.savedAt,a.savedAt));return result;
        }
        public static PrototypeRun Restore(string id, string activeSaveOverride = null)
        {
            var path=RecordPath(id); if(!File.Exists(path)) return null;
            var r=JsonUtility.FromJson<Record>(File.ReadAllText(path));
            if(r==null || r.finished) return null;
            // Read into a detached transient run first. A failed import never overwrites the active save.
            var run=new PrototypeRun(true,true);
            if(!run.ImportSnapshot(r.snapshot) || run.GameOver || run.Parked) return null;
            return PrototypeRun.FromHistorySnapshot(run.ExportSnapshot(),activeSaveOverride);
        }
    }
}
