using System;
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    public sealed class CollisionRules
    {
        readonly ArenaDirector world;
        ArenaTuning tuning => world.tuning;
        List<ThermoBody> bodies => world.bodies;
        RunMetrics metrics => world.metrics;
        WorldGenerator worldGenerator => world.worldGenerator;
        bool Has(WorldLaw law) => world.Has(law);
        void Pulse(Vector2 p,Color c,float radius,float duration=.45f) => world.Pulse(p,c,radius,duration);
        void Feedback(Vector2 p,string s,Color c) => world.Feedback(p,s,c);
        readonly Dictionary<long,float> impactAt=new Dictionary<long,float>();
        readonly Dictionary<int,float> terrainImpactAt=new Dictionary<int,float>();
        public CollisionRules(ArenaDirector world) { this.world=world; }
        public void Clear() { impactAt.Clear(); terrainImpactAt.Clear(); }
        public void ResolveTerrainCollision(ThermoBody body,TerrainCell tile,float speed,Vector2 point,Vector2 normal)
        {
            if(!world.SimulationActive || body.Dead || tile.rough || tile.phase==FloorPhase.Ice) return;
            // Neighboring river tiles form one surface: avoid multiplying a hit at tile seams.
            float previous; int id=body.GetInstanceID();
            if(terrainImpactAt.TryGetValue(id,out previous) && Time.time-previous<.1f) return;
            terrainImpactAt[id]=Time.time; if(terrainImpactAt.Count>4096) terrainImpactAt.Clear(); metrics.collisions++;
            if(Has(WorldLaw.Fission) && body.QualifiesForFission(speed))
            {
                Fission(body,.5f*body.Mass*speed*speed); body.Die("高速碎石撞击河岸爆炸",DamageKind.Fission); return;
            }
            float damage=ImpactDamage(tuning,body.Mass,speed);
            body.Damage(damage,"撞击河岸",DamageKind.Impact);
            if(damage>0 && ArenaAudio.Instance!=null) ArenaAudio.Instance.PlayWorld(ArenaAudio.Instance.waterImpact,point,.6f);
            metrics.largestImpact=Mathf.Max(metrics.largestImpact,damage);
            if(damage>10) Feedback(point,"河岸撞击 "+Mathf.RoundToInt(damage),new Color(1,.81f,.38f));
            DeliverProjectileHeat(body,point);
            if(Has(WorldLaw.ImpactHeat) && speed>tuning.impactHeatThreshold)
            {
                float heat=.5f*body.Mass*speed*speed*tuning.impactHeatFraction*tuning.impactTemperatureGain;
                body.AddHeat(Mathf.Min(400,heat/Mathf.Max(.1f,body.Mass))); tile.AddHeat(Mathf.Min(400,heat));
                body.Body.velocity-=normal*Vector2.Dot(body.Body.velocity,normal)*(1-Mathf.Sqrt(1-tuning.impactHeatFraction));
                body.ThermalStep(0);
            }
        }
        public void ResolveCollision(ThermoBody a,ThermoBody b,float speed,Vector2 point,float impulse=0)
        {
            if(!world.SimulationActive || a.Dead || b.Dead) return;
            int lo=Mathf.Min(a.GetInstanceID(),b.GetInstanceID()),hi=Mathf.Max(a.GetInstanceID(),b.GetInstanceID());
            long key=((long)lo<<32)|(uint)hi;
            float previous; if(impactAt.TryGetValue(key,out previous) && Time.time-previous<.1f) return;
            impactAt[key]=Time.time; if(impactAt.Count>4096) impactAt.Clear(); metrics.collisions++;
            if(Has(WorldLaw.Fission))
            {
                var particle=a.QualifiesForFission(speed) ? a : b.QualifiesForFission(speed) ? b : null;
                if(particle!=null)
                {
                    Fission(particle,.5f*particle.Mass*speed*speed);
                    particle.Die("高速碎石撞击爆炸",DamageKind.Fission);
                    return;
                }
            }
            float diff=Mathf.Abs(a.temperature-b.temperature);
            bool arc=Has(WorldLaw.ThermalArc) && ((a.temperature>80 && b.temperature<-50)||(b.temperature>80 && a.temperature<-50));
            float equilibrium=(a.temperature*a.Mass+b.temperature*b.Mass)/(a.Mass+b.Mass);
            var shot=a.kind==BodyKind.Projectile ? a : b.kind==BodyKind.Projectile ? b : null;
            if(shot!=null) (shot==a ? b : a).lastInteractor=shot.lastInteractor!=null ? shot.lastInteractor : shot.owner;
            if(speed>tuning.impactThreshold)
            {
                float effective=a.Body.bodyType!=RigidbodyType2D.Dynamic ? b.Mass : b.Body.bodyType!=RigidbodyType2D.Dynamic ? a.Mass : a.Mass*b.Mass/(a.Mass+b.Mass);
                float damage=ImpactDamage(tuning,effective,speed);
                a.Damage(damage,"环境撞击",DamageKind.Impact); b.Damage(damage,"环境撞击",DamageKind.Impact);
                if(damage>0 && (!a.IsActor || !b.IsActor) && ArenaAudio.Instance!=null) ArenaAudio.Instance.PlayWorld(ArenaAudio.Instance.stoneImpact,point,Mathf.Clamp(speed/30,.3f,1));
                metrics.largestImpact=Mathf.Max(metrics.largestImpact,damage);
                if(damage>10) Feedback(point,"撞击 "+Mathf.RoundToInt(damage),new Color(1,.81f,.38f));
            }
            if(arc)
            {
                a.temperature=equilibrium; b.temperature=equilibrium;
                float damage=diff*Mathf.Min(3,Mathf.Min(a.Mass,b.Mass))*tuning.arcDamagePerDegreeMass;
                React(point,2.7f,damage,0,null,DamageKind.Arc); metrics.arcs++;
                Feedback(point,"冷热相撞 → 放电",new Color(.77f,.7f,1));
            }
            // Contact equalization consumes the old temperature difference;
            // the tool's delivered heat is a separate input and must remain.
            DeliverProjectileHeat(a,point);
            DeliverProjectileHeat(b,point);
            if(Has(WorldLaw.ImpactHeat) && speed>tuning.impactHeatThreshold)
            {
                float effective=a.Body.bodyType!=RigidbodyType2D.Dynamic ? b.Mass : b.Body.bodyType!=RigidbodyType2D.Dynamic ? a.Mass : a.Mass*b.Mass/(a.Mass+b.Mass);
                float energy=.5f*effective*speed*speed;
                // Convert contact-normal relative kinetic energy into heat.
                float heat=energy*tuning.impactHeatFraction*tuning.impactTemperatureGain;
                float oldMassA=a.Mass,oldMassB=b.Mass;
                a.AddHeat(Mathf.Min(400,heat/Mathf.Max(.1f,a.Mass))); b.AddHeat(Mathf.Min(400,heat/Mathf.Max(.1f,b.Mass)));
                float retained=Mathf.Sqrt(1-tuning.impactHeatFraction);
                Vector2 centerVelocity=a.Body.bodyType!=RigidbodyType2D.Dynamic || b.Body.bodyType!=RigidbodyType2D.Dynamic ? Vector2.zero
                    : (a.Body.velocity*a.Mass+b.Body.velocity*b.Mass)/(a.Mass+b.Mass);
                Vector2 normal=(Vector2)b.Collider.bounds.center-(Vector2)a.Collider.bounds.center;
                if(normal.sqrMagnitude>.0001f)
                {
                    normal.Normalize();
                    if(a.Body.bodyType==RigidbodyType2D.Dynamic) a.Body.velocity-=normal*Vector2.Dot(a.Body.velocity-centerVelocity,normal)*(1-retained);
                    if(b.Body.bodyType==RigidbodyType2D.Dynamic) b.Body.velocity-=normal*Vector2.Dot(b.Body.velocity-centerVelocity,normal)*(1-retained);
                }
                a.ThermalStep(0); b.ThermalStep(0);
                Pulse(point,new Color(1,.4f,.1f),.65f,.25f);
                Feedback(point,a.Mass<oldMassA*.88f || b.Mass<oldMassB*.88f ? "撞击 → 升温 → 变轻" : "撞击 → 升温",new Color(1,.6f,.3f));
            }
        }
        public void DeliverProjectileHeat(ThermoBody body,Vector2 point)
        {
            if(body.kind!=BodyKind.Projectile || body.shotHeat==0) return;
            float heat=body.shotHeat; body.shotHeat=0;
            ApplyHeatBlast(point,heat,1.1f,body);
        }
        public void SteamWave(ThermoBody source,float cooling)
        {
            var center=(Vector2)source.Collider.bounds.center;
            using(var snapshot=BodySnapshot.Rent(bodies)) foreach(var b in snapshot.Bodies)
            {
                if(b==null || b==source || b.Dead || !b.gameObject.activeInHierarchy || b.Body.bodyType!=RigidbodyType2D.Dynamic) continue;
                Vector2 away=(Vector2)b.Collider.bounds.center-center;
                float distance=away.magnitude;
                if(distance>=tuning.steamWaveRadius) continue;
                if(distance<.001f) away=Vector2.up;
                b.ApplyForce(away.normalized*tuning.steamWaveImpulse*Mathf.Clamp(cooling/tuning.steamCoolingThreshold,1,3)*(1-distance/tuning.steamWaveRadius),ForceMode2D.Impulse);
            }
            Pulse(center,new Color(.8f,.93f,1),tuning.steamWaveRadius,.45f);
            Feedback(center,"骤冷 → 蒸汽冲击波",new Color(.8f,.93f,1)); metrics.vaporBursts++;
        }
        public static float ImpactDamage(ArenaTuning t,float mass,float speed)
        {
            if(speed<=(t!=null ? t.impactThreshold : 6.1f)) return 0;
            return Mathf.Min(t!=null ? t.impactDamageCap : 500,(t!=null ? t.impactAlpha : .1f)*mass*speed*speed+(t!=null ? t.impactBeta : 6)*mass);
        }
        public void ApplyHeatBlast(Vector2 p,float heat,float radius,ThermoBody source)
        {
            using(var snapshot=BodySnapshot.Rent(bodies)) foreach(var b in snapshot.Bodies)
                if(b!=null && !b.Dead && b.gameObject.activeInHierarchy && b!=source && Vector2.Distance(b.Collider.ClosestPoint(p),p)<radius) b.AddHeat(heat);
            if(worldGenerator==null) return;
            var c=new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y)); int r=Mathf.CeilToInt(radius)+1;
            for(int x=-r;x<=r;x++) for(int y=-r;y<=r;y++)
            {
                TerrainCell cell;
                if(!worldGenerator.cells.TryGetValue(c+new Vector2Int(x,y),out cell) || cell.rough || Vector2.Distance(cell.transform.position,p)>radius+.5f) continue;
                var old=cell.phase; cell.AddHeat(heat); if(old!=FloorPhase.Ice && cell.phase==FloorPhase.Ice) metrics.frozenTiles++;
            }
        }
        public void Fission(ThermoBody source,float energy)
        {
            metrics.explosions++; float damage=Mathf.Min(tuning.impactDamageCap,energy*tuning.fissionDamagePerEnergy);
            React(source.Body.position,2.8f,damage,Mathf.Min(80,energy*.08f),source,DamageKind.Fission);
            Feedback(source.Body.position,"变轻 + 高速 → 爆炸",new Color(.88f,.64f,1));
        }
        void React(Vector2 p,float radius,float damage,float heat,ThermoBody source,DamageKind kind)
        {
            Pulse(p,new Color(.8f,.6f,1),radius,.5f);
            using(var snapshot=BodySnapshot.Rent(bodies)) foreach(var b in snapshot.Bodies)
            {
                if(b==null || b.Dead || !b.gameObject.activeInHierarchy || b==source) continue;
                Vector2 delta=b.Body.position-p; if(delta.magnitude>radius+b.radius) continue;
                float attenuation=Mathf.Clamp01(1-delta.magnitude/(radius+1));
                b.AddHeat(heat*attenuation);
                if(b.Body.bodyType==RigidbodyType2D.Dynamic) b.ApplyForce(delta.normalized*Mathf.Min(36,damage*.07f)*attenuation,ForceMode2D.Impulse);
                b.Damage(damage*attenuation,"世界法则连锁反应",kind);
            }
            if(heat!=0) ApplyHeatBlast(p,heat,radius,source);
        }
    }
}
