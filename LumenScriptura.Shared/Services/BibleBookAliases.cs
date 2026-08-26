namespace LumenScriptura.Services;

public static class BibleBookAliases
{
    private static readonly Dictionary<string, string> NormalizedAliases = new(StringComparer.OrdinalIgnoreCase);

    static BibleBookAliases()
    {
        Register("Genesis", "GEN", "GENESIS", "GN");
        Register("Exodus", "EX", "EXO", "EXOD", "EXODUS");
        Register("Leviticus", "LEV", "LEVIT", "LEVITICUS", "LV");
        Register("Numbers", "NUM", "NUMBERS", "NBRS", "NM");
        Register("Deuteronomy", "DEUT", "DEUTERONOMY", "DT");
        Register("Joshua", "JOSH", "JOSHUA", "JSH");
        Register("Judges", "JUDG", "JUDGES", "JDG");
        Register("Ruth", "RUTH", "RTH", "RU");
        Register("1 Samuel", "1 SAM", "1SAM", "1 SAMUEL", "1SAMUEL", "1 SM", "1SM", "1S");
        Register("2 Samuel", "2 SAM", "2SAM", "2 SAMUEL", "2SAMUEL", "2 SM", "2SM", "2S");
        Register("1 Kings", "1 KGS", "1KGS", "1 KING", "1KING", "1 KINGS", "1KINGS", "1 KI", "1KI", "1K");
        Register("2 Kings", "2 KGS", "2KGS", "2 KING", "2KING", "2 KINGS", "2KINGS", "2 KI", "2KI", "2K");
        Register("1 Chronicles", "1 CHRON", "1CHRON", "1 CHRONICLES", "1CHRONICLES", "1 CHR", "1CHR", "1CH");
        Register("2 Chronicles", "2 CHRON", "2CHRON", "2 CHRONICLES", "2CHRONICLES", "2 CHR", "2CHR", "2CH");
        Register("Ezra", "EZRA", "EZR");
        Register("Nehemiah", "NEH", "NEHEMIAH", "NE");
        Register("Esther", "ESTH", "ESTHER", "EST", "ES");
        Register("Job", "JOB", "JB");
        Register("Psalms", "PS", "PSA", "PSALM", "PSALMS", "PSS", "PSM");
        Register("Proverbs", "PROV", "PROVERBS", "PRV", "PR");
        Register("Ecclesiastes", "ECC", "ECCL", "ECCLESIASTES", "QOH", "KOHELETH", "EC");
        Register("Song of Solomon", "SONG", "SONGS", "SONG OF SOLOMON", "SONG OF SONGS", "CANTICLES", "CANTICLE", "SOS");
        Register("Isaiah", "ISA", "ISAIAH", "IS");
        Register("Jeremiah", "JER", "JEREMIAH", "JR");
        Register("Lamentations", "LAM", "LAMENTATIONS", "LM");
        Register("Ezekiel", "EZEK", "EZEKIEL", "EZE");
        Register("Daniel", "DAN", "DANIEL", "DN");
        Register("Hosea", "HOS", "HOSEA");
        Register("Joel", "JOEL", "JL");
        Register("Amos", "AMOS", "AM");
        Register("Obadiah", "OBAD", "OBADIAH", "OB");
        Register("Jonah", "JONAH", "JON");
        Register("Micah", "MIC", "MICAH");
        Register("Nahum", "NAH", "NAHUM");
        Register("Habakkuk", "HAB", "HABAKKUK");
        Register("Zephaniah", "ZEPH", "ZEPHANIAH", "ZP");
        Register("Haggai", "HAG", "HAGGAI");
        Register("Zechariah", "ZECH", "ZECHARIAH", "ZC");
        Register("Malachi", "MAL", "MALACHI");
        Register("Matthew", "MATT", "MATTHEW", "MT");
        Register("Mark", "MARK", "MRK", "MK");
        Register("Luke", "LUKE", "LUK", "LK");
        Register("John", "JOHN", "JHN", "JN");
        Register("Acts", "ACTS", "ACT", "AC");
        Register("Romans", "ROM", "ROMANS", "RM", "RO");
        Register("1 Corinthians", "1 COR", "1COR", "1 CORINTHIANS", "1CORINTHIANS", "1 CO", "1CO", "1C");
        Register("2 Corinthians", "2 COR", "2COR", "2 CORINTHIANS", "2CORINTHIANS", "2 CO", "2CO", "2C");
        Register("Galatians", "GAL", "GALATIANS", "GA");
        Register("Ephesians", "EPH", "EPHESIANS");
        Register("Philippians", "PHIL", "PHILIPPIANS", "PHP");
        Register("Colossians", "COL", "COLOSSIANS");
        Register("1 Thessalonians", "1 THESS", "1THESS", "1 THESSALONIANS", "1THESSALONIANS", "1 TH", "1TH", "1TS");
        Register("2 Thessalonians", "2 THESS", "2THESS", "2 THESSALONIANS", "2THESSALONIANS", "2 TH", "2TH", "2TS");
        Register("1 Timothy", "1 TIM", "1TIM", "1 TIMOTHY", "1TIMOTHY", "1 TI", "1TI");
        Register("2 Timothy", "2 TIM", "2TIM", "2 TIMOTHY", "2TIMOTHY", "2 TI", "2TI");
        Register("Titus", "TIT", "TITUS", "TI");
        Register("Philemon", "PHILEM", "PHILEMON", "PHM");
        Register("Hebrews", "HEB", "HEBREWS");
        Register("James", "JAS", "JAMES", "JM");
        Register("1 Peter", "1 PET", "1PET", "1 PETER", "1PETER", "1 PT", "1PT", "1PE");
        Register("2 Peter", "2 PET", "2PET", "2 PETER", "2PETER", "2 PT", "2PT", "2PE");
        Register("1 John", "1 JN", "1JN", "1 JOHN", "1JOHN", "1 JHN", "1JHN", "1J");
        Register("2 John", "2 JN", "2JN", "2 JOHN", "2JOHN", "2 JHN", "2JHN", "2J");
        Register("3 John", "3 JN", "3JN", "3 JOHN", "3JOHN", "3 JHN", "3JHN", "3J");
        Register("Jude", "JUDE", "JUD", "JD");
        Register("Revelation", "REV", "REVELATION", "REVELATIONS", "APOCALYPSE", "RV");
    }

    private static void Register(string canonical, params string[] variations)
    {
        NormalizedAliases[NormalizeKey(canonical)] = canonical;
        foreach (var v in variations)
        {
            NormalizedAliases[NormalizeKey(v)] = canonical;
        }
    }

    public static string NormalizeKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }
        return sb.ToString();
    }

    public static bool TryGetCanonicalName(string? input, out string canonicalName)
    {
        canonicalName = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var key = NormalizeKey(input);
        if (NormalizedAliases.TryGetValue(key, out var match))
        {
            canonicalName = match;
            return true;
        }

        return false;
    }
}
