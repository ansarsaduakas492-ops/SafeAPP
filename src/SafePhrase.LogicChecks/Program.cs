using SafePhrase;

var cases = new (string? Saved, string? Heard, bool Expected)[]
{
    ("Call Aunty Aigul", "call aunty aigul", true),
    ("Help me please", "  HELP,\tme   please!  ", true),
    ("Help me please", "help me", false),
    ("Help me please", "do not help me please", false),
    ("help", "shelf", false),
    ("", "", false),
    ("!!!", "...", false),
    (null, "help", false),
    ("help", null, false),
    ("Позови тётю", "ПОЗОВИ ТЁТЮ!", true),
    ("caf\u00e9", "cafe\u0301", true)
};

foreach (var test in cases)
{
    if (SafePhraseMatcher.Matches(test.Saved, test.Heard) != test.Expected)
        throw new InvalidOperationException($"Phrase matching failed: {test.Saved} / {test.Heard}");
}

Console.WriteLine($"Passed {cases.Length} phrase matching checks.");
