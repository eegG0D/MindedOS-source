using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic, lexicon-locked reordering of brain-decoded words into speakable
/// English. Words are bucketed by part of speech and poured into a clause template;
/// every input word is used exactly once and nothing is invented. The output multiset
/// equals the input multiset (case/punctuation aside). No LM Studio, no I/O.
/// </summary>
public static class RightSpeech
{
    // Each slot lists candidate classes in preference order; the slot pops from the
    // first candidate that has a word available, else is skipped.
    private static readonly PosClass[][] Template =
    {
        new[] { PosClass.Interjection },
        new[] { PosClass.Conjunction },
        new[] { PosClass.Determiner },
        new[] { PosClass.Adjective },
        new[] { PosClass.Pronoun, PosClass.Noun }, // subject
        new[] { PosClass.Adverb },
        new[] { PosClass.Verb },
        new[] { PosClass.Determiner },
        new[] { PosClass.Adjective },
        new[] { PosClass.Noun },                   // object
        new[] { PosClass.Preposition },
        new[] { PosClass.Determiner },
        new[] { PosClass.Noun },                   // object of preposition
    };

    public static string Speak(IEnumerable<string> words)
    {
        var queues = new Dictionary<PosClass, Queue<string>>();
        foreach (PosClass c in Enum.GetValues(typeof(PosClass))) queues[c] = new Queue<string>();

        foreach (var raw in words)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var w = raw.Trim();
            queues[PosTagger.Tag(w)].Enqueue(w);
        }

        var referenced = Template.SelectMany(s => s).Distinct().ToArray();
        bool AnyReferenced() => referenced.Any(c => queues[c].Count > 0);

        var clauses = new List<List<string>>();

        // Build one clause per pass until every template-referenced bucket is empty.
        while (AnyReferenced())
        {
            var clause = new List<string>();
            foreach (var slot in Template)
                foreach (var c in slot)
                    if (queues[c].Count > 0) { clause.Add(queues[c].Dequeue()); break; }
            if (clause.Count > 0) clauses.Add(clause);
        }

        // Drain anything with no slot (Number, Other) so no word is lost.
        var leftover = new List<string>();
        foreach (PosClass c in Enum.GetValues(typeof(PosClass)))
            while (queues[c].Count > 0) leftover.Add(queues[c].Dequeue());
        if (leftover.Count > 0) clauses.Add(leftover);

        var sb = new StringBuilder();
        foreach (var clause in clauses)
        {
            if (clause.Count == 0) continue;
            var text = string.Join(' ', clause);
            text = char.ToUpperInvariant(text[0]) + text[1..];
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(text).Append('.');
        }
        return sb.ToString();
    }
}
