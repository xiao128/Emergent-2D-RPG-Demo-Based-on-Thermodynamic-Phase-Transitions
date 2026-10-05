using UnityEngine;
namespace PhaseArena
{
    // A single physical/visual description shared by preview, clearance and creation.
    public readonly struct ProjectileProfile
    {
        public readonly float Radius,Mass;
        public readonly Vector2 VisualSize;
        public ProjectileProfile(float radius,float mass,Vector2 visualSize)
        { Radius=radius; Mass=mass; VisualSize=visualSize; }
    }
}
