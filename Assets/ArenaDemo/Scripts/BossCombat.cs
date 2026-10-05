using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(EnemyBrain))]
    public sealed class BossCombat : MonoBehaviour
    {
        ThermoBody self;
        EnemyBrain brain;
        float volleyAt,summonAt;
        readonly Collider2D[] occupied=new Collider2D[32];
        public int Volleys { get; private set; }
        public int Summons { get; private set; }
        public float NextSummonAt => summonAt;
        public void ResetTimers()
        {
            self=GetComponent<ThermoBody>(); brain=GetComponent<EnemyBrain>();
            var g=ArenaDirector.Instance;
            if(g==null) return;
            volleyAt=Time.time+VolleyInterval(g);
            summonAt=Time.time+Mathf.Max(.1f,g.tuning.bossSummonInterval);
            Volleys=Summons=0;
        }
        float VolleyInterval(ArenaDirector g) => Mathf.Max(.1f,g.tuning.EnemyAttackTime(g.tuning.bossVolleyInterval)/brain.TemperatureAttackMultiplier);
        void FixedUpdate()
        {
            var g=ArenaDirector.Instance;
            if(g==null || !g.CombatActive || self==null || self.Dead || !brain.enabled || g.player==null || g.player.Dead) return;
            if(Time.time>=summonAt)
            {
                var guard=SummonGuard(g);
                summonAt=Time.time+(guard!=null ? Mathf.Max(.1f,g.tuning.bossSummonInterval) : 1);
            }
            // Ranged pressure continues during melee recovery instead of waiting for that AI branch.
            if(!brain.Alert || Time.time<volleyAt) return;
            volleyAt=Time.time+VolleyInterval(g); Volleys++;
            Vector2 aim=(g.player.Body.position-self.Body.position).normalized;
            if(aim.sqrMagnitude>.01f) g.Shoot(self,aim,Volleys%2==0 ? 105 : -95);
            int radialCount=Mathf.Max(0,g.tuning.bossRadialProjectiles);
            float step=Mathf.PI*2/Mathf.Max(1,radialCount);
            float rotation=Mathf.Atan2(aim.y,aim.x)+step*.5f;
            for(int i=0;i<radialCount;i++)
            {
                float angle=rotation+i*step;
                g.Shoot(self,new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),i%2==0 ? 105 : -95);
            }
            g.Pulse(self.Body.position,new Color(1,.4f,.2f),2);
        }
        ThermoBody SummonGuard(ArenaDirector g)
        {
            if(g.guardPrefab==null) return null;
            float radius=g.guardPrefab.radius+.25f;
            float distance=Mathf.Max(g.tuning.bossSummonDistance,self.radius+radius+.25f);
            Vector2 toward=(g.player.Body.position-self.Body.position).normalized;
            float angle=Mathf.Atan2(toward.y,toward.x);
            for(int i=0;i<16;i++)
            {
                float a=angle+i*Mathf.PI/8;
                Vector2 desired=self.Body.position+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*distance;
                Vector2 p=g.worldGenerator.SafeDryPosition(desired,radius);
                // A distant fallback is not a valid local summon spot.
                if(Vector2.Distance(p,self.Body.position)>distance+3) continue;
                int count=Physics2D.OverlapCircleNonAlloc(p,radius,occupied);
                bool blocked=count==occupied.Length;
                for(int j=0;j<count;j++) if(!occupied[j].isTrigger) { blocked=true; break; }
                if(blocked) continue;
                var guard=g.Spawn(g.guardPrefab,p);
                guard.isElite=true; guard.maxHealth=g.tuning.EnemyHealthAt(g.tuning.guardHealth,g.World); guard.health=guard.maxHealth;
                guard.GetComponent<EnemyBrain>().SetHome(p,false);
                Summons++; g.Pulse(p,new Color(1,.65f,.2f),radius+1,.6f);
                g.Feedback(p,"魔王召唤守卫",new Color(1,.8f,.35f)); return guard;
            }
            return null;
        }
    }
}
