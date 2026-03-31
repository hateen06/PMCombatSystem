using System.Collections;
using UnityEngine;
using DG.Tweening;
public class HitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private int flashCount = 3;

    private Color _originalColor;
    private Coroutine _flashRoutine;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            _originalColor = spriteRenderer.color;
    }

    public void Flash()
    {
        if (spriteRenderer == null) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float interval = flashDuration / (flashCount * 2);
        Color hitColor = new Color(1f, 0.3f, 0.3f, 1f); // 빨간 틴트

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = hitColor;
            yield return new WaitForSeconds(interval);
            spriteRenderer.color = _originalColor;
            yield return new WaitForSeconds(interval);
        }

        spriteRenderer.color = _originalColor;
        _flashRoutine = null;
    }
}
