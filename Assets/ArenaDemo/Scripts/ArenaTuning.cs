using UnityEngine;

namespace PhaseArena
{
    [CreateAssetMenu(menuName = "Phase Arena/World Tuning")]
    public class ArenaTuning : ScriptableObject
    {
        [Header("世界与生成数量")]
        [Tooltip("世界宽度，单位：地格；重新生成世界时生效。")]
        public int width = 72;
        [Tooltip("世界高度，单位：地格；重新生成世界时生效。")]
        public int height = 56;
        [Tooltip("一轮游戏的世界数量。")]
        public int worldCount = 9;
        [Tooltip("每个世界随机生成的游荡怪物数量，不含守卫和 Boss。")]
        public int roamingCount = 19;
        [Tooltip("可移动普通石头的生成数量。")]
        public int rockCount = 48;
        [Tooltip("可破坏树木的生成数量。")]
        public int treeCount = 22;
        [Tooltip("不可破坏巨石障碍物的生成数量。")]
        public int largeRockCount = 12;
        [HideInInspector] public float aggroRadius=6; // Legacy fixed perception radius.
        [Tooltip("游荡怪物离出生位置超过此距离后返回，单位：世界单位。")]
        public float leashRadius = 10;
        [Tooltip("守卫第一世界的基础生命，后续世界乘怪物生命成长。")]
        public float guardHealth = 60;
        [Tooltip("Boss 第一世界的基础生命，后续世界乘怪物生命成长。")]
        public float bossHealth = 120;
        [Tooltip("普通场地石头的基础生命；重新构建对应预制体时应用，现有石头生命在 SmallRock 预制体设置。")]
        public float rockHealth = 360;
        [Header("基础生命与怪物成长")]
        [Tooltip("新开局玩家的基础最大生命；过关不自动增加，升级另外增加。")]
        public float playerHealth = 100;
        [Tooltip("每进入下一世界，怪物生命乘此倍率；1.55 表示增加 55%。")]
        public float enemyHealthGrowth = 1.55f;
        [HideInInspector] public float playerHealthGrowth=1.25f;
        [Tooltip("第一世界小怪基础生命。")]
        public float lightHealth = 18;
        [Tooltip("第一世界中怪基础生命。")]
        public float mediumHealth = 28;
        [Tooltip("第一世界大怪基础生命。")]
        public float heavyHealth = 42;
        [Header("经验与升级（全局设置）")]
        [Min(0), Tooltip("所有怪物击杀经验的倍率。1 为原值，2 为双倍，0 为不获取；不足 1 点的部分累计到后续击杀。")]
        public float experienceGainMultiplier=1;
        [Min(1), Tooltip("一级升二级所需经验。")]
        public int firstLevelExperience=100;
        [Min(0), Tooltip("每升一级，下一次升级需求增加多少经验。")]
        public int experienceIncreasePerLevel=50;
        [Min(0), Tooltip("每次升级增加的最大生命，并恢复同等数量的生命。")]
        public float healthGainPerLevel=10;
        [Min(0), Tooltip("每次升级增加的最大法力，并恢复同等数量的法力。")]
        public float manaGainPerLevel=10;
        [Header("结算展示")]
        [Min(0), Tooltip("死亡或通关时先移除玩家，等待多少真实秒后显示结算面板；不受暂停影响。")]
        public float resultDisplayDelay=5;
        [Header("固定地图：出生点、世界钟与河流")]
        [Tooltip("出生点和世界钟距离左右边界的内缩距离，单位：地格。")]
        public float edgeInset = 8;
        [Tooltip("固定横穿河流的中心纵坐标，单位：地格。")]
        public int riverCenterY = 10;
        [Tooltip("固定河流的宽度，单位：地格。")]
        public int riverWidth = 4;
        [Header("玩家法杖与投射物")]
        [Tooltip("F 钝击施加给目标的冲量；相同冲量推动轻物体更快。")]
        public float staffImpulse = 22.5f;
        [Tooltip("F 钝击的直接伤害，不包括推动后产生的撞击伤害。")]
        public float staffDamage = 12;
        [Tooltip("普通冷热法球的基础质量；挥石魔法改用石头质量并随蓄力面积增长。")]
        public float spellMass = 1;
        [HideInInspector] public float spellImpulse=6;
        [Tooltip("玩家与敌方投射物的初始生命值。")]
        public float projectileHealth = 160;
        [Tooltip("鼠标按住到允许生成投射物的最短时间，单位：秒。")]
        public float projectileChargeDuration = .3f;
        [Header("挥石魔法蓄力与投射物池")]
        [Tooltip("挥石魔法达到最大尺寸所需总蓄力时间，单位：秒。")]
        public float stoneFullChargeDuration=2f;
        [Tooltip("满蓄力石弹相对标准场地石头的线性尺寸倍率；质量按面积同步增长。")]
        public float stoneMaximumRockScale=1.5f;
        [Tooltip("玩家与敌方共享的场上投射物数量上限；超出后移除最先生成的投射物。")]
        public int maximumProjectiles = 250;
        [Tooltip("释放投射物后，两种法术共用的冷却时间，单位：秒。")]
        public float castCooldown = .65f;
        [HideInInspector] public float shieldRadius = 1.15f, shieldLifetime = 4, shieldCooldown = 7;
        [Tooltip("F 钝击冷却时间，单位：秒。")]
        public float staffCooldown = .65f;
        [HideInInspector] public float shieldRepelSpeed = 6.5f, shieldFormationDelay = .28f;
        [Header("空格技能：回血并恢复常温")]
        [Tooltip("空格恢复技能的冷却时间，单位：秒；不消耗法力。")]
        public float recoveryCooldown=7;
        [Range(0, 1), Tooltip("空格恢复的生命占当前最大生命的比例；0.1=10%，0.2=20%，1=100%。同时清除冷热状态，生命不超过上限。")]
        public float recoveryHealthFraction=.2f;
        [Header("怪物攻击节奏")]
        [Min(.01f), Tooltip("全体怪物基础攻速倍率；1 为原速，1.75 为当前速度。影响前摇、攻击冷却和恢复。")]
        public float enemyAttackSpeedMultiplier=1.75f;
        [Tooltip("攻击基础前摇，单位：秒；实际时间再除以全局攻速和高温攻速倍率。")]
        public float enemyWindup = .85f;
        [Tooltip("攻击基础冷却，单位：秒；实际时间再除以攻速倍率。")]
        public float enemyAttackCooldown = 3.5f;
        [Tooltip("攻击后恢复的基础时间，单位：秒；近战恢复至少覆盖冲撞保留时间。")]
        public float enemyRecovery = 1.2f;
        [HideInInspector] public float enemyLungeImpulse = 12, guardLungeImpulse = 20, bossLungeImpulse = 36;
        [HideInInspector] public float enemyLungeSpeed=30, guardLungeSpeed=34, bossLungeSpeed=38;
        [Header("各类怪物冲撞冲量（直接生效）")]
        [Min(0), Tooltip("小怪冲撞的实际冲量。起步速度 = 冲量 / 当前质量。0 表示不施加冲量。")]
        public float lightAttackImpulse=24;
        [Min(0), Tooltip("中怪冲撞的实际冲量；调小也会直接降低速度与攻击范围。")]
        public float mediumAttackImpulse=54;
        [Min(0), Tooltip("大怪冲撞的实际冲量。")]
        public float heavyAttackImpulse=120;
        [Min(0), Tooltip("守卫冲撞的实际冲量。")]
        public float guardAttackImpulse=136;
        [Min(0), Tooltip("Boss 冲撞的实际冲量。")]
        public float bossAttackImpulse=228;
        [Header("冲撞范围与站位")]
        [Min(.02f), Tooltip("施加冲量后保留物理冲撞的时间，期间 AI 不会转向或反向拉回。")]
        public float enemyLungeDuration=.8f;
        [Tooltip("预估冲撞伤害行程的安全倍率；0.9 表示使用理论行程的 90%。")]
        public float attackReachSafety=.9f;
        [Tooltip("解除警戒时，在攻击范围外额外保留的距离，单位：世界单位。")]
        public float enemyAlertHysteresis=1;
        [Tooltip("攻击冷却期间的站位距离占攻击范围的比例；0.75 表示 75%。")]
        public float enemyStandOffFraction=.75f;
        [Header("敌方投射物")]
        [Min(0), Tooltip("普通/远程怪物投射物的发射冲量，适用于冷热法球与石弹。")]
        public float enemyProjectileImpulse=24;
        [Min(0), Tooltip("远程怪物索敌及发射范围。")]
        public float rangedAttackRange=9;
        [Header("魔王追击、弹幕与召唤")]
        [Min(.0001f), Tooltip("魔王基础质量；实际质量仍受磨损与世界法则影响，质量会影响同一冲量产生的速度。")]
        public float bossBaseMass=6;
        [Min(0), Tooltip("魔王追逐时的步行速度上限，单位：世界单位/秒；不限制物理冲撞速度。")]
        public float bossMoveSpeed=2;
        [Min(0), Tooltip("魔王追逐时的推动力；越大步行加速越快，速度仍受 Boss Move Speed 限制。")]
        public float bossDriveForce=40;
        [Min(.01f), Tooltip("魔王近战攻击的基础前摇，单位：秒；实际再除以全局与高温攻速。")]
        public float bossWindup=1.1f;
        [Min(0), Tooltip("魔王近战攻击的基础冷却，单位：秒；独立于普通怪物，实际再除以攻速。")]
        public float bossAttackCooldown=3.5f;
        [Min(0), Tooltip("魔王攻击后的基础恢复时间，单位：秒；实际除以攻速，且至少覆盖冲撞保留时间。")]
        public float bossRecovery=1.2f;
        [Min(0), Tooltip("Boss 弹幕的发射冲量，可以独立于远程小怪调整。")]
        public float bossProjectileImpulse=48;
        [Min(0), Tooltip("魔王开始锁定玩家的距离，单位：世界单位；锁定后持续追击，不受出生点活动半径限制。")]
        public float bossAggroRadius=60;
        [Min(.1f), Tooltip("魔王两次弹幕的基础间隔，单位：秒；实际再除以全局及高温攻速倍率。")]
        public float bossVolleyInterval=3;
        [Min(0), Tooltip("魔王每次环形弹幕的投射物数量；另外固定有一发朝玩家瞄准的投射物。0 表示只发瞄准弹。")]
        public int bossRadialProjectiles=8;
        [Min(.1f), Tooltip("魔王每隔多少秒召唤一名守卫；按实际游戏时间计算，不受攻速倍率影响，暂停时不计时。")]
        public float bossSummonInterval=20;
        [Min(0), Tooltip("召唤守卫与魔王的基础间距；会寻找附近空旷陆地，找不到位置则稍后重试。")]
        public float bossSummonDistance=3;
        [Header("高温怪物攻速")]
        [Tooltip("怪物温度高于此值时获得额外攻速，单位：°C；无需随机法则。")]
        public float hotEnemyTemperature=80;
        [Tooltip("高温怪物的额外攻速倍率；与基础攻速相乘，1.5 表示额外提升 50%。")]
        public float hotEnemyAttackMultiplier=1.5f;
        [Header("撞击、速度与环境温度")]
        [Tooltip("撞击伤害的速度平方系数：伤害=此值×有效质量×速度² + Impact Beta×有效质量，再受单次上限限制。")]
        public float impactAlpha = .2f;
        [Tooltip("撞击伤害的质量系数；只有速度超过伤害门槛才会计算，静止接触不扣血。")]
        public float impactBeta = 2;
        [Tooltip("造成撞击伤害所需的法向相对速度门槛，单位：世界单位/秒；当前略高于玩家步行速度。")]
        public float impactThreshold = 6.1f;
        [Tooltip("单次撞击及部分法则反应的伤害上限。")]
        public float impactDamageCap = 2000;
        [Tooltip("撞击升温开始生效的法向相对速度门槛，独立于伤害门槛，单位：世界单位/秒。")]
        public float impactHeatThreshold = 1.8f;
        [Tooltip("可移动物品及双方投射物的速度上限，单位：世界单位/秒。")]
        public float rockSpeedLimit = 240;
        [Tooltip("玩家与怪物的速度上限，单位：世界单位/秒。")]
        public float actorSpeedLimit = 60;
        [Tooltip("参与物理引擎全局最大位移速度设置，与另两个速度上限取最大值；投射物自身仍按 Rock Speed Limit 限速。")]
        public float spellSpeedLimit = 160;
        [Tooltip("环境常温；物体逐渐回到此温度，空格清除冷热也回到此温度，单位：°C。")]
        public float ambientTemperature = 20;
        [Tooltip("所有实体温度的硬性下限，单位：°C。")]
        public float minimumTemperature = -1000;
        [Tooltip("所有实体温度的硬性上限，单位：°C。")]
        public float maximumTemperature = 1000;
        [Tooltip("静止物体每秒向常温恢复的温度，单位：°C/秒。")]
        public float ambientRecovery = 1.8f;
        [Tooltip("每单位移动速度带来的额外每秒回温；速度越快，回到常温越快。")]
        public float windRecovery = .18f;
        [HideInInspector] public float overheatDamageLimit=.1f;
        [Tooltip("普通地面上物品的阻尼系数；越大减速越快，角色另有步行阻尼。")]
        public float normalRockDrag = .6f;
        [Tooltip("粗糙地面上物品的阻尼系数；越大减速越快。")]
        public float roughRockDrag = 1.6f;
        [Tooltip("冰面阻尼系数；越小滑行越远。")]
        public float iceDrag = .02f;
        [Header("撞击升温与历史法则参数")]
        [Tooltip("旧冷热改变重量法则的最小质量倍率；该法则已移出随机池。")]
        public float hotMassFactor = .12f;
        [Tooltip("旧冷热改变重量法则的最大质量倍率；该法则已移出随机池。")]
        public float coldMassFactor = 8;
        [Tooltip("旧冷热改变重量法则中，每度温差引起的质量倍率变化；该法则已移出随机池。")]
        public float massTemperatureSlope = .012f;
        [Tooltip("旧冷热放电法则的温差与有效质量伤害系数；该法则已移出随机池。")]
        public float arcDamagePerDegreeMass = .65f;
        [Tooltip("撞击升温法则将法向相对动能转换成热量的比例，0.45 表示 45%。")]
        public float impactHeatFraction = .45f;
        [Tooltip("撞击热量的额外增益；温升还会除以目标的质量和比热容。")]
        public float impactTemperatureGain = 8;
        [Header("摩擦生热与热磨损（两个独立法则）")]
        [Min(0), Tooltip("摩擦生热的热量系数：速度平方 × 地面阻尼 × 此值 × 时间；温升除以热容量，冰面不生热。")]
        public float frictionHeatGain=.09f;
        [Min(0), Tooltip("热磨损要求速度严格大于此值，单位：世界单位/秒。默认 8，普通走路不磨损。")]
        public float abrasionSpeedThreshold=8;
        [Tooltip("热磨损要求温度严格高于此值，单位：°C；默认 40，与热外圈的门槛一致。")]
        public float abrasionHotTemperature=40;
        [HideInInspector] public float abrasionPerMeter=.15f; // Legacy percentage, no longer used.
        [Min(0), Tooltip("热磨损每滑行一米损失的固定质量，默认 0.2；不是百分比。模型与碰撞体同步缩小。")]
        public float abrasionMassPerMeter=.2f;
        [Tooltip("磨损后剩余质量占基础质量的最低比例；低于此值会耗尽或触发爆炸，0.1 表示 10%。")]
        public float minimumMassFraction = .1f;
        [Header("统一热容量与传热")]
        [Min(.001f), Tooltip("单位质量的比热容。温度变化 = 输入热量 / (质量 × 比热容)，质量越大越难改变温度。")]
        public float specificHeatCapacity=1;
        [Min(.001f), Tooltip("热容量的计算下限，避免极小物体接收极小热量时数值发散。")]
        public float minimumHeatCapacity=.05f;
        [Min(.001f), Tooltip("一格河流的等效质量，用于水/冰/蒸汽的热容量。")]
        public float terrainThermalMass=4;
        [Min(0), Tooltip("物体之间的传热速率，按双方热容量计算；每轮不超过热平衡需要的热量。")]
        public float bodyHeatConductivity=.45f;
        [Min(0), Tooltip("物体与河流地格之间的传热速率，同样考虑双方热容量。")]
        public float terrainHeatConductivity=.35f;
        [Header("第九世界：世界钟召唤仪式")]
        [Min(0), Tooltip("点击最终世界钟后，把玩家向左传送的距离，避免与刷怪点重叠。")]
        public float bossPlayerTeleportDistance=5;
        [Min(0), Tooltip("启动世界钟和 Boss 音乐后，多少游戏秒开始刷随机怪。")]
        public float bossEncounterMinionDelay=3;
        [Min(.1f), Tooltip("世界钟每次生成一只随机普通怪物的间隔，游戏秒。")]
        public float bossEncounterMinionInterval=1;
        [Min(0), Tooltip("启动仪式/Boss 音乐后，多少游戏秒召唤魔王。默认 15，配合音乐高潮。")]
        public float bossEncounterSpawnDelay=15;
        [Min(0), Tooltip("世界钟交互距离；鼠标点击和 E 都共用此距离。")]
        public float clockInteractionRadius=2.25f;
        [Min(.1f), Tooltip("鼠标距离世界钟中心多近视为点中了世界钟。")]
        public float clockClickRadius=1.1f;
        [Tooltip("最终世界钟启动后的颜色。")]
        public Color activeBossClockColor=new Color(.25f,.7f,1);
        [Header("核聚变：双高速迎面碰撞")]
        [Min(0), Tooltip("双方碰撞前速度都必须严格超过此值，才可触发核聚变。")]
        public float fusionMinimumSpeed=12;
        [Range(-1,0), Tooltip("双方速度方向点积须低于此值；越接近 -1，越要求迎面对撞。还会检查双方都沿法线靠近。")]
        public float fusionOpposingDot=-.2f;
        [Min(0), Tooltip("核聚变爆炸半径，世界单位。")]
        public float fusionRadius=3;
        [Min(0), Tooltip("爆炸伤害 = 接近方向相对动能 × 此系数，受撞击伤害上限限制。")]
        public float fusionDamagePerEnergy=.12f;
        [Min(0), Tooltip("核聚变对爆炸半径内实体施加的最大径向冲量。")]
        public float fusionImpulse=20;
        [Min(0), Tooltip("核聚变爆炸向实体输入的热量；温升仍除以目标热容量。")]
        public float fusionHeat=60;
        [Header("莱顿弗罗斯特气垫滑行")]
        [Tooltip("接触常温水或冰面时，物体温度须严格高于此值。")]
        public float glideHotTemperature=60;
        [Tooltip("常温液体的最高温度；蒸汽不触发，冰面不受此水温门槛限制。")]
        public float glideLiquidMaximumTemperature=40;
        [Min(0), Tooltip("触发后持续多久将地面滑动摩擦锁定为零，游戏秒。")]
        public float glideDuration=5;
        [Min(0), Tooltip("气垫沿接触法线持续施加的反推力；加速度随质量增大而减小。")]
        public float glideRepulsionForce=8;
        [Header("压强对流场：冷热径向风场")]
        [Min(0), Tooltip("冷热物体影响附近动态实体的风场半径；不要求质量门槛。")]
        public float pressureRadius=4;
        [Min(0), Tooltip("与环境温度之差超过此值才产生风场，避免常温噪声。")]
        public float pressureTemperatureDeadZone=10;
        [Min(0), Tooltip("风力 = 与环境的温差 × 此系数 × 距离衰减。热排斥，冷吸引；目标加速度反比于质量。")]
        public float pressureForcePerDegree=.025f;
        [Min(0), Tooltip("单个源对单个目标施加的最大风力。")]
        public float pressureMaximumForce=50;
        [Header("反冲工质：高温受热喷气")]
        [Tooltip("此温度以上的新增热量可用于消耗自身质量并产生反冲。")]
        public float recoilHotTemperature=200;
        [Range(0,1), Tooltip("超过温度门槛所需热量后的输入热量，分配给喷气的比例；默认 50%。")]
        public float recoilEnergyFraction=.5f;
        [Min(.001f), Tooltip("每消耗一单位质量需要的热量；数值越大，磨掉的质量越少。")]
        public float recoilEnergyPerMass=500;
        [Min(0), Tooltip("喷气速度上限；喷出质量 × 速度形成反推冲量，同时受输入能量限制。")]
        public float recoilExhaustSpeed=30;
        [Min(0), Tooltip("喷气可视微粒的寿命，秒；微粒仅用于表现，不占投射物池、不参与物理。")]
        public float recoilParticleLifetime=.4f;
        [Header("熔融粘滞与表面熔接")]
        [Tooltip("接触的双方温度都必须高于此值，才形成临时粘滞和法线拉力。")]
        public float weldingHotTemperature=200;
        [Min(0), Tooltip("高温表面动摩擦的基础系数。")]
        public float weldingBaseFriction=.6f;
        [Min(0), Tooltip("双方较低温度每高于熔接门槛一度，增加多少切向摩擦系数。")]
        public float weldingFrictionPerDegree=.08f;
        [Min(0), Tooltip("分离时的法线弹簧系数；越大越难从热接触面拉开。")]
        public float weldingSpring=80;
        [Min(0), Tooltip("法线分离速度的阻尼，抑制接触面的拉开。")]
        public float weldingDamping=8;
        [Min(0), Tooltip("单个熔接面法线拉力的上限。")]
        public float weldingMaximumForce=120;
        [Min(.02f), Tooltip("离开接触后维持临时熔接的时间；冷却或移除法则会立即解除。")]
        public float weldingContactGrace=.2f;
        [Min(1), Tooltip("同时维护的熔接面数量上限，限制密集场景开销。")]
        public int weldingMaximumPairs=64;
        [Header("伯努利尾流负压")]
        [Min(0), Tooltip("速度除以碰撞体直径，严格超过此值产生尾流；小物体更容易达到相对体积的高速。")]
        public float wakeSpeedPerDiameter=10;
        [Min(0), Tooltip("尾流半径 = 碰撞体直径 × 此倍率，受最大半径限制。")]
        public float wakeRadiusPerDiameter=2.5f;
        [Min(0), Tooltip("尾流最小影响半径。")]
        public float wakeMinimumRadius=1.2f;
        [Min(0), Tooltip("尾流最大影响半径，限制高速大物体的查询范围。")]
        public float wakeMaximumRadius=8;
        [Range(-1,1), Tooltip("后方和侧翼的方向范围；与前进方向点积大于此值的正前方目标不受影响。")]
        public float wakeForwardExclusionDot=.35f;
        [Min(0), Tooltip("尾流吸力 = 速度平方 × 占地面积 × 此系数 × 距离衰减；目标加速度反比质量。")]
        public float wakeForceCoefficient=.012f;
        [Min(0), Tooltip("单个尾流源对单个目标的最大吸力。")]
        public float wakeMaximumForce=50;
        [Tooltip("轻小高速爆炸法则的质量上限；质量小于此值才有资格爆炸。")]
        [Header("轻小高速爆炸（原法则）")]
        public float fissionMass = .3f;
        [Tooltip("轻小高速爆炸法则的速度门槛，单位：世界单位/秒。")]
        public float fissionSpeed = 10;
        [Tooltip("轻小高速爆炸所需最小动能；动能=0.5×质量×速度²。")]
        public float fissionMinimumEnergy = 6;
        [Tooltip("爆炸动能转为伤害的系数，最终伤害受 Impact Damage Cap 限制。")]
        public float fissionDamagePerEnergy = .6f;
        [Tooltip("满足爆炸条件后到爆炸的延迟，单位：秒。")]
        public float fissionDelay = .1f;
        [HideInInspector] public float vaporImpulse = 30;

