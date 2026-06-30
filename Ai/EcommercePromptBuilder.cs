using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where the user's EEG drives LM Studio to design an
/// eCommerce concept: a title, an 8-paragraph description and details, and a
/// full list of the store's features / SKUs presented as niches.
/// </summary>
public static class EcommercePromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are an expert eCommerce strategist and copywriter. From a person's EEG-derived condition " +
            "and the words decoded from their brain, design ONE eCommerce business or product line the user " +
            "can launch. The decoded words seed the store's domain, products and brand; the cognitive state " +
            "shapes its character (focus → premium/technical catalog; calm → lifestyle/wellness; stress → " +
            "fast-moving essentials; flow → creative/novelty; drowsy → comfort/home). Output GitHub-flavored " +
            "MARKDOWN with this exact structure:\n" +
            "# <Store/Brand Title>\n\n" +
            "Then a DESCRIPTION AND DETAILS of EXACTLY EIGHT paragraphs (no headings between them):\n" +
            "1. The store concept and the problem it solves for shoppers.\n" +
            "2. The target customer and the market niche.\n" +
            "3. The product catalog and what makes it distinct.\n" +
            "4. Pricing, value proposition and positioning.\n" +
            "5. The storefront experience (UX, search, checkout, mobile).\n" +
            "6. Fulfilment, shipping, inventory and operations.\n" +
            "7. Marketing, acquisition, retention and brand voice.\n" +
            "8. Growth roadmap, risks and the path to profitability.\n\n" +
            "After the eight paragraphs, add a section titled exactly '## Features & SKUs (Niches)' and " +
            "under it a Markdown bullet list of EVERY store feature and product SKU, each written as a " +
            "distinct niche in the form '- <Feature/SKU name> — <one-line niche it serves>'. List them all; " +
            "do not summarise. Output ONLY the title, the eight paragraphs, and that list — no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (the store's concept seeds) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Design this brain's eCommerce store — title, exactly 8 paragraphs of description and details, " +
            "then the full list of features and SKUs as niches — now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
