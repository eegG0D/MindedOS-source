using System.Text.Json.Serialization;

namespace MindedOS.Engine;

/// <summary>One move in a predetermined series: run an action, repeat, then wait.</summary>
public sealed class MoveStep
{
    [JsonPropertyName("action")] public string Action { get; set; } = "";
    [JsonPropertyName("repeat")] public int Repeat { get; set; } = 1;
    [JsonPropertyName("delayMs")] public int DelayMs { get; set; } = 100;
    [JsonPropertyName("comment")] public string? Comment { get; set; }
}

/// <summary>
/// How the live raw EEG is matched against this sequence's predetermined EEG.
/// </summary>
public sealed class MatchSpec
{
    /// <summary>"waveform" | "value" | "word".</summary>
    [JsonPropertyName("mode")] public string Mode { get; set; } = "value";

    // --- waveform: correlate the live sliding window against a stored reference ---
    [JsonPropertyName("reference")] public List<int>? Reference { get; set; }
    [JsonPropertyName("threshold")] public double Threshold { get; set; } = 0.8;

    // --- value: a raw sample within target +/- tolerance, held for N samples ---
    [JsonPropertyName("target")] public int? Target { get; set; }
    [JsonPropertyName("tolerance")] public int Tolerance { get; set; } = 50;
    [JsonPropertyName("holdSamples")] public int HoldSamples { get; set; } = 3;

    // --- word: the raw stream maps (via the lexicon) to this word sequence in order ---
    [JsonPropertyName("words")] public List<string>? Words { get; set; }
    [JsonPropertyName("resetMs")] public int ResetMs { get; set; } = 4000;

    // --- common ---
    /// <summary>Minimum time between two fires of this sequence.</summary>
    [JsonPropertyName("cooldownMs")] public int CooldownMs { get; set; } = 5000;
}

/// <summary>
/// A predetermined series of moves that runs when the live raw EEG matches the
/// sequence's <see cref="MatchSpec"/>. Loaded from a (large) JSON file; a program
/// can attach two or more of these.
/// </summary>
public sealed class EegSequence
{
    [JsonPropertyName("name")] public string Name { get; set; } = "Untitled Sequence";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("match")] public MatchSpec Match { get; set; } = new();
    [JsonPropertyName("moves")] public List<MoveStep> Moves { get; set; } = new();

    [JsonIgnore] public string SourcePath { get; set; } = "";
}
