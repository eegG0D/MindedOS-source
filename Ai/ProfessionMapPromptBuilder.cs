namespace MindedOS.Ai;

/// <summary>
/// Asks LM Studio to generate a profession_map.csv mapping professions to common
/// English keyword sets, in the exact format ProfessionMap parses.
/// </summary>
public static class ProfessionMapPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build()
    {
        string system =
            "You build a 'profession_map.csv' that classifies streams of decoded EEG words into " +
            "professions. Output ONLY CSV (no prose, no code fences). EXACT format:\n" +
            "profession,keywords\n" +
            "- One row per profession. After the profession name and a comma, list ~8 COMMON English " +
            "words (space-separated) strongly associated with that profession.\n" +
            "- Use everyday words (people, build, money, care, learn…) so they match a generic word " +
            "stream. 14-18 professions covering a broad economy.\n" +
            "First line must be exactly: profession,keywords";

        string user = "Generate the profession_map.csv now. CSV only.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
