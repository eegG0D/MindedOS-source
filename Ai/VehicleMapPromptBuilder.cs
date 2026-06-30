namespace MindedOS.Ai;

/// <summary>
/// Asks LM Studio to generate an eeg_map_vehicle.csv mapping live EEG signals to
/// vehicle moves, in the exact format the game's VehicleMoveMap parses.
/// </summary>
public static class VehicleMapPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build()
    {
        string system =
            "You design control mappings for an EEG-driven top-down driving game. Output ONLY a CSV " +
            "(no prose, no code fences) that maps live EEG signals to vehicle moves. EXACT format:\n" +
            "priority,signal,op,value,move\n" +
            "- One rule per line. The highest-priority rule whose condition is true wins.\n" +
            "- signal ∈ {attention, meditation, blink, signal, raw, delta, theta, lowAlpha, highAlpha, " +
            "lowBeta, highBeta, lowGamma, midGamma}\n" +
            "- op ∈ {>, <, >=, <=, ==, !=}; value is a number (attention/meditation 0-100, blink 0-255, " +
            "band powers in the tens/hundreds of thousands).\n" +
            "- move ∈ {stop, forward, backward, left, right, go}\n" +
            "- The LAST line MUST be: 0,default,,,go\n" +
            "Make it playable: a blink should stop, high attention should drive forward, calm should " +
            "turn, and there must be ways to go left, right and backward. 6–9 rules.";

        string user =
            "Generate a fresh, playable eeg_map_vehicle.csv now. CSV only.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
