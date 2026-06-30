namespace MindedOS.Ai;

/// <summary>
/// Asks LM Studio to generate an eeg_map_bmi.csv mapping live EEG signals to the
/// four character directions, in the format VehicleMoveMap parses.
/// </summary>
public static class BmiMapPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build()
    {
        string system =
            "You design control mappings for an EEG brain-machine-interface mini-game where the player " +
            "moves a character in 4 directions. Output ONLY a CSV (no prose, no code fences). EXACT format:\n" +
            "priority,signal,op,value,move\n" +
            "- One rule per line; the highest-priority rule whose condition is true wins.\n" +
            "- signal ∈ {attention, meditation, blink, signal, raw, delta, theta, lowAlpha, highAlpha, " +
            "lowBeta, highBeta, lowGamma, midGamma}; op ∈ {>, <, >=, <=}; value is a number.\n" +
            "- move ∈ {up, down, left, right, idle}\n" +
            "- Provide a way to go up, down, left and right, plus an idle/blink rule.\n" +
            "- The LAST line MUST be: 0,default,,,idle";

        string user = "Generate a fresh, playable eeg_map_bmi.csv now. CSV only.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
