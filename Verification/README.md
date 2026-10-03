# 当前验证结果

2026-10-03 在运行中的 Unity Editor 完成；最终退出 Play，不保存测试夹具。

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

LegacyTests 保存旧 ArenaPlayVerification、ArenaFeedbackVerification 和 meta；旧断言包含热物碰水反冲及旧施法，所以移出编译目录，未删除。ArenaProjectileVerification 保留但本轮未重跑，旧结果不作为本轮通过证据。

Assets 中 LegacyLawTickBenchmark 仅 Editor 编译，用于旧 Tick 对照。BeforeRefactor-20261002 是重构前快照；refactor_*.py 等是一次性生成记录，请勿重复应用。

最终保存检查：Play=False，场景无 Missing Script，Prefab 与场景小蓝条引用均有效，树木仍为 CapsuleCollider2D。编译无错误；旧 Editor 生成器仍有 TextureImporter.spritesheet 弃用警告，MCP 重载期间记录过一次 WebSocket 传输警告，连接恢复后保存与查询成功。
