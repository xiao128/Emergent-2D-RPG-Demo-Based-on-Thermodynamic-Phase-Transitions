using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    // Contact callbacks maintain short-lived pairs. Physics ticks resolve tangential
    // damping and a normal spring without creating hundreds of Joint components.
    public sealed class ContactLawSimulation
    {
        sealed class Weld { public ThermoBody a,b; public float rest,lastSeen; }
        readonly ArenaDirector g;
        readonly Dictionary<long,Weld> pairs=new Dictionary<long,Weld>();
        readonly List<long> expired=new List<long>();
        PhysicsMaterial2D glideNormal,glideBouncy;
        public int ActiveWelds => pairs.Count;
        public ContactLawSimulation(ArenaDirector world) {g=world;}
        public void Clear() {pairs.Clear(); expired.Clear();}
        public PhysicsMaterial2D GlideMaterial(bool bounce)
        {
            var material=bounce ? glideBouncy : glideNormal;
            var original=bounce ? g.bouncyMaterial : g.normalMaterial;
            if(material==null)
            {
                material=new PhysicsMaterial2D("Vapor cushion (runtime)"){friction=0,bounciness=original!=null?original.bounciness:0};
                if(bounce) glideBouncy=material; else glideNormal=material;
            }
            return material;
        }
        public void Dispose()
        {
            Clear(); if(glideNormal!=null) Object.Destroy(glideNormal); if(glideBouncy!=null) Object.Destroy(glideBouncy);
        }
        bool Hot(ThermoBody a,ThermoBody b) => a!=null && b!=null && !a.Dead && !b.Dead && a.gameObject.activeInHierarchy && b.gameObject.activeInHierarchy
            && a.temperature>g.tuning.weldingHotTemperature && b.temperature>g.tuning.weldingHotTemperature;
        public void Touch(ThermoBody a,ThermoBody b,Vector2 point)
        {
            if(!g.SimulationActive || !g.Has(WorldLaw.ViscousWelding) || !Hot(a,b)
                || (a.Body.bodyType!=RigidbodyType2D.Dynamic && b.Body.bodyType!=RigidbodyType2D.Dynamic)) return;
            int lo=Mathf.Min(a.GetInstanceID(),b.GetInstanceID()),hi=Mathf.Max(a.GetInstanceID(),b.GetInstanceID());
            long key=((long)lo<<32)|(uint)hi; Weld pair;
            if(!pairs.TryGetValue(key,out pair))
            {
                if(pairs.Count>=g.tuning.weldingMaximumPairs) return;
                pair=new Weld{a=a,b=b,rest=Vector2.Distance(a.Collider.bounds.center,b.Collider.bounds.center)}; pairs.Add(key,pair);
                g.metrics.welds++;
                g.Feedback(point,"高热接触 → 表面熔接",new Color(1,.55f,.3f));
            }
            pair.lastSeen=Time.time;
        }
        public void Tick(float dt)
        {
            if(!g.Has(WorldLaw.ViscousWelding)) {Clear(); return;}
            expired.Clear(); var t=g.tuning;
            foreach(var entry in pairs)
            {
                var p=entry.Value;
                if(!Hot(p.a,p.b) || Time.time-p.lastSeen>t.weldingContactGrace) {expired.Add(entry.Key); continue;}
                var a=p.a; var b=p.b; Vector2 delta=(Vector2)b.Collider.bounds.center-(Vector2)a.Collider.bounds.center;
                float distance=delta.magnitude; if(distance<.0001f) continue;
                Vector2 n=delta/distance,tangent=new Vector2(-n.y,n.x),relative=b.Body.velocity-a.Body.velocity;
                bool ma=a.Body.bodyType==RigidbodyType2D.Dynamic,mb=b.Body.bodyType==RigidbodyType2D.Dynamic;
                float effective=ma&&mb ? a.Mass*b.Mass/(a.Mass+b.Mass) : ma?a.Mass:b.Mass;
                float friction=t.weldingBaseFriction+t.weldingFrictionPerDegree*(Mathf.Min(a.temperature,b.temperature)-t.weldingHotTemperature);
                Vector2 damping=tangent*Vector2.Dot(relative,tangent)*effective*Mathf.Clamp01(friction*dt);
                float pull=Mathf.Clamp((distance-p.rest)*t.weldingSpring+Vector2.Dot(relative,n)*t.weldingDamping,0,t.weldingMaximumForce);
                Vector2 impulse=damping+n*pull*dt;
                if(ma) a.ApplyForce(impulse,ForceMode2D.Impulse); if(mb) b.ApplyForce(-impulse,ForceMode2D.Impulse);
            }
            foreach(long key in expired) pairs.Remove(key);
        }
        public bool TryFusion(ThermoBody a,ThermoBody b,Vector2 point)
        {
            if(!g.Has(WorldLaw.NuclearFusion) || a.Body.bodyType!=RigidbodyType2D.Dynamic || b.Body.bodyType!=RigidbodyType2D.Dynamic) return false;
            Vector2 va=a.IncomingVelocity,vb=b.IncomingVelocity;
            float sa=va.magnitude,sb=vb.magnitude; var t=g.tuning;
            if(sa<=t.fusionMinimumSpeed || sb<=t.fusionMinimumSpeed || Vector2.Dot(va/sa,vb/sb)>=t.fusionOpposingDot) return false;
            Vector2 normal=(Vector2)b.Collider.bounds.center-(Vector2)a.Collider.bounds.center;
            if(normal.sqrMagnitude<.0001f) normal=va-vb;
            normal.Normalize();
            if(Vector2.Dot(va,normal)<=0 || Vector2.Dot(vb,normal)>=0) return false;
            float closing=Vector2.Dot(va-vb,normal);
            float effective=a.Mass*b.Mass/(a.Mass+b.Mass);
            g.Collisions.Fusion(point,.5f*effective*closing*closing);
            return true;
        }
    }
}
