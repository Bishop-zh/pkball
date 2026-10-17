using System.Collections.Generic;
using UnityEngine;
namespace PKBall.Prototype {
    // Real-time voice budgets affect presentation only; they never postpone gameplay events.
    public sealed class PrototypeAudioPlayback : MonoBehaviour {
        const int Capacity=16;readonly List<AudioSource> voices=new List<AudioSource>(Capacity);readonly Dictionary<string,float> last=new Dictionary<string,float>();float window;int starts,cursor;
        public int PlayCount {get;private set;}
        public void Initialize(){if(voices.Count>0)return;for(int i=0;i<Capacity;i++){var go=new GameObject("AudioVoice_"+i);go.transform.SetParent(transform,false);var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.dopplerLevel=0;source.priority=160;voices.Add(source);}EnsureListener();}
        static void EnsureListener(){foreach(var listener in Object.FindObjectsOfType<AudioListener>())if(listener.isActiveAndEnabled)return;var camera=Camera.main;if(camera==null)camera=Object.FindObjectOfType<Camera>();if(camera!=null){var listener=camera.GetComponent<AudioListener>();if(listener==null)listener=camera.gameObject.AddComponent<AudioListener>();listener.enabled=true;}}
        public void Play(string id,float volume=1,bool preview=false){var sound=PrototypeAudioLibrary.Find(id);if(sound?.clip==null||volume<=0)return;Initialize();float now=Time.unscaledTime;if(!preview){if(last.TryGetValue(id,out var previous)&&now-previous<.045f)return;if(now-window>=.1f){window=now;starts=0;}if(starts>=10)return;starts++;last[id]=now;}else Stop();
            AudioSource chosen=null;foreach(var voice in voices)if(!voice.isPlaying){chosen=voice;break;}if(chosen==null)chosen=voices[cursor++%voices.Count];chosen.Stop();chosen.clip=sound.clip;chosen.volume=Mathf.Clamp01(volume)*sound.volume;chosen.pitch=1;chosen.Play();PlayCount++;}
        public void Stop(){foreach(var voice in voices)voice.Stop();last.Clear();starts=0;}
        void OnDisable(){Stop();}
    }
}
