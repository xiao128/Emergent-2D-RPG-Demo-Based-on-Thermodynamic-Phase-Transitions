#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaHitFeedbackVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        int failures;
        void Check(bool ok,string text) { results.Add((ok ? "PASS " : "FAIL ")+text); if(!ok) failures++; }
        public void Run() { StartCoroutine(Verify()); }
        static void ElapseHit(PlayerHitFeedback feedback,float elapsed)
        {
            // Narrow 0.1-second windows cannot be sampled reliably using frame waits
            // in a slow Editor. Drive the timestamp, then exercise actual rendering/damage.
            typeof(PlayerHitFeedback).GetField("<LastHitTime>k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(feedback,Time.time-elapsed);
        }
        IEnumerator Verify()
        {
            var g=ArenaDirector.Instance; g.Restart();
            foreach(var ai in FindObjectsOfType<EnemyBrain>()) ai.enabled=false;
            var p=g.player; p.GetComponent<PlayerMage>().enabled=false; p.ResetAt(new Vector2(-24,-20));
            var feedback=p.GetComponent<PlayerHitFeedback>(); var guard=p.GetComponent<PlayerDeathGuard>();
            var appearance=p.GetComponent<BodyAppearance>(); var overlay=FindObjectOfType<PlayerScreenFeedback>();
            p.Damage(10,"high health hit"); appearance.SendMessage("Update");
            Check(feedback.Invulnerable && feedback.Flashing && feedback.EdgeOpacity==0,"High-health hit starts flash and short immunity without dark edge");
            Check(p.visual.color.a>.9f,"First flash phase remains bright and visible");
            float health=p.health;
            ElapseHit(feedback,.06f); appearance.SendMessage("Update");
            Check(p.visual.color.a<.3f,"Next flash phase visibly reduces sprite alpha");
            p.Damage(999,"repeat hit"); p.Damage(999,"continuous repeat",DamageKind.Overheat,true);
            Check(p.health==health && !guard.Invulnerable,"Short immunity blocks both repeat impact and continuous damage");
            ElapseHit(feedback,.11f);
            Check(!feedback.Invulnerable,"Ordinary immunity expires after 0.1 seconds");
            p.Damage(5,"after short immunity");
            Check(p.health==health-5,"Damage after 0.1 seconds is accepted despite old 0.45-second timing");
            ElapseHit(feedback,.11f); appearance.SendMessage("Update");
            Check(!feedback.Flashing && p.visual.color.a>.9f,"Flash ends and restores visible sprite");
            feedback.invulnerabilityDuration=.35f; ElapseHit(feedback,.2f);
            Check(feedback.Invulnerable && feedback.Flashing,"Public duration extends immunity and flashing together");
            ElapseHit(feedback,.36f);
            Check(!feedback.Invulnerable && !feedback.Flashing,"Custom duration ends immunity and flashing together");
            feedback.invulnerabilityDuration=.1f;
            p.ResetAt(new Vector2(-24,-20)); p.health=p.maxHealth*.25f; p.Damage(p.maxHealth*.1f,"low health hit");
            overlay.SendMessage("LateUpdate"); var image=overlay.GetComponent<UnityEngine.UI.RawImage>();
            Check(feedback.EdgeOpacity>.6f && image.enabled && image.color.a>.6f,"Hit crossing below 20 percent darkens the edge");
            Check(!image.raycastTarget && overlay.transform.GetSiblingIndex()==0,"Border remains behind HUD and cannot intercept clicks");
            yield return new WaitForSeconds(.72f); overlay.SendMessage("LateUpdate");
            Check(feedback.EdgeOpacity==0 && !image.enabled,"Low-health edge fades after 0.7 seconds");
            p.ResetAt(new Vector2(-24,-20)); p.health=p.maxHealth*.3f; p.Damage(p.maxHealth*.1f,"exact threshold");
            Check(feedback.EdgeOpacity==0,"Exactly 20 percent health uses ordinary hit feedback");
            p.ResetAt(new Vector2(-24,-20)); overlay.SendMessage("LateUpdate");
            Check(!feedback.Invulnerable && !feedback.Flashing && !image.enabled,"Reset removes feedback and short immunity");
            p.Damage(p.maxHealth+1,"lethal burst");
            yield return new WaitForSeconds(.12f); p.Damage(5,"after short but before death guard immunity");
            Check(p.health==1 && !p.Dead && guard.Invulnerable && feedback.Invulnerable && feedback.Flashing,"One-second death guard remains immune and flashing beyond ordinary hit duration");
            yield return new WaitForSeconds(.92f); p.Damage(1,"after full immunity");
            Check(p.Dead,"Damage resumes after one-second death guard expires");
            g.Restart();
            string report="failures="+failures+"\n"+string.Join("\n",results);
            Directory.CreateDirectory("Verification"); File.WriteAllText("Verification/hit-feedback-regression.txt",report);
            Debug.Log("[HitFeedbackVerification] "+report); enabled=false;
        }
    }
}
#endif
