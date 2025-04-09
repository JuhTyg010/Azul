using Azul;
using DeepQLearningBot;
using SaveSystem;

namespace PPO;

public class Bot : IBot {

    private const int LearnBuffer = 15;
    
    private PPONeuralNetwork _policyNet;
    private int _id;
    private Random _random;
    private int _fromLastLearn = 0;
    
    static List<double[]> states = new List<double[]>();
    static List<int> actions = new List<int>();
    static List<double> rewards = new List<double>();
    static List<double[]> probs = new List<double[]>();
    
    
    
    public Bot(int id) {
        
        _policyNet = new PPONeuralNetwork(88, 128, 300);
        _policyNet.TryLoadPolicy(Trainer.PolicyNetworkPath);
        _policyNet.TryLoadValue(Trainer.ValueNetworkPath);
        
        _id = id;
        _random = new Random();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

    }

    public string DoMove(Board board) {
        var state = board.EncodeBoardState(_id);
        double[] actionProbs = Softmax(_policyNet.PredictPolicy(state));
        int[] validActions = EncodeMoves(board.GetValidMoves());
        // Mask invalid actions by setting their probability to zero
        for (int i = 0; i < actionProbs.Length; i++)
            if (!validActions.Contains(i))
                actionProbs[i] = 0;

        // Normalize probabilities after masking
        double sum = actionProbs.Sum();
        if (sum > 0)
            for (int i = 0; i < actionProbs.Length; i++)
                actionProbs[i] /= sum;

        Move move = DecodeAction(SampleAction(actionProbs));
        var gain = Trainer.GainIfPlayed(move, board);
        for (int i = 0; i < Trainer.sampleCount; i++) {
            var tempMove = DecodeAction(SampleAction(actionProbs));
            var tempGain = Trainer.GainIfPlayed(move, board);
            var tempReward = Trainer.CalculateReward(move, state, board);
            string vals = $"{tempGain} {tempReward}";
            
            File.AppendAllText("/home/juhtyg/Desktop/Azul/gainReward.txt", vals + Environment.NewLine);            
            if (tempGain > gain) {
                gain = tempGain;
                move = tempMove;
            }
        }
        if (!board.CanMove(move)) move = board.GetValidMoves()[_random.Next(validActions.Length)];

        //train mechanism
        states.Add(state);
        actions.Add(EncodeMove(move));
        probs.Add(actionProbs);
        rewards.Add(Trainer.CalculateReward(move, state, board));
        _fromLastLearn++;

        if (_fromLastLearn >= LearnBuffer) {
            _fromLastLearn = 0;
            _policyNet.Train(states, actions, rewards, probs);
            states.Clear();
            actions.Clear();
            probs.Clear();
            rewards.Clear();
        }

    return $"{move.plateId} {move.tileId} {move.bufferId}";
    }

    public string Place(Board board) {
        throw new NotImplementedException();
    }

    public int GetId() {
        return _id;
    }

    public void Result(Dictionary<int, int> result) {
        
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
        
        private static Move DecodeAction(int action) {
            int tileId = action / (10 * 6);
            int plate = (action % (10 * 6)) / 6;
            int buffer = (action % 6);
            return new Move(tileId, plate, buffer);
        }
        

        private double[] Softmax(double[] values) {
            double max = values.Max();
            double sum = values.Sum(v => Math.Exp(v - max));
            return values.Select(v => Math.Exp(v - max) / sum).ToArray();
        }

        private int SampleAction(double[] probs) {
            double randomValue = _random.NextDouble();
            double prob = 0;
            //int bestIndex = 0;
            for (int i = 0; i < probs.Length; i++) {
                prob += probs[i];
                if (prob > randomValue) {
                    return i;
                }
            }
            //Logger.WriteLine($"best value: {best}"); 
            return 0;
        }
        
        private void OnProcessExit(object sender, EventArgs e) {
            _policyNet.SavePolicy(Trainer.PolicyNetworkPath);
            _policyNet.SaveValue(Trainer.ValueNetworkPath);
        }
}