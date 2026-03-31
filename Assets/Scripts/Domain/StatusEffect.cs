public enum StatusType
{
    Bleed,
    Paralysis,
    Burn,
    Tremor,
    Rupture
}

public class StatusEffect
{
    public StatusType type;
    public int potency;
    public int count;

    public StatusEffect(StatusType type, int potency, int count)
    {
        this.type = type;
        this.potency = potency;
        this.count = count;
    }

    public bool IsExpired => count <= 0;

    public int Consume(int amount)
    {
        int actual = amount < count ? amount : count;
        count -= actual;
        return actual;
    }
}
