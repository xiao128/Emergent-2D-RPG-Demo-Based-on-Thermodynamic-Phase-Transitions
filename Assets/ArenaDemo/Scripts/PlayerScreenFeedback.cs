using UnityEngine;
namespace PhaseArena
{
    // UI-only damage border. Generated once, with a transparent central view.
    [RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class PlayerScreenFeedback : MonoBehaviour
    {
        UnityEngine.UI.RawImage border;
        Texture2D texture;
        PlayerHitFeedback tracked;
        public static void Ensure(Canvas canvas)
        {
            if(canvas==null || canvas.GetComponentInChildren<PlayerScreenFeedback>(true)!=null) return;
            var go=new GameObject("Damage Edge Overlay",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
            go.layer=canvas.gameObject.layer;
            var rect=go.GetComponent<RectTransform>(); rect.SetParent(canvas.transform,false);
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            rect.SetAsFirstSibling();
            var image=go.GetComponent<UnityEngine.UI.RawImage>(); image.raycastTarget=false; image.color=Color.clear;
            go.AddComponent<PlayerScreenFeedback>();
        }
        void Awake()
        {
            border=GetComponent<UnityEngine.UI.RawImage>(); border.raycastTarget=false;
            const int size=128;
            texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name="Runtime damage edge"; texture.wrapMode=TextureWrapMode.Clamp; texture.filterMode=FilterMode.Bilinear;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float edge=Mathf.Max(Mathf.Abs(2f*x/(size-1)-1),Mathf.Abs(2f*y/(size-1)-1));
                byte alpha=(byte)(255*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,edge)));
                pixels[y*size+x]=new Color32(0,0,0,alpha);
            }
            texture.SetPixels32(pixels); texture.Apply(false,true); border.texture=texture;
            border.color=Color.clear; border.enabled=false;
        }
        void LateUpdate()
        {
            var world=ArenaDirector.Instance;
            if(world!=null && world.player!=null && (tracked==null || tracked.gameObject!=world.player.gameObject))
                tracked=world.player.GetComponent<PlayerHitFeedback>();
            float alpha=world!=null && tracked!=null ? tracked.EdgeOpacity : 0;
            border.enabled=alpha>0; border.color=new Color(0,0,0,alpha);
        }
        void OnDestroy() { if(texture!=null) Destroy(texture); }
    }
}
