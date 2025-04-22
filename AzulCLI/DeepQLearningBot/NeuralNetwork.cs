    using System;

    using System.Text.Json.Serialization;


    namespace DeepQLearningBot;

    public class NeuralNetwork {
        private const double Epsilon = 1e-6; // Small value to prevent NaN in log calculations
        private const double WeightInitRange = 1.0; // Range for weight initialization
        private const int ClampMin = -100;
        private const int ClampMax = 100;
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
            double[] hidden1 = Add(Dot(input, weights1), biases1);
            double[] hidden2 = Add(Dot(hidden1, weightsHidden2), biasesHidden2);
            double[] output = Add(Dot(hidden2, weights2), biases2);
            return output;
        }
        
        public void Train(double[][] states, double[][] targets, double learningRate, double discountFactor) {
            for (int it = 0; it < states.Length; it++) {
                var state = states[it];
                var target = targets[it];

                double[] z1 = Add(Dot(state, weights1), biases1);
                double[] a1 = ReLU(z1);
                double[] z2 = Add(Dot(a1, weightsHidden2), biasesHidden2);
                double[] a2 = ReLU(z2);
                double[] qValues = Add(Dot(a2, weights2), biases2);

                double[] error = new double[qValues.Length];
                for (int action = 0; action < qValues.Length; action++) {
                    error[action] = Clamp(target[action] - qValues[action], ClampMin, ClampMax);
                }

                double[] dOutput = error;

                for (int i = 0; i < weights2.Length; i++) {
                    for (int j = 0; j < weights2[0].Length; j++) {
                        weights2[i][j] += Clamp(learningRate * dOutput[j] * a2[i], ClampMin, ClampMax);
                        weights2[i][j] = Clamp(weights2[i][j], ClampMin, ClampMax);
                    }
                }
                for (int j = 0; j < biases2.Length; j++) {
                    biases2[j] += Clamp(learningRate * dOutput[j], ClampMin, ClampMax);
                    biases2[j] = Clamp(biases2[j], ClampMin, ClampMax);
                }

                double[] dz2 = DotTranspose(weights2, dOutput);
                dz2 = Multiply(dz2, z2, true);

                for (int i = 0; i < weightsHidden2.Length; i++) {
                    for (int j = 0; j < weightsHidden2[0].Length; j++) {
                        weightsHidden2[i][j] += Clamp(learningRate * dz2[j] * a1[i], ClampMin, ClampMax);
                        weightsHidden2[i][j] = Clamp(weightsHidden2[i][j], ClampMin, ClampMax);
                    }
                }
                for (int j = 0; j < biasesHidden2.Length; j++) {
                    biasesHidden2[j] += learningRate * dz2[j];
                    biasesHidden2[j] = Clamp(biasesHidden2[j], ClampMin, ClampMax);
                }

                double[] dz1 = DotTranspose(weightsHidden2, dz2);
                dz1 = Multiply(dz1, z1, true);

                for (int i = 0; i < weights1.Length; i++) {
                    for (int j = 0; j < weights1[0].Length; j++) {
                        weights1[i][j] += Clamp(learningRate * dz1[j] * state[i], ClampMin, ClampMax);
                        weights1[i][j] = Clamp(weights1[i][j], ClampMin, ClampMax);
                    }
                }
                for (int j = 0; j < biases1.Length; j++) {
                    biases1[j] += Clamp(learningRate * dz1[j], ClampMin, ClampMax);
                    biases1[j] = Clamp(biases1[j], ClampMin, ClampMax);
                }
            }
        }
        
        private double Clamp(double value, double min, double max) {
            if (double.IsNaN(value)) {
                return min;
            }
            return Math.Max(min, Math.Min(max, value));
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
