using UnityEngine;

namespace PhaseArena
{
    public class ArenaPulse : MonoBehaviour
    {
        public float duration = .45f, radius = 1;
        public Color color = Color.white;
        public SpriteRenderer visual;
        float elapsed;
        void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.one * Mathf.Lerp(.12f,radius*2,t);
            if (visual != null) visual.color = new Color(color.r,color.g,color.b,(1-t)*.7f);
            if (t >= 1) Destroy(gameObject);
        }
    }
}
