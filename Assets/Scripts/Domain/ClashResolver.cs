public static class ClashResolver
{
    public static ClashResult Resolve(
        string atkName, SkillData atkSkill, int atkHeadsChance, int atkSpeed,
        string defName, SkillData defSkill, int defHeadsChance, int defSpeed)
    {
        var result = new ClashResult();
        result.attackerSkill = atkSkill;
        result.defenderSkill = defSkill;

        if (atkSkill == null || defSkill == null)
        {
            result.outcome = ClashOutcome.Draw;
            result.log = "전투 정보 오류";
            return result;
        }

        int atkCoins = atkSkill.coinCount;
        int defCoins = defSkill.coinCount;

        while (atkCoins > 0 && defCoins > 0)
        {
            int atkPower = CoinCalculator.RollPower(atkSkill, 1, atkHeadsChance);
            int defPower = CoinCalculator.RollPower(defSkill, 1, defHeadsChance);

            if (atkPower > defPower) defCoins--;
            else if (defPower > atkPower) atkCoins--;
            else
            {
                if (atkSpeed >= defSpeed) defCoins--;
                else atkCoins--;
            }
        }

        if (atkCoins > 0)
        {
            result.outcome = ClashOutcome.AttackerWin;
            result.winnerIsAttacker = true;
            result.damage = CoinCalculator.RollPower(atkSkill, atkCoins, atkHeadsChance);
            result.winnerSPChange = 10;
            result.log = $"{atkName} 클래시 승리! 피해 {result.damage}";

            if (atkSkill.statusPotency > 0 && atkSkill.statusCount > 0)
                result.statusApplications.Add(new StatusApplication {
                    applyToAttacker = false, type = atkSkill.inflictStatus,
                    potency = atkSkill.statusPotency, count = atkSkill.statusCount
                });
        }
        else if (defCoins > 0)
        {
            result.outcome = ClashOutcome.DefenderWin;
            result.winnerIsAttacker = false;
            result.damage = CoinCalculator.RollPower(defSkill, defCoins, defHeadsChance);
            result.winnerSPChange = 10;
            result.log = $"{defName} 클래시 승리! 피해 {result.damage}";

            if (defSkill.statusPotency > 0 && defSkill.statusCount > 0)
                result.statusApplications.Add(new StatusApplication {
                    applyToAttacker = true, type = defSkill.inflictStatus,
                    potency = defSkill.statusPotency, count = defSkill.statusCount
                });
        }
        else
        {
            result.outcome = ClashOutcome.Draw;
            result.damage = 0;
            result.log = "클래시 무승부";
        }

        return result;
    }

    // 기존 호환 래퍼
    public static ClashResult Resolve(
        Unit attacker, SkillData atkSkill,
        Unit defender, SkillData defSkill,
        int atkSpeed, int defSpeed)
    {
        return Resolve(
            attacker?.UnitName ?? "?", atkSkill, attacker?.CoinHeadsChance ?? 50, atkSpeed,
            defender?.UnitName ?? "?", defSkill, defender?.CoinHeadsChance ?? 50, defSpeed);
    }
}
