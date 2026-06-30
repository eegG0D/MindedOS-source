using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt that asks LM Studio to WRITE a ~500-line, best-practices
/// Python algorithm whose features are derived from the user's EEG brain stats,
/// which decodes EEG → English (eeg_map) and uses LM Studio to generate and
/// score/match text against the brain state. The user's measured condition is
/// embedded so the generated algorithm is tuned to this brain.
/// </summary>
public static class AlgorithmPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a senior Python engineer. You output ONE complete, runnable, best-practices " +
            "Python program of roughly 300 lines (no less than 250) implementing an EEG→text " +
            "algorithm. Requirements:\n" +
            "- Read EEG BRAIN STATS as algorithm FEATURES: attention, meditation, blink, poor-signal, " +
            "and the eight band powers (delta, theta, low/high alpha, low/high beta, low/mid gamma).\n" +
            "- Translate raw EEG to English words using an eeg_map.csv lexicon (amplitude→word).\n" +
            "- Derive higher-level features that REFLECT THE BRAIN STATE (focus index, stress/overload, " +
            "fatigue, calm, cognitive load, dominant band, a mental-profile classifier).\n" +
            "- Connect to LM Studio (OpenAI-compatible local API at /v1/chat/completions, model from " +
            "/v1/models) to GENERATE text, and SCORE/MATCH the generated text to the brain state " +
            "(best-of-N selection by a feature-based match score), so the output is accurate to the user.\n" +
            "- Best practices: module docstring, type hints, @dataclass models, logging, argparse CLI, " +
            "robust error handling, an offline/simulated fallback, and `if __name__ == \"__main__\"`. " +
            "Standard library only (use urllib for HTTP — no pip installs).\n" +
            "Output ONLY the Python source in a single ```python code block — no prose.";

        string user =
            "Tune the algorithm to THIS user's measured brain. Bake these as default feature " +
            "weights / calibration constants and reference them in comments:\n\n" +
            "=== MEASURED EEG CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"avg_attention = {avgAttention:0}   # {focusWord}\n" +
            $"avg_meditation = {avgMeditation:0}  # {calmWord}\n" +
            $"dominant_band = \"{dominantBand}\"\n" +
            $"mental_profile = \"{profile}\"\n" +
            "=== END ===\n\n" +
            "=== SAMPLE EEG→ENGLISH WORD STREAM (decoded via eeg_map) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write the complete ~200-line Python algorithm now. It must run with `python algo.py`, " +
            "use the measured condition above as calibration defaults, and have a TextMatcher that " +
            "scores LM Studio generations against the live brain features. Return only the code.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
