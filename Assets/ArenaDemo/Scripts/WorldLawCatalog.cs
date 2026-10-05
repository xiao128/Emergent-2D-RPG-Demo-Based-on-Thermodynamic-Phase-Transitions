using System;
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    public static class WorldLawCatalog
    {
        public static bool IsAvailable(WorldLaw law) => law!=WorldLaw.ThermalMass && law!=WorldLaw.ThermalArc && law!=WorldLaw.ThermalShock && law!=WorldLaw.DoubleForce && law!=WorldLaw.Leidenfrost;
        public static string LawName(WorldLaw law)
        {
            switch(law)
            {
                case WorldLaw.ThermalMass:return "冷热改变重量"; case WorldLaw.ColdBounce:return "冷物体会反弹";
                case WorldLaw.SuperSlide:return "冷物体更滑"; case WorldLaw.VaporRecoil:return "骤冷蒸汽冲击波";
                case WorldLaw.ThermalArc:return "冷热相撞会放电"; case WorldLaw.ImpactHeat:return "撞击会升温";
                case WorldLaw.FrictionHeat:return "摩擦生热"; case WorldLaw.Abrasion:return "热磨损"; case WorldLaw.Fission:return "轻小高速物体会爆炸";
                case WorldLaw.Gravity:return "压强对流场"; case WorldLaw.Leidenfrost:return "热胀斥力（已移除）";
                case WorldLaw.ThermalShock:return "极寒脆性"; case WorldLaw.DoubleForce:return "更大的力";
                case WorldLaw.ThermalExpansion:return "热胀冷缩"; case WorldLaw.Crowding:return "拥挤效应";
                case WorldLaw.StoneMagic:return "挥石魔法";
                case WorldLaw.ExpandedRadiation:return "冷热辐射范围扩大";
                case WorldLaw.ThermalInjury:return "灼伤、冰伤";
                case WorldLaw.NuclearFusion:return "核聚变";
                case WorldLaw.VaporGlide:return "莱顿弗罗斯特气垫滑行";
                case WorldLaw.RecoilPropellant:return "反冲工质";
                case WorldLaw.ViscousWelding:return "熔融粘滞与表面熔接";
                case WorldLaw.BernoulliWake:return "伯努利尾流负压";
                default:return law.ToString();
            }
        }
        public static string LawDescription(WorldLaw law)
        {
            switch(law)
            {
                case WorldLaw.ThermalMass:return "越冷越重，越热越轻；变质量不改速度。\n例：先加热再推动更容易加速；先击飞再冷却，保留速度并增加质量。";
                case WorldLaw.ColdBounce:return "冻冷的物体碰撞后会反弹。\n例：冷却石头，借障碍物弹向敌人。";
                case WorldLaw.SuperSlide:return "冻冷的物体滑得更远。\n例：冷却石头，打出长距离撞击。";
                case WorldLaw.VaporRecoil:return "100°C 以上物体一次降温至少 60°C，释放蒸汽冲击波。\n例：冰球撞击热石头，推开周围实体。";
                case WorldLaw.ThermalArc:return "很热与很冷的物体相撞会放电。\n例：热石头撞冷怪物；大物体反应更强。";
                case WorldLaw.ImpactHeat:return "碰撞的相对动能转为热量；速度越快，升温越强。\n例：把石头踢向障碍，撞击升温后融冰。";
                case WorldLaw.FrictionHeat:return "物体在地面滑动时，按摩擦系数持续生热；冰面不生热。\n例：在粗糙地面推动物体，使其升温。";
                case WorldLaw.Abrasion:
                    var t=ArenaDirector.Instance!=null ? ArenaDirector.Instance.tuning : null;
                    return "温度 > "+(t!=null ? t.abrasionHotTemperature : 40).ToString("0.#")+"°C 且速度 > "+(t!=null ? t.abrasionSpeedThreshold : 8).ToString("0.#")+" 时，每滑行 1 米损失固定 "+(t!=null ? t.abrasionMassPerMeter : .2f).ToString("0.##")+" 质量，并缩小外形和碰撞体；低于质量下限直接消失。\n作用于玩家、怪物和物品，可与摩擦生热搭配。";
                case WorldLaw.Fission:return "足够轻、足够快的物体会爆炸。\n例：击飞小碎石；动能越大，爆炸越强。";
                case WorldLaw.Gravity:return "高温物体向外排斥，低温物体向内吸引；风力与相对环境的温差成正比，加速度与受力物体质量成反比。\n不再要求源质量门槛；调整冷热状态可以改变风向。";
                case WorldLaw.Leidenfrost:return "该温差互斥法则已删除，不再产生力。";
                case WorldLaw.ThermalShock:return "冻结单位在 1 秒内升到极热，会受到热冲击伤害。\n例：先冻冷怪物，再快速加热。";
                case WorldLaw.DoubleForce:return "所有实体施加的力与冲量变为两倍。\n例：石头飞得更快，怪物冲撞也更强。";
                case WorldLaw.ThermalExpansion:return "热物变大，冷物变小；空间不足时停止膨胀。\n例：加热大石头，把夹在它和墙间的怪物挤伤。";
                case WorldLaw.Crowding:return "玩家或怪物周围 2.5 范围内，其他实体超过 6 个，该角色持续扣血。\n怪物、投射物、石头、树木等均计数；每秒错开检测。";
                case WorldLaw.StoneMagic:return "玩家与小怪的投射物变成冷热石弹，保留调温效果。玩家继续蓄力会增大尺寸与质量，最大为标准场地石头的 1.5 倍大小。\n例：蓄力生成大石头，用 F 推动后借环境与法则造成撞击。";
                case WorldLaw.ExpandedRadiation:return "冷热物体的辐射圈与实际传热距离扩大到 1.5 倍。\n例：让热石弹隔着更远的距离加热怪物。";
                case WorldLaw.ThermalInjury:return "极端温度持续伤害角色和可破坏物体。\n灼伤：>80°C，每 0.5 秒 2 点。\n冰伤：<-50°C，每 2 秒 6 点。\n维持敌人的极端温度来消耗生命。";
                case WorldLaw.NuclearFusion:return "双方都高速且互相迎面靠近，实际相撞时在接触点爆炸；单个高速物体撞静止物体、同向追撞不触发。\n默认双方速度 >12，方向和速度取碰撞前状态。";
                case WorldLaw.VaporGlide:return "温度 >60°C 的物体接触常温水或冰面，沿接触法线持续反推，地面滑动摩擦锁为零，持续 5 秒。\n离开表面后气垫仍持续至时间结束；冷却不取消已形成的气垫。";
                case WorldLaw.RecoilPropellant:return "超过 200°C 后输入的过量热能，默认 50% 用于消耗自身质量；体积同步缩小，喷出可见气体微粒并产生反冲。\n喷气朝向受热来源，刚体被推离受热来源；冷却不消耗工质。";
                case WorldLaw.ViscousWelding:return "接触双方均 >200°C 时，切向动摩擦随温度线性增强；法线方向形成临时弹簧拉力，阻止分离。\n冷却、失去接触或移除法则后解除。";
                case WorldLaw.BernoulliWake:return "速度÷自身碰撞体直径 >10 时，后方和侧翼形成指向自身质心的尾流吸力，正前方不受影响。\n受力物体越轻，加速度越大；影响半径随物体大小增加。";
                default:return "";
            }
        }
    }
}
