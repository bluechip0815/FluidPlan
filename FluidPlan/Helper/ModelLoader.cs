// Create a new file: ModelLoader.cs
using System.Text.Json;

namespace FluidSimu
{
    public static class ModelLoader
    {
        public static T LoadJson<T>(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? throw new InvalidOperationException($"Could not deserialize {path}");
        }
    }
}