using UnityEngine;
namespace PhaseArena
{
    public enum EnemyArchetype
    {
        [InspectorName("小怪")] Light,
        [InspectorName("中怪")] Medium,
        [InspectorName("大怪")] Heavy,
        [InspectorName("守卫")] Guard,
        [InspectorName("远程怪")] Ranged,
        [InspectorName("Boss")] Boss
    }
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
        [Header("怪物类型与单独覆盖")]
        [Tooltip("选择对应的全局冲量配置；修改质量不会改变怪物的类型。")]
        public EnemyArchetype archetype=EnemyArchetype.Medium;
        [Min(-1), Tooltip("-1 使用 WorldTuning 的对应类型冲量；非负值直接覆盖此怪物的冲量。")]
        public float lungeImpulseOverride=-1;
        [Min(0), Tooltip("此怪物击杀的基础经验，再乘全局 Experience Gain Multiplier。")]
        public int experienceReward=20;
        public int Throws { get; private set; }
        ThermoBody self;
        float wanderAt,chargeAt,chargeUntil,chargeStarted,recoveryUntil,lungeUntil;
        Vector2 wanderTarget,chargeDirection;
        Vector3 visualScale;
        System.Random wanderRandom;
        LineRenderer warning;
        public float LungeImpulse => EnemyAttackPhysics.LungeImpulse(self,ArenaDirector.Instance.tuning);
        public float AttackReach => EnemyAttackPhysics.AttackReach(self,ArenaDirector.Instance.player,ArenaDirector.Instance,ranged);
        public bool IsLunging => !self.Dead && Time.time<lungeUntil;
        void ApplyBossTuning(ArenaDirector g)
        {
            if(g==null || self.kind!=BodyKind.Boss) return;
            self.topSpeed=Mathf.Max(0,g.tuning.bossMoveSpeed); self.driveForce=Mathf.Max(0,g.tuning.bossDriveForce);
            float mass=Mathf.Max(.0001f,g.tuning.bossBaseMass);
            if(!Mathf.Approximately(self.baseMass,mass)) {self.baseMass=mass; self.ThermalStep(0);}
        }
        float RecoveryTime(ArenaDirector g) => AttackTime(self.kind==BodyKind.Boss ? g.tuning.bossRecovery : g.tuning.enemyRecovery);
        public float TemperatureAttackMultiplier
        {
            get
            {
                var g=ArenaDirector.Instance;
                return g!=null && self!=null && self.temperature>g.tuning.hotEnemyTemperature
                    ? Mathf.Max(1,g.tuning.hotEnemyAttackMultiplier) : 1;
            }
        }
        float AttackTime(float duration)
        {
            var g=ArenaDirector.Instance;
            return (g!=null ? g.tuning.EnemyAttackTime(duration) : duration/ArenaTuning.EnemyAttackSpeedMultiplier)/TemperatureAttackMultiplier;
        }
        void Awake()
        {
            self=GetComponent<ThermoBody>();
            if(self.visual!=null) visualScale=self.visual.transform.localScale;
            if(self.kind==BodyKind.Boss && GetComponent<BossCombat>()==null) gameObject.AddComponent<BossCombat>();
            SetHome(transform.position,false);
        }
        public void SetHome(Vector2 home,bool guard)
        {
            Home=home; guarding=guard; Alert=false; wanderTarget=home;
            wanderRandom=new System.Random(GetInstanceID()); wanderAt=Time.time+1;
            chargeAt=Time.time+AttackTime(1); chargeUntil=0; recoveryUntil=0; lungeUntil=0;
            ApplyBossTuning(ArenaDirector.Instance);
            var boss=GetComponent<BossCombat>(); if(boss!=null) boss.ResetTimers();
        }
        public void InterruptAttack(float recovery)
        {
            chargeUntil=0; recoveryUntil=Mathf.Max(recoveryUntil,Time.time+recovery);
            lungeUntil=0;
            chargeAt=Mathf.Max(chargeAt,recoveryUntil+AttackTime(.4f));
            if(warning!=null) warning.enabled=false;
        }
        void BeginAttack(Vector2 direction,ArenaDirector game)
        {
            chargeDirection=direction.normalized; chargeStarted=Time.time;
            chargeUntil=Time.time+AttackTime(self.kind==BodyKind.Boss ? game.tuning.bossWindup : game.tuning.enemyWindup);
            chargeAt=chargeUntil+AttackTime(self.kind==BodyKind.Boss ? game.tuning.bossAttackCooldown : game.tuning.enemyAttackCooldown);
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
            ApplyBossTuning(g);
            if(Time.time<recoveryUntil) return; // No AI force cancels a physical knockback.
            if(chargeUntil>0)
            {
                if(self.Body.velocity.magnitude>Mathf.Max(3.5f,self.topSpeed*1.7f))
                {
                    InterruptAttack(RecoveryTime(g)); return;
                }
                warning.SetPosition(0,self.Body.position);
                warning.SetPosition(1,self.Body.position+chargeDirection*AttackReach);
                if(Time.time<chargeUntil)
                {
                    self.ApplyForce(-self.Body.velocity*self.Mass*6);
                    return;
                }
                chargeUntil=0; warning.enabled=false; Lunges++;
                if(ranged)
                {
                    g.Shoot(self,chargeDirection,Throws%2==0 ? 105 : -95);
                    Throws++; recoveryUntil=Time.time+RecoveryTime(g); return;
                }
                self.ApplyForce(chargeDirection*LungeImpulse,ForceMode2D.Impulse);
                if(ArenaAudio.Instance!=null) ArenaAudio.Instance.PlayCharge(self,archetype);
                lungeUntil=Time.time+Mathf.Max(.02f,g.tuning.enemyLungeDuration);
                recoveryUntil=Time.time+Mathf.Max(g.tuning.enemyLungeDuration,RecoveryTime(g));
                return;
            }
            Vector2 toPlayer=g.player.Body.position-self.Body.position;
            float distance=toPlayer.magnitude,homeDistance=Vector2.Distance(self.Body.position,Home);
            float leash=guarding ? 8 : g.tuning.leashRadius;
            float attackRange=AttackReach;
            if(self.kind==BodyKind.Boss)
            {
                if(distance<=g.tuning.bossAggroRadius) Alert=true;
                // Once acquired, the boss keeps pursuing across the arena.
            }
            else
            {
                if(distance<=attackRange && homeDistance<leash) Alert=true;
                if(distance>attackRange+g.tuning.enemyAlertHysteresis || homeDistance>leash) Alert=false;
            }
            Vector2 destination=Home;
            if(Alert)
            {
                if(Time.time>chargeAt && distance<=attackRange && distance>self.radius+g.player.radius+.3f)
                {
                    BeginAttack(toPlayer,g); return;
                }
                float standOff=attackRange*Mathf.Clamp(g.tuning.enemyStandOffFraction,.1f,1);
                // Stay inside our own reach instead of retreating to its outer boundary.
                destination=distance>standOff ? g.player.Body.position
                    : self.Body.position-toPlayer.normalized*(standOff-distance);
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
