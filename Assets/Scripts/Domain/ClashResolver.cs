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

        // 코인 합 (클래시)
        while (attackerCoins > 0 && defenderCoins > 0)
        {
            int attackerPower = CoinCalculator.RollPower(attackerSkill, 1);
            int defenderPower = CoinCalculator.RollPower(defenderSkill, 1);

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

        // 승패 결정
        if (attackerCoins > 0)
        {
            result.outcome = ClashOutcome.AttackerWin;
            result.damage = CoinCalculator.RollPower(attackerSkill, attackerCoins);
            result.log = $"{attacker.UnitName} 클래시 승리! 피해 {result.damage}";

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
            result.damage = CoinCalculator.RollPower(defenderSkill, defenderCoins);
            result.log = $"{defender.UnitName} 클래시 승리! 피해 {result.damage}";

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
