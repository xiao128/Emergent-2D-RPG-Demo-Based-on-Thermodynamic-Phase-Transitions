using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaAudio : MonoBehaviour
    {
        public static ArenaAudio Instance { get; private set; }
        public const string MusicPreference="PhaseArena.Audio.Music";
        public const string EffectsPreference="PhaseArena.Audio.Effects";
        [Header("三个场景的音乐")]
        public AudioClip menuMusic,normalMusic,bossMusic;
        [Header("界面与角色音效")]
        public AudioClip uiClick,uiHover,staffHit,playerHit,enemyShot,slimeDeath;
        public AudioClip lightCharge,mediumCharge,heavyCharge;
        public AudioClip stoneImpact,waterImpact,grassFootstep,iceFootstep;
        [Range(0,1), Tooltip("没有保存音量设置时的默认音乐音量。")]
        public float defaultMusicVolume=.65f;
        [Range(0,1), Tooltip("没有保存音量设置时的默认音效音量。")]
        public float defaultEffectsVolume=.8f;
        [Min(1), Tooltip("同时播放的游戏音效声道上限，界面点击有独立声道。")]
        public int effectVoices=16;
        [Min(1), Tooltip("游戏音效在距玩家此距离后不再播放，避免远处怪物干扰。")]
        public float audibleRadius=24;
        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }
        public AudioSource MusicSource { get; private set; }
        public AudioClip LastEffectClip { get; private set; }
        public int EffectPlays { get; private set; }
        AudioSource uiSource;
        AudioSource[] voices;
        float[] gains;
        int nextVoice;
        readonly Dictionary<AudioClip,float> lastPlayed=new Dictionary<AudioClip,float>();
        void Awake()
        {
            Instance=this;
            MusicVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(MusicPreference,defaultMusicVolume));
            EffectsVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsPreference,defaultEffectsVolume));
            MusicSource=Source("Music",true); MusicSource.loop=true; MusicSource.priority=0;
            uiSource=Source("UI Sounds",true);
            voices=new AudioSource[Mathf.Max(1,effectVoices)]; gains=new float[voices.Length];
            for(int i=0;i<voices.Length;i++) voices[i]=Source("Effect Voice "+i,false);
            RefreshMusic();
        }
        AudioSource Source(string name,bool ignorePause)
        {
            var go=new GameObject(name); go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>(); source.playOnAwake=false; source.spatialBlend=0;
            source.ignoreListenerPause=ignorePause; return source;
        }
        void Update() { RefreshMusic(); }
        public void RefreshMusic()
        {
            if(MusicSource==null) return;
            var g=ArenaDirector.Instance;
            AudioClip clip=g==null || g.State==RunState.Title ? menuMusic
                : g.Encounter.FinalWorld && g.Encounter.Started ? bossMusic : normalMusic;
            // Match the summon timeline to the music when pausing the encounter.
            if(MusicSource.clip==bossMusic && g!=null)
            { if(g.Paused && MusicSource.isPlaying) MusicSource.Pause(); else if(!g.Paused && !MusicSource.isPlaying) MusicSource.UnPause(); }
            if(MusicSource.clip==clip) return;
            MusicSource.Stop(); MusicSource.clip=clip; MusicSource.volume=MusicVolume;
            if(clip!=null) MusicSource.Play();
        }
        public void SetMusicVolume(float value)
        {
            MusicVolume=Mathf.Clamp01(value); if(MusicSource!=null) MusicSource.volume=MusicVolume;
            PlayerPrefs.SetFloat(MusicPreference,MusicVolume);
        }
        public void SetEffectsVolume(float value)
        {
            EffectsVolume=Mathf.Clamp01(value); uiSource.volume=EffectsVolume;
            for(int i=0;i<voices.Length;i++) voices[i].volume=EffectsVolume*gains[i];
            PlayerPrefs.SetFloat(EffectsPreference,EffectsVolume);
        }
        public void SavePreferences() { PlayerPrefs.Save(); }
        public void PlayUi(bool hover=false)
        {
            var clip=hover ? uiHover : uiClick;
            if(clip==null || EffectsVolume<=0) return;
            uiSource.volume=EffectsVolume; uiSource.PlayOneShot(clip,hover ? .35f : .7f);
            LastEffectClip=clip; EffectPlays++;
        }
        public void PlayWorld(AudioClip clip,Vector2 point,float gain=1,float repeatDelay=.08f)
        {
            if(clip==null || EffectsVolume<=0) return;
            var g=ArenaDirector.Instance;
            if(g==null || !g.SimulationActive) return;
            float distance=Vector2.Distance(point,g.player.Body.position);
            if(distance>=audibleRadius) return;
            float previous;
            if(lastPlayed.TryGetValue(clip,out previous) && Time.unscaledTime-previous<repeatDelay) return;
            lastPlayed[clip]=Time.unscaledTime;
            gain*=Mathf.Clamp01(1-distance/Mathf.Max(1,audibleRadius));
            int index=nextVoice++%voices.Length;
            for(int i=0;i<voices.Length;i++) if(!voices[i].isPlaying) {index=i; break;}
            var source=voices[index]; source.Stop(); source.clip=clip; gains[index]=Mathf.Clamp01(gain);
            source.volume=EffectsVolume*gains[index]; source.pitch=1; source.Play();
            LastEffectClip=clip; EffectPlays++;
        }
        public void PlayCharge(ThermoBody body,EnemyArchetype type)
        {
            if(body.kind==BodyKind.Boss) return;
            var clip=type==EnemyArchetype.Light ? lightCharge : type==EnemyArchetype.Heavy || type==EnemyArchetype.Guard ? heavyCharge : mediumCharge;
            PlayWorld(clip,body.Body.position,.7f);
        }
        public void StopEffects()
        {
            if(voices==null) return;
            foreach(var voice in voices) voice.Stop(); lastPlayed.Clear();
        }
        void OnApplicationQuit() { SavePreferences(); }
        void OnDestroy() { if(Instance==this) Instance=null; }
    }
}
