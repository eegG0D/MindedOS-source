using MindedOS.Core;
using MindedOS.Sensor;

namespace MindedOS.Engine;

/// <summary>A sequence registered against a program, with its live match state.</summary>
public sealed class SequenceBinding
{
    public required string ProgramName { get; init; }
    public required SequenceMatcher Matcher { get; init; }
    public bool Running { get; set; }
    public EegSequence Sequence => Matcher.Sequence;
}

/// <summary>
/// Watches the shared raw EEG stream and, when a registered sequence's pattern is
/// matched, runs that sequence's predetermined series of moves (each move = an
/// action, repeated, then a delay). Honors the executor's Safe Mode. Multiple
/// sequences can be registered per program — a program can attach two or more.
/// </summary>
public sealed class SequenceEngine : IDisposable
{
    private readonly IEegSource _source;
    private readonly RawLexicon _lexicon;
    private readonly ActionExecutor _executor;
    private readonly List<SequenceBinding> _bindings = new();
    private readonly object _gate = new();

    public SequenceEngine(IEegSource source, RawLexicon lexicon, ActionExecutor executor)
    {
        _source = source;
        _lexicon = lexicon;
        _executor = executor;
        _source.Event += OnEvent;
    }

    /// <summary>Raised when a sequence starts firing its moves.</summary>
    public event Action<EegSequence>? Matched;

    /// <summary>Raised after each move step runs: (sequence, stepIndex, action).</summary>
    public event Action<EegSequence, int, string>? MoveExecuted;

    /// <summary>Raised when a sequence finishes its full series of moves.</summary>
    public event Action<EegSequence>? Completed;

    public void Register(string programName, EegSequence sequence)
    {
        lock (_gate)
            _bindings.Add(new SequenceBinding
            {
                ProgramName = programName,
                Matcher = new SequenceMatcher(sequence, _lexicon),
            });
    }

    public void UnregisterProgram(string programName)
    {
        lock (_gate) _bindings.RemoveAll(b => b.ProgramName == programName);
    }

    public int ActiveCount { get { lock (_gate) return _bindings.Count; } }

    private void OnEvent(EegEvent e)
    {
        if (e is not RawEvent raw) return;

        List<SequenceBinding> snapshot;
        lock (_gate) snapshot = new List<SequenceBinding>(_bindings);

        foreach (var b in snapshot)
        {
            if (b.Running) continue;             // don't overlap a sequence with itself
            if (!b.Matcher.Offer(raw.Amplitude)) continue;

            b.Running = true;
            _ = RunMovesAsync(b);
        }
    }

    private async Task RunMovesAsync(SequenceBinding b)
    {
        var seq = b.Sequence;
        Matched?.Invoke(seq);
        try
        {
            for (int i = 0; i < seq.Moves.Count; i++)
            {
                var step = seq.Moves[i];
                int repeat = Math.Max(1, step.Repeat);
                for (int r = 0; r < repeat; r++)
                {
                    if (!string.IsNullOrWhiteSpace(step.Action))
                        _executor.Run(step.Action);
                }
                MoveExecuted?.Invoke(seq, i, step.Action);
                if (step.DelayMs > 0) await Task.Delay(step.DelayMs);
            }
            Completed?.Invoke(seq);
        }
        finally
        {
            b.Running = false;
        }
    }

    public void Dispose() => _source.Event -= OnEvent;
}
