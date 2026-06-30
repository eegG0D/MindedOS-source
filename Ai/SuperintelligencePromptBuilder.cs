using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Superintelligence program. Self-contained.</summary>
public static class SuperintelligencePromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string DomainList(IReadOnlyList<SuperDomainScore> domains, int n) =>
        string.Join(", ", domains.Take(n).Select(d => $"{d.Domain} {d.Percent:0}%"));

    /// <summary>One reply with four marked sections → problem solving, systems thinking, future knowledge, growth.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<SuperDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a Superintelligence research engine working from a person's EEG-decoded words and domains. " +
            "Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# PROBLEM SOLVING  (level-2 headings ## Science, ## Engineering, ## Robotics, ## Artificial Intelligence, ## Architecture, ## Mathematics, ## Business — a simulated challenge and a concrete solution under each)\n" +
            "# SYSTEMS THINKING  (cause and effect, network thinking, interconnected concepts, hierarchical reasoning, complex system understanding)\n" +
            "# FUTURE KNOWLEDGE  (hypothetical learning paths over 1 year, 5 years, 10 years — skills acquired, knowledge growth, research opportunities)\n" +
            "# GROWTH  (recommendations for learning efficiency, critical thinking, research methodology, creativity, scientific reasoning, engineering design)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Domains: {DomainList(domains, 10)}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The artificial research council — 10 specialist agents, Markdown.</summary>
    public static ArmyPromptBuilder.Prompt BuildCouncil(
        string wordSeed, IReadOnlyList<SuperDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are an Artificial Research Council of 10 specialist agents writing in GitHub-flavored Markdown. " +
            "Start with '# Artificial Research Council'. Then a Markdown bullet for EACH agent — Scientist, Engineer, " +
            "Architect, Economist, Researcher, Roboticist, Programmer, Educator, Analyst, Inventor — where each analyzes " +
            "the EEG concepts and contributes one concrete recommendation. No code fences.";
        string user =
            $"Domains: {DomainList(domains, 10)}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the council report now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The superintelligence research report — six level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildResearchReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<SuperDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a cognitive research scientist writing a Superintelligence research report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Cognitive Analysis\n## Innovation Analysis\n## Knowledge Integration Analysis\n## Future Simulations\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Domains: {DomainList(domains, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 12-slide presentation — exactly 12 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<SuperDomainScore> domains, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 12 slides from the Superintelligence analysis.\n" +
            "Output EXACTLY 12 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Cognitive Capabilities, 4) Knowledge Integration, 5) Innovation Assessment, " +
            "6) Problem Solving Analysis, 7) Systems Thinking, 8) Research Council Results, 9) Future Simulations, " +
            "10) Expert Comparisons, 11) Recommendations, 12) Conclusions.";
        string user =
            $"Domains: {DomainList(domains, 5)}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 12 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
