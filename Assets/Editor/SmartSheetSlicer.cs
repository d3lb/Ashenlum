using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// Region-aware slicer for the Shattered Grove sheet (1448x1086).
// Backgrounds  -> Strip mode  (N equal panels, kept whole).
// Everything   -> Blob mode   (alpha mask -> morphological CLOSE to bridge
//                              JPEG gaps -> connected components -> bbox).
// Headers are excluded because each region starts BELOW its title text, with a
// wide/short/low-fill text filter as a second line of defense.
public class SmartSheetSlicer : EditorWindow
{
    private Texture2D sheet;     // the *_Transparent.png
    private int  minAreaPx   = 130;
    private byte alphaCut    = 16;
    private bool alsoPrefabs = false;

    private const string ROOT = "Assets/prefabs/Area1_ShatteredGrove";

    private enum Mode { Strip, Blob }
    private struct Section
    {
        public string  cat;
        public RectInt region;   // TOP-LEFT pixel coords
        public Mode    mode;
        public int     strips;
        public int     closeR;
        public Section(string c, RectInt r, Mode m, int s, int cr)
        { cat = c; region = r; mode = m; strips = s; closeR = cr; }
    }

    // Tuned for 1448x1086. Nudge a region if a section is clipped or grabs a header.
    private static readonly Section[] Sections =
    {
        new Section("Background",   new RectInt( 12,  75, 566, 480), Mode.Strip, 3, 0),
        new Section("Tile",         new RectInt(595,  70, 360, 495), Mode.Blob,  0, 2),
        new Section("Platform",     new RectInt(980,  70, 460, 165), Mode.Blob,  0, 4),
        new Section("Architecture", new RectInt(980, 315, 460, 250), Mode.Blob,  0, 4),
        new Section("Natural",      new RectInt( 10, 620, 455, 380), Mode.Blob,  0, 3),
        new Section("Interactable", new RectInt(475, 620, 450, 415), Mode.Blob,  0, 3),
        new Section("Detail",       new RectInt(980, 620, 460, 380), Mode.Blob,  0, 2),
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

    [MenuItem("Ashenlum/2. Smart Slice Sheet")]
    public static void Open()
    {
        var w = GetWindow<SmartSheetSlicer>("Smart Slicer");
        w.minSize = new Vector2(380, 250);
    }

    private void OnGUI()
    {
        GUILayout.Label("Smart Slice Sheet", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        sheet = (Texture2D)EditorGUILayout.ObjectField("Transparent Sheet", sheet, typeof(Texture2D), false);
        EditorGUILayout.Space(4);
        minAreaPx = EditorGUILayout.IntField("Min Object Size (px²)", minAreaPx);
        alsoPrefabs = EditorGUILayout.Toggle("Also create prefabs", alsoPrefabs);

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Use the *_Transparent.png from step 1.\n" +
            "Slice → verify in Sprite Editor → then create prefabs.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        GUI.enabled = sheet != null;
        if (GUILayout.Button("Slice Sheet", GUILayout.Height(32)))
            Slice();
        if (GUILayout.Button("Create Prefabs From Slices", GUILayout.Height(26)))
            CreatePrefabs();
        GUI.enabled = true;
    }

    // ── Slicing ──────────────────────────────────────────────────────────────────
    private void Slice()
    {
        string path = AssetDatabase.GetAssetPath(sheet);
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            EditorUtility.DisplayProgressBar("Smart Slice", "Reading pixels…", 0.1f);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.isReadable        = true;
            imp.spriteImportMode  = SpriteImportMode.Multiple;
            imp.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int W = tex.width, H = tex.height;
            Color32[] px = tex.GetPixels32();   // index 0 = bottom-left

            var metas    = new List<SpriteMetaData>();
            var counters = new Dictionary<string, int>();

            int sIndex = 0;
            foreach (var sec in Sections)
            {
                EditorUtility.DisplayProgressBar("Smart Slice",
                    $"Section {sec.cat}…", 0.2f + 0.7f * (sIndex++ / (float)Sections.Length));

                var boxes = sec.mode == Mode.Strip
                    ? StripBoxes(sec)
                    : BlobBoxes(sec, px, W, H);

                foreach (var b in boxes)   // b = TOP-LEFT pixel rect
                {
                    // Clamp strictly inside the texture (x>=0, y>=0, x+w<=W, y+h<=H)
                    int bx = Mathf.Clamp(b.x, 0, W - 1);
                    int by = Mathf.Clamp(b.y, 0, H - 1);
                    int bw = Mathf.Clamp(b.width,  1, W - bx);
                    int bh = Mathf.Clamp(b.height, 1, H - by);
                    if (bw < 2 || bh < 2) continue;   // drop degenerate slivers

                    float unityY = Mathf.Clamp(H - by - bh, 0, H - bh);   // flip to bottom-left

                    if (!counters.ContainsKey(sec.cat)) counters[sec.cat] = 0;
                    int idx = counters[sec.cat]++;

                    metas.Add(new SpriteMetaData
                    {
                        name      = $"{sec.cat}_{idx:D2}",
                        rect      = new Rect(bx, unityY, bw, bh),
                        pivot     = new Vector2(0.5f, 0.5f),
                        alignment = (int)SpriteAlignment.Center,
                    });
                }
            }

            ApplyRects(imp, metas);   // Unity 6 supported path — fully replaces old slices
            AssetDatabase.Refresh();

            Debug.Log($"[SmartSlicer] Sliced {metas.Count} sprites.");
            if (alsoPrefabs) CreatePrefabs();
            else EditorUtility.DisplayDialog("Sliced",
                $"Created {metas.Count} slices.\nOpen Sprite Editor to verify, then 'Create Prefabs'.", "OK");
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    // ── Apply rects via Unity 6 data provider (replaces ALL old slices) ──────────
    private static void ApplyRects(TextureImporter imp, List<SpriteMetaData> metas)
    {
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(imp);
        provider.InitSpriteEditorDataProvider();

        var rects = metas.Select(m => new SpriteRect
        {
            name      = m.name,
            rect      = m.rect,
            pivot     = new Vector2(0.5f, 0.5f),
            alignment = SpriteAlignment.Center,
            border    = Vector4.zero,
            spriteID  = GUID.Generate(),
        }).ToArray();

        provider.SetSpriteRects(rects);   // overwrites the old list entirely

        // Keep the name<->id table in sync (Unity 2021+ sprite naming)
        var nameIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIdProvider != null)
        {
            var pairs = rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList();
            nameIdProvider.SetNameFileIdPairs(pairs);
        }

        provider.Apply();
        ((AssetImporter)provider.targetObject).SaveAndReimport();
    }

    // ── Strip mode (backgrounds) ─────────────────────────────────────────────────
    private static List<RectInt> StripBoxes(Section sec)
    {
        var list = new List<RectInt>();
        int sh = sec.region.height / Mathf.Max(1, sec.strips);
        for (int s = 0; s < sec.strips; s++)
            list.Add(new RectInt(sec.region.x, sec.region.y + s * sh, sec.region.width, sh));
        return list;
    }

    // ── Blob mode (objects) ──────────────────────────────────────────────────────
    private List<RectInt> BlobBoxes(Section sec, Color32[] px, int W, int H)
    {
        int rx = sec.region.x, ry = sec.region.y, rw = sec.region.width, rh = sec.region.height;

        // Build alpha mask for the region (region-local, top-left origin)
        bool[] mask = new bool[rw * rh];
        for (int y = 0; y < rh; y++)
        for (int x = 0; x < rw; x++)
        {
            int ix = rx + x, iytl = ry + y;
            if (ix < 0 || ix >= W || iytl < 0 || iytl >= H) continue;
            int arr = (H - 1 - iytl) * W + ix;
            mask[y * rw + x] = px[arr].a > alphaCut;
        }

        // Morphological closing to bridge JPEG-gap cracks
        bool[] closed = Close(mask, rw, rh, sec.closeR);

        // Connected components (BFS, 8-neighbour)
        var boxes   = new List<RectInt>();
        bool[] seen = new bool[rw * rh];
        var stack   = new Stack<int>();

        for (int start = 0; start < closed.Length; start++)
        {
            if (!closed[start] || seen[start]) continue;

            int minX = rw, minY = rh, maxX = 0, maxY = 0, count = 0;
            seen[start] = true; stack.Push(start);

            while (stack.Count > 0)
            {
                int i = stack.Pop();
                int x = i % rw, y = i / rw;
                count++;
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;

                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= rw || ny < 0 || ny >= rh) continue;
                    int ni = ny * rw + nx;
                    if (closed[ni] && !seen[ni]) { seen[ni] = true; stack.Push(ni); }
                }
            }

            int bw = maxX - minX + 1, bh = maxY - minY + 1;
            int area = bw * bh;
            if (area < minAreaPx) continue;

            // Text filter: wide + short + sparse  =>  header word, skip.
            float aspect = bw / (float)bh;
            float fill   = count / (float)area;
            if (aspect > 3.5f && bh < 0.035f * H && fill < 0.30f) continue;

            boxes.Add(new RectInt(rx + minX, ry + minY, bw, bh));
        }
        return boxes;
    }

