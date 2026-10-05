# 调参数的位置

在 Unity 的 Project 窗口选择 `Assets/ArenaDemo/WorldTuning.asset`，右侧 Inspector 即可修改全局参数。所有可见参数均有中文分组和鼠标悬停提示，都是 public 序列化字段。

WorldTuning 的经验倍率、各类冲量、攻速等支持运行时直接读取，新击杀/新攻击会采用当前设置。建议先停止 Play，再修改资产以保留配置；若运行时试调，结束后检查数值并保存。场景对象和运行时生成的怪物在 Play 中修改，退出后会恢复；要长期修改单个类型，请修改对应 Prefab。

## 经验与升级（全局设置）

| Inspector 字段 | 中文作用 | 当前初始值 |
| --- | --- | --- |
| Experience Gain Multiplier | 经验获取倍率：2=双倍，0.5=一半，0=关闭经验获取 | 1 |
| First Level Experience | 一级升二级需要的经验 | 100 |
| Experience Increase Per Level | 每升一级，下一次需求增加的经验 | 50 |
| Health Gain Per Level | 每级最大生命增长，同时回复同等生命 | 10 |
| Mana Gain Per Level | 每级最大法力增长，同时回复同等法力 | 10 |

不足 1 点的经验会累积。例如小怪基础经验 10，倍率 0.05 时，每两次击杀合计获得 1 点；重开会清除这部分累计。需求参数修改后，HUD 会显示新需求，下一次获得经验时检查升级。倍率影响环境击杀和直接击杀。

单种怪物的基础经验在 Prefab 的 **EnemyBrain → Experience Reward**：LightSlime 小怪 10、MediumSlime 中怪 20、HeavySlime 大怪 30、RangedSlime 远程 20、ClockGuard 守卫 40、DemonKing Boss 100。实际经验=基础经验×全局倍率。

## 各类怪物冲撞冲量（直接生效）

| Inspector 字段 | 怪物 | 当前初始值 |
| --- | --- | --- |
| Light Attack Impulse | 小怪 | 24 |
| Medium Attack Impulse | 中怪 | 54 |
| Heavy Attack Impulse | 大怪 | 120 |
| Guard Attack Impulse | 守卫 | 136 |
| Boss Attack Impulse | Boss | 228 |

现在直接使用这些值，调小也生效，0 表示不施加冲撞冲量。起步速度=冲量÷当前质量，仍受速度上限约束。改变质量不会改变怪物类型，但会自然改变速度和撞击结果；攻击范围及方向提示随速度和地面阻力重算。

Prefab 的 **EnemyBrain → Archetype** 选择对应类型，已有六种 Prefab 已配置。**Lunge Impulse Override** 为 -1 时使用全局数值；改为非负数后，仅覆盖该怪物的冲撞冲量。远程怪物主攻是投射物，不会使用近战冲撞冲量。

旧的 Enemy Lunge Impulse、Guard Lunge Impulse、Boss Lunge Impulse 以及 Lunge Speed 字段已隐藏并停止参与计算，避免“把冲量调低却被起步速度下限覆盖”。它们仍留在序列化数据中以兼容旧记录。

## 怪物攻击节奏、范围与远程投射物

| Inspector 字段 | 作用 | 当前初始值 |
| --- | --- | --- |
| Enemy Attack Speed Multiplier | 基础攻速倍率；提高会缩短前摇与冷却 | 1.75 |
| Enemy Windup | 基础前摇秒数，实际再除以攻速倍率 | 0.85 |
| Enemy Attack Cooldown | 基础攻击冷却秒数 | 3.5 |
| Enemy Recovery | 基础恢复秒数，近战至少覆盖物理冲撞窗口 | 1.2 |
| Enemy Lunge Duration | 冲撞保留时间，AI 在此期间不会提前反向拉回 | 0.8 |
| Attack Reach Safety | 理论可造成撞击伤害距离的安全比例 | 0.9 |
| Enemy Stand Off Fraction | 冷却期间站位距离占攻击范围的比例 | 0.75 |
| Enemy Alert Hysteresis | 离开攻击范围后，额外多远才解除警戒 | 1 |
| Enemy Projectile Impulse | 普通/远程怪物法球和石弹的发射冲量 | 24 |
| Boss Projectile Impulse | Boss 弹幕独立发射冲量 | 48 |
| Ranged Attack Range | 远程怪物索敌和发射范围 | 9 |
| Hot Enemy Temperature | 高温攻速加成的触发温度 | 80 |
| Hot Enemy Attack Multiplier | 高温时额外攻速倍率 | 1.5 |

