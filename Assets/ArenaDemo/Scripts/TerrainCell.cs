using UnityEngine;

namespace PhaseArena
{
    public enum FloorPhase { Water, Ice, Steam }
    public class TerrainCell : MonoBehaviour
    {
        public float temperature = 20;
        public bool rough;
        public FloorPhase phase;
        public SpriteRenderer surface;
        public GameObject waterBlocker;
        public Vector2 size = Vector2.one;
        public float HeatCapacity => ArenaDirector.Instance!=null ? Mathf.Max(ArenaDirector.Instance.tuning.minimumHeatCapacity,ArenaDirector.Instance.tuning.terrainThermalMass*ArenaDirector.Instance.tuning.specificHeatCapacity) : 4;
        public bool Contains(Vector2 p) => Mathf.Abs(p.x - transform.position.x) <= size.x / 2 && Mathf.Abs(p.y - transform.position.y) <= size.y / 2;
        public void AddHeat(float amount)
        {
            if(rough) return;
            var t=ArenaDirector.Instance!=null ? ArenaDirector.Instance.tuning : null;
            temperature=Mathf.Clamp(temperature+amount/HeatCapacity,t!=null ? t.minimumTemperature : -1000,t!=null ? t.maximumTemperature : 1000);
            Refresh();
        }
        public void Tick(float dt) { if (!rough) { temperature = Mathf.MoveTowards(temperature,20,1.2f*dt); Refresh(); } }
        public void Refresh()
        {
            if (rough) return;
            FloorPhase old = phase;
            if (temperature <= 0) phase = FloorPhase.Ice;
            else if (temperature > 100) phase = FloorPhase.Steam;
            else if (phase == FloorPhase.Ice && temperature < 60) phase = FloorPhase.Ice;
            else if (phase == FloorPhase.Steam && temperature >= 75) phase = FloorPhase.Steam;
            else phase = FloorPhase.Water;
            if (surface != null) surface.color = phase == FloorPhase.Ice ? new Color(.55f,.85f,.94f) : phase == FloorPhase.Steam ? new Color(.64f,.76f,.8f,.65f) : new Color(.1f,.4f,.6f);
            if (waterBlocker != null) waterBlocker.SetActive(phase != FloorPhase.Ice);
            if (old == FloorPhase.Ice && phase != FloorPhase.Ice && ArenaDirector.Instance != null)
                ArenaDirector.Instance.OnIceMelted(this);
        }
    }
}
