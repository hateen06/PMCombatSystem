using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResistanceIndicatorUI : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text resistText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, -0.8f, 0f);

    private Unit _target;
    private Camera _cam;
    private Canvas _canvas;
    private DamageType _currentType;

    private void Awake()
    {
        if (root == null) root = transform as RectTransform;
        if (resistText == null)
        {
            var go = new GameObject("ResistText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            resistText = go.GetComponent<TMP_Text>();
            resistText.fontSize = 16;
            resistText.alignment = TextAlignmentOptions.Center;
        }
        gameObject.SetActive(false);
    }

    public void Bind(Unit target, Canvas canvas)
    {
        _target = target;
        _canvas = canvas;
        _cam = Camera.main;
    }

    public void Show(DamageType attackType)
    {
        if (_target == null) { gameObject.SetActive(false); return; }

        _currentType = attackType;
        float resist = _target.GetResistance(attackType);
        string label;
        Color color;

        if (resist < 1f) { label = "내성"; color = new Color(0.4f, 1f, 0.6f); }
        else if (resist > 1f) { label = "취약"; color = new Color(1f, 0.4f, 0.4f); }
        else { label = "보통"; color = new Color(0.8f, 0.8f, 0.8f); }

        resistText.text = $"{label} x{resist:0.0}";
        resistText.color = color;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    private void LateUpdate()
    {
        if (_target == null || _canvas == null || _cam == null || root == null) return;
        Vector3 worldPos = _target.transform.position + worldOffset;
        Vector2 screenPos = _cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, screenPos, _canvas.worldCamera, out Vector2 canvasPos);
        root.anchoredPosition = canvasPos;
    }
}
