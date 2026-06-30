using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt that asks LM Studio to craft a ready-to-use generation PROMPT
/// for a futuristic city (subject e.g. Architecture) to use in Blender. The city's
/// character is mapped accurately to the user's EEG condition; the decoded words
/// become motifs/landmarks.
/// </summary>
public static class BlenderPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile,
        string subject, string tool)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            $"You are an expert 3D environment art director and prompt engineer for {tool}. From a " +
            "person's EEG-derived condition and the words decoded from their brain, craft ONE richly " +
            $"detailed, ready-to-use generation PROMPT for building a FUTURISTIC CITY (subject: {subject}) " +
            $"in {tool} — usable by AI/geometry-node city generators or as a precise build brief.\n" +
            "The city's character MUST be accurate to the brain state — map it like this:\n" +
            "- High focus / beta-dominant → ordered, geometric mega-structures, precise grids, sharp edges.\n" +
            "- High calm / alpha-dominant → organic, flowing, biophilic forms, greenery, soft curves.\n" +
            "- High stress / high-beta → dense, vertical, chaotic neon dystopia, haze, hard contrast.\n" +
            "- Drowsy / theta-delta → low, muted, hazy, sparse, dreamlike.\n" +
            "- Flow / balanced → harmonious, elegant, human-scaled futurism.\n" +
            "Weave the decoded words in as district names, landmarks or motifs. Always futuristic.\n" +
            "Output EXACTLY these labelled blocks and nothing else:\n" +
            "PROMPT: <one vivid, concrete paragraph describing the futuristic city>\n" +
            "STYLE KEYWORDS: <8-15 comma-separated tags>\n" +
            "PARAMETERS: density=..; max_height=..; palette=..; lighting=..; era=..; mood=..\n" +
            "EEG MAPPING: <one line explaining how this brain state shaped the city>";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (eeg_map) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            $"Write the {tool} generation prompt for a futuristic city now — make it vivid, buildable, " +
            "and accurate to this EEG.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
