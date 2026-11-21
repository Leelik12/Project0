using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Шрифт интерфейса: буквы берутся из TechnoDeath UI SDF, цифры и знаки препинания (включая знак эдди) из Symbols,
/// старые шрифты больше нигде не подключены, а все тексты карточек отрисовываются без пустых квадратов.
/// </summary>
public class UiFontTests
{
    private const string UiPath = "Assets/_Project/Resources/Fonts/TechnoDeath UI SDF.asset";
    private const string SymbolsPath = "Assets/_Project/Resources/Fonts/TechnoDeath Symbols SDF.asset";
    private const string ScenesFolder = "Assets/_Project/Scenes";
    private const string DataFolder = "Assets/_Project/Data";
    private const string Punctuation = ".,:;-+%/!?()«»—€";

    // Известное исключение: математический минус в описании Imp_Armor рисуется запасным LiberationSans (см. отчёт тестировщика)

    private static readonly string[] OldFontGuids =
    {
        "f9f93a1497c50c34a8ede47696015182", // Cyberpunk_RUS_BY_LYAJKA SDF
        "f675742eaf9c20a4f909d78ee7a14fed", // Inter-Regular SDF
    };

    private TMP_FontAsset ui;
    private TMP_FontAsset symbols;

    [SetUp]
    public void SetUp()
    {
        ui = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiPath);
        symbols = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SymbolsPath);
        Assert.IsNotNull(ui, "Нет шрифта интерфейса " + UiPath);
        Assert.IsNotNull(symbols, "Нет шрифта символов " + SymbolsPath);
    }

    // Символ, найденный по всей цепочке: сам шрифт, затем его запасные
    private static TMP_Character Resolve(TMP_FontAsset font, char ch)
    {
        return TMP_FontAssetUtilities.GetCharacterFromFontAsset(ch, font, true, FontStyles.Normal, FontWeight.Regular, out _);
    }

    [Test]
    public void Digits_And_Punctuation_Resolve_To_Symbols_Font()
    {
        var bad = new List<string>();
        foreach (char ch in "0123456789" + Punctuation)
        {
            if (!symbols.characterLookupTable.ContainsKey(ch)) { bad.Add($"'{ch}' нет в Symbols"); continue; }
            if (ui.characterLookupTable.ContainsKey(ch)) { bad.Add($"'{ch}' лежит и в основном шрифте"); continue; }
            TMP_Character found = Resolve(ui, ch);
            if (found == null || found.textAsset != symbols) bad.Add($"'{ch}' идёт не из Symbols ({(found?.textAsset ? found.textAsset.name : "не найден")})");
        }
        Assert.IsEmpty(bad, string.Join("\n", bad));
    }

    [Test]
    public void Cyrillic_Letters_Resolve_In_Ui_Font_Itself()
    {
        var missing = new List<char>();
        for (char ch = 'А'; ch <= 'я'; ch++)
            if (!ui.characterLookupTable.ContainsKey(ch)) missing.Add(ch);
        foreach (char ch in "Ёё")
            if (!ui.characterLookupTable.ContainsKey(ch)) missing.Add(ch);
        Assert.IsEmpty(missing, "Нет в основном шрифте: " + string.Join(" ", missing));
    }

    [Test]
    public void Symbols_Atlas_Is_Not_Empty_And_Glyphs_Lie_Inside()
    {
        Assert.IsNotNull(symbols.atlasTextures);
        Assert.Greater(symbols.atlasTextures.Length, 0, "Нет текстуры атласа");
        Assert.IsNotNull(symbols.atlasTextures[0], "Текстура атласа Symbols пустая");
        Assert.Greater(symbols.glyphTable.Count, 0, "Нет глифов");
        int w = symbols.atlasWidth, h = symbols.atlasHeight;
        Assert.Greater(w, 0);
        Assert.Greater(h, 0);
        foreach (var glyph in symbols.glyphTable)
        {
            var r = glyph.glyphRect;
            Assert.IsTrue(r.x >= 0 && r.y >= 0 && r.width >= 0 && r.height >= 0
                          && r.x + r.width <= w && r.y + r.height <= h,
                $"Глиф {glyph.index} ({r.x},{r.y},{r.width},{r.height}) выходит за атлас {w}x{h}");
        }
    }

    [Test]
    public void Scenes_Do_Not_Reference_Old_Fonts()
    {
        string[] scenes = Directory.GetFiles(ScenesFolder, "*.unity");
        Assert.IsNotEmpty(scenes);
        var bad = new List<string>();
        foreach (string path in scenes)
        {
            string text = File.ReadAllText(path);
            foreach (string guid in OldFontGuids)
                if (text.Contains(guid)) bad.Add($"{Path.GetFileName(path)}: {guid}");
        }
        Assert.IsEmpty(bad, string.Join("\n", bad));
    }

    // Все строки, которые игра строит из карточек и редкостей
    private static IEnumerable<(string source, string text)> CardTexts()
    {
        var catalog = Resources.Load<UpgradeCatalog>("UpgradeCatalog");
        Assert.IsNotNull(catalog, "Нет Resources/UpgradeCatalog");
        foreach (var tier in catalog.rarities) yield return ("редкость", tier.name);
        yield return ("кнопка", UiText.Widen("Куплено"));
        yield return ("кнопка", UiText.Widen("Взять"));
        yield return ("цена", UiText.Widen("1234567890 €"));

        var cards = new HashSet<UpgradeCardData>(catalog.cards.Where(c => c != null));
        foreach (string guid in AssetDatabase.FindAssets("t:UpgradeCardData", new[] { DataFolder }))
            cards.Add(AssetDatabase.LoadAssetAtPath<UpgradeCardData>(AssetDatabase.GUIDToAssetPath(guid)));

        foreach (var card in cards)
        {
            yield return ("заголовок " + card.name, UiText.Widen(card.title));
            foreach (float power in new[] { 1f, 1.5f, 2f })
                yield return ($"описание {card.name} x{power}", UiText.Widen(card.FormatDescription(power)));
        }
    }

    [Test]
    public void Card_Texts_Render_Without_Missing_Glyphs()
    {
        var texts = CardTexts().ToList();
        Assert.Greater(texts.Count, 10, "Подозрительно мало текстов");
        var missing = new SortedSet<string>();
        foreach (var (source, text) in texts)
        {
            // Теги разметки (<b>, <color=...>) не рисуются
            string plain = Regex.Replace(text ?? "", "<[^>]*>", "");
            foreach (char ch in plain)
            {
                if (ch == ' ' || ch == '\n' || ch == '\r' || ch == '\t') continue;
                TMP_Character found = Resolve(ui, ch);
                if (found == null) missing.Add($"U+{(int)ch:X4} '{ch}' в: {source}");
            }
        }
        Assert.IsEmpty(missing, string.Join("\n", missing));
    }

    [Test]
    public void Card_Texts_Do_Not_Fall_Through_To_Last_Resort_Font()
    {
        var fallen = new SortedSet<string>();
        foreach (var (source, text) in CardTexts())
        {
            string plain = Regex.Replace(text ?? "", "<[^>]*>", "");
            foreach (char ch in plain)
            {
                if (char.IsWhiteSpace(ch)) continue;
                TMP_Character found = Resolve(ui, ch);
                if (found != null && found.textAsset != ui && found.textAsset != symbols)
                    fallen.Add($"'{ch}' (U+{(int)ch:X4}) из {found.textAsset.name} в: {source}");
            }
        }
        Assert.IsEmpty(fallen, string.Join("\n", fallen));
    }
}
