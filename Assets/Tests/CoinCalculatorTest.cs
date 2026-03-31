using NUnit.Framework;
using UnityEngine;

public class FixedCoinRandom : ICoinRandom
{
    private readonly bool _alwaysHeads;
    public FixedCoinRandom(bool alwaysHeads) => _alwaysHeads = alwaysHeads;
    public int Range(int min, int max) => _alwaysHeads ? 0 : 99;
}

[TestFixture]
public class CoinCalculatorTest
{
    private SkillData MakeSkill(int basePower, int coinCount, int coinPower)
    {
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.basePower = basePower;
        skill.coinCount = coinCount;
        skill.coinPower = coinPower;
        return skill;
    }

    [TearDown]
    public void TearDown() => CoinCalculator.SetRandom(null);

    [Test]
    public void AllHeads_ReturnsMaxPower()
    {
        CoinCalculator.SetRandom(new FixedCoinRandom(true));
        var skill = MakeSkill(4, 3, 2);
        int result = CoinCalculator.RollPower(skill, 3, 50);
        Assert.AreEqual(4 + 3 * 2, result); // 10
    }

    [Test]
    public void AllTails_ReturnsBasePower()
    {
        CoinCalculator.SetRandom(new FixedCoinRandom(false));
        var skill = MakeSkill(4, 3, 2);
        int result = CoinCalculator.RollPower(skill, 3, 50);
        Assert.AreEqual(4, result);
    }

    [Test]
    public void Paralysis_SkipsCoins()
    {
        CoinCalculator.SetRandom(new FixedCoinRandom(true));
        var skill = MakeSkill(4, 3, 2);
        int result = CoinCalculator.RollPower(skill, 3, 50, paralyzedCoins: 2);
        Assert.AreEqual(4 + 1 * 2, result); // 6 (2개 마비)
    }

    [Test]
    public void NullSkill_ReturnsZero()
    {
        Assert.AreEqual(0, CoinCalculator.RollPower(null, 3, 50));
    }
}
