using System;
using System.Collections.Generic;
using System.Linq;

namespace MindedOS.Engine;

/// <summary>Coarse part-of-speech classes used by <see cref="RightSpeech"/>.</summary>
public enum PosClass
{
    Determiner, Pronoun, Adjective, Noun, Verb, Adverb,
    Preposition, Conjunction, Interjection, Number, Other,
}

/// <summary>
/// Deterministic, offline part-of-speech tagger: a small closed-class word list
/// (finite, stable) plus suffix heuristics for open-class words. No data file,
/// no I/O. Unknown words default to <see cref="PosClass.Noun"/>.
/// </summary>
public static class PosTagger
{
    private static readonly HashSet<string> Determiners = new(StringComparer.OrdinalIgnoreCase)
    { "the","a","an","this","that","these","those","my","your","his","her","its","our","their","some","any","no","every","each" };

    private static readonly HashSet<string> Pronouns = new(StringComparer.OrdinalIgnoreCase)
    { "i","you","he","she","it","we","they","me","him","them","us","who","what","which","mine","yours","hers","ours","theirs" };

    private static readonly HashSet<string> Prepositions = new(StringComparer.OrdinalIgnoreCase)
    { "of","in","on","at","to","for","with","from","by","about","as","into","over","under","through","between","against","without","within","upon","across","behind","beyond","near" };

    private static readonly HashSet<string> Conjunctions = new(StringComparer.OrdinalIgnoreCase)
    { "and","or","but","so","yet","nor","because","although","while","if","when","than","whether" };

    private static readonly HashSet<string> Verbs = new(StringComparer.OrdinalIgnoreCase)
    { "is","are","was","were","be","been","am","do","does","did","has","have","had","will","would","can","could","should","may","might","must","shall" };

    private static readonly HashSet<string> Interjections = new(StringComparer.OrdinalIgnoreCase)
    { "oh","ah","hey","wow","ouch","hmm","yes","okay","ok","alas","hello","hi" };

    public static PosClass Tag(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return PosClass.Other;
        var w = word.Trim();

        // Closed classes; lookup order resolves overlaps (e.g. "this") deterministically.
        if (Determiners.Contains(w)) return PosClass.Determiner;
        if (Pronouns.Contains(w)) return PosClass.Pronoun;
        if (Prepositions.Contains(w)) return PosClass.Preposition;
        if (Conjunctions.Contains(w)) return PosClass.Conjunction;
        if (Verbs.Contains(w)) return PosClass.Verb;
        if (Interjections.Contains(w)) return PosClass.Interjection;

        if (w.All(char.IsDigit)) return PosClass.Number;

        var lower = w.ToLowerInvariant();
        if (lower.EndsWith("ly")) return PosClass.Adverb;
        if (lower.EndsWith("ing") || lower.EndsWith("ed")) return PosClass.Verb;
        if (lower.EndsWith("ous") || lower.EndsWith("ful") || lower.EndsWith("ive")
            || lower.EndsWith("al") || lower.EndsWith("ic") || lower.EndsWith("able")
            || lower.EndsWith("ible")) return PosClass.Adjective;

        return PosClass.Noun;
    }
}
