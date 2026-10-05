using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(ThermoBody))]
    public sealed class CrowdingIndicator : MonoBehaviour
    {
        const int Segments=64;
        readonly Vector3[] points=new Vector3[Segments];
        ThermoBody body;
        float drawnRadius=-1;
        public LineRenderer Ring { get; private set; }
        void Awake() {body=GetComponent<ThermoBody>();}
        void LateUpdate()
        {
            var g=ArenaDirector.Instance;
            // Reuse the staggered scan's cached count; this visual never queries physics.
            bool active=g!=null && body.IsActor && !body.Dead && body.Collider!=null
                && (g.State==RunState.Explore || g.State==RunState.Boss)
                && g.Has(WorldLaw.Crowding) && body.CrowdingNeighborCount>g.tuning.crowdingItemThreshold;
            if(!active) {if(Ring!=null) Ring.enabled=false; return;}
            if(Ring==null)
            {
                var go=new GameObject("Crowding Warning Ring"); go.transform.SetParent(transform,false);
                Ring=go.AddComponent<LineRenderer>(); Ring.sharedMaterial=body.visual.sharedMaterial;
                Ring.useWorldSpace=false; Ring.loop=true; Ring.positionCount=Segments;
                Ring.sortingLayerID=body.visual.sortingLayerID;
            }
            Ring.enabled=true;
            float radius=Mathf.Max(.01f,g.tuning.crowdingRadius);
            if(!Mathf.Approximately(radius,drawnRadius))
            {
                drawnRadius=radius;
                for(int i=0;i<Segments;i++) {float angle=i*Mathf.PI*2/Segments; points[i]=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*radius;}
                Ring.SetPositions(points);
            }
            // Match the world-space detection circle even with expanded/scaled actors.
            Ring.transform.position=body.Collider.bounds.center;
            Ring.transform.rotation=Quaternion.identity;
            var scale=transform.lossyScale;
            Ring.transform.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1);
            Ring.widthMultiplier=Mathf.Max(.01f,g.tuning.crowdingRingWidth);
            Ring.sortingOrder=body.visual.sortingOrder+2;
            var color=g.tuning.crowdingRingColor; color.a*=.7f+.3f*(.5f+.5f*Mathf.Sin(Time.time*5));
            Ring.startColor=Ring.endColor=color;
        }
        void OnDisable() {if(Ring!=null) Ring.enabled=false;}
    }
}
