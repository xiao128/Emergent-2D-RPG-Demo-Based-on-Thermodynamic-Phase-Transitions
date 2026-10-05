#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    public static class ArenaCrowdRiverBossVerification
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static ArenaDirector g;
        static readonly List<ThermoBody> fixtures=new List<ThermoBody>();
        static readonly List<GameObject> objects=new List<GameObject>();
        static readonly List<string> results=new List<string>();
        static int failures;
        static void Check(bool ok,string text) { results.Add((ok ? "PASS " : "FAIL ")+text); if(!ok) failures++; }
        static void Place(ThermoBody body,Vector2 point) { body.transform.position=point; body.Body.position=point; if(body.Body.bodyType==RigidbodyType2D.Dynamic) body.Body.velocity=Vector2.zero; }
        static ThermoBody Fixture(Vector2 point,BodyKind kind=BodyKind.Projectile,ThermoBody prefab=null)
        {
            var b=g.Spawn(prefab!=null ? prefab : g.projectilePrefab,point); fixtures.Add(b);
            b.kind=kind; b.shotHeat=0; b.health=b.maxHealth=10000;
            var brain=b.GetComponent<EnemyBrain>(); if(brain!=null) brain.enabled=false;
            if(kind==BodyKind.Tree || kind==BodyKind.StaticObstacle) b.Body.bodyType=RigidbodyType2D.Static;
            return b;
        }
        static void Clear()
        {
            foreach(var b in fixtures) if(b!=null) {b.gameObject.SetActive(false); UnityEngine.Object.Destroy(b.gameObject);} fixtures.Clear();
            foreach(var o in objects) if(o!=null) {o.SetActive(false); UnityEngine.Object.Destroy(o);} objects.Clear();
            g.laws.Clear(); g.Collisions.Clear(); g.player.ResetAt(new Vector2(-32,-24)); g.player.transform.position=g.player.Body.position;
            Physics2D.SyncTransforms();
        }
        static void ForceScan(ThermoBody b)
        {
            var status=(CrowdingStatus)typeof(ThermoBody).GetField("crowding",Hidden).GetValue(b);
            typeof(CrowdingStatus).GetField("<NextScanAt>k__BackingField",Hidden).SetValue(status,Time.time-.01f);
        }
        public static string Run()
        {
            g=ArenaDirector.Instance; g.Restart(); g.enabled=false; Time.timeScale=0;
            g.player.GetComponent<PlayerMage>().enabled=false;
            foreach(var b in g.bodies.ToArray()) if(b!=null && b!=g.player) b.gameObject.SetActive(false);
            results.Clear(); failures=0; Clear();
            var originalMode=Physics2D.simulationMode;
            Physics2D.simulationMode=SimulationMode2D.Script;
            float interval=g.tuning.crowdingCheckInterval;
            try { Crowding(); Clear(); River(); Clear(); Fragments(); Clear(); Boss(); }
            catch(Exception ex) {Check(false,"Exception: "+ex);}
            finally {g.tuning.crowdingCheckInterval=interval; Clear(); Physics2D.simulationMode=originalMode;}
            string report="failures="+failures+"\n"+string.Join("\n",results);
            File.WriteAllText("Verification/crowd-river-boss-regression.txt",report); Debug.Log("[CrowdRiverBoss] "+report); return report;
        }
        static void Crowding()
        {
            g.laws.Add(WorldLaw.Crowding); g.tuning.crowdingCheckInterval=1;
            var victim=Fixture(new Vector2(-20.1f,-20.1f),BodyKind.Enemy);
            var kinds=new[]{BodyKind.Projectile,BodyKind.Rock,BodyKind.Tree,BodyKind.StaticObstacle,BodyKind.Enemy,BodyKind.Boss,BodyKind.Projectile};
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;
                Fixture(victim.Body.position+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*2.2f,kinds[i]);
            }
            Physics2D.SyncTransforms();
            Check(g.LawSimulation.CountCrowdingNeighbors(victim)==7,"Counts seven mixed entities across former grid boundaries, excluding self");
            fixtures[1].gameObject.AddComponent<BoxCollider2D>().size=Vector2.one*.1f; Physics2D.SyncTransforms();
            Check(g.LawSimulation.CountCrowdingNeighbors(victim)==7,"Multiple colliders on one entity count once");
            ForceScan(victim); g.LawSimulation.Tick(.1f);
            Check(victim.CrowdingNeighborCount==7 && Mathf.Abs(victim.health-9999.7f)<.002f,"Seven neighbors damage the actor at three HP per second");
            Check(fixtures[1].health==10000 && fixtures[2].health==10000 && fixtures[3].health==10000,"Crowding damages actors rather than nearby projectiles, rocks or trees");
            g.LawSimulation.Tick(.1f);
            Check(g.LawSimulation.LastCrowdingQueries==0 && Mathf.Abs(victim.health-9999.4f)<.003f,"Cached count continues damage without another spatial query");
            fixtures[7].gameObject.SetActive(false); Physics2D.SyncTransforms();
            ForceScan(victim); g.LawSimulation.Tick(.1f);
            Check(victim.CrowdingNeighborCount==6 && Mathf.Abs(victim.health-9999.4f)<.003f,"Exactly six neighbors do not cause damage after refreshed count");
            fixtures[7].gameObject.SetActive(true); fixtures[7].IsCharging=true; Physics2D.SyncTransforms();
            Check(g.LawSimulation.CountCrowdingNeighbors(victim)==6,"Unfinished charge previews do not count");
            fixtures[7].IsCharging=false; Place(fixtures[7],new Vector2(-10,-20)); Physics2D.SyncTransforms();
            Check(g.LawSimulation.CountCrowdingNeighbors(victim)==6,"Outside-radius entity does not count");
            Place(fixtures[7],victim.Body.position+Vector2.right*2); Place(g.player,victim.Body.position); victim.gameObject.SetActive(false);
            Physics2D.SyncTransforms(); Vector2 playerCenter=g.player.Collider.bounds.center;
            for(int i=1;i<=7;i++) {float a=(i-1)*Mathf.PI*2/7; Place(fixtures[i],playerCenter+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*2.2f);}
            Physics2D.SyncTransforms(); ForceScan(g.player); g.player.GetComponent<PlayerHitFeedback>().ResetFeedback(); g.LawSimulation.Tick(.1f);
            results.Add("MEASURE playerCrowdCount="+g.player.CrowdingNeighborCount+" hp="+g.player.health+" max="+g.player.maxHealth);
            Check(g.player.CrowdingNeighborCount==7 && Mathf.Abs(g.player.health-(g.player.maxHealth-.3f))<.002f && g.player.LastDamageKind==DamageKind.Crowding,"Player takes the same crowding damage with normal hit feedback");
            g.tuning.crowdingCheckInterval=.3f; ForceScan(g.player); g.player.GetComponent<PlayerHitFeedback>().ResetFeedback(); g.LawSimulation.Tick(.1f);
            Check(Mathf.Abs(g.player.NextCrowdingScanAt-Time.time-.3f)<.001f,"Public 0.3-second interval determines the next per-actor scan");
            g.tuning.crowdingCheckInterval=.5f; ForceScan(g.player); g.LawSimulation.Tick(0);
            Check(Mathf.Abs(g.player.NextCrowdingScanAt-Time.time-.5f)<.001f,"Public 0.5-second interval also changes scheduling without changing damage coefficient");
            g.laws.Clear(); g.LawSimulation.Tick(.1f);
            Check(g.player.CrowdingNeighborCount==0 && g.player.NextCrowdingScanAt<0,"Removing law clears cached hazard and timer");
            Clear(); g.laws.Add(WorldLaw.Crowding); g.tuning.crowdingCheckInterval=1;
            float earliest=float.MaxValue,latest=float.MinValue;
            for(int i=0;i<20;i++) {var b=Fixture(new Vector2(-20+i*.1f,-20),BodyKind.Enemy); b.TickCrowding(0); earliest=Mathf.Min(earliest,b.NextCrowdingScanAt); latest=Mathf.Max(latest,b.NextCrowdingScanAt);}
            Check(latest-earliest>.6f,"Per-actor first scans are spread over the one-second interval");
            for(int i=0;i<250;i++) Fixture(new Vector2(-19+(i%10)*.01f,-20+(i/10)*.01f));
            Physics2D.SyncTransforms(); var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<20;i++) ForceScan(fixtures[i]); g.LawSimulation.Tick(.1f); watch.Stop();
            Check(g.LawSimulation.LastCrowdingQueries==20 && fixtures[0].CrowdingNeighborCount>=260,"Dense 250-projectile query grows reusable buffer without truncating counts");
            results.Add("MEASURE forcedWorstCase20Scans250ProjectilesMs="+watch.Elapsed.TotalMilliseconds.ToString("0.00"));
            watch.Restart(); for(int i=0;i<20;i++) g.LawSimulation.CountCrowdingNeighbors(fixtures[i]); watch.Stop();
            results.Add("MEASURE crowdQueriesOnly20Actors250ProjectilesMs="+watch.Elapsed.TotalMilliseconds.ToString("0.00"));
        }
        static TerrainCell Tile(Vector2 point)
        {
            var o=new GameObject("Verification River"); objects.Add(o); o.transform.position=point;
            var tile=o.AddComponent<TerrainCell>(); tile.phase=FloorPhase.Water; tile.temperature=20;
            var blocker=new GameObject("Water Blocker",typeof(BoxCollider2D)); blocker.layer=12; blocker.transform.SetParent(o.transform,false);
            blocker.GetComponent<BoxCollider2D>().size=Vector2.one; tile.waterBlocker=blocker; return tile;
        }
        static void Simulate(ThermoBody b,int frames)
        {
            for(int i=0;i<frames;i++) {b.SendMessage("FixedUpdate"); Physics2D.Simulate(.02f);}
        }
        static void River()
        {
            var tile=Tile(new Vector2(-20,-20)); var b=Fixture(new Vector2(-20,-21.2f),BodyKind.Enemy);
            Physics2D.SyncTransforms(); b.Body.velocity=Vector2.up*20; Simulate(b,8);
            Check(b.health<10000 && b.LastDamageKind==DamageKind.Impact,"Real fast monster contact with water blocker causes impact damage");
            results.Add("MEASURE riverImpactDamage="+(10000-b.health));
            g.Collisions.Clear(); Place(b,new Vector2(-20,-21.2f)); b.health=10000; b.Body.velocity=Vector2.up*6; Physics2D.SyncTransforms(); Simulate(b,30);
            Check(b.health==10000,"Walking-speed river contact does not deal impact damage");
            g.Collisions.Clear(); g.laws.Add(WorldLaw.ImpactHeat); b.temperature=20; tile.temperature=20;
            Place(b,new Vector2(-20,-21.2f)); b.health=10000; b.Body.velocity=Vector2.up*20; Physics2D.SyncTransforms(); Simulate(b,8);
            Check(b.health<10000 && b.temperature>20 && tile.temperature>20,"River impact shares kinetic heating rule for body and terrain");
            g.laws.Clear(); g.Collisions.Clear(); tile.temperature=-50; tile.Refresh();
            Place(b,new Vector2(-20,-21.2f)); b.health=10000; b.Body.velocity=Vector2.up*20; Physics2D.SyncTransforms(); Simulate(b,10);
            Check(!tile.waterBlocker.activeSelf && b.health==10000 && b.Body.position.y>-20,"Frozen river permits crossing without phantom wall damage");
            tile.temperature=20; tile.Refresh(); var adjacent=Tile(new Vector2(-19,-20));
            g.Collisions.Clear(); Place(b,new Vector2(-19.5f,-21.2f)); b.health=10000; b.Body.velocity=Vector2.up*20; Physics2D.SyncTransforms(); int collisions=g.metrics.collisions; Simulate(b,8);
            Check(g.metrics.collisions-collisions==1 && b.health<10000,"River tile seam registers one impact, not multiplied adjacent hits");
        }
        static void Fragments()
        {
            var guard=Fixture(new Vector2(15,-20),BodyKind.Enemy,g.guardPrefab); guard.isElite=true;
            guard.Die("fragment marker check",DamageKind.Staff);
            Check(g.shards.Count==1 && Vector2.Distance(g.shards[0].transform.position,g.player.Body.position)>20,"Distant guard death creates real clock fragment");
            var map=g.hud.minimap; map.Rebuild();
            Check(map.FragmentMarkerCount==1,"Distant fragment receives independent minimap marker");
            var marker=map.image.transform.Find("Clock Fragment Marker "+g.shards[0].GetInstanceID()) as RectTransform;
            Check(marker!=null && Vector2.Distance(marker.anchoredPosition,map.MapPosition(g.shards[0].transform.position))<.001f && marker.sizeDelta.x==18,"Outlined diamond is positioned at actual dropped fragment");
            Check(marker!=null && !marker.GetComponent<UnityEngine.UI.Image>().raycastTarget,"Fragment marker does not block UI input");
            g.Collect(g.shards[0]); map.SendMessage("Draw");
            Check(map.FragmentMarkerCount==0,"Collecting fragment removes marker without leaving stale icon");
        }
        static void Boss()
        {
            var b=Fixture(new Vector2(-20,-20),BodyKind.Boss,g.bossPrefab); var brain=b.GetComponent<EnemyBrain>(); var combat=b.GetComponent<BossCombat>();
            brain.enabled=true; brain.SetHome(b.Body.position,true); Place(g.player,new Vector2(5,-20)); Physics2D.SyncTransforms();
            typeof(EnemyBrain).GetField("chargeAt",Hidden).SetValue(brain,Time.time+100);
            brain.SendMessage("FixedUpdate"); Physics2D.Simulate(.02f);
            Check(combat!=null && brain.Alert && b.Body.velocity.x>0 && Vector2.Distance(b.Body.position,g.player.Body.position)>brain.AttackReach,"Boss acquires distant player and actively approaches outside charge reach");
            Place(b,new Vector2(-5,-20)); Place(g.player,new Vector2(15,-20)); Physics2D.SyncTransforms(); brain.SendMessage("FixedUpdate"); Physics2D.Simulate(.02f);
            Check(brain.Alert && b.Body.velocity.x>0 && Vector2.Distance(b.Body.position,brain.Home)>8,"Boss keeps pursuing beyond old eight-unit home leash");
            Place(g.player,new Vector2(35,25)); brain.SendMessage("FixedUpdate");
            Check(brain.Alert,"Once locked, boss does not drop distant target");
            Place(g.player,new Vector2(15,-20)); Place(b,new Vector2(-5,-20)); Physics2D.SyncTransforms();
            int shots=g.metrics.shots; typeof(BossCombat).GetField("volleyAt",Hidden).SetValue(combat,Time.time-.01f);
            typeof(EnemyBrain).GetField("recoveryUntil",Hidden).SetValue(brain,Time.time+10);
            combat.SendMessage("FixedUpdate");
            var targeted=g.bodies.Find(x=>x!=null && x.kind==BodyKind.Projectile && x.owner==b);
            Check(combat.Volleys==1 && g.metrics.shots-shots==9,"Boss fires aimed projectile plus eight radial shots even during melee recovery");
            Check(targeted!=null && Mathf.Abs(targeted.Mass*targeted.Body.velocity.magnitude-g.tuning.bossProjectileImpulse)<.001f && targeted.Body.velocity.x>0,"Aimed boss projectile receives configured 48 impulse toward player");
            float nextVolley=(float)typeof(BossCombat).GetField("volleyAt",Hidden).GetValue(combat);
            Check(Mathf.Abs(nextVolley-Time.time-g.tuning.EnemyAttackTime(g.tuning.bossVolleyInterval)/brain.TemperatureAttackMultiplier)<.001f,"Volley interval uses configurable base interval and attack speed");
            foreach(var body in g.bodies.ToArray()) if(body!=null && body.owner==b) body.gameObject.SetActive(false);
            Physics2D.SyncTransforms();
            int summons=combat.Summons; typeof(BossCombat).GetField("summonAt",Hidden).SetValue(combat,Time.time+.01f); combat.SendMessage("FixedUpdate");
            Check(combat.Summons==summons,"Guard is not summoned before its deadline");
            typeof(BossCombat).GetField("summonAt",Hidden).SetValue(combat,Time.time-.01f); combat.SendMessage("FixedUpdate");
            var guard=g.bodies.Find(x=>x!=null && x!=b && x.isElite && !x.Dead);
            Check(combat.Summons==summons+1 && guard!=null && guard.GetComponent<EnemyBrain>().archetype==EnemyArchetype.Guard,"Summon deadline creates exactly one real guard");
            Check(Mathf.Abs(combat.NextSummonAt-Time.time-20)<.001f,"Successful summon schedules next guard in twenty game seconds");
            if(guard!=null) {fixtures.Add(guard); Check(guard.health==g.tuning.EnemyHealthAt(g.tuning.guardHealth,g.World) && !guard.GetComponent<EnemyBrain>().guarding,"Summoned guard uses world health growth and can roam to attack");}
            int after=combat.Summons; combat.SendMessage("FixedUpdate"); Check(combat.Summons==after,"Multiple updates at one deadline do not duplicate summons");
            g.TogglePause(); typeof(BossCombat).GetField("summonAt",Hidden).SetValue(combat,Time.time-.01f); combat.SendMessage("FixedUpdate");
            Check(combat.Summons==after,"Paused battle suppresses summoning"); g.TogglePause(); Time.timeScale=0;
            brain.enabled=false; combat.SendMessage("FixedUpdate"); Check(combat.Summons==after,"Disabled enemy AI suppresses boss combat actions");
            float mass=g.tuning.bossBaseMass,speed=g.tuning.bossMoveSpeed,force=g.tuning.bossDriveForce;
            float windup=g.tuning.bossWindup,cooldown=g.tuning.bossAttackCooldown,recovery=g.tuning.bossRecovery;
            int radial=g.tuning.bossRadialProjectiles;
            try
            {
                g.tuning.bossBaseMass=12; g.tuning.bossMoveSpeed=4; g.tuning.bossDriveForce=80;
                g.tuning.bossWindup=2; g.tuning.bossAttackCooldown=6; g.tuning.bossRecovery=4; g.tuning.bossRadialProjectiles=4;
                brain.enabled=true; brain.SendMessage("FixedUpdate");
                Check(b.Mass==12 && b.topSpeed==4 && b.driveForce==80,"Changing WorldTuning boss mass, movement speed and force applies to live boss");
                typeof(EnemyBrain).GetMethod("BeginAttack",Hidden).Invoke(brain,new object[]{Vector2.right,g});
                float until=(float)typeof(EnemyBrain).GetField("chargeUntil",Hidden).GetValue(brain);
                float attackAt=(float)typeof(EnemyBrain).GetField("chargeAt",Hidden).GetValue(brain);
                Check(Mathf.Abs(until-Time.time-g.tuning.EnemyAttackTime(2))<.001f && Mathf.Abs(attackAt-until-g.tuning.EnemyAttackTime(6))<.001f,"Independent boss windup and cooldown drive real attack timers");
                b.Body.velocity=Vector2.zero; typeof(EnemyBrain).GetField("chargeUntil",Hidden).SetValue(brain,Time.time-.01f);
                typeof(EnemyBrain).GetField("recoveryUntil",Hidden).SetValue(brain,Time.time-.01f); brain.SendMessage("FixedUpdate");
                float recoverAt=(float)typeof(EnemyBrain).GetField("recoveryUntil",Hidden).GetValue(brain);
                Check(Mathf.Abs(recoverAt-Time.time-g.tuning.EnemyAttackTime(4))<.001f,"Independent boss recovery drives post-lunge timer");
                typeof(BossCombat).GetField("summonAt",Hidden).SetValue(combat,Time.time+100);
                typeof(BossCombat).GetField("volleyAt",Hidden).SetValue(combat,Time.time-.01f);
                shots=g.metrics.shots; combat.SendMessage("FixedUpdate");
                Check(g.metrics.shots-shots==5,"Public radial count four produces four ring shots plus one aimed shot");
            }
            finally
            {
                g.tuning.bossBaseMass=mass; g.tuning.bossMoveSpeed=speed; g.tuning.bossDriveForce=force;
                g.tuning.bossWindup=windup; g.tuning.bossAttackCooldown=cooldown; g.tuning.bossRecovery=recovery;
                g.tuning.bossRadialProjectiles=radial;
            }
        }
    }
}
#endif
