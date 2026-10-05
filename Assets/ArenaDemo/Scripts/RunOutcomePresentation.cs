using System.Collections;
using UnityEngine;
namespace PhaseArena
{
    public sealed class RunOutcomePresentation
    {
        readonly ArenaDirector world;
        Coroutine reveal;
        public bool ResultReady { get; private set; }
        public RunOutcomePresentation(ArenaDirector world) {this.world=world;}
        public void Begin()
        {
            Cancel(); world.Players.Remove(); world.SetPaused(false);
            if(ArenaAudio.Instance!=null) ArenaAudio.Instance.StopEffects();
            reveal=world.StartCoroutine(Reveal());
        }
        IEnumerator Reveal()
        {
            // Ended runs freeze physics, so use real seconds rather than game time.
            yield return new WaitForSecondsRealtime(Mathf.Max(0,world.tuning.resultDisplayDelay));
            ResultReady=true; reveal=null;
        }
        public void Cancel()
        {
            if(reveal!=null) world.StopCoroutine(reveal);
            reveal=null; ResultReady=false;
        }
    }
}
