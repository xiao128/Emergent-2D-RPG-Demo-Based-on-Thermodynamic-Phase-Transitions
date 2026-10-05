using System;
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    public sealed class WorldLawSimulation
    {
        readonly ArenaDirector world;
        ArenaTuning tuning => world.tuning;
        List<ThermoBody> bodies => world.bodies;
        RunMetrics metrics => world.metrics;
        WorldGenerator worldGenerator => world.worldGenerator;
        bool Has(WorldLaw law) => world.Has(law);
        void Pulse(Vector2 p,Color c,float radius,float duration=.45f) => world.Pulse(p,c,radius,duration);
        void Feedback(Vector2 p,string s,Color c) => world.Feedback(p,s,c);
        readonly Dictionary<Vector2Int,List<ThermoBody>> thermalGrid=new Dictionary<Vector2Int,List<ThermoBody>>();
        readonly List<List<ThermoBody>> gridPool=new List<List<ThermoBody>>();
        readonly Dictionary<ThermoBody,Vector2> centers=new Dictionary<ThermoBody,Vector2>();
        bool denseFallback;
        Collider2D[] nearbyColliders=new Collider2D[64];
        readonly HashSet<ThermoBody> crowdingNeighbors=new HashSet<ThermoBody>();
        public int LastNeighborCandidates { get; private set; }
        public int LastCrowdingQueries { get; private set; }
        public float RadiationMultiplier => Has(WorldLaw.ExpandedRadiation) ? tuning.expandedRadiationMultiplier : 1;
        public float RadiationRadius => tuning.thermalRadiationRadius*RadiationMultiplier;
        int Query(Vector2 center,float radius)
        {
            int count;
            while(true)
            {
                count=Physics2D.OverlapCircleNonAlloc(center,radius,nearbyColliders);
                if(count<nearbyColliders.Length) return count;
                Array.Resize(ref nearbyColliders,nearbyColliders.Length*2);
            }
        }
        TerrainCell[] terrain => world.terrain;
        TerrainCell FloorAt(Vector2 p) => world.FloorAt(p);
        public WorldLawSimulation(ArenaDirector world) { this.world=world; }
        public bool IsGravitySource(ThermoBody body) => body!=null && !body.Dead
            && body.Mass>tuning.gravityMassThreshold && body.temperature<tuning.gravityColdTemperature;
        public int CountCrowdingNeighbors(ThermoBody actor)
        {
            LastCrowdingQueries++;
            crowdingNeighbors.Clear();
            int count=Query(actor.Collider.bounds.center,Mathf.Max(0,tuning.crowdingRadius));
            for(int i=0;i<count;i++)
            {
                var other=world.BodyFor(nearbyColliders[i]);
                if(other==null) other=nearbyColliders[i].GetComponentInParent<ThermoBody>();
                if(other!=null && other!=actor && !other.Dead && !other.IsCharging && other.gameObject.activeInHierarchy)
                    crowdingNeighbors.Add(other);
            }
            return crowdingNeighbors.Count;
        }
        public void Tick(float dt)
        {
            LastNeighborCandidates=0; LastCrowdingQueries=0;
            int poolIndex=0; thermalGrid.Clear(); centers.Clear(); denseFallback=false;
            using(var snapshot=BodySnapshot.Rent(bodies)) for(int i=snapshot.Bodies.Count-1;i>=0;i--)
            {
                var b=snapshot.Bodies[i]; if(b==null || b.Dead || b.IsCharging || !b.gameObject.activeInHierarchy) continue;
                b.ThermalStep(dt); if(b.Dead) continue;
                centers[b]=b.Collider.bounds.center;
                var key=new Vector2Int(Mathf.FloorToInt(b.Body.position.x/2.5f),Mathf.FloorToInt(b.Body.position.y/2.5f));
                List<ThermoBody> bucket;
                if(!thermalGrid.TryGetValue(key,out bucket))
                {
                    if(poolIndex==gridPool.Count) gridPool.Add(new List<ThermoBody>());
                    bucket=gridPool[poolIndex++]; bucket.Clear(); thermalGrid[key]=bucket;
                }
                bucket.Add(b);
                if(bucket.Count>=64) denseFallback=true;
            }
            float heatRadius=RadiationRadius;
            int neighborReach=Mathf.CeilToInt(heatRadius/2.5f);
            foreach(var cell in thermalGrid) foreach(var a in cell.Value)
            {
                if(a.Dead) continue;
                for(int x=-neighborReach;x<=neighborReach;x++) for(int y=-neighborReach;y<=neighborReach;y++)
                {
                    List<ThermoBody> nearby;
                    if(!thermalGrid.TryGetValue(cell.Key+new Vector2Int(x,y),out nearby)) continue;
                    foreach(var b in nearby)
                    {
                        if(b.Dead || b==a || b.GetInstanceID()<=a.GetInstanceID() || (a.Body.position-b.Body.position).sqrMagnitude>heatRadius*heatRadius) continue;
                        float q=(a.temperature-b.temperature)*.45f*dt/(1/a.Mass+1/b.Mass);
                        a.AddHeat(-q/a.Mass); b.AddHeat(q/b.Mass);
                    }
                }
                if(Has(WorldLaw.Gravity) && IsGravitySource(a))
                {
                    if(denseFallback)
                    {
                        for(int i=0;i<bodies.Count;i++) ApplyAttraction(a,bodies[i],dt);
                    }
                    else
                    {
                        int count=Query(centers[a],tuning.gravityRadius);
                        for(int i=0;i<count;i++) ApplyAttraction(a,world.BodyFor(nearbyColliders[i]),dt);
                    }
                }
                var floor=FloorAt(a.Body.position);
                if(floor!=null && !floor.rough)
                {
                    float q=(a.temperature-floor.temperature)*.35f*dt; a.AddHeat(-q); floor.AddHeat(q*.7f);
                }
            }
            using(var snapshot=BodySnapshot.Rent(bodies)) foreach(var actor in snapshot.Bodies)
                {
                    if(actor==null || !actor.IsActor || actor.Dead || actor.IsCharging || !actor.gameObject.activeInHierarchy) continue;
                    actor.TickCrowding(dt);
                }
            if(Has(WorldLaw.Leidenfrost))
            {
                if(denseFallback)
                {
                    using(var snapshot=BodySnapshot.Rent(bodies))
                    for(int i=0;i<snapshot.Bodies.Count;i++) for(int j=i+1;j<snapshot.Bodies.Count;j++)
                    {
                        LastNeighborCandidates++;
                        var a=snapshot.Bodies[i]; var b=snapshot.Bodies[j];
                        if(a==null || b==null || Mathf.Abs(a.temperature-b.temperature)<=tuning.repulsionTemperatureGap) continue;
                        ApplyRepulsion(a,b,dt);
                    }
                }
                else
                using(var snapshot=BodySnapshot.Rent(bodies)) foreach(var a in snapshot.Bodies)
                {
                    if(a==null || a.Dead || !a.gameObject.activeInHierarchy || a.Body.bodyType!=RigidbodyType2D.Dynamic) continue;
                    int count=Query(a.Collider.bounds.center,a.Collider.bounds.extents.magnitude+tuning.repulsionRange);
                    for(int i=0;i<count;i++)
                    {
                        var b=world.BodyFor(nearbyColliders[i]); LastNeighborCandidates++;
                        if(b==null || b==a || (b.Body.bodyType==RigidbodyType2D.Dynamic && b.GetInstanceID()<=a.GetInstanceID())) continue;
                        ApplyRepulsion(a,b,dt);
                    }
                }
            }
            foreach(var tile in terrain) if(tile!=null) tile.Tick(dt);
        }
        void ApplyAttraction(ThermoBody source,ThermoBody target,float dt)
        {
            LastNeighborCandidates++;
            Vector2 targetCenter;
            if(target==null || target==source || target.Dead || target.IsCharging || !centers.TryGetValue(target,out targetCenter) || target.Body.bodyType!=RigidbodyType2D.Dynamic) return;
            Vector2 delta=centers[source]-targetCenter;
            float distanceSquared=delta.sqrMagnitude;
            if(distanceSquared>=tuning.gravityRadius*tuning.gravityRadius || distanceSquared<=.25f) return;
            target.ApplyForce(delta.normalized*(tuning.gravityForce/(1+distanceSquared*.08f))*dt,ForceMode2D.Impulse);
        }
        public void ApplyRepulsion(ThermoBody a,ThermoBody b,float dt)
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
