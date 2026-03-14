public static class ClashResolver
{
    public static ClashResult Resolve(
        Unit attacker, SkillData attackerSkill,
        Unit defender, SkillData defenderSkill,
        int attackerSpeed, int defenderSpeed)
    {
        ClashResult result = new ClashResult();

        if (attacker == null || defender == null ||
            attackerSkill == null || defenderSkill == null)
        {
            result.outcome = ClashOutcome.Draw;
            result.log = "※ 전투 정보 오류";
            return result;
        }

        int attackerCoins = attackerSkill.coinCount;
        int defenderCoins = defenderSkill.coinCount;

        // 코인 합 (클래시) — 코인마다 출혈 소모
        while (attackerCoins > 0 && defenderCoins > 0)
        {
            // 출혈 처리: 공격 측이 코인 사용 시 출혈 1회 소모
            int atkBleed = attacker.ConsumeBleed(1);
            if (atkBleed > 0)
                result.log += $" | {attacker.UnitName} 출혈 -{atkBleed}";
            if (!attacker.IsAlive) break;

            int defBleed = defender.ConsumeBleed(1);
            if (defBleed > 0)
                result.log += $" | {defender.UnitName} 출혈 -{defBleed}";
            if (!defender.IsAlive) break;

            int attackerPower = CoinCalculator.RollPower(attackerSkill, 1, attacker.CoinHeadsChance);
            int defenderPower = CoinCalculator.RollPower(defenderSkill, 1, defender.CoinHeadsChance);

            if (attackerPower > defenderPower)
                defenderCoins--;
            else if (defenderPower > attackerPower)
                attackerCoins--;
            else
            {
                // 동점 → 속도로 결정
                if (attackerSpeed >= defenderSpeed)
                    defenderCoins--;
                else
                    attackerCoins--;
            }
        }

        // 출혈로 사망 체크
        if (!attacker.IsAlive && !defender.IsAlive)
        {
            result.outcome = ClashOutcome.Draw;
            result.damage = 0;
            result.log += " | 양측 출혈 사망";
            return result;
        }
        if (!attacker.IsAlive)
        {
            result.outcome = ClashOutcome.DefenderWin;
            result.damage = 0;
            result.log += $" | {attacker.UnitName} 출혈 사망";
            return result;
        }
        if (!defender.IsAlive)
        {
            result.outcome = ClashOutcome.AttackerWin;
            result.damage = 0;
            result.log += $" | {defender.UnitName} 출혈 사망";
            return result;
        }

        // 승패 결정
        if (attackerCoins > 0)
        {
            result.outcome = ClashOutcome.AttackerWin;
            result.damage = CoinCalculator.RollPower(attackerSkill, attackerCoins, attacker.CoinHeadsChance);
            result.log = $"{attacker.UnitName} 클래시 승리! 피해 {result.damage}";

            // SP 변동: 승자 +10
            attacker.OnClashWin();

            // 상태이상 부여
            if (attackerSkill.statusPotency > 0 && attackerSkill.statusCount > 0)
            {
                defender.AddStatus(attackerSkill.inflictStatus,
                    attackerSkill.statusPotency, attackerSkill.statusCount);
                result.log += $" | {attackerSkill.inflictStatus} +{attackerSkill.statusPotency}/{attackerSkill.statusCount}";
            }
        }
        else if (defenderCoins > 0)
        {
            result.outcome = ClashOutcome.DefenderWin;
            result.damage = CoinCalculator.RollPower(defenderSkill, defenderCoins, defender.CoinHeadsChance);
            result.log = $"{defender.UnitName} 클래시 승리! 피해 {result.damage}";

            // SP 변동: 승자 +10
            defender.OnClashWin();

            if (defenderSkill.statusPotency > 0 && defenderSkill.statusCount > 0)
            {
                attacker.AddStatus(defenderSkill.inflictStatus,
                    defenderSkill.statusPotency, defenderSkill.statusCount);
                result.log += $" | {defenderSkill.inflictStatus} +{defenderSkill.statusPotency}/{defenderSkill.statusCount}";
            }
        }
        else
        {
            result.outcome = ClashOutcome.Draw;
            result.damage = 0;
            result.log = "클래시 무승부";
        }

        return result;
    }
}
