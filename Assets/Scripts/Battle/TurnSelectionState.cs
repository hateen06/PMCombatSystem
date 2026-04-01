using System.Collections.Generic;

public class UnitTurnSelection
{
    public SkillData skill;
    public SkillData originalSkill;
    public SkillData evadeSkill;
    public int cardIndex = -1;
    public bool isEvading;
    public bool isGuarding;
    public int guardCardIndex = -1;
    public Unit target;

    public void Clear()
    {
        skill = null;
        originalSkill = null;
        evadeSkill = null;
        cardIndex = -1;
        isEvading = false;
        isGuarding = false;
        guardCardIndex = -1;
        target = null;
    }
}

public class TurnSelectionState
{
    public Dictionary<int, UnitTurnSelection> units = new();

    public UnitTurnSelection Get(int unitIndex)
    {
        if (!units.TryGetValue(unitIndex, out var selection))
        {
            selection = new UnitTurnSelection();
            units[unitIndex] = selection;
        }
        return selection;
    }

    public void Reset()
    {
        foreach (var pair in units)
            pair.Value?.Clear();
        units.Clear();
    }
}
