using UnityEngine;

public class LayoutDumpOverlay : MonoBehaviour
{
    [SerializeField] private LayoutDumper dumper;
    [SerializeField] private bool visible = true;

    public void Toggle() => visible = !visible;

    private void OnGUI()
    {
        if (!visible || dumper == null || dumper.LastSnapshot == null) return;

        var snapshot = dumper.LastSnapshot;
        for (int i = 0; i < snapshot.items.Count; i++)
        {
            var item = snapshot.items[i];
            var rect = item.screenRect;
            rect.y = Screen.height - rect.yMax;

            Color color = item.kind == "World" ? new Color(1f, 0.7f, 0.2f) : new Color(0.3f, 0.9f, 1f);
            if (item.behindCamera) color = Color.red;
            else if (item.visibility == "Partial") color = Color.yellow;
            else if (item.overlapSeverity > 0.05f) color = new Color(1f, 0.4f, 0.8f);

            DrawRect(rect, color);
            GUI.color = color;
            GUI.Label(new Rect(rect.x, rect.y - 18f, 220f, 18f), item.name + " [" + item.visibility + "]");
        }

        GUI.color = Color.white;
        GUI.Label(new Rect(10f, 10f, 500f, 24f), "Layout issues: " + snapshot.issues.Count);
    }

    private void DrawRect(Rect rect, Color color)
    {
        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax, rect.y, 1f, rect.height + 1f), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
