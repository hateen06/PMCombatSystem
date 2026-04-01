using System.Collections.Generic;
using UnityEngine;

public interface ICoinRandom
{
    int Range(int min, int max);
}

public class UnityCoinRandom : ICoinRandom
{
    public int Range(int min, int max) => UnityEngine.Random.Range(min, max);
}

public static class CoinCalculator
{
    private static ICoinRandom _random = new UnityCoinRandom();

    public static void SetRandom(ICoinRandom random) => _random = random ?? new UnityCoinRandom();

    private static bool FlipCoin(int headsChance = 50)
    {
        return _random.Range(0, 100) < headsChance;
    }

    public static int RollCoinPower(SkillData skill, int headsChance, bool ignoreCoin = false)
    {
        if (skill == null) return 0;
        if (ignoreCoin) return Mathf.Max(0, skill.basePower);
        return FlipCoin(headsChance) ? Mathf.Max(0, skill.basePower + skill.coinPower) : Mathf.Max(0, skill.basePower);
    }

    public static List<int> RollHitPowers(SkillData skill, int coinCount, int headsChance, int paralyzedCoins = 0)
    {
        var hits = new List<int>();
        if (skill == null || coinCount <= 0) return hits;

        for (int i = 0; i < coinCount; i++)
            hits.Add(RollCoinPower(skill, headsChance, i < paralyzedCoins));

        return hits;
    }

    public static int RollPower(SkillData skill, int coinCount, int headsChance, int paralyzedCoins = 0)
    {
        if (skill == null || coinCount <= 0) return 0;

        int power = skill.basePower;
        for (int i = 0; i < coinCount; i++)
        {
            if (i < paralyzedCoins) continue;
            if (FlipCoin(headsChance)) power += skill.coinPower;
        }
        return Mathf.Max(0, power);
    }

    public static int RollPower(SkillData skill, int coinCount) => RollPower(skill, coinCount, 50);
    public static int RollPower(SkillData skill) => skill != null ? RollPower(skill, skill.coinCount, 50) : 0;
}
