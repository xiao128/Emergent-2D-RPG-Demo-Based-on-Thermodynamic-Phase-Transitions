#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    // Frozen pre-refactor tick for comparative CPU benchmarks only.
    public sealed class LegacyLawTickBenchmark
    {
        readonly ArenaDirector world;
        ArenaTuning tuning => world.tuning;
        List<ThermoBody> bodies => world.bodies;
        RunMetrics metrics => world.metrics;
        TerrainCell[] terrain => world.terrain;
        bool Has(WorldLaw law) => world.Has(law);
        TerrainCell FloorAt(Vector2 p) => world.FloorAt(p);
        void Feedback(Vector2 p,string s,Color c) => world.Feedback(p,s,c);
        readonly Dictionary<Vector2Int,List<ThermoBody>> thermalGrid=new Dictionary<Vector2Int,List<ThermoBody>>();
        readonly List<List<ThermoBody>> gridPool=new List<List<ThermoBody>>();
        public LegacyLawTickBenchmark(ArenaDirector world) { this.world=world; }
        public void Tick(float dt)
        {
            int poolIndex=0; thermalGrid.Clear();
            for(int i=bodies.Count-1;i>=0;i--)
            {
                var b=bodies[i]; if(b==null) { bodies.RemoveAt(i); continue; } if(b.Dead || b.IsCharging || !b.gameObject.activeInHierarchy) continue;
                b.ThermalStep(dt); if(b.Dead) continue;
                var key=new Vector2Int(Mathf.FloorToInt(b.Body.position.x/2.5f),Mathf.FloorToInt(b.Body.position.y/2.5f));
                List<ThermoBody> bucket;
                if(!thermalGrid.TryGetValue(key,out bucket))
                {
                    if(poolIndex==gridPool.Count) gridPool.Add(new List<ThermoBody>());
                    bucket=gridPool[poolIndex++]; bucket.Clear(); thermalGrid[key]=bucket;
                }
                bucket.Add(b);
            }
            foreach(var cell in thermalGrid) foreach(var a in cell.Value)
            {
                if(a.Dead) continue;
                for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++)
                {
                    List<ThermoBody> nearby;
                    if(!thermalGrid.TryGetValue(cell.Key+new Vector2Int(x,y),out nearby)) continue;
                    foreach(var b in nearby)
                    {
                        if(b.Dead || b==a || b.GetInstanceID()<=a.GetInstanceID() || (a.Body.position-b.Body.position).sqrMagnitude>5.76f) continue;
                        float q=(a.temperature-b.temperature)*.45f*dt/(1/a.Mass+1/b.Mass);
                        a.AddHeat(-q/a.Mass); b.AddHeat(q/b.Mass);
                    }
                }
                if(Has(WorldLaw.Gravity) && a.Density>=tuning.gravityDensityThreshold)
                {
                    foreach(var b in bodies)
                    {
                        if(b==null || b==a || b.Dead || b.IsCharging || !b.gameObject.activeInHierarchy || b.Body.bodyType!=RigidbodyType2D.Dynamic) continue;
                        Vector2 d=(Vector2)a.Collider.bounds.center-(Vector2)b.Collider.bounds.center;
                        if(d.sqrMagnitude<tuning.gravityRadius*tuning.gravityRadius && d.sqrMagnitude>.25f)
                            // Thermal ticks are less frequent than physics steps.
                            // Integrate force over the full tick, rather than one fixed frame.
                            b.ApplyForce(d.normalized*(tuning.gravityForce/(1+d.sqrMagnitude*.08f))*dt,ForceMode2D.Impulse);
                    }
                }
                var floor=FloorAt(a.Body.position);
                if(floor!=null && !floor.rough)
                {
                    float q=(a.temperature-floor.temperature)*.35f*dt; a.AddHeat(-q); floor.AddHeat(q*.7f);
                }
            }
            if(Has(WorldLaw.Crowding))
                foreach(var cell in thermalGrid)
                {
                    int count=cell.Value.FindAll(b=>b!=null && !b.Dead && b.IsCrowdingItem).Count;
                    if(count<=tuning.crowdingItemThreshold) continue;
                    float damage=(count-tuning.crowdingItemThreshold)*tuning.crowdingDamagePerExcessPerSecond*dt;
                    foreach(var b in cell.Value) if(b!=null && !b.Dead && b.IsCrowdingItem) b.Damage(damage,"拥挤",DamageKind.Crowding,true);
                }
            if(Has(WorldLaw.Leidenfrost))
            {
                for(int i=0;i<bodies.Count;i++) for(int j=i+1;j<bodies.Count;j++)
                    ApplyRepulsion(bodies[i],bodies[j],dt);
            }
            foreach(var tile in terrain) if(tile!=null) tile.Tick(dt);
        }
        void ApplyRepulsion(ThermoBody a,ThermoBody b,float dt)
        {
            if(a==null || b==null || a.Dead || b.Dead || a.IsCharging || b.IsCharging || !a.gameObject.activeInHierarchy || !b.gameObject.activeInHierarchy
                || !a.Collider.enabled || !b.Collider.enabled) return;
            bool movableA=a.Body.bodyType==RigidbodyType2D.Dynamic, movableB=b.Body.bodyType==RigidbodyType2D.Dynamic;
            if(!movableA && !movableB) return;
            float difference=Mathf.Abs(a.temperature-b.temperature);
            if(difference<=tuning.repulsionTemperatureGap) return;
            if(a.kind!=BodyKind.StaticObstacle && b.kind!=BodyKind.StaticObstacle &&
                Vector2.Distance(a.Body.position,b.Body.position)>a.radius+b.radius+tuning.repulsionRange+.1f) return;
            var separation=a.Collider.Distance(b.Collider);
            if(!separation.isValid || separation.distance>tuning.repulsionRange) return;
            Vector2 direction=separation.pointB-separation.pointA;
            if(separation.isOverlapped || direction.sqrMagnitude<.0001f) direction=b.Body.position-a.Body.position;
            if(direction.sqrMagnitude<.0001f) direction=Vector2.right;
            direction.Normalize();
            float effective=movableA && movableB ? a.Mass*b.Mass/(a.Mass+b.Mass) : movableA ? a.Mass : b.Mass;
            float strength=difference*tuning.repulsionPerDegree*effective*
                Mathf.Clamp01(1-Mathf.Max(0,separation.distance)/tuning.repulsionRange);
            // Equal and opposite impulses; a fixed obstacle transfers its
            // reaction to the world rather than becoming movable.
            if(movableA) a.ApplyForce(-direction*strength*dt,ForceMode2D.Impulse);
            if(movableB) b.ApplyForce(direction*strength*dt,ForceMode2D.Impulse);
            metrics.repulsions++;
            if(strength>8) Feedback((separation.pointA+separation.pointB)*.5f,"温差 → 热胀斥力",new Color(1,.65f,.38f));
        }
    }
}
#endif
