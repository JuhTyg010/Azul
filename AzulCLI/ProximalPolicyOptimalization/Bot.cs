using Azul;
using DeepQLearningBot;
using SaveSystem;

namespace PPO;

public class Bot : IBot {

    private const string NetworkFile = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/policy_network.json";
    private NeuralNetwork _policyNet;
    private int _id;
    private Random _random;
    
    public Bot(int id) {
        
        _policyNet = JsonSaver.Load<NeuralNetwork>(NetworkFile) ?? 
                     new NeuralNetwork(199, 128, 300);
        //TODO: maybe handle that network must exist
        this._id = id;
        _random = new Random();
    }
    public string DoMove(Board board) {
        double[] actionProbs = Softmax(_policyNet.Predict(board.EncodeBoardState(_id)));
        int[] validActions = EncodeMoves(board.GetValidMoves());
        // Mask invalid actions by setting their probability to zero
        for (int i = 0; i < actionProbs.Length; i++)
            if (!validActions.Contains(i)) actionProbs[i] = 0;

        // Normalize probabilities after masking
        double sum = actionProbs.Sum();
        if (sum > 0)
            for (int i = 0; i < actionProbs.Length; i++)
                actionProbs[i] /= sum;

        Move move = DecodeAction(SampleAction(actionProbs));
        if(!board.CanMove(move)) move = board.GetValidMoves()[_random.Next(validActions.Length)];
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
            double cumulative = 0;
            for (int i = 0; i < probs.Length; i++) {
                cumulative += probs[i];
                if (randomValue < cumulative)
                    return i;
            }

            return probs.Length - 1;
        }
}