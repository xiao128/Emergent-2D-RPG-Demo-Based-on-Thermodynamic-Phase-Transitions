using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(PlayerMage))]
    public sealed class ArenaFootsteps : MonoBehaviour
    {
        ThermoBody body; PlayerMage mage; float stepAt;
        void Awake() {body=GetComponent<ThermoBody>(); mage=GetComponent<PlayerMage>();}
        void Update()
        {
            var g=ArenaDirector.Instance; var audio=ArenaAudio.Instance;
            if(g==null || audio==null || !g.SimulationActive || body.Dead || !mage.IsWalking || body.Body.velocity.magnitude<.3f) {stepAt=0; return;}
            if(Time.time<stepAt) return;
            stepAt=Time.time+Mathf.Lerp(.55f,.27f,Mathf.InverseLerp(1,6,body.Body.velocity.magnitude));
            var floor=g.FloorAt(body.Body.position);
            audio.PlayWorld(floor!=null && !floor.rough && floor.phase==FloorPhase.Ice ? audio.iceFootstep : audio.grassFootstep,body.Body.position,.35f);
        }
    }
}
