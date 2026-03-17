using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private UnitData unitData;

    private int currentHP;
    private bool isAlive;
    private int _sp;
    private readonly List<StatusEffect> statusEffects = new List<StatusEffect>();

    // ── SP 상수 (림버스 규칙) ──
    public const int SP_MIN = -45;
    public const int SP_MAX = 45;
    public const int SP_CLASH_WIN = 10;
    public const int SP_ALLY_DEATH = -10;
    public const int SP_PANIC_THRESHOLD = -45;

    // 외부에서 읽기만 가능 (캡슐화)
    public string UnitName => unitData != null ? unitData.unitName : "없음";
    public int CurrentHP => currentHP;
    public int MaxHP => unitData != null ? unitData.maxHP : 0;
    public bool IsAlive => isAlive;
    public float HPRatio => MaxHP > 0 ? (float)currentHP / MaxHP : 0f;
    public SkillData[] SkillSlots => unitData != null ? unitData.skillSlot : null;
    public int SP => _sp;
    public bool IsPanicked => _sp <= SP_PANIC_THRESHOLD;

    // ── Stagger ──
    private bool _isStaggered;
    private int _staggerTurnsLeft;

    public bool IsStaggered => _isStaggered;
    public float StaggerThreshold => unitData != null ? unitData.staggerThreshold : 0.5f;

    /// <summary>
    /// 흐트러짐 상태면 받는 피해 1.5배
    /// </summary>
    public float DamageMultiplier => _isStaggered ? 1.5f : 1f;

    /// <summary>
    /// 코인 앞면 확률 (0~100). 림버스 공식: 50 + SP
    /// </summary>
    public int CoinHeadsChance => Mathf.Clamp(50 + _sp, 5, 95);

    private SkillDeck _deck;
    public SkillDeck Deck => _deck;

    public void Initialize()
    {
        if (unitData == null)
        {
            isAlive = false;
            return;
        }
        currentHP = unitData.maxHP;
        isAlive = true;
        _sp = 0;
        _isStaggered = false;
        _staggerTurnsLeft = 0;
        statusEffects.Clear();

        // 스킬 덱 초기화 (3개 슬롯 → 스킬1, 스킬2, 스킬3)
        var slots = SkillSlots;
        if (slots != null && slots.Length >= 3)
            _deck = new SkillDeck(slots[0], slots[1], slots[2]);
        else if (slots != null && slots.Length == 2)
            _deck = new SkillDeck(slots[0], slots[1], null);
        else if (slots != null && slots.Length == 1)
            _deck = new SkillDeck(slots[0], null, null);
    }

    // ── SP 시스템 ──

    public void ChangeSP(int amount)
    {
        _sp = Mathf.Clamp(_sp + amount, SP_MIN, SP_MAX);
    }

    public void OnClashWin()
    {
        ChangeSP(SP_CLASH_WIN);
    }

    public void OnAllyDeath()
    {
        ChangeSP(SP_ALLY_DEATH);
    }

    public int RollSpeed()
    {
        if (unitData == null) return 0;
        return Random.Range(unitData.minSpeed, unitData.maxSpeed + 1);
    }

    public void TakeDamage(int damage)
    {
        if (!isAlive || damage <= 0) return;

        // 흐트러짐 상태면 피해 1.5배
        int finalDamage = Mathf.RoundToInt(damage * DamageMultiplier);
        currentHP -= finalDamage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            isAlive = false;
            return;
        }

        // 흐트러짐 체크: HP가 임계선 이하로 내려갔는가
        CheckStagger();
    }

    private void CheckStagger()
    {
        if (_isStaggered) return; // 이미 흐트러진 상태
        if (HPRatio <= StaggerThreshold)
        {
            _isStaggered = true;
            _staggerTurnsLeft = 2; // 현재 턴 + 다음 턴
            Debug.Log($"[Stagger] {UnitName} 흐트러짐! (HP {HPRatio:P0} <= {StaggerThreshold:P0})");
        }
    }

    /// <summary>
    /// 턴 시작 시 호출. 흐트러짐 카운트다운.
    /// </summary>
    public void OnTurnStart()
    {
        if (!_isStaggered) return;

        _staggerTurnsLeft--;
        if (_staggerTurnsLeft <= 0)
        {
            _isStaggered = false;
            Debug.Log($"[Stagger] {UnitName} 흐트러짐 회복!");
        }
    }

    // ── 상태이상 시스템 ──

    public void AddStatus(StatusType type, int potency, int count)
    {
        if (!isAlive || potency <= 0 || count <= 0) return;

        var existing = GetStatus(type);
        if (existing != null)
        {
            // 중첩: 위력은 더 높은 값, 횟수는 합산
            existing.potency = existing.potency > potency ? existing.potency : potency;
            existing.count += count;
        }
        else
        {
            statusEffects.Add(new StatusEffect(type, potency, count));
        }
    }

    public StatusEffect GetStatus(StatusType type)
    {
        for (int i = 0; i < statusEffects.Count; i++)
            if (statusEffects[i].type == type) return statusEffects[i];
        return null;
    }

    /// <summary>
    /// 출혈 소모: 공격 시 코인당 피해. 소모한 총 피해량 반환.
    /// </summary>
    public int ConsumeBleed(int coinCount)
    {
        if (!isAlive) return 0;
        var bleed = GetStatus(StatusType.Bleed);
        if (bleed == null || bleed.IsExpired) return 0;

        int consumed = bleed.Consume(coinCount);
        int damage = bleed.potency * consumed;
        TakeDamage(damage);

        if (bleed.IsExpired) statusEffects.Remove(bleed);
        return damage;
    }

    /// <summary>
    /// 마비 효과: 코인 수 감소량 반환.
    /// </summary>
    public int ConsumeParalysis()
    {
        var para = GetStatus(StatusType.Paralysis);
        if (para == null || para.IsExpired) return 0;

        int reduction = para.potency;
        para.Consume(1);

        if (para.IsExpired) statusEffects.Remove(para);
        return reduction;
    }

    /// <summary>
    /// 턴 종료 시 만료된 상태이상 정리.
    /// </summary>
    public void CleanupExpiredStatuses()
    {
        statusEffects.RemoveAll(s => s.IsExpired);
    }

    public bool HasStatus(StatusType type) => GetStatus(type) != null && !GetStatus(type).IsExpired;
    public int StatusCount => statusEffects.Count;
}