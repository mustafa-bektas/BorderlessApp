using System.Text.Encodings.Web;
using System.Text.Json;

namespace BorderlessApp;

internal sealed class Settings
{
    public const string DefaultHotkey = "Ctrl+Alt+Enter";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // Keep "Ctrl+Alt+Enter" readable instead of "Ctrl+Alt+Enter".
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BorderlessApp", "settings.json");

    /// <summary>Hotkey that toggles borderless fullscreen, e.g. "Ctrl+Alt+Enter" or "Win+Shift+F11".</summary>
    public string Hotkey { get; set; } = DefaultHotkey;

    /// <summary>Loads settings, creating the file with defaults if it doesn't exist. Throws if the file is invalid.</summary>
    public static Settings Load()
    {
        if (!File.Exists(FilePath))
        {
            var defaults = new Settings();
            defaults.Save();
            return defaults;
        }

        return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), JsonOptions) ?? new Settings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