        [HideInInspector] public float gravityRadius = 8;

        [HideInInspector] public float gravityForce = 30;

        [HideInInspector] public float gravityMassThreshold=3;

        [HideInInspector] public float gravityColdTemperature=-10;
        [HideInInspector] public float gravityDensityThreshold=12; // Historical benchmark only.
        [Header("冷热辐射、灼伤冰伤与蒸汽波")]
        [Tooltip("基础传热距离，单位：世界单位；附近实体交换热量的实际半径。")]
        public float thermalRadiationRadius=2.4f;
        [Tooltip("辐射圈扩大法则的倍率，同时扩大传热距离和可视外圈；1.5 表示扩大到 150%。")]
        public float expandedRadiationMultiplier=1.5f;
        [Tooltip("灼伤冰伤法则中，温度高于此值开始累积灼伤，单位：°C。")]
        public float burnTemperature=80;
        [Tooltip("灼伤冰伤法则中，温度低于此值开始累积冰伤，单位：°C。")]
        public float frostTemperature=-50;
        [Tooltip("连续处于灼伤温度时的扣血间隔，单位：秒；需启用灼伤冰伤法则。")]
        public float burnInterval=.5f;
        [Tooltip("每次灼伤扣除的生命，需启用灼伤冰伤法则。")]
        public float burnDamage=2;
        [Tooltip("连续处于冰伤温度时的扣血间隔，单位：秒；需启用灼伤冰伤法则。")]
        public float frostInterval=2;
        [Tooltip("每次冰伤扣除的生命，需启用灼伤冰伤法则。")]
        public float frostDamage=6;
        [Tooltip("骤冷蒸汽冲击波要求降温前至少达到的温度，单位：°C。")]
        public float steamHotThreshold=100;
        [Tooltip("骤冷蒸汽冲击波要求一次降温的最小幅度，单位：°C。")]
        public float steamCoolingThreshold=60;
        [Tooltip("骤冷蒸汽冲击波作用半径，单位：世界单位。")]
        public float steamWaveRadius=3;
        [Tooltip("蒸汽冲击波推动周围动态实体的基础冲量。")]
        public float steamWaveImpulse=18;
        [Tooltip("同一物体两次蒸汽冲击波之间的最短间隔，单位：秒。")]
        public float steamWaveCooldown=.5f;
        [Header("拥挤效应：角色周围检测")]
        [Min(0), Tooltip("玩家/怪物周围允许的其他实体数量；超过后该角色持续扣血。计入怪物、玩家、投射物、石头、树木和巨石，不计自身。")]
        public int crowdingItemThreshold=6;
        [Min(0), Tooltip("以角色碰撞体中心为圆心的拥挤检测半径，单位：世界单位；附近物体的碰撞体进入圆内就计数。")]
        public float crowdingRadius=2.5f;
        [Min(.1f), Tooltip("每个角色独立检测周围数量的间隔，单位：秒；首次错开检测时间，期间按缓存数量持续扣血。")]
        public float crowdingCheckInterval=1;
        [Min(.01f), Tooltip("触发拥挤时提示环的线宽，单位：世界单位；环的半径直接使用拥挤检测半径。")]
        public float crowdingRingWidth=.06f;
        [Tooltip("拥挤提示环的颜色；透明度随脉动变化，与冷热外圈独立。")]
        public Color crowdingRingColor=new Color(1,.58f,.15f,.9f);
        [Min(0), Tooltip("超过数量门槛后，每多一个周围实体，该角色每秒增加的拥挤伤害。")]
        public float crowdingDamagePerExcessPerSecond=3;

