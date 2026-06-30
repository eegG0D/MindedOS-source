namespace MindedOS.Core;

/// <summary>
/// Loads the eeg_map lexicon and folds a raw EEG amplitude onto an English word.
/// Ported from RawLexicon in translator.php (sort by amplitude, modulo indexing
/// from the minimum amplitude). The CSV on disk may be EEG1-encrypted
/// (eeg_map.encrypted.csv) or plaintext — <see cref="FileCrypto"/> decrypts the
/// former in-memory with the key embedded in MindedOS.dll, so the lexicon never
/// touches disk in cleartext.
/// </summary>
public sealed class RawLexicon
{
    private string[] _words = Array.Empty<string>();
    private int _minAmplitude;

    public bool IsLoaded { get; private set; }
    public int Count => _words.Length;

    public void Load(string csvPath)
    {
        if (IsLoaded) return;

        var text = FileCrypto.ReadTextMaybeEncrypted(csvPath);
        var rows = new List<(int amp, string word)>();
        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 2) continue;
            if (!int.TryParse(parts[0].Trim(), out int amp)) continue;
            var word = parts[1].Trim();
            if (word.Length == 0) continue;
            rows.Add((amp, word));
        }
        if (rows.Count == 0) return;

        rows.Sort((a, b) => a.amp.CompareTo(b.amp));
        _minAmplitude = rows[0].amp;
        _words = rows.Select(r => r.word).ToArray();
        IsLoaded = true;
    }

    /// <summary>Map an amplitude to a word, wrapping with positive modulo.</summary>
    public string WordFor(int amplitude)
    {
        int span = _words.Length;
        if (span == 0) return "—";
        int idx = ((amplitude - _minAmplitude) % span + span) % span;
        return _words[idx];
    }
}
