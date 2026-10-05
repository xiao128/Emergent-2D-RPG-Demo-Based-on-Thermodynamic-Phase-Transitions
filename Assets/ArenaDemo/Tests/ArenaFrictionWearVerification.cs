#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PhaseArena
{
    public static class ArenaFrictionWearVerification
    {
        static readonly List<string> results=new List<string>();
        static ArenaDirector g;
        static int failures;
        static void Check(bool ok,string label) {results.Add((ok?"PASS ":"FAIL ")+label); if(!ok) failures++;}
        static bool Near(float a,float b) => Mathf.Abs(a-b)<.0001f;
        static void Prepare(ThermoBody b,float heat=60,float speed=10)
        {
            b.ResetAt(new Vector2(-25,-20)); b.temperature=heat; b.Body.velocity=Vector2.right*speed;
        }
        public static string Run()
        {
            if(!Application.isPlaying) throw new InvalidOperationException("Play mode required");
            results.Clear(); failures=0; g=ArenaDirector.Instance; g.Restart();
            var p=g.player; p.GetComponent<PlayerMage>().enabled=false;
            foreach(var body in g.bodies.ToArray()) if(body!=null && body!=p) body.gameObject.SetActive(false);
            var b=g.Spawn(g.projectilePrefab,new Vector2(-25,-20)); b.enabled=false;
            var t=g.tuning; float threshold=t.abrasionSpeedThreshold,hot=t.abrasionHotTemperature,rate=t.abrasionPerMeter;
            try
            {
                Check(WorldLawCatalog.IsAvailable(WorldLaw.FrictionHeat)&&WorldLawCatalog.IsAvailable(WorldLaw.Abrasion),"Both laws independently available");
                Check((int)WorldLaw.Abrasion==6 && (int)WorldLaw.FrictionHeat==17,"Existing serialized law IDs preserved");
                g.laws.Clear(); g.laws.Add(WorldLaw.FrictionHeat); Prepare(b); b.ApplyGroundFrictionLaws(1,false,.1f);
                Check(b.temperature>60 && Near(b.massRemaining,1),"Heating alone never erodes mass");
                float heat=b.temperature-60; Prepare(b); b.ApplyGroundFrictionLaws(2,false,.1f);
                Check(Near(b.temperature-60,heat*2),"Heating scales with friction coefficient");
                Prepare(b); b.ApplyGroundFrictionLaws(1,true,.1f); Check(Near(b.temperature,60),"Ice prevents friction heating");
                g.laws.Clear(); g.laws.Add(WorldLaw.Abrasion); Prepare(b);
                float radius=b.radius; Vector3 scale=b.visual.transform.localScale; float area=b.Area;
                b.ApplyGroundFrictionLaws(1,false,.1f);
                Check(Near(b.massRemaining,.85f)&&Near(b.Mass,b.baseMass*.85f),"One meter removes 15 percent remaining mass");
                Check(Near(b.temperature,60),"Wear alone does not heat");
                Check(Near(b.radius/radius,Mathf.Sqrt(.85f)) && Near(b.visual.transform.localScale.x/scale.x,Mathf.Sqrt(.85f)),"Visual and collider shrink with square root mass");
                Physics2D.SyncTransforms(); Check(Near(b.Area/area,.85f),"2D occupied area follows remaining mass");
                Check(Near(b.Body.velocity.magnitude,10),"Mass change preserves velocity");
                for(int i=0;i<10;i++) b.ThermalStep(0);
                Check(Near(b.radius/radius,Mathf.Sqrt(.85f)),"Repeated updates do not compound visual shrink");
                g.laws.Clear(); b.ThermalStep(0); Check(Near(b.ShapeScale,Mathf.Sqrt(.85f)),"Lost mass stays lost when wear law removed");
                Prepare(b); Check(Near(b.radius,radius)&&Near(b.visual.transform.localScale.x,scale.x),"Reset restores original geometry and mass");
                g.laws.Add(WorldLaw.Abrasion);
                foreach(float speed in new[]{6f,8f,8.01f}) {Prepare(b,60,speed); b.ApplyGroundFrictionLaws(1,false,.1f); Check(speed>8 ? b.massRemaining<1 : Near(b.massRemaining,1),"Speed threshold: "+speed);}
                foreach(float temp in new[]{20f,40f,40.01f}) {Prepare(b,temp); b.ApplyGroundFrictionLaws(1,false,.1f); Check(temp>40 ? b.massRemaining<1 : Near(b.massRemaining,1),"Temperature threshold: "+temp);}
                Prepare(b); b.ApplyGroundFrictionLaws(1,true,.1f); Check(Near(b.massRemaining,.85f),"Already hot object still wears while sliding on ice");
                Prepare(b); for(int i=0;i<10;i++) b.ApplyGroundFrictionLaws(1,false,.01f); Check(Near(b.massRemaining,.85f),"Wear independent of simulation step subdivision");
                t.abrasionSpeedThreshold=12; Prepare(b); b.ApplyGroundFrictionLaws(1,false,.1f); Check(Near(b.massRemaining,1),"Live tuning speed threshold honored"); t.abrasionSpeedThreshold=threshold;
                t.abrasionHotTemperature=70; Prepare(b); b.ApplyGroundFrictionLaws(1,false,.1f); Check(Near(b.massRemaining,1),"Live tuning heat threshold honored"); t.abrasionHotTemperature=hot;
                t.abrasionPerMeter=.3f; Prepare(b); b.ApplyGroundFrictionLaws(1,false,.1f); Check(Near(b.massRemaining,.7f),"Live tuning erosion rate honored"); t.abrasionPerMeter=rate;
                foreach(var kind in new[]{BodyKind.Player,BodyKind.Enemy,BodyKind.Boss,BodyKind.Rock,BodyKind.Projectile})
                {b.kind=kind; Prepare(b); b.ApplyGroundFrictionLaws(1,false,.1f); Check(Near(b.massRemaining,.85f),"Shared erosion applies to "+kind);}
                b.kind=BodyKind.Projectile; Prepare(b); b.Body.bodyType=RigidbodyType2D.Static; b.ApplyGroundFrictionLaws(1,false,1); Check(Near(b.massRemaining,1),"Immovable obstacles do not wear"); b.Body.bodyType=RigidbodyType2D.Dynamic;
                Prepare(b); b.ApplyGroundFrictionLaws(1,false,0); Check(Near(b.massRemaining,1),"Zero duration does not wear");
                g.laws.Add(WorldLaw.FrictionHeat); Prepare(p,60,6); p.health=p.maxHealth*.5f;
                for(int i=0;i<500;i++) p.ApplyGroundFrictionLaws(4.5f,false,.02f);
                Check(!p.Dead&&Near(p.massRemaining,1)&&Near(p.health,p.maxHealth*.5f),"Half-health player walking at speed 6 survives both laws without erosion");
                g.laws.Remove(WorldLaw.FrictionHeat); g.laws.Add(WorldLaw.ThermalExpansion); Prepare(b,100); b.ThermalStep(.25f);
                float expanded=b.ShapeScale; b.ApplyGroundFrictionLaws(1,false,.1f);
                Check(expanded>1 && Near(b.ShapeScale,expanded*Mathf.Sqrt(.85f)),"Thermal expansion composes with permanent wear");
                g.laws.Remove(WorldLaw.ThermalExpansion); b.ThermalStep(0); Check(Near(b.ShapeScale,Mathf.Sqrt(.85f)),"Removing expansion retains worn geometry");
                Prepare(b); b.massRemaining=t.minimumMassFraction*1.01f; b.ApplyGroundFrictionLaws(1,false,.1f); Check(b.Dead,"Below minimum mass disappears rather than ignoring actors");
            }
            catch(Exception ex) {Check(false,ex.ToString());}
            finally {t.abrasionSpeedThreshold=threshold; t.abrasionHotTemperature=hot; t.abrasionPerMeter=rate; if(b!=null) UnityEngine.Object.Destroy(b.gameObject); g.Restart();}
            string report="failures="+failures+"\n"+string.Join("\n",results); Directory.CreateDirectory("Verification"); File.WriteAllText("Verification/friction-wear-regression.txt",report); Debug.Log(report); return report;
        }
    }
}
#endif