        [HideInInspector] public float repulsionTemperatureGap = 80;

        [HideInInspector] public float repulsionRange = .6f;

        [HideInInspector] public float repulsionPerDegree = .3f;
        [Header("旧极寒脆性（已移出随机池）")]
        [Tooltip("旧极寒脆性法则的冻结温度门槛，单位：°C；该法则已移出随机池。")]
        public float shockFrozenTemperature = -50;
        [Tooltip("旧极寒脆性法则的骤热温度门槛，单位：°C；该法则已移出随机池。")]
        public float shockHotTemperature = 100;
        [Tooltip("旧极寒脆性要求从冻结升到高温的时间窗口，单位：秒；该法则已移出随机池。")]
        public float shockWindow = 1;
        [Tooltip("旧极寒脆性按温差和质量计算伤害的系数；该法则已移出随机池。")]
        public float shockDamagePerDegreeMass = .6f;
        [Header("热胀冷缩与挤压伤害")]
        [Tooltip("热胀冷缩法则中，每度温差造成的线性尺寸倍率变化。")]
        public float sizeTemperatureSlope = .002f;
        [Tooltip("热胀冷缩的最小线性尺寸倍率。")]
        public float minimumSizeFactor = .6f;
        [Tooltip("热胀冷缩的最大线性尺寸倍率。")]
        public float maximumSizeFactor = 1.8f;
        [Tooltip("检查并尝试膨胀的时间间隔，单位：秒。")]
        public float expansionInterval = .2f;
        [Tooltip("每次检查允许的最大线性尺寸倍率变化；越大膨胀越快。")]
        public float expansionStep = .12f;
        [Tooltip("被膨胀物体夹住时，按尺寸增长计算挤压伤害的系数。")]
        public float crushDamagePerSize = 120;
        public float PlayerHealthAt(int world) => Mathf.Round(playerHealth);
        public const float EnemyAttackSpeedMultiplier=1.75f;
        public float EnemyAttackTime(float baseDuration) => baseDuration/Mathf.Max(.01f,enemyAttackSpeedMultiplier);
        public float AttackImpulse(EnemyArchetype type)
        {
            switch(type)
            {
                case EnemyArchetype.Light:return Mathf.Max(0,lightAttackImpulse);
                case EnemyArchetype.Heavy:return Mathf.Max(0,heavyAttackImpulse);
                case EnemyArchetype.Guard:return Mathf.Max(0,guardAttackImpulse);
                case EnemyArchetype.Boss:return Mathf.Max(0,bossAttackImpulse);
                default:return Mathf.Max(0,mediumAttackImpulse);
            }
        }
        public float EnemyHealthAt(float baseHealth,int world) => Mathf.Round(baseHealth*Mathf.Pow(enemyHealthGrowth,Mathf.Max(0,world-1)));
    }
}
