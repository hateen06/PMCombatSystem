using UnityEngine;
public static class DamageProcessor
{
    public struct DamageContext
    {
        public Unit attacker;
        public Unit target;
        public SkillData skill;
        public int rawDamage;          // 이미 굴린 경우 (합 결과 등)
        public bool skipCoinRoll;      // rawDamage를 그대로 쓸지
    }

    public struct DamageResult
    {
        public int finalDamage;        // 타겟에 실제 적용된 피해
        public int bleedDamage;        // 공격자 출혈 자기 피해
        public int shieldAbsorbed;     // Shield가 흡수한 양
        public int paralyzedCoins;     // 마비로 무효화된 코인 수
        public float levelMod;         // 레벨 보정 배율
        public float resistMod;        // 저항 배율
        public bool wasStaggered;      // 흐트러짐 상태였는지
    }
    public static DamageResult Process(DamageContext ctx)
    {
        var result = new DamageResult();

        if (ctx.target == null || !ctx.target.IsAlive) return result;
        if (ctx.skill == null && !ctx.skipCoinRoll) return result;

        // 1~2. 코인 굴림 + 마비
        int damage;
        if (ctx.skipCoinRoll)
        {
            damage = ctx.rawDamage;
        }
        else
        {
            result.paralyzedCoins = ctx.attacker != null ? ctx.attacker.GetParalyzedCoins() : 0;
            int headsChance = ctx.attacker != null ? ctx.attacker.CoinHeadsChance : 50;
            damage = CoinCalculator.RollPower(ctx.skill, ctx.skill.coinCount, headsChance, result.paralyzedCoins);
        }

        // 3. 출혈 (공격자 자기 피해)
        if (ctx.attacker != null && ctx.skill != null)
        {
            result.bleedDamage = ctx.attacker.ConsumeBleed(ctx.skill.coinCount);
        }

        // 4~8. TakeDamage가 레벨/저항/흐트러짐/Shield/HP 전부 처리
        int offLv = ctx.attacker != null ? ctx.attacker.OffenseLevel : 0;
        result.wasStaggered = ctx.target.IsStaggered;
        result.resistMod = ctx.target.GetResistance(ctx.skill != null ? ctx.skill.damageType : DamageType.Slash);

        // 레벨 보정 미리 계산 (로그용)
        int defLv = ctx.target.DefenseLevel;
        if (offLv > 0)
        {
            int diff = offLv - defLv;
            result.levelMod = 1f + (float)diff / (Mathf.Abs(diff) + 25);
        }
        else
        {
            result.levelMod = 1f;
        }

        // 진동: 피격 시 추가 피해
        int tremorBonus = ctx.target.GetTremorBonus();
        damage += tremorBonus;

        // 파열: 피격 시 위력만큼 추가 피해
        int ruptureBonus = ctx.target.GetRuptureBonus();
        damage += ruptureBonus;
        if (ruptureBonus > 0) ctx.target.TickRupture();

        DamageType dmgType = ctx.skill != null ? ctx.skill.damageType : DamageType.Slash;
        result.finalDamage = ctx.target.TakeDamage(damage, dmgType, offLv);

        return result;
    }
}
