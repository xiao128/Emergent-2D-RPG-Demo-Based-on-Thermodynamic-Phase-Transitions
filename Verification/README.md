# 本次：第九关仪式与扩展物理法则

热磨损已改为每米固定损失 0.2 质量；核聚变、气垫滑行、反冲工质、粘滞熔接、伯努利尾流进入随机池，压强对流替换旧冷质量引力，旧温差互斥移除。温度上限 -1000～+1000，热输入统一按质量/比热容折算。参数见 TUNING.md，各项有中文 Tooltip。

本次累计 265 项受控检查全部通过，Unity Console 最终无错误；下方此前记录保留为历史快照，旧法则的历史结论不代表当前玩法。

| 报告 | 通过项 | 验证重点 |
| --- | --- | --- |
| expanded-physics-regression.txt | 44 | 新随机池、固定磨损、质量传热、原生对撞核聚变、风场方向与质量、喷气/树木缩小、熔接、气垫 |
| friction-wear-regression.txt | 35 | 固定质量损失、40°C/8 门槛、模型/碰撞体缩小、普通走路、重置 |
| boss-entrance-regression.txt | 21 | 实际音轨、3/1/15 秒召唤、暂停同步、零初始敌人、蓝钟、安全传送、回钟确认及五秒结算 |
| glide-timing-regression.txt | 3 | 实际五秒持续，离开冰面/清温不提前取消，接触摩擦与阻力归零及恢复 |
| progression-regression.txt | 48 | 九世界守卫/碎片/选法则流程，空最终关、仪式、魔王死后回钟通关、胜败重启 |
| architecture-regression.txt | 41 | 物理伤害、冷热球、共享法则、实体池和 UI 回归 |
| stone-charge-regression.txt | 33 | 冷热石弹蓄力、尺寸/质量、无初动量、F 推动及安全生成 |
| thermal-law-regression.txt | 40 | 压强场、辐射、灼伤冰伤、恢复技能与高温怪物攻速 |

音乐同步实测：魔王出场计时 15.01866 秒，AudioSource 播放位置也为 15.01866 秒。空白关/启钟为实际场景状态，Game View 见 Captures/ninth-world-blue-clock.png。气垫 4.7057 秒时仍有零阻力、18.8 水平速度；5.15 秒后普通摩擦与物理材质恢复。

性能记录 flow-field-density.txt：250 个投射物挤在约 3×3 区域，同时启用压强与尾流，三轮包含热交换的平均法则 Tick 为 94.64ms（优化前约 129.93ms）。每个目标每轮仅一次风场物理施力，最多 250 次，邻居候选仍有 62500。这是 Editor 极端密集测量，不是正常场景帧率；高密度热交换仍有开销。缓存位置/热容量，发生喷气失重时立即刷新热容量；复用网格和邻居列表，熔接最多 64 对，喷气每实体最多 24 个视觉微粒。

受控验证不替代自然试玩与平衡评估。旧专门验证冷质量门槛或温差互斥的脚本为历史夹具，应使用上述新入口检验当前法则。

---

# 当前验证结果

2026-10-05 拥挤/河岸/碎片/魔王：crowd-river-boss-regression.txt 共 41 项通过；原架构 41 项、怪物攻击 39 项重跑通过，共 121 项受控检查。拥挤改为角色周围半径统计所有基本实体，超过 6 个伤害角色，检测每角色独立计时并错开，默认 1 秒，0.3/0.5 参数实际生效；同体多碰撞体去重、缓存继续扣血、移除法则清缓存均通过。20 角色与 250 密集投射物测试，单独 20 次拥挤查询约 3.83ms；包括热交换的强制集中整轮约 63.64ms，这是 Editor 的人为极密集场景单次测量，不代表实际帧率，旧热交换仍是密集场景主要成本。

