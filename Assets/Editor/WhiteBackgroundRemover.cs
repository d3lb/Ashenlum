using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

// Turns the white background of the asset sheet transparent.
// - Border flood-fill: only white connected to the image edge is erased,
//   so bright INTERIOR details (light beams, glass, crystals) survive.
// - Preserve rects: the background-panel area is kept fully opaque so the
//   misty/bright backgrounds are not eaten.
// - Multi-pass de-fringe: removes the light anti-aliased halo around shapes.
public class WhiteBackgroundRemover : EditorWindow
{
    private Texture2D source;
    [Range(0.80f, 1.00f)] private float whiteThreshold = 0.95f;
    [Range(0, 6)]         private int   defringePasses = 3;
    private bool eraseJunkZones = true;

    // Areas to keep fully opaque (the 3 stacked Background panels), in TOP-LEFT
    // normalized coords [x, y, w, h]. Tweak if your backgrounds sit elsewhere.
    private static readonly Rect[] PreserveTopLeft =
    {
        new Rect(0.004f, 0.066f, 0.40f, 0.45f),
    };

    // Junk to wipe so the slicer never sees it (titles, section labels, palette,
    // scale-reference). Normalized TOP-LEFT [x, y, w, h], measured from this sheet
    // template. If a future sheet shifts the layout, nudge these numbers.
    private static readonly Rect[] EraseTopLeft =
    {
        new Rect(0.00f, 0.000f, 1.00f, 0.050f),  // title + subtitle + "1. BACKGROUNDS"
        new Rect(0.41f, 0.046f, 0.59f, 0.034f),  // "2. TILES" + "3. PLATFORMS"
        new Rect(0.66f, 0.268f, 0.20f, 0.032f),  // "4. ARCHITECTURE / RUINS"
        new Rect(0.00f, 0.546f, 1.00f, 0.032f),  // "5. NATURAL" + "6. INTERACTABLES" + "7. DETAILS"
        new Rect(0.00f, 0.872f, 0.48f, 0.128f),  // "8. COLOR PALETTE" + swatches
        new Rect(0.65f, 0.872f, 0.31f, 0.128f),  // "9. SCALE REFERENCE" + raven + bar
    };

    [MenuItem("Ashenlum/1. Remove White Background")]
    public static void Open()
    {
        var w = GetWindow<WhiteBackgroundRemover>("White BG Remover");
        w.minSize = new Vector2(360, 250);
    }

