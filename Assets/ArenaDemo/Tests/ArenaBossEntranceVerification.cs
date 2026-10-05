#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaBossEntranceVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        int failures;
        ArenaDirector g;
        void Check(bool ok,string label) {results.Add((ok?"PASS ":"FAIL ")+label); if(!ok) failures++;}
        public void Run() {StartCoroutine(Verify());}
        void DisableAttackers()
        {
            foreach(var b in g.bodies.ToArray()) if(b!=null && b.IsEnemy) {var brain=b.GetComponent<EnemyBrain>(); if(brain!=null) brain.enabled=false; var combat=b.GetComponent<BossCombat>(); if(combat!=null) combat.enabled=false; b.Body.velocity=Vector2.zero;}
        }
        IEnumerator Verify()
        {
            g=ArenaDirector.Instance; g.Restart();
            typeof(ArenaDirector).GetProperty("World").SetValue(g,9);
            typeof(ArenaDirector).GetMethod("BuildWorld",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,null);
            g.player.indestructible=true; g.player.GetComponent<PlayerMage>().enabled=false;
            Check(g.State==RunState.Explore && g.bodies.FindAll(b=>b!=null&&b.IsEnemy).Count==0,"Ninth world initially has zero enemies including guards and boss");
            ArenaAudio.Instance.RefreshMusic(); Check(ArenaAudio.Instance.MusicSource.clip==ArenaAudio.Instance.normalMusic,"Final world exploration keeps normal music until activation");
            Check(!g.TryOpenClock(),"Distant player cannot activate the clock");
            g.player.Body.position=g.clock.position; g.player.transform.position=g.clock.position;
            Check(g.TryOpenClock() && g.Encounter.Phase==BossEncounterPhase.Summoning,"Clock interaction starts the summoning phase");
            var expected=g.worldGenerator.SafeDryPosition((Vector2)g.clock.position+Vector2.left*g.tuning.bossPlayerTeleportDistance,g.player.radius+.3f);
            Check(Vector2.Distance(g.player.Body.position,expected)<.01f && g.player.Body.position.x<g.clock.position.x-3 && g.player.Body.velocity==Vector2.zero,"Player teleports left to safe dry position with no residual motion");
            var face=g.clock.Find("Clock Face").GetComponent<SpriteRenderer>();
            Check(face.color==g.tuning.activeBossClockColor,"World clock turns blue");
            Check(ArenaAudio.Instance.MusicSource.clip==ArenaAudio.Instance.bossMusic && ArenaAudio.Instance.MusicSource.time<.2f,"Boss BGM starts at the beginning of the ritual");
            g.player.GetComponent<PlayerMage>().enabled=false;
            while(g.Encounter.Elapsed<1.2f) yield return null;
            g.SetPaused(true); ArenaAudio.Instance.RefreshMusic(); float time=g.Encounter.Elapsed,music=ArenaAudio.Instance.MusicSource.time;
            yield return new WaitForSecondsRealtime(.6f);
            Check(g.Encounter.Elapsed==time && Mathf.Abs(ArenaAudio.Instance.MusicSource.time-music)<.1f,"Pause freezes both ritual countdown and boss music");
            g.SetPaused(false); ArenaAudio.Instance.RefreshMusic();
            while(g.Encounter.Elapsed<2.8f) yield return null;
            Check(g.Encounter.MinionsSpawned==0 && g.Encounter.Boss==null,"No enemy appears before three seconds");
            while(g.Encounter.Elapsed<3.2f) yield return null; DisableAttackers();
            Check(g.Encounter.MinionsSpawned==1,"First random minion appears at three seconds");
            while(g.Encounter.Elapsed<4.2f) yield return null; DisableAttackers();
            Check(g.Encounter.MinionsSpawned==2,"Second random minion appears one second later");
            while(g.Encounter.Elapsed<14.8f) {DisableAttackers(); yield return null;}
            Check(g.Encounter.Boss==null && g.Encounter.MinionsSpawned==12,"Twelve minions stage the entrance from second 3 through 14");
            while(g.Encounter.Phase==BossEncounterPhase.Summoning) {DisableAttackers(); yield return null;}
            DisableAttackers(); var boss=g.Encounter.Boss;
            Check(boss!=null && g.Encounter.Elapsed>=15 && g.Encounter.Elapsed<15.15f,"Boss arrives at 15 game seconds");
            Check(Vector2.Distance(boss.Body.position,g.clock.position)<.2f,"Boss appears at world clock position");
            Check(Mathf.Abs(ArenaAudio.Instance.MusicSource.time-15)<.4f,"Boss arrival aligns with the fifteen-second music cue");
            results.Add("MEASURE arrival="+g.Encounter.Elapsed+" music="+ArenaAudio.Instance.MusicSource.time+" minions="+g.Encounter.MinionsSpawned);
            g.Encounter.Tick(1); Check(g.Encounter.MinionsSpawned==12,"Intro minion spawning stops after boss arrival");
            var player=g.player; boss.Die("Final sequence verification");
            Check(g.State==RunState.Boss && g.Encounter.Phase==BossEncounterPhase.Cleared && g.player==player && !g.Outcome.ResultReady,"Boss death retains player and waits for final clock interaction");
            Check(!g.TryOpenClock(),"Player must return to clock to claim completion");
            player.Body.position=g.clock.position; player.transform.position=g.clock.position;
            Check(g.TryOpenClock() && g.State==RunState.Victory && g.player==null && !g.Outcome.ResultReady,"Second clock interaction completes run and removes player before popup");
            yield return new WaitForSecondsRealtime(5.1f); yield return null;
            Check(g.Outcome.ResultReady && g.hud.resultPanel.activeSelf,"Existing five-second result delay remains in effect");
            g.Restart(); Check(g.World==1 && !g.Encounter.Started && face.color!=g.tuning.activeBossClockColor,"Restart resets encounter and clock color");
            string report="failures="+failures+"\n"+string.Join("\n",results); File.WriteAllText("Verification/boss-entrance-regression.txt",report); Debug.Log(report); enabled=false;
        }
    }
}
#endif
