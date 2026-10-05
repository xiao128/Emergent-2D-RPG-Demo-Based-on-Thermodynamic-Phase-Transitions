#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaCrowdingOutcomeVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>(); int failures;
        ArenaDirector g;
        void Check(bool ok,string text) {results.Add((ok ? "PASS " : "FAIL ")+text); if(!ok) failures++;}
        public void Run() {g=ArenaDirector.Instance; StartCoroutine(Verify());}
        void Isolate()
        {
            g.player.GetComponent<PlayerMage>().enabled=false;
            foreach(var b in g.bodies.ToArray()) if(b!=null && b!=g.player) b.gameObject.SetActive(false);
            g.player.ResetAt(new Vector2(0,-8)); g.player.transform.position=g.player.Body.position; Physics2D.SyncTransforms();
        }
        IEnumerator Verify()
        {
            float originalMana=g.player.GetComponent<PlayerMage>().maxMana;
            float originalSpeed=g.player.topSpeed;
            float originalHitDuration=g.player.GetComponent<PlayerHitFeedback>().invulnerabilityDuration;
            g.Restart(); Isolate(); g.laws.Add(WorldLaw.Crowding);
            var items=new List<ThermoBody>(); int needed=g.tuning.crowdingItemThreshold+1;
            for(int i=0;i<needed;i++)
            {
                float angle=i*Mathf.PI*2/needed;
                var b=g.Spawn(g.projectilePrefab,g.player.Body.position+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*1.5f);
                b.Body.bodyType=RigidbodyType2D.Static; b.shotHeat=0; b.temperature=20; items.Add(b);
            }
            float before=g.player.health;
            yield return new WaitForSecondsRealtime(g.tuning.crowdingCheckInterval+.25f);
            var indicator=g.player.GetComponent<CrowdingIndicator>();
            Check(g.player.CrowdingNeighborCount==needed && g.player.health<before,"Cached crowding count causes real player damage");
            Check(indicator.Ring!=null && indicator.Ring.enabled,"Player crowding warning ring visible");
            Check(indicator.Ring.loop && indicator.Ring.positionCount==64 && indicator.Ring.GetComponent<Collider2D>()==null,"Ring is a closed visual, without physical collider");
            float scan=g.player.NextCrowdingScanAt; g.SetPaused(true);
            g.player.transform.localScale*=1.5f; Physics2D.SyncTransforms(); yield return null;
            float radius=Vector3.Distance(indicator.Ring.transform.TransformPoint(indicator.Ring.GetPosition(0)),g.player.Collider.bounds.center);
            Check(Mathf.Abs(radius-g.tuning.crowdingRadius)<.01f,"Ring radius stays equal to detection radius on scaled player");
            Check(g.player.NextCrowdingScanAt==scan,"Showing ring adds no per-frame crowding scan");
            g.player.transform.localScale/=1.5f; Physics2D.SyncTransforms(); g.SetPaused(false); yield return null;
            ScreenCapture.CaptureScreenshot("Verification/Captures/crowding-ring.png"); yield return null;
            items[0].gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(g.tuning.crowdingCheckInterval+.25f);
            Check(g.player.CrowdingNeighborCount==g.tuning.crowdingItemThreshold && !indicator.Ring.enabled,"Ring disappears after cached count drops to threshold");
            items[0].gameObject.SetActive(true);
            var enemy=g.Spawn(g.lightEnemyPrefab,g.player.Body.position+Vector2.right*.8f);
            enemy.GetComponent<EnemyBrain>().enabled=false; enemy.Body.bodyType=RigidbodyType2D.Static; enemy.health=enemy.maxHealth=1000;
            yield return new WaitForSecondsRealtime(g.tuning.crowdingCheckInterval+.25f);
            Check(enemy.GetComponent<CrowdingIndicator>().Ring!=null && enemy.GetComponent<CrowdingIndicator>().Ring.enabled,"Crowded enemies also show a warning ring");
            Check(items[0].GetComponent<CrowdingIndicator>()==null,"Props/projectiles do not receive actor crowding rings");
            g.laws.Clear(); yield return null;
            Check(!indicator.Ring.enabled && !enemy.GetComponent<CrowdingIndicator>().Ring.enabled,"Removing law immediately hides warning rings");

            var oldPlayer=g.player; int oldId=oldPlayer.GetInstanceID();
            oldPlayer.ResetAt(oldPlayer.Body.position); oldPlayer.GetComponent<PlayerMage>().Cast(true,Vector2.up); oldPlayer.health=1;
            float deathAt=Time.realtimeSinceStartup; oldPlayer.Damage(1,"周围实体拥挤",DamageKind.Crowding);
            Check(g.State==RunState.Defeat && g.player==null && !oldPlayer.gameObject.activeSelf,"Fatal actual damage immediately removes player from active world");
            Check(!g.Outcome.ResultReady,"Defeat does not reveal result immediately");
            yield return null;
            yield return new WaitForSecondsRealtime(.15f);
            Check(oldPlayer==null && GameObject.Find("Spell charge preview")==null,"Player is actually destroyed, including charge preview");
            Check(!g.hud.resultPanel.activeSelf && !g.hud.minimap.playerMarker.gameObject.activeSelf && Camera.main.GetComponent<ArenaCameraFollow>().target==null,"Death scene has no result popup or player marker; camera remains at scene");
            Check(g.hud.messageLabel.text.Contains("周围实体拥挤"),"Death cause remains readable without player object");
            ScreenCapture.CaptureScreenshot("Verification/Captures/defeat-before-result.png");
            yield return new WaitForSecondsRealtime(4.25f);
            Check(Time.realtimeSinceStartup-deathAt<5 && !g.Outcome.ResultReady && !g.hud.resultPanel.activeSelf,"Defeat result remains hidden before five real seconds");
            while(!g.Outcome.ResultReady && Time.realtimeSinceStartup-deathAt<6) yield return null;
            yield return null;
            float shownAfter=Time.realtimeSinceStartup-deathAt;
            Check(shownAfter>=5 && shownAfter<5.5f && g.hud.resultPanel.activeSelf,"Defeat result appears after five real seconds; measured="+shownAfter.ToString("0.000"));
            Check(Time.timeScale==0 && g.hud.resultBody.text.Contains("周围实体拥挤"),"Result works with frozen time and preserves cause/statistics");
            ScreenCapture.CaptureScreenshot("Verification/Captures/defeat-result-delayed.png"); yield return null;
            g.hud.restartButton.onClick.Invoke(); yield return null;
            Check(g.State==RunState.Explore && g.player!=null && g.player.GetInstanceID()!=oldId && !g.player.Dead,"Result restart button creates a new living player");
            Check(g.player.GetComponent<PlayerMage>().maxMana==originalMana && g.player.topSpeed==originalSpeed && g.player.GetComponent<PlayerHitFeedback>().invulnerabilityDuration==originalHitDuration,"Respawn preserves scene-authored mana/speed/hit duration");
            Check(g.player.GetComponent<PlayerMage>().staff!=null && g.player.GetComponent<PlayerMage>().facing!=null && g.player.GetComponent<BodySpriteAnimator>().HasDirections,"Respawn preserves internal sprite/staff/animation references");
            Check(Camera.main.GetComponent<ArenaCameraFollow>().target==g.player.transform && g.bodies.FindAll(b=>b!=null && b.kind==BodyKind.Player).Count==1,"Camera and registry track exactly one restored player");
            Check(!g.hud.resultPanel.activeSelf && !g.Outcome.ResultReady && Time.timeScale==1,"Restart cancels old result and resumes simulation");

            Isolate(); typeof(ArenaDirector).GetProperty("World").SetValue(g,g.tuning.worldCount); typeof(ArenaDirector).GetProperty("State").SetValue(g,RunState.Boss);
            var boss=g.Spawn(g.bossPrefab,new Vector2(4,-8)); boss.GetComponent<EnemyBrain>().enabled=false;
            var winner=g.player; float winAt=Time.realtimeSinceStartup; boss.Die("胜利展示验证"); g.CompleteRun();
            Check(g.State==RunState.Victory && g.player==null && !winner.gameObject.activeSelf && !g.Outcome.ResultReady,"Clock completion removes player and starts victory delay");
            yield return null;
            Check(winner==null && !g.hud.resultPanel.activeSelf,"Victory player is actually destroyed and popup stays hidden");
            yield return new WaitForSecondsRealtime(4.25f);
            Check(Time.realtimeSinceStartup-winAt<5 && !g.hud.resultPanel.activeSelf,"Victory popup stays hidden before five seconds");
            while(!g.Outcome.ResultReady && Time.realtimeSinceStartup-winAt<6) yield return null;
            yield return null;
            Check(Time.realtimeSinceStartup-winAt>=5 && g.hud.resultPanel.activeSelf && g.hud.resultTitle.text=="魔王已被击败","Victory result appears after five seconds");
            g.hud.GetComponent<ArenaMenuUI>().ReturnToMainMenu(); yield return null;
            Check(g.State==RunState.Title && g.player!=null && g.hud.titlePanel.activeSelf && !g.hud.resultPanel.activeSelf,"Return to title after victory restores player and menu");
            Check(g.player.GetComponent<PlayerProgression>().Level==1 && g.player.GetComponent<PlayerMage>().maxMana==originalMana,"New run resets growth without losing initial scene mana");

            g.StartRun(); Isolate(); g.player.Die("等待中返回菜单验证"); yield return null;
            g.hud.GetComponent<ArenaMenuUI>().ReturnToMainMenu();
            yield return new WaitForSecondsRealtime(5.3f);
            Check(g.State==RunState.Title && g.player!=null && !g.Outcome.ResultReady && !g.hud.resultPanel.activeSelf,"Returning during delay cancels stale popup permanently");
            string report="failures="+failures+"\n"+string.Join("\n",results); File.WriteAllText("Verification/crowding-outcome-regression.txt",report); Debug.Log("[CrowdingOutcomeVerification] "+report); enabled=false;
        }
    }
}
#endif
