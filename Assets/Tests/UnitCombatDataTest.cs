using NUnit.Framework;

[TestFixture]
public class UnitCombatDataTest
{
    private UnitCombatData MakeUnit(int hp = 100, int sp = 0)
    {
        var u = new UnitCombatData();
        u.unitName = "TestUnit";
        u.maxHP = hp;
        u.currentHP = hp;
        u.isAlive = true;
        u.sp = sp;
        u.slashResist = 1f;
        u.pierceResist = 0.5f;
        u.bluntResist = 2f;
        return u;
    }

    [Test]
    public void CoinHeadsChance_ReflectsSP()
    {
        var u = MakeUnit(sp: 20);
        Assert.AreEqual(70, u.CoinHeadsChance);
    }

    [Test]
    public void CoinHeadsChance_Clamped()
    {
        var u = MakeUnit(sp: -100);
        Assert.AreEqual(5, u.CoinHeadsChance);
    }

    [Test]
    public void Resistance_ByType()
    {
        var u = MakeUnit();
        Assert.AreEqual(0.5f, u.GetResistance(DamageType.Pierce));
        Assert.AreEqual(2f, u.GetResistance(DamageType.Blunt));
    }

    [Test]
    public void AddStatus_Stacks()
    {
        var u = MakeUnit();
        u.AddStatus(StatusType.Bleed, 3, 2);
        u.AddStatus(StatusType.Bleed, 5, 1);
        var bleed = u.GetStatus(StatusType.Bleed);
        Assert.AreEqual(5, bleed.potency);
        Assert.AreEqual(3, bleed.count);
    }

    [Test]
    public void DamageMultiplier_WhenStaggered()
    {
        var u = MakeUnit();
        u.isStaggered = true;
        Assert.AreEqual(1.5f, u.DamageMultiplier);
    }

    [Test]
    public void TakeDamage_ReducesHP()
    {
        var u = MakeUnit(100);
        int dealt = u.TakeDamage(30, DamageType.Slash);
        Assert.AreEqual(70, u.currentHP);
        Assert.AreEqual(30, dealt);
    }

    [Test]
    public void TakeDamage_ResistanceApplied()
    {
        var u = MakeUnit(100);
        int dealt = u.TakeDamage(20, DamageType.Pierce); // 0.5x resist
        Assert.AreEqual(10, dealt);
        Assert.AreEqual(90, u.currentHP);
    }

    [Test]
    public void TakeDamage_ShieldAbsorbs()
    {
        var u = MakeUnit(100);
        u.shield = 15;
        int dealt = u.TakeDamage(20, DamageType.Slash);
        Assert.AreEqual(5, dealt);
        Assert.AreEqual(0, u.shield);
    }

    [Test]
    public void TakeDamage_KillsUnit()
    {
        var u = MakeUnit(10);
        u.TakeDamage(100, DamageType.Slash);
        Assert.IsFalse(u.isAlive);
        Assert.AreEqual(0, u.currentHP);
    }

    [Test]
    public void ChangeSP_Clamped()
    {
        var u = MakeUnit();
        u.ChangeSP(100);
        Assert.AreEqual(45, u.sp);
        u.ChangeSP(-200);
        Assert.AreEqual(-45, u.sp);
    }

    [Test]
    public void Stagger_TriggersAtThreshold()
    {
        var u = MakeUnit(100);
        u.staggerThreshold1 = 0.6f;
        u.TakeDamage(50, DamageType.Slash);
        u.CheckStagger();
        Assert.IsTrue(u.isStaggered);
        Assert.AreEqual(1, u.staggerCount);
    }

    [Test]
    public void Stagger_RecoverAfterTurnStart()
    {
        var u = MakeUnit(100);
        u.staggerThreshold1 = 0.6f;
        u.TakeDamage(50, DamageType.Slash);
        u.CheckStagger();
        Assert.IsTrue(u.isStaggered);
        u.OnTurnStart(); // staggerAppliedThisTurn skip
        Assert.IsTrue(u.isStaggered);
        u.OnTurnStart(); // actual recover
        Assert.IsFalse(u.isStaggered);
    }

    [Test]
    public void Bleed_ConsumeDealsDamage()
    {
        var u = MakeUnit(100);
        u.AddStatus(StatusType.Bleed, 5, 3);
        int dmg = u.ConsumeBleed(2);
        Assert.AreEqual(10, dmg); // 5 potency * 2 consumed
        Assert.AreEqual(1, u.GetStatus(StatusType.Bleed).count);
    }

    [Test]
    public void Burn_TickDealsDamage()
    {
        var u = MakeUnit(100);
        u.AddStatus(StatusType.Burn, 3, 2);
        int dmg = u.TickBurn();
        Assert.AreEqual(6, dmg); // 3 * 2
        Assert.Less(u.currentHP, 100);
    }

    [Test]
    public void Tremor_ReturnsBonusAndConsumes()
    {
        var u = MakeUnit(100);
        u.AddStatus(StatusType.Tremor, 2, 3);
        int bonus = u.GetTremorBonus();
        Assert.AreEqual(3, bonus);
        Assert.AreEqual(2, u.GetStatus(StatusType.Tremor).count);
    }

    [Test]
    public void Rupture_ReturnsBonus()
    {
        var u = MakeUnit(100);
        u.AddStatus(StatusType.Rupture, 4, 2);
        Assert.AreEqual(4, u.GetRuptureBonus());
        u.TickRupture();
        Assert.AreEqual(1, u.GetStatus(StatusType.Rupture).count);
    }

    [Test]
    public void Paralysis_ReturnsCoins()
    {
        var u = MakeUnit(100);
        u.AddStatus(StatusType.Paralysis, 2, 3);
        Assert.AreEqual(2, u.GetParalyzedCoins());
        u.TickParalysis();
        Assert.AreEqual(2, u.GetStatus(StatusType.Paralysis).count);
    }

    [Test]
    public void StaggerMultiplier_AffectsDamage()
    {
        var u = MakeUnit(100);
        u.isStaggered = true;
        int dmg = u.TakeDamage(20, DamageType.Slash);
        Assert.AreEqual(30, dmg); // 20 * 1.5
    }
}