    // ── Separable morphology ─────────────────────────────────────────────────────
    private static bool[] Close(bool[] m, int W, int H, int r)
        => r <= 0 ? (bool[])m.Clone() : Erode(Dilate(m, W, H, r), W, H, r);

    private static bool[] Dilate(bool[] m, int W, int H, int r) => Morph(m, W, H, r, true);
    private static bool[] Erode (bool[] m, int W, int H, int r) => Morph(m, W, H, r, false);

    private static bool[] Morph(bool[] m, int W, int H, int r, bool dilate)
    {
        bool[] tmp = new bool[W * H];
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            bool acc = !dilate;
            for (int k = -r; k <= r; k++)
            {
                int xx = x + k; if (xx < 0 || xx >= W) continue;
                bool v = m[y * W + xx];
                if (dilate) { if (v) { acc = true; break; } }
                else        { if (!v) { acc = false; break; } }
            }
            tmp[y * W + x] = acc;
        }
        bool[] outp = new bool[W * H];
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            bool acc = !dilate;
            for (int k = -r; k <= r; k++)
            {
                int yy = y + k; if (yy < 0 || yy >= H) continue;
                bool v = tmp[yy * W + x];
                if (dilate) { if (v) { acc = true; break; } }
                else        { if (!v) { acc = false; break; } }
            }
            outp[y * W + x] = acc;
        }
        return outp;
    }

    // ── Prefab generation ────────────────────────────────────────────────────────
    private void CreatePrefabs()
    {
        string path = AssetDatabase.GetAssetPath(sheet);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("No slices", "Slice the sheet first.", "OK");
            return;
        }

        EnsureSortingLayers();
        EnsureDirs();

        int made = 0;
        foreach (var sprite in sprites)
        {
            string cat = LayerMap.Keys.FirstOrDefault(k => sprite.name.StartsWith(k + "_"));
            if (cat == null) continue;

            string prefabPath = $"{ROOT}/{cat}/{sprite.name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) continue;

            var (layer, order) = LayerMap[cat];
            var go = new GameObject(sprite.name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingLayerName = layer; sr.sortingOrder = order;

            switch (cat)
            {
                case "Tile":
                case "Architecture":
                    var rb = go.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Static;
                    go.AddComponent<CompositeCollider2D>();
                    go.GetComponent<BoxCollider2D>().usedByComposite = true;
                    break;
                case "Platform":
                    var pb = go.AddComponent<BoxCollider2D>();
                    pb.size   = new Vector2(sprite.bounds.size.x, 0.1f);
                    pb.offset = new Vector2(0, -sprite.bounds.extents.y + 0.05f);
                    break;
                case "Interactable":
                    go.AddComponent<BoxCollider2D>().isTrigger = true;
                    break;
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);
            made++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Prefabs", $"Created {made} prefab(s) under {ROOT}.", "OK");
    }

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
