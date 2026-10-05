using UnityEngine;
namespace PhaseArena
{
    // Same-frame burst protection; slow attrition may still be lethal.
    public sealed class PlayerDeathGuard : MonoBehaviour
    {
        public bool Invulnerable => Time.time<invulnerableUntil;
        float invulnerableUntil;
        int damageFrame=-1;
        float burstDamage;
        public void ResetProtection() { invulnerableUntil=0; damageFrame=-1; burstDamage=0; }
        public float LimitDamage(ThermoBody body,float requested)
        {
            if(Invulnerable) return 0;
            if(damageFrame!=Time.frameCount) { damageFrame=Time.frameCount; burstDamage=0; }
            burstDamage+=Mathf.Min(requested,Mathf.Max(0,body.health));
            if(requested>=body.health && burstDamage>body.maxHealth*.2f && body.health>0)
            {
                invulnerableUntil=Time.time+1;
                var world=ArenaDirector.Instance;
                if(world!=null) world.Feedback(body.Body.position,"免死 · 1 秒无敌",new Color(1,.9f,.45f));
                return Mathf.Max(0,body.health-1);
            }
            return requested;
        }
    }
}
