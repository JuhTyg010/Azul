using System.Text.Json.Serialization;

namespace DeepQLearningBot;

public class DQNSetting
{
    public int ActionSize { get; set; } //300
    public int StateSize { get; set; }
    public int ReplayBufferCapacity { get; set; }
    public int BatchSize { get; set; }
    
    public int FromLastBatch { get; set; }
    public double Epsilon { get; set; }
    public double EpsilonDecay { get; set; }
    public double EpsilonMin { get; set; }
    public double Gamma { get; set; }

    [JsonConstructor]
    public DQNSetting(int actionSize, int stateSize, int replayBufferCapacity, int batchSize, double epsilon, double epsilonDecay, double epsilonMin, double gamma)
    {
        ActionSize = actionSize;
        StateSize = stateSize;
        ReplayBufferCapacity = replayBufferCapacity;
        BatchSize = batchSize;
        Epsilon = epsilon;
        EpsilonDecay = epsilonDecay;
        EpsilonMin = epsilonMin;
        Gamma = gamma;
    }
}