using System.Collections.Generic;
using UnityEngine;

namespace PhaseArena
{
    // Occupancy changes are checked at a fixed interval. The root transform
    // never scales, so rigidbody motion and sprite animation remain independent.
    [RequireComponent(typeof(ThermoBody))]
    public class ThermalShape : MonoBehaviour
    {
        public float SizeFactor { get; private set; }=1;
        float thermalFactor=1;
        float WearFactor => Mathf.Sqrt(Mathf.Clamp(body.massRemaining,.0001f,1));
        ThermoBody body;
        CircleCollider2D circle;
        BoxCollider2D box;
        CapsuleCollider2D capsule;
        float baseRadius,baseCircleRadius,timer,cueAt;
        Vector2 baseBoxSize,baseHealthSize;
        Vector2 baseCapsuleSize,baseCapsuleOffset;
        Vector3 visualScale;
        Transform staff;
        Vector3 staffScale,staffPosition;
        bool captured;
        void Awake() { body=GetComponent<ThermoBody>(); circle=GetComponent<CircleCollider2D>(); box=GetComponent<BoxCollider2D>(); capsule=GetComponent<CapsuleCollider2D>(); }
        void Capture()
        {
            if(captured) return;
            captured=true; baseRadius=body.radius; baseHealthSize=body.healthBarSize;
            if(circle!=null) baseCircleRadius=circle.radius;
            if(box!=null) baseBoxSize=box.size;
            if(capsule!=null) { baseCapsuleSize=capsule.size; baseCapsuleOffset=capsule.offset; }
            var brain=body.GetComponent<EnemyBrain>();
            if(body.visual!=null) visualScale=brain!=null ? brain.BaseVisualScale : body.visual.transform.localScale;
            var mage=body.GetComponent<PlayerMage>();
            if(mage!=null && mage.staff!=null) {staff=mage.staff; staffScale=staff.localScale; staffPosition=staff.localPosition;}
        }
        public void Restore()
        {
            timer=0;
            thermalFactor=1;
            if(captured) RefreshWear();
        }
        public void RefreshWear()
        {
            Capture();
            float factor=thermalFactor*WearFactor;
            if(!Mathf.Approximately(SizeFactor,factor)) SetSize(factor);
        }
        public void Step(float dt)
        {
            var world=ArenaDirector.Instance;
            if(world==null || body.Dead || body.kind==BodyKind.StaticObstacle || (circle==null && box==null && capsule==null)) return;
            Capture(); timer+=dt;
            if(timer<world.tuning.expansionInterval) return;
            float elapsed=timer; timer=0;
            var t=world.tuning;
            float desired=Mathf.Clamp(1+(body.temperature-t.ambientTemperature)*t.sizeTemperatureSlope,t.minimumSizeFactor,t.maximumSizeFactor);
            float proposed=Mathf.MoveTowards(thermalFactor,desired,t.expansionStep);
            if(proposed<=thermalFactor) { thermalFactor=proposed; RefreshWear(); return; }
            float proposedSize=proposed*WearFactor;
            var occupied=new List<Collider2D>();
            Collider2D[] overlaps;
            if(circle!=null) overlaps=Physics2D.OverlapCircleAll(body.Body.position,baseCircleRadius*proposedSize+.01f);
            else if(capsule!=null)
                overlaps=Physics2D.OverlapCapsuleAll(body.Body.position+(Vector2)transform.TransformVector(baseCapsuleOffset*proposedSize),
                    Vector2.Scale(baseCapsuleSize,(Vector2)transform.lossyScale)*proposedSize,capsule.direction,body.Body.rotation);
            else overlaps=Physics2D.OverlapBoxAll(body.Body.position,baseBoxSize*proposedSize,body.Body.rotation);
            foreach(var hit in overlaps)
            {
                if(hit==body.Collider || !hit.enabled || hit.isTrigger || hit.attachedRigidbody==body.Body
                    || Physics2D.GetIgnoreLayerCollision(gameObject.layer,hit.gameObject.layer)
                    || Physics2D.GetIgnoreCollision(body.Collider,hit)) continue;
                occupied.Add(hit);
            }
            if(occupied.Count==0) { thermalFactor=proposed; RefreshWear(); return; }
            // Mere overlap does not hurt: a creature must have an immovable
            // obstacle immediately behind it, between that obstacle and us.
            foreach(var hit in occupied)
            {
                var creature=hit.GetComponentInParent<ThermoBody>();
                if(creature==null || !creature.IsActor || creature.Dead || creature==body) continue;
                Vector2 direction=creature.Body.position-body.Body.position;
                if(direction.sqrMagnitude<.0001f) continue;
                float reach=baseRadius*(proposedSize-SizeFactor)+.2f;
                bool trapped=false;
                foreach(var barrier in Physics2D.CircleCastAll(creature.Body.position,creature.radius*.95f,direction.normalized,reach,1<<10))
                {
                    if(barrier.collider==null || barrier.collider==body.Collider || barrier.collider==creature.Collider) continue;
                    if(barrier.rigidbody!=null && barrier.rigidbody.bodyType!=RigidbodyType2D.Static) continue;
                    trapped=true; break;
                }
                if(!trapped) continue;
                creature.Damage((desired-thermalFactor)*WearFactor*t.crushDamagePerSize*elapsed,"热膨胀挤压",DamageKind.Crush,true);
                world.metrics.crushTicks++;
                if(Time.time>cueAt)
                {
                    cueAt=Time.time+.7f;
                    world.Feedback(creature.Body.position,"膨胀受阻 → 挤压",new Color(1,.64f,.3f));
                }
            }
        }
        void SetSize(float factor)
        {
            SizeFactor=factor; body.radius=baseRadius*factor;
            if(circle!=null) circle.radius=baseCircleRadius*factor;
            if(box!=null) box.size=baseBoxSize*factor;
            if(capsule!=null) { capsule.size=baseCapsuleSize*factor; capsule.offset=baseCapsuleOffset*factor; }
            if(body.visual!=null) body.visual.transform.localScale=visualScale*factor;
            if(staff!=null) {staff.localScale=staffScale*factor; staff.localPosition=staffPosition*factor;}
            body.healthBarSize=new Vector2(baseHealthSize.x*factor,baseHealthSize.y);
            if(body.healthFill!=null) body.healthFill.transform.parent.localPosition=new Vector2(-body.radius,body.radius+.25f);
            var back=body.transform.Find("Health Back");
            if(back!=null) { back.localPosition=new Vector2(0,body.radius+.25f); back.localScale=new Vector3(body.radius*2,.065f,1); }
            if(body.kind==BodyKind.Tree) body.LayoutTreeIndicators();
        }
    }
}
