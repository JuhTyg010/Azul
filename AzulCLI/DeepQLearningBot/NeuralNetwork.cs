using System;

using System.Text.Json.Serialization;


namespace DeepQLearningBot;

public class NeuralNetwork {
    private const double Epsilon = 1e-6; // Small value to prevent NaN in log calculations
    private const double WeightInitRange = 1.0; // Range for weight initialization
    private const int ClipMin = -50;
    private const int ClipMax = 50;
    [JsonInclude] private double[][] weights1;
    [JsonInclude] private double[][] weightsHidden2;
    [JsonInclude] private double[][] weights2;
    
    [JsonInclude] private double[] biases1;
    [JsonInclude] private double[] biasesHidden2;
    [JsonInclude] private double[] biases2;
    private Random random = new Random();
    
    [JsonConstructor]
    public NeuralNetwork(double[][] weights1, double[][] weightsHidden2, double[][] weights2,
        double[] biases1, double[] biasesHidden2, double[] biases2) {
        this.weights1 = weights1;
        this.weightsHidden2 = weightsHidden2;
        this.weights2 = weights2;
        this.biases1 = biases1;
        this.biasesHidden2 = biasesHidden2;
        this.biases2 = biases2;
    }
    
    public NeuralNetwork(int inputSize, int hiddenSize1, int hiddenSize2, int outputSize) {
        weights1 = InitializeMatrix(inputSize, hiddenSize1);
        biases1 = InitializeVector(hiddenSize1);
        weightsHidden2 = InitializeMatrix(hiddenSize1, hiddenSize2);
        biasesHidden2 = InitializeVector(hiddenSize2);
        
        weights2 = InitializeMatrix(hiddenSize2, outputSize);
        biases2 = InitializeVector(outputSize);
    }
    public double[] Predict(double[] input) {//ReLU cause there cant be negative probability
        double[] hidden1 = ReLU(Add(Dot(input, weights1), biases1));
        double[] hidden2 = ReLU(Add(Dot(hidden1, weightsHidden2), biasesHidden2));
        double[] output = Add(Dot(hidden2, weights2), biases2);
        return Softmax(output);
    }
    
    public void Train(double[][] inputs, double[][] targets, double learningRate) {
        for (int i = 0; i < inputs.Length; i++) {
            double[] input = inputs[i];
            double[] target = targets[i];
            // Forward pass
            double[] hidden1_pre = Add(Dot(input, weights1), biases1);
            double[] hidden1 = (hidden1_pre);
            
            double[] hidden2_pre = Add(Dot(hidden1, weightsHidden2), biasesHidden2);
            double[] hidden2 = (hidden2_pre);
            double[] output_pre = Add(Dot(hidden2, weights2), biases2);
            double[] probs = (output_pre);
            
            // Cross entropy loss gradient: dL/dz = probs - target
            double[] outputError = Subtract(probs, target);
            double[] hidden2Error = Multiply(DotTranspose(weights2, outputError), hidden2_pre, true);
            double[] hidden1Error = Multiply(DotTranspose(weightsHidden2, hidden2Error), hidden1_pre, true);
            
            UpdateWeightsAndBiases(input, hidden1, hidden2, outputError, hidden2Error, hidden1Error, learningRate);
        }
    }

