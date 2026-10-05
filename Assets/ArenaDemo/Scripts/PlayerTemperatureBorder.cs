using UnityEngine;
namespace PhaseArena
{
    // Persistent temperature cue, independent of injury laws and low-health damage feedback.
    public sealed class PlayerTemperatureBorder : MonoBehaviour
    {
        [Min(1)] public float thicknessPixels=2;
        public Color hotColor=new Color(1,.2f,.12f,.9f), coldColor=new Color(.2f,.65f,1,.9f);
        readonly UnityEngine.UI.Image[] edges=new UnityEngine.UI.Image[4];
        Canvas canvas;
        public static void Ensure(Canvas parent)
        {
            if(parent==null || parent.GetComponentInChildren<PlayerTemperatureBorder>(true)!=null) return;
            var go=new GameObject("Temperature Screen Border",typeof(RectTransform)); go.layer=parent.gameObject.layer;
            var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent.transform,false);
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            var damage=parent.GetComponentInChildren<PlayerScreenFeedback>(true);
            rect.SetSiblingIndex(damage!=null ? damage.transform.GetSiblingIndex()+1 : 0);
            go.AddComponent<PlayerTemperatureBorder>();
        }
        void Awake()
        {
            canvas=GetComponentInParent<Canvas>();
            edges[0]=MakeEdge("Left",new Vector2(0,0),new Vector2(0,1),new Vector2(0,.5f));
            edges[1]=MakeEdge("Right",new Vector2(1,0),new Vector2(1,1),new Vector2(1,.5f));
            edges[2]=MakeEdge("Top",new Vector2(0,1),new Vector2(1,1),new Vector2(.5f,1));
            edges[3]=MakeEdge("Bottom",new Vector2(0,0),new Vector2(1,0),new Vector2(.5f,0));
        }
        UnityEngine.UI.Image MakeEdge(string name,Vector2 min,Vector2 max,Vector2 pivot)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.layer=gameObject.layer;
            var rect=go.GetComponent<RectTransform>(); rect.SetParent(transform,false);
            rect.anchorMin=min; rect.anchorMax=max; rect.pivot=pivot;
            var image=go.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget=false; image.enabled=false;
            return image;
        }
        public void Refresh()
        {
            var world=ArenaDirector.Instance;
            var player=world!=null ? world.player : null;
            bool active=player!=null && !player.Dead && world.State!=RunState.Title && world.State!=RunState.Victory && world.State!=RunState.Defeat;
            int state=active ? player.temperature>world.tuning.burnTemperature ? 1 : player.temperature<world.tuning.frostTemperature ? -1 : 0 : 0;
            float width=Mathf.Max(1,thicknessPixels)/Mathf.Max(.01f,canvas!=null ? canvas.scaleFactor : 1);
            for(int i=0;i<edges.Length;i++)
            {
                edges[i].rectTransform.sizeDelta=i<2 ? new Vector2(width,0) : new Vector2(0,width);
                edges[i].color=state>0 ? hotColor : coldColor;
                edges[i].enabled=state!=0;
            }
        }
        void LateUpdate() { Refresh(); }
    }
}