玩家法球仍然静止生成，以上发射冲量只作用于敌方。撞击伤害仍由质量和碰撞速度换算；如果把冲量降得很低，怪物可能无法达到 Impact Threshold 对应的伤害速度。

## 已经可以直接修改的其他参数

WorldTuning 中还有法杖冲量/伤害、成球时间、石弹最大尺寸、物体速度上限、撞击伤害系数、灼伤/冰伤间隔与伤害、温度上下限、辐射范围、引力阈值/强度、回血比例和冷却等。

玩家的基础法力、施法消耗和回蓝速度在场景玩家对象的 **PlayerMage** 组件；受击闪烁/无敌时间在 **PlayerHitFeedback → Invulnerability Duration**。角色的质量、移动驱动力和步行速度在 **ThermoBody**。这些组件字段也已公开，不需要改代码。

## 空格技能：回血并恢复常温

选择 WorldTuning.asset，在同名中文分组中修改：

| Inspector 字段 | 中文作用 | 当前值 |
| --- | --- | --- |
| Recovery Health Fraction | 恢复当前最大生命的比例，0.2=20%，不是填写 20 | 0.2 |
| Recovery Cooldown | 技能冷却，单位：秒 | 7 |

升高最大生命后，回血量也按新上限计算；不会超过满血。同时清除冷热状态，不耗法力。

## 冷质量重力畸变

位于「撞击升温、磨损、爆炸与冷质量引力」分组，需要抽到该法则才生效。

| Inspector 字段 | 中文作用 | 当前值 |
| --- | --- | --- |
| Gravity Cold Temperature | 引力源温度必须低于此值，单位：°C | -10 |
| Gravity Mass Threshold | 引力源实际质量必须大于此值，等于不触发 | 3 |
| Gravity Radius | 吸引范围，单位：世界单位 | 8 |
| Gravity Force | 吸力强度，随距离衰减 | 30 |

两项门槛同时满足才产生引力。质量 1 的小冷球不触发，质量约 3.402 的满蓄冰石会触发，大热石不触发；加热或磨损使其不再达标就停止吸引。角色、物品和双方投射物共用判断；被吸引的目标只需是附近可移动实体，不必低温。

## 摩擦生热与热磨损

WorldTuning「摩擦生热与热磨损（两个独立法则）」分组，四项均有中文 Tooltip。两个法则各自抽取，任意一项都能独立生效。

| Inspector 字段 | 作用 | 默认值 |
| --- | --- | --- |
| Friction Heat Gain | 速度平方 × 地面摩擦阻尼 × 此系数 × 时间产生温升；冰面不生热 | 0.09 |
| Abrasion Speed Threshold | 速度严格高于此值才磨损，世界单位/秒 | 8 |
| Abrasion Hot Temperature | 温度严格高于此值才磨损，°C | 40 |
| Abrasion Per Meter | 每米损失的剩余质量比例，0.15 表示 15% | 0.15 |

热磨损要求同时满足速度和温度门槛。每滑行 d 米，剩余质量乘以 `(1 - Abrasion Per Meter)^d`，不受步长影响。Minimum Mass Fraction 继续作为相对初始质量的消失门槛，默认 0.1。玩家、怪物和物品同样适用，普通步行速度 6 不磨损。

模型和碰撞体长宽按剩余质量的平方根缩小，让二维占地面积与质量同比变化；温度膨胀另外乘入。质量减半时长宽约为原来的 71%。缩小不改变当前速度，移除法则也不会自动补回已损失的质量；重新开始会复原。

## 拥挤效应：角色周围检测

位于 WorldTuning 的「拥挤效应：角色周围检测」分组。需要抽到拥挤法则才生效。以玩家/怪物碰撞体中心为圆心，其他实体碰撞体进入半径就计数，排除自身、死亡、未激活和未成形预览。计入其他怪物/玩家、双方投射物、石头、树木及固定巨石，同一实体多个碰撞体只算一次。超过门槛后受伤的是该角色。

