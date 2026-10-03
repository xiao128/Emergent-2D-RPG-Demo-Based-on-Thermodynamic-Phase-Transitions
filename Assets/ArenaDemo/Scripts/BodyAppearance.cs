using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(ThermoBody))]
    public sealed class BodyAppearance : MonoBehaviour
    {
        ThermoBody body;
        void Awake() { body=GetComponent<ThermoBody>(); }
        void Update()
        {
            if(body.visual!=null)
            {
                Color tint=body.temperature<-10 ? Color.Lerp(body.baseColor,new Color(.3f,.85f,1),Mathf.InverseLerp(-10,-100,body.temperature))
                    : Color.Lerp(body.baseColor,new Color(1,.32f,.13f),Mathf.InverseLerp(40,150,body.temperature));
                body.visual.color=Time.time-body.LastDamageTime<.15f ? Color.white : tint;
                if(body.kind!=BodyKind.StaticObstacle) body.visual.sortingOrder=500-Mathf.RoundToInt(transform.position.y*3);
            }
            if(body.heatRing!=null)
            {
                body.heatRing.color=body.temperature<-10 ? new Color(.28f,.85f,1,.5f) : new Color(1,.35f,.12f,.5f);
                body.heatRing.enabled=body.temperature<-10 || body.temperature>40;
            }
        }
    }
}
