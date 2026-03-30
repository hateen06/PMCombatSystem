using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 림버스 스킬 덱 시스템.
/// 스킬1 x3, 스킬2 x2, 스킬3 x1 = 6장.
/// 매 턴 2장 뽑아서 선택지 제공, 다 쓰면 리필.
/// </summary>
public class SkillDeck
{
    private readonly SkillData[] _allSkills;

    private List<SkillData> _drawPile = new List<SkillData>();
    private List<SkillData> _currentHand = new List<SkillData>();

    public IReadOnlyList<SkillData> CurrentHand => _currentHand;
    public int RemainingCards => _drawPile.Count;

    public SkillDeck(SkillData skill1, SkillData skill2, SkillData skill3)
        : this(new[] { skill1, skill2, skill3 }) { }

    /// <summary>
    /// 전체 skillSlot 배열로 초기화.
    /// 공격: 스킬1 x3 + 스킬2 x2 + 스킬3 x1
    /// 방어/회피: 각 1장
    /// </summary>
    public SkillDeck(SkillData[] skills)
    {
        _allSkills = skills ?? new SkillData[0];
        Refill();
    }

    /// <summary>
    /// 덱 리필: 공격 스킬은 3/2/1 비율, 방어/회피는 1장씩
    /// </summary>
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

    /// <summary>
    /// 매 턴 호출: 2장 뽑아서 핸드에 올림.
    /// </summary>
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
    }

    /// <summary>
    /// 선택한 스킬을 핸드에서 제거.
    /// 림버스 방식: 사용한 카드만 소모, 안 쓴 카드는 그대로 남음.
    /// </summary>
    public SkillData UseCard(int handIndex)
    {
        if (handIndex < 0 || handIndex >= _currentHand.Count)
            return null;

        var skill = _currentHand[handIndex];
        _currentHand.RemoveAt(handIndex);
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
