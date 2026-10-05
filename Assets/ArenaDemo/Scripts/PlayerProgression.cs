using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(PlayerMage),typeof(ThermoBody))]
    public sealed class PlayerProgression : MonoBehaviour
    {
        public int Level { get; private set; }=1;
        public int Experience { get; private set; }
        ArenaTuning Tuning => ArenaDirector.Instance!=null ? ArenaDirector.Instance.tuning : null;
        public int RequiredExperience => Mathf.Max(1,Tuning!=null ? Tuning.firstLevelExperience : 100)+(Level-1)*Mathf.Max(0,Tuning!=null ? Tuning.experienceIncreasePerLevel : 50);
        ThermoBody body;
        PlayerMage mage;
        float startingMana;
        float fractionalExperience;
        void Awake() { body=GetComponent<ThermoBody>(); mage=GetComponent<PlayerMage>(); startingMana=mage.maxMana; }
        public void ResetRun(float startingHealth)
        {
            Level=1; Experience=0; fractionalExperience=0; body.maxHealth=startingHealth; mage.maxMana=startingMana;
        }
        public void AwardKill(int experience)
        {
            if(body.Dead) return;
            var tuning=Tuning;
            float earned=Mathf.Max(0,experience)*Mathf.Max(0,tuning!=null ? tuning.experienceGainMultiplier : 1)+fractionalExperience;
            int whole=Mathf.FloorToInt(earned+.00001f);
            fractionalExperience=Mathf.Max(0,earned-whole); Experience+=whole;
            while(Experience>=RequiredExperience)
            {
                Experience-=RequiredExperience; Level++;
                float healthGain=Mathf.Max(0,tuning!=null ? tuning.healthGainPerLevel : 10);
                float manaGain=Mathf.Max(0,tuning!=null ? tuning.manaGainPerLevel : 10);
                body.maxHealth+=healthGain; body.health=Mathf.Min(body.maxHealth,body.health+healthGain);
                mage.maxMana+=manaGain; mage.AddMana(manaGain);
                var world=ArenaDirector.Instance;
                if(world!=null) world.Feedback(body.Body.position,"升级！生命 +"+healthGain.ToString("0.#")+" · 法力 +"+manaGain.ToString("0.#"),new Color(.65f,1,.45f));
            }
        }
    }
}
