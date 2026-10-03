using UnityEngine;

namespace PhaseArena
{
    [CreateAssetMenu(menuName = "Phase Arena/World Tuning")]
    public class ArenaTuning : ScriptableObject
    {
        [Header("World")]
        public int width = 72, height = 56, worldCount = 8, roamingCount = 19;
        public int rockCount = 48, treeCount = 22, largeRockCount = 12;
        public float aggroRadius = 6, leashRadius = 10, guardHealth = 60, bossHealth = 120;
        public float rockHealth = 360;
        [Header("Health progression (staff damage stays fixed)")]
        public float playerHealth = 100, playerHealthGrowth = 1.25f, enemyHealthGrowth = 1.55f;
        public float lightHealth = 18, mediumHealth = 28, heavyHealth = 42;
        [Header("Fixed map landmarks")]
        public float edgeInset = 8;
        public int riverCenterY = 10, riverWidth = 4;
        [Header("Player tools")]
        public float staffImpulse = 22.5f, staffDamage = 12, spellMass = 1, spellImpulse = 6;
        public float projectileHealth = 160, projectileChargeDuration = .3f;
        public int maximumProjectiles = 250;
        public float castCooldown = .65f, shieldRadius = 1.15f, shieldLifetime = 4, shieldCooldown = 7;
        public float staffCooldown = .65f;
        public float shieldRepelSpeed = 6.5f, shieldFormationDelay = .28f;
        [Header("Enemy attack rhythm")]
        public float enemyWindup = .85f, enemyAttackCooldown = 3.5f, enemyRecovery = 1.2f;
        public float enemyLungeImpulse = 12, guardLungeImpulse = 20, bossLungeImpulse = 36;
        [Header("Universal simulation")]
        public float impactAlpha = .2f, impactBeta = 2, impactThreshold = 1.8f, impactDamageCap = 2000;
        public float rockSpeedLimit = 240, actorSpeedLimit = 60, spellSpeedLimit = 160;
        public float ambientTemperature = 20, minimumTemperature = -600, maximumTemperature = 800;
        public float ambientRecovery = 1.8f, windRecovery = .18f, overheatDamageLimit = .1f;
        public float normalRockDrag = .6f, roughRockDrag = 1.6f, iceDrag = .02f;
        [Header("World laws")]
        public float hotMassFactor = .12f, coldMassFactor = 8, massTemperatureSlope = .012f;
        public float arcDamagePerDegreeMass = .65f, impactHeatFraction = .45f;
        public float impactTemperatureGain = 8;
        public float abrasionPerMeter = .10f, minimumMassFraction = .1f;
        public float fissionMass = .3f, fissionSpeed = 10, fissionMinimumEnergy = 6, fissionDamagePerEnergy = .6f;
        public float fissionDelay = .1f;
        public float vaporImpulse = 30, gravityRadius = 8, gravityForce = 30;
        public float gravityDensityThreshold=12;
        public float steamHotThreshold=100,steamCoolingThreshold=60,steamWaveRadius=3,steamWaveImpulse=18,steamWaveCooldown=.5f;
        public int crowdingItemThreshold=6;
        public float crowdingDamagePerExcessPerSecond=3;
        [Header("Leidenfrost repulsion")]
        public float repulsionTemperatureGap = 80, repulsionRange = .6f, repulsionPerDegree = .3f;
        [Header("Cold brittleness / thermal shock")]
        public float shockFrozenTemperature = -50, shockHotTemperature = 100, shockWindow = 1;
        public float shockDamagePerDegreeMass = .6f;
        [Header("Thermal expansion and trapped-body crushing")]
        public float sizeTemperatureSlope = .002f, minimumSizeFactor = .6f, maximumSizeFactor = 1.8f;
        public float expansionInterval = .2f, expansionStep = .12f, crushDamagePerSize = 120;
        public float PlayerHealthAt(int world) => Mathf.Round(playerHealth*Mathf.Pow(playerHealthGrowth,Mathf.Max(0,world-1)));
        public float EnemyHealthAt(float baseHealth,int world) => Mathf.Round(baseHealth*Mathf.Pow(enemyHealthGrowth,Mathf.Max(0,world-1)));
    }
}
