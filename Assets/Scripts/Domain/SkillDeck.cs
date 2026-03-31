using System.Collections.Generic;
using UnityEngine;
public class SkillDeck
{
    private readonly SkillData[] _allSkills;

    private List<SkillData> _drawPile = new List<SkillData>();
    private List<SkillData> _currentHand = new List<SkillData>();

    public IReadOnlyList<SkillData> CurrentHand => _currentHand;
    public int RemainingCards => _drawPile.Count;
    public System.Action<IReadOnlyList<SkillData>> OnHandChanged;

    public SkillDeck(SkillData skill1, SkillData skill2, SkillData skill3)
        : this(new[] { skill1, skill2, skill3 }) { }
    public SkillDeck(SkillData[] skills)
    {
        _allSkills = skills ?? new SkillData[0];
        Refill();
    }
    public void Refill()
    {
        _drawPile.Clear();

        int attackIdx = 0;
        int[] attackCounts = { 3, 2, 1 };

        for (int i = 0; i < _allSkills.Length; i++)
        {
            var skill = _allSkills[i];
            if (skill == null) continue;

            // 방어/회피는 덱에 넣지 않음 (우클릭으로만 발동)
            if (skill.skillType != SkillType.Attack) continue;

            int count = attackIdx < attackCounts.Length ? attackCounts[attackIdx] : 1;
            for (int c = 0; c < count; c++) _drawPile.Add(skill);
            attackIdx++;
        }

        Shuffle();
    }
    public void DrawHand(int count = 2)
    {
        _currentHand.Clear();

        if (_drawPile.Count == 0)
            Refill();

        for (int i = 0; i < count && _drawPile.Count > 0; i++)
        {
            _currentHand.Add(_drawPile[0]);
            _drawPile.RemoveAt(0);
        }

        OnHandChanged?.Invoke(CurrentHand);
    }
    public SkillData UseCard(int handIndex)
    {
        if (handIndex < 0 || handIndex >= _currentHand.Count)
            return null;

        var skill = _currentHand[handIndex];
        _currentHand.RemoveAt(handIndex);
        OnHandChanged?.Invoke(CurrentHand);
        return skill;
    }

    private void Shuffle()
    {
        for (int i = _drawPile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = _drawPile[i];
            _drawPile[i] = _drawPile[j];
            _drawPile[j] = temp;
        }
    }
}
