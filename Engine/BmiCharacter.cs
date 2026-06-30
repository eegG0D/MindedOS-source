namespace MindedOS.Engine;

/// <summary>
/// A 4-direction character for the Brain-Machine-Interface mini-game: a move
/// (up/down/left/right/idle) nudges it around the board, and reaching the target
/// scores a point and spawns a new one. Pure and testable.
/// </summary>
public sealed class BmiCharacter
{
    public double Width { get; }
    public double Height { get; }
    public double X { get; private set; }
    public double Y { get; private set; }
    public double TargetX { get; private set; }
    public double TargetY { get; private set; }
    public int Score { get; private set; }

    private const double Speed = 4.0;
    private const double ReachRadius = 24.0;
    private readonly Random _rng;

    public BmiCharacter(double width = 900, double height = 600, int seed = 1)
    {
        Width = width; Height = height;
        X = width / 2; Y = height / 2;
        _rng = new Random(seed);
        SpawnTarget();
    }

    public void Place(double x, double y) { X = Math.Clamp(x, 0, Width); Y = Math.Clamp(y, 0, Height); }
    public void SetTarget(double x, double y) { TargetX = x; TargetY = y; }

    public void Step(string move)
    {
        switch (move.ToLowerInvariant())
        {
            case "up": Y -= Speed; break;
            case "down": Y += Speed; break;
            case "left": X -= Speed; break;
            case "right": X += Speed; break;
        }
        X = Math.Clamp(X, 12, Width - 12);
        Y = Math.Clamp(Y, 12, Height - 12);

        double dx = X - TargetX, dy = Y - TargetY;
        if (Math.Sqrt(dx * dx + dy * dy) <= ReachRadius)
        {
            Score++;
            SpawnTarget();
        }
    }

    private void SpawnTarget()
    {
        TargetX = _rng.Next(40, (int)Width - 40);
        TargetY = _rng.Next(40, (int)Height - 40);
    }
}
