using System.Collections.Generic;
using UnityEngine;

public class UnitCombatData
{
    public string unitName;
    public int currentHP;
    public int maxHP;
    public bool isAlive;
    public int sp;
    public int shield;

    public int offenseLevel;
    public int defenseLevel;
    public float slashResist = 1f;
    public float pierceResist = 1f;
    public float bluntResist = 1f;

    public bool isStaggered;
    public int staggerCount;
    public int staggerTurnsLeft;
    public bool staggerAppliedThisTurn;
    public float staggerThreshold1 = 0.65f;
    public float staggerThreshold2 = 0.35f;
    public float staggerThreshold3 = 0.15f;

    public List<StatusEffect> statusEffects = new();

    public const int SP_MIN = -45;
    public const int SP_MAX = 45;

    public float HPRatio => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public int CoinHeadsChance => Mathf.Clamp(50 + sp, 5, 95);
    public float DamageMultiplier => isStaggered ? 1.5f : 1f;
    public bool IsPanicked => sp <= SP_MIN;

    public void Initialize(UnitData data)
    {
        unitName = data.unitName;
        maxHP = data.LevelHP;
        currentHP = maxHP;
        isAlive = true;
        sp = 0;
        shield = 0;
        isStaggered = false;
        staggerCount = 0;
        offenseLevel = data.OffenseLevel;
        defenseLevel = data.DefenseLevel;
        slashResist = data.slashResist;
        pierceResist = data.pierceResist;
        bluntResist = data.bluntResist;
        staggerThreshold1 = data.staggerThreshold1;
        staggerThreshold2 = data.staggerThreshold2;
        staggerThreshold3 = data.staggerThreshold3;
        statusEffects.Clear();
    }

    public float GetResistance(DamageType type)
    {
        switch (type)
        {
            case DamageType.Slash: return slashResist;
            case DamageType.Pierce: return pierceResist;
            case DamageType.Blunt: return bluntResist;
            default: return 1f;
        }
    }

    public StatusEffect GetStatus(StatusType type)
    {
        for (int i = 0; i < statusEffects.Count; i++)
            if (statusEffects[i].type == type) return statusEffects[i];
        return null;
    }

    public void AddStatus(StatusType type, int potency, int count)
    {
        if (!isAlive || potency <= 0 || count <= 0) return;
        var existing = GetStatus(type);
        if (existing != null)
        {
            existing.potency = existing.potency > potency ? existing.potency : potency;
            existing.count += count;
        }
        else
        {
            statusEffects.Add(new StatusEffect(type, potency, count));
        }
    }

    public void ChangeSP(int amount)
    {
        sp = Mathf.Clamp(sp + amount, SP_MIN, SP_MAX);
    }

    public int ConsumeBleed(int coinCount)
    {
        var bleed = GetStatus(StatusType.Bleed);
        if (bleed == null || bleed.IsExpired) return 0;
        int consumed = bleed.Consume(coinCount);
        int dmg = bleed.potency * consumed;
        TakeDamage(dmg, DamageType.Slash);
        if (bleed.IsExpired) statusEffects.Remove(bleed);
        return dmg;
    }

    public int GetParalyzedCoins()
    {
        var para = GetStatus(StatusType.Paralysis);
        return (para != null && !para.IsExpired) ? para.potency : 0;
    }

    public void TickParalysis()
    {
        var para = GetStatus(StatusType.Paralysis);
        if (para == null || para.IsExpired) return;
        para.Consume(1);
        if (para.IsExpired) statusEffects.Remove(para);
    }

    public int TickBurn()
    {
        var burn = GetStatus(StatusType.Burn);
        if (burn == null || burn.IsExpired) return 0;
        int dmg = burn.potency * burn.count;
        burn.Consume(1);
        if (burn.IsExpired) statusEffects.Remove(burn);
        TakeDamage(dmg, DamageType.Slash);
        return dmg;
    }

    public int GetTremorBonus()
    {
        var tremor = GetStatus(StatusType.Tremor);
        if (tremor == null || tremor.IsExpired) return 0;
        int bonus = tremor.count;
        tremor.Consume(1);
        if (tremor.IsExpired) statusEffects.Remove(tremor);
        return bonus;
    }

    public int GetRuptureBonus()
    {
        var rupture = GetStatus(StatusType.Rupture);
        return (rupture != null && !rupture.IsExpired) ? rupture.potency : 0;
    }

    public void TickRupture()
    {
        var rupture = GetStatus(StatusType.Rupture);
        if (rupture == null || rupture.IsExpired) return;
        rupture.Consume(1);
        if (rupture.IsExpired) statusEffects.Remove(rupture);
    }

    public void CheckStagger()
    {
        if (isStaggered) return;
        float ratio = HPRatio;
        float threshold = 0f;

        if (staggerCount == 0 && ratio <= staggerThreshold1)
            threshold = staggerThreshold1;
        else if (staggerCount == 1 && ratio <= staggerThreshold2)
            threshold = staggerThreshold2;
        else if (staggerCount == 2 && ratio <= staggerThreshold3)
            threshold = staggerThreshold3;
        else return;

        staggerCount++;
        isStaggered = true;
        staggerTurnsLeft = 1;
        staggerAppliedThisTurn = true;
    }

    public void OnTurnStart()
    {
        if (!isStaggered) return;
        if (staggerAppliedThisTurn) { staggerAppliedThisTurn = false; return; }
        staggerTurnsLeft--;
        if (staggerTurnsLeft <= 0) isStaggered = false;
    }

    public void CleanupExpiredStatuses()
    {
        statusEffects.RemoveAll(s => s.IsExpired);
    }

    public int TakeDamage(int damage, DamageType damageType, int attackerOffenseLevel = 0)
    {
        if (!isAlive || damage <= 0) return 0;

        float resist = GetResistance(damageType);
        float levelMod = 1f;
        if (attackerOffenseLevel > 0)
        {
            int diff = attackerOffenseLevel - defenseLevel;
            levelMod = 1f + (float)diff / (Mathf.Abs(diff) + 25);
        }

        int finalDamage = Mathf.RoundToInt(damage * resist * DamageMultiplier * levelMod);

        if (shield > 0)
        {
            int absorbed = Mathf.Min(shield, finalDamage);
            shield -= absorbed;
            finalDamage -= absorbed;
        }

        currentHP -= finalDamage;
        if (currentHP <= 0)
        {
            currentHP = 0;
            isAlive = false;
        }
        return finalDamage;
    }
}
