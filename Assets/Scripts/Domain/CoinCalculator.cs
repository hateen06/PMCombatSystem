using UnityEngine;

public static class CoinCalculator
{
    // 코인 1개 굴리기 — 50% 확률로 앞면
    private static bool FlipCoin()
    {
        return UnityEngine.Random.Range(0, 2) == 0;
    }

    // 스킬 위력 계산
    // basePower + (앞면 나온 횟수 × coinPower)
    public static int RollPower(SkillData skill, int coinCount)
    {
        if (skill == null || coinCount <= 0)
            return 0;

        int power = skill.basePower;

        for (int i = 0; i < coinCount; i++)
        {
            if (FlipCoin())
                power += skill.coinPower;
        }

        return power < 0 ? 0 : power;
    }

    public static int RollPower(SkillData skill)
    {
        return skill != null ? RollPower(skill, skill.coinCount) : 0;
    }
}