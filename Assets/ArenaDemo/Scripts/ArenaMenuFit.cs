using UnityEngine;
namespace PhaseArena
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class ArenaMenuFit : MonoBehaviour
    {
        // Menu coordinates stay readable at the authored size, then shrink only if necessary.
        public Vector2 authoredSize=new Vector2(900,900);
        void LateUpdate()
        {
            var parent=transform.parent as RectTransform; if(parent==null) return;
            float scale=Mathf.Min(1,(parent.rect.width-24)/authoredSize.x,(parent.rect.height-24)/authoredSize.y);
            var desired=Vector3.one*Mathf.Max(.01f,scale);
            if((transform.localScale-desired).sqrMagnitude>.000001f) transform.localScale=desired;
        }
    }
}
