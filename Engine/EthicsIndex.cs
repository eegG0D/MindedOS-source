using MindedOS.Core;
using MindedOS.Ai;

namespace MindedOS.Engine;

/// <summary>
/// Computes how ethical the brain can be (0–100%) from the EEG and builds the
/// fixed 10-slide ethics deck. Ethics is read deterministically from calm/
/// restraint (meditation), reflective alpha/theta activity and the absence of
/// impulsive fast-beta dominance — so the score and the slide COUNT never depend
/// on the language model; LM Studio only fills the explanatory bullets.
/// </summary>
public static class EthicsIndex
{
    /// <summary>Ethical potential as a percentage, 0–100.</summary>
    public static double Compute(double avgAttention, double avgMeditation, IReadOnlyList<BandReading> bands)
    {
        double total = 0;
        foreach (var b in bands) total += b.Value;
        if (total <= 0) total = 1;

        double Val(string key)
        {
            foreach (var b in bands) if (b.Key == key) return b.Value;
            return 0;
        }

        double alpha = (Val("lowAlpha") + Val("highAlpha")) / total;   // reflection, calm
        double theta = Val("theta") / total;                            // introspection
        double beta = (Val("lowBeta") + Val("highBeta")) / total;       // impulsivity

        double reflection = alpha + theta;       // 0..1 — moral reasoning capacity
        // Calm/restraint is the backbone of ethical conduct; reflective bands add
        // moral reasoning; impulsive fast-beta dominance erodes restraint.
        double raw = avgMeditation * 0.55 + reflection * 100.0 * 0.45 - beta * 100.0 * 0.15;
        return Math.Clamp(raw, 1, 100);
    }

    public static string Tier(double score) => score switch
    {
        < 40 => "Developing",
        < 60 => "Considerate",
        < 75 => "Principled",
        < 90 => "Conscientious",
        _ => "Exemplary",
    };

    /// <summary>Deterministic opening title slide.</summary>
    public static SlideContent TitleSlide() => new(
        "Ethical AI — Reading the Moral Brain",
        new[]
        {
            "A 10-minute EEG portrait of how — and how much — this brain is ethical",
            "Decoded from focus, calm, the EEG spectrum and the words the brain produced",
            "mindedOS · Ethical AI",
        });

    /// <summary>Deterministic score slide carrying the exact computed percentage.</summary>
    public static SlideContent ScoreSlide(double score, double avgAttention, double avgMeditation, string domKey) => new(
        $"Your Ethical Score: {score:0}% ({Tier(score)})",
        new[]
        {
            $"Ethical potential: {score:0}% on a 0–100% scale",
            $"Restraint / calm (meditation): {avgMeditation:0}/100 — the backbone of ethical conduct",
            $"Focus (attention): {avgAttention:0}/100 — steadies moral attention",
            $"Dominant EEG band: {domKey} — reflective bands raise the score, impulsive fast-beta lowers it",
            "A directional indicator from single-channel consumer EEG — not a moral verdict",
        });

    /// <summary>The eight explanatory slide titles LM Studio fills (after title + score).</summary>
    public static IReadOnlyList<string> BodyTitles { get; } = new[]
    {
        "Why This Brain Is Ethical",
        "Empathy — Feeling Others",
        "Fairness — Weighing Right and Wrong",
        "Restraint — Impulse Control",
        "Conscience — The Inner Check",
        "Reflection — Moral Reasoning",
        "Where the Signals Show It",
        "Summary & Ethical Potential",
    };

    /// <summary>Deterministic fallback bullets for an explanatory slide when LM Studio is offline.</summary>
    private static IReadOnlyList<string> FallbackBullets(string title, double score, string seed) => title switch
    {
        "Why This Brain Is Ethical" => new[]
        {
            "Calm, reflective activity dominates over impulsive fast-beta bursts",
            "Restraint and self-regulation are present rather than reactivity",
            $"The decoded words trace deliberation, not impulse: {Short(seed)}",
        },
        "Empathy — Feeling Others" => new[]
        {
            "Alpha-band calm supports perspective-taking and attunement to others",
            "A regulated state makes room for considering how actions affect people",
        },
        "Fairness — Weighing Right and Wrong" => new[]
        {
            "Steady attention lets the brain weigh competing interests evenly",
            "Reflection bands support holding two sides of a choice in mind",
        },
        "Restraint — Impulse Control" => new[]
        {
            "High meditation/calm indicates the brake on impulsive action is engaged",
            "Lower fast-beta share means fewer reactive, unconsidered urges",
        },
        "Conscience — The Inner Check" => new[]
        {
            "Theta introspection reflects an inward monitor reviewing intentions",
            "The brain checks its own conduct before acting",
        },
        "Reflection — Moral Reasoning" => new[]
        {
            "Alpha + theta together form the substrate of deliberate moral reasoning",
            "Time spent reflecting raises the ethical potential score",
        },
        "Where the Signals Show It" => new[]
        {
            "Ethics is read from calm (meditation), reflective alpha/theta, and low impulsive beta",
            "No single signal is 'the' ethics signal — it emerges from their balance",
        },
        _ => new[]
        {
            $"This brain's ethical potential is {score:0}% ({Tier(score)})",
            "Strengthen it with calm, reflection and deliberate restraint",
            "A directional EEG indicator, not a clinical or moral judgement",
        },
    };

    private static string Short(string seed)
    {
        seed = (seed ?? "").Trim();
        if (seed.Length == 0) return "(no words captured)";
        return seed.Length <= 80 ? seed : seed[..80] + "…";
    }

    /// <summary>
    /// Builds the final, guaranteed 10-slide deck: deterministic title + score
    /// slides, then the eight explanatory slides whose bullets come from the
    /// parsed LM Studio output when available, else the deterministic fallback.
    /// </summary>
    public static List<SlideContent> BuildDeck(double score, double avgAttention, double avgMeditation,
        string domKey, string seed, IReadOnlyList<SlideContent>? lmSlides)
    {
        var deck = new List<SlideContent> { TitleSlide(), ScoreSlide(score, avgAttention, avgMeditation, domKey) };
        var lm = lmSlides ?? Array.Empty<SlideContent>();

        for (int i = 0; i < BodyTitles.Count; i++)
        {
            string title = BodyTitles[i];
            // Pair each fixed title with the matching parsed slide (by order) if it
            // carried real bullets; otherwise fall back to the deterministic bullets.
            IReadOnlyList<string> bullets =
                i < lm.Count && lm[i].Bullets.Count > 0 ? lm[i].Bullets : FallbackBullets(title, score, seed);
            deck.Add(new SlideContent(title, bullets));
        }
        return deck; // exactly 10
    }

    /// <summary>Markdown preview of the deck for the on-screen result box.</summary>
    public static string ToMarkdown(IReadOnlyList<SlideContent> deck)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < deck.Count; i++)
        {
            sb.Append($"## SLIDE {i + 1}: {deck[i].Title}\n");
            foreach (var b in deck[i].Bullets) sb.Append($"- {b}\n");
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
