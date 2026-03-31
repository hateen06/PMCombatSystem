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

    public static int RollPower(SkillData skill, int coinCount, int headsChance, int paralyzedCoins = 0)
    {
        if (skill == null || coinCount <= 0) return 0;

        int power = skill.basePower;
        for (int i = 0; i < coinCount; i++)
        {
            if (i < paralyzedCoins) continue;
            if (FlipCoin(headsChance)) power += skill.coinPower;
        }
        return power < 0 ? 0 : power;
    }

    public static int RollPower(SkillData skill, int coinCount) => RollPower(skill, coinCount, 50);
    public static int RollPower(SkillData skill) => skill != null ? RollPower(skill, skill.coinCount, 50) : 0;
}
