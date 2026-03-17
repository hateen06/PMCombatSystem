using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 림버스 스킬 덱 시스템.
/// 스킬1 x3, 스킬2 x2, 스킬3 x1 = 6장.
/// 매 턴 2장 뽑아서 선택지 제공, 다 쓰면 리필.
/// </summary>
public class SkillDeck
{
    private readonly SkillData _skill1;
    private readonly SkillData _skill2;
    private readonly SkillData _skill3;

    private List<SkillData> _drawPile = new List<SkillData>();
    private List<SkillData> _currentHand = new List<SkillData>();

    public IReadOnlyList<SkillData> CurrentHand => _currentHand;
    public int RemainingCards => _drawPile.Count;

    public SkillDeck(SkillData skill1, SkillData skill2, SkillData skill3)
    {
        _skill1 = skill1;
        _skill2 = skill2;
        _skill3 = skill3;
        Refill();
    }

    /// <summary>
    /// 덱 리필: 스킬1 x3 + 스킬2 x2 + 스킬3 x1 = 6장
    /// </summary>
    public void Refill()
    {
        _drawPile.Clear();

        if (_skill1 != null) { _drawPile.Add(_skill1); _drawPile.Add(_skill1); _drawPile.Add(_skill1); }
        if (_skill2 != null) { _drawPile.Add(_skill2); _drawPile.Add(_skill2); }
        if (_skill3 != null) { _drawPile.Add(_skill3); }

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
    /// 선택한 스킬을 핸드에서 제거. 안 쓴 카드는 버림.
    /// </summary>
    public SkillData UseCard(int handIndex)
    {
        if (handIndex < 0 || handIndex >= _currentHand.Count)
            return null;

        var skill = _currentHand[handIndex];
        _currentHand.Clear(); // 안 쓴 카드도 소모됨
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