真实 Physics2D 河岸碰撞：高速怪物受击 58.67，步行速度不受伤，撞击升温传给实体与河岸，冰面可过，地格交界只算一次。碎片远处真实掉落生成白边金色菱形，拾取清除、不拦截输入；Game View 为 Captures/clock-fragment-minimap.png。魔王远处索敌、越过旧出生点半径主动追击、瞄准加八向弹幕、48 冲量、攻速/间隔、每次一名守卫及 20 秒后排程、暂停/禁用不召唤通过。普通攻击回归固定草地基准，另保留粗糙走廊验证，避免随机粗糙地块使均匀地面行程比较失效。

第八世界实际计时另见 boss-live-timing.txt：37.20 游戏秒时魔王仍锁定玩家，离出生点 57.61、距玩家 2.16，已发射 25 次弹幕并召唤 1 名守卫；下一次召唤时间表明首次于约 20.02 秒发生。该计时夹具临时提高双方生命以避免测试提前结束，不是平衡或自然通关验证。退出 Play 后恢复作者配置。

新增入口：`PhaseArena.ArenaCrowdRiverBossVerification.Run();`（Play 模式的受控夹具）。字段与默认值见根目录 TUNING.md。新增独立魔王质量、移动速度/推动力、前摇/冷却/恢复及环形弹数量参数，验证运行修改确实影响实体、攻击计时与弹幕数量。

2026-10-05 中文 Tooltip 与冷质量重力畸变：WorldTuning 全部 115 个可见字段有中文 Tooltip，分组改为中文；tooltip-settings.txt 确认实际资产回血比例 0.2、冷温门槛 -10、质量门槛 3。thermal-law-regression.txt 现为 45 项全部通过：高质量热石不吸引，满蓄冰石吸引温暖目标，小冷球不吸引，质量等于或低于门槛不触发，冷温边界严格比较，运行修改冷门槛即时生效，加热停止引力，冷重怪物共用规则；回血按配置与升级后生命上限计算，保留清除状态、冷却、不耗法力和不产生蒸汽波。架构 41 项重跑通过，本轮共 86 项受控运行检查。Game View 见 Captures/cold-mass-gravity-card.png。字体已重新烘焙为 Static，测试后退出 Play 并选中 WorldTuning；旧版“冷热重石均吸引”的记录为历史行为，已由本次冷温条件替代。

2026-10-05 调参项公开：tuning-regression.txt 共 29 项通过，验证实际死亡的双倍经验、小数累积与重置、零倍率、自定义升级需求/生命法力成长、降低冲量不被旧速度下限覆盖、单体覆盖、类型与质量独立、攻速修改、NPC/Boss 分开发射冲量，以及 public/Inspector 序列化字段。既有怪物攻击 39 项和成长 23 项重跑通过，默认效果保留。本轮合计 91 项受控检查。入口：`PhaseArena.ArenaTuningVerification.Run();`。字段说明见根目录 TUNING.md。最后一次 Play 模式脚本重载曾导致 MCP 反复掉线，已通过一次性 Editor 保存操作恢复并移除辅助脚本；tuning-final-save.txt 确认 Play=False、场景已保存、MissingScripts=0。

2026-10-05 怪物冲撞修正：enemy-attack-regression.txt 共 39 项通过，覆盖小/中/大怪、守卫、Boss 实际冲量与位移、前摇方向线、粗糙地面、出生时阻力估算、范围内外索敌、边缘目标真实命中与推开、冷却期间站位、低摩擦法则、普通/石弹发射和玩家静止成球。实测普通怪草地 6.454 米、粗糙地面 4.946 米，守卫草地 7.315 米，Boss 草地 8.176 米；预测与物理解算相符。架构 41 项与成长 23 项重跑通过。入口：`PhaseArena.ArenaEnemyAttackVerification.Run();`。这是受控物理验证，实际地形障碍与试玩平衡另外评估。

2026-10-05 较早版本（引力当时不限制温度）：thermal-law-regression.txt 当时共 39 项通过，验证按实际质量引力、满蓄冷热石弹吸引、小法球不吸引、辐射距离与跨网格传热、外圈大小与膨胀组合、高温怪物攻速、空格回血与状态清除，以及真实间隔的灼伤/冰伤扣血。原架构 41 项、蓄力石弹 33 项、受击反馈 16 项重跑通过，均 failures=0。加上温度细边的 12 项，本轮合计 141 项受控检查通过；不是自然通关或全部法则组合平衡测试。

