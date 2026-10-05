using System.Collections.Generic;
using UnityEngine;

namespace PhaseArena
{
    public enum BodyKind { Player, Enemy, Boss, Rock, Tree, Projectile, Shield, StaticObstacle }
    public enum WorldLaw { ThermalMass, ColdBounce, SuperSlide, VaporRecoil, ThermalArc, ImpactHeat, Abrasion, Fission, Gravity,
        Leidenfrost, ThermalShock, DoubleForce, ThermalExpansion, Crowding, StoneMagic, ExpandedRadiation, ThermalInjury, FrictionHeat,
        NuclearFusion, VaporGlide, RecoilPropellant, ViscousWelding, BernoulliWake }
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(Rigidbody2D),typeof(Collider2D))]
    public class ThermoBody : MonoBehaviour
    {
        public BodyKind kind;
        public float baseMass=1,temperature=20,maxHealth=50,health=50;
        public float massRemaining=1,radius=.4f,driveForce=20,topSpeed=5;
        public SpriteRenderer visual,healthFill,heatRing;
        public Vector2 healthBarSize=new Vector2(.8f,.045f);
        public ThermoBody owner,lastInteractor;
        public bool indestructible,isElite;
        public float lifetime,shotHeat;
        public bool IsCharging { get; set; }
        public Color baseColor=Color.white;
        public Rigidbody2D Body { get; private set; }
        public Vector2 LastSafePosition { get; set; }
        public float LastDamageTime { get; private set; }=-100;
        public DamageKind LastDamageKind { get; private set; }
        public bool Dead { get; private set; }
        public bool IsActor => kind==BodyKind.Player || kind==BodyKind.Enemy || kind==BodyKind.Boss;
        public bool IsEnemy => kind==BodyKind.Enemy || kind==BodyKind.Boss;
        public float Mass => Body!=null ? Body.mass : baseMass;
        public float HeatCapacity => Mathf.Max(ArenaDirector.Instance.tuning.minimumHeatCapacity,Mass*ArenaDirector.Instance.tuning.specificHeatCapacity);
        public Vector2 IncomingVelocity { get; private set; }
        readonly VaporGlideStatus glide=new VaporGlideStatus();
        public bool VaporGliding => glide.Active;
        float age,wetCooldown;
        float fissionAt=-1;
        bool steamSlowed;
        Collider2D bodyCollider;
        ThermalShape shape;
        readonly ThermalInjuryStatus thermalInjury=new ThermalInjuryStatus();
        readonly CrowdingStatus crowding=new CrowdingStatus();
        public int CrowdingNeighborCount => crowding.Count;
        public float NextCrowdingScanAt => crowding.NextScanAt;
        internal void TickCrowding(float dt) { crowding.Tick(this,ArenaDirector.Instance,dt); }
        bool shockArmed;
        float frozenAt,frozenTemperature;
        public Collider2D Collider => bodyCollider;
        public float ShapeScale => shape!=null ? shape.SizeFactor : 1;
        public float Area
        {
            get
            {
                var col=bodyCollider!=null ? bodyCollider : GetComponent<Collider2D>(); var size=col.bounds.size;
                if(col is CircleCollider2D) return Mathf.Max(.001f,Mathf.PI*size.x*size.y*.25f);
                if(col is CapsuleCollider2D)
                {
                    float small=Mathf.Min(size.x,size.y),large=Mathf.Max(size.x,size.y);
                    return Mathf.Max(.001f,small*(large-small)+Mathf.PI*small*small*.25f);
                }
                return Mathf.Max(.001f,size.x*size.y);
            }
        }
        public float Density => Mass/Area;
        public float GroundDrag
        {
            get
            {
                var world=ArenaDirector.Instance;
                return glide.Active ? 0 : world!=null ? GroundDragAt(world.FloorAt(Body.position),world) : Body.drag;
            }
        }
        float GroundDragAt(TerrainCell floor,ArenaDirector world)
        {
            var t=world.tuning;
            bool ice=floor!=null && !floor.rough && floor.phase==FloorPhase.Ice;
            float drag=ice ? t.iceDrag : floor!=null && floor.rough ? (IsActor ? 6 : t.roughRockDrag) : IsActor ? 4.5f : t.normalRockDrag;
            if(world.Has(WorldLaw.SuperSlide) && temperature<-50) drag*=.05f;
            return drag;
        }
        // Kept only for the historical benchmark; current crowding counts nearby entities around actors.
        public bool IsCrowdingItem => kind==BodyKind.Rock || kind==BodyKind.Projectile || kind==BodyKind.Tree;
        TrailRenderer trail;
        readonly HashSet<TerrainCell> visitedTiles=new HashSet<TerrainCell>();
        void Awake()
        {
            Body=GetComponent<Rigidbody2D>(); bodyCollider=GetComponent<Collider2D>();
            health=maxHealth; LastSafePosition=transform.position; Body.mass=Mathf.Max(.0001f,baseMass);
            if(Body.bodyType==RigidbodyType2D.Dynamic) Body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        }
        void OnEnable() { if(ArenaDirector.Instance!=null) ArenaDirector.Instance.Register(this); }
        void Start()
        {
            if(GetComponent<BodyAppearance>()==null) gameObject.AddComponent<BodyAppearance>();
            if(IsActor && GetComponent<CrowdingIndicator>()==null) gameObject.AddComponent<CrowdingIndicator>();
            if(healthFill!=null && GetComponent<BodyStatusBars>()==null) gameObject.AddComponent<BodyStatusBars>();
            if(ArenaDirector.Instance!=null) ArenaDirector.Instance.Register(this);
            if((kind==BodyKind.Rock || kind==BodyKind.Projectile) && Body.bodyType==RigidbodyType2D.Dynamic)
            {
                trail=gameObject.AddComponent<TrailRenderer>(); trail.sharedMaterial=visual.sharedMaterial;
                trail.time=.28f; trail.startWidth=.1f; trail.endWidth=0; trail.minVertexDistance=.12f;
                trail.startColor=new Color(.95f,.82f,.5f,.6f); trail.endColor=Color.clear; trail.sortingOrder=480; trail.emitting=false;
            }
        }
        void OnDisable() { crowding.Reset(); if(ArenaDirector.Instance!=null) ArenaDirector.Instance.Unregister(this); }
        public float SpeedLimit
        {
            get
            {
                var t=ArenaDirector.Instance!=null ? ArenaDirector.Instance.tuning : null;
                return IsActor ? (t!=null ? t.actorSpeedLimit : 60) : (t!=null ? t.rockSpeedLimit : 240);
            }
        }
        public void ResetAt(Vector2 position)
        {
            massRemaining=1;
            glide.Reset(); IncomingVelocity=Vector2.zero;
            if(shape!=null) shape.Restore();
            shockArmed=false;
            thermalInjury.Reset();
            crowding.Reset();
            Dead=false; health=maxHealth; temperature=20; massRemaining=1; age=0; wetCooldown=0; LastDamageTime=-100; fissionAt=-1;
            var guard=GetComponent<PlayerDeathGuard>(); if(guard!=null) guard.ResetProtection();
            var hitFeedback=GetComponent<PlayerHitFeedback>(); if(hitFeedback!=null) hitFeedback.ResetFeedback();
            bodyCollider.enabled=true; Body.position=position; Body.velocity=Vector2.zero; Body.angularVelocity=0; Body.mass=baseMass; LastSafePosition=position;
            visitedTiles.Clear(); ThermalStep(0);
        }
        void FixedUpdate()
        {
            var world=ArenaDirector.Instance;
            if(world==null || world.tuning==null || Dead || IsCharging || !world.SimulationActive) return;
            var t=world.tuning; age+=Time.fixedDeltaTime;
            if(kind!=BodyKind.Projectile && lifetime>0 && age>lifetime) { Destroy(gameObject); return; }
            if(Body.bodyType!=RigidbodyType2D.Dynamic) return;
            var floor=world.FloorAt(Body.position);
            if(kind==BodyKind.Projectile && floor!=null && !floor.rough && visitedTiles.Add(floor))
            {
                var old=floor.phase; floor.AddHeat(shotHeat);
                if(old!=FloorPhase.Ice && floor.phase==FloorPhase.Ice) world.metrics.frozenTiles++;
            }
            bool ice=floor!=null && !floor.rough && floor.phase==FloorPhase.Ice;
            glide.Tick(this,world,Time.fixedDeltaTime);
            UpdateSurfaceMaterial(world);
            float friction=GroundDragAt(floor,world);
            if(glide.Active) friction=0;
            Body.drag=friction;
            Body.velocity=Vector2.ClampMagnitude(Body.velocity,SpeedLimit);
            if(floor==null || floor.rough) LastSafePosition=Body.position;
            bool steam=floor!=null && !floor.rough && floor.phase==FloorPhase.Steam;
            if(steam && !steamSlowed && kind==BodyKind.Projectile) Body.velocity*=.5f;
            steamSlowed=steam;
            ApplyGroundFrictionLaws(friction,ice,Time.fixedDeltaTime);
            if(Dead) return;
            float kinetic=.5f*Mass*Body.velocity.sqrMagnitude;
            if(world.Has(WorldLaw.Fission) && QualifiesForFission(Body.velocity.magnitude))
            {
                if(fissionAt<0) fissionAt=Time.time+t.fissionDelay;
                else if(Time.time>=fissionAt) { world.Fission(this,kinetic); Die("轻小高速爆炸",DamageKind.Fission); }
            }
            else fissionAt=-1;
            if(trail!=null) { trail.enabled=Body.velocity.sqrMagnitude>16; trail.emitting=trail.enabled; trail.startColor=temperature>80 ? new Color(1,.47f,.2f,.65f) : temperature<-40 ? new Color(.4f,.84f,1,.65f) : new Color(.95f,.82f,.5f,.6f); }
            RecordIncomingMotion();
        }
        public void RecordIncomingMotion() { IncomingVelocity=Body.velocity; }
        // Shared by every dynamic body: actors, rocks and both sides' projectiles.
        // Heating and erosion are independent global laws.
        public void ApplyGroundFrictionLaws(float friction,bool ice,float dt)
        {
            var world=ArenaDirector.Instance;
            if(world==null || Dead || IsCharging || !world.SimulationActive || Body.bodyType!=RigidbodyType2D.Dynamic || dt<=0) return;
            var t=world.tuning;
            if(world.Has(WorldLaw.FrictionHeat) && !ice) AddHeat(Body.velocity.sqrMagnitude*Mathf.Max(0,friction)*t.frictionHeatGain*dt);
            if(!world.Has(WorldLaw.Abrasion) || Body.velocity.magnitude<=t.abrasionSpeedThreshold || temperature<=t.abrasionHotTemperature) return;
            float loss=t.abrasionMassPerMeter*Body.velocity.magnitude*dt;
            float energy=.5f*Mass*Body.velocity.sqrMagnitude;
            if(!indestructible && Mass-loss<baseMass*t.minimumMassFraction && world.Has(WorldLaw.Fission) && energy>=t.fissionMinimumEnergy) world.Fission(this,energy);
            ConsumeMass(loss,"磨损耗尽");
        }
        public float ConsumeMass(float amount,string cause)
        {
            if(Dead || indestructible || amount<=0) return 0;
            float actual=Mathf.Min(amount,Mass);
            massRemaining*=Mathf.Max(0,Mass-actual)/Mathf.Max(.0001f,Mass);
            ThermalStep(0);
            if(massRemaining<ArenaDirector.Instance.tuning.minimumMassFraction) Die(cause,DamageKind.Terrain);
            return actual;
        }
        public void ThermalStep(float dt)
        {
            // Enemy Awake may request its tuning before our late execution-order
            // Awake. Cache dependencies without relying on component callback order.
            if(Body==null) Body=GetComponent<Rigidbody2D>();
            if(bodyCollider==null) bodyCollider=GetComponent<Collider2D>();
            var world=ArenaDirector.Instance;
            if(Dead || IsCharging || world==null || world.tuning==null) return;
            var t=world.tuning;
            temperature=Mathf.Clamp(temperature,t.minimumTemperature,t.maximumTemperature);
            temperature=Mathf.MoveTowards(temperature,t.ambientTemperature,(t.ambientRecovery+Body.velocity.magnitude*t.windRecovery)*dt/HeatCapacity);
            TrackThermalShock(); if(Dead) return;
            if(kind==BodyKind.Tree && temperature>80) AddHeat(12*dt);
            thermalInjury.Tick(this,world,dt); if(Dead) return;
            float factor=world.Has(WorldLaw.ThermalMass) ? Mathf.Clamp(1-(temperature-20)*t.massTemperatureSlope,t.hotMassFactor,t.coldMassFactor) : 1;
            float next=Mathf.Clamp(baseMass*massRemaining*factor,.0001f,80);
            if(Mathf.Abs(next-Body.mass)>.000001f)
            {
                // Temperature changes inertia, but does not change current velocity.
                Body.mass=next;
            }
            UpdateSurfaceMaterial(world);
            if(kind!=BodyKind.StaticObstacle && (world.Has(WorldLaw.ThermalExpansion) || massRemaining<1 || shape!=null))
            {
                if(shape==null) shape=gameObject.AddComponent<ThermalShape>();
                shape.RefreshWear();
                if(world.Has(WorldLaw.ThermalExpansion)) shape.Step(dt); else shape.Restore();
            }
        }
        void UpdateSurfaceMaterial(ArenaDirector world)
        {
            bool bounce=world.Has(WorldLaw.ColdBounce) && temperature<-40;
            var material=glide.Active ? world.ContactLaws.GlideMaterial(bounce) : bounce ? world.bouncyMaterial : world.normalMaterial;
            if(bodyCollider.sharedMaterial!=material) bodyCollider.sharedMaterial=material;
        }
        public bool QualifiesForFission(float speed)
        {
            var t=ArenaDirector.Instance!=null ? ArenaDirector.Instance.tuning : null;
            return t!=null && Body.bodyType==RigidbodyType2D.Dynamic && !indestructible && !Dead && age>.2f
                && Mass<t.fissionMass && speed>t.fissionSpeed && .5f*Mass*speed*speed>=t.fissionMinimumEnergy;
        }
        // Input is energy, not a direct temperature delta. Direction points from
        // the heating surface/source into this body; exhaust points the other way.
        public void AddHeat(float amount,Vector2 heatingDirection=default(Vector2))
        {
            if(Dead) return;
            var world=ArenaDirector.Instance;
            if(world==null) {temperature=Mathf.Clamp(temperature+amount/Mathf.Max(.05f,Mass),-1000,1000); return;}
            ExchangeHeat(amount,heatingDirection,HeatCapacity,world);
        }
        // The world tick supplies its cached capacity. Ordinary inputs still read
        // current mass; shedding propellant invalidates this tick's capacity cache.
        internal void ExchangeHeat(float amount,Vector2 heatingDirection,float capacity,ArenaDirector world)
        {
            if(Dead) return;
            var t=world.tuning;
            bool shock=world.Has(WorldLaw.ThermalShock);
            if(shock) TrackThermalShock();
            float before=temperature;
            if(amount>0 && world.Has(WorldLaw.RecoilPropellant)) {amount=ThermalRecoil.Receive(this,world,amount,heatingDirection); capacity=HeatCapacity;}
            if(Dead) return;
            temperature=Mathf.Clamp(temperature+amount/capacity,t.minimumTemperature,t.maximumTemperature);
            if(world.Has(WorldLaw.VaporRecoil) && before>=t.steamHotThreshold && before-temperature>=t.steamCoolingThreshold && Time.time>=wetCooldown)
            {
                wetCooldown=Time.time+t.steamWaveCooldown;
                world.SteamWave(this,before-temperature);
            }
            if(shock) TrackThermalShock();
        }
        public void ClearThermalStatus()
        {
            var world=ArenaDirector.Instance;
            temperature=world!=null ? world.tuning.ambientTemperature : 20;
            shockArmed=false; thermalInjury.Reset();
            // A cleanse sets the state directly: it is not a cooling collision/steam reaction.
            ThermalStep(0);
        }
        public void ApplyForce(Vector2 force,ForceMode2D mode=ForceMode2D.Force)
        {
            if(Dead || Body.bodyType!=RigidbodyType2D.Dynamic) return;
            var world=ArenaDirector.Instance;
            Body.AddForce(force*(world!=null ? world.ForceMultiplier : 1),mode);
            if(mode==ForceMode2D.Impulse) Body.velocity=Vector2.ClampMagnitude(Body.velocity,SpeedLimit);
        }
        void TrackThermalShock()
        {
            var world=ArenaDirector.Instance;
            if(world==null || world.tuning==null || !world.Has(WorldLaw.ThermalShock) || Dead) { shockArmed=false; return; }
            var t=world.tuning;
            if(temperature<=t.shockFrozenTemperature)
            {
                shockArmed=true; frozenAt=Time.time; frozenTemperature=temperature; return;
            }
            if(!shockArmed) return;
            if(Time.time-frozenAt>t.shockWindow) { shockArmed=false; return; }
            if(temperature<t.shockHotTemperature) return;
            shockArmed=false;
            float damage=Mathf.Min(t.impactDamageCap,(temperature-frozenTemperature)*Mass*t.shockDamagePerDegreeMass);
            world.metrics.thermalShocks++;
            world.Feedback(Body.position,"极寒 → 骤热 → 脆性伤害",new Color(.7f,.9f,1));
            Damage(damage,"极寒脆性",DamageKind.ThermalShock);
        }
        public void Damage(float amount,string cause,DamageKind kindOfDamage=DamageKind.Impact,bool continuous=false)
        {
            if(Dead || indestructible || amount<=0) return;
            var hitFeedback=kind==BodyKind.Player ? GetComponent<PlayerHitFeedback>() : null;
            if(hitFeedback!=null && hitFeedback.Invulnerable) return;
            if(kind==BodyKind.Player)
            {
                var guard=GetComponent<PlayerDeathGuard>();
                if(guard!=null) amount=guard.LimitDamage(this,amount);
                if(amount<=0) return;
            }
            float actual=Mathf.Min(amount,Mathf.Max(health,0));
            if(ArenaDirector.Instance!=null) ArenaDirector.Instance.RecordDamage(this,actual,kindOfDamage);
            LastDamageKind=kindOfDamage;
            if(!continuous || kind==BodyKind.Player) LastDamageTime=Time.time;
            health-=amount;
            if(hitFeedback!=null && actual>0) hitFeedback.NotifyDamage();
            if(kind==BodyKind.Player && actual>0 && ArenaAudio.Instance!=null) ArenaAudio.Instance.PlayWorld(ArenaAudio.Instance.playerHit,Body.position,.7f);
            if(health<=0) Die(cause,kindOfDamage);
        }
        public void Die(string cause,DamageKind kindOfDamage=DamageKind.Terrain)
        {
            if(Dead) return;
            if(health>0 && ArenaDirector.Instance!=null) ArenaDirector.Instance.RecordDamage(this,health,kindOfDamage);
            health=0; Dead=true;
            if(ArenaDirector.Instance!=null) ArenaDirector.Instance.OnBodyDied(this,cause);
            if(kind!=BodyKind.Player) Destroy(gameObject);
            else { Body.velocity=Vector2.zero; bodyCollider.enabled=false; }
        }
        public void LayoutTreeIndicators()
        {
            if(kind!=BodyKind.Tree || visual==null) return;
            var collider=GetComponent<Collider2D>();
            if(collider==null) return;
            var bounds=collider.bounds;
            float width=Mathf.Max(.5f,bounds.size.x);
            float sx=Mathf.Max(.01f,Mathf.Abs(transform.lossyScale.x));
            float sy=Mathf.Max(.01f,Mathf.Abs(transform.lossyScale.y));
            healthBarSize=new Vector2(width/sx,.06f/sy);
            float top=visual.bounds.max.y;
            if(visual.sprite!=null)
            {
                // Tight sprite geometry excludes transparent margins in the texture.
                top=float.MinValue;
                foreach(var vertex in visual.sprite.vertices) top=Mathf.Max(top,visual.transform.TransformPoint(vertex).y);
                if(top==float.MinValue) top=visual.bounds.max.y;
            }
            Vector3 center=new Vector3(bounds.center.x,top+.18f,transform.position.z);
            if(healthFill!=null)
            {
                healthFill.transform.parent.position=center-Vector3.right*width*.5f;
                float ratio=Mathf.Clamp01(health/maxHealth);
                healthFill.transform.localScale=new Vector3(healthBarSize.x*ratio,healthBarSize.y,1);
                healthFill.transform.localPosition=new Vector3(healthBarSize.x*.5f*ratio,0,0);
            }
            var back=transform.Find("Health Back");
            if(back!=null) { back.position=center; back.localScale=new Vector3(width/sx,.085f/sy,1); }
            if(heatRing!=null && heatRing.sprite!=null)
            {
                float diameter=Mathf.Max(bounds.size.x,bounds.size.y)*1.15f;
                heatRing.transform.position=bounds.center;
                var size=heatRing.sprite.bounds.size;
                heatRing.transform.localScale=new Vector3(diameter/(sx*Mathf.Max(.01f,size.x)),diameter/(sy*Mathf.Max(.01f,size.y)),1);
            }
        }
        void OnCollisionEnter2D(Collision2D c)
        {
            var world=ArenaDirector.Instance; if(world==null || Dead) return;
            var other=c.collider.GetComponentInParent<ThermoBody>();
            if(other!=null)
            {
                var contact=c.contactCount>0 ? c.GetContact(0) : default(ContactPoint2D);
                float speed=c.contactCount>0 ? Mathf.Abs(Vector2.Dot(c.relativeVelocity,contact.normal)) : c.relativeVelocity.magnitude;
                if(kind==BodyKind.Shield || other.kind==BodyKind.Shield)
                {
                    // Following a shield is locomotion, not an extra damaging attack.
                    var shield=kind==BodyKind.Shield ? this : other;
                    var mover=shield==this ? other : this;
                    // Use the contact's incoming relative speed, rather than
                    // the rock's already-stopped velocity after the solver.
                    // Only the shield callback charges its health once.
                    if(kind==BodyKind.Shield) shield.Damage(ArenaDirector.ImpactDamage(mover.Mass,speed),"护盾抵挡",DamageKind.Impact);
                    return;
                }
                world.ContactLaws.Touch(this,other,c.contactCount>0 ? contact.point : Body.position);
                world.ResolveCollision(this,other,speed,c.contactCount>0 ? contact.point : Body.position,c.contactCount>0 ? contact.normalImpulse : 0);
            }
            else
            {
                var contact=c.contactCount>0 ? c.GetContact(0) : default(ContactPoint2D);
                float speed=c.contactCount>0 ? Mathf.Abs(Vector2.Dot(c.relativeVelocity,contact.normal)) : c.relativeVelocity.magnitude;
                var terrain=c.collider.GetComponentInParent<TerrainCell>();
                if(terrain!=null) {glide.Touch(this,world,terrain,contact.normal); world.Collisions.ResolveTerrainCollision(this,terrain,speed,c.contactCount>0 ? contact.point : Body.position,contact.normal);}
                else world.DeliverProjectileHeat(this,c.contactCount>0 ? contact.point : Body.position);
            }
        }
        void OnCollisionStay2D(Collision2D c)
        {
            var world=ArenaDirector.Instance; if(world==null || !world.SimulationActive || Dead) return;
            var other=c.collider.GetComponentInParent<ThermoBody>();
            if(other!=null) world.ContactLaws.Touch(this,other,c.contactCount>0 ? c.GetContact(0).point : Body.position);
            else { var tile=c.collider.GetComponentInParent<TerrainCell>(); if(tile!=null) glide.Touch(this,world,tile,c.contactCount>0 ? c.GetContact(0).normal : Vector2.zero); }
        }
    }
}
