using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ClashResolverTest
{
    private SkillData MakeSkill(int basePower, int coinCount, int coinPower)
    {
        var s = ScriptableObject.CreateInstance<SkillData>();
        s.basePower = basePower;
        s.coinCount = coinCount;
        s.coinPower = coinPower;
        s.skillType = SkillType.Attack;
        return s;
    }

    [SetUp]
    public void Setup() => CoinCalculator.SetRandom(new FixedCoinRandom(true));

    [TearDown]
    public void TearDown() => CoinCalculator.SetRandom(null);

    [Test]
    public void StrongerAttacker_Wins()
    {
        var atk = MakeSkill(10, 3, 5);
        var def = MakeSkill(2, 2, 1);
        var result = ClashResolver.Resolve("A", atk, 50, 5, "B", def, 50, 3);
        Assert.AreEqual(ClashOutcome.AttackerWin, result.outcome);
        Assert.Greater(result.damage, 0);
    }

    [Test]
    public void StrongerDefender_Wins()
    {
        var atk = MakeSkill(2, 1, 1);
        var def = MakeSkill(10, 4, 5);
        var result = ClashResolver.Resolve("A", atk, 50, 5, "B", def, 50, 3);
        Assert.AreEqual(ClashOutcome.DefenderWin, result.outcome);
    }

    [Test]
    public void EqualPower_SpeedBreaksTie()
    {
        var skill = MakeSkill(5, 2, 3);
        var r1 = ClashResolver.Resolve("A", skill, 50, 10, "B", skill, 50, 1);
        Assert.AreEqual(ClashOutcome.AttackerWin, r1.outcome);
    }

    [Test]
    public void NullSkill_ReturnsDraw()
    {
        var result = ClashResolver.Resolve("A", null, 50, 5, "B", null, 50, 3);
        Assert.AreEqual(ClashOutcome.Draw, result.outcome);
    }

    [Test]
    public void WinnerSPChange_Is10()
    {
        var atk = MakeSkill(10, 3, 5);
        var def = MakeSkill(2, 1, 1);
        var result = ClashResolver.Resolve("A", atk, 50, 5, "B", def, 50, 3);
        Assert.AreEqual(10, result.winnerSPChange);
    }
}
