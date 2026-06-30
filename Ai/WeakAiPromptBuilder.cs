using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Weak AI program. Self-contained.</summary>
public static class WeakAiPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string DomainList(IReadOnlyList<WeakAiDomainScore> domains, int n) =>
        string.Join(", ", domains.Take(n).Select(d => $"{d.Domain} {d.Percent:0}%"));

    /// <summary>Assistant bundle → research, engineering, programming, learning assistant reports.</summary>
    public static ArmyPromptBuilder.Prompt BuildAssistants(
        string wordSeed, IReadOnlyList<WeakAiDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a NARROW, task-oriented Weak AI (specialized assistant, NOT general intelligence) working " +
            "from a person's EEG-decoded concepts. Output FOUR sections and nothing else, each starting with its " +
            "exact marker line on its own line:\n" +
            "# RESEARCH ASSISTANT  (research ideas, study plans, literature review suggestions, future investigations)\n" +
            "# ENGINEERING ASSISTANT  (engineering concepts, system designs, project suggestions, technical recommendations)\n" +
            "# PROGRAMMING ASSISTANT  (software ideas, algorithms, application concepts, automation opportunities)\n" +
            "# LEARNING ASSISTANT  (learning plans, skill recommendations, educational pathways, knowledge gaps)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Domains: {DomainList(domains, 10)}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>Support bundle → chat log, decision support, future predictions, expert profiles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSupport(
        string wordSeed, IReadOnlyList<WeakAiDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a narrow, task-oriented Weak AI working from a person's EEG-decoded concepts. " +
            "Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# CHAT LOG  (a short dialogue alternating 'You:' questions from the concepts and 'Weak AI:' task-focused answers)\n" +
            "# DECISION SUPPORT  (pros and cons, option comparisons, priority rankings, a suggested decision)\n" +
            "# FUTURE PREDICTIONS  (future interests, likely projects, potential learning goals, emerging expertise areas)\n" +
            "# EXPERT PROFILES  (Markdown bullets for Engineer, Scientist, Researcher, Architect, Programmer, Educator using the EEG as context)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Domains: {DomainList(domains, 6)}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The domain knowledge report — five level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<WeakAiDomainScore> domains, string cognitive, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a specialized Weak AI writing a domain-knowledge report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Domain Knowledge\n## Cognitive Profile\n## Recommendations\n## Conclusions. " +
            "Be concrete and task-oriented. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Dominant cognition: {cognitive}. Domains: {DomainList(domains, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<WeakAiDomainScore> domains, string cognitive, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the Weak AI analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Domain Detection, 4) Cognitive Classification, 5) Knowledge Extraction, " +
            "6) Task Recommendations, 7) Decision Support, 8) Productivity Analysis, 9) Specialized AI Outputs, 10) Conclusions.";
        string user =
            $"Dominant cognition: {cognitive}. Domains: {DomainList(domains, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
