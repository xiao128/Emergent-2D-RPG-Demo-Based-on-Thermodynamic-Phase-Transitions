#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    public static class ArenaTuningVerification
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()
        {
            var g=ArenaDirector.Instance; g.Restart(); g.enabled=false; Time.timeScale=0;
            var t=g.tuning; var p=g.player; var mage=p.GetComponent<PlayerMage>(); mage.enabled=false;
            foreach(var b in g.bodies.ToArray()) if(b!=null && b!=p) b.gameObject.SetActive(false);
            p.ResetAt(new Vector2(-32,-24)); p.transform.position=p.Body.position;
            var xp=p.GetComponent<PlayerProgression>();
            float gain=t.experienceGainMultiplier, healthGain=t.healthGainPerLevel, manaGain=t.manaGainPerLevel, speed=t.enemyAttackSpeedMultiplier;
            float heavyImpulse=t.heavyAttackImpulse, oldImpulse=t.enemyLungeImpulse, npcImpulse=t.enemyProjectileImpulse, bossImpulse=t.bossProjectileImpulse;
            int first=t.firstLevelExperience, increase=t.experienceIncreasePerLevel;
            float baseHealth=p.maxHealth, baseMana=mage.maxMana;
            var results=new List<string>(); int failures=0; var fixtures=new List<ThermoBody>();
            System.Action<bool,string> check=(ok,text)=>{results.Add((ok ? "PASS " : "FAIL ")+text); if(!ok) failures++;};
            var begin=typeof(EnemyBrain).GetMethod("BeginAttack",Hidden); var until=typeof(EnemyBrain).GetField("chargeUntil",Hidden);
            try
            {
                t.experienceGainMultiplier=2;
                var victim=g.Spawn(g.lightEnemyPrefab,new Vector2(-20,-20)); victim.GetComponent<EnemyBrain>().enabled=false;
                victim.Die("tuning environmental kill",DamageKind.Terrain);
                check(xp.Experience==20,"Actual light-monster death grants double its ten base XP");
                t.experienceGainMultiplier=.05f;
                xp.AwardKill(10); check(xp.Experience==20,"Half-point XP is retained instead of rounded up per kill");
                xp.AwardKill(10); check(xp.Experience==21,"Two half-point awards accumulate into one real XP");
                t.experienceGainMultiplier=0; xp.AwardKill(100); check(xp.Experience==21,"Zero XP multiplier disables XP gain");
                t.experienceGainMultiplier=1; t.firstLevelExperience=25; t.experienceIncreasePerLevel=25;
                t.healthGainPerLevel=7; t.manaGainPerLevel=3; xp.ResetRun(baseHealth); p.health=baseHealth; mage.ResetTools();
                xp.AwardKill(80);
                check(xp.Level==3 && xp.Experience==5 && xp.RequiredExperience==75,"Custom thresholds 25 then 50 award two levels and carry five XP");
                check(p.maxHealth==baseHealth+14 && p.health==baseHealth+14 && mage.maxMana==baseMana+6 && mage.Mana==baseMana+6,"Custom per-level health and mana gains update maxima and current pools");
                t.experienceGainMultiplier=.05f; xp.ResetRun(baseHealth); xp.AwardKill(10); xp.ResetRun(baseHealth); xp.AwardKill(10);
                check(xp.Experience==0,"Run reset also clears fractional XP carry");
                t.firstLevelExperience=0; t.experienceIncreasePerLevel=-10;
                check(xp.RequiredExperience>=1,"Invalid level requirement is clamped away from infinite leveling");
                var enemy=g.Spawn(g.heavyEnemyPrefab,new Vector2(-20,-20)); fixtures.Add(enemy);
                var ai=enemy.GetComponent<EnemyBrain>(); ai.enabled=false; enemy.enabled=false; enemy.SendMessage("FixedUpdate");
                check(ai.archetype==EnemyArchetype.Heavy,"Heavy prefab retains its explicit archetype");
                t.heavyAttackImpulse=120; float fullReach=ai.AttackReach;
                t.heavyAttackImpulse=12; t.enemyLungeImpulse=999;
                check(ai.LungeImpulse==12 && ai.AttackReach<fullReach,"Lower global heavy impulse directly reduces impulse and reach despite old hidden floors");
                begin.Invoke(ai,new object[]{Vector2.right,g}); until.SetValue(ai,Time.time-.01f); ai.SendMessage("FixedUpdate");
                check(Mathf.Abs(enemy.Body.velocity.x-3)<.001f,"Actual heavy launch uses twelve impulse / four mass = three speed");
                enemy.Body.velocity=Vector2.zero; ai.SetHome(enemy.Body.position,false); ai.lungeImpulseOverride=40;
                begin.Invoke(ai,new object[]{Vector2.right,g}); until.SetValue(ai,Time.time-.01f); ai.SendMessage("FixedUpdate");
                check(ai.LungeImpulse==40 && Mathf.Abs(enemy.Body.velocity.x-10)<.001f,"Single enemy override changes actual launch independently of global setting");
                ai.lungeImpulseOverride=-1; t.heavyAttackImpulse=0;
                check(ai.LungeImpulse==0,"Global zero impulse is honored rather than forced up by a speed floor");
                t.heavyAttackImpulse=120; enemy.baseMass=8; enemy.ThermalStep(0);
                check(ai.archetype==EnemyArchetype.Heavy && ai.LungeImpulse==120,"Changing mass keeps enemy type and configured impulse unchanged");
                enemy.Body.velocity=Vector2.zero; enemy.temperature=20;
                t.enemyAttackSpeedMultiplier=1.75f; begin.Invoke(ai,new object[]{Vector2.right,g}); float normal=(float)until.GetValue(ai)-Time.time;
                t.enemyAttackSpeedMultiplier=3.5f; begin.Invoke(ai,new object[]{Vector2.right,g}); float faster=(float)until.GetValue(ai)-Time.time;
                check(Mathf.Abs(normal/faster-2)<.001f,"Editable global attack-speed multiplier changes actual windup");
                enemy.gameObject.SetActive(false);
                t.enemyProjectileImpulse=12; t.bossProjectileImpulse=36;
                foreach(var prefab in new[]{g.rangedEnemyPrefab,g.bossPrefab})
                {
                    var caster=g.Spawn(prefab,new Vector2(-20,-20)); fixtures.Add(caster); caster.GetComponent<EnemyBrain>().enabled=false;
                    Physics2D.SyncTransforms(); var shot=g.Shoot(caster,Vector2.right,105); fixtures.Add(shot);
                    float expected=caster.kind==BodyKind.Boss ? 36 : 12;
                    check(shot!=null && Mathf.Abs(shot.Mass*shot.Body.velocity.x-expected)<.001f,prefab.name+" independently configurable projectile impulse reaches actual body");
                    shot.gameObject.SetActive(false); caster.gameObject.SetActive(false);
                }
                var serialized=new UnityEditor.SerializedObject(t);
                foreach(string field in new[]{"experienceGainMultiplier","firstLevelExperience","experienceIncreasePerLevel","healthGainPerLevel","manaGainPerLevel","lightAttackImpulse","mediumAttackImpulse","heavyAttackImpulse","guardAttackImpulse","bossAttackImpulse","bossProjectileImpulse","enemyAttackSpeedMultiplier"})
                    check(typeof(ArenaTuning).GetField(field).IsPublic && serialized.FindProperty(field)!=null,field+" is public and Inspector-serialized");
            }
            catch(System.Exception ex) {check(false,"Exception: "+ex);}
            finally
            {
                t.experienceGainMultiplier=gain; t.healthGainPerLevel=healthGain; t.manaGainPerLevel=manaGain; t.enemyAttackSpeedMultiplier=speed;
                t.firstLevelExperience=first; t.experienceIncreasePerLevel=increase; t.heavyAttackImpulse=heavyImpulse; t.enemyLungeImpulse=oldImpulse;
                t.enemyProjectileImpulse=npcImpulse; t.bossProjectileImpulse=bossImpulse;
                foreach(var body in fixtures) if(body!=null) {body.gameObject.SetActive(false); Object.Destroy(body.gameObject);}
                xp.ResetRun(baseHealth); p.ResetAt(g.SpawnPosition); mage.ResetTools(); g.enabled=true;
            }
            string report="failures="+failures+"\n"+string.Join("\n",results);
            File.WriteAllText("Verification/tuning-regression.txt",report); Debug.Log("[TuningVerification] "+report); return report;
        }
    }
}
#endif
