using UnityEngine;
namespace PhaseArena
{
    // One model shared by launch, perception, positioning and the warning line.
    // Physics still resolves motion, obstacles and damage; this estimates unobstructed reach.
    public static class EnemyAttackPhysics
    {
        public static float LungeImpulse(ThermoBody body,ArenaTuning tuning)
        {
            var brain=body.GetComponent<EnemyBrain>();
            if(brain!=null && brain.lungeImpulseOverride>=0) return brain.lungeImpulseOverride;
            var type=body.kind==BodyKind.Boss ? EnemyArchetype.Boss : body.isElite ? EnemyArchetype.Guard
                : brain!=null ? brain.archetype : EnemyArchetype.Medium;
            return tuning.AttackImpulse(type);
        }
        public static float TravelDistance(ThermoBody body,ArenaDirector world,bool damagingOnly=false)
        {
            float speed=Mathf.Min(body.SpeedLimit,LungeImpulse(body,world.tuning)*world.ForceMultiplier/Mathf.Max(.0001f,body.Mass));
            return TravelDistance(speed,body.GroundDrag,world.tuning,damagingOnly);
        }
        static float TravelDistance(float speed,float groundDrag,ArenaTuning tuning,bool damagingOnly)
        {
            float dt=Mathf.Max(.001f,Time.fixedDeltaTime);
            int steps=Mathf.Max(1,Mathf.CeilToInt(tuning.enemyLungeDuration/dt));
            float drag=Mathf.Max(0,groundDrag);
            if(damagingOnly && speed<=tuning.impactThreshold) return 0;
            if(drag<.0001f) return speed*steps*dt;
            // Box2D damps each step by 1/(1+drag*dt), then integrates position.
            float retained=1/(1+drag*dt);
            if(damagingOnly)
            {
                // Stop the estimate before speed drops below the collision-damage threshold.
                int damagingSteps=Mathf.Max(0,Mathf.FloorToInt(Mathf.Log(Mathf.Max(.0001f,tuning.impactThreshold)/speed)/Mathf.Log(retained)));
                steps=Mathf.Min(steps,damagingSteps);
            }
            return speed/drag*(1-Mathf.Pow(retained,steps));
        }
        public static float SpawnClearance(ArenaDirector world)
        {
            float reach=0;
            foreach(var prefab in new[]{world.lightEnemyPrefab,world.mediumEnemyPrefab,world.heavyEnemyPrefab,world.rangedEnemyPrefab})
            {
                if(prefab==null) continue;
                var brain=prefab.GetComponent<EnemyBrain>();
                if(brain!=null && brain.ranged) reach=Mathf.Max(reach,world.tuning.rangedAttackRange);
                else
                {
                    float speed=Mathf.Min(world.tuning.actorSpeedLimit,LungeImpulse(prefab,world.tuning)*world.ForceMultiplier/Mathf.Max(.0001f,prefab.baseMass));
                    // New worlds start at ambient temperature on dry ground. Grass
                    // has less drag than rough terrain, so use its longer reach.
                    float travel=TravelDistance(speed,ThermoBody.NormalActorDrag,world.tuning,true);
                    reach=Mathf.Max(reach,prefab.radius+world.player.radius+travel);
                }
            }
            return reach+Mathf.Max(0,world.tuning.enemySpawnSafetyMargin);
        }
        public static float AttackReach(ThermoBody body,ThermoBody target,ArenaDirector world,bool ranged)
        {
            if(ranged) return world.tuning.rangedAttackRange;
            return body.radius+target.radius+TravelDistance(body,world,true)*Mathf.Clamp01(world.tuning.attackReachSafety);
        }
    }
}
