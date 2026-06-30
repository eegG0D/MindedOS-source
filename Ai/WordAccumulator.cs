namespace MindedOS.Ai;

/// <summary>
/// Collects the EEG-to-English words sampled over an accumulation window into an
/// ordered list, collapsing consecutive duplicates and capping the total so the
/// resulting prompt stays a reasonable size.
/// </summary>
public sealed class WordAccumulator
{
    private readonly int _maxWords;
    private readonly List<string> _words = new();
    private string? _last;

    public WordAccumulator(int maxWords = 600) => _maxWords = maxWords;

    public int Count => _words.Count;
    public IReadOnlyList<string> Words => _words;

    /// <summary>Add one sampled word; consecutive duplicates and blanks are ignored.</summary>
    public void Add(string? word)
    {
        if (string.IsNullOrWhiteSpace(word) || word == "—") return;
        word = word.Trim();
        if (word == _last) return;
        if (_words.Count >= _maxWords) return;
        _words.Add(word);
        _last = word;
    }

    /// <summary>The accumulated words as a single space-separated seed string.</summary>
    public string Seed() => string.Join(' ', _words);
}
