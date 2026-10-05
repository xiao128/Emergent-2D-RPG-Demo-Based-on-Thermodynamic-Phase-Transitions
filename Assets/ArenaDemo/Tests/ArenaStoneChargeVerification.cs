#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PhaseArena
{
    public static class ArenaStoneChargeVerification
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()
        {
            var g=ArenaDirector.Instance; g.Restart();
            var p=g.player; var mage=p.GetComponent<PlayerMage>(); mage.enabled=false;
            foreach(var body in g.bodies.ToArray()) if(body!=null && body!=p) body.gameObject.SetActive(false);
            p.ResetAt(new Vector2(-24,-20)); g.laws.Add(WorldLaw.StoneMagic);
            var results=new List<string>(); int failures=0;
            var fixtures=new List<GameObject>();
            System.Action<bool,string> check=(ok,text)=>{ results.Add((ok ? "PASS " : "FAIL ")+text); if(!ok) failures++; };
            var advance=typeof(PlayerMage).GetMethod("AdvanceCharge",Hidden);
            float formedWidth=0,formedMass=0,maxRadius=0;
            try
            {
                foreach(float fraction in new[]{0f,.5f,1f,2f})
                {
                    mage.ResetTools(); mage.Cast(true,Vector2.right);
                    float time=g.tuning.projectileChargeDuration+Mathf.Max(.01f,g.tuning.stoneFullChargeDuration-g.tuning.projectileChargeDuration)*fraction;
                    advance.Invoke(mage,new object[]{time});
                    var preview=typeof(PlayerMage).GetField("chargePreview",Hidden).GetValue(mage) as SpriteRenderer;
                    float beforeMana=mage.Mana;
                    float width=preview.bounds.size.x;
                    check(preview.GetComponent<Collider2D>()==null && preview.GetComponent<Rigidbody2D>()==null,"Charge "+fraction+" remains a movable visual-only preview");
                    check(mage.ReleaseCharge(Vector2.right),"Charge "+fraction+" releases successfully");
                    var stone=g.bodies.FindLast(b=>b!=null && b.kind==BodyKind.Projectile);
                    fixtures.Add(stone.gameObject); Physics2D.SyncTransforms();
                    check(Mathf.Abs(stone.visual.bounds.size.x-width)<.001f && Mathf.Abs(stone.Collider.bounds.extents.x-stone.radius)<.001f,"Charge "+fraction+" preview matches actual sprite and collider");
                    check(!stone.Collider.Distance(p.Collider).isOverlapped && Mathf.Abs(beforeMana-mage.Mana-mage.spellManaCost)<.001f && stone.Body.velocity==Vector2.zero,"Charge "+fraction+" spawns clear stationary stone and charges mana once");
                    if(fraction==0) { formedWidth=width; formedMass=stone.Mass; }
                    else check(width>formedWidth && Mathf.Abs(stone.Mass/formedMass-Mathf.Pow(width/formedWidth,2))<.001f,"Charge "+fraction+" increases physical mass with 2D area");
                    if(fraction>=1)
                    {
                        float reference=g.worldGenerator.rockPrefab.radius*WorldLayout.TypicalRockScale;
                        check(Mathf.Abs(stone.radius/reference-g.tuning.stoneMaximumRockScale)<.001f,"Charge "+fraction+" caps size at configured 1.5 times standard rock");
                        maxRadius=stone.radius;
                    }
                    check(stone.temperature==150 && stone.shotHeat==105,"Charge "+fraction+" preserves fire temperature and heating");
                    check(mage.Melee(Vector2.right) && mage.LastMeleeTarget==stone && stone.Body.velocity.x>0,"Charge "+fraction+" stone can be kicked by F");
                    stone.gameObject.SetActive(false);
                }
                mage.ResetTools(); mage.Cast(false,Vector2.right); advance.Invoke(mage,new object[]{g.tuning.stoneFullChargeDuration});
                check(mage.ReleaseCharge(Vector2.right),"Fully charged ice stone releases");
                var cold=g.bodies.FindLast(b=>b!=null && b.kind==BodyKind.Projectile); fixtures.Add(cold.gameObject);
                check(Mathf.Abs(cold.radius-maxRadius)<.001f && cold.temperature==-120 && cold.shotHeat==-95,"Fully charged ice stone has same size and mass policy and retains cooling");
                cold.gameObject.SetActive(false);
                var wall=new GameObject("Stone charge clearance wall"); fixtures.Add(wall); wall.layer=10;
                var box=wall.AddComponent<BoxCollider2D>(); box.size=new Vector2(.1f,3);
                wall.transform.position=(Vector2)p.Collider.bounds.center+Vector2.right*1.25f; Physics2D.SyncTransforms();
                mage.ResetTools(); mage.Cast(true,Vector2.right); advance.Invoke(mage,new object[]{g.tuning.stoneFullChargeDuration});
                float mana=mage.Mana; int shots=g.metrics.shots;
                check(!mage.ReleaseCharge(Vector2.right) && mage.Mana==mana && g.metrics.shots==shots,"Large stone cannot appear in narrow gap or spend mana on failed placement");
                wall.SetActive(false); mage.ResetTools(); g.laws.Clear();
                var ordinary=g.Shoot(p,Vector2.right,105); fixtures.Add(ordinary.gameObject);
                check(Mathf.Abs(ordinary.radius-g.projectilePrefab.radius)<.001f && ordinary.Mass==g.tuning.spellMass,"Normal ice/fire spell size and mass are unchanged without stone law");
            }
            catch(System.Exception ex) { check(false,"Exception: "+ex); }
            finally
            {
                foreach(var fixture in fixtures) if(fixture!=null) { fixture.SetActive(false); Object.Destroy(fixture); }
                mage.ResetTools(); g.Restart();
            }
            string report="failures="+failures+"\n"+string.Join("\n",results);
            Directory.CreateDirectory("Verification"); File.WriteAllText("Verification/stone-charge-regression.txt",report);
            Debug.Log("[StoneChargeVerification] "+report); return report;
        }
    }
}
#endif
