using UnityEngine;
namespace PhaseArena
{
    // A contact-triggered state, with no extra collider or per-frame scene scan.
    public sealed class VaporGlideStatus
    {
        float until;
        bool latched;
        Vector2 normal;
        public bool Active => Time.time<until;
        public void Reset() {until=0; latched=false; normal=Vector2.zero;}
        public void Touch(ThermoBody b,ArenaDirector g,TerrainCell tile,Vector2 contactNormal)
        {
            if(!g.Has(WorldLaw.VaporGlide) || tile==null || tile.rough || b.temperature<=g.tuning.glideHotTemperature
                || tile.phase==FloorPhase.Steam || (tile.phase==FloorPhase.Water && tile.temperature>g.tuning.glideLiquidMaximumTemperature)) return;
            if(contactNormal.sqrMagnitude>.0001f) normal=contactNormal.normalized;
            if(latched) return;
            latched=true; until=Time.time+g.tuning.glideDuration;
            if(normal.sqrMagnitude<.0001f)
            {
                // An ice tile has no blocking collision. Use the nearest tile edge
                // as its in-plane surface normal in this top-down 2D simulation.
                var offset=b.Body.position-(Vector2)tile.transform.position;
                normal=Mathf.Abs(offset.x)>Mathf.Abs(offset.y) ? new Vector2(offset.x>=0?1:-1,0) : new Vector2(0,offset.y>=0?1:-1);
            }
            g.Feedback(b.Body.position,"热物接触水/冰 → 气垫滑行",new Color(.65f,.9f,1));
        }
        public void Tick(ThermoBody b,ArenaDirector g,float dt)
        {
            if(!g.Has(WorldLaw.VaporGlide)) {Reset(); return;}
            Vector2 point;
            var tile=g.worldGenerator.WetAt(b.Body.position,b.radius,out point);
            if(tile==null) latched=false;
            else Touch(b,g,tile,b.Body.position-point);
            if(Active) b.ApplyForce(normal*g.tuning.glideRepulsionForce*dt,ForceMode2D.Impulse);
        }
    }
}
