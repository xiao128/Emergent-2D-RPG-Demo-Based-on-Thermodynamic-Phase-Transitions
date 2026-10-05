#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PhaseArena
{
    // State-transition fixture, not a balance or natural-playthrough claim.
    public sealed class ArenaProgressionVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        int failures;
        ArenaDirector world;
        void Check(bool ok,string text) { results.Add((ok ? "PASS " : "FAIL ")+text); if(!ok) failures++; }
        public void Run() { world=ArenaDirector.Instance; StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            world.Restart(); world.player.GetComponent<PlayerMage>().enabled=false;
            for(int level=1;level<world.tuning.worldCount;level++)
            {
                foreach(var ai in FindObjectsOfType<EnemyBrain>()) ai.enabled=false;
                var guards=world.bodies.FindAll(b=>b!=null && !b.Dead && b.isElite);
                Check(world.World==level && guards.Count==2,"World "+level+" has two clock guards");
                foreach(var guard in guards) guard.Damage(guard.health+1,"流程验证",DamageKind.Staff);
                foreach(var shard in world.shards.ToArray()) world.Collect(shard);
                world.player.Body.position=world.clock.position; Physics2D.SyncTransforms();
                Check(world.Fragments==2 && world.TryOpenClock(),"World "+level+" fragments unlock the clock");
                Check(System.Array.TrueForAll(world.Choices,WorldLawCatalog.IsAvailable),"World "+level+" offers only current document rules");
                var choice=world.Choices[0]; Check(world.ChooseLaw(0),"World "+level+" choice starts transition");
                float deadline=Time.realtimeSinceStartup+6;
                while(world.World==level && Time.realtimeSinceStartup<deadline) yield return null;
                Check(world.World==level+1 && world.Has(choice),"World "+level+" advances and retains its rule");
            }
            foreach(var ai in FindObjectsOfType<EnemyBrain>()) ai.enabled=false;
            var boss=world.bodies.Find(b=>b!=null && !b.Dead && b.kind==BodyKind.Boss);
            Check(world.World==9 && world.State==RunState.Explore && boss==null && world.bodies.FindAll(b=>b!=null && b.IsEnemy && !b.Dead).Count==0,"Ninth world starts empty and waits for clock activation");
            world.player.Body.position=world.clock.position; world.player.transform.position=world.clock.position;
            Check(world.TryOpenClock() && world.State==RunState.Boss,"Final clock starts the encounter");
            world.Encounter.Tick(world.tuning.bossEncounterSpawnDelay);
            boss=world.Encounter.Boss;
            Check(boss!=null,"Boss appears after the summon timeline");
            if(boss!=null) boss.Damage(boss.health+1,"流程验证",DamageKind.Staff);
            Check(world.State==RunState.Boss && world.player!=null && world.Encounter.Phase==BossEncounterPhase.Cleared,"Boss defeat waits for final clock interaction");
            world.player.Body.position=world.clock.position; world.player.transform.position=world.clock.position;
            Check(world.TryOpenClock() && world.State==RunState.Victory,"Final clock interaction enters victory");
            world.Restart();
            Check(world.World==1 && world.laws.Count==0 && world.Fragments==0,"Restart clears progress and laws");
            world.player.health=5;
            world.player.Damage(5,"流程验证",DamageKind.Impact);
            Check(world.State==RunState.Defeat,"Player death enters defeat");
            world.Restart();
            Check(world.State==RunState.Explore && !world.player.Dead,"Restart after defeat restores playable state");
            string report="failures="+failures+"\n"+string.Join("\n",results);
            File.WriteAllText("Verification/progression-regression.txt",report);
            Debug.Log("[ProgressionVerification] "+report); enabled=false;
        }
    }
}
#endif
