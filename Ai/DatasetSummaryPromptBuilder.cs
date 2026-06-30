namespace MindedOS.Ai;

/// <summary>
/// Asks LM Studio to write ONE short descriptive TITLE PHRASE for a profiled CSV,
/// "through the EEG": the user's live decoded brain word + state flavor the title,
/// while it stays accurate to the dataset profile.
/// </summary>
public static class DatasetTitlePromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(string profileText, string eegWord, string focusWord)
    {
        string system =
            "You are a data analyst working through a brain-computer interface. Given a CSV dataset's " +
            "profile (rows, columns, types, samples) AND the user's live EEG (a decoded brain word and a " +
            "cognitive state), write EXACTLY ONE short DESCRIPTIVE TITLE PHRASE (3–8 words, no trailing " +
            "punctuation) for the dataset. The EEG brain word should flavor the angle or wording of the " +
            "title, but the title MUST stay accurate to what the profile shows — never invent fields. " +
            "Output ONLY the title phrase, nothing else.";

        string user =
            "=== DATASET PROFILE ===\n" + profileText + "\n" +
            $"=== EEG ===\nBrain word: {(string.IsNullOrWhiteSpace(eegWord) ? "(none)" : eegWord)} · state: {focusWord} focus\n\n" +
            "Write the one EEG-flavored descriptive title phrase for this dataset.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
