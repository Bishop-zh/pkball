using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
namespace PKBall.Prototype
{
    // Only immutable JSON crosses threads. One writer per path; later snapshots can replace queued older snapshots.
    internal static class PrototypeRunSaveWriter
    {
        private sealed class Writer {public string pending;public Task task;public Exception error;public readonly object gate=new object();}
        private static readonly Dictionary<string,Writer> writers=new Dictionary<string,Writer>();
        private static Writer Get(string path)
        {
            lock(writers) {if(!writers.TryGetValue(path,out var value)) {value=new Writer();writers.Add(path,value);}return value;}
        }
        public static void Enqueue(string path,string snapshot)
        {
            var writer=Get(path);
            lock(writer.gate)
            {
                writer.pending=snapshot;
                if(writer.task==null || writer.task.IsCompleted) writer.task=Task.Run(()=>Drain(path,writer));
            }
        }
        private static void Drain(string path,Writer writer)
        {
            while(true)
            {
                string snapshot;
                lock(writer.gate)
                {
                    if(writer.pending==null) {writer.task=null;return;}
                    snapshot=writer.pending;writer.pending=null;
                }
                try
                {
                    WriteAtomically(path,snapshot);
                    lock(writer.gate) writer.error=null;
                }
                catch(Exception error)
                {
                    // Keep the most recent snapshot available for the next checkpoint to retry.
                    lock(writer.gate) {writer.error=error;if(writer.pending==null) writer.pending=snapshot;writer.task=null;}
                    return;
                }
            }
        }
        private static void WriteAtomically(string path,string snapshot)
        {
            // Windows file watchers can briefly hold the destination without delete sharing.
            // Retry the same atomic operation; never delete the old save as a fallback.
            for(int attempt=0;;attempt++)
            {
                try
                {
                    string temp=path+".tmp";File.WriteAllText(temp,snapshot);
                    if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
                    return;
                }
                catch(IOException) when(attempt<3) {System.Threading.Thread.Sleep(25*(attempt+1));}
            }
        }
        public static void Flush(string path)
        {
            var writer=Get(path);Task task;
            lock(writer.gate) task=writer.task;
            task?.Wait();
            lock(writer.gate) if(writer.error!=null) throw new IOException("PKBall save failed",writer.error);
        }
    }
}
