# 开始界面与音频

打开 `Assets/Scenes/PhaseArenaDemo.unity`，点击 Unity Play。

开始界面提供「开始游戏」「设置」「退出游戏」，没有继续游戏。Esc 打开或关闭设置；游戏中打开设置会暂停模拟，但背景音乐继续。设置提供音乐、音效两个独立滑杆、返回游戏、返回主菜单和退出游戏。主菜单的设置将「返回游戏」显示为「关闭设置」，隐藏「返回主菜单」。

返回主菜单会结束当前局：清理怪物、投射物、碎片、法则和蓄力预览，重置等级、经验、生命和法力。再次开始生成新的一局。只用 PlayerPrefs 保存两路音量，没有长期游戏存档。在 Editor 点击退出会停止 Play；发布版调用 Application.Quit。

死亡和通关先销毁当前玩家并保留现场，5 秒真实时间后显示结算。等待期间返回主菜单会取消延迟弹窗，并依据场景初始配置重新创建玩家；玩法成长仍清零。

## 美术

`Assets/Resources/魔法时钟竞技场.png` 是用户的参考图，保留原文件。

新绘背景位于 `Assets/ArenaDemo/Art/MainMenuBackground.png`，使用内置 imagegen，以参考图的悬浮时钟、法师、史莱姆、森林遗迹构图绘制。背景没有烘焙文字或按钮；标题、按钮和音量滑杆全部是 uGUI/TMP 控件，可以点击和修改。生成提示记录在 `ArtSources/MainMenu/prompt.md`。背景使用 Point 采样、关闭 mipmap；自动填满屏幕，宽高比不同时裁掉边缘，菜单内容缩放以完整显示。

## 音频对应

| 文件 | 触发 |
| --- | --- |
| Music/StartSceneMusic.mp3 | 主菜单，包括主菜单设置 |
| Music/NormalMusic.mp3 | 第 1–8 关，包括暂停和选法则 |
| Music/BossMusic.mp3 | 最终关，直到返回主菜单 |
| 鼠标点击1-xys20070412.wav | 按钮悬停 |
| 鼠标点击2-xys20070412.wav | 按钮点击 |
| F打击.wav | 玩家 F 挥杖 |
| 拍打2.wav | 玩家实际掉血 |
| 敌人发射法球.wav | 远程怪和魔王发射 |
| 史莱姆死亡.wav | 普通怪和守卫死亡 |
| 小／中／大史莱姆冲撞 | 对应怪物开始冲撞，守卫使用大怪音效 |
| 土石类击中－mcx20070509.wav | 造成撞击伤害的物体碰撞 |
| mud_06.ogg | 造成伤害的河岸撞击 |
| 脚步-草地.wav / 脚步-冰上.wav | 玩家有移动意图且实际移动，依据脚下地形切换 |

音效完整路径在 `Assets/SoundsEffect/`。已有的 13 个音效均已接入。暂时没有单独的玩家蓄力／施法、回血和魔王吼叫／召唤音效，未使用敌人发射声替代玩家施法。

## 调整位置与职责

Hierarchy 的 **Arena Audio** → ArenaAudio 可以修改全部音频引用、默认音乐／音效音量、同时播放的游戏音效声道数（默认 16）和可听距离（默认 24）。保存过的音量优先于默认音量；玩家可直接在设置面板修改。长音乐使用 Streaming 导入，同一首不会每帧重启。

ArenaAudio 负责音乐选曲与固定音效通道；ArenaMenuUI 只处理设置和菜单操作；ArenaUiSound 接按钮事件；ArenaFootsteps 处理脚步。已有的玩家、怪物、碰撞代码只在实际事件处发出音效请求。相同音效短时间内合并，远离玩家的音效不播放。菜单缩放只在比例改变时修改 Transform。

菜单和声音的场景引用已保存。需要重新生成这些界面时，可在停止 Play 后使用 `Tools > Phase Arena > Install Main Menu and Audio`；它只重建菜单与音频，不重置 WorldTuning、敌人 Prefab 或其他玩法参数。

## 验证

2026-10-05：菜单／音频自动检查 42 项通过，八周目流程回归 40 项通过，指针、脚步和退出检查 7 项通过。编译和运行控制台无错误。验证了音频引用、实际 AudioSource 播放状态和音量，没有对扬声器听感或发布版退出做验证。

- `Verification/menu-audio-regression.txt`
- `Verification/progression-regression.txt`
- `Verification/menu-ui-pointer-checks.txt`
- `Verification/Captures/main-menu-1.png`
- `Verification/Captures/esc-settings-final.png`

自动检查将世界编号设为 1–8 来验证音乐映射；八关流程回归另外通过真实的守卫、碎片、世界钟与 Boss 状态转换。它用直接伤害完成流程，不代表自然通关或平衡测试。

最终世界（第九关）探索阶段仍用普通关卡音乐，点击世界钟才从头播放 Boss 音乐。召唤时机跟随该音轨播放位置；暂停最终战会暂停音轨，防止音乐高潮与魔王登场错位。
