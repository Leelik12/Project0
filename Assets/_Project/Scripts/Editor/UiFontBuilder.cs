using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// Собирает шрифт интерфейса из кибершрифта (Cyberpunk_RUS_BY_LYAJKA SDF):
/// буквы сжимаются по ширине, чтобы надписи помещались в кнопки и карточки,
/// а цифры и знаки рисуются заново угловатыми штрихами в стиле шрифта (в исходнике их нет или они обычные).
/// Результат — два ассета в Resources/Fonts: основной шрифт и подшрифт цифр и знаков (fallback).
/// Пересборка не меняет GUID, сцены ссылаются на основной шрифт.
/// </summary>
public static class UiFontBuilder
{
    private const string SourcePath = "Assets/ThirdParty/ArtDesireBoutique/TextMesh Pro/Resources/Fonts & Materials/Cyberpunk_RUS_BY_LYAJKA SDF.asset";
    private const string LastResortPath = "Assets/ThirdParty/ArtDesireBoutique/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    public const string UiFontPath = "Assets/_Project/Resources/Fonts/TechnoDeath UI SDF.asset";
    public const string SymbolsPath = "Assets/_Project/Resources/Fonts/TechnoDeath Symbols SDF.asset";
    public const string UiFontResource = "Fonts/TechnoDeath UI SDF";

    /// <summary>Во сколько раз буквы уже исходных.</summary>
    public const float Condense = 0.8f;

    private const int Padding = 5;
    private const int AtlasSize = 512;
    private const float StrokeWidth = 0.13f; // толщина штриха в долях высоты цифры
    private const float Slant = 0.18f;       // наклон, как у кистевых букв
    private const float SideBearing = 0.07f; // поля слева и справа в долях высоты
    private const float TipLength = 1.4f;    // длина острия на концах штрихов в толщинах штриха

