using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Turns the sliced *_Transparent.png into ready-to-drop prefabs.
// Category is decided by WHERE each sprite sits on the sheet (works even though
// Unity's auto-slicer names them generically). Each prefab gets the correct
// sorting layer + collider so it behaves right the moment you drag it in.
public class SheetPrefabBuilder : EditorWindow
{
    private Texture2D sheet;
    private const string ROOT = "Assets/prefabs/Area1_ShatteredGrove";

    // Section regions in TOP-LEFT pixel coords (sheet is 1448x1086).
    private static readonly (string cat, RectInt region)[] Sections =
    {
        ("Background",   new RectInt( 12,  75, 566, 480)),
        ("Tile",         new RectInt(595,  70, 360, 495)),
        ("Platform",     new RectInt(980,  70, 460, 165)),
        ("Architecture", new RectInt(980, 315, 460, 250)),
        ("Natural",      new RectInt( 10, 620, 455, 380)),
        ("Interactable", new RectInt(475, 620, 450, 415)),
        ("Detail",       new RectInt(980, 620, 460, 380)),
    };

    private static readonly Dictionary<string, (string layer, int order)> LayerMap = new()
    {
        ["Background"]   = ("Background",  -1),
        ["Tile"]         = ("Environment",  0),
        ["Platform"]     = ("Environment",  0),
        ["Architecture"] = ("Environment",  0),
        ["Natural"]      = ("Detail",       1),
        ["Interactable"] = ("Object",       0),
        ["Detail"]       = ("Detail",       1),
    };

    [MenuItem("Ashenlum/3. Build Prefabs From Sheet")]
    public static void Open()
    {
        var w = GetWindow<SheetPrefabBuilder>("Prefab Builder");
        w.minSize = new Vector2(360, 200);
    }

    private void OnGUI()
    {
        GUILayout.Label("Build Prefabs From Sheet", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);
        sheet = (Texture2D)EditorGUILayout.ObjectField("Sliced Sheet", sheet, typeof(Texture2D), false);

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Use the sliced *_Transparent.png.\n" +
            "Creates categorized prefabs under " + ROOT + "/[Category]/\n" +
            "with the right sorting layer + collider for each type.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        GUI.enabled = sheet != null;
        if (GUILayout.Button("Build Prefabs", GUILayout.Height(34)))
            Build();
        GUI.enabled = true;
    }

    private void Build()
    {
        string path = AssetDatabase.GetAssetPath(sheet);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("No slices",
                "This texture has no sprites. Slice it first (Sprite Editor → Automatic).", "OK");
            return;
        }

        EnsureSortingLayers();
        EnsureDirs();

        int H = sheet.height;
        var counters = new Dictionary<string, int>();
        int made = 0;

        try
        {
            for (int s = 0; s < sprites.Count; s++)
            {
                var sprite = sprites[s];
                EditorUtility.DisplayProgressBar("Prefab Builder",
                    $"Building {sprite.name}…", s / (float)sprites.Count);

                // sprite.rect is BOTTOM-LEFT pixels → convert center to TOP-LEFT
                float cx   = sprite.rect.x + sprite.rect.width  * 0.5f;
                float cyTL = H - (sprite.rect.y + sprite.rect.height * 0.5f);

                string cat = CategoryAt(cx, cyTL);
                if (!counters.ContainsKey(cat)) counters[cat] = 0;
                string name = $"{cat}_{counters[cat]++:D2}";

                string prefabPath = $"{ROOT}/{cat}/{name}.prefab";

                var go = new GameObject(name);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite           = sprite;
                var (layer, order)  = LayerMap[cat];
                sr.sortingLayerName = layer;
                sr.sortingOrder     = order;

                AddCollider(go, sprite, cat);

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                DestroyImmediate(go);
                made++;
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Prefabs built",
            $"Created {made} prefab(s) under {ROOT}/.\n\nNext: drag the Background prefabs into the scene.", "OK");
    }

    private static string CategoryAt(float xTL, float yTL)
    {
        foreach (var (cat, r) in Sections)
            if (xTL >= r.xMin && xTL < r.xMax && yTL >= r.yMin && yTL < r.yMax)
                return cat;
        return "Detail";   // fallback for stragglers
    }

    private static void AddCollider(GameObject go, Sprite sprite, string cat)
    {
        Vector2 size   = sprite.bounds.size;
        Vector2 center = sprite.bounds.center;

        switch (cat)
        {
            case "Tile":
            case "Architecture":
                var box = go.AddComponent<BoxCollider2D>();
                box.size = size; box.offset = center;
                break;

            case "Platform":   // thin lip on top surface only
                var pb = go.AddComponent<BoxCollider2D>();
                pb.size   = new Vector2(size.x, 0.12f);
                pb.offset = new Vector2(center.x, center.y + size.y * 0.5f - 0.06f);
                break;

            case "Interactable":
                var ib = go.AddComponent<BoxCollider2D>();
                ib.size = size; ib.offset = center; ib.isTrigger = true;
                break;

            // Background, Natural, Detail → no collider
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────────
    private static void EnsureDirs()
    {
        EnsureDir(ROOT);
        foreach (var c in LayerMap.Keys) EnsureDir($"{ROOT}/{c}");
    }
    private static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void EnsureSortingLayers()
    {
        string[] req = { "Background", "Environment", "Object", "Detail" };
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("m_SortingLayers");
        var have = new HashSet<string>();
        for (int i = 0; i < layers.arraySize; i++)
            have.Add(layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
        bool dirty = false;
        foreach (var n in req)
        {
            if (have.Contains(n)) continue;
            layers.InsertArrayElementAtIndex(layers.arraySize);
            var el = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            el.FindPropertyRelative("name").stringValue  = n;
            el.FindPropertyRelative("uniqueID").intValue = n.GetHashCode();
            dirty = true;
        }
        if (dirty) tm.ApplyModifiedProperties();
    }
}
