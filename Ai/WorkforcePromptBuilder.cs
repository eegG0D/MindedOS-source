namespace MindedOS.Ai;

/// <summary>
/// Small prompt asking LM Studio for a short, themed elaboration of the augmented
/// workforce. The diagram, deploy prompt and 200-agent roster are generated
/// deterministically by mindedOS; the LLM only writes the intro prose.
/// </summary>
public static class WorkforcePromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(int agentCount, string wordSeed)
    {
        string system =
            "You are an org designer. Write a vivid 2–3 paragraph elaboration of an 'Augmented " +
            $"Workforce' — a fleet of {agentCount} AI coding agents deployed as subagents in Claude " +
            "Code (driven by a local LM Studio model), organized into guilds that collaborate to build " +
            "software. Explain how one operator orchestrates the swarm. Theme it around the brain-" +
            "decoded concepts provided. Output ONLY the prose (markdown inline ok), no headings, no lists.";

        string user =
            "Brain-decoded concept seeds: " +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(none)" : wordSeed) +
            "\n\nWrite the elaboration now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
