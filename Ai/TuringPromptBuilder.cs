using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Turing Test program. Self-contained.</summary>
public static class TuringPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    /// <summary>One reply with two marked sections → artificial thoughts and a human-vs-AI chat.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are generating AI-side material for a Turing test against a person's EEG-decoded thoughts. " +
            "Output TWO sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# ARTIFICIAL THOUGHTS  (AI thought streams, reasoning samples, explanations, a conversation snippet, and a problem-solving example — clearly machine-like: structured, explicit, low emotional variance)\n" +
            "# HUMAN VS AI CHAT  (a turn-by-turn dialogue alternating 'Human:' lines built from the EEG concepts and 'AI:' lines; they discuss and debate the ideas and compare reasoning styles)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM (the human thoughts) ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the two marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The Turing-test research report — five level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, string verdict, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a cognitive scientist writing a Turing-test report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Statistical Analysis\n## Comparison Charts\n## Human vs AI Findings\n## Conclusions. " +
            "Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Current verdict: {verdict}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(string verdict, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the Turing-test analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Human Thought Profile, 4) AI Thought Profile, 5) Human-Likeness Scores, " +
            "6) Machine-Likeness Scores, 7) Blind Judge Results, 8) Creativity Comparison, 9) Reasoning Comparison, 10) Conclusions.";
        string user =
            $"Verdict: {verdict}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
