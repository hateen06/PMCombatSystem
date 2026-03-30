using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// HP 게이지 바. anchorMax.x 기반으로 게이지가 줄어드는 방식.
/// fillAmount 방식은 stretch RectTransform에서 시각적으로 안 보이는 문제가 있어
/// anchorMax.x를 HP 비율에 맞춰 조절하는 방식으로 변경.
/// </summary>
public class HPBar : MonoBehaviour
{
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private Image fillImage;
    [SerializeField] private float tweenDuration = 0.4f;

    private Unit _target;
    private Tween _currentTween;

    public void Bind(Unit unit)
    {
        _target = unit;

        // fillRect 자동 탐색
        if (fillRect == null && fillImage != null)
            fillRect = fillImage.GetComponent<RectTransform>();
        if (fillRect == null)
        {
            var fill = transform.Find("Fill");
            if (fill != null) fillRect = fill.GetComponent<RectTransform>();
        }

        // 초기 상태: 꽉 참
        if (fillRect != null)
        {
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
        }

        // fillImage는 Simple 타입으로 (Filled 안 씀)
        if (fillImage != null)
            fillImage.type = Image.Type.Simple;
    }

    /// <summary>
    /// 외부에서 호출 — HP 변경 시.
    /// anchorMax.x를 HP 비율에 맞춰 줄여서 게이지가 시각적으로 줄어듦.
    /// </summary>
    public void Refresh()
    {
        if (_target == null || fillRect == null) return;

        float targetRatio = Mathf.Clamp01(_target.HPRatio);

        _currentTween?.Kill();

        _currentTween = DOTween.To(
            () => fillRect.anchorMax.x,
            x => {
                var a = fillRect.anchorMax;
                a.x = x;
                fillRect.anchorMax = a;
            },
            targetRatio,
            tweenDuration
        ).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 피격 시 흔들림 + HP 갱신
    /// </summary>
    public void OnHit()
    {
        Refresh();
        transform.DOShakePosition(0.2f, 5f, 20)
            .SetEase(Ease.OutQuad);
    }
}
