using System.Collections.Generic;

public class TurnSelectionState
{
    public SkillData selectedSkill;
    public SkillData evadeSkill;
    public int selectedIndex = -1;
    public bool isEvading;
    public bool isGuarding;
    public int guardCardIndex = -1;

    public Dictionary<int, SkillData> unitSelectedSkills = new();
    public Dictionary<int, int> unitSelectedIndices = new();
    public Dictionary<int, Unit> unitTargets = new();
    public Dictionary<int, bool> unitDefenseActive = new();
    public Dictionary<int, SkillData> unitOriginalSkill = new();
    public Unit currentTarget;

    public void Reset()
    {
        selectedSkill = null;
        evadeSkill = null;
        selectedIndex = -1;
        isEvading = false;
        isGuarding = false;
        guardCardIndex = -1;
        unitSelectedSkills.Clear();
        unitSelectedIndices.Clear();
        unitTargets.Clear();
        currentTarget = null;
        unitDefenseActive.Clear();
        unitOriginalSkill.Clear();
    }
}
