using System;
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    public static class WorldLawCatalog
    {
        public static bool IsAvailable(WorldLaw law) => law!=WorldLaw.ThermalMass && law!=WorldLaw.ThermalArc && law!=WorldLaw.ThermalShock && law!=WorldLaw.DoubleForce;
        public static string LawName(WorldLaw law)
        {
            switch(law)
            {
                case WorldLaw.ThermalMass:return "冷热改变重量"; case WorldLaw.ColdBounce:return "冷物体会反弹";
                case WorldLaw.SuperSlide:return "冷物体更滑"; case WorldLaw.VaporRecoil:return "骤冷蒸汽冲击波";
                case WorldLaw.ThermalArc:return "冷热相撞会放电"; case WorldLaw.ImpactHeat:return "撞击会升温";
                case WorldLaw.FrictionHeat:return "摩擦生热"; case WorldLaw.Abrasion:return "热磨损"; case WorldLaw.Fission:return "轻小高速物体会爆炸";
                case WorldLaw.Gravity:return "冷质量重力畸变"; case WorldLaw.Leidenfrost:return "莱顿弗罗斯特：热胀斥力";
                case WorldLaw.ThermalShock:return "极寒脆性"; case WorldLaw.DoubleForce:return "更大的力";
                case WorldLaw.ThermalExpansion:return "热胀冷缩"; case WorldLaw.Crowding:return "拥挤效应";
                case WorldLaw.StoneMagic:return "挥石魔法";
                case WorldLaw.ExpandedRadiation:return "冷热辐射范围扩大";
                case WorldLaw.ThermalInjury:return "灼伤、冰伤";
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
                    return "温度 > "+(t!=null ? t.abrasionHotTemperature : 40).ToString("0.#")+"°C 且速度 > "+(t!=null ? t.abrasionSpeedThreshold : 8).ToString("0.#")+" 时，每滑行 1 米损失 "+((t!=null ? t.abrasionPerMeter : .15f)*100).ToString("0.#")+"% 剩余质量，并缩小外形和碰撞体；低于质量下限直接消失。\n作用于玩家、怪物和物品，可与摩擦生热搭配。";
                case WorldLaw.Fission:return "足够轻、足够快的物体会爆炸。\n例：击飞小碎石；动能越大，爆炸越强。";
                case WorldLaw.Gravity:return "温度低于冷门槛且质量超过阈值，吸引附近动态实体。默认：<-10°C 且质量>3。\n例：蓄满冰石聚拢怪物；加热可关闭吸力。";
                case WorldLaw.Leidenfrost:return "靠近时温差超过 80°C，会相互排斥。\n例：加热石头，推开附近冷怪物。";
                case WorldLaw.ThermalShock:return "冻结单位在 1 秒内升到极热，会受到热冲击伤害。\n例：先冻冷怪物，再快速加热。";
                case WorldLaw.DoubleForce:return "所有实体施加的力与冲量变为两倍。\n例：石头飞得更快，怪物冲撞也更强。";
                case WorldLaw.ThermalExpansion:return "热物变大，冷物变小；空间不足时停止膨胀。\n例：加热大石头，把夹在它和墙间的怪物挤伤。";
                case WorldLaw.Crowding:return "玩家或怪物周围 2.5 范围内，其他实体超过 6 个，该角色持续扣血。\n怪物、投射物、石头、树木等均计数；每秒错开检测。";
                case WorldLaw.StoneMagic:return "玩家与小怪的投射物变成冷热石弹，保留调温效果。玩家继续蓄力会增大尺寸与质量，最大为标准场地石头的 1.5 倍大小。\n例：蓄力生成大石头，用 F 推动后借环境与法则造成撞击。";
                case WorldLaw.ExpandedRadiation:return "冷热物体的辐射圈与实际传热距离扩大到 1.5 倍。\n例：让热石弹隔着更远的距离加热怪物。";
                case WorldLaw.ThermalInjury:return "极端温度持续伤害角色和可破坏物体。\n灼伤：>80°C，每 0.5 秒 2 点。\n冰伤：<-50°C，每 2 秒 6 点。\n维持敌人的极端温度来消耗生命。";
                default:return "";
            }
        }
    }
}
