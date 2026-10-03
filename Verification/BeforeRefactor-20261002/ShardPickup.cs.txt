using UnityEngine;
namespace PhaseArena
{
    public class ShardPickup : MonoBehaviour
    {
        public bool Collected { get; set; }
        public SpriteRenderer visual;
        void Update()
        {
            if(visual!=null) { visual.transform.localRotation=Quaternion.Euler(0,0,Time.time*35); visual.transform.localScale=Vector3.one*(.55f+Mathf.Sin(Time.time*4)*.06f); }
            var g=ArenaDirector.Instance;
            if(g!=null && g.SimulationActive && !Collected && Vector2.Distance(transform.position,g.player.Body.position)<1) g.Collect(this);
        }
    }
}
