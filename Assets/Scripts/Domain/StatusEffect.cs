/// <summary>
/// 상태이상 타입.
/// 새 상태이상 추가 시 여기에 enum만 추가하면 된다.
/// </summary>
public enum StatusType
{
    Bleed,      // 출혈: 공격 시 코인당 피해
    Paralysis   // 마비: 코인 수 감소
}

/// <summary>
/// 런타임 상태이상 인스턴스.
/// ScriptableObject가 아닌 일반 클래스 — 유닛마다 개별 상태를 갖는다.
/// </summary>
public class StatusEffect
{
    public StatusType type;
    public int potency;   // 위력 (출혈: 데미지, 마비: 코인 감소)
    public int count;     // 남은 횟수

    public StatusEffect(StatusType type, int potency, int count)
    {
        this.type = type;
        this.potency = potency;
        this.count = count;
    }

    public bool IsExpired => count <= 0;

    /// <summary>
    /// 횟수 소모. 소모한 만큼 반환.
    /// </summary>
    public int Consume(int amount)
    {
        int actual = amount < count ? amount : count;
        count -= actual;
        return actual;
    }
}
