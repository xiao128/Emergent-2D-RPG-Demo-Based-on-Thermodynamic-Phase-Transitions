using System;
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    public static class WorldLawCatalog
    {
        public static bool IsAvailable(WorldLaw law) => law!=WorldLaw.ThermalMass && law!=WorldLaw.ThermalArc && law!=WorldLaw.ThermalShock;
        public static string LawName(WorldLaw law)
        {
            switch(law)
            {
                case WorldLaw.ThermalMass:return "冷热改变重量"; case WorldLaw.ColdBounce:return "冷物体会反弹";
                case WorldLaw.SuperSlide:return "冷物体更滑"; case WorldLaw.VaporRecoil:return "骤冷蒸汽冲击波";
                case WorldLaw.ThermalArc:return "冷热相撞会放电"; case WorldLaw.ImpactHeat:return "撞击会升温";
                case WorldLaw.Abrasion:return "滑行会磨损"; case WorldLaw.Fission:return "轻小高速物体会爆炸";
                case WorldLaw.Gravity:return "高密度物体产生引力"; case WorldLaw.Leidenfrost:return "莱顿弗罗斯特：热胀斥力";
                case WorldLaw.ThermalShock:return "极寒脆性"; case WorldLaw.DoubleForce:return "更大的力";
                case WorldLaw.ThermalExpansion:return "热胀冷缩"; case WorldLaw.Crowding:return "拥挤效应"; default:return law.ToString();
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
                case WorldLaw.Abrasion:return "滑行生热；温暖高速物体会磨损变轻。\n例：把石头推过粗糙地面，继续连锁。";
                case WorldLaw.Fission:return "足够轻、足够快的物体会爆炸。\n例：击飞小碎石；动能越大，爆炸越强。";
                case WorldLaw.Gravity:return "质量 / 占地面积达到阈值，吸引附近动态实体。\n例：冷缩提高密度，让石头、法球或怪物聚拢周围物体。";
                case WorldLaw.Leidenfrost:return "靠近时温差超过 80°C，会相互排斥。\n例：加热石头，推开附近冷怪物。";
                case WorldLaw.ThermalShock:return "冻结单位在 1 秒内升到极热，会受到热冲击伤害。\n例：先冻冷怪物，再快速加热。";
                case WorldLaw.DoubleForce:return "所有实体施加的力与冲量变为两倍。\n例：石头飞得更快，怪物冲撞也更强。";
                case WorldLaw.ThermalExpansion:return "热物变大，冷物变小；空间不足时停止膨胀。\n例：加热大石头，把夹在它和墙间的怪物挤伤。";
                case WorldLaw.Crowding:return "2.5 × 2.5 区域内物品超过 6 个，物品持续受到拥挤伤害。\n例：把法球和石头聚成一团，触发拥挤损毁。";
                default:return "";
            }
        }
    }
}
