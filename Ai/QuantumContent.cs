using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Quantum content: the vocabulary/simulations/
/// researcher-comparison CSVs, the dashboard, and fallbacks for the LM artifacts
/// (eight narratives, research paper, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// Education/research framing only.
/// </summary>
public static class QuantumContent
{
    private static readonly (string Term, string Def)[] Glossary =
    {
        ("Qubit", "the basic unit of quantum information; can be 0, 1, or a superposition of both"),
        ("Superposition", "a qubit holding a combination of states at once until measured"),
        ("Entanglement", "a correlation between qubits so one's state depends on another's"),
        ("Interference", "quantum amplitudes adding or cancelling to favor correct answers"),
        ("Quantum Gate", "a reversible operation that transforms qubit states"),
        ("Quantum Circuit", "a sequence of gates applied to qubits to run an algorithm"),
        ("Measurement", "reading a qubit, collapsing its superposition to a classical bit"),
        ("Decoherence", "loss of quantum state from interaction with the environment"),
        ("Grover's Algorithm", "a quantum search giving a quadratic speedup over brute force"),
        ("Shor's Algorithm", "a quantum algorithm that factors integers efficiently"),
        ("Quantum Fourier Transform", "the quantum analog of the discrete Fourier transform"),
        ("Error Correction", "encoding logical qubits across many physical qubits to resist noise"),
        ("Quantum Annealing", "finding low-energy states to solve optimization problems"),
        ("Variational Quantum Eigensolver", "a hybrid method estimating ground-state energies"),
        ("Teleportation", "transferring a qubit state using entanglement and classical bits"),
    };

    private static readonly string[] AiConcepts =
    {
        "qubit", "entanglement", "superposition", "algorithm", "optimization",
        "simulation", "cryptography", "error", "network", "speedup",
    };

    public static string VocabularyCsv()
    {
        var sb = new StringBuilder("term,definition\n");
        foreach (var (term, def) in Glossary) sb.AppendLine($"{term},\"{def}\"");
        return sb.ToString();
    }

    public static string SimulationsCsv()
    {
        var sb = new StringBuilder("domain,project\n");
        sb.AppendLine("Molecules,Simulate a small molecule's ground-state energy");
        sb.AppendLine("Materials,Model a material's electronic structure");
        sb.AppendLine("Optimization,Solve a scheduling or routing problem");
        sb.AppendLine("Economic,Model a portfolio optimization scenario");
        sb.AppendLine("Physical,Simulate a simple spin system");
        return sb.ToString();
    }

