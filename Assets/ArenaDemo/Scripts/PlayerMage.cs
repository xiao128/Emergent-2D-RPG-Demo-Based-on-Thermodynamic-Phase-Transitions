using UnityEngine;
using UnityEngine.EventSystems;

namespace PhaseArena
{
    [RequireComponent(typeof(ThermoBody))]
    public class PlayerMage : MonoBehaviour
    {
        public Transform staff;
        public SpriteRenderer facing;
        [Header("Mana")]
        public float maxMana=200, spellManaCost=20, manaRecoveryPerSecond=5;
        [SerializeField] float mana=200;
        public float Mana => mana;
        public float FireReady { get; private set; }
        public float IceReady { get; private set; }
        public float ShieldReady { get; private set; }
        public float MeleeReady { get; private set; }
        public ThermoBody LastMeleeTarget { get; private set; }
        ThermoBody entity;
        BodySpriteAnimator spriteAnimator;
        Vector2 move, aim = Vector2.right;
        bool slowWalk;
        SpriteRenderer chargePreview;
        bool charging;
        bool chargeFire,waitForRelease;
        float chargeElapsed;
        public bool IsCharging => charging;
        public float ChargeProgress => IsCharging ? Mathf.Clamp01(chargeElapsed/Mathf.Max(.01f,ArenaDirector.Instance.tuning.projectileChargeDuration)) : 0;
#if UNITY_EDITOR
        public Vector2? VerificationMove;
        public bool? VerificationSlowWalk;
        public bool? VerificationHoldCharge;
#endif
        void Awake() { entity = GetComponent<ThermoBody>(); spriteAnimator=GetComponent<BodySpriteAnimator>(); mana=maxMana; }
        void Update()
        {
            var game = ArenaDirector.Instance;
            if (game == null || !game.SimulationActive || entity.Dead) { move = Vector2.zero; if(entity.Dead) CancelCharge(); return; }
            mana=Mathf.Min(maxMana,mana+manaRecoveryPerSecond*Time.deltaTime);
            move = new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")).normalized;
            slowWalk=Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#if UNITY_EDITOR
            if (VerificationMove.HasValue) move=Vector2.ClampMagnitude(VerificationMove.Value,1);
            if (VerificationSlowWalk.HasValue) slowWalk=VerificationSlowWalk.Value;
#endif
            if (Camera.main != null) aim = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
            if (aim.sqrMagnitude < .01f) aim = Vector2.right;
            if (staff != null) staff.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg-90);
            if(spriteAnimator!=null && spriteAnimator.HasDirections) spriteAnimator.SetFacing(move);
            else if (facing != null) facing.flipX = aim.x < 0;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if(!Input.GetMouseButton(0) && !Input.GetMouseButton(1)) waitForRelease=false;
            if(IsCharging)
            {
                bool held=Input.GetMouseButton(chargeFire ? 0 : 1);
#if UNITY_EDITOR
                if(VerificationHoldCharge.HasValue) held=VerificationHoldCharge.Value;
#endif
                AdvanceCharge(Time.deltaTime);
                if(!held) { if(ChargeProgress<1 || !ReleaseCharge(aim)) CancelCharge(); }
            }
            else if(!overUI && !waitForRelease)
            {
                if(Input.GetMouseButton(0)) Cast(true,aim);
                else if(Input.GetMouseButton(1)) Cast(false,aim);
            }
            if (Input.GetKeyDown(KeyCode.Space)) Shield();
            if (Input.GetKeyDown(KeyCode.F)) Melee(aim);
        }
        void FixedUpdate()
        {
            if (ArenaDirector.Instance == null || !ArenaDirector.Instance.SimulationActive || entity.Dead) return;
            float agility=entity.temperature<-50 ? .5f : entity.temperature<-10 ? .75f : 1;
            float walkingSpeed=entity.topSpeed*agility*(slowWalk ? .5f : 1);
            float along=Vector2.Dot(entity.Body.velocity,move.normalized);
            if(along<walkingSpeed && move.sqrMagnitude>.0001f)
            {
                float requested=move.magnitude*entity.driveForce*agility*(slowWalk ? .5f : 1);
                float remaining=(walkingSpeed-along)*entity.Mass/Time.fixedDeltaTime;
                entity.ApplyForce(move.normalized*Mathf.Min(requested,remaining));
            }
        }
        public bool Cast(bool fire, Vector2 direction)
        {
            if (entity == null || entity.Dead || !ArenaDirector.Instance.SimulationActive) return false;
            if (Time.time < (fire ? FireReady : IceReady)) return false;
            if(direction.sqrMagnitude<.01f || mana<spellManaCost) return false;
            if(IsCharging) return false;
            aim=direction.normalized; chargeFire=fire; chargeElapsed=0; charging=true;
            // Preview only: no rigidbody, collider, temperature or projectile slot.
            chargePreview=new GameObject("Spell charge preview").AddComponent<SpriteRenderer>();
            chargePreview.sprite=fire ? ArenaDirector.Instance.fireSprite : ArenaDirector.Instance.iceSprite;
            chargePreview.sharedMaterial=ArenaDirector.Instance.projectilePrefab.visual.sharedMaterial;
            chargePreview.color=fire ? new Color(1,.75f,.35f,.8f) : new Color(.65f,.94f,1,.8f);
            UpdateChargePreview();
            return true;
        }
        void AdvanceCharge(float dt)
        {
            chargeElapsed+=dt;
            UpdateChargePreview();
        }
        void UpdateChargePreview()
        {
            if(chargePreview==null) return;
            var g=ArenaDirector.Instance;
            chargePreview.transform.position=g.ProjectileSpawnPosition(entity,aim);
            chargePreview.sortingOrder=entity.visual.sortingOrder+1;
            float diameter=g.projectilePrefab.radius*2*Mathf.Lerp(.15f,1,ChargeProgress);
            var size=chargePreview.sprite.bounds.size;
            chargePreview.transform.localScale=new Vector3(diameter/Mathf.Max(.01f,size.x),diameter/Mathf.Max(.01f,size.y),1);
        }
        public bool ReleaseCharge(Vector2 direction)
        {
            if(!IsCharging || ChargeProgress<1 || mana<spellManaCost) return false;
            var shot=ArenaDirector.Instance.Shoot(entity,direction,chargeFire ? 105 : -95);
            if(shot==null)
            {
                ArenaDirector.Instance.Feedback(entity.Body.position,"前方空间不足，无法成球",new Color(1,.8f,.4f));
                return false;
            }
            mana-=spellManaCost;
            FireReady=IceReady=Time.time+ArenaDirector.Instance.tuning.castCooldown;
            CancelCharge(); waitForRelease=true;
            return true;
        }
        void CancelCharge()
        {
            if(chargePreview!=null) { chargePreview.gameObject.SetActive(false); Destroy(chargePreview.gameObject); }
            chargePreview=null; charging=false; chargeElapsed=0;
        }
        void OnDestroy() { CancelCharge(); }
        public void ResetTools()
        {
            FireReady=IceReady=ShieldReady=MeleeReady=0;
            CancelCharge(); waitForRelease=false;
            mana=maxMana;
            move=Vector2.zero; slowWalk=false;
#if UNITY_EDITOR
            VerificationMove=null;
            VerificationSlowWalk=null;
            VerificationHoldCharge=null;
#endif
        }
        public bool Shield()
        {
            if (Time.time < ShieldReady || entity.Dead || !ArenaDirector.Instance.SimulationActive) return false;
            ShieldReady = Time.time + ArenaDirector.Instance.tuning.shieldCooldown;
            ArenaDirector.Instance.CreateShields(entity);
            return true;
        }
        public bool Melee(Vector2 direction)
        {
            if (Time.time < MeleeReady || entity.Dead || !ArenaDirector.Instance.SimulationActive) return false;
            if(IsCharging && ChargeProgress>=1) ReleaseCharge(direction);
            MeleeReady = Time.time + ArenaDirector.Instance.tuning.staffCooldown;
            LastMeleeTarget=ArenaDirector.Instance.Melee(entity,direction);
            return true;
        }
    }
}
