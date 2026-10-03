from pathlib import Path
import re

root = Path(__file__).resolve().parents[1] / 'Assets/ArenaDemo/Scripts'
path = root / 'ArenaDirector.cs'
text = path.read_text(encoding='utf-8-sig')

def take(name):
    global text
    pattern = r'^        (?:public |private |static )*(?:void|bool|float|string|ThermoBody|Vector2|IEnumerator) '+name+r'\([^\n]*\)\s*\{'
    m = re.search(pattern, text, re.M)
    if not m:
        raise RuntimeError(name)
    start = m.start()
    opening = text.index('{', m.start())
    depth = 1
    end = opening+1
    while depth:
        if text[end] == '{': depth += 1
        if text[end] == '}': depth -= 1
        end += 1
    result = text[start:end]
    text = text[:start]+text[end:]
    return result

def write(name, header, methods):
    (root / (name+'.cs')).write_text('using System;\nusing System.Collections.Generic;\nusing UnityEngine;\nnamespace PhaseArena\n{\n'+header+'\n'+ '\n'.join(methods)+'\n    }\n}\n',encoding='utf-8')

catalog = [take('LawName'), take('LawDescription')]
write('WorldLawCatalog','    public static class WorldLawCatalog\n    {\n        public static bool IsAvailable(WorldLaw law) => law!=WorldLaw.ThermalMass && law!=WorldLaw.ThermalArc && law!=WorldLaw.ThermalShock;',catalog)
context = '''        readonly ArenaDirector world;
        ArenaTuning tuning => world.tuning;
        List<ThermoBody> bodies => world.bodies;
        RunMetrics metrics => world.metrics;
        WorldGenerator worldGenerator => world.worldGenerator;
        bool Has(WorldLaw law) => world.Has(law);
        void Pulse(Vector2 p,Color c,float radius,float duration=.45f) => world.Pulse(p,c,radius,duration);
        void Feedback(Vector2 p,string s,Color c) => world.Feedback(p,s,c);'''
collision_names = ['ResolveCollision','DeliverProjectileHeat','SteamWave','ImpactDamage','ApplyHeatBlast','Fission','React']
collision = [take(n) for n in collision_names]
collision = [s.replace('!SimulationActive','!world.SimulationActive').replace('var t=Instance!=null ? Instance.tuning : null;', 'var t=ArenaDirector.Instance!=null ? ArenaDirector.Instance.tuning : null;') for s in collision]
write('CollisionRules','    public sealed class CollisionRules\n    {\n'+context+'\n        readonly Dictionary<long,float> impactAt=new Dictionary<long,float>();\n        public CollisionRules(ArenaDirector world) { this.world=world; }\n        public void Clear() { impactAt.Clear(); }',collision)
thermal = [take('ThermalTick').replace('void ThermalTick','public void Tick'),take('ApplyRepulsion').replace('void ApplyRepulsion','public void ApplyRepulsion')]
write('WorldLawSimulation','    public sealed class WorldLawSimulation\n    {\n'+context+'''\n        readonly Dictionary<Vector2Int,List<ThermoBody>> thermalGrid=new Dictionary<Vector2Int,List<ThermoBody>>();
        readonly List<List<ThermoBody>> gridPool=new List<List<ThermoBody>>();
        TerrainCell[] terrain => world.terrain;
        TerrainCell FloorAt(Vector2 p) => world.FloorAt(p);
        public WorldLawSimulation(ArenaDirector world) { this.world=world; }''',thermal)
text = re.sub(r'^        readonly Dictionary<long,float> impactAt=.*\n', '', text,flags=re.M)
text = re.sub(r'^        readonly Dictionary<Vector2Int,List<ThermoBody>> thermalGrid=.*\n', '', text,flags=re.M)
text = re.sub(r'^        readonly List<List<ThermoBody>> gridPool=.*\n', '', text,flags=re.M)
text = text.replace('shards.Clear(); impactAt.Clear();','shards.Clear(); Collisions.Clear();')
text = text.replace('law!=WorldLaw.ThermalMass && law!=WorldLaw.ThermalArc && law!=WorldLaw.ThermalShock && !Has(law)','WorldLawCatalog.IsAvailable(law) && !Has(law)')
forward = '''
        CollisionRules collisionRules;
        WorldLawSimulation lawSimulation;
        public CollisionRules Collisions => collisionRules ?? (collisionRules=new CollisionRules(this));
        public WorldLawSimulation LawSimulation => lawSimulation ?? (lawSimulation=new WorldLawSimulation(this));
        void ThermalTick(float dt) => LawSimulation.Tick(dt);
        public void ResolveCollision(ThermoBody a,ThermoBody b,float speed,Vector2 point,float impulse=0) => Collisions.ResolveCollision(a,b,speed,point,impulse);
        public void DeliverProjectileHeat(ThermoBody b,Vector2 p) => Collisions.DeliverProjectileHeat(b,p);
        public void SteamWave(ThermoBody b,float cooling) => Collisions.SteamWave(b,cooling);
        public static float ImpactDamage(float mass,float speed) => CollisionRules.ImpactDamage(mass,speed);
        public void ApplyHeatBlast(Vector2 p,float heat,float radius,ThermoBody source) => Collisions.ApplyHeatBlast(p,heat,radius,source);
        public void Fission(ThermoBody source,float energy) => Collisions.Fission(source,energy);
        public static string LawName(WorldLaw law) => WorldLawCatalog.LawName(law);
        public static string LawDescription(WorldLaw law) => WorldLawCatalog.LawDescription(law);
'''
end = text.rfind('    }')
text = text[:end]+forward+text[end:]
text = re.sub(r'\n[ \t]*\n(?:[ \t]*\n)+','\n\n',text)
path.write_text(text,encoding='utf-8')
print('Extracted law catalog, collision rules and world law simulation. Director:',len(text.splitlines()),'lines')
