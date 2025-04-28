using Azul;
using DeepQLearningBot;
using SaveSystem;


namespace PPO;

public class Bot : IBot {

    private const string PolicyNetwork = "PPO/policy_network.json";
    private const string ValueNetwork = "PPO/value_network.json";

    private const string RewardAveragePath = "reward_avg.txt";
    private const int LearnBuffer = 100;
    
    public int Id { get; private set; }
    public string WorkingDirectory { get; private set; }
    
    private readonly NeuralNetwork _policyNet;
    private readonly NeuralNetwork _valueNet;
    
    private readonly Random _random;
    private int _fromLastLearn = 0;

    private double _gamma = 0.99;
    
    static List<double[]> _states = new List<double[]>();
    static List<int> _actions = new List<int>();
    static List<double> _rewards = new List<double>();
    static List<double[]> _probs = new List<double[]>();
    
    private static List<double> _lastGameRewards = new List<double>();
    private int _rewardType;
    
    
    
    public Bot(int id, int rewardType, string workingDirectory = null) {

        workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        WorkingDirectory = workingDirectory;
        
        _rewardType = rewardType;

        string networkDir = PathCombiner(WorkingDirectory, "PPO");
        if (!Path.Exists(networkDir)) Directory.CreateDirectory(networkDir);
        
        _policyNet = JsonSaver.Load<NeuralNetwork>(PathCombiner(WorkingDirectory, PolicyNetwork)) ?? 
                     new NeuralNetwork(59, 256, 256, 300);
        
        _valueNet = JsonSaver.Load<NeuralNetwork>(PathCombiner(WorkingDirectory, ValueNetwork)) ??
                    new NeuralNetwork(59, 256, 256, 1);
        
        this.Id = id;
        _random = new Random();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit!;

    }

    public string DoMove(Board board) {
        var state = board.EncodeBoardState(Id);
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
        
        _states.Add(state);
        _actions.Add(Trainer.EncodeMove(move));
        _probs.Add(actionProbs);
        _lastGameRewards.Add(Trainer.CalculateReward(move, state, Id, _rewardType));

        return move.ToString();
    }
    
    public string Place(Board board) {
        throw new NotImplementedException();
    }
    
    public void Result(Dictionary<int, int> result) {
        int winner = 0;
        int winScore = 0;
        int myScore = 0;
        foreach(KeyValuePair<int, int> pair in result) {
            if (pair.Value > winScore) {
                winner = pair.Key;
            }
            if(pair.Key == Id) myScore = pair.Value;
        }

        if (winner == Id) {
            foreach (var reward in _lastGameRewards) {
                _rewards.Add(reward + 2 + myScore / 8);
                _fromLastLearn++;
            }
        }
        else {
            foreach (var reward in _lastGameRewards) {
                _rewards.Add(reward - 2 + myScore / 16);
                _fromLastLearn++;
            }
        }
        _lastGameRewards.Clear();
        if (_fromLastLearn >= LearnBuffer) Train();
    }

    private void Train() {
        double averageReward = _rewards.Average();
        File.AppendAllText(PathCombiner(WorkingDirectory, RewardAveragePath), averageReward.ToString() + Environment.NewLine);
            
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
        _valueNet.Train(stateArray, valueTargets, 0.0001); 

        _fromLastLearn = 0;
        _policyNet.TrainPPO(stateArray, actionsArray, advantages, probsArray, 0.4, 0.0001);

        _states.Clear();
        _actions.Clear();
        _probs.Clear();
        _rewards.Clear();
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
        JsonSaver.Save(_policyNet, PathCombiner(WorkingDirectory, PolicyNetwork));
        JsonSaver.Save(_valueNet, PathCombiner(WorkingDirectory, ValueNetwork));
    }
    
    private static string PathCombiner(string baseName, string fileName) {
        if (baseName[^1] != '/') baseName += '/';
        return baseName + fileName;
    }
}