#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaGlideTimingVerification : MonoBehaviour
    {
        readonly List<string> results=new List<string>(); int failures;
        void Check(bool ok,string label) {results.Add((ok?"PASS ":"FAIL ")+label); if(!ok) failures++;}
        public void Run() {StartCoroutine(Verify());}
        IEnumerator Verify()
        {
            var g=ArenaDirector.Instance; g.Restart(); g.player.GetComponent<PlayerMage>().enabled=false;
            foreach(var body in g.bodies.ToArray()) if(body!=null && body!=g.player) body.gameObject.SetActive(false);
            var b=g.Spawn(g.projectilePrefab,new Vector2(-20,-20)); b.baseMass=2; b.temperature=150; b.shotHeat=0; b.ThermalStep(0);
            TerrainCell ice=null; foreach(var cell in g.worldGenerator.cells.Values) if(!cell.rough) {ice=cell; break;}
            ice.temperature=-50; ice.Refresh(); g.laws.Add(WorldLaw.VaporGlide);
            b.Body.position=(Vector2)ice.transform.position+Vector2.right*.45f; b.transform.position=b.Body.position;
            b.SendMessage("FixedUpdate"); float began=Time.time;
            Check(b.VaporGliding && b.Body.drag==0 && b.Collider.sharedMaterial.friction==0,"Native body state activates at ice overlap and zeros ground/contact friction");
            b.Body.position=new Vector2(-10,-20); b.transform.position=b.Body.position; b.Body.velocity=Vector2.zero; b.ClearThermalStatus();
            while(Time.time-began<4.7f) yield return null;
            results.Add("MEASURE elapsed="+(Time.time-began)+" active="+b.VaporGliding+" drag="+b.Body.drag+" velocity="+b.Body.velocity);
            Check(b.VaporGliding && b.Body.drag==0 && b.Body.velocity.sqrMagnitude>0,"Glide persists off surface after cooling for almost five seconds");
            while(Time.time-began<5.15f) yield return null;
            Check(!b.VaporGliding && b.Body.drag>0 && b.Collider.sharedMaterial==g.normalMaterial,"At five seconds glide expires and ordinary friction/material resumes");
            string report="failures="+failures+"\n"+string.Join("\n",results); File.WriteAllText("Verification/glide-timing-regression.txt",report); Debug.Log(report);
            g.Restart(); enabled=false;
        }
    }
}
#endif
