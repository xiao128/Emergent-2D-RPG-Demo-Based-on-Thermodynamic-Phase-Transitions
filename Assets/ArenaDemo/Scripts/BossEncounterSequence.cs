using UnityEngine;
using TMPro;
namespace PhaseArena
{
    public enum BossEncounterPhase { Dormant, Summoning, Fighting, Cleared }
    public sealed class BossEncounterSequence
    {
        readonly ArenaDirector g;
        System.Random random;
        Color faceColor,rimColor,handColor;
        bool colorsCaptured;
        SpriteRenderer face,rim,hand;
        public BossEncounterPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public int MinionsSpawned { get; private set; }
        public bool Started => Phase!=BossEncounterPhase.Dormant;
        public bool FinalWorld => g.World==g.tuning.worldCount;
        public ThermoBody Boss { get; private set; }
        float nextMinion;
        public BossEncounterSequence(ArenaDirector world) {g=world;}
        void CaptureClock()
        {
            if(colorsCaptured || g.clock==null) return;
            var f=g.clock.Find("Clock Face"); var r=g.clock.Find("Clock Rim");
            face=f!=null?f.GetComponent<SpriteRenderer>():null; rim=r!=null?r.GetComponent<SpriteRenderer>():null;
            hand=g.clockHand!=null?g.clockHand.GetComponent<SpriteRenderer>():null;
            if(face!=null) faceColor=face.color; if(rim!=null) rimColor=rim.color; if(hand!=null) handColor=hand.color;
            colorsCaptured=true;
        }
        public void Reset()
        {
            CaptureClock(); Phase=BossEncounterPhase.Dormant; Elapsed=0; MinionsSpawned=0; Boss=null;
            if(face!=null) face.color=faceColor; if(rim!=null) rim.color=rimColor; if(hand!=null) hand.color=handColor;
            UpdateLabel("世界钟");
        }
        void UpdateLabel(string text) { if(g.clock!=null) {var label=g.clock.GetComponentInChildren<TMP_Text>(); if(label!=null) label.text=text;} }
        public bool Activate()
        {
            if(!FinalWorld || !g.SimulationActive || g.player==null || g.player.Dead) return false;
            if(Phase==BossEncounterPhase.Cleared) {g.CompleteRun(); return true;}
            if(Phase!=BossEncounterPhase.Dormant) return false;
            Phase=BossEncounterPhase.Summoning; Elapsed=0; nextMinion=g.tuning.bossEncounterMinionDelay;
            random=new System.Random(unchecked(g.RunSeed^7334147));
            var color=g.tuning.activeBossClockColor;
            if(face!=null) face.color=color; if(rim!=null) rim.color=color; if(hand!=null) hand.color=color;
            UpdateLabel("召唤已启动");
            Vector2 destination=g.worldGenerator.SafeDryPosition((Vector2)g.clock.position+Vector2.left*g.tuning.bossPlayerTeleportDistance,g.player.radius+.3f);
            g.player.Body.position=destination; g.player.transform.position=destination;
            g.player.Body.velocity=Vector2.zero; g.player.LastSafePosition=destination;
            g.player.GetComponent<PlayerMage>().ResetTools();
            g.player.GetComponent<PlayerMage>().ConsumeClockClick();
            g.BeginBossEncounter();
            if(ArenaAudio.Instance!=null) ArenaAudio.Instance.RefreshMusic();
            g.Pulse(g.clock.position,color,2,1);
            return true;
        }
        public void Tick(float dt,bool synchronizeMusic=false)
        {
            if(Phase!=BossEncounterPhase.Summoning || !g.SimulationActive) return;
            Elapsed+=Mathf.Max(0,dt);
            // Streaming audio can start slightly later than the frame calling Play.
            // Use its playback position for the live entrance, preserving the cue
            // despite editor stalls or buffering. Deterministic fixtures use dt.
            var audio=ArenaAudio.Instance;
            if(synchronizeMusic && audio!=null && audio.MusicSource!=null && audio.MusicSource.clip==audio.bossMusic && audio.MusicSource.isPlaying)
                Elapsed=audio.MusicSource.time;
            // Minions stage the entrance; once the boss arrives, its own summons
            // take over. A frame crossing both schedules still preserves ordering.
            while(nextMinion<=Elapsed && nextMinion<g.tuning.bossEncounterSpawnDelay)
            {
                ThermoBody[] options={g.lightEnemyPrefab,g.mediumEnemyPrefab,g.heavyEnemyPrefab,g.rangedEnemyPrefab};
                var prefab=options[random.Next(options.Length)]; if(prefab==null) prefab=g.mediumEnemyPrefab;
                g.Spawn(prefab,g.clock.position); MinionsSpawned++;
                nextMinion+=Mathf.Max(.1f,g.tuning.bossEncounterMinionInterval);
            }
            if(Elapsed<g.tuning.bossEncounterSpawnDelay) return;
            Boss=g.Spawn(g.bossPrefab,g.clock.position);
            Boss.maxHealth=g.tuning.EnemyHealthAt(g.tuning.bossHealth,g.World); Boss.health=Boss.maxHealth;
            Boss.GetComponent<EnemyBrain>().SetHome(g.clock.position,true);
            Phase=BossEncounterPhase.Fighting; UpdateLabel("击败魔王");
            g.SetMessage("魔王降临！击败它，再回到蓝色世界钟点击完成试炼。");
            g.Pulse(g.clock.position,new Color(.35f,.7f,1),3,1);
        }
        public void BossDefeated()
        {
            if(Phase!=BossEncounterPhase.Fighting) return;
            Phase=BossEncounterPhase.Cleared; UpdateLabel("点击通关");
            g.SetMessage("魔王已被击败：返回蓝色世界钟，点击或按 E 完成试炼。");
        }
        public string Prompt => Phase==BossEncounterPhase.Dormant ? "[点击 / E] 启动世界钟召唤仪式"
            : Phase==BossEncounterPhase.Cleared ? "[点击 / E] 魔王已被击败 · 完成试炼"
            : Phase==BossEncounterPhase.Summoning ? "世界钟召唤中 · 魔王将在 "+Mathf.CeilToInt(Mathf.Max(0,g.tuning.bossEncounterSpawnDelay-Elapsed))+" 秒后降临"
            : "击败魔王后，回到世界钟点击通关";
    }
}
