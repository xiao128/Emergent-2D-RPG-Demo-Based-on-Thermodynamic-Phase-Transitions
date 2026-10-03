from pathlib import Path
import re
base=Path(__file__).resolve().parents[1]
text=(base/'Verification/BeforeRefactor-20261002/ArenaDirector.cs.txt').read_text(encoding='utf-8-sig')
methods=[]
for name in ['ThermalTick','ApplyRepulsion']:
    m=re.search(r'^        void '+name+r'\([^\n]*\)\s*\{',text,re.M)
    start=m.start();end=text.index('{',start)+1;depth=1
    while depth:
        if text[end]=='{':depth+=1
        if text[end]=='}':depth-=1
        end+=1
    methods.append(text[start:end].replace('void ThermalTick','public void Tick'))
header='''#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    // Frozen pre-refactor tick for comparative CPU benchmarks only.
    public sealed class LegacyLawTickBenchmark
    {
        readonly ArenaDirector world;
        ArenaTuning tuning => world.tuning;
        List<ThermoBody> bodies => world.bodies;
        RunMetrics metrics => world.metrics;
        TerrainCell[] terrain => world.terrain;
        bool Has(WorldLaw law) => world.Has(law);
        TerrainCell FloorAt(Vector2 p) => world.FloorAt(p);
        void Feedback(Vector2 p,string s,Color c) => world.Feedback(p,s,c);
        readonly Dictionary<Vector2Int,List<ThermoBody>> thermalGrid=new Dictionary<Vector2Int,List<ThermoBody>>();
        readonly List<List<ThermoBody>> gridPool=new List<List<ThermoBody>>();
        public LegacyLawTickBenchmark(ArenaDirector world) { this.world=world; }
'''
(base/'Assets/ArenaDemo/Tests/LegacyLawTickBenchmark.cs').write_text(header+'\n'.join(methods)+'\n    }\n}\n#endif\n',encoding='utf-8')
