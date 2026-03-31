using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SpeedDiceUI : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.7f, 0f);

    private void Awake()
    {
        if (root == null) root = transform as RectTransform;

        if (background == null)
        {
            var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.transform as RectTransform;
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(70f, 46f);
            background = bgGo.GetComponent<Image>();
        }

        if (valueText == null)
        {
            var textGo = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(transform, false);
            var textRt = textGo.transform as RectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            valueText = textGo.GetComponent<TextMeshProUGUI>();
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.fontSize = 30;
            valueText.color = Color.white;
        }
    }

    private Unit _target;
    private Camera _cam;
    private Canvas _canvas;
    private bool _isEnemy;

    public void Bind(Unit target, Canvas canvas, bool isEnemy)
    {
        _target = target;
        _canvas = canvas;
        _cam = Camera.main;
        _isEnemy = isEnemy;
        gameObject.SetActive(target != null);
    }

    public void SetValue(int speed, bool highlighted = false)
    {
        if (valueText != null)
            valueText.text = speed.ToString();

        if (background != null)
        {
            background.color = highlighted
                ? new Color(1f, 0.85f, 0.3f, 0.95f)
                : _isEnemy
                    ? new Color(0.55f, 0.25f, 0.25f, 0.92f)
                    : new Color(0.22f, 0.26f, 0.34f, 0.92f);
        }
    }

    private void LateUpdate()
    {
        if (_target == null || _canvas == null || _cam == null || root == null) return;

        Vector3 worldPos = _target.transform.position + worldOffset;
        Vector2 screenPos = _cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            screenPos,
            _canvas.worldCamera,
            out Vector2 canvasPos);
        root.anchoredPosition = canvasPos;
    }
}
