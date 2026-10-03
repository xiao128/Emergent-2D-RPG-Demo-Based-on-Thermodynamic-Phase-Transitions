using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhaseArena
{
    public enum PropKind { Rock, Tree, LargeRock, MicroRock }
    [Serializable] public struct PropPlacement
    {
        public PropKind kind;
        public Vector2 position;
        public float scale, mass;
        public PropPlacement(PropKind k, Vector2 p, float s, float m) { kind=k; position=p; scale=s; mass=m; }
    }

    // Pure seeded layout. Used by the live generator and connectivity acceptance checks.
    public sealed class WorldLayout
    {
        public readonly int width, height, seed;
        public readonly Vector2 spawn, clock;
        public readonly Vector2[] guards;
        public readonly HashSet<Vector2Int> water = new HashSet<Vector2Int>();
        public readonly HashSet<Vector2Int> rough = new HashSet<Vector2Int>();
        public readonly List<PropPlacement> props = new List<PropPlacement>();
        public readonly List<Vector2> monsters = new List<Vector2>();
        public readonly HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
        readonly System.Random rng;
        public Vector2 Min => new Vector2(-width*.5f,-height*.5f);
        public Vector2 Max => -Min;
        public Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y));
        public Vector2 Center(Vector2Int c) => new Vector2(c.x+.5f,c.y+.5f);
        public static Vector2 SpawnAt(ArenaTuning t) => new Vector2(-t.width*.5f+t.edgeInset,0);
        public static Vector2 ClockAt(ArenaTuning t) => new Vector2(t.width*.5f-t.edgeInset,0);

        public WorldLayout(ArenaTuning tuning, int layoutSeed, Vector2 start)
        {
            width=tuning.width; height=tuning.height; seed=layoutSeed; spawn=start;
            clock=ClockAt(tuning); guards=new[]{clock+new Vector2(-4,-3),clock+new Vector2(-4,3)};
            rng=new System.Random(seed);
            // One continuous river in the upper middle. Its footprint never
            // changes with the seed; only its water/ice/steam state changes.
            int riverBottom=tuning.riverCenterY-tuning.riverWidth/2;
            for(int x=-width/2;x<width/2;x++)
                for(int y=riverBottom;y<riverBottom+tuning.riverWidth;y++) water.Add(new Vector2Int(x,y));
            for(int n=0;n<12;n++)
            {
                Vector2 p=n<4 ? guards[n%2]+new Vector2(-2,n<2 ? -2 : 2) : RandomPoint(4);
                for(int x=-2;x<=2;x++) for(int y=-1;y<=1;y++)
                {
                    var c=Cell(p)+new Vector2Int(x,y);
                    if(InBounds(c) && !water.Contains(c)) rough.Add(c);
                }
            }
            // Both guard approaches contain reusable physical ingredients in every world.
            foreach(int side in new[]{-1,1})
            {
                Vector2 g=guards[side<0 ? 0 : 1];
                AddProp(PropKind.Rock,g+new Vector2(-2,-side*1.8f),1,1.05f);
                AddProp(PropKind.Rock,g+new Vector2(-4,side*1.8f),1.1f,1.3f);
                AddProp(PropKind.Rock,g+new Vector2(-1,side*2.7f),.9f,.85f);
                AddProp(PropKind.MicroRock,g+new Vector2(-5,0),.55f,1);
                AddProp(PropKind.MicroRock,g+new Vector2(-6,side*2),.55f,1);
                AddProp(PropKind.LargeRock,g+new Vector2(7,0),2.2f,12);
            }
            PlaceRandom(PropKind.Rock,tuning.rockCount-6);
            PlaceRandom(PropKind.Tree,tuning.treeCount);
            PlaceRandom(PropKind.LargeRock,tuning.largeRockCount-2);
            BuildReachability();
            // Spread the reduced population across the accessible south bank.
            for(int row=0;row<4;row++) for(int col=0;col<5 && monsters.Count<tuning.roamingCount;col++)
            {
                Vector2 p=new Vector2(-width*.5f+6+col*(width-12)/4f+(float)(rng.NextDouble()-.5)*3,
                    -height*.5f+6+row*(riverBottom+height*.5f-9)/3f+(float)(rng.NextDouble()-.5)*2);
                p=NearestDry(p,1);
                if(Vector2.Distance(p,clock)<8 || Vector2.Distance(p,spawn)<5)
                {
                    for(int attempt=0;attempt<80;attempt++)
                    {
                        p=NearestDry(RandomPoint(3),1);
                        if(Vector2.Distance(p,clock)>=8 && Vector2.Distance(p,spawn)>=5 && monsters.TrueForAll(m=>Vector2.Distance(m,p)>5)) break;
                    }
                }
                if(monsters.TrueForAll(m=>Vector2.Distance(m,p)>3.5f)) monsters.Add(p);
            }
            for(int attempt=0;attempt<2000 && monsters.Count<tuning.roamingCount;attempt++)
            {
                Vector2 p=NearestDry(RandomPoint(3),1);
                if(Vector2.Distance(p,clock)>8 && Vector2.Distance(p,spawn)>5 && monsters.TrueForAll(m=>Vector2.Distance(m,p)>3.5f)) monsters.Add(p);
            }
            if(monsters.Count>tuning.roamingCount) monsters.RemoveRange(tuning.roamingCount,monsters.Count-tuning.roamingCount);
        }
        float DistanceToSegment(Vector2 p,Vector2 a,Vector2 b)
        {
            Vector2 d=b-a; float t=d.sqrMagnitude>.001f ? Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude) : 0;
            return Vector2.Distance(p,a+d*t);
        }
        bool Reserved(Vector2 p,float margin)
        {
            return Vector2.Distance(p,clock)<2.8f+margin || Vector2.Distance(p,spawn)<3+margin
                || DistanceToSegment(p,spawn,clock)<1.5f+margin
                || DistanceToSegment(p,clock,guards[0])<1.25f+margin || DistanceToSegment(p,clock,guards[1])<1.25f+margin
                || Vector2.Distance(p,guards[0])<1.4f+margin || Vector2.Distance(p,guards[1])<1.4f+margin;
        }
        bool InBounds(Vector2Int c) => c.x>-width/2 && c.x<width/2-1 && c.y>-height/2 && c.y<height/2-1;
        Vector2 RandomPoint(float margin) => new Vector2((float)rng.NextDouble()*(width-2*margin)-width*.5f+margin,(float)rng.NextDouble()*(height-2*margin)-height*.5f+margin);
        bool ClearForProp(Vector2 p,float radius)
        {
            if(Reserved(p,radius+.5f)) return false;
            foreach(var c in water) if(Vector2.Distance(Center(c),p)<radius+1) return false;
            foreach(var a in props) if(Vector2.Distance(a.position,p)<radius+a.scale*(a.kind==PropKind.LargeRock ? .7f : .5f)+1) return false;
            return true;
        }
        void PlaceRandom(PropKind kind,int count)
        {
            for(int i=0;i<count;i++)
            {
                float scale=kind==PropKind.Rock ? .85f+(float)rng.NextDouble()*.7f : kind==PropKind.Tree ? 1.65f+(float)rng.NextDouble()*.45f : 1.9f+(float)rng.NextDouble()*.6f;
                for(int attempt=0;attempt<160;attempt++)
                {
                    Vector2 p=RandomPoint(3);
                    if(ClearForProp(p,scale*(kind==PropKind.LargeRock ? .7f : .5f)))
                    {
                        if(AddProp(kind,p,scale,kind==PropKind.Rock ? Mathf.Clamp(scale*scale*1.05f,.8f,2.4f) : kind==PropKind.Tree ? 3 : 12)) break;
                    }
                }
            }
        }
        bool AddProp(PropKind kind,Vector2 p,float scale,float mass)
        {
            // Props never carve holes in the permanent river.
            foreach(var c in water) if(Vector2.Distance(Center(c),p)<scale*.7f+.8f) return false;
            props.Add(new PropPlacement(kind,p,scale,mass));
            return true;
        }
        void BuildReachability()
        {
            blocked.Clear(); reachable.Clear();
            foreach(var c in water)
                for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++)
                    if(Mathf.Abs(x)+Mathf.Abs(y)<=1) blocked.Add(c+new Vector2Int(x,y));
            foreach(var p in props)
            {
                if(p.kind!=PropKind.Tree && p.kind!=PropKind.LargeRock) continue;
                int r=Mathf.CeilToInt(p.scale*.7f+.6f); var c=Cell(p.position);
                for(int x=-r;x<=r;x++) for(int y=-r;y<=r;y++)
                    if(Vector2.Distance(Center(c+new Vector2Int(x,y)),p.position)<p.scale*.7f+.85f) blocked.Add(c+new Vector2Int(x,y));
            }
            var queue=new Queue<Vector2Int>(); var first=Cell(spawn);
            queue.Enqueue(first); reachable.Add(first);
            Vector2Int[] steps={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(queue.Count>0)
            {
                var c=queue.Dequeue();
                foreach(var d in steps)
                {
                    var next=c+d;
                    if(InBounds(next) && !blocked.Contains(next) && reachable.Add(next)) queue.Enqueue(next);
                }
            }
        }
        public bool IsReachable(Vector2 p) => reachable.Contains(Cell(p));
        public bool IsDry(Vector2 p) => IsReachable(p) && !water.Contains(Cell(p));
        public Vector2 NearestDry(Vector2 p,float clearance=.6f)
        {
            float best=float.PositiveInfinity; Vector2 result=spawn;
            var origin=Cell(p);
            for(int ring=0;ring<width+height;ring++)
            {
                for(int x=-ring;x<=ring;x++) for(int y=-ring;y<=ring;y++)
                {
                    if(ring>0 && Mathf.Abs(x)!=ring && Mathf.Abs(y)!=ring) continue;
                    var c=origin+new Vector2Int(x,y);
                    if(!reachable.Contains(c)) continue;
                    Vector2 q=Center(c); bool clear=true;
                    foreach(var prop in props)
                        if(Vector2.Distance(prop.position,q)<clearance+(prop.kind==PropKind.LargeRock ? .7f : .4f)*prop.scale) { clear=false; break; }
                    if(!clear) continue;
                    float d=(q-p).sqrMagnitude;
                    if(d<best) { best=d; result=q; }
                }
                if(best<float.PositiveInfinity && ring*ring>best+3) break;
            }
            return result;
        }
        public bool Validate()
        {
            if(!IsReachable(clock) || !IsReachable(guards[0]) || !IsReachable(guards[1])) return false;
            foreach(var p in monsters) if(!IsDry(p)) return false;
            return reachable.Count>width*height*.45f;
        }
    }
}
