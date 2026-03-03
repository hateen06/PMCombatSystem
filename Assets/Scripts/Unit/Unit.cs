using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private UnitData unitData;

    private int currentHP;
    private bool isAlive;
    private readonly List<StatusEffect> statusEffects = new List<StatusEffect>();

    // 외부에서 읽기만 가능 (캡슐화)
    public string UnitName => unitData != null ? unitData.unitName : "없음";
    public int CurrentHP => currentHP;
    public int MaxHP => unitData != null ? unitData.maxHP : 0;
    public bool IsAlive => isAlive;
    public float HPRatio => MaxHP > 0 ? (float)currentHP / MaxHP : 0f;
    public SkillData[] SkillSlots => unitData != null ? unitData.skillSlot : null;

    public void Initialize()
    {
        if (unitData == null)
        {
            isAlive = false;
            return;
        }
        currentHP = unitData.maxHP;
        isAlive = true;
        statusEffects.Clear();
    }

    public int RollSpeed()
    {
        if (unitData == null) return 0;
        return Random.Range(unitData.minSpeed, unitData.maxSpeed + 1);
    }

    public void TakeDamage(int damage)
    {
        if (!isAlive || damage <= 0) return;

        currentHP -= damage;
        if (currentHP <= 0)
        {
            currentHP = 0;
            isAlive = false;
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