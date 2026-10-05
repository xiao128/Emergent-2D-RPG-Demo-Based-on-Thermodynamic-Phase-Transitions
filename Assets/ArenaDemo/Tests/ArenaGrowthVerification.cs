#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaGrowthVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        int failures;
        void Check(bool ok,string name) { results.Add((ok ? "PASS " : "FAIL ")+name); if(!ok) failures++; }
        public void Run() { StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            var g=ArenaDirector.Instance; g.Restart();
            var p=g.player; var mage=p.GetComponent<PlayerMage>(); mage.enabled=false;
            foreach(var ai in FindObjectsOfType<EnemyBrain>()) ai.enabled=false;
            var xp=p.GetComponent<PlayerProgression>(); var guard=p.GetComponent<PlayerDeathGuard>();
            float hp=p.maxHealth,mp=mage.maxMana;
            Check(xp.Level==1 && xp.Experience==0 && xp.RequiredExperience==100,"Run begins at level 1 with 100 XP requirement");
            for(int i=0;i<10;i++)
            {
                var enemy=g.Spawn(g.lightEnemyPrefab,new Vector2(-20+i,-20));
                enemy.Die("environment fixture",DamageKind.Terrain);
                enemy.Die("duplicate callback",DamageKind.Terrain);
            }
            Check(xp.Level==2 && xp.Experience==0 && xp.RequiredExperience==150,"Ten small environmental kills level up once; duplicate death gives no XP");
            Check(p.maxHealth==hp+10 && mage.maxMana==mp+10,"Level up adds exactly 10 max health and mana");
            Check(g.lightEnemyPrefab.GetComponent<EnemyBrain>().experienceReward==10 && g.mediumEnemyPrefab.GetComponent<EnemyBrain>().experienceReward==20 && g.heavyEnemyPrefab.GetComponent<EnemyBrain>().experienceReward==30,"Small medium large monsters grant 10 20 30 XP");
            for(int i=0;i<8;i++) xp.AwardKill(20);
            Check(xp.Level==3 && xp.Experience==10 && xp.RequiredExperience==200,"Requirement rises 50 per level and surplus XP carries over");
            typeof(ArenaDirector).GetMethod("BuildWorld",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,null);
            Check(xp.Level==3 && xp.Experience==10 && p.maxHealth==hp+20 && mage.maxMana==mp+20,"World rebuild preserves level XP and maximum stats");
            Check(g.tuning.PlayerHealthAt(8)==g.tuning.PlayerHealthAt(1),"World progression no longer scales player health");
            foreach(var ai in FindObjectsOfType<EnemyBrain>()) ai.enabled=false;
            foreach(var b in g.bodies.ToArray()) if(b!=p && b!=null) b.gameObject.SetActive(false);
            p.ResetAt(new Vector2(-24,-20));
            Check(!WorldLawCatalog.IsAvailable(WorldLaw.DoubleForce) && WorldLawCatalog.IsAvailable(WorldLaw.StoneMagic),"Stone magic replaces double force in random pool");
            g.laws.Add(WorldLaw.StoneMagic);
            var shot=g.Shoot(p,Vector2.right,105);
            Check(shot!=null && shot.shotHeat==105 && shot.temperature==150 && shot.visual.sprite==g.worldGenerator.rockPrefab.visual.sprite,"Player casts hot stone retaining fire temperature and delivered heat");
            Check(shot.kind==BodyKind.Projectile && Mathf.Abs(shot.Mass-g.worldGenerator.rockPrefab.baseMass)<.001f && shot.Body.velocity==Vector2.zero,"Stone uses ordinary rock mass and retains shared FIFO and stationary player release");
            var heatTarget=g.Spawn(g.worldGenerator.rockPrefab,(Vector2)shot.Collider.bounds.center+Vector2.right*.5f);
            heatTarget.temperature=20; Physics2D.SyncTransforms();
            g.DeliverProjectileHeat(shot,shot.Collider.bounds.center);
            float heated=heatTarget.temperature; g.DeliverProjectileHeat(shot,shot.Collider.bounds.center);
            Check(heated>20 && heatTarget.temperature==heated && shot.shotHeat==0,"Hot stone still delivers its one-time heating pulse");
            heatTarget.gameObject.SetActive(false); Destroy(heatTarget.gameObject);
            shot.gameObject.SetActive(false); Destroy(shot.gameObject);
            var coldShot=g.Shoot(p,Vector2.right,-95);
            Check(coldShot!=null && coldShot.temperature==-120 && coldShot.shotHeat==-95 && coldShot.visual.sprite==g.worldGenerator.rockPrefab.visual.sprite,"Player casts cold stone retaining ice temperature and delivered cooling");
            coldShot.gameObject.SetActive(false); Destroy(coldShot.gameObject);
            var enemyCaster=g.Spawn(g.rangedEnemyPrefab,new Vector2(-20,-20)); enemyCaster.GetComponent<EnemyBrain>().enabled=false;
            var enemyShot=g.Shoot(enemyCaster,Vector2.right,-95);
            Check(enemyShot!=null && enemyShot.shotHeat==-95 && enemyShot.temperature==-120 && enemyShot.visual.sprite==g.worldGenerator.rockPrefab.visual.sprite && enemyShot.Body.velocity.x>0,"Ranged small enemy throws cold stone retaining cooling and launch impulse");
            enemyShot.gameObject.SetActive(false); enemyCaster.gameObject.SetActive(false);
            Destroy(enemyShot.gameObject); Destroy(enemyCaster.gameObject);
            mage.ResetTools(); mage.Cast(true,Vector2.right);
            var preview=typeof(PlayerMage).GetField("chargePreview",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(mage) as SpriteRenderer;
            float small=preview.transform.localScale.x;
            typeof(PlayerMage).GetMethod("AdvanceCharge",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(mage,new object[]{g.tuning.projectileChargeDuration});
            Check(preview.sprite==g.worldGenerator.rockPrefab.visual.sprite && preview.transform.localScale.x>small*5,"Stone charge preview gradually enlarges the rock sprite");
            mage.ResetTools(); g.laws.Clear();
            p.ResetAt(new Vector2(-24,-20)); p.Damage(p.maxHealth+1,"burst",DamageKind.Impact);
            Check(!p.Dead && p.health==1 && guard.Invulnerable,"Lethal burst locks player at one HP for one second");
            p.Damage(999,"continuous while invulnerable",DamageKind.Overheat,true);
            Check(p.health==1 && !p.Dead,"One-second immunity also blocks continuous damage");
            yield return new WaitForSeconds(1.05f);
            Check(!guard.Invulnerable,"Immunity expires after one second");
            p.ResetAt(new Vector2(-24,-20)); p.health=p.maxHealth*.2f;
            p.Damage(p.health,"exact threshold",DamageKind.Impact);
            Check(p.Dead && g.State==RunState.Defeat,"Lethal damage of exactly 20 percent does not trigger greater-than-20 protection");
            g.Restart(); mage=g.player.GetComponent<PlayerMage>(); mage.enabled=false;
            foreach(var ai in FindObjectsOfType<EnemyBrain>()) ai.enabled=false;
            Check(xp.Level==1 && xp.Experience==0 && p.maxHealth==hp && mage.maxMana==mp,"Restart restores base stats and clears XP");
            p.ResetAt(new Vector2(-24,-20));
            p.Damage(p.maxHealth*.19f,"same-frame first hit",DamageKind.Overheat,true);
            p.Damage(p.health,"same-frame lethal hit",DamageKind.Overheat,true);
            Check(Mathf.Abs(p.health-p.maxHealth*.81f)<.001f && !p.Dead,"New short invulnerability blocks the second same-frame hit");
            float normal=g.tuning.enemyWindup+g.tuning.enemyAttackCooldown;
            float faster=g.tuning.EnemyAttackTime(g.tuning.enemyWindup)+g.tuning.EnemyAttackTime(g.tuning.enemyAttackCooldown);
            Check(Mathf.Abs(normal/faster-1.75f)<.001f,"Attack timing preserves windup at 175 percent speed");
            var attacker=g.Spawn(g.rangedEnemyPrefab,new Vector2(-18,-20));
            var brain=attacker.GetComponent<EnemyBrain>(); brain.enabled=false;
            const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            float began=Time.time;
            typeof(EnemyBrain).GetMethod("BeginAttack",hidden).Invoke(brain,new object[]{Vector2.right,g});
            float until=(float)typeof(EnemyBrain).GetField("chargeUntil",hidden).GetValue(brain);
            float next=(float)typeof(EnemyBrain).GetField("chargeAt",hidden).GetValue(brain);
            Check(brain.IsWindingUp && Mathf.Abs(until-began-g.tuning.enemyWindup/1.75f)<.001f && Mathf.Abs(next-until-g.tuning.enemyAttackCooldown/1.75f)<.001f,"Actual ranged AI schedules shortened telegraph and cooldown");
            yield return new WaitForSeconds(g.tuning.EnemyAttackTime(g.tuning.enemyWindup)+.05f);
            brain.SendMessage("FixedUpdate");
            Check(brain.Throws==1 && brain.Lunges==1,"Actual ranged AI throws after shortened windup");
            attacker.gameObject.SetActive(false); Destroy(attacker.gameObject);
            g.Restart();
            string report="failures="+failures+"\n"+string.Join("\n",results);
            Directory.CreateDirectory("Verification"); File.WriteAllText("Verification/growth-regression.txt",report);
            Debug.Log("[GrowthVerification] "+report); enabled=false;
        }
    }
}
#endif
