from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]/'Assets/ArenaDemo/Scripts'
path=root/'ArenaDirector.cs'
text=path.read_text(encoding='utf-8-sig')
methods=[]
for name in ['Shoot','ProjectileSpawnPosition','TryGetProjectileSpawnPosition','RestoreCasterCollision','CreateShields','FormShields','Melee']:
    m=re.search(r'^        (?:public )?(?:ThermoBody|Vector2|bool|void|IEnumerator) '+name+r'\([^\n]*\)\s*\{',text,re.M)
    if not m: raise RuntimeError(name)
    start=m.start(); end=text.index('{',start)+1; depth=1
    while depth:
        if text[end]=='{': depth+=1
        if text[end]=='}': depth-=1
        end+=1
    method=text[start:end]
    method=re.sub(r'\bInstantiate\(', 'UnityEngine.Object.Instantiate(',method)
    method=re.sub(r'\bDestroy\(', 'UnityEngine.Object.Destroy(',method)
    method=method.replace('StartCoroutine(', 'world.StartCoroutine(').replace('!SimulationActive','!world.SimulationActive')
    methods.append(method)
    text=text[:start]+text[end:]
header='''using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    // Tool creation, clearance and physical impulses. Run progression stays in the director.
    public sealed class ArenaTools
    {
        readonly ArenaDirector world;
        public ArenaTools(ArenaDirector world) { this.world=world; }
        ArenaTuning tuning => world.tuning;
        BodyRegistry registry => world.Registry;
        List<ThermoBody> bodies => world.bodies;
        ThermoBody projectilePrefab => world.projectilePrefab;
        ThermoBody shieldPrefab => world.shieldPrefab;
        Transform entities => world.entities;
        Sprite fireSprite => world.fireSprite;
        Sprite iceSprite => world.iceSprite;
        RunMetrics metrics => world.metrics;
        void Pulse(Vector2 p,Color c,float radius,float duration=.45f) => world.Pulse(p,c,radius,duration);
        void Feedback(Vector2 p,string s,Color c) => world.Feedback(p,s,c);
'''
(root/'ArenaTools.cs').write_text(header+'\n'.join(methods)+'\n    }\n}\n',encoding='utf-8')
forward='''
        ArenaTools tools;
        internal BodyRegistry Registry => registry;
        public ArenaTools Tools => tools ?? (tools=new ArenaTools(this));
        public ThermoBody Shoot(ThermoBody caster,Vector2 direction,float heat) => Tools.Shoot(caster,direction,heat);
        public Vector2 ProjectileSpawnPosition(ThermoBody caster,Vector2 direction) => Tools.ProjectileSpawnPosition(caster,direction);
        public bool TryGetProjectileSpawnPosition(ThermoBody caster,Vector2 direction,out Vector2 position) => Tools.TryGetProjectileSpawnPosition(caster,direction,out position);
        public void CreateShields(ThermoBody caster) => Tools.CreateShields(caster);
        public ThermoBody Melee(ThermoBody caster,Vector2 direction) => Tools.Melee(caster,direction);
'''
end=text.rfind('    }');text=text[:end]+forward+text[end:]
text=re.sub(r'\n[ \t]*\n(?:[ \t]*\n)+','\n\n',text)
path.write_text(text,encoding='utf-8')
print('Director now',len(text.splitlines()),'lines. Tools extracted.')
