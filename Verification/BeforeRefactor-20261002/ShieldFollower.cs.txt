using UnityEngine;
using System.Collections.Generic;
namespace PhaseArena
{
    [RequireComponent(typeof(ThermoBody))]
    public class ShieldFollower : MonoBehaviour
    {
        public Vector2 offset;
        ThermoBody shield;
        BoxCollider2D box;
        readonly List<ThermoBody> exiting=new List<ThermoBody>();
        void Awake() { shield=GetComponent<ThermoBody>(); box=GetComponent<BoxCollider2D>(); }
        public void AllowExit(ThermoBody enemy)
        {
            if(enemy==null || exiting.Contains(enemy)) return;
            exiting.Add(enemy); Physics2D.IgnoreCollision(box,enemy.GetComponent<Collider2D>(),true);
        }
        void FixedUpdate()
        {
            if(shield.owner==null || shield.owner.Dead) { Destroy(gameObject); return; }
            var g=ArenaDirector.Instance; if(g==null || !g.SimulationActive) return;
            for(int i=exiting.Count-1;i>=0;i--)
            {
                var enemy=exiting[i];
                if(enemy==null || enemy.Dead) { exiting.RemoveAt(i); continue; }
                if(Vector2.Distance(enemy.Body.position,shield.owner.Body.position)>offset.magnitude+enemy.radius+.24f)
                {
                    Physics2D.IgnoreCollision(box,enemy.GetComponent<Collider2D>(),false); exiting.RemoveAt(i);
                }
            }
            Vector2 target=shield.owner.Body.position+offset;
            Vector2 size=box.size;
            float angle=shield.Body.rotation;
            bool blocked=Physics2D.OverlapBox(target,size,angle,1<<10)!=null
                || Physics2D.BoxCast(shield.owner.Body.position,size,angle,offset.normalized,offset.magnitude,1<<10).collider!=null;
            box.enabled=!blocked;
            shield.Body.MovePosition(target);
            if(shield.visual!=null) shield.visual.enabled=!blocked;
        }
    }
}
