#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PhaseArena
{
    // Short, isolated runtime fixtures; never saved as a scene component.
    public class ArenaProjectileVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        int failures;
        void Check(bool ok,string message)
        {
            results.Add((ok ? "PASS " : "FAIL ")+message);
            if(!ok) failures++;
        }
        ThermoBody Clone(ThermoBody prefab,Vector2 point)
        {
            var body=Instantiate(prefab,point,Quaternion.identity);
            var ai=body.GetComponent<EnemyBrain>(); if(ai!=null) ai.enabled=false;
            body.temperature=20; body.ThermalStep(0); return body;
        }
        IEnumerator Start()
        {
            var g=ArenaDirector.Instance; g.StartRun();
            yield return null;
            int rangedCount=0;
            foreach(var ai in FindObjectsOfType<EnemyBrain>())
            {
                if(ai.ranged) rangedCount++;
                ai.enabled=false;
            }
            Check(rangedCount==3,"Three ranged roamers replace existing enemies");
            var rock=Clone(g.worldGenerator.rockPrefab,new Vector2(90,10));
            rock.baseMass=1; rock.Body.velocity=new Vector2(4,2);
            g.laws.Add(WorldLaw.ThermalMass);
            rock.temperature=-120; rock.ThermalStep(0); float cold=rock.Mass;
            Check((rock.Body.velocity-new Vector2(4,2)).sqrMagnitude<.0001f && cold>1,"Cooling increases mass without changing velocity");
            rock.temperature=150; rock.ThermalStep(0);
            Check((rock.Body.velocity-new Vector2(4,2)).sqrMagnitude<.0001f && rock.Mass<1,"Heating decreases mass without changing velocity");
            g.laws.Clear(); Destroy(rock.gameObject);

            var caster=Clone(g.worldGenerator.rockPrefab,new Vector2(90,0));
            var other=Clone(g.worldGenerator.rockPrefab,new Vector2(94,0));
            caster.Body.simulated=false; other.Body.simulated=false;
            g.Shoot(caster,Vector2.right,105); g.Shoot(other,Vector2.left,-95);
            var shots=g.bodies.FindAll(b=>b!=null && b.kind==BodyKind.Projectile);
            var a=shots[shots.Count-2]; var b=shots[shots.Count-1];
            Check(!Physics2D.GetIgnoreLayerCollision(a.gameObject.layer,b.gameObject.layer),"Projectiles share a colliding body layer");
            yield return new WaitForSeconds(.6f);
            Check(a!=null && b!=null && !a.Dead && !b.Dead && a.health<160 && b.health<160,"Actual airborne collision damages both projectiles without hit deletion");
            if(a!=null) Destroy(a.gameObject); if(b!=null) Destroy(b.gameObject);
            other.Body.position=new Vector2(94,4);
            g.Shoot(caster,Vector2.right,105);
            var shot=g.bodies.FindLast(x=>x!=null && x.kind==BodyKind.Projectile && x.health==160);
            shot.Body.position=caster.Body.position+Vector2.right; shot.Body.velocity=Vector2.right*3;
            Physics2D.SyncTransforms(); var hit=g.Melee(caster,Vector2.right);
            Check(hit==shot && shot.Body.velocity.x>3,"F selects and accelerates a projectile with an actual impulse");
            var target=Clone(g.mediumEnemyPrefab,new Vector2(94,0));
            target.maxHealth=500; target.health=500;
            yield return new WaitForSeconds(.25f);
            // Subsequent continuous overheat can replace LastDamageKind;
            // its capped 0.1 HP/s cannot account for this much health loss.
            Check(target.health<499 && g.metrics.largestImpact>1,"Accelerated projectile hits an enemy through shared physical damage, without the old spell cap; HP="+target.health);
            if(shot!=null) Destroy(shot.gameObject); Destroy(target.gameObject);
            var ranged=Clone(g.rangedEnemyPrefab,new Vector2(90,4));
            var rangedAI=ranged.GetComponent<EnemyBrain>(); rangedAI.enabled=true;
            g.player.ResetAt(new Vector2(95,4));
            g.player.GetComponent<PlayerMage>().VerificationMove=Vector2.zero;
            int before=g.metrics.shots;
            yield return new WaitForSeconds(3f);
            Check(rangedAI.Windups>0 && rangedAI.Throws>0 && g.metrics.shots>before,"Ranged enemy telegraphs and throws the same projectile through Shoot");
            string report="failures="+failures+"\n"+string.Join("\n",results.ToArray());
            Directory.CreateDirectory("Verification");
            File.WriteAllText("Verification/projectile-shared-physics.txt",report);
            Debug.Log("[ProjectileVerification] "+report);
            enabled=false;
        }
    }
}
#endif
