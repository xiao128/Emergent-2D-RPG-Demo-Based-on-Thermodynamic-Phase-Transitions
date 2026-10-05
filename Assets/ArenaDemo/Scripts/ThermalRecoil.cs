using UnityEngine;
namespace PhaseArena
{
    public static class ThermalRecoil
    {
        public static float Receive(ThermoBody b,ArenaDirector g,float input,Vector2 direction)
        {
            if(b.indestructible) return input;
            var t=g.tuning;
            float thresholdEnergy=Mathf.Max(0,t.recoilHotTemperature-b.temperature)*b.HeatCapacity;
            float used=Mathf.Max(0,input-thresholdEnergy)*Mathf.Clamp01(t.recoilEnergyFraction);
            if(used<=0) return input;
            float oldCapacity=b.HeatCapacity;
            float lost=b.ConsumeMass(used/Mathf.Max(.001f,t.recoilEnergyPerMass),"反冲工质耗尽");
            if(lost>0 && !b.Dead)
            {
                if(direction.sqrMagnitude>.0001f)
                {
                    direction.Normalize();
                    float speed=Mathf.Min(t.recoilExhaustSpeed,Mathf.Sqrt(2*used/Mathf.Max(.0001f,lost)));
                    b.ApplyForce(direction*lost*speed,ForceMode2D.Impulse);
                }
                var gas=b.GetComponent<RecoilGasParticles>(); if(gas==null) gas=b.gameObject.AddComponent<RecoilGasParticles>();
                gas.Emit(-direction,lost,t.recoilParticleLifetime);
                g.metrics.recoilEvents++;
            }
            // Preserve the allocated thermal temperature increase even though the
            // retained body's capacity changed while shedding its propellant.
            return (input-used)*b.HeatCapacity/oldCapacity;
        }
    }
    public sealed class RecoilGasParticles : MonoBehaviour
    {
        const int Capacity=24;
        struct Particle { public SpriteRenderer view; public Vector2 velocity; public float remaining,total; }
        readonly Particle[] particles=new Particle[Capacity];
        int next;
        public void Emit(Vector2 direction,float mass,float lifetime)
        {
            var b=GetComponent<ThermoBody>(); if(b.visual==null || lifetime<=0) return;
            int count=Mathf.Clamp(Mathf.CeilToInt(mass*30),1,6);
            for(int n=0;n<count;n++)
            {
                int i=next++%Capacity; var p=particles[i];
                if(p.view==null) {p.view=new GameObject("Propellant gas (visual only)").AddComponent<SpriteRenderer>(); p.view.transform.SetParent(transform,false); p.view.sprite=ArenaDirector.Instance.worldGenerator.square; p.view.sharedMaterial=b.visual.sharedMaterial;}
                p.view.enabled=true; p.view.sortingOrder=b.visual.sortingOrder+1;
                p.view.transform.position=(Vector2)b.Collider.bounds.center+direction*b.radius;
                p.view.transform.localScale=Vector3.one*.07f;
                p.velocity=direction*3+Random.insideUnitCircle*.5f; p.remaining=p.total=lifetime; particles[i]=p;
            }
        }
        void Update()
        {
            for(int i=0;i<Capacity;i++)
            {
                var p=particles[i]; if(p.view==null || p.remaining<=0) continue;
                p.remaining-=Time.deltaTime; p.view.transform.position+=(Vector3)p.velocity*Time.deltaTime;
                p.view.color=new Color(.8f,.9f,1,Mathf.Clamp01(p.remaining/p.total)*.8f); p.view.enabled=p.remaining>0; particles[i]=p;
            }
        }
    }
}
