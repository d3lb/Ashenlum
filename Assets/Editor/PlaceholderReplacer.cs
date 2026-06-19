using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class PlaceholderReplacer : EditorWindow
{
    [System.Serializable]
    private class MappingEntry
    {
        public GameObject placeholder;
        public GameObject targetPrefab;
        public string     tagFilter   = "";
        public string     nameFilter  = "";
        public bool       useTag      = false;
        public bool       useName     = false;
    }

    private List<MappingEntry> mappings    = new();
    private Vector2            scrollPos;
    private bool               previewMode = false;
    private List<GameObject>   previewHighlights = new();

    private const string CONTAINER_NAME = "Environment_Art_Rendered";

    [MenuItem("Ashenlum/Placeholder Replacer")]
    public static void OpenWindow()
    {
        var w = GetWindow<PlaceholderReplacer>("Placeholder Replacer");
        w.minSize = new Vector2(520, 400);
    }

    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(4);
        DrawMappingList();
        EditorGUILayout.Space(6);
        DrawActionBar();
    }

    // ── Header ──────────────────────────────────────────────────────────────────
    private void DrawHeader()
    {
        GUILayout.Label("Placeholder → Final Asset Replacer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Map placeholder GameObjects (by reference, tag, or name) to final prefabs.\n" +
            "Replacements are parented under « " + CONTAINER_NAME + " » in the active scene.",
            MessageType.Info);
    }

    // ── Mapping list ────────────────────────────────────────────────────────────
    private void DrawMappingList()
    {
        GUILayout.Label("Mappings", EditorStyles.miniBoldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(300));

        for (int i = 0; i < mappings.Count; i++)
        {
            var m = mappings[i];
            EditorGUILayout.BeginVertical("box");

            // Row 1: Placeholder reference + match mode toggles
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Placeholder", GUILayout.Width(78));
            m.placeholder = (GameObject)EditorGUILayout.ObjectField(
                m.placeholder, typeof(GameObject), true, GUILayout.Width(160));

            m.useTag  = GUILayout.Toggle(m.useTag,  "Tag",  GUILayout.Width(38));
            if (m.useTag)
                m.tagFilter = EditorGUILayout.TagField(m.tagFilter, GUILayout.Width(110));

            m.useName = GUILayout.Toggle(m.useName, "Name", GUILayout.Width(44));
            if (m.useName)
                m.nameFilter = EditorGUILayout.TextField(m.nameFilter, GUILayout.Width(100));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                mappings.RemoveAt(i);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            // Row 2: Target prefab
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Target Prefab", GUILayout.Width(78));
            m.targetPrefab = (GameObject)EditorGUILayout.ObjectField(
                m.targetPrefab, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ Add Mapping"))
            mappings.Add(new MappingEntry());
    }

    // ── Action bar ──────────────────────────────────────────────────────────────
    private void DrawActionBar()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Preview Matches", GUILayout.Height(28)))
            RunPreview();

        if (GUILayout.Button("Clear Preview", GUILayout.Height(28)))
            ClearPreview();

        GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
        if (GUILayout.Button("Replace All", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog(
                    "Replace All Placeholders",
                    "This will destroy all matched placeholder GameObjects and replace them " +
                    "with the target prefabs. This action is undoable.\n\nProceed?",
                    "Replace", "Cancel"))
                RunReplacement();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        GUILayout.Label($"Active scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}",
            EditorStyles.miniLabel);
    }

    // ── Preview ─────────────────────────────────────────────────────────────────
    private void RunPreview()
    {
        ClearPreview();
        var matches = FindAllMatches();
        foreach (var (go, _) in matches)
        {
            // Highlight in scene via Selection
            previewHighlights.Add(go);
        }
        Selection.objects = previewHighlights.ToArray();
        Debug.Log($"[PlaceholderReplacer] Preview: {matches.Count} placeholder(s) found.");
        SceneView.RepaintAll();
    }

    private void ClearPreview()
    {
        previewHighlights.Clear();
        Selection.objects = new Object[0];
    }

    // ── Replacement ─────────────────────────────────────────────────────────────
    private void RunReplacement()
    {
        ClearPreview();
        var scene    = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var matches  = FindAllMatches();

        if (matches.Count == 0)
        {
            EditorUtility.DisplayDialog("No Matches", "No placeholders were found in the active scene.", "OK");
            return;
        }

        // Get or create container
        var container = GetOrCreateContainer();

        Undo.SetCurrentGroupName("Replace Placeholders");
        int group = Undo.GetCurrentGroup();

        int replaced = 0;
        foreach (var (placeholder, targetPrefab) in matches)
        {
            if (placeholder == null || targetPrefab == null) continue;

            var t = placeholder.transform;

            // Instantiate final prefab
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Final Asset");

            instance.transform.position   = t.position;
            instance.transform.rotation   = t.rotation;
            instance.transform.localScale = t.localScale;
            instance.name                 = targetPrefab.name;

            Undo.SetTransformParent(instance.transform, container.transform, "Parent to Container");

            // Destroy placeholder
            Undo.DestroyObjectImmediate(placeholder);
            replaced++;
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[PlaceholderReplacer] Replaced {replaced} placeholder(s) → parented under '{CONTAINER_NAME}'.");
        EditorUtility.DisplayDialog("Done", $"Replaced {replaced} placeholder(s) successfully!", "OK");
    }

    // ── Match finding ────────────────────────────────────────────────────────────
    private List<(GameObject go, GameObject prefab)> FindAllMatches()
    {
        var results = new List<(GameObject, GameObject)>();
        var scene   = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots   = scene.GetRootGameObjects();

        // Flatten all scene GameObjects
        var allObjects = new List<GameObject>();
        foreach (var root in roots)
            CollectAll(root, allObjects);

        foreach (var entry in mappings)
        {
            if (entry.targetPrefab == null) continue;

            foreach (var go in allObjects)
            {
                if (go == null) continue;

                bool matched = false;

                // Match by direct reference
                if (entry.placeholder != null)
                {
                    // Check if this scene object was spawned from the same prefab
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (source == entry.placeholder || go == entry.placeholder)
                        matched = true;

                    // Also match by prefab asset path
                    if (!matched)
                    {
                        string entryPath = AssetDatabase.GetAssetPath(entry.placeholder);
                        string goPath    = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                        if (!string.IsNullOrEmpty(entryPath) && entryPath == goPath)
                            matched = true;
                    }
                }

                // Match by tag
                if (!matched && entry.useTag && !string.IsNullOrEmpty(entry.tagFilter))
                {
                    try { if (go.CompareTag(entry.tagFilter)) matched = true; }
                    catch { /* tag doesn't exist */ }
                }

                // Match by name (contains)
                if (!matched && entry.useName && !string.IsNullOrEmpty(entry.nameFilter))
                {
                    if (go.name.ToLower().Contains(entry.nameFilter.ToLower()))
                        matched = true;
                }

                if (matched)
                    results.Add((go, entry.targetPrefab));
            }
        }

        return results;
    }

    private void CollectAll(GameObject root, List<GameObject> list)
    {
        list.Add(root);
        foreach (Transform child in root.transform)
            CollectAll(child.gameObject, list);
    }

    // ── Container ────────────────────────────────────────────────────────────────
    private GameObject GetOrCreateContainer()
    {
        var existing = GameObject.Find(CONTAINER_NAME);
        if (existing != null) return existing;

        var container = new GameObject(CONTAINER_NAME);
        Undo.RegisterCreatedObjectUndo(container, "Create Container");
        return container;
    }
}
