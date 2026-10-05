using UnityEngine;
using UnityEngine.EventSystems;
namespace PhaseArena
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class ArenaUiSound : MonoBehaviour,IPointerEnterHandler
    {
        void Awake() { GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>{if(ArenaAudio.Instance!=null) ArenaAudio.Instance.PlayUi();}); }
        public void OnPointerEnter(PointerEventData data) {if(GetComponent<UnityEngine.UI.Button>().IsInteractable() && ArenaAudio.Instance!=null) ArenaAudio.Instance.PlayUi(true);}
    }
}
