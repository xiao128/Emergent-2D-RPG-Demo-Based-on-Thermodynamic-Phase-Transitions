#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaThermalLawVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        readonly List<ThermoBody> fixtures=new List<ThermoBody>();
        int failures;
        ArenaDirector g;
        ThermoBody p;
        PlayerMage mage;
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        void Check(bool ok,string text) { results.Add((ok ? "PASS " : "FAIL ")+text); if(!ok) failures++; }
        ThermoBody Fixture(Vector2 pos,BodyKind kind=BodyKind.Projectile,float mass=1)
        {
            var b=g.Spawn(g.projectilePrefab,pos); b.kind=kind; b.baseMass=mass;
            b.shotHeat=0; b.health=b.maxHealth=1000; b.ThermalStep(0); fixtures.Add(b); return b;
        }
        void Clear()
        {
            foreach(var b in fixtures) if(b!=null) { b.gameObject.SetActive(false); Destroy(b.gameObject); }
            fixtures.Clear(); g.laws.Clear();
            p.ResetAt(new Vector2(-32,-24)); p.transform.position=p.Body.position;
            mage.ResetTools(); Physics2D.SyncTransforms();
        }
        public void Run() { StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            g=ArenaDirector.Instance; g.Restart(); p=g.player; mage=p.GetComponent<PlayerMage>();
            mage.enabled=false; g.enabled=false;
            foreach(var b in g.bodies.ToArray()) if(b!=null && b!=p) b.gameObject.SetActive(false);
            Clear();
            try
            {
                Check(WorldLawCatalog.IsAvailable(WorldLaw.ExpandedRadiation) && WorldLawCatalog.IsAvailable(WorldLaw.ThermalInjury),"Both new laws enter random catalog");
                int count=0; foreach(WorldLaw law in Enum.GetValues(typeof(WorldLaw))) if(WorldLawCatalog.IsAvailable(law)) count++;
                Check(count==18,"Random pool has 18 laws, retaining all retired exclusions");
                Gravity(); Clear();
                Radiation(); Clear();
                HotAttack(); Clear();
                Recovery(); Clear();
                p.temperature=150; float hp=p.health;
                for(int i=0;i<20;i++) p.ThermalStep(.1f);
                Check(p.health==hp && !p.GetComponent<PlayerHitFeedback>().Flashing,"Without injury law hot player neither loses HP nor flashes");
                p.temperature=-120; for(int i=0;i<20;i++) p.ThermalStep(.1f);
                Check(p.health==hp,"Without injury law cold player takes no temperature damage");
                Clear(); g.laws.Add(WorldLaw.ThermalInjury); p.temperature=150;
                var cold=Fixture(new Vector2(-18,-20),BodyKind.Enemy); cold.temperature=-120;
                for(int i=1;i<=20;i++)
                {
                    yield return new WaitForSeconds(.1f);
                    p.ThermalStep(.1f); cold.ThermalStep(.1f);
                    if(i==4) Check(p.health==p.maxHealth && cold.health==1000,"Thermal exposure waits for first tick instead of micro damage every update");
                    if(i==5) Check(p.health==p.maxHealth-2 && p.GetComponent<PlayerHitFeedback>().Flashing && cold.health==1000,"Burn applies 2 HP at 0.5 seconds with matched hit feedback");
                    if(i==7) Check(!p.GetComponent<PlayerHitFeedback>().Flashing,"Burn flash ends between damage ticks");
                    if(i==10) Check(p.health==p.maxHealth-4 && cold.health==1000,"Burn repeats at one second while frost still waits");
                }
                Check(p.health==p.maxHealth-8 && cold.health==994 && cold.LastDamageKind==DamageKind.Frostbite,"Two seconds: burn totals 8 HP, frost delivers one 6 HP tick");
                results.Add("MEASURE burnHp="+p.health+" frostHp="+cold.health);
                Clear(); g.laws.Add(WorldLaw.ThermalInjury);
                foreach(var kind in new[]{BodyKind.Projectile,BodyKind.Rock,BodyKind.Tree,BodyKind.Enemy,BodyKind.Boss})
                {
                    var b=Fixture(new Vector2(-18,-20),kind); b.temperature=150; b.ThermalStep(.5f);
                    Check(b.health==998,kind+" participates in thermal injury");
                    b.ClearThermalStatus(); b.temperature=-120; b.ThermalStep(2);
                    Check(b.health==992,kind+" also participates in frost injury");
                    b.gameObject.SetActive(false);
                }
                var barrier=Fixture(new Vector2(-18,-20),BodyKind.StaticObstacle); barrier.indestructible=true; barrier.temperature=150; barrier.ThermalStep(2);
                Check(barrier.health==1000,"Indestructible obstacles remain indestructible");
            }
            finally
            {
                Clear(); g.enabled=true;
                string report="failures="+failures+"\n"+string.Join("\n",results);
                File.WriteAllText("Verification/thermal-law-regression.txt",report); Debug.Log("[ThermalLawVerification] "+report);
            }
        }
        void Gravity()
        {
            g.laws.Add(WorldLaw.Gravity);
            var source=Fixture(new Vector2(-20,-20)); var target=Fixture(new Vector2(-18,-20));
            source.temperature=-120; Physics2D.SyncTransforms(); g.LawSimulation.Tick(0); g.FlowFields.TickSource(source,.1f);
            Check(target.Body.velocity.x<0,"Small cold source creates inward convection without old mass gate");
            target.Body.velocity=Vector2.zero; source.temperature=150; g.FlowFields.TickSource(source,.1f);
            Check(target.Body.velocity.x>0,"Hot source creates outward convection");
            target.Body.velocity=Vector2.zero; source.temperature=20; g.FlowFields.TickSource(source,.1f);
            Check(target.Body.velocity==Vector2.zero,"Ambient source creates no convection");
            source.temperature=20+g.tuning.pressureTemperatureDeadZone;
            Check(!g.LawSimulation.IsGravitySource(source),"Temperature exactly at convection dead-zone does not qualify");
            source.temperature=21+g.tuning.pressureTemperatureDeadZone;
            Check(g.LawSimulation.IsGravitySource(source),"Temperature beyond dead-zone qualifies without mass threshold");
        }
        void Radiation()
        {
            // These centers cross two grid cells: increased range must query beyond old 3x3 grid.
            var a=Fixture(new Vector2(-20.1f,-20)); var b=Fixture(new Vector2(-17.1f,-20));
            a.temperature=150; b.temperature=20; Physics2D.SyncTransforms(); g.LawSimulation.Tick(.1f);
            Check(b.temperature==20,"Baseline thermal range does not reach body three units away");
            var look=a.GetComponent<BodyAppearance>(); if(look==null) look=a.gameObject.AddComponent<BodyAppearance>();
            look.RefreshRing(); float width=a.heatRing.bounds.size.x;
            g.laws.Add(WorldLaw.ExpandedRadiation); a.temperature=150; g.LawSimulation.Tick(.1f); look.RefreshRing();
            Check(b.temperature>20 && Mathf.Approximately(g.LawSimulation.RadiationRadius,3.6f),"Expanded 3.6-unit heat exchange reaches across two grid cells");
            Check(Mathf.Abs(a.heatRing.bounds.size.x/width-1.5f)<.001f,"Visible radiation ring is exactly 1.5 times baseline");
            for(int i=0;i<5;i++) look.RefreshRing();
            Check(Mathf.Abs(a.heatRing.bounds.size.x/width-1.5f)<.001f,"Repeated updates do not compound ring size");
            g.laws.Clear(); look.RefreshRing();
            Check(Mathf.Abs(a.heatRing.bounds.size.x-width)<.001f,"Removing radiation law restores baseline ring");
            var tree=Fixture(new Vector2(-10,-20),BodyKind.Tree);
            Physics2D.SyncTransforms(); float baseDiameter=Mathf.Max(tree.Collider.bounds.size.x,tree.Collider.bounds.size.y)*1.15f;
            g.laws.Add(WorldLaw.ExpandedRadiation); g.laws.Add(WorldLaw.ThermalExpansion);
            tree.temperature=200; tree.ThermalStep(.3f); Physics2D.SyncTransforms();
            var treeLook=tree.GetComponent<BodyAppearance>(); if(treeLook==null) treeLook=tree.gameObject.AddComponent<BodyAppearance>();
            treeLook.RefreshRing();
            Check(Mathf.Abs(tree.heatRing.bounds.size.x-baseDiameter*tree.ShapeScale*1.5f)<.001f,"Tree radiation and thermal expansion compose once even on first render");
        }
        void HotAttack()
        {
            var enemy=g.Spawn(g.mediumEnemyPrefab,new Vector2(-18,-20)); fixtures.Add(enemy);
            var brain=enemy.GetComponent<EnemyBrain>(); brain.enabled=false;
            var begin=typeof(EnemyBrain).GetMethod("BeginAttack",Hidden);
            var until=typeof(EnemyBrain).GetField("chargeUntil",Hidden); var at=typeof(EnemyBrain).GetField("chargeAt",Hidden);
            enemy.temperature=20; begin.Invoke(brain,new object[]{Vector2.left,g});
            float normal=(float)until.GetValue(brain)-Time.time, normalCooldown=(float)at.GetValue(brain)-(float)until.GetValue(brain);
            enemy.temperature=150; begin.Invoke(brain,new object[]{Vector2.left,g});
            float hot=(float)until.GetValue(brain)-Time.time, hotCooldown=(float)at.GetValue(brain)-(float)until.GetValue(brain);
            Check(Mathf.Abs(normal/hot-1.5f)<.001f && hot>.2f,"Hot enemy windup is faster and still visibly telegraphed");
            Check(Mathf.Abs(normalCooldown/hotCooldown-1.5f)<.001f,"Hot enemy attack cooldown accelerates equally");
            enemy.temperature=20; Check(brain.TemperatureAttackMultiplier==1,"Cooling enemy to ambient restores normal attack speed");
        }
        void Recovery()
        {
            g.laws.Add(WorldLaw.ThermalInjury); g.laws.Add(WorldLaw.VaporRecoil);
            p.health=p.maxHealth*.5f; p.temperature=150; p.ThermalStep(.4f);
            float mana=mage.Mana; int waves=g.metrics.vaporBursts; int shields=g.bodies.FindAll(b=>b!=null && b.kind==BodyKind.Shield).Count;
            Check(mage.Recover() && Mathf.Abs(p.health-p.maxHealth*(.5f+g.tuning.recoveryHealthFraction))<.001f && p.temperature==g.tuning.ambientTemperature,"Space recovery restores configured fraction of maximum HP and normal temperature");
            Check(mage.Mana==mana && !mage.Recover() && Mathf.Approximately(mage.RecoveryReady-Time.time,7),"Recovery has seven-second cooldown and no mana cost");
            Check(g.metrics.vaporBursts==waves && shields==g.bodies.FindAll(b=>b!=null && b.kind==BodyKind.Shield).Count,"Cleanse creates neither steam wave nor shields");
            float hp=p.health; p.temperature=150; p.ThermalStep(.1f);
            Check(p.health==hp,"Cleanse also removes accumulated burn exposure");
            mage.ResetTools(); p.health=p.maxHealth-1; p.temperature=-120;
            Check(mage.Recover() && p.health==p.maxHealth && p.temperature==20,"Recovery clamps at full HP and clears cold state");
            mage.ResetTools(); p.maxHealth+=g.tuning.healthGainPerLevel; p.health=p.maxHealth*.5f;
            Check(mage.Recover() && Mathf.Abs(p.health-p.maxHealth*(.5f+g.tuning.recoveryHealthFraction))<.001f,"Recovery scales with upgraded maximum HP");
            p.maxHealth-=g.tuning.healthGainPerLevel;
        }
    }
}
#endif
