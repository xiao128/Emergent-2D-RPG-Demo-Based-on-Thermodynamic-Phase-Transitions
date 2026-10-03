using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    internal static class SpriteGeometry
    {
        static readonly Dictionary<Sprite,Bounds> cache=new Dictionary<Sprite,Bounds>();
        public static float VisibleTop(SpriteRenderer renderer)
        {
            var sprite=renderer.sprite;
            if(sprite==null) return renderer.bounds.max.y;
            Bounds bounds;
            if(!cache.TryGetValue(sprite,out bounds))
            {
                var vertices=sprite.vertices;
                bounds=vertices.Length>0 ? new Bounds(vertices[0],Vector3.zero) : sprite.bounds;
                foreach(var vertex in vertices) bounds.Encapsulate(vertex);
                cache.Add(sprite,bounds);
            }
            float top=float.MinValue;
            for(int x=0;x<2;x++) for(int y=0;y<2;y++)
                top=Mathf.Max(top,renderer.transform.TransformPoint(new Vector3(x==0 ? bounds.min.x : bounds.max.x,y==0 ? bounds.min.y : bounds.max.y)).y);
            return top;
        }
    }
}
