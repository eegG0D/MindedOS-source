using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Transfer Learning program. Self-contained.</summary>
public static class TransferPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string DomainList(IReadOnlyList<TransferDomainScore> domains, int n) =>
        string.Join(", ", domains.Take(n).Select(d => $"{d.Domain} {d.Percent:0}%"));

    /// <summary>One reply with four marked sections → skill transfer, knowledge reuse, research transfer, innovation transfer.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<TransferDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a transfer-learning engine working from a person's EEG-decoded concepts and domains. " +
            "Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# SKILL TRANSFER  (existing strengths, transferable skills, hidden competencies, cross-domain abilities)\n" +
            "# KNOWLEDGE REUSE  (reusable concepts, problem-solving methods, mental models, learning strategies)\n" +
            "# RESEARCH TRANSFER  (new research topics, interdisciplinary studies, technology applications, engineering applications)\n" +
            "# INNOVATION TRANSFER  (new inventions, technologies, products, scientific theories, engineering projects)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Domains: {DomainList(domains, 12)}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The transfer-learning research report — six level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<TransferDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a learning scientist writing a transfer-learning report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Knowledge Profile\n## Transfer Analysis\n## Innovation Opportunities\n## Research Opportunities\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Domains: {DomainList(domains, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<TransferDomainScore> domains, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the transfer-learning analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Knowledge Profile, 4) Transfer Mapping, 5) Skill Transfer, " +
            "6) Cognitive Adaptation, 7) Innovation Opportunities, 8) Career Analysis, 9) Future Learning Paths, 10) Conclusions.";
        string user =
            $"Domains: {DomainList(domains, 5)}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
