using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    // Shares the thermal spatial grid. No scene scans, per-source allocations or
    // unbounded field radii; all forces are impulses integrated with the tick dt.
    public sealed class FlowFieldSimulation
    {
        readonly ArenaDirector g;
        readonly List<ThermoBody> nearby=new List<ThermoBody>();
        readonly Dictionary<ThermoBody,Vector2> impulses=new Dictionary<ThermoBody,Vector2>();
        bool deferred;
        public int LastAppliedBodies { get; private set; }
        public FlowFieldSimulation(ArenaDirector world) {g=world;}
        public void BeginTick() {impulses.Clear(); deferred=true; LastAppliedBodies=0;}
        public void EndTick()
        {
            foreach(var item in impulses) if(item.Key!=null && !item.Key.Dead) {item.Key.ApplyForce(item.Value,ForceMode2D.Impulse); LastAppliedBodies++;}
            deferred=false;
        }
        public void TickSource(ThermoBody source,float dt)
        {
            var t=g.tuning;
            float gap=source.temperature-t.ambientTemperature;
            bool pressure=g.Has(WorldLaw.Gravity) && Mathf.Abs(gap)>t.pressureTemperatureDeadZone;
            float diameter=Mathf.Max(.1f,Mathf.Max(source.Collider.bounds.size.x,source.Collider.bounds.size.y));
            float speed=source.Body.velocity.magnitude;
            bool wake=g.Has(WorldLaw.BernoulliWake) && source.Body.bodyType==RigidbodyType2D.Dynamic && speed/diameter>t.wakeSpeedPerDiameter;
            if(!pressure && !wake) return;
            float wakeRadius=Mathf.Clamp(diameter*t.wakeRadiusPerDiameter,t.wakeMinimumRadius,Mathf.Max(t.wakeMinimumRadius,t.wakeMaximumRadius));
            g.LawSimulation.FillNearby(source,Mathf.Max(pressure?t.pressureRadius:0,wake?wakeRadius:0),nearby);
            Vector2 center=g.LawSimulation.CachedCenter(source),forward=speed>0 ? source.Body.velocity/speed : Vector2.zero;
            float wakeForce=wake ? Mathf.Min(t.wakeMaximumForce,speed*speed*source.Area*t.wakeForceCoefficient) : 0;
            foreach(var target in nearby)
            {
                if(target==source || target==null || target.Dead || target.IsCharging || target.Body.bodyType!=RigidbodyType2D.Dynamic) continue;
                Vector2 away=g.LawSimulation.CachedCenter(target)-center; float distance=away.magnitude;
                if(distance<.05f) continue;
                Vector2 direction=away/distance,force=Vector2.zero;
                if(pressure && distance<t.pressureRadius)
                    force+=direction*Mathf.Clamp(gap*t.pressureForcePerDegree,-t.pressureMaximumForce,t.pressureMaximumForce)*Mathf.Pow(1-distance/t.pressureRadius,2);
                if(wake && distance<wakeRadius && Vector2.Dot(direction,forward)<=t.wakeForwardExclusionDot)
                    force-=direction*wakeForce*Mathf.Pow(1-distance/wakeRadius,2);
                if(force.sqrMagnitude==0) continue;
                if(!deferred) target.ApplyForce(force*dt,ForceMode2D.Impulse);
                else {Vector2 accumulated; impulses.TryGetValue(target,out accumulated); impulses[target]=accumulated+force*dt;}
            }
        }
    }
}
