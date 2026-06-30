using System.IO;

namespace MindedOS.Core;

/// <summary>
/// Resolves a bundled data-file path tolerantly: the same logical file may sit on
/// disk as the plain name (<c>eeg_map.csv</c>), the encrypted name
/// (<c>eeg_map.encrypted.csv</c>) or the decrypted name (<c>eeg_map.decrypted.csv</c>).
/// Callers ask for any one of them and get back whichever actually exists, so the
/// app runs regardless of which naming the data folder uses. Content is decrypted
/// downstream by <see cref="FileCrypto"/>.
/// </summary>
public static class DataFile
{
    /// <summary>
    /// Return the existing file matching <paramref name="path"/>, trying the
    /// encrypted, plain and decrypted variants in turn. Falls back to the
    /// requested path (so the caller's own existence check still reports a miss).
    /// </summary>
    public static string Resolve(string path)
    {
        if (File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path) ?? string.Empty;
        var file = Path.GetFileName(path);
        var ext = Path.GetExtension(file);                  // ".csv"
        var stem = Path.GetFileNameWithoutExtension(file);  // "eeg_map" or "eeg_map.encrypted"

        // Strip any .encrypted/.decrypted marker to get the logical stem.
        if (stem.EndsWith(".encrypted", StringComparison.OrdinalIgnoreCase))
            stem = stem[..^".encrypted".Length];
        else if (stem.EndsWith(".decrypted", StringComparison.OrdinalIgnoreCase))
            stem = stem[..^".decrypted".Length];

        foreach (var candidate in new[]
        {
            Path.Combine(dir, stem + ".encrypted" + ext),
            Path.Combine(dir, stem + ext),
            Path.Combine(dir, stem + ".decrypted" + ext),
        })
        {
            if (File.Exists(candidate)) return candidate;
        }
        return path;
    }

    /// <summary>True if any variant of <paramref name="path"/> exists on disk.</summary>
    public static bool Exists(string path) => File.Exists(Resolve(path));
}
