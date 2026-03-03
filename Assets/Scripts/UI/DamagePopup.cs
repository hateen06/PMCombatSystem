using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 데미지 숫자가 떠오르며 사라지는 팝업.
/// DOTween 사용으로 부드러운 연출.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float floatDistance = 80f;
    [SerializeField] private float duration = 0.8f;

    public void Setup(int damage, Color color)
    {
        if (damageText == null)
            damageText = GetComponentInChildren<TextMeshProUGUI>();

        damageText.text = damage.ToString();
        damageText.color = color;

        var rect = GetComponent<RectTransform>();

        // 시퀀스: 동시에 떠오르기 + 페이드 + 스케일
        var seq = DOTween.Sequence();

        // 위로 떠오르기
        seq.Join(rect.DOAnchorPosY(
            rect.anchoredPosition.y + floatDistance, duration)
            .SetEase(Ease.OutQuad));

        // 스케일: 커졌다 줄기
        seq.Join(rect.DOScale(1.5f, duration * 0.3f)
            .SetEase(Ease.OutBack));
        seq.Append(rect.DOScale(0f, duration * 0.5f)
            .SetEase(Ease.InQuad));

        // 페이드아웃 (후반부)
        seq.Insert(duration * 0.4f,
            damageText.DOFade(0f, duration * 0.6f));

        // 끝나면 삭제
        seq.OnComplete(() => Destroy(gameObject));
    }
}
