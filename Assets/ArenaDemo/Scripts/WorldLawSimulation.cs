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
        readonly Dictionary<ThermoBody,float> capacities=new Dictionary<ThermoBody,float>();
        Collider2D[] nearbyColliders=new Collider2D[64];
        readonly HashSet<ThermoBody> crowdingNeighbors=new HashSet<ThermoBody>();
        public int LastNeighborCandidates { get; private set; }
        public int LastCrowdingQueries { get; private set; }
        public float RadiationMultiplier => Has(WorldLaw.ExpandedRadiation) ? tuning.expandedRadiationMultiplier : 1;
        public float RadiationRadius => tuning.thermalRadiationRadius*RadiationMultiplier;
        public Vector2 CachedCenter(ThermoBody body)
        {
            Vector2 center; return centers.TryGetValue(body,out center) ? center : (Vector2)body.Collider.bounds.center;
        }
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
            && Mathf.Abs(body.temperature-tuning.ambientTemperature)>tuning.pressureTemperatureDeadZone;
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
            int poolIndex=0; thermalGrid.Clear(); centers.Clear(); capacities.Clear();
            using(var snapshot=BodySnapshot.Rent(bodies)) for(int i=snapshot.Bodies.Count-1;i>=0;i--)
            {
                var b=snapshot.Bodies[i]; if(b==null || b.Dead || b.IsCharging || !b.gameObject.activeInHierarchy) continue;
                b.ThermalStep(dt); if(b.Dead) continue;
                centers[b]=b.Collider.bounds.center;
                capacities[b]=b.HeatCapacity;
                var key=new Vector2Int(Mathf.FloorToInt(centers[b].x/2.5f),Mathf.FloorToInt(centers[b].y/2.5f));
                List<ThermoBody> bucket;
                if(!thermalGrid.TryGetValue(key,out bucket))
                {
                    if(poolIndex==gridPool.Count) gridPool.Add(new List<ThermoBody>());
                    bucket=gridPool[poolIndex++]; bucket.Clear(); thermalGrid[key]=bucket;
                }
                bucket.Add(b);
            }
            float heatRadius=RadiationRadius;
            world.FlowFields.BeginTick();
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
                        if(b.Dead || b==a || b.GetInstanceID()<=a.GetInstanceID() || (centers[a]-centers[b]).sqrMagnitude>heatRadius*heatRadius) continue;
                        float ca=capacities[a],cb=capacities[b];
                        float q=(a.temperature-b.temperature)*Mathf.Clamp01(tuning.bodyHeatConductivity*dt)/(1/ca+1/cb);
                        Vector2 direction=centers[b]-centers[a];
                        a.ExchangeHeat(-q,-direction,ca,world); b.ExchangeHeat(q,direction,cb,world);
                        if(Has(WorldLaw.RecoilPropellant)) {capacities[a]=a.HeatCapacity; capacities[b]=b.HeatCapacity;}
                    }
                }
                world.FlowFields.TickSource(a,dt);
                var floor=FloorAt(a.Body.position);
                if(floor!=null && !floor.rough)
                {
                    float q=(a.temperature-floor.temperature)*Mathf.Clamp01(tuning.terrainHeatConductivity*dt)/(1/a.HeatCapacity+1/floor.HeatCapacity);
                    a.AddHeat(-q,a.Body.position-(Vector2)floor.transform.position); floor.AddHeat(q);
                    if(Has(WorldLaw.RecoilPropellant)) capacities[a]=a.HeatCapacity;
                }
            }
            using(var snapshot=BodySnapshot.Rent(bodies)) foreach(var actor in snapshot.Bodies)
                {
                    if(actor==null || !actor.IsActor || actor.Dead || actor.IsCharging || !actor.gameObject.activeInHierarchy) continue;
                    actor.TickCrowding(dt);
                }
            world.FlowFields.EndTick();
            foreach(var tile in terrain) if(tile!=null) tile.Tick(dt);
        }
        public void FillNearby(ThermoBody source,float radius,List<ThermoBody> result)
        {
            result.Clear();
            Vector2 center=source.Collider.bounds.center;
            var key=new Vector2Int(Mathf.FloorToInt(center.x/2.5f),Mathf.FloorToInt(center.y/2.5f));
            int reach=Mathf.CeilToInt(radius/2.5f);
            for(int x=-reach;x<=reach;x++) for(int y=-reach;y<=reach;y++)
            {
                List<ThermoBody> bucket;
                if(!thermalGrid.TryGetValue(key+new Vector2Int(x,y),out bucket)) continue;
                foreach(var b in bucket)
                {
                    LastNeighborCandidates++;
                    if(b!=null && !b.Dead && (centers[b]-center).sqrMagnitude<radius*radius) result.Add(b);
                }
            }
        }
        // Compatibility entry point for historical verification scripts. This law
        // was retired; even manually adding its old enum no longer applies force.
        public void ApplyRepulsion(ThermoBody a,ThermoBody b,float dt) { }
    }
}
