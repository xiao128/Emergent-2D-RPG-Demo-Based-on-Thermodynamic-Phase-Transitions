using UnityEngine;
namespace PhaseArena
{
    // Runtime-only template preserves scene overrides when the actual player is destroyed.
    public sealed class ArenaPlayerLifecycle
    {
        readonly ArenaDirector world;
        GameObject template;
        Transform originalParent;
        string originalName;
        public ArenaPlayerLifecycle(ArenaDirector world) {this.world=world;}
        public void Capture()
        {
            if(template!=null || world.player==null) return;
            originalParent=world.player.transform.parent; originalName=world.player.name;
            var holder=new GameObject("Initial Player Configuration"); holder.transform.SetParent(world.transform,false); holder.SetActive(false);
            template=Object.Instantiate(world.player.gameObject,holder.transform);
            template.name="Player Template"; template.SetActive(false);
        }
        public void Remove()
        {
            if(world.player==null) return;
            var follow=Camera.main!=null ? Camera.main.GetComponent<ArenaCameraFollow>() : null;
            if(follow!=null) {follow.Snap(); follow.target=null;}
            world.player.GetComponent<PlayerMage>().ResetTools();
            var go=world.player.gameObject; world.player=null;
            go.SetActive(false); Object.Destroy(go);
        }
        public void Restore()
        {
            if(world.player!=null) return;
            var go=Object.Instantiate(template,originalParent); go.name=originalName;
            world.player=go.GetComponent<ThermoBody>();
            go.transform.position=world.SpawnPosition; go.SetActive(true);
            var follow=Camera.main!=null ? Camera.main.GetComponent<ArenaCameraFollow>() : null;
            if(follow!=null) follow.target=go.transform;
        }
    }
}