    public static string ResearcherComparisonCsv(IReadOnlyList<string> words)
    {
        var user = new HashSet<string>(NlpContent.TopWords(words, 12), StringComparer.OrdinalIgnoreCase);
        var ai = new HashSet<string>(AiConcepts, StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder("concept,source\n");
        foreach (var u in user) sb.AppendLine($"{u},{(ai.Contains(u) ? "shared" : "user")}");
        foreach (var a in ai) if (!user.Contains(a)) sb.AppendLine($"{a},ai");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> scores, IReadOnlyList<QuantumScore> concepts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Quantum Computing Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Score | Value |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in scores) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Concept Distribution");
        sb.AppendLine("| Concept | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var c in concepts.Take(9)) sb.AppendLine($"| {c.Topic} | {c.Percent:0} |");
        return sb.ToString();
    }

    public static string DefaultAlgorithms(IReadOnlyList<QuantumScore> concepts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Quantum Algorithms");
        sb.AppendLine();
        foreach (var (name, purpose) in new[]
        {
            ("Grover-style Search", "search an unstructured space faster than brute force"),
            ("Quantum Optimization", "find low-cost solutions to combinatorial problems"),
            ("Quantum Simulation", "model quantum systems like molecules and materials"),
            ("Quantum Machine Learning", "explore quantum kernels and variational models"),
            ("Quantum Communication", "exchange information using entanglement"),
        })
        {
            sb.AppendLine($"## {name}");
            sb.AppendLine($"- Purpose: {purpose}.");
            sb.AppendLine("- Description: an educational sketch of the idea and its steps.");
            sb.AppendLine("- Advantages: potential speedups or new capabilities over classical methods.");
            sb.AppendLine("- Research opportunities: noise resilience, scaling, and practical encodings.");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string DefaultResearchTopics(IReadOnlyList<QuantumScore> concepts)
    {
        string top = concepts.Count > 0 ? concepts[0].Topic : "quantum computing";
        var sb = new StringBuilder();
        sb.AppendLine("QUANTUM RESEARCH TOPICS");
        sb.AppendLine("=======================");
        sb.AppendLine($"Research questions: what makes {top} practical at scale?");
        sb.AppendLine("Research hypotheses: error rates dominate near-term usefulness.");
        sb.AppendLine("Experiment ideas: benchmark a small algorithm on a simulator.");
        sb.AppendLine("Simulation projects: simulate a few-qubit system end to end.");
        sb.AppendLine("Learning objectives: master the math and one algorithm deeply.");
        return sb.ToString();
    }

    public static string DefaultProblemSolving(IReadOnlyList<QuantumScore> concepts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Quantum Problem Solving");
        sb.AppendLine();
        sb.AppendLine("## Optimization problems");
        sb.AppendLine("- Frame as energy minimization for annealing or variational methods.");
        sb.AppendLine("## Scheduling problems");
        sb.AppendLine("- Encode constraints as penalties in a cost Hamiltonian.");
        sb.AppendLine("## Data analysis challenges");
        sb.AppendLine("- Explore quantum kernels for classification.");
        sb.AppendLine("## Scientific simulations");
        sb.AppendLine("- Simulate molecular ground states.");
        sb.AppendLine("## Computational research problems");
        sb.AppendLine("- Study speedups and their assumptions honestly.");
        return sb.ToString();
    }

    public static string DefaultTheories(IReadOnlyList<QuantumScore> concepts, IReadOnlyList<string> words)
    {
        string top = concepts.Count > 0 ? concepts[0].Topic : "Quantum Computing";
        var sb = new StringBuilder();
        sb.AppendLine("# Quantum Theories (educational)");
        sb.AppendLine();
        sb.AppendLine($"## Title: A {top}-inspired learning model");
        sb.AppendLine("- Description: an educational analogy connecting the EEG concepts to quantum ideas.");
        sb.AppendLine("- Assumptions: simplified, conceptual, non-operational.");
        sb.AppendLine("- Potential applications: teaching intuition for superposition and interference.");
        sb.AppendLine("- Limitations: an analogy only; not a physical claim.");
        sb.AppendLine("- Future research directions: formalize the analogy and test its teaching value.");
        return sb.ToString();
    }

    public static string DefaultArchitectures(IReadOnlyList<QuantumScore> concepts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Quantum Architectures (concept prompts)");
        sb.AppendLine();
        sb.AppendLine("## Quantum processors");
        sb.AppendLine("- A small gate-based processor with error-corrected logical qubits.");
        sb.AppendLine("## Quantum networking systems");
        sb.AppendLine("- Entanglement-distribution nodes linked by quantum channels.");
        sb.AppendLine("## Quantum data centers");
        sb.AppendLine("- Hybrid classical/quantum scheduling of jobs.");
        sb.AppendLine("## Quantum AI platforms");
        sb.AppendLine("- Variational models trained with classical optimizers.");
        sb.AppendLine("## Future computing infrastructures");
        sb.AppendLine("- Fault-tolerant, modular, networked quantum resources.");
        return sb.ToString();
    }

    public static string DefaultAiReport(IReadOnlyList<QuantumScore> concepts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("QUANTUM AI REPORT");
        sb.AppendLine("=================");
        sb.AppendLine("Quantum Computing x AI: quantum kernels and variational circuits.");
        sb.AppendLine("Neural networks: hybrid quantum-classical training loops.");
        sb.AppendLine("Cognitive systems: analogies between superposition and ambiguity.");
        sb.AppendLine("Brain-inspired computing: exploring parallel hypothesis evaluation.");
        return sb.ToString();
    }

    public static string DefaultCurriculum(IReadOnlyList<QuantumScore> concepts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Quantum Curriculum");
        sb.AppendLine();
        sb.AppendLine("## Beginner");
        sb.AppendLine("- Topics: qubits, superposition, gates. Project: simulate one qubit.");
        sb.AppendLine("- Books: an intro text. Papers: a survey. Milestone: run a 1-qubit demo.");
        sb.AppendLine("## Intermediate");
        sb.AppendLine("- Topics: entanglement, Grover, QFT. Project: implement Grover on a simulator.");
        sb.AppendLine("- Books: a standard text. Papers: algorithm papers. Milestone: a small algorithm.");
        sb.AppendLine("## Advanced");
        sb.AppendLine("- Topics: error correction, VQE. Project: a variational solver.");
        sb.AppendLine("- Books: advanced references. Papers: recent research. Milestone: an original study.");
        return sb.ToString();
    }

    public static string DefaultKnowledgeGraph(IReadOnlyList<QuantumScore> concepts)
    {
        var top = concepts.Take(4).Select(c => c.Topic).ToList();
        if (top.Count == 0) top.Add("Quantum Computing");
        var sb = new StringBuilder();
        sb.AppendLine("# Quantum Knowledge Graph");
        sb.AppendLine();
        sb.AppendLine("Relationships between concepts, algorithms, technologies and research areas:");
        sb.AppendLine();
        for (int i = 0; i < top.Count; i++)
        {
            string next = i + 1 < top.Count ? top[i + 1] : "Applications";
            sb.AppendLine($"- **{top[i]}** → connects to → **{next}**");
        }
        sb.AppendLine();
        sb.AppendLine($"Pathway: {string.Join(" → ", top)} → Applications");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<QuantumScore> concepts, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = concepts.Count > 0 ? concepts[0].Topic : "Quantum Computing";
        var conceptWords = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Quantum Computing Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"The EEG concepts map most strongly to {top}, with recurring words {string.Join(", ", conceptWords.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Concept Analysis");
        sb.AppendLine($"Leading concepts: {string.Join(", ", concepts.Take(3).Select(c => $"{c.Topic} {c.Percent:0}%"))}.");
        sb.AppendLine();
        sb.AppendLine("## Learning Profile");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Research Opportunities");
        sb.AppendLine($"Explore practical, noise-aware approaches to {top}.");
        sb.AppendLine();
        sb.AppendLine("## Generated Theories");
        sb.AppendLine("Educational analogies connecting the EEG concepts to quantum intuition.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Start with the fundamentals, simulate small systems, and study one algorithm deeply.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<QuantumScore> concepts, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Concept(int i) => i < concepts.Count ? $"{concepts[i].Topic} ({concepts[i].Percent:0}%)" : "—";
        var conceptWords = NlpContent.TopWords(words, 3);
        string top = concepts.Count > 0 ? concepts[0].Topic : "Quantum Computing";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", conceptWords.Count > 0 ? $"Recurring: {string.Join(", ", conceptWords)}" : "—" }),
            new("Quantum Concepts", new[] { Concept(0), Concept(1), Concept(2) }),
            new("Learning Profile", new[] { "Math / physics / CS interest", "Learning readiness" }),
            new("Algorithm Ideas", new[] { "Search & optimization", "Simulation & ML" }),
            new("Research Topics", new[] { $"Open questions in {top}", "Experiment ideas" }),
            new("Quantum AI", new[] { "Quantum kernels", "Hybrid variational models" }),
            new("Architectures", new[] { "Processors & networks", "Quantum data centers" }),
            new("Future Opportunities", new[] { "Error correction", "Practical applications" }),
            new("Conclusions", new[] { "EEG concepts → quantum learning paths", "Educational exploration" }),
        };
    }
}
