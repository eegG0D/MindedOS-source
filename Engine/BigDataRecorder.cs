using System.Globalization;
using System.IO;
using MindedOS.Core;
using MindedOS.Sensor;

namespace MindedOS.Engine;

/// <summary>
/// Records a long EEG session (designed for 4+ hours) as a Big Data CSV of the
/// translated EEG: one row per second with the decoded word (via the lexicon) plus
/// attention/meditation. The word column is what the profession analysis reads.
/// </summary>
public sealed class BigDataRecorder
{
    /// <param name="seconds">Recording length (e.g. 14400 for 4 hours).</param>
    /// <returns>The number of data rows written.</returns>
    public async Task<int> RecordAsync(
        IEegSource source, RawLexicon lexicon, int seconds, string csvPath,
        Action<double>? progress = null, CancellationToken ct = default)
    {
        string word = "—";
        int attention = 0, meditation = 0;

        void OnEvent(EegEvent e)
        {
            switch (e)
            {
                case RawEvent r when lexicon.IsLoaded: word = lexicon.WordFor(r.Amplitude); break;
                case AttentionEvent a: attention = a.Level; break;
                case MeditationEvent m: meditation = m.Level; break;
            }
        }

        if (source.State != LinkState.Streaming) await source.ConnectAsync(ct);
        source.Event += OnEvent;

        var rows = new List<string> { "t_sec,word,attention,meditation" };
        try
        {
            var start = DateTime.UtcNow;
            var window = TimeSpan.FromSeconds(Math.Max(1, seconds));
            for (int t = 0; ; t++)
            {
                ct.ThrowIfCancellationRequested();
                rows.Add(string.Create(CultureInfo.InvariantCulture, $"{t},{word},{attention},{meditation}"));
                var elapsed = DateTime.UtcNow - start;
                progress?.Invoke(Math.Clamp(elapsed / window, 0, 1));
                if (elapsed >= window) break;
                await Task.Delay(1000, ct);
            }
        }
        finally
        {
            source.Event -= OnEvent;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);
        File.WriteAllLines(csvPath, rows);
        return rows.Count - 1;
    }
}