    private void OnGUI()
    {
        GUILayout.Label("Remove White Background", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        source = (Texture2D)EditorGUILayout.ObjectField("Source Sheet", source, typeof(Texture2D), false);

        EditorGUILayout.Space(4);
        whiteThreshold = EditorGUILayout.Slider("White Threshold", whiteThreshold, 0.80f, 1.00f);
        defringePasses = EditorGUILayout.IntSlider("Halo Cleanup Passes", defringePasses, 0, 6);
        eraseJunkZones = EditorGUILayout.Toggle("Erase Text/Palette Zones", eraseJunkZones);

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Outputs <name>_Transparent.png next to the source.\n" +
            "Background panels are preserved opaque. Raise Halo Cleanup if white\n" +
            "fringe remains around webs/chains; lower it if thin art gets eaten.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        GUI.enabled = source != null;
        if (GUILayout.Button("Remove White Background", GUILayout.Height(34)))
            Process();
        GUI.enabled = true;
    }

    private void Process()
    {
        string srcPath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(srcPath))
        {
            EditorUtility.DisplayDialog("Error", "Texture must be inside the project Assets folder.", "OK");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Remove White BG", "Reading pixels…", 0.1f);

            var imp = (TextureImporter)AssetImporter.GetAtPath(srcPath);
            bool prevReadable    = imp.isReadable;
            var  prevCompression = imp.textureCompression;
            imp.isReadable         = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            int w = tex.width, h = tex.height;
            Color32[] px = tex.GetPixels32();   // index 0 = BOTTOM-left

            byte th = (byte)Mathf.RoundToInt(whiteThreshold * 255f);

            // Precompute preserve mask (TOP-LEFT rects → array coords)
            bool[] preserve = new bool[w * h];
            foreach (var r in PreserveTopLeft)
            {
                int rx = Mathf.RoundToInt(r.x * w), rw = Mathf.RoundToInt(r.width  * w);
                int ry = Mathf.RoundToInt(r.y * h), rh = Mathf.RoundToInt(r.height * h);
                for (int ytl = ry; ytl < ry + rh; ytl++)
                for (int x = rx; x < rx + rw; x++)
                {
                    if (x < 0 || x >= w || ytl < 0 || ytl >= h) continue;
                    int yb = h - 1 - ytl;
                    preserve[yb * w + x] = true;
                }
            }

            EditorUtility.DisplayProgressBar("Remove White BG", "Flood-filling background…", 0.4f);

            bool[] isBg  = new bool[w * h];
            var    queue = new Queue<int>();

            bool IsWhite(int i) => px[i].r >= th && px[i].g >= th && px[i].b >= th;
            void TrySeed(int i)
            {
                if (!isBg[i] && !preserve[i] && IsWhite(i)) { isBg[i] = true; queue.Enqueue(i); }
            }

            for (int x = 0; x < w; x++) { TrySeed(x); TrySeed((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { TrySeed(y * w); TrySeed(y * w + (w - 1)); }

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                int x = i % w, y = i / w;
                if (x > 0)     TrySeed(i - 1);
                if (x < w - 1) TrySeed(i + 1);
                if (y > 0)     TrySeed(i - w);
                if (y < h - 1) TrySeed(i + w);
            }

            for (int i = 0; i < px.Length; i++)
                if (isBg[i]) px[i].a = 0;

            EditorUtility.DisplayProgressBar("Remove White BG", "Cleaning halos…", 0.7f);
            for (int p = 0; p < defringePasses; p++)
                DefringePass(px, preserve, w, h);

            // Wipe template junk (titles / section labels / palette / scale ref)
            if (eraseJunkZones)
            {
                foreach (var r in EraseTopLeft)
                {
                    int rx = Mathf.RoundToInt(r.x * w), rw = Mathf.RoundToInt(r.width  * w);
                    int ry = Mathf.RoundToInt(r.y * h), rh = Mathf.RoundToInt(r.height * h);
                    for (int ytl = ry; ytl < ry + rh; ytl++)
                    for (int x = rx; x < rx + rw; x++)
                    {
                        if (x < 0 || x >= w || ytl < 0 || ytl >= h) continue;
                        int yb = h - 1 - ytl;          // top-left -> bottom-left array index
                        px[yb * w + x].a = 0;
                    }
                }
            }

            EditorUtility.DisplayProgressBar("Remove White BG", "Writing PNG…", 0.9f);

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels32(px);
            outTex.Apply();

            string dir     = Path.GetDirectoryName(srcPath).Replace('\\', '/');
            string name    = Path.GetFileNameWithoutExtension(srcPath);
            string outPath = $"{dir}/{name}_Transparent.png";
            File.WriteAllBytes(outPath, outTex.EncodeToPNG());
            DestroyImmediate(outTex);

            imp.isReadable         = prevReadable;
            imp.textureCompression = prevCompression;
            imp.SaveAndReimport();

            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
            var outImp = (TextureImporter)AssetImporter.GetAtPath(outPath);
            outImp.textureType         = TextureImporterType.Sprite;
            outImp.spriteImportMode    = SpriteImportMode.Multiple;
            outImp.spritePixelsPerUnit = 64;
            outImp.filterMode          = FilterMode.Point;
            outImp.alphaIsTransparency = true;
            outImp.textureCompression  = TextureImporterCompression.Uncompressed;
            outImp.SaveAndReimport();

            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
            EditorGUIUtility.PingObject(Selection.activeObject);

            EditorUtility.DisplayDialog("Done",
                $"Created:\n{outPath}\n\nNext: Ashenlum → 2. Smart Slice Sheet.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // Clears light pixels that touch a transparent pixel — dissolves the white halo.
    private static void DefringePass(Color32[] px, bool[] preserve, int w, int h)
    {
        var clear = new List<int>();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (px[i].a == 0 || preserve[i]) continue;
            bool touchesClear =
                (x > 0     && px[i - 1].a == 0) ||
                (x < w - 1 && px[i + 1].a == 0) ||
                (y > 0     && px[i - w].a == 0) ||
                (y < h - 1 && px[i + w].a == 0);
            if (touchesClear && px[i].r > 190 && px[i].g > 190 && px[i].b > 190)
                clear.Add(i);
        }
        foreach (int i in clear) px[i].a = 0;
    }
}
