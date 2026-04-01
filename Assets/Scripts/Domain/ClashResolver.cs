using System.Text;

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
        result.attackerStartingCoins = atkCoins;
        result.defenderStartingCoins = defCoins;
        var log = new StringBuilder();

        while (atkCoins > 0 && defCoins > 0)
        {
            int atkPower = CoinCalculator.RollCoinPower(atkSkill, atkHeadsChance);
            int defPower = CoinCalculator.RollCoinPower(defSkill, defHeadsChance);
            log.Append($"{atkName} {atkPower} vs {defName} {defPower}|");

            if (atkPower > defPower)
            {
                defCoins--;
                log.Append($"{defName} 코인 파괴|");
                continue;
            }

            if (defPower > atkPower)
            {
                atkCoins--;
                log.Append($"{atkName} 코인 파괴|");
                continue;
            }

            if (atkSpeed >= defSpeed)
            {
                defCoins--;
                log.Append($"속도 우위로 {defName} 코인 파괴|");
            }
            else
            {
                atkCoins--;
                log.Append($"속도 우위로 {atkName} 코인 파괴|");
            }
        }

        result.winnerSPChange = 10;
        result.loserSPChange = -10;

        result.attackerRemainingCoins = atkCoins;
        result.defenderRemainingCoins = defCoins;

        if (atkCoins > 0)
        {
            result.outcome = ClashOutcome.AttackerWin;
            result.winnerIsAttacker = true;
            result.remainingCoins = atkCoins;
            result.followUpHitPowers = CoinCalculator.RollHitPowers(atkSkill, atkCoins, atkHeadsChance);
            result.damage = Sum(result.followUpHitPowers);
            log.Append($"{atkName} 클래시 승리|");

            if (atkSkill.statusPotency > 0 && atkSkill.statusCount > 0)
                result.statusApplications.Add(new StatusApplication
                {
                    applyToAttacker = false,
                    type = atkSkill.inflictStatus,
                    potency = atkSkill.statusPotency,
                    count = atkSkill.statusCount
                });
        }
        else if (defCoins > 0)
        {
            result.outcome = ClashOutcome.DefenderWin;
            result.winnerIsAttacker = false;
            result.remainingCoins = defCoins;
            result.followUpHitPowers = CoinCalculator.RollHitPowers(defSkill, defCoins, defHeadsChance);
            result.damage = Sum(result.followUpHitPowers);
            log.Append($"{defName} 클래시 승리|");

            if (defSkill.statusPotency > 0 && defSkill.statusCount > 0)
                result.statusApplications.Add(new StatusApplication
                {
                    applyToAttacker = true,
                    type = defSkill.inflictStatus,
                    potency = defSkill.statusPotency,
                    count = defSkill.statusCount
                });
        }
        else
        {
            result.outcome = ClashOutcome.Draw;
            result.damage = 0;
            result.winnerSPChange = 0;
            result.loserSPChange = 0;
            log.Append("클래시 무승부|");
        }

        result.log = log.ToString();
        return result;
    }

    public static ClashResult Resolve(
        Unit attacker, SkillData atkSkill,
        Unit defender, SkillData defSkill,
        int atkSpeed, int defSpeed)
    {
        return Resolve(
            attacker?.UnitName ?? "?", atkSkill, attacker?.CoinHeadsChance ?? 50, atkSpeed,
            defender?.UnitName ?? "?", defSkill, defender?.CoinHeadsChance ?? 50, defSpeed);
    }

    private static int Sum(System.Collections.Generic.IReadOnlyList<int> values)
    {
        int total = 0;
        if (values == null) return total;
        for (int i = 0; i < values.Count; i++)
            total += values[i];
        return total;
    }
}
