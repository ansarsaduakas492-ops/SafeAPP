using System.Text;

namespace SafePhrase;

public static class SafePhraseMatcher
{
    public static bool Matches(string? savedPhrase, string? recognizedText)
    {
        string expected = Normalize(savedPhrase);
        return expected.Length > 0 && expected == Normalize(recognizedText);
    }

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        StringBuilder result = new();
        bool pendingSpace = false;
        foreach (char character in text.Normalize(NormalizationForm.FormC))
        {
            if (char.IsPunctuation(character))
                continue;

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = result.Length > 0;
                continue;
            }

            if (pendingSpace)
                result.Append(' ');
            result.Append(char.ToLowerInvariant(character));
            pendingSpace = false;
        }

        return result.ToString();
    }
}
