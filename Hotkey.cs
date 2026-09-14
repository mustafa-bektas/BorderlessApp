using static BorderlessApp.NativeMethods;

namespace BorderlessApp;

/// <summary>A global hotkey parsed from text such as "Ctrl+Alt+Enter".</summary>
internal readonly record struct Hotkey(uint Modifiers, Keys Key, string Text)
{
    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        uint modifiers = 0;
        Keys? key = null;

        var parts = (text ?? "").Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control":
                    modifiers |= MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= MOD_ALT;
                    break;
                case "shift":
                    modifiers |= MOD_SHIFT;
                    break;
                case "win" or "windows":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    if (key is not null || !TryParseKey(part, out var parsed))
                        return false;
                    key = parsed;
                    break;
            }
        }

        if (key is null)
            return false;

        hotkey = new Hotkey(modifiers, key.Value, string.Join("+", parts));
        return true;
    }

    private static bool TryParseKey(string text, out Keys key)
    {
        // Let "1" mean the 1 key rather than the enum value 1.
        if (text.Length == 1 && char.IsAsciiDigit(text[0]))
        {
            key = Keys.D0 + (text[0] - '0');
            return true;
        }

        return Enum.TryParse(text, ignoreCase: true, out key)
               && !int.TryParse(text, out _)
               && key != Keys.None
               && (key & Keys.Modifiers) == 0;
    }
}
