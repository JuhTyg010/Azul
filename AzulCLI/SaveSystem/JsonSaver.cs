using System.Text.Json;

namespace SaveSystem;

public static class JsonSaver {
    
    public static void Save<T>(T instance, string filePath, string name = null) where T : struct {
    
        string json = JsonSerializer.Serialize(instance, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        Console.WriteLine($"[SaveSystem] {name} saved to {filePath}");
    }

    public static T Load<T>(string filePath) where T : struct {

        if (!File.Exists(filePath)) {
            Console.WriteLine("[SaveSystem] No saved model found.");
            return default;
        }

        try {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<T>(json);
                Console.WriteLine($"[SaveSystem] Model loaded from {filePath}");
                return data;
        }
        catch (Exception ex) {
            Console.WriteLine($"[SaveSystem] Failed to load model: {ex.Message}");
        }
        return default;
    }
}