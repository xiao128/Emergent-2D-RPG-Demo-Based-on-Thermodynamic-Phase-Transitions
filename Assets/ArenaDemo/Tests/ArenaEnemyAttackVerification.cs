#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    public static class ArenaEnemyAttackVerification
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()
        {
            var g=ArenaDirector.Instance; g.Restart(); g.enabled=false; Time.timeScale=0;
            var p=g.player; p.GetComponent<PlayerMage>().enabled=false;
            foreach(var b in g.bodies.ToArray()) if(b!=null && b!=p) b.gameObject.SetActive(false);
            p.ResetAt(new Vector2(-32,-24)); p.transform.position=p.Body.position;
            var results=new List<string>(); int failures=0;
            System.Action<bool,string> check=(ok,name)=>{ results.Add((ok ? "PASS " : "FAIL ")+name); if(!ok) failures++; };
            var fixtures=new List<ThermoBody>();
            var originalMode=Physics2D.simulationMode;
            // Baseline travel expects uniform grass; generated rough patches can cross this corridor.
            var authoredCells=new Dictionary<Vector2Int,TerrainCell>(g.worldGenerator.cells);
            g.worldGenerator.cells.Clear();
            Physics2D.simulationMode=SimulationMode2D.Script;
            var begin=typeof(EnemyBrain).GetMethod("BeginAttack",Hidden);
            var until=typeof(EnemyBrain).GetField("chargeUntil",Hidden);
            var at=typeof(EnemyBrain).GetField("chargeAt",Hidden);
            try
            {
                float previousImpulse=0;
                foreach(var prefab in new[]{g.lightEnemyPrefab,g.mediumEnemyPrefab,g.heavyEnemyPrefab,g.guardPrefab,g.bossPrefab})
                {
                    var body=g.Spawn(prefab,new Vector2(-20,-20)); fixtures.Add(body);
                    var ai=body.GetComponent<EnemyBrain>(); ai.enabled=false;
                    body.enabled=false; body.health=body.maxHealth=10000; body.Body.velocity=Vector2.zero;
                    Physics2D.SyncTransforms(); body.SendMessage("FixedUpdate");
                    float predicted=EnemyAttackPhysics.TravelDistance(body,g), impulse=ai.LungeImpulse;
                    check(impulse>previousImpulse,prefab.name+" launch impulse increases with size/tier"); previousImpulse=impulse;
                    check(predicted>4.8f,prefab.name+" estimated charge covers at least 4.8 units on grass");
                    begin.Invoke(ai,new object[]{Vector2.right,g}); ai.SendMessage("FixedUpdate");
                    var warning=body.GetComponent<LineRenderer>();
                    check(ai.IsWindingUp && Mathf.Abs(Vector2.Distance(warning.GetPosition(0),warning.GetPosition(1))-ai.AttackReach)<.001f,prefab.name+" telegraph matches actual attack reach");
                    until.SetValue(ai,Time.time-.01f); ai.SendMessage("FixedUpdate");
                    float launch=body.Body.velocity.x; Vector2 start=body.Body.position;
                    check(ai.Lunges==1 && ai.IsLunging && launch>=30,prefab.name+" actual AI emits configured launch impulse");
                    int frames=Mathf.CeilToInt(g.tuning.enemyLungeDuration/Time.fixedDeltaTime);
                    for(int i=0;i<frames;i++) { body.SendMessage("FixedUpdate"); ai.SendMessage("FixedUpdate"); Physics2D.Simulate(Time.fixedDeltaTime); }
                    float distance=body.Body.position.x-start.x;
                    check(Mathf.Abs(distance-predicted)<.03f && distance>4.8f,prefab.name+" actual physical charge matches prediction");
                    results.Add("MEASURE "+prefab.name+" mass="+body.Mass+" impulse="+impulse+" speed="+launch+" distance="+distance+" predicted="+predicted);
                    body.gameObject.SetActive(false);
                }
                var savedCells=new Dictionary<Vector2Int,TerrainCell>(g.worldGenerator.cells);
                var roughObject=new GameObject("Verification rough corridor");
                var rough=roughObject.AddComponent<TerrainCell>(); rough.rough=true;
                try
                {
                    for(int x=-21;x<=-10;x++) for(int y=-21;y<=-19;y++) g.worldGenerator.cells[new Vector2Int(x,y)]=rough;
                    var heavy=g.Spawn(g.heavyEnemyPrefab,new Vector2(-20,-20)); fixtures.Add(heavy);
                    var heavyBrain=heavy.GetComponent<EnemyBrain>(); heavyBrain.enabled=false; heavy.enabled=false;
                    check(heavy.Body.drag==0 && heavyBrain.AttackReach<10,"Fresh enemy estimates range from ground friction before its first physics update");
                    heavy.SendMessage("FixedUpdate"); float expected=EnemyAttackPhysics.TravelDistance(heavy,g);
                    begin.Invoke(heavyBrain,new object[]{Vector2.right,g}); until.SetValue(heavyBrain,Time.time-.01f); heavyBrain.SendMessage("FixedUpdate");
                    Vector2 start=heavy.Body.position;
                    for(int i=0;i<40;i++) { heavy.SendMessage("FixedUpdate"); heavyBrain.SendMessage("FixedUpdate"); Physics2D.Simulate(.02f); }
                    float distance=heavy.Body.position.x-start.x;
                    check(distance>4.8f && Mathf.Abs(distance-expected)<.03f,"Heavy enemy also charges nearly five units across rough terrain");
                    results.Add("MEASURE roughHeavyDistance="+distance+" predicted="+expected);
                    heavy.gameObject.SetActive(false);
                }
                finally { g.worldGenerator.cells.Clear(); foreach(var cell in savedCells) g.worldGenerator.cells[cell.Key]=cell.Value; roughObject.SetActive(false); Object.Destroy(roughObject); }
                var enemy=g.Spawn(g.heavyEnemyPrefab,new Vector2(-20,-20)); fixtures.Add(enemy);
                var brain=enemy.GetComponent<EnemyBrain>(); brain.enabled=false; enemy.enabled=false;
                enemy.SendMessage("FixedUpdate"); float range=brain.AttackReach;
                check(range>4.2f,"Heavy enemy is no longer limited to old 4.2-unit attack range");
                p.ResetAt(enemy.Body.position+Vector2.right*(range+.2f)); p.transform.position=p.Body.position;
                brain.SetHome(enemy.Body.position,false); at.SetValue(brain,Time.time-1); brain.SendMessage("FixedUpdate");
                check(!brain.Alert && !brain.IsWindingUp,"Target outside physical charge reach is not acquired");
                p.ResetAt(enemy.Body.position+Vector2.right*(range-.2f)); p.transform.position=p.Body.position;
                Physics2D.SyncTransforms(); brain.SendMessage("FixedUpdate");
                check(brain.Alert && brain.IsWindingUp,"Target inside physical charge reach is acquired and telegraphed");
                float originalHp=p.maxHealth; p.health=p.maxHealth=10000;
                enemy.Body.velocity=Vector2.zero; until.SetValue(brain,Time.time-.01f); brain.SendMessage("FixedUpdate");
                for(int i=0;i<40;i++) { enemy.SendMessage("FixedUpdate"); brain.SendMessage("FixedUpdate"); p.SendMessage("FixedUpdate"); Physics2D.Simulate(.02f); }
                check(p.health<10000 && p.Body.velocity.x>0,"Charge from near the attack boundary actually reaches, damages and pushes player");
                results.Add("MEASURE boundaryHitDamage="+(10000-p.health)+" playerVelocity="+p.Body.velocity.x);
                p.maxHealth=originalHp; p.ResetAt(new Vector2(-32,-24));
                enemy.ResetAt(new Vector2(-20,-20)); enemy.transform.position=enemy.Body.position;
                brain.SetHome(enemy.Body.position,false); at.SetValue(brain,Time.time+100);
                p.ResetAt(enemy.Body.position+Vector2.right*range*.85f); p.transform.position=p.Body.position;
                enemy.Body.velocity=Vector2.zero; Physics2D.SyncTransforms(); brain.SendMessage("FixedUpdate"); Physics2D.Simulate(Time.fixedDeltaTime);
                check(enemy.Body.velocity.x>0,"Cooldown movement approaches inner attack window instead of retreating to outer range");
                enemy.Body.velocity=Vector2.zero; enemy.temperature=-120; g.laws.Add(WorldLaw.SuperSlide);
                check(brain.AttackReach>range*2,"Reach expands when law actually reduces ground friction");
                g.laws.Clear(); enemy.gameObject.SetActive(false); p.ResetAt(new Vector2(-32,-24)); p.transform.position=p.Body.position;
                var caster=g.Spawn(g.rangedEnemyPrefab,new Vector2(-20,-20)); fixtures.Add(caster);
                var ranged=caster.GetComponent<EnemyBrain>(); ranged.enabled=false; caster.enabled=false; Physics2D.SyncTransforms();
                foreach(bool stone in new[]{false,true})
                {
                    g.laws.Clear(); if(stone) g.laws.Add(WorldLaw.StoneMagic);
                    var shot=g.Shoot(caster,Vector2.right,105); fixtures.Add(shot);
                    check(shot!=null && Mathf.Abs(shot.Body.velocity.x*shot.Mass-g.tuning.enemyProjectileImpulse)<.001f,(stone ? "Stone" : "Normal")+" ranged projectile receives 24 physical impulse");
                    shot.enabled=false; shot.SendMessage("FixedUpdate"); Vector2 start=shot.Body.position;
                    for(int i=0;i<25;i++) { shot.SendMessage("FixedUpdate"); Physics2D.Simulate(.02f); }
                    check(shot.Body.position.x-start.x>7 && shot.Body.velocity.x>10,(stone ? "Stone" : "Normal")+" ranged projectile remains fast after half a second");
                    results.Add("MEASURE ranged stone="+stone+" distance0.5="+(shot.Body.position.x-start.x)+" speed0.5="+shot.Body.velocity.x);
                    shot.gameObject.SetActive(false); Physics2D.SyncTransforms();
                }
                check(ranged.AttackReach==g.tuning.rangedAttackRange,"Ranged enemy uses its configured firing range");
                g.laws.Clear(); Physics2D.SyncTransforms();
                var own=g.Shoot(p,Vector2.right,105); fixtures.Add(own);
                check(own!=null && own.Body.velocity==Vector2.zero,"Player projectile still spawns stationary for manual F launch");
            }
            catch(System.Exception ex) { check(false,"Exception: "+ex); }
            finally
            {
                Physics2D.simulationMode=originalMode;
                g.worldGenerator.cells.Clear(); foreach(var cell in authoredCells) g.worldGenerator.cells[cell.Key]=cell.Value;
                foreach(var body in fixtures) if(body!=null) { body.gameObject.SetActive(false); Object.Destroy(body.gameObject); }
                g.laws.Clear(); g.enabled=true;
            }
            string report="failures="+failures+"\n"+string.Join("\n",results);
            File.WriteAllText("Verification/enemy-attack-regression.txt",report); Debug.Log("[EnemyAttackVerification] "+report);
            return report;
        }
    }
}
#endif
