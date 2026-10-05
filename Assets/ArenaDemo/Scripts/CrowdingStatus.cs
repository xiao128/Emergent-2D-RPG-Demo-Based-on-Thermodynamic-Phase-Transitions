using UnityEngine;
namespace PhaseArena
{
    // Per-actor scan timer; damage remains time-based between spatial queries.
    public sealed class CrowdingStatus
    {
        public int Count { get; private set; }
        public float NextScanAt { get; private set; }=-1;
        public void Reset() { Count=0; NextScanAt=-1; }
        public void Tick(ThermoBody actor,ArenaDirector world,float dt)
        {
            if(!world.Has(WorldLaw.Crowding)) { Reset(); return; }
            float interval=Mathf.Max(.1f,world.tuning.crowdingCheckInterval);
            if(NextScanAt<0)
            {
                uint hash=unchecked((uint)actor.GetInstanceID()*2654435761u);
                NextScanAt=Time.time+(hash&65535)/65536f*interval;
            }
            if(Time.time>=NextScanAt)
            {
                Count=world.LawSimulation.CountCrowdingNeighbors(actor);
                NextScanAt=Time.time+interval;
            }
            int excess=Count-world.tuning.crowdingItemThreshold;
            if(excess>0) actor.Damage(excess*world.tuning.crowdingDamagePerExcessPerSecond*dt,"周围实体拥挤",DamageKind.Crowding,true);
        }
    }
}
