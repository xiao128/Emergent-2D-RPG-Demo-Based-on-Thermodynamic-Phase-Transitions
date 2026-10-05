using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(ThermoBody))]
    public sealed class BodyAppearance : MonoBehaviour
    {
        ThermoBody body;
        PlayerHitFeedback playerFeedback;
        Vector3 baseRingScale;
        bool ringCaptured;
        void Awake() { body=GetComponent<ThermoBody>(); }
        void Update()
        {
            if(body.visual!=null)
            {
                Color tint=body.temperature<-10 ? Color.Lerp(body.baseColor,new Color(.3f,.85f,1),Mathf.InverseLerp(-10,-100,body.temperature))
                    : Color.Lerp(body.baseColor,new Color(1,.32f,.13f),Mathf.InverseLerp(40,150,body.temperature));
                if(body.kind==BodyKind.Player && playerFeedback==null) playerFeedback=GetComponent<PlayerHitFeedback>();
                body.visual.color=!body.IsActor ? tint
                    : playerFeedback!=null ? playerFeedback.FlashColor(tint)
                    : Time.time-body.LastDamageTime<.15f ? Color.white : tint;
                if(body.kind!=BodyKind.StaticObstacle) body.visual.sortingOrder=500-Mathf.RoundToInt(transform.position.y*3);
            }
        }
        public void RefreshRing()
        {
            if(body.heatRing!=null)
            {
                var world=ArenaDirector.Instance;
                float multiplier=world!=null ? world.LawSimulation.RadiationMultiplier : 1;
                if(body.kind==BodyKind.Tree && body.heatRing.sprite!=null)
                {
                    // Tree layout follows its actual (possibly expanded) collider.
                    // Do not capture an already expanded ring and scale it a second time.
                    var bounds=body.Collider.bounds;
                    float diameter=Mathf.Max(bounds.size.x,bounds.size.y)*1.15f*multiplier;
                    var size=body.heatRing.sprite.bounds.size; var scale=transform.lossyScale;
                    body.heatRing.transform.position=bounds.center;
                    body.heatRing.transform.localScale=new Vector3(diameter/Mathf.Max(.01f,Mathf.Abs(scale.x)*size.x),diameter/Mathf.Max(.01f,Mathf.Abs(scale.y)*size.y),1);
                }
                else
                {
                    if(!ringCaptured) { baseRingScale=body.heatRing.transform.localScale; ringCaptured=true; }
                    body.heatRing.transform.localScale=baseRingScale*body.ShapeScale*multiplier;
                }
                body.heatRing.color=body.temperature<-10 ? new Color(.28f,.85f,1,.5f) : new Color(1,.35f,.12f,.5f);
                body.heatRing.enabled=body.temperature<-10 || body.temperature>40;
            }
        }
        void LateUpdate() { RefreshRing(); }
    }
}
