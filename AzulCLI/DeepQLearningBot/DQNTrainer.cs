namespace DeepQLearningBot;

using System;

public class DQNTrainer
{
    private NeuralNetwork policyNet;
    private NeuralNetwork targetNet;
    private ReplayBuffer replayBuffer;
    
    private string fileName = "DQNTrainer";
    
    private int stateSize;
    private int actionSize;
    private double epsilon = 1.0;
    private double epsilonDecay = 0.995;
    private double epsilonMin = 0.01;
    private double gamma = 0.99;
    
    private int episodeCount = 0;
    private const int AutoSaveInterval = 10;

    public DQNTrainer(DQNSetting setting)
    {
        this.stateSize = setting.stateSize;
        this.actionSize = setting.actionSize;

        policyNet = new NeuralNetwork(stateSize, 128, actionSize);
        targetNet = policyNet.Clone();
        replayBuffer = new ReplayBuffer(setting.replayBufferCapacity);


        if (ModelSaver.Load(policyNet)) {
            targetNet = policyNet.Clone();
            episodeCount = targetNet.episodeCount;
        }

    }

    public void TrainEpisode(Func<double[]> getInitialState, Func<double[], int, (double[], double, bool)> step)
    {
        var state = getInitialState();
        bool done = false;

        while (!done) {
            
            int action;
            if (new Random().NextDouble() < epsilon) {
                action = new Random().Next(actionSize); // Random action
            }
            else {
                var qValues = policyNet.Predict(state);
                action = Array.IndexOf(qValues, Max(qValues)); // Greedy action
            }

            var (nextState, reward, isDone) = step(state, action);
            replayBuffer.Add(state, action, reward, nextState, isDone);
            state = nextState;
            done = isDone;

            if (replayBuffer.Count >= 32)
                TrainOnBatch(32);

            epsilon = Math.Max(epsilonMin, epsilon * epsilonDecay);
        }

        // Auto-save model every N episodes
        episodeCount++;
        if (episodeCount % AutoSaveInterval == 0) {
            ModelSaver.Save(policyNet, fileName + episodeCount + ".json");
        }
    }

    private void TrainOnBatch(int batchSize)
    {
        var batch = replayBuffer.Sample(batchSize);
        var states = new double[batchSize][];
        var targets = new double[batchSize][];

        for (int i = 0; i < batchSize; i++)
        {
            var (state, action, reward, nextState, done) = batch[i];
            var qValues = policyNet.Predict(state);
            var nextQValues = targetNet.Predict(nextState);

            // 🔹 Compute Q-learning target
            qValues[action] = reward + (done ? 0 : gamma * Max(nextQValues));

            states[i] = state;
            targets[i] = qValues;
        }

        policyNet.Train(states, targets, 0.001);
    }

    private double Max(double[] values)
    {
        double max = double.MinValue;
        foreach (var value in values)
            if (value > max)
                max = value;
        return max;
    }
    
    public double[] Predict(double[] state)
    {
        return policyNet.Predict(state);
    }

    public void UpdateTargetNetwork()
    {
        targetNet = policyNet.Clone();
    }
}
