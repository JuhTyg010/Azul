
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using DeepQLearningBot;

namespace PPO;

public class PPONeuralNetwork {
    [JsonInclude] private NeuralNetwork policyNet;
    [JsonInclude] private NeuralNetwork valueNet;
    [JsonInclude] private double epsilon;
    [JsonInclude] private double learningRate;

    
    [JsonConstructor]
    public PPONeuralNetwork(int inputSize, int hiddenSize, int actionSize, double epsilon = 0.2, double learningRate = 0.001) {
        this.policyNet = new NeuralNetwork(inputSize, hiddenSize, actionSize);
        this.valueNet = new NeuralNetwork(inputSize, hiddenSize, 1);
        this.epsilon = epsilon;
        this.learningRate = learningRate;
    }

    public PPONeuralNetwork(string policyPath, string valuePath, double epsilon = 0.2, double learningRate = 0.001) {
        this.policyNet = SaveSystem.JsonSaver.Load<NeuralNetwork>(policyPath) ??
                         throw new FileNotFoundException("Could not load policy");
        this.valueNet = SaveSystem.JsonSaver.Load<NeuralNetwork>(valuePath) ??
                        throw new FileNotFoundException("Could not load value");
        this.epsilon = epsilon;
        this.learningRate = learningRate;
    }

    public double[] PredictPolicy(double[] state) => policyNet.Predict(state);
    public double PredictValue(double[] state) => valueNet.Predict(state)[0];

    public void Train(List<double[]> states, List<int> actions, List<double> rewards, List<double[]> oldActionProbs) {
        var returns = ComputeReturns(rewards);
        var advantages = ComputeAdvantages(states, returns);

        double[][] stateArray = states.ToArray();
        int[] actionArray = actions.ToArray();
        double[] advantageArray = advantages;
        double[][] oldProbsArray = oldActionProbs.ToArray();

        policyNet.TrainPPO(stateArray, actionArray, advantageArray, oldProbsArray, epsilon, learningRate);

        double[][] returnTargets = new double[returns.Count][];
        for (int i = 0; i < returns.Count; i++) {
            returnTargets[i] = returns[i];
        }

        valueNet.Train(stateArray, returnTargets, learningRate);
    }

    private List<double[]> ComputeReturns(List<double> rewards, double gamma = 0.99) {
        List<double[]> returns = new List<double[]>();
        double G = 0;
        for (int i = rewards.Count - 1; i >= 0; i--) {
            G = rewards[i] + gamma * G;
            returns.Insert(0, new double[] { G });
        }
        return returns;
    }

    private double[] ComputeAdvantages(List<double[]> states, List<double[]> returns) {
        double[] advantages = new double[states.Count];
        for (int i = 0; i < states.Count; i++) {
            double predictedValue = valueNet.Predict(states[i])[0];
            advantages[i] = returns[i][0] - predictedValue;
        }
        return Normalize(advantages);
    }

    private double[] Normalize(double[] values) {
        double mean = 0, std = 1e-8;
        foreach (var v in values) mean += v;
        mean /= values.Length;

        foreach (var v in values) std += Math.Pow(v - mean, 2);
        std = Math.Sqrt(std / values.Length);

        double[] normalized = new double[values.Length];
        for (int i = 0; i < values.Length; i++) {
            normalized[i] = (values[i] - mean) / std;
        }

        return normalized;
    }

    public void SavePolicy(string path) => SaveSystem.JsonSaver.Save(policyNet, path);
    public void SaveValue(string path) => SaveSystem.JsonSaver.Save(valueNet, path);

    public NeuralNetwork GetPolicyNet() => policyNet;
    public NeuralNetwork GetValueNet() => valueNet;

    public void TryLoadPolicy(string path) {
        policyNet = SaveSystem.JsonSaver.Load<NeuralNetwork>(path);
    }

    public void TryLoadValue(string path) {
        valueNet = SaveSystem.JsonSaver.Load<NeuralNetwork>(path);
    }
}
