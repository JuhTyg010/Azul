using Azul;
using DeepQLearningBot;
using SaveSystem;

namespace PPO;

public class Bot : IBot {

    private const string NetworkFile = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/policy_network.json";
    private const string ValueNetwork = "/home/juhtyg/Desktop/Azul/AI_Data/PPO/value_network.json";

    private const string RewardAveragePath = "/home/juhtyg/Desktop/Azul/AzulCLI/reward_avg.txt";
    private const int LearnBuffer = 100;
    
    private readonly NeuralNetwork _policyNet;
    private readonly NeuralNetwork _valueNet;

    private readonly int _id;
    private Random _random;
    private int _fromLastLearn = 0;

    private double _gamma = 0.99;
    
    static List<double[]> _states = new List<double[]>();
    static List<int> _actions = new List<int>();
    static List<double> _rewards = new List<double>();
    static List<double[]> _probs = new List<double[]>();
    
    
    
    public Bot(int id) {
        
        _policyNet = JsonSaver.Load<NeuralNetwork>(NetworkFile) ?? 
                     new NeuralNetwork(59, 256, 256, 300);
        //TODO: maybe handle that network must exist
        
        _valueNet = JsonSaver.Load<NeuralNetwork>(ValueNetwork) ??
                    new NeuralNetwork(59, 256, 256, 1);
        
        this._id = id;
        _random = new Random();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit!;

    }

    public string DoMove(Board board) {
        var state = board.EncodeBoardState(_id);
        double[] actionProbs = _policyNet.Predict(state);
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

        Move move = Trainer.DecodeAction(SampleAction(actionProbs));
        
        if (!board.CanMove(move)) move = board.GetValidMoves()[_random.Next(validActions.Length)];

        //train mechanism
        _states.Add(state);
        _actions.Add(Trainer.EncodeMove(move));
        _probs.Add(actionProbs);
        _rewards.Add(Trainer.CalculateReward(move, state, board));
        _fromLastLearn++;

        if (_fromLastLearn >= LearnBuffer) {
            
            double averageReward = _rewards.Average();
            File.AppendAllText(RewardAveragePath, averageReward.ToString() + Environment.NewLine);
            
            double[][] stateArray = _states.ToArray();
            double[] rewardsArray = _rewards.ToArray();
            double[][] probsArray = _probs.ToArray();
            int[] actionsArray = _actions.ToArray();
            
            double[] discountedRewards = new double[rewardsArray.Length];
            double runningReward = 0;
            for (int t = rewardsArray.Length - 1; t >= 0; t--) {
                runningReward = rewardsArray[t] + _gamma * runningReward;
                discountedRewards[t] = runningReward;
            }

            double[] values = stateArray.Select(s => _valueNet.Predict(s)[0]).ToArray();

            double[] advantages = new double[rewardsArray.Length];
            for (int i = 0; i < rewardsArray.Length; i++)
                advantages[i] = rewardsArray[i] - values[i];

            double mean = advantages.Average();
            double std = Math.Sqrt(advantages.Select(a => Math.Pow(a - mean, 2)).Average());
            if (std < 1e-8) std = 1e-8;
            advantages = advantages.Select(a => (a - mean) / std).ToArray();

            double[][] valueTargets = discountedRewards.Select(r => new double[] { r }).ToArray();
            _valueNet.Train(stateArray, valueTargets, 0.001); 

            _fromLastLearn = 0;
            _policyNet.TrainPPO(stateArray, actionsArray, advantages, probsArray, 0.5, 0.001);

            _states.Clear();
            _actions.Clear();
            _probs.Clear();
            _rewards.Clear();
        }

    return $"{move.plateId} {move.tileId} {move.bufferId}";
    }
    
    public string Place(Board board) {
        throw new NotImplementedException();
    }

    public int GetId() => _id;

    public void Result(Dictionary<int, int> result) {
        
    }
    
    private int[] EncodeMoves(Move[] moves) {
        List<int> result = new List<int>();
        foreach (Move move in moves) {
            result.Add(Trainer.EncodeMove(move));
        }
        return result.ToArray();
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
        JsonSaver.Save(_policyNet, NetworkFile);
        JsonSaver.Save(_valueNet, ValueNetwork);
    }
}