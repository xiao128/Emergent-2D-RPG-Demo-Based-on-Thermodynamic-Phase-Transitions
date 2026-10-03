using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(ThermoBody))]
    public class EnemyBrain : MonoBehaviour
    {
        public Vector2 Home { get; private set; }
        public bool Alert { get; private set; }
        public bool IsWindingUp => chargeUntil>Time.time;
        public Vector2 AttackDirection => chargeDirection;
        public int Windups { get; private set; }
        public int Lunges { get; private set; }
        public Vector3 BaseVisualScale => visualScale;
        public bool guarding;
        public bool ranged;
        public int Throws { get; private set; }
        ThermoBody self;
        float wanderAt,shotAt,chargeAt,chargeUntil,chargeStarted,recoveryUntil;
        Vector2 wanderTarget,chargeDirection;
        Vector3 visualScale;
        System.Random wanderRandom;
        LineRenderer warning;
        void Awake()
        {
            self=GetComponent<ThermoBody>();
            if(self.visual!=null) visualScale=self.visual.transform.localScale;
            SetHome(transform.position,false);
        }
        public void SetHome(Vector2 home,bool guard)
        {
            Home=home; guarding=guard; Alert=false; wanderTarget=home;
            wanderRandom=new System.Random(GetInstanceID()); wanderAt=Time.time+1;
            shotAt=Time.time+4; chargeAt=Time.time+1; chargeUntil=0; recoveryUntil=0;
        }
        public void InterruptAttack(float recovery)
        {
            chargeUntil=0; recoveryUntil=Mathf.Max(recoveryUntil,Time.time+recovery);
            chargeAt=Mathf.Max(chargeAt,recoveryUntil+.4f);
            if(warning!=null) warning.enabled=false;
        }
        void BeginAttack(Vector2 direction,ArenaDirector game)
        {
            chargeDirection=direction.normalized; chargeStarted=Time.time;
            chargeUntil=Time.time+(self.kind==BodyKind.Boss ? 1.1f : game.tuning.enemyWindup);
            chargeAt=chargeUntil+game.tuning.enemyAttackCooldown;
            Windups++;
            if(warning==null)
            {
                warning=gameObject.AddComponent<LineRenderer>(); warning.sharedMaterial=self.visual.sharedMaterial;
                warning.startWidth=.09f; warning.endWidth=.25f; warning.positionCount=2;
                warning.sortingOrder=750; warning.useWorldSpace=true;
            }
            warning.enabled=true;
            warning.startColor=new Color(1,.65f,.2f,.85f); warning.endColor=new Color(1,.25f,.15f,.15f);
            game.Pulse(self.Body.position,new Color(1,.55f,.18f,.75f),self.radius+1,chargeUntil-Time.time);
        }
        void FixedUpdate()
        {
            var g=ArenaDirector.Instance;
            if(g==null || !g.CombatActive || self.Dead || g.player==null || g.player.Dead) return;
            if(Time.time<recoveryUntil) return; // No AI force cancels a physical knockback.
            if(chargeUntil>0)
            {
                if(self.Body.velocity.magnitude>Mathf.Max(3.5f,self.topSpeed*1.7f))
                {
                    InterruptAttack(g.tuning.enemyRecovery); return;
                }
                warning.SetPosition(0,self.Body.position);
                warning.SetPosition(1,self.Body.position+chargeDirection*(self.kind==BodyKind.Boss ? 6 : 4));
                if(Time.time<chargeUntil)
                {
                    self.ApplyForce(-self.Body.velocity*self.Mass*6);
                    return;
                }
                chargeUntil=0; warning.enabled=false; Lunges++;
                if(ranged)
                {
                    g.Shoot(self,chargeDirection,Throws%2==0 ? 105 : -95);
                    Throws++; recoveryUntil=Time.time+g.tuning.enemyRecovery; return;
                }
                float impulse=self.kind==BodyKind.Boss ? g.tuning.bossLungeImpulse
                    : self.isElite ? g.tuning.guardLungeImpulse : g.tuning.enemyLungeImpulse;
                self.ApplyForce(chargeDirection*impulse,ForceMode2D.Impulse);
                recoveryUntil=Time.time+g.tuning.enemyRecovery;
                return;
            }
            Vector2 toPlayer=g.player.Body.position-self.Body.position;
            float distance=toPlayer.magnitude,homeDistance=Vector2.Distance(self.Body.position,Home);
            float leash=guarding ? 8 : g.tuning.leashRadius;
            if(distance<g.tuning.aggroRadius && homeDistance<leash) Alert=true;
            if(distance>g.tuning.aggroRadius+3 || homeDistance>leash) Alert=false;
            Vector2 destination=Home;
            if(Alert)
            {
                float impulse=self.kind==BodyKind.Boss ? g.tuning.bossLungeImpulse
                    : self.isElite ? g.tuning.guardLungeImpulse : g.tuning.enemyLungeImpulse;
                float attackRange=Mathf.Clamp(self.radius+g.player.radius+
                    impulse*g.ForceMultiplier/(self.Mass*Mathf.Max(.1f,self.Body.drag))*.45f,
                    self.radius+g.player.radius+.35f,4.2f);
                if(ranged) attackRange=5;
                if(self.kind==BodyKind.Boss && Time.time>shotAt)
                {
                    shotAt=Time.time+5;
                    for(int i=0;i<8;i++) g.Shoot(self,new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4)),i%2==0 ? 105 : -95);
                    g.Pulse(self.Body.position,new Color(1,.4f,.2f),2);
                }
                if(Time.time>chargeAt && distance<(ranged ? g.tuning.aggroRadius : attackRange+.2f) && distance>self.radius+g.player.radius+.3f)
                {
                    BeginAttack(toPlayer,g); return;
                }
                // Approach the attack distance; during cooldown leave breathing room.
                destination=distance>attackRange ? g.player.Body.position
                    : self.Body.position-toPlayer.normalized*(attackRange-distance);
            }
            else if(homeDistance<leash*.8f)
            {
                if(Time.time>wanderAt)
                {
                    wanderAt=Time.time+2+(float)wanderRandom.NextDouble()*3;
                    float a=(float)wanderRandom.NextDouble()*Mathf.PI*2;
                    Vector2 desired=Home+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(guarding ? 1 : 3);
                    wanderTarget=g.worldGenerator.SafeDryPosition(desired,self.radius+.25f);
                }
                destination=wanderTarget;
            }
            Vector2 delta=destination-self.Body.position;
            if(delta.magnitude<.65f || self.Body.velocity.magnitude>self.topSpeed*1.7f) return;
            Vector2 dir=delta.normalized;
            var hit=Physics2D.CircleCast(self.Body.position,self.radius+.08f,dir,1.3f,(1<<10)|(1<<12));
            if(hit.collider!=null)
            {
                Vector2 tangent=dir-Vector2.Dot(dir,hit.normal)*hit.normal;
                if(tangent.sqrMagnitude<.03f) tangent=new Vector2(-hit.normal.y,hit.normal.x)*(GetInstanceID()%2==0 ? 1 : -1);
                dir=tangent.normalized;
                if(Physics2D.CircleCast(self.Body.position,self.radius+.08f,dir,.8f,(1<<10)|(1<<12)).collider!=null) return;
            }
            float agility=self.temperature<-50 ? .5f : self.temperature<-10 ? .75f : 1;
            if(Vector2.Dot(self.Body.velocity,dir)<self.topSpeed*agility) self.ApplyForce(dir*self.driveForce*agility);
            if(self.visual!=null && Mathf.Abs(dir.x)>.1f) self.visual.flipX=dir.x<0;
        }
        void LateUpdate()
        {
            if(self==null || self.visual==null) return;
            bool winding=IsWindingUp && !self.Dead && ArenaDirector.Instance!=null && ArenaDirector.Instance.SimulationActive;
            float amount=winding ? Mathf.Clamp01((Time.time-chargeStarted)/Mathf.Max(.1f,chargeUntil-chargeStarted)) : 0;
            self.visual.transform.localScale=Vector3.Scale(visualScale*self.ShapeScale,new Vector3(1+.2f*amount,1-.25f*amount,1));
            if(self.Dead && warning!=null) warning.enabled=false;
        }
    }
}
