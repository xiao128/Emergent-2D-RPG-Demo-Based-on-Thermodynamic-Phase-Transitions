using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PhaseArena
{
    public enum RunState { Title, Explore, Upgrade, Transition, Boss, Victory, Defeat }
    public enum DamageKind { Spell, Staff, Overheat, Impact, Arc, Fission, Terrain, ThermalShock, Crush, Crowding }
    [Serializable] public class RunMetrics
    {
        public int spawned, kills, shots, collisions, frozenTiles, meltedTiles, upgrades, arcs, vaporBursts, explosions, collectedFragments;
        public int repulsions, thermalShocks, crushTicks;
        public float seconds, enemyDamage, environmentalEnemyDamage, directEnemyDamage, largestImpact;
        public float EnvironmentShare => enemyDamage>0 ? environmentalEnemyDamage/enemyDamage : 0;
    }
    public class ArenaDirector : MonoBehaviour
    {
        public static ArenaDirector Instance { get; private set; }
        [Header("Scene references")]
        public ThermoBody player;
        public ArenaHud hud;
        public Transform clock, clockHand, entities;
        public TerrainCell[] terrain=new TerrainCell[0];
        public ArenaTuning tuning;
        public WorldGenerator worldGenerator;
        [Header("Reusable prefabs")]
        public ThermoBody lightEnemyPrefab, mediumEnemyPrefab, heavyEnemyPrefab, guardPrefab, bossPrefab, projectilePrefab, shieldPrefab;
        public ThermoBody rangedEnemyPrefab;
        public ArenaPulse pulsePrefab;
        public ShardPickup shardPrefab;
        public Sprite fireSprite, iceSprite;
        public PhysicsMaterial2D normalMaterial, bouncyMaterial;
        public TMP_FontAsset feedbackFont;
        [Header("Seed (0 creates a new run)")]
        public int randomSeed;
        public RunState State { get; private set; }=RunState.Title;
        public int World { get; private set; }
        public int RunSeed { get; private set; }
        public int Fragments { get; private set; }
        public int GuardsRemaining { get; private set; }
        public Vector2 SpawnPosition { get; private set; }
        public bool Paused { get; private set; }
        public bool SimulationActive => !Paused && (State==RunState.Explore || State==RunState.Boss);
        public bool CombatActive => SimulationActive;
        public WorldLaw[] Choices { get; private set; }=new WorldLaw[0];
        public readonly HashSet<WorldLaw> laws=new HashSet<WorldLaw>();
        readonly BodyRegistry registry=new BodyRegistry();
        public List<ThermoBody> bodies => registry.Bodies;
        public readonly List<ShardPickup> shards=new List<ShardPickup>();
        public RunMetrics metrics=new RunMetrics();
        public string Message { get; private set; }="冰火调温，法杖推动；利用世界击败守卫。";
        public string DeathCause { get; private set; }
        public float AverageFrameMs => frameAverage*1000;
        public string RecentReaction { get; private set; }
        public float ReactionUntil { get; private set; }
        float thermalTimer,frameAverage=.016f,messageUntil,feedbackAt;
        System.Random lawRandom;
        void Awake()
        {
            Instance=this; Time.timeScale=1;
            foreach(var b in FindObjectsOfType<ThermoBody>()) Register(b);
        }
        void OnDestroy() { if(Instance==this) { Instance=null; Time.timeScale=1; } }
        public bool Has(WorldLaw law) => laws.Contains(law);
        public float ForceMultiplier => Has(WorldLaw.DoubleForce) ? 2 : 1;
        public void Register(ThermoBody b) { registry.Register(b); }
        public void Unregister(ThermoBody b) { registry.Unregister(b); }
        public ThermoBody BodyFor(Collider2D collider) => registry.BodyFor(collider);
        public void StartRun()
        {
            if(State!=RunState.Title) return;
            RunSeed=randomSeed!=0 ? randomSeed : Guid.NewGuid().GetHashCode() & int.MaxValue;
            lawRandom=new System.Random(RunSeed^1103515245);
            SpawnPosition=WorldLayout.SpawnAt(tuning);
            clock.position=WorldLayout.ClockAt(tuning);
            Physics2D.maxTranslationSpeed=Mathf.Max(tuning.rockSpeedLimit,tuning.actorSpeedLimit,tuning.spellSpeedLimit);
            laws.Clear(); metrics=new RunMetrics(); World=1; BuildWorld();
        }
        public void Restart()
        {
            StopAllCoroutines(); Time.timeScale=1; Paused=false; State=RunState.Title;
            laws.Clear(); Choices=new WorldLaw[0]; Fragments=0; World=0; DeathCause=null;
            ClearRuntime(); player.ResetAt(SpawnPosition);
            Message="新的一局：所有世界法则与碎片已重置。"; StartRun();
        }
        void ClearRuntime()
        {
            var children=new List<GameObject>();
            foreach(Transform child in entities) children.Add(child.gameObject);
            foreach(var child in children) { child.SetActive(false); Destroy(child); }
            shards.Clear(); Collisions.Clear();
        }
        void BuildWorld()
        {
            ClearRuntime(); Fragments=0; GuardsRemaining=World<tuning.worldCount ? 2 : 0; Choices=new WorldLaw[0];
            int seed=unchecked(RunSeed+World*104729);
            clock.position=WorldLayout.ClockAt(tuning);
            worldGenerator.Generate(seed,SpawnPosition);
            player.maxHealth=tuning.PlayerHealthAt(World);
            player.ResetAt(SpawnPosition); player.GetComponent<PlayerMage>().ResetTools();
            int i=0;
            foreach(var p in worldGenerator.Layout.monsters)
            {
                Spawn(i%5==4 && rangedEnemyPrefab!=null ? rangedEnemyPrefab : i%3==0 ? lightEnemyPrefab : i%3==1 ? mediumEnemyPrefab : heavyEnemyPrefab,p); i++;
            }
            if(World<tuning.worldCount)
            {
                int guardIndex=0;
                foreach(var p in worldGenerator.Layout.guards)
                {
                    var guard=Spawn(guardPrefab,p); guard.isElite=true; guard.baseMass=guardIndex++==0 ? 4 : 6;
                    guard.maxHealth=tuning.EnemyHealthAt(tuning.guardHealth,World); guard.health=guard.maxHealth; guard.baseColor=new Color(1,.84f,.52f); guard.ThermalStep(0);
                    guard.GetComponent<EnemyBrain>().SetHome(p,true);
                }
                State=RunState.Explore;
                if(World==1) Message="从左向右探索 → 击败右侧守卫 → 拾取碎片 → 世界钟按 E";
            }
            else
            {
                var boss=Spawn(bossPrefab,clock.position); boss.maxHealth=tuning.EnemyHealthAt(tuning.bossHealth,World); boss.health=boss.maxHealth;
                boss.GetComponent<EnemyBrain>().SetHome(clock.position,true);
                State=RunState.Boss; Message="第八周目：魔王出现在中央世界钟！用累计的法则击败它。";
            }
            if(hud!=null && hud.minimap!=null) hud.minimap.Rebuild();
            var follow=Camera.main!=null ? Camera.main.GetComponent<ArenaCameraFollow>() : null;
            if(follow!=null) follow.Snap();
            Debug.Log("[PhaseArena] World "+World+" seed="+seed+" laws="+laws.Count+" roamers="+worldGenerator.Layout.monsters.Count);
        }
        public ThermoBody Spawn(ThermoBody prefab,Vector2 p)
        {
            var b=Instantiate(prefab,p,Quaternion.identity,entities); Register(b); metrics.spawned++;
            if(b.IsEnemy) { b.maxHealth=tuning.EnemyHealthAt(prefab.maxHealth,World); b.health=b.maxHealth; }
            var ai=b.GetComponent<EnemyBrain>(); if(ai!=null) ai.SetHome(p,b.kind==BodyKind.Boss);
            return b;
        }
        public void TogglePause()
        {
            if(State!=RunState.Explore && State!=RunState.Boss) return;
            Paused=!Paused; Time.timeScale=Paused ? 0 : 1;
        }
        void Update()
        {
            frameAverage=Mathf.Lerp(frameAverage,Time.unscaledDeltaTime,.04f);
            if(Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if(Input.GetKeyDown(KeyCode.R) && (State==RunState.Victory || State==RunState.Defeat)) Restart();
            if(!SimulationActive) return;
            metrics.seconds+=Time.deltaTime;
            if(clockHand!=null) clockHand.Rotate(0,0,-Time.deltaTime*24);
            if(Input.GetKeyDown(KeyCode.E)) TryOpenClock();
            if(messageUntil>0 && Time.time>messageUntil)
            {
                messageUntil=0;
                Message=State==RunState.Boss ? "魔王在右侧世界钟附近。冰火准备条件，法杖推动环境。" : "新世界已生成：向右探索并带回两枚世界钟碎片。";
            }
        }
        void FixedUpdate()
        {
            if(!SimulationActive) return;
            thermalTimer+=Time.fixedDeltaTime;
            if(thermalTimer<.1f) return;
            float dt=thermalTimer; thermalTimer=0; ThermalTick(dt);
        }

        public bool TryOpenClock()
        {
            if(State!=RunState.Explore || player.Dead || Vector2.Distance(player.Body.position,clock.position)>2.25f) return false;
            if(Fragments<2) { Message="世界钟需要两枚碎片：击败守卫后，靠近碎片拾取。"; return false; }
            var available=new List<WorldLaw>();
            // Temporarily excluded from random offers while hot-projectile damage is rebalanced.
            foreach(WorldLaw law in Enum.GetValues(typeof(WorldLaw)))
                if(WorldLawCatalog.IsAvailable(law) && !Has(law)) available.Add(law);
            for(int i=available.Count-1;i>0;i--) { int j=lawRandom.Next(i+1); var t=available[i]; available[i]=available[j]; available[j]=t; }
            Choices=available.GetRange(0,3).ToArray(); State=RunState.Upgrade; Time.timeScale=0; hud.ShowChoices(); return true;
        }
        public bool ChooseLaw(int index)
        {
            if(State!=RunState.Upgrade || index<0 || index>=Choices.Length) return false;
            laws.Add(Choices[index]); metrics.upgrades++; Fragments=0;
            Debug.Log("[PhaseArena] Global law "+Choices[index]); StartCoroutine(NextWorld()); return true;
        }
        IEnumerator NextWorld()
        {
            State=RunState.Transition; Time.timeScale=1; Message="世界法则改变了";
            Pulse(clock.position,new Color(1,.85f,.4f),3,1);
            yield return new WaitForSecondsRealtime(1.2f);
            World++; BuildWorld();
            if(State!=RunState.Boss) { Message="世界法则改变了"; messageUntil=Time.time+2.4f; }
        }
        public bool Collect(ShardPickup shard)
        {
            if(!SimulationActive || shard==null || shard.Collected || !shards.Contains(shard)) return false;
            shard.Collected=true; shards.Remove(shard); Fragments++; metrics.collectedFragments++;
            Feedback(player.Body.position,"世界钟碎片 "+Fragments+"/2",new Color(1,.84f,.4f));
            Message=Fragments==2 ? "碎片已集齐：靠近右侧世界钟，按 E 选择世界法则。" : "已拾取一枚碎片，还需要另一名守卫的碎片。";
            Destroy(shard.gameObject); return true;
        }
        public TerrainCell FloorAt(Vector2 p) => worldGenerator!=null ? worldGenerator.FloorAt(p) : null;
        public void OnIceMelted(TerrainCell cell)
        {
            metrics.meltedTiles++;
            foreach(var b in bodies.ToArray())
            {
                if(b==null || b.Dead || !b.IsActor || !cell.Contains(b.Body.position)) continue;
                if(b.kind==BodyKind.Player || b.kind==BodyKind.Boss || b.isElite)
                {
                    b.Damage(40,"融冰落水",DamageKind.Terrain);
                    if(!b.Dead) { b.Body.position=worldGenerator.SafeDryPosition(b.Body.position,b.radius+.3f); b.Body.velocity=Vector2.zero; b.LastSafePosition=b.Body.position; }
                }
                else b.Die("融冰地形杀",DamageKind.Terrain);
            }
        }

        public void RecordDamage(ThermoBody target,float amount,DamageKind kind)
        {
            if(!target.IsEnemy) return;
            metrics.enemyDamage+=amount;
            if(kind==DamageKind.Spell || kind==DamageKind.Staff || kind==DamageKind.Overheat) metrics.directEnemyDamage+=amount;
            else metrics.environmentalEnemyDamage+=amount;
        }
        public void Pulse(Vector2 p,Color color,float radius,float duration=.45f)
        {
            if(pulsePrefab==null) return;
            var fx=Instantiate(pulsePrefab,p,Quaternion.identity,entities); fx.radius=radius; fx.color=color; fx.duration=duration;
        }
        public void Feedback(Vector2 p,string text,Color color)
        {
            RecentReaction=text; ReactionUntil=Time.time+2;
            if(feedbackFont==null || Time.time<feedbackAt) return;
            feedbackAt=Time.time+.15f;
            var go=new GameObject("Rule Feedback",typeof(TextMeshPro),typeof(RuleFeedback)); go.transform.SetParent(entities,false); go.transform.position=p+Vector2.up*.7f;
            var label=go.GetComponent<TextMeshPro>(); label.font=feedbackFont; label.text=text; label.fontSize=2.5f; label.color=color;
            label.alignment=TextAlignmentOptions.Center; label.rectTransform.sizeDelta=new Vector2(8,1.4f); label.GetComponent<MeshRenderer>().sortingOrder=850;
        }
        public void OnBodyDied(ThermoBody b,string cause)
        {
            if(b.IsEnemy)
            {
                metrics.kills++; Pulse(b.Body.position,new Color(.6f,.9f,.65f),.65f);
                if(b.isElite && State==RunState.Explore)
                {
                    GuardsRemaining=Mathf.Max(0,GuardsRemaining-1);
                    Vector2 p=worldGenerator.SafeDryPosition(b.Body.position,.7f);
                    var shard=Instantiate(shardPrefab,p,Quaternion.identity,entities); shards.Add(shard);
                    Message="守卫掉落了世界钟碎片：靠近拾取。";
                }
                if(b.kind==BodyKind.Boss && State==RunState.Boss)
                {
                    State=RunState.Victory; Time.timeScale=0; Message="魔王已被击败，世界钟试炼完成！";
                    Debug.Log("[PhaseArena] Victory worlds="+World+" environmentShare="+metrics.EnvironmentShare);
                }
            }
            if(b.kind==BodyKind.Player)
            {
                DeathCause=cause; State=RunState.Defeat; Time.timeScale=0; Message="整局结束："+cause;
                Debug.Log("[PhaseArena] Defeat: "+cause);
            }
        }

        CollisionRules collisionRules;
        WorldLawSimulation lawSimulation;
        public CollisionRules Collisions => collisionRules ?? (collisionRules=new CollisionRules(this));
        public WorldLawSimulation LawSimulation => lawSimulation ?? (lawSimulation=new WorldLawSimulation(this));
        void ThermalTick(float dt) => LawSimulation.Tick(dt);
        public void ResolveCollision(ThermoBody a,ThermoBody b,float speed,Vector2 point,float impulse=0) => Collisions.ResolveCollision(a,b,speed,point,impulse);
        public void DeliverProjectileHeat(ThermoBody b,Vector2 p) => Collisions.DeliverProjectileHeat(b,p);
        public void SteamWave(ThermoBody b,float cooling) => Collisions.SteamWave(b,cooling);
        public static float ImpactDamage(float mass,float speed) => CollisionRules.ImpactDamage(Instance!=null ? Instance.tuning : null,mass,speed);
        public void ApplyHeatBlast(Vector2 p,float heat,float radius,ThermoBody source) => Collisions.ApplyHeatBlast(p,heat,radius,source);
        public void Fission(ThermoBody source,float energy) => Collisions.Fission(source,energy);
        public static string LawName(WorldLaw law) => WorldLawCatalog.LawName(law);
        public static string LawDescription(WorldLaw law) => WorldLawCatalog.LawDescription(law);

        ArenaTools tools;
        internal BodyRegistry Registry => registry;
        public ArenaTools Tools => tools ?? (tools=new ArenaTools(this));
        public ThermoBody Shoot(ThermoBody caster,Vector2 direction,float heat) => Tools.Shoot(caster,direction,heat);
        public Vector2 ProjectileSpawnPosition(ThermoBody caster,Vector2 direction) => Tools.ProjectileSpawnPosition(caster,direction);
        public bool TryGetProjectileSpawnPosition(ThermoBody caster,Vector2 direction,out Vector2 position) => Tools.TryGetProjectileSpawnPosition(caster,direction,out position);
        public void CreateShields(ThermoBody caster) => Tools.CreateShields(caster);
        public ThermoBody Melee(ThermoBody caster,Vector2 direction) => Tools.Melee(caster,direction);
    }
}
