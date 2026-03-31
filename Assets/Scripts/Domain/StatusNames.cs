public static class StatusNames
{
    public static string ToKorean(StatusType type)
    {
        return type switch
        {
            StatusType.Bleed => "출혈",
            StatusType.Paralysis => "마비",
            StatusType.Burn => "화상",
            StatusType.Tremor => "진동",
            StatusType.Rupture => "파열",
            _ => type.ToString()
        };
    }
}
