using UnityEngine;
namespace PhaseArena
{
    public class ArenaMinimap : MonoBehaviour
    {
        public UnityEngine.UI.RawImage image;
        public RectTransform playerMarker,clockMarker,spawnMarker;
        Texture2D texture;
        Color[] basePixels,pixels;
        float refreshAt;
        int width,height;
        readonly System.Collections.Generic.Dictionary<ShardPickup,RectTransform> fragmentMarkers=new System.Collections.Generic.Dictionary<ShardPickup,RectTransform>();
        readonly System.Collections.Generic.List<ShardPickup> expiredMarkers=new System.Collections.Generic.List<ShardPickup>();
        public int FragmentMarkerCount => fragmentMarkers.Count;
        public Vector2 MapPosition(Vector2 p)
        {
            if(image==null || width==0) return Vector2.zero;
            return new Vector2(p.x/width*image.rectTransform.rect.width,p.y/height*image.rectTransform.rect.height);
        }
        public void Rebuild()
        {
            var g=ArenaDirector.Instance; if(g==null || g.worldGenerator.Layout==null) return;
            width=g.tuning.width; height=g.tuning.height;
            if(texture!=null) Destroy(texture);
            texture=new Texture2D(width,height,TextureFormat.RGBA32,false); texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;
            basePixels=new Color[width*height]; pixels=new Color[basePixels.Length];
            for(int i=0;i<basePixels.Length;i++) basePixels[i]=new Color(.18f,.27f,.2f);
            foreach(var p in g.worldGenerator.Layout.props) Set(basePixels,p.position,p.kind==PropKind.Tree ? new Color(.29f,.44f,.23f) : new Color(.48f,.48f,.4f),1);
            image.texture=texture; image.color=Color.white; refreshAt=0; Draw();
        }
        void Set(Color[] destination,Vector2 p,Color color,int radius=0)
        {
            int cx=Mathf.FloorToInt(p.x)+width/2,cy=Mathf.FloorToInt(p.y)+height/2;
            for(int x=-radius;x<=radius;x++) for(int y=-radius;y<=radius;y++)
            {
                int px=cx+x,py=cy+y;
                if(px>=0 && px<width && py>=0 && py<height) destination[py*width+px]=color;
            }
        }
        void Draw()
        {
            var g=ArenaDirector.Instance; if(g==null || texture==null) return;
            System.Array.Copy(basePixels,pixels,pixels.Length);
            foreach(var cell in g.worldGenerator.cells.Values)
                Set(pixels,cell.transform.position,cell.rough ? new Color(.49f,.38f,.24f) : cell.phase==FloorPhase.Ice ? new Color(.58f,.84f,.93f) : cell.phase==FloorPhase.Steam ? new Color(.6f,.67f,.69f) : new Color(.11f,.45f,.65f));
            foreach(var b in g.bodies)
                if(b!=null && !b.Dead && (b.isElite || b.kind==BodyKind.Boss || (b.IsEnemy && g.player!=null && Vector2.Distance(b.Body.position,g.player.Body.position)<8)))
                    Set(pixels,b.Body.position,new Color(.95f,.3f,.27f),b.kind==BodyKind.Boss ? 1 : 0);
            foreach(var s in g.shards) if(s!=null && !s.Collected) Set(pixels,s.transform.position,new Color(1,.9f,.35f),1);
            texture.SetPixels(pixels); texture.Apply(false);
            RefreshFragmentMarkers(g);
        }
        RectTransform MarkerSquare(string name,Transform parent,float size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));
            var rt=go.GetComponent<RectTransform>(); rt.SetParent(parent,false);
            rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f; rt.sizeDelta=Vector2.one*size;
            var graphic=go.GetComponent<UnityEngine.UI.Image>(); graphic.color=color; graphic.raycastTarget=false;
            return rt;
        }
        void RefreshFragmentMarkers(ArenaDirector g)
        {
            expiredMarkers.Clear();
            foreach(var pair in fragmentMarkers)
                if(pair.Key==null || pair.Key.Collected || !pair.Key.gameObject.activeInHierarchy || !g.shards.Contains(pair.Key)) expiredMarkers.Add(pair.Key);
            foreach(var key in expiredMarkers)
            {
                fragmentMarkers[key].gameObject.SetActive(false); Destroy(fragmentMarkers[key].gameObject); fragmentMarkers.Remove(key);
            }
            foreach(var shard in g.shards)
            {
                if(shard==null || shard.Collected || !shard.gameObject.activeInHierarchy) continue;
                RectTransform marker;
                if(!fragmentMarkers.TryGetValue(shard,out marker))
                {
                    marker=MarkerSquare("Clock Fragment Marker "+shard.GetInstanceID(),image.transform,18,Color.white);
                    marker.localRotation=Quaternion.Euler(0,0,45);
                    var back=MarkerSquare("Dark Outline",marker,15,new Color(.12f,.08f,.02f));
                    MarkerSquare("Gold Fragment",back,11,new Color(1,.78f,.2f));
                    fragmentMarkers.Add(shard,marker);
                }
                marker.anchoredPosition=MapPosition(shard.transform.position);
            }
            playerMarker.SetAsLastSibling();
        }
        void Update()
        {
            var g=ArenaDirector.Instance; if(g==null || texture==null) return;
            playerMarker.gameObject.SetActive(g.player!=null);
            if(g.player!=null) playerMarker.anchoredPosition=MapPosition(g.player.Body.position);
            playerMarker.localScale=Vector3.one*(1+Mathf.Sin(Time.unscaledTime*5)*.12f);
            clockMarker.anchoredPosition=MapPosition(g.clock.position); spawnMarker.anchoredPosition=MapPosition(g.SpawnPosition);
            foreach(var pair in fragmentMarkers) pair.Value.localScale=Vector3.one*(1+Mathf.Sin(Time.unscaledTime*5)*.15f);
            if(Time.unscaledTime>refreshAt) { refreshAt=Time.unscaledTime+.25f; Draw(); }
        }
        void OnDestroy() { if(texture!=null) Destroy(texture); }
    }
}
