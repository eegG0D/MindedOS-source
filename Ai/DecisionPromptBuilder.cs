namespace MindedOS.Ai;

/// <summary>
/// Asks LM Studio to make a DECISION about a document on the user's behalf, where
/// the user's intent is expressed through their EEG: the document content + the
/// decoded brain word/state are rewritten into a one-line decision.
/// </summary>
public static class DecisionPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(string fileName, string pdfText, string eegWord, string focusWord)
    {
        string system =
            "You are an executive assistant who makes decisions about documents on behalf of a user whose " +
            "intent is read from their EEG. Given a document's text and the user's live EEG (a decoded " +
            "brain word and a cognitive state), decide what to do with the document. Output EXACTLY ONE " +
            "line in this form:\n" +
            "<DECISION> — <short reason>\n" +
            "where <DECISION> is one of: KEEP, ARCHIVE, DELETE, REVIEW, PRIORITIZE. The EEG brain word and " +
            "state express the user's current focus/intent — let them steer the decision (e.g. PRIORITIZE " +
            "documents that match the brain's focus), but GROUND the reason in the document content. Output " +
            "only that one line, nothing else.";

        string user =
            $"Document: {fileName}\n" +
            "Content (excerpt):\n" +
            (string.IsNullOrWhiteSpace(pdfText) ? "(no extractable text)" : pdfText) +
            $"\n\nUser EEG — brain word: {(string.IsNullOrWhiteSpace(eegWord) ? "(none)" : eegWord)} · state: {focusWord} focus\n\n" +
            "Decide what to do with this document.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