    [MenuItem("TechnoDeath/Build UI Fonts")]
    public static void Build()
    {
        var source = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SourcePath);
        var lastResort = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LastResortPath);
        if (source == null) { Debug.LogError("UiFontBuilder: нет исходного шрифта " + SourcePath); return; }

        System.IO.Directory.CreateDirectory("Assets/_Project/Resources/Fonts");
        TMP_FontAsset symbols = BuildSymbols(source);
        TMP_FontAsset ui = BuildCondensed(source, symbols, lastResort);

        AssetDatabase.SaveAssets();
        TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, ui);
        Debug.Log($"UiFontBuilder: {ui.characterTable.Count} букв, {symbols.characterTable.Count} цифр и знаков");
    }

    // ---------- Сжатый основной шрифт ----------

    private static TMP_FontAsset BuildCondensed(TMP_FontAsset source, TMP_FontAsset symbols, TMP_FontAsset lastResort)
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontPath) == null)
        {
            AssetDatabase.CopyAsset(SourcePath, UiFontPath); // атлас и материал копируются вместе со шрифтом
            AssetDatabase.ImportAsset(UiFontPath);
        }
        var ui = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontPath);

        var replaced = new HashSet<uint>();
        foreach (TMP_Character c in symbols.characterTable) replaced.Add(c.unicode);

        var glyphs = new List<Glyph>();
        var byIndex = new Dictionary<uint, Glyph>();
        foreach (Glyph g in source.glyphTable)
        {
            GlyphMetrics m = g.metrics;
            var condensed = new GlyphMetrics(m.width * Condense, m.height, m.horizontalBearingX * Condense, m.horizontalBearingY, m.horizontalAdvance * Condense);
            var copy = new Glyph(g.index, condensed, g.glyphRect, g.scale, g.atlasIndex);
            glyphs.Add(copy);
            byIndex[g.index] = copy;
        }

        var characters = new List<TMP_Character>();
        foreach (TMP_Character c in source.characterTable)
        {
            if (replaced.Contains(c.unicode)) continue; // цифры берутся из подшрифта
            characters.Add(new TMP_Character(c.unicode, ui, byIndex[c.glyphIndex]));
        }

        Set(ui, "m_GlyphTable", glyphs);
        Set(ui, "m_CharacterTable", characters);
        // Свои цифры первыми, затем запасные шрифты исходника и последний — LiberationSans
        var fallbacks = new List<TMP_FontAsset> { symbols };
        if (source.fallbackFontAssetTable != null) foreach (TMP_FontAsset f in source.fallbackFontAssetTable) if (f != null && !fallbacks.Contains(f)) fallbacks.Add(f);
        if (lastResort != null && !fallbacks.Contains(lastResort)) fallbacks.Add(lastResort);
        ui.fallbackFontAssetTable = fallbacks;
        ui.ReadFontAssetDefinition();
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ---------- Цифры и знаки ----------

    private sealed class GlyphShape
    {
        public uint Unicode;
        public float WidthFactor;       // ширина рисунка в долях высоты
        public readonly List<Vector2[]> Polygons = new(); // выпуклые многоугольники в пикселях, до наклона
    }

    private static TMP_FontAsset BuildSymbols(TMP_FontAsset source)
    {
        FaceInfo face = source.faceInfo;
        float height = CapHeight(source);

        var shapes = new List<GlyphShape>();
        foreach ((char ch, float width, string[] strokes) in Designs())
        {
            shapes.Add(Shape(ch, width, height, strokes));
        }

        var symbols = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SymbolsPath);
        if (symbols == null)
        {
            symbols = ScriptableObject.CreateInstance<TMP_FontAsset>();
            symbols.name = "TechnoDeath Symbols SDF";
            AssetDatabase.CreateAsset(symbols, SymbolsPath);
        }

        Texture2D atlas = SubAsset<Texture2D>(symbols);
        if (atlas == null)
        {
            atlas = new Texture2D(AtlasSize, AtlasSize, TextureFormat.Alpha8, false);
            AssetDatabase.AddObjectToAsset(atlas, symbols);
        }
        else
        {
            atlas.Reinitialize(AtlasSize, AtlasSize, TextureFormat.Alpha8, false);
        }
        atlas.name = "TechnoDeath Symbols Atlas";
        atlas.filterMode = FilterMode.Bilinear;
        atlas.wrapMode = TextureWrapMode.Clamp;

        var pixels = new byte[AtlasSize * AtlasSize];
        var glyphs = new List<Glyph>();
        var characters = new List<TMP_Character>();
        int penX = 0, penY = 0, rowHeight = 0;
        uint index = 1;

        foreach (GlyphShape shape in shapes)
        {
            Bounds(shape, out float xMin, out float yMin, out float xMax, out float yMax);
            int left = Mathf.FloorToInt(xMin), bottom = Mathf.FloorToInt(yMin);
            int w = Mathf.CeilToInt(xMax) - left, h = Mathf.CeilToInt(yMax) - bottom;
            int cellW = w + Padding * 2, cellH = h + Padding * 2;
            if (penX + cellW > AtlasSize) { penX = 0; penY += rowHeight; rowHeight = 0; }
            if (penY + cellH > AtlasSize) { Debug.LogError("UiFontBuilder: атлас цифр переполнен"); break; }

            for (int y = 0; y < cellH; y++)
            {
                for (int x = 0; x < cellW; x++)
                {
                    var p = new Vector2(left - Padding + x + 0.5f, bottom - Padding + y + 0.5f);
                    float sd = SignedDistance(shape, p);
                    float a = Mathf.Clamp01(0.5f + sd / (2f * (Padding + 1)));
                    pixels[(penY + y) * AtlasSize + penX + x] = (byte)Mathf.RoundToInt(a * 255f);
                }
            }

            float bearing = SideBearing * height;
            var metrics = new GlyphMetrics(w, h, bearing, bottom + h, w + bearing * 2f);
            var glyph = new Glyph(index++, metrics, new GlyphRect(penX + Padding, penY + Padding, w, h), 1f, 0);
            glyphs.Add(glyph);
            characters.Add(new TMP_Character(shape.Unicode, symbols, glyph));

            penX += cellW;
            rowHeight = Mathf.Max(rowHeight, cellH);
        }

        atlas.SetPixelData(pixels, 0);
        atlas.Apply(false, false);

        Material material = SubAsset<Material>(symbols);
        if (material == null)
        {
            material = new Material(source.material);
            AssetDatabase.AddObjectToAsset(material, symbols);
        }
        else
        {
            material.CopyPropertiesFromMaterial(source.material);
        }
        material.name = "TechnoDeath Symbols SDF Material";
        material.SetTexture("_MainTex", atlas);
        material.SetFloat("_TextureWidth", AtlasSize);
        material.SetFloat("_TextureHeight", AtlasSize);
        material.SetFloat("_GradientScale", Padding + 1);

        Set(symbols, "m_Version", "1.1.0");
        symbols.faceInfo = face;
        Set(symbols, "m_AtlasWidth", AtlasSize);
        Set(symbols, "m_AtlasHeight", AtlasSize);
        Set(symbols, "m_AtlasPadding", Padding);
        Set(symbols, "m_AtlasRenderMode", source.atlasRenderMode);
        Set(symbols, "m_AtlasPopulationMode", AtlasPopulationMode.Static);
        symbols.atlasTextures = new[] { atlas };
        symbols.material = material;
        Set(symbols, "m_GlyphTable", glyphs);
        Set(symbols, "m_CharacterTable", characters);
        symbols.ReadFontAssetDefinition();

        EditorUtility.SetDirty(atlas);
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(symbols);
        return symbols;
    }

    // Высота прописных букв исходного шрифта: по ней подгоняются цифры
    private static float CapHeight(TMP_FontAsset source)
    {
        if (source.characterLookupTable.TryGetValue('H', out TMP_Character h)) return h.glyph.metrics.height * 0.84f;
        return source.faceInfo.capLine > 0 ? source.faceInfo.capLine : source.faceInfo.ascentLine * 0.8f;
    }

    /// <summary>
    /// Рисунки знаков: координаты в долях (x — от ширины знака, y — от высоты цифры), штрихи через пробел.
    /// «z» в конце — замкнутый контур, «d x,y [размер]» — квадратная точка.
    /// </summary>
    private static IEnumerable<(char, float, string[])> Designs()
    {
        const float digit = 0.58f;
        yield return ('0', digit, new[] { "0.22,0 0.78,0 1,0.16 1,0.84 0.78,1 0.22,1 0,0.84 0,0.16 z" });
        yield return ('1', 0.42f, new[] { "0,0.74 0.72,1 0.72,0" });
        yield return ('2', digit, new[] { "0,0.82 0.2,1 0.8,1 1,0.84 1,0.62 0,0.16 0,0 1,0" });
        yield return ('3', digit, new[] { "0,1 1,1 0.4,0.58 0.8,0.58 1,0.42 1,0.16 0.8,0 0,0" });
        yield return ('4', digit, new[] { "0.74,0 0.74,1 0,0.3 1,0.3" });
        yield return ('5', digit, new[] { "1,1 0,1 0,0.56 0.8,0.56 1,0.4 1,0.16 0.8,0 0,0" });
        yield return ('6', digit, new[] { "0.92,1 0.2,1 0,0.84 0,0.16 0.2,0 0.8,0 1,0.16 1,0.42 0.8,0.58 0,0.58" });
        yield return ('7', digit, new[] { "0,1 1,1 1,0.82 0.36,0" });
        yield return ('8', digit, new[] {
            "0.22,0.56 0.06,0.7 0.06,0.86 0.24,1 0.76,1 0.94,0.86 0.94,0.7 0.78,0.56 z",
            "0.22,0.56 0,0.4 0,0.16 0.2,0 0.8,0 1,0.16 1,0.4 0.78,0.56 z" });
        yield return ('9', digit, new[] { "0.08,0 0.8,0 1,0.16 1,0.84 0.8,1 0.2,1 0,0.84 0,0.58 0.2,0.42 1,0.42" });

        yield return ('.', 0.16f, new[] { "d 0.5,0.08" });
        yield return (',', 0.22f, new[] { "0.8,0.16 0.2,-0.2" });
        yield return (':', 0.16f, new[] { "d 0.5,0.08", "d 0.5,0.62" });
        yield return (';', 0.22f, new[] { "d 0.6,0.62", "0.8,0.16 0.2,-0.2" });
        yield return ('-', 0.42f, new[] { "0,0.36 1,0.36" });
        yield return ('–', 0.55f, new[] { "0,0.36 1,0.36" });
        yield return ('—', 0.95f, new[] { "0,0.36 1,0.36" });
        yield return ('−', 0.56f, new[] { "0,0.44 1,0.44" }); // математический минус (в описаниях карточек), на высоте «+»
        yield return ('_', 0.6f, new[] { "0,-0.12 1,-0.12" });
        yield return ('+', 0.56f, new[] { "0,0.44 1,0.44", "0.5,0.1 0.5,0.78" });
        yield return ('=', 0.56f, new[] { "0,0.3 1,0.3", "0,0.6 1,0.6" });
        yield return ('×', 0.5f, new[] { "0,0.16 1,0.74", "0,0.74 1,0.16" });
        yield return ('*', 0.5f, new[] { "0.5,1 0.5,0.42", "0,0.86 1,0.56", "0,0.56 1,0.86" });
        yield return ('/', 0.42f, new[] { "0,-0.04 1,1.04" });
        yield return ('\\', 0.42f, new[] { "0,1.04 1,-0.04" });
        yield return ('|', 0.16f, new[] { "0.5,1.12 0.5,-0.16" });
        yield return ('%', 0.78f, new[] { "0,0 1,1", "d 0.12,0.86 1.5", "d 0.88,0.14 1.5" });
        yield return ('!', 0.16f, new[] { "0.5,1 0.5,0.3", "d 0.5,0.08" });
        yield return ('?', 0.55f, new[] { "0,0.82 0.2,1 0.8,1 1,0.84 1,0.64 0.5,0.44 0.5,0.3", "d 0.5,0.08" });
        yield return ('(', 0.28f, new[] { "1,1.12 0,0.76 0,0.2 1,-0.16" });
        yield return (')', 0.28f, new[] { "0,1.12 1,0.76 1,0.2 0,-0.16" });
        yield return ('[', 0.3f, new[] { "1,1.12 0,1.12 0,-0.16 1,-0.16" });
        yield return (']', 0.3f, new[] { "0,1.12 1,1.12 1,-0.16 0,-0.16" });
        yield return ('<', 0.5f, new[] { "1,0.8 0,0.44 1,0.08" });
        yield return ('>', 0.5f, new[] { "0,0.8 1,0.44 0,0.08" });
        yield return ('«', 0.68f, new[] { "0.44,0.74 0,0.44 0.44,0.14", "1,0.74 0.56,0.44 1,0.14" });
        yield return ('»', 0.68f, new[] { "0,0.74 0.44,0.44 0,0.14", "0.56,0.74 1,0.44 0.56,0.14" });
        yield return ('"', 0.36f, new[] { "0.1,1.02 0.1,0.72", "0.9,1.02 0.9,0.72" });
        yield return ('\'', 0.16f, new[] { "0.5,1.02 0.5,0.72" });
        yield return ('#', 0.7f, new[] { "0.28,0 0.42,1", "0.58,0 0.72,1", "0,0.32 1,0.32", "0,0.68 1,0.68" });
        yield return ('…', 0.8f, new[] { "d 0.08,0.08", "d 0.5,0.08", "d 0.92,0.08" });
        // Знак эдди — угловатое «€» с двумя перекладинами (UpgradeCardView.CurrencySuffix)
        yield return ('€', 0.62f, new[] { "1,0.9 0.84,1 0.24,1 0.04,0.84 0.04,0.16 0.24,0 0.84,0 1,0.1", "-0.14,0.6 0.66,0.6", "-0.14,0.4 0.66,0.4" });
    }

    private static GlyphShape Shape(char ch, float widthFactor, float height, string[] strokes)
    {
        var shape = new GlyphShape { Unicode = ch, WidthFactor = widthFactor };
        float stroke = StrokeWidth * height;
        float width = Mathf.Max(widthFactor * height - stroke, 0f); // ширина по осям штрихов: снаружи добавится полштриха с каждой стороны

        Vector2 ToPixels(string pair)
        {
            string[] xy = pair.Split(',');
            float x = float.Parse(xy[0], CultureInfo.InvariantCulture);
            float y = float.Parse(xy[1], CultureInfo.InvariantCulture);
            var p = new Vector2(stroke * 0.5f + x * width, stroke * 0.5f + y * (height - stroke));
            p.x += p.y * Slant;
            return p;
        }

        foreach (string line in strokes)
        {
            string[] parts = line.Split(' ');
            if (parts[0] == "d")
            {
                float size = stroke * (parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : 1.15f);
                shape.Polygons.Add(Square(ToPixels(parts[1]), size));
                continue;
            }

            bool closed = parts[^1] == "z";
            var points = new List<Vector2>();
            foreach (string part in parts) if (part != "z") points.Add(ToPixels(part));

            int segments = closed ? points.Count : points.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                shape.Polygons.Add(Segment(points[i], points[(i + 1) % points.Count], stroke));
            }
            // Стыки заполняются восьмиугольником: углы остаются угловатыми, но без зазубрин
            int firstJoint = closed ? 0 : 1, lastJoint = closed ? points.Count - 1 : points.Count - 2;
            for (int i = firstJoint; i <= lastJoint; i++) shape.Polygons.Add(Octagon(points[i], stroke * 0.5f));

            // Свободные концы заострены, как росчерки кистевых букв
            if (!closed)
            {
                shape.Polygons.Add(Tip(points[0], points[1], stroke));
                shape.Polygons.Add(Tip(points[^1], points[^2], stroke));
            }
        }
        return shape;
    }

    private static Vector2[] Tip(Vector2 end, Vector2 previous, float width)
    {
        Vector2 dir = (end - previous).normalized;
        Vector2 n = new Vector2(-dir.y, dir.x) * (width * 0.5f);
        return new[] { end - n, end + dir * (width * TipLength), end + n };
    }

    private static Vector2[] Segment(Vector2 a, Vector2 b, float width)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 n = new Vector2(-dir.y, dir.x) * (width * 0.5f);
        return new[] { a - n, b - n, b + n, a + n };
    }

    private static Vector2[] Square(Vector2 c, float size)
    {
        float h = size * 0.5f;
        float skew = h * Slant; // точка наклонена вместе со штрихами
        return new[] { new Vector2(c.x - h - skew, c.y - h), new Vector2(c.x + h - skew, c.y - h), new Vector2(c.x + h + skew, c.y + h), new Vector2(c.x - h + skew, c.y + h) };
    }

    private static Vector2[] Octagon(Vector2 c, float r)
    {
        var points = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            float angle = (i + 0.5f) * Mathf.PI / 4f;
            points[i] = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (r / Mathf.Cos(Mathf.PI / 8f));
        }
        return points;
    }

    private static void Bounds(GlyphShape shape, out float xMin, out float yMin, out float xMax, out float yMax)
    {
        xMin = yMin = float.MaxValue;
        xMax = yMax = float.MinValue;
        foreach (Vector2[] poly in shape.Polygons)
        {
            foreach (Vector2 p in poly)
            {
                xMin = Mathf.Min(xMin, p.x); yMin = Mathf.Min(yMin, p.y);
                xMax = Mathf.Max(xMax, p.x); yMax = Mathf.Max(yMax, p.y);
            }
        }
    }

    // Знаковое расстояние до объединения выпуклых многоугольников: снаружи точное, внутри — по самому «глубокому» из них
    private static float SignedDistance(GlyphShape shape, Vector2 p)
    {
        float outside = float.MaxValue, inside = 0f;
        bool isInside = false;
        foreach (Vector2[] poly in shape.Polygons)
        {
            float edge = EdgeDistance(poly, p);
            if (Contains(poly, p))
            {
                isInside = true;
                inside = Mathf.Max(inside, edge);
            }
            else
            {
                outside = Mathf.Min(outside, edge);
            }
        }
        return isInside ? inside : -outside;
    }

    private static bool Contains(Vector2[] poly, Vector2 p)
    {
        bool? sign = null;
        for (int i = 0; i < poly.Length; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
            float cross = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            if (Mathf.Abs(cross) < 1e-6f) continue;
            bool positive = cross > 0f;
            if (sign == null) sign = positive;
            else if (sign != positive) return false;
        }
        return true;
    }

    private static float EdgeDistance(Vector2[] poly, Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 0; i < poly.Length; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
        }
        return best;
    }

    private static T SubAsset<T>(Object main) where T : Object
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(main)))
        {
            if (o is T typed) return typed;
        }
        return null;
    }

    private static void Set(object target, string field, object value)
    {
        for (System.Type t = target.GetType(); t != null; t = t.BaseType)
        {
            FieldInfo f = t.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f == null) continue;
            f.SetValue(target, value);
            return;
        }
        throw new System.MissingFieldException(target.GetType().Name, field);
    }
}
