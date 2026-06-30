using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Swarm Intelligence program. Self-contained.</summary>
public static class SwarmPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string RoleList(IReadOnlyList<SwarmDomainScore> domains, int n) =>
        string.Join(", ", domains.Take(n).Select(d => $"{d.Domain} {d.Percent:0}%"));

    /// <summary>One reply with four marked sections → collective intelligence, emergent behavior, innovation swarm, forecast.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<SwarmDomainScore> domains, int agents, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a swarm-intelligence engine modeling a person's EEG-decoded concepts as a colony of cooperating agents. " +
            "Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# COLLECTIVE INTELLIGENCE  (shared concepts, common goals, emerging solutions, knowledge convergence)\n" +
            "# EMERGENT BEHAVIOR  (unexpected solutions, novel combinations, creative discoveries, emergent innovations)\n" +
            "# INNOVATION SWARM  (new inventions, research directions, engineering concepts, business opportunities, AI systems)\n" +
            "# SWARM FORECAST  (future ideas, emerging interests, research opportunities, innovation potential)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Swarm: {agents} agents. Dominant roles: {RoleList(domains, 8)}.\n" +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>Distributed problem solving — five domain headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildSolution(
        string wordSeed, IReadOnlyList<SwarmDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a distributed problem-solving swarm writing a report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Engineering\n" +
            "## Robotics\n## AI\n## Architecture\n## Science. Under each, give a challenge and a swarm-collaborated " +
            "solution where agents cooperate and converge. No code fences.";
        string user =
            $"Dominant roles: {RoleList(domains, 5)}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The swarm-intelligence research paper — six level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<SwarmDomainScore> domains, int agents, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a complex-systems scientist writing a swarm-intelligence research paper in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Methodology\n## Swarm Analysis\n## Results\n## Discussion\n## Conclusions. " +
            "Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Swarm: {agents} agents. Dominant roles: {RoleList(domains, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the paper with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<SwarmDomainScore> domains, int agents, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the swarm-intelligence analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Agent Creation, 4) Swarm Formation, 5) Collective Intelligence, " +
            "6) Knowledge Ecosystem, 7) Emergent Behaviors, 8) Innovation Analysis, 9) Forecasting, 10) Conclusions.";
        string user =
            $"Swarm: {agents} agents. Dominant roles: {RoleList(domains, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
