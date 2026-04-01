using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PMTools
{
    [MenuItem("Tools/PM/프로젝트 상태 확인")]
    public static void Status()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var goCount = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        Debug.Log($"Scene: {scene.name} | Objects: {goCount} | Compile Error: {EditorUtility.scriptCompilationFailed}");
    }

    [MenuItem("Tools/PM/레이아웃 덤프")]
    public static void DumpLayout()
    {
        var dumper = EnsureLayoutDumper();
        if (dumper == null)
        {
            Debug.LogWarning("LayoutDumper not found");
            return;
        }

        dumper.Dump();
        Debug.Log("Layout dump saved");
    }

    [MenuItem("Tools/PM/레이아웃 오버레이 토글")]
    public static void ToggleLayoutOverlay()
    {
        var dumper = EnsureLayoutDumper();
        if (dumper == null)
        {
            Debug.LogWarning("LayoutDumper not found");
            return;
        }

        var overlay = dumper.GetComponent<LayoutDumpOverlay>();
        if (overlay == null)
            overlay = Undo.AddComponent<LayoutDumpOverlay>(dumper.gameObject);

        overlay.SetVisible(!overlay.Visible);
        EditorUtility.SetDirty(overlay);
        Debug.Log($"Layout overlay: {(overlay.Visible ? "ON" : "OFF")}");
    }

    [MenuItem("Tools/PM/스프라이트 FullRect 수정")]
    public static void SpriteFixAll()
    {
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites" });
        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(settings);
                imp.SaveAndReimport();
                count++;
            }
        }
        Debug.Log($"Fixed {count} sprites to FullRect");
    }

    [MenuItem("Tools/PM/씬 저장")]
    public static void Save()
    {
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Saved");
    }

    private static LayoutDumper EnsureLayoutDumper()
    {
        var dumper = Object.FindFirstObjectByType<LayoutDumper>();
        if (dumper != null) return dumper;

        var go = new GameObject("LayoutDumper");
        Undo.RegisterCreatedObjectUndo(go, "Create LayoutDumper");
        dumper = go.AddComponent<LayoutDumper>();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return dumper;
    }
}
