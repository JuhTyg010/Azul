namespace DeepQLearningBot;

using System;

public class DQNTrainer
{
    private NeuralNetwork policyNet;
    private NeuralNetwork targetNet;
    private ReplayBuffer replayBuffer;

    private int stateSize;
    private int actionSize;
    private double epsilon = 1.0;
    private double epsilonDecay = 0.995;
    private double epsilonMin = 0.01;
    private double gamma = 0.99;

    public DQNTrainer(int stateSize, int actionSize, int replayBufferCapacity)
    {
        this.stateSize = stateSize;
        this.actionSize = actionSize;

        policyNet = new NeuralNetwork( 20, 128,50);
        targetNet = policyNet.Clone(); // Target network is a copy of the policy network
        replayBuffer = new ReplayBuffer(replayBufferCapacity);
    }

    public void TrainEpisode(Func<double[]> getInitialState, Func<double[], int, (double[], double, bool)> step)
    {
        var state = getInitialState();
        bool done = false;

        while (!done)
        {
            // Epsilon-greedy action selection
            int action;
            if (new Random().NextDouble() < epsilon)
            {
                action = new Random().Next(actionSize); // Random action
            }
            else
            {
                var qValues = policyNet.Predict(state);
                action = Array.IndexOf(qValues, Max(qValues)); // Greedy action
            }

            // Perform action and store the experience
            var (nextState, reward, isDone) = step(state, action);
            replayBuffer.Add(state, action, reward, nextState, isDone);
            state = nextState;
            done = isDone;

            // Training
            if (replayBuffer.Count >= 32)
                TrainOnBatch(32);

            // Decay epsilon
            epsilon = Math.Max(epsilonMin, epsilon * epsilonDecay);
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

            // Q-learning target
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

    public void UpdateTargetNetwork()
    {
        targetNet = policyNet.Clone();
    }
}