temperature-border-regression.txt 共 12 项通过：极热红边、极寒蓝边、2 像素宽度、区间阈值、低血量暗边共存、空格清除、死亡隐藏、点击不拦截与新增字形。画面见 Captures/hot-temperature-border-1.png、cold-temperature-border-1.png、new-thermal-law-cards-1.png。测试后退出 Play，未保存运行夹具。字体通过既有 BakeWorldFont 工具重新烘焙，维持 Static 模式。

入口：`new UnityEngine.GameObject("Thermal Law Verification").AddComponent<PhaseArena.ArenaThermalLawVerification>().Run();`；`PhaseArena.ArenaTemperatureBorderVerification.Run();`。均在 Play 模式执行，会控制测试场景。

持续蓄力石头：stone-charge-regression.txt 全部通过，覆盖刚成形/中途/满蓄/超时释放、预览与实体一致、碰撞体、二维面积质量增长、1.5 倍上限、冷热效果、F 推动、窄空间拒绝及单次扣法力。stone-charge-movement.txt 另验证满蓄力时实际移动仍有效。原 41 项架构回归和 23 项成长/冷热石弹回归重跑通过。入口：`PhaseArena.ArenaStoneChargeVerification.Run();`。画面比较见 Captures/charged-stone-sizes-2.png；场景对照夹具未保存。

撞击伤害门槛：impact-threshold-regression.txt，实机物理接触验证速度 6 撞固定石头伤害为 0、速度 6.2 伤害为 19.376，速度恰好达到 6.1 时不扣血，均通过。阈值设为当前场景玩家移动速度 6 +0.1；撞击升温单独保留 1.8 阈值。原 41 项架构回归重跑通过。

受击反馈新增验证：hit-feedback-regression.txt 共 16 项全部通过；同时重跑架构 41 项和成长/冷热石弹 23 项，均无失败。覆盖 public 时长、闪烁/无敌同步、连续伤害阻挡、20% 边界、边框淡出、重置及一秒免死保护兼容。窄时间窗测试通过受控时间戳驱动实际伤害与渲染，避免低帧率 Editor 的等待越过 0.1 秒窗口。入口：`new UnityEngine.GameObject("Hit Feedback Verification").AddComponent<PhaseArena.ArenaHitFeedbackVerification>().Run();`。

画面确认：Captures/low-health-hit-bright.png 与 low-health-hit-dim.png，受控冻结时钟分别截取同一次受击的两个闪烁阶段；最终退出 Play，未保存夹具。

2026-10-03 在运行中的 Unity Editor 完成；最终退出 Play，不保存测试夹具。

2026-10-05：新增 growth-regression.txt（21 项），验证石弹、经验分档、升级与重开、跨关上限、免死边界与持续伤害免疫、实际远程 AI 前摇后投掷。原 41 项架构和 40 项流程回归重新运行。性能文件仍是 10-03 的历史数据，未在本轮重跑。

新增测试入口：`new UnityEngine.GameObject("Growth Verification").AddComponent<PhaseArena.ArenaGrowthVerification>().Run();`，在干净 Play 会话使用。画面记录：Captures/stone-growth-ui.png。

基础法力改为 20 后的画面：Captures/stone-growth-ui-1.png（升级至二级，上限 30）。

挥石魔法语义修正：石弹保留冰火温度与调温效果，不是常温石弹。growth-regression.txt 已重跑为 23 项全部通过，包含火石/冰石初温、调温热量和热脉冲只释放一次。冷石实际冷却另见 cold-stone-cooling.txt；当前画面见 Captures/hot-cold-stones.png。

架构测试临时给足多次施法所需法力，结束恢复作者配置。流程测试的失败分支使用小额致死伤害，避免把新的爆发免死机制误判为不能失败。

| 文件 | 内容 |
| --- | --- |
| architecture-regression.txt | 架构/物理功能回归，failures=0 |
| progression-regression.txt | 七次选法则、第八周目 Boss、胜败重开，failures=0 |
| law-performance.txt | 250 分散法球的法则 Tick CPU 对比 |
| law-performance-dense.txt | 250 极密集法球的同条件对比 |
| Captures/refactor-bars.png | 玩家血蓝条和树木血条画面确认 |

