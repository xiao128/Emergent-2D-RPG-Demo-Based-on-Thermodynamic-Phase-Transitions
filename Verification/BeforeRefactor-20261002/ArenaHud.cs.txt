using System.Text;
using TMPro;
using UnityEngine;
namespace PhaseArena
{
    public class ArenaHud : MonoBehaviour
    {
        public TMP_Text waveLabel,healthLabel,physicalLabel,lawsLabel,messageLabel,skillsLabel,targetLabel,reactionLabel;
        public TMP_Text upgradeTitle,resultTitle,resultBody;
        public TMP_Text[] cardTitles,cardBodies;
        public UnityEngine.UI.Image healthBar;
        public UnityEngine.UI.Image manaBar;
        public TMP_Text manaLabel;
        public GameObject titlePanel,upgradePanel,resultPanel,pausePanel;
        public UnityEngine.UI.Button startButton,restartButton,resumeButton;
        public UnityEngine.UI.Button[] choiceButtons;
        public Transform hoverCursor;
        public ArenaMinimap minimap;
        readonly StringBuilder builder=new StringBuilder();
        float nextUiAt;
        bool missingMageReported;
        void Awake()
        {
            startButton.onClick.AddListener(()=>ArenaDirector.Instance.StartRun());
            restartButton.onClick.AddListener(()=>ArenaDirector.Instance.Restart());
            resumeButton.onClick.AddListener(()=>ArenaDirector.Instance.TogglePause());
            for(int i=0;i<choiceButtons.Length;i++) { int slot=i; choiceButtons[i].onClick.AddListener(()=>ArenaDirector.Instance.ChooseLaw(slot)); }
        }
        void Update()
        {
            var g=ArenaDirector.Instance; if(g==null || g.player==null || g.tuning==null) return;
            titlePanel.SetActive(g.State==RunState.Title);
            upgradePanel.SetActive(g.State==RunState.Upgrade);
            resultPanel.SetActive(g.State==RunState.Victory || g.State==RunState.Defeat);
            pausePanel.SetActive(g.Paused);
            if(Time.unscaledTime<nextUiAt) return; nextUiAt=Time.unscaledTime+.1f;
            waveLabel.text=g.State==RunState.Title ? "PHASE ARENA  /  世界钟试炼"
                : g.State==RunState.Boss ? "第 8 / 8 周目  ·  右侧世界钟魔王"
                : "第 "+g.World+" / 8 周目    世界钟碎片 "+g.Fragments+"/2    右侧守卫 "+g.GuardsRemaining;
            healthBar.fillAmount=Mathf.Clamp01(g.player.health/g.player.maxHealth);
            healthLabel.text="生命 "+Mathf.CeilToInt(Mathf.Max(0,g.player.health))+" / "+g.player.maxHealth.ToString("0");
            physicalLabel.text="温度 "+g.player.temperature.ToString("0")+"°C\n质量 "+g.player.Mass.ToString("0.00")+"   速度 "+g.player.Body.velocity.magnitude.ToString("0.0");
            if(g.Has(WorldLaw.Abrasion)) physicalLabel.text+="\n物质剩余 "+Mathf.RoundToInt(g.player.massRemaining*100)+"%";
            else if(g.Has(WorldLaw.ThermalExpansion)) physicalLabel.text+="\n占地大小 ×"+g.player.ShapeScale.ToString("0.00");
            builder.Clear();
            if(g.laws.Count==0) builder.Append("始终生效的规则\n\n冲量决定推动\n质量、速度决定撞击\n物体逐渐回到常温\n速度越快，回温越快\n温度与速度存在上限\n\n探索世界，解锁新法则");
            else
            {
                builder.Append("已改变的世界法则\n\n");
                foreach(WorldLaw l in System.Enum.GetValues(typeof(WorldLaw))) if(g.Has(l)) builder.Append("◆ ").Append(ArenaDirector.LawName(l)).Append('\n');
            }
            lawsLabel.text=builder.ToString();
            string prompt=g.Message;
            if(g.State==RunState.Explore && Vector2.Distance(g.player.Body.position,g.clock.position)<2.25f)
                prompt=g.Fragments>=2 ? "[ E ] 交付两枚碎片 · 三选一改变世界法则" : "世界钟碎片 "+g.Fragments+"/2：击败守卫后靠近碎片拾取";
            messageLabel.text=prompt;
            if(reactionLabel!=null) reactionLabel.text=g.State==RunState.Transition ? "世界法则改变了" : g.SimulationActive && Time.time<g.ReactionUntil ? g.RecentReaction : "";
            var mage=g.player.GetComponent<PlayerMage>();
            if(mage==null)
            {
                if(!missingMageReported) { Debug.LogError("[PhaseArena] 玩家缺少有效的 PlayerMage 组件，请检查 Missing Script 或脚本导入状态。",g.player); missingMageReported=true; }
                if(manaBar!=null) manaBar.fillAmount=0;
                if(manaLabel!=null) manaLabel.text="法力：玩家组件未加载";
                skillsLabel.text="玩家控制组件未加载，请检查 PlayerMage。";
                return;
            }
            missingMageReported=false;
            if(manaBar!=null) manaBar.fillAmount=Mathf.Clamp01(mage.Mana/Mathf.Max(1,mage.maxMana));
            if(manaLabel!=null) manaLabel.text="法力 "+Mathf.FloorToInt(mage.Mana)+" / "+mage.maxMana.ToString("0");
            skillsLabel.text="左键 蓄火 "+Cooldown(mage.FireReady)+"    右键 蓄冰 "+Cooldown(mage.IceReady)+"    空格 护盾 "+Cooldown(mage.ShieldReady)+"    F 踢球 / 推动 "+Cooldown(mage.MeleeReady)
                +"\n蓄力 "+g.tuning.projectileChargeDuration.ToString("0.0")+" 秒 · 松开成球 / F 成球并踢出    WASD 移动 · Shift 慢行 · E 世界钟 · Esc 暂停"
                +(mage.IsCharging ? "  蓄力 "+Mathf.RoundToInt(mage.ChargeProgress*100)+"%" : "");
            targetLabel.text="环境实验\n移动蓄力，松开生成球；F 成球并踢出。\n冻结水面造桥，加热冰面使其融化。\n\n悬停查看温度、质量和速度。";
            if(Camera.main!=null)
            {
                Vector2 p=Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if(hoverCursor!=null) { hoverCursor.position=p; hoverCursor.gameObject.SetActive(g.SimulationActive); }
                foreach(var hit in Physics2D.OverlapCircleAll(p,.35f))
                {
                    var b=hit.GetComponentInParent<ThermoBody>(); if(b==null || b.Dead) continue;
                    string name=b.indestructible ? "巨型石头 / 固定障碍物" : b.isElite ? "世界钟守卫" : KindName(b.kind);
                    string condition=b.indestructible ? "\n固定 · 不可破坏" : b.kind==BodyKind.Tree ? "\n固定 · 可以破坏\n生命 "+Mathf.Max(0,b.health).ToString("0") : "\n生命 "+Mathf.Max(0,b.health).ToString("0");
                    targetLabel.text="目标 "+name+condition
                        +"\n温度 "+b.temperature.ToString("0")+"°C  质量 "+b.Mass.ToString(b.kind==BodyKind.Projectile ? "0.0000" : "0.00")
                        +"\n速度 "+b.Body.velocity.magnitude.ToString("0.0")
                        +(g.Has(WorldLaw.Abrasion) ? "\n物质剩余 "+Mathf.RoundToInt(b.massRemaining*100)+"% · 温度高于常温且速度 > 5 时磨损" : "")
                        +(g.Has(WorldLaw.ThermalExpansion) ? "  大小 ×"+b.ShapeScale.ToString("0.00") : ""); break;
                }
            }
            if(g.State==RunState.Victory || g.State==RunState.Defeat)
            {
                resultTitle.text=g.State==RunState.Victory ? "魔王已被击败" : "整局试炼结束";
                resultBody.text=(g.State==RunState.Victory ? "你用累计的世界法则完成了试炼。" : "倒下原因："+g.DeathCause)
                    +"\n\n到达第 "+g.World+" 周目 · 改写 "+g.metrics.upgrades+" 条法则\n"
                    +Mathf.RoundToInt(g.metrics.EnvironmentShare*100)+"% 伤害来自环境与法则反应\n击败 "+g.metrics.kills+" 个敌人 · 拾取 "+g.metrics.collectedFragments+" 枚碎片\n"
                    +"用时 "+Mathf.FloorToInt(g.metrics.seconds/60)+"分"+Mathf.FloorToInt(g.metrics.seconds%60)+"秒\n\n重新开始将清空本局法则与进度";
            }
        }
        public void ShowChoices()
        {
            var g=ArenaDirector.Instance; upgradePanel.SetActive(true);
            upgradeTitle.text="世界钟 / 第 "+g.World+" 次改写\n选择一条法则，进入下一周目";
            for(int i=0;i<choiceButtons.Length;i++)
            {
                bool valid=i<g.Choices.Length; choiceButtons[i].gameObject.SetActive(valid);
                if(valid) { cardTitles[i].text=ArenaDirector.LawName(g.Choices[i]); cardBodies[i].text=ArenaDirector.LawDescription(g.Choices[i])+"\n\n对玩家、怪物和环境共同生效"; }
            }
            Canvas.ForceUpdateCanvases();
        }
        string Cooldown(float time) => time<=Time.time ? "就绪" : (time-Time.time).ToString("0.0")+"s";
        string KindName(BodyKind k)
        {
            switch(k) { case BodyKind.Player:return "法师"; case BodyKind.Boss:return "魔王"; case BodyKind.Enemy:return "游荡怪物"; case BodyKind.Rock:return "石头"; case BodyKind.Tree:return "树木"; case BodyKind.Shield:return "魔法护盾"; case BodyKind.StaticObstacle:return "固定障碍物"; default:return "调温弹丸"; }
        }
    }
}
