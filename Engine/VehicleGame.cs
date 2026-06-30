namespace MindedOS.Engine;

/// <summary>
/// Top-down vehicle physics for the EEG driving game: a move (stop/forward/back/
/// left/right/go) updates speed and heading, and the car integrates its position,
/// clamped to the play field. Pure and testable.
/// </summary>
public sealed class VehicleGame
{
    public double Width { get; }
    public double Height { get; }

    public double X { get; private set; }
    public double Y { get; private set; }
    public double Heading { get; set; }   // radians; 0 = facing +X (right)
    public double Speed { get; private set; }
    public double Distance { get; private set; }

    private const double MaxSpeed = 5.0;
    private const double Accel = 0.35;
    private const double TurnRate = 0.09;
    private const double Brake = 0.82;

    public VehicleGame(double width = 940, double height = 600, double startX = 150, double startY = 300)
    {
        Width = width; Height = height;
        X = startX; Y = startY; Heading = 0; Speed = 0;
    }

    public void ForceStop() => Speed = 0;

    /// <summary>Apply one move and integrate one step.</summary>
    public void Step(string move)
    {
        switch (move.ToLowerInvariant())
        {
            case "stop":
                Speed *= Brake;
                if (Math.Abs(Speed) < 0.05) Speed = 0;
                break;
            case "forward":
            case "go":
                Speed = Math.Min(MaxSpeed, Speed + Accel);
                break;
            case "backward":
                Speed = Math.Max(-MaxSpeed * 0.5, Speed - Accel);
                break;
            case "left":
                Heading -= TurnRate;
                Speed = Math.Min(MaxSpeed, Speed + Accel * 0.3); // a turn keeps you rolling
                break;
            case "right":
                Heading += TurnRate;
                Speed = Math.Min(MaxSpeed, Speed + Accel * 0.3);
                break;
        }

        X += Math.Cos(Heading) * Speed;
        Y += Math.Sin(Heading) * Speed;
        Distance += Math.Abs(Speed);

        // bounce off the field edges so the car stays on screen
        if (X < 12) { X = 12; Speed *= 0.4; }
        if (X > Width - 12) { X = Width - 12; Speed *= 0.4; }
        if (Y < 12) { Y = 12; Speed *= 0.4; }
        if (Y > Height - 12) { Y = Height - 12; Speed *= 0.4; }
    }
}
