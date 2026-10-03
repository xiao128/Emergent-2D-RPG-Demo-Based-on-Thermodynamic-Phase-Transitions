using System;
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    public class WorldGenerator : MonoBehaviour
    {
        public ArenaTuning tuning;
        public ThermoBody rockPrefab, largeRockPrefab, treePrefab;
        public Sprite square, ring;
        public Material spriteMaterial;
        public PhysicsMaterial2D normalMaterial;
        public Transform generatedRoot;
        public WorldLayout Layout { get; private set; }
        public readonly Dictionary<Vector2Int,TerrainCell> cells=new Dictionary<Vector2Int,TerrainCell>();
        public readonly List<ThermoBody> props=new List<ThermoBody>();
        public Vector2 Min => new Vector2(-tuning.width*.5f,-tuning.height*.5f);
        public Vector2 Max => -Min;
        public void Generate(int seed,Vector2 spawn)
        {
            if(generatedRoot!=null)
            {
                generatedRoot.gameObject.SetActive(false);
                if(Application.isPlaying) Destroy(generatedRoot.gameObject); else DestroyImmediate(generatedRoot.gameObject);
            }
            props.Clear(); cells.Clear(); Layout=new WorldLayout(tuning,seed,spawn);
            if(!Layout.Validate()) throw new InvalidOperationException("Seeded world has an unreachable clock/guard: "+seed);
            generatedRoot=new GameObject("Generated World · "+seed).transform; generatedRoot.SetParent(transform,false);
            Draw("Meadow",generatedRoot,Vector2.zero,new Vector2(tuning.width,tuning.height),new Color(.13f,.22f,.18f),-100);
            var deco=new GameObject("Grass and Trails").transform; deco.SetParent(generatedRoot,false);
            var random=new System.Random(seed^8191);
            for(int i=0;i<420;i++)
            {
                var p=new Vector2((float)random.NextDouble()*(tuning.width-2)-tuning.width*.5f+1,(float)random.NextDouble()*(tuning.height-2)-tuning.height*.5f+1);
                Draw("Grass",deco,p,new Vector2(.045f,.19f),new Color(.24f,.35f,.22f,.65f),-94);
            }
            Wall("North",new Vector2(0,Max.y),new Vector2(tuning.width+1,1));
            Wall("South",new Vector2(0,Min.y),new Vector2(tuning.width+1,1));
            Wall("West",new Vector2(Min.x,0),new Vector2(1,tuning.height+1));
            Wall("East",new Vector2(Max.x,0),new Vector2(1,tuning.height+1));
            var terrain=new GameObject("Phase Terrain").transform; terrain.SetParent(generatedRoot,false);
            foreach(var c in Layout.water) CreateCell(c,false,terrain);
            foreach(var c in Layout.rough) CreateCell(c,true,terrain);
            var propRoot=new GameObject("Physical Ingredients").transform; propRoot.SetParent(generatedRoot,false);
            foreach(var p in Layout.props)
            {
                var prefab=p.kind==PropKind.Tree ? treePrefab : p.kind==PropKind.LargeRock ? largeRockPrefab : rockPrefab;
                var b=Instantiate(prefab,p.position,Quaternion.identity,propRoot);
                b.name=p.kind==PropKind.LargeRock ? "巨型石头 · 不可破坏" : p.kind==PropKind.Tree ? "树木 · 可破坏障碍" : p.kind.ToString(); b.baseMass=p.mass; b.radius=prefab.radius*p.scale;
                b.visual.transform.localScale=prefab.visual.transform.localScale*p.scale;
                var circle=b.GetComponent<CircleCollider2D>();
                var capsule=b.GetComponent<CapsuleCollider2D>();
                var box=b.GetComponent<BoxCollider2D>();
                if(circle!=null) circle.radius=b.radius/Mathf.Max(.01f,b.transform.lossyScale.x);
                else if(capsule!=null)
                {
                    var source=prefab.GetComponent<CapsuleCollider2D>();
                    capsule.size=source.size*p.scale; capsule.offset=source.offset*p.scale;
                }
                else if(box!=null)
                {
                    var source=prefab.GetComponent<BoxCollider2D>();
                    box.size=source.size*p.scale; box.offset=source.offset*p.scale;
                }
                b.indestructible=p.kind==PropKind.LargeRock;
                var rb=b.Body!=null ? b.Body : b.GetComponent<Rigidbody2D>();
                rb.mass=Mathf.Max(.0001f,p.mass);
                rb.bodyType=p.kind==PropKind.LargeRock || p.kind==PropKind.Tree ? RigidbodyType2D.Static : RigidbodyType2D.Dynamic;
                if(p.kind==PropKind.LargeRock || p.kind==PropKind.Tree) b.gameObject.layer=10;
                if(p.kind==PropKind.MicroRock) { b.maxHealth=160; b.health=160; b.baseColor=new Color(.77f,.75f,.55f); }
                b.healthBarSize=new Vector2(b.radius*2,.045f);
                if(b.healthFill!=null) b.healthFill.transform.parent.localPosition=new Vector2(-b.radius,b.radius+.25f);
                var back=b.transform.Find("Health Back");
                if(back!=null) { back.localPosition=new Vector2(0,b.radius+.25f); back.localScale=new Vector3(b.radius*2,.065f,1); }
                if(b.indestructible)
                {
                    if(back!=null) back.gameObject.SetActive(false);
                    if(b.healthFill!=null) b.healthFill.transform.parent.gameObject.SetActive(false);
                }
                if(b.heatRing!=null) b.heatRing.transform.localScale=Vector3.one*b.radius*2.7f;
                if(b.kind==BodyKind.Tree) b.LayoutTreeIndicators();
                props.Add(b);
                if(Application.isPlaying) b.ThermalStep(0);
            }
            Draw("Run Spawn",generatedRoot,spawn,Vector2.one*3,new Color(.3f,.82f,.6f,.7f),-70).sprite=ring;
            if(ArenaDirector.Instance!=null) ArenaDirector.Instance.terrain=new List<TerrainCell>(cells.Values).ToArray();
        }
        void CreateCell(Vector2Int c,bool rough,Transform root)
        {
            var go=new GameObject(rough ? "Rough "+c : "Water "+c); go.transform.SetParent(root,false); go.transform.position=Layout.Center(c);
            var tile=go.AddComponent<TerrainCell>(); tile.rough=rough; tile.size=Vector2.one; tile.phase=FloorPhase.Water;
            tile.surface=Draw("Surface",go.transform,Vector2.zero,Vector2.one,rough ? new Color(.38f,.31f,.21f) : new Color(.1f,.39f,.55f),-80);
            if(!rough)
            {
                var barrier=new GameObject("Water Blocker",typeof(BoxCollider2D)); barrier.layer=12; barrier.transform.SetParent(go.transform,false);
                barrier.GetComponent<BoxCollider2D>().size=Vector2.one; tile.waterBlocker=barrier;
            }
            else Draw("Grain",go.transform,new Vector2(.12f,.07f),new Vector2(.16f,.025f),new Color(.57f,.45f,.29f),-79);
            cells.Add(c,tile);
        }
        void Wall(string name,Vector2 p,Vector2 size)
        {
            var go=new GameObject(name); go.layer=10; go.transform.SetParent(generatedRoot,false); go.transform.position=p;
            var rb=go.AddComponent<Rigidbody2D>(); rb.bodyType=RigidbodyType2D.Static; rb.gravityScale=0;
            var col=go.AddComponent<BoxCollider2D>(); col.size=size; col.sharedMaterial=normalMaterial;
            var body=go.AddComponent<ThermoBody>(); body.kind=BodyKind.StaticObstacle; body.baseMass=12; body.indestructible=true;
            body.visual=Draw("Stone Boundary",go.transform,Vector2.zero,size,new Color(.32f,.37f,.31f),-50); body.baseColor=new Color(.32f,.37f,.31f);
        }
        SpriteRenderer Draw(string name,Transform parent,Vector2 p,Vector2 size,Color color,int order)
        {
            var go=new GameObject(name,typeof(SpriteRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=p; go.transform.localScale=new Vector3(size.x,size.y,1);
            var sr=go.GetComponent<SpriteRenderer>(); sr.sprite=square; sr.sharedMaterial=spriteMaterial; sr.color=color; sr.sortingOrder=order; return sr;
        }
        public TerrainCell FloorAt(Vector2 p)
        {
            TerrainCell result; cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y)),out result); return result;
        }
        public TerrainCell WetAt(Vector2 p,float radius,out Vector2 contact)
        {
            contact=p; float best=float.PositiveInfinity; TerrainCell result=null;
            var c=new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y)); int reach=Mathf.CeilToInt(radius)+1;
            for(int x=-reach;x<=reach;x++) for(int y=-reach;y<=reach;y++)
            {
                TerrainCell tile;
                if(!cells.TryGetValue(c+new Vector2Int(x,y),out tile) || tile.rough || tile.phase==FloorPhase.Steam) continue;
                Vector2 mid=tile.transform.position;
                Vector2 closest=new Vector2(Mathf.Clamp(p.x,mid.x-.5f,mid.x+.5f),Mathf.Clamp(p.y,mid.y-.5f,mid.y+.5f));
                float d=(closest-p).sqrMagnitude;
                if(d<=radius*radius+.01f && d<best) { best=d; contact=closest; result=tile; }
            }
            return result;
        }
        public Vector2 SafeDryPosition(Vector2 p,float radius=.6f) => Layout==null ? p : Layout.NearestDry(p,radius);
    }
}