| Inspector 字段 | 作用 | 当前值 |
| --- | --- | --- |
| Crowding Radius | 检测半径，世界单位 | 2.5 |
| Crowding Item Threshold | 允许的周围实体数量；6 不扣血，7 开始扣血 | 6 |
| Crowding Check Interval | 每个角色独立查询的间隔，秒；支持 0.5、0.3，最低 0.1 | 1 |
| Crowding Ring Width | 触发拥挤时橙色提示环的线宽，世界单位 | 0.06 |
| Crowding Ring Color | 提示环颜色，透明度随脉动变化 | 橙色 |
| Crowding Damage Per Excess Per Second | 每超过一个实体，该角色每秒增加的伤害 | 3 |

首次查询按角色错开时间，其后每个角色独立计时。两次查询之间按缓存数量和经过时间扣血；更改间隔不改变每秒伤害系数。进入/离开拥挤区最多延迟一个检测间隔生效；玩家受击保护仍然生效。检测使用可复用的 NonAlloc 查询数组与去重集合，只在容量不够时扩容。

角色周围缓存数量超过门槛时显示橙色脉动环，半径直接使用 Crowding Radius；玩家和怪物都有提示，物品不显示。提示不添加物理碰撞体、不进行额外的邻居查询。数量回到门槛以内或移除法则时隐藏，出现／消失沿用缓存更新时机。角色热胀冷缩不会改变环表示的实际检测范围。

## 死亡与通关结算

WorldTuning「结算展示」分组的 **Result Display Delay** 默认 5，单位为真实秒。死亡或通关时立即停用并销毁当前玩家，镜头停在现场；期间不显示结算，HUD 保留死亡原因或胜利提示。5 秒后显示结算，等待不受 Time.timeScale=0 影响。R 快捷重开在结算显示后启用。

重开或返回菜单会取消尚未显示的结算，避免新一局突然出现旧弹窗。重新创建玩家使用运行开始时复制的场景初始配置，保留玩家的手调参数和动画引用；等级、经验等本局成长仍正常重置。

## 魔王追击、弹幕与召唤

| Inspector 字段 | 作用 | 当前值 |
| --- | --- | --- |
| Boss Base Mass | 魔王基础质量；实际质量仍受磨损与法则影响 | 6 |
| Boss Move Speed | 魔王追击步行速度上限，不限制冲撞速度 | 2 |
| Boss Drive Force | 魔王步行推动力，影响加速 | 40 |
| Boss Windup | 魔王基础近战前摇，秒；实际除以攻速 | 1.1 |
| Boss Attack Cooldown | 魔王独立近战冷却，秒；实际除以攻速 | 3.5 |
| Boss Recovery | 魔王独立恢复时间，秒；实际除以攻速，至少覆盖冲撞窗口 | 1.2 |
| Boss Aggro Radius | 初次锁定玩家的距离；锁定后持续追逐，不受出生点范围限制 | 60 |
| Boss Projectile Impulse | 弹幕和瞄准弹的冲量，原值 24，现为双倍 | 48 |
| Boss Volley Interval | 基础弹幕间隔，秒；实际除以全局和高温攻速 | 3 |
| Boss Radial Projectiles | 每次环形弹数量；另外有一发瞄准弹，0=只发瞄准弹 | 8 |
| Boss Summon Interval | 每召唤一个守卫的间隔，游戏秒；不受攻速影响，暂停不计时 | 20 |
| Boss Summon Distance | 召唤位置与魔王的基础距离，世界单位 | 3 |

弹幕包含一发朝玩家瞄准的投射物及八发环形投射物，与近战恢复计时分离。召唤寻找附近空旷陆地，放不下时每秒重试，避免把守卫生成在障碍内部。召唤守卫沿用世界生命成长；Boss 战中不掉世界钟碎片。

Boss Health 在「世界与生成数量」分组，仍是最终 Boss 的基础生命，实际乘以 Enemy Health Growth 的世界成长；Boss Attack Impulse 在「各类怪物冲撞冲量」分组，其余魔王独立参数统一放在「魔王追击、弹幕与召唤」。移动速度/推动力/基础质量以 WorldTuning 为准，修改魔王 Prefab 对应字段不再覆盖这些全局配置。
