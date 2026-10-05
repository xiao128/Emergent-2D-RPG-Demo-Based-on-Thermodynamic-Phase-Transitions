#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PhaseArena
{
    public static class ArenaTemperatureBorderVerification
    {
        public static string Run()
        {
            var g=ArenaDirector.Instance; g.Restart(); g.enabled=false;
            var p=g.player; p.GetComponent<PlayerMage>().enabled=false;
            foreach(var ai in Object.FindObjectsOfType<EnemyBrain>()) ai.enabled=false;
            Time.timeScale=0;
            var edge=Object.FindObjectOfType<PlayerTemperatureBorder>();
            var images=edge.GetComponentsInChildren<UnityEngine.UI.Image>();
            var results=new List<string>(); int failures=0;
            System.Action<bool,string> check=(ok,text)=>{ results.Add((ok ? "PASS " : "FAIL ")+text); if(!ok) failures++; };
            check(images.Length==4,"Exactly four temperature edge lines");
            p.temperature=150; edge.Refresh();
            check(System.Array.TrueForAll(images,img=>img.enabled && img.color==edge.hotColor),"Extreme heat turns all four edges red without injury law");
            p.temperature=-120; edge.Refresh();
            check(System.Array.TrueForAll(images,img=>img.enabled && img.color==edge.coldColor),"Extreme cold turns all four edges blue");
            var canvas=edge.GetComponentInParent<Canvas>();
            check(Mathf.Abs(images[0].rectTransform.rect.width*canvas.scaleFactor-2)<.001f && Mathf.Abs(images[2].rectTransform.rect.height*canvas.scaleFactor-2)<.001f,"Vertical and horizontal edges are exactly two screen pixels");
            check(System.Array.TrueForAll(images,img=>!img.raycastTarget),"Temperature edges cannot intercept clicks");
            foreach(float temperature in new[]{20f,g.tuning.burnTemperature,g.tuning.frostTemperature})
            {
                p.temperature=temperature; edge.Refresh();
                check(System.Array.TrueForAll(images,img=>!img.enabled),"Normal interval/boundary "+temperature+" hides temperature edges");
            }
            p.GetComponent<PlayerHitFeedback>().ResetFeedback(); p.temperature=150; p.health=p.maxHealth*.22f; p.Damage(p.maxHealth*.04f,"Border coexistence");
            var damage=Object.FindObjectOfType<PlayerScreenFeedback>(); damage.SendMessage("LateUpdate"); edge.Refresh();
            check(damage.GetComponent<UnityEngine.UI.RawImage>().enabled && images[0].enabled && images[0].color==edge.hotColor,"Low-health damage edge and hot line coexist");
            p.GetComponent<PlayerMage>().ResetTools(); p.GetComponent<PlayerMage>().Recover(); edge.Refresh();
            check(System.Array.TrueForAll(images,img=>!img.enabled),"Space cleanse removes the temperature edge");
            p.ResetAt(g.SpawnPosition); p.temperature=-120; p.Die("UI death fixture"); edge.Refresh();
            check(System.Array.TrueForAll(images,img=>!img.enabled),"Death hides the temperature edge");
            var font=g.hud.skillsLabel.font;
            check(font.HasCharacter('恢') && font.HasCharacter('复') && font.HasCharacter('灼') && font.HasCharacter('辐'),"New recovery and law text glyphs are baked");
            string report="failures="+failures+"\n"+string.Join("\n",results);
            File.WriteAllText("Verification/temperature-border-regression.txt",report); Debug.Log("[TemperatureBorderVerification] "+report);
            g.enabled=true; return report;
        }
    }
}
#endif