    public void TrainPPO(double[][] states, int[] actions, double[] advantages,
                         double[][] oldActionProbs, double epsilon, double learningRate) {
        for (int i = 0; i < states.Length; i++) {
            double[] state = states[i];
            int action = actions[i];
            double advantage = advantages[i];
            
            double[] hidden1_pre = Add(Dot(state, weights1), biases1);
            double[] hidden1 = ReLU(hidden1_pre);
            double[] hidden2_pre = Add(Dot(hidden1, weightsHidden2), biasesHidden2);
            double[] hidden2 = ReLU(hidden2_pre);
            
            double[] output_pre = Add(Dot(hidden2, weights2), biases2);
            double[] probs = Softmax(output_pre);//same as in predict
            double logProb = Math.Log(Math.Max(probs[action], Epsilon));
            
            double oldProb = Math.Max(oldActionProbs[i][action], Epsilon);
            double oldLogProb = Math.Log(oldProb);
            
            //-min( r_t * A_t , clip(r_t, 1 − e , 1 + e )A_t) pre loss = -L
            double ratio = Math.Exp(logProb - oldLogProb); // log(a/b) = log(a) - log(b)
            double clippedRatio = Math.Clamp(ratio, 1 - epsilon, 1 + epsilon);
            double safeAdv = Math.Clamp(advantage, ClipMin, ClipMax);
            double gradCoef = -Math.Min(ratio * safeAdv, clippedRatio * safeAdv);
            
            //for support of exploration
            double entropy = -probs.Sum(p => p > 0 ? p * Math.Log(p) : 0);
            double entropyBonus = 0.01 * entropy;
            gradCoef += entropyBonus;
            
            double[] outputError = new double[probs.Length];
            outputError[action] = gradCoef;
            
            double[] hidden2Error = Multiply(DotTranspose(weights2, outputError), hidden2_pre, true);
            double[] hidden1Error = Multiply(DotTranspose(weightsHidden2, hidden2Error), hidden1_pre, true);
            UpdateWeightsAndBiases(state, hidden1, hidden2, outputError, hidden2Error, hidden1Error, learningRate);
        }
    }
    
    private void UpdateWeightsAndBiases(double[] input, double[] hidden1, double[] hidden2,
        double[] outputError, double[] hidden2Error, double[] hidden1Error, double learningRate) {
        
        
        
        for (int i = 0; i < weights2.Length; i++)
            for(int j = 0; j < weights2[0].Length; j++)
                weights2[i][j] -= learningRate * hidden2[i] * outputError[j];
        ClipWeights(weights2, ClipMin, ClipMax);
        for (int i = 0; i < biases2.Length; i++)
            biases2[i] -= learningRate * outputError[i];

        biases2 = Clip(biases2, ClipMin, ClipMax);
        for (int i = 0; i < weightsHidden2.Length; i++)
            for (int j = 0; j < weightsHidden2[0].Length; j++)
                weightsHidden2[i][j] -= learningRate * hidden1[i] * hidden2Error[j];
            
        ClipWeights(weightsHidden2, ClipMin, ClipMax);
        for(int i = 0; i < biasesHidden2.Length; i++)
            biasesHidden2[i] -= learningRate * hidden2Error[i];
        
        biasesHidden2 = Clip(biasesHidden2, ClipMin, ClipMax);
        for (int i = 0; i < weights1.Length; i++)
            for (int j = 0; j < weights1[0].Length; j++)
                weights1[i][j] -= learningRate * input[i] * hidden1Error[j];
        ClipWeights(weights1, ClipMin, ClipMax);
        for (int i = 0; i < biases1.Length; i++)
            biases1[i] -= learningRate * hidden1Error[i];
        biases1 = Clip(biases1, ClipMin, ClipMax);
    }
    
    private double[] Clip(double[] vector, double min, double max) {
        double[] result = new double[vector.Length];
        
        Parallel.For(0, vector.Length, i => {
            result[i] = Math.Clamp(vector[i], min, max);
        });
        return result;
    }

    private void ClipWeights(double[][] weights, double min, double max) {
        for (int i = 0; i < weights.Length; i++)
            for (int j = 0; j < weights[i].Length; j++)
                weights[i][j] = Math.Clamp(weights[i][j], min, max);
    }

    private double[] Dot(double[] vector, double[][] matrix) {
        double[] result = new double[matrix[0].Length];
        for (int j = 0; j < matrix[0].Length; j++)
            for (int i = 0; i < matrix.Length; i++)
                result[j] += vector[i] * matrix[i][j];
        return result;
    }

    private double[] DotTranspose(double[][] matrix, double[] vector) {
        double[] result = new double[matrix.Length];
        for (int i = 0; i < matrix.Length; i++)
            for (int j = 0; j < matrix[0].Length; j++)
                result[i] += matrix[i][j] * vector[j];
        return result;
    }

