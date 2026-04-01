using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private UnitData unitData;

    private UnitCombatData _combat = new();
    private SkillDeck _deck;

    public System.Action<int, int> OnHPChanged;
    public System.Action<int> OnSPChanged;
    public System.Action<int> OnShieldChanged;
    public System.Action<bool, int> OnStaggerChanged;
    public System.Action OnStatusChanged;

    public string UnitName => _combat.unitName;
    public int Level => unitData != null ? unitData.level : 0;
    public int CurrentHP => _combat.currentHP;
    public int MaxHP => _combat.maxHP;
    public bool IsAlive => _combat.isAlive;
    public float HPRatio => _combat.HPRatio;
    public int SP => _combat.sp;
    public bool IsPanicked => _combat.IsPanicked;
    public int Shield => _combat.shield;
    public bool IsStaggered => _combat.isStaggered;
    public int StaggerCount => _combat.staggerCount;
    public float StaggerThreshold1 => _combat.staggerThreshold1;
    public float StaggerThreshold2 => _combat.staggerThreshold2;
    public float StaggerThreshold3 => _combat.staggerThreshold3;
    public int OffenseLevel => _combat.offenseLevel;
    public int DefenseLevel => _combat.defenseLevel;
    public float DamageMultiplier => _combat.DamageMultiplier;
    public int CoinHeadsChance => _combat.CoinHeadsChance;
    public SkillData[] SkillSlots => unitData != null ? unitData.skillSlot : null;
    public SkillDeck Deck => _deck;
    public UnitCombatData Combat => _combat;

    public void Initialize()
    {
        if (unitData == null) { _combat.isAlive = false; return; }
        _combat.Initialize(unitData);
        var slots = SkillSlots;
        if (slots != null && slots.Length > 0) _deck = new SkillDeck(slots);
        NotifyAll();
    }

    public void ChangeSP(int amount) { int prev = _combat.sp; _combat.ChangeSP(amount); if (_combat.sp != prev) OnSPChanged?.Invoke(_combat.sp); }
    public void OnClashWin() => ChangeSP(10);
    public void OnAllyDeath() => ChangeSP(-10);
    public int RollSpeed() => unitData != null ? Random.Range(unitData.minSpeed, unitData.maxSpeed + 1) : 0;

    public void ApplyShield(int amount) { _combat.shield += Mathf.Max(0, amount); OnShieldChanged?.Invoke(_combat.shield); }
    public void ClearShield() { _combat.shield = 0; OnShieldChanged?.Invoke(0); }

    public int TakeDamage(int damage) => TakeDamage(damage, DamageType.Slash);
    public int TakeDamage(int damage, DamageType damageType, int attackerOffenseLevel = 0)
    {
        bool wasStaggered = _combat.isStaggered;
        int result = _combat.TakeDamage(damage, damageType, attackerOffenseLevel);
        OnHPChanged?.Invoke(_combat.currentHP, _combat.maxHP);
        _combat.CheckStagger();
        if (_combat.isStaggered != wasStaggered) OnStaggerChanged?.Invoke(_combat.isStaggered, _combat.staggerCount);
        return result;
    }

    public float GetResistance(DamageType type) => _combat.GetResistance(type);

    public void OnTurnStart()
    {
        bool was = _combat.isStaggered;
        _combat.OnTurnStart();
        if (_combat.isStaggered != was) OnStaggerChanged?.Invoke(_combat.isStaggered, _combat.staggerCount);
    }

    public void AddStatus(StatusType type, int potency, int count) { _combat.AddStatus(type, potency, count); OnStatusChanged?.Invoke(); }
    public StatusEffect GetStatus(StatusType type) => _combat.GetStatus(type);
    public bool HasStatus(StatusType type) { var s = _combat.GetStatus(type); return s != null && !s.IsExpired; }
    public int StatusCount => _combat.statusEffects.Count;

    public int ConsumeBleed(int coinCount) { int r = _combat.ConsumeBleed(coinCount); OnHPChanged?.Invoke(_combat.currentHP, _combat.maxHP); OnStatusChanged?.Invoke(); return r; }
    public int GetParalyzedCoins() => _combat.GetParalyzedCoins();
    public void TickParalysis() { _combat.TickParalysis(); OnStatusChanged?.Invoke(); }
    public int TickBurn() { int r = _combat.TickBurn(); OnHPChanged?.Invoke(_combat.currentHP, _combat.maxHP); OnStatusChanged?.Invoke(); return r; }
    public int GetTremorBonus() { int r = _combat.GetTremorBonus(); OnStatusChanged?.Invoke(); return r; }
    public int GetRuptureBonus() => _combat.GetRuptureBonus();
    public void TickRupture() { _combat.TickRupture(); OnStatusChanged?.Invoke(); }
    public void CleanupExpiredStatuses() { _combat.CleanupExpiredStatuses(); OnStatusChanged?.Invoke(); }

    public SkillData EgoSkill
    {
        get
        {
            if (unitData?.skillSlot == null) return null;
            foreach (var s in unitData.skillSlot)
                if (s != null && s.skillType == SkillType.EGO) return s;
            return null;
        }
    }
    public bool CanUseEgo() { var ego = EgoSkill; return ego != null && _combat.sp >= ego.egoCost; }
    public SkillData UseEgo() { var ego = EgoSkill; if (ego == null || !CanUseEgo()) return null; ChangeSP(-ego.egoCost); return ego; }

    private void NotifyAll()
    {
        OnHPChanged?.Invoke(_combat.currentHP, _combat.maxHP);
        OnSPChanged?.Invoke(_combat.sp);
        OnShieldChanged?.Invoke(_combat.shield);
        OnStaggerChanged?.Invoke(_combat.isStaggered, _combat.staggerCount);
        OnStatusChanged?.Invoke();
    }
}
