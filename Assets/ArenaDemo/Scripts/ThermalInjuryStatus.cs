using UnityEngine;
namespace PhaseArena
{
    // Per-body thermal exposure, driven by the world's simulation clock.
    // Whole damage ticks keep hit feedback readable instead of flashing for fractional damage every update.
    public sealed class ThermalInjuryStatus
    {
        float elapsed;
        int exposure;
        public void Reset() { elapsed=0; exposure=0; }
        public void Tick(ThermoBody body,ArenaDirector world,float dt)
        {
            var t=world.tuning;
            int next=body.temperature>t.burnTemperature ? 1 : body.temperature<t.frostTemperature ? -1 : 0;
            if(!world.Has(WorldLaw.ThermalInjury) || body.indestructible || body.Dead || next==0) { Reset(); return; }
            if(next!=exposure) { Reset(); exposure=next; }
            if(dt<=0) return;
            elapsed+=dt;
            float interval=Mathf.Max(.01f,exposure>0 ? t.burnInterval : t.frostInterval);
            if(elapsed+.00001f<interval) return;
            // The normal fixed tick is shorter than either interval. Coalesce catch-up
            // damage if a diagnostic/large simulation step spans multiple intervals.
            int ticks=Mathf.FloorToInt((elapsed+.00001f)/interval);
            elapsed=Mathf.Max(0,elapsed-ticks*interval);
            body.Damage(ticks*(exposure>0 ? t.burnDamage : t.frostDamage),exposure>0 ? "灼伤" : "冰伤",
                exposure>0 ? DamageKind.Overheat : DamageKind.Frostbite);
        }
    }
}
