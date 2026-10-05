using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(ThermoBody))]
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [Min(0)] public float invulnerabilityDuration=.1f;
        public const float EdgeDuration=.7f;
        public float LastHitTime { get; private set; }=-100;
        float lowHealthHitAt=-100;
        ThermoBody body;
        PlayerDeathGuard deathGuard;
        void Awake() { body=GetComponent<ThermoBody>(); deathGuard=GetComponent<PlayerDeathGuard>(); }
        public bool Invulnerable => Time.time-LastHitTime<Mathf.Max(0,invulnerabilityDuration) || (deathGuard!=null && deathGuard.Invulnerable);
        public bool Flashing => Invulnerable;
        public float EdgeOpacity => body!=null && !body.Dead && body.health<body.maxHealth*.2f
            ? .75f*Mathf.Clamp01(1-(Time.time-lowHealthHitAt)/EdgeDuration) : 0;
        public void NotifyDamage()
        {
            LastHitTime=Time.time;
            if(body.health<body.maxHealth*.2f) lowHealthHitAt=Time.time;
        }
        public void ResetFeedback() { LastHitTime=lowHealthHitAt=-100; }
        public Color FlashColor(Color tint)
        {
            if(!Flashing) return tint;
            bool bright=Mathf.FloorToInt((Time.time-LastHitTime)/.05f)%2==0;
            if(bright) return Color.Lerp(tint,Color.white,.85f);
            tint.a*=.18f; return tint;
        }
    }
}