    private double[] Add(double[] vector, double[] biases) {
        double[] result = new double[vector.Length];

        Parallel.For(0, vector.Length, i => {
            result[i] = vector[i] + biases[i];
        });
        return result;
    }

    private double[] Subtract(double[] vector1, double[] vector2) {
        double[] result = new double[vector1.Length];
        Parallel.For(0, vector1.Length, i => {
            result[i] = vector1[i] - vector2[i];
        });
        return result;
    }

    private double[] Multiply(double[] vector, double[] preActivation, bool derivative = false) {
        double[] result = new double[vector.Length];
    
        Parallel.For(0, vector.Length, i => {
            double grad = derivative ? (preActivation[i] > 0 ? 1 : 0) : 1;
            result[i] = vector[i] * grad;
        });

        return result;
    }

    private double[] Softmax(double[] logits) {
        double max = double.MinValue;
        foreach (var logit in logits)
            if (logit > max) max = logit;

        double sum = 0;
        double[] exp = new double[logits.Length];
        for (int i = 0; i < logits.Length; i++) {
            double shifted = logits[i] - max;
            if (shifted > 700) shifted = 700; // Prevent overflow in exp
            exp[i] = Math.Exp(shifted);
            sum += exp[i];
        }

        if (sum < Epsilon) sum = Epsilon;

        for (int i = 0; i < logits.Length; i++)
            exp[i] /= sum;

        return exp;
    }


    private double[] ReLU(double[] vector) {
        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
            result[i] = Math.Max(0, vector[i]);
        return result;
    }

    private double[][] InitializeMatrix(int rows, int cols) {
        var matrix = new double[rows][];
        double scale = Math.Sqrt(2.0 / rows); // He initialization
        for (int i = 0; i < rows; i++) {
            matrix[i] = new double[cols];
            for (int j = 0; j < cols; j++) {
                matrix[i][j] = random.NextDouble() * 2 - 1;
                matrix[i][j] *= scale;
            }
        }
        return matrix;
    }


    private double[] InitializeVector(int size) {
        var vector = new double[size];
        for (int i = 0; i < size; i++)
            vector[i] = random.NextDouble() * 2 - 1;
        return vector;
    }

    private double[] Activate(double[] vector) {
        for (int i = 0; i < vector.Length; i++)
            vector[i] = Math.Max(0, vector[i]);
        return vector;
    }

    public NeuralNetwork Clone() {
        var clone = new NeuralNetwork(weights1.Length, biases1.Length, biasesHidden2.Length, biases2.Length);

        clone.weights1 = CloneMatrix(weights1);
        clone.biases1 = (double[])biases1.Clone();

        clone.weightsHidden2 = CloneMatrix(weightsHidden2);
        clone.biasesHidden2 = (double[])biasesHidden2.Clone();

        clone.weights2 = CloneMatrix(weights2);
        clone.biases2 = (double[])biases2.Clone();

        return clone;
    }

    private double[][] CloneMatrix(double[][] matrix) {
        double[][] result = new double[matrix.Length][];
        for (int i = 0; i < matrix.Length; i++) {
            result[i] = new double[matrix[i].Length];
            Array.Copy(matrix[i], result[i], matrix[i].Length);
        }
        return result;
    }

    public double[][] GetWeights1() => weights1;
    public double[][] GetWeightsHidden2() => weightsHidden2;
    public double[][] GetWeights2() => weights2;

    public double[] GetBiases1() => biases1;
    public double[] GetBiasesHidden2() => biasesHidden2;
    public double[] GetBiases2() => biases2;

    public void SetWeights1(double[][] w) => weights1 = w;
    public void SetWeightsHidden2(double[][] w) => weightsHidden2 = w;
    public void SetWeights2(double[][] w) => weights2 = w;

    public void SetBiases1(double[] b) => biases1 = b;
    public void SetBiasesHidden2(double[] b) => biasesHidden2 = b;
    public void SetBiases2(double[] b) => biases2 = b;
}
