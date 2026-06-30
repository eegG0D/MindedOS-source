using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Quantum Computing program (education/research framed).</summary>
public static class QuantumPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<QuantumScore> concepts, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", concepts.Take(9).Select(c => $"{c.Topic} {c.Percent:0}%"));
        string system =
            "You are a quantum computing educator working from a person's EEG-decoded words and detected " +
            "quantum concepts. Stay educational and research-oriented; no operational hardware claims. " +
            "Output EIGHT sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# ALGORITHMS  (Markdown: for each of ~5 algorithms give Name, Purpose, Description, Advantages, Research opportunities)\n" +
            "# RESEARCH TOPICS  (research questions, hypotheses, experiment ideas, simulation projects, learning objectives)\n" +
            "# PROBLEM SOLVING  (Markdown: speculative approaches to optimization, scheduling, data analysis, scientific simulation, computational research)\n" +
            "# THEORIES  (Markdown: educational theories, each with Title, Description, Assumptions, Applications, Limitations, Future research)\n" +
            "# ARCHITECTURES  (Markdown: prompts for quantum processors, networking, data centers, AI platforms, future infrastructures)\n" +
            "# AI REPORT  (intersections of quantum computing, AI, neural networks, cognitive and brain-inspired computing)\n" +
            "# CURRICULUM  (Markdown with ## Beginner, ## Intermediate, ## Advanced; topics, projects, books, papers, milestones)\n" +
            "# KNOWLEDGE GRAPH  (Markdown bullet list relating concepts, algorithms, technologies, research areas)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Detected concepts: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the eight marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<QuantumScore> concepts, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", concepts.Take(6).Select(c => $"{c.Topic} {c.Percent:0}%"));
        string system =
            "You are a quantum computing educator writing an analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Concept Analysis\n## Learning Profile\n## Research Opportunities\n## Generated Theories\n" +
            "## Recommendations. Education/research framed; no operational hardware claims. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Concepts: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<QuantumScore> concepts, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", concepts.Take(6).Select(c => $"{c.Topic} {c.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the quantum computing analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Quantum Concepts, 4) Learning Profile, 5) Algorithm Ideas, " +
            "6) Research Topics, 7) Quantum AI, 8) Architectures, 9) Future Opportunities, 10) Conclusions.";
        string user =
            $"Concepts: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
