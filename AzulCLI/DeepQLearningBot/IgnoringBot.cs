using System.Transactions;
using Azul;
using SaveSystem;
using PPO;
namespace DeepQLearningBot;

public class IgnoringBot : IBot{
    
    public int Id { get; private set; }
    public string WorkingDirectory { get; private set; }
    
    private const string SettingFile = "IgnoringBot/DQNsettings.json";
    private const string ReplayBufferFile = "IgnoringBot/replayBuffer.json";
    private const string NetworkFile = "IgnoringBot/network.json";
    private DQNSetting _settings;
    private NeuralNetwork _policyNet;
    private NeuralNetwork _targetNet;
    private ReplayBuffer _replayBuffer;
    private Random _random;
    public bool SaveThis = false;
    private int _updatesPassed = 0;
    private int _rewardType;
    public IgnoringBot(int id, int rewardType, string workingDirectory = null) {
        
        workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        WorkingDirectory = workingDirectory;
        
        _rewardType = rewardType;
        
        string networkDir = PathCombiner(WorkingDirectory, "IgnoringBot");
        if (!Path.Exists(networkDir)) Directory.CreateDirectory(networkDir);
        
        _settings = JsonSaver.Load<DQNSetting>(PathCombiner(WorkingDirectory,SettingFile)) ?? 
                                               new DQNSetting(300, 59, 300, 32, 1, 0.95, 0.01, 0.9);
        
        _policyNet = JsonSaver.Load<NeuralNetwork>(PathCombiner(WorkingDirectory, NetworkFile)) ?? 
                                                   new NeuralNetwork(_settings.StateSize, 128, 128, _settings.ActionSize);

        _targetNet = _policyNet.Clone();
        
        _replayBuffer = new ReplayBuffer(_settings.ReplayBufferCapacity);

        this.Id = id;
        _random = new Random();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    public string DoMove(Board board) {
        double[] state = board.EncodeBoardState(Id);

        int bestAction;
        if (_random.NextDouble() < _settings.Epsilon) {
            bestAction = GetRandomValidAction(board);
        }
        else {
            double[] qValues = _policyNet.Predict(state);
            bestAction = GetBestValidAction(qValues, board); // Greedy action
        }
        
        if(!IsLegalAction(bestAction, board)) throw new ApplicationException("Invalid action");
        
        double reward = Trainer.CalculateReward(Trainer.DecodeAction(bestAction), state, Id, _rewardType);
        
        Logger.WriteLine($"Move: {DecodeAction(bestAction)} reward: {reward}");
        var nextState = Board.GetNextState(state, DecodeToMove(bestAction));

        _replayBuffer.Add(state, bestAction, reward, nextState, true);
        _settings.FromLastBatch++;
        if (_settings.FromLastBatch >= _settings.BatchSize) {
            _settings.FromLastBatch = 0;
            TrainFromReplayBuffer(board);
            _settings.Epsilon = Math.Max(_settings.EpsilonMin, _settings.Epsilon * _settings.EpsilonDecay);
        }

        return DecodeAction(bestAction);
    }

    public void Result(Dictionary<int, int> result) {
    }

    public void LoadNetwork(NeuralNetwork nc) {
        _policyNet = nc;
    }

    public NeuralNetwork GetNetwork() {
        return _policyNet;
    }

    private int GetRandomValidAction(Board board) {
        Move[] validMoves = board.GetValidMoves();
        if (validMoves.Length == 0)
            throw new Exception("No valid moves found!");
        int index = _random.Next(validMoves.Length);
        return EncodeAction(validMoves[index]);
    }

    public string Place(Board board) {

        return "-1";
    }
    
    private void TrainFromReplayBuffer(Board board) {
        // Sample a batch of experiences from the replay buffer
        var batch = _replayBuffer.Sample(_settings.BatchSize);
        var states = new double[_settings.BatchSize][];
        var nextStates = new double[_settings.BatchSize][];
        var rewards = new double[_settings.BatchSize];
        var actions = new int[_settings.BatchSize];


        for (int i = 0; i < _settings.BatchSize; i++)
        {
            var replay = batch[i];
            states[i] = replay.State;
            nextStates[i] = replay.NextState;
            rewards[i] = replay.Reward;
            actions[i] = replay.Action;
        }

        // Train the neural network on the batch
        _policyNet.Train(states, actions, rewards, nextStates, _targetNet, 0.001, 0.5);
        if (_updatesPassed >= _settings.BatchSize) {
            _updatesPassed = 0;
            _targetNet = _policyNet.Clone();
        }
    }
    
    private int GetBestValidAction(double[] qValues, Board board) {
        int bestAction = -1;
        int incorrectCount = 0;
        double bestValue = double.MinValue;
        
        for (int action = 0; action < qValues.Length; action++) {
            if (IsLegalAction(action, board) && qValues[action] > bestValue) {
                bestValue = qValues[action];
                bestAction = action;
            } 
        }
        
        for (int action = 0; action < qValues.Length; action++) {
            if (qValues[action] > bestValue) {
                incorrectCount++;
                Logger.WriteLine($"tried invalid move: {DecodeAction(action)} qValues: {qValues[action]}");
                _replayBuffer.Add(board.EncodeBoardState(Id), action, -10, board.EncodeBoardState(Id), false);
            }
        }

        if (bestAction == -1) throw new IllegalOptionException("no valid action"); //should be always false
        Logger.WriteLine($"Best action's Qvalue: {bestValue} after {incorrectCount} failed actions");
        return bestAction;
    }

    private bool IsLegalAction(int action, Board board) {
        if (action < 0) return false;
        Move move = DecodeToMove(action);
        return board.CanMove(move);
    }
    
    private Move DecodeToMove(int actionId) {
        int tileId = actionId / (10 * 6);
        int plate = (actionId % (10 * 6)) / 6;
        int buffer = (actionId % 6);
        return new Move(tileId, plate, buffer);
    }

    private int EncodeAction(Move move) {
        int actionId = move.BufferId;
        actionId += 60 * move.TileId;
        actionId += 6 * move.PlateId;
        return actionId;
    }
    
    private string DecodeAction(int actionId) {
        Move move = DecodeToMove(actionId);
        return move.ToString();
    }
    
    private double Max(double[] values) {
        double max = double.MinValue;
        foreach (var value in values)
            if (value > max)
                max = value;
        return max;
    }
    
    private double Min(double[] values) {
        double min = double.MaxValue;
        foreach (var value in values)
            if (value < min)
                min = value;
        return min;
    }

    private void OnProcessExit(object sender, EventArgs e) {
        //JsonSaver.Save(settings, settingFile);
        //JsonSaver.Save(replayBuffer, replayBufferFile);
        JsonSaver.Save(_targetNet, PathCombiner(WorkingDirectory, NetworkFile));    
    }
    
    private static string PathCombiner(string baseName, string fileName) {
        if (baseName[^1] != '/') baseName += '/';
        return baseName + fileName;
    }
}