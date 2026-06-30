namespace MindedOS.Engine;

/// <summary>Evaluates a single clause against a current signal value.</summary>
public static class ClauseEval
{
    public static bool Holds(Clause? clause, Func<string, double> signal)
    {
        if (clause is null) return true; // absent condition is always satisfied
        double actual = signal(clause.Signal);
        return clause.Op switch
        {
            ">" => actual > clause.Value,
            "<" => actual < clause.Value,
            ">=" => actual >= clause.Value,
            "<=" => actual <= clause.Value,
            "==" => Math.Abs(actual - clause.Value) < 0.0001,
            "!=" => Math.Abs(actual - clause.Value) >= 0.0001,
            _ => false,
        };
    }
}

/// <summary>A live trigger binding: when the rising edge fires, run the action.</summary>
public sealed class TriggerBinding
{
    public required string ProgramName { get; init; }
    public required Clause Trigger { get; init; }
    public Clause? Condition { get; init; }
    public required string ActionId { get; init; }
    public bool WasActive { get; set; }
}

/// <summary>
/// Watches the <see cref="SignalHub"/> and fires bound actions on the rising edge
/// of each trigger (gated by its optional condition), so an action fires once per
/// crossing rather than continuously while the threshold is exceeded.
/// </summary>
public sealed class TriggerEngine : IDisposable
{
    private readonly SignalHub _hub;
    private readonly ActionExecutor _executor;
    private readonly List<TriggerBinding> _bindings = new();
    private readonly object _gate = new();

    public TriggerEngine(SignalHub hub, ActionExecutor executor)
    {
        _hub = hub;
        _executor = executor;
        _hub.Updated += OnUpdated;
    }

    public event Action<TriggerBinding>? Fired;

    public void Register(TriggerBinding binding)
    {
        lock (_gate) _bindings.Add(binding);
    }

    /// <summary>Drop every trigger belonging to a program (e.g. on window close).</summary>
    public void UnregisterProgram(string programName)
    {
        lock (_gate) _bindings.RemoveAll(b => b.ProgramName == programName);
    }

    private void OnUpdated(string changedSignal)
    {
        List<TriggerBinding> snapshot;
        lock (_gate) snapshot = new List<TriggerBinding>(_bindings);

        foreach (var b in snapshot)
        {
            bool active = ClauseEval.Holds(b.Trigger, _hub.GetSignal)
                       && ClauseEval.Holds(b.Condition, _hub.GetSignal);

            if (active && !b.WasActive)
            {
                _executor.Run(b.ActionId);
                Fired?.Invoke(b);
            }
            b.WasActive = active;
        }
    }

    public void Dispose() => _hub.Updated -= OnUpdated;
}
