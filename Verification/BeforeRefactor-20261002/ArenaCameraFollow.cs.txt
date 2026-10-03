using UnityEngine;
namespace PhaseArena
{
    [RequireComponent(typeof(Camera))]
    public class ArenaCameraFollow : MonoBehaviour
    {
        public Transform target;
        public ArenaTuning tuning;
        Camera view;
        void Awake() { view=GetComponent<Camera>(); }
        Vector3 Desired()
        {
            if(view==null) view=GetComponent<Camera>();
            if(target==null || tuning==null) return transform.position;
            float halfY=view.orthographicSize,halfX=halfY*view.aspect;
            return new Vector3(Mathf.Clamp(target.position.x,-tuning.width*.5f+halfX,tuning.width*.5f-halfX),
                Mathf.Clamp(target.position.y,-tuning.height*.5f+halfY,tuning.height*.5f-halfY),-10);
        }
        public void Snap() { transform.position=Desired(); }
        void LateUpdate() { transform.position=Vector3.Lerp(transform.position,Desired(),1-Mathf.Exp(-12*Time.unscaledDeltaTime)); }
    }
}
