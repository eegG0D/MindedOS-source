namespace MindedOS.Ai;

/// <summary>
/// Small prompt asking LM Studio for an elaboration of the Group Policy baseline.
/// The 35 rules are generated deterministically by mindedOS; the LLM only writes
/// the threat-model prose explaining how they work together.
/// </summary>
public static class GpoPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(string codename, int ruleCount, string wordSeed)
    {
        string system =
            "You are a Windows security architect. Write a sharp 2–3 paragraph elaboration explaining " +
            $"how the '{codename}' hardening baseline of {ruleCount} Group Policy rules forms ONE coherent, " +
            "defense-in-depth security posture in which the rules SUPPORT and reinforce one another with no " +
            "conflicts (credential hardening + audit + firewall + Defender + legacy-protocol removal). " +
            "Theme it lightly around the brain-decoded concepts. Output ONLY the prose — no headings, no lists.";

        string user =
            "Brain-decoded concept seeds: " + (string.IsNullOrWhiteSpace(wordSeed) ? "(none)" : wordSeed) +
            "\n\nWrite the elaboration now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