## 复测

进入 PhaseArenaDemo 的 Play 并开始试炼，通过 Editor C# 执行：

```csharp
PhaseArena.ArenaArchitectureVerification.Run();
```

另起干净 Play 会话检查流程：

```csharp
new UnityEngine.GameObject("Progression Verification")
    .AddComponent<PhaseArena.ArenaProgressionVerification>().Run();
```

性能检查各自在干净 Play 会话运行：

```csharp
PhaseArena.ArenaArchitectureVerification.Benchmark(false); // 分散
PhaseArena.ArenaArchitectureVerification.Benchmark(true);  // 极密集
```

测试会控制夹具、禁用行为或修改运行态，完成后退出 Play 再进入手动试玩；不要保存测试运行态。报告写入本目录。

流程测试使用直接伤害击败守卫/Boss检查状态，不是自然通关。性能采用 dt=0 固定物体，只对比法则 Tick，不代表整局帧率。

## 历史记录

2026-10-05 拥挤提示／延迟结算：`crowding-outcome-regression.txt` 29 项通过，死亡显示实测约 5.02 秒；检查真实掉血与提示环、缩放角色上的检测半径、无额外查询、物品无提示环、玩家销毁与等待、死亡／胜利结算、重建玩家参数／动画引用、旧弹窗取消。八关流程和菜单音频分别重跑 40／42 项通过。菜单音效夹具已排除随机障碍阻挡，并检查实际成功发射。截图：`Captures/crowding-ring.png`、`Captures/defeat-before-result.png`、`Captures/defeat-result-delayed.png`。这些是受控夹具验证，不是自然通关或战斗平衡测试。

2026-10-05 菜单／音频：`menu-audio-regression.txt` 42 项通过，`progression-regression.txt` 本轮重跑 40 项通过，`menu-ui-pointer-checks.txt` 7 项指针／脚步／Editor 退出检查通过。界面截图为 `Captures/main-menu-1.png` 和 `Captures/esc-settings-final.png`。检查音频 Source 播放状态、音量和资源引用，不代表扬声器听感验证；发布版退出未运行独立构建验证。详情见项目根目录 AUDIO_AND_MENU.md。

LegacyTests 保存旧 ArenaPlayVerification、ArenaFeedbackVerification 和 meta；旧断言包含热物碰水反冲及旧施法，所以移出编译目录，未删除。ArenaProjectileVerification 保留但本轮未重跑，旧结果不作为本轮通过证据。

Assets 中 LegacyLawTickBenchmark 仅 Editor 编译，用于旧 Tick 对照。BeforeRefactor-20261002 是重构前快照；refactor_*.py 等是一次性生成记录，请勿重复应用。

最终保存检查：Play=False，场景无 Missing Script，Prefab 与场景小蓝条引用均有效，树木仍为 CapsuleCollider2D。编译无错误；旧 Editor 生成器仍有 TextureImporter.spritesheet 弃用警告，MCP 重载期间记录过一次 WebSocket 传输警告，连接恢复后保存与查询成功。
# 摩擦生热与热磨损拆分验证

`PhaseArena.ArenaFrictionWearVerification.Run()`：friction-wear-regression.txt 共 35 项通过。覆盖独立随机法则、40°C/8 严格门槛、15% 每米质量损失、步长一致性、实时调整参数、所有动态实体、半血玩家正常走路、外形与碰撞面积同步缩小、热胀冷缩叠加、重置与低质量消失。架构 41 项、挥石蓄力 33 项、温度法则 45 项、八世界流程 40 项回归通过。额外动画检查确认敌人动画不覆盖磨损尺寸，玩家动画位置偏移随尺寸缩放；见 friction-wear-animation.txt。Game View 对照截图：Captures/friction-wear-shrink.png，玩家/怪物/右侧石头剩余 25% 质量，右侧石头长宽为左侧完整石头的一半。受控验证不代替实际试玩平衡。

