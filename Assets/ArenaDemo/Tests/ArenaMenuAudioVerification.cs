#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaMenuAudioVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>(); int failures;
        void Check(bool ok,string message) {results.Add((ok ? "PASS " : "FAIL ")+message); if(!ok) failures++;}
        public void Run() {StartCoroutine(Verify());}
        IEnumerator Verify()
        {
            var g=ArenaDirector.Instance; var h=g.hud; var m=h.GetComponent<ArenaMenuUI>(); var a=ArenaAudio.Instance;
            bool hadMusic=PlayerPrefs.HasKey(ArenaAudio.MusicPreference),hadEffects=PlayerPrefs.HasKey(ArenaAudio.EffectsPreference);
            float savedMusic=a.MusicVolume,savedEffects=a.EffectsVolume;
            var mage=g.player.GetComponent<PlayerMage>(); var xp=g.player.GetComponent<PlayerProgression>();
            try
            {
                m.ReturnToMainMenu(); yield return null;
                Check(g.State==RunState.Title && h.titlePanel.activeSelf,"Initial title is visible");
                Check(a.MusicSource.clip==a.menuMusic && a.MusicSource.isPlaying && a.MusicSource.loop,"Title music loops");
                Check(h.titlePanel.GetComponentInChildren<UnityEngine.UI.RawImage>().texture!=null,"Generated art is assigned");
                Check(h.titlePanel.GetComponentsInChildren<UnityEngine.UI.Button>().Length==3,"Title has exactly Start / Settings / Quit, no Continue");
                Check(a.GetComponentsInChildren<AudioSource>().Length==18,"Fixed 16 effect voices plus music and UI");
                Check(FindObjectsOfType<AudioListener>().Length==1,"Exactly one audio listener");
                Check(a.uiClick!=null && a.uiHover!=null && a.staffHit!=null && a.playerHit!=null && a.enemyShot!=null && a.slimeDeath!=null && a.lightCharge!=null && a.mediumCharge!=null && a.heavyCharge!=null && a.stoneImpact!=null && a.waterImpact!=null && a.grassFootstep!=null && a.iceFootstep!=null,"All thirteen provided effects assigned");
                m.settingsButton.onClick.Invoke(); yield return null;
                Check(g.Paused && Time.timeScale==0 && h.pausePanel.activeSelf && !m.mainMenuButton.gameObject.activeSelf,"Settings works on title; Return Main Menu is hidden");
                Check(m.resumeLabel.text=="关闭设置" && a.MusicSource.isPlaying,"Title settings has Close; music continues");
                m.musicSlider.value=.23f; m.effectsSlider.value=.47f;
                Check(Mathf.Approximately(a.MusicSource.volume,.23f) && Mathf.Approximately(a.EffectsVolume,.47f),"Independent sliders drive actual music/effects volume");
                Check(m.musicValue.text=="23%" && m.effectsValue.text=="47%","Slider percent labels update");
                h.resumeButton.onClick.Invoke(); yield return null;
                Check(!g.Paused && Time.timeScale==1 && g.State==RunState.Title && !h.pausePanel.activeSelf,"Closing title settings does not start a run");
                h.startButton.onClick.Invoke(); yield return null;
                foreach(var ai in FindObjectsOfType<EnemyBrain>()) ai.enabled=false;
                Check(g.State==RunState.Explore && g.World==1 && !h.titlePanel.activeSelf,"Start button begins the first world");
                Check(a.MusicSource.clip==a.normalMusic && a.MusicSource.isPlaying,"World 1 music playing");
                var worldProperty=typeof(ArenaDirector).GetProperty("World");
                for(int level=2;level<=7;level++) {worldProperty.SetValue(g,level); a.RefreshMusic(); Check(a.MusicSource.clip==a.normalMusic,"World "+level+" uses normal music");}
                worldProperty.SetValue(g,8); a.RefreshMusic();
                Check(a.MusicSource.clip==a.bossMusic && a.MusicSource.isPlaying,"Final world switches to boss music");
                worldProperty.SetValue(g,1); a.RefreshMusic();
                mage.enabled=false; a.StopEffects(); a.SetEffectsVolume(.47f);
                int plays=a.EffectPlays; mage.Melee(Vector2.right);
                Check(a.EffectPlays>plays && a.LastEffectClip==a.staffHit,"Actual F melee callback emits supplied staff sound");
                a.StopEffects(); plays=a.EffectPlays; g.Shoot(g.player,Vector2.right,105);
                Check(a.EffectPlays==plays,"Player cast does not borrow enemy casting sound");
                var enemy=g.Spawn(g.rangedEnemyPrefab,g.player.Body.position+Vector2.up*4); enemy.GetComponent<EnemyBrain>().enabled=false;
                // This fixture verifies emitted-shot audio; random obstacles must not block it.
                foreach(var b in g.bodies.ToArray())
                    if(b!=null && b!=enemy && b!=g.player && Vector2.Distance(b.Body.position,enemy.Body.position)<4) b.gameObject.SetActive(false);
                Physics2D.SyncTransforms();
                var enemyShot=g.Shoot(enemy,Vector2.right,105);
                Check(enemyShot!=null && a.LastEffectClip==a.enemyShot,"Enemy projectile callback emits shooting sound");
                enemy.Die("audio verification"); Check(a.LastEffectClip==a.slimeDeath,"Enemy death callback emits death sound");
                a.StopEffects(); g.player.Damage(1,"audio verification"); Check(a.LastEffectClip==a.playerHit,"Player actual health loss emits hit sound");
                a.StopEffects(); plays=a.EffectPlays; a.PlayWorld(a.stoneImpact,g.player.Body.position); a.PlayWorld(a.stoneImpact,g.player.Body.position);
                Check(a.EffectPlays==plays+1,"Repeated identical impact sounds are throttled");
                plays=a.EffectPlays; a.PlayWorld(a.waterImpact,g.player.Body.position+Vector2.right*(a.audibleRadius+1)); Check(a.EffectPlays==plays,"Distant effects are culled");
                a.SetEffectsVolume(0); plays=a.EffectPlays; a.PlayWorld(a.staffHit,g.player.Body.position); a.PlayUi(); Check(a.EffectPlays==plays && a.MusicSource.volume>.2f,"Effects mute is independent of music");
                a.SetEffectsVolume(.47f); a.PlayUi(); Check(a.LastEffectClip==a.uiClick,"UI click sound plays through its independent source");
                m.HandleEscape(); yield return null;
                Check(g.Paused && !g.SimulationActive && Time.timeScale==0 && h.pausePanel.activeSelf && m.mainMenuButton.gameObject.activeSelf,"Esc opens paused game settings");
                Check(a.MusicSource.isPlaying && m.resumeLabel.text=="返回游戏","Music continues while gameplay pauses");
                h.resumeButton.onClick.Invoke(); Check(g.SimulationActive && Time.timeScale==1,"Resume restores gameplay");
                typeof(ArenaDirector).GetProperty("State").SetValue(g,RunState.Upgrade); Time.timeScale=0;
                m.OpenSettings(); m.CloseSettings(); Check(Time.timeScale==0 && g.State==RunState.Upgrade,"Closing settings preserves law-choice time freeze");
                typeof(ArenaDirector).GetProperty("State").SetValue(g,RunState.Explore); Time.timeScale=1;
                xp.AwardKill(1000); g.laws.Add(WorldLaw.StoneMagic); mage.Cast(true,Vector2.right);
                Check(xp.Level>1 && mage.IsCharging,"Return test starts with growth, law and a charge preview");
                m.OpenSettings(); m.mainMenuButton.onClick.Invoke(); yield return null;
                Check(g.State==RunState.Title && g.World==0 && g.laws.Count==0 && g.Fragments==0 && !g.Paused && Time.timeScale==1,"Return button clears run state");
                Check(g.entities.childCount==0 && g.shards.Count==0 && !g.worldGenerator.generatedRoot.gameObject.activeSelf,"Return removes actors/projectiles/shards and hides generated world");
                Check(xp.Level==1 && xp.Experience==0 && g.player.health==g.player.maxHealth && Mathf.Approximately(mage.maxMana,20),"Return resets player growth and health/mana");
                Check(!mage.IsCharging && GameObject.Find("Spell charge preview")==null,"Return cancels charge preview");
                Check(a.MusicSource.clip==a.menuMusic && !h.pausePanel.activeSelf,"Return restores title music and hides settings");
                h.startButton.onClick.Invoke(); yield return null;
                Check(g.World==1 && xp.Level==1 && g.laws.Count==0 && !g.player.Dead,"Starting again creates a fresh playable run");
                m.ReturnToMainMenu(); mage.enabled=true;
                Check(a.GetComponentsInChildren<AudioSource>().Length==18,"Changing worlds and runs does not grow audio source count");
            }
            finally
            {
                mage.enabled=true; a.SetMusicVolume(savedMusic); a.SetEffectsVolume(savedEffects);
                if(!hadMusic) PlayerPrefs.DeleteKey(ArenaAudio.MusicPreference); if(!hadEffects) PlayerPrefs.DeleteKey(ArenaAudio.EffectsPreference); PlayerPrefs.Save();
                var report="failures="+failures+"\n"+string.Join("\n",results);
                File.WriteAllText("Verification/menu-audio-regression.txt",report); Debug.Log("[MenuAudioVerification] "+report);
            }
            enabled=false;
        }
    }
}
#endif
