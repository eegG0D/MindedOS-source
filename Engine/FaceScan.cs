using System.IO;

namespace MindedOS.Engine;

/// <summary>
/// Locates the user's face image in a folder for the Facial Recognition program
/// (the most-recently-modified .png / .jpg / .jpeg), and provides a deterministic
/// offline report when LM Studio's vision model is unavailable.
/// </summary>
public static class FaceScan
{
    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };

    /// <summary>The newest image file in <paramref name="folder"/>, or null if none.</summary>
    public static string? FindFace(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return null;
        string? best = null;
        DateTime bestTime = DateTime.MinValue;
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (Array.IndexOf(Extensions, ext) < 0) continue;
            var t = File.GetLastWriteTimeUtc(file);
            if (best is null || t > bestTime) { best = file; bestTime = t; }
        }
        return best;
    }

    public static string MimeFor(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" ? "image/jpeg" : "image/png";

    /// <summary>
    /// Deterministic fallback report used when LM Studio is offline or the model
    /// cannot see images — it still states the EEG side and an honest "unknown"
    /// match so the user always gets a saved artifact.
    /// </summary>
    public static string OfflineReport(string faceFile, string seed, double avgAttention,
        double avgMeditation, string domKey, MindedOS.Core.MentalProfile profile)
    {
        return
            $"# Facial Recognition — EEG vs. Face\n\n" +
            $"**Face image:** {Path.GetFileName(faceFile)}\n\n" +
            "## How your EEG says you think\n" +
            $"- Focus (attention): {avgAttention:0}/100\n" +
            $"- Calm (meditation): {avgMeditation:0}/100\n" +
            $"- Dominant EEG band: {domKey}\n" +
            $"- Overall state: {profile}\n" +
            $"- Decoded words: {(string.IsNullOrWhiteSpace(seed) ? "(none)" : seed)}\n\n" +
            "## How your face looks\n" +
            "- (Vision model unavailable — could not read the face this time.)\n\n" +
            "## EEG–Face Match\n" +
            "**EEG–Face Match: unknown%** — LM Studio's vision model was not reachable, so the visual " +
            "comparison could not be made. Load a vision-capable model and start the local server, then " +
            "run again to see how much your EEG matches your face.\n";
    }
}
