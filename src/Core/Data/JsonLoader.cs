using System.IO;
using System.Text.Json;

namespace Nyvorn.Data;

public static class JsonLoader
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static T LoadFromContent<T>(string relativePath)
    {
        // relativePath tipo: "Data/player.json"
        string full = Path.Combine("Content", relativePath);
        string json = File.ReadAllText(full);
        return JsonSerializer.Deserialize<T>(json, _opts)!;
    }
}
