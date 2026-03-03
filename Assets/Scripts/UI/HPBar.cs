using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// HP 게이지 바. 이벤트 기반으로 DOTween 애니메이션.
/// </summary>
public class HPBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float tweenDuration = 0.4f;

    private Unit _target;
    private Tweener _currentTween;

    public void Bind(Unit unit)
    {
        _target = unit;
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
        }
    }

    /// <summary>
    /// 외부에서 호출 — HP 변경 시
    /// </summary>
    public void Refresh()
    {
        if (_target == null || fillImage == null) return;

        float targetRatio = _target.HPRatio;

        // 진행 중인 트윈 취소
        _currentTween?.Kill();

        _currentTween = fillImage
            .DOFillAmount(targetRatio, tweenDuration)
            .SetEase(Ease.OutQuad);
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
