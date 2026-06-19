using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Linq;

// One-click backdrop: measures the level's Tilemap bounds and tiles the Background
// prefabs across it (uniform scale = no distortion), cycling the variants for a
// long continuous hall. Everything is parented under a single "Backgrounds" object.
public class SheetBackgroundPlacer : EditorWindow
{
    private const string BG_DIR     = "Assets/prefabs/Area1_ShatteredGrove/Background";
    private const string CONTAINER  = "Backgrounds";

    private float heightFill = 1.10f;   // 1.0 = exactly level height, >1 = overscan
    private float zDepth     = 0f;      // world Z (sorting layer already puts it behind)

    [MenuItem("Ashenlum/4. Place Backgrounds")]
    public static void Open()
    {
        var w = GetWindow<SheetBackgroundPlacer>("Place Backgrounds");
        w.minSize = new Vector2(360, 220);
    }

    private void OnGUI()
    {
        GUILayout.Label("Place Backgrounds", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        heightFill = EditorGUILayout.Slider("Height Fill", heightFill, 1.0f, 2.0f);
        zDepth     = EditorGUILayout.FloatField("Z Depth", zDepth);

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Reads the scene's Tilemap bounds and tiles the Background prefabs behind\n" +
            "the level, parented under \"" + CONTAINER + "\". Run after step 3 (Build Prefabs).",
            MessageType.Info);

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Place Backgrounds", GUILayout.Height(34))) Place();
        if (GUILayout.Button("Remove Backgrounds", GUILayout.Height(24))) RemoveContainer();
    }

    private void Place()
    {
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { BG_DIR })
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(p => p != null && p.GetComponent<SpriteRenderer>() != null
                                  && p.GetComponent<SpriteRenderer>().sprite != null)
            .OrderBy(p => p.name)
            .ToList();

        if (prefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("No backgrounds",
                "No Background prefabs found in:\n" + BG_DIR + "\n\nRun step 3 (Build Prefabs) first.", "OK");
            return;
        }

        if (!TryGetLevelBounds(out Bounds b))
        {
            EditorUtility.DisplayDialog("No bounds",
                "Couldn't find a Tilemap (or any renderers) to measure the level.", "OK");
            return;
        }

        EnsureBackgroundLayerFirst();   // guarantee backgrounds render behind the level
        RemoveContainer();
        var container = new GameObject(CONTAINER);
        Undo.RegisterCreatedObjectUndo(container, "Place Backgrounds");
        int group = Undo.GetCurrentGroup();

        float targetH = Mathf.Max(b.size.y * heightFill, 4f);
        float x       = b.min.x;
        int   i = 0, placed = 0;

        // tile edge-to-edge across the width; uniform scale keeps aspect intact
        while (x < b.max.x && placed < 400)
        {
            var prefab = prefabs[i % prefabs.Count];
            var sprite = prefab.GetComponent<SpriteRenderer>().sprite;
            float h0 = sprite.bounds.size.y;
            float w0 = sprite.bounds.size.x;
            float scale = h0 > 0.0001f ? targetH / h0 : 1f;
            float w = w0 * scale;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Place Background");
            go.transform.SetParent(container.transform, true);
            go.transform.localScale = Vector3.one * scale;
            go.transform.position   = new Vector3(x + w * 0.5f, b.center.y, zDepth);

            x += w; i++; placed++;
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = container;

        EditorUtility.DisplayDialog("Done",
            $"Placed {placed} background panel(s) behind the level.\n" +
            "If any appear in front, your Background sorting layer needs to sit below Environment.", "OK");
    }

    private void RemoveContainer()
    {
        var existing = GameObject.Find(CONTAINER);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }

    // Make "Background" the first sorting layer so it always draws behind the rest.
    private static void EnsureBackgroundLayerFirst()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("m_SortingLayers");

        int idx = -1;
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Background")
            { idx = i; break; }

        if (idx < 0)
        {
            layers.InsertArrayElementAtIndex(layers.arraySize);
            var el = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            el.FindPropertyRelative("name").stringValue  = "Background";
            el.FindPropertyRelative("uniqueID").intValue = "Background".GetHashCode();
            idx = layers.arraySize - 1;
        }

        if (idx != 0) layers.MoveArrayElement(idx, 0);
        tm.ApplyModifiedProperties();
    }

    private static bool TryGetLevelBounds(out Bounds b)
    {
        b = new Bounds();
        bool has = false;

        // Preferred: the terrain Tilemap(s)
        foreach (var r in UnityEngine.Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None))
        {
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        if (has) return true;

        // Fallback: any world renderers (sprites/meshes); UI uses CanvasRenderer so it's excluded
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.transform.root.name == CONTAINER) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return has;
    }
}
