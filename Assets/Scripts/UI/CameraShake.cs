using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    // 외부에서 호출하면 카메라가 흔들림
    public void Shake(float duration = 0.2f, float strength = 0.3f)
    {
        transform.DOShakePosition(duration, strength)
            .SetEase(Ease.OutQuad);
    }
}
