using System.Text.Json;

namespace DeepQLearningBot;

public static class ModelSaver {
     private static string defaultPath = "DQN_Model.json";

        // 🔹 Save the network to a JSON file
        public static void Save(NeuralNetwork network, string filePath = null)
        {
            filePath ??= defaultPath;

            var modelData = new
            {
                weights1 = network.GetWeights1(),
                weights2 = network.GetWeights2(),
                biases1 = network.GetBiases1(),
                biases2 = network.GetBiases2()
            };

            string json = JsonSerializer.Serialize(modelData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
            Console.WriteLine($"[ModelSaver] Model saved to {filePath}");
        }

        // 🔹 Load the network from a JSON file
        public static bool Load(NeuralNetwork network, string filePath = null)
        {
            filePath ??= defaultPath;

            if (!File.Exists(filePath))
            {
                Console.WriteLine("[ModelSaver] No saved model found.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var modelData = JsonSerializer.Deserialize<ModelData>(json);

                if (modelData != null)
                {
                    network.SetWeights1(modelData.weights1);
                    network.SetWeights2(modelData.weights2);
                    network.SetBiases1(modelData.biases1);
                    network.SetBiases2(modelData.biases2);
                    Console.WriteLine($"[ModelSaver] Model loaded from {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModelSaver] Failed to load model: {ex.Message}");
            }

            return false;
        }

        private class ModelData
        {
            public double[][] weights1 { get; set; }
            public double[][] weights2 { get; set; }
            public double[] biases1 { get; set; }
            public double[] biases2 { get; set; }
        }
}