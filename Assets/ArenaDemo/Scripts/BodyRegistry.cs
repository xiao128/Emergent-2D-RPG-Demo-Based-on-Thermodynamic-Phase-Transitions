using System.Collections.Generic;
namespace PhaseArena
{
    // Registration and projectile creation order are maintained once, not rebuilt per shot.
    public sealed class BodyRegistry
    {
        public readonly List<ThermoBody> Bodies=new List<ThermoBody>();
        readonly HashSet<ThermoBody> membership=new HashSet<ThermoBody>();
        readonly LinkedList<ThermoBody> projectiles=new LinkedList<ThermoBody>();
        readonly Dictionary<ThermoBody,LinkedListNode<ThermoBody>> nodes=new Dictionary<ThermoBody,LinkedListNode<ThermoBody>>();
        readonly Dictionary<UnityEngine.Collider2D,ThermoBody> colliderOwners=new Dictionary<UnityEngine.Collider2D,ThermoBody>();
        public ThermoBody BodyFor(UnityEngine.Collider2D collider)
        {
            ThermoBody body;
            return colliderOwners.TryGetValue(collider,out body) ? body : null;
        }
        public void Register(ThermoBody body)
        {
            if(body==null || !membership.Add(body)) return;
            Bodies.Add(body);
            var collider=body.Collider;
            if(collider!=null) colliderOwners[collider]=body;
            if(body.kind==BodyKind.Projectile) nodes.Add(body,projectiles.AddLast(body));
        }
        public void Unregister(ThermoBody body)
        {
            if(!membership.Remove(body)) return;
            Bodies.Remove(body);
            if(body.Collider!=null) colliderOwners.Remove(body.Collider);
            LinkedListNode<ThermoBody> node;
            if(nodes.TryGetValue(body,out node)) { projectiles.Remove(node); nodes.Remove(body); }
        }
        public ThermoBody OldestProjectile
        {
            get
            {
                while(projectiles.First!=null)
                {
                    var body=projectiles.First.Value;
                    if(body!=null && !body.Dead && body.gameObject.activeInHierarchy) return body;
                    nodes.Remove(body); projectiles.RemoveFirst();
                }
                return null;
            }
        }
        public int ProjectileCount { get { var oldest=OldestProjectile; return projectiles.Count; } }
    }
}
