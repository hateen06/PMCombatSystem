using UnityEngine;

/// <summary>
/// 코인 위력 계산기.
/// SP에 따라 앞면 확률이 변동 — 림버스 공식: 50 + SP (%)
/// </summary>
public static class CoinCalculator
{
    /// <summary>
    /// 코인 1개 굴리기. headsChance = 앞면 확률 (0~100)
    /// </summary>
    private static bool FlipCoin(int headsChance = 50)
    {
        return UnityEngine.Random.Range(0, 100) < headsChance;
    }

    /// <summary>
    /// SP 반영 위력 계산.
    /// </summary>
    public static int RollPower(SkillData skill, int coinCount, int headsChance, int paralyzedCoins = 0)
    {
        if (skill == null || coinCount <= 0)
            return 0;

        int power = skill.basePower;

        for (int i = 0; i < coinCount; i++)
        {
            // 마비: 해당 코인은 위력 0 (앞면이든 뒷면이든 무효)
            if (i < paralyzedCoins)
                continue;

            if (FlipCoin(headsChance))
                power += skill.coinPower;
        }

        return power < 0 ? 0 : power;
    }

    /// <summary>
    /// SP 없이 기본 50% 확률 (하위호환)
    /// </summary>
    public static int RollPower(SkillData skill, int coinCount)
    {
        return RollPower(skill, coinCount, 50);
    }

    public static int RollPower(SkillData skill)
    {
        return skill != null ? RollPower(skill, skill.coinCount, 50) : 0;
    }
}