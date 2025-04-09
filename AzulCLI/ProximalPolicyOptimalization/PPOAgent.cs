using DeepQLearningBot;
using Azul;

namespace PPO {


    public class PPOAgent {
        private PPONeuralNetwork policyNet;
        //private NeuralNetwork valueNet;
        private Random random;
        public double gamma { get; private set; } = 0.9;
        private double epsilon = 0.3;
        private double learningRate = 0.001;

        public PPOAgent(int stateSize, int actionSize) {
            policyNet = new PPONeuralNetwork(stateSize, 128, actionSize, epsilon, learningRate);
            //valueNet = new NeuralNetwork(stateSize, 128, 1);
            random = new Random();
        }

        public (int, double[]) SelectAction(double[] state, Move[] validMoves) {
            double[] actionProbs = Softmax(policyNet.PredictPolicy(state));
            int[] validActions = EncodeMoves(validMoves);
            // Mask invalid actions by setting their probability to zero
            for (int i = 0; i < actionProbs.Length; i++)
                if (!validActions.Contains(i)) actionProbs[i] = 0;

            // Normalize probabilities after masking
            double sum = actionProbs.Sum();
            if (sum > 0)
                for (int i = 0; i < actionProbs.Length; i++)
                    actionProbs[i] /= sum;

            return (SampleAction(actionProbs), actionProbs);
        }

        private int[] EncodeMoves(Move[] moves) {
            List<int> result = new List<int>();
            foreach (Move move in moves) {
                result.Add(EncodeMove(move));
            }
            return result.ToArray();
        }

        private int EncodeMove(Move move) {
            int actionId = move.bufferId;
            actionId += 60 * move.tileId;
            actionId += 6 * move.plateId;
            return actionId;
        }

        public void Train(List<double[]> states, List<int> actions, List<double> rewards, List<double[]> oldProbs) {

            policyNet.Train(states, actions, rewards, oldProbs);
            //SaveSystem.JsonSaver.Save(policyNet, "/home/juhtyg/Desktop/Azul/AI_Data/PPO/network.json");
        }

        public void SavePolicy(string path) {
            policyNet.SavePolicy(path);
        }

        public void SaveValue(string path) {
            policyNet.SaveValue(path);
        }
        
        private List<double[]> ComputeReturns(List<double> rewards) {
            List<double[]> returns = new List<double[]>();
            double G = 0;
            for (int i = rewards.Count - 1; i >= 0; i--) {
                G = rewards[i] + gamma * G;
                returns.Insert(0, new double[] { G }); 
            }
            return returns;
        }
        
        private double[] NormalizeValues(double[] values) {
            double mean = values.Average();
            double variance = values.Select(x => Math.Pow(x - mean, 2)).Average();
            double stdDev = Math.Sqrt(variance + 1e-8); // Add small value to avoid division by zero

            return values.Select(x => (x - mean) / stdDev).ToArray();
        }
        
        private double[] Softmax(double[] values, double temperature = 1.0) {
            values = NormalizeValues(values);
            if (temperature <= 0) throw new ArgumentException("Temperature must be positive.");

            double maxLogit = values.Max();
            double[] expValues = values.Select(v => Math.Exp((v - maxLogit) / temperature)).ToArray();
    
            double sumExp = expValues.Sum();
            return expValues.Select(v => v / sumExp).ToArray();
        }

        private int SampleAction(double[] probs) {
            double commutative = random.NextDouble();
            double prob = 0;
            for (int i = 0; i < probs.Length; i++) {
                prob += probs[i];
                if (prob > commutative) {
                    return i;
                }
            }

            return 0;
        }
    }
}