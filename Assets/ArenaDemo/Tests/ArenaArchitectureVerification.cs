#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    // Controlled live-Editor integration checks. No saved scene or player asset is modified.
    public static class ArenaArchitectureVerification
    {
        static ArenaDirector world;
        static PlayerMage mage;
        static readonly List<ThermoBody> fixtures=new List<ThermoBody>();
        static readonly List<string> results=new List<string>();
        static int failures;
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool ok,string name) { results.Add((ok ? "PASS " : "FAIL ")+name); if(!ok) failures++; }
        static ThermoBody Fixture(Vector2 point,BodyKind kind=BodyKind.Projectile)
        {
            var body=world.Spawn(world.projectilePrefab,point);
            body.kind=kind; body.shotHeat=0; body.health=body.maxHealth=10000;
            fixtures.Add(body); return body;
        }
        static void Clear()
        {
            foreach(var body in fixtures) if(body!=null) { body.gameObject.SetActive(false); UnityEngine.Object.Destroy(body.gameObject); }
            fixtures.Clear(); world.laws.Clear(); world.Collisions.Clear();
            world.player.ResetAt(new Vector2(-25,-20)); mage.ResetTools();
            Physics2D.SyncTransforms();
        }
        static void Step(int frames)
        {
            var mode=Physics2D.simulationMode;
            try { Physics2D.simulationMode=SimulationMode2D.Script; for(int i=0;i<frames;i++) Physics2D.Simulate(.02f); }
            finally { Physics2D.simulationMode=mode; }
        }
        public static string Run()
        {
            if(!Application.isPlaying) throw new InvalidOperationException("Run these checks in Play mode.");
            world=ArenaDirector.Instance; if(world==null) throw new InvalidOperationException("Director not ready.");
            results.Clear(); fixtures.Clear(); failures=0;
            if(world.State==RunState.Title) world.StartRun(); else world.Restart();
            mage=world.player.GetComponent<PlayerMage>(); mage.enabled=false;
            float authoredMana=mage.maxMana;
            // This fixture exercises many spells independently of authored balance.
            mage.maxMana=Mathf.Max(authoredMana,mage.spellManaCost*20); mage.ResetTools();
            foreach(var ai in UnityEngine.Object.FindObjectsOfType<EnemyBrain>()) ai.gameObject.SetActive(false);
            foreach(var prop in world.worldGenerator.props) prop.gameObject.SetActive(false);
            Clear();
            try
            {
                Check(!WorldLawCatalog.IsAvailable(WorldLaw.ThermalMass) && !WorldLawCatalog.IsAvailable(WorldLaw.ThermalArc) && !WorldLawCatalog.IsAvailable(WorldLaw.ThermalShock),"Retired laws excluded from random catalog");
                Check(WorldLawCatalog.IsAvailable(WorldLaw.Crowding) && WorldLawCatalog.IsAvailable(WorldLaw.VaporRecoil),"Updated law catalog includes crowding and steam waves");
                Check(world.worldGenerator.Layout.Validate(),"World layout remains reachable");
                ChargeAndSpawn(); Clear();
                CollisionAndEnergy(); Clear();
                UniversalLaws(); Clear();
                SteamDensityAndCrowding(); Clear();
                ProjectileQueue(); Clear();
                StatusBars(); Clear();
            }
            catch(Exception ex) { Check(false,"Exception: "+ex); }
            finally { Clear(); mage.maxMana=authoredMana; mage.ResetTools(); }
            string report="failures="+failures+"\n"+string.Join("\n",results);
            Directory.CreateDirectory("Verification"); File.WriteAllText("Verification/architecture-regression.txt",report);
            Debug.Log("[ArchitectureVerification] "+report);
            return report;
        }
        static void ChargeAndSpawn()
        {
            int before=world.metrics.shots;
            float mana=mage.Mana;
            mage.Cast(true,Vector2.right);
            typeof(PlayerMage).GetMethod("AdvanceCharge",Hidden).Invoke(mage,new object[]{.3f});
            Check(mage.IsCharging && world.metrics.shots==before && mage.Mana==mana,"Charge is a visual preview without physical projectile or mana cost");
            typeof(PlayerMage).GetField("move",Hidden).SetValue(mage,Vector2.right);
            mage.SendMessage("FixedUpdate"); Step(1);
            Check(world.player.Body.velocity.x>0,"Charging permits movement through the physical controller");
            Check(mage.ReleaseCharge(Vector2.right),"Mouse release creates charged ball");
            var shot=world.bodies.FindLast(b=>b!=null && b.kind==BodyKind.Projectile);
            fixtures.Add(shot);
            Check(shot.Body.velocity.sqrMagnitude<.0001f && Mathf.Approximately(mage.Mana,mana-mage.spellManaCost),"Released ball is stationary and charges mana once");
            Clear();
            int safe=0,kicks=0;
            for(int i=0;i<8;i++)
            {
                Vector2 direction=new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4));
                shot=world.Shoot(world.player,direction,0); fixtures.Add(shot); Physics2D.SyncTransforms();
                if(!shot.Collider.Distance(world.player.Collider).isOverlapped) safe++;
                if(world.Melee(world.player,direction)==shot) kicks++;
                shot.gameObject.SetActive(false);
            }
            Check(safe==8 && kicks==8,"Eight release directions have clearance and can be kicked");
            var wall=new GameObject("Verification wall"); wall.layer=10;
            var box=wall.AddComponent<BoxCollider2D>(); box.size=new Vector2(.2f,3);
            wall.transform.position=(Vector2)world.player.Collider.bounds.center+Vector2.right*.7f;
            Physics2D.SyncTransforms();
            Check(world.Shoot(world.player,Vector2.right,0)==null,"No ball created inside a tight wall/player gap");
            wall.SetActive(false); UnityEngine.Object.Destroy(wall);
        }
        static void CollisionAndEnergy()
        {
            var p=world.player;
            var e=world.Spawn(world.mediumEnemyPrefab,(Vector2)p.Collider.bounds.center+Vector2.right*2);
            fixtures.Add(e); e.GetComponent<EnemyBrain>().enabled=false; e.health=e.maxHealth=1000;
            e.Body.velocity=Vector2.left*8; world.laws.Add(WorldLaw.ImpactHeat);
            Physics2D.SyncTransforms(); float hp=p.health; Step(20);
            Check(p.health<hp && world.metrics.collisions>0,"Actual enemy-player contact damages the player");
            Check(p.temperature>60 && e.temperature>60,"Actual collision causes visible kinetic-energy heating");
            results.Add("MEASURE contactDamage="+(hp-p.health)+" temperatures="+p.temperature+","+e.temperature);
            Clear();
            var a=Fixture(new Vector2(-20,-20)); var b=Fixture(new Vector2(-18,-20));
            world.laws.Add(WorldLaw.ImpactHeat);
            a.temperature=b.temperature=20; world.ResolveCollision(a,b,5,a.Body.position,0);
            float low=a.temperature-20;
            world.Collisions.Clear(); a.temperature=b.temperature=20;
            world.ResolveCollision(a,b,10,a.Body.position,0); float high=a.temperature-20;
            world.Collisions.Clear(); a.temperature=b.temperature=20;
            world.ResolveCollision(a,b,10,a.Body.position,100000);
            Check(Mathf.Abs(high/low-4)<.001f && Mathf.Abs(a.temperature-20-high)<.001f,"Heating follows speed squared and ignores contact-impulse magnitude");
            Clear();
            var own=world.Shoot(p,Vector2.right,0); fixtures.Add(own); own.temperature=20;
            own.Body.velocity=Vector2.left*20; hp=p.health; Physics2D.SyncTransforms(); Step(10);
            Check(p.health<hp,"Returning own projectile still causes collision damage");
        }
        static void UniversalLaws()
        {
            foreach(var kind in new[]{BodyKind.Rock,BodyKind.Projectile,BodyKind.Enemy,BodyKind.Boss,BodyKind.Player})
            {
                var body=Fixture(new Vector2(-20,-20),kind);
                world.laws.Clear(); world.laws.Add(WorldLaw.Abrasion); world.laws.Add(WorldLaw.FrictionHeat);
                body.temperature=60; body.Body.velocity=Vector2.right*10;
                body.ApplyGroundFrictionLaws(.6f,false,.1f);
                Check(body.temperature>60 && body.Mass<1,kind+" friction heating and mass erosion");
                body.massRemaining=1; body.temperature=-100;
                body.ApplyGroundFrictionLaws(.6f,false,.1f);
                Check(body.temperature>-100 && body.massRemaining==1,kind+" cold body heats before erosion");
                world.laws.Clear(); world.laws.Add(WorldLaw.ColdBounce); body.ThermalStep(0);
                bool bounce=body.Collider.sharedMaterial==world.bouncyMaterial;
                world.laws.Clear(); body.SendMessage("FixedUpdate"); float drag=body.Body.drag;
                world.laws.Add(WorldLaw.SuperSlide); body.SendMessage("FixedUpdate");
                Check(bounce && Mathf.Abs(body.Body.drag-drag*.05f)<.0001f,kind+" cold bounce and superconductive friction");
                world.laws.Clear(); world.laws.Add(WorldLaw.ThermalExpansion); body.temperature=200; body.ThermalStep(.3f);
                Check(body.ShapeScale>1,kind+" thermal expansion");
                body.gameObject.SetActive(false);
            }
        }
        static void SteamDensityAndCrowding()
        {
            var a=Fixture(new Vector2(-20,-20)); var b=Fixture(new Vector2(-18,-20));
            a.temperature=160; b.temperature=20; world.laws.Add(WorldLaw.VaporRecoil);
            Physics2D.SyncTransforms(); int bursts=world.metrics.vaporBursts;
            a.AddHeat(-80);
            Check(world.metrics.vaporBursts==bursts+1 && b.Body.velocity.x>0,"Rapid cooling emits steam wave without water contact");
            a.temperature=160; a.AddHeat(-80);
            Check(world.metrics.vaporBursts==bursts+1,"Steam wave cooldown prevents recursive wave spam");
            world.laws.Clear(); world.laws.Add(WorldLaw.Gravity);
            a.temperature=-120; b.temperature=20; a.baseMass=world.tuning.gravityMassThreshold+.1f; a.ThermalStep(0);
            b.baseMass=5; b.ThermalStep(0); b.Body.velocity=Vector2.zero;
            world.LawSimulation.Tick(.1f);
            Check(world.LawSimulation.IsGravitySource(a) && !world.LawSimulation.IsGravitySource(b) && b.Body.velocity.x<0,"Cold heavy source attracts warm heavy target");
            Clear(); world.laws.Add(WorldLaw.Crowding);
            for(int i=0;i<8;i++) Fixture(new Vector2(-21+i*.03f,-21));
            world.LawSimulation.Tick(.1f);
            Check(world.LawSimulation.CountCrowdingNeighbors(fixtures[0])==7 && fixtures.TrueForAll(body=>body.health==10000),"Nearby items count across actor-radius queries and are not themselves crowding damage targets");
        }
        static void ProjectileQueue()
        {
            for(int i=0;i<250;i++) Fixture(new Vector2(100+i,100));
            var first=fixtures[0]; var second=fixtures[1];
            var extra=world.Shoot(world.player,Vector2.right,0); fixtures.Add(extra);
            int active=0; foreach(var body in world.bodies) if(body!=null && !body.Dead && body.kind==BodyKind.Projectile && body.gameObject.activeInHierarchy) active++;
            Check(!first.gameObject.activeInHierarchy && second.gameObject.activeInHierarchy && active==250,"Shared projectile FIFO removes oldest at 250, preserving second");
        }
        static void StatusBars()
        {
            var p=world.player; var bars=p.GetComponent<BodyStatusBars>();
            p.transform.rotation=Quaternion.Euler(0,0,37); bars.Refresh();
            Check(Quaternion.Angle(p.healthFill.transform.parent.rotation,Quaternion.identity)<.001f && Quaternion.Angle(bars.manaFill.transform.parent.rotation,Quaternion.identity)<.001f,"Health and mana bars remain horizontal under root rotation");
            p.transform.rotation=Quaternion.identity; bars.Refresh();
            Check(bars.manaBack.transform.position.y<p.healthFill.transform.parent.position.y,"Mini mana sits beneath mini health");
            float ratio=bars.manaFill.transform.localScale.x/p.healthBarSize.x;
            Check(Mathf.Abs(ratio-mage.Mana/mage.maxMana)<.001f,"Mini mana uses real controller mana ratio");
        }
        public static string Benchmark(bool dense=false)
        {
            var g=ArenaDirector.Instance;
            if(g==null || !Application.isPlaying) throw new InvalidOperationException("Play-mode director required.");
            foreach(var body in g.bodies.ToArray()) if(body!=null) body.gameObject.SetActive(false);
            var samples=new List<ThermoBody>();
            var font=g.feedbackFont; g.feedbackFont=null;
            try
            {
                for(int i=0;i<250;i++)
                {
                    Vector2 position=dense ? new Vector2(-20+(i%16)*.015f,-20+(i/16)*.015f) : new Vector2(-24+(i%16)*3,-24+(i/16)*3);
                    var body=g.Spawn(g.projectilePrefab,position);
                    body.shotHeat=0; body.temperature=i%2==0 ? 150 : -120; body.health=body.maxHealth=10000;
                    samples.Add(body);
                }
                g.laws.Clear(); g.laws.Add(WorldLaw.Gravity); g.laws.Add(WorldLaw.Leidenfrost);
                Physics2D.SyncTransforms();
                var legacy=new LegacyLawTickBenchmark(g);
                for(int i=0;i<3;i++) { legacy.Tick(0); g.LawSimulation.Tick(0); }
                int ticks=dense ? 10 : 30;
                var watch=System.Diagnostics.Stopwatch.StartNew();
                for(int i=0;i<ticks;i++) legacy.Tick(0);
                watch.Stop(); double oldMs=watch.Elapsed.TotalMilliseconds/ticks;
                watch.Restart(); for(int i=0;i<ticks;i++) g.LawSimulation.Tick(0);
                watch.Stop(); double newMs=watch.Elapsed.TotalMilliseconds/ticks;
                string report="250 "+(dense ? "dense" : "dispersed")+" projectiles; gravity+repulsion; dt=0; "+ticks+" ticks. LegacyMeanMs="+oldMs.ToString("F3")+" NewMeanMs="+newMs.ToString("F3")+" NeighborCandidates="+g.LawSimulation.LastNeighborCandidates+" speedup="+(oldMs/newMs).ToString("F2");
                File.WriteAllText(dense ? "Verification/law-performance-dense.txt" : "Verification/law-performance.txt",report);
                return report;
            }
            finally
            {
                foreach(var body in samples) { body.gameObject.SetActive(false); UnityEngine.Object.Destroy(body.gameObject); }
                g.player.gameObject.SetActive(true); g.player.ResetAt(new Vector2(-25,-20));
                g.feedbackFont=font; g.laws.Clear();
            }
        }
    }
}
#endif
