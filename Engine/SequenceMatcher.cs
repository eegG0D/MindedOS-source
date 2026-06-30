using System.Diagnostics;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Holds the live-matching state for one <see cref="EegSequence"/>. Feed it raw
/// EEG samples; it returns true on the rising edge of a match (and enforces a
/// cooldown + re-arm so the moves don't fire continuously while matched).
///
/// Three modes (per the sequence's MatchSpec):
///   waveform — Pearson cross-correlation of the live window vs. a stored reference
///   value    — a raw sample within target±tolerance, held for N consecutive samples
///   word     — the raw stream maps (via the lexicon) to a word sequence, in order
/// </summary>
public sealed class SequenceMatcher
{
    private readonly EegSequence _seq;
    private readonly RawLexicon _lexicon;
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    // waveform
    private readonly int[] _ring;
    private int _ringPos;
    private int _ringFilled;
    private readonly double _refMean;
    private readonly double _refStd;

    // value
    private int _holdCount;

    // word
    private int _wordIndex;
    private long _wordProgressMs;

    // common
    private bool _armed = true;
    private bool _fired;
    private long _lastFireMs;

    public EegSequence Sequence => _seq;

    public SequenceMatcher(EegSequence seq, RawLexicon lexicon)
    {
        _seq = seq;
        _lexicon = lexicon;

        var reference = seq.Match.Reference ?? new List<int>();
        _ring = new int[Math.Max(1, reference.Count)];
        if (reference.Count > 0)
        {
            _refMean = reference.Average();
            double varSum = reference.Sum(v => (v - _refMean) * (v - _refMean));
            _refStd = Math.Sqrt(varSum / reference.Count);
        }
    }

    /// <summary>Offer one raw sample; returns true exactly once per matched event.</summary>
    public bool Offer(int raw)
    {
        bool hit = _seq.Match.Mode.ToLowerInvariant() switch
        {
            "waveform" => OfferWaveform(raw),
            "word" => OfferWord(raw),
            _ => OfferValue(raw),
        };

        if (!hit) return false;

        // Rising-edge + cooldown gate.
        long now = _clock.ElapsedMilliseconds;
        if (!_armed) return false;
        if (_fired && now - _lastFireMs < _seq.Match.CooldownMs) return false;

        _armed = false;
        _fired = true;
        _lastFireMs = now;
        return true;
    }

    private bool OfferValue(int raw)
    {
        var m = _seq.Match;
        int target = m.Target ?? 0;
        bool inRange = Math.Abs(raw - target) <= m.Tolerance;
        if (inRange)
        {
            _holdCount++;
            if (_holdCount >= Math.Max(1, m.HoldSamples)) return true;
        }
        else
        {
            _holdCount = 0;
            _armed = true; // left the band — ready to fire again next time
        }
        return false;
    }

    private bool OfferWaveform(int raw)
    {
        var reference = _seq.Match.Reference;
        if (reference is null || reference.Count == 0 || _refStd <= 0) return false;

        _ring[_ringPos] = raw;
        _ringPos = (_ringPos + 1) % _ring.Length;
        if (_ringFilled < _ring.Length) _ringFilled++;
        if (_ringFilled < _ring.Length) return false; // still filling

        double corr = Correlation(reference);
        if (corr >= _seq.Match.Threshold) return true;

        // re-arm once the live window stops resembling the reference
        if (corr < _seq.Match.Threshold * 0.6) _armed = true;
        return false;
    }

    private double Correlation(List<int> reference)
    {
        // live window in chronological order starting at _ringPos (oldest)
        int n = _ring.Length;
        double mean = 0;
        for (int i = 0; i < n; i++) mean += _ring[i];
        mean /= n;

        double cov = 0, var = 0;
        for (int i = 0; i < n; i++)
        {
            int idx = (_ringPos + i) % n;
            double a = _ring[idx] - mean;
            double b = reference[i] - _refMean;
            cov += a * b;
            var += a * a;
        }
        double std = Math.Sqrt(var / n);
        if (std <= 0) return 0;
        return cov / n / (std * _refStd);
    }

    private bool OfferWord(int raw)
    {
        var words = _seq.Match.Words;
        if (words is null || words.Count == 0 || !_lexicon.IsLoaded) return false;

        long now = _clock.ElapsedMilliseconds;
        if (_wordIndex > 0 && now - _wordProgressMs > _seq.Match.ResetMs)
            _wordIndex = 0; // too slow — start the phrase over

        string word = _lexicon.WordFor(raw);
        if (string.Equals(word, words[_wordIndex], StringComparison.OrdinalIgnoreCase))
        {
            _wordIndex++;
            _wordProgressMs = now;
            if (_wordIndex >= words.Count)
            {
                _wordIndex = 0;
                _armed = true; // word mode is event-like; allow next phrase immediately (cooldown still applies)
                return true;
            }
        }
        return false;
    }
}
