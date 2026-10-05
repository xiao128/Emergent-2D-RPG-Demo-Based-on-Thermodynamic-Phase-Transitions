#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PhaseArena
{
    public static class ArenaExpandedPhysicsVerification
    {
        static ArenaDirector g;
        static readonly List<string> results=new List<string>();
        static readonly List<ThermoBody> fixtures=new List<ThermoBody>();
        static int failures;
        static void Check(bool ok,string name) {results.Add((ok?"PASS ":"FAIL ")+name); if(!ok) failures++;}
        static bool Near(float a,float b) => Mathf.Abs(a-b)<.001f;
        static ThermoBody Body(Vector2 pos,float mass=1)
        {
            var b=g.Spawn(g.projectilePrefab,pos); b.baseMass=mass; b.shotHeat=0; b.health=b.maxHealth=100000; b.ThermalStep(0); fixtures.Add(b); return b;
        }
        static void Clear()
        {
            foreach(var b in fixtures) if(b!=null) {b.gameObject.SetActive(false); UnityEngine.Object.Destroy(b.gameObject);} fixtures.Clear();
            g.laws.Clear(); g.ContactLaws.Clear(); g.Collisions.Clear(); g.player.ResetAt(new Vector2(-32,-24)); Physics2D.SyncTransforms();
        }
        public static string Run()
        {
            results.Clear(); failures=0; g=ArenaDirector.Instance; g.Restart();
            foreach(var b in g.bodies.ToArray()) if(b!=g.player && b!=null) b.gameObject.SetActive(false);
            g.player.GetComponent<PlayerMage>().enabled=false;
            try
            {
                Check(!WorldLawCatalog.IsAvailable(WorldLaw.Leidenfrost),"Temperature-gap isolation law removed from random pool");
                int pool=0; foreach(WorldLaw l in Enum.GetValues(typeof(WorldLaw))) if(WorldLawCatalog.IsAvailable(l)) pool++;
                Check(pool==18,"Random pool has 18 available laws after replacement and additions");
                foreach(var l in new[]{WorldLaw.NuclearFusion,WorldLaw.VaporGlide,WorldLaw.RecoilPropellant,WorldLaw.ViscousWelding,WorldLaw.BernoulliWake}) Check(WorldLawCatalog.IsAvailable(l),l+" available");
                HeatAndMass(); Clear(); Fusion(); Clear(); Fields(); Clear(); Recoil(); Clear(); Welding(); Clear(); Glide(); Clear();
            }
            catch(Exception ex) {Check(false,ex.ToString());}
            finally {Clear(); g.Restart();}
            string report="failures="+failures+"\n"+string.Join("\n",results); File.WriteAllText("Verification/expanded-physics-regression.txt",report); Debug.Log(report); return report;
        }
        static void HeatAndMass()
        {
            var small=Body(new Vector2(-20,-20),1); var large=Body(new Vector2(-15,-20),5);
            small.AddHeat(100); large.AddHeat(100);
            Check(Near((small.temperature-20)/(large.temperature-20),5),"Equal energy heats a mass-1 object five times as much as mass-5");
            small.temperature=large.temperature=20; small.AddHeat(-100); large.AddHeat(-100);
            Check(Near((20-small.temperature)/(20-large.temperature),5),"Cooling also accounts for mass");
            small.AddHeat(100000); large.AddHeat(-100000);
            Check(small.temperature==1000 && large.temperature==-1000,"Hard temperature limits are -1000 and +1000");
            g.laws.Add(WorldLaw.Abrasion); small.ResetAt(new Vector2(-20,-20)); large.ResetAt(new Vector2(-15,-20));
            small.temperature=large.temperature=60; small.Body.velocity=large.Body.velocity=Vector2.right*10;
            small.ApplyGroundFrictionLaws(1,false,.1f); large.ApplyGroundFrictionLaws(1,false,.1f);
            Check(Near(small.Mass,.8f)&&Near(large.Mass,4.8f),"Every meter removes the same 0.2 absolute mass from light and heavy bodies");
            Check(Near(large.ShapeScale,Mathf.Sqrt(4.8f/5)),"Heavy bodies lose proportionately less size");
            g.laws.Clear(); small.temperature=100; large.temperature=20; large.Body.position=small.Body.position+Vector2.right; Physics2D.SyncTransforms();
            float total=small.temperature*small.HeatCapacity+large.temperature*large.HeatCapacity;
            float ambient=g.tuning.ambientRecovery,wind=g.tuning.windRecovery; g.tuning.ambientRecovery=g.tuning.windRecovery=0;
            try {g.LawSimulation.Tick(.1f);} finally {g.tuning.ambientRecovery=ambient; g.tuning.windRecovery=wind;}
            Check(Near(total,small.temperature*small.HeatCapacity+large.temperature*large.HeatCapacity),"Body heat exchange conserves heat energy");
        }
        static void Fusion()
        {
            var a=Body(new Vector2(-20,-20)); var b=Body(new Vector2(-19,-20)); g.laws.Add(WorldLaw.NuclearFusion);
            Action<Vector2,Vector2,bool,string> test=(va,vb,expected,name)=>{
                a.Body.velocity=va; b.Body.velocity=vb; a.RecordIncomingMotion(); b.RecordIncomingMotion();
                Check(g.ContactLaws.TryFusion(a,b,new Vector2(-19.5f,-20))==expected,name);
            };
            test(Vector2.right*20,Vector2.zero,false,"Fast versus stationary does not fuse");
            test(Vector2.right*20,Vector2.right*15,false,"Same-direction chasing does not fuse");
            test(Vector2.left*20,Vector2.right*20,false,"Opposed but separating objects do not fuse");
            test(Vector2.right*12,Vector2.left*20,false,"Both speeds must strictly exceed 12");
            int before=g.metrics.fusions;
            a.Body.velocity=Vector2.right*20; b.Body.velocity=Vector2.left*20; a.RecordIncomingMotion(); b.RecordIncomingMotion();
            // Solver may stop both bodies. Incoming directions must survive it.
            a.Body.velocity=b.Body.velocity=Vector2.zero;
            g.Collisions.ResolveCollision(a,b,40,new Vector2(-19.5f,-20)); g.Collisions.ResolveCollision(b,a,40,new Vector2(-19.5f,-20));
            Check(g.metrics.fusions==before+1,"Two opposite callback directions resolve exactly one blast using pre-solver velocities");
            Check(a.health<100000 && b.health<100000,"Fusion blast damages both collision participants");
            g.Collisions.Clear();
            a.Body.position=new Vector2(-22,-20); b.Body.position=new Vector2(-18,-20);
            a.Body.velocity=Vector2.right*20; b.Body.velocity=Vector2.left*20; a.Body.drag=b.Body.drag=0;
            Physics2D.SyncTransforms(); before=g.metrics.fusions; var mode=Physics2D.simulationMode;
            try {Physics2D.simulationMode=SimulationMode2D.Script; for(int i=0;i<8;i++) {a.RecordIncomingMotion(); b.RecordIncomingMotion(); Physics2D.Simulate(.02f);}}
            finally {Physics2D.simulationMode=mode;}
            Check(g.metrics.fusions==before+1,"Actual Physics2D head-on collision triggers one fusion through native callbacks");
        }
        static void Fields()
        {
            var source=Body(new Vector2(-20,-20)); var light=Body(new Vector2(-18,-20),1); var heavy=Body(new Vector2(-22,-20),4);
            g.laws.Add(WorldLaw.Gravity); source.temperature=120; Physics2D.SyncTransforms(); g.LawSimulation.Tick(0); g.FlowFields.TickSource(source,.1f);
            Check(light.Body.velocity.x>0 && heavy.Body.velocity.x<0,"Hot source pushes radially outward");
            Check(Near(light.Body.velocity.magnitude/heavy.Body.velocity.magnitude,4),"Equal field force produces inverse-mass acceleration");
            source.temperature=-80; light.Body.velocity=heavy.Body.velocity=Vector2.zero; g.LawSimulation.Tick(0); g.FlowFields.TickSource(source,.1f);
            Check(light.Body.velocity.x<0 && heavy.Body.velocity.x>0,"Cold source pulls radially inward without a mass gate");
            source.temperature=20; light.Body.velocity=heavy.Body.velocity=Vector2.zero; g.FlowFields.TickSource(source,.1f);
            Check(light.Body.velocity==Vector2.zero&&heavy.Body.velocity==Vector2.zero,"Ambient source has no convection field");
            g.laws.Clear(); g.laws.Add(WorldLaw.Leidenfrost); source.temperature=500; g.LawSimulation.Tick(.1f);
            Check(light.Body.velocity==Vector2.zero&&heavy.Body.velocity==Vector2.zero,"Even manually adding retired law no longer applies isolation force");
            g.laws.Clear(); g.laws.Add(WorldLaw.BernoulliWake);
            source.Body.velocity=Vector2.right*30; light.Body.position=new Vector2(-19.4f,-20); heavy.Body.position=new Vector2(-20.6f,-20);
            var side=Body(new Vector2(-20,-19.4f)); Physics2D.SyncTransforms(); g.LawSimulation.Tick(0); g.FlowFields.TickSource(source,.1f);
            Check(light.Body.velocity==Vector2.zero && heavy.Body.velocity.x>0 && side.Body.velocity.y<0,"Wake pulls rear and flanks while excluding the front");
            float diameter=Mathf.Max(source.Collider.bounds.size.x,source.Collider.bounds.size.y);
            source.Body.velocity=Vector2.right*diameter*g.tuning.wakeSpeedPerDiameter; heavy.Body.velocity=side.Body.velocity=Vector2.zero; g.FlowFields.TickSource(source,.1f);
            Check(heavy.Body.velocity==Vector2.zero && side.Body.velocity==Vector2.zero,"Wake uses strict speed-to-diameter threshold");
        }
        static void Recoil()
        {
            var b=Body(new Vector2(-20,-20),2); g.laws.Add(WorldLaw.RecoilPropellant);
            b.temperature=100; b.AddHeat(20,Vector2.right); Check(Near(b.Mass,2)&&b.Body.velocity==Vector2.zero,"Below extreme heat no propellant consumed");
            b.temperature=300; float mass=b.Mass; b.AddHeat(100,Vector2.right);
            Check(Near(mass-b.Mass,.1f),"50 percent of 100 heat consumes 0.1 mass at cost 500");
            Check(b.Body.velocity.x>0 && Near(b.ShapeScale,Mathf.Sqrt(b.Mass/2)),"Heat from left produces rightward recoil and matching shrink");
            Check(b.GetComponent<RecoilGasParticles>()!=null && g.Registry.ProjectileCount==1,"Gas is visible-only and does not consume projectile slots");
            mass=b.Mass; b.AddHeat(-100,Vector2.right); Check(Near(b.Mass,mass),"Cooling does not consume propellant");
            mass=b.Mass; var velocity=b.Body.velocity; b.AddHeat(100);
            Check(b.Mass<mass && b.Body.velocity==velocity,"Uniform heating emits propellant without inventing a directional kick");
            b.kind=BodyKind.Tree; b.Body.bodyType=RigidbodyType2D.Static; mass=b.Mass; b.AddHeat(100,Vector2.right);
            Check(b.Mass<mass && b.ShapeScale<1 && b.Body.bodyType==RigidbodyType2D.Static,"Destructible fixed trees can lose propellant and shrink while remaining fixed");
        }
        static void Welding()
        {
            var a=Body(new Vector2(-20,-20)); var b=Body(new Vector2(-19.5f,-20)); g.laws.Add(WorldLaw.ViscousWelding);
            a.temperature=300; b.temperature=200; g.ContactLaws.Touch(a,b,a.Body.position);
            Check(g.ContactLaws.ActiveWelds==0,"Both temperatures must strictly exceed 200");
            b.temperature=300; g.ContactLaws.Touch(a,b,a.Body.position); b.Body.velocity=Vector2.up*10;
            g.ContactLaws.Tick(.02f); Check(g.ContactLaws.ActiveWelds==1 && b.Body.velocity.y<10 && a.Body.velocity.y>0,"High heat contact damps tangential relative motion");
            a.Body.velocity=b.Body.velocity=Vector2.zero; b.Body.position+=Vector2.right*.1f; Physics2D.SyncTransforms(); g.ContactLaws.Tick(.02f);
            Check(a.Body.velocity.x>0&&b.Body.velocity.x<0,"Normal spring pulls separating welded bodies together");
            b.temperature=200; g.ContactLaws.Tick(.02f); Check(g.ContactLaws.ActiveWelds==0,"Cooling releases the temporary weld");
            b.temperature=300; g.ContactLaws.Touch(a,b,a.Body.position); g.laws.Clear(); g.ContactLaws.Tick(.02f); Check(g.ContactLaws.ActiveWelds==0,"Removing welding law clears all constraints");
        }
        static void Glide()
        {
            var b=Body(new Vector2(-20,-20)); var tile=new GameObject("Glide test water").AddComponent<TerrainCell>(); tile.temperature=20; tile.phase=FloorPhase.Water;
            var state=new VaporGlideStatus(); g.laws.Add(WorldLaw.VaporGlide);
            try {
                b.temperature=60; state.Touch(b,g,tile,Vector2.left); Check(!state.Active,"Exactly 60 degrees does not trigger glide");
                b.temperature=100; state.Touch(b,g,tile,Vector2.left); Check(state.Active,"Hot body contacting ambient water triggers five-second glide");
                // The production status is triggered by actual wet overlap/collision.
                Vector2 point; var river=g.worldGenerator.cells.Values; TerrainCell ice=null;
                foreach(var cell in river) if(!cell.rough) {ice=cell; break;}
                ice.temperature=-20; ice.Refresh(); b.Body.position=ice.transform.position; b.transform.position=ice.transform.position; b.SendMessage("FixedUpdate");
                Check(b.VaporGliding && b.Body.drag==0 && b.GroundDrag==0,"Ice contact activates production state and locks ground drag to zero");
                Check(b.Body.velocity.sqrMagnitude>0,"Glide provides persistent normal repulsion");
                g.laws.Clear(); b.SendMessage("FixedUpdate"); Check(!b.VaporGliding && b.Body.drag>0,"Removing glide restores normal ground friction");
            } finally {UnityEngine.Object.Destroy(tile.gameObject);}
        }
    }
}
#endif
