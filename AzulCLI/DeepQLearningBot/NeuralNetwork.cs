using System;

namespace DeepQLearningBot;

public class NeuralNetwork
{
    private double[][] weights1; // Input to hidden layer
    private double[][] weights2; // Hidden layer to output
    private double[] biases1;    // Hidden layer biases
    private double[] biases2;    // Output layer biases

    private Random random = new Random();

    public NeuralNetwork(double[][] weights1, double[][] weights2, double[] biases1, double[] biases2) {
        this.weights1 = weights1;
        this.weights2 = weights2;
        this.biases1 = biases1;
        this.biases2 = biases2;
    }
    public NeuralNetwork(int inputSize, int hiddenSize, int outputSize)
    {
        // Initialize weights and biases with random values
        weights1 = InitializeMatrix(inputSize, hiddenSize);
        weights2 = InitializeMatrix(hiddenSize, outputSize);
        biases1 = InitializeVector(hiddenSize);
        biases2 = InitializeVector(outputSize);
    }

    // Forward pass: Compute outputs from inputs
    public double[] Predict(double[] input)
    {
        double[] hidden = Activate(Add(Dot(input, weights1), biases1));
        return Add(Dot(hidden, weights2), biases2);
    }

    // Backward pass: Update weights and biases
    public void Train(double[][] inputs, double[][] targets, double learningRate)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            // For each input in the batch, perform forward pass and backpropagation
            var input = inputs[i];
            var target = targets[i];

            // Forward pass
            double[] hidden = Activate(Add(Dot(input, weights1), biases1));
            double[] output = Add(Dot(hidden, weights2), biases2);

            // Calculate errors
            double[] outputError = Subtract(target, output); // Error at output
            double[] hiddenError = Multiply(DotTranspose(weights2, outputError), hidden, derivative: true);

            // Update weights and biases
            UpdateWeightsAndBiases(input, hidden, outputError, hiddenError, learningRate);
        }
    }


    // Helper functions
    private double[][] InitializeMatrix(int rows, int cols)
    {
        double[][] matrix = new double[rows][];
        for (int i = 0; i < rows; i++)
        {
            matrix[i] = new double[cols];
            for (int j = 0; j < cols; j++)
                matrix[i][j] = random.NextDouble() * 2 - 1; // Random values between -1 and 1
        }
        return matrix;
    }

    private double[] InitializeVector(int size)
    {
        double[] vector = new double[size];
        for (int i = 0; i < size; i++)
            vector[i] = random.NextDouble() * 2 - 1;
        return vector;
    }

    private double[] Dot(double[] vector, double[][] matrix)
    {
        double[] result = new double[matrix[0].Length];
        for (int j = 0; j < matrix[0].Length; j++)
            for (int i = 0; i < matrix.Length; i++)
                result[j] += vector[i] * matrix[i][j];
        return result;
    }

    private double[] DotTranspose(double[][] matrix, double[] vector)
    {
        double[] result = new double[matrix.Length];
        for (int i = 0; i < matrix.Length; i++)
            for (int j = 0; j < matrix[0].Length; j++)
                result[i] += matrix[i][j] * vector[j];
        return result;
    }

    private double[] Add(double[] vector, double[] biases)
    {
        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; i++)
            result[i] = vector[i] + biases[i];
        return result;
    }

    private double[] Activate(double[] vector)
    {
        for (int i = 0; i < vector.Length; i++)
            vector[i] = Math.Max(0, vector[i]); // ReLU activation
        return vector;
    }

    private double[] Subtract(double[] vector1, double[] vector2)
    {
        double[] result = new double[vector1.Length];
        for (int i = 0; i < vector1.Length; i++)
            result[i] = vector1[i] - vector2[i];
        return result;
    }

    private double[] Multiply(double[] vector1, double[] vector2, bool derivative = false)
    {
        double[] result = new double[vector1.Length];
        for (int i = 0; i < vector1.Length; i++)
            result[i] = derivative ? vector1[i] * (vector2[i] > 0 ? 1 : 0) : vector1[i] * vector2[i];
        return result;
    }

    private void UpdateWeightsAndBiases(double[] input, double[] hidden, double[] outputError, double[] hiddenError, double learningRate)
    {
        for (int i = 0; i < weights2.Length; i++)
            for (int j = 0; j < weights2[0].Length; j++)
                weights2[i][j] += learningRate * hidden[i] * outputError[j];

        for (int i = 0; i < biases2.Length; i++)
            biases2[i] += learningRate * outputError[i];

        for (int i = 0; i < weights1.Length; i++)
            for (int j = 0; j < weights1[0].Length; j++)
                weights1[i][j] += learningRate * input[i] * hiddenError[j];

        for (int i = 0; i < biases1.Length; i++)
            biases1[i] += learningRate * hiddenError[i];
    }
    
    public NeuralNetwork Clone()
    {
        var clonedNetwork = new NeuralNetwork(weights1.Length, biases1.Length, biases2.Length);

        // Deep copy weights1
        clonedNetwork.weights1 = new double[weights1.Length][];
        for (int i = 0; i < weights1.Length; i++)
        {
            clonedNetwork.weights1[i] = new double[weights1[i].Length];
            Array.Copy(weights1[i], clonedNetwork.weights1[i], weights1[i].Length);
        }

        // Deep copy weights2
        clonedNetwork.weights2 = new double[weights2.Length][];
        for (int i = 0; i < weights2.Length; i++)
        {
            clonedNetwork.weights2[i] = new double[weights2[i].Length];
            Array.Copy(weights2[i], clonedNetwork.weights2[i], weights2[i].Length);
        }

        // Deep copy biases1
        clonedNetwork.biases1 = new double[biases1.Length];
        Array.Copy(biases1, clonedNetwork.biases1, biases1.Length);

        // Deep copy biases2
        clonedNetwork.biases2 = new double[biases2.Length];
        Array.Copy(biases2, clonedNetwork.biases2, biases2.Length);

        return clonedNetwork;
    }

    public double[] GetBiases1() {
        return biases1;
    }

    public double[] GetBiases2() {
        return biases2;
    }

    public double[][] GetWeights1() {
        return weights1;
    }

    public double[][] GetWeights2() {
        return weights2;
    }
    
    public void SetBiases1(double[] biases1) {
        this.biases1 = biases1;
    }

    public void SetBiases2(double[] biases2) {
        this.biases2 = biases2;
    }

    public void SetWeights1(double[][] weights1) {
        this.weights1 = weights1;
    }

    public void SetWeights2(double[][] weights2) {
        this.weights2 = weights2;
    }
}
